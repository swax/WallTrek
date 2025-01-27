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
            label1 = new Label();
            PromptTextBox = new TextBox();
            label2 = new Label();
            ApiKeyTextBox = new TextBox();
            GenerateButton = new Button();
            CancelButton = new Button();
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
            PromptTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            PromptTextBox.Location = new Point(12, 32);
            PromptTextBox.Multiline = true;
            PromptTextBox.Name = "PromptTextBox";
            PromptTextBox.Size = new Size(473, 105);
            PromptTextBox.TabIndex = 1;
            PromptTextBox.KeyPress += TextBox_KeyPress;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 155);
            label2.Name = "label2";
            label2.Size = new Size(113, 20);
            label2.TabIndex = 2;
            label2.Text = "OpenAI API Key";
            // 
            // ApiKeyTextBox
            // 
            ApiKeyTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            ApiKeyTextBox.Location = new Point(12, 178);
            ApiKeyTextBox.Name = "ApiKeyTextBox";
            ApiKeyTextBox.Size = new Size(473, 27);
            ApiKeyTextBox.TabIndex = 3;
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
            // CancelButton
            // 
            CancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            CancelButton.Location = new Point(391, 224);
            CancelButton.Name = "CancelButton";
            CancelButton.Size = new Size(94, 29);
            CancelButton.TabIndex = 5;
            CancelButton.Text = "Cancel";
            CancelButton.UseVisualStyleBackColor = true;
            CancelButton.Click += CancelButton_Click;
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
            OpenFolderButton.Location = new Point(12, 224);
            OpenFolderButton.Name = "OpenFolderButton";
            OpenFolderButton.Size = new Size(120, 29);
            OpenFolderButton.TabIndex = 7;
            OpenFolderButton.Text = "Open Folder";
            OpenFolderButton.UseVisualStyleBackColor = true;
            OpenFolderButton.Click += OpenFolderButton_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(497, 265);
            Controls.Add(OpenFolderButton);
            Controls.Add(progressBar1);
            Controls.Add(CancelButton);
            Controls.Add(GenerateButton);
            Controls.Add(ApiKeyTextBox);
            Controls.Add(label2);
            Controls.Add(PromptTextBox);
            Controls.Add(label1);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form1";
            Text = "WallTrek";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox PromptTextBox;
        private Label label2;
        private TextBox ApiKeyTextBox;
        private Button GenerateButton;
        private Button CancelButton;
        private ProgressBar progressBar1;
        private Button OpenFolderButton;
    }
}
