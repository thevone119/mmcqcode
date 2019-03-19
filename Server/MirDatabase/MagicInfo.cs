using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Server.MirEnvir;
using S = ServerPackets;
using System.Data.SQLite;
using System.Data.Common;
using Newtonsoft.Json;

namespace Server.MirDatabase
{
    //魔法技能啊
    public class MagicInfo
    {
        public string Name;
        public Spell Spell;//主键
        public byte BaseCost, LevelCost, Icon;
        public byte Level1, Level2, Level3;
        public ushort Need1, Need2, Need3;
        public uint DelayBase = 1800, DelayReduction;
        public ushort PowerBase, PowerBonus;
        //消耗的MP能量,基础能量，额外能量
        public ushort MPowerBase, MPowerBonus;
        public float MultiplierBase = 1.0f, MultiplierBonus;
        //释放距离
        public byte Range = 9;

        public override string ToString()
        {
            return Name;
        }

        public MagicInfo()
        {

        }



        /// <summary>
        /// 加载所有的魔法技能，从数据库中加载
        /// </summary>
        /// <returns></returns>
        public static List<MagicInfo>  loadAll()
        {
            List<MagicInfo> list = new List<MagicInfo>();
            DbDataReader read = MirConfigDB.ExecuteReader("select * from MagicInfo");
            while (read.Read())
            {
                MagicInfo magic = new MagicInfo();
                if (read.IsDBNull(read.GetOrdinal("Name")))
                {
                    continue;
                }
                magic.Name = read.GetString(read.GetOrdinal("Name"));
                if (magic.Name == null)
                {
                    continue;
                }
                magic.BaseCost = read.GetByte(read.GetOrdinal("BaseCost"));
                magic.LevelCost = read.GetByte(read.GetOrdinal("LevelCost"));
                magic.Icon = read.GetByte(read.GetOrdinal("Icon"));
                magic.Level1 = read.GetByte(read.GetOrdinal("Level1"));
                magic.Level2 = read.GetByte(read.GetOrdinal("Level2"));
                magic.Level3 = read.GetByte(read.GetOrdinal("Level3"));

                magic.Need1 = (ushort)read.GetInt32(read.GetOrdinal("Need1"));
                magic.Need2 = (ushort)read.GetInt32(read.GetOrdinal("Need2"));
                magic.Need3 = (ushort)read.GetInt32(read.GetOrdinal("Need3"));

                magic.DelayBase = (uint)read.GetInt32(read.GetOrdinal("DelayBase"));
                magic.DelayReduction = (uint)read.GetInt32(read.GetOrdinal("DelayReduction"));

                magic.PowerBase = (ushort)read.GetInt32(read.GetOrdinal("PowerBase"));
                magic.PowerBonus = (ushort)read.GetInt32(read.GetOrdinal("PowerBonus"));
                magic.MPowerBase = (ushort)read.GetInt32(read.GetOrdinal("MPowerBase"));
                magic.MPowerBonus = (ushort)read.GetInt32(read.GetOrdinal("MPowerBonus"));

                magic.Range = read.GetByte(read.GetOrdinal("Range"));
                magic.Spell = (Spell)read.GetByte(read.GetOrdinal("Spell"));

                magic.MultiplierBase = read.GetFloat(read.GetOrdinal("MultiplierBase"));
                magic.MultiplierBonus = read.GetFloat(read.GetOrdinal("MultiplierBonus"));
                DBObjectUtils.updateObjState(magic, (int)magic.Spell);
                list.Add(magic);
            }
            return list;
        }

