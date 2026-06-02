

using zone.bankconnector.moniepoint.Dtos;

namespace zone.bankconnector.moniepoint.Interfaces
{
    public interface IMonieTransferService
    {
        Task<NameEnquiryResponseDto> NameEnquiryAsync(
            NameEnquiryDto request,
            CancellationToken cancellationToken = default);

        Task<FundsTransferResponseDto> FundsTransferAsync(
            FundsTransferDto request,
            CancellationToken cancellationToken = default);

        Task<TransactionStatusQueryResponseDto> QueryTransactionStatusAsync(
            string uniqueReference,
            CancellationToken cancellationToken = default);

        Task<TransactionStatusQueryResponseDto> QueryTransactionStatusWithRetryAsync(
            string uniqueReference,
            CancellationToken cancellationToken = default);

        Task<InstitutionBalanceResponseDto> GetInstitutionBalanceAsync(
            string? type = null,
            CancellationToken cancellationToken = default);
    }
}
