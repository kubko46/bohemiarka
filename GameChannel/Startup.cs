using GameChannel.Consumers;
using GameChannel.Network;
using GameChannel.RecurrentJobs;
using GameChannel.Services;
using GameChannel.Ticks;
using GameChannel.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PhoenixLib.Configuration;
using PhoenixLib.DAL;
using PhoenixLib.DAL.Redis;
using PhoenixLib.Events;
using PhoenixLib.Logging;
using PhoenixLib.Scheduler.ReactiveX;
using PhoenixLib.ServiceBus.Extensions;
using Plugin.Act4;
using Plugin.CoreImpl;
using Plugin.FamilyImpl;
using Plugin.PlayerLogs;
using Plugin.QuestImpl;
using Plugin.Raids;
using Plugin.RainbowBattle;
using Plugin.ResourceLoader;
using Plugin.TimeSpaces;
using WingsAPI.Communication.Punishment;
using WingsAPI.Communication.ServerApi;
using WingsAPI.Communication.Services.Messages;
using WingsAPI.Plugins;
using WingsAPI.Plugins.Exceptions;
using WingsEmu.Commands;
using WingsEmu.Commands.Interfaces;
using WingsEmu.Communication.gRPC.Extensions;
using WingsEmu.Game.Commands;
using WingsEmu.Game.Logs;
using WingsEmu.Health.Extensions;
using WingsEmu.Packets;
using WingsEmu.Plugins.BasicImplementations;
using WingsEmu.Plugins.BasicImplementations.Algorithms;
using WingsEmu.Plugins.BasicImplementations.BCards;
using WingsEmu.Plugins.DistributedGameEvents;
using WingsEmu.Plugins.DistributedGameEvents.Consumer;
using WingsEmu.Plugins.DistributedGameEvents.Mails;
using WingsEmu.Plugins.DistributedGameEvents.PlayerEvents;
using WingsEmu.Plugins.DistributedGameEvents.Relation;
using WingsEmu.Plugins.Essentials;
using WingsEmu.Plugins.GameEvents;
using WingsEmu.Plugins.PacketHandling;
using WingsEmu.Plugins.PacketHandling.Customization;

