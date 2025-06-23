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
            
            if (settings.AutoGenerateEnabled && settings.AutoGenerateMinutes > 0)
            {
                // If there's a saved next generation time, restore from that
                if (settings.NextAutoGenerateTime.HasValue && settings.NextAutoGenerateTime > DateTime.Now)
                {
                    StartFromSavedTime();
                }
                else
                {
                    // Start fresh with current settings
                    Start(settings.AutoGenerateMinutes);
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

        public void Start(int minutes)
        {
            Stop();

            if (minutes <= 0)
            {
                return;
            }

            var nextTime = DateTime.Now.AddMinutes(minutes);
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
                Settings.Instance.NextAutoGenerateTime = null;
                Settings.Instance.Save();
                Stop();
                AutoGenerateTriggered?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                UpdateNextGenerateTime();
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