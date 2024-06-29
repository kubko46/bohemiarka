using LoginServer.Auth;
using LoginServer.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PhoenixLib.Caching;
using PhoenixLib.DAL.Redis;
using PhoenixLib.Logging;
using PhoenixLib.ServiceBus.Extensions;
using WingsAPI.Communication.Auth;
using WingsAPI.Packets;
using WingsEmu.Communication.gRPC.Extensions;
using WingsEmu.Health.Extensions;
using WingsEmu.Packets;

namespace LoginServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMqttConfigurationFromEnv();
            services.AddMaintenanceMode();
            services.AddPhoenixLogging();
            services.AddSingleton<IGlobalPacketProcessor, GlobalPacketProcessor>();

            services.AddClientPacketsInAssembly<UnresolvedPacket>();

            services.TryAddSingleton<IPacketDeserializer, PacketDeserializer>();

            services.AddTransient<TypedCredentialsLoginPacketHandler>();

            services.TryAddConnectionMultiplexerFromEnv();

            services.TryAddSingleton<IClientVersionCheckingService, RedisClientVersionCheckingService>();
            services.TryAddSingleton<IHardwareIdService, RedisCachedHardwareIdService>();

            services.TryAddSingleton(typeof(IKeyValueCache<>), typeof(InMemoryKeyValueCache<>));
            services.AddGrpcSessionServiceClient();
            services.AddServerApiServiceClient();
            services.AddGrpcDbServerServiceClient();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
        }
    }
}