// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PhoenixLib.Logging;
using WingsEmu.Core.Generics;

namespace GameChannel.Utils
{
    /// <summary>
    ///     Todo MOVE to Redis + AlertManager + Prometheus
    /// </summary>
    public class SmartSpamProtector : ISpamProtector
    {
        private const int CONNECTION_ATTEMPTS_BEFORE_BLACKLIST = 2;
        private static readonly TimeSpan TimeBetweenConnection = TimeSpan.FromMilliseconds(150);

        private static readonly ThreadSafeHashSet<string> BlacklistedIps = new();
        private static readonly ConcurrentDictionary<string, List<DateTime>> ConnectionsByIp = new();

        public bool CanConnect(string ipAddress)
        {
            if (BlacklistedIps.Contains(ipAddress))
            {
                return false;
            }

            if (!ConnectionsByIp.TryGetValue(ipAddress, out List<DateTime> dates))
            {
                dates = new List<DateTime>();
                ConnectionsByIp[ipAddress] = dates;
            }

            DateTime lastConnection = dates.LastOrDefault();
            dates.Add(DateTime.UtcNow);

            if (dates.Count > CONNECTION_ATTEMPTS_BEFORE_BLACKLIST)
            {
                BlacklistedIps.Add(ipAddress);
                Log.Warn($"[SPAM_PROTECTOR] Blacklisted {ipAddress}");
                return false;
            }

            // should be accepted
            if (lastConnection.Add(TimeBetweenConnection) >= DateTime.UtcNow)
            {
                return false;
            }

            dates.Clear();
            return true;
        }
    }
}