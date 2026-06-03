using System.ComponentModel.DataAnnotations;

namespace bankconnector.moniepoint.Models
{
    public class TransactionStatusRequest
    {
        [Required]
        public string? TransactionReference { get; set; }

        public long Amount { get; set; }

        public string? AccountNumber { get; set; }

        public DateTime? TransactionDate { get; set; }
    }
}
