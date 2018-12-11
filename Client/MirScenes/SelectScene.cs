using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirSounds;
using C = ClientPackets;
using S = ServerPackets;
using System.Threading;
using Client.MirScenes.Dialogs;

namespace Client.MirScenes
{
    public class SelectScene : MirScene
    {
        public MirImageControl Background, Title;
        private NewCharacterDialog _character;

        public MirLabel ServerLabel, GoldLabel, CreditLabel;
        public MirAnimatedControl CharacterDisplay;
        public MirButton StartGameButton, NewCharacterButton, DeleteCharacterButton, CreditsButton, ExitGame,RechargeButton;
        public CharacterButton[] CharacterButtons;
        public MirLabel LastAccessLabel, LastAccessLabelLabel;
        public List<SelectInfo> Characters = new List<SelectInfo>();
        public uint Gold;//金币,金币是账号上的金币，多角色共享
        public uint Credit;//积分，信用,也可称作元宝
        private int _selected;
        //充值
        public MirMessageBox RechargeBox;
        public RechargeDialog RechargeDialog;
        public PayForm payForm;
        //最后支付时间，没有成功前，不允许重复触发支付，间隔30秒
        private long lastPayTime = 0;

        public SelectScene(List<SelectInfo> characters)
        {
            SoundManager.PlaySound(SoundList.SelectMusic, true);
            Disposing += (o, e) => SoundManager.StopSound(SoundList.SelectMusic);


            Characters = characters;
            SortList();

            KeyPress += SelectScene_KeyPress;

            Background = new MirImageControl
            {
                Index = 64,
                Library = Libraries.Prguse,
                Parent = this,
            };

            Title = new MirImageControl
            {
                Index = 40,
                Library = Libraries.Title,
                Parent = this,
                Location = new Point(364, 12)
            };

            ServerLabel = new MirLabel
            {
                Location = new Point(322, 44),
                Parent = Background,
                Size = new Size(155, 17),
                Text = Settings.serverName==null?"Legend of Mir 2": Settings.serverName,
                DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
            };
            //金币
            GoldLabel = new MirLabel
            {
                Location = new Point(8, 48),
                Parent = Background,
                Size = new Size(175, 17),
                Text = Gold.ToString("账号共享金币:###,###,##0"),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            };
            //元宝
            CreditLabel = new MirLabel
            {
                Location = new Point(8, 68),
                Parent = Background,
                Size = new Size(175, 17),
                Text = Credit.ToString("账号共享元宝:###,###,##0"),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            };
            //充值按钮
            RechargeButton = new MirButton
            {
                Enabled = true,
                HoverIndex = 383,
                Index = 385,
                PressedIndex = 384,
                Size = new Size(25, 84),
                Library = Libraries.Prguse2,
                Location = new Point(8, 92),
                Sound = SoundList.ButtonA,
                Parent = Background,
                Text = "账号充值",
                CenterText = true,
                GrayScale = true
            };
            RechargeButton.Click += (o, e) => {
                
                if (payForm != null)
                {
                    payForm.Close();
                }
                if (RechargeBox != null)
                {
                    RechargeBox.Dispose();
                }
                if (CMain.Time < lastPayTime)
                {
                    //创建支付
                    if (RechargeBox == null || RechargeBox.IsDisposed)
                    {
                        RechargeBox = new MirMessageBox(string.Format("为避免重复充值，请在{0}秒后再进行充值操作.", ( lastPayTime- CMain.Time) /1000), MirMessageBoxButtons.OK);
                    }
                    RechargeBox.Visible = true;
                    RechargeBox.Show();
                    return;
                }
                
                if (RechargeDialog == null|| RechargeDialog.IsDisposed)
                {
                    RechargeDialog = new RechargeDialog() { Parent = this };
                }
                RechargeDialog.Visible = true;
                
            };


            StartGameButton = new MirButton
            {
                Enabled = false,
                HoverIndex = 341,
                Index = 340,
                Library = Libraries.Title,
                Location = new Point(110, 568),
                Parent = Background,
                PressedIndex = 342,
                GrayScale = true
            };
            StartGameButton.Click += (o, e) => StartGame();

            NewCharacterButton = new MirButton
            {
                HoverIndex = 344,
                Index = 343,
                Library = Libraries.Title,
                Location = new Point(230, 568),
                Parent = Background,
                PressedIndex = 345,
            };
            NewCharacterButton.Click += (o, e) => _character = new NewCharacterDialog { Parent = this };

            DeleteCharacterButton = new MirButton
            {
                HoverIndex = 347,
                Index = 346,
                Library = Libraries.Title,
                Location = new Point(350, 568),
                Parent = Background,
                PressedIndex = 348
            };
            DeleteCharacterButton.Click += (o, e) => DeleteCharacter();


            CreditsButton = new MirButton
            {
                HoverIndex = 350,
                Index = 349,
                Library = Libraries.Title,
                Location = new Point(470, 568),
                Parent = Background,
                PressedIndex = 351
            };

            ExitGame = new MirButton
            {
                HoverIndex = 353,
                Index = 352,
                Library = Libraries.Title,
                Location = new Point(590, 568),
                Parent = Background,
                PressedIndex = 354
            };
            ExitGame.Click += (o, e) => Program.Form.Close();


            CharacterDisplay = new MirAnimatedControl
            {
                Animated = true,
                AnimationCount = 16,
                AnimationDelay = 250,
                FadeIn = true,
                FadeInDelay = 75,
                FadeInRate = 0.1F,
                Index = 220,
                Library = Libraries.ChrSel,
                Location = new Point(200, 300),
                Parent = Background,
                UseOffSet = true,
                Visible = false
            };
            CharacterDisplay.AfterDraw += (o, e) =>
            {
                // if (_selected >= 0 && _selected < Characters.Count && characters[_selected].Class == MirClass.Wizard)
                Libraries.ChrSel.DrawBlend(CharacterDisplay.Index + 560, CharacterDisplay.DisplayLocationWithoutOffSet, Color.White, true);
            };

            CharacterButtons = new CharacterButton[4];

            CharacterButtons[0] = new CharacterButton
            {
                Location = new Point(447, 122),
                Parent = Background,
                Sound = SoundList.ButtonA,
            };
            CharacterButtons[0].Click += (o, e) =>
            {
                if (characters.Count <= 0) return;

                _selected = 0;
                UpdateInterface();
            };

            CharacterButtons[1] = new CharacterButton
            {
                Location = new Point(447, 226),
                Parent = Background,
                Sound = SoundList.ButtonA,
            };
            CharacterButtons[1].Click += (o, e) =>
            {
                if (characters.Count <= 1) return;
                _selected = 1;
                UpdateInterface();
            };

            CharacterButtons[2] = new CharacterButton
            {
                Location = new Point(447, 330),
                Parent = Background,
                Sound = SoundList.ButtonA,
            };
            CharacterButtons[2].Click += (o, e) =>
            {
                if (characters.Count <= 2) return;
                _selected = 2;
                UpdateInterface();
            };

            CharacterButtons[3] = new CharacterButton
            {
                Location = new Point(447, 434),
                Parent = Background,
                Sound = SoundList.ButtonA,
            };
            CharacterButtons[3].Click += (o, e) =>
            {
                if (characters.Count <= 3) return;

                _selected = 3;
                UpdateInterface();
            };

            LastAccessLabel = new MirLabel
            {
                Location = new Point(140, 509),
                Parent = Background,
                Size = new Size(180, 21),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter,
                Border = true,
            };
            LastAccessLabelLabel = new MirLabel
            {
                Location = new Point(-80, -1),
                Parent = LastAccessLabel,
                Text = "最后在线:",
                Size = new Size(100, 21),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter,
                Border = true,
            };
            RechargeBox = new MirMessageBox("正在创建支付订单二维码，请稍后.", MirMessageBoxButtons.OK);
            UpdateInterface();

            Network.Enqueue(new C.RefreshUserGold());
        }

