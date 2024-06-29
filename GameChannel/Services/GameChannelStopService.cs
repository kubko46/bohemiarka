// WingsEmu
// 
// Developed by NosWings Team

using System.Threading.Tasks;
using WingsAPI.Communication.ServerApi;
using WingsAPI.Communication.ServerApi.Protocol;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Managers;
using WingsEmu.Packets.Enums.Chat;

namespace GameChannel.Services
{
    public class GameChannelStopService
    {
        private readonly IGameLanguageService _gameLanguage;
        private readonly SerializableGameServer _gameServer;
        private readonly IServerApiService _serverApiService;
        private readonly IServerManager _serverManager;
        private readonly ISessionManager _sessionManager;

        public GameChannelStopService(ISessionManager sessionManager, IServerApiService serverApiService, IServerManager serverManager, SerializableGameServer gameServer,
            IGameLanguageService gameLanguage)
        {
            _sessionManager = sessionManager;
            _serverApiService = serverApiService;
            _serverManager = serverManager;
            _gameServer = gameServer;
            _gameLanguage = gameLanguage;
        }

        public async Task StopAsync()
        {
            await _serverApiService.UnregisterWorldServer(new UnregisterWorldServerRequest
            {
                ChannelId = _gameServer.ChannelId
            });

            _serverManager.PutIdle();
            await _sessionManager.BroadcastAsync(async session =>
            {
                string message = _gameLanguage.GetLanguageFormat(GameDialogKey.INFORMATION_SHOUTMESSAGE_SHUTDOWN_SEC, session.UserLanguage, 15);
                return session.GenerateMsgPacket(message, MsgMessageType.MiddleYellow);
            });
            await _sessionManager.DisconnectAllAsync();
            await Task.Delay(15000);
        }
    }
}