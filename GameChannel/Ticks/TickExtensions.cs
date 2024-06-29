// WingsEmu
// 
// Developed by NosWings Team

using GameChannel.Ticks.DispatchQueueWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WingsEmu.Game._ECS;

namespace GameChannel.Ticks
{
    public static class TickExtensions
    {
        public static void AddTickSystem(this IServiceCollection services)
        {
            string tickSystemType = TickConfiguration.TickSystemType;
            switch (tickSystemType)
            {
                case TickConfiguration.DISPATCH_WORKERS_TICK_TYPE:
                    services.TryAddSingleton<ITickManager, WorkerDispatchTickManager>();
                    break;
            }
        }
    }
}