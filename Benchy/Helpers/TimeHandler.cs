using System;
using System.Diagnostics;

namespace Benchy.Helpers
{
    public interface ITimeHandler
    {
        void Start();
        void Stop();
        long ElapsedMilliseconds();
        double ElapsedSecond();
        TimeSpan ElapsedTimeSpan();
    }

    public class TimeHandler : ITimeHandler
    {
        private readonly Stopwatch _stopwatch;

        public TimeHandler()
        {
            _stopwatch = new Stopwatch();
        }

        public void Start()
        {
            _stopwatch.Restart();
        }

        public void Stop()
        {
            _stopwatch.Stop();
        }

        public long ElapsedMilliseconds() => _stopwatch.ElapsedMilliseconds;

        public double ElapsedSecond() => _stopwatch.Elapsed.TotalSeconds;

        public TimeSpan ElapsedTimeSpan() => _stopwatch.Elapsed;
    }
}