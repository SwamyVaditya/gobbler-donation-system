using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gobbler.Domain;
using PayPal.PayPalAPIInterfaceService.Model;

namespace Gobbler.Helpers
{
    public class MapHelper
    {
        public static Payment ToPayment( PaymentTransactionType tx )
        {
            var steamId = tx.PaymentItemInfo
                .PaymentItem.First()
                .Options.First( o => o.name == "SteamId" ).value;

            var payment = new Payment() {
                GrossAmount = decimal.Parse( tx.PaymentInfo.GrossAmount.value ),
                FeeAmount = decimal.Parse( tx.PaymentInfo.FeeAmount.value ),
                Currency = tx.PaymentInfo.GrossAmount.currencyID.Value,
                PayerName = tx.PayerInfo.Payer,
                ProductId = ProductType.Donation,
                PayedAt = DateTime.Parse( tx.PaymentInfo.PaymentDate ),
                TransactionId = tx.PaymentInfo.TransactionID,
                SteamId = steamId,
                Quantity = 1
            };

            return payment;
        }

        public static IList<Payment> ToPayments( IList<PaymentTransactionType> txs )
        {
            var list = new List<Payment>( txs.Count );
            foreach( var item in txs ) {
                list.Add( ToPayment( item ) );
            }
            return list;
        }
    }
}
