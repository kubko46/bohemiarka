using System.Threading.Tasks;

namespace LoginServer.Auth
{
    /// <summary>
    ///     Todo dedicated authoritative service
    /// </summary>
    public interface IClientVersionCheckingService
    {
        Task<bool> IsAuthorized(string sentHash);
        Task<string> GetClientVersion(string sentHash);
    }
}