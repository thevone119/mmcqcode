using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using Client.MirGraphics;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Font = System.Drawing.Font;

namespace Client.MirControls
{
    public class MirExtImage : MirControl
    {


        private byte[] _imagebytes;

        public byte[] ImageBytes
        {
            get { return _imagebytes; }
            set
            {
                if (_imagebytes == value)
                    return;
                _imagebytes = value;
                MemoryStream ms1 = new MemoryStream(_imagebytes);
                Bitmap bm = (Bitmap)Image.FromStream(ms1);
                ms1.Close();
                Size = bm.Size;
                if (ControlTexture != null && !ControlTexture.Disposed)
                {
                    ControlTexture.Dispose();
                }
            }
        }
  


        public MirExtImage()
        {
            DrawControlTexture = true;

        }
        
        protected override unsafe void CreateTexture()
        {
            if (Size.Width == 0 || Size.Height == 0 || _imagebytes==null)
                return;

            if (ControlTexture != null && !ControlTexture.Disposed && TextureSize != Size)
                ControlTexture.Dispose();

            if (ControlTexture == null || ControlTexture.Disposed)
            {
                DXManager.ControlList.Add(this);

                ControlTexture = new Texture(DXManager.Device, Size.Width, Size.Height, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
                ControlTexture.Disposing += ControlTexture_Disposing;
                TextureSize = Size;
            }
            
            using (GraphicsStream stream = ControlTexture.LockRectangle(0, LockFlags.Discard))
            using (Bitmap image = new Bitmap(Size.Width, Size.Height, Size.Width * 4, PixelFormat.Format32bppArgb, (IntPtr) stream.InternalDataPointer))
            {
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.Clear(BackColour);
                    MemoryStream ms1 = new MemoryStream(_imagebytes);
                    Bitmap bm = (Bitmap)Image.FromStream(ms1);
                    graphics.DrawImage(bm,0,0);
                    ms1.Close();
                }
            }
            ControlTexture.UnlockRectangle(0);
            DXManager.Sprite.Flush();
            TextureValid = true;
        }

        #region Disposable
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;
            _imagebytes = null;
        }
        #endregion

    }
}