using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WingsEmu.Game._ECS;

namespace GameChannel.Ticks.DispatchQueueWork
{
    public class WorkerDispatchTickManager : ITickManager
    {
        private static readonly uint MaxTickWorkers = TickConfiguration.TickWorkers;
        private readonly ConcurrentDictionary<Guid, int> _processables = new();
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
        private readonly DispatchedTickWorker[] _workers = new DispatchedTickWorker[MaxTickWorkers];
        private bool _isStarted;

        public WorkerDispatchTickManager()
        {
            for (int i = 0; i < MaxTickWorkers; i++)
            {
                _workers[i] = new DispatchedTickWorker(i);
            }
        }

        public void AddProcessable(ITickProcessable processable)
        {
            if (_processables.ContainsKey(processable.Id))
            {
                return;
            }

            DispatchedTickWorker worker = _workers.OrderBy(s => s.AverageProcessingTime).First();
            _processables.TryAdd(processable.Id, worker.Id);
            worker.AddTickProcessable(processable);
        }

        public void RemoveProcessable(ITickProcessable processable)
        {
            if (!_processables.Remove(processable.Id, out int workerId))
            {
                return;
            }

            _workers[workerId].RemoveTickProcessable(processable);
        }

        public void Start()
        {
            _semaphoreSlim.Wait();
            try
            {
                if (_isStarted)
                {
                    return;
                }

                _isStarted = true;

                for (int i = 0; i < MaxTickWorkers; i++)
                {
                    _workers[i].Start();
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public void Stop()
        {
            _semaphoreSlim.Wait();
            try
            {
                if (!_isStarted)
                {
                    return;
                }

                _isStarted = false;

                for (int i = 0; i < MaxTickWorkers; i++)
                {
                    _workers[i].Stop();
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}