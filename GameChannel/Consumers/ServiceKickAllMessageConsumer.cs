using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.ServiceBus;
using WingsAPI.Communication.Services.Messages;
using WingsEmu.DTOs.Account;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Health;

namespace GameChannel.Consumers
{
    public class ServiceKickAllMessageConsumer : IMessageConsumer<ServiceKickAllMessage>
    {
        private readonly IMaintenanceManager _maintenanceManager;
        private readonly ISessionManager _sessionManager;

        public ServiceKickAllMessageConsumer(ISessionManager sessionManager, IMaintenanceManager maintenanceManager)
        {
            _sessionManager = sessionManager;
            _maintenanceManager = maintenanceManager;
        }

        public async Task HandleAsync(ServiceKickAllMessage notification, CancellationToken token)
        {
            if (!notification.IsGlobal && notification.TargetedService != _maintenanceManager.ServiceName)
            {
                return;
            }

            List<IClientSession> sessionsToKick = SessionsToKick();
            if (sessionsToKick.Count < 1)
            {
                return;
            }

            foreach (IClientSession session in sessionsToKick)
            {
                session.ForceDisconnect();
            }
        }

        private List<IClientSession> SessionsToKick()
        {
            var list = new List<IClientSession>();

            foreach (IClientSession session in _sessionManager.Sessions)
            {
                if (session.Account.Authority >= AuthorityType.GameMaster)
                {
                    continue;
                }

                list.Add(session);
            }

            return list;
        }
    }
}