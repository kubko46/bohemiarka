// WingsEmu
// 
// Developed by NosWings Team

using System.Threading.Tasks;
using LoginServer.Network;
using WingsEmu.Packets;

namespace LoginServer.Handlers
{
    public abstract class GenericLoginPacketHandlerBase<T> : IPacketHandler where T : IPacket
    {
        public async Task HandleAsync(LoginClientSession session, IPacket packet)
        {
            if (packet is T typedPacket)
            {
                await HandlePacketAsync(session, typedPacket);
            }
        }

        public void Handle(LoginClientSession session, IPacket packet)
        {
            HandleAsync(session, packet).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        protected abstract Task HandlePacketAsync(LoginClientSession session, T packet);
    }
}