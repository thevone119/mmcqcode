using System;
using System.Drawing;
using Client.MirGraphics;

namespace Client.MirControls
{
    /// <summary>
    /// 图像窗口
    /// 这个是有图的,通过图像加载做显示等
    /// </summary>
    public class MirImageControl : MirControl
    {
        public override Point DisplayLocation { get { return UseOffSet ? base.DisplayLocation.Add(Library.GetOffSet(Index)) : base.DisplayLocation; } }
        public Point DisplayLocationWithoutOffSet { get { return base.DisplayLocation; } }

        //是否画图
        #region DrawImage
        private bool _drawImage;
        public bool DrawImage
        {
            get { return _drawImage; }
            set
            {
                if (_drawImage == value)
                    return;
                _drawImage = value;
                OnDrawImageChanged();
            }
        }
        public event EventHandler DrawImageChanged;
        private void OnDrawImageChanged()
        {
            Redraw();
            if (DrawImageChanged != null)
                DrawImageChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //定义图像的索引位置
        #region Index
        private int _index;
        public virtual int Index
        {
            get { return _index; }
            set
            {
                if (_index == value)
                    return;
                _index = value;
                OnIndexChanged();
            }
        }
        public event EventHandler IndexChanged;
        protected void OnIndexChanged()
        {
            OnSizeChanged();
            if (IndexChanged != null)
                IndexChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //图像的lib引用
        #region Library
        private MLibrary _library;
        public MLibrary Library
        {
            get { return _library; }
            set
            {
                if (_library == value)
                    return;
                _library = value;
                OnLibraryChanged();
            }
        }
        public event EventHandler LibraryChanged;
        private void OnLibraryChanged()
        {
            OnSizeChanged();
            if (LibraryChanged != null)
                LibraryChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //是否检测像素
        #region PixelDetect
        private bool _pixelDetect;
        protected bool PixelDetect
        {
            set
            {
                if (_pixelDetect == value)
                    return;
                _pixelDetect = value;
                OnPixelDetectChanged();
            }
        }
        public event EventHandler PixelDetectChanged;
        private void OnPixelDetectChanged()
        {
            Redraw();
            if (PixelDetectChanged != null)
                PixelDetectChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //是否偏移，如果偏移，则位置以父窗口的为偏移进行计算
        #region UseOffset
        private bool _useOffSet;
        public bool UseOffSet
        {
            protected get { return _useOffSet; }
            set
            {
                if (_useOffSet == value)
                    return;
                _useOffSet = value;
                OnUseOffSetChanged();
            }
        }
        public event EventHandler UseOffSetChanged;
        private void OnUseOffSetChanged()
        {
            OnLocationChanged();
            if (UseOffSetChanged != null)
                UseOffSetChanged.Invoke(this, EventArgs.Empty);
        }
        #endregion
        //图像的大小
        #region Size
        public override Size Size
        {
            set { base.Size = value; }
            get
            {
                if (Library != null && Index >= 0)
                    return Library.GetTrueSize(Index);
                return base.Size;
            }
        }

        public override Size TrueSize
        {
            get
            {
                if (Library != null && Index >= 0)
                    return Library.GetTrueSize(Index);
                return base.TrueSize;
            }
        }

        #endregion

        public MirImageControl()
        {
            _drawImage = true;
            _index = -1;
            ForeColour = Color.White;//图片一开始是白色
        }

        protected internal override void DrawControl()
        {
            base.DrawControl();

            if (DrawImage && Library != null)
            {
                bool oldGray = DXManager.GrayScale;

                if (GrayScale)
                {
                    DXManager.SetGrayscale(true);
                }

                if (Blending)
                    Library.DrawBlend(Index, DisplayLocation, ForeColour, false, BlendingRate);
                else
                    Library.Draw(Index, DisplayLocation, ForeColour, false, Opacity);

                if (GrayScale) DXManager.SetGrayscale(oldGray);
            }
        }

        public override bool IsMouseOver(Point p)
        {
            return base.IsMouseOver(p) && (!_pixelDetect || Library.VisiblePixel(Index, p.Subtract(DisplayLocation),true) || Moving);
        }

        #region Disposable
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            
            if (!disposing) return;

            DrawImageChanged = null;
            _drawImage = false;

            IndexChanged = null;
            _index = 0;

            LibraryChanged = null;
            Library = null;

            PixelDetectChanged = null;
            _pixelDetect = false;

            UseOffSetChanged = null;
            _useOffSet = false;
        }
        #endregion
    }
}