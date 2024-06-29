using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
using WingsAPI.Communication.Auth;

namespace LoginServer.Auth
{
    public class RedisCachedHardwareIdService : IHardwareIdService
    {
        private const string DataPrefix = "blacklist:hardware-id:";
        private readonly IDatabase _database;

        public RedisCachedHardwareIdService(IConnectionMultiplexer connectionMultiplexer) => _database = connectionMultiplexer.GetDatabase(1);

        public Task<bool> SynchronizeWithDbAsync(IEnumerable<BlacklistedHwidDto> dtos) => Task.FromResult(true);

        public async Task<bool> CanLogin(string hardwareId) => await _database.KeyExistsAsync(GetKey(hardwareId)) == false;

        public async Task RegisterHardwareId(string hardwareId)
        {
            await _database.StringSetAsync(GetKey(hardwareId), hardwareId);
        }

        public async Task UnregisterHardwareId(string hardwareId)
        {
            await _database.KeyDeleteAsync(GetKey(hardwareId));
        }

        private static string GetKey(string hwid) => DataPrefix + hwid;
    }
}