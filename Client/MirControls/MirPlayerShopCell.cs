using System;
using System.Drawing;
using System.Windows.Forms;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirObjects;
using Client.MirScenes;
using Client.MirSounds;
using Client.MirScenes.Dialogs;
using C = ClientPackets;

namespace Client.MirControls
{
    //新版寄售物品格子显示
    public sealed class MirPlayerShopCell : MirImageControl
    {
        public MirLabel nameLabel, typeLabel, goldLabel, gpLabel, StockLabel, countLabel;
        public ClientAuction Item;
        public UserItem ShowItem;
        Rectangle ItemDisplayArea;
        //购买，查看,下架
        public MirButton BuyItem;
        public MirImageControl ViewerBackground;

        public MirLabel quantity;
        //UserMode：卖：true, 买：false
        public  bool UserMode = false;
        private long MarketTime;

        public MirPlayerShopCell()
        {
            Size = new Size(125, 146);
            Index = 750;
            Library = Libraries.Title;
            MouseLeave += (o, e) =>
            {
                GameScene.Scene.DisposeItemLabel();
                GameScene.HoverItem = null;
            };

            nameLabel = new MirLabel
            {
                Size = new Size(125, 15),
                DrawFormat = TextFormatFlags.HorizontalCenter,
                Location = new Point(0, 13),
                Parent = this,
                NotControl = true,
                Font = new Font(Settings.FontName, 8F),
            };

            goldLabel = new MirLabel
            {
                Size = new Size(95, 20),
                DrawFormat = TextFormatFlags.RightToLeft | TextFormatFlags.Right,
                Location = new Point(2, 102),
                Parent = this,
                NotControl = true,
                Font = new Font(Settings.FontName, 8F)
            };

            gpLabel = new MirLabel
            {
                Size = new Size(95, 20),
                DrawFormat = TextFormatFlags.RightToLeft | TextFormatFlags.Right,
                Location = new Point(2, 81),
                Parent = this,
                NotControl = true,
                Font = new Font(Settings.FontName, 8F)
            };

            StockLabel = new MirLabel
            {
                Size = new Size(50, 20),
                Location = new Point(53, 37),
                Parent = this,
                NotControl = true,
                ForeColour = Color.Gray,
                Font = new Font(Settings.FontName, 8F),
                Text = "-"
            };

     

            countLabel = new MirLabel
            {
                Size = new Size(30, 20),
                DrawFormat = TextFormatFlags.Right,
                Location = new Point(16, 60),
                Parent = this,
                NotControl = true,
                Font = new Font(Settings.FontName, 7F),
            };


            BuyItem = new MirButton
            {
                Index = 778,
                HoverIndex = 779,
                PressedIndex = 780,
                Location = new Point(42, 122),
                Library = Libraries.Title,
                Parent = this,
                Sound = SoundList.ButtonA,
            };
            BuyItem.Click += (o, e) =>
            {
                BuyProduct();
            };
            




            quantity = new MirLabel
            {
                Size = new Size(78, 14),
                DrawFormat = TextFormatFlags.Left | TextFormatFlags.VerticalCenter,
                Location = new Point(52, 56),
                ForeColour = Color.Gray,
                Parent = this,
                NotControl = true,
                Font = new Font(Settings.FontName, 8F),
            };



        }

