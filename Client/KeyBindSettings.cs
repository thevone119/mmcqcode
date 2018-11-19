using System.IO;
using System;
using Client.MirSounds;
using System.Windows.Forms;
using System.Collections.Generic;

/// <summary>
/// 
/// 
/// 
/// 
/// 
/// （一）键事件按下列顺序发生： 
/**
KeyDown

KeyPress

KeyUp

 （二）KeyDown触发后，不一定触发KeyUp，当KeyDown 按下后，拖动鼠标，那么将不会触发KeyUp事件。

 （三）定义

KeyDown：在控件有焦点的情况下按下键时发生。

KeyPress：在控件有焦点的情况下按下键时发生。(下面会说和KeyDown 的区别)

KeyUp：在控件有焦点的情况下释放键时发生。

 （四）KeyPress 和KeyDown 、KeyPress之间的区别

          1.KeyPress主要用来捕获数字(注意：包括Shift+数字的符号)、字母（注意：包括大小写）、小键盘等除了F1-12、SHIFT、Alt、Ctrl、Insert、Home、PgUp、Delete、End、PgDn、ScrollLock、Pause、NumLock、{菜单键}、{开始键}和方向键外的ANSI字符 KeyDown 和KeyUp 通常可以捕获键盘除了PrScrn所有按键(这里不讨论特殊键盘的特殊键）

           2.KeyPress 只能捕获单个字符


  KeyDown 和KeyUp 可以捕获组合键。

          3.KeyPress 可以捕获单个字符的大小写

          4.KeyDown和KeyUp 对于单个字符捕获的KeyValue 都是一个值，也就是不能判断单个字符的大小写。

          5.KeyPress 不区分小键盘和主键盘的数字字符。


  KeyDown 和KeyUp 区分小键盘和主键盘的数字字符。

          6.其中PrScrn 按键KeyPress、KeyDown和KeyUp 都不能捕获。

 （五）系统组合键的判定

在使用键盘的时候，通常会使用到CTRL+SHIFT+ALT 类似的组合键功能。对于此，我们如何来判定？

通过KeyUp 事件能够来处理（这里说明一下为什么不用KeyDown，因为在判定KeyDown的时候，CTRL、SHIFT和ALT 属于一直按下状态，然后再加另外一个键是不能准确捕获组合键，所以使用KeyDown 是不能准确判断出的，要通过KeyUp 事件来判定 ）
*/
/// 
/// 
/// 
/// 
/// 
/// </summary>
namespace Client
{

    public enum KeybindOptions : int
    {
        Bar1Skill1 = 0,
        Bar1Skill2,
        Bar1Skill3,
        Bar1Skill4,
        Bar1Skill5,
        Bar1Skill6,
        Bar1Skill7,
        Bar1Skill8,
        Bar2Skill1,
        Bar2Skill2,
        Bar2Skill3,
        Bar2Skill4,
        Bar2Skill5,
        Bar2Skill6,
        Bar2Skill7,
        Bar2Skill8,
        Inventory,
        Inventory2,
        Equipment,
        Equipment2,
        Skills,
        Skills2,
        Creature,
        MountWindow,
        Mount,
        Fishing,
        Skillbar,
        Mentor,
        Relationship,
        Friends,
        Guilds,
        GameShop,
        Quests,
        Closeall,
        Options,
        Options2,
        Group,
        Belt,
        BeltFlip,
        Pickup,
        Belt1,
        Belt1Alt,
        Belt2,
        Belt2Alt,
        Belt3,
        Belt3Alt,
        Belt4,
        Belt4Alt,
        Belt5,
        Belt5Alt,
        Belt6,
        Belt6Alt,
        Logout,
        Exit,
        CreaturePickup,
        CreatureAutoPickup,
        Minimap,
        Bigmap,
        Trade,
        Rental,
        ChangeAttackmode,
        AttackmodePeace,
        AttackmodeGroup,
        AttackmodeGuild,
        AttackmodeEnemyguild,
        AttackmodeRedbrown,
        AttackmodeAll,
        ChangePetmode,
        PetmodeBoth,
        PetmodeMoveonly,
        PetmodeAttackonly,
        PetmodeNone,
        Help,
        Autorun,
        Cameramode,
        Screenshot,
        DropView,
        TargetDead,//这个是挖肉么？
        Ranking,
        AddGroupMember
    }
    //自定义键，0：没有按住。1：必须按住，2：按不按都可以
    public class KeyBind
    {
        public KeybindOptions function = KeybindOptions.Bar1Skill1;//捆绑的事件
        public byte RequireCtrl = 0; //so these requirexxx: 0 < only works if you DONT hold the key, 1 < only works if you HOLD the key, 2 < works REGARDLESSS of the key
        public byte RequireShift = 0;
        public byte RequireAlt = 0;
        public byte RequireTilde = 0;
        public Keys Key = 0;


