namespace WallTrek
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            label1 = new Label();
            ApiKeyTextBox = new TextBox();
            SaveButton = new Button();
            CancelButton = new Button();
            autoGenerateCheckbox = new CheckBox();
            autoGenerateMinutes = new NumericUpDown();
            minutesLabel = new Label();
            startupCheckbox = new CheckBox();
            ((System.ComponentModel.ISupportInitialize)autoGenerateMinutes).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(113, 20);
            label1.Text = "OpenAI API Key";
            // 
            // ApiKeyTextBox
            // 
            ApiKeyTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            ApiKeyTextBox.Location = new Point(12, 32);
            ApiKeyTextBox.Name = "ApiKeyTextBox";
            ApiKeyTextBox.Size = new Size(360, 27);
            // 
            // autoGenerateCheckbox
            // 
            autoGenerateCheckbox.AutoSize = true;
            autoGenerateCheckbox.Location = new Point(12, 65);
            autoGenerateCheckbox.Name = "autoGenerateCheckbox";
            autoGenerateCheckbox.Size = new Size(170, 24);
            autoGenerateCheckbox.TabIndex = 2;
            autoGenerateCheckbox.Text = "Auto-generate every:";
            autoGenerateCheckbox.CheckedChanged += AutoGenerateCheckbox_CheckedChanged;
            // 
            // startupCheckbox
            // 
            startupCheckbox.AutoSize = true;
            startupCheckbox.Location = new Point(12, 98);
            startupCheckbox.Name = "startupCheckbox";
            startupCheckbox.Size = new Size(184, 24);
            startupCheckbox.TabIndex = 5;
            startupCheckbox.Text = "Launch on Windows startup";
            startupCheckbox.CheckedChanged += StartupCheckbox_CheckedChanged;
            // 
            // autoGenerateMinutes
            // 
            autoGenerateMinutes.Enabled = false;
            autoGenerateMinutes.Location = new Point(188, 65);
            autoGenerateMinutes.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
            autoGenerateMinutes.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            autoGenerateMinutes.Name = "autoGenerateMinutes";
            autoGenerateMinutes.Size = new Size(70, 27);
            autoGenerateMinutes.TabIndex = 3;
            autoGenerateMinutes.Value = new decimal(new int[] { 60, 0, 0, 0 });
            autoGenerateMinutes.TextChanged += AutoGenerateMinutes_TextChanged;
            autoGenerateMinutes.ValueChanged += AutoGenerateMinutes_ValueChanged;
            // 
            // minutesLabel
            // 
            minutesLabel.AutoSize = true;
            minutesLabel.Location = new Point(264, 69);
            minutesLabel.Name = "minutesLabel";
            minutesLabel.Size = new Size(61, 20);
            minutesLabel.TabIndex = 4;
            minutesLabel.Text = "minutes";
            // 
            // SaveButton
            // 
            SaveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            SaveButton.Location = new Point(178, 137);
            SaveButton.Name = "SaveButton";
            SaveButton.Size = new Size(94, 29);
            SaveButton.Text = "Save";
            SaveButton.Click += SaveButton_Click;
            // 
            // CancelButton
            // 
            CancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            CancelButton.Location = new Point(278, 137);
            CancelButton.Name = "CancelButton";
            CancelButton.Size = new Size(94, 29);
            CancelButton.Text = "Cancel";
            CancelButton.Click += CancelButton_Click;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(384, 178);
            Controls.Add(startupCheckbox);
            Controls.Add(minutesLabel);
            Controls.Add(autoGenerateMinutes);
            Controls.Add(autoGenerateCheckbox);
            Controls.Add(CancelButton);
            Controls.Add(SaveButton);
            Controls.Add(ApiKeyTextBox);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Settings";
            ((System.ComponentModel.ISupportInitialize)autoGenerateMinutes).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private Label label1;
        private TextBox ApiKeyTextBox;
        private Button SaveButton;
        private new Button CancelButton;
        private CheckBox autoGenerateCheckbox;
        private NumericUpDown autoGenerateMinutes;
        private Label minutesLabel;
        private CheckBox startupCheckbox;
    }
}
