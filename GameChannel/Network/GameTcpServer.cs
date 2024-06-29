// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Net;
using System.Net.Sockets;
using GameChannel.Utils;
using NetCoreServer;
using PhoenixLib.Logging;
using WingsEmu.Game.Managers;

namespace GameChannel.Network
{
    public class GameTcpServer : TcpServer
    {
        private readonly IServerManager _serverManager;
        private readonly GameSessionFactory _sessionFactory;
        private readonly ISpamProtector _spamProtector;

        public GameTcpServer(IPAddress address, int port, ISpamProtector spamProtector, IServerManager serverManager, GameSessionFactory sessionFactory) :
            base(address, port)
        {
            _spamProtector = spamProtector;
            _serverManager = serverManager;
            _sessionFactory = sessionFactory;
        }

        protected override TcpSession CreateSession()
        {
            Log.Info("Creating session");
            GameSession tmp = _sessionFactory.CreateSession(this);
            Log.Info("Returning session");
            return tmp;
        }


        protected override void OnConnected(TcpSession session)
        {
            try
            {
                if (session.IsSocketDisposed)
                {
                    return;
                }

                if (!(session.Socket.RemoteEndPoint is IPEndPoint ip))
                {
                    session.Disconnect();
                    return;
                }

                if (!_spamProtector.CanConnect(ip.Address.ToString()))
                {
                    session.Disconnect();
                    return;
                }

                Log.Info($"[TCP_SERVER] Connected : {ip.Address}");
            }
            catch (Exception e)
            {
                Log.Error("[TCP_SERVER] OnConnected", e);
                session.Dispose();
            }
        }

        protected override void OnStarted()
        {
            Log.Info("[TCP_SERVER] Started");
        }

        protected override void OnError(SocketError error)
        {
            Log.Warn("[TCP_SERVER] caught an error with code {error}");
            _serverManager.Shutdown();
            Stop();
        }
    }
}