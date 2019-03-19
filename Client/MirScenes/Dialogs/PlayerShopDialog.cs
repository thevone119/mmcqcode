using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirObjects;
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
    //玩家商城。支持金币，元宝交易
    public sealed class PlayerShopDialog : MirImageControl
    {
        public MirLabel PageNumberLabel, totalGold, totalCredits;
        //职业筛选
        public MirButton ALL, War, Sin, Tao, Wiz, Arch;

        //关闭，下一页，上一页
        public MirButton CloseButton, PreviousButton, NextButton;
        //分类下拉滚动条
        public MirButton UpButton, DownButton, PositionBar;

        //增加的标题
        public MirLabel TitleLabel;

        public MirPlayerShopCell[] Grid;
        public MirLabel[] Filters = new MirLabel[22];
        //这个是分类
        Dictionary<string, byte> CategoryList = new Dictionary<string, byte>();
        //public List<ClientAuction> filteredShop = new List<ClientAuction>();
        public List<ClientAuction> SearchResult = new List<ClientAuction>();
        public MirTextBox Search;
        public MirButton FindButton;//查询按钮
        public MirImageControl  FilterBackground;

        public string ClassFilter = "";
        public string TypeFilter = "";
        public string SectionFilter = "";

        public int StartIndex = 0;
        public int Page = 0;
        public int CStartIndex = 0;
        public int PageCount;

        //UserMode：卖：true, 买：false
        public static bool UserMode = false;

        private long SearchTime;//最后查询时间，3秒只能查询一次

        public PlayerShopDialog()
        {
            //分类
            CategoryList.Add("--所有--",0);
            CategoryList.Add("武器",1 );
            CategoryList.Add("盔甲",2);
            CategoryList.Add("头盔",4 );
            CategoryList.Add("项链",5);
            CategoryList.Add("手链",6 );
            CategoryList.Add("戒指",7 );
            CategoryList.Add("腰带",9);
            CategoryList.Add("靴子",10);
            CategoryList.Add("宝石",11);
            CategoryList.Add("火把",12);
            CategoryList.Add("药剂",13 );
            CategoryList.Add("矿石",14);
            CategoryList.Add("书籍",20 );
            CategoryList.Add("卷轴",17);
            CategoryList.Add("宝玉", 18);
            CategoryList.Add("材料", 16);
            CategoryList.Add("坐骑", 19);
            CategoryList.Add("肉类", 15);
            CategoryList.Add("--其他--",100);


            Index = 749;
            Library = Libraries.Title;
            Movable = true;
            Location = Center;
            Sort = true;

        
            TitleLabel = new MirLabel
            {
                Text = "",
                Parent = this,
                Font = new Font(Settings.FontName, 10F, FontStyle.Bold),
                ForeColour = Color.BurlyWood,
                Location = new Point(30, 6),
                AutoSize = true
            };

            Grid = new MirPlayerShopCell[4 * 2];
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    int idx = 4 * y + x;
                    Grid[idx] = new MirPlayerShopCell
                    {
                        Size = new Size(125, 146),
                        Visible = true,
                        Parent = this,
                    };
                }
            }

            CloseButton = new MirButton
            {
                HoverIndex = 361,
                Index = 360,
                Location = new Point(671, 4),
                Library = Libraries.Prguse2,
                Parent = this,
                PressedIndex = 362,
                Sound = SoundList.ButtonA,
            };
            CloseButton.Click += (o, e) => Hide();

            totalGold = new MirLabel
            {
                Size = new Size(100, 20),
                DrawFormat = TextFormatFlags.RightToLeft | TextFormatFlags.Right,

                Location = new Point(123, 449),
                Parent = this,
                NotControl = true,
                Font = new Font(Settings.FontName, 8F),
            };
            totalCredits = new MirLabel
            {
                Size = new Size(100, 20),
                DrawFormat = TextFormatFlags.RightToLeft | TextFormatFlags.Right,
                Location = new Point(5, 449),
                Parent = this,
                NotControl = true,
                Font = new Font(Settings.FontName, 8F)
            };

            UpButton = new MirButton
            {
                Index = 197,
                HoverIndex = 198,
                PressedIndex = 199,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(16, 14),
                Location = new Point(120, 103),
                Sound = SoundList.ButtonA,
                Visible = true
            };
            UpButton.Click += (o, e) =>
            {
                if (CStartIndex <= 0) return;

                CStartIndex--;

                SetCategories();
                UpdatePositionBar();
            };

            DownButton = new MirButton
            {
                Index = 207,
                HoverIndex = 208,
                Library = Libraries.Prguse2,
                PressedIndex = 209,
                Parent = this,
                Size = new Size(16, 14),
                Location = new Point(120, 421),
                Sound = SoundList.ButtonA,
                Visible = true
            };
            DownButton.Click += (o, e) =>
            {
                if (CStartIndex + 22 >= CategoryList.Count) return;

                CStartIndex++;

                SetCategories();
                UpdatePositionBar();
            };

            PositionBar = new MirButton
            {
                Index = 205,
                HoverIndex = 206,
                PressedIndex = 206,
                Library = Libraries.Prguse2,
                Location = new Point(120, 117),
                Parent = this,
                Movable = true,
                Sound = SoundList.None,
                Visible = true
            };
            PositionBar.OnMoving += PositionBar_OnMoving;




            FilterBackground = new MirImageControl
            {
                Index = 769,
                Library = Libraries.Title,
                Location = new Point(11, 102),
                Parent = this,
            };
            FilterBackground.MouseWheel += FilterScrolling;

            Search = new MirTextBox
            {
                //BackColour = Color.FloralWhite,
                BorderColour = Color.BurlyWood,
                Border = true,
                BackColour = Color.FromArgb(4, 4, 4),
                ForeColour = Color.White,
                Parent = this,
                Size = new Size(101, 18),
                Location = new Point(11, 70),
                Font = new Font(Settings.FontName, 9F),
                MaxLength = 23,
                CanLoseFocus = true,
            };

            Search.TextBox.KeyUp += (o, e) =>
            {
                SectionFilter = Search.TextBox.Text;
                //UpdateShop();
            };

            FindButton = new MirButton
            {
                HoverIndex = 481,
                Index = 480,
                Location = new Point(138, 66),
                Library = Libraries.Title,
                Parent = this,
                PressedIndex = 482,
                Sound = SoundList.ButtonA,
            };
            FindButton.Click += (o, e) =>
            {
                if (CMain.Time < SearchTime)
                {
                    GameScene.Scene.ChatDialog.ReceiveChat(LanguageUtils.Format("You can search again after {0} seconds.", Math.Ceiling((SearchTime - CMain.Time) / 1000D)), ChatType.System);
                    return;
                }
                SearchTime = CMain.Time + Globals.SearchDelay;
                Page = 0;
                StartIndex = 0;
                selectShop();
            };




            ALL = new MirButton
            {
                Index = 751,
                HoverIndex = 752,
                PressedIndex = 753,
                Library = Libraries.Title,
                Location = new Point(539, 37),
                Visible = true,
                Parent = this,
            };
            ALL.Click += (o, e) =>
            {
                ClassFilter = "";
                GetCategories();
                ResetClass();
            };
            War = new MirButton
            {
                Index = 754,
                HoverIndex = 755,
                PressedIndex = 756,
                Library = Libraries.Title,
                Location = new Point(568, 38),
                Visible = true,
                Parent = this,
            };
            War.Click += (o, e) =>
            {
                ClassFilter = "1";
                GetCategories();
                ResetClass();
            };
            Sin = new MirButton
            {
                Index = 757,
                HoverIndex = 758,
                PressedIndex = 759,
                Library = Libraries.Title,
                Location = new Point(591, 38),
                Visible = true,
                Parent = this,
            };
            Sin.Click += (o, e) =>
            {
                ClassFilter = "8";
                GetCategories();
                ResetClass();
            };
            Tao = new MirButton
            {
                Index = 760,
                HoverIndex = 761,
                PressedIndex = 762,
                Library = Libraries.Title,
                Location = new Point(614, 38),
                Visible = true,
                Parent = this,
            };
            Tao.Click += (o, e) =>
            {
                ClassFilter = "4";
                GetCategories();
                ResetClass();
            };
            Wiz = new MirButton
            {
                Index = 763,
                HoverIndex = 764,
                PressedIndex = 765,
                Library = Libraries.Title,
                Location = new Point(637, 38),
                Visible = true,
                Parent = this,
            };
            Wiz.Click += (o, e) =>
            {
                ClassFilter = "2";
                GetCategories();
                ResetClass();
            };
            Arch = new MirButton
            {
                Index = 766,
                HoverIndex = 767,
                PressedIndex = 768,
                Library = Libraries.Title,
                Location = new Point(660, 38),
                Visible = true,
                Parent = this,
            };
            Arch.Click += (o, e) =>
            {
                ClassFilter = "16";
                GetCategories();
                ResetClass();
            };

            PageNumberLabel = new MirLabel
            {
                Text = "",
                Parent = this,
                Size = new Size(83, 17),
                Location = new Point(597, 446),
                DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                Font = new Font(Settings.FontName, 7F),
            };

            PreviousButton = new MirButton
            {
                Index = 240,
                HoverIndex = 241,
                PressedIndex = 242,
                Library = Libraries.Prguse2,
                Parent = this,
                Location = new Point(600, 448),
                Sound = SoundList.ButtonA,
            };
            PreviousButton.Click += (o, e) =>
            {
                Page--;
                if (Page < 0) Page = 0;
                StartIndex = Grid.Length * Page;

                UpdateShop();
            };

            NextButton = new MirButton
            {
                Index = 243,
                HoverIndex = 244,
                PressedIndex = 245,
                Library = Libraries.Prguse2,
                Parent = this,
                Location = new Point(660, 448),
                Sound = SoundList.ButtonA,
            };
            NextButton.Click += (o, e) =>
            {
                if (Page >= PageCount - 1) return;
                if (Page < (SearchResult.Count - 1) / 8)
                {
                    Page++;
                    StartIndex = Grid.Length * Page;
                    UpdateShop();
                    return;
                }
                Page = Page + 1;
                StartIndex = Grid.Length * Page;
                selectShop();
                //UpdateShop();
            };

            for (int i = 0; i < Filters.Length; i++)
            {
                Filters[i] = new MirLabel
                {
                    Parent = this,
                    Size = new Size(90, 20),
                    Location = new Point(15, 103 + (15 * i)),
                    Text = "" ,
                    ForeColour = Color.Gray,
                    Font = new Font(Settings.FontName, 8F),
                };
                //开始默认选择第一个
                if (i == 0)
                {
                    Filters[i].ForeColour = Color.FromArgb(230, 200, 160);
                }
                Filters[i].Click += (o, e) =>
                {
                    MirLabel lab = (MirLabel)o;
                    TypeFilter = lab.Text;
                    Page = 0;
                    StartIndex = 0;
                    //UpdateShop();
                    for (int p = 0; p < Filters.Length; p++)
                    {
                        if (Filters[p].Text == lab.Text) Filters[p].ForeColour = Color.FromArgb(230, 200, 160);
                        else Filters[p].ForeColour = Color.Gray;
                    }
                    selectShop();
                };
                Filters[i].MouseEnter += (o, e) =>
                {
                    MirLabel lab = (MirLabel)o;
                    for (int p = 0; p < Filters.Length; p++)
                    {
                        if (Filters[p].Text == lab.Text && Filters[p].ForeColour != Color.FromArgb(230, 200, 160)) Filters[p].ForeColour = Color.FromArgb(160, 140, 110);
                    }
                };
                Filters[i].MouseLeave += (o, e) =>
                {
                    MirLabel lab = (MirLabel)o;
                    for (int p = 0; p < Filters.Length; p++)
                    {
                        if (Filters[p].Text == lab.Text && Filters[p].ForeColour != Color.FromArgb(230, 200, 160)) Filters[p].ForeColour = Color.Gray;
                    }
                };
                Filters[i].MouseWheel += FilterScrolling;
            }
        }

        public void Hide()
        {
            if (!Visible) return;
            TypeFilter = "";
            Visible = false;
        }
        public void Show()
        {
            if (Visible) return;
            Visible = true;
            //ClassFilter = GameScene.User.Class;
            //ClassFilter = "";
            //SectionFilter = "";
            ResetTabs();
            ResetClass();
            GetCategories();

            NPCObject npc = (NPCObject)MapControl.GetObject(GameScene.NPCID);
            if (npc != null)
            {
                string[] nameSplit = npc.Name.Split('_');
                TitleLabel.Text = nameSplit[0];
            }
            else if (TitleLabel.Text == "")
            {
                TitleLabel.Text = "玩家集市";
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Search.Dispose();
            Search = null;

            PageNumberLabel = null;
            totalGold = null;
            totalCredits = null;
            ALL = null;
            War = null;
            Sin = null;
            Tao = null;
            Wiz = null;
            Arch = null;

            CloseButton = null;
            PreviousButton = null;
            NextButton = null;

            UpButton = null;
            DownButton = null;
            PositionBar = null;

            Grid = null;
            Filters = null;
            FilterBackground = null;

            SearchResult.Clear();
        }

        public void Process()
        {
            totalCredits.Text = GameScene.Credit.ToString("###,###,##0");
            totalGold.Text = GameScene.Gold.ToString("###,###,##0");
        }


        public void FilterScrolling(object sender, MouseEventArgs e)
        {
            int count = e.Delta / SystemInformation.MouseWheelScrollDelta;

            if (CStartIndex == 0 && count >= 0) return;
            if (CStartIndex == CategoryList.Count - 1 && count <= 0) return;
            if (CategoryList.Count <= 22) return;

            CStartIndex -= count;

            if (CStartIndex < 0) CStartIndex = 0;
            if (CStartIndex + 22 > CategoryList.Count - 1) CStartIndex = CategoryList.Count - 22;

            SetCategories();

            UpdatePositionBar();

        }

        private void UpdatePositionBar()
        {
            if (CategoryList.Count <= 22) return;

            int interval = 290 / (CategoryList.Count - 22);

            int x = 120;
            int y = 117 + (CStartIndex * interval);

            if (y >= 401) y = 401;
            if (y <= 117) y = 117;

            PositionBar.Location = new Point(x, y);
        }

        void PositionBar_OnMoving(object sender, MouseEventArgs e)
        {
            int x = 120;
            int y = PositionBar.Location.Y;

            if (y >= 401) y = 401;
            if (y <= 117) y = 117;

            if (CategoryList.Count > 22)
            {
                int location = y - 117;
                int interval = 284 / (CategoryList.Count - 22);

                double yPoint = location / interval;

                CStartIndex = Convert.ToInt16(Math.Floor(yPoint));
                SetCategories();
            }

            PositionBar.Location = new Point(x, y);
        }

        public void ResetTabs()
        {
            
        }

        public void ResetClass()
        {
            ALL.Index = 751;
            War.Index = 754;
            Sin.Index = 757;
            Tao.Index = 760;
            Wiz.Index = 763;
            Arch.Index = 766;

            if (ClassFilter == "") ALL.Index = 752;
            if (ClassFilter == "1") War.Index = 755;
            if (ClassFilter == "8") Sin.Index = 758;
            if (ClassFilter == "4") Tao.Index = 761;
            if (ClassFilter == "2") Wiz.Index = 764;
            if (ClassFilter == "16") Arch.Index = 767;
        }

        public void GetCategories()
        {
            //TypeFilter = "";
            Page = 0;
            StartIndex = 0;
            //List<GameShopItem> shopList;

            
            //Filters[0].ForeColour = Color.FromArgb(230, 200, 160);
            CStartIndex = 0;
            SetCategories();
            UpdateShop();
        }

        public void SetCategories()
        {
            List<string> listc = new List<string>();
            foreach(string value in CategoryList.Keys)
            {
                listc.Add(value);
            }
            //如果是卖家查看自己的额，只看到所有的
            if (UserMode)
            {
                string v = listc[0];
                listc.Clear();
                listc.Add(v);
            }

            for (int i = 0; i < Filters.Length; i++)
            {
                if (i < listc.Count)
                {
                    Filters[i].Text = listc[i + CStartIndex];
                       
                    Filters[i].ForeColour = Filters[i].Text == TypeFilter ? Color.FromArgb(230, 200, 160) : Color.Gray;
                    Filters[i].NotControl = false;
                }
                else
                {
                    Filters[i].Text = "";
                    Filters[i].NotControl = true;
                }
            }
        }


        public void selectShop()
        {
            byte itemtype = 0;
            if (CategoryList.ContainsKey(TypeFilter))
            {
                itemtype = CategoryList[TypeFilter];
            }
            Network.Enqueue(new C.MarketSearch
            {
                Page = Page,
                itemtype = itemtype,
                Match = SectionFilter,
            });
            //MirLog.info("itemtype:"+ itemtype+",page:"+Page);

            //SearchResult.Clear();
        }

        //更新商城
        public void UpdateShop()
        {
            for (int i = 0; i < Grid.Length; i++)
            {
                if (Grid[i] != null) Grid[i].Dispose();
                Grid[i].Item = null;
            };
            //过滤排除后的，这里主要过滤职业
            List<ClientAuction> FilterResult = new List<ClientAuction>();
            for (int i = 0; i < SearchResult.Count; i++)
            {
                if (ClassFilter == "")
                {
                    FilterResult.Add(SearchResult[i]);
                    continue;
                }
                if (ClassFilter == "1" && SearchResult[i].Item.Info.RequiredClass.HasFlag(RequiredClass.Warrior))
                {
                    FilterResult.Add(SearchResult[i]);
                    continue;
                }
                if (ClassFilter == "2" && SearchResult[i].Item.Info.RequiredClass.HasFlag(RequiredClass.Wizard))
                {
                    FilterResult.Add(SearchResult[i]);
                    continue;
                }
                if (ClassFilter == "4" && SearchResult[i].Item.Info.RequiredClass.HasFlag(RequiredClass.Taoist))
                {
                    FilterResult.Add(SearchResult[i]);
                    continue;
                }
                if (ClassFilter == "8" && SearchResult[i].Item.Info.RequiredClass.HasFlag(RequiredClass.Assassin))
                {
                    FilterResult.Add(SearchResult[i]);
                    continue;
                }
                if (ClassFilter == "16" && SearchResult[i].Item.Info.RequiredClass.HasFlag(RequiredClass.Archer))
                {
                    FilterResult.Add(SearchResult[i]);
                    continue;
                }
            }


            PageNumberLabel.Text = (Page + 1) + " / " + PageCount;

            int maxIndex = FilterResult.Count - 1;

            if (StartIndex > maxIndex) StartIndex = maxIndex;
            if (StartIndex < 0) StartIndex = 0;

            for (int i = 0; i < Grid.Length; i++)
            {
                if (i + StartIndex >= FilterResult.Count) break;

                if (Grid[i] != null) Grid[i].Dispose();

                Grid[i] = new MirPlayerShopCell
                {
                    Visible = true,
                    UserMode= UserMode,
                    Item = FilterResult[i + StartIndex],
                    Size = new Size(125, 146),
                    Location = i < 4 ? new Point(152 + (i * 132), 115) : new Point(152 + ((i - 4) * 132), 275),
                    Parent = this,
                };
            }
        }

    }
}
