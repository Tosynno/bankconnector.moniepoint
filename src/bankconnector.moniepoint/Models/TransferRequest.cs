using System.ComponentModel.DataAnnotations;

namespace bankconnector.moniepoint.Models
{
    public class TransferRequest
    {

        public string? FromAccount { get; set; }

        public string? FromAccountName { get; set; }

        public string? ToAccountName { get; set; }

        [Required(ErrorMessage = "ToAccount is required")]
        //[StringLength(10, MinimumLength = 10, ErrorMessage = "ToAccount must be 10 digits")]
        //[RegularExpression(@"^\d{10}$", ErrorMessage = "ToAccount must be numeric")]
        public string? ToAccount { get; set; }

        [Required(ErrorMessage = "AmountToDebit is required")]
        [Range(1, long.MaxValue, ErrorMessage = "Amount must be positive")]
        public string? AmountToDebit { get; set; }

        [StringLength(6, MinimumLength = 3, ErrorMessage = "DestinationBankCode must be 3-6 digits")]
        public string? DestinationBankCode { get; set; }

        public long FeeToCredit { get; set; }

        [Required(ErrorMessage = "TransactionReference is required")]
        [StringLength(100, ErrorMessage = "TransactionReference max 100 chars")]
        public string? TransactionReference { get; set; }

        public string? NameEnquiryID { get; set; }

        [StringLength(200, ErrorMessage = "Narration max 200 chars")]
        public string? Narration { get; set; }

        [Required(ErrorMessage = "CurrencyCode is required")]
        [StringLength(3, MinimumLength = 3)]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "CurrencyCode must be 3 uppercase letters (e.g. NGN)")]
        public string? CurrencyCode { get; set; }

        public string? FeeType { get; set; }

        public string? OriginatorBVN { get; set; }

        public string? BeneficiaryBVN { get; set; }

        public int? OriginatorKYCLevel { get; set; }

        public int? BeneficiaryKYCLevel { get; set; }

        public DateTime? TransactionDate { get; set; } = DateTime.UtcNow;
    }
}
