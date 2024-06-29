using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.ServiceBus;
using WingsAPI.Communication.ServerApi;
using WingsEmu.Game.Managers;

namespace GameChannel.Consumers
{
    public class WorldServerShutdownConsumer : IMessageConsumer<WorldServerShutdownMessage>
    {
        private readonly IServerManager _serverManager;

        public WorldServerShutdownConsumer(IServerManager serverManager) => _serverManager = serverManager;

        public async Task HandleAsync(WorldServerShutdownMessage notification, CancellationToken token)
        {
            if (notification.ChannelId != _serverManager.ChannelId)
            {
                return;
            }

            _serverManager.Shutdown();
        }
    }
}