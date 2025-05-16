using System.Windows.Forms;
using static SuperUtils.SuperParcel;

namespace SuperUtils
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
            Console.WriteLine("hefdafdas");
        }
        
        private void LoadClipboard()
        {
            this.clipboardDataParcel = new SuperParcel();
            panel4.Controls.Clear();

            bool found = false;

            // Check if the clipboard contains an image
            if (Clipboard.ContainsImage())
            {
                found = true;
                Image? image = Clipboard.GetImage();
                if (image != null)
                {
                    // Convert image to PNG byte array
                    using (MemoryStream ms = new MemoryStream())
                    {
                        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        byte[] imageData = ms.ToArray();
                        clipboardDataParcel.AddItem("image/png", imageData);
                    }

                    PictureBox pictureBox = new PictureBox
                    {
                        Image = image,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Dock = DockStyle.Fill,
                        AutoSize = true
                    };

                    panel4.Controls.Add(pictureBox);
                }
            }

            // Check if the clipboard contains text
            if (Clipboard.ContainsText())
            {
                found = true;
                string clipboardText = Clipboard.GetText();
                clipboardDataParcel.AddItem("text/plain", clipboardText);

                Panel container = new Panel
                {
                    //BackColor = Color.LightGray,
                    BackColor = Color.FromArgb(255, 30, 30, 30),
                    Padding = new Padding(5),
                    Margin = new Padding(5),
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink
                };

                Label label = new Label
                {
                    Text = clipboardText,
                    AutoSize = true,
                    ForeColor = Color.White
                };

                container.Controls.Add(label);
                panel4.Controls.Add(container);
            }

            if (!found)
            {
                Label label = new Label
                {
                    Text = "Nothing in Clipboard...",
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = true,
                    ForeColor = Color.CadetBlue,
                };

                panel4.Controls.Add(label);
            }
        }

        private Control SingleSelectedFileView(byte[] fileBytes, string mimeType, FileInfo fileInfo)
        {
            DebugConsole.Instance.WriteLine($"Creating file view for: {fileInfo.Name}, MimeType: {mimeType}");

            //Panel container = new Panel
            //{
            //    Width = 200,
            //    Height = selectedDataPanel.Height,
            //    BackColor = Color.Transparent,
            //    Margin = new Padding(10)
            //};

            var maxHeight = selectedDataPanel.Height - 30;

            if (mimeType.StartsWith("image"))
            {
                DebugConsole.Instance.WriteLine("Rendering as image preview.");
                using (var ms = new MemoryStream(fileBytes))
                {
                    var image = Image.FromStream(ms);

                    var height = maxHeight;
                    var width = ((image.Width * 1.0f) / image.Height) * height;

                    PictureBox pictureBox = new PictureBox
                    {
                        Image = image,
                        Width = (int)width,
                        Height = height,
                        Margin = new Padding(12, 0, 12, 0),
                        SizeMode = PictureBoxSizeMode.Zoom,
                    };
                    return pictureBox;
                }
            }
            else
            {
                DebugConsole.Instance.WriteLine("Rendering as generic file icon.");

                FlowLayoutPanel iconPanel = new FlowLayoutPanel
                {
                    Width = maxHeight,
                    Height = maxHeight,
                    Margin = new Padding(8, 0, 8, 0),
                    FlowDirection = FlowDirection.TopDown,
                    BorderStyle = BorderStyle.FixedSingle,
                };

                Label iconLabel = new Label
                {
                    Text = "🗎",
                    Font = new Font("Segoe UI Emoji", 16),
                    Margin = new Padding(0, 4, 0, 4),
                    Height = 40,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleCenter,
                    //ForeColor = Color.Black,
                };

                Label fileNameLabel = new Label
                {
                    Text = fileInfo.Name,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe", 8),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoEllipsis = true
                };

                iconPanel.Controls.Add(iconLabel);
                iconPanel.Controls.Add(fileNameLabel);

                return iconPanel;
            }
        }

        private void PreviewSelectedFiles()
        {
            DebugConsole.Instance.WriteLine("PreviewSelectedFiles called. Clearing panel...");

            selectedDataPanel.Controls.Clear();
            List<ParcelItem> parcelItems = selectedDataParcel.GetAllItems();

            if (parcelItems.Count == 0)
            {
                DebugConsole.Instance.WriteLine("No files selected.");
                Label label = new Label
                {
                    Text = "Nothing Selected...",
                    ForeColor = Color.LightGray,
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                };
                selectedDataPanel.Controls.Add(label);
            }
            else
            {
                DebugConsole.Instance.WriteLine($"Found {parcelItems.Count} files to preview.");

                // Important fix: do not dock the fileViewsPanel, let it size itself
                FlowLayoutPanel fileViewsPanel = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.LeftToRight,
                    AutoSize = true,            
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    WrapContents = false,
                    Height = selectedDataPanel.Height - 30 // Ensures it fills the height
                };

                Button clearAllButton = new Button
                {
                    Text = "🗑️",
                    ForeColor= Color.White,
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI Emoji", 12),
                    Height = selectedDataPanel.Height - 30,
                    Width = 50,
                    BackColor = Color.Firebrick,
                    TextAlign = ContentAlignment.MiddleCenter,
                    //FlatStyle = FlatStyle.Flat,
                };
                clearAllButton.Click += (object sender, EventArgs e) => 
                {
                    this.selectedDataParcel.Clear();
                    PreviewSelectedFiles();
                };

                fileViewsPanel.Controls.Add(clearAllButton);

                foreach (var item in parcelItems)
                {
                    try
                    {
                        DebugConsole.Instance.WriteLine($"Loading file: {(item.RawData as FileInfo)?.FullName}");
                        byte[] bytes = File.ReadAllBytes((item.RawData as FileInfo).FullName);
                        Control fileView = SingleSelectedFileView(bytes, item.MimeType, item.RawData as FileInfo);
                        fileViewsPanel.Controls.Add(fileView);
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.Instance.WriteLine($"Error loading file: {ex.Message}");
                    }
                }

                selectedDataPanel.Controls.Add(fileViewsPanel);
                DebugConsole.Instance.WriteLine("Finished setting up file previews.");
            }
        }


        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanel1 = new TableLayoutPanel();
            openFolderButton = new Button();
            tableLayoutPanel2 = new TableLayoutPanel();
            panel1 = new Panel();
            tableLayoutPanel4 = new TableLayoutPanel();
            flowLayoutPanel1 = new FlowLayoutPanel();
            label2 = new Label();
            selectedDataPanel = new Panel();
            selectDataButton = new Button();
            sendSelectedButton = new Button();
            panel3 = new Panel();
            tableLayoutPanel3 = new TableLayoutPanel();
            sendClipboardButton = new Button();
            flowLayoutPanel3 = new FlowLayoutPanel();
            label1 = new Label();
            refreshClipboardDataButton = new Button();
            panel4 = new Panel();
            flowLayoutPanel2 = new FlowLayoutPanel();
            label6 = new Label();
            connectionStatusLabel = new Label();
            label3 = new Label();
            label5 = new Label();
            parcelStatusLabel = new Label();
            label4 = new Label();
            label7 = new Label();
            parcelProgressLabel = new Label();
            retryConnectionButton = new Button();
            label9 = new Label();
            forceRefreshConnectionButton = new Button();
            label8 = new Label();
            killConnectionButton = new Button();
            label10 = new Label();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            panel1.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            panel3.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            flowLayoutPanel3.SuspendLayout();
            flowLayoutPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(openFolderButton, 0, 1);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 0);
            tableLayoutPanel1.Controls.Add(flowLayoutPanel2, 0, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 75F));
            tableLayoutPanel1.Size = new Size(756, 582);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // openFolderButton
            // 
            openFolderButton.Anchor = AnchorStyles.None;
            openFolderButton.Location = new Point(278, 460);
            openFolderButton.Name = "openFolderButton";
            openFolderButton.Size = new Size(200, 44);
            openFolderButton.TabIndex = 5;
            openFolderButton.Text = "Open Folder";
            openFolderButton.UseVisualStyleBackColor = true;
            openFolderButton.Click += openFolderButton_Click;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Controls.Add(panel1, 1, 0);
            tableLayoutPanel2.Controls.Add(panel3, 0, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(0, 0);
            tableLayoutPanel2.Margin = new Padding(0);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.Padding = new Padding(20);
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(756, 457);
            tableLayoutPanel2.TabIndex = 1;
            // 
            // panel1
            // 
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(tableLayoutPanel4);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(381, 23);
            panel1.Name = "panel1";
            panel1.Size = new Size(352, 411);
            panel1.TabIndex = 3;
            // 
            // tableLayoutPanel4
            // 
            tableLayoutPanel4.BackColor = Color.FromArgb(40, 40, 40);
            tableLayoutPanel4.ColumnCount = 1;
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Controls.Add(flowLayoutPanel1, 0, 0);
            tableLayoutPanel4.Controls.Add(selectedDataPanel, 0, 2);
            tableLayoutPanel4.Controls.Add(selectDataButton, 0, 1);
            tableLayoutPanel4.Controls.Add(sendSelectedButton, 0, 3);
            tableLayoutPanel4.Dock = DockStyle.Fill;
            tableLayoutPanel4.Location = new Point(0, 0);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 4;
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Absolute, 130F));
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel4.Size = new Size(350, 409);
            tableLayoutPanel4.TabIndex = 2;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(label2);
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.Location = new Point(3, 3);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(344, 44);
            flowLayoutPanel1.TabIndex = 18;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Dock = DockStyle.Fill;
            label2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label2.ForeColor = SystemColors.Control;
            label2.Location = new Point(3, 3);
            label2.Margin = new Padding(3, 3, 3, 0);
            label2.Name = "label2";
            label2.Size = new Size(105, 25);
            label2.TabIndex = 2;
            label2.Text = "Select Files";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // selectedDataPanel
            // 
            selectedDataPanel.AutoScroll = true;
            selectedDataPanel.Dock = DockStyle.Fill;
            selectedDataPanel.Location = new Point(3, 232);
            selectedDataPanel.Name = "selectedDataPanel";
            selectedDataPanel.Size = new Size(344, 124);
            selectedDataPanel.TabIndex = 17;
            // 
            // selectDataButton
            // 
            selectDataButton.AllowDrop = true;
            selectDataButton.BackgroundImageLayout = ImageLayout.Zoom;
            selectDataButton.Dock = DockStyle.Fill;
            selectDataButton.FlatAppearance.BorderColor = Color.DodgerBlue;
            selectDataButton.FlatAppearance.BorderSize = 4;
            selectDataButton.FlatAppearance.MouseDownBackColor = Color.RoyalBlue;
            selectDataButton.FlatAppearance.MouseOverBackColor = Color.DodgerBlue;
            selectDataButton.Location = new Point(3, 53);
            selectDataButton.Name = "selectDataButton";
            selectDataButton.Size = new Size(344, 173);
            selectDataButton.TabIndex = 16;
            selectDataButton.TextAlign = ContentAlignment.TopCenter;
            selectDataButton.UseVisualStyleBackColor = true;
            selectDataButton.Click += selectDataButton_Click;
            // 
            // sendSelectedButton
            // 
            sendSelectedButton.Dock = DockStyle.Fill;
            sendSelectedButton.Location = new Point(20, 362);
            sendSelectedButton.Margin = new Padding(20, 3, 20, 3);
            sendSelectedButton.Name = "sendSelectedButton";
            sendSelectedButton.Size = new Size(310, 44);
            sendSelectedButton.TabIndex = 13;
            sendSelectedButton.Text = "Send To Phone";
            sendSelectedButton.UseVisualStyleBackColor = true;
            sendSelectedButton.Click += sendSelectedButton_Click;
            // 
            // panel3
            // 
            panel3.BorderStyle = BorderStyle.FixedSingle;
            panel3.Controls.Add(tableLayoutPanel3);
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new Point(23, 23);
            panel3.Name = "panel3";
            panel3.Size = new Size(352, 411);
            panel3.TabIndex = 4;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.BackColor = Color.FromArgb(40, 40, 40);
            tableLayoutPanel3.ColumnCount = 1;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Controls.Add(sendClipboardButton, 0, 2);
            tableLayoutPanel3.Controls.Add(flowLayoutPanel3, 0, 0);
            tableLayoutPanel3.Controls.Add(panel4, 0, 1);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(0, 0);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 3;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel3.Size = new Size(350, 409);
            tableLayoutPanel3.TabIndex = 5;
            // 
            // sendClipboardButton
            // 
            sendClipboardButton.Dock = DockStyle.Fill;
            sendClipboardButton.Location = new Point(20, 362);
            sendClipboardButton.Margin = new Padding(20, 3, 20, 3);
            sendClipboardButton.Name = "sendClipboardButton";
            sendClipboardButton.Size = new Size(310, 44);
            sendClipboardButton.TabIndex = 3;
            sendClipboardButton.Text = "Send To Phone";
            sendClipboardButton.UseVisualStyleBackColor = true;
            sendClipboardButton.Click += sendClipboardButton_Click;
            // 
            // flowLayoutPanel3
            // 
            flowLayoutPanel3.Controls.Add(label1);
            flowLayoutPanel3.Controls.Add(refreshClipboardDataButton);
            flowLayoutPanel3.Dock = DockStyle.Fill;
            flowLayoutPanel3.Location = new Point(3, 3);
            flowLayoutPanel3.Name = "flowLayoutPanel3";
            flowLayoutPanel3.Size = new Size(344, 44);
            flowLayoutPanel3.TabIndex = 4;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Dock = DockStyle.Fill;
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label1.ForeColor = SystemColors.Control;
            label1.Location = new Point(3, 0);
            label1.Name = "label1";
            label1.Size = new Size(183, 34);
            label1.TabIndex = 2;
            label1.Text = "Computer Clipboard";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // refreshClipboardDataButton
            // 
            refreshClipboardDataButton.Location = new Point(189, 0);
            refreshClipboardDataButton.Margin = new Padding(0);
            refreshClipboardDataButton.Name = "refreshClipboardDataButton";
            refreshClipboardDataButton.Size = new Size(35, 34);
            refreshClipboardDataButton.TabIndex = 3;
            refreshClipboardDataButton.Text = "⟳";
            refreshClipboardDataButton.UseVisualStyleBackColor = true;
            refreshClipboardDataButton.Click += refreshClipboardDataButton_Click;
            // 
            // panel4
            // 
            panel4.Dock = DockStyle.Fill;
            panel4.Location = new Point(3, 53);
            panel4.Name = "panel4";
            panel4.Size = new Size(344, 303);
            panel4.TabIndex = 5;
            // 
            // flowLayoutPanel2
            // 
            flowLayoutPanel2.Controls.Add(label6);
            flowLayoutPanel2.Controls.Add(connectionStatusLabel);
            flowLayoutPanel2.Controls.Add(label3);
            flowLayoutPanel2.Controls.Add(label5);
            flowLayoutPanel2.Controls.Add(parcelStatusLabel);
            flowLayoutPanel2.Controls.Add(label4);
            flowLayoutPanel2.Controls.Add(label7);
            flowLayoutPanel2.Controls.Add(parcelProgressLabel);
            flowLayoutPanel2.Controls.Add(label9);
            flowLayoutPanel2.Controls.Add(retryConnectionButton);
            flowLayoutPanel2.Controls.Add(label8);
            flowLayoutPanel2.Controls.Add(forceRefreshConnectionButton);
            flowLayoutPanel2.Controls.Add(label10);
            flowLayoutPanel2.Controls.Add(killConnectionButton);
            flowLayoutPanel2.Dock = DockStyle.Fill;
            flowLayoutPanel2.Location = new Point(3, 510);
            flowLayoutPanel2.Name = "flowLayoutPanel2";
            flowLayoutPanel2.Size = new Size(750, 69);
            flowLayoutPanel2.TabIndex = 2;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Dock = DockStyle.Fill;
            label6.ForeColor = Color.White;
            label6.Location = new Point(3, 0);
            label6.Name = "label6";
            label6.Size = new Size(106, 25);
            label6.TabIndex = 27;
            label6.Text = "Connection:";
            label6.TextAlign = ContentAlignment.MiddleRight;
            // 
            // connectionStatusLabel
            // 
            connectionStatusLabel.AutoSize = true;
            connectionStatusLabel.Dock = DockStyle.Fill;
            connectionStatusLabel.ForeColor = Color.DodgerBlue;
            connectionStatusLabel.Location = new Point(115, 0);
            connectionStatusLabel.Name = "connectionStatusLabel";
            connectionStatusLabel.Size = new Size(141, 25);
            connectionStatusLabel.TabIndex = 28;
            connectionStatusLabel.Text = "$EMPTY_LABEL$";
            connectionStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Dock = DockStyle.Fill;
            label3.ForeColor = Color.Gray;
            label3.Location = new Point(262, 0);
            label3.Name = "label3";
            label3.Size = new Size(16, 25);
            label3.TabIndex = 29;
            label3.Text = "|";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Dock = DockStyle.Fill;
            label5.ForeColor = Color.White;
            label5.Location = new Point(284, 0);
            label5.Name = "label5";
            label5.Size = new Size(69, 25);
            label5.TabIndex = 30;
            label5.Text = "Parcels:";
            label5.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // parcelStatusLabel
            // 
            parcelStatusLabel.AutoSize = true;
            parcelStatusLabel.Dock = DockStyle.Fill;
            parcelStatusLabel.ForeColor = Color.DodgerBlue;
            parcelStatusLabel.Location = new Point(359, 0);
            parcelStatusLabel.Name = "parcelStatusLabel";
            parcelStatusLabel.Size = new Size(141, 25);
            parcelStatusLabel.TabIndex = 31;
            parcelStatusLabel.Text = "$EMPTY_LABEL$";
            parcelStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Dock = DockStyle.Fill;
            label4.ForeColor = Color.Gray;
            label4.Location = new Point(506, 0);
            label4.Name = "label4";
            label4.Size = new Size(16, 25);
            label4.TabIndex = 32;
            label4.Text = "|";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Dock = DockStyle.Fill;
            label7.ForeColor = Color.White;
            label7.Location = new Point(528, 0);
            label7.Name = "label7";
            label7.Size = new Size(85, 25);
            label7.TabIndex = 33;
            label7.Text = "Progress:";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // parcelProgressLabel
            // 
            parcelProgressLabel.AutoSize = true;
            parcelProgressLabel.Dock = DockStyle.Fill;
            parcelProgressLabel.ForeColor = Color.DodgerBlue;
            parcelProgressLabel.Location = new Point(3, 25);
            parcelProgressLabel.Name = "parcelProgressLabel";
            parcelProgressLabel.Size = new Size(141, 34);
            parcelProgressLabel.TabIndex = 34;
            parcelProgressLabel.Text = "$EMPTY_LABEL$";
            parcelProgressLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // retryConnectionButton
            // 
            retryConnectionButton.Location = new Point(169, 25);
            retryConnectionButton.Margin = new Padding(0);
            retryConnectionButton.Name = "retryConnectionButton";
            retryConnectionButton.Size = new Size(35, 34);
            retryConnectionButton.TabIndex = 36;
            retryConnectionButton.Text = "⟳";
            retryConnectionButton.UseVisualStyleBackColor = true;
            retryConnectionButton.Click += retryConnectionButton_Click;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Dock = DockStyle.Fill;
            label9.ForeColor = Color.Gray;
            label9.Location = new Point(150, 25);
            label9.Name = "label9";
            label9.Size = new Size(16, 34);
            label9.TabIndex = 37;
            label9.Text = "|";
            label9.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // forceRefreshConnectionButton
            // 
            forceRefreshConnectionButton.Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            forceRefreshConnectionButton.ForeColor = Color.Goldenrod;
            forceRefreshConnectionButton.Location = new Point(226, 25);
            forceRefreshConnectionButton.Margin = new Padding(0);
            forceRefreshConnectionButton.Name = "forceRefreshConnectionButton";
            forceRefreshConnectionButton.Size = new Size(35, 34);
            forceRefreshConnectionButton.TabIndex = 38;
            forceRefreshConnectionButton.Text = "⚠️";
            forceRefreshConnectionButton.UseVisualStyleBackColor = true;
            forceRefreshConnectionButton.Click += forceRefreshConnectionButton_Click;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Dock = DockStyle.Fill;
            label8.ForeColor = Color.Gray;
            label8.Location = new Point(207, 25);
            label8.Name = "label8";
            label8.Size = new Size(16, 34);
            label8.TabIndex = 39;
            label8.Text = "|";
            label8.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // killConnectionButton
            // 
            killConnectionButton.ForeColor = Color.Firebrick;
            killConnectionButton.Location = new Point(283, 25);
            killConnectionButton.Margin = new Padding(0);
            killConnectionButton.Name = "killConnectionButton";
            killConnectionButton.Size = new Size(35, 34);
            killConnectionButton.TabIndex = 40;
            killConnectionButton.Text = "\U0001f6d1";
            killConnectionButton.UseVisualStyleBackColor = true;
            killConnectionButton.Click += killConnectionButton_Click;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Dock = DockStyle.Fill;
            label10.ForeColor = Color.Gray;
            label10.Location = new Point(264, 25);
            label10.Name = "label10";
            label10.Size = new Size(16, 34);
            label10.TabIndex = 41;
            label10.Text = "|";
            label10.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(20, 20, 20);
            ClientSize = new Size(756, 582);
            Controls.Add(tableLayoutPanel1);
            Name = "MainForm";
            Text = "SuperUtils";
            Load += MainForm_Load;
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            panel1.ResumeLayout(false);
            tableLayoutPanel4.ResumeLayout(false);
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            panel3.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            flowLayoutPanel3.ResumeLayout(false);
            flowLayoutPanel3.PerformLayout();
            flowLayoutPanel2.ResumeLayout(false);
            flowLayoutPanel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private Panel panel1;
        private TableLayoutPanel tableLayoutPanel4;
        private Panel panel3;
        private TableLayoutPanel tableLayoutPanel3;
        private Button sendClipboardButton;
        private FlowLayoutPanel flowLayoutPanel3;
        private Label label1;
        private Button refreshClipboardDataButton;
        private Panel panel4;
        private FlowLayoutPanel flowLayoutPanel2;
        private Button openFolderButton;
        private Button sendSelectedButton;
        private Panel selectedDataPanel;
        private Button selectDataButton;
        private FlowLayoutPanel flowLayoutPanel1;
        private Label label2;
        private Label label6;
        private Label connectionStatusLabel;
        private Label label3;
        private Label label5;
        private Label parcelStatusLabel;
        private Label label4;
        private Label label7;
        private Label parcelProgressLabel;
        private Button retryConnectionButton;
        private Label label9;
        private Button forceRefreshConnectionButton;
        private Label label8;
        private Button killConnectionButton;
        private Label label10;
    }
}