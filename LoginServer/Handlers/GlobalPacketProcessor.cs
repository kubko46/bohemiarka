using System;
using System.Collections.Generic;
using LoginServer.Network;
using WingsEmu.Packets;

namespace LoginServer.Handlers
{
    public class GlobalPacketProcessor : IGlobalPacketProcessor
    {
        private readonly Dictionary<Type, IPacketHandler> _handlers = new();

        public void RegisterHandler(Type packetType, IPacketHandler packetHandler)
        {
            _handlers[packetType] = packetHandler;
        }

        public void Execute(LoginClientSession session, IPacket packet, Type packetType)
        {
            if (!_handlers.TryGetValue(packetType, out IPacketHandler handler))
            {
                return;
            }

            handler.HandleAsync(session, packet).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}