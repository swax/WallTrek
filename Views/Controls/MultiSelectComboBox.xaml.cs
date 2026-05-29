// MultiSelectComboBox.xaml.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WallTrek.Views.Controls
{
    /// <summary>
    /// Wraps a catalog option with an observable checked state for the multi-select dropdown
    /// rows. <see cref="ContentTemplate"/> carries the host-supplied template so each row can
    /// render the underlying option's columns.
    /// </summary>
    public class SelectableOption : INotifyPropertyChanged
    {
        public object Item { get; init; } = null!;
        public DataTemplate? ContentTemplate { get; init; }

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// A ComboBox-like dropdown supporting two interaction modes from one list: clicking a row
    /// selects only that row and closes the dropdown (exclusive, like a ComboBox), while clicking
    /// a row's checkbox toggles it and keeps the dropdown open (multi-select). The collapsed header
    /// shows the single selection via <see cref="ItemTemplate"/>, or "N selected · ~avg¢" when
    /// several are checked. At least one row is always kept selected.
    /// </summary>
    public sealed partial class MultiSelectComboBox : UserControl
    {
        private readonly ObservableCollection<SelectableOption> _wrappers = new();

        public MultiSelectComboBox()
        {
            this.InitializeComponent();
            ItemsListView.ItemsSource = _wrappers;
        }

        /// <summary>Raised whenever the set of checked options changes.</summary>
        public event EventHandler? SelectionChanged;

        /// <summary>Extracts an option's approximate cost in cents for the averaged summary line.</summary>
        public Func<object, decimal>? CostSelector { get; set; }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource), typeof(IEnumerable), typeof(MultiSelectComboBox),
                new PropertyMetadata(null, (d, _) => ((MultiSelectComboBox)d).RebuildWrappers()));

        public IEnumerable? ItemsSource
        {
            get => (IEnumerable?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(
                nameof(ItemTemplate), typeof(DataTemplate), typeof(MultiSelectComboBox),
                new PropertyMetadata(null, (d, _) => ((MultiSelectComboBox)d).RebuildWrappers()));

        public DataTemplate? ItemTemplate
        {
            get => (DataTemplate?)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>The underlying options that are currently checked, in catalog order.</summary>
        public IReadOnlyList<object> SelectedItems =>
            _wrappers.Where(w => w.IsChecked).Select(w => w.Item).ToList();

        /// <summary>Checks exactly the rows whose underlying option is in <paramref name="items"/>.</summary>
        public void SetSelectedItems(IEnumerable<object> items)
        {
            var set = new HashSet<object>(items);
            foreach (var w in _wrappers)
            {
                w.IsChecked = set.Contains(w.Item);
            }
            UpdateHeader();
        }

        private void RebuildWrappers()
        {
            // Preserve any checked items across a rebuild (e.g. ItemsSource/ItemTemplate set in any order).
            var previouslyChecked = new HashSet<object>(_wrappers.Where(w => w.IsChecked).Select(w => w.Item));
            _wrappers.Clear();

            if (ItemsSource != null)
            {
                foreach (var item in ItemsSource)
                {
                    _wrappers.Add(new SelectableOption
                    {
                        Item = item,
                        ContentTemplate = ItemTemplate,
                        IsChecked = previouslyChecked.Contains(item)
                    });
                }
            }

            UpdateHeader();
        }

        private void ItemsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // A bare row click behaves like a ComboBox: select only this row, then collapse.
            if (e.ClickedItem is SelectableOption clicked)
            {
                foreach (var w in _wrappers)
                {
                    w.IsChecked = ReferenceEquals(w, clicked);
                }
                RaiseSelectionChanged();
                DropFlyout.Hide();
            }
        }

        private void ItemCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // The checkbox keeps the dropdown open for multi-select, but the selection is never
            // allowed to become empty.
            if (sender is FrameworkElement fe && fe.DataContext is SelectableOption wrapper)
            {
                if (!wrapper.IsChecked && _wrappers.All(w => !w.IsChecked))
                {
                    wrapper.IsChecked = true; // re-check: always keep at least one selected
                    return;
                }
            }
            RaiseSelectionChanged();
        }

        private void RaiseSelectionChanged()
        {
            UpdateHeader();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateHeader()
        {
            if (SingleContent is null || SummaryBlock is null)
            {
                return; // template not realized yet
            }

            var selected = _wrappers.Where(w => w.IsChecked).ToList();

            if (selected.Count == 1)
            {
                SingleContent.ContentTemplate = ItemTemplate;
                SingleContent.Content = selected[0].Item;
                SingleContent.Visibility = Visibility.Visible;
                SummaryBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                SingleContent.Content = null;
                SingleContent.Visibility = Visibility.Collapsed;
                SummaryBlock.Text = BuildSummary(selected);
                SummaryBlock.Visibility = Visibility.Visible;
            }
        }

        private string BuildSummary(IReadOnlyList<SelectableOption> selected)
        {
            if (selected.Count == 0)
            {
                return "None selected";
            }

            if (CostSelector != null)
            {
                var avg = selected.Average(w => CostSelector(w.Item));
                return $"{selected.Count} selected  ·  ~{avg:0}¢ avg";
            }

            return $"{selected.Count} selected";
        }
    }
}