        //保存到数据库中,update
        public void SaveDB()
        {
            byte state = DBObjectUtils.ObjState(this, (int)Spell);
            if (state == 0)//没有改变
            {
                return;
            }
            List<SQLiteParameter> lp = new List<SQLiteParameter>();
            lp.Add(new SQLiteParameter("Name", Name));
            lp.Add(new SQLiteParameter("BaseCost", BaseCost));
            lp.Add(new SQLiteParameter("LevelCost", LevelCost));
            lp.Add(new SQLiteParameter("Icon", Icon));
            lp.Add(new SQLiteParameter("Level1", Level1));
            lp.Add(new SQLiteParameter("Level2", Level2));
            lp.Add(new SQLiteParameter("Level3", Level3));
            lp.Add(new SQLiteParameter("Need1", Need1));
            lp.Add(new SQLiteParameter("Need2", Need2));
            lp.Add(new SQLiteParameter("Need3", Need3));
            lp.Add(new SQLiteParameter("DelayBase", DelayBase));
            lp.Add(new SQLiteParameter("DelayReduction", DelayReduction));
            lp.Add(new SQLiteParameter("PowerBase", PowerBase));
            lp.Add(new SQLiteParameter("PowerBonus", PowerBonus));
            lp.Add(new SQLiteParameter("MPowerBase", MPowerBase));
            lp.Add(new SQLiteParameter("MPowerBonus", MPowerBonus));
            lp.Add(new SQLiteParameter("Range", Range));
            lp.Add(new SQLiteParameter("MultiplierBase", MultiplierBase));
            lp.Add(new SQLiteParameter("MultiplierBonus", MultiplierBonus));

            //执行更新
            //新增
            if (state == 1)
            {
                lp.Add(new SQLiteParameter("Spell", Spell));
                string sql = "insert into MagicInfo" + SQLiteHelper.createInsertSql(lp.ToArray()); ;
                MirConfigDB.Execute(sql, lp.ToArray());
            }
            //修改
            if (state == 2)
            {
                string sql = "update MagicInfo set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where Spell=@Spell";
                lp.Add(new SQLiteParameter("Spell", Spell));
                MirConfigDB.Execute(sql, lp.ToArray());
            }
            DBObjectUtils.updateObjState(this, (int)Spell);

        }

        //创建一个零时的魔法技能，这个是用于副本随机产生各种魔法效果的
        public UserMagic createNewMagic()
        {
            UserMagic mac = new UserMagic();
            mac.Spell = Spell;
            mac.Info = this;
            mac.IsTempSpell = true;
            return mac;
        }
    }

    //用户释放的魔法，这个是捆绑到快捷键的
    public class UserMagic
    {
        public int Index;//添加字段，这个是主键ID
        public ulong userid;//添加字段，这个是用户ID
        public Spell Spell;
        [JsonIgnore]
        public MagicInfo Info;

        public byte Level, Key;
        public ushort Experience;
        public bool IsTempSpell;
        public long CastTime;

        //添加这个字段，可以动态更改技能CD
        //更改技能CD，要记得发送给客户端变更CD，否则服务器和客户端技能CD会有差异
        public long addDelay;//增加的延时

        public UserMagic()
        {

        }

        //绑定魔法信息
        public bool BindInfo()
        {
            Info = GetMagicInfo(Spell);
            if (Info == null)
            {
                return false;
            }
            return true;
        }


        private MagicInfo GetMagicInfo(Spell spell)
        {
            for (int i = 0; i < SMain.Envir.MagicInfoList.Count; i++)
            {
                MagicInfo info = SMain.Envir.MagicInfoList[i];
                if (info.Spell != spell) continue;
                return info;
            }
            return null;
        }

        public UserMagic(Spell spell)
        {
            Spell = spell;
            
            Info = GetMagicInfo(Spell);
        }



        //这个后续作废
        public UserMagic(int back,BinaryReader reader)
        {
            Spell = (Spell) reader.ReadByte();
            Info = GetMagicInfo(Spell);

            Level = reader.ReadByte();
            Key = reader.ReadByte();
            Experience = reader.ReadUInt16();

            if (Envir.LoadVersion < 15) return;
            IsTempSpell = reader.ReadBoolean();

            if (Envir.LoadVersion < 65) return;
            CastTime = reader.ReadInt64();
        }
        //作废，不单独保存
       //根据用户ID查找用户的魔法设置
       public static List<UserMagic> loadByUserid(ulong userid)
       {
            List<UserMagic> list = new List<UserMagic>();
            DbDataReader read = MirRunDB.ExecuteReader("select * from UserMagic where userid=@userid", new SQLiteParameter("userid", userid));
            while (read.Read())
            {
                if (read.IsDBNull(read.GetOrdinal("Spell")))
                {
                    continue;
                }
                UserMagic magic = new UserMagic((Spell)read.GetByte(read.GetOrdinal("Spell")));
                magic.userid = userid;
                magic.Index = read.GetInt32(read.GetOrdinal("Idx"));
                magic.Level = read.GetByte(read.GetOrdinal("Level"));
                magic.Key = read.GetByte(read.GetOrdinal("Key"));
                magic.Experience = (ushort)read.GetInt32(read.GetOrdinal("Experience"));
                magic.CastTime = read.GetInt64(read.GetOrdinal("CastTime"));
                magic.IsTempSpell = read.GetBoolean(read.GetOrdinal("IsTempSpell"));

                DBObjectUtils.updateObjState(magic, magic.Index);
                list.Add(magic);
            }
            return list;
        }

