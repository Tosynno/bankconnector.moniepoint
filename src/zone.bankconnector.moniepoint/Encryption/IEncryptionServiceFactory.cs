using zone.bankconnector.moniepoint.Interfaces;

namespace zone.bankconnector.moniepoint.Encryption
{
    public interface IEncryptionServiceFactory
    {
        IEncryptionService GetService();
    }
}
