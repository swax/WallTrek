// LlmModelSelector.xaml.cs
using System;
using Microsoft.UI.Xaml.Controls;
using WallTrek.Services;
using WallTrek.Services.TextGen;

namespace WallTrek.Views.Controls
{
    /// <summary>
    /// A reusable single-select LLM picker backed by <see cref="LlmModelCatalog"/>. Mirrors the
    /// main page's LLM dropdown (model name + per-prompt cost) but only ever holds one selection.
    /// It defaults to the saved <see cref="Settings.SelectedLlmModel"/> and does not write back to
    /// settings — callers read <see cref="SelectedModelId"/> for a one-off generation.
    /// </summary>
    public sealed partial class LlmModelSelector : UserControl
    {
        public LlmModelSelector()
        {
            this.InitializeComponent();
            ModelComboBox.ItemsSource = LlmModelCatalog.Options;
            ModelComboBox.SelectedItem =
                LlmModelCatalog.FindById(Settings.Instance.SelectedLlmModel) ?? LlmModelCatalog.Default;
        }

        /// <summary>Raised when the chosen model changes.</summary>
        public event EventHandler? SelectionChanged;

        /// <summary>The currently selected model (falls back to the catalog default).</summary>
        public LlmModelOption SelectedModel =>
            ModelComboBox.SelectedItem as LlmModelOption ?? LlmModelCatalog.Default;

        /// <summary>API id of the currently selected model.</summary>
        public string SelectedModelId => SelectedModel.ModelId;

        private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
