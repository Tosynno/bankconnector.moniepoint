using bankconnector.moniepoint.Interfaces;

namespace bankconnector.moniepoint.Encryption
{
    public interface IEncryptionServiceFactory
    {
        IEncryptionService GetService();
    }
}
