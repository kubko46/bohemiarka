// WingsEmu
// 
// Developed by NosWings Team

namespace LoginServer.Utils
{
    public interface ISpamProtector
    {
        bool CanConnect(string ipAddress);
    }
}