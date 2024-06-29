using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.ServiceBus;
using WingsEmu.Game.Managers;
using WingsEmu.Plugins.DistributedGameEvents.PlayerEvents;

namespace GameChannel.Consumers
{
    public class PlayerDisconnectedChannelConsumer : IMessageConsumer<PlayerDisconnectedChannelMessage>
    {
        private readonly ISessionManager _sessionManager;

        public PlayerDisconnectedChannelConsumer(ISessionManager sessionManager) => _sessionManager = sessionManager;

        public async Task HandleAsync(PlayerDisconnectedChannelMessage e, CancellationToken cancellation) => _sessionManager.RemoveOnline(e.CharacterName, e.CharacterId);
    }
}