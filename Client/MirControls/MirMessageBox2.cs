using System;
using System.Drawing;
using System.Windows.Forms;
using Client.MirGraphics;

namespace Client.MirControls
{
    //扩展原来的MessageBox,支持按钮名称的定制
    public sealed class MirMessageBox2 : MirImageControl
    {
        public MirLabel Label;
        public MirButton OKButton, CancelButton, NoButton, YesButton;
        public string OKText="确定", CancelText="取消", NoText="否", YesText="是";
        public MirMessageBoxButtons Buttons;
        public bool AllowKeyPress = true;

        //固定参数
        private static int butIndex = 385, butPressedIndex = 384, butHoverIndex = 383;

        public MirMessageBox2(string message, MirMessageBoxButtons b = MirMessageBoxButtons.OK, bool allowKeys = true)
        {
            DrawImage = true;
            ForeColour = Color.White;
            Buttons = b;
            Modal = true;
            Movable = false;
            AllowKeyPress = allowKeys;

            Index = 360;
            Library = Libraries.Prguse;

            Location = new Point((Settings.ScreenWidth - Size.Width) / 2, (Settings.ScreenHeight - Size.Height) / 2);


            Label = new MirLabel
            {
                AutoSize = false,
                // DrawFormat = StringFormatFlags.FitBlackBox,
                Location = new Point(35, 35),
                Size = new Size(390, 110),
                Parent = this,
                Text = message
            };


            switch (Buttons)
            {
                case MirMessageBoxButtons.OK:
                    OKButton = new MirButton
                    {
                        Index = butIndex,
                        HoverIndex = butHoverIndex,
                        PressedIndex = butPressedIndex,
                        Library = Libraries.Prguse2,
                        Text = OKText,
                        CenterText = true,
                        Location = new Point(360, 157),
                        Parent = this,
                    };
                    OKButton.Click += (o, e) => Dispose();
                    break;
                case MirMessageBoxButtons.OKCancel:
                    OKButton = new MirButton
                    {
                        Index = butIndex,
                        HoverIndex = butHoverIndex,
                        PressedIndex = butPressedIndex,
                        Library = Libraries.Prguse2,
                        Text = OKText,
                        CenterText = true,
                        Location = new Point(260, 157),
                        Parent = this,
                    };
                    OKButton.Click += (o, e) => Dispose();
                    CancelButton = new MirButton
                    {
                        Index = butIndex,
                        HoverIndex = butHoverIndex,
                        PressedIndex = butPressedIndex,
                        Library = Libraries.Prguse2,
                        Text = CancelText,
                        CenterText = true,
                        Location = new Point(360, 157),
                        Parent = this,
                    };
                    CancelButton.Click += (o, e) => Dispose();
                    break;
                case MirMessageBoxButtons.YesNo:
                    YesButton = new MirButton
                    {
                        Index = butIndex,
                        HoverIndex = butHoverIndex,
                        PressedIndex = butPressedIndex,
                        Library = Libraries.Prguse2,
                        Text = YesText,
                        CenterText = true,
                        Location = new Point(260, 157),
                        Parent = this,
                    };
                    YesButton.Click += (o, e) => Dispose();
                    NoButton = new MirButton
                    {
                        Index = butIndex,
                        HoverIndex = butHoverIndex,
                        PressedIndex = butPressedIndex,
                        Library = Libraries.Prguse2,
                        Text = NoText,
                        CenterText = true,
                        Location = new Point(360, 157),
                        Parent = this,
                    };
                    NoButton.Click += (o, e) => Dispose();
                    break;
                case MirMessageBoxButtons.YesNoCancel:
                    YesButton = new MirButton
                    {
                        Index = butIndex,
                        HoverIndex = butHoverIndex,
                        PressedIndex = butPressedIndex,
                        Library = Libraries.Prguse2,
                        Text = YesText,
                        CenterText = true,
                        Location = new Point(160, 157),
                        Parent = this,
                    };
                    YesButton.Click += (o, e) => Dispose();
                    NoButton = new MirButton
                    {
                        Index = butIndex,
                        HoverIndex = butHoverIndex,
                        PressedIndex = butPressedIndex,
                        Library = Libraries.Prguse2,
                        Text = NoText,
                        CenterText = true,
                        Location = new Point(260, 157),
                        Parent = this,
                    };
                    NoButton.Click += (o, e) => Dispose();
                    CancelButton = new MirButton
                    {
                        Index = butIndex,
                        HoverIndex = butHoverIndex,
                        PressedIndex = butPressedIndex,
                        Library = Libraries.Prguse2,
                        Text = CancelText,
                        CenterText = true,
                        Location = new Point(360, 157),
                        Parent = this,
                    };
                    CancelButton.Click += (o, e) => Dispose();
                    break;
                case MirMessageBoxButtons.Cancel:
                    CancelButton = new MirButton
                    {
                        Index = butIndex,
                        HoverIndex = butHoverIndex,
                        PressedIndex = butPressedIndex,
                        Library = Libraries.Prguse2,
                        Text = CancelText,
                        CenterText = true,
                        Location = new Point(360, 157),
                        Parent = this,
                    };
                    CancelButton.Click += (o, e) => Dispose();
                    break;
            }
        }

