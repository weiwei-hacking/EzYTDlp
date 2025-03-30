using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace EzYTDlp
{
    public partial class MainForm : Form
    {
        private List<YouTubeVideo> playlistVideos = new List<YouTubeVideo>();
        private string userUrl = string.Empty;
        private bool isPlaylist = false;
        private string ytDlpPath = string.Empty;
        private string ffmpegPath = string.Empty;

        public MainForm()
        {
            InitializeComponent();
            ExtractEmbeddedTools();
        }

        private void InitializeComponent()
        {
            this.Text = "YouTube下載器";
            this.Size = new Size(500, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label urlLabel = new Label
            {
                Text = "請輸入YouTube連結：",
                Location = new Point(20, 20),
                Size = new Size(150, 20)
            };

            TextBox urlTextBox = new TextBox
            {
                Location = new Point(20, 50),
                Size = new Size(450, 25),
                Name = "urlTextBox"
            };

            Button confirmButton = new Button
            {
                Text = "確認",
                Location = new Point(150, 100),
                Size = new Size(80, 30),
                Name = "confirmButton"
            };
            confirmButton.Click += new EventHandler(ConfirmButton_Click);

            Button exitButton = new Button
            {
                Text = "離開",
                Location = new Point(250, 100),
                Size = new Size(80, 30),
                Name = "exitButton"
            };
            exitButton.Click += new EventHandler(ExitButton_Click);

            this.Controls.Add(urlLabel);
            this.Controls.Add(urlTextBox);
            this.Controls.Add(confirmButton);
            this.Controls.Add(exitButton);
        }

        private void ExtractEmbeddedTools()
        {
            foreach (string resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())

            try
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), "YouTubeDownloaderTools");
                Directory.CreateDirectory(tempFolder);

                ytDlpPath = Path.Combine(tempFolder, "yt-dlp.exe");
                ffmpegPath = Path.Combine(tempFolder, "ffmpeg.exe");

                if (!File.Exists(ytDlpPath))
                {
                    using (Stream? resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("EzYTDlp.yt_dlp.exe"))
                    {
                        if (resource == null)
                        {
                            throw new Exception("無法找到嵌入資源 yt-dlp.exe，請檢查資源名稱是否正確。");
                        }
                        using (FileStream file = new FileStream(ytDlpPath, FileMode.Create, FileAccess.Write))
                        {
                            resource.CopyTo(file);
                        }
                    }
                }

                if (!File.Exists(ffmpegPath))
                {
                    using (Stream? resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("EzYTDlp.ffmpeg.exe"))
                    {
                        if (resource == null)
                        {
                            throw new Exception("無法找到嵌入資源 ffmpeg.exe，請檢查資源名稱是否正確。");
                        }
                        using (FileStream file = new FileStream(ffmpegPath, FileMode.Create, FileAccess.Write))
                        {
                            resource.CopyTo(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"無法提取工具: {ex.Message}");
                Application.Exit();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            try
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), "YouTubeDownloaderTools");
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
            }
            catch { /* 忽略清理錯誤 */ }
        }

        private async void ConfirmButton_Click(object? sender, EventArgs e)
        {
            if (sender == null) return;

            var urlTextBox = this.Controls["urlTextBox"] as TextBox;
            if (urlTextBox == null) return;

            userUrl = urlTextBox.Text.Trim();

            if (string.IsNullOrEmpty(userUrl))
            {
                MessageBox.Show("請輸入有效的連結");
                return;
            }

            isPlaylist = userUrl.Contains("list=");

            if (isPlaylist)
            {
                await FetchPlaylistInfo();
                ShowPlaylistSelectionForm();
            }
            else
            {
                playlistVideos.Clear();
                playlistVideos.Add(new YouTubeVideo { Url = userUrl, Title = "單個影片", IsSelected = true });
                ShowDownloadForm();
            }
        }

        private void ExitButton_Click(object? sender, EventArgs e)
        {
            if (sender == null) return;
            Application.Exit();
        }

        private async Task FetchPlaylistInfo()
        {
            playlistVideos.Clear();
            await Task.Delay(1000);
            playlistVideos.Add(new YouTubeVideo { Url = userUrl, Title = "播放列表視頻 1", ThumbnailUrl = string.Empty, IsSelected = true });
            playlistVideos.Add(new YouTubeVideo { Url = userUrl, Title = "播放列表視頻 2", ThumbnailUrl = string.Empty, IsSelected = false });
            playlistVideos.Add(new YouTubeVideo { Url = userUrl, Title = "播放列表視頻 3", ThumbnailUrl = string.Empty, IsSelected = false });
        }

        private void ShowPlaylistSelectionForm()
        {
            PlaylistSelectionForm playlistForm = new PlaylistSelectionForm(playlistVideos, this);
            this.Hide();
            playlistForm.Show();
        }

        public void ShowDownloadForm()
        {
            DownloadForm downloadForm = new DownloadForm(playlistVideos, this, ytDlpPath, ffmpegPath);
            this.Hide();
            downloadForm.Show();
        }

        public void ShowMainForm()
        {
            this.Show();
        }
    }

    public class PlaylistSelectionForm : Form
    {
        private List<YouTubeVideo> videos;
        private MainForm mainForm;
        private List<Panel> videoPanels = new List<Panel>();

        public PlaylistSelectionForm(List<YouTubeVideo> videos, MainForm mainForm)
        {
            this.videos = videos;
            this.mainForm = mainForm;
            InitializeComponent();
            PopulateVideoList();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (e.CloseReason == CloseReason.UserClosing && !mainForm.IsDisposed)
                mainForm.ShowMainForm();
        }

        private void InitializeComponent()
        {
            this.Text = "選擇要下載的視頻";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label titleLabel = new Label
            {
                Text = "點擊選擇要下載的視頻（藍色表示已選擇）：",
                Location = new Point(20, 20),
                Size = new Size(300, 20)
            };

            Panel videosPanel = new Panel
            {
                Location = new Point(20, 50),
                Size = new Size(550, 350),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                Name = "videosPanel"
            };

            Button confirmButton = new Button
            {
                Text = "確認",
                Location = new Point(150, 420),
                Size = new Size(80, 30)
            };
            confirmButton.Click += new EventHandler(ConfirmButton_Click);

            Button backButton = new Button
            {
                Text = "上一步",
                Location = new Point(250, 420),
                Size = new Size(80, 30)
            };
            backButton.Click += new EventHandler(BackButton_Click);

            // 添加「離開」按鈕
            Button exitButton = new Button
            {
                Text = "離開",
                Location = new Point(350, 420),
                Size = new Size(80, 30)
            };
            exitButton.Click += new EventHandler(ExitButton_Click);

            this.Controls.Add(titleLabel);
            this.Controls.Add(videosPanel);
            this.Controls.Add(confirmButton);
            this.Controls.Add(backButton);
            this.Controls.Add(exitButton);
        }

        private void PopulateVideoList()
        {
            var videosPanel = this.Controls["videosPanel"] as Panel;
            if (videosPanel == null) return;

            videosPanel.Controls.Clear();
            videoPanels.Clear();

            int yPos = 10;
            for (int i = 0; i < videos.Count; i++)
            {
                var video = videos[i];
                Panel videoPanel = new Panel
                {
                    Location = new Point(10, yPos),
                    Size = new Size(510, 60),
                    Tag = i
                };
                videoPanel.BackColor = video.IsSelected ? Color.LightBlue : SystemColors.Control;

                Panel thumbnailPanel = new Panel
                {
                    Location = new Point(5, 5),
                    Size = new Size(80, 50),
                    BackColor = Color.Gray
                };

                Label titleLabel = new Label
                {
                    Text = video.Title,
                    Location = new Point(95, 20),
                    Size = new Size(400, 20),
                    AutoEllipsis = true
                };

                videoPanel.Controls.Add(thumbnailPanel);
                videoPanel.Controls.Add(titleLabel);
                videoPanel.Click += new EventHandler(VideoPanel_Click);

                videosPanel.Controls.Add(videoPanel);
                videoPanels.Add(videoPanel);
                yPos += 70;
            }
        }

        private void VideoPanel_Click(object? sender, EventArgs e)
        {
            if (sender == null) return;
            Panel panel = (Panel)sender;
            if (panel.Tag == null) return;

            int index = (int)panel.Tag;
            videos[index].IsSelected = !videos[index].IsSelected;
            panel.BackColor = videos[index].IsSelected ? Color.LightBlue : SystemColors.Control;
        }

        private void ConfirmButton_Click(object? sender, EventArgs e)
        {
            if (sender == null) return;
            mainForm.ShowDownloadForm();
            this.Close();
        }

        private void BackButton_Click(object? sender, EventArgs e)
        {
            if (sender == null) return;
            mainForm.ShowMainForm();
            this.Close();
        }

        private void ExitButton_Click(object? sender, EventArgs e)
        {
            if (sender == null) return;
            Application.Exit();
        }
    }

    public class DownloadForm : Form
    {
        private List<YouTubeVideo> videos;
        private MainForm mainForm;
        private string downloadPath = string.Empty;
        private bool isDownloading = false;
        private TextBox logTextBox = null!;
        private string ytDlpPath;
        private string ffmpegPath;
        private Button videoButton = null!;
        private Button audioButton = null!;
        private Button exitButton = null!;

        public DownloadForm(List<YouTubeVideo> videos, MainForm mainForm, string ytDlpPath, string ffmpegPath)
        {
            this.videos = videos;
            this.mainForm = mainForm;
            this.ytDlpPath = ytDlpPath;
            this.ffmpegPath = ffmpegPath;
            InitializeComponent();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (e.CloseReason == CloseReason.UserClosing && !mainForm.IsDisposed)
                mainForm.ShowMainForm();
        }

        private void InitializeComponent()
        {
            this.Text = "下載中";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            logTextBox = new TextBox
            {
                Location = new Point(20, 20),
                Size = new Size(450, 280),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                HideSelection = true
            };

            videoButton = new Button
            {
                Text = "影片",
                Location = new Point(100, 320),
                Size = new Size(80, 30)
            };
            videoButton.Click += new EventHandler((s, e) => DownloadButton_Click(s, e, "mp4"));

            audioButton = new Button
            {
                Text = "音樂",
                Location = new Point(200, 320),
                Size = new Size(80, 30)
            };
            audioButton.Click += new EventHandler((s, e) => DownloadButton_Click(s, e, "mp3"));

            // 添加「離開」按鈕
            exitButton = new Button
            {
                Text = "離開",
                Location = new Point(300, 320),
                Size = new Size(80, 30)
            };
            exitButton.Click += new EventHandler(ExitButton_Click);

            this.Controls.Add(logTextBox);
            this.Controls.Add(videoButton);
            this.Controls.Add(audioButton);
            this.Controls.Add(exitButton);
        }

        private void DownloadButton_Click(object? sender, EventArgs e, string format)
        {
            if (sender == null || isDownloading) return;

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "選擇下載位置";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    downloadPath = dialog.SelectedPath;
                    logTextBox.Clear();
                    SetButtonsEnabled(false);
                    StartDownload(format);
                }
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            if (videoButton.InvokeRequired || audioButton.InvokeRequired || exitButton.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    videoButton.Enabled = enabled;
                    audioButton.Enabled = enabled;
                    // 離開按鈕始終啟用
                }));
            }
            else
            {
                videoButton.Enabled = enabled;
                audioButton.Enabled = enabled;
                // 離開按鈕始終啟用
            }
        }

        private async void StartDownload(string format)
        {
            if (string.IsNullOrEmpty(downloadPath)) return;

            isDownloading = true;
            List<YouTubeVideo> selectedVideos = videos.FindAll(v => v.IsSelected);

            if (selectedVideos.Count == 0)
            {
                MessageBox.Show("沒有選擇要下載的影片");
                isDownloading = false;
                SetButtonsEnabled(true);
                return;
            }

            foreach (var video in selectedVideos)
            {
                await DownloadVideo(video, format);
            }

            ShowNotification("下載完成", $"所有{(format == "mp4" ? "影片" : "音樂")}已下載完成");
            isDownloading = false;
            SetButtonsEnabled(true);
        }

        private async Task DownloadVideo(YouTubeVideo video, string format)
        {
            UpdateLog($"開始下載: {video.Title}");

            try
            {
                string formatOption = format == "mp4"
                    ? "-f bestvideo+bestaudio --merge-output-format mp4"
                    : "-x --audio-format mp3 --audio-quality 0";

                string outputTemplate = Path.Combine(downloadPath, "%(title)s.%(ext)s");
                string arguments = $"{video.Url} {formatOption} -o \"{outputTemplate}\" --ffmpeg-location \"{ffmpegPath}\" --newline";

                UpdateLog($"執行命令: yt-dlp {arguments}");

                using (Process process = new Process())
                {
                    process.StartInfo.FileName = ytDlpPath;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    string lastProgressLine = "";
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null) return;
                        if (e.Data.Contains("[download]") && e.Data.Contains("%"))
                        {
                            if (e.Data != lastProgressLine)
                            {
                                lastProgressLine = e.Data;
                                UpdateLog(e.Data);
                            }
                        }
                        else
                        {
                            UpdateLog(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                            UpdateLog("錯誤: " + e.Data);
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();
                }
            }
            catch (Exception ex)
            {
                UpdateLog($"錯誤: {ex.Message}");
            }
        }

        private void UpdateLog(string message)
        {
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() =>
                {
                    logTextBox.AppendText(message + Environment.NewLine);
                    logTextBox.ScrollToCaret();
                }));
            }
            else
            {
                logTextBox.AppendText(message + Environment.NewLine);
                logTextBox.ScrollToCaret();
            }
        }

        private void ShowNotification(string title, string message)
        {
            NotifyIcon notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = SystemIcons.Information,
                BalloonTipTitle = title,
                BalloonTipText = message
            };

            notifyIcon.BalloonTipClicked += (s, e) =>
            {
                if (s == null) return;
                Process.Start("explorer.exe", downloadPath);
                notifyIcon.Dispose();
            };

            notifyIcon.ShowBalloonTip(5000);

            Timer timer = new Timer();
            timer.Interval = 6000;
            timer.Tick += (s, e) =>
            {
                if (s == null) return;
                notifyIcon.Dispose();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private void ExitButton_Click(object? sender, EventArgs e)
        {
            if (sender == null) return;

            // 如果正在下載，提示使用者是否確定離開
            if (isDownloading)
            {
                DialogResult result = MessageBox.Show(
                    "目前正在下載中，確定要離開嗎？這將中斷下載。",
                    "確認離開",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return; // 使用者選擇「否」，不退出
            }

            Application.Exit();
        }
    }

    public class YouTubeVideo
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}