        private void SelectScene_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != (char)Keys.Enter) return;
            if (StartGameButton.Enabled)
                StartGame();
            e.Handled = true;
        }


        public void SortList()
        {
            if (Characters != null)
                Characters.Sort((c1, c2) => c2.LastAccess.CompareTo(c1.LastAccess));
        }

        //开始游戏
        public void StartGame()
        {
            if (!Libraries.Loaded)
            {
                MirMessageBox message = new MirMessageBox(LanguageUtils.Format("Please wait, The game is still loading... {0:##0}%", Libraries.Progress / (double)Libraries.Count * 100), MirMessageBoxButtons.Cancel);

                message.BeforeDraw += (o, e) => message.Label.Text = LanguageUtils.Format("Please wait, The game is still loading... {0:##0}%", Libraries.Progress / (double)Libraries.Count * 100);

                message.AfterDraw += (o, e) =>
                {
                    if (!Libraries.Loaded) return;
                    message.Dispose();
                    StartGame();
                };

                message.Show();

                return;
            }
            StartGameButton.Enabled = false;

            Network.Enqueue(new C.StartGame
            {
                CharacterIndex = Characters[_selected].Index
            });
        }

        public override void Process()
        {
            GoldLabel.Text = Gold.ToString("账号共享金币:###,###,##0");
            CreditLabel.Text = Credit.ToString("账号共享元宝:###,###,##0");
            if (RechargeDialog.RechargeState == 1)//正在创建
            {
                RechargeDialog.RechargeState = 0;
                //创建支付
                if (RechargeBox==null || RechargeBox.IsDisposed)
                {
                    RechargeBox = new MirMessageBox("正在创建支付订单二维码，请稍后.", MirMessageBoxButtons.OK);
                }
                RechargeBox.Label.Text = "正在创建支付订单二维码，请稍后.";
                RechargeBox.Visible = true;
                RechargeBox.Show();
                
            }
           
        }
        public override void ProcessPacket(Packet p)
        {
            switch (p.Index)
            {
                case (short)ServerPacketIds.NewCharacter:
                    NewCharacter((S.NewCharacter)p);
                    break;
                case (short)ServerPacketIds.NewCharacterSuccess:
                    NewCharacter((S.NewCharacterSuccess)p);
                    break;
                case (short)ServerPacketIds.DeleteCharacter:
                    DeleteCharacter((S.DeleteCharacter)p);
                    break;
                case (short)ServerPacketIds.DeleteCharacterSuccess:
                    DeleteCharacter((S.DeleteCharacterSuccess)p);
                    break;
                case (short)ServerPacketIds.StartGame:
                    StartGame((S.StartGame)p);
                    break;
                case (short)ServerPacketIds.StartGameBanned:
                    StartGame((S.StartGameBanned)p);
                    break;
                case (short)ServerPacketIds.StartGameDelay:
                    StartGame((S.StartGameDelay)p);
                    break;
                case (short)ServerPacketIds.UserGold://刷新金币
                    Gold = ((S.UserGold)p).Gold;
                    Credit = ((S.UserGold)p).Credit;
                    break;
                case (short)ServerPacketIds.RechargeLink://返回支付链接
                    lastPayTime = CMain.Time + 30 * 1000;
                    if (RechargeBox != null)
                    {
                        RechargeBox.Visible = false;
                        RechargeBox.Dispose();
                    }
                    if (RechargeDialog != null)
                    {
                        RechargeDialog.Dispose();
                    }
                    if (payForm != null)
                    {
                        payForm.Close();
                        payForm = null;
                    }
                    S.RechargeLink rl = (S.RechargeLink)p;
                    if (payForm == null|| payForm.IsDisposed)
                    {
                        payForm = new PayForm() {oid=rl.orderid,payType=rl.payType,money=rl.money, payurl=rl.ret_Link, query_Link=rl.query_Link };
                        payForm.Show();
                    }
                    break;
                case (short)ServerPacketIds.RechargeResult://返回支付成功
                    lastPayTime = 0;
                    if (payForm != null)
                    {
                        payForm.Close();
                    }
                    if (RechargeDialog != null)
                    {
                        RechargeDialog.Dispose();
                    }

                    if (RechargeBox == null || RechargeBox.IsDisposed)
                    {
                        RechargeBox = new MirMessageBox("充值完成，你的元宝已到账.", MirMessageBoxButtons.OK);
                    }
                    RechargeBox.Visible = true;
                    RechargeBox.Label.Text = "充值成功，已到账元宝数："+ ((S.RechargeResult)p).addCredit;
                    RechargeBox.Show();
                    break;
                default:
                    base.ProcessPacket(p);
                    break;
            }
        }

        private void NewCharacter(S.NewCharacter p)
        {
            _character.OKButton.Enabled = true;

            switch (p.Result)
            {
                case 0:
                    MirMessageBox.Show(LanguageUtils.Format("Creating new characters is currently disabled."));
                    _character.Dispose();
                    break;
                case 1:
                    MirMessageBox.Show("你的角色名字是不可接受的.");
                    _character.NameTextBox.SetFocus();
                    break;
                case 2:
                    MirMessageBox.Show("您选择的性别不存在.");
                    break;
                case 3:
                    MirMessageBox.Show("您选择的职业不存在.");
                    break;
                case 4:
                    MirMessageBox.Show("最多只能创建 " + Globals.MaxCharacterCount + " 个角色.");
                    _character.Dispose();
                    break;
                case 5:
                    MirMessageBox.Show("这个名称的角色已经存在.");
                    _character.NameTextBox.SetFocus();
                    break;
            }
        }
        private void NewCharacter(S.NewCharacterSuccess p)
        {
            _character.Dispose();
            MirMessageBox.Show("角色创建成功.");

            Characters.Insert(0, p.CharInfo);
            _selected = 0;
            UpdateInterface();
        }

        private void DeleteCharacter()
        {
            if (_selected < 0 || _selected >= Characters.Count) return;

            MirMessageBox message = new MirMessageBox(string.Format("确实要删除角色吗 {0}?", Characters[_selected].Name), MirMessageBoxButtons.YesNo);
            ulong index = Characters[_selected].Index;

            message.YesButton.Click += (o, e) =>
            {
                DeleteCharacterButton.Enabled = false;
                Network.Enqueue(new C.DeleteCharacter { CharacterIndex = index });
            };

            message.Show();
        }

        private void DeleteCharacter(S.DeleteCharacter p)
        {
            DeleteCharacterButton.Enabled = true;
            switch (p.Result)
            {
                case 0:
                    MirMessageBox.Show("删除角色当前已禁用.");
                    break;
                case 1:
                    MirMessageBox.Show("您所选择的角色不存在.");
                    break;
            }
        }
        private void DeleteCharacter(S.DeleteCharacterSuccess p)
        {
            DeleteCharacterButton.Enabled = true;
            MirMessageBox.Show("你的角色被成功删除.");

            for (int i = 0; i < Characters.Count; i++)
                if (Characters[i].Index == p.CharacterIndex)
                {
                    Characters.RemoveAt(i);
                    break;
                }

            UpdateInterface();
        }

        private void StartGame(S.StartGameDelay p)
        {
            StartGameButton.Enabled = true;

            long time = CMain.Time + p.Milliseconds;

            MirMessageBox message = new MirMessageBox(string.Format("你不能登录当前角色在 {0} 秒内.", Math.Ceiling(p.Milliseconds / 1000M)));

            message.BeforeDraw += (o, e) => message.Label.Text = string.Format("你不能登录当前角色在 {0} 秒内.", Math.Ceiling((time - CMain.Time) / 1000M));


            message.AfterDraw += (o, e) =>
            {
                if (CMain.Time <= time) return;
                message.Dispose();
                StartGame();
            };

            message.Show();
        }
        public void StartGame(S.StartGameBanned p)
        {
            StartGameButton.Enabled = true;

            TimeSpan d = p.ExpiryDate - CMain.Now;
            MirMessageBox.Show(string.Format("这个帐户被禁止了.\n\n原因: {0}\n期限: {1}\n持续: {2:#,##0} 时, {3} 分, {4} 秒", p.Reason,
                                             p.ExpiryDate, Math.Floor(d.TotalHours), d.Minutes, d.Seconds));
        }
        public void StartGame(S.StartGame p)
        {
            StartGameButton.Enabled = true;

            if (p.Resolution < Settings.Resolution || Settings.Resolution == 0) Settings.Resolution = p.Resolution;

            if (p.Resolution < 1024 || Settings.Resolution < 1024) Settings.Resolution = 800;
            else if (p.Resolution < 1366 || Settings.Resolution < 1280) Settings.Resolution = 1024;
            else if (p.Resolution < 1366 || Settings.Resolution < 1366) Settings.Resolution = 1280;//not adding an extra setting for 1280 on server cause well it just depends on the aspect ratio of your screen
            else if (p.Resolution >= 1366 && Settings.Resolution >= 1366) Settings.Resolution = 1366;

            switch (p.Result)
            {
                case 0:
                    MirMessageBox.Show("启动游戏目前禁用.");
                    break;
                case 1:
                    MirMessageBox.Show("您没有登录.");
                    break;
                case 2:
                    MirMessageBox.Show("Your character could not be found.");
                    break;
                case 3:
                    MirMessageBox.Show("未找到活动地图和/或起始点.");
                    break;
                case 4:
                    if (Settings.Resolution == 1024)
                        CMain.SetResolution(1024, 768);
                    else if (Settings.Resolution == 1280)
                        CMain.SetResolution(1280, 800);
                    else if (Settings.Resolution == 1366)
                        CMain.SetResolution(1366, 768);
                    ActiveScene = new GameScene();
                    Dispose();
                    break;
            }
        }
        private void UpdateInterface()
        {
            for (int i = 0; i < CharacterButtons.Length; i++)
            {
                CharacterButtons[i].Selected = i == _selected;
                CharacterButtons[i].Update(i >= Characters.Count ? null : Characters[i]);
            }

            if (_selected >= 0 && _selected < Characters.Count)
            {
                CharacterDisplay.Visible = true;
                //CharacterDisplay.Index = ((byte)Characters[_selected].Class + 1) * 20 + (byte)Characters[_selected].Gender * 280; 

                switch ((MirClass)Characters[_selected].Class)
                {
                    case MirClass.Warrior:
                        CharacterDisplay.Index = (byte)Characters[_selected].Gender == 0 ? 20 : 300; //220 : 500;
                        break;
                    case MirClass.Wizard:
                        CharacterDisplay.Index = (byte)Characters[_selected].Gender == 0 ? 40 : 320; //240 : 520;
                        break;
                    case MirClass.Taoist:
                        CharacterDisplay.Index = (byte)Characters[_selected].Gender == 0 ? 60 : 340; //260 : 540;
                        break;
                    case MirClass.Assassin:
                        CharacterDisplay.Index = (byte)Characters[_selected].Gender == 0 ? 80 : 360; //280 : 560;
                        break;
                    case MirClass.Archer:
                        CharacterDisplay.Index = (byte)Characters[_selected].Gender == 0 ? 100 : 140; //160 : 180;
                        break;
                }

                LastAccessLabel.Text = Characters[_selected].LastAccess == DateTime.MinValue ? "Never" : Characters[_selected].LastAccess.ToString();
                LastAccessLabel.Visible = true;
                LastAccessLabelLabel.Visible = true;
                StartGameButton.Enabled = true;
            }
            else
            {
                CharacterDisplay.Visible = false;
                LastAccessLabel.Visible = false;
                LastAccessLabelLabel.Visible = false;
                StartGameButton.Enabled = false;
            }
        }


        #region Disposable
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Background = null;
                _character = null;

                ServerLabel = null;
                CharacterDisplay = null;
                StartGameButton = null;
                NewCharacterButton = null;
                DeleteCharacterButton = null;
                CreditsButton = null;
                ExitGame = null;
                CharacterButtons = null;
                LastAccessLabel = null; LastAccessLabelLabel = null;
                Characters = null;
                _selected = 0;
            }

            base.Dispose(disposing);
        }
        #endregion
        public sealed class NewCharacterDialog : MirImageControl
        {
            //正则匹配所有字符
            //private static readonly Regex Reg = new Regex(@"^[*]{" + Globals.MinCharacterNameLength + "," + Globals.MaxCharacterNameLength + "}$");

            public MirImageControl TitleLabel;
            public MirAnimatedControl CharacterDisplay;

            public MirButton OKButton,
                             CancelButton,
                             WarriorButton,
                             WizardButton,
                             TaoistButton,
                             AssassinButton,
                             ArcherButton,
                             MaleButton,
                             FemaleButton;

            public MirTextBox NameTextBox;

            public MirLabel Description;

            private MirClass _class;
            private MirGender _gender;

            #region Descriptions
            public const string WarriorDescription =
                "战士是一个伟大的力量和生命的职业。他们不容易在战斗中丧生，并且具有能够使用各种重型武器和装备的优势。因此，战士喜欢基于近战物理伤害的攻击。他们在远程攻击中很弱，然而专门为战士开发的各种装备弥补了他们在远程战斗中的弱点。";

            public const string WizardDescription =
                "法师是低力量和体力的职业，但是具有使用强力法术的能力。他们的攻击性法术非常有效，但是因为施放这些法术需要时间，所以他们可能会对敌人的攻击敞开大门。因此，身体虚弱的巫师必须保持安全的距离攻击敌人。";

            public const string TaoistDescription =
                "道士在生物学、植物学等方面的研究都很有成就。他们的专长不是直接与敌人交战，而是协助他们的盟友提供支持。道士能召唤强大的生物，对魔法有很强的抵抗力，是一个攻防能力很平衡的职业。";

            public const string AssassinDescription =
                "刺客是秘密组织的成员，他们的历史是相对未知的。他们能够隐藏自己，在别人看不见的情况下进行攻击，这自然使他们擅长快速杀戮。由于生命力和力量薄弱，他们必须避免与多个敌人作战。";


            public const string ArcherDescription =
                "弓箭手是一类非常精准和强壮的人，他们使用弓箭的强大技能来对敌人造成非凡的伤害。就像法师一样，他们依靠敏锐的本能来躲避即将到来的攻击，避免被敌人过于靠近。然而，他们的身体力量和致命目标允许他们向任何他们攻击的人产生恐惧。";
               

            #endregion

            public NewCharacterDialog()
            {
                Index = 73;
                Library = Libraries.Prguse;
                Location = new Point((Settings.ScreenWidth - Size.Width) / 2, (Settings.ScreenHeight - Size.Height) / 2);
                Modal = true;

                TitleLabel = new MirImageControl
                {
                    Index = 20,
                    Library = Libraries.Title,
                    Location = new Point(206, 11),
                    Parent = this,
                };

                CancelButton = new MirButton
                {
                    HoverIndex = 281,
                    Index = 280,
                    Library = Libraries.Title,
                    Location = new Point(425, 425),
                    Parent = this,
                    PressedIndex = 282
                };
                CancelButton.Click += (o, e) => Dispose();


                OKButton = new MirButton
                {
                    Enabled = false,
                    HoverIndex = 361,
                    Index = 360,
                    Library = Libraries.Title,
                    Location = new Point(160, 425),
                    Parent = this,
                    PressedIndex = 362,
                };
                OKButton.Click += (o, e) => CreateCharacter();

                NameTextBox = new MirTextBox
                {
                    Location = new Point(325, 268),
                    Parent = this,
                    Size = new Size(240, 20),
                    MaxLength = Globals.MaxCharacterNameLength
                };
                NameTextBox.TextBox.KeyPress += TextBox_KeyPress;
                NameTextBox.TextBox.TextChanged += CharacterNameTextBox_TextChanged;
                NameTextBox.SetFocus();

                CharacterDisplay = new MirAnimatedControl
                {
                    Animated = true,
                    AnimationCount = 16,
                    AnimationDelay = 250,
                    Index = 20,
                    Library = Libraries.ChrSel,
                    Location = new Point(120, 250),
                    Parent = this,
                    UseOffSet = true,
                };
                CharacterDisplay.AfterDraw += (o, e) =>
                {
                    if (_class == MirClass.Wizard)
                        Libraries.ChrSel.DrawBlend(CharacterDisplay.Index + 560, CharacterDisplay.DisplayLocationWithoutOffSet, Color.White, true);
                };


                WarriorButton = new MirButton
                {
                    HoverIndex = 2427,
                    Index = 2427,
                    Library = Libraries.Prguse,
                    Location = new Point(323, 296),
                    Parent = this,
                    PressedIndex = 2428,
                    Sound = SoundList.ButtonA,
                };
                WarriorButton.Click += (o, e) =>
                {
                    _class = MirClass.Warrior;
                    UpdateInterface();
                };


                WizardButton = new MirButton
                {
                    HoverIndex = 2430,
                    Index = 2429,
                    Library = Libraries.Prguse,
                    Location = new Point(373, 296),
                    Parent = this,
                    PressedIndex = 2431,
                    Sound = SoundList.ButtonA,
                };
                WizardButton.Click += (o, e) =>
                {
                    _class = MirClass.Wizard;
                    UpdateInterface();
                };


                TaoistButton = new MirButton
                {
                    HoverIndex = 2433,
                    Index = 2432,
                    Library = Libraries.Prguse,
                    Location = new Point(423, 296),
                    Parent = this,
                    PressedIndex = 2434,
                    Sound = SoundList.ButtonA,
                };
                TaoistButton.Click += (o, e) =>
                {
                    _class = MirClass.Taoist;
                    UpdateInterface();
                };

                AssassinButton = new MirButton
                {
                    HoverIndex = 2436,
                    Index = 2435,
                    Library = Libraries.Prguse,
                    Location = new Point(473, 296),
                    Parent = this,
                    PressedIndex = 2437,
                    Sound = SoundList.ButtonA,
                };
                AssassinButton.Click += (o, e) =>
                {
                    _class = MirClass.Assassin;
                    UpdateInterface();
                };

                ArcherButton = new MirButton
                {
                    HoverIndex = 2439,
                    Index = 2438,
                    Library = Libraries.Prguse,
                    Location = new Point(523, 296),
                    Parent = this,
                    PressedIndex = 2440,
                    Sound = SoundList.ButtonA,
                };
                ArcherButton.Click += (o, e) =>
                {
                    _class = MirClass.Archer;
                    UpdateInterface();
                };


                MaleButton = new MirButton
                {
                    HoverIndex = 2421,
                    Index = 2421,
                    Library = Libraries.Prguse,
                    Location = new Point(323, 343),
                    Parent = this,
                    PressedIndex = 2422,
                    Sound = SoundList.ButtonA,
                };
                MaleButton.Click += (o, e) =>
                {
                    _gender = MirGender.Male;
                    UpdateInterface();
                };

                FemaleButton = new MirButton
                {
                    HoverIndex = 2424,
                    Index = 2423,
                    Library = Libraries.Prguse,
                    Location = new Point(373, 343),
                    Parent = this,
                    PressedIndex = 2425,
                    Sound = SoundList.ButtonA,
                };
                FemaleButton.Click += (o, e) =>
                {
                    _gender = MirGender.Female;
                    UpdateInterface();
                };

                Description = new MirLabel
                {
                    Border = true,
                    Location = new Point(279, 70),
                    Parent = this,
                    Size = new Size(278, 170),
                    Text = WarriorDescription,
                };
            }

            private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
            {
                if (sender == null) return;
                if (e.KeyChar != (char)Keys.Enter) return;
                e.Handled = true;

                if (OKButton.Enabled)
                    OKButton.InvokeMouseClick(null);
            }
            private void CharacterNameTextBox_TextChanged(object sender, EventArgs e)
            {
                if (string.IsNullOrEmpty(NameTextBox.Text))
                {
                    OKButton.Enabled = false;
                    NameTextBox.Border = false;
                }
                else if (NameTextBox.Text==null|| NameTextBox.Text.Length< Globals.MinCharacterNameLength || NameTextBox.Text.Length> Globals.MaxCharacterNameLength)
                {
                    OKButton.Enabled = false;
                    NameTextBox.Border = true;
                    NameTextBox.BorderColour = Color.Red;
                }
                else
                {
                    OKButton.Enabled = true;
                    NameTextBox.Border = true;
                    NameTextBox.BorderColour = Color.Green;
                }
            }

            private void CreateCharacter()
            {
                OKButton.Enabled = false;

                Network.Enqueue(new C.NewCharacter
                {
                    Name = NameTextBox.Text,
                    Class = _class,
                    Gender = _gender
                });
            }

            private void UpdateInterface()
            {
                MaleButton.Index = 2420;
                FemaleButton.Index = 2423;

                WarriorButton.Index = 2426;
                WizardButton.Index = 2429;
                TaoistButton.Index = 2432;
                AssassinButton.Index = 2435;
                ArcherButton.Index = 2438;

                switch (_gender)
                {
                    case MirGender.Male:
                        MaleButton.Index = 2421;
                        break;
                    case MirGender.Female:
                        FemaleButton.Index = 2424;
                        break;
                }

                switch (_class)
                {
                    case MirClass.Warrior:
                        WarriorButton.Index = 2427;
                        Description.Text = WarriorDescription;
                        CharacterDisplay.Index = (byte)_gender == 0 ? 20 : 300; //220 : 500;
                        break;
                    case MirClass.Wizard:
                        WizardButton.Index = 2430;
                        Description.Text = WizardDescription;
                        CharacterDisplay.Index = (byte)_gender == 0 ? 40 : 320; //240 : 520;
                        break;
                    case MirClass.Taoist:
                        TaoistButton.Index = 2433;
                        Description.Text = TaoistDescription;
                        CharacterDisplay.Index = (byte)_gender == 0 ? 60 : 340; //260 : 540;
                        break;
                    case MirClass.Assassin:
                        AssassinButton.Index = 2436;
                        Description.Text = AssassinDescription;
                        CharacterDisplay.Index = (byte)_gender == 0 ? 80 : 360; //280 : 560;
                        break;
                    case MirClass.Archer:
                        ArcherButton.Index = 2439;
                        Description.Text = ArcherDescription;
                        CharacterDisplay.Index = (byte)_gender == 0 ? 100 : 140; //160 : 180;
                        break;
                }

                //CharacterDisplay.Index = ((byte)_class + 1) * 20 + (byte)_gender * 280;
            }
        }
        public sealed class CharacterButton : MirImageControl
        {
            public MirLabel NameLabel, LevelLabel, ClassLabel;
            public bool Selected;

            public CharacterButton()
            {
                Index = 44; //45 locked
                Library = Libraries.Prguse;
                Sound = SoundList.ButtonA;

                NameLabel = new MirLabel
                {
                    Location = new Point(107, 9),
                    Parent = this,
                    NotControl = true,
                    Size = new Size(170, 18)
                };

                LevelLabel = new MirLabel
                {
                    Location = new Point(107, 28),
                    Parent = this,
                    NotControl = true,
                    Size = new Size(30, 18)
                };

                ClassLabel = new MirLabel
                {
                    Location = new Point(178, 28),
                    Parent = this,
                    NotControl = true,
                    Size = new Size(100, 18)
                };
            }

            public void Update(SelectInfo info)
            {
                if (info == null)
                {
                    Index = 44;
                    Library = Libraries.Prguse;
                    NameLabel.Text = string.Empty;
                    LevelLabel.Text = string.Empty;
                    ClassLabel.Text = string.Empty;

                    NameLabel.Visible = false;
                    LevelLabel.Visible = false;
                    ClassLabel.Visible = false;

                    return;
                }

                Library = Libraries.Title;
                //这里改下，改成6职业的
                //Index = 660 + (byte)info.Class;
                Index = 658 + (byte)info.Class;
                //if (Selected) Index += 5;
                if (Selected) Index += 6;

                string classname = "战士";
                switch (info.Class)
                {
                    case MirClass.Warrior:
                        classname = "战士";
                        break;
                    case MirClass.Wizard:
                        classname = "法师";
                        break;
                    case MirClass.Taoist:
                        classname = "道士";
                        break;
                    case MirClass.Assassin:
                        classname = "刺客";
                        break;
                    case MirClass.Archer:
                        classname = "弓箭手";
                        break;
                    case MirClass.Monk:
                        classname = "武僧";
                        break;

                }

                NameLabel.Text = info.Name;
                LevelLabel.Text = info.Level.ToString();
                ClassLabel.Text = classname;

                NameLabel.Visible = true;
                LevelLabel.Visible = true;
                ClassLabel.Visible = true;
            }
        }
    }
}
