using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Client.MirGraphics;
using Client.MirSounds;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace Client.MirControls
{
    /// <summary>
    /// 视图类，控制类的基类
    /// 这个基类下有很多图片控制类，按钮控制类，场景控制类等类
    /// </summary>
    public class MirControl : IDisposable
    {
        //激活视图（点击视图），鼠标子视图（鼠标对应的视图，其实是个格子？）,这个是静态，全局的？什么鬼哦，也就是说任何时候只有一个是激活的，一个是鼠标的场景咯？
        public static MirControl ActiveControl, MouseControl;
        
        //实际显示的位置=父窗口的位置+当前位置,等于在父窗口的偏移
        public virtual Point DisplayLocation { get { return Parent == null ? Location : Parent.DisplayLocation.Add(Location); } }
        //显示区域
        public Rectangle DisplayRectangle { get { return new Rectangle(DisplayLocation, Size); } }
        //是否灰度？
        public bool GrayScale { get; set; }

        //其实下面3个参数可以合并？
        //混合显示
        public bool Blending { get; set; }
        //混合度？
        public float BlendingRate { get; set; }
        //混合模式
        public BlendMode BlendMode { get; set; }

        //背景色，默认红色么？坑啊
        #region Back Colour
        private Color _backColour;
        public Color BackColour
        {
            get { return _backColour; }
            set
            {
                if (_backColour == value)
                    return;
                _backColour = value;
                OnBackColourChanged();
            }
        }
        public event EventHandler BackColourChanged;
        protected virtual void OnBackColourChanged()
        {
            TextureValid = false;
            Redraw();
            if (BackColourChanged != null)
                BackColourChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion

        //边界矩形,边框
        #region Border
        protected Rectangle BorderRectangle;
        private bool _border;
        protected Vector2[] _borderInfo;
        protected virtual Vector2[] BorderInfo
        {
            get
            {
                if (Size == Size.Empty)
                    return null;

                if (BorderRectangle != DisplayRectangle)
                {   //这个是通过8个点，画一个边框线么？
                    _borderInfo = new[]
                        {
                            new Vector2(DisplayRectangle.Left - 1, DisplayRectangle.Top - 1),//左上
                            new Vector2(DisplayRectangle.Right, DisplayRectangle.Top - 1),//右上

                            new Vector2(DisplayRectangle.Left - 1, DisplayRectangle.Top - 1),//左上
                            new Vector2(DisplayRectangle.Left - 1, DisplayRectangle.Bottom),//左下

                            new Vector2(DisplayRectangle.Left - 1, DisplayRectangle.Bottom),//左下
                            new Vector2(DisplayRectangle.Right, DisplayRectangle.Bottom),//右下

                            new Vector2(DisplayRectangle.Right, DisplayRectangle.Top - 1),//右上
                            new Vector2(DisplayRectangle.Right, DisplayRectangle.Bottom)//右下
                        };

                    BorderRectangle = DisplayRectangle;
                }
                return _borderInfo;
            }
        }
        public virtual bool Border
        {
            get { return _border; }
            set
            {
                if (_border == value)
                    return;
                _border = value;
                OnBorderChanged();
            }
        }
        public event EventHandler BorderChanged;
        private void OnBorderChanged()
        {
            Redraw();
            if (BorderChanged != null)
                BorderChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //边框的颜色？
        #region Border Colour
        private Color _borderColour;
        public Color BorderColour
        {
            get { return _borderColour; }
            set
            {
                if (_borderColour == value)
                    return;
                _borderColour = value;
                OnBorderColourChanged();
            }
        }
        public event EventHandler BorderColourChanged;
        private void OnBorderColourChanged()
        {
            Redraw();
            if (BorderColourChanged != null)
                BorderColourChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion

        //纹理，通过dx画纹理？
        #region Control Texture
        //清除时间,清除是针对纹理的？也对，主要针对图像等大数据进行清除
        public long CleanTime;
        //纹理
        protected Texture ControlTexture;
        //是否显示？
        protected internal bool TextureValid;
        //是否画纹理
        private bool _drawControlTexture;
        //纹理的大小，宽和高
        protected Size TextureSize;
        public bool DrawControlTexture
        {
            get { return _drawControlTexture; }
            set
            {
                if (_drawControlTexture == value)
                    return;
                _drawControlTexture = value;
                Redraw();
            }
        }
        //创建纹理
        protected virtual void CreateTexture()
        {
            if (ControlTexture != null && !ControlTexture.Disposed && Size != TextureSize)
                ControlTexture.Dispose();

            if (ControlTexture == null || ControlTexture.Disposed)
            {
                //加入dx队列？
                DXManager.ControlList.Add(this);
                //初始化纹理，1层的纹理？ rgb8格式?
                ControlTexture = new Texture(DXManager.Device, Size.Width, Size.Height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
                ControlTexture.Disposing += ControlTexture_Disposing;
                TextureSize = Size;
            }
            //这是交换么？
            Surface oldSurface = DXManager.CurrentSurface;
            Surface surface = ControlTexture.GetSurfaceLevel(0);
            DXManager.SetSurface(surface);
            DXManager.Device.Clear(ClearFlags.Target, BackColour, 0, 0);
            DXManager.SetSurface(oldSurface);

            TextureValid = true;
            surface.Dispose();
        }
        //释放纹理
        protected void ControlTexture_Disposing(object sender, EventArgs e)
        {
            ControlTexture = null;
            TextureValid = false;
            TextureSize = Size.Empty;

            DXManager.ControlList.Remove(this);
        }
        internal void DisposeTexture()
        {
            if (ControlTexture == null || ControlTexture.Disposed) return;

            ControlTexture.Dispose();
        }
        #endregion
        //控制层定义，其他的各种控制层，界面层
        #region Controls
        public List<MirControl> Controls { get; private set; }
        public event EventHandler ControlAdded , ControlRemoved;
        //这个是私有的？靠
        private void AddControl(MirControl control)
        {
            Controls.Add(control);
            OnControlAdded();
        }
        
        //插入视图
        public void InsertControl(int index, MirControl control)
        {
            if (control.Parent != this)
            {
                control.Parent = null;
                control._parent = this;
            }

            if (index >= Controls.Count)
                Controls.Add(control);
            else
            {
                Controls.Insert(index, control);
                OnControlAdded();
            }
        }
        private void RemoveControl(MirControl control)
        {
            Controls.Remove(control);
            OnControlRemoved();
        }
        private void OnControlAdded()
        {
            Redraw();
            if (ControlAdded != null)
                ControlAdded.Invoke(this, EventArgs.Empty);
        }
        private void OnControlRemoved()
        {
            Redraw();
            if (ControlRemoved != null)
                ControlRemoved.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //是否有效(是否启用)，如果父窗口无效，则子窗口也无效,同时把ActiveControl停用了
        #region Enabled
        private bool _enabled;
        public bool Enabled
        {
            internal get { return Parent == null ? _enabled : Parent.Enabled && _enabled; }
            set
            {
                if (_enabled == value)
                    return;
                _enabled = value;
                OnEnabledChanged();
            }
        }
        public event EventHandler EnabledChanged;
        protected virtual void OnEnabledChanged()
        {
            Redraw();

            if (EnabledChanged != null)
                EnabledChanged.Invoke(this, EventArgs.Empty);

            if (!Enabled && ActiveControl == this)
                ActiveControl.Deactivate();

            if (Controls != null)
                foreach (MirControl control in Controls)
                    control.OnEnabledChanged();
        }
        #endregion

        //定义窗体各种事件，单击，双击，画前，画后，鼠标进入，鼠标离开，展示，展示前，销毁等事件
        //定义鼠标事件，滚轮事件，鼠标移动事件，鼠标按下，鼠标放开
        //定义键盘事件，键按下，键放开等
        #region Events
        protected bool HasShown;
        public event EventHandler Click , DoubleClick, BeforeDraw , AfterDraw , MouseEnter , MouseLeave , Shown , BeforeShown, Disposing;
        public event MouseEventHandler MouseWheel,MouseMove, MouseDown, MouseUp;
        public event KeyEventHandler KeyDown , KeyUp;
        public event KeyPressEventHandler KeyPress;
        #endregion
        //前景色,如果有前景色，则纹理不显示?
        #region Fore Colour
        private Color _foreColour;
        public Color ForeColour
        {
            get { return _foreColour; }
            set
            {
                if (_foreColour == value)
                    return;
                _foreColour = value;
                OnForeColourChanged();
            }
        }
        public event EventHandler ForeColourChanged;
        protected virtual void OnForeColourChanged()
        {
            TextureValid = false;
            if (ForeColourChanged != null)
                ForeColourChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //位置发生变化，如果位置发生变化，则所有组件会收到变化事件？
        #region Location
        private Point _location;
        public Point Location
        {
            get { return _location; }
            set
            {
                if (_location == value)
                    return;
                _location = value;
                OnLocationChanged();
            }
        }
        public event EventHandler LocationChanged;
        protected virtual void OnLocationChanged()
        {
            Redraw();
            if (Controls != null)
                for (int i = 0; i < Controls.Count; i++)
                    Controls[i].OnLocationChanged();

            if (LocationChanged != null)
                LocationChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //提示文字？
        #region Hint
        private string _hint;
        public string Hint
        {
            get { return _hint; }
            set
            {
                if (_hint == value)
                    return;

                _hint = value;
                OnHintChanged(EventArgs.Empty);
            }
        }
        public event EventHandler HintChanged;
        private void OnHintChanged(EventArgs e)
        {
            Redraw();
            if (HintChanged != null)
                HintChanged.Invoke(this, e);
        }
        #endregion
        //是否模态窗口？
        #region Modal
        private bool _modal;
        public bool Modal
        {
            get { return _modal; }
            set
            {
                if (_modal == value)
                    return;
                _modal = value;
                OnModalChanged();
            }
        }
        public event EventHandler ModalChanged;
        private void OnModalChanged()
        {
            Redraw();
            if (ModalChanged != null)
                ModalChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //是否可以移动
        #region Movable
        protected internal bool Moving;
        private bool _movable;
        private Point _movePoint;

        public bool Movable
        {
            get { return _movable; }
            set
            {
                if (_movable == value)
                    return;
                _movable = value;
                OnMovableChanged();
            }
        }

        public event EventHandler MovableChanged;
        public event MouseEventHandler OnMoving;

        private void OnMovableChanged()
        {
            Redraw();
            if (MovableChanged != null)
                MovableChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //不能控制？如果不能控制，则不能相应相关的鼠标事件
        #region Not Control
        private bool _notControl;
        public bool NotControl
        {
            private get { return _notControl; }
            set
            {
                if (_notControl == value)
                    return;
                _notControl = value;
                OnNotControlChanged();
            }
        }
        public event EventHandler NotControlChanged;
        private void OnNotControlChanged()
        {
            Redraw();
            if (NotControlChanged != null)
                NotControlChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //透明度0-1，1代表不透明,0代表透明
        #region Opacity
        private float _opacity;
        public float Opacity
        {
            get { return _opacity; }
            set
            {
                if (value > 1F)
                    value = 1F;
                if (value < 0F)
                    value = 0;

                if (_opacity == value)
                    return;

                _opacity = value;
                OnOpacityChanged();
            }
        }
        public event EventHandler OpacityChanged;
        private void OnOpacityChanged()
        {
            Redraw();
            if (OpacityChanged != null)
                OpacityChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //父窗口,父窗口也加入窗口列表中，如果改变父窗口，则也改变窗口列表中的父窗口
        #region Parent
        private MirControl _parent;
        public MirControl Parent
        {
            get { return _parent; }
            set
            {
                if (_parent == value) return;

                if (_parent != null)
                    _parent.RemoveControl(this);
                _parent = value;
                if (_parent != null)
                    _parent.AddControl(this);
                OnParentChanged();
            }
        }
        public event EventHandler ParentChanged;
        protected virtual void OnParentChanged()
        {
            OnLocationChanged();
            if (ParentChanged != null)
                ParentChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //窗口大小，如果改变窗口大小，则纹理不可用
        #region Size

// ReSharper disable InconsistentNaming
        protected Size _size;
// ReSharper restore InconsistentNaming

        public virtual Size Size
        {
            get { return _size; }
            set
            {
                if (_size == value)
                    return;
                _size = value;
                OnSizeChanged();
            }
        }

        public virtual Size TrueSize
        {
            get { return _size; }
        }

        public event EventHandler SizeChanged;
        protected virtual void OnSizeChanged()
        {
            TextureValid = false;
            Redraw();
            
            if (SizeChanged != null)
                SizeChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //声音
        #region Sound
        private int _sound;
        public int Sound
        {
            get { return _sound; }
            set
            {
                if (_sound == value)
                    return;
                _sound = value;
                OnSoundChanged();
            }
        }
        public event EventHandler SoundChanged;
        private void OnSoundChanged()
        {
            if (SoundChanged != null)
                SoundChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //是否排序?如果排序，则把当前窗口放最后
        #region Sort
        private bool _sort;
        public bool Sort
        {
            get { return _sort; }
            set
            {
                if (_sort == value)
                    return;
                _sort = value;
                OnSortChanged();
            }
        }
        public event EventHandler SortChanged;
        private void OnSortChanged()
        {
            Redraw();
            if (SortChanged != null)
                SortChanged.Invoke(this, EventArgs.Empty);
        }
        public void TrySort()
        {
            if (Parent == null)
                return;

            Parent.TrySort();

            if (Parent.Controls[Parent.Controls.Count - 1] == this)
                return;

            if (!Sort) return;

            Parent.Controls.Remove(this);
            Parent.Controls.Add(this);

            Redraw();
        }
        #endregion
        //是否可以访问(是否可见)，父窗口不可见，则当前窗口也不可见
        //如果可见性变化，则窗口不能移动，并且还原回原来的位置，
        #region Visible
        private bool _visible;
        public virtual bool Visible
        {
            get { return Parent == null ? _visible : Parent.Visible && _visible; }
            set
            {
                if (_visible == value)
                    return;
                _visible = value;
                OnVisibleChanged();
            }
        }
        public event EventHandler VisibleChanged;
        protected virtual void OnVisibleChanged()
        {
            Redraw();
            if (VisibleChanged != null)
                VisibleChanged.Invoke(this, EventArgs.Empty);
            //窗口不能移动，并且还原回原来位置
            Moving = false;
            _movePoint = Point.Empty;
            //如果是可排序的，则放到最上面
            if (Sort && Parent != null)
            {
                Parent.Controls.Remove(this);
                Parent.Controls.Add(this);
            }
            //如果是鼠标控制的窗口，并且隐藏了，则停用,
            if (MouseControl == this && !Visible)
            {
                Dehighlight();
                Deactivate();
            }
            else if (IsMouseOver(CMain.MPoint))
                Highlight();

            //所有子窗口都收到变化事件
            if (Controls != null)
                foreach (MirControl control in Controls)
                    control.OnVisibleChanged();
        }
        protected void OnBeforeShown()
        {
            if (HasShown)
                return;

            if (Visible && IsMouseOver(CMain.MPoint))
                Highlight();

            if (BeforeShown != null)
                BeforeShown.Invoke(this, EventArgs.Empty);
        }
        protected void OnShown()
        {
            if (HasShown)
                return;

            if (Shown != null)
                Shown.Invoke(this, EventArgs.Empty);
            
            HasShown = true;
        }
        #endregion

        //多行？虚函数
        #region MultiLine

        public virtual void MultiLine()
        {
        }

        #endregion

        //位置信息，包括中心点位置，上下左右，左上，左下，右上，右下等9个点的位置
        #region Positions

        protected Point Center
        {
            get { return new Point((Settings.ScreenWidth - Size.Width) / 2, (Settings.ScreenHeight - Size.Height) / 2); }
        }

        protected Point Left
        {
            get { return new Point(0, (Settings.ScreenHeight - Size.Height) / 2); }
        }

        protected Point Top
        {
            get { return new Point((Settings.ScreenWidth - Size.Width) / 2, 0); }
        }

        protected Point Right
        {
            get { return new Point(Settings.ScreenWidth - Size.Width, (Settings.ScreenHeight - Size.Height) / 2); }
        }

        protected Point Bottom
        {
            get { return new Point((Settings.ScreenWidth - Size.Width) / 2, Settings.ScreenHeight - Size.Height); }
        }

        protected Point TopLeft
        {
            get { return new Point(0, 0); }
        }

        protected Point TopRight
        {
            get { return new Point(Settings.ScreenWidth - Size.Width, 0); }
        }

        protected Point BottomRight
        {
            get { return new Point(Settings.ScreenWidth - Size.Width, Settings.ScreenHeight - Size.Height); }
        }

        protected Point BottomLeft
        {
            get { return new Point(0, Settings.ScreenHeight - Size.Height); }
        }

        #endregion

        //靠前显示？放到前面来
        public void BringToFront()
        {
            if (Parent == null) return;
            int index = _parent.Controls.IndexOf(this);
            if (index == _parent.Controls.Count - 1) return;

            _parent.Controls.RemoveAt(index);
            _parent.Controls.Add(this);
            Redraw();
        }

        public MirControl()
        {
            //初始化默认数据，有效，可见，不透明的
            Controls = new List<MirControl>();
            _opacity = 1F;
            _enabled = true;
            _foreColour = Color.White;
            _visible = true;
            _sound = SoundList.None;
        }
        //画图方法
        public virtual void Draw()
        {
            if (IsDisposed || !Visible /*|| Size.Width == 0 || Size.Height == 0*/ || Size.Width > Settings.ScreenWidth || Size.Height > Settings.ScreenHeight)
                return;
            //显示前
            OnBeforeShown();
            //画前
            BeforeDrawControl();
            //画控件
            DrawControl();
            //画子控件
            DrawChildControls();
            //画边框
            DrawBorder();
            //完成绘画
            AfterDrawControl();
            //10分钟清理缓存
            CleanTime = CMain.Time + Settings.CleanDelay;

            OnShown();
        }

        protected virtual void BeforeDrawControl()
        {
            if (BeforeDraw != null)
                BeforeDraw.Invoke(this, EventArgs.Empty);
        }
        protected internal virtual void DrawControl()
        {
            if (!DrawControlTexture)
                return;

            if (!TextureValid)
                CreateTexture();

            if (ControlTexture == null || ControlTexture.Disposed)
                return;
            //这里有点奇怪，每次画，透明度都设置回去？好像没必要？
            float oldOpacity = DXManager.Opacity;
            DXManager.SetOpacity(Opacity);
            DXManager.Sprite.Draw2D(ControlTexture, Point.Empty, 0F, DisplayLocation, Color.White);
            DXManager.SetOpacity(oldOpacity);
            //这里和上面的重复了？
            CleanTime = CMain.Time + Settings.CleanDelay;
        }
        protected void DrawChildControls()
        {
            if (Controls != null)
                for (int i = 0; i < Controls.Count; i++)
                    if (Controls[i] != null)
                        Controls[i].Draw();
        }
        protected virtual void DrawBorder()
        {
            if (!Border || BorderInfo == null)
                return;

            DXManager.Sprite.Flush();
            DXManager.Line.Draw(BorderInfo, _borderColour);
        }
        protected void AfterDrawControl()
        {
            if (AfterDraw != null)
                AfterDraw.Invoke(this, EventArgs.Empty);
        }
        //销毁活动的界面？
        protected virtual void Deactivate()
        {
            if (ActiveControl != this)
                return;

            ActiveControl = null;
            Moving = false;
            _movePoint = Point.Empty;
        }
        //销毁鼠标界面
        protected virtual void Dehighlight()
        {
            if (MouseControl != this)
                return;
            MouseControl.OnMouseLeave();
            MouseControl = null;
        }
        //重新激活
        protected virtual void Activate()
        {
            if (ActiveControl == this)
                return;

            if (ActiveControl != null)
                ActiveControl.Deactivate();

            ActiveControl = this;
        }
        //突出鼠标控制窗口？
        protected virtual void Highlight()
        {
            if (MouseControl == this)
                return;
            if (NotControl)
            {

            }
            if (MouseControl != null)
                MouseControl.Dehighlight();

            if (ActiveControl != null && ActiveControl != this) return;

            OnMouseEnter();
            MouseControl = this;
        }
        //是否鼠标经过？可见，可控状态下，如果移动，模态，或者在当前区域内，即算鼠标经过
        public virtual bool IsMouseOver(Point p)
        {
            return Visible && (DisplayRectangle.Contains(p) || Moving || Modal) && !NotControl;
        }
        //鼠标进入事件
        protected virtual void OnMouseEnter()
        {
            if (!_enabled)
                return;

            Redraw();

            if (MouseEnter != null)
                MouseEnter.Invoke(this, EventArgs.Empty);
        }
        //鼠标离开
        protected virtual void OnMouseLeave()
        {
            if (!_enabled)
                return;

            Redraw();

            if (MouseLeave != null)
                MouseLeave.Invoke(this, EventArgs.Empty);
        }
        //鼠标点击，播放音乐哦
        public virtual void OnMouseClick(MouseEventArgs e)
        {
            if (!Enabled)
                return;

            if (Sound != SoundList.None)
                SoundManager.PlaySound(Sound);

            if (Click != null)
                InvokeMouseClick(e);
        }
        //鼠标双击，播放音乐哦
        public virtual void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (!Enabled)
                return;

            if (DoubleClick != null)
            {
                if (Sound != SoundList.None)
                    SoundManager.PlaySound(Sound);
                InvokeMouseDoubleClick(e);
            }
            else
                OnMouseClick(e);
        }
        public void InvokeMouseClick(EventArgs e)
        {
            if (Click != null)
                Click.Invoke(this, e);
        }
        public void InvokeMouseDoubleClick(EventArgs e)
        {
            DoubleClick.Invoke(this, e);
        }
        //鼠标移动，拖动窗口，这里可以优化下？
        public virtual void OnMouseMove(MouseEventArgs e)
        {
            if (!_enabled)
                return;


            if (Moving)
            {
                //Subtract这个方法哪里来的，那么神奇的么。靠了
                Point tempPoint = CMain.MPoint.Subtract(_movePoint);

                if (Parent == null)
                {
                    if (tempPoint.Y + TrueSize.Height > Settings.ScreenHeight)
                        tempPoint.Y = Settings.ScreenHeight - TrueSize.Height - 1;

                    if (tempPoint.X + TrueSize.Width > Settings.ScreenWidth)
                        tempPoint.X = Settings.ScreenWidth - TrueSize.Width - 1;
                }
                else
                {
                    if (tempPoint.Y + TrueSize.Height > Parent.TrueSize.Height)
                        tempPoint.Y = Parent.TrueSize.Height - TrueSize.Height;

                    if (tempPoint.X + TrueSize.Width > Parent.TrueSize.Width)
                        tempPoint.X = Parent.TrueSize.Width - TrueSize.Width;
                }

                if (tempPoint.X < 0)
                    tempPoint.X = 0;
                if (tempPoint.Y < 0)
                    tempPoint.Y = 0;

                Location = tempPoint;
                if (OnMoving != null)
                    OnMoving.Invoke(this, e);
                return;
            }

            if (Controls != null)
                for (int i = Controls.Count - 1; i >= 0; i--)
                    if (Controls[i].IsMouseOver(CMain.MPoint))
                    {
                        Controls[i].OnMouseMove(e);
                        return;
                    }

            Highlight();

            if (MouseMove != null)
                MouseMove.Invoke(this, e);
        }
        //按下鼠标，物品激活，并进入移动状态
        public virtual void OnMouseDown(MouseEventArgs e)
        {
            if (!_enabled)
                return;

            Activate();

            TrySort();

            if (_movable)
            {
                Moving = true;
                _movePoint = CMain.MPoint.Subtract(Location);
            }

            if (MouseDown != null)
                MouseDown.Invoke(this, e);
        }
        //放开鼠标，物品进入不能移动
        public virtual void OnMouseUp(MouseEventArgs e)
        {
            if (!_enabled)
                return;

            if (Moving)
            {
                Moving = false;
                _movePoint = Point.Empty;
            }

            if (ActiveControl != null) ActiveControl.Deactivate();

            if (MouseUp != null)
                MouseUp.Invoke(this, e);
        }
        //鼠标滚动
        public virtual void OnMouseWheel(MouseEventArgs e)
        {
            if (!Enabled)
                return;

            if (MouseWheel != null)
                MouseWheel(this, e);
        }
        public virtual void OnKeyPress(KeyPressEventArgs e)
        {
            if (!_enabled)
                return;

            if (Controls != null)
                for (int i = Controls.Count - 1; i >= 0; i--)
                    if (e.Handled)
                        return;
                    else
                        Controls[i].OnKeyPress(e);

            if (KeyPress == null)
                return;
            KeyPress.Invoke(this, e);
        }
        public virtual void OnKeyDown(KeyEventArgs e)
        {
            if (!_enabled)
                return;

            if (Controls != null)
                for (int i = Controls.Count - 1; i >= 0; i--)
                    if (e.Handled)
                        return;
                    else
                        Controls[i].OnKeyDown(e);

            if (KeyDown == null)
                return;
            KeyDown.Invoke(this, e);
        }
        public virtual void OnKeyUp(KeyEventArgs e)
        {
            if (!_enabled)
                return;

            if (Controls != null)
                for (int i = Controls.Count - 1; i >= 0; i--)
                    if (e.Handled)
                        return;
                    else
                        Controls[i].OnKeyUp(e);

            if (KeyUp == null)
                return;
            KeyUp.Invoke(this, e);
        }
        //重画，什么也不做，让子类自己实现吧
        public virtual void Redraw()
        {
            if (Parent != null) Parent.Redraw();

        }

        #region Disposable
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Disposing != null)
                    Disposing.Invoke(this, EventArgs.Empty);

                Disposing = null;

                BackColourChanged = null;
                _backColour = Color.Empty;

                BorderChanged = null;
                _border = false;
                BorderRectangle = Rectangle.Empty;
                _borderInfo = null;

                BorderColourChanged = null;
                _borderColour = Color.Empty;

                DrawControlTexture = false;
                if (ControlTexture != null && !ControlTexture.Disposed)
                    ControlTexture.Dispose();
                ControlTexture = null;
                TextureValid = false;

                ControlAdded = null;
                ControlRemoved = null;

                if (Controls != null)
                {
                    for (int i = Controls.Count - 1; i >= 0; i--)
                    {
                        if (Controls[i] != null && !Controls[i].IsDisposed)
                            Controls[i].Dispose();
                    }

                    Controls = null;
                }
                _enabled = false;
                EnabledChanged = null;

                HasShown = false;

                BeforeDraw = null;
                AfterDraw = null;
                Shown = null;
                BeforeShown = null;

                Click = null;
                DoubleClick = null;
                MouseEnter = null;
                MouseLeave = null;
                MouseMove = null;
                MouseDown = null;
                MouseUp = null;
                MouseWheel = null;

                KeyPress = null;
                KeyUp = null;
                KeyDown = null;

                ForeColourChanged = null;
                _foreColour = Color.Empty;

                LocationChanged = null;
                _location = Point.Empty;

                ModalChanged = null;
                _modal = false;

                MovableChanged = null;
                _movePoint = Point.Empty;
                Moving = false;
                OnMoving = null;
                _movable = false;

                NotControlChanged = null;
                _notControl = false;

                OpacityChanged = null;
                _opacity = 0F;

                if (Parent != null && Parent.Controls != null)
                    Parent.Controls.Remove(this);
                ParentChanged = null;
                _parent = null;

                SizeChanged = null;
                _size = Size.Empty;

                SoundChanged = null;
                _sound = 0;

                VisibleChanged = null;
                _visible = false;

                if (ActiveControl == this) ActiveControl = null;
                if (MouseControl == this) MouseControl = null;
            }

            IsDisposed = true;
        }
        #endregion



    }
}
