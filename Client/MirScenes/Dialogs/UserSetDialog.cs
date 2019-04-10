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
        public MirCheckBox ShowLevelBox, ExcuseShiftBox, ShowPingBox, ShowFashionBox, ShowMonNameBox, ShowTargetDead, ShowMonCorpse;
        //职业
        public MirCheckBox SeptumBox, AutoFlamingBox, AutoShieldBox, switchPoisonBox, AutoHasteBox, AutoFuryBox;
        public MirLabel tx_df;//提醒自动毒符
        //保护
        public MirCheckBox OpenProtectBox;
      

        //保护的设置,输入框
        public MirTextBox HPLower1Text, HPLower2Text, HPLower3Text, HPUse1Text, HPUse2Text, HPUse3Text, MPLower1Text, MPUse1Text;
        //文字框
        public MirLabel HP11,HP12,HP21,HP22,HP31,HP32,MP11,MP12;
        //物品分页
        public MirCheckBox AutoPickUpBox;
        public MirCheckBox[] ItemRows;//这个是显示所有的物品的，要做分页处理
        public List<PickItem> ItemAll;//所有物品
        public MirLabel PageNumberLabel;

        public int StartIndex = 0;
        public int Page = 0;

        //固定参数
        private static int tabIndex= 385,tabPressedIndex = 384, tabHoverIndex = 383;

        public UserSetDialog()
        {
            Index = 311;
            Library = Libraries.Prguse;
            Movable = false;
            Sort = true;
            Location = Center;
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
            CloseButton.Click += (o, e) => Hide();
            PreviousButton = new MirButton
            {
                Index = 240,
                HoverIndex = 241,
                PressedIndex = 242,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(16, 16),
                Location = new Point(160, 198),
                Sound = SoundList.ButtonA,
                Hint="上一页",
            };
            PreviousButton.Click += (o, e) =>
            {
                Page--;
                if (Page < 0) Page = 0;
                StartIndex = ItemRows.Length * Page;
                UpdatePage();
            };
            PageNumberLabel = new MirLabel
            {
                Text = "",
                Parent = this,
                Size = new Size(60, 17),
                Location = new Point(180, 195),
                DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
            };
            NextButton = new MirButton
            {
                Index = 243,
                HoverIndex = 244,
                PressedIndex = 245,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(16, 16),
                Location = new Point(240, 198),
                Sound = SoundList.ButtonA,
                Hint = "下一页",
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
                Index = tabIndex,
                HoverIndex = tabHoverIndex,
                PressedIndex = tabPressedIndex,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(25, 84),
                Location = new Point(left, 33),
                Sound = SoundList.ButtonA,
                Text = "基础",
                CenterText = true,
            };

            BaseButton.Click += (o, e) =>
            {
                currTab=1;
                UpdateDisplay();
            };
            left += 80;
            ClassButton = new MirButton
            {
                Index = tabIndex,
                HoverIndex = tabHoverIndex,
                PressedIndex = tabPressedIndex,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(25, 84),
                Location = new Point(left, 33),
                Sound = SoundList.ButtonA,
                Text = "职业",
                CenterText = true,
            };
            ClassButton.Click += (o, e) =>
            {
                currTab = 2;
                UpdateDisplay();
            };
            left += 80;
            ProtectButton = new MirButton
            {
                Index = tabIndex,
                HoverIndex = tabHoverIndex,
                PressedIndex = tabPressedIndex,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(25, 84),
                Location = new Point(left, 33),
                Sound = SoundList.ButtonA,
                Text = "保护",
                CenterText = true,
            };
            ProtectButton.Click += (o, e) =>
            {
                currTab = 3;
                UpdateDisplay();
            };
            left += 80;
            ItemButton = new MirButton
            {
                Index = tabIndex,
                HoverIndex = tabHoverIndex,
                PressedIndex = tabPressedIndex,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(25, 84),
                Location = new Point(left, 33),
                Sound = SoundList.ButtonA,
                Text="物品",
                CenterText = true,
            };
            ItemButton.Click += (o, e) =>
            {
                currTab = 4;
                UpdateDisplay();
            };
            //1.基础
            left = 50;
            top = 70;
            ShowLevelBox = new MirCheckBox { Location = new Point(left, top), LabelText = "显示等级", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this};
            ShowLevelBox.Click += (o, e) =>
            {
                changeData();
            };
            top += 30;
            ExcuseShiftBox = new MirCheckBox { Location = new Point(left, top), LabelText = "免Shift", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            ExcuseShiftBox.Click += (o, e) =>
            {
                changeData();
            };
            top += 30;
            ShowPingBox = new MirCheckBox { Location = new Point(left, top), LabelText = "显示Ping", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            ShowPingBox.Click += (o, e) =>
            {
                changeData();
            };
            top += 30;
            ShowFashionBox = new MirCheckBox { Location = new Point(left, top), LabelText = "显示时装", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            ShowFashionBox.Click += (o, e) =>
            {
                changeData();
            };
            //第二列
            top = 70;
            left += 80;
            ShowMonNameBox = new MirCheckBox { Location = new Point(left, top), LabelText = "怪物显名", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            ShowMonNameBox.Click += (o, e) =>
            {
                changeData();
            };
            top += 30;
            ShowMonCorpse = new MirCheckBox { Location = new Point(left, top), LabelText = "显示尸体", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            ShowMonCorpse.Click += (o, e) =>
            {
                changeData();
            };
            top += 30;
            ShowTargetDead = new MirCheckBox { Location = new Point(left, top), LabelText = "尸体可点", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            ShowTargetDead.Click += (o, e) =>
            {
                changeData();
            };
      

            

            //2.职业
            left = 50;
            top = 70;
            SeptumBox = new MirCheckBox { Location = new Point(left, top), LabelText = "隔位刺杀", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            SeptumBox.Click += (o, e) =>
            {
                changeData();
            };
            top += 30;
            AutoFlamingBox = new MirCheckBox { Location = new Point(left, top), LabelText = "自动烈火", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            AutoFlamingBox.Click += (o, e) =>
            {
                changeData();
            };
            top += 30;
            AutoShieldBox = new MirCheckBox { Location = new Point(left, top), LabelText = "自动开盾", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            AutoShieldBox.Click += (o, e) =>
            {
                changeData();
            };
            top += 30;
            tx_df = new MirLabel
            {
                Text = "符/毒放背包中，自动使用,如关闭自动换毒,则只使用装备的毒",
                Parent = this,
                Size = new Size(400, 17),
                Location = new Point(left, top),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            };
            //第二列
            top = 70;
            left += 80;
            switchPoisonBox = new MirCheckBox { Location = new Point(left, top), LabelText = "自动换毒", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            switchPoisonBox.Click += (o, e) =>
            {
                changeData();
            };
            //自动体迅
            top += 30;
            AutoHasteBox = new MirCheckBox { Location = new Point(left, top), LabelText = "自动体迅", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            AutoHasteBox.Click += (o, e) =>
            {
                changeData();
            };
            //自动血龙
            top += 30;
            AutoFuryBox = new MirCheckBox { Location = new Point(left, top), LabelText = "自动血龙", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            AutoFuryBox.Click += (o, e) =>
            {
                changeData();
            };

            
            //4.物品
            left = 50;
            top = 70;
            AutoPickUpBox = new MirCheckBox { Location = new Point(left, top), LabelText = "自动拾取", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            AutoPickUpBox.Click += (o, e) =>
            {
                changeData();
            };
            //捡取的物品列表4行，3列

            ItemRows = new MirCheckBox[12];
            for(int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    ItemRows[r*3+c] = new MirCheckBox { Location = new Point(30+c*130, 100+r*23), LabelText = "", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this,Visible=true };
                    ItemRows[r * 3 + c].Click += (o, e) =>
                    {
                        changeData();
                    };
                }
            }

            //3.保护配置
            left = 50;
            top = 70;
            OpenProtectBox = new MirCheckBox { Location = new Point(left, top), LabelText = "开启保护", Library = Libraries.Prguse, Index = 2086, UnTickedIndex = 2086, TickedIndex = 2087, Parent = this };
            OpenProtectBox.Click += (o, e) =>
            {
                changeData();
            };
            left = 205; top = 93;
            HPLower1Text = new MirTextBox
            {
                BackColour = Color.Green,
                ForeColour = Color.White,
                Parent = this,
                Size = new Size(40, 17),
                Location = new Point(left, top),
                Font = new Font(Settings.FontName, 9F),
                MaxLength = 2,
                CanLoseFocus = true,
            };
            
            top += 23;
            HPLower2Text = new MirTextBox
            {
                BackColour = Color.Green,
                ForeColour = Color.White,
                Parent = this,
                Size = new Size(40, 17),
                Location = new Point(left, top),
                Font = new Font(Settings.FontName, 9F),
                MaxLength = 2,
                CanLoseFocus = true,
            };
            top += 23;
            HPLower3Text = new MirTextBox
            {
                BackColour = Color.Green,
                ForeColour = Color.White,
                Parent = this,
                Size = new Size(40, 17),
                Location = new Point(left, top),
                Font = new Font(Settings.FontName, 9F),
                MaxLength = 2,
                CanLoseFocus = true,
            };
            top += 23;
            MPLower1Text = new MirTextBox
            {
                BackColour = Color.Green,
                ForeColour = Color.White,
                Parent = this,
                Size = new Size(40, 17),
                Location = new Point(left, top),
                Font = new Font(Settings.FontName, 9F),
                MaxLength = 2,
                CanLoseFocus = true,
            };
            left = 285; top = 93;
            HPUse1Text = new MirTextBox
            {
                BackColour = Color.Green,
                ForeColour = Color.White,
                Parent = this,
                Size = new Size(100, 17),
                Location = new Point(left, top),
                Font = new Font(Settings.FontName, 9F),
                MaxLength = 20,
                CanLoseFocus = true,
            };
            top += 23;
            HPUse2Text = new MirTextBox
            {
                BackColour = Color.Green,
                ForeColour = Color.White,
                Parent = this,
                Size = new Size(100, 17),
                Location = new Point(left, top),
                Font = new Font(Settings.FontName, 9F),
                MaxLength = 20,
                CanLoseFocus = true,
            };
            top += 23;
            HPUse3Text = new MirTextBox
            {
                BackColour = Color.Green,
                ForeColour = Color.White,
                Parent = this,
                Size = new Size(100, 17),
                Location = new Point(left, top),
                Font = new Font(Settings.FontName, 9F),
                MaxLength = 20,
                CanLoseFocus = true,
            };
            top += 23;
            MPUse1Text = new MirTextBox
            {
                BackColour = Color.Green,
                ForeColour = Color.White,
                Parent = this,
                Size = new Size(100, 17),
                Location = new Point(left, top),
                Font = new Font(Settings.FontName, 9F),
                MaxLength = 20,
                CanLoseFocus = true,
            };

            //各种文字说明
            left = 50;top = 90;
            HP11 = new MirLabel
            {
                Text = "普通喝药，生命百分比低于",
                Parent = this,
                Size = new Size(150, 17),
                Location = new Point(left, top),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            };
            top += 23;
            HP21 = new MirLabel
            {
                Text = "快速喝药，生命百分比低于",
                Parent = this,
                Size = new Size(150, 17),
                Location = new Point(left, top),
                DrawFormat =  TextFormatFlags.VerticalCenter
            };
            top += 23;
            HP31 = new MirLabel
            {
                Text = "使用卷轴，生命百分比低于",
                Parent = this,
                Size = new Size(150, 17),
                Location = new Point(left, top),
                DrawFormat =  TextFormatFlags.VerticalCenter
            };
            top += 23;
            MP11 = new MirLabel
            {
                Text = "普通喝蓝，魔法百分比低于",
                Parent = this,
                Size = new Size(150, 17),
                Location = new Point(left, top),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            };
            

            left = 250; top = 90;
            HP12 = new MirLabel
            {
                Text = "使用",
                Parent = this,
                Size = new Size(30, 17),
                Location = new Point(left, top),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            };
            top += 23;
            HP22 = new MirLabel
            {
                Text = "使用",
                Parent = this,
                Size = new Size(30, 17),
                Location = new Point(left, top),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            };
            top += 23;
            HP32 = new MirLabel
            {
                Text = "使用",
                Parent = this,
                Size = new Size(30, 17),
                Location = new Point(left, top),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            };
            top += 23;
            MP12 = new MirLabel
            {
                Text = "使用",
                Parent = this,
                Size = new Size(30, 17),
                Location = new Point(left, top),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            };
            #endregion


        }
        //装载配置到窗口
        public void initData()
        {
            ShowLevelBox.Checked = GameScene.UserSet.ShowLevel;
            ExcuseShiftBox.Checked = GameScene.UserSet.ExcuseShift;
            ShowPingBox.Checked = GameScene.UserSet.ShowPing;
            ShowFashionBox.Checked = GameScene.UserSet.ShowFashion;
            ShowMonNameBox.Checked = GameScene.UserSet.ShowMonName;
            ShowTargetDead.Checked = Settings.TargetDead;
            ShowMonCorpse.Checked = GameScene.UserSet.ShowMonCorpse;
            SeptumBox.Checked = GameScene.UserSet.Septum;
            AutoFlamingBox.Checked = GameScene.UserSet.AutoFlaming;
            AutoShieldBox.Checked = GameScene.UserSet.AutoShield;

            AutoHasteBox.Checked = GameScene.UserSet.AutoHaste;
            AutoFuryBox.Checked = GameScene.UserSet.AutoFury;
            
            OpenProtectBox.Checked = GameScene.UserSet.OpenProtect;
            AutoPickUpBox.Checked = GameScene.UserSet.AutoPickUp;
            switchPoisonBox.Checked = GameScene.UserSet.switchPoison;

            HPLower1Text.Text = GameScene.UserSet.HPLower1+"";
            HPLower2Text.Text = GameScene.UserSet.HPLower2 + "";
            HPLower3Text.Text = GameScene.UserSet.HPLower3 + "";

            HPUse1Text.Text = GameScene.UserSet.HPUse1 + "";
            HPUse2Text.Text = GameScene.UserSet.HPUse2 + "";
            HPUse3Text.Text = GameScene.UserSet.HPUse3 + "";

            MPLower1Text.Text= GameScene.UserSet.MPLower1 + "";
            MPUse1Text.Text = GameScene.UserSet.MPUse1 + "";

            //初始物品数据
            ItemAll = GameScene.UserSet.PickUpList;
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

            GameScene.UserSet.AutoHaste = AutoHasteBox.Checked;
            GameScene.UserSet.AutoFury = AutoFuryBox.Checked;
            GameScene.UserSet.OpenProtect = OpenProtectBox.Checked ;
            GameScene.UserSet.AutoPickUp = AutoPickUpBox.Checked ;
            GameScene.UserSet.ShowMonName = ShowMonNameBox.Checked;
            GameScene.UserSet.switchPoison = switchPoisonBox.Checked;

            Settings.TargetDead = ShowTargetDead.Checked;
            GameScene.UserSet.ShowMonCorpse = ShowMonCorpse.Checked;

            byte.TryParse(HPLower1Text.Text,out GameScene.UserSet.HPLower1);
            byte.TryParse(HPLower2Text.Text, out GameScene.UserSet.HPLower2);
            byte.TryParse(HPLower3Text.Text, out GameScene.UserSet.HPLower3);
            byte.TryParse(MPLower1Text.Text, out GameScene.UserSet.MPLower1);
            GameScene.UserSet.HPUse1 = HPUse1Text.Text;
            GameScene.UserSet.HPUse2 = HPUse2Text.Text;
            GameScene.UserSet.HPUse3 = HPUse3Text.Text;
            GameScene.UserSet.MPUse1 = MPUse1Text.Text;
            //物品选择
            for (int i = StartIndex; i < StartIndex + 12 && i < ItemAll.Count; i++)
            {
                ItemAll[i].pick=ItemRows[i - StartIndex].Checked;
            }
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
                default:
                    TabVisible1(true);
                    break;
            }
            UpdatePage();
        }
        //刷新分页
        private void UpdatePage()
        {
            for (int i = 0; i < ItemRows.Length; i++)
            {
                ItemRows[i].Visible = false;
            }
            int maxPage = ItemAll.Count / ItemRows.Length + 1;
            if (maxPage < 1) maxPage = 1;

            PageNumberLabel.Text = (Page + 1) + " / " + maxPage;

            for (int i= StartIndex;i< StartIndex+12 && i < ItemAll.Count; i++)
            {
                ItemRows[i - StartIndex].LabelText = ItemAll[i].itemname;
                ItemRows[i - StartIndex].Checked = ItemAll[i].pick;
                ItemRows[i - StartIndex].Visible = NextButton.Visible;
            }
        }


        //是否显示基础按钮
        private void TabVisible1(bool visible)
        {
            ShowLevelBox.Visible = visible;
            ExcuseShiftBox.Visible = visible;
            ShowPingBox.Visible = visible;
            ShowMonNameBox.Visible = visible;
            ShowTargetDead.Visible = visible;
            ShowMonCorpse.Visible = visible;
            ShowFashionBox.Visible = visible;
            if (visible)
            {
                BaseButton.Index = tabPressedIndex;
            }
            else
            {
                BaseButton.Index = tabIndex;
            }
        }

        //是否职业按钮
        private void TabVisible2(bool visible)
        {
            SeptumBox.Visible = visible;
            AutoFlamingBox.Visible = visible;
            AutoShieldBox.Visible = visible;
            AutoHasteBox.Visible = visible;
            AutoFuryBox.Visible = visible;
            switchPoisonBox.Visible = visible;
            tx_df.Visible = visible;
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
            //HP11,HP12,HP21,HP22,HP31,HP32,MP11,MP12
            HP11.Visible = visible;
            HP12.Visible = visible;
            HP21.Visible = visible;
            HP22.Visible = visible;
            HP31.Visible = visible;
            HP32.Visible = visible;
            MP11.Visible = visible;
            MP12.Visible = visible;
         
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
            PreviousButton.Visible = visible;
            NextButton.Visible = visible;
            for (int i=0;i< ItemRows.Length; i++)
            {
                if(ItemRows[i].LabelText != null && ItemRows[i].LabelText.Length > 0)
                {
                    ItemRows[i].Visible = visible;
                }
                else
                {
                    ItemRows[i].Visible=false;
                }
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
            //关闭保存
            changeData();
            GameScene.UserSet.Save();
            Visible = false;
        }
        public void Show()
        {
            if (Visible) return;
            Visible = true;
            initData();
            UpdateDisplay();
        }
    }
    
}
