using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Server.MirEnvir;
using System.Data.SQLite;
using System.Data.Common;

namespace Server.MirDatabase
{
    /// <summary>
    /// 怪物数据，这个可能要大改？
    /// 1.不用每个怪物写一个类吧？
    /// 2.怪物数据在服务器，每种怪物应该只有一个副本，其他的都只是记录ID，位置，血量几个核心的信息而已。
    /// 3.怪物刷新等逻辑
    /// 各种属性,其实没必要那么多？先不考虑人物怪，人物怪当中人物进行处理的，因为客户端可以看到人物怪的装备的
    /// </summary>
    public class MonsterInfo
    {   
        public int Index;
        public string Name = string.Empty, Name_en= string.Empty;
        public Monster Image;
        //
        public byte AI, Effect, ViewRange = 7, CoolEye;
        //等级，这个主要作用是等级高的不允许被推动的哦
        public ushort Level;

        public uint HP;

        //准确，敏捷，光
        public byte Accuracy, Agility, Light;
        //攻击，魔法，道术等
        public ushort MinAC, MaxAC, MinMAC, MaxMAC, MinDC, MaxDC, MinMC, MaxMC, MinSC, MaxSC;
        //攻击速度，移动速度
        public ushort AttackSpeed = 2500, MoveSpeed = 1800;
        //经验
        public uint Experience;
        //怪物的掉落物品
        public List<DropInfo> Drops = new List<DropInfo>();
        //驯服，推动，自动走动？不死系？
        public bool CanTame = true, CanPush = true, AutoRev = true, Undead = false;

        //非数据库字段，是否有重生脚本(比如重生提醒？)
        public bool HasSpawnScript;
        //非数据库字段，是否有死亡脚本(比如死亡出发任务？)
        public bool HasDieScript;

        public MonsterInfo()
        {
        }

        /// <summary>
        /// 加载所有数据
        /// </summary>
        /// <returns></returns>
        public static List<MonsterInfo> loadAll()
        {
            List<MonsterInfo> list = new List<MonsterInfo>();
            DbDataReader read = MirConfigDB.ExecuteReader("select * from MonsterInfo");

            while (read.Read())
            {
                MonsterInfo obj = new MonsterInfo();
                if (read.IsDBNull(read.GetOrdinal("Name")))
                {
                    continue;
                }
                obj.Name = read.GetString(read.GetOrdinal("Name"));
                obj.Name_en = read.GetString(read.GetOrdinal("Name_en"));
                
                if (obj.Name == null)
                {
                    continue;
                }
                obj.Index = read.GetInt32(read.GetOrdinal("Idx"));

                obj.Image = (Monster)read.GetByte(read.GetOrdinal("Image"));
                obj.AI = read.GetByte(read.GetOrdinal("AI"));
                obj.Effect = read.GetByte(read.GetOrdinal("Effect"));
                obj.ViewRange = read.GetByte(read.GetOrdinal("ViewRange"));
                obj.CoolEye = read.GetByte(read.GetOrdinal("CoolEye"));
                obj.Level = (ushort)read.GetInt32(read.GetOrdinal("Level"));
                obj.HP = (uint)read.GetInt32(read.GetOrdinal("HP"));
                obj.Accuracy = read.GetByte(read.GetOrdinal("Accuracy"));
                obj.Agility = read.GetByte(read.GetOrdinal("Agility"));
                obj.Light = read.GetByte(read.GetOrdinal("Light"));

                obj.MinAC = (ushort)read.GetInt32(read.GetOrdinal("MinAC"));
                obj.MaxAC = (ushort)read.GetInt32(read.GetOrdinal("MaxAC"));

                obj.MinMAC = (ushort)read.GetInt32(read.GetOrdinal("MinMAC"));
                obj.MaxMAC = (ushort)read.GetInt32(read.GetOrdinal("MaxMAC"));

                obj.MinDC = (ushort)read.GetInt32(read.GetOrdinal("MinDC"));
                obj.MaxDC = (ushort)read.GetInt32(read.GetOrdinal("MaxDC"));

                obj.MinMC = (ushort)read.GetInt32(read.GetOrdinal("MinMC"));
                obj.MaxMC = (ushort)read.GetInt32(read.GetOrdinal("MaxMC"));

                obj.MinSC = (ushort)read.GetInt32(read.GetOrdinal("MinSC"));
                obj.MaxSC = (ushort)read.GetInt32(read.GetOrdinal("MaxSC"));

                obj.AttackSpeed = (ushort)read.GetInt32(read.GetOrdinal("AttackSpeed"));
                obj.MoveSpeed = (ushort)read.GetInt32(read.GetOrdinal("MoveSpeed"));
                obj.Experience = (ushort)read.GetInt32(read.GetOrdinal("Experience"));
                obj.CanPush = read.GetBoolean(read.GetOrdinal("CanPush"));
                obj.CanTame = read.GetBoolean(read.GetOrdinal("CanTame"));
                obj.AutoRev = read.GetBoolean(read.GetOrdinal("AutoRev"));
                obj.Undead = read.GetBoolean(read.GetOrdinal("Undead"));

                DBObjectUtils.updateObjState(obj, obj.Index);
                list.Add(obj);
            }
            return list;
        }

