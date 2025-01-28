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
            notifyIcon = new NotifyIcon(components);
            trayContextMenu = new ContextMenuStrip(components);
            quitMenuItem = new ToolStripMenuItem();
            SettingsButton = new Button();

            label1 = new Label();
            PromptTextBox = new TextBox();
            GenerateButton = new Button();
            CloseButton = new Button();
            progressBar1 = new ProgressBar();
            OpenFolderButton = new Button();
            SuspendLayout();
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
            PromptTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            PromptTextBox.Location = new Point(12, 32);
            PromptTextBox.Multiline = true;
            PromptTextBox.Name = "PromptTextBox";
            PromptTextBox.Size = new Size(473, 176);
            PromptTextBox.TabIndex = 1;
            PromptTextBox.KeyPress += TextBox_KeyPress;
            // 
            // GenerateButton
            // 
            GenerateButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            GenerateButton.Location = new Point(291, 224);
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
            CloseButton.Location = new Point(391, 224);
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
            progressBar1.Location = new Point(138, 224);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(147, 29);
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.Visible = false;
            progressBar1.TabIndex = 6;
            // 
            // OpenFolderButton
            // 
            OpenFolderButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            OpenFolderButton.Location = new Point(112, 224);
            OpenFolderButton.Name = "OpenFolderButton";
            OpenFolderButton.Size = new Size(120, 29);
            OpenFolderButton.TabIndex = 7;
            OpenFolderButton.Text = "Open Folder";
            OpenFolderButton.UseVisualStyleBackColor = true;
            OpenFolderButton.Click += OpenFolderButton_Click;
            // 
            // SettingsButton
            // 
            SettingsButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            SettingsButton.Location = new Point(12, 224);
            SettingsButton.Name = "SettingsButton";
            SettingsButton.Size = new Size(94, 29);
            SettingsButton.TabIndex = 8;
            SettingsButton.Text = "Settings";
            SettingsButton.UseVisualStyleBackColor = true;
            SettingsButton.Click += SettingsButton_Click;
            // 
            // notifyIcon
            // 
            notifyIcon.Text = "WallTrek";
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = trayContextMenu;
            notifyIcon.Icon = Icon;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            // 
            // trayContextMenu
            // 
            trayContextMenu.Items.AddRange(new ToolStripItem[] { quitMenuItem });
            // 
            // quitMenuItem
            // 
            quitMenuItem.Text = "Quit";
            quitMenuItem.Click += QuitMenuItem_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(497, 265);
            Controls.Add(SettingsButton);
            Controls.Add(OpenFolderButton);
            Controls.Add(progressBar1);
            Controls.Add(CloseButton);
            Controls.Add(GenerateButton);
            Controls.Add(PromptTextBox);
            Controls.Add(label1);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form1";
            Text = "WallTrek";
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox PromptTextBox;
        private Button GenerateButton;
        private Button CloseButton;
        private ProgressBar progressBar1;
        private Button OpenFolderButton;
        private NotifyIcon notifyIcon;
        private ContextMenuStrip trayContextMenu;
        private ToolStripMenuItem quitMenuItem;
        private Button SettingsButton;
    }
}
