using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace WallTrek.Services
{
    public class AutoGenerateService
    {
        private static AutoGenerateService? _instance;
        private static readonly object _lock = new object();
        
        public static AutoGenerateService Instance 
        { 
            get 
            { 
                if (_instance == null) 
                { 
                    lock (_lock) 
                    { 
                        if (_instance == null) 
                            _instance = new AutoGenerateService(); 
                    } 
                } 
                return _instance; 
            } 
        }

        private DispatcherQueueTimer? pollTimer;
        private readonly DispatcherQueue dispatcherQueue;

        public event EventHandler? AutoGenerateTriggered;
        public event EventHandler<string>? NextGenerateTimeUpdated;

        private AutoGenerateService() 
        { 
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        public void RefreshFromSettings()
        {
            var settings = Settings.Instance;
            
            if (settings.AutoGenerateEnabled && settings.AutoGenerateHours > 0)
            {
                // If there's a saved next generation time, check if it's in the future or past
                if (settings.NextAutoGenerateTime.HasValue)
                {
                    if (settings.NextAutoGenerateTime > DateTime.Now)
                    {
                        StartFromSavedTime();
                    }
                    else
                    {
                        // Time has passed, trigger immediate generation and start new cycle
                        TriggerGenerationAndRestart();
                    }
                }
                else
                {
                    // Start fresh with current settings
                    Start(settings.AutoGenerateHours);
                }
            }
            else
            {
                // Stop if disabled
                Cancel();
            }
        }

        public bool IsEnabled => pollTimer != null;
        public DateTime? NextGenerateTime => Settings.Instance.NextAutoGenerateTime;

        public void Start(double hours)
        {
            Stop();

            if (hours <= 0)
            {
                return;
            }

            var nextTime = DateTime.Now.AddHours(hours);
            Settings.Instance.NextAutoGenerateTime = nextTime;
            Settings.Instance.Save();

            StartPolling();
        }

        public void StartFromSavedTime()
        {
            if (Settings.Instance.NextAutoGenerateTime.HasValue && 
                Settings.Instance.NextAutoGenerateTime > DateTime.Now)
            {
                StartPolling();
            }
            else
            {
                Settings.Instance.NextAutoGenerateTime = null;
                Settings.Instance.Save();
                NextGenerateTimeUpdated?.Invoke(this, "");
            }
        }

        private void StartPolling()
        {
            Stop();

            pollTimer = dispatcherQueue.CreateTimer();
            pollTimer.Interval = TimeSpan.FromSeconds(1); // Poll every second
            pollTimer.Tick += OnPollTimer_Tick;
            pollTimer.Start();

            UpdateNextGenerateTime();
        }

        public void Stop()
        {
            pollTimer?.Stop();
            pollTimer = null;

            NextGenerateTimeUpdated?.Invoke(this, "");
        }

        public void Cancel()
        {
            Stop();
            Settings.Instance.NextAutoGenerateTime = null;
            Settings.Instance.Save();
        }

        private void OnPollTimer_Tick(object? sender, object e)
        {
            var nextTime = Settings.Instance.NextAutoGenerateTime;
            
            if (!nextTime.HasValue)
            {
                Stop();
                return;
            }

            if (DateTime.Now >= nextTime.Value)
            {
                TriggerGenerationAndRestart();
            }
            else
            {
                UpdateNextGenerateTime();
            }
        }

        private void TriggerGenerationAndRestart()
        {
            Settings.Instance.NextAutoGenerateTime = null;
            Settings.Instance.Save();
            Stop();
            AutoGenerateTriggered?.Invoke(this, EventArgs.Empty);
            
            // Restart timer if auto-generate is still enabled
            if (Settings.Instance.AutoGenerateEnabled && Settings.Instance.AutoGenerateHours > 0)
            {
                Start(Settings.Instance.AutoGenerateHours);
            }
        }

        private void UpdateNextGenerateTime()
        {
            var nextTime = Settings.Instance.NextAutoGenerateTime;
            
            if (!nextTime.HasValue)
            {
                NextGenerateTimeUpdated?.Invoke(this, "");
                return;
            }

            string timeText = $"Next generation at: {nextTime.Value:MMM d, h:mm tt}";
            NextGenerateTimeUpdated?.Invoke(this, timeText);
        }
    }
}