        //后续作废
        public void Save(BinaryWriter writer)
        {
            writer.Write((byte)Spell);

            writer.Write(Level);
            writer.Write(Key);
            writer.Write(Experience);
            writer.Write(IsTempSpell);
            writer.Write(CastTime);
        }

        //作废，不单独保存
        //保存到数据库
        public void SaveDB()
        {
            byte state = DBObjectUtils.ObjState(this, Index);
            if (state == 0)//没有改变
            {
                return;
            }
            List<SQLiteParameter> lp = new List<SQLiteParameter>();
            lp.Add(new SQLiteParameter("Level", Level));
            lp.Add(new SQLiteParameter("Key", Key));
            lp.Add(new SQLiteParameter("Experience", Experience));
            lp.Add(new SQLiteParameter("IsTempSpell", IsTempSpell));
            lp.Add(new SQLiteParameter("CastTime", CastTime));
            lp.Add(new SQLiteParameter("Spell", Spell));
            lp.Add(new SQLiteParameter("userid", userid));

            //新增
            if (state == 1)
            {
                if (Index > 0)
                {
                    lp.Add(new SQLiteParameter("Idx", Index));
                }
                string sql = "insert into UserMagic" + SQLiteHelper.createInsertSql(lp.ToArray());
                MirRunDB.Execute(sql, lp.ToArray());
            }
            //修改
            if (state == 2)
            {
                string sql = "update UserMagic set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where Idx=@Idx";
                lp.Add(new SQLiteParameter("Idx", Index));
                MirRunDB.Execute(sql, lp.ToArray());
            }
            DBObjectUtils.updateObjState(this, Index);
        }

        public Packet GetInfo()
        {
            return new S.NewMagic
                {
                    Magic = CreateClientMagic()
                };
        }

        public ClientMagic CreateClientMagic()
        {
            return new ClientMagic
                {
                    Name= Info.Name,
                    Spell = Spell,
                    BaseCost = Info.BaseCost,
                    LevelCost = Info.LevelCost,
                    Icon = Info.Icon,
                    Level1 = Info.Level1,
                    Level2 = Info.Level2,
                    Level3 = Info.Level3,
                    Need1 = Info.Need1,
                    Need2 = Info.Need2,
                    Need3 = Info.Need3,
                    Level = Level,
                    Key = Key,
                    Experience = Experience,
                    IsTempSpell = IsTempSpell,
                    Delay = GetDelay(),
                    Range = Info.Range,
                    CastTime = (CastTime != 0) && (SMain.Envir.Time > CastTime)? SMain.Envir.Time - CastTime: 0
            };
        }

        public int GetDamage(int DamageBase)
        {
            return (int)((DamageBase + GetPower()) * GetMultiplier());
        }

        public float GetMultiplier()
        {
            return (Info.MultiplierBase + (Level * Info.MultiplierBonus));
        }

        public int GetPower()
        {
            return (int)Math.Round((MPower() / 4F) * (Level + 1) + DefPower());
        }
        //每级增长多少
        public int MPower()
        {
            if (Info.MPowerBonus > 0)
            {
                return RandomUtils.Next(Info.MPowerBase, Info.MPowerBonus + Info.MPowerBase);
            }
            else
                return Info.MPowerBase;
        }
        //基础多少
        public int DefPower()
        {
            if (Info.PowerBonus > 0)
            {
                return RandomUtils.Next(Info.PowerBase, Info.PowerBonus + Info.PowerBase);
            }
            else
                return Info.PowerBase;
        }

        public int GetPower(int power)
        {
            return (int)Math.Round(power / 4F * (Level + 1) + DefPower());
        }

        public long GetDelay()
        {
            return Info.DelayBase - (Level * Info.DelayReduction);
        }
    }
}
