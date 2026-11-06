using System;
using System.Collections.Generic;
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
        static string presetFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets.txt");

        static Form form;
        static Label lblStatus;

        static FlowLayoutPanel presetPanel;
        static Form presetWindow;

        static string currentVersion = "4.7";
        static string versionUrl = "https://ghproxy.net/https://raw.githubusercontent.com/114514imageset/spaceparty_Resolution_change/main/Version.txt";
        static string updateUrl = "https://github.com/114514imageset/spaceparty_Resolution_change/releases";

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern IntPtr LoadCursorFromFile(string lpFileName);

        static Cursor myCustomDefault = Cursors.Default;
        static Cursor myCustomHand = Cursors.Hand;

        public class Preset
        {
            public string Name { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        static void LoadSystemCursor()
        {
            try {
                string arrowPath = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors", "Arrow", null) as string;
                if (!string.IsNullOrEmpty(arrowPath)) {
                    arrowPath = Environment.ExpandEnvironmentVariables(arrowPath);
                    IntPtr hCursor = LoadCursorFromFile(arrowPath);
                    if (hCursor != IntPtr.Zero) myCustomDefault = new Cursor(hCursor);
                }
                string handPath = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors", "Hand", null) as string;
                if (!string.IsNullOrEmpty(handPath)) {
                    handPath = Environment.ExpandEnvironmentVariables(handPath);
                    IntPtr hCursor = LoadCursorFromFile(handPath);
                    if (hCursor != IntPtr.Zero) myCustomHand = new Cursor(hCursor);
                }
            } catch { }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            LoadSystemCursor();

            // 【回归】作者说明与免责弹窗
            string msg = "欢迎使用 太空杀分辨率切换工具 " + currentVersion + "！\n\n" +
                         "【提示】修改前请确保游戏已完全关闭。\n" +
                         "【注意】本工具仅修改本地画面参数，若出现异常请使用选项 1 或 4 还原。\n\n" +
                         "作者主页：https://space.bilibili.com/1392176543\n" +
                         "用 DeepSeek 做的，尽力了\n\n" +
                         "是否打开作者的主页？";

            if (MessageBox.Show(msg, "关于软件", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                Process.Start("https://space.bilibili.com/1392176543");
            }

            form = new Form();
            form.Text = "太空杀 - 分辨率切换工具 v" + currentVersion;
            form.Size = new Size(400, 380);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.FormBorderStyle = FormBorderStyle.None;
            form.BackColor = Color.White;
            form.Cursor = myCustomDefault;
            form.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, form.Width, form.Height, 10, 10));

            form.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(form.Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero); }
            };

            try { form.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            Label lblClose = new Label();
            lblClose.Text = "✕";
            lblClose.Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold);
            lblClose.ForeColor = Color.Gray;
            lblClose.Location = new Point(365, 10);
            lblClose.AutoSize = true;
            lblClose.Cursor = myCustomHand;
            lblClose.MouseEnter += (s, e) => { lblClose.ForeColor = Color.Red; };
            lblClose.MouseLeave += (s, e) => { lblClose.ForeColor = Color.Gray; };
            lblClose.Click += (s, e) => { Application.Exit(); };
            form.Controls.Add(lblClose);

            Label lblTitle = new Label();
            lblTitle.Text = "请选择要执行的操作：";
            lblTitle.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            lblTitle.Location = new Point(30, 40);
            lblTitle.Size = new Size(300, 30);
            form.Controls.Add(lblTitle);

            Button btn1 = CreateStyledButton("1. 我的分辨率预设", 85);
            btn1.Click += (s, e) => ShowPresetWindow();
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
            lblStatus.Size = new Size(300, 20);
            lblStatus.ForeColor = Color.DimGray;
            lblStatus.Font = new Font("微软雅黑", 9F, FontStyle.Regular);
            form.Controls.Add(lblStatus);

            UpdateCurrentResolution();

            form.Shown += (s, e) => {
                Task.Run(() => { System.Threading.Thread.Sleep(1500); CheckForUpdates(); });
            };

            Application.Run(form);
        }

        static void UpdateCurrentResolution()
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

        static void ShowPresetWindow()
        {
            presetWindow = new Form();
            presetWindow.Text = "我的预设";
            presetWindow.Size = new Size(400, 520);
            presetWindow.FormBorderStyle = FormBorderStyle.None;
            presetWindow.BackColor = Color.White;
            presetWindow.Cursor = myCustomDefault;
            presetWindow.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, presetWindow.Width, presetWindow.Height, 10, 10));
            presetWindow.StartPosition = FormStartPosition.CenterParent;
            
            presetWindow.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(presetWindow.Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero); }
            };

            Label lblClose = new Label();
            lblClose.Text = "✕";
            lblClose.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
            lblClose.ForeColor = Color.Gray;
            lblClose.Location = new Point(365, 10);
            lblClose.AutoSize = true;
            lblClose.Cursor = myCustomHand;
            lblClose.MouseEnter += (s, e) => { lblClose.ForeColor = Color.Red; };
            lblClose.MouseLeave += (s, e) => { lblClose.ForeColor = Color.Gray; };
            lblClose.Click += (s, e) => { presetWindow.Close(); };
            presetWindow.Controls.Add(lblClose);

            Label title = new Label();
            title.Text = "已保存的分辨率预设：";
            title.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            title.Location = new Point(20, 25);
            title.AutoSize = true;
            presetWindow.Controls.Add(title);

            presetPanel = new FlowLayoutPanel();
            presetPanel.Location = new Point(20, 60);
            presetPanel.Size = new Size(360, 340);
            presetPanel.AutoScroll = true;
            presetPanel.FlowDirection = FlowDirection.TopDown;
            presetPanel.WrapContents = false;
            presetWindow.Controls.Add(presetPanel);

            RefreshPresetList();

            Button btnSaveNew = new Button();
            btnSaveNew.Text = "＋ 添加新的预设";
            btnSaveNew.Size = new Size(360, 35);
            btnSaveNew.Location = new Point(20, 420);
            btnSaveNew.FlatStyle = FlatStyle.Flat;
            btnSaveNew.FlatAppearance.BorderColor = Color.LightGray;
            btnSaveNew.BackColor = Color.White;
            btnSaveNew.Cursor = myCustomHand;
            btnSaveNew.Click += (s, e) => {
                string[] result = ShowEditPresetDialog("", 1914, 536, false);
                if (result != null) {
                    try {
                        List<Preset> list = LoadPresets();
                        list.Add(new Preset { Name = result[0], Width = int.Parse(result[1]), Height = int.Parse(result[2]) });
                        SavePresets(list);
                        RefreshPresetList();
                    } catch { }
                }
            };
            presetWindow.Controls.Add(btnSaveNew);

            presetWindow.ShowDialog();
        }

        static void RefreshPresetList()
        {
            presetPanel.Controls.Clear();
            List<Preset> presets = LoadPresets();
            
            if (presets.Count == 0)
            {
                Label empty = new Label();
                empty.Text = "暂无预设，可点击下方按钮添加。";
                empty.Font = new Font("微软雅黑", 10F);
                empty.ForeColor = Color.Gray;
                empty.AutoSize = true;
                presetPanel.Controls.Add(empty);
            }
            else
            {
                foreach (var p in presets)
                {
                    Panel item = new Panel();
                    item.Size = new Size(340, 60);
                    item.BackColor = Color.FromArgb(245, 245, 245);
                    item.Margin = new Padding(0, 0, 0, 10);
                    
                    Label lblName = new Label();
                    lblName.Text = p.Name + " (" + p.Width.ToString() + "x" + p.Height.ToString() + ")";
                    lblName.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
                    lblName.Location = new Point(10, 18);
                    lblName.AutoSize = true;
                    item.Controls.Add(lblName);

                    Button btnApply = new Button();
                    btnApply.Text = "应用";
                    btnApply.Size = new Size(45, 30);
                    btnApply.Location = new Point(195, 15);
                    btnApply.FlatStyle = FlatStyle.Flat;
                    btnApply.Cursor = myCustomHand;
                    string w = p.Width.ToString(); string h = p.Height.ToString();
                    btnApply.Click += (s, e) => { ApplyResolution(w, h); presetWindow.Close(); };
                    item.Controls.Add(btnApply);

                    Button btnRename = new Button();
                    btnRename.Text = "修改"; 
                    btnRename.Size = new Size(45, 30);
                    btnRename.Location = new Point(245, 15);
                    btnRename.FlatStyle = FlatStyle.Flat;
                    btnRename.Cursor = myCustomHand;
                    btnRename.Click += (s, e) => {
                        string[] newData = ShowEditPresetDialog(p.Name, p.Width, p.Height, true);
                        if (newData != null) {
                            p.Name = newData[0];
                            p.Width = int.Parse(newData[1]);
                            p.Height = int.Parse(newData[2]);
                            SavePresets(presets);
                            RefreshPresetList();
                        }
                    };
                    item.Controls.Add(btnRename);

                    Button btnDel = new Button();
                    btnDel.Text = "删除";
                    btnDel.Size = new Size(45, 30);
                    btnDel.Location = new Point(295, 15);
                    btnDel.FlatStyle = FlatStyle.Flat;
                    btnDel.ForeColor = Color.Red;
                    btnDel.Cursor = myCustomHand;
                    btnDel.Click += (s, e) => {
                        if (MessageBox.Show("确定删除该预设吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                            presets.Remove(p);
                            SavePresets(presets);
                            RefreshPresetList();
                        }
                    };
                    item.Controls.Add(btnDel);

                    presetPanel.Controls.Add(item);
                }
            }
        }

        static string[] ShowEditPresetDialog(string name, int w, int h, bool isEdit)
        {
            Form f = new Form();
            f.Text = isEdit ? "修改预设" : "新建预设";
            f.Size = new Size(320, 280);
            f.FormBorderStyle = FormBorderStyle.None;
            f.BackColor = Color.White;
            f.Cursor = myCustomDefault;
            f.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, f.Width, f.Height, 10, 10));
            f.StartPosition = FormStartPosition.CenterParent;

            Label l1 = new Label() { Text = "名称:", Left = 20, Top = 20, AutoSize = true, Font = new Font("微软雅黑", 10F) };
            TextBox t1 = new TextBox() { Left = 80, Top = 18, Width = 200, Text = name, Font = new Font("微软雅黑", 10F) };
            f.Controls.Add(l1); f.Controls.Add(t1);

            Label l2 = new Label() { Text = "宽 (W):", Left = 20, Top = 65, AutoSize = true, Font = new Font("微软雅黑", 10F) };
            TextBox t2 = new TextBox() { Left = 80, Top = 63, Width = 200, Text = w.ToString(), Font = new Font("微软雅黑", 10F) };
            f.Controls.Add(l2); f.Controls.Add(t2);

            Label l3 = new Label() { Text = "高 (H):", Left = 20, Top = 110, AutoSize = true, Font = new Font("微软雅黑", 10F) };
            TextBox t3 = new TextBox() { Left = 80, Top = 108, Width = 200, Text = h.ToString(), Font = new Font("微软雅黑", 10F) };
            f.Controls.Add(l3); f.Controls.Add(t3);

            Button btn = new Button() { Text = isEdit ? "保存修改" : "添加预设", Left = 100, Top = 170, Width = 120, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.White, Cursor = myCustomHand };
            f.Controls.Add(btn);

            string[] result = null;
            btn.Click += (s, e) => {
                if (!string.IsNullOrEmpty(t1.Text) && !string.IsNullOrEmpty(t2.Text) && !string.IsNullOrEmpty(t3.Text)) {
                    result = new string[] { t1.Text, t2.Text, t3.Text };
                    f.Close();
                }
            };
            f.ShowDialog();
            return result;
        }

        static void Btn3_Click(object sender, EventArgs e)
        {
            string w = ShowCustomInput("自定义宽度", "请输入宽度 (如 1914):", "1914");
            if (string.IsNullOrEmpty(w)) return;
            string h = ShowCustomInput("自定义高度", "请输入高度 (如 536):", "536");
            if (string.IsNullOrEmpty(h)) return;

            ApplyResolution(w, h);
        }

        static string ShowCustomInput(string title, string labelText, string defaultValue)
        {
            Form f = new Form();
            f.Text = title;
            f.Size = new Size(320, 180);
            f.FormBorderStyle = FormBorderStyle.None;
            f.BackColor = Color.White;
            f.Cursor = myCustomDefault;
            f.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, f.Width, f.Height, 10, 10));
            f.StartPosition = FormStartPosition.CenterParent;

            Label lbl = new Label() { Text = labelText, Left = 20, Top = 30, AutoSize = true, Font = new Font("微软雅黑", 10F) };
            TextBox txt = new TextBox() { Left = 20, Top = 60, Width = 260, Text = defaultValue, Font = new Font("微软雅黑", 10F) };
            Button btn = new Button() { Text = "确定", Left = 110, Top = 110, Width = 80, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = Color.White, Cursor = myCustomHand };
            f.Controls.Add(lbl); f.Controls.Add(txt); f.Controls.Add(btn);

            string result = null;
            btn.Click += (s, e) => { 
                if (!string.IsNullOrEmpty(txt.Text)) { result = txt.Text; f.Close(); }
            };
            f.ShowDialog();
            return result;
        }

        static List<Preset> LoadPresets()
        {
            List<Preset> list = new List<Preset>();
            if (!File.Exists(presetFile)) return list;
            try
            {
                string[] lines = File.ReadAllLines(presetFile);
                foreach (string line in lines)
                {
                    string[] parts = line.Split('|');
                    if (parts.Length == 3)
                    {
                        list.Add(new Preset { Name = parts[0], Width = int.Parse(parts[1]), Height = int.Parse(parts[2]) });
                    }
                }
            }
            catch { }
            return list;
        }

        static void SavePresets(List<Preset> list)
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (var p in list) { lines.Add(p.Name + "|" + p.Width + "|" + p.Height); }
                File.WriteAllLines(presetFile, lines);
            }
            catch { }
        }

        static void ApplyResolution(string w, string h)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regPath))
                {
                    int iw = int.Parse(w); int ih = int.Parse(h);
                    key.SetValue("Screenmanager Resolution Width_h182942802", iw, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Height_h2627697771", ih, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Window Width_h2524650974", iw, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Window Height_h1684712807", ih, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Width Default_h680557497", iw, RegistryValueKind.DWord);
                    key.SetValue("Screenmanager Resolution Height Default_h1380706816", ih, RegistryValueKind.DWord);
                    key.SetValue("ResolutionNew_h3187609911", 2, RegistryValueKind.DWord);
                }
                MessageBox.Show("已应用: " + w + "x" + h + "，请进游戏查看！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateCurrentResolution();
            } catch { MessageBox.Show("应用失败，请以管理员身份运行", "错误"); }
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
            btn.Cursor = myCustomHand;
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
                                DialogResult result = MessageBox.Show("发现新版本！\n\n当前版本：" + currentVersion + "\n最新版本：" + remoteVersionStr + "\n\n是否前往 GitHub 下载最新版？", "版本更新提醒", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                if (result == DialogResult.Yes) Process.Start(updateUrl);
                            }));
                        }
                    }
                }
            }
            catch { }
        }

        static void Btn2_Click(object sender, EventArgs e)
        {
            ApplyResolution("1914", "536");
        }

        static void Btn4_Click(object sender, EventArgs e)
        {
            if (File.Exists(backupFile))
            {
                if (MessageBox.Show("确定要删除备份吗？删除后无法还原！", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    File.Delete(backupFile);
                    MessageBox.Show("备份已删除！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else { MessageBox.Show("没有找到备份文件！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
    }
}
