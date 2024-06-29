// WingsEmu
// 
// Developed by NosWings Team

using System.Collections.Generic;
using System.Text;
using LoginServer.Network;
using PhoenixLib.Logging;
using PhoenixLib.MultiLanguage;
using WingsAPI.Communication.ServerApi.Protocol;
using WingsEmu.Packets.Enums;

namespace LoginServer.Handlers
{
    internal static class LoginPacketsExtensions
    {
        internal static string GenerateFailcPacket(this LoginClientSession session, LoginFailType failType) => $"failc {((short)failType).ToString()}";

        internal static void SendChannelPacketList(this LoginClientSession session, int encryptionKey, string sessionId, RegionLanguageType region, IEnumerable<SerializableGameServer> worldServers,
            bool isOldLogin)
        {
            string lastGroup = string.Empty;
            int worldGroupCount = 0;
            var packetBuilder = new StringBuilder();
            packetBuilder.AppendFormat($"NsTeST  {(byte)region} {sessionId} 2 ");

            packetBuilder.Append(
                isOldLogin
                    ? $"-99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 {encryptionKey} "
                    : $"-99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 -99 0 {encryptionKey} ");

            foreach (SerializableGameServer world in worldServers)
            {
                if (lastGroup != world.WorldGroup)
                {
                    worldGroupCount++;
                }

                lastGroup = world.WorldGroup;
                int color = (int)(world.SessionCount / (double)world.AccountLimit * 20);
                packetBuilder.AppendFormat("{0}:{1}:{2}:{3}.{4}.{5} ", world.EndPointIp, world.EndPointPort, color, worldGroupCount, world.ChannelId, world.WorldGroup.Replace(' ', '^'));
            }

            packetBuilder.Append("-1:-1:-1:10000.10000.1");

            string packet = packetBuilder.ToString();

            Log.Info($"[CHANNEL_LIST] {packet}");

            session.SendPacket(packet);
        }
    }
}