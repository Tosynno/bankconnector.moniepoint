using System.ComponentModel.DataAnnotations;

namespace bankconnector.moniepoint.Models
{
    public class NameEnquiryRequest
    {
        [Required]
        public string? AccountNumber { get; set; }

        public string? DestinationBankCode { get; set; }

        //[MaxLength(30)]
        public string? TransactionReference { get; set; }
    }
}