        public override  string ToString()
        {
            return "function:" + function + ",RequireCtrl:" + RequireCtrl + ",RequireShift:" + RequireShift + ",RequireAlt:" + RequireAlt + ",RequireTilde:" + RequireTilde + ",Key:" + Key;
        }
    }

    //按键设置，这个后面要改下，改成保存到角色下？
    public class KeyBindSettings
    {
        private static InIReader Reader = new InIReader(@".\KeyBinds.ini");
        public List<KeyBind> Keylist = new List<KeyBind>();
        private Dictionary<KeybindOptions, KeyBind> funcdict = new Dictionary<KeybindOptions, KeyBind>();
        private Dictionary<Keys, KeyBind> keydict = new Dictionary<Keys, KeyBind>();

        public KeyBindSettings()
        {
            New();
            if (!File.Exists(@".\KeyBinds.ini"))
            {
                Save();
                return;
            }
            Load();
            for (int i = 0; i < Keylist.Count; i++)
            {
                if (!funcdict.ContainsKey(Keylist[i].function))
                {
                    funcdict.Add(Keylist[i].function, Keylist[i]);
                }
                if (!keydict.ContainsKey(Keylist[i].Key))
                {
                    keydict.Add(Keylist[i].Key, Keylist[i]);
                }
            }
        }

        public void Load()
        {
            foreach (KeyBind Inputkey in Keylist)
            {
                Inputkey.RequireAlt = Reader.ReadByte(Inputkey.function.ToString(), "RequireAlt", Inputkey.RequireAlt);
                Inputkey.RequireShift = Reader.ReadByte(Inputkey.function.ToString(), "RequireShift", Inputkey.RequireShift);
                Inputkey.RequireTilde = Reader.ReadByte(Inputkey.function.ToString(), "RequireTilde", Inputkey.RequireTilde);
                Inputkey.RequireCtrl = Reader.ReadByte(Inputkey.function.ToString(), "RequireCtrl", Inputkey.RequireCtrl);
                string Input = Reader.ReadString(Inputkey.function.ToString(), "RequireKey", Inputkey.Key.ToString());
                Enum.TryParse(Input, out Inputkey.Key);
                
            }
        }

        public void Save()
        {
            Reader.Write("Guide", "01", "RequireAlt,RequireShift,RequireTilde,RequireCtrl");
            Reader.Write("Guide", "02", "have 3 options: 0/1/2");
            Reader.Write("Guide", "03", "0 < you cannot have this key pressed to use the function");
            Reader.Write("Guide", "04", "1 < you have to have this key pressed to use this function");
            Reader.Write("Guide", "05", "2 < it doesnt matter if you press this key to use this function");
            Reader.Write("Guide", "06", "by default just use 2, unless you have 2 functions on the same key");
            Reader.Write("Guide", "07", "example: change attack mode (ctrl+h) and help (h)");
            Reader.Write("Guide", "08", "if you set either of those to requireshift 2, then they wil both work at the same time or not work");
            Reader.Write("Guide", "09", "");
            Reader.Write("Guide", "10", "To get the value for RequireKey look at:");
            Reader.Write("Guide", "11", "https://msdn.microsoft.com/en-us/library/system.windows.forms.keys(v=vs.110).aspx");
        
            foreach (KeyBind Inputkey in Keylist)
            {
                Reader.Write(Inputkey.function.ToString(), "RequireAlt", Inputkey.RequireAlt);
                Reader.Write(Inputkey.function.ToString(), "RequireShift", Inputkey.RequireShift);
                Reader.Write(Inputkey.function.ToString(), "RequireTilde", Inputkey.RequireTilde);
                Reader.Write(Inputkey.function.ToString(), "RequireCtrl", Inputkey.RequireCtrl);
                Reader.Write(Inputkey.function.ToString(), "RequireKey", Inputkey.Key.ToString());
            }
        }

        public void New()
        {
            KeyBind InputKey;
            InputKey = new KeyBind{ function = KeybindOptions.Bar1Skill1, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 0, Key = Keys.F1 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar1Skill2, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 0, Key = Keys.F2 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar1Skill3, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 0, Key = Keys.F3 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar1Skill4, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 0, Key = Keys.F4 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar1Skill5, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 0, Key = Keys.F5 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar1Skill6, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 0, Key = Keys.F6 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar1Skill7, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 0, Key = Keys.F7 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar1Skill8, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 0, Key = Keys.F8 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar2Skill1, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 1, Key = Keys.F1 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar2Skill2, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 1, Key = Keys.F2 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar2Skill3, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 1, Key = Keys.F3 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar2Skill4, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 1, Key = Keys.F4 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar2Skill5, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 1, Key = Keys.F5 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar2Skill6, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 1, Key = Keys.F6 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar2Skill7, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 1, Key = Keys.F7 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bar2Skill8, RequireAlt = 2, RequireShift = 2, RequireTilde = 0, RequireCtrl = 1, Key = Keys.F8 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Inventory, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.F9 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Inventory2, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.I };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Equipment, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.F10 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Equipment2, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.C };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Skills, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.F11 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Skills2, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.S };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Creature, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.E };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.MountWindow, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.J };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Mount, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.M };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Fishing, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.N };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Skillbar, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.R };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Mentor, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.W };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Relationship, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.L };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Friends, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.F };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Guilds, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 0, Key = Keys.G };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.GameShop, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.Y };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Quests, RequireAlt = 0, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.Q };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Closeall, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.Escape };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Options, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.F12 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Options2, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.O };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Group, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.P };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Belt, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 0, Key = Keys.Z };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.BeltFlip, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 1, Key = Keys.Z };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Pickup, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.Tab };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Belt1, RequireAlt = 2, RequireShift = 0, RequireTilde = 2, RequireCtrl = 2, Key = Keys.D1 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Belt1Alt, RequireAlt = 2, RequireShift = 0, RequireTilde = 2, RequireCtrl = 2, Key = Keys.NumPad1 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Belt2, RequireAlt = 0, RequireShift = 0, RequireTilde = 2, RequireCtrl = 2, Key = Keys.D2 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Belt2Alt, RequireAlt = 2, RequireShift = 0, RequireTilde = 2, RequireCtrl = 2, Key = Keys.NumPad2 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Belt3, RequireAlt = 2, RequireShift = 0, RequireTilde = 2, RequireCtrl = 2, Key = Keys.D3 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Belt3Alt, RequireAlt = 2, RequireShift = 0, RequireTilde = 2, RequireCtrl = 2, Key = Keys.NumPad3 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Belt4, RequireAlt = 2, RequireShift = 0, RequireTilde = 2, RequireCtrl = 2, Key = Keys.D4 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Belt4Alt, RequireAlt = 2, RequireShift = 0, RequireTilde = 2, RequireCtrl = 2, Key = Keys.NumPad4 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Belt5, RequireAlt = 2, RequireShift = 0, RequireTilde = 2, RequireCtrl = 2, Key = Keys.D5 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Belt5Alt, RequireAlt = 2, RequireShift = 0, RequireTilde = 2, RequireCtrl = 2, Key = Keys.NumPad5 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Belt6, RequireAlt = 2, RequireShift = 0, RequireTilde = 2, RequireCtrl = 2, Key = Keys.D6 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Belt6Alt, RequireAlt = 2, RequireShift = 0, RequireTilde = 2, RequireCtrl = 2, Key = Keys.NumPad6 };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Logout, RequireAlt = 1, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.X };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Exit, RequireAlt = 1, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.Q };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.CreaturePickup, RequireAlt = 0, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.X };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.CreatureAutoPickup, RequireAlt = 1, RequireShift = 0, RequireTilde = 2, RequireCtrl = 2, Key = Keys.A };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Minimap, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.V };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Bigmap, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.B };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Trade, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.T };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Rental, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 0, Key = Keys.A };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.ChangeAttackmode, RequireAlt = 2, RequireShift = 0, RequireTilde = 2, RequireCtrl = 1, Key = Keys.H };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.AttackmodePeace, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.None};
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.AttackmodeGroup, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.None };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.AttackmodeGuild, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.None };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.AttackmodeEnemyguild, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.None };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.AttackmodeRedbrown, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.None };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.AttackmodeAll, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.None };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.ChangePetmode, RequireAlt = 0, RequireShift = 0, RequireTilde = 2, RequireCtrl = 1, Key = Keys.A };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.PetmodeBoth, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.None };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.PetmodeMoveonly, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.None };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.PetmodeAttackonly, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.None };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.PetmodeNone, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.None};
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Help, RequireAlt = 2, RequireShift = 0, RequireTilde = 2, RequireCtrl = 2, Key = Keys.H };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Autorun, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.D };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Cameramode, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.Insert };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Screenshot, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.PrintScreen };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.DropView, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.Tab };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.TargetDead, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 1, Key = Keys.ControlKey };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.Ranking, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 2, Key = Keys.K };
            Keylist.Add(InputKey);
            InputKey = new KeyBind { function = KeybindOptions.AddGroupMember, RequireAlt = 2, RequireShift = 2, RequireTilde = 2, RequireCtrl = 1, Key = Keys.G };
            Keylist.Add(InputKey);
        }

        public string GetKey(KeybindOptions Option)
        {
            string output = "";
            if (!funcdict.ContainsKey(Option))
            {
                return output;
            }
            KeyBind kb = funcdict[Option];
            if (kb != null)
            {
                if (kb.Key == Keys.None) return output;
                if (kb.RequireAlt == 1)
                    output = "Alt";
                if (kb.RequireCtrl == 1)
                    output = output != "" ? output + "\nCtrl" : "Ctrl";
                if (kb.RequireShift == 1)
                    output = output != "" ? output + "\nShift" : "Shift";
                if (kb.RequireTilde == 1)
                    output = output != "" ? output + "\n~" : "~";

                output = output != "" ? output + "\n" + kb.Key.ToString() : kb.Key.ToString();
                return output;
            }
           
            return "";
        }

        //通过fun查询绑定
        public KeyBind GetKeyBindByFun(KeybindOptions Option)
        {
            if (!funcdict.ContainsKey(Option))
            {
                return null;
            }
            return funcdict[Option];
        }
        //通过key查询绑定
        public KeyBind GetKeyBindByKey(Keys key)
        {
            if (!keydict.ContainsKey(key))
            {
                return null;
            }
            return keydict[key];
        }

        //通过组合参数查询key
        public KeyBind GetKeyBind(bool Shift, bool Alt, bool Ctrl, bool Tilde, Keys key)
        {
            if (key == Keys.None) return null;
            if (!keydict.ContainsKey(key))
            {
                return null;
            }
            //自定义键，0：没有按住。1：必须按住，2：按不按都可以
            //MirLog.debug("key" + key);
            KeyBind kb = keydict[key];
            if (kb == null)
            {
                return null;
            }
            //MirLog.debug("kb" + kb.ToString());
            if (kb.RequireShift != 2 && kb.RequireShift != (Shift ? 1 : 0))
            {
                return null;
            }
            if (kb.RequireAlt != 2 && kb.RequireAlt != (Alt ? 1 : 0))
            {
                return null;
            }
            if (kb.RequireCtrl != 2 && kb.RequireCtrl != (Ctrl ? 1 : 0))
            {
                return null;
            }
            if (kb.RequireTilde != 2 && kb.RequireTilde != (Tilde ? 1 : 0))
            {
                return null;
            }
            return kb;
        }

    }

    
}
