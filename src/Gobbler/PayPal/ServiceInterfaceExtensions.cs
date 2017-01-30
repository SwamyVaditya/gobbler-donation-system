using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayPal.PayPalAPIInterfaceService;
using PayPal.PayPalAPIInterfaceService.Model;

namespace Gobbler.Helpers
{
    public static class ServiceInterfaceExtensions
    {
        /// <summary>
        /// Attempts to overcome the max. item limit (100) for transaction searching
        /// by reissuing a request using the latest timestamp in the previous result
        /// </summary>
        public static TransactionSearchResponseType SearchTransactions( this PayPalAPIInterfaceServiceService client, TransactionSearchRequestType req )
        {
            TransactionSearchResponseType response = null, 
                first = null;
            string lastTimestamp = req.StartDate;
            int counter = 0;

            // attempting to paginate through transactions
            // see https://developer.paypal.com/webapps/developer/docs/classic/express-checkout/ht_searchRetrieveTransactionData-curl-etc/
            do {
                // Set request start date to the latest transaction timestamp
                // from the previous result set
                req.StartDate = lastTimestamp;

                response = client.TransactionSearch( new TransactionSearchReq {
                    TransactionSearchRequest = req
                } );
                // Save the first response object
                // Use it to dump the transaction sets into it then return it
                if ( counter == 0 ) { 
                    first = response;
                }
                // Save the latest transaction timestamp 
                // (used for pagination)
                if ( response.PaymentTransactions.Count > 0 ) {
                    lastTimestamp = response.PaymentTransactions.Max( r => DateTime.Parse( r.Timestamp ) )
                        .ToString( "o" );
                }
                // Dump subsequent transactions into the first response object
                if ( counter > 0 ) {
                    first.PaymentTransactions.AddRange( response.PaymentTransactions );
                }
                counter++;
            } while ( response.Ack == AckCodeType.SUCCESSWITHWARNING );

            return first;
        }
    }
}