        //玩家集市购买物品
        public void BuyProduct()
        {
            if (CMain.Time < MarketTime) return;

            if (UserMode)
            {
                if (!Item.Sold)
                {
                    MirMessageBox box = new MirMessageBox(LanguageUtils.Format("{0} 还在售，你确定需要取回么?", Item.Item.FriendlyName), MirMessageBoxButtons.YesNo);
                    box.YesButton.Click += (o1, e2) =>
                    {
                        MarketTime = CMain.Time + 3000;
                        Network.Enqueue(new C.MarketGetBack { AuctionID = Item.AuctionID });
                    };
                    box.Show();
                }
                else
                {
                    MarketTime = CMain.Time + 3000;
                    Network.Enqueue(new C.MarketGetBack { AuctionID = Item.AuctionID });
                }
            }
            else
            {

                //byte payType;//支付类型 0：金币 1：元宝
                MirMessageBox2 messageBox;
                //如果物品的金币价格是0，则只能是元宝购买
                if (Item.GoldPrice <= 0 && Item.CreditPrice > 0)
                {
                    messageBox = new MirMessageBox2(LanguageUtils.Format("请选择购买{1} x {0} 使用的购买货币", Item.Item.FriendlyName, Item.Item.Count), MirMessageBoxButtons.YesNo) { YesText = "元宝购买", NoText = "取消" };
                    messageBox.YesButton.Click += (o, e) =>
                    {
                        MarketTime = CMain.Time + 3000;
                        Network.Enqueue(new C.MarketBuy { AuctionID = Item.AuctionID, payType=1 });
                    };
                    messageBox.NoButton.Click += (o, e) => { };
                }
                else if (Item.GoldPrice > 0 && Item.CreditPrice <= 0)
                {
                    messageBox = new MirMessageBox2(LanguageUtils.Format("请选择购买{1} x {0} 使用的购买货币", Item.Item.FriendlyName, Item.Item.Count), MirMessageBoxButtons.YesNo) { YesText = "金币购买", NoText = "取消" };
                    messageBox.YesButton.Click += (o, e) =>
                    {
                        MarketTime = CMain.Time + 3000;
                        Network.Enqueue(new C.MarketBuy { AuctionID = Item.AuctionID, payType = 0 });
                    };
                    messageBox.NoButton.Click += (o, e) => { };
                }
                else
                {
                    messageBox = new MirMessageBox2(LanguageUtils.Format("请选择购买{1} x {0} 使用的购买货币", Item.Item.FriendlyName, Item.Item.Count), MirMessageBoxButtons.YesNoCancel) { YesText = "元宝购买", NoText = "金币购买" };
                    messageBox.YesButton.Click += (o, e) =>
                    {
                        MarketTime = CMain.Time + 3000;
                        Network.Enqueue(new C.MarketBuy { AuctionID = Item.AuctionID, payType = 1 });
                    };
                    messageBox.NoButton.Click += (o, e) =>
                    {
                        MarketTime = CMain.Time + 3000;
                        Network.Enqueue(new C.MarketBuy { AuctionID = Item.AuctionID, payType = 0 });
                    };
                    messageBox.CancelButton.Click += (o, e) => { };
                }
                messageBox.Show();
            }
        }

        public override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (GameScene.HoverItem != null && (Item.Item.UniqueID != GameScene.HoverItem.UniqueID))
            {
                GameScene.Scene.DisposeItemLabel();
                GameScene.HoverItem = null;
                ShowItem = null;
            }

            if (ShowItem == null && ItemDisplayArea != null && ItemDisplayArea.Contains(CMain.MPoint))
            {
                ShowItem = Item.Item;
                GameScene.Scene.CreateItemLabel(ShowItem);
            }
            else if (ShowItem != null && ItemDisplayArea != null && !ItemDisplayArea.Contains(CMain.MPoint))
            {
                GameScene.Scene.DisposeItemLabel();
                GameScene.HoverItem = null;
                ShowItem = null;
            }
        }

        public void UpdateText()
        {
            BuyItem.Index = UserMode ? 400 : 778;
            BuyItem.HoverIndex = UserMode ? 401 : 779;
            BuyItem.PressedIndex = UserMode ? 402 : 780;

            nameLabel.Text = Item.Item.FriendlyName;
            nameLabel.Text = nameLabel.Text.Length > 17 ? nameLabel.Text.Substring(0, 17) : nameLabel.Text;
            nameLabel.ForeColour = Item.Item.Info.getNameColor();
            quantity.Text = Item.Seller;
            if (Item.GoldPrice <= 0)
            {
                goldLabel.Text = "-";
            }
            else
            {
                goldLabel.Text = (Item.GoldPrice ).ToString("###,###,##0");
            }
            TimeSpan t = (Item.ConsignmentDate.AddDays(Globals.ConsignmentLength) - DateTime.Now);
    
            if (t.TotalSeconds < 0)
            {
                StockLabel.Text = "过期";
            }
            else if (t.TotalMinutes < 1.0)
            {
                StockLabel.Text = string.Format("{0}秒", t.Seconds);
            }
            else if (t.TotalHours < 1.0)
            {
                StockLabel.Text =  string.Format("{0}分", t.Minutes);
            }
            else if (t.TotalDays < 1.0)
            {
                StockLabel.Text = string.Format("{0}时{1:D2}分", (int)t.TotalHours, t.Minutes);
            }
            else // more than 1 day
            {
                StockLabel.Text =  string.Format("{0}天{1}时 ", (int)t.TotalDays, (int)t.Hours);
            }
           

            

            gpLabel.Text = (Item.CreditPrice).ToString("###,###,##0");
      
            countLabel.Text = Item.Item.Count.ToString();
        }

        protected internal override void DrawControl()
        {
            
            base.DrawControl();

            if (Item == null) return;

            UpdateText();

            Size size = Libraries.Items.GetTrueSize(Item.Item.Image);
            Point offSet = new Point((32 - size.Width) / 2, (32 - size.Height) / 2);

            Libraries.Items.Draw(Item.Item.Image, offSet.X + DisplayLocation.X + 12, offSet.Y + DisplayLocation.Y + 40);
            ItemDisplayArea = new Rectangle(new Point(offSet.X + DisplayLocation.X + 12, offSet.Y + DisplayLocation.Y + 40), size);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Item = null;
            GameScene.HoverItem = null;
            ShowItem = null;
        }

    }

    
}
