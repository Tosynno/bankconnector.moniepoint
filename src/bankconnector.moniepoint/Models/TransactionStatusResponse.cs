namespace bankconnector.moniepoint.Models
{
    public class TransactionStatusResponse : BaseResponse
    {
        public string? TransactionReference { get; set; }

        public DateTime? TransactionDateTime { get; set; }

        public string? Status { get; set; }
    }
}
