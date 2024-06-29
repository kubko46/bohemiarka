using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Logging;
using PhoenixLib.ServiceBus;
using WingsEmu.Game.Managers;
using WingsEmu.Plugins.DistributedGameEvents.PlayerEvents;

namespace GameChannel.Consumers
{
    public class KickAccountConsumer : IMessageConsumer<KickAccountMessage>
    {
        private readonly ISessionManager _sessionManager;

        public KickAccountConsumer(ISessionManager sessionManager) => _sessionManager = sessionManager;

        public async Task HandleAsync(KickAccountMessage notification, CancellationToken token)
        {
            Log.Warn($"[NOTIF_KICK] {notification.AccountId} is supposed to be kicked");

            await _sessionManager.KickAsync(notification.AccountId);
        }
    }
}