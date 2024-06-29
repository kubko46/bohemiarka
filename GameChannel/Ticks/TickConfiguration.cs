// WingsEmu
// 
// Developed by NosWings Team

using System;

namespace GameChannel.Ticks
{
    public static class TickConfiguration
    {
        public const string SINGLE_THREAD_TICK_TYPE = "SINGLE_THREAD";
        public const string DISPATCH_WORKERS_TICK_TYPE = "DISPATCH_WORKERS";
        public const string PEEK_WORKERS_TICK_TYPE = "PEEK_WORKERS";

        public static uint TickFrequency = Convert.ToUInt32(Environment.GetEnvironmentVariable("WINGSEMU_TICK_RATE") ?? "20");
        public static uint TickWorkers = Convert.ToUInt32(Environment.GetEnvironmentVariable("WINGSEMU_TICK_WORKERS") ?? "2");
        public static string TickSystemType = Environment.GetEnvironmentVariable("WINGSEMU_TICK_SYSTEM_TYPE") ?? DISPATCH_WORKERS_TICK_TYPE;
    }
}