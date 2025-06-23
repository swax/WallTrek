// MainWindow.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WallTrek.Services;

namespace WallTrek
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "WallTrek Settings";
            LoadSettingsToUI();
        }

        private void LoadSettingsToUI()
        {
            var settings = Settings.Instance;
            ApiKeyTextBox.Text = settings.ApiKey ?? string.Empty;
            LastPromptTextBox.Text = settings.LastPrompt ?? string.Empty;
            AutoGenerateCheckBox.IsChecked = settings.AutoGenerateEnabled;
            AutoGenerateMinutesNumberBox.Value = settings.AutoGenerateMinutes > 0 ? settings.AutoGenerateMinutes : 60;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = Settings.Instance;
            settings.ApiKey = ApiKeyTextBox.Text;
            settings.LastPrompt = LastPromptTextBox.Text;
            settings.AutoGenerateEnabled = AutoGenerateCheckBox.IsChecked ?? false;
            settings.AutoGenerateMinutes = (int)AutoGenerateMinutesNumberBox.Value;
            
            settings.Save();
            StatusTextBlock.Text = "Settings saved successfully!";
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.Load();
            LoadSettingsToUI();
            StatusTextBlock.Text = "Settings loaded successfully!";
        }
    }
}