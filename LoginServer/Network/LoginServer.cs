// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Net;
using System.Net.Sockets;
using LoginServer.Handlers;
using LoginServer.Utils;
using NetCoreServer;
using PhoenixLib.Logging;
using WingsEmu.Packets;

namespace LoginServer.Network
{
    public class LoginServer : TcpServer
    {
        private readonly IGlobalPacketProcessor _globalPacketProcessor;
        private readonly IPacketDeserializer _packetDeserializer;
        private readonly ISpamProtector _spamProtector;

        public LoginServer(IPAddress address, int port, ISpamProtector spamProtector, IGlobalPacketProcessor globalPacketProcessor, IPacketDeserializer packetDeserializer) : base(address, port)
        {
            _spamProtector = spamProtector;
            _globalPacketProcessor = globalPacketProcessor;
            _packetDeserializer = packetDeserializer;
        }

        protected override TcpSession CreateSession()
        {
            Log.Info("[TCP_SERVER] CreateSession");
            var tmp = new LoginClientSession(this, _globalPacketProcessor, _packetDeserializer);
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

                if (session.Socket.RemoteEndPoint is not IPEndPoint ip)
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
            Log.Info("[TCP-SERVER] Started!");
        }

        protected override void OnError(SocketError error)
        {
            Log.Info("[TCP-SERVER] SocketError");
            Stop();
        }
    }
}