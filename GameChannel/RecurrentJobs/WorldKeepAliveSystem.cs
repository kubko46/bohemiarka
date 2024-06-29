// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using PhoenixLib.Logging;
using WingsAPI.Communication.ServerApi;
using WingsAPI.Communication.ServerApi.Protocol;
using WingsEmu.Game.Managers;

namespace GameChannel.RecurrentJobs
{
    public class WorldKeepAliveSystem : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
        private readonly SerializableGameServer _gameInfos;
        private readonly IServerApiService _serverApiService;
        private readonly ISessionManager _sessionManager;

        public WorldKeepAliveSystem(IServerApiService serverApiService, SerializableGameServer gameInfos, ISessionManager sessionManager)
        {
            _serverApiService = serverApiService;
            _gameInfos = gameInfos;
            _sessionManager = sessionManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Info("[GAME_PULSE_SYSTEM] Starting...");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Log.Debug("[GAME_PULSE_SYSTEM] Pulsing...");
                    await _serverApiService.PulseWorldServer(new PulseWorldServerRequest
                    {
                        ChannelId = _gameInfos.ChannelId,
                        SessionsCount = _sessionManager.SessionsCount
                    });
                }
                catch (Exception e)
                {
                    Log.Error("[GAME_PULSE_SYSTEM] Pulse error", e);
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
    }
}