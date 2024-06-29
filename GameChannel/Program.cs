using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dotenv.net;
using GameChannel.Network;
using GameChannel.Services;
using GameChannel.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PhoenixLib.Events;
using PhoenixLib.Logging;
using PhoenixLib.ServiceBus.MQTT;
using WingsAPI.Communication;
using WingsAPI.Communication.ServerApi;
using WingsAPI.Communication.ServerApi.Protocol;
using WingsAPI.Communication.Sessions;
using WingsAPI.Plugins;
using WingsEmu.Game.Commands;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Miniland;
using WingsEmu.Packets;

namespace GameChannel
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
            // workaround
            // http2 needs SSL
            // https://github.com/grpc/grpc-dotnet/issues/626
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            PrintHeader();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            DotEnv.Load(new DotEnvOptions(true, new[] { "world.env" }, Encoding.UTF8));
            using IHost httpServer = CreateHostBuilder(args).Build();
            {
                IServiceProvider services = httpServer.Services;

                IServerApiService serverApiService = services.GetRequiredService<IServerApiService>();
                BasicRpcResponse response = null;

                while (response == null)
                {
                    try
                    {
                        response = await serverApiService.IsMasterOnline(new EmptyRpcRequest());
                    }
                    catch (Exception e)
                    {
                        Log.Warn("Failed to contact with Master Server, retrying...");
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                }

                GameSession.Initialize(
                    services.GetService<IGlobalCommandExecutor>(),
                    services.GetService<IAsyncEventPipeline>(),
                    services.GetService<IServerManager>(),
                    services.GetService<ISessionManager>(),
                    services.GetService<ISessionService>(),
                    services.GetRequiredService<IMinilandManager>(),
                    services.GetRequiredService<IPacketDeserializer>()
                );

                SerializableGameServer gameServer = services.GetRequiredService<SerializableGameServer>();

                IEnumerable<IGamePlugin> plugins = services.GetServices<IGamePlugin>();
                foreach (IGamePlugin gamePlugin in plugins)
                {
                    gamePlugin.OnLoad();
                }

                IServerManager serverManager = services.GetRequiredService<IServerManager>();
                StaticServerManager.Initialize(serverManager);
                serverManager.InitializeAsync();

                using var stopService = new DockerGracefulStopService();
                GameTcpServer server;
                portLoop:
                try
                {
                    Log.Warn($"Binding TCP port {gameServer.EndPointPort}");
                    server = new GameTcpServer(
                        IPAddress.Any,
                        gameServer.EndPointPort,
                        new SmartSpamProtector(),
                        serverManager, httpServer.Services.GetRequiredService<GameSessionFactory>());
                    server.Start();
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode == 10048)
                    {
                        gameServer.EndPointPort++;
                        Log.Info("Port already in use! Incrementing...");
                        goto portLoop;
                    }

                    Log.Error("General Error", ex);
                    Environment.Exit(1);
                    return;
                }

                await httpServer.StartAsync();

                IMessagingService messagingService = services.GetRequiredService<IMessagingService>();
                await messagingService.StartAsync();

                Log.Warn($"Registering GameChannel [{gameServer.WorldGroup}] {gameServer.EndPointIp}:{gameServer.EndPointPort}");

                BasicRpcResponse response2 = null;
                try
                {
                    response2 = await serverApiService.RegisterWorldServer(new RegisterWorldServerRequest
                    {
                        GameServer = gameServer
                    });
                }
                catch (Exception e)
                {
                    server.Stop();
                    Log.Error("Could not register channel. Already running?", e);
                    Console.ReadKey();
                }


                if (response2?.ResponseType != RpcResponseType.SUCCESS)
                {
                    Log.Info($"New channel: {gameServer.ChannelId.ToString()} registered");
                }

                Log.Warn($"[GAME_CHANNEL] Started as : {gameServer.WorldGroup}:{gameServer.ChannelId}");
                serverManager.PutIdle();

                GameChannelStopService serverShutdown = services.GetRequiredService<GameChannelStopService>();
                serverManager.ListenCancellation(stopService.TokenSource);

                await httpServer.WaitForShutdownAsync(stopService.CancellationToken);

                await serverShutdown.StopAsync();

                Log.Warn("Properly shutting down server...");
                server?.Stop();
                await httpServer?.StopAsync();
                await messagingService.DisposeAsync();
                Serilog.Log.CloseAndFlush();
            }
        }

        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        // Additional configuration is required to successfully run gRPC on macOS.
        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);
            hostBuilder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(s =>
                {
                    s.ListenAnyIP(Convert.ToInt32(Environment.GetEnvironmentVariable("HTTP_LISTEN_PORT") ?? "17500"),
                        options => { options.Protocols = HttpProtocols.Http1AndHttp2; });
                });
                webBuilder.UseStartup<Startup>().UseDefaultServiceProvider(options => options.ValidateScopes = false);
            });
            return hostBuilder;
        }


        private static void PrintHeader()
        {
            Console.Title = "WingsEmu - World";
            const string text = @"
██╗    ██╗██╗███╗   ██╗ ██████╗ ███████╗███████╗███╗   ███╗██╗   ██╗                     
██║    ██║██║████╗  ██║██╔════╝ ██╔════╝██╔════╝████╗ ████║██║   ██║                     
██║ █╗ ██║██║██╔██╗ ██║██║  ███╗███████╗█████╗  ██╔████╔██║██║   ██║                     
██║███╗██║██║██║╚██╗██║██║   ██║╚════██║██╔══╝  ██║╚██╔╝██║██║   ██║                     
╚███╔███╔╝██║██║ ╚████║╚██████╔╝███████║███████╗██║ ╚═╝ ██║╚██████╔╝                     
 ╚══╝╚══╝ ╚═╝╚═╝  ╚═══╝ ╚═════╝ ╚══════╝╚══════╝╚═╝     ╚═╝ ╚═════╝                      
                                                                                         
 ██████╗  █████╗ ███╗   ███╗███████╗    ███████╗███████╗██████╗ ██╗   ██╗███████╗██████╗ 
██╔════╝ ██╔══██╗████╗ ████║██╔════╝    ██╔════╝██╔════╝██╔══██╗██║   ██║██╔════╝██╔══██╗
██║  ███╗███████║██╔████╔██║█████╗█████╗███████╗█████╗  ██████╔╝██║   ██║█████╗  ██████╔╝
██║   ██║██╔══██║██║╚██╔╝██║██╔══╝╚════╝╚════██║██╔══╝  ██╔══██╗╚██╗ ██╔╝██╔══╝  ██╔══██╗
╚██████╔╝██║  ██║██║ ╚═╝ ██║███████╗    ███████║███████╗██║  ██║ ╚████╔╝ ███████╗██║  ██║
 ╚═════╝ ╚═╝  ╚═╝╚═╝     ╚═╝╚══════╝    ╚══════╝╚══════╝╚═╝  ╚═╝  ╚═══╝  ╚══════╝╚═╝  ╚═╝
";
            string separator = new('=', Console.WindowWidth);
            string logo = text.Split('\n')
                .Select(s => string.Format("{0," + (Console.WindowWidth / 2 + s.Length / 2) + "}\n", s))
                .Aggregate("", (current, i) => current + i);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(separator + logo + $"Version: {Assembly.GetExecutingAssembly().GetName().Version}\n" + separator);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}