namespace FhirIngestion.Tools.Publisher
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FhirIngestion.Tools.Common.Helpers;

    /// <summary>
    /// Simple class for collecting events.
    /// Contains a circular buffer used to bin events.
    /// </summary>
    public class MetricsCollector
    {
        private readonly Stopwatch _stopwatch = new ();
        private readonly object _metricsLock = new ();
        private readonly object _stopwatchLock = new ();
        private readonly int _bins;
        private readonly int _resolutionMs;
        private readonly long[] _counts;
        private bool _timerEnabled = true;
        private int _totalSuccessRequests;
        private int _startBin;
        private int _maxBinIndex;
        private DateTime? _startTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsCollector"/> class.
        /// </summary>
        public MetricsCollector(int bins = 30, int resolutionMs = 1000)
        {
            _totalSuccessRequests = 0;
            _bins = bins;
            _counts = new long[bins];
            _resolutionMs = resolutionMs;
        }

        /// <summary>
        /// Gets the events per second.
        /// </summary>
        public double EventsPerSecond
        {
            get
            {
                lock (_metricsLock)
                {
                    return (double)_counts.Sum() / (_resolutionMs * (_maxBinIndex + 1) / 1000.0);
                }
            }
        }

        /// <summary>
        /// Gets the total success requests.
        /// </summary>
        public long TotalSuccessRequests
        {
            get
            {
                lock (_metricsLock)
                {
                    return _totalSuccessRequests;
                }
            }
        }

        /// <summary>
        /// Gets the elapsed timespan from the Stopwatch.
        /// </summary>
        public TimeSpan StopwatchElapsed
        {
            get
            {
                return _stopwatch.Elapsed;
            }
        }

        /// <summary>
        /// Starts the output on a timer.
        /// </summary>
        /// <param name="refreshInterval">the refreshInterval for the output logs.</param>
        public void Start(int refreshInterval)
        {
            StartStopwatch();
            _timerEnabled = true;

            var t = new Task(() =>
            {
                while (_timerEnabled)
                {
                    lock (_stopwatchLock)
                    {
                        if (_timerEnabled)
                        {
                            Thread.Sleep(1000 * refreshInterval);
                            MessageHelper.Verbose($"Successful requests: {TotalSuccessRequests} in {StopwatchElapsed} ({Math.Ceiling(EventsPerSecond)} res/sec)");
                        }
                    }
                }
            });

            t.Start();
        }

        /// <summary>
        /// Stops the metrics collector timer.
        /// </summary>
        public void Stop()
        {
            StopStopwatch();

            lock (_stopwatchLock)
            {
                _timerEnabled = false;
            }
        }

        /// <summary>
        /// Register event at specific time.
        /// </summary>
        /// <param name="eventTime">the event datetime.</param>
        /// <param name="isSuccessStatusCode">the event isSuccessStatusCode.</param>
        public void Collect(DateTime eventTime, bool isSuccessStatusCode)
        {
            lock (_metricsLock)
            {
                if (_startTime is null)
                {
                    _startTime = DateTime.Now;
                }

                if (isSuccessStatusCode)
                {
                    _totalSuccessRequests++;
                }

                int binIndex = (int)((eventTime - _startTime.Value).TotalMilliseconds / _resolutionMs);

                while (binIndex >= _bins)
                {
                    _counts[_startBin] = 0;
                    _startBin = (_startBin + 1) % _bins;
                    _startTime += TimeSpan.FromMilliseconds(_resolutionMs);
                    binIndex--;
                }

                _counts[(binIndex + _startBin) % _bins]++;

                // We keep track of this to make sure that in the warm up, we take the average only of bins used
                _maxBinIndex = binIndex;
            }
        }

        private void StartStopwatch()
        {
            if (!_stopwatch.IsRunning)
            {
                _stopwatch.Start();
            }
        }

        private void StopStopwatch()
        {
            if (_stopwatch.IsRunning)
            {
                _stopwatch.Stop();
            }
        }
    }
}
