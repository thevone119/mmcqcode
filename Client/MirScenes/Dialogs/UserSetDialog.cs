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
    //用户设置，外挂，辅助设置等
    public sealed class UserSetDialog : MirImageControl
    {
        //关闭按钮，上一页，下一页按钮
        public MirButton CloseButton, PreviousButton, NextButton;
        //4个选项卡按钮
        public MirButton BaseButton, ClassButton, ProtectButton, ItemButton;
        public byte currTab = 1;//当前选择的TAB
        //各种开关
        //基本
        public MirCheckBox ShowLevelBox, ExcuseShiftBox, ShowPingBox, ShowFashionBox;
        //职业
        public MirCheckBox SeptumBox, AutoFlamingBox, AutoShieldBox;
        //保护
        public MirCheckBox OpenProtectBox;
      

        //保护的设置,输入框
        public MirTextBox HPLower1Text, HPLower2Text, HPLower3Text, HPUse1Text, HPUse2Text, HPUse3Text, MPLower1Text, MPUse1Text;

        //物品分页
        public MirCheckBox AutoPickUpBox;
        public MirCheckBox[] ItemRows;//这个是显示所有的物品的，要做分页处理
        public List<string> ItemAll;//所有物品
        public MirLabel PageNumberLabel;
        public int SelectedIndex = 0;
        public int StartIndex = 0;
        public int Page = 0;

        //固定参数
        private static int tabIndex= 1083,tabPressedIndex = 1082, tabHoverIndex = 1081;

        public UserSetDialog()
        {
            Index = 311;
            Library = Libraries.Prguse;
            Movable = false;
            Sort = true;
            Location = Center;

            PageNumberLabel = new MirLabel
            {
                Text = "",
                Parent = this,
                Size = new Size(83, 17),
                Location = new Point(87, 216),
                DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
            };

            #region Buttons
            //关闭按钮，上一页，下一页按钮
            CloseButton = new MirButton
            {
                HoverIndex = 361,
                Index = 360,
                Location = new Point(237, 3),
                Library = Libraries.Prguse2,
                Parent = this,
                PressedIndex = 362,
                Sound = SoundList.ButtonA,
            };
            CloseButton.Click += (o, e) => Hide();
            PreviousButton = new MirButton
            {
                Index = 240,
                HoverIndex = 241,
                PressedIndex = 242,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(16, 16),
                Location = new Point(70, 218),
                Sound = SoundList.ButtonA,
            };
            PreviousButton.Click += (o, e) =>
            {
                Page--;
                if (Page < 0) Page = 0;
                StartIndex = ItemRows.Length * Page;
                UpdatePage();
            };

            NextButton = new MirButton
            {
                Index = 243,
                HoverIndex = 244,
                PressedIndex = 245,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(16, 16),
                Location = new Point(171, 218),
                Sound = SoundList.ButtonA,
            };
            NextButton.Click += (o, e) =>
            {
                Page++;
                if (Page > ItemAll.Count() / ItemRows.Length) Page = ItemAll.Count() / ItemRows.Length;
                StartIndex = ItemRows.Length * Page;
                UpdatePage();
            };
            //4个分页按钮BaseButton ClassButton, ProtectButton, ItemButton
            BaseButton = new MirButton
            {
                Index = 1083,
                HoverIndex = 1081,
                PressedIndex = 1082,
                Library = Libraries.Prguse,
                Parent = this,
                Size = new Size(16, 50),
                Location = new Point(171, 218),
                Sound = SoundList.ButtonA,
            };
            BaseButton.Click += (o, e) =>
            {
                currTab=1;
                UpdateDisplay();
            };
            ClassButton = new MirButton
            {
                Index = 1083,
                HoverIndex = 1081,
                PressedIndex = 1082,
                Library = Libraries.Prguse,
                Parent = this,
                Size = new Size(16, 50),
                Location = new Point(171, 218),
                Sound = SoundList.ButtonA,
            };
            ClassButton.Click += (o, e) =>
            {
                currTab = 2;
                UpdateDisplay();
            };
            ProtectButton = new MirButton
            {
                Index = 1083,
                HoverIndex = 1081,
                PressedIndex = 1082,
                Library = Libraries.Prguse,
                Parent = this,
                Size = new Size(16, 50),
                Location = new Point(171, 218),
                Sound = SoundList.ButtonA,
            };
            ProtectButton.Click += (o, e) =>
            {
                currTab = 3;
                UpdateDisplay();
            };
            ItemButton = new MirButton
            {
                Index = 1083,
                HoverIndex = 1081,
                PressedIndex = 1082,
                Library = Libraries.Prguse,
                Parent = this,
                Size = new Size(16, 50),
                Location = new Point(171, 218),
                Sound = SoundList.ButtonA,
            };
            ItemButton.Click += (o, e) =>
            {
                currTab = 4;
                UpdateDisplay();
            };
            //各种单选框
            ShowLevelBox = new MirCheckBox { Location = new Point(16, 50), LabelText = "显示等级", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this};
            ShowLevelBox.Click += (o, e) =>
            {
                changeData();
            };
            ExcuseShiftBox = new MirCheckBox { Location = new Point(16, 50), LabelText = "免Shift", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            ExcuseShiftBox.Click += (o, e) =>
            {
                changeData();
            };
            ShowPingBox = new MirCheckBox { Location = new Point(16, 50), LabelText = "显示Ping", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            ShowPingBox.Click += (o, e) =>
            {
                changeData();
            };
            ShowFashionBox = new MirCheckBox { Location = new Point(16, 50), LabelText = "显示时装", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            ShowFashionBox.Click += (o, e) =>
            {
                changeData();
            };
            SeptumBox = new MirCheckBox { Location = new Point(16, 50), LabelText = "隔位刺杀", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            SeptumBox.Click += (o, e) =>
            {
                changeData();
            };
            AutoFlamingBox = new MirCheckBox { Location = new Point(16, 50), LabelText = "自动烈火", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            AutoFlamingBox.Click += (o, e) =>
            {
                changeData();
            };
            AutoShieldBox = new MirCheckBox { Location = new Point(16, 50), LabelText = "自动开盾", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            AutoShieldBox.Click += (o, e) =>
            {
                changeData();
            };
            OpenProtectBox = new MirCheckBox { Location = new Point(16, 50), LabelText = "开启保护", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            OpenProtectBox.Click += (o, e) =>
            {
                changeData();
            };
            AutoPickUpBox = new MirCheckBox { Location = new Point(16, 50), LabelText = "自动拾取", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            AutoPickUpBox.Click += (o, e) =>
            {
                changeData();
            };

            #endregion
        }
        //装载配置到窗口
        private void initData()
        {
            ShowLevelBox.Checked = GameScene.UserSet.ShowLevel;
            ExcuseShiftBox.Checked = GameScene.UserSet.ExcuseShift;
            ShowPingBox.Checked = GameScene.UserSet.ShowPing;
            ShowFashionBox.Checked = GameScene.UserSet.ShowFashion;
            SeptumBox.Checked = GameScene.UserSet.Septum;
            AutoFlamingBox.Checked = GameScene.UserSet.AutoFlaming;
            AutoShieldBox.Checked = GameScene.UserSet.AutoShield;
            OpenProtectBox.Checked = GameScene.UserSet.OpenProtect;
            AutoPickUpBox.Checked = GameScene.UserSet.AutoPickUp;

            HPLower1Text.Text = GameScene.UserSet.HPLower1+"";
            HPLower2Text.Text = GameScene.UserSet.HPLower2 + "";
            HPLower3Text.Text = GameScene.UserSet.HPLower3 + "";

            HPUse1Text.Text = GameScene.UserSet.HPUse1 + "";
            HPUse2Text.Text = GameScene.UserSet.HPUse2 + "";
            HPUse3Text.Text = GameScene.UserSet.HPUse3 + "";

            MPLower1Text.Text= GameScene.UserSet.MPLower1 + "";
            MPUse1Text.Text = GameScene.UserSet.MPUse1 + "";
        }
        //数据发送变更
        private void changeData()
        {
            GameScene.UserSet.ShowLevel = ShowLevelBox.Checked;
            GameScene.UserSet.ExcuseShift=ExcuseShiftBox.Checked ;
            GameScene.UserSet.ShowPing= ShowPingBox.Checked ;
            GameScene.UserSet.ShowFashion= ShowFashionBox.Checked;
            GameScene.UserSet.Septum = SeptumBox.Checked ;
            GameScene.UserSet.AutoFlaming = AutoFlamingBox.Checked ;
            GameScene.UserSet.AutoShield = AutoShieldBox.Checked ;
            GameScene.UserSet.OpenProtect = OpenProtectBox.Checked ;
            GameScene.UserSet.AutoPickUp = AutoPickUpBox.Checked ;
            byte.TryParse(HPLower1Text.Text,out GameScene.UserSet.HPLower1);
            byte.TryParse(HPLower2Text.Text, out GameScene.UserSet.HPLower2);
            byte.TryParse(HPLower3Text.Text, out GameScene.UserSet.HPLower3);
            byte.TryParse(MPUse1Text.Text, out GameScene.UserSet.MPLower1);
            GameScene.UserSet.HPUse1 = HPUse1Text.Text;
            GameScene.UserSet.HPUse2 = HPUse2Text.Text;
            GameScene.UserSet.HPUse3 = HPUse3Text.Text;
            GameScene.UserSet.MPUse1 = MPUse1Text.Text;
        }

        //刷新显示
        private void UpdateDisplay()
        {
            if (!Visible) return;
            //选项卡处理
            switch (currTab)
            {
                case 1:
                    TabVisible1(true);
                    TabVisible2(false);
                    TabVisible3(false);
                    TabVisible4(false);
                    break;
                case 2:
                    TabVisible1(false);
                    TabVisible2(true);
                    TabVisible3(false);
                    TabVisible4(false);
                    break;
                case 3:
                    TabVisible1(false);
                    TabVisible2(false);
                    TabVisible3(true);
                    TabVisible4(false);
                    break;
                case 4:
                    TabVisible1(false);
                    TabVisible2(false);
                    TabVisible3(false);
                    TabVisible4(true);
                    break;
            }
        }
        //刷新分页
        private void UpdatePage()
        {

        }


        //是否显示基础按钮
        private void TabVisible1(bool visible)
        {
            ShowLevelBox.Visible = visible;
            ExcuseShiftBox.Visible = visible;
            ShowPingBox.Visible = visible;
            ShowFashionBox.Visible = false;
            if (visible)
            {
                BaseButton.Index = tabPressedIndex;
            }
            else
            {
                BaseButton.Index = tabIndex;
            }
        }

        //是否显示基础按钮
        private void TabVisible2(bool visible)
        {
            SeptumBox.Visible = visible;
            AutoFlamingBox.Visible = visible;
            AutoShieldBox.Visible = visible;
            if (visible)
            {
                ClassButton.Index = tabPressedIndex;
            }
            else
            {
                ClassButton.Index = tabIndex;
            }
        }
        //保护
        private void TabVisible3(bool visible)
        {
            OpenProtectBox.Visible = visible;
            HPLower1Text.Visible = visible;
            HPLower2Text.Visible = visible;
            HPLower3Text.Visible = visible;
            HPUse1Text.Visible = visible;
            HPUse2Text.Visible = visible;
            HPUse3Text.Visible = visible;
            MPLower1Text.Visible = visible;
            MPUse1Text.Visible = visible;
            if (visible)
            {
                ProtectButton.Index = tabPressedIndex;
            }
            else
            {
                ProtectButton.Index = tabIndex;
            }
        }
        //物品
        private void TabVisible4(bool visible)
        {
            AutoPickUpBox.Visible = visible;
            PageNumberLabel.Visible = visible;
            for(int i=0;i< ItemRows.Length; i++)
            {
                ItemRows[i].Visible = visible;
            }
            if (visible)
            {
                ItemButton.Index = tabPressedIndex;
            }
            else
            {
                ItemButton.Index = tabIndex;
            }
        }




        public void Hide()
        {
            if (!Visible) return;
            Visible = false;
        }
        public void Show()
        {
            if (Visible) return;
            Visible = true;
            UpdateDisplay();
        }
    }
    
}
