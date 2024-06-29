// WingsEmu
// 
// Developed by NosWings Team

using WingsEmu.Game.Networking;

namespace GameChannel.Network
{
    public interface IClientSessionFactory
    {
        IClientSession CreateSession(GameTcpServer session);
    }
}