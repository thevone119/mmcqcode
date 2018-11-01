using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirScenes;
using Client.MirSounds;
using Microsoft.DirectX.Direct3D;
using Font = System.Drawing.Font;

namespace Client
{
    /// <summary>
    /// 传奇的主窗体
    /// 这个主窗体销毁，则游戏关闭了哦
    /// 窗体->场景->各种空间变换显示
    /// 1个窗口，1个场景(多个场景切换，最有一个场景有效)，N种不同的控件进行显示
    /// </summary>
    public partial class CMain : Form
    {
        //dubug提醒，鼠标提示
        public static MirControl DebugBaseLabel, HintBaseLabel;
        public static MirLabel DebugTextLabel, HintTextLabel, ScreenshotTextLabel;
        public static Graphics Graphics;
        //这个是什么点哦？地图中心点么？应该是当前的鼠标坐标点
        public static Point MPoint;
        //计算系统运行的总时间
        public readonly static Stopwatch Timer = Stopwatch.StartNew();
        //计算开始时间
        public readonly static DateTime StartTime = DateTime.Now;
        //Time:运行开始到现在过去的毫秒数
        public static long Time, OldTime;
        public static DateTime Now { get { return StartTime.AddMilliseconds(Time); } }
        //随机数
        public static readonly Random Random = new Random();

        public static bool DebugOverride;
        //显示帧数，每秒显示一次帧数
        private static long _fpsTime;
        private static int _fps;
        public static int FPS;

        //快捷键处理
        public static bool Shift, Alt, Ctrl, Tilde;
        public static KeyBindSettings InputKeys = new KeyBindSettings();

        public CMain()
        {
            InitializeComponent();
            //主窗体的所有事件，传播到子控件
            Application.Idle += Application_Idle;
            MouseClick += CMain_MouseClick;
            MouseDown += CMain_MouseDown;
            MouseUp += CMain_MouseUp;
            MouseMove += CMain_MouseMove;
            MouseDoubleClick += CMain_MouseDoubleClick;
            KeyPress += CMain_KeyPress;
            KeyDown += CMain_KeyDown;
            KeyUp += CMain_KeyUp;
            Deactivate += CMain_Deactivate;
            MouseWheel += CMain_MouseWheel;


            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            FormBorderStyle = Settings.FullScreen ? FormBorderStyle.None : FormBorderStyle.FixedDialog;

            Graphics = CreateGraphics();
            Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            Graphics.CompositingQuality = CompositingQuality.HighQuality;
            Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Graphics.TextContrast = 0;

            //这句话会卡屏？神经咯。
            //this.ControlBox = true;
            
        }

        //.net 提供了ProcessCmdKey 重新实现Form的键盘消息,这个针对F10进行特殊处理，避免卡屏
        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, System.Windows.Forms.Keys keyData)
        {
            int WM_KEYDOWN = 256;
            int WM_SYSKEYDOWN = 260;
            if (msg.Msg == WM_KEYDOWN | msg.Msg == WM_SYSKEYDOWN)
            {
                //F10键自定义处理，不经过系统
                if (keyData == Keys.F10)
                {
                    MirScene.ActiveScene.OnKeyDown(new KeyEventArgs(keyData));
                    return true;
                }
                //屏蔽win键
            }
            return false;
        }

        private void CMain_Load(object sender, EventArgs e)
        {

            try
            {
                ClientSize = new Size(Settings.ScreenWidth, Settings.ScreenHeight);

                //DX画面控制
                DXManager.Create();
                //声音控制
                SoundManager.Create();
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }

        //空闲执行，应该是没有消息需要处理为空闲
        private static void Application_Idle(object sender, EventArgs e)
        {
            try
            {
                //当系统没有消息时执行
                while (AppStillIdle)
                {
                    //更新系统时间Time,
                    UpdateTime();
                    //更新系统，更新画面，每秒更新一次，同时计算帧数
                    UpdateEnviroment();
                    //重置环境，重画，拼命重画
                    RenderEnvironment();
                }
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        //失去焦点时
        private static void CMain_Deactivate(object sender, EventArgs e)
        {
            MapControl.MapButtons = MouseButtons.None;
            Shift = false;
            Alt = false;
            Ctrl = false;
            Tilde = false;
        }
        //获取当前有效的按键
        public static KeyBind GetKeyBind(Keys k)
        {
            return CMain.InputKeys.GetKeyBind(Shift, Alt, Ctrl, Tilde, k);
        }

        //按键时
        public static void CMain_KeyDown(object sender, KeyEventArgs e)
        {
            Shift = e.Shift;
            Alt = e.Alt;
            Ctrl = e.Control;

            if (e.KeyCode == Keys.Oem8)
                CMain.Tilde = true;

            try
            {
                //切换全屏
                if (e.Alt && e.KeyCode == Keys.Enter)
                {
                    ToggleFullScreen();
                    return;
                }

                if (MirScene.ActiveScene != null)
                {
                    MirScene.ActiveScene.OnKeyDown(e);
                }

            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
       
  
        //按键结束
        public static void CMain_KeyUp(object sender, KeyEventArgs e)
        {
            Shift = e.Shift;
            Alt = e.Alt;
            Ctrl = e.Control;

            if (e.KeyCode == Keys.Oem8)
                CMain.Tilde = false;
            //这个进行截屏处理？应该捕获不了这个键哦
            KeyBind kb = GetKeyBind(e.KeyCode);
            if(kb!=null && KeybindOptions.Screenshot == kb.function)
            {
                Program.Form.CreateScreenShot();
            }

            try
            {
                if (MirScene.ActiveScene != null)
                {
                    MirScene.ActiveScene.OnKeyUp(e);
                }
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (MirScene.ActiveScene != null)
                {
                    MirScene.ActiveScene.OnKeyPress(e);
                }
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_MouseMove(object sender, MouseEventArgs e)
        {
            if (Settings.FullScreen)
                Cursor.Clip = new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight);

            //当前鼠标的坐标，在窗口内的坐标
            MPoint = Program.Form.PointToClient(Cursor.Position);

            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnMouseMove(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnMouseClick(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_MouseUp(object sender, MouseEventArgs e)
        {
            MapControl.MapButtons &= ~e.Button;
            if (!MapControl.MapButtons.HasFlag(MouseButtons.Right))
                GameScene.CanRun = false;

            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnMouseUp(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_MouseDown(object sender, MouseEventArgs e)
        {
            if (Program.Form.ActiveControl is TextBox)
            {
                MirTextBox textBox = Program.Form.ActiveControl.Tag as MirTextBox;

                if (textBox != null && textBox.CanLoseFocus)
                    Program.Form.ActiveControl = null;
            }

            if (e.Button == MouseButtons.Right && (GameScene.SelectedCell != null || GameScene.PickedUpGold))
            {
                GameScene.SelectedCell = null;
                GameScene.PickedUpGold = false;
                return;
            }

            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnMouseDown(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnMouseClick(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        public static void CMain_MouseWheel(object sender, MouseEventArgs e)
        {
            try
            {
                if (MirScene.ActiveScene != null)
                    MirScene.ActiveScene.OnMouseWheel(e);
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());
            }
        }
        //更新时间，更新Now时间
        private static void UpdateTime()
        {
            Time = Timer.ElapsedMilliseconds;
        }
        //Enviroment：更新系统，更新画面，每秒更新一次，同时计算帧数，这个帧数计算很不准吧?通过空闲调用计算
        private static void UpdateEnviroment()
        {
            //更新系统，更新画面，每秒更新一次，同时计算帧数
            if (Time >= _fpsTime)
            {
                _fpsTime = Time + 1000;
                FPS = _fps;
                _fps = 0;
                DXManager.Clean(); // Clean once a second.
            }
            else
                _fps++;
            //网络数据处理
            Network.Process();

            //场景处理
            if (MirScene.ActiveScene != null)
                MirScene.ActiveScene.Process();

       
           
            //动画要一直更新么
            for (int i = 0; i < MirAnimatedControl.Animations.Count; i++)
                MirAnimatedControl.Animations[i].UpdateOffSet();

            for (int i = 0; i < MirAnimatedButton.Animations.Count; i++)
                MirAnimatedButton.Animations[i].UpdateOffSet();

            //这个是鼠标移动到物体，菜单上显示的提示信息
            CreateHintLabel();
            //FPS等信息输出
            CreateDebugLabel();
        }

        //循环执行，重置DX环境,重画？
        private static void RenderEnvironment()
        {
            try
            {
                if (DXManager.DeviceLost)
                {
                    DXManager.AttemptReset();
                    Thread.Sleep(1);
                    return;
                }
                else
                {
                    DXManager.Device.Clear(ClearFlags.Target, Color.CornflowerBlue, 0, 0);
                    DXManager.Device.BeginScene();
                    DXManager.Sprite.Begin(SpriteFlags.AlphaBlend);
                    DXManager.SetSurface(DXManager.MainSurface);

                    if (MirScene.ActiveScene != null)
                        MirScene.ActiveScene.Draw();

                    DXManager.Sprite.End();
                    DXManager.Device.EndScene();
                    DXManager.Device.Present();
                }
            }
            catch (DeviceLostException)
            {
            }
            catch (Exception ex)
            {
                SaveError(ex.ToString());

                DXManager.AttemptRecovery();
            }
        }

        //这个是输出FPS等信息的，在左上角输出，这个和技能栏重复了。要调整下
        private static void CreateDebugLabel()
        {
            //if (!Settings.DebugMode) return;

            if (DebugBaseLabel == null || DebugBaseLabel.IsDisposed)
            {
                DebugBaseLabel = new MirControl
                    {
                        BackColour = Color.FromArgb(50, 50, 50),
                        Border = true,
                        BorderColour = Color.Black,
                        DrawControlTexture = true,
                        Location = new Point(5, 5),
                        NotControl = true,
                        Opacity = 0.5F
                    };
            }
            
            if (DebugTextLabel == null || DebugTextLabel.IsDisposed)
            {
                DebugTextLabel = new MirLabel
                {
                    AutoSize = true,
                    BackColour = Color.Transparent,
                    ForeColour = Color.White,
                    Parent = DebugBaseLabel,
                };

                DebugTextLabel.SizeChanged += (o, e) => DebugBaseLabel.Size = DebugTextLabel.Size;
            }

            if (DebugOverride) return;
            
            string text;
            if (MirControl.MouseControl != null)
            {
                text = string.Format("FPS: {0}", FPS);

                if (MirControl.MouseControl is MapControl)
                {
                    text += string.Format(", Co Ords: {0}", MapControl.MapLocation);

                    //text += "\r\n";

                    //var cell = GameScene.Scene.MapControl.M2CellInfo[MapControl.MapLocation.X, MapControl.MapLocation.Y];

                    //if (cell != null)
                    //{
                    //    text += string.Format("BackImage : {0}. BackIndex : {1}. MiddleImage : {2}. MiddleIndex {3}. FrontImage : {4}. FrontIndex : {5}", cell.BackImage, cell.BackIndex, cell.MiddleImage, cell.MiddleIndex, cell.FrontImage, cell.FrontIndex);
                    //}
                }

                if (MirScene.ActiveScene is GameScene)
                {
                    //text += "\r\n";
                    text += string.Format(", Objects: {0}", MapControl.Objects.Count);
                }
                if (MirObjects.MapObject.MouseObject != null)
                {
                    text += string.Format(", Target: {0}", MirObjects.MapObject.MouseObject.Name);
                }
                else
                {
                    text += string.Format(", Target: none");
                }
            }
            else
            {
                text = string.Format("FPS: {0}", FPS);
            }
            

            DebugTextLabel.Text = text;
        }

        public static void SendDebugMessage(string text)
        {
            if (!Settings.DebugMode) return;

            if (DebugBaseLabel == null || DebugTextLabel == null)
            {
                CreateDebugLabel();
            }

            DebugOverride = true;

            DebugTextLabel.Text = text;
        }

        private static void CreateHintLabel()
        {
            if (HintBaseLabel == null || HintBaseLabel.IsDisposed)
            {
                HintBaseLabel = new MirControl
                {
                    BackColour = Color.FromArgb(128, 128, 50),
                    Border = true,
                    DrawControlTexture = true,
                    BorderColour = Color.Yellow,
                    ForeColour = Color.Yellow,
                    Parent = MirScene.ActiveScene,
                    NotControl = true,
                    Opacity = 0.5F
                };
            }


            if (HintTextLabel == null || HintTextLabel.IsDisposed)
            {
                HintTextLabel = new MirLabel
                {
                    AutoSize = true,
                    BackColour = Color.Transparent,
                    ForeColour = Color.White,
                    Parent = HintBaseLabel,
                };

                HintTextLabel.SizeChanged += (o, e) => HintBaseLabel.Size = HintTextLabel.Size;
            }

            if (MirControl.MouseControl == null || string.IsNullOrEmpty(MirControl.MouseControl.Hint))
            {
                HintBaseLabel.Visible = false;
                return;
            }

            HintBaseLabel.Visible = true;

            HintTextLabel.Text = MirControl.MouseControl.Hint;

            Point point = MPoint.Add(-HintTextLabel.Size.Width, 20);

            if (point.X + HintBaseLabel.Size.Width >= Settings.ScreenWidth)
                point.X = Settings.ScreenWidth - HintBaseLabel.Size.Width - 1;
            if (point.Y + HintBaseLabel.Size.Height >= Settings.ScreenHeight)
                point.Y = Settings.ScreenHeight - HintBaseLabel.Size.Height - 1;

            if (point.X < 0)
                point.X = 0;
            if (point.Y < 0)
                point.Y = 0;

            HintBaseLabel.Location = point;
        }

        //是否全屏的处理
        private static void ToggleFullScreen()
        {
            Settings.FullScreen = !Settings.FullScreen;

            Program.Form.FormBorderStyle = Settings.FullScreen ? FormBorderStyle.None : FormBorderStyle.FixedDialog;

            DXManager.Parameters.Windowed = !Settings.FullScreen;
            DXManager.Device.Reset(DXManager.Parameters);
            Program.Form.ClientSize = new Size(Settings.ScreenWidth, Settings.ScreenHeight);
        }

        //截屏处理？
        public void CreateScreenShot()
        {
            Point location = PointToClient(Location);

            location = new Point(-location.X, -location.Y);

            string text = string.Format("[{0}  {1}] {2} {3:hh\\:mm\\:ss}", 
                Settings.P_ServerName.Length > 0 ? Settings.P_ServerName : "热血传奇", 
                MapControl.User != null ? MapControl.User.Name : "", 
                Now.ToShortDateString(), 
                Now.TimeOfDay);

            using (Bitmap image = GetImage(Handle, new Rectangle(location, ClientSize)))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                StringFormat sf = new StringFormat();
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Center;

                graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 3, 10), sf);
                graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 4, 9), sf);
                graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 5, 10), sf);
                graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.Black, new Point((Settings.ScreenWidth / 2) + 4, 11), sf);
                graphics.DrawString(text, new Font(Settings.FontName, 9F), Brushes.White, new Point((Settings.ScreenWidth / 2) + 4, 10), sf);//SandyBrown               

                string path = Path.Combine(Application.StartupPath, @"Screenshots\");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                int count = Directory.GetFiles(path, "*.png").Length;

                image.Save(Path.Combine(path, string.Format("Image {0}.Png", count)), ImageFormat.Png);
            }
        }

        public static void SaveError(string ex)
        {
            try
            {
                if (Settings.RemainingErrorLogs-- > 0)
                {
                    File.AppendAllText(@".\Error.txt",
                                       string.Format("[{0}] {1}{2}", Now, ex, Environment.NewLine));
                }
            }
            catch
            {
            }
        }

        public static void SetResolution(int width, int height)
        {
            if (Settings.ScreenWidth == width && Settings.ScreenHeight == height) return;

            DXManager.Device.Clear(ClearFlags.Target, Color.Black, 0, 0);
            DXManager.Device.Present();

            DXManager.Device.Dispose();

            Settings.ScreenWidth = width;
            Settings.ScreenHeight = height;
            Program.Form.ClientSize = new Size(width, height);

            DXManager.Create();
        }
            

        #region ScreenCapture

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr handle);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr handle);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr handle, int width, int height);
        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr handle, IntPtr handle2);
        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr handle, int destX, int desty, int width, int height,
                                         IntPtr handle2, int sourX, int sourY, int flag);
        [DllImport("gdi32.dll")]
        public static extern int DeleteDC(IntPtr handle);
        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr handle, IntPtr handle2);
        [DllImport("gdi32.dll")]
        public static extern int DeleteObject(IntPtr handle);

