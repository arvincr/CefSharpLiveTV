using CefSharp;
using CefSharp.WinForms;
using Open.WinKeyboardHook;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CefSharpLiveTV
{
    public partial class Form1 : Form
    {
        private const int SW_HIDE = 0;  //隐藏任务栏
        private const int SW_RESTORE = 9;//显示任务栏
        [DllImport("user32.dll")]
        public static extern int ShowWindow(int hwnd, int nCmdShow);
        [DllImport("user32.dll")]
        public static extern int FindWindow(string lpClassName, string lpWindowName);

        ChromiumWebBrowser chromeBrowser;
        LiveTVChannel liveTVChannel;

        private readonly IKeyboardInterceptor _interceptor;
        private bool isCaptureScreen = false;
        
        public Form1()
        {
            var settings = new CefSettings
            {
                LogSeverity = LogSeverity.Verbose,
                Locale = "zh-CN",
                AcceptLanguageList = "zh-CN",
                MultiThreadedMessageLoop = true,
                CachePath = System.AppDomain.CurrentDomain.BaseDirectory + @"\cache",
                PersistSessionCookies = true
            };
            settings.CefCommandLineArgs.Add("ppapi-flash-path", System.AppDomain.CurrentDomain.BaseDirectory + "plugins\\pepflashplayer64_32_0_0_101.dll"); //指定flash的版本，不使用系统安装的flash版本
            settings.CefCommandLineArgs.Add("ppapi-flash-version", "32_0_0_101");
            Cef.Initialize(settings);
            chromeBrowser = new ChromiumWebBrowser("about:blank");
            chromeBrowser.FrameLoadEnd += ChromeBrowser_FrameLoadEnd;
            chromeBrowser.MenuHandler = new MyMenuHandler();
            chromeBrowser.RequestHandler = new MyRequestHandler();
            chromeBrowser.Dock = DockStyle.Fill;
            chromeBrowser.Visible = true;
            this.Controls.Add(chromeBrowser);
            InitializeComponent();
            label1.BringToFront();
            label2.BringToFront();
            _interceptor = new KeyboardInterceptor();
            _interceptor.KeyDown += (sender, args) => Hook_KeyDown(sender, args);
        }
        private void CaptureScreen(int x, int y, int width, int height)
        {
            System.Drawing.Bitmap bitmap = new Bitmap(width, height);
            using (System.Drawing.Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));

                SaveFileDialog dialog = new SaveFileDialog();
                dialog.Filter = "Png Files|*.png";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    bitmap.Save(dialog.FileName, ImageFormat.Png);
                }
            }
        }
        private void Hook_KeyDown(object sender, KeyEventArgs e)
        {
            if (this.ContainsFocus)
            {
                switch (e.KeyCode)
                {
                    case Keys.Back:
                        isCaptureScreen = true;
                        CaptureScreen(this.Left, this.Top, this.Width, this.Height);
                        isCaptureScreen = false;
                        break;
                    case Keys.Left://chs--
                        if (backgroundWorker1.IsBusy)
                        {
                            break;
                        }
                        if (liveTVChannel.now > 0)
                        {
                            liveTVChannel.now--;
                        }
                        else
                        {
                            liveTVChannel.now = (byte)(liveTVChannel.size - 1);
                        }
                        this.Text = (liveTVChannel.now + 1).ToString() + liveTVChannel.name[liveTVChannel.now] + liveTVChannel.url[liveTVChannel.now];
                        label1.Text = this.Text;
                        label1.Visible = true;
                        chromeBrowser.Stop();
                        chromeBrowser.Load("about:blank");
                        chromeBrowser.Load(liveTVChannel.url[liveTVChannel.now]);
                        break;
                    case Keys.Right://chs++
                        if (backgroundWorker1.IsBusy)
                        {
                            break;
                        }
                        if (liveTVChannel.now < (byte)(liveTVChannel.size - 1))
                        {
                            liveTVChannel.now++;
                        }
                        else
                        {
                            liveTVChannel.now = 0;
                        }
                        this.Text = (liveTVChannel.now + 1).ToString() + liveTVChannel.name[liveTVChannel.now] + liveTVChannel.url[liveTVChannel.now];
                        label1.Text = this.Text;
                        label1.Visible = true;
                        chromeBrowser.Stop();
                        chromeBrowser.Load("about:blank");
                        chromeBrowser.Load(liveTVChannel.url[liveTVChannel.now]);
                        break;
                    case Keys.Enter://全屏切换
                        if (this.FormBorderStyle == FormBorderStyle.None)
                        {
                            ShowWindow(FindWindow("Shell_TrayWnd", null), SW_RESTORE);
                            ShowWindow(FindWindow("Button", null), SW_RESTORE);
                            this.WindowState = FormWindowState.Normal;
                            this.FormBorderStyle = FormBorderStyle.Sizable;
                            this.WindowState = FormWindowState.Maximized;
                        }
                        else
                        {
                            ShowWindow(FindWindow("Shell_TrayWnd", null), SW_HIDE);
                            ShowWindow(FindWindow("Button", null), SW_HIDE);
                            this.WindowState = FormWindowState.Normal;
                            this.FormBorderStyle = FormBorderStyle.None;
                            this.WindowState = FormWindowState.Maximized;
                        }
                        break;
                    case Keys.F4://关闭
                        this.Close();
                        break;
                    default:
                        break;
                }
            }
        }

        private void ChromeBrowser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            this.BeginInvoke(new EventHandler(delegate
            {
                string script = @"
                (function() {
                    var flag = 0;
                    document.body.style.overflow = 'hidden';
                    document.body.style.backgroundColor='#000000';
                    if (typeof(wsplayer)=='undefined')
                    {
                        return 0;
                    }
                    do
                    {
                        flag = 0;
                        var div = document.getElementsByTagName('div');
                        for(i = 0; i < div.length; i++)
                        {
                            if(div[i].innerHTML.indexOf('WsPlayer.swf') == -1)
                            {
                                div[i].remove();
                                flag = 1;
                            }
                        }
                    } while (flag == 1);
                    do
                    {
                        flag = 0;
                        var link = document.getElementsByTagName('link');
                        for(i = 0; i < link.length; i++)
                        {
                            link[i].remove();
                            flag = 1;
                        }
                    } while (flag == 1);
                    wsplayer.style.marginTop = '0px';
                    wsplayer.style.marginLeft = '0px';
                    wsplayer.style.width = '" + (this.ClientSize.Width - 20).ToString() + @"px';
                    wsplayer.style.height = '" + (this.ClientSize.Height - 20).ToString() + @"px';
                    return 1;
                })();";
                Task<CefSharp.JavascriptResponse> task = chromeBrowser.EvaluateScriptAsync(script);
                task.ContinueWith(t =>
                {
                    if (!t.IsFaulted)
                    {
                        var response = t.Result;
                        if (response.Success == true)
                        {
                            int result = 0;
                            int.TryParse(response.Result.ToString(), out result);
                            if (result == 1)
                            {
                                timer1.Stop();
                                timer1.Start();
                            }
                        }
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "频道信息加载中……";
            label1.Text = this.Text;
            liveTVChannel = new LiveTVChannel();
            backgroundWorker1.RunWorkerAsync();
            _interceptor.StartCapturing();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cef.Shutdown();
            _interceptor.StopCapturing();
            ShowWindow(FindWindow("Shell_TrayWnd", null), SW_RESTORE);
            ShowWindow(FindWindow("Button", null), SW_RESTORE);
            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Maximized;
            if (backgroundWorker1.IsBusy)
            {
                backgroundWorker1.CancelAsync();
            }
        }
        [System.Runtime.InteropServices.DllImport("user32")]
        private static extern int mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
        //移动鼠标 
        const int MOUSEEVENTF_MOVE = 0x0001;
        //模拟鼠标左键按下 
        const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        //模拟鼠标左键抬起 
        const int MOUSEEVENTF_LEFTUP = 0x0004;
        //模拟鼠标右键按下 
        const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        //模拟鼠标右键抬起 
        const int MOUSEEVENTF_RIGHTUP = 0x0010;
        //模拟鼠标中键按下 
        const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        //模拟鼠标中键抬起 
        const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        //标示是否采用绝对坐标 
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        [DllImport("user32.dll")]
        private static extern int SetCursorPos(int x, int y);
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public override string ToString()
            {
                return ("X:" + X + ", Y:" + Y);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetCursorPos(out POINT pt);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool BlockInput(bool fBlockIt);//必须以管理员权限运行才有效
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (chromeBrowser.Created)
            {
                string script = @"
                (function() {
                    if (typeof(wsplayer)=='undefined')
                    {
                        return;
                    }
                    wsplayer.style.marginTop = '0px';
                    wsplayer.style.marginLeft = '0px';
                    wsplayer.style.width = '" + (this.ClientSize.Width - 20).ToString() + @"px';
                    wsplayer.style.height = '" + (this.ClientSize.Height - 20).ToString() + @"px';
                })();";
                chromeBrowser.ExecuteScriptAsync(script);
            }
            label2.Left = this.ClientSize.Width / 2 - 70;
            label2.Top = this.ClientSize.Height / 2 - 120;
            label2.Visible = false;
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            label1.Visible = false;
            if (this.FormBorderStyle == FormBorderStyle.None)
            {
                //必须以管理员权限运行才有效
                BlockInput(true);
                POINT pt = new POINT();
                //记录鼠标位置
                GetCursorPos(out pt);
                Point ps = new Point();
                Point pc = new Point();
                //设置
                pc.X = this.ClientSize.Width - 82;
                pc.Y = this.ClientSize.Height - 32;
                ps = this.PointToScreen(pc);
                SetCursorPos(ps.X, ps.Y);
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                //比例
                pc.X = this.ClientSize.Width / 2 - 70;
                pc.Y = this.ClientSize.Height / 2 - 120;
                ps = this.PointToScreen(pc);
                SetCursorPos(ps.X, ps.Y);
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                //铺满
                pc.X = this.ClientSize.Width / 2 + 125;
                pc.Y = this.ClientSize.Height / 2 - 32;
                ps = this.PointToScreen(pc);
                SetCursorPos(ps.X, ps.Y);
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                //确定
                pc.X = this.ClientSize.Width / 2 - 5;
                pc.Y = this.ClientSize.Height / 2 + 68;
                ps = this.PointToScreen(pc);
                SetCursorPos(ps.X, ps.Y);
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                //恢复鼠标位置
                SetCursorPos(pt.X, pt.Y);
                BlockInput(false);
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = liveTVChannel.GetChannel();
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((bool)e.Result)
            {
                label1.Text = "频道信息加载完成";
                chromeBrowser.Load(liveTVChannel.url[liveTVChannel.now]);
                this.Text = (liveTVChannel.now + 1).ToString() + liveTVChannel.name[liveTVChannel.now] + liveTVChannel.url[liveTVChannel.now];
                label1.Text = this.Text;
                ShowWindow(FindWindow("Shell_TrayWnd", null), SW_HIDE);
                ShowWindow(FindWindow("Button", null), SW_HIDE);
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.Text = "频道信息加载失败";
                label1.Text = this.Text;
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if ((this.FormBorderStyle == FormBorderStyle.None) && (!isCaptureScreen))
            {
                //退出flash全屏
                SendKeys.Send("{ESC}");
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {

        }
    }
}
