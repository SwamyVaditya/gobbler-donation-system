using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gobbler.Domain;
using Gobbler.Helpers;
using Gobbler.Models;
using Gobbler.PayPal;
using Gobbler.Stores;
using MailKit;
using MailKit.Net.Imap;
using MySql.Data.MySqlClient;
using NLog;
using PayPal.PayPalAPIInterfaceService;
using PayPal.PayPalAPIInterfaceService.Model;

namespace Gobbler
{
    internal class ProcessDonationsCmd
    {
        static readonly ILogger log = LogManager.GetCurrentClassLogger();

        PayPalAPIInterfaceServiceService _paypal;
        IDbConnection _db;
        PaymentStore _payments;
        AppConfig _cfg;

        public ProcessDonationsCmd( AppConfig cfg )
        {
            _paypal = CreatePayPalClient( cfg.PayPal );
            _db = CreateMySqlConnection( cfg.MySql.ConnectionString );
            _payments = new PaymentStore( _db );
            _cfg = cfg;
        }

        public void Execute()
        {
            var messages = ParseTransactionsFromImap();
            //var transactions = RetrieveTransactionDetails( new List<MessageInfo> {
            //    new MessageInfo {
            //        TransactionId = "3LA94787A4578660U" // "66P43347986772714"
            //    }
            //} );
            var transactions = RetrieveTransactionDetails( messages );
            var validated = ValidateTransactionDetails( transactions );
            var saved = SaveToDatabase( MapHelper.ToPayments( validated ) );
            MarkMessageAsProcessed( saved, messages );
        }

        void MarkMessageAsProcessed( IList<Payment> saved, IList<MessageInfo> messages )
        {
            if ( messages.Count == 0 ) {
                return;
            }
            var index = new HashSet<string>( saved.Select( o => o.TransactionId ) );

            using ( var imap = CreateImapConnection( _cfg ) ) {
                imap.Inbox.Open( FolderAccess.ReadWrite );

                foreach ( var msg in messages ) {
                    var uniqueId = new UniqueId( msg.MessageId );
                    // Payment has been processed and saved successfully
                    // Mark as success
                    if ( index.Contains( msg.TransactionId ) ) {
                        ImapTransactionParser.MarkMessage( imap.Inbox, uniqueId, true );
                    }
                    // There was a problem somewhere
                    // Mark as failure
                    else {
                        log.Warn( "Marking message {0} and transaction {1} as failed", msg.MessageId, msg.TransactionId );
                        ImapTransactionParser.MarkMessage( imap.Inbox, uniqueId, false );
                    }
                }
            }
        }

        /// <summary>
        /// Save `Payment` objects to the database
        /// </summary>
        /// <returns>
        /// Returns saved objects
        /// </returns>
        IList<Payment> SaveToDatabase( IList<Payment> payments )
        {
            var added = new List<Payment>();

            if ( payments.Count == 0 ) {
                return added;
            }
            log.Debug( "Saving {0} payment(s) to the database", payments.Count );

            var tx = _db.BeginTransaction();

            foreach ( var payment in payments ) {
                try {
                    _payments.Add( payment );
                    log.Debug( "Staging payment {0} for {1} with amount {2} {3} to database",
                        payment.TransactionId, payment.PayerName, payment.Currency, payment.GrossAmount );
                    added.Add( payment );
                }
                catch ( MySqlException ex ) {
                    log.Error( $"Failed to save payment w/ transaction {payment.TransactionId} to database. Reason: {ex.Message}" );
                }
            }

            tx.Commit();

            log.Debug( "Staged changes committed successfully." );

            return added;
        }

        /// <summary>
        /// Validate given transaction details (payment status, product id, steam id etc) 
        /// and filter out invalid entries
        /// </summary>
        IList<PaymentTransactionType> ValidateTransactionDetails( IList<PaymentTransactionType> transactions )
        {
            var verified = new List<PaymentTransactionType>( transactions.Count );

            foreach ( var tx in transactions ) {
                // Payment must be completed
                if ( tx.PaymentInfo.PaymentStatus != PaymentStatusCodeType.COMPLETED ) {
                    log.Warn( "Payment status for {0} is <{1}>", tx.PaymentInfo.TransactionID,
                        tx.PaymentInfo.PaymentStatus );
                    continue;
                }
                // A product must be present denoting the payment (not just simply sending money)
                var product = tx.PaymentItemInfo.PaymentItem?.FirstOrDefault();
                if ( product == null ) {
                    log.Warn( "Missing product for {0}", tx.PaymentInfo.TransactionID );
                    continue;
                }
                // Lastly a valid SteamId is required for matching with the player
                var steamId = product.Options.FirstOrDefault( o => o.name == "SteamId" )?.value;
                if ( steamId == null || !SteamHelper.IsSteamIdValid( steamId.Trim() ) ) {
                    log.Warn( "Missing or invalid SteamID ({0}) for {1}", steamId ?? "empty", tx.PaymentInfo.TransactionID );
                    continue;
                }
                verified.Add( tx );
            }

            return verified;
        }

        /// <summary>
        /// Parse transaction IDs from email notifications sent
        /// by PayPal
        /// </summary>
        IList<MessageInfo> ParseTransactionsFromImap()
        {
            log.Info( "Connecting to IMAP account <{0}>...", _cfg.Gmail.Username );

            IList<MessageInfo> list = new List<MessageInfo>();

            using ( var imap = CreateImapConnection( _cfg ) ) {
                var parser = new ImapTransactionParser( imap, _cfg.TransactionParser.FilterCodes );
                list = parser.ParseTransaction();

                log.Debug( "Successfuly parsed {0} transaction ids.", list.Count );
            }

            return list;
        }

        /// <summary>
        /// Retrieve transaction details from PayPal for the given transaction ids
        /// </summary>
        IList<PaymentTransactionType> RetrieveTransactionDetails( IList<MessageInfo> transactions )
        {
            var verified = new List<PaymentTransactionType>();

            foreach ( var tx in transactions ) {
                var result = _paypal.GetTransactionDetails( new GetTransactionDetailsReq {
                    GetTransactionDetailsRequest = new GetTransactionDetailsRequestType {
                        TransactionID = tx.TransactionId
                    }
                } );
                if ( result.Ack != AckCodeType.SUCCESS ) {
                    var error = result.Errors.First();
                    log.Warn( "Error when getting transaction details for <{0}> with code {1} and message: '{2}'",
                        tx.TransactionId, error.ErrorCode, error.LongMessage );
                    continue;
                }
                verified.Add( result.PaymentTransactionDetails );
            }

            return verified;
        }

        static ImapClient CreateImapConnection( AppConfig cfg )
        {
            var imap = new ImapClient();
            imap.Connect( cfg.Gmail.Host, cfg.Gmail.Port, MailKit.Security.SecureSocketOptions.Auto );
            imap.Authenticate( cfg.Gmail.Username, cfg.Gmail.Password );

            return imap;
        }

        static IDbConnection CreateMySqlConnection( string connectionString )
        {
            var connection = new MySqlConnection( connectionString );
            connection.Open(); // throws if database doesn't exist which is good

            // Validate schema sort of
            if ( !PaymentStore.ValidateTable( connection ) ) {
                throw new DataException( "Invalid schema detected for payments table" );
            }

            return connection;
        }

        static PayPalAPIInterfaceServiceService CreatePayPalClient( AppConfig.PayPalSettings cfg )
        {
            var config = new Dictionary<string, string> {
                ["mode"] = cfg.Mode,
                ["account1.apiUsername"] = cfg.Username,
                ["account1.apiPassword"] = cfg.Password,
                ["account1.apiSignature"] = cfg.Signature
            };
            return new PayPalAPIInterfaceServiceService( config );
        }
    }
}