        public static Bitmap GetImage(IntPtr handle, Rectangle r)
        {
            IntPtr sourceDc = GetWindowDC(handle);
            IntPtr destDc = CreateCompatibleDC(sourceDc);

            IntPtr hBmp = CreateCompatibleBitmap(sourceDc, r.Width, r.Height);
            if (hBmp != IntPtr.Zero)
            {
                IntPtr hOldBmp = SelectObject(destDc, hBmp);
                BitBlt(destDc, 0, 0, r.Width, r.Height, sourceDc, r.X, r.Y, 0xCC0020); //0, 0, 13369376);
                SelectObject(destDc, hOldBmp);
                DeleteDC(destDc);
                ReleaseDC(handle, sourceDc);

                Bitmap bmp = Image.FromHbitmap(hBmp);


                DeleteObject(hBmp);

                return bmp;
            }

            return null;
        }
        #endregion

        #region Idle Check
        private static bool AppStillIdle
        {
            get
            {
                PeekMsg msg;
                return !PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
            }
        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern bool PeekMessage(out PeekMsg msg, IntPtr hWnd, uint messageFilterMin,
                                               uint messageFilterMax, uint flags);

        [StructLayout(LayoutKind.Sequential)]
        private struct PeekMsg
        {
            private readonly IntPtr hWnd;
            private readonly Message msg;
            private readonly IntPtr wParam;
            private readonly IntPtr lParam;
            private readonly uint time;
            private readonly Point p;
        }
        #endregion

        private void CMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CMain.Time < GameScene.LogTime)
            {
                GameScene.Scene.ChatDialog.ReceiveChat("Cannot leave game for " + (GameScene.LogTime - CMain.Time) / 1000 + " seconds.", ChatType.System);
                e.Cancel = true;
            }
        }
    }
}
