// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using LoginServer.Handlers;
using NetCoreServer;
using PhoenixLib.Logging;
using WingsEmu.Packets;

namespace LoginServer.Network
{
    public class LoginClientSession : TcpSession
    {
        private readonly IPacketDeserializer _deserializer;
        private readonly IGlobalPacketProcessor _loginHandlers;


        public LoginClientSession(TcpServer server, IGlobalPacketProcessor globalPacketProcessor, IPacketDeserializer deserializer) : base(server)
        {
            _loginHandlers = globalPacketProcessor;
            _deserializer = deserializer;
        }

        public string IpAddress { get; private set; }

        public void SendPacket(string packet) => Send(NostaleLoginEncrypter.Encode(packet, Encoding.Default).ToArray());

        protected override void OnConnected()
        {
            try
            {
                if (IsDisposed)
                {
                    return;
                }

                if (IsSocketDisposed)
                {
                    Disconnect();
                    return;
                }

                if (Socket == null)
                {
                    return;
                }

                if (Socket?.RemoteEndPoint is IPEndPoint ip)
                {
                    IpAddress = ip.Address.ToString();
                }
            }
            catch (Exception e)
            {
                Log.Error("[LOGIN_SERVER_SESSION] OnConnected", e);
                Disconnect();
            }
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            try
            {
                string packet = NostaleLoginDecrypter.Decode(buffer.AsSpan((int)offset, (int)size));
                string[] packetSplit = packet.Replace('^', ' ').Split(' ');
                string packetHeader = packetSplit[0];
                if (string.IsNullOrWhiteSpace(packetHeader))
                {
                    Disconnect();
                    return;
                }

                TriggerHandler(packetHeader.Replace("#", ""), packet);
            }
            catch
            {
                Disconnect();
            }
        }


        private void TriggerHandler(string packetHeader, string packetString)
        {
            if (IsDisposed)
            {
                return;
            }

            try
            {
                (IClientPacket typedPacket, Type packetType) = _deserializer.Deserialize(packetString, false);

                if (packetType == typeof(UnresolvedPacket) && typedPacket != null)
                {
                    Log.Warn($"UNRESOLVED_PACKET : {packetHeader}");
                    return;
                }

                if (packetType == null && typedPacket == null)
                {
                    Log.Debug($"DESERIALIZATION_ERROR : {packetString}");
                    return;
                }

                _loginHandlers.Execute(this, typedPacket, packetType);
            }
            catch (Exception ex)
            {
                // disconnect if something unexpected happens
                Log.Error("Handler Error SessionId: " + Id, ex);
                Disconnect();
            }
        }

        protected override void OnError(SocketError error)
        {
            Disconnect();
        }
    }
}