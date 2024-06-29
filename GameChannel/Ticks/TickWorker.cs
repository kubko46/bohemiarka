// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using PhoenixLib.Logging;
using WingsEmu.Game._ECS;

namespace GameChannel.Ticks.DispatchQueueWork
{
    public class DispatchedTickWorker
    {
        private static readonly uint TickFrequency = TickConfiguration.TickFrequency;
        private readonly List<ITickProcessable> _processables = new();
        private readonly ConcurrentQueue<ITickProcessable> _toAddQueue = new();
        private readonly ConcurrentQueue<ITickProcessable> _toRemoveQueue = new();
        private readonly string _workerName;
        private readonly string[] _workersLabel;

        private int _averageProcessingTime;

        private DateTime _lastTick = DateTime.UtcNow;

        private Thread _thread;


        public DispatchedTickWorker(int id)
        {
            Id = id;
            _workerName = "GameTickThread-" + id;
            _workersLabel = new[] { _workerName };
        }

        public int Id { get; }

        private bool IsRunning { get; set; }
        public int AverageProcessingTime => _averageProcessingTime;

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            IsRunning = true;
            _thread = new Thread(TickLoop)
            {
                Priority = ThreadPriority.AboveNormal,
                Name = _workerName
            };
            Log.Warn("[TICK_PROCESSOR] Starting worker " + _workerName);
            _thread.Start();
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void AddTickProcessable(ITickProcessable toAdd)
        {
            _toAddQueue.Enqueue(toAdd);
        }

        public void RemoveTickProcessable(ITickProcessable toRemove)
        {
            _toRemoveQueue.Enqueue(toRemove);
        }

        private static TimeSpan TimeBetweenTicks() => TimeSpan.FromSeconds(1) / TickFrequency;

        private DateTime GetNextTick()
        {
            DateTime nextSupposedTick = _lastTick + TimeBetweenTicks();
            if (DateTime.UtcNow < nextSupposedTick)
            {
                return nextSupposedTick;
            }

            _lastTick = nextSupposedTick;
            nextSupposedTick = _lastTick + TimeBetweenTicks();

            return nextSupposedTick;
        }

        private void TickLoop()
        {
            while (IsRunning)
            {
                try
                {
                    DateTime timeToNextTick = GetNextTick();
                    DateTime tickBegin = DateTime.UtcNow;

                    var stopWatch = Stopwatch.StartNew();

                    if (!_toRemoveQueue.IsEmpty)
                    {
                        while (_toRemoveQueue.TryDequeue(out ITickProcessable processable))
                        {
                            Log.Warn($"[TICK][{_workerName}] {processable.Name} removed");
                            _processables.Remove(processable);
                        }
                    }

                    if (!_toAddQueue.IsEmpty)
                    {
                        while (_toAddQueue.TryDequeue(out ITickProcessable processable))
                        {
                            _processables.Add(processable);
                        }
                    }

                    for (int index = 0; index < _processables.Count; index++)
                    {
                        try
                        {
                            ITickProcessable processable = _processables[index];
                            var watch = Stopwatch.StartNew();

                            processable.ProcessTick(tickBegin);

                            watch.Stop();
                        }
                        catch (Exception e)
                        {
                            Log.Error("[TICK][PROCESS]", e);
                        }
                    }

                    // Log.Debug($"[TICK_SYSTEM] {watch.ElapsedMilliseconds}ms to process {toProcess.Count} processable");
                    stopWatch.Stop();
                    DateTime finishedTime = DateTime.UtcNow;
                    long processingTime = stopWatch.ElapsedTicks;
                    // Log.Warn($"[TICK_SYSTEM][{_workerName}] processing took {processingTime}ms to process {_processables.Count.ToString()} systems");

                    Interlocked.Exchange(ref _averageProcessingTime, (int)processingTime);
                    TimeSpan sleepTime = timeToNextTick - finishedTime;
                    // Log.Debug($"[TICK_SYSTEM] sleeping {sleepTime.TotalMilliseconds}ms until next tick");
                    if (sleepTime.TotalMilliseconds < 0)
                    {
                        continue;
                    }

                    Thread.Sleep(sleepTime);
                }
                catch (Exception e)
                {
                    Log.Error("[TICK_LOOP]", e);
                }
            }
        }
    }
}