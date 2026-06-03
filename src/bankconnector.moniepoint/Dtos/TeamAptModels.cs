using System.Text.Json.Serialization;

namespace bankconnector.moniepoint.Dtos
{

    public class NameEnquiryDto
    {
        [JsonPropertyName("uniqueReference")]
        public string UniqueReference { get; set; } = string.Empty;

        [JsonPropertyName("destinationInstitutionCode")]
        public string DestinationInstitutionCode { get; set; } = string.Empty;

        [JsonPropertyName("beneficiaryAccountNumber")]
        public string BeneficiaryAccountNumber { get; set; } = string.Empty;
    }

    public class NameEnquiryResponseDto
    {
        [JsonPropertyName("uniqueReference")]
        public string UniqueReference { get; set; } = string.Empty;

        [JsonPropertyName("destinationInstitutionCode")]
        public string DestinationInstitutionCode { get; set; } = string.Empty;

        [JsonPropertyName("beneficiaryKycLevel")]
        public string BeneficiaryKycLevel { get; set; } = string.Empty;

        [JsonPropertyName("beneficiaryBankVerificationNumber")]
        public string BeneficiaryBankVerificationNumber { get; set; } = string.Empty;

        [JsonPropertyName("responseCode")]
        public string ResponseCode { get; set; } = string.Empty;

        [JsonPropertyName("beneficiaryAccountNumber")]
        public string BeneficiaryAccountNumber { get; set; } = string.Empty;

        [JsonPropertyName("beneficiaryAccountName")]
        public string BeneficiaryAccountName { get; set; } = string.Empty;

        [JsonIgnore]
        public bool IsSuccessful => ResponseCode == TeamAptResponseCodes.Approved;
    }

    public class FundsTransferDto
    {
        [JsonPropertyName("uniqueReference")]
        public string UniqueReference { get; set; } = string.Empty;

        [JsonPropertyName("destinationInstitutionCode")]
        public string DestinationInstitutionCode { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonPropertyName("nameEnquiryReference")]
        public string NameEnquiryReference { get; set; } = string.Empty;

        [JsonPropertyName("beneficiaryKycLevel")]
        public string BeneficiaryKycLevel { get; set; } = string.Empty;

        [JsonPropertyName("beneficiaryBankVerificationNumber")]
        public string BeneficiaryBankVerificationNumber { get; set; } = string.Empty;

        [JsonPropertyName("originatorBankVerificationNumber")]
        public string OriginatorBankVerificationNumber { get; set; } = string.Empty;

        [JsonPropertyName("paymentReference")]
        public string PaymentReference { get; set; } = string.Empty;

        [JsonPropertyName("narration")]
        public string Narration { get; set; } = string.Empty;

        [JsonPropertyName("beneficiaryAccountNumber")]
        public string BeneficiaryAccountNumber { get; set; } = string.Empty;

        [JsonPropertyName("beneficiaryAccountName")]
        public string BeneficiaryAccountName { get; set; } = string.Empty;

        [JsonPropertyName("originatorAccountName")]
        public string OriginatorAccountName { get; set; } = string.Empty;

        [JsonPropertyName("originatorAccountNumber")]
        public string OriginatorAccountNumber { get; set; } = string.Empty;

        [JsonPropertyName("originatorKycLevel")]
        public string OriginatorKycLevel { get; set; } = string.Empty;
    }

    public class FundsTransferResponseDto
    {
        [JsonPropertyName("uniqueReference")]
        public string UniqueReference { get; set; } = string.Empty;

        [JsonPropertyName("destinationInstitutionCode")]
        public string DestinationInstitutionCode { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonPropertyName("nameEnquiryReference")]
        public string NameEnquiryReference { get; set; } = string.Empty;

        [JsonPropertyName("beneficiaryKycLevel")]
        public string BeneficiaryKycLevel { get; set; } = string.Empty;

        [JsonPropertyName("beneficiaryBankVerificationNumber")]
        public string BeneficiaryBankVerificationNumber { get; set; } = string.Empty;

        [JsonPropertyName("originatorBankVerificationNumber")]
        public string OriginatorBankVerificationNumber { get; set; } = string.Empty;

        [JsonPropertyName("paymentReference")]
        public string PaymentReference { get; set; } = string.Empty;

        [JsonPropertyName("responseCode")]
        public string ResponseCode { get; set; } = string.Empty;

        [JsonPropertyName("narration")]
        public string Narration { get; set; } = string.Empty;

        [JsonPropertyName("beneficiaryAccountNumber")]
        public string BeneficiaryAccountNumber { get; set; } = string.Empty;

        [JsonPropertyName("beneficiaryAccountName")]
        public string BeneficiaryAccountName { get; set; } = string.Empty;

        [JsonPropertyName("originatorAccountName")]
        public string OriginatorAccountName { get; set; } = string.Empty;

        [JsonPropertyName("originatorAccountNumber")]
        public string OriginatorAccountNumber { get; set; } = string.Empty;

        [JsonPropertyName("originatorKycLevel")]
        public string OriginatorKycLevel { get; set; } = string.Empty;

        [JsonIgnore]
        public bool IsSuccessful => ResponseCode == TeamAptResponseCodes.Approved;

        [JsonIgnore]
        public bool IsPending =>
            ResponseCode == TeamAptResponseCodes.Pending ||
            ResponseCode == TeamAptResponseCodes.Timeout;
    }

    public class TransactionStatusQueryDto
    {
        [JsonPropertyName("uniqueReference")]
        public string UniqueReference { get; set; } = string.Empty;
    }

    public class TransactionStatusQueryResponseDto
    {
        [JsonPropertyName("uniqueReference")]
        public string UniqueReference { get; set; } = string.Empty;

        [JsonPropertyName("responseCode")]
        public string ResponseCode { get; set; } = string.Empty;

        [JsonIgnore] public bool IsSuccessful => ResponseCode == TeamAptResponseCodes.Approved;
        [JsonIgnore] public bool IsPending => ResponseCode == TeamAptResponseCodes.Pending || ResponseCode == TeamAptResponseCodes.Timeout;
        [JsonIgnore] public bool IsFinal => !IsPending;
    }


    public class InstitutionBalanceDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class InstitutionBalanceResponseDto
    {
        [JsonPropertyName("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonPropertyName("responseCode")]
        public string ResponseCode { get; set; } = string.Empty;

        [JsonIgnore] public bool IsSuccessful => ResponseCode == TeamAptResponseCodes.Approved;
        [JsonIgnore] public decimal AmountDecimal => decimal.TryParse(Amount, out var v) ? v : 0m;
    }


    public class EncryptedRequestEnvelope
    {
        [JsonPropertyName("request")]
        public string Request { get; set; } = string.Empty;
    }

    public class EncryptedResponseEnvelope
    {
        [JsonPropertyName("statusCode")]
        public string StatusCode { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; }
    }


    public class ApiErrorDto
    {
        [JsonPropertyName("statusMessage")]
        public string? StatusMessage { get; set; }

        [JsonPropertyName("responseCode")]
        public string? ResponseCode { get; set; }
    }
}
