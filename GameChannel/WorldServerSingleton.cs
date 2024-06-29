using System;
using GameChannel.Utils;
using WingsAPI.Communication.ServerApi.Protocol;
using WingsEmu.DTOs.Account;

namespace GameChannel
{
    public static class WorldServerSingleton
    {
        public static SerializableGameServer Instance { get; } = new()
        {
            EndPointIp = Environment.GetEnvironmentVariable(EnvironmentConsts.GAME_SERVER_IP) ?? "185.32.183.90",
            EndPointPort = Convert.ToInt32(Environment.GetEnvironmentVariable(EnvironmentConsts.GAME_SERVER_PORT) ?? "8000"),
            WorldGroup = Environment.GetEnvironmentVariable(EnvironmentConsts.GAME_SERVER_GROUP) ?? "Pravaleon",
            AccountLimit = Convert.ToInt32(Environment.GetEnvironmentVariable(EnvironmentConsts.GAME_SERVER_SESSION_LIMIT) ?? "500"),
            ChannelId = Convert.ToInt32(Environment.GetEnvironmentVariable(EnvironmentConsts.GAME_SERVER_CHANNEL_ID) ?? "1"),
            ChannelType = Enum.Parse<GameChannelType>(Environment.GetEnvironmentVariable(EnvironmentConsts.GAME_SERVER_CHANNEL_TYPE) ?? GameChannelType.PVE_NORMAL.ToString(), true),
            Authority = (AuthorityType)Convert.ToInt32(Environment.GetEnvironmentVariable(EnvironmentConsts.GAME_SERVER_AUTHORITY) ?? $"{((int)AuthorityType.User).ToString()}")
        };
    }
}