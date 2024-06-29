using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.MultiLanguage;
using PhoenixLib.ServiceBus;
using WingsAPI.Communication.Services.Messages;
using WingsEmu.Core.Extensions;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums.Chat;

namespace GameChannel.Consumers
{
    public class ServiceMaintenanceNotificationMessageConsumer : IMessageConsumer<ServiceMaintenanceNotificationMessage>
    {
        private readonly IGameLanguageService _languageService;
        private readonly ISessionManager _sessionManager;

        public ServiceMaintenanceNotificationMessageConsumer(IGameLanguageService languageService, ISessionManager sessionManager)
        {
            _languageService = languageService;
            _sessionManager = sessionManager;
        }

        public async Task HandleAsync(ServiceMaintenanceNotificationMessage notification, CancellationToken token)
        {
            GameDialogKey dialog;
            double? value;

            switch (notification.NotificationType)
            {
                case ServiceMaintenanceNotificationType.Rescheduled:
                case ServiceMaintenanceNotificationType.ScheduleWarning:
                    if (notification.TimeLeft.Hours > 0)
                    {
                        dialog = notification.NotificationType switch
                        {
                            ServiceMaintenanceNotificationType.Rescheduled => GameDialogKey.MAINTENANCE_SHOUTMESSAGE_NOTIFY_RESCHEDULED_HOURS,
                            ServiceMaintenanceNotificationType.ScheduleWarning => GameDialogKey.MAINTENANCE_SHOUTMESSAGE_NOTIFY_WARNING_HOURS
                        };
                        value = notification.TimeLeft.TotalHours;
                    }
                    else if (notification.TimeLeft.Minutes > 0)
                    {
                        dialog = notification.NotificationType switch
                        {
                            ServiceMaintenanceNotificationType.Rescheduled => GameDialogKey.MAINTENANCE_SHOUTMESSAGE_NOTIFY_RESCHEDULED_MINUTES,
                            ServiceMaintenanceNotificationType.ScheduleWarning => GameDialogKey.MAINTENANCE_SHOUTMESSAGE_NOTIFY_WARNING_MINUTES
                        };
                        value = notification.TimeLeft.TotalMinutes;
                    }
                    else
                    {
                        dialog = notification.NotificationType switch
                        {
                            ServiceMaintenanceNotificationType.Rescheduled => GameDialogKey.MAINTENANCE_SHOUTMESSAGE_NOTIFY_RESCHEDULED_SECONDS,
                            ServiceMaintenanceNotificationType.ScheduleWarning => GameDialogKey.MAINTENANCE_SHOUTMESSAGE_NOTIFY_WARNING_SECONDS
                        };
                        value = notification.TimeLeft.TotalSeconds;
                    }

                    break;
                case ServiceMaintenanceNotificationType.ScheduleStopped:
                    dialog = GameDialogKey.MAINTENANCE_SHOUTMESSAGE_NOTIFY_STOPPED;
                    value = null;
                    break;
                case ServiceMaintenanceNotificationType.Executed:
                case ServiceMaintenanceNotificationType.EmergencyExecuted:
                    dialog = GameDialogKey.MAINTENANCE_SHOUTMESSAGE_NOTIFY_EXECUTED;
                    value = null;
                    break;
                case ServiceMaintenanceNotificationType.Lifted:
                    dialog = GameDialogKey.MAINTENANCE_SHOUTMESSAGE_NOTIFY_LIFTED;
                    value = null;
                    break;
                default:
                    return;
            }

            Dictionary<RegionLanguageType, string> dictionary = new();
            int? roundedValue = value.HasValue ? (int)Math.Round(value.Value) : null;

            foreach (IClientSession session in _sessionManager.Sessions)
            {
                string message = dictionary.GetOrSetDefault(session.UserLanguage,
                    roundedValue.HasValue
                        ? _languageService.GetLanguageFormat(dialog, session.UserLanguage, roundedValue.ToString())
                        : _languageService.GetLanguage(dialog, session.UserLanguage));

                session.SendPacket(session.GenerateMsgPacket(message, MsgMessageType.MiddleYellow));
                session.SendPacket(session.GenerateSayPacket($"({session.GetLanguage(GameDialogKey.ADMIN_BROADCAST_CHATMESSAGE_SENDER)}): {message}", ChatMessageColorType.Yellow));
            }
        }
    }
}