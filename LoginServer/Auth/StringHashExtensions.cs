using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using WingsAPI.Communication.Auth;
using BCrypt.Net;

namespace LoginServer.Auth
{
    internal static class StringHashExtensions
    {
        public static string ToBcrypt(this string str)
        {
            return BCrypt.Net.BCrypt.HashPassword(str);
        }

        public static string GetComputedClientHash(this AuthorizedClientVersionDto clientVersion)
        {
            string dllHash = clientVersion.DllHash;
            string executableHash = clientVersion.ExecutableHash;

            return (executableHash + dllHash).ToBcrypt();
        }
    }
}