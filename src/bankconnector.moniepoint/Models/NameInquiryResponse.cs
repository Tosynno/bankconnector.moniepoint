namespace bankconnector.moniepoint.Models
{
    public class NameInquiryResponse : BaseResponse
    {
        public string? AccountName { get; set; }

        public string? AccountNumber { get; set; }

        public string? Type { get; set; }

        public string? BVN { get; set; }

        public string? Status { get; set; }

        public long AvailableBalance { get; set; }

        public string? AccountCurrency { get; set; }

        public int? ChannelCode { get; set; }

        public int? KYCLevel { get; set; }
        public string? TransactionReference { get; set; }
    }
}
