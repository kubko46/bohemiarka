// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Runtime.Loader;
using System.Threading;
using PhoenixLib.Logging;

namespace LoginServer.Utils
{
    public class DockerGracefulStopService : IDisposable
    {
        private readonly ManualResetEventSlim _stoppedEvent;

        public DockerGracefulStopService()
        {
            TokenSource = new CancellationTokenSource();
            _stoppedEvent = new ManualResetEventSlim();
            // SIGINT
            Console.CancelKeyPress += (sender, eventArgs) => GracefulStop(TokenSource, _stoppedEvent);
            // SIGTERM
            AssemblyLoadContext.Default.Unloading += context => GracefulStop(TokenSource, _stoppedEvent);
            // EXCEPTION
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Log.Error("UnhandledException", args.ExceptionObject as Exception);
                GracefulStop(TokenSource, _stoppedEvent);
            };
        }

        public CancellationToken CancellationToken => TokenSource.Token;
        public CancellationTokenSource TokenSource { get; }

        public void Dispose()
        {
            _stoppedEvent.Set();
        }

        private static void GracefulStop(CancellationTokenSource cancellationTokenSource, ManualResetEventSlim stoppedEvent)
        {
            Log.Info("DockerGracefulStopService Stopping service");
            cancellationTokenSource.Cancel();
            stoppedEvent.Wait();
            Log.Info("DockerGracefulStopService Stop finished");
        }
    }
}