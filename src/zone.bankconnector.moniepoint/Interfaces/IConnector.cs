using zone.bankconnector.moniepoint.Models;

namespace zone.bankconnector.moniepoint.Interfaces
{
    public interface IConnector
    {
        Task<GenericResponse<TransferResponse>> IntraBankAsync(TransferRequest request, CancellationToken ct = default);
        Task<GenericResponse<TransactionStatusResponse>> IntraBankStatusAsync(TransactionStatusRequest request, CancellationToken ct = default);
        Task<NameInquiryResponse> NameEnquiryAsync(NameEnquiryRequest request, CancellationToken ct = default);
    }
}
