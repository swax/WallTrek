namespace WallTrek
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            SettingsButton = new LinkLabel();
            label1 = new Label();
            PromptTextBox = new TextBox();
            GenerateButton = new Button();
            CloseButton = new Button();
            progressBar1 = new ProgressBar();
            OpenFolderButton = new LinkLabel();
            autoGenerateCheckbox = new CheckBox();
            autoGenerateMinutes = new NumericUpDown();
            minutesLabel = new Label();
            nextGenerateLabel = new Label();
            ((System.ComponentModel.ISupportInitialize)autoGenerateMinutes).BeginInit();
            SuspendLayout();
            // 
            // SettingsButton
            // 
            SettingsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            SettingsButton.LinkBehavior = LinkBehavior.HoverUnderline;
            SettingsButton.Location = new Point(479, 9);
            SettingsButton.Name = "SettingsButton";
            SettingsButton.Size = new Size(75, 20);
            SettingsButton.TabIndex = 8;
            SettingsButton.TabStop = true;
            SettingsButton.Text = "Settings";
            SettingsButton.TextAlign = ContentAlignment.TopRight;
            SettingsButton.Click += SettingsButton_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(58, 20);
            label1.TabIndex = 0;
            label1.Text = "Prompt";
            // 
            // PromptTextBox
            // 
            PromptTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            PromptTextBox.Location = new Point(12, 32);
            PromptTextBox.Multiline = true;
            PromptTextBox.Name = "PromptTextBox";
            PromptTextBox.Size = new Size(542, 82);
            PromptTextBox.TabIndex = 1;
            PromptTextBox.KeyPress += TextBox_KeyPress;
            // 
            // GenerateButton
            // 
            GenerateButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            GenerateButton.Location = new Point(360, 135);
            GenerateButton.Name = "GenerateButton";
            GenerateButton.Size = new Size(94, 29);
            GenerateButton.TabIndex = 4;
            GenerateButton.Text = "Generate";
            GenerateButton.UseVisualStyleBackColor = true;
            GenerateButton.Click += GenerateButton_Click;
            // 
            // CloseButton
            // 
            CloseButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            CloseButton.Location = new Point(460, 135);
            CloseButton.Name = "CloseButton";
            CloseButton.Size = new Size(94, 29);
            CloseButton.TabIndex = 5;
            CloseButton.Text = "Close";
            CloseButton.UseVisualStyleBackColor = true;
            CloseButton.Click += CloseButton_Click;
            // 
            // progressBar1
            // 
            progressBar1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            progressBar1.Location = new Point(-2, 185);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(569, 10);
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.TabIndex = 6;
            progressBar1.Visible = false;
            // 
            // OpenFolderButton
            // 
            OpenFolderButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            OpenFolderButton.LinkBehavior = LinkBehavior.HoverUnderline;
            OpenFolderButton.Location = new Point(324, 9);
            OpenFolderButton.Name = "OpenFolderButton";
            OpenFolderButton.Size = new Size(149, 20);
            OpenFolderButton.TabIndex = 7;
            OpenFolderButton.TabStop = true;
            OpenFolderButton.Text = "Open Image Folder";
            OpenFolderButton.TextAlign = ContentAlignment.TopRight;
            OpenFolderButton.Click += OpenFolderButton_Click;
            // 
            // autoGenerateCheckbox
            // 
            autoGenerateCheckbox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            autoGenerateCheckbox.AutoSize = true;
            autoGenerateCheckbox.Location = new Point(12, 135);
            autoGenerateCheckbox.Name = "autoGenerateCheckbox";
            autoGenerateCheckbox.Size = new Size(170, 24);
            autoGenerateCheckbox.TabIndex = 4;
            autoGenerateCheckbox.Text = "Auto-generate every:";
            autoGenerateCheckbox.CheckedChanged += AutoGenerateCheckbox_CheckedChanged;
            // 
            // autoGenerateMinutes
            // 
            autoGenerateMinutes.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            autoGenerateMinutes.Enabled = false;
            autoGenerateMinutes.Location = new Point(188, 135);
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
            minutesLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            minutesLabel.AutoSize = true;
            minutesLabel.Location = new Point(264, 139);
            minutesLabel.Name = "minutesLabel";
            minutesLabel.Size = new Size(61, 20);
            minutesLabel.TabIndex = 2;
            minutesLabel.Text = "minutes";
            // 
            // nextGenerateLabel
            // 
            nextGenerateLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            nextGenerateLabel.AutoSize = true;
            nextGenerateLabel.Location = new Point(12, 166);
            nextGenerateLabel.Name = "nextGenerateLabel";
            nextGenerateLabel.Size = new Size(0, 20);
            nextGenerateLabel.TabIndex = 1;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(566, 196);
            Controls.Add(nextGenerateLabel);
            Controls.Add(minutesLabel);
            Controls.Add(autoGenerateMinutes);
            Controls.Add(autoGenerateCheckbox);
            Controls.Add(SettingsButton);
            Controls.Add(OpenFolderButton);
            Controls.Add(progressBar1);
            Controls.Add(CloseButton);
            Controls.Add(GenerateButton);
            Controls.Add(PromptTextBox);
            Controls.Add(label1);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MainForm";
            ShowInTaskbar = false;
            Text = "WallTrek";
            WindowState = FormWindowState.Minimized;
            ((System.ComponentModel.ISupportInitialize)autoGenerateMinutes).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        public static Icon GetEmbeddedIcon()
        {
            using var stream = typeof(MainForm).Assembly.GetManifestResourceStream("WallTrek.walltrek.ico");
            return new Icon(stream);
        }

        #endregion

        private Label label1;
        private TextBox PromptTextBox;
        private Button GenerateButton;
        private Button CloseButton;
        private ProgressBar progressBar1;
        private LinkLabel OpenFolderButton;
        private LinkLabel SettingsButton;
        private CheckBox autoGenerateCheckbox;
        private NumericUpDown autoGenerateMinutes;
        private Label minutesLabel;
        private Label nextGenerateLabel;
    }
}