        public void Show()
        {
            //重设各种值
            if (YesButton != null)
            {
                YesButton.Text = YesText;
            }
            if (NoButton != null)
            {
                NoButton.Text = NoText;
            }
            if (OKButton != null)
            {
                OKButton.Text = OKText;
            }
            if (CancelButton != null)
            {
                CancelButton.Text = CancelText;
            }

            if (Parent != null) return;

            Parent = MirScene.ActiveScene;

            Highlight();

            for (int i = 0; i < Program.Form.Controls.Count; i++)
            {
                TextBox T = Program.Form.Controls[i] as TextBox;
                if (T != null && T.Tag != null && T.Tag != null)
                    ((MirTextBox)T.Tag).DialogChanged();
            }
        }


        public override void OnKeyDown(KeyEventArgs e)
        {
            if (AllowKeyPress)
            {
                base.OnKeyDown(e);
                e.Handled = true;
            }
        }
        public override void OnKeyUp(KeyEventArgs e)
        {
            if (AllowKeyPress)
            {
                base.OnKeyUp(e);
                e.Handled = true;
            }
        }
        public override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            if (AllowKeyPress)
            {
                if (e.KeyChar == (char)Keys.Escape)
                {
                    switch (Buttons)
                    {
                        case MirMessageBoxButtons.OK:
                            if (OKButton != null && !OKButton.IsDisposed) OKButton.InvokeMouseClick(null);
                            break;
                        case MirMessageBoxButtons.OKCancel:
                        case MirMessageBoxButtons.YesNoCancel:
                            if (CancelButton != null && !CancelButton.IsDisposed) CancelButton.InvokeMouseClick(null);
                            break;
                        case MirMessageBoxButtons.YesNo:
                            if (NoButton != null && !NoButton.IsDisposed) NoButton.InvokeMouseClick(null);
                            break;
                    }
                }

                else if (e.KeyChar == (char)Keys.Enter)
                {
                    switch (Buttons)
                    {
                        case MirMessageBoxButtons.OK:
                        case MirMessageBoxButtons.OKCancel:
                            if (OKButton != null && !OKButton.IsDisposed) OKButton.InvokeMouseClick(null);
                            break;
                        case MirMessageBoxButtons.YesNoCancel:
                        case MirMessageBoxButtons.YesNo:
                            if (YesButton != null && !YesButton.IsDisposed) YesButton.InvokeMouseClick(null);
                            break;

                    }
                }
                e.Handled = true;
            }
        }


        public static void Show(string message, bool close = false)
        {
            MirMessageBox2 box = new MirMessageBox2(message);

            if (close) box.OKButton.Click += (o, e) => Program.Form.Close();

            box.Show();
        }

        #region Disposable

        protected override void Dispose(bool disposing)
        {

            base.Dispose(disposing);

            if (!disposing) return;

            Label = null;
            OKButton = null;
            CancelButton = null;
            NoButton = null;
            YesButton = null;
            Buttons = 0;

            for (int i = 0; i < Program.Form.Controls.Count; i++)
            {
                TextBox T = (TextBox)Program.Form.Controls[i];
                if (T != null && T.Tag != null)
                    ((MirTextBox)T.Tag).DialogChanged();
            }
        }

        #endregion
    }
}
