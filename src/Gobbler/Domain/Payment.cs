using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Gobbler.Stores;
using PayPal.PayPalAPIInterfaceService.Model;

namespace Gobbler.Domain
{
    [Table( PaymentStore.TableName )]
    public class Payment
    {
        [Key]
        public int? Id { get; set; }
        public string SteamId { get; set; }
        public ProductType ProductId { get; set; }
        public uint Quantity { get; set; }
        public string PayerName { get; set; }
        public string TransactionId { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal FeeAmount { get; set; }
        public CurrencyCodeType Currency { get; set; }
        public DateTime PayedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AppliedAt { get; set; }
    }
}
