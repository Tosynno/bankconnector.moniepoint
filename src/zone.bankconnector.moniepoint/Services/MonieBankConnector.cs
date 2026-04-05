using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Ocsp;
using zone.bankconnector.moniepoint.Dtos;
using zone.bankconnector.moniepoint.Interfaces;
using zone.bankconnector.moniepoint.Models;
using zone.bankconnector.moniepoint.Utilities;

namespace zone.bankconnector.moniepoint.Services
{
    public class MonieBankConnector(IMonieTransferService monieTransferService, ILogger<MonieBankConnector> logger, IOptions<TeamAptOptions> aptOptions) : IConnector
    {
        private readonly IMonieTransferService _monieTransferService = monieTransferService;
        private readonly ILogger<MonieBankConnector> _logger = logger;
        private readonly TeamAptOptions _aptOptions = aptOptions.Value;
        public async Task<GenericResponse<TransferResponse>> IntraBankAsync(TransferRequest request, CancellationToken ct = default)
        {
            GenericResponse<TransferResponse> response = new GenericResponse<TransferResponse>();
            FundsTransferDto req = new()
            {
                UniqueReference = request.TransactionReference!,
                DestinationInstitutionCode = request.DestinationBankCode!,
                Amount = request.AmountToDebit.ToString()!,
                NameEnquiryReference = request.NameEnquiryID!,
                BeneficiaryKycLevel = request.BeneficiaryKYCLevel.ToString()!,
                BeneficiaryBankVerificationNumber = request.BeneficiaryBVN!,
                OriginatorBankVerificationNumber = request.OriginatorBVN!,
                PaymentReference = request.TransactionReference!,
                Narration = request.Narration!,
                BeneficiaryAccountNumber = request.ToAccount!,
                BeneficiaryAccountName = request.ToAccountName!,
                OriginatorAccountName = _aptOptions.GLAccountName!,
                OriginatorAccountNumber = _aptOptions.GLAccountNumber!,
                OriginatorKycLevel = _aptOptions.GLKycLevel!
            };

            var res = await _monieTransferService.FundsTransferAsync(req, ct);
            response.ResponseCode = res.ResponseCode;
            response.Data.ResponseCode = res.ResponseCode;
            response.Data.Status = res.ResponseCode;
            response.Data.TransactionReference = res.PaymentReference;
            response.Data.TransactionDateTime = DateTime.Now;
            return response;
        }

        public async Task<GenericResponse<TransactionStatusResponse>> IntraBankStatusAsync(TransactionStatusRequest request, CancellationToken ct = default)
        {
            GenericResponse<TransactionStatusResponse> response = new GenericResponse<TransactionStatusResponse>();

            var res = await _monieTransferService.QueryTransactionStatusAsync(request.TransactionReference!, ct);
            response.ResponseCode = res.ResponseCode;
            response.Data.ResponseCode= res.ResponseCode;
            response.Data.Status = res.ResponseCode;
            return response;
        }

        public async Task<NameInquiryResponse> NameEnquiryAsync(NameEnquiryRequest request, CancellationToken ct = default)
        {
            NameInquiryResponse response = new NameInquiryResponse();
            NameEnquiryDto req = new NameEnquiryDto();
            req.UniqueReference = request.TransactionReference!;
            req.BeneficiaryAccountNumber = request.AccountNumber!;
            req.DestinationInstitutionCode = request.DestinationBankCode!;

            var res = await _monieTransferService.NameEnquiryAsync(req, ct);

            response.ResponseCode = res.ResponseCode;
            response.AccountName = res.BeneficiaryAccountName;
            response.AccountNumber = res.BeneficiaryAccountNumber;
            response.BVN = res.BeneficiaryBankVerificationNumber;
            response.KYCLevel = Convert.ToInt32(res.BeneficiaryKycLevel);

            return response;
        }
    }
}