namespace GameChannel
{
    public class Startup
    {
        private static ServiceProvider GetPluginsProvider()
        {
            var pluginBuilder = new ServiceCollection();
            pluginBuilder.AddTransient<IDependencyInjectorPlugin, FileResourceLoaderPlugin>();
            pluginBuilder.AddTransient<IGameServerPlugin, GamePacketHandlersCorePlugin>();
            pluginBuilder.AddTransient<IGameServerPlugin, CharScreenPacketHandlerCorePlugin>();
            pluginBuilder.AddTransient<IGameServerPlugin, ItemHandlerPluginCore>();
            pluginBuilder.AddTransient<IGameServerPlugin, GenericEventPluginCore>();
            pluginBuilder.AddTransient<IGameServerPlugin, NpcDialogPluginCore>();
            pluginBuilder.AddTransient<IGameServerPlugin, GuriPluginCore>();
            pluginBuilder.AddTransient<IGameServerPlugin, BCardPluginCore>();
            pluginBuilder.AddTransient<IGameServerPlugin, GameManagersPluginCore>();
            pluginBuilder.AddTransient<IGameServerPlugin, RaidsPluginCore>();
            pluginBuilder.AddTransient<IGameServerPlugin, TimeSpacesPluginCore>();
            pluginBuilder.AddTransient<IGameServerPlugin, Act4PluginCore>();
            pluginBuilder.AddTransient<IGameServerPlugin, CustomizationCorePlugin>();
            pluginBuilder.AddTransient<IGameServerPlugin, GameEventsPluginCore>();
            pluginBuilder.AddTransient<IGameServerPlugin, AlgorithmPluginCore>();
            pluginBuilder.AddTransient<IGameServerPlugin, ScheduledEventSubscriberCorePlugin>();
            pluginBuilder.AddTransient<IGameServerPlugin, PlayerLoggingDependencyPlugin>();
            pluginBuilder.AddTransient<IGameServerPlugin, CoreImplDependencyPlugin>();
            pluginBuilder.AddTransient<IGameServerPlugin, FamilyPluginCore>();
            pluginBuilder.AddTransient<IGameServerPlugin, QuestPluginCore>();
            pluginBuilder.AddTransient<IGameServerPlugin, RainbowBattlePluginCore>();
            pluginBuilder.AddTransient<IGameServerPlugin, GameResourceLoaderPlugin>();


            return pluginBuilder.BuildServiceProvider();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddGrpc(options =>
            {
                options.MaxReceiveMessageSize = null;
                options.MaxSendMessageSize = null;
            });

            //No idea if this SerializableWorld thingy has to be removed in the future, but I need this info for loading or not loading Act4 thingies.
            services.AddSingleton(WorldServerSingleton.Instance);
            var loader = new GameServerLoader
            {
                Type = WorldServerSingleton.Instance.ChannelType
            };
            using ServiceProvider plugins = GetPluginsProvider();

            foreach (IDependencyInjectorPlugin plugin in plugins.GetServices<IDependencyInjectorPlugin>())
            {
                try
                {
                    Log.Debug($"[PLUGIN_LOADER] Loading generic plugin {plugin.Name}...");
                    plugin.AddDependencies(services);
                }
                catch (PluginException e)
                {
                    Log.Error($"{plugin.Name} : plugin.OnLoad", e);
                }
            }


            foreach (IGameServerPlugin plugin in plugins.GetServices<IGameServerPlugin>())
            {
                try
                {
                    Log.Debug($"[PLUGIN_LOADER] Loading game plugin {plugin.Name}...");
                    plugin.AddDependencies(services, loader);
                }
                catch (PluginException e)
                {
                    Log.Error($"{plugin.Name} : plugin.OnLoad", e);
                }
            }

            services.TryAddSingleton<IPacketDeserializer, PacketDeserializer>();
            services.AddPhoenixLogging();
            // add redis
            services.TryAddSingleton(s => RedisConfiguration.FromEnv());
            services.TryAddSingleton(s => s.GetRequiredService<RedisConfiguration>().GetConnectionMultiplexer());

            services.AddMaintenanceMode();

            services.AddYamlConfigurationHelper();
            services.AddSingleton<IPacketSerializer>(s => new PacketSerializer());
            // client session factory
            services.AddTransient<GameSessionFactory>();

            services.TryAddTransient(typeof(IMapper<,>), typeof(MapsterMapper<,>));

            services.AddServerApiServiceClient();
            services.AddGrpcSessionServiceClient();
            services.AddGrpcMailServiceClient();
            services.AddGrpcRelationServiceClient();
            services.AddGrpcClusterStatusServiceClient();
            services.AddClusterCharacterServiceClient();
            services.AddTranslationsGrpcClient();
            services.AddTickSystem();

            services.AddMqttConfigurationFromEnv();

            services.AddMessagePublisher<PlayerDisconnectedChannelMessage>();

            services.AddMessagePublisher<PlayerConnectedOnChannelMessage>();
            services.AddMessagePublisher<PlayerDisconnectedChannelMessage>();
            services.AddMessageSubscriber<PlayerConnectedOnChannelMessage, PlayerConnectedChannelGameConsumer>();
            services.AddMessageSubscriber<PlayerDisconnectedChannelMessage, PlayerDisconnectedChannelConsumer>();

            services.AddMessageSubscriber<KickAccountMessage, KickAccountConsumer>();

            services.AddMessagePublisher<PlayerKickMessage>();
            services.AddMessageSubscriber<PlayerKickMessage, PlayerKickConsumer>();

            services.AddMessageSubscriber<ServiceKickAllMessage, ServiceKickAllMessageConsumer>();
            services.AddMessageSubscriber<ServiceMaintenanceNotificationMessage, ServiceMaintenanceNotificationMessageConsumer>();

            services.AddMessageSubscriber<NoteReceivedMessage, NoteReceivedMessageConsumer>();
            services.AddMessageSubscriber<MailReceivedMessage, MailReceivedMessageConsumer>();

            services.AddMessageSubscriber<NoteReceivePendingOnConnectedMessage, NoteReceivePendingOnConnectedMessageConsumer>();
            services.AddMessageSubscriber<MailReceivePendingOnConnectedMessage, MailReceivePendingOnConnectedMessageConsumer>();

            services.AddMessageSubscriber<RelationCharacterJoinMessage, RelationCharacterJoinMessageConsumer>();
            services.AddMessageSubscriber<RelationCharacterLeaveMessage, RelationCharacterLeaveMessageConsumer>();

            services.AddMessageSubscriber<RelationCharacterAddMessage, RelationCharacterAddMessageConsumer>();
            services.AddMessageSubscriber<RelationCharacterRemoveMessage, RelationCharacterRemoveMessageConsumer>();

            services.AddMessagePublisher<RelationSendTalkMessage>();
            services.AddMessageSubscriber<RelationSendTalkMessage, RelationSendTalkMessageConsumer>();

            services.AddMessageSubscriber<WorldServerShutdownMessage, WorldServerShutdownConsumer>();
            
            services.AddEventPipeline();
            services.AddHostedService<WorldKeepAliveSystem>();

            services.AddSingleton<IPlayerLogManager, PlayerLogManager>();
            services.AddSingleton<IHostedService>(provider => (PlayerLogManager)provider.GetService<IPlayerLogManager>());

            /*
             * Helpers to remove
             */

            services.AddScheduler();
            services.AddCron();
            services.AddSingleton<GameChannelStopService>();
            services.AddSingleton<ICommandContainer, CommandHandler>();
            services.AddTransient<IGlobalCommandExecutor, CommandGlobalExecutorWrapper>();
            services.AddTransient<IGamePlugin, GameHelpersPlugin>();
            services.AddTransient<IGamePlugin, GameManagerPlugin>();
            services.AddTransient<IGamePlugin, GamePacketHandlersGamePlugin>();
            services.AddTransient<IGamePlugin, CharScreenPacketHandlerGamePlugin>();
            services.AddTransient<IGamePlugin, EssentialsPlugin>();
            services.AddTransient<IGamePlugin, ItemHandlerPlugin>();
            services.AddTransient<IGamePlugin, NpcDialogPlugin>();
            services.AddTransient<IGamePlugin, GuriPlugin>();
            services.AddTransient<IGamePlugin, BCardGamePlugin>();
            services.AddTransient<IGamePlugin, GameEventsPlugin>();
            services.AddTransient<IGamePlugin, RaidsPlugin>();
            services.AddTransient<IGamePlugin, TimeSpacesPlugin>();
            services.AddTransient<IGamePlugin, FamilyPlugin>();
            services.AddTransient<IGamePlugin, Act4Plugin>();
            services.AddTransient<IGamePlugin, QuestPlugin>();
            services.AddTransient<IGamePlugin, RainbowBattlePlugin>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(e => e.MapControllers());
        }
    }
}