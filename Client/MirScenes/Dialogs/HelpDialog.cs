using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirObjects;
using Client.MirSounds;
using S = ServerPackets;
using C = ClientPackets;

namespace Client.MirScenes.Dialogs
{
    public sealed class HelpDialog : MirImageControl
    {
        public List<HelpPage> Pages = new List<HelpPage>();

        public MirButton CloseButton, NextButton, PreviousButton;
        public MirLabel PageLabel;
        public HelpPage CurrentPage;

        public int CurrentPageNumber = 0;

        public HelpDialog()
        {
            Index = 920;
            Library = Libraries.Prguse;
            Movable = true;
            Sort = true;

            Location = Center;

            MirImageControl TitleLabel = new MirImageControl
            {
                Index = 57,
                Library = Libraries.Title,
                Location = new Point(18, 9),
                Parent = this
            };

            PreviousButton = new MirButton
            {
                Index = 240,
                HoverIndex = 241,
                PressedIndex = 242,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(16, 16),
                Location = new Point(210, 485),
                Sound = SoundList.ButtonA,
            };
            PreviousButton.Click += (o, e) =>
            {
                CurrentPageNumber--;

                if (CurrentPageNumber < 0) CurrentPageNumber = Pages.Count - 1;

                DisplayPage(CurrentPageNumber);
            };

            NextButton = new MirButton
            {
                Index = 243,
                HoverIndex = 244,
                PressedIndex = 245,
                Library = Libraries.Prguse2,
                Parent = this,
                Size = new Size(16, 16),
                Location = new Point(310, 485),
                Sound = SoundList.ButtonA,
            };
            NextButton.Click += (o, e) =>
            {
                CurrentPageNumber++;

                if (CurrentPageNumber > Pages.Count - 1) CurrentPageNumber = 0;

                DisplayPage(CurrentPageNumber);
            };

            PageLabel = new MirLabel
            {
                Text = "",
                Font = new Font(Settings.FontName, 9F),
                DrawFormat = TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter,
                Parent = this,
                NotControl = true,
                Location = new Point(230, 480),
                Size = new Size(80, 20)
            };

            CloseButton = new MirButton
            {
                HoverIndex = 361,
                Index = 360,
                Location = new Point(509, 3),
                Library = Libraries.Prguse2,
                Parent = this,
                PressedIndex = 362,
                Sound = SoundList.ButtonA,
            };
            CloseButton.Click += (o, e) => Hide();

            LoadImagePages();

            DisplayPage();
        }

        private void LoadImagePages()
        {
            Point location = new Point(12, 35);

            Dictionary<string, string> keybinds = new Dictionary<string, string>();

            List<HelpPage> imagePages = new List<HelpPage> { 
                new HelpPage("快捷键使用", -1, new ShortcutPage1 { Parent = this } ) { Parent = this, Location = location, Visible = false }, 
                new HelpPage("快捷键使用", -1, new ShortcutPage2 { Parent = this } ) { Parent = this, Location = location, Visible = false }, 
                new HelpPage("聊天快捷方式", -1, new ShortcutPage3 { Parent = this } ) { Parent = this, Location = location, Visible = false }, 
                new HelpPage("Movements", 0, null) { Parent = this, Location = location, Visible = false }, 
                new HelpPage("Attacking", 1, null) { Parent = this, Location = location, Visible = false }, 
                new HelpPage("Collecting Items", 2, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Health", 3, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Skills", 4, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Skills", 5, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Mana", 6, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Chatting", 7, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Groups", 8, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Durability", 9, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Purchasing", 10, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Selling", 11, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Repairing", 12, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Trading", 13, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Inspecting", 14, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Statistics", 15, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Statistics", 16, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Statistics", 17, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Statistics", 18, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Statistics", 19, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Statistics", 20, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Quests", 21, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Quests", 22, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Quests", 23, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Quests", 24, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Mounts", 25, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Mounts", 26, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Fishing", 27, null) { Parent = this, Location = location, Visible = false },
                new HelpPage("Gems and Orbs", 28, null) { Parent = this, Location = location, Visible = false },
            };

            Pages.AddRange(imagePages);
        }


        public void DisplayPage(string pageName)
        {
            if (Pages.Count < 1) return;

            for (int i = 0; i < Pages.Count; i++)
            {
                if (Pages[i].Title.ToLower() != pageName.ToLower()) continue;

                DisplayPage(i);
                break;
            }
        }

        public void DisplayPage(int id = 0)
        {
            if (Pages.Count < 1) return;

            if (id > Pages.Count - 1) id = Pages.Count - 1;
            if (id < 0) id = 0;

            if (CurrentPage != null)
            {
                CurrentPage.Visible = false;
                if (CurrentPage.Page != null) CurrentPage.Page.Visible = false;
            }

            CurrentPage = Pages[id];

            if (CurrentPage == null) return;

            CurrentPage.Visible = true;
            if (CurrentPage.Page != null) CurrentPage.Page.Visible = true;
            CurrentPageNumber = id;

            CurrentPage.PageTitleLabel.Text = id + 1 + ". " + CurrentPage.Title;

            PageLabel.Text = string.Format("{0} / {1}", id + 1, Pages.Count);

            Show();
        }

        public void Show()
        {
            if (Visible) return;
            Visible = true;
        }

        public void Hide()
        {
            if (!Visible) return;
            Visible = false;
        }

        public void Toggle()
        {
            if (!Visible)
                Show();
            else
                Hide();
        }
    }

