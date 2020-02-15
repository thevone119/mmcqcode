using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirObjects;
using Client.MirSounds;
using Microsoft.DirectX.Direct3D;
using Font = System.Drawing.Font;
using S = ServerPackets;
using C = ClientPackets;
using Effect = Client.MirObjects.Effect;

using Client.MirScenes.Dialogs;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace Client.MirScenes.Dialogs
{
    //实现图形验证码
    public sealed class CheckCodeDialog : MirImageControl
    {

        public MirButton CloseButton, ConfirmButton, CancelButton;

        public MirLabel NameLabel;

        public MirLabel tx_str1;//提醒文字
        public MirLabel tx_str2;//提醒文字
        public MirLabel tx_str3;//提醒文字

        public MirTextBox codeinput;


        public MirExtImage codemsg;//校验码的code


        private string _checkcode;
        public string checkcode
        {
            get { return _checkcode; }
            set
            {
                if (_checkcode == value)
                    return;
                _checkcode = value;
                string tempcode = EncryptHelper.DesDecrypt(_checkcode);
                if (tempcode.Length > 2)
                {
                    codemsg.ImageBytes = CreateValidateGraphic(tempcode.Substring(0, 2));
                }
            }
        }
        public long LastCheckTime;

        private long lastChangeTime;//

        public CheckCodeDialog()
        {
            Index = 995;
            Library = Libraries.Prguse;

            Sort = true;

            BeforeDraw += (o, e) => OnBeforeDraw();
            KeyPress += on_KeyPress;

            NameLabel = new MirLabel
            {
                Text = "请输入验证码",
                Parent = this,
                Font = new Font(Settings.FontName, 10F, FontStyle.Bold),
                ForeColour = Color.BurlyWood,
                Location = new Point(30, 6),
                AutoSize = true
            };


            CloseButton = new MirButton
            {
                HoverIndex = 361,
                Index = 360,
                Location = new Point(413, 3),
                Library = Libraries.Prguse2,
                Parent = this,
                PressedIndex = 362,
                Sound = SoundList.ButtonA,
            };
            CloseButton.Click += (o, e) => Hide();



            tx_str1 = new MirLabel
            {
                Text = "请在",
                Parent = this,
                Size = new Size(40, 18),
                Location = new Point(40, 40),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            };

            tx_str2 = new MirLabel
            {
                Text = "X秒",
                Parent = this,
                Size = new Size(60, 18),
                Location = new Point(75, 40),
                ForeColour = Color.Gold,
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            };


            tx_str3 = new MirLabel
            {
                Text = "内输入下列图形验证码以完成验证...",
                Parent = this,
                Size = new Size(300, 18),
                Location = new Point(120, 40),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            };

            codemsg = new MirExtImage
            {
                Parent = this,
                Location = new Point(160, 70),
                ImageBytes = CreateValidateGraphic("00")
            };

            //
            codeinput = new MirTextBox
            {
                BackColour = Color.Green,
                ForeColour = Color.White,
                Parent = this,
                Size = new Size(40, 25),
                Location = new Point(160, 110),
                Font = new Font(Settings.FontName, 9F),
                MaxLength = 4,
                CanLoseFocus = true,
            };
            codeinput.KeyPress += on_KeyPress;

            ConfirmButton = new MirButton
            {
                Index = 385,
                HoverIndex = 383,
                PressedIndex = 384,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(25, 84),
                Location = new Point(140, 140),
                Sound = SoundList.ButtonA,
                Text = "确 认",
                CenterText = true,
            };
            ConfirmButton.Click += (o, e) =>
            {
                Confirm();
            };
        }
        private void OnBeforeDraw()
        {
            if (CMain.Time > LastCheckTime)
            {
                this.Hide();
                return;
            }
            if(lastChangeTime==0||CMain.Time> lastChangeTime)
            {
                lastChangeTime = CMain.Time + 1000;
                tx_str2.Text = string.Format("{0} 秒", (LastCheckTime- CMain.Time)/1000);
            }
        }


        private void on_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != (char)Keys.Enter) return;
            Confirm();
            e.Handled = true;
        }

        private void Confirm()
        {
            string c = codeinput.Text;
            if (c == null)
            {
                c = "0";
            }
            //清空
            MirScene.LastCheckCode = "";
            MirScene.LastCheckTime = 0;
            Network.Enqueue(new C.CheckCode
            {
                code = c.Trim()
            });
            Hide();
        }

        public void Hide()
        {
            Visible = false;
            codeinput.Text = "";
            //GameScene.Scene.TrustMerchantDialog.Hide();
            //GameScene.Scene.PlayerShopDialog.Hide();
            //GameScene.Scene.InventoryDialog.Location = new Point(0, 0);
        }

        public void Show()
        {
            Visible = true;
        }

        /// <summary>
        /// 创建验证码图片
        /// </summary>
        /// <param name="validateCode"></param>
        /// <returns></returns>
        public byte[] CreateValidateGraphic(string validateCode)
        {
  
            Bitmap image = new Bitmap((int)Math.Ceiling(validateCode.Length * 16.0), 27);
            Graphics g = Graphics.FromImage(image);
            try
            {
                //生成随机生成器
                Random random = new Random();
                //清空图片背景色
                g.Clear(Color.White);
                //画图片的干扰线
                for (int i = 0; i < 25; i++)
                {
                    int x1 = random.Next(image.Width);
                    int x2 = random.Next(image.Width);
                    int y1 = random.Next(image.Height);
                    int y2 = random.Next(image.Height);
                    g.DrawLine(new Pen(Color.Silver), x1, x2, y1, y2);
                }
                Font font = new Font("Arial", 13, (FontStyle.Bold | FontStyle.Italic));
                LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, image.Width, image.Height), Color.Blue, Color.DarkRed, 1.2f, true);
                g.DrawString(validateCode, font, brush, 3, 2);

                //画图片的前景干扰线
                for (int i = 0; i < 100; i++)
                {
                    int x = random.Next(image.Width);
                    int y = random.Next(image.Height);
                    image.SetPixel(x, y, Color.FromArgb(random.Next()));
                }
                //画图片的边框线
                g.DrawRectangle(new Pen(Color.Silver), 0, 0, image.Width - 1, image.Height - 1);

                //保存图片数据
                MemoryStream stream = new MemoryStream();
                image.Save(stream, ImageFormat.Jpeg);

                //输出图片流
                return stream.ToArray();
            }
            finally
            {
                g.Dispose();
                image.Dispose();
            }
        }
    }
}