        //游戏中显示的名字,把数字全部去掉
        public string GameName
        {
            get { return Regex.Replace(Name, @"[\d-]", string.Empty); }
        }

       
        //保存到数据库
        public void SaveDB()
        {
            byte state = DBObjectUtils.ObjState(this, Index);
            if (state == 0)//没有改变
            {
                return;
            }
            List<SQLiteParameter> lp = new List<SQLiteParameter>();
            lp.Add(new SQLiteParameter("Name", Name));
            lp.Add(new SQLiteParameter("Image", Image));
            lp.Add(new SQLiteParameter("AI", AI));
            lp.Add(new SQLiteParameter("Effect", Effect));
            lp.Add(new SQLiteParameter("ViewRange", ViewRange));
            lp.Add(new SQLiteParameter("CoolEye", CoolEye));

            lp.Add(new SQLiteParameter("Level", Level));
            lp.Add(new SQLiteParameter("HP", HP));

            lp.Add(new SQLiteParameter("Accuracy", Accuracy));
            lp.Add(new SQLiteParameter("Agility", Agility));
            lp.Add(new SQLiteParameter("Light", Light));

            lp.Add(new SQLiteParameter("MinAC", MinAC));
            lp.Add(new SQLiteParameter("MaxAC", MaxAC));

            lp.Add(new SQLiteParameter("MinMAC", MinMAC));
            lp.Add(new SQLiteParameter("MaxMAC", MaxMAC));

            lp.Add(new SQLiteParameter("MinDC", MinDC));
            lp.Add(new SQLiteParameter("MaxDC", MaxDC));

            lp.Add(new SQLiteParameter("MinMC", MinMC));
            lp.Add(new SQLiteParameter("MaxMC", MaxMC));


            lp.Add(new SQLiteParameter("MinSC", MinSC));
            lp.Add(new SQLiteParameter("MaxSC", MaxSC));

            lp.Add(new SQLiteParameter("AttackSpeed", AttackSpeed));
            lp.Add(new SQLiteParameter("MoveSpeed", MoveSpeed));
            lp.Add(new SQLiteParameter("Experience", Experience));

            lp.Add(new SQLiteParameter("CanPush", CanPush));
            lp.Add(new SQLiteParameter("CanTame", CanTame));
            lp.Add(new SQLiteParameter("AutoRev", AutoRev));
            lp.Add(new SQLiteParameter("Undead", Undead));

      
            //新增
            if (state == 1)
            {
                if (Index > 0)
                {
                    lp.Add(new SQLiteParameter("Idx", Index));
                }
                string sql = "insert into MonsterInfo" + SQLiteHelper.createInsertSql(lp.ToArray()); ;
                MirConfigDB.Execute(sql, lp.ToArray());
            }
            //修改
            if (state == 2)
            {
                string sql = "update MonsterInfo set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where Idx=@Idx"; 
                lp.Add(new SQLiteParameter("Idx", Index));
                MirConfigDB.Execute(sql, lp.ToArray());
            }
            DBObjectUtils.updateObjState(this, Index);
        }

