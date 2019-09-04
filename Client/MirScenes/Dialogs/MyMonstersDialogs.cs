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
    //灵兽系统
    //契约兽系统
    //
    public sealed class MyMonstersDialogs : MirImageControl
    {
        //怪物名称
        public MirLabel MonsterName, MonsterLevel, MonsterLevelExp, MonsterSkill, MonsterSpiritual, MonsterAc, MonsterMac, MonsterDc,UpMonsterName;
        //关闭，怪物重命名按钮，召唤按钮，解雇，回收,赠送
        public MirButton CloseButton, MonstersRenameButton, SummonButton, DismissButton, ReleaseButton, GiveButton,viewButton;

        //标题栏
        public MirLabel NameLabel;
        Font font = new Font(Settings.FontName, 9F);

        public MirImageControl eatItemImage;



        //所有的宠物列表按钮(10个宠物)
        public MyMonstersButton[] MyMonstersButtons;
        //当前选择的宠物槽口
        public int SelectedMonsterSlot = -1;
        //选中的提示
        public MirControl HoverLabelParent = null;

        private MirAnimatedControl MonstersImage;//怪物的动画






        public MyMonstersDialogs()
        {
            Index = 41;
            Library = Libraries.MyUi;
            Movable = true;
            Sort = true;
            Location = Center;
            BeforeDraw += MyMonstersDialog_BeforeDraw;

            NameLabel = new MirLabel
            {
                Text = "契 约 兽",
                Parent = this,
                Font = new Font(Settings.FontName, 10F, FontStyle.Bold),
                ForeColour = Color.BurlyWood,
                Location = new Point(30, 6),
                AutoSize = true
            };

     

            #region CreatureButtons
            CloseButton = new MirButton
            {
                HoverIndex = 361,
                Index = 360,
                Location = new Point(Size.Width - 25, 3),
                Library = Libraries.Prguse2,
                Parent = this,
                PressedIndex = 362,
                Sound = SoundList.ButtonA,
            };
            CloseButton.Click += (o, e) => Hide();


            MonstersRenameButton = new MirButton
            {
                HoverIndex = 571,
                Index = 570,
                Location = new Point(360, 38),
                Library = Libraries.Title,
                Parent = this,
                PressedIndex = 572,
                Sound = SoundList.ButtonA,
                Visible = true,
            };
            MonstersRenameButton.Click += ButtonClick;
            //查看详细信息
            viewButton = new MirButton
            {
                HoverIndex = 782,
                Index = 781,
                Location = new Point(58, 170),
                Library = Libraries.Title,
                Parent = this,
                PressedIndex = 783,
                Sound = SoundList.ButtonA,
                Visible = true,
            };
            viewButton.Click += ButtonClick;
            

            SummonButton = new MirButton
            {
                Index = 576,
                HoverIndex = 577,
                PressedIndex = 578,
                Location = new Point(80, 220),
                Library = Libraries.Title,
                Parent = this,
                Sound = SoundList.ButtonA,
            };
            SummonButton.Click += ButtonClick;

            DismissButton = new MirButton//Dismiss the summoned pet
            {
                HoverIndex = 581,
                Index = 580,
                Location = new Point(84, 220),
                Library = Libraries.Title,
                Parent = this,
                PressedIndex = 582,
                Sound = SoundList.ButtonA,
                Visible = false,
            };
            DismissButton.Click += ButtonClick;

            ReleaseButton = new MirButton//Removes the selected pet
            {
                HoverIndex = 584,
                Index = 583,
                Location = new Point(184, 220),
                Library = Libraries.Title,
                Parent = this,
                PressedIndex = 585,
                Sound = SoundList.ButtonA,
            };
            ReleaseButton.Click += ButtonClick;


            GiveButton = new MirButton
            {
                HoverIndex = 608,
                Index = 607,
                Location = new Point(284, 220),
                Library = Libraries.Title,
                Parent = this,
                PressedIndex = 609,
                Sound = SoundList.ButtonA,
            };

            GiveButton.Click += ButtonClick;
            

            MyMonstersButtons = new MyMonstersButton[10];
            for (int i = 0; i < MyMonstersButtons.Length; i++)
            {
                int offsetX = i * 81;
                int offsetY = 259;
                if (i >= 5)
                {
                    offsetX = (i - 5) * 81;
                    offsetY += 40;
                }
                MyMonstersButtons[i] = new MyMonstersButton { idx = i, Parent = this, Visible = false, Location = new Point((44 + offsetX), offsetY) };
            }
            #endregion

            #region MonstersImage

            MonstersImage = new MirAnimatedControl
            {
                Animated = true,
                AnimationCount = 32,
                AnimationDelay = 300,
                Index = 0,
                Library = Libraries.Monsters[18],
                Loop = true,
                Parent = this,
                NotControl = false,
                UseOffSet = true,
                Location = new Point(45, 100),
            };
            MonstersImage.MouseDown += ImageOnMouseDown;



            #endregion

            #region Labels

            //进化
            UpMonsterName = new MirLabel
            {
                AutoSize = true,
                Parent = this,
                Location = new Point(18, 180),
                ForeColour = Color.DarkOrange,
                DrawFormat = TextFormatFlags.VerticalCenter,
                NotControl = true,
            };

            int left=220,top = 40;
            MonsterName = new MirLabel
            {
                AutoSize = true,
                Parent = this,
                Location = new Point(left, top),
                DrawFormat = TextFormatFlags.VerticalCenter ,
                NotControl = true,
            };
            top += 20;
            MonsterLevel = new MirLabel
            {
                AutoSize = true,
                Parent = this,
                Location = new Point(left, top),
                DrawFormat = TextFormatFlags.VerticalCenter ,
                NotControl = true,
            };
            top += 20;
            MonsterLevelExp = new MirLabel
            {
                AutoSize = true,
                Parent = this,
                Location = new Point(left, top),
                DrawFormat = TextFormatFlags.VerticalCenter,
                NotControl = true,
            };
            
            top += 20;
            MonsterSpiritual = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.DarkOrange,
                DrawFormat = TextFormatFlags.VerticalCenter ,
                AutoSize = true,
                NotControl = true,
            };
            top += 20;
            MonsterSkill = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.DarkOrange,
                DrawFormat = TextFormatFlags.VerticalCenter,
                Size = new Size(166, 21),
                NotControl = true,
            };
            

            top += 20;
            MonsterAc = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter ,
                Size = new Size(166, 21),
                NotControl = true,
            };
            top += 20;
            MonsterMac = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter ,
                Size = new Size(166, 21),
                NotControl = true,
            };
            top += 20;
            MonsterDc = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter ,
                Size = new Size(166, 21),
                NotControl = true,
            };


            #endregion

        }

        #region EventHandlers
        private void MyMonstersDialog_BeforeDraw(object sender, EventArgs e)
        {
            RefreshDialog();
        }

        //契约兽喂食事件
        private  void ImageOnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (GameScene.SelectedCell != null)
            {
                if (GameScene.SelectedCell.GridType != MirGridType.Inventory)
                {
                    GameScene.SelectedCell = null;
                    return;
                }
                MyMonster mon = GameScene.User.MyMonsters[SelectedMonsterSlot];
                if (mon == null)
                {
                    GameScene.Scene.ChatDialog.ReceiveChat("契约兽不存在!!", ChatType.System);
                    GameScene.SelectedCell = null;
                    return;
                }
                MirItemCell cell = GameScene.SelectedCell;
                //
                Network.Enqueue(new C.MyMonsterOperation { monidx = mon.idx, operation = 5, parameter1 = cell.Item.UniqueID+"" });
                //cell.Locked = true;
                GameScene.SelectedCell = null;
                return;
            }
        }



        private void ButtonClick(object sender, EventArgs e)
        {
            BeforeAfterDraw();

            //改名
            if (sender == MonstersRenameButton && SelectedMonsterSlot>=0)
            {
                MyMonster mon = GameScene.User.MyMonsters[SelectedMonsterSlot];
                if (mon == null)
                {
                    GameScene.Scene.ChatDialog.ReceiveChat("契约兽不存在!!", ChatType.System);
                    return;
                }
                MirInputBox inputBox = new MirInputBox("请输入该契约兽的新名称.名字中请勿输入数字下划线等特殊字符");
                inputBox.InputTextBox.Text = mon.getName();
                inputBox.OKButton.Click += (o1, e1) =>
                {
                    Update();//refresh changes
                    mon.rMonName = inputBox.InputTextBox.Text;
                    Network.Enqueue(new C.MyMonsterOperation { monidx = mon.idx, operation=1, parameter1= mon.rMonName });
                    inputBox.Dispose();
                };
                inputBox.Show();
                return;
            }


            //召唤
            if (sender == SummonButton && SelectedMonsterSlot >= 0)
            {
                MyMonster mon = GameScene.User.MyMonsters[SelectedMonsterSlot];
                if (mon == null)
                {
                    GameScene.Scene.ChatDialog.ReceiveChat("契约兽不存在!!", ChatType.System);
                    return;
                }
                Network.Enqueue(new C.MyMonsterOperation { monidx = mon.idx, operation = 2 });
                return;
            }

            //解雇
            if (sender == ReleaseButton && SelectedMonsterSlot >= 0)
            {
                MyMonster mon = GameScene.User.MyMonsters[SelectedMonsterSlot];
                if (mon == null)
                {
                    GameScene.Scene.ChatDialog.ReceiveChat("契约兽不存在!!", ChatType.System);
                    return;
                }
                MirInputBox verificationBox = new MirInputBox("解雇后契约兽将消失，请输入契约兽名称以进行验证.");
                verificationBox.OKButton.Click += (o1, e1) =>
                {
                    if (String.Compare(verificationBox.InputTextBox.Text, mon.getName(), StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        GameScene.Scene.ChatDialog.ReceiveChat("验证失败!!", ChatType.System);
                    }
                    else
                    {
                        //clear all and get new info after server got update
                        for (int i = 0; i < MyMonstersButtons.Length; i++) MyMonstersButtons[i].Clear();
                        Hide();
                        Network.Enqueue(new C.MyMonsterOperation { monidx = mon.idx, operation = 3 });
                    }
                    verificationBox.Dispose();
                };
                verificationBox.Show();
                return;
            }

            //赠送
            if (sender == GiveButton && SelectedMonsterSlot >= 0)
            {
                MyMonster mon = GameScene.User.MyMonsters[SelectedMonsterSlot];
                if (mon == null)
                {
                    GameScene.Scene.ChatDialog.ReceiveChat("契约兽不存在!!", ChatType.System);
                    return;
                }
                MirInputBox inputBox = new MirInputBox("发送操作为赠送契约兽给好友，请输入赠送玩家名称.");
                inputBox.InputTextBox.Text = "";
                inputBox.OKButton.Click += (o1, e1) =>
                {
                    string sendname = inputBox.InputTextBox.Text;
                    if(sendname==null|| sendname.Length < 2)
                    {
                        GameScene.Scene.ChatDialog.ReceiveChat("请输入赠送玩家名称!!", ChatType.System);
                        return;
                    }
                    Update();//refresh changes
                    Network.Enqueue(new C.MyMonsterOperation { monidx = mon.idx, operation = 4, parameter1 = sendname });

                    for (int i = 0; i < MyMonstersButtons.Length; i++) MyMonstersButtons[i].Clear();
                    Hide();

                    inputBox.Dispose();
                };
                inputBox.Show();
                return;
            }

            if (sender == viewButton && SelectedMonsterSlot >= 0)
            {
                MyMonster mon = GameScene.User.MyMonsters[SelectedMonsterSlot];
                if (mon == null)
                {
                    GameScene.Scene.ChatDialog.ReceiveChat("契约兽不存在!!", ChatType.System);
                    return;
                }
                GameScene.Scene.MyMonstersViewDialogs.SelectedMonsterSlot = SelectedMonsterSlot;
                if (!GameScene.Scene.MyMonstersViewDialogs.Visible)
                {
                    GameScene.Scene.MyMonstersViewDialogs.Show();
                }
                else {
                    GameScene.Scene.MyMonstersViewDialogs.Hide();
                }
            }
 
            

            Update();//refresh changes

        }

        #endregion

        #region Process
        public void Update()
        {
            if (!Visible) return;
            RefreshDialog();
        }
        public void RefreshDialog()
        {
            RefreshInfo();
            RefreshUI();
            BeforeAfterDraw();
        }
        private void RefreshInfo()
        {
            int SelectedButton = -1;

            for (int i = 0; i < MyMonstersButtons.Length; i++)
            {
                if (i >= GameScene.User.MyMonsters.Length)
                {
                    MyMonstersButtons[i].Clear();
                    continue;
                }
                if (GameScene.User.MyMonsters[i] == null)
                {
                    MyMonstersButtons[i].Clear();
                    continue;
                }

                MyMonstersButtons[i].Visible = true;
                MyMonstersButtons[i].Update(GameScene.User.MyMonsters[i]);
                if (SelectedMonsterSlot >= 0)
                {
                    if (SelectedMonsterSlot == i)
                    {
                        SelectedButton = i;
                    }
                }
                else
                {
                    SelectedButton = i;
                }
            }


            if (SelectedButton < 0) return;
            MyMonstersButtons[SelectedButton].SelectButton();


        }
        //刷新UI
        private void RefreshUI()
        {
            bool error = false;
            if (SelectedMonsterSlot < 0)
            {
                error = true;
            }
            //作废按钮
            DismissButton.Enabled = false;
            DismissButton.Visible = false;
            //召唤按钮
            SummonButton.Index = 576;
            SummonButton.HoverIndex = 577;
            SummonButton.PressedIndex = 578;
            SummonButton.Enabled = true;
            //回收
            ReleaseButton.Enabled = true;

            //显示怪物名称，等级等数据
            if (SelectedMonsterSlot < 0 || GameScene.User.MyMonsters[SelectedMonsterSlot]==null)
            {
                MonsterName.Text = "";
                MonsterLevel.Text = "";
                MonsterLevelExp.Text = "";
                MonsterSpiritual.Text = "";
                MonsterSkill.Text = "";
                MonsterAc.Text = "";
                MonsterMac.Text = "";
                MonsterDc.Text = "";
                return;
            }
            MyMonster mon = GameScene.User.MyMonsters[SelectedMonsterSlot];
            if (mon != null)
            {
                MonsterName.Text = "名称  " + mon.getName();

                MonsterLevel.Text = "等级  " + mon.MonLevel + "  体力 "+ mon.callTime;
                string Expstr = "";
                if (mon.maxExp > 10000)
                {
                    Expstr += mon.currExp / 10000 + "万/" + mon.maxExp / 10000 + "万";
                }
                else
                {
                    Expstr += mon.currExp + "/" + mon.maxExp + "";
                }
                Expstr += string.Format("({0:0.##%})", mon.currExp / (double)mon.maxExp);

                MonsterLevelExp.Text = "经验  " + Expstr;
                MonsterSpiritual.Text = "成长系数  防御 " + mon.AcUp + "   魔御 " + mon.MacUp + "   攻击 " + mon.DcUp+"";
                //MonsterLevelUp.Text = "成长 : " + mon.LevelUp;
                if (mon.skillname1 == null || mon.skillname1.Length < 2)
                {
                    MonsterSkill.Text = "领悟技能  " + "(未领悟)";
                }
                else
                {
                    if(mon.skillname2==null|| mon.skillname2.Length < 2)
                    {
                        MonsterSkill.Text = "领悟技能  " + mon.skillname1;
                    }
                    else
                    {
                        MonsterSkill.Text = "领悟技能  " + mon.skillname1 + " + " + mon.skillname2;
                    }
                }


                MonsterAc.Text = "防御 + " + mon.tMinAC + "~" + mon.tMaxAC;
                MonsterMac.Text = "魔御 + " + mon.tMinMAC + "~" + mon.tMaxMAC;
                MonsterDc.Text = "攻击 + " + mon.tMinDC + "~" + mon.tMaxDC;

                if(mon.UpMonName!=null && mon.UpMonName.Length > 0)
                {
                    UpMonsterName.Text = "进化为  "+ mon.UpMonName;
                }
                else
                {
                    UpMonsterName.Text = "";
                }
            }
            


        }
   

        public int BeforeAfterDraw()//No idea why.. but without this FullnessForeGround_AfterDraw wont work...
        {
            if (SelectedMonsterSlot >= 0 && GameScene.User.MyMonsters[SelectedMonsterSlot]!=null)
            {
                MonstersImage.Index = 0;
                MonstersImage.Animated = true;
                MonstersImage.Visible = true;
                MonstersImage.Library = Libraries.Monsters[GameScene.User.MyMonsters[SelectedMonsterSlot].MonImage];
            }
            else
            {
                MonstersImage.Index = 0;
                MonstersImage.Animated = false;
                MonstersImage.Library = Libraries.MyUi;
                MonstersImage.Visible = false;
            }

            return -1;
        }



        #endregion

        public void Hide()
        {
            if (!Visible) return;
            Visible = false;
        }
        public void Show()
        {
            if (Visible) return;

            if (!GameScene.User.MyMonsters.Any())
            {
                MirMessageBox messageBox = new MirMessageBox("您还没有契约兽.", MirMessageBoxButtons.OK);
                messageBox.Show();
                return;
            }

            if (!MyMonstersButtons.Any(x => x.Selected))
            {
                MyMonstersButtons[0].SelectButton();
            }


            Visible = true;
            RefreshDialog();
        }
    }


    public sealed class MyMonstersButton : MirControl
    {
        //宠物的小头像，统一用一个猪的吧
        public MirImageControl SelectionImage;
        //宠物的名字
        public MirLabel NameLabel;
        //宠物的按钮
        public MirButton PetButton;
        //位置0-9
        public int idx;
        //是否选择
        public bool Selected;



        public MyMonstersButton()
        {
            Size = new Size(231, 33);

            PetButton = new MirButton
            {
                Index = 502,
                PressedIndex = 502,
                Library = Libraries.Prguse2,
                Parent = this,
                Location = new Point(0, 0),
                Sound = SoundList.ButtonA,
            };
            PetButton.Click += PetButtonClick;
            PetButton.MouseEnter += PetButtonMouseEnter;
            PetButton.MouseLeave += PetButtonMouseLeave;

            SelectionImage = new MirImageControl
            {
                Index = 535,
                Library = Libraries.Prguse2,
                Location = new Point(-2, -2),
                Parent = this,
                NotControl = true,
                Visible = false,
            };

            NameLabel = new MirLabel
            {
                Parent = this,
                Location = new Point(-22, -12),
                DrawFormat = TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter,
                Size = new Size(80, 15),
                NotControl = true,
                Visible = false,
            };

        }

        private void SetButtonInfo(MyMonster pet)
        {
            if (pet == null) return;

            NameLabel.Text = pet.MonName.ToString();
        }

        public void Update(MyMonster pet)
        {
            if (pet == null) return;
            SetButtonInfo(pet);
        }

        void PetButtonClick(object sender, EventArgs e)
        {
            SelectButton();
        }
        void PetButtonMouseEnter(object sender, EventArgs e)
        {
            NameLabel.Visible = true;
        }
        void PetButtonMouseLeave(object sender, EventArgs e)
        {
            NameLabel.Visible = false;
        }

        public void SelectButton()
        {
            if (Selected) return;
            for (int i = 0; i < GameScene.Scene.MyMonstersDialogs.MyMonstersButtons.Length; i++)
            {
                if (i == idx) continue;
                GameScene.Scene.MyMonstersDialogs.MyMonstersButtons[i].SelectButton(false);
            }

            SelectButton(true);
            GameScene.Scene.MyMonstersDialogs.SelectedMonsterSlot = idx;
            GameScene.Scene.MyMonstersDialogs.Update();
        }
        private void SelectButton(bool selection)
        {
            Selected = selection;
            SelectionImage.Visible = Selected;
        }

        public void Clear()
        {
            Visible = false;
            SelectButton(false);
        }


    }


    public sealed class MyMonstersViewDialogs : MirImageControl
    {
        //标题栏
        public MirLabel NameLabel;
        Font font = new Font(Settings.FontName, 9F);

        public MirLabel HpLab, ACLab, MACLab, DCLab, AccuracyLab, AgilityLab;

        //契约兽本体属性
        public MirLabel sName, sHp, sAC, sMAC, sDC, sAccuracy, sAgility;

        //契约兽成长属性
        public MirLabel uName, uHp, uAC, uMAC, uDC, uAccuracy, uAgility;

        //契约兽最终属性
        public MirLabel tName, tHp, tAC, tMAC, tDC, tAccuracy, tAgility;

        public MirLabel memo,memo2;

        public MirButton CloseButton;

        //当前选择的宠物槽口
        public int SelectedMonsterSlot = -1;


        public MyMonstersViewDialogs()
        {
            Index = 43;
            Library = Libraries.MyUi;
            Movable = true;
            Sort = true;
            Location = Center;
            BeforeDraw += RefreshDialog;


            NameLabel = new MirLabel
            {
                Text = "契约兽属性",
                Parent = this,
                Font = new Font(Settings.FontName, 10F, FontStyle.Bold),
                ForeColour = Color.BurlyWood,
                Location = new Point(30, 6),
                AutoSize = true,
            };

            CloseButton = new MirButton
            {
                HoverIndex = 361,
                Index = 360,
                Location = new Point(Size.Width - 25, 3),
                Library = Libraries.Prguse2,
                Parent = this,
                PressedIndex = 362,
                Sound = SoundList.ButtonA,
            };
            CloseButton.Click += (o, e) => Hide();

            //前缀备注
            int left = 30, top = 70;
            HpLab = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text="血 量",
            };
            top += 25;
            ACLab = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
   
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "防 御",
            };
            top += 25;
            MACLab = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),

                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "魔 御",
            };
            top += 25;
            DCLab = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),

                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "攻 击",
            };
            top += 25;
            AccuracyLab = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),

                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "准 确",
            };
            top += 25;
            AgilityLab = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),

                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "敏 捷",
            };

            //本体
            left = 80; top = 45;
            sName = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "本体",
            };
            top += 25;
            sHp = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "血量",
            };
            top += 25;
            sAC = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "防御",
            };
            top += 25;
            sMAC = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "魔御",
            };
            top += 25;
            sDC = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "攻击",
            };
            top += 25;
            sAccuracy = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter,
                Size = new Size(120, 21),
                NotControl = true,
                Text = "准确",
            };
            top += 25;
            sAgility = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "敏捷",
            };

            //成长体
            left += 120; top = 45;
            uName = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "成长体",
            };
            top += 25;
            uHp = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "血量",
            };
            top += 25;
            uAC = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "防御",
            };
            top += 25;
            uMAC = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "魔御",
            };
            top += 25;
            uDC = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "攻击",
            };
            top += 25;
            uAccuracy = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "准确",
            };
            top += 25;
            uAgility = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.Cyan,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "敏捷",
            };

            //最终体
            left += 120; top = 45;
            tName = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "最终体",
            };
            top += 25;
            tHp = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.DarkOrange,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "血量",
            };
            top += 25;
            tAC = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.DarkOrange,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "防御",
            };
            top += 25;
            tMAC = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.DarkOrange,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "魔御",
            };
            top += 25;
            tDC = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.DarkOrange,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "攻击",
            };
            top += 25;
            tAccuracy = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.DarkOrange,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "准确",
            };
            top += 25;
            tAgility = new MirLabel
            {
                Parent = this,
                Location = new Point(left, top),
                ForeColour = Color.DarkOrange,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "敏捷",
            };

            memo = new MirLabel
            {
                Parent = this,
                Location = new Point(30, top + 70),
                ForeColour = Color.DarkOrange,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "最终属性 = ( 本体属性 + 成长属性 ) * 成长系数",
            };

            memo2 = new MirLabel
            {
                Parent = this,
                Location = new Point(30, top + 100),
                ForeColour = Color.DarkOrange,
                DrawFormat = TextFormatFlags.VerticalCenter,
                AutoSize = true,
                NotControl = true,
                Text = "召唤出来的属性比最终属性还高一些，因为有 1~7 级的宝宝属性",
            };

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
        }

        private void RefreshDialog(object sender, EventArgs e)
        {
            //SelectedMonsterSlot = GameScene.Scene.MyMonstersDialogs.SelectedMonsterSlot;
            MyMonster mon = GameScene.User.MyMonsters[SelectedMonsterSlot];
            if (mon == null)
            {
                //GameScene.Scene.ChatDialog.ReceiveChat("契约兽不存在!!", ChatType.System);
                return;
            }
            sName.Text = ""+mon.MonName+""+" 属性";
            if(mon.UpMonName!=null&& mon.UpMonName.Length > 1)
            {
                sName.Text = "" + mon.UpMonName + "" + " 属性";
            }
            uName.Text = "等级成长属性";
            tName.Text = "最终属性";

            sHp.Text = mon.sHP + "";
            uHp.Text = "+"+mon.uHP ;
            tHp.Text = "*"+ mon.getAllUp().ToString("f2") + " = " +mon.tHP ;

            sAC.Text = "" + mon.sMinAC + "~" + mon.sMaxAC;
            uAC.Text = "+" + mon.uMinAC + "~" + mon.uMaxAC;
            tAC.Text = "*"+ (mon.AcUp/10.0).ToString("f2") + " = "  + mon.tMinAC + "~" + mon.tMaxAC;

            sMAC.Text = "" + mon.sMinMAC + "~" + mon.sMaxMAC;
            uMAC.Text = "+" + mon.uMinMAC + "~" + mon.uMaxMAC;
            tMAC.Text = "*" + (mon.MacUp / 10.0).ToString("f2") + " = " + mon.tMinMAC + "~" + mon.tMaxMAC;

            sDC.Text = "" + mon.sMinDC + "~" + mon.sMaxDC;
            uDC.Text = "+" + mon.uMinDC + "~" + mon.uMaxDC;
            tDC.Text = "*" + (mon.DcUp / 10.0).ToString("f2") + " = " + mon.tMinDC + "~" + mon.tMaxDC;

            sAccuracy.Text = "" + mon.sAccuracy ;
            uAccuracy.Text = "+" + mon.uAccuracy ;
            tAccuracy.Text = "*" + mon.getAllUp().ToString("f2") + " = " + mon.tAccuracy ;


            sAgility.Text = "" + mon.sAgility;
            uAgility.Text = "+" + mon.uAgility;
            tAgility.Text = "*" + mon.getAllUp().ToString("f2") + " = " + mon.tAgility;

        }


    }

}
