using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.ServiceBus;
using WingsAPI.Communication.Punishment;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;

namespace GameChannel.Consumers
{
    public class PlayerKickConsumer : IMessageConsumer<PlayerKickMessage>
    {
        private readonly ISessionManager _sessionManager;

        public PlayerKickConsumer(ISessionManager sessionManager) => _sessionManager = sessionManager;

        public async Task HandleAsync(PlayerKickMessage notification, CancellationToken token)
        {
            long? playerId = notification.PlayerId;
            string playerName = notification.PlayerName;

            if (playerId.HasValue)
            {
                IClientSession sessionById = _sessionManager.GetSessionByCharacterId(playerId.Value);
                sessionById?.ForceDisconnect();
                return;
            }

            if (string.IsNullOrEmpty(playerName))
            {
                return;
            }

            IClientSession sessionByName = _sessionManager.GetSessionByCharacterName(playerName);
            sessionByName?.ForceDisconnect();
        }
    }
}