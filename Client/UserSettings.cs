using System.IO;
using System;
using Client.MirSounds;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Client
{
    //用户的设置，针对角色的
    public class UserSettings
    {

        private  InIReader Reader = new InIReader(@".\Config\uid_cfg");

        //是否显示职业等级
        public  bool ShowLevel = true;
        //免Shift
        public bool ExcuseShift = true;
        //显示ping
        public bool ShowPing = true;
        //显示时装
        public bool ShowFashion = false;

        //显示怪物名称
        public bool ShowMonName = true;
        //显示玩家姓名
        public bool ShowPlayName = true;

        //隔位刺杀
        public bool Septum = true;
        //自动烈火
        public bool AutoFlaming = true;
        public long LastFlamingTime = 0;//最后自动烈火时间
        //自动开盾
        public bool AutoShield = true;
        public long LastShieldTime = 0;//最后自动开盾时间
        //自动毒符,这个不要了，在服务器端实现了
        public bool AutoPoison = true;
        //开启切换则自动切换，不开启切换，则优先使用最近的毒
        public bool switchPoison = true;//切换红绿毒

        //自动体迅风
        public bool AutoHaste = true;
        public long LastHasteTime = 0;//最后体迅风时间
        //风身术
        public bool AutoLightBody = true;

        //保护
        public bool OpenProtect = true;

        //血量保护1普通喝药
        public byte HPLower1 = 50;
        public string HPUse1 = "金创药";
        public long LastHPUse1Time = 0;//最后使用物品时间
        //血量保护2快速喝药
        public byte HPLower2 = 70;
        public string HPUse2 = "太阳水";
        public long LastHPUse2Time = 0;//最后使用物品时间
        //血量保护3逃生卷
        public byte HPLower3 = 10;
        public string HPUse3 = "回城卷";
        public long LastHPUse3Time = 0;//最后使用物品时间
        //蓝量保护
        public byte MPLower1 = 20;
        public string MPUse1 = "魔法药";
        public long LastMPUse1Time = 0;//最后使用物品时间


        //自动捡物品
        public bool AutoPickUp = true;
        public long AutoPickUpTime = 0;//最后使用物品时间
      
        //捡取物品的列表
        public List<PickItem> PickUpList = new List<PickItem>();//物品列表已名字_0;名字_1这种格式保存
        public long CheckPickTime = 0;//最后使用物品时间

        public UserSettings(ulong uid)
        {
            if (!Directory.Exists(@".\Config\")) Directory.CreateDirectory(@".\Config\");
            Reader = new InIReader(@".\Config\"+ uid + "_cfg");
            //Load();
        }


        public void addItem(string _itemname)
        {
            bool has = false;
            for(int i=0;i< PickUpList.Count; i++)
            {
                if (PickUpList[i].itemname == _itemname)
                {
                    has=true;
                    break;
                }
            }
            if (!has)
            {
                PickUpList.Add(new PickItem() { itemname= _itemname });
            }
        }
        //是否捡取某个物品
        public bool pickItem(string _itemname)
        {
            for (int i = 0; i < PickUpList.Count; i++)
            {
                if (PickUpList[i].itemname == _itemname && PickUpList[i].pick == false)
                {
                    return false;
                }
            }
            return true;
        }

        private string PickUpListToString()
        {
            string picklist = "";
            for (int i = 0; i < PickUpList.Count; i++)
            {
                picklist += PickUpList[i].ToString()+";";
            }
            return picklist;
        }

        public  void Load()
        {
            ShowLevel = Reader.ReadBoolean("UserSettings", "ShowLevel", ShowLevel);
            ExcuseShift = Reader.ReadBoolean("UserSettings", "ExcuseShift", ExcuseShift);
            ShowPing = Reader.ReadBoolean("UserSettings", "ShowPing", ShowPing);
            ShowFashion = Reader.ReadBoolean("UserSettings", "ShowFashion", ShowFashion);
            Septum = Reader.ReadBoolean("UserSettings", "Septum", Septum);
            AutoFlaming = Reader.ReadBoolean("UserSettings", "AutoFlaming", AutoFlaming);
            AutoShield = Reader.ReadBoolean("UserSettings", "AutoShield", AutoShield);
            AutoPoison = Reader.ReadBoolean("UserSettings", "AutoPoison", AutoPoison);
            AutoHaste = Reader.ReadBoolean("UserSettings", "AutoHaste", AutoHaste);
            AutoLightBody = Reader.ReadBoolean("UserSettings", "AutoLightBody", AutoLightBody);
            OpenProtect = Reader.ReadBoolean("UserSettings", "OpenProtect", OpenProtect);
            AutoPickUp = Reader.ReadBoolean("UserSettings", "AutoPickUp", AutoPickUp);
            ShowMonName = Reader.ReadBoolean("UserSettings", "ShowMonName", ShowMonName);
            

            HPLower1 = Reader.ReadByte("UserSettings", "HPLower1", HPLower1);
            HPLower2 = Reader.ReadByte("UserSettings", "HPLower2", HPLower2);
            HPLower3 = Reader.ReadByte("UserSettings", "HPLower3", HPLower3);
            MPLower1 = Reader.ReadByte("UserSettings", "MPLower1", MPLower1);

            HPUse1 = Reader.ReadString("UserSettings", "HPUse1", HPUse1);
            HPUse2 = Reader.ReadString("UserSettings", "HPUse2", HPUse2);
            MPUse1 = Reader.ReadString("UserSettings", "MPUse1", MPUse1);
            string PickUpListstr = Reader.ReadString("UserSettings", "PickUpList", "金币_True;");
            string[] ps = PickUpListstr.Split(';');
            for(int i = 0; i < ps.Length; i++)
            {
                PickItem p = new PickItem(ps[i]);
                if (p.itemname != null)
                {
                    PickUpList.Add(new PickItem(ps[i]));
                }
            }
        }

        public  void Save()
        {
            Reader.Write("UserSettings", "ShowLevel", ShowLevel);
            Reader.Write("UserSettings", "ExcuseShift", ExcuseShift);
            Reader.Write("UserSettings", "ShowPing", ShowPing);
            Reader.Write("UserSettings", "ShowFashion", ShowFashion);
            Reader.Write("UserSettings", "Septum", Septum);
            Reader.Write("UserSettings", "AutoFlaming", AutoFlaming);
            Reader.Write("UserSettings", "AutoShield", AutoShield);
            Reader.Write("UserSettings", "AutoPoison", AutoPoison);

            Reader.Write("UserSettings", "AutoHaste", AutoHaste);
            Reader.Write("UserSettings", "AutoLightBody", AutoLightBody);
  
            Reader.Write("UserSettings", "OpenProtect", OpenProtect);
            Reader.Write("UserSettings", "AutoPickUp", AutoPickUp);
            Reader.Write("UserSettings", "ShowMonName", ShowMonName);
            
            Reader.Write("UserSettings", "HPLower1", HPLower1);
            Reader.Write("UserSettings", "HPLower2", HPLower2);
            Reader.Write("UserSettings", "HPLower3", HPLower3);
            Reader.Write("UserSettings", "MPLower1", MPLower1);
            Reader.Write("UserSettings", "HPUse1", HPUse1);
            Reader.Write("UserSettings", "HPUse2", HPUse2);
            Reader.Write("UserSettings", "MPUse1", MPUse1);
            Reader.Write("UserSettings", "PickUpList", PickUpListToString());
        }
    }

    public class PickItem
    {
        public string itemname;
        public bool pick = true;

        public PickItem()
        {

        }

        public PickItem(string line)
        {
            if(line==null|| line.Length < 3)
            {
                return;
            }
            string[] s = line.Split('_');
            if (s.Length == 2)
            {
                itemname = s[0];
                bool.TryParse(s[1], out pick);
            }
        }

        public override string ToString()
        {
            return itemname+"_"+ pick;
        }
    }
}