    public class ShortcutPage1 : ShortcutInfoPage
    {
        public ShortcutPage1()
        {
            Shortcuts = new List<ShortcutInfo>();
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Exit), "退出游戏"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Logout), "小退"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Bar1Skill1) + "-" + CMain.InputKeys.GetKey(KeybindOptions.Bar1Skill8), "技能按钮"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Inventory), "角色背包"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Equipment), "角色信息"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Skills), "技能窗口"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Group), "组队窗口"));
            Shortcuts.Add(new ShortcutInfo("Ctrl + G", "快速添加队友"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Trade), "交易窗口"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Guilds), "公会信息"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.GameShop), "商城信息"));
            //Shortcuts.Add(new ShortcutInfo("K", "Rental window (open / close)"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Bigmap), "显示大地图，大地图中右键点击可自动寻路"));


            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Options), "系统选项，内挂辅助"));
            Shortcuts.Add(new ShortcutInfo("O", "系统设置，技能模式，声音等"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Help), "帮助窗口"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Mount), "骑马"));

            LoadKeyBinds();
        }
    }
    public class ShortcutPage2 : ShortcutInfoPage
    {
        public ShortcutPage2()
        {
            Shortcuts = new List<ShortcutInfo>();
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.ChangePetmode), "切换宠物攻击模式"));
            //Shortcuts.Add(new ShortcutInfo("Ctrl + F", "Change the font in the chat box"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.ChangeAttackmode), "切换自身攻击模式"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Belt), "关闭/开启 数字按钮"));
            
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Minimap), "小地图开关"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Skillbar), "显示技能栏"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Autorun), "自动跑步开关"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Cameramode), "显示/隐藏操作界面"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Pickup), "手动捡取脚下的物品"));
            Shortcuts.Add(new ShortcutInfo("Ctrl + 鼠标右键", "查看玩家信息"));
            //Shortcuts.Add(new ShortcutInfo("F12", "Chat macros"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Screenshot), "截屏"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Fishing), "打开/关闭钓鱼窗口"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Relationship), "结婚窗口"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Mentor), "师徒窗口"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.Friends), "好友窗口"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.CreaturePickup), "宠物捡取 (多目标)"));
            Shortcuts.Add(new ShortcutInfo(CMain.InputKeys.GetKey(KeybindOptions.CreatureAutoPickup), "宠物捡取 (单目标)"));

            LoadKeyBinds();
        }
    }
    public class ShortcutPage3 : ShortcutInfoPage
    {
        public ShortcutPage3()
        {
            Shortcuts = new List<ShortcutInfo>();
            //Shortcuts.Add(new ShortcutInfo("` / Ctrl", "Change the skill bar"));
            Shortcuts.Add(new ShortcutInfo("/(玩家名称)", "私聊玩家"));
            Shortcuts.Add(new ShortcutInfo("!", "向附近的人喊话，配合喇叭可全服喊话"));
            Shortcuts.Add(new ShortcutInfo("!~", "公会聊天"));

            LoadKeyBinds();
        }
    }

    public class ShortcutInfo
    {
        public string Shortcut { get; set; }
        public string Information { get; set; }

        public ShortcutInfo(string shortcut, string info)
        {
            Shortcut = shortcut.Replace("\n", " + ");
            Information = info;
        }
    }

    public class ShortcutInfoPage : MirControl
    {
        protected List<ShortcutInfo> Shortcuts = new List<ShortcutInfo>();

        public ShortcutInfoPage()
        {
            Visible = false;

            MirLabel shortcutTitleLabel = new MirLabel
            {
                Text = "快捷键",
                DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                ForeColour = Color.White,
                Font = new Font(Settings.FontName, 10F),
                Parent = this,
                AutoSize = true,
                Location = new Point(13, 75),
                Size = new Size(100, 30)
            };

            MirLabel infoTitleLabel = new MirLabel
            {
                Text = "功能",
                DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                ForeColour = Color.White,
                Font = new Font(Settings.FontName, 10F),
                Parent = this,
                AutoSize = true,
                Location = new Point(114, 75),
                Size = new Size(405, 30)
            };
        }

        public void LoadKeyBinds()
        {
            if (Shortcuts == null) return;

            for (int i = 0; i < Shortcuts.Count; i++)
            {
                MirLabel shortcutLabel = new MirLabel
                {
                    Text = Shortcuts[i].Shortcut,
                    ForeColour = Color.Yellow,
                    DrawFormat = TextFormatFlags.VerticalCenter,
                    Font = new Font(Settings.FontName, 9F),
                    Parent = this,
                    AutoSize = true,
                    Location = new Point(18, 107 + (20 * i)),
                    Size = new Size(95, 23),
                };

                MirLabel informationLabel = new MirLabel
                {
                    Text = Shortcuts[i].Information,
                    DrawFormat = TextFormatFlags.VerticalCenter,
                    ForeColour = Color.White,
                    Font = new Font(Settings.FontName, 9F),
                    Parent = this,
                    AutoSize = true,
                    Location = new Point(119, 107 + (20 * i)),
                    Size = new Size(400, 23),
                };
            }  
        }
    }

    public class HelpPage : MirControl
    {
        public string Title;
        public int ImageID;
        public MirControl Page;

        public MirLabel PageTitleLabel;

        public HelpPage(string title, int imageID, MirControl page)
        {
            Title = title;
            ImageID = imageID;
            Page = page;

            NotControl = true;
            Size = new System.Drawing.Size(508, 396 + 40);

            BeforeDraw += HelpPage_BeforeDraw;

            PageTitleLabel = new MirLabel
            {
                Text = Title,
                Font = new Font(Settings.FontName, 10F, FontStyle.Bold),
                DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                Parent = this,
                Size = new System.Drawing.Size(242, 30),
                Location = new Point(135, 4)
            };
        }

        void HelpPage_BeforeDraw(object sender, EventArgs e)
        {
            if (ImageID < 0) return;

            Libraries.Help.Draw(ImageID, new Point(DisplayLocation.X, DisplayLocation.Y + 40), Color.White, false);
        }
    }
}