        //加载怪物的掉落物品数据
        public void LoadDrops()
        {
            Drops.Clear();
            string path = Path.Combine(Settings.DropPath, Name + ".txt");
            if (!File.Exists(path))
            {
                string[] contents = new[]
                    {
                        ";怪物物品爆率配置，规则如下：几率 物品（支持多个） 一次最多爆多少个 是否任务物品（Q）",
                        ";普通物品:",
                        ";1/10 物品1|物品2|物品3|(HP)DrugMedium 3",
                        ";任务物品:1/10 物品1|物品2|物品3|(HP)DrugMedium 3 Q",
                        ";1/1 物品名称 1 Q",
                    };
                
                File.WriteAllLines(path, contents);
                return;
            }

            string[] lines = File.ReadAllLines(path);

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(";") || string.IsNullOrWhiteSpace(lines[i])) continue;

                DropInfo drop = DropInfo.FromLine(lines[i]);
                if (drop == null)
                {
                    SMain.Enqueue(string.Format("Could not load Drop: {0}, Line {1}", Name, lines[i]));
                    continue;
                }
                
                Drops.Add(drop);
            }

            //排序其实没用？
            Drops.Sort((drop1, drop2) =>
                {
                    if (drop1.Gold > 0 && drop2.Gold == 0)
                        return 1;
                    if (drop1.Gold == 0 && drop2.Gold > 0)
                        return -1;

                    return 0;
                });
        }

        public static void FromText(string text)
        {
            string[] data = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (data.Length < 28) return; //28

            MonsterInfo info = new MonsterInfo {Name = data[0]};
            ushort image;
            if (!ushort.TryParse(data[1], out image)) return;
            info.Image = (Monster) image;

            if (!byte.TryParse(data[2], out info.AI)) return;
            if (!byte.TryParse(data[3], out info.Effect)) return;
            if (!ushort.TryParse(data[4], out info.Level)) return;
            if (!byte.TryParse(data[5], out info.ViewRange)) return;

            if (!uint.TryParse(data[6], out info.HP)) return;

            if (!ushort.TryParse(data[7], out info.MinAC)) return;
            if (!ushort.TryParse(data[8], out info.MaxAC)) return;
            if (!ushort.TryParse(data[9], out info.MinMAC)) return;
            if (!ushort.TryParse(data[10], out info.MaxMAC)) return;
            if (!ushort.TryParse(data[11], out info.MinDC)) return;
            if (!ushort.TryParse(data[12], out info.MaxDC)) return;
            if (!ushort.TryParse(data[13], out info.MinMC)) return;
            if (!ushort.TryParse(data[14], out info.MaxMC)) return;
            if (!ushort.TryParse(data[15], out info.MinSC)) return;
            if (!ushort.TryParse(data[16], out info.MaxSC)) return;
            if (!byte.TryParse(data[17], out info.Accuracy)) return;
            if (!byte.TryParse(data[18], out info.Agility)) return;
            if (!byte.TryParse(data[19], out info.Light)) return;

            if (!ushort.TryParse(data[20], out info.AttackSpeed)) return;
            if (!ushort.TryParse(data[21], out info.MoveSpeed)) return;

            if (!uint.TryParse(data[22], out info.Experience)) return;
            
            if (!bool.TryParse(data[23], out info.CanTame)) return;
            if (!bool.TryParse(data[24], out info.CanPush)) return;

            if (!bool.TryParse(data[25], out info.AutoRev)) return;
            if (!bool.TryParse(data[26], out info.Undead)) return;
            if (!byte.TryParse(data[27], out info.CoolEye)) return;

            //int count;

            //if (!int.TryParse(data[27], out count)) return;

            //if (28 + count * 3 > data.Length) return;

            info.Index = (int)DBObjectUtils.getObjNextId(info);
            SMain.EditEnvir.MonsterInfoList.Add(info);
        }
        public string ToText()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27}", Name, (ushort)Image, AI, Effect, Level, ViewRange,
                HP, MinAC, MaxAC, MinMAC, MaxMAC, MinDC, MaxDC, MinMC, MaxMC, MinSC, MaxSC, Accuracy, Agility, Light, AttackSpeed, MoveSpeed, Experience, CanTame, CanPush, AutoRev, Undead, CoolEye);
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Index, Name);
            //return string.Format("{0}", Name);
        }

    }

    //掉落物品几率配置
    //删掉，重新做爆率
    //爆率要改动，制定规则如下
    //目前的规则 ;Potion
    //1/1 (HP)DrugMedium Q
    //优化规则 1/1 (HP)DrugMedium|(HP)DrugMedium|(HP)DrugMedium|(HP)DrugMedium 3 Q
    public class DropInfo
    {
        public double Chance;//几率，改成double,好计算一点哦。
        public uint Gold;//黄金

        public byte Type;//类型
        public bool QuestRequired;//是否任务掉落

        //物品列表
        public List<ItemInfo> ItemList = new List<ItemInfo>();
        //最多爆多少个
        public int MaxCount=1;

        public static DropInfo FromLine(string s)
        {
            string[] parts = s.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);


            DropInfo info = new DropInfo();
            //几率解析
            if (parts[0] == null)
            {
                return null;
            }
            string[] Chances = parts[0].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if(Chances==null || Chances.Length != 2)
            {
                return null;
            }
            int c1 = 1;
            int c2 = 1;
            if (!int.TryParse(Chances[0], out c1)) return null;
            if (!int.TryParse(Chances[1], out c2)) return null;
            if (c1 == 0 || c2 == 0)
            {
                return null;
            }
            info.Chance = (c1 * 1.0) / (c2 * 1.0);

            if (string.Compare(parts[1], "金币", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (parts.Length < 3) return null;
                if (!uint.TryParse(parts[2], out info.Gold) || info.Gold == 0) return null;
            }
            else
            {
                //物品列表解析
                string its = parts[1];
                if (its == null || its.Length < 2)
                {
                    return null;
                }
                string[] its2 = its.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string itname in its2)
                {
                    if(string.IsNullOrWhiteSpace(itname))
                    {
                        continue;
                    }
                    ItemInfo _info  = ItemInfo.getItem(itname);
                    if (_info == null)
                    {
                        continue;
                    }
                    info.ItemList.Add(_info);
                }
                if (info.ItemList.Count == 0)
                {
                    return null;
                }
                //总数解析
                if (parts.Length > 2)
                {
                    if (!int.TryParse(parts[2], out info.MaxCount) || info.MaxCount <= 0) return null;
                }
                //任务解析
                if (parts.Length > 3)
                {
                    string dropRequirement = parts[3];
                    if (dropRequirement.ToUpper() == "Q") info.QuestRequired = true;
                }
            }

            return info;
        }

        public string toLine()
        {
            string line = "1/" + Chance + " ";
            if (Gold > 0)
            {
                line += "金币 " + Gold;
                return line;
            }

            foreach (ItemInfo it in ItemList)
            {
                line += it.Name + "|";
            }
            line += " ";
            line += MaxCount;
            if (QuestRequired)
            {
                line += " Q";
            }
            return line;
        }

        //是否掉落
        //DropRate：这个是外部的爆率，目前有地图爆率和玩家爆率的组合
        public bool isDrop(float DropRate = 1.0f)
        {
            double rate = Chance * DropRate * Settings.DropRate;
            if (RandomUtils.NextDouble() <= rate)
            {
                return true;
            }
            return false;
        }

        //一次爆多个出来，可能一个，也可能是多个
        public List<ItemInfo> DropItems()
        {
            List<ItemInfo> DropItems = new List<ItemInfo>();
            int DropCount = 1;
            //先计算掉落多少个
            if (MaxCount > 1)
            {
                DropCount = RandomUtils.Next(MaxCount) + 1;
            }
            for(int i=0;i< DropCount; i++)
            {
                DropItems.Add(ItemList[RandomUtils.Next(ItemList.Count)]);
            }
            return DropItems;
        }

        //随机爆一个出来
        public ItemInfo DropItem()
        {
            if (ItemList.Count == 0)
            {
                return null;
            }
            return ItemList[RandomUtils.Next(ItemList.Count)];
        }

        //掉落的金币
        //实际掉落的金币上下浮动50%
        public uint DropGold(float addGold = 0)
        {
            uint minGold = Gold - Gold / 2;
            uint maxGold = Gold + Gold / 2;
            return (uint)(RandomUtils.Next((int)minGold, (int)maxGold)+ addGold);
        }

    }
}
