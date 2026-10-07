using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mini_Tax_Payment_System;

internal class Models
{
    public record Sales
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public bool IsTaxPaid { get; set; }
    }

    public record TaxPayments
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
    }
}
