// WingsEmu
// 
// Developed by NosWings Team

namespace GameChannel.Utils
{
    public static class EnvironmentConsts
    {
        /*
         * FilePath configurations
         */
        public const string PLUGIN_PATH = "PLUGINS_PATH";
        public const string CONFIG_PATH = "CONFIG_PATH";

        /*
         * MASTER Communication env keys
         * GRPC
         */
        public const string MASTER_IP = "MASTER_IP";
        public const string MASTER_PORT = "MASTER_PORT";
        public const string MASTER_SSL_PATH = "MASTER_SSL_PATH";

        /*
         * World server
         */
        public const string GAME_SERVER_IP = "GAME_SERVER_IP";
        public const string GAME_SERVER_PORT = "GAME_SERVER_PORT";
        public const string GAME_SERVER_GROUP = "GAME_SERVER_GROUP";
        public const string GAME_SERVER_SESSION_LIMIT = "GAME_SERVER_SESSION_LIMIT";
        public const string GAME_SERVER_CHANNEL_ID = "GAME_SERVER_CHANNEL_ID";
        public const string GAME_SERVER_CHANNEL_TYPE = "GAME_SERVER_CHANNEL_TYPE";
        public const string GAME_SERVER_AUTHORITY = "GAME_SERVER_AUTHORITY";
    }
}