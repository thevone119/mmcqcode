using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirSounds;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using C = ClientPackets;

namespace Client.MirScenes.Dialogs
{
    //充值窗口
    public sealed class RechargeDialog : MirImageControl
    {
        //关闭按钮
        public MirButton CloseButton;
        //充值金额按钮
        public MirButton Recharge10Button, Recharge20Button, Recharge50Button, Recharge100Button;
        //充值提醒，说明
        public MirLabel RechargeRemind;
        //
        //确认，取消
        public MirButton pay1Button, pay2Button, CancelButton;

        public static int RechargeState = 0;//充值状态，0未充值，1：正在创建充值链接，2：正在充值 3：充值完成
        private byte money = 10;//当前选择的金额
        private byte payType = 1;//支付方式1：支付宝；2：微信支付 


        //固定参数
        private static int tabIndex= 385,tabPressedIndex = 384, tabHoverIndex = 383;

        public RechargeDialog()
        {
            Index = 311;
            Library = Libraries.Prguse;
            Movable = false;
            Sort = true;
            Location = Center;
            RechargeState = 0;
            //临时变量
            int left = 50;
            int top = 0;
         

            #region Buttons
            //关闭按钮，上一页，下一页按钮
            CloseButton = new MirButton
            {
                HoverIndex = 361,
                Index = 360,
                Location = new Point(411, 5),
                Library = Libraries.Prguse2,
                Parent = this,
                PressedIndex = 362,
                Sound = SoundList.ButtonA,
            };
            CloseButton.Click += (o, e) => Dispose();
            
            //4个分页按钮BaseButton ClassButton, ProtectButton, ItemButton

            Recharge10Button = new MirButton
            {
                Index = tabIndex,
                HoverIndex = tabHoverIndex,
                PressedIndex = tabPressedIndex,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(25, 84),
                Location = new Point(left, 33),
                Sound = SoundList.ButtonA,
                Text = "充值10元",
                CenterText = true,
            };

            Recharge10Button.Click += (o, e) =>
            {
                money = 10;
                UpdateDisplay();
            };
            left += 80;
            Recharge20Button = new MirButton
            {
                Index = tabIndex,
                HoverIndex = tabHoverIndex,
                PressedIndex = tabPressedIndex,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(25, 84),
                Location = new Point(left, 33),
                Sound = SoundList.ButtonA,
                Text = "充值20元",
                CenterText = true,
            };
            Recharge20Button.Click += (o, e) =>
            {
                money = 20;
                UpdateDisplay();
            };
            left += 80;
            Recharge50Button = new MirButton
            {
                Index = tabIndex,
                HoverIndex = tabHoverIndex,
                PressedIndex = tabPressedIndex,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(25, 84),
                Location = new Point(left, 33),
                Sound = SoundList.ButtonA,
                Text = "充值50元",
                CenterText = true,
            };
            Recharge50Button.Click += (o, e) =>
            {
                money = 50;
                UpdateDisplay();
            };
            left += 80;
            Recharge100Button = new MirButton
            {
                Index = tabIndex,
                HoverIndex = tabHoverIndex,
                PressedIndex = tabPressedIndex,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(25, 84),
                Location = new Point(left, 33),
                Sound = SoundList.ButtonA,
                Text = "充值100元",
                CenterText = true,
            };
            Recharge100Button.Click += (o, e) =>
            {
                money = 100;
                UpdateDisplay();
            };
          
            RechargeRemind = new MirLabel
            {
                Text = "",
                Parent = this,
                Size = new Size(350, 17),
                Location = new Point(50, 80),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            };
            //确认充值，取消
            pay1Button = new MirButton
            {
                Index = tabIndex,
                HoverIndex = tabHoverIndex,
                PressedIndex = tabPressedIndex,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(25, 84),
                Location = new Point(80, 150),
                Sound = SoundList.ButtonA,
                Text = "支付宝充值",
                CenterText = true,
            };
            pay1Button.Click += (o, e) =>
            {
                payType = 1;
                RechargeState = 1;
                Network.Enqueue(new C.RechargeCredit() { pay_type= payType, price= money });
                Dispose();
            };

            pay2Button = new MirButton
            {
                Index = tabIndex,
                HoverIndex = tabHoverIndex,
                PressedIndex = tabPressedIndex,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(25, 84),
                Location = new Point(170, 150),
                Sound = SoundList.ButtonA,
                Text = "微信充值",
                CenterText = true,
            };
            pay2Button.Click += (o, e) =>
            {
                payType = 2;
                RechargeState = 1;
                Network.Enqueue(new C.RechargeCredit() { pay_type = payType, price = money });
                Dispose();
            };

            CancelButton = new MirButton
            {
                Index = tabIndex,
                HoverIndex = tabHoverIndex,
                PressedIndex = tabPressedIndex,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(25, 84),
                Location = new Point(260, 150),
                Sound = SoundList.ButtonA,
                Text = "取消",
                CenterText = true,
            };
            CancelButton.Click += (o, e) =>
            {
                RechargeState = 0;
                Dispose();
            };
            UpdateDisplay();

            #endregion
        }
        //装载配置到窗口
        public void initData()
        {
            
        }
       
        //刷新显示
        private void UpdateDisplay()
        {
            if (!Visible) return;
            //选项卡处理
            switch (money)
            {
                case 10:
                    TabVisible10(true);
                    TabVisible20(false);
                    TabVisible50(false);
                    TabVisible100(false);

                    break;
                case 20:
                    TabVisible10(false);
                    TabVisible20(true);
                    TabVisible50(false);
                    TabVisible100(false);

                    break;
                case 50:
                    TabVisible10(false);
                    TabVisible20(false);
                    TabVisible50(true);
                    TabVisible100(false);
                    break;
                case 100:
                    TabVisible10(false);
                    TabVisible20(false);
                    TabVisible50(false);
                    TabVisible100(true);
                    break;
                default:
                    TabVisible10(true);
                    break;
            }
            //UpdatePage();
        }


        //充值10
        private void TabVisible10(bool visible)
        {
            if (visible)
            {
                RechargeRemind.Text = "充值10元，可获得10%的赠送，最终可获得"+ Globals.Recharge10 + "点元宝";
                Recharge10Button.Index = tabPressedIndex;
            }
            else
            {
                Recharge10Button.Index = tabIndex;
            }
        }

        //充值20
        private void TabVisible20(bool visible)
        {

            if (visible)
            {
                RechargeRemind.Text = "充值20元，可获得15%的赠送，最终可获得" + Globals.Recharge20 + "点元宝";
                Recharge20Button.Index = tabPressedIndex;
            }
            else
            {
                Recharge20Button.Index = tabIndex;
            }
        }
        //充值50
        private void TabVisible50(bool visible)
        {
            if (visible)
            {
                RechargeRemind.Text = "充值50元，可获得20%的赠送，最终可获得" + Globals.Recharge50 + "点元宝";
                Recharge50Button.Index = tabPressedIndex;
            }
            else
            {
                Recharge50Button.Index = tabIndex;
            }
        }
        //充值100
        private void TabVisible100(bool visible)
        {
            if (visible)
            {
                RechargeRemind.Text = "充值100元，可获得30%的赠送，最终可获得" + Globals.Recharge100 + "点元宝";
                Recharge100Button.Index = tabPressedIndex;
            }
            else
            {
                Recharge100Button.Index = tabIndex;
            }
        }
    }
    
}
