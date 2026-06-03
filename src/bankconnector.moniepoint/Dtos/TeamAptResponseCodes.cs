namespace bankconnector.moniepoint.Dtos
{
    public static class TeamAptResponseCodes
    {
        public const string Approved                   = "00";
        public const string DoNotHonor                 = "05";
        public const string DormantAccount             = "06";
        public const string InvalidAccount             = "07";
        public const string AccountNameMismatch        = "08";
        public const string Pending                    = "09";
        public const string InvalidTransaction         = "12";
        public const string InvalidAmount              = "13";
        public const string InvalidUniqueReference     = "15";
        public const string UnknownBeneficiaryBankCode = "16";
        public const string UnableToLocateRecord       = "25";
        public const string DuplicateRecord            = "26";
        public const string FormatError                = "30";
        public const string TransactionNotPermitted    = "57";
        public const string TransferLimitExceeded      = "61";
        public const string SecurityViolation          = "63";
        public const string BeneficiaryBankNotAvailable= "91";
        public const string RoutingError               = "92";
        public const string SystemMalfunction          = "96";
        public const string Timeout                    = "97";

        private static readonly Dictionary<string, string> Descriptions = new()
        {
            [Approved]                   = "Approved or completed successfully",
            [DoNotHonor]                 = "Do not honor",
            [DormantAccount]             = "Dormant account",
            [InvalidAccount]             = "Invalid account",
            [AccountNameMismatch]        = "Account name mismatch",
            [Pending]                    = "Request pending (non-final – re-query)",
            [InvalidTransaction]         = "Invalid transaction",
            [InvalidAmount]              = "Invalid amount",
            [InvalidUniqueReference]     = "Invalid unique reference",
            [UnknownBeneficiaryBankCode] = "Unknown beneficiary bank code",
            [UnableToLocateRecord]       = "Unable to locate record",
            [DuplicateRecord]            = "Duplicate record",
            [FormatError]                = "Format error",
            [TransactionNotPermitted]    = "Transaction not permitted to sender",
            [TransferLimitExceeded]      = "Transfer limit exceeded",
            [SecurityViolation]          = "Security violation",
            [BeneficiaryBankNotAvailable]= "Beneficiary bank not available",
            [RoutingError]               = "Routing error",
            [SystemMalfunction]          = "System malfunction",
            [Timeout]                    = "Timeout (non-final – re-query)",
        };

        public static string GetDescription(string code) =>
            Descriptions.TryGetValue(code, out var d) ? d : $"Unknown code: {code}";

        public static bool IsFinal(string code) => code != Pending && code != Timeout;
    }
}
