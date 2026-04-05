using zone.bankconnector.moniepoint.Utilities;

namespace zone.bankconnector.moniepoint.Interfaces
{
    
    public interface IEncryptionService
    {
        EncryptionMode Mode { get; }
        string EncryptToHex(string plainJson);
        string DecryptFromHex(string hexCipherData);
    }
}
