using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.ServiceBus;
using WingsAPI.Communication.Player;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Plugins.DistributedGameEvents.PlayerEvents;

namespace GameChannel.Consumers
{
    public class PlayerConnectedChannelGameConsumer : IMessageConsumer<PlayerConnectedOnChannelMessage>
    {
        private readonly IServerManager _serverManager;
        private readonly ISessionManager _sessionManager;

        public PlayerConnectedChannelGameConsumer(ISessionManager sessionManager, IServerManager serverManager)
        {
            _sessionManager = sessionManager;
            _serverManager = serverManager;
        }

        public async Task HandleAsync(PlayerConnectedOnChannelMessage e, CancellationToken cancellation)
        {
            IClientSession session = _sessionManager.GetSessionByCharacterId(e.CharacterId);
            if (session != null)
            {
                await session.NotifyStrangeBehavior(StrangeBehaviorSeverity.DANGER, $"Looks like {e.CharacterName} was connected from {e.ChannelId} while being on {_serverManager.ChannelId}");
                session.ForceDisconnect();
                return;
            }

            _sessionManager.AddOnline(new ClusterCharacterInfo
            {
                Id = e.CharacterId,
                Class = e.Class,
                Gender = e.Gender,
                Level = e.Level,
                Name = e.CharacterName,
                ChannelId = (byte?)e.ChannelId,
                HeroLevel = e.HeroLevel,
                HardwareId = e.HardwareId
            });
        }
    }
}