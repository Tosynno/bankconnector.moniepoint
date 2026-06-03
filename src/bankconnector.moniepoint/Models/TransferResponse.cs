namespace bankconnector.moniepoint.Models
{
    public class TransferResponse : BaseResponse
    {
        public string? TransactionReference { get; set; }

        public string? Status { get; set; }

        public DateTime? TransactionDateTime { get; set; }
    }
}
