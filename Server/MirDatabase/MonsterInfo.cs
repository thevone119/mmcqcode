using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Server.MirEnvir;
using System.Data.SQLite;
using System.Data.Common;
using Newtonsoft.Json;

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
        //驯服，推动，AutoRev:自动发送血量变化 ,Undead不死系(不能圣言，不能召唤，地狱雷光效果很差),CanMove是否可以移动
        public bool CanTame = true, CanPush = true, AutoRev = true, Undead = false, CanMove=true;

        //非数据库字段，是否有重生脚本(比如重生提醒？)
        public bool HasSpawnScript;
        //非数据库字段，是否有死亡脚本(比如死亡出发任务？)
        public bool HasDieScript;

        private long lastFileTime = 0;//爆率文件最后的更事件，如果发生变更，自动重新加载

        public string dropText = "";//爆率配置，如果这里配置了，就不读取文件中的配置了.

        //这里配置支持通用AI.
        public byte AttackAi=1;//采用了几种攻击方式，默认1
        public byte AttackRangeAi = 0;//采用了几种范围攻击方式，默认0

        public byte bosstype = 0;//BOSS的分类，0普通小怪，1：精英小怪（例如邪恶钳虫） 2：首领BOSS怪 3：超级BOSS怪



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
            DbDataReader read = MirConfigDB.ExecuteReader("select * from MonsterInfo where isdel=0");
            while (read.Read())
            {
                MonsterInfo obj = new MonsterInfo();
                if (read.IsDBNull(read.GetOrdinal("Name")))
                {
                    continue;
                }
                obj.Name = read.GetString(read.GetOrdinal("Name"));
                if (obj.Name == null)
                {
                    continue;
                }
                if (!read.IsDBNull(read.GetOrdinal("Name_en")))
                {
                    obj.Name_en = read.GetString(read.GetOrdinal("Name_en"));
                }
                if (!read.IsDBNull(read.GetOrdinal("dropText")))
                {
                    obj.dropText = read.GetString(read.GetOrdinal("dropText"));
                }

                
                obj.Index = read.GetInt32(read.GetOrdinal("Idx"));

                obj.Image = (Monster)read.GetInt16(read.GetOrdinal("Image"));
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
                obj.CanMove = read.GetBoolean(read.GetOrdinal("CanMove"));

                if (!read.IsDBNull(read.GetOrdinal("bosstype")))
                {
                    obj.bosstype = read.GetByte(read.GetOrdinal("bosstype"));
                }

                DBObjectUtils.updateObjState(obj, obj.Index);
                list.Add(obj);
                //是否格式化爆率文件,这个是把所有文件进行重写了
                bool Format = false;
                if (Format)
                {
                    obj.LoadDrops();
                    string path = Path.Combine(Settings.DropPath, obj.Name + "_" + obj.Index + ".txt");
                    if (File.Exists(path))
                    {
                        List<string> lines = new List<string>();
                        foreach (DropInfo di in obj.Drops)
                        {
                            lines.Add(di.toLine());
                        }
                        File.WriteAllLines(path, lines);
                    }
                }

            }
            return list;
        }

        //游戏中显示的名字,把数字全部去掉
        //如果有下划线的，也把下划线后面的去掉
        private string _gameName;
        public string GameName
        {
            get {
                if (_gameName != null)
                {
                    return _gameName;
                }
                _gameName = Regex.Replace(Name, @"[\d-]", string.Empty);
                if (_gameName.IndexOf("_") != -1)
                {
                    _gameName = _gameName.Substring(0, _gameName.IndexOf("_"));
                }
                return _gameName;
            }
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
            lp.Add(new SQLiteParameter("CanMove", CanMove));

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
            string[] lines = null;
            if (dropText != null && dropText.Length > 5)
            {
                lines = dropText.Split(new string[] { "\n" }, StringSplitOptions.None);
            }
            else
            {
                string path = Path.Combine(Settings.DropPath, Name + "_" + Index + ".txt");
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
                long LastWriteTime = File.GetLastWriteTime(path).ToBinary();

                if (LastWriteTime == lastFileTime)
                {
                    return;
                }
                if (lastFileTime != 0)
                {
                    SMain.Enqueue(string.Format("怪物爆率重载: {0}", Name));
                }
                lastFileTime = LastWriteTime;
                lines = File.ReadAllLines(path);
            }
            Drops.Clear();
            if (lines == null)
            {
                return;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(";") || string.IsNullOrWhiteSpace(lines[i])) continue;

                DropInfo drop = DropInfo.FromLine(this.Name,lines[i]);
                if (drop == null)
                {
                    SMain.Enqueue(string.Format("Could not load Drop: {0}, Line {1}", Name, lines[i]));
                    continue;
                }
                
                Drops.Add(drop);
            }

            //对爆率进行排序，金币放前面，然后是爆率高的放前面
            Drops.Sort((drop1, drop2) =>
                {
                    if (drop1.Gold > 0 && drop2.Gold == 0)
                        return -1;
                    if (drop1.Gold == 0 && drop2.Gold > 0)
                        return 1;
                    if (drop1.Chance > drop2.Chance)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                });
        }

        //这个是通用的爆率
        public void LoadCommonDrops()
        {
            bool baoyu = false;//宝玉
            bool baoshi = false;//宝石
            bool shuye = false;//书页残卷
            foreach (DropInfo di in Drops)
            {
                if (di.dropLine == null)
                {
                    continue;
                }
                if(di.dropLine.IndexOf("宝玉")!=-1)
                {
                    baoyu=true;
                }
                if (di.dropLine.IndexOf("宝石") != -1)
                {
                    baoshi = true;
                }
                if (di.dropLine.IndexOf("书页残卷") != -1)
                {
                    shuye = true;
                }
            }
            //大于1000血的都爆宝玉
            //1000-1万血，血量越高，爆率越高
            if (!baoyu && HP >= 10000)
            {
                string line = HP + "/" + 40000 + " G_宝玉";
                DropInfo di = DropInfo.FromLine(this.Name,line);
                Drops.Add(di);
            }
            //宝石爆率
            if (!baoshi)
            {
                if ( HP >= 1000 && HP < 2000)
                {
                    string line = HP + "/" + 80000 + " G_宝石1";
                    DropInfo di = DropInfo.FromLine(this.Name,line);
                    Drops.Add(di);
                }
                if (HP >= 2000 && HP < 5000)
                {
                    string line = HP + "/" + 80000 + " G_宝石1|G_宝石2";
                    DropInfo di = DropInfo.FromLine(this.Name,line);
                    Drops.Add(di);
                }
                if (HP >= 5000 && HP < 10000)
                {
                    string line = HP + "/" + 80000 + " G_宝石1|G_宝石2|G_宝石3";
                    DropInfo di = DropInfo.FromLine(this.Name,line);
                    Drops.Add(di);
                }
                if (HP >= 10000)
                {
                    string line = HP + "/" + 80000 + " G_宝石2|G_宝石3";
                    DropInfo di = DropInfo.FromLine(this.Name,line);
                    Drops.Add(di);
                }
            }
            if (!shuye && bosstype > 0 && HP>1000)
            {
                DropInfo drop5 = DropInfo.FromLine("副本_书页", String.Format("1/10 G_书页残卷"));
                drop5.Chance = HP * 1.01 / 200000;
                Drops.Add(drop5);
            }
            
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

        //创建副本,采用序列化进行克隆
        //注意，静态属性，私有属性不被克隆
        public MonsterInfo Clone()
        {
            MonsterInfo mInfo = JsonConvert.DeserializeObject<MonsterInfo>(JsonConvert.SerializeObject(this));
            //物品还是要采用引用的方式，不能用克隆的方式，否则物品会有问题
            for(int i=0;i< Drops.Count; i++)
            {
                mInfo.Drops[i].ItemList = Drops[i].ItemList;
            }
            return mInfo;
        }

    }

    //掉落物品几率配置
    //删掉，重新做爆率
    //爆率要改动，制定规则如下
    //目前的规则 ;Potion
    //1/1 (HP)DrugMedium Q
    //优化规则 1/1 (HP)DrugMedium|(HP)DrugMedium|(HP)DrugMedium|(HP)DrugMedium 3 Q
    //支持物品分组，规则：1/1 g_物品组1|g_物品组2|(g_物品组3|g_物品组4 3 Q
    //数量支持4-5这种最少爆多少个，最多爆多少个的配置
    public class DropInfo
    {
        public string Percentage;//这个是配置中的百分比
        public string dropLine;//这是原始的配置
        public double Chance;//几率，改成double,好计算一点哦。
        public uint Gold;//黄金

        public byte Type;//类型
        public bool QuestRequired;//是否任务掉落

        //物品列表
        public List<ItemInfo> ItemList = new List<ItemInfo>();
        public uint PriceCount;//总价格,针对一组爆率里有多个物品的，根据价格决定爆率，价格越低的，出得越多
        //最少爆多少个
        public int MinCount = 1;
        //最多爆多少个
        public int MaxCount = 1;
        public bool isPrice = true;//是否根据价格决定爆率

        public static DropInfo FromLine(string monname,string s)
        {
            string[] parts = s.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);


            DropInfo info = new DropInfo();
            //几率解析
            if (parts[0] == null)
            {
                return null;
            }
            info.dropLine = s;
            info.Percentage = parts[0];
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
                    //分组物品
                    if (itname.ToUpper().StartsWith("G_"))
                    {
                        string groupName = itname.Substring(2);
                        info.ItemList.AddRange(ItemInfo.queryByGroupName(groupName));
                    }
                    else
                    {
                        ItemInfo _info = ItemInfo.getItem(itname);
                        if (_info == null)
                        {
                            SMain.Enqueue(string.Format("Could not load Drop FOR ITEMNAME : {0},mob:{1}", itname, monname));
                            continue;
                        }
                        info.ItemList.Add(_info);
                    }
                }
                if (info.ItemList.Count == 0)
                {
                    return null;
                }
                //总数解析
                if (parts.Length > 2)
                {
                    if (parts[2].IndexOf("-") != -1)
                    {
                        string[] counts = parts[2].Split('-');
                        int.TryParse(counts[0], out info.MinCount);
                        int.TryParse(counts[1], out info.MaxCount);
                        if (info.MinCount <= 0 || info.MaxCount <= 0) return null;
                    }
                    else
                    {
                        if (!int.TryParse(parts[2], out info.MaxCount) || info.MaxCount <= 0) return null;
                    }
                }
                //任务解析,是否随机爆解析
                if (parts.Length > 3)
                {
                    string dropRequirement = parts[3];
                    if (dropRequirement.ToUpper() == "Q") info.QuestRequired = true;
                    if (dropRequirement.ToUpper() == "R") info.isPrice = false;
                }
                //总价格解析,价格默认在1000-1000万之间，1000万除价格，得到爆率的价格，这样价格越高，占比就会越低
                foreach(ItemInfo _info in info.ItemList)
                {
                    if (_info.DropWeight < 1000)
                    {
                        info.PriceCount += 10000000 / 1000;
                    }
                    else
                    {
                        info.PriceCount += 10000000 / _info.DropWeight;
                    }
                }
            }

            return info;
        }



        public string toLine()
        {
            string line = Percentage + " ";
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
        public bool isDrop(double DropRate = 1.0d)
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
                DropCount = RandomUtils.Next(MinCount,MaxCount+1);
            }
            for (int i=0;i< DropCount; i++)
            {
                DropItems.Add(DropItem());
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
            if (ItemList.Count == 1)
            {
                return ItemList[0];
            }
            if (!isPrice || PriceCount<10)
            {
                return ItemList[RandomUtils.Next(ItemList.Count)];
            }
            //针对多个物品，根据价格决定爆率
            int macp = RandomUtils.Next((int)PriceCount);
            uint price1 = 0;
            foreach (ItemInfo _info in ItemList)
            {
                if (_info.DropWeight < 1000)
                {
                    price1 += 10000000 / 1000;
                }
                else
                {
                    price1 += 10000000 / _info.DropWeight;
                }
                if ( macp < price1)
                {
                    return _info;
                }
            }
            //不会走到这里
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
