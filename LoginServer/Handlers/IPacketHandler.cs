using System.Threading.Tasks;
using LoginServer.Network;
using WingsEmu.Packets;

namespace LoginServer.Handlers
{
    public interface IPacketHandler
    {
        public Task HandleAsync(LoginClientSession session, IPacket packet);
    }
}