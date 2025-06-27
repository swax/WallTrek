// RandomPromptSettingsView.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using WallTrek.Services;

namespace WallTrek.Views
{
    public sealed partial class RandomPromptSettingsView : UserControl
    {
        public event EventHandler? NavigateBack;
        
        private List<string> Categories { get; set; } = new();
        private List<string> Styles { get; set; } = new();
        private List<string> Moods { get; set; } = new();

        public RandomPromptSettingsView()
        {
            this.InitializeComponent();
            LoadSettingsToUI();
        }

        public void ClearStatus()
        {
            StatusTextBlock.Text = "";
        }

        private void LoadSettingsToUI()
        {
            var settings = Settings.Instance;
            var randomPrompts = settings.RandomPrompts;
            
            // Load from settings
            Categories = new List<string>(randomPrompts.Categories);
            Styles = new List<string>(randomPrompts.Styles);
            Moods = new List<string>(randomPrompts.Moods);
            
            // Populate UI
            PopulateCategoriesUI();
            PopulateStylesUI();
            PopulateMoodsUI();
        }

        private void PopulateCategoriesUI()
        {
            CategoriesPanel.Children.Clear();
            for (int i = 0; i < Categories.Count; i++)
            {
                var grid = CreateItemGrid(Categories[i], i, RemoveCategoryButton_Click);
                CategoriesPanel.Children.Add(grid);
            }
            UpdateExpanderHeaders();
        }

        private void PopulateStylesUI()
        {
            StylesPanel.Children.Clear();
            for (int i = 0; i < Styles.Count; i++)
            {
                var grid = CreateItemGrid(Styles[i], i, RemoveStyleButton_Click);
                StylesPanel.Children.Add(grid);
            }
            UpdateExpanderHeaders();
        }

        private void PopulateMoodsUI()
        {
            MoodsPanel.Children.Clear();
            for (int i = 0; i < Moods.Count; i++)
            {
                var grid = CreateItemGrid(Moods[i], i, RemoveMoodButton_Click);
                MoodsPanel.Children.Add(grid);
            }
            UpdateExpanderHeaders();
        }

        private void UpdateExpanderHeaders()
        {
            CategoriesExpander.Header = $"Categories ({Categories.Count})";
            StylesExpander.Header = $"Styles ({Styles.Count})";
            MoodsExpander.Header = $"Moods ({Moods.Count})";
        }

        private Grid CreateItemGrid(string text, int index, RoutedEventHandler removeHandler)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Margin = new Thickness(0, 0, 20, 0); // Right margin for scroll bar space

            var textBox = new TextBox
            {
                Text = text,
                Margin = new Thickness(0, 0, 10, 0)
            };
            textBox.TextChanged += (s, e) => UpdateTextInList(s as TextBox, index);
            
            // Fix mouse wheel scrolling by allowing events to bubble up to ScrollViewer
            textBox.PointerWheelChanged += (s, e) => {
                e.Handled = false; // Let the ScrollViewer handle it
            };
            Grid.SetColumn(textBox, 0);

            var removeButton = new Button
            {
                Content = "🗑️",
                Width = 40,
                Height = 30,
                Tag = index
            };
            
            // Add hover effect
            removeButton.PointerEntered += (s, e) => {
                if (s is Button btn) {
                    btn.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                }
            };
            
            removeButton.PointerExited += (s, e) => {
                if (s is Button btn) {
                    btn.Background = null; // Reset to default
                }
            };
            
            removeButton.Click += removeHandler;
            Grid.SetColumn(removeButton, 1);

            grid.Children.Add(textBox);
            grid.Children.Add(removeButton);

            return grid;
        }

        private void UpdateTextInList(TextBox? textBox, int index)
        {
            if (textBox == null) return;

            // Find which list this textbox belongs to by checking the parent
            var parent = textBox.Parent as Grid;
            var grandParent = parent?.Parent as StackPanel;

            if (grandParent == CategoriesPanel && index < Categories.Count)
            {
                Categories[index] = textBox.Text;
            }
            else if (grandParent == StylesPanel && index < Styles.Count)
            {
                Styles[index] = textBox.Text;
            }
            else if (grandParent == MoodsPanel && index < Moods.Count)
            {
                Moods[index] = textBox.Text;
            }
        }

        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            Categories.Add("New Category");
            PopulateCategoriesUI();
        }

        private void RemoveCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int index && index < Categories.Count)
            {
                Categories.RemoveAt(index);
                PopulateCategoriesUI();
            }
        }

        private void AddStyleButton_Click(object sender, RoutedEventArgs e)
        {
            Styles.Add("New Style");
            PopulateStylesUI();
        }

        private void RemoveStyleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int index && index < Styles.Count)
            {
                Styles.RemoveAt(index);
                PopulateStylesUI();
            }
        }

        private void AddMoodButton_Click(object sender, RoutedEventArgs e)
        {
            Moods.Add("New Mood");
            PopulateMoodsUI();
        }

        private void RemoveMoodButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int index && index < Moods.Count)
            {
                Moods.RemoveAt(index);
                PopulateMoodsUI();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = Settings.Instance;
            var randomPrompts = settings.RandomPrompts;
            
            // Clear and update settings from UI
            randomPrompts.Categories.Clear();
            randomPrompts.Categories.AddRange(Categories.Where(c => !string.IsNullOrWhiteSpace(c)));
            
            randomPrompts.Styles.Clear();
            randomPrompts.Styles.AddRange(Styles.Where(s => !string.IsNullOrWhiteSpace(s)));
            
            randomPrompts.Moods.Clear();
            randomPrompts.Moods.AddRange(Moods.Where(m => !string.IsNullOrWhiteSpace(m)));

            settings.Save();
            
            StatusTextBlock.Text = "Random prompt settings saved successfully!";
            
            // Navigate back to settings
            NavigateBack?.Invoke(this, EventArgs.Empty);
        }

        private void RestoreDefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a new RandomPromptsSettings to get the default values
            var defaultSettings = new RandomPromptsSettings();
            
            // Update local lists with default values
            Categories = new List<string>(defaultSettings.Categories);
            Styles = new List<string>(defaultSettings.Styles);
            Moods = new List<string>(defaultSettings.Moods);
            
            // Refresh the UI to show the restored defaults
            PopulateCategoriesUI();
            PopulateStylesUI();
            PopulateMoodsUI();
            
            StatusTextBlock.Text = "Default settings restored. Click 'Save Settings' to apply.";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Reload settings to discard changes
            LoadSettingsToUI();
            StatusTextBlock.Text = "";
            
            // Navigate back to settings
            NavigateBack?.Invoke(this, EventArgs.Empty);
        }
    }
}