using System.Timers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Render.Services.StopwatchServices
{
    public class ReactiveStopwatch : ReactiveObject, IDisposable
    {
        private readonly double _interval;
        private readonly System.Timers.Timer _timer;
        
        /// <summary>
        /// Returns the elapsed time in seconds.
        /// </summary>
        [Reactive]
        public double ElapsedTime { get; private set; }

        public ReactiveStopwatch(double interval = 0.1)
        {
            _interval = interval;
            _timer = new (_interval*1000)
            {
                AutoReset = true
            };
            _timer.Elapsed += OnTimerElapsed;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            ElapsedTime += _interval;
        }
        
        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Reset()
        {
            Stop();
            ElapsedTime = 0.0;
        }

        public void Dispose()
        {
            _timer.Elapsed -= OnTimerElapsed;
        }
    }
}