using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Server.MirDatabase
{
    //临时处理数据，生产无效
    public class DataHandle
    {
        //物品检查
        public static void HandleItem()
        {
            string filepath = "d:/物品检查表.csv";
            string[] lines = File.ReadAllLines(filepath, EncodingType.GetType(filepath));
            List<ItemInfo> list = new List<ItemInfo>();
            for(int i=0; i< lines.Length; i++)
            {
                string line = lines[i];
                if (line == null)
                {
                    continue;
                }
                string[] arr = line.Split(',');
                
                int v = 0;
                bool b = false;
                ItemInfo info = new ItemInfo();
                info.Name = arr[0];
                int.TryParse(arr[2], out v);
                info.Type = (ItemType)v;
                if (v == 0)
                {
                    continue;
                }
                if (info.Name.StartsWith("//"))
                {
                    continue;
                }
                v = 0; int.TryParse(arr[3], out v);
                info.Grade = (ItemGrade)v;
                v = 0; int.TryParse(arr[4], out v);
                info.RequiredType = (RequiredType)v;
                v = 0; int.TryParse(arr[5], out v);
                info.RequiredClass = (RequiredClass)v;
                v = 0; int.TryParse(arr[6], out v);
                info.RequiredGender = (RequiredGender)v;
                v = 0; int.TryParse(arr[7], out v);
                info.Set = (ItemSet)v;
                v = 0; int.TryParse(arr[8], out v);
                info.Shape = (short)v;
                v = 0; int.TryParse(arr[9], out v);
                info.Weight = (byte)v;
                v = 0; int.TryParse(arr[10], out v);
                info.Light = (byte)v;
                v = 0; int.TryParse(arr[11], out v);
                info.RequiredAmount = (byte)v;
                v = 0; int.TryParse(arr[12], out v);
                info.MinAC = (byte)v;
                v = 0; int.TryParse(arr[13], out v);
                info.MaxAC = (byte)v;
                v = 0; int.TryParse(arr[14], out v);
                info.MinMAC = (byte)v;
                v = 0; int.TryParse(arr[15], out v);
                info.MaxMAC = (byte)v;
                v = 0; int.TryParse(arr[16], out v);
                info.MinDC = (byte)v;
                v = 0; int.TryParse(arr[17], out v);
                info.MaxDC = (byte)v;
                v = 0; int.TryParse(arr[18], out v);
                info.MinMC = (byte)v;
                v = 0; int.TryParse(arr[19], out v);
                info.MaxMC = (byte)v;
                v = 0; int.TryParse(arr[20], out v);
                info.MinSC = (byte)v;
                v = 0; int.TryParse(arr[21], out v);
                info.MaxSC = (byte)v;
                v = 0; int.TryParse(arr[22], out v);
                info.Accuracy = (byte)v;
                v = 0; int.TryParse(arr[23], out v);
                info.Agility = (byte)v;
                v = 0; int.TryParse(arr[24], out v);
                info.HP = (ushort)v;
                v = 0; int.TryParse(arr[25], out v);
                info.MP = (ushort)v;
                v = 0; int.TryParse(arr[26], out v);
                info.AttackSpeed = (sbyte)v;
                v = 0; int.TryParse(arr[27], out v);
                info.Luck = (sbyte)v;
                v = 0; int.TryParse(arr[28], out v);
                info.BagWeight = (byte)v;
                v = 0; int.TryParse(arr[29], out v);
                info.HandWeight = (byte)v;
                v = 0; int.TryParse(arr[30], out v);
                info.WearWeight = (byte)v;
                b = false; bool.TryParse(arr[31], out b);
                info.StartItem = b;
                v = 0; int.TryParse(arr[32], out v);
                info.Image = (ushort)v;
                v = 0; int.TryParse(arr[33], out v);
                info.Durability = (ushort)v;
                v = 0; int.TryParse(arr[34], out v);
                info.Price = (uint)v;
                v = 1; int.TryParse(arr[35], out v);
                info.StackSize = (uint)v;
                v = 0; int.TryParse(arr[36], out v);
                info.Effect = (byte)v;
                v = 0; int.TryParse(arr[37], out v);
                info.Strong = (byte)v;
                v = 0; int.TryParse(arr[38], out v);
                info.MagicResist = (byte)v;
                v = 0; int.TryParse(arr[39], out v);
                info.PoisonResist = (byte)v;
                v = 0; int.TryParse(arr[40], out v);
                info.HealthRecovery = (byte)v;
                v = 0; int.TryParse(arr[41], out v);
                info.SpellRecovery = (byte)v;
                v = 0; int.TryParse(arr[42], out v);
                info.PoisonRecovery = (byte)v;
                v = 0; int.TryParse(arr[43], out v);
                info.HPrate = (byte)v;
                v = 0; int.TryParse(arr[44], out v);
                info.MPrate = (byte)v;
                v = 0; int.TryParse(arr[45], out v);
                info.CriticalRate = (byte)v;
                v = 0; int.TryParse(arr[46], out v);
                info.CriticalDamage = (byte)v;
                b = false; bool.TryParse(arr[47], out b);
                info.NeedIdentify = b;
                b = false; bool.TryParse(arr[48], out b);
                info.ShowGroupPickup = b;
                v = 0; int.TryParse(arr[49], out v);
                info.MaxAcRate = (byte)v;
                v = 0; int.TryParse(arr[50], out v);
                info.MaxMacRate = (byte)v;
                v = 0; int.TryParse(arr[51], out v);
                info.Holy = (byte)v;
                v = 0; int.TryParse(arr[52], out v);
                info.Freezing = (byte)v;
                v = 0; int.TryParse(arr[53], out v);
                info.PoisonAttack = (byte)v;
                v = 0; int.TryParse(arr[54], out v);
                info.ClassBased = (byte)v;
                v = 0; int.TryParse(arr[55], out v);
                info.LevelBased = (byte)v;
                v = 0; int.TryParse(arr[56], out v);
                info.Bind = (BindMode)v;
                v = 0; int.TryParse(arr[57], out v);
                info.Reflect = (byte)v;
                v = 0; int.TryParse(arr[58], out v);
                info.HpDrainRate = (byte)v;
                v = 0; int.TryParse(arr[59], out v);
                info.Unique = (SpecialItemMode)v;
                v = 0; int.TryParse(arr[60], out v);
                info.RandomStatsId = (byte)v;
                b = false; bool.TryParse(arr[61], out b);
                info.CanMine = b;
                b = false; bool.TryParse(arr[62], out b);
                info.CanFastRun = b;
                b = false; bool.TryParse(arr[63], out b);
                info.CanAwakening = b;
                string ToolTip = "";
                for (int j=64; j< arr.Length; j++)
                {
                    if(j== arr.Length - 1)
                    {
                        ToolTip += arr[j];
                    }
                    else
                    {
                        ToolTip += arr[j]+",";
                    }
                }
                info.ToolTip = ToolTip;
                //添加
                list.Add(info);
            }
            SMain.Enqueue("检查物品总数："+list.Count);
            //开始检查
            foreach(ItemInfo _info in list)
            {
                List<ItemInfo> listdb = ItemInfo.queryByImage(_info.Image);
                if(listdb==null|| listdb.Count == 0)
                {
                    _info.SaveDB();
                    SMain.Enqueue("没有此物品：" + _info.Name);
                    continue;
                }
                //if(_info.Name=="")

                ItemInfo rinfo = null;
                foreach (ItemInfo _rinfo in listdb)
                {
                    if(_rinfo.Name == _info.Name)
                    {
                        rinfo = _rinfo;
                        break;
                    }
                }
                if (rinfo == null)
                {
                    SMain.Enqueue("没有匹配的物品：" + _info.Name+","+ _info.Image+","+ _info.RequiredClass);
                    continue;
                }
            }
        }

        //怪物检查
        public static void HandleMonster()
        {
            string filepath = "d:/怪物检查表.csv";
            string[] lines = File.ReadAllLines(filepath, EncodingType.GetType(filepath));
            List<MonsterInfo> list = new List<MonsterInfo>();
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line == null)
                {
                    continue;
                }
                string[] arr = line.Split(',');

                int v = 0;
                bool b = false;
                MonsterInfo info = new MonsterInfo();
                info.Name = arr[0];



                v = 0; int.TryParse(arr[1], out v);
                info.Image = (Monster)v;
                v = 0; int.TryParse(arr[2], out v);
                info.AI = (byte)v;
                v = 0; int.TryParse(arr[3], out v);
                info.Effect = (byte)v;
                v = 0; int.TryParse(arr[4], out v);
                info.Level = (ushort)v;
                v = 0; int.TryParse(arr[5], out v);
                info.ViewRange = (byte)v;
                v = 0; int.TryParse(arr[6], out v);
                info.HP = (uint)v;
                v = 0; int.TryParse(arr[7], out v);
                info.MinAC = (ushort)v;
                v = 0; int.TryParse(arr[8], out v);
                info.MaxAC = (ushort)v;
                v = 0; int.TryParse(arr[9], out v);
                info.MinMAC = (ushort)v;
                v = 0; int.TryParse(arr[10], out v);
                info.MaxMAC = (ushort)v;
                v = 0; int.TryParse(arr[11], out v);
                info.MinDC = (ushort)v;
                v = 0; int.TryParse(arr[12], out v);
                info.MaxDC = (ushort)v;
                v = 0; int.TryParse(arr[13], out v);
                info.MinMC = (ushort)v;
                v = 0; int.TryParse(arr[14], out v);
                info.MaxMC = (ushort)v;
                v = 0; int.TryParse(arr[15], out v);
                info.MinSC = (ushort)v;
                v = 0; int.TryParse(arr[16], out v);
                info.MaxSC = (ushort)v;
                v = 0; int.TryParse(arr[17], out v);
                info.Accuracy = (byte)v;
                v = 0; int.TryParse(arr[18], out v);
                info.Agility = (byte)v;
                v = 0; int.TryParse(arr[19], out v);
                info.Light = (byte)v;
                v = 0; int.TryParse(arr[20], out v);
                info.AttackSpeed = (ushort)v;
                v = 0; int.TryParse(arr[21], out v);
                info.MoveSpeed = (ushort)v;
                v = 0; int.TryParse(arr[22], out v);
                info.Experience = (uint)v;
                b = false; bool.TryParse(arr[23], out b);
                info.CanTame = b;
                b = false; bool.TryParse(arr[24], out b);
                info.CanPush = b;

                //添加
                list.Add(info);
            }
            SMain.Enqueue("检查怪物总数：" + list.Count);
            //开始检查
            foreach (MonsterInfo _info in list)
            {
                MonsterInfo rinfo = SMain.Envir.GetMonsterInfoByImage(_info.Image);
                if (rinfo == null)
                {
                    _info.SaveDB();
                    SMain.Enqueue("没有此怪物：" + _info.Name);
                }
            }
        }

    }
}
