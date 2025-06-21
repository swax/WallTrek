using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WallTrek.Services
{
    public class AutoGenerateService
    {
        private System.Windows.Forms.Timer? pollTimer;

        public event EventHandler? AutoGenerateTriggered;
        public event EventHandler<string>? NextGenerateTimeUpdated;

        public bool IsEnabled => pollTimer?.Enabled ?? false;
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

            pollTimer = new System.Windows.Forms.Timer();
            pollTimer.Interval = 1000; // Poll every second
            pollTimer.Tick += OnPollTimer_Tick;
            pollTimer.Start();

            UpdateNextGenerateTime();
        }

        public void Stop()
        {
            pollTimer?.Stop();
            pollTimer?.Dispose();
            pollTimer = null;

            NextGenerateTimeUpdated?.Invoke(this, "");
        }

        public void Cancel()
        {
            Stop();
            Settings.Instance.NextAutoGenerateTime = null;
            Settings.Instance.Save();
        }

        private void OnPollTimer_Tick(object? sender, EventArgs e)
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