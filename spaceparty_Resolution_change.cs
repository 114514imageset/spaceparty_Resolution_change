using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SpacePartyTool
{
    static class Program
    {
        static string regPath = @"SOFTWARE\je\太空杀spaceparty.cn";
        static string backupFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "1.reg");

        static Form form;
        static Label lblStatus;
        static Label lblBackupStatus;
        static Label lblBackupRes;

        static string currentVersion = "4.5";
        static string versionUrl = "https://ghproxy.net/https://raw.githubusercontent.com/114514imageset/spaceparty_Resolution_change/main/Version.txt";
        static string updateUrl = "https://github.com/114514imageset/spaceparty_Resolution_change/releases";

        // 引入底层 API 用于 MessageBox 居中
        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern IntPtr LoadCursorFromFile(string lpFileName);

        static Cursor GetSystemCursor()
        {
            try {
                string cursorPath = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors", "", null) as string;
                if (!string.IsNullOrEmpty(cursorPath)) {
                    IntPtr hCursor = LoadCursorFromFile(cursorPath);
                    if (hCursor != IntPtr.Zero) return new Cursor(hCursor);
                }
            } catch { }
            return Cursors.Default;
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string msg = "欢迎使用 太空杀分辨率切换工具 " + currentVersion + "！\n\n" +
                         "【提示】修改前请确保游戏已完全关闭。\n" +
                         "【注意】本工具仅修改本地画面参数，若出现异常请使用选项 1 还原。\n\n" +
                         "作者主页：https://space.bilibili.com/1392176543\n" +
                         "用 DeepSeek 做的，尽力了\n\n" +
                         "是否打开作者的主页？";

            if (ShowSystemMessage(msg, "关于软件", true) == DialogResult.Yes)
            {
                Process.Start("https://space.bilibili.com/1392176543");
            }

            form = new Form();
            form.Text = "太空杀 - 分辨率切换工具 v" + currentVersion;
            form.Size = new Size(400, 380);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.FormBorderStyle = FormBorderStyle.None;
            form.BackColor = Color.White;
            form.Cursor = GetSystemCursor();
            form.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, form.Width, form.Height, 10, 10));

            form.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left) {
                    ReleaseCapture();
                    SendMessage(form.Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero);
                }
            };

            try { form.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            Label lblClose = new Label();
            lblClose.Text = "✕";
            lblClose.Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold);
            lblClose.ForeColor = Color.Gray;
            lblClose.Location = new Point(365, 10);
            lblClose.AutoSize = true;
            lblClose.Cursor = Cursors.Hand;
            lblClose.MouseEnter += (s, e) => { lblClose.ForeColor = Color.Red; };
            lblClose.MouseLeave += (s, e) => { lblClose.ForeColor = Color.Gray; };
            lblClose.Click += (s, e) => { Application.Exit(); };
            form.Controls.Add(lblClose);

            lblBackupRes = new Label();
            lblBackupRes.Location = new Point(30, 15);
            lblBackupRes.Size = new Size(320, 20);
            lblBackupRes.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            form.Controls.Add(lblBackupRes);

            Label lblTitle = new Label();
            lblTitle.Text = "请选择要执行的操作：";
            lblTitle.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            lblTitle.Location = new Point(30, 45);
            lblTitle.Size = new Size(300, 30);
            form.Controls.Add(lblTitle);

            Button btn1 = CreateStyledButton("1. 备份 / 还原原本分辨率", 85);
            btn1.Click += new EventHandler(Btn1_Click);
            form.Controls.Add(btn1);

            Button btn2 = CreateStyledButton("2. 一键切换超宽分辨率", 135);
            btn2.Click += new EventHandler(Btn2_Click);
            form.Controls.Add(btn2);

            Button btn3 = CreateStyledButton("3. 自定义宽高修改", 185);
            btn3.Click += new EventHandler(Btn3_Click);
            form.Controls.Add(btn3);

            Button btn4 = CreateStyledButton("4. 删除本地备份", 235);
            btn4.Click += new EventHandler(Btn4_Click);
            form.Controls.Add(btn4);

            lblStatus = new Label();
            lblStatus.Location = new Point(20, 300);
            lblStatus.Size = new Size(200, 20);
            lblStatus.ForeColor = Color.DimGray;
            lblStatus.Font = new Font("微软雅黑", 9F, FontStyle.Regular);
            form.Controls.Add(lblStatus);

            lblBackupStatus = new Label();
            lblBackupStatus.Location = new Point(220, 300);
            lblBackupStatus.Size = new Size(150, 20);
            lblBackupStatus.TextAlign = ContentAlignment.MiddleRight;
            lblBackupStatus.Font = new Font("微软雅黑", 9F, FontStyle.Regular);
            form.Controls.Add(lblBackupStatus);

            UpdateAllDisplays();

            form.Shown += (s, e) => {
                Task.Run(() => {
                    System.Threading.Thread.Sleep(1500); 
                    CheckForUpdates();
                });
            };

            Application.Run(form);
        }

        // 【核心破解】使用系统原生 MessageBox，并强行居中到主界面
        static DialogResult ShowSystemMessage(string text, string caption, bool isYesNo)
        {
            // 启动一个定时器，在弹窗出现后立刻把它拉回屏幕中央
            Timer timer = new Timer();
            timer.Interval = 50;
            timer.Tick += (s, e) => {
                IntPtr hWnd = FindWindow(null, caption);
                if (hWnd != IntPtr.Zero)
                {
                    RECT rect;
                    GetWindowRect(hWnd, out rect);
                    int w = rect.Right - rect.Left;
                    int h = rect.Bottom - rect.Top;
                    
                    int screenW = Screen.PrimaryScreen.WorkingArea.Width;
                    int screenH = Screen.PrimaryScreen.WorkingArea.Height;
                    
                    SetWindowPos(hWnd, IntPtr.Zero, (screenW - w) / 2, (screenH - h) / 2, 0, 0, 0x0001);
                    timer.Stop();
                }
            };
            timer.Start();

            if (isYesNo) return MessageBox.Show(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            else return MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        static Button CreateStyledButton(string text, int top) {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new Point(30, top);
            btn.Size = new Size(320, 35);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.LightGray;
            btn.BackColor = Color.White;
            btn.Font = new Font("微软雅黑", 10F, FontStyle.Regular);
            btn.Cursor = Cursors.Hand;
            btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(245, 245, 245); };
            btn.MouseLeave += (s, e) => { btn.BackColor = Color.White; };
            return btn;
        }

        static void CheckForUpdates()
        {
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0");
                    string url = versionUrl + "?t=" + DateTime.Now.Ticks;
                    string remoteVersionStr = client.DownloadString(url).Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("\t", "");
                    Version remoteVer;
                    Version currentVer;
                    if (Version.TryParse(remoteVersionStr, out remoteVer) && Version.TryParse(currentVersion, out currentVer))
                    {
                        if (remoteVer > currentVer)
                        {
                            form.Invoke(new Action(() => {
                                DialogResult result = ShowSystemMessage(
                                    "发现新版本！\n\n当前版本：" + currentVersion + "\n最新版本：" + remoteVersionStr + "\n\n是否前往 GitHub 下载最新版？",
                                    "版本更新提醒", true);
                                if (result == DialogResult.Yes) Process.Start(updateUrl);
                            }));
                        }
                    }
                }
            }
            catch { }
        }

        static void UpdateAllDisplays()
        {
            UpdateResolutionDisplay();
            UpdateBackupStatus();
            UpdateBackupResDisplay();
        }

        static void UpdateResolutionDisplay()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath))
                {
                    if (key != null)
                    {
                        object w = key.GetValue("Screenmanager Resolution Width_h182942802");
                        object h = key.GetValue("Screenmanager Resolution Height_h2627697771");
                        if (w != null && h != null) { lblStatus.Text = "当前游戏分辨率: " + w.ToString() + " x " + h.ToString(); return; }
                    }
                    lblStatus.Text = "当前游戏分辨率: 未找到";
                }
            }
            catch { lblStatus.Text = "当前游戏分辨率: 读取失败"; }
        }

        static void UpdateBackupStatus()
        {
            if (File.Exists(backupFile)) { lblBackupStatus.Text = "备份状态: 已备份"; lblBackupStatus.ForeColor = Color.Green; }
            else { lblBackupStatus.Text = "备份状态: 无备份"; lblBackupStatus.ForeColor = Color.Red; }
        }

        static void UpdateBackupResDisplay()
        {
            if (!File.Exists(backupFile)) { lblBackupRes.Text = "备份分辨率: 未备份"; lblBackupRes.ForeColor = Color.Red; return; }
            try
            {
                string content = File.ReadAllText(backupFile);
                System.Text.RegularExpressions.Match mW = System.Text.RegularExpressions.Regex.Match(content, @"Screenmanager Resolution Width_h182942802""=dword:([0-9a-fA-F]{8})");
                System.Text.RegularExpressions.Match mH = System.Text.RegularExpressions.Regex.Match(content, @"Screenmanager Resolution Height_h2627697771""=dword:([0-9a-fA-F]{8})");
                if (mW.Success && mH.Success)
                {
                    int w = Convert.ToInt32(mW.Groups[1].Value, 16);
                    int h = Convert.ToInt32(mH.Groups[1].Value, 16);
                    lblBackupRes.Text = "备份分辨率: " + w.ToString() + " x " + h.ToString();
                    lblBackupRes.ForeColor = Color.Green;
                }
                else { lblBackupRes.Text = "备份分辨率: 已备份"; lblBackupRes.ForeColor = Color.Green; }
            }
            catch { lblBackupRes.Text = "备份分辨率: 已备份"; lblBackupRes.ForeColor = Color.Green; }
        }

        static void Btn1_Click(object sender, EventArgs e)
        {
            if (File.Exists(backupFile))
            {
                Process.Start("reg", "import \"" + backupFile + "\"").WaitForExit();
                ShowSystemMessage("原本分辨率还原成功！", "成功", false);
                UpdateAllDisplays();
            }
            else
            {
                Process.Start("reg", "export \"HKEY_CURRENT_USER\\" + regPath + "\" \"" + backupFile + "\" /y").WaitForExit();
                ShowSystemMessage("备份成功！已保存在软件同目录下的 1.reg", "成功", false);
                UpdateAllDisplays();
            }
        }

        static void Btn2_Click(object sender, EventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regPath))
                {
                    key.SetValue("Screenmanager Resolution Width_h182942802", 1914, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Height_h2627697771", 536, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Window Width_h2524650974", 1914, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Window Height_h1684712807", 536, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Width Default_h680557497", 1914, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Height Default_h1380706816", 536, RegistryValueKind.DWord);
                    key.SetValue("ResolutionNew_h3187609911", 2, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Fullscreen mode Default_h401710285", 3, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Fullscreen mode_h3630240806", 3, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Use Native Default_h1405981789", 1, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Use Native_h1405027254", 0, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Stereo 3D_h1665754519", 1, RegistryValueKind.DWord);
                }
                ShowSystemMessage("超宽分辨率已开启！直接进游戏即可生效。", "成功", false);
                UpdateAllDisplays();
            }
            catch
            {
                ShowSystemMessage("写入失败，请右键以管理员身份运行！", "错误", false);
            }
        }

        static void Btn3_Click(object sender, EventArgs e)
        {
            string w = ShowInputBox("请输入宽度 (例如 1920):", "自定义宽度");
            if (string.IsNullOrEmpty(w)) return;
            string h = ShowInputBox("请输入高度 (例如 1080):", "自定义高度");
            if (string.IsNullOrEmpty(h)) return;

            int iw = 0; int ih = 0;
            if (int.TryParse(w, out iw) && int.TryParse(h, out ih))
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regPath))
                    {
                        key.SetValue("Screenmanager Resolution Width_h182942802", iw, RegistryValueKind.DWord);
                        key.SetValue("Screenmanager Resolution Height_h2627697771", ih, RegistryValueKind.DWord);
                        key.SetValue("Screenmanager Resolution Window Width_h2524650974", iw, RegistryValueKind.DWord);
                        key.SetValue("Screenmanager Resolution Window Height_h1684712807", ih, RegistryValueKind.DWord);
                        key.SetValue("Screenmanager Resolution Width Default_h680557497", iw, RegistryValueKind.DWord);
                        key.SetValue("Screenmanager Resolution Height Default_h1380706816", ih, RegistryValueKind.DWord);
                        key.SetValue("ResolutionNew_h3187609911", 2, RegistryValueKind.DWord);
                    }
                    ShowSystemMessage("自定义分辨率修改成功！", "成功", false);
                    UpdateAllDisplays();
                }
                catch
                {
                    ShowSystemMessage("写入失败，请右键以管理员身份运行！", "错误", false);
                }
            }
            else
            {
                ShowSystemMessage("请输入有效的数字！", "错误", false);
            }
        }

        static void Btn4_Click(object sender, EventArgs e)
        {
            if (File.Exists(backupFile))
            {
                if (ShowSystemMessage("确定要删除备份吗？删除后无法还原！", "确认删除", true) == DialogResult.Yes)
                {
                    File.Delete(backupFile);
                    ShowSystemMessage("备份已删除！", "成功", false);
                    UpdateAllDisplays();
                }
            }
            else
            {
                ShowSystemMessage("没有找到备份文件！", "提示", false);
            }
        }

        static string ShowInputBox(string prompt, string title)
        {
            Form f = new Form();
            f.Text = title;
            f.Width = 350;
            f.Height = 200;
            f.FormBorderStyle = FormBorderStyle.None;
            f.BackColor = Color.White;
            f.Cursor = GetSystemCursor();
            f.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, f.Width, f.Height, 10, 10));
            f.ShowInTaskbar = false;
            f.StartPosition = FormStartPosition.CenterScreen;

            f.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left) {
                    ReleaseCapture();
                    SendMessage(f.Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero);
                }
            };

            Label lblClose = new Label();
            lblClose.Text = "✕";
            lblClose.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
            lblClose.ForeColor = Color.Gray;
            lblClose.Location = new Point(315, 10);
            lblClose.AutoSize = true;
            lblClose.Cursor = Cursors.Hand;
            lblClose.MouseEnter += (s, e) => { lblClose.ForeColor = Color.Red; };
            lblClose.MouseLeave += (s, e) => { lblClose.ForeColor = Color.Gray; };
            lblClose.Click += (s, e) => { f.DialogResult = DialogResult.Cancel; f.Close(); };
            f.Controls.Add(lblClose);

            Label lbl = new Label();
            lbl.Text = prompt;
            lbl.Font = new Font("微软雅黑", 10F);
            lbl.Location = new Point(20, 35);
            lbl.Size = new Size(300, 25);
            f.Controls.Add(lbl);

            TextBox txt = new TextBox();
            txt.Left = 20;
            txt.Top = 70;
            txt.Width = 290;
            txt.Font = new Font("微软雅黑", 10F);
            f.Controls.Add(txt);

            Button btnOk = new Button();
            btnOk.Text = "确定";
            btnOk.Size = new Size(80, 30);
            btnOk.Location = new Point(140, 130);
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.FlatAppearance.BorderColor = Color.LightGray;
            btnOk.BackColor = Color.White;
            btnOk.Cursor = Cursors.Hand;
            f.Controls.Add(btnOk);

            Button btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Size = new Size(80, 30);
            btnCancel.Location = new Point(230, 130);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderColor = Color.LightGray;
            btnCancel.BackColor = Color.White;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.DialogResult = DialogResult.Cancel;
            f.Controls.Add(btnCancel);
            
            string result = "";
            btnOk.MouseDown += (s, e) => { result = txt.Text; f.DialogResult = DialogResult.OK; f.Close(); };
            
            f.AcceptButton = btnOk;
            f.CancelButton = btnCancel;

            btnOk.BringToFront();
            btnCancel.BringToFront();
            lblClose.BringToFront();
            txt.BringToFront();

            f.ShowDialog();
            return result;
        }
    }
}
