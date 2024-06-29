using System;
using LoginServer.Network;
using WingsEmu.Packets;

namespace LoginServer.Handlers
{
    public interface IGlobalPacketProcessor
    {
        public void RegisterHandler(Type packetType, IPacketHandler packetHandler);
        public void Execute(LoginClientSession session, IPacket packet, Type packetType);
    }
}