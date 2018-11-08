using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Server.MirEnvir;
using System.Data.SQLite;

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
        public string Name = string.Empty;
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
        public MonsterInfo(BinaryReader reader)
        {
            Index = reader.ReadInt32();
            Name = reader.ReadString();

            Image = (Monster) reader.ReadUInt16();
            AI = reader.ReadByte();
            Effect = reader.ReadByte();
            if (Envir.LoadVersion < 62)
            {
                Level = (ushort)reader.ReadByte();
            }
            else
            {
                Level = reader.ReadUInt16();
            }

            ViewRange = reader.ReadByte();
            if (Envir.LoadVersion >= 3) CoolEye = reader.ReadByte();

            HP = reader.ReadUInt32();

            if (Envir.LoadVersion < 62)
            {
                MinAC = (ushort)reader.ReadByte();
                MaxAC = (ushort)reader.ReadByte();
                MinMAC = (ushort)reader.ReadByte();
                MaxMAC = (ushort)reader.ReadByte();
                MinDC = (ushort)reader.ReadByte();
                MaxDC = (ushort)reader.ReadByte();
                MinMC = (ushort)reader.ReadByte();
                MaxMC = (ushort)reader.ReadByte();
                MinSC = (ushort)reader.ReadByte();
                MaxSC = (ushort)reader.ReadByte();
            }
            else
            {
                MinAC = reader.ReadUInt16();
                MaxAC = reader.ReadUInt16();
                MinMAC = reader.ReadUInt16();
                MaxMAC = reader.ReadUInt16();
                MinDC = reader.ReadUInt16();
                MaxDC = reader.ReadUInt16();
                MinMC = reader.ReadUInt16();
                MaxMC = reader.ReadUInt16();
                MinSC = reader.ReadUInt16();
                MaxSC = reader.ReadUInt16();
            }

            Accuracy = reader.ReadByte();
            Agility = reader.ReadByte();
            Light = reader.ReadByte();

            AttackSpeed = reader.ReadUInt16();
            MoveSpeed = reader.ReadUInt16();
            Experience = reader.ReadUInt32();

            if (Envir.LoadVersion < 6)
            {
                reader.BaseStream.Seek(8, SeekOrigin.Current);

                int count = reader.ReadInt32();
                reader.BaseStream.Seek(count*12, SeekOrigin.Current);
            }

            CanPush = reader.ReadBoolean();
            CanTame = reader.ReadBoolean();

            if (Envir.LoadVersion < 18) return;
            AutoRev = reader.ReadBoolean();
            Undead = reader.ReadBoolean();
        }

        public string GameName
        {
            get { return Regex.Replace(Name, @"[\d-]", string.Empty); }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(Name);

            writer.Write((ushort) Image);
            writer.Write(AI);
            writer.Write(Effect);
            writer.Write(Level);
            writer.Write(ViewRange);
            writer.Write(CoolEye);

            writer.Write(HP);

            writer.Write(MinAC);
            writer.Write(MaxAC);
            writer.Write(MinMAC);
            writer.Write(MaxMAC);
            writer.Write(MinDC);
            writer.Write(MaxDC);
            writer.Write(MinMC);
            writer.Write(MaxMC);
            writer.Write(MinSC);
            writer.Write(MaxSC);

            writer.Write(Accuracy);
            writer.Write(Agility);
            writer.Write(Light);

            writer.Write(AttackSpeed);
            writer.Write(MoveSpeed);
            writer.Write(Experience);

            writer.Write(CanPush);
            writer.Write(CanTame);
            writer.Write(AutoRev);
            writer.Write(Undead);
            SaveDB();
        }
        
        //保存到数据库
        public void SaveDB()
        {
            StringBuilder sb = new StringBuilder();
            List<SQLiteParameter> lp = new List<SQLiteParameter>();
            sb.Append("update MonsterInfo set ");

            sb.Append(" Name=@Name, "); lp.Add(new SQLiteParameter("Name", Name));
            sb.Append(" Image=@Image, "); lp.Add(new SQLiteParameter("Image", Image));
            sb.Append(" AI=@AI, "); lp.Add(new SQLiteParameter("AI", AI));
            sb.Append(" Effect=@Effect, "); lp.Add(new SQLiteParameter("Effect", Effect));
            sb.Append(" ViewRange=@ViewRange, "); lp.Add(new SQLiteParameter("ViewRange", ViewRange));
            sb.Append(" CoolEye=@CoolEye, "); lp.Add(new SQLiteParameter("CoolEye", CoolEye));

            sb.Append(" Level=@Level, "); lp.Add(new SQLiteParameter("Level", Level));
            sb.Append(" HP=@HP, "); lp.Add(new SQLiteParameter("HP", HP));

            sb.Append(" Accuracy=@Accuracy, "); lp.Add(new SQLiteParameter("Accuracy", Accuracy));
            sb.Append(" Agility=@Agility, "); lp.Add(new SQLiteParameter("Agility", Agility));
            sb.Append(" Light=@Light, "); lp.Add(new SQLiteParameter("Light", Light));

            sb.Append(" MinAC=@MinAC, "); lp.Add(new SQLiteParameter("MinAC", MinAC));
            sb.Append(" MaxAC=@MaxAC, "); lp.Add(new SQLiteParameter("MaxAC", MaxAC));

            sb.Append(" MinMAC=@MinMAC, "); lp.Add(new SQLiteParameter("MinMAC", MinMAC));
            sb.Append(" MaxMAC=@MaxMAC, "); lp.Add(new SQLiteParameter("MaxMAC", MaxMAC));
            sb.Append(" MinDC=@MinDC, "); lp.Add(new SQLiteParameter("MinDC", MinDC));
            sb.Append(" MaxDC=@MaxDC, "); lp.Add(new SQLiteParameter("MaxDC", MaxDC));
            sb.Append(" MinMC=@MinMC, "); lp.Add(new SQLiteParameter("MinMC", MinMC));
            sb.Append(" MaxMC=@MaxMC, "); lp.Add(new SQLiteParameter("MaxMC", MaxMC));
            sb.Append(" MinMC=@MinMC, "); lp.Add(new SQLiteParameter("MinMC", MinMC));
            sb.Append(" MaxMC=@MaxMC, "); lp.Add(new SQLiteParameter("MaxMC", MaxMC));
            sb.Append(" MinSC=@MinSC, "); lp.Add(new SQLiteParameter("MinSC", MinSC));
            sb.Append(" MaxSC=@MaxSC, "); lp.Add(new SQLiteParameter("MaxSC", MaxSC));

            sb.Append(" AttackSpeed=@AttackSpeed, "); lp.Add(new SQLiteParameter("AttackSpeed", AttackSpeed));
            sb.Append(" MoveSpeed=@MoveSpeed, "); lp.Add(new SQLiteParameter("MoveSpeed", MoveSpeed));
            sb.Append(" Experience=@Experience, "); lp.Add(new SQLiteParameter("Experience", Experience));

            sb.Append(" CanPush=@CanPush, "); lp.Add(new SQLiteParameter("CanPush", CanPush));
            sb.Append(" CanTame=@CanTame, "); lp.Add(new SQLiteParameter("CanTame", CanTame));
            sb.Append(" AutoRev=@AutoRev, "); lp.Add(new SQLiteParameter("AutoRev", AutoRev));
            sb.Append(" Undead=@Undead "); lp.Add(new SQLiteParameter("Undead", Undead));

            sb.Append(" where  Idx=@Idx "); lp.Add(new SQLiteParameter("Idx", Index));
            //执行更新
            int ucount = MirConfigDB.Execute(sb.ToString(), lp.ToArray());

            //没有得更新，则执行插入
            if (ucount <= 0)
            {
                sb.Clear();

                sb.Append("insert into MonsterInfo(Idx,Name,Image,AI,Effect,Level,ViewRange,CoolEye,HP,MinAC,MaxAC,MinMAC,MaxMAC,MinDC,MaxDC,MinMC,MaxMC,MinSC,MaxSC,Accuracy,Agility,Light,AttackSpeed,MoveSpeed,Experience,CanPush,CanTame,AutoRev,Undead) values(@Idx,@Name,@Image,@AI,@Effect,@Level,@ViewRange,@CoolEye,@HP,@MinAC,@MaxAC,@MinMAC,@MaxMAC,@MinDC,@MaxDC,@MinMC,@MaxMC,@MinSC,@MaxSC,@Accuracy,@Agility,@Light,@AttackSpeed,@MoveSpeed,@Experience,@CanPush,@CanTame,@AutoRev,@Undead) ");
                //执行插入
                MirConfigDB.Execute(sb.ToString(), lp.ToArray());
            }
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
                        ";Pots + Other", string.Empty, string.Empty,
                        ";Weapons", string.Empty, string.Empty,
                        ";Armour", string.Empty, string.Empty,
                        ";Helmets", string.Empty, string.Empty,
                        ";Necklace", string.Empty, string.Empty,
                        ";Bracelets", string.Empty, string.Empty,
                        ";Rings", string.Empty, string.Empty,
                        ";Shoes", string.Empty, string.Empty,
                        ";Belts", string.Empty, string.Empty,
                        ";Stone",
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

            Drops.Sort((drop1, drop2) =>
                {
                    if (drop1.Gold > 0 && drop2.Gold == 0)
                        return 1;
                    if (drop1.Gold == 0 && drop2.Gold > 0)
                        return -1;

                    return drop1.Item.Type.CompareTo(drop2.Item.Type);
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

            info.Index = ++SMain.EditEnvir.MonsterIndex;
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
    public class DropInfo
    {
        public int Chance;//机会
        public ItemInfo Item;//物品
        public uint Gold;//黄金

        public byte Type;//类型
        public bool QuestRequired;//需要的？掉落要求么?

        public static DropInfo FromLine(string s)
        {
            string[] parts = s.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            DropInfo info = new DropInfo();

            if (!int.TryParse(parts[0].Substring(2), out info.Chance)) return null;
            if (string.Compare(parts[1], "Gold", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (parts.Length < 3) return null;
                if (!uint.TryParse(parts[2], out info.Gold) || info.Gold == 0) return null;
            }
            else
            {
                info.Item = SMain.Envir.GetItemInfo(parts[1]);
                if (info.Item == null) return null;

                if (parts.Length > 2)
                {
                    string dropRequirement = parts[2];
                    if (dropRequirement.ToUpper() == "Q") info.QuestRequired = true;
                }
            }

            return info;
        }
    }
}
