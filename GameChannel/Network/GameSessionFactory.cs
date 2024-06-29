using WingsEmu.Game._i18n;
using WingsEmu.Game._packetHandling;

namespace GameChannel.Network
{
    public class GameSessionFactory
    {
        private readonly IPacketHandlerContainer<ICharacterScreenPacketHandler> _characterScreenHandlers;
        private readonly IPacketHandlerContainer<IGamePacketHandler> _gameHandlers;
        private readonly IGameLanguageService _gameLanguage;

        public GameSessionFactory(IPacketHandlerContainer<IGamePacketHandler> gameHandlers,
            IPacketHandlerContainer<ICharacterScreenPacketHandler> characterScreenHandlers, IGameLanguageService gameLanguage)
        {
            _gameHandlers = gameHandlers;
            _characterScreenHandlers = characterScreenHandlers;
            _gameLanguage = gameLanguage;
        }

        public GameSession CreateSession(GameTcpServer server) => new(server, _gameHandlers, _characterScreenHandlers, _gameLanguage);
    }
}