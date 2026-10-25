using System;
using System.Diagnostics;
using System.IO;
using System.Net;
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

        // ==========================================
        // 【唯一需要修改的地方】以后更新版本，只改这里！
        // ==========================================
        static string currentVersion = "4.3";
        // ==========================================

        static string versionUrl = "https://ghproxy.net/https://raw.githubusercontent.com/114514imageset/spaceparty_Resolution_change/main/Version.txt";
        static string updateUrl = "https://github.com/114514imageset/spaceparty_Resolution_change/releases";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 欢迎弹窗自动读取版本号
            string msg = "欢迎使用 太空杀分辨率切换工具 " + currentVersion + "！\n\n" +
                         "【提示】修改前请确保游戏已完全关闭。\n" +
                         "【注意】本工具仅修改本地画面参数，若出现异常请使用选项 1 还原。\n\n" +
                         "作者主页：https://space.bilibili.com/1392176543\n" +
                         "用 DeepSeek 做的，尽力了\n\n" +
                         "是否打开作者的主页？";

            if (MessageBox.Show(msg, "关于软件", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                Process.Start("https://space.bilibili.com/1392176543");
            }

            form = new Form();
            // 标题自动读取版本号
            form.Text = "太空杀 - 分辨率切换工具 v" + currentVersion;
            form.Size = new System.Drawing.Size(400, 380);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MaximizeBox = false;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            
            try
            {
                form.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            lblBackupRes = new Label();
            lblBackupRes.Location = new System.Drawing.Point(30, 15);
            lblBackupRes.Size = new System.Drawing.Size(320, 20);
            lblBackupRes.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            form.Controls.Add(lblBackupRes);

            Label lblTitle = new Label();
            lblTitle.Text = "请选择要执行的操作：";
            lblTitle.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            lblTitle.Location = new System.Drawing.Point(30, 45);
            lblTitle.Size = new System.Drawing.Size(300, 30);
            form.Controls.Add(lblTitle);

            Button btn1 = new Button();
            btn1.Text = "1. 备份 / 还原原本分辨率";
            btn1.Location = new System.Drawing.Point(30, 85);
            btn1.Size = new System.Drawing.Size(320, 35);
            btn1.Click += new EventHandler(Btn1_Click);
            form.Controls.Add(btn1);

            Button btn2 = new Button();
            btn2.Text = "2. 一键切换超宽分辨率";
            btn2.Location = new System.Drawing.Point(30, 135);
            btn2.Size = new System.Drawing.Size(320, 35);
            btn2.Click += new EventHandler(Btn2_Click);
            form.Controls.Add(btn2);

            Button btn3 = new Button();
            btn3.Text = "3. 自定义宽高修改";
            btn3.Location = new System.Drawing.Point(30, 185);
            btn3.Size = new System.Drawing.Size(320, 35);
            btn3.Click += new EventHandler(Btn3_Click);
            form.Controls.Add(btn3);

            Button btn4 = new Button();
            btn4.Text = "4. 删除本地备份";
            btn4.Location = new System.Drawing.Point(30, 235);
            btn4.Size = new System.Drawing.Size(320, 35);
            btn4.Click += new EventHandler(Btn4_Click);
            form.Controls.Add(btn4);

            lblStatus = new Label();
            lblStatus.Location = new System.Drawing.Point(20, 300);
            lblStatus.Size = new System.Drawing.Size(200, 20);
            lblStatus.ForeColor = System.Drawing.Color.DimGray;
            lblStatus.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular);
            form.Controls.Add(lblStatus);

            lblBackupStatus = new Label();
            lblBackupStatus.Location = new System.Drawing.Point(220, 300);
            lblBackupStatus.Size = new System.Drawing.Size(150, 20);
            lblBackupStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            lblBackupStatus.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular);
            form.Controls.Add(lblBackupStatus);

            UpdateAllDisplays();

            // 延迟 1.5 秒后检查更新，防止被主界面覆盖
            form.Shown += (s, e) => {
                Task.Run(() => {
                    System.Threading.Thread.Sleep(1500); 
                    CheckForUpdates();
                });
            };

            Application.Run(form);
        }

        // 检查更新（丝滑无感版）
        static void CheckForUpdates()
        {
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    string url = versionUrl + "?t=" + DateTime.Now.Ticks;
                    
                    // 同步等待下载
                    string remoteVersionStr = client.DownloadString(url).Trim();
                    remoteVersionStr = remoteVersionStr.Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("\t", "");

                    Version remoteVer;
                    Version currentVer;
                    
                    if (Version.TryParse(remoteVersionStr, out remoteVer) && Version.TryParse(currentVersion, out currentVer))
                    {
                        if (remoteVer > currentVer)
                        {
                            form.Invoke(new Action(() => {
                                DialogResult result = MessageBox.Show(
                                    "发现新版本！\n\n当前版本：" + currentVersion + "\n最新版本：" + remoteVersionStr + "\n\n是否前往 GitHub 下载最新版？",
                                    "版本更新提醒",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Information,
                                    MessageBoxDefaultButton.Button1,
                                    MessageBoxOptions.DefaultDesktopOnly); 

                                if (result == DialogResult.Yes)
                                {
                                    Process.Start(updateUrl);
                                }
                            }));
                        }
                    }
                }
            }
            catch
            {
                // 网络不好时静默失败，不打扰用户
            }
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
                        if (w != null && h != null)
                        {
                            lblStatus.Text = "当前游戏分辨率: " + w.ToString() + " x " + h.ToString();
                            return;
                        }
                    }
                    lblStatus.Text = "当前游戏分辨率: 未找到";
                }
            }
            catch
            {
                lblStatus.Text = "当前游戏分辨率: 读取失败";
            }
        }

        static void UpdateBackupStatus()
        {
            if (File.Exists(backupFile))
            {
                lblBackupStatus.Text = "备份状态: 已备份";
                lblBackupStatus.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                lblBackupStatus.Text = "备份状态: 无备份";
                lblBackupStatus.ForeColor = System.Drawing.Color.Red;
            }
        }

        static void UpdateBackupResDisplay()
        {
            if (!File.Exists(backupFile))
            {
                lblBackupRes.Text = "备份分辨率: 未备份";
                lblBackupRes.ForeColor = System.Drawing.Color.Red;
                return;
            }

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
                    lblBackupRes.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    lblBackupRes.Text = "备份分辨率: 已备份 (读取内容失败)";
                    lblBackupRes.ForeColor = System.Drawing.Color.Green;
                }
            }
            catch
            {
                lblBackupRes.Text = "备份分辨率: 已备份 (读取异常)";
                lblBackupRes.ForeColor = System.Drawing.Color.Green;
            }
        }

        static void Btn1_Click(object sender, EventArgs e)
        {
            if (File.Exists(backupFile))
            {
                Process.Start("reg", "import \"" + backupFile + "\"").WaitForExit();
                MessageBox.Show("原本分辨率还原成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateAllDisplays();
            }
            else
            {
                Process.Start("reg", "export \"HKEY_CURRENT_USER\\" + regPath + "\" \"" + backupFile + "\" /y").WaitForExit();
                MessageBox.Show("备份成功！已保存在软件同目录下的 1.reg", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show("超宽分辨率已开启！直接进游戏即可生效。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateAllDisplays();
            }
            catch
            {
                MessageBox.Show("写入失败，请右键以管理员身份运行！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void Btn3_Click(object sender, EventArgs e)
        {
            string w = ShowInputBox("请输入宽度 (例如 1920):", "自定义宽度");
            if (string.IsNullOrEmpty(w)) return;
            string h = ShowInputBox("请输入高度 (例如 1080):", "自定义高度");
            if (string.IsNullOrEmpty(h)) return;

            int iw = 0;
            int ih = 0;
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
                    MessageBox.Show("自定义分辨率修改成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateAllDisplays();
                }
                catch
                {
                    MessageBox.Show("写入失败，请右键以管理员身份运行！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("请输入有效的数字！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        static void Btn4_Click(object sender, EventArgs e)
        {
            if (File.Exists(backupFile))
            {
                if (MessageBox.Show("确定要删除备份吗？删除后无法还原！", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    File.Delete(backupFile);
                    MessageBox.Show("备份已删除！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateAllDisplays();
                }
            }
            else
            {
                MessageBox.Show("没有找到备份文件！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        static string ShowInputBox(string prompt, string title)
        {
            Form form = new Form();
            form.Text = title;
            form.Width = 350;
            form.Height = 200;
            form.StartPosition = FormStartPosition.CenterParent;
            Label lbl = new Label();
            lbl.Text = prompt;
            lbl.Left = 20;
            lbl.Top = 20;
            lbl.Width = 300;
            TextBox txt = new TextBox();
            txt.Left = 20;
            txt.Top = 50;
            txt.Width = 290;
            Button btnOk = new Button();
            btnOk.Text = "确定";
            btnOk.Left = 140;
            btnOk.Top = 90;
            btnOk.Width = 80;
            Button btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Left = 230;
            btnCancel.Top = 90;
            btnCancel.Width = 80;
            btnCancel.DialogResult = DialogResult.Cancel;
            
            string result = "";
            btnOk.Click += new EventHandler(delegate(object s, EventArgs e) { result = txt.Text; form.Close(); });
            
            form.Controls.Add(lbl);
            form.Controls.Add(txt);
            form.Controls.Add(btnOk);
            form.Controls.Add(btnCancel);
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;
            form.ShowDialog();
            return result;
        }
    }
}