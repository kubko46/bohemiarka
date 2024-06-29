using System.Threading.Tasks;
using StackExchange.Redis;
using WingsAPI.Communication.Auth;

namespace LoginServer.Auth
{
    public class RedisClientVersionCheckingService : IClientVersionCheckingService
    {
        private const string KEY_PREFIX = "client-version:cache:";
        private readonly IDatabase _database;

        public RedisClientVersionCheckingService(IConnectionMultiplexer connectionMultiplexer) => _database = connectionMultiplexer.GetDatabase(1);

        public async Task<bool> IsAuthorized(string sentHash) => await _database.KeyExistsAsync(KEY_PREFIX + sentHash);

        public async Task<string> GetClientVersion(string sentHash) => await _database.StringGetAsync(KEY_PREFIX + sentHash);

        public async Task AddAuthorizedClient(AuthorizedClientVersionDto clientVersion)
        {
            await _database.StringSetAsync(KEY_PREFIX + clientVersion.GetComputedClientHash(), clientVersion.ClientVersion);
        }

        public async Task RemoveAuthorizedClient(AuthorizedClientVersionDto clientVersion)
        {
            await _database.KeyDeleteAsync(KEY_PREFIX + clientVersion.GetComputedClientHash());
        }
    }
}