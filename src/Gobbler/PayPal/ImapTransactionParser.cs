using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gobbler.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using NLog;

namespace Gobbler.PayPal
{
    /// <summary>
    /// Fetches PayPal transaction ids via imap
    /// </summary>
    internal class ImapTransactionParser
    {
        static readonly ILogger log = LogManager.GetCurrentClassLogger();

        ImapClient _imap;
        IMailFolder _inbox;

        public const string successLabel = "Donations/OK";
        public const string failureLabel = "Donations/Error";
        const string ppEmailTypeHeader = "X-Email-Type-Id";
        const string transPattern = @"[0-9A-Z]{15,20}";

        // PP1304 donations
        // PP341 payment (business/products)
        // PP1662 (money sent to you/non-business)
        readonly string[] _filterCodes = new string[] {
            "PP1662"
        };

        public ImapTransactionParser( ImapClient imap ) // testable if ImapClient replaced by IMailFolder
            : this( imap, null )
        {
        }

        public ImapTransactionParser( ImapClient imap, string[] filterCodes )
        {
            if ( !imap.IsConnected || !imap.IsAuthenticated ) {
                throw new ArgumentException( "imap client must be connected and authenticated" );
            }
            _imap = imap;
            _filterCodes = filterCodes ?? _filterCodes;
            OpenInbox();
        }

        void OpenInbox()
        {
            _inbox = _imap.Inbox;
            _inbox.Open( FolderAccess.ReadWrite );
        }

        /// <summary>
        /// Fetch unprocessed transaction ids from email messages
        /// </summary>
        public IList<MessageInfo> ParseTransaction()
        {
            var headers = new HashSet<string>( new[] { ppEmailTypeHeader } );
            var ids = new List<MessageInfo>();

            var q = SearchQuery.FromContains( "paypal.com" )
                .And( SearchQuery.Not( SearchQuery.HasGMailLabel( successLabel ) ) )
                .And( SearchQuery.Not( SearchQuery.HasGMailLabel( failureLabel ) ) );

            // Fetch messages metadata and filter out
            // messages without the required email type codes
            var msgs = _inbox.Fetch( _inbox.Search( q ), MessageSummaryItems.Full, headers );
            var filtered = msgs.Where( m => m.Headers.Contains( ppEmailTypeHeader )
                && IsCorrectCode( m.Headers[ppEmailTypeHeader] ) ).ToArray();

            log.Debug( "Found {0} unprocessed messages from paypal.com w/ filter codes: [ {1} ]",
                filtered.Length, string.Join( ", ", _filterCodes ) );

            // Download text body and parse transaction ids
            foreach ( var msg in filtered ) {
                TextPart textBody = _inbox.GetBodyPart( msg.UniqueId, msg.TextBody ) as TextPart;
                var id = ParseTransactionId( textBody.Text );

                if ( id == null ) {
                    log.Warn( "Failed to find any transaction id in message <{0}> with subject <{1}>",
                        msg.Envelope.MessageId, msg.Envelope.Subject );
                    continue;
                }
                log.Info( "Found transaction <{0}> in message <{1}>", id, msg.Envelope.MessageId );

                ids.Add( new MessageInfo {
                    TransactionId = id,
                    MessageId = msg.UniqueId.Id
                } );
            }

            return ids;
        }

        static public string ParseTransactionId( string text )
        {
            var match = Regex.Match( text, transPattern );
            if ( match.Success ) {
                return match.Value;
            }
            return null;
        }

        bool IsCorrectCode( string value )
        {
            foreach( var filter in _filterCodes ) {
                if ( value.Contains( filter ) ) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Mark an email message as processed by tagging it with a special label
        /// </summary>
        public static void MarkMessage( IMailFolder folder, UniqueId msgId, bool success )
        {
            folder.AddLabels( msgId, new string[] {
                success ? successLabel : failureLabel
            }, true );
        }
    }
}
