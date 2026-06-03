using bankconnector.moniepoint.Utilities;

namespace bankconnector.moniepoint.Interfaces
{
    
    public interface IEncryptionService
    {
        EncryptionMode Mode { get; }
        string EncryptToHex(string plainJson);
        string DecryptFromHex(string hexCipherData);
    }
}
