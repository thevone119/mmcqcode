using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using C = ClientPackets;
using S = ServerPackets;
using System.Linq;
using System.Data.SQLite;
using System.Text;
using System.Data.Common;
using Newtonsoft.Json;


//定义物品信息(这里应该分为2类，一类是装备，一类是物品)
//保存到数据库中
//装备的发展要尽量简单，参考英雄联盟的装备属性?
//装备应该尽量多变，支持不同的组合，达到不同的效果
//各属性间有相关的互斥，约束关系(比如致命节奏，重伤，)
//支持装备轮回，部分属性，只能通过轮回增加（）
//1.攻击，魔法，道术（这里应该只分2种比较好，一种是攻击，一种是魔法，道术也魔法攻击）
//2.最小攻击，最大攻击，有区间（幸运增加最大攻击几率，最大幸运7）
//3.防御，魔法防御，这个区间只是固定的，每次区个随机的值即可
//4.敏捷，精确对应躲避，和命中。敏捷+1增加10%躲避，精确+1增加10%命中(感觉可以把这2个属性去掉)
//5.暴击(最大攻击+30%)
//6.吸血(吸血装备，要尽量减少吸血，最大吸血不超过30%)         
//7.邪恶（1-5），后面可以加邪恶，邪恶的意思是，血越少，攻击越高,比如血只有10%，就是增加90%的攻击,1-5的邪恶
//物品有期限？期限怎么处理，比如时装，比如租借的物品等
public class ItemInfo
{
    public static List<ItemInfo> _listAll = new List<ItemInfo>();

    public int Index;
    public string Name = string.Empty;
    public ItemType Type;
    public ItemGrade Grade;
    public RequiredType RequiredType = RequiredType.Level;
    public RequiredClass RequiredClass = RequiredClass.None;
    public RequiredGender RequiredGender = RequiredGender.None;
    public ItemSet Set;



    public short Shape;//形状
    //重量，发光，需要的数量（可能需要等级，可能需要攻击，可能需要魔法,具体看RequiredType的定义了）
    public byte Weight, Light, RequiredAmount;
    //图片，基础持久
    public ushort Image, Durability;
    //价格，可堆叠数,默认是1，就是不可堆叠的
    public uint Price, StackSize = 1;
    //基础属性,Agility：敏捷,Accuracy精确
    public byte MinAC, MaxAC, MinMAC, MaxMAC, MinDC, MaxDC, MinMC, MaxMC, MinSC, MaxSC, Accuracy, Agility;
    //血，蓝,增加的血，蓝，如果是药品则是喝药增加的血，蓝
    public ushort HP, MP;
    //攻击速度，幸运
    public sbyte AttackSpeed, Luck;
    //背包重量，腕力(武器)，穿戴重量
    public byte BagWeight, HandWeight, WearWeight;

    public bool StartItem;//是否开始赠送的装备，其实开始赠送不应该放这里
    public byte Effect;//效果

    public byte Strong;//强度，降持久的时候，每次降的持久会和这个数比较，一般都是0，如果是1-3
    //MagicResist:魔法躲避？PoisonResist：中毒躲避？HealthRecovery恢复，SpellRecovery技能恢复，PoisonRecovery：中毒恢复，HPrate：HP增加百分比，MPrate：Mp增加百分百
    public byte MagicResist, PoisonResist, HealthRecovery, SpellRecovery, PoisonRecovery, HPrate, MPrate;

    //暴击几率，暴击伤害
    public byte CriticalRate, CriticalDamage;

    public byte bools;//这一个决定下面6个参数 NeedIdentify,ShowGroupPickup,ClassBased,LevelBased.CanMine,GlobalDropNotify
    public bool NeedIdentify, ShowGroupPickup, GlobalDropNotify;
    public bool ClassBased;
    public bool LevelBased;
    public bool CanMine;
    //是否可以助跑，所有鞋子都可以助跑
    public bool CanFastRun;
    //是否可以觉醒(感觉没用)
    public bool CanAwakening;
    //最大防御几率，最大魔防御几率，神圣，冰冻，毒术攻击，HP消耗几率
    public byte MaxAcRate, MaxMacRate, Holy, Freezing, PoisonAttack, HpDrainRate;

    //各种绑定属性，不能丢弃，死亡不掉落，背包不掉落，下线销毁，不能交易，不能出售，不能存储，不能升级，不能修理，不能特殊修理等
    public BindMode Bind = BindMode.none;
    //这个是什么？也是发光么？
    public byte Reflect;
    //特殊属性
    public SpecialItemMode Unique = SpecialItemMode.None;
    //装备的极品几率
    public byte RandomStatsId;
    public RandomItemStat RandomStats;
    //装备说明，提示
    public string ToolTip = string.Empty;


    public bool IsConsumable
    {
        get { return Type == ItemType.Potion || Type == ItemType.Scroll || Type == ItemType.Food || Type == ItemType.Transform || Type == ItemType.Script; }
    }

    public string FriendlyName
    {
        get
        {
            string temp = Name;
            temp = Regex.Replace(temp, @"\d+$", string.Empty); //hides end numbers
            temp = Regex.Replace(temp, @"\[[^]]*\]", string.Empty); //hides square brackets

            return temp;
        }
    }

    public ItemInfo()
    {
    }
    public ItemInfo(BinaryReader reader, int version = int.MaxValue, int Customversion = int.MaxValue)
    {
        Index = reader.ReadInt32();
        Name = reader.ReadString();
        Type = (ItemType)reader.ReadByte();
        if (version >= 40) Grade = (ItemGrade)reader.ReadByte();
        RequiredType = (RequiredType)reader.ReadByte();
        RequiredClass = (RequiredClass)reader.ReadByte();
        RequiredGender = (RequiredGender)reader.ReadByte();
        if (version >= 17) Set = (ItemSet)reader.ReadByte();

        Shape = version >= 30 ? reader.ReadInt16() : reader.ReadSByte();
        Weight = reader.ReadByte();
        Light = reader.ReadByte();
        RequiredAmount = reader.ReadByte();

        Image = reader.ReadUInt16();
        Durability = reader.ReadUInt16();

        StackSize = reader.ReadUInt32();
        Price = reader.ReadUInt32();

        MinAC = reader.ReadByte();
        MaxAC = reader.ReadByte();
        MinMAC = reader.ReadByte();
        MaxMAC = reader.ReadByte();
        MinDC = reader.ReadByte();
        MaxDC = reader.ReadByte();
        MinMC = reader.ReadByte();
        MaxMC = reader.ReadByte();
        MinSC = reader.ReadByte();
        MaxSC = reader.ReadByte();
        if (version < 25)
        {
            HP = reader.ReadByte();
            MP = reader.ReadByte();
        }
        else
        {
            HP = reader.ReadUInt16();
            MP = reader.ReadUInt16();
        }
        Accuracy = reader.ReadByte();
        Agility = reader.ReadByte();

        Luck = reader.ReadSByte();
        AttackSpeed = reader.ReadSByte();

        StartItem = reader.ReadBoolean();

        BagWeight = reader.ReadByte();
        HandWeight = reader.ReadByte();
        WearWeight = reader.ReadByte();

        if (version >= 9) Effect = reader.ReadByte();
        if (version >= 20)
        {
            Strong = reader.ReadByte();
            MagicResist = reader.ReadByte();
            PoisonResist = reader.ReadByte();
            HealthRecovery = reader.ReadByte();
            SpellRecovery = reader.ReadByte();
            PoisonRecovery = reader.ReadByte();
            HPrate = reader.ReadByte();
            MPrate = reader.ReadByte();
            CriticalRate = reader.ReadByte();
            CriticalDamage = reader.ReadByte();
            bools = reader.ReadByte();
            NeedIdentify = (bools & 0x01) == 0x01;
            ShowGroupPickup = (bools & 0x02) == 0x02;
            ClassBased = (bools & 0x04) == 0x04;
            LevelBased = (bools & 0x08) == 0x08;
            CanMine = (bools & 0x10) == 0x10;

            if (version >= 77)
                GlobalDropNotify = (bools & 0x20) == 0x20;

            MaxAcRate = reader.ReadByte();
            MaxMacRate = reader.ReadByte();
            Holy = reader.ReadByte();
            Freezing = reader.ReadByte();
            PoisonAttack = reader.ReadByte();
            if (version < 55)
            {
                Bind = (BindMode)reader.ReadByte();
            }
            else
            {
                Bind = (BindMode)reader.ReadInt16();
            }

        }
        if (version >= 21)
        {
            Reflect = reader.ReadByte();
            HpDrainRate = reader.ReadByte();
            Unique = (SpecialItemMode)reader.ReadInt16();
        }
        if (version >= 24)
        {
            RandomStatsId = reader.ReadByte();
        }
        else
        {
            RandomStatsId = 255;
            if ((Type == ItemType.Weapon) || (Type == ItemType.Armour) || (Type == ItemType.Helmet) || (Type == ItemType.Necklace) || (Type == ItemType.Bracelet) || (Type == ItemType.Ring) || (Type == ItemType.Mount))
                RandomStatsId = (byte)Type;
            if ((Type == ItemType.Belt) || (Type == ItemType.Boots))
                RandomStatsId = 7;
        }

        if (version >= 40) CanFastRun = reader.ReadBoolean();

        if (version >= 41)
        {
            CanAwakening = reader.ReadBoolean();
            bool isTooltip = reader.ReadBoolean();
            if (isTooltip)
                ToolTip = reader.ReadString();
        }
        if (version < 70) //before db version 70 all specialitems had wedding rings disabled, after that it became a server option
        {
            if ((Type == ItemType.Ring) && (Unique != SpecialItemMode.None))
                Bind |= BindMode.NoWeddingRing;
        }
    }

    //根据ID查找物品
    //后面如果使用得比较多，则改成hash结构
    public static ItemInfo getItem(int idx)
    {
        if (_listAll == null || _listAll.Count == 0)
        {
            loadAll();
        }
        for(int i=0;i< _listAll.Count; i++)
        {
            if(_listAll[i].Index== idx)
            {
                return _listAll[i];
            }
        }
        return null;
    }

    //通过名称取物品，不区分大小写
    public static ItemInfo getItem(string name)
    {
        if (_listAll == null || _listAll.Count == 0)
        {
            loadAll();
        }
        for (int i = 0; i < _listAll.Count; i++)
        {
            ItemInfo info = _listAll[i];
            if (String.Compare(info.Name.Replace(" ", ""), name, StringComparison.OrdinalIgnoreCase) != 0) continue;
            return info;
        }
        return null;
    }

    /// <summary>
    /// 加载所有数据
    /// </summary>
    /// <returns></returns>
    public static List<ItemInfo> loadAll()
    {
        if(_listAll!=null&& _listAll.Count > 0)
        {
            return _listAll;
        }
        DbDataReader read = MirConfigDB.ExecuteReader("select * from ItemInfo");

        while (read.Read())
        {
            ItemInfo obj = new ItemInfo();
            if (read.IsDBNull(read.GetOrdinal("Name")))
            {
                continue;
            }
            obj.Name = read.GetString(read.GetOrdinal("Name"));
            if (obj.Name == null)
            {
                continue;
            }
            obj.Index = read.GetInt32(read.GetOrdinal("Idx"));


            obj.Type = (ItemType)read.GetByte(read.GetOrdinal("Type"));
            obj.Grade = (ItemGrade)read.GetByte(read.GetOrdinal("Grade"));
            obj.RequiredType = (RequiredType)read.GetByte(read.GetOrdinal("RequiredType"));
            obj.RequiredClass = (RequiredClass)read.GetByte(read.GetOrdinal("RequiredClass"));
            obj.RequiredGender = (RequiredGender)read.GetByte(read.GetOrdinal("RequiredGender"));
            obj.Set = (ItemSet)read.GetByte(read.GetOrdinal("ItemSet"));
            obj.Shape = (short)read.GetInt32(read.GetOrdinal("Shape"));
            obj.Weight = read.GetByte(read.GetOrdinal("Weight"));
            obj.Light = read.GetByte(read.GetOrdinal("Light"));
            obj.RequiredAmount = read.GetByte(read.GetOrdinal("RequiredAmount"));

            obj.Image = (ushort)read.GetInt32(read.GetOrdinal("Image"));
            obj.Durability = (ushort)read.GetInt32(read.GetOrdinal("Durability"));
            obj.StackSize = (uint)read.GetInt32(read.GetOrdinal("StackSize"));
            obj.Price = (uint)read.GetInt32(read.GetOrdinal("Price"));
       
            obj.MinAC = read.GetByte(read.GetOrdinal("MinAC"));
            obj.MaxAC = read.GetByte(read.GetOrdinal("MaxAC"));

            obj.MinMAC = read.GetByte(read.GetOrdinal("MinMAC"));
            obj.MaxMAC = read.GetByte(read.GetOrdinal("MaxMAC"));

            obj.MinDC = read.GetByte(read.GetOrdinal("MinDC"));
            obj.MaxDC = read.GetByte(read.GetOrdinal("MaxDC"));

            obj.MinMC = read.GetByte(read.GetOrdinal("MinMC"));
            obj.MaxMC = read.GetByte(read.GetOrdinal("MaxMC"));

            obj.MinSC = read.GetByte(read.GetOrdinal("MinSC"));
            obj.MaxSC = read.GetByte(read.GetOrdinal("MaxSC"));

            obj.HP = (ushort)read.GetInt32(read.GetOrdinal("HP"));
            obj.MP = (ushort)read.GetInt32(read.GetOrdinal("MP"));

            obj.Accuracy = read.GetByte(read.GetOrdinal("Accuracy"));
            obj.Agility = read.GetByte(read.GetOrdinal("Agility"));
            obj.Luck = (sbyte)read.GetByte(read.GetOrdinal("Luck"));
            obj.AttackSpeed = (sbyte)read.GetByte(read.GetOrdinal("AttackSpeed"));
            obj.StartItem = read.GetBoolean(read.GetOrdinal("StartItem"));
            obj.BagWeight = read.GetByte(read.GetOrdinal("BagWeight"));
            obj.HandWeight = read.GetByte(read.GetOrdinal("HandWeight"));
            obj.WearWeight = read.GetByte(read.GetOrdinal("WearWeight"));
            obj.Effect = read.GetByte(read.GetOrdinal("Effect"));
            obj.Strong = read.GetByte(read.GetOrdinal("Strong"));
            obj.MagicResist = read.GetByte(read.GetOrdinal("MagicResist"));
            obj.PoisonResist = read.GetByte(read.GetOrdinal("PoisonResist"));
            obj.HealthRecovery = read.GetByte(read.GetOrdinal("HealthRecovery"));
            obj.SpellRecovery = read.GetByte(read.GetOrdinal("SpellRecovery"));
            obj.PoisonRecovery = read.GetByte(read.GetOrdinal("PoisonRecovery"));
            obj.HPrate = read.GetByte(read.GetOrdinal("HPrate"));
            obj.MPrate = read.GetByte(read.GetOrdinal("MPrate"));
            obj.CriticalRate = read.GetByte(read.GetOrdinal("CriticalRate"));
            obj.CriticalDamage = read.GetByte(read.GetOrdinal("CriticalDamage"));
            obj.bools = read.GetByte(read.GetOrdinal("bools"));
            obj.MaxAcRate = read.GetByte(read.GetOrdinal("MaxAcRate"));
            obj.MaxMacRate = read.GetByte(read.GetOrdinal("MaxMacRate"));
            obj.Holy = read.GetByte(read.GetOrdinal("Holy"));
            obj.Freezing = read.GetByte(read.GetOrdinal("Freezing"));
            obj.PoisonAttack = read.GetByte(read.GetOrdinal("PoisonAttack"));
            obj.Bind = (BindMode)read.GetByte(read.GetOrdinal("Bind"));
            obj.Reflect = read.GetByte(read.GetOrdinal("Reflect"));
            obj.HpDrainRate = read.GetByte(read.GetOrdinal("HpDrainRate"));
            obj.Unique = (SpecialItemMode)read.GetByte(read.GetOrdinal("SpecialItemMode"));
            obj.RandomStatsId = read.GetByte(read.GetOrdinal("RandomStatsId"));
            obj.CanFastRun = read.GetBoolean(read.GetOrdinal("CanFastRun"));
            obj.CanAwakening = read.GetBoolean(read.GetOrdinal("CanAwakening"));
            obj.ToolTip = read.GetString(read.GetOrdinal("ToolTip"));

            DBObjectUtils.updateObjState(obj, obj.Index);
            _listAll.Add(obj);
        }
        return _listAll;
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(Name);
        writer.Write((byte)Type);
        writer.Write((byte)Grade);
        writer.Write((byte)RequiredType);
        writer.Write((byte)RequiredClass);
        writer.Write((byte)RequiredGender);
        writer.Write((byte)Set);

        writer.Write(Shape);
        writer.Write(Weight);
        writer.Write(Light);
        writer.Write(RequiredAmount);

        writer.Write(Image);
        writer.Write(Durability);

        writer.Write(StackSize);
        writer.Write(Price);

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
        writer.Write(HP);
        writer.Write(MP);
        writer.Write(Accuracy);
        writer.Write(Agility);

        writer.Write(Luck);
        writer.Write(AttackSpeed);

        writer.Write(StartItem);

        writer.Write(BagWeight);
        writer.Write(HandWeight);
        writer.Write(WearWeight);

        writer.Write(Effect);
        writer.Write(Strong);
        writer.Write(MagicResist);
        writer.Write(PoisonResist);
        writer.Write(HealthRecovery);
        writer.Write(SpellRecovery);
        writer.Write(PoisonRecovery);
        writer.Write(HPrate);
        writer.Write(MPrate);
        writer.Write(CriticalRate);
        writer.Write(CriticalDamage);
        writer.Write(bools);
        writer.Write(MaxAcRate);
        writer.Write(MaxMacRate);
        writer.Write(Holy);
        writer.Write(Freezing);
        writer.Write(PoisonAttack);
        writer.Write((short)Bind);
        writer.Write(Reflect);
        writer.Write(HpDrainRate);
        writer.Write((short)Unique);
        writer.Write(RandomStatsId);
        writer.Write(CanFastRun);
        writer.Write(CanAwakening);
        writer.Write(ToolTip != null);
        if (ToolTip != null)
            writer.Write(ToolTip);

        SaveDB();
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
        lp.Add(new SQLiteParameter("Type", Type));
        lp.Add(new SQLiteParameter("Grade", Grade));
        lp.Add(new SQLiteParameter("RequiredType", RequiredType));
        lp.Add(new SQLiteParameter("RequiredClass", RequiredClass));
        lp.Add(new SQLiteParameter("RequiredGender", RequiredGender));
        lp.Add(new SQLiteParameter("ItemSet", Set));
        lp.Add(new SQLiteParameter("Shape", Shape));
        lp.Add(new SQLiteParameter("Weight", Weight));
        lp.Add(new SQLiteParameter("Light", Light));
        lp.Add(new SQLiteParameter("RequiredAmount", RequiredAmount));
        lp.Add(new SQLiteParameter("Image", Image));
        lp.Add(new SQLiteParameter("Durability", Durability));
        lp.Add(new SQLiteParameter("StackSize", StackSize));
        lp.Add(new SQLiteParameter("Price", Price));
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
        lp.Add(new SQLiteParameter("HP", HP));
        lp.Add(new SQLiteParameter("MP", MP));
        lp.Add(new SQLiteParameter("Accuracy", Accuracy));
        lp.Add(new SQLiteParameter("Agility", Agility));
        lp.Add(new SQLiteParameter("Luck", Luck));
        lp.Add(new SQLiteParameter("AttackSpeed", AttackSpeed));
        lp.Add(new SQLiteParameter("StartItem", StartItem));
        lp.Add(new SQLiteParameter("BagWeight", BagWeight));
        lp.Add(new SQLiteParameter("HandWeight", HandWeight));
        lp.Add(new SQLiteParameter("WearWeight", WearWeight));
        lp.Add(new SQLiteParameter("Effect", Effect));
        lp.Add(new SQLiteParameter("Strong", Strong));
        lp.Add(new SQLiteParameter("MagicResist", MagicResist));
        lp.Add(new SQLiteParameter("PoisonResist", PoisonResist));
        lp.Add(new SQLiteParameter("HealthRecovery", HealthRecovery));
        lp.Add(new SQLiteParameter("SpellRecovery", SpellRecovery));
        lp.Add(new SQLiteParameter("PoisonRecovery", PoisonRecovery));
        lp.Add(new SQLiteParameter("HPrate", HPrate));
        lp.Add(new SQLiteParameter("MPrate", MPrate));
        lp.Add(new SQLiteParameter("CriticalRate", CriticalRate));
        lp.Add(new SQLiteParameter("CriticalDamage", CriticalDamage));
        lp.Add(new SQLiteParameter("bools", bools));
        lp.Add(new SQLiteParameter("MaxAcRate", MaxAcRate));
        lp.Add(new SQLiteParameter("MaxMacRate", MaxMacRate));
        lp.Add(new SQLiteParameter("Holy", Holy));
        lp.Add(new SQLiteParameter("Freezing", Freezing));
        lp.Add(new SQLiteParameter("PoisonAttack", PoisonAttack));
        lp.Add(new SQLiteParameter("Bind", Bind));
        lp.Add(new SQLiteParameter("Reflect", Reflect));
        lp.Add(new SQLiteParameter("HpDrainRate", HpDrainRate));
        lp.Add(new SQLiteParameter("SpecialItemMode", Unique));
        lp.Add(new SQLiteParameter("RandomStatsId", RandomStatsId));
        lp.Add(new SQLiteParameter("CanFastRun", CanFastRun));
        lp.Add(new SQLiteParameter("CanAwakening", CanAwakening));
        lp.Add(new SQLiteParameter("ToolTip", ToolTip));

        //新增
        if (state == 1)
        {
            if (Index > 0)
            {
                lp.Add(new SQLiteParameter("Idx", Index));
            }
            string sql = "insert into ItemInfo" + SQLiteHelper.createInsertSql(lp.ToArray()); ;
            MirConfigDB.Execute(sql, lp.ToArray());
        }
        //修改
        if (state == 2)
        {
            string sql = "update ItemInfo set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where Idx=@Idx";
            lp.Add(new SQLiteParameter("Idx", Index));
            MirConfigDB.Execute(sql, lp.ToArray());
        }
        DBObjectUtils.updateObjState(this, Index);

    }

    public static ItemInfo FromText(string text)
    {
        string[] data = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        if (data.Length < 33) return null;

        ItemInfo info = new ItemInfo { Name = data[0] };

        if (!Enum.TryParse(data[1], out info.Type)) return null;
        if (!Enum.TryParse(data[2], out info.Grade)) return null;
        if (!Enum.TryParse(data[3], out info.RequiredType)) return null;
        if (!Enum.TryParse(data[4], out info.RequiredClass)) return null;
        if (!Enum.TryParse(data[5], out info.RequiredGender)) return null;
        if (!Enum.TryParse(data[6], out info.Set)) return null;
        if (!short.TryParse(data[7], out info.Shape)) return null;

        if (!byte.TryParse(data[8], out info.Weight)) return null;
        if (!byte.TryParse(data[9], out info.Light)) return null;
        if (!byte.TryParse(data[10], out info.RequiredAmount)) return null;

        if (!byte.TryParse(data[11], out info.MinAC)) return null;
        if (!byte.TryParse(data[12], out info.MaxAC)) return null;
        if (!byte.TryParse(data[13], out info.MinMAC)) return null;
        if (!byte.TryParse(data[14], out info.MaxMAC)) return null;
        if (!byte.TryParse(data[15], out info.MinDC)) return null;
        if (!byte.TryParse(data[16], out info.MaxDC)) return null;
        if (!byte.TryParse(data[17], out info.MinMC)) return null;
        if (!byte.TryParse(data[18], out info.MaxMC)) return null;
        if (!byte.TryParse(data[19], out info.MinSC)) return null;
        if (!byte.TryParse(data[20], out info.MaxSC)) return null;
        if (!byte.TryParse(data[21], out info.Accuracy)) return null;
        if (!byte.TryParse(data[22], out info.Agility)) return null;
        if (!ushort.TryParse(data[23], out info.HP)) return null;
        if (!ushort.TryParse(data[24], out info.MP)) return null;

        if (!sbyte.TryParse(data[25], out info.AttackSpeed)) return null;
        if (!sbyte.TryParse(data[26], out info.Luck)) return null;

        if (!byte.TryParse(data[27], out info.BagWeight)) return null;

        if (!byte.TryParse(data[28], out info.HandWeight)) return null;
        if (!byte.TryParse(data[29], out info.WearWeight)) return null;

        if (!bool.TryParse(data[30], out info.StartItem)) return null;

        if (!ushort.TryParse(data[31], out info.Image)) return null;
        if (!ushort.TryParse(data[32], out info.Durability)) return null;
        if (!uint.TryParse(data[33], out info.Price)) return null;
        if (!uint.TryParse(data[34], out info.StackSize)) return null;
        if (!byte.TryParse(data[35], out info.Effect)) return null;

        if (!byte.TryParse(data[36], out info.Strong)) return null;
        if (!byte.TryParse(data[37], out info.MagicResist)) return null;
        if (!byte.TryParse(data[38], out info.PoisonResist)) return null;
        if (!byte.TryParse(data[39], out info.HealthRecovery)) return null;
        if (!byte.TryParse(data[40], out info.SpellRecovery)) return null;
        if (!byte.TryParse(data[41], out info.PoisonRecovery)) return null;
        if (!byte.TryParse(data[42], out info.HPrate)) return null;
        if (!byte.TryParse(data[43], out info.MPrate)) return null;
        if (!byte.TryParse(data[44], out info.CriticalRate)) return null;
        if (!byte.TryParse(data[45], out info.CriticalDamage)) return null;
        if (!bool.TryParse(data[46], out info.NeedIdentify)) return null;
        if (!bool.TryParse(data[47], out info.ShowGroupPickup)) return null;
        if (!byte.TryParse(data[48], out info.MaxAcRate)) return null;
        if (!byte.TryParse(data[49], out info.MaxMacRate)) return null;
        if (!byte.TryParse(data[50], out info.Holy)) return null;
        if (!byte.TryParse(data[51], out info.Freezing)) return null;
        if (!byte.TryParse(data[52], out info.PoisonAttack)) return null;
        if (!bool.TryParse(data[53], out info.ClassBased)) return null;
        if (!bool.TryParse(data[54], out info.LevelBased)) return null;
        if (!Enum.TryParse(data[55], out info.Bind)) return null;
        if (!byte.TryParse(data[56], out info.Reflect)) return null;
        if (!byte.TryParse(data[57], out info.HpDrainRate)) return null;
        if (!Enum.TryParse(data[58], out info.Unique)) return null;
        if (!byte.TryParse(data[59], out info.RandomStatsId)) return null;
        if (!bool.TryParse(data[60], out info.CanMine)) return null;
        if (!bool.TryParse(data[61], out info.CanFastRun)) return null;
        if (!bool.TryParse(data[62], out info.CanAwakening)) return null;
        if (data[63] == "-")
            info.ToolTip = "";
        else
        {
            info.ToolTip = data[63];
            info.ToolTip = info.ToolTip.Replace("&^&", "\r\n");
        }

        return info;

    }

    public string ToText()
    {
        string TransToolTip = ToolTip;
        int length = TransToolTip.Length;

        if (TransToolTip == null || TransToolTip.Length == 0)
        {
            TransToolTip = "-";
        }
        else
        {
            TransToolTip = TransToolTip.Replace("\r\n", "&^&");
        }

        return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26}," +
                             "{27},{28},{29},{30},{31},{32},{33},{34},{35},{36},{37},{38},{39},{40},{41},{42},{43},{44},{45},{46},{47},{48},{49},{50},{51}," +
                             "{52},{53},{54},{55},{56},{57},{58},{59},{60},{61},{62},{63}",
            Name, (byte)Type, (byte)Grade, (byte)RequiredType, (byte)RequiredClass, (byte)RequiredGender, (byte)Set, Shape, Weight, Light, RequiredAmount, MinAC, MaxAC, MinMAC, MaxMAC, MinDC, MaxDC,
            MinMC, MaxMC, MinSC, MaxSC, Accuracy, Agility, HP, MP, AttackSpeed, Luck, BagWeight, HandWeight, WearWeight, StartItem, Image, Durability, Price,
            StackSize, Effect, Strong, MagicResist, PoisonResist, HealthRecovery, SpellRecovery, PoisonRecovery, HPrate, MPrate, CriticalRate, CriticalDamage, NeedIdentify,
            ShowGroupPickup, MaxAcRate, MaxMacRate, Holy, Freezing, PoisonAttack, ClassBased, LevelBased, (short)Bind, Reflect, HpDrainRate, (short)Unique,
            RandomStatsId, CanMine, CanFastRun, CanAwakening, TransToolTip);
    }

    public override string ToString()
    {
        return string.Format("{0}: {1}", Index, Name);
    }

    //掉落物品
    public UserItem CreateDropItem()
    {
        UserItem item = new UserItem(this)
        {
            UniqueID = (ulong)UniqueKeyHelper.UniqueNext(),
            MaxDura = Durability,
            CurrentDura = (ushort)Math.Min(Durability, RandomUtils.Next(Durability) + 1000)
        };

        item.UpgradeItem();

        item.UpdateItemExpiry();

        if (!NeedIdentify) item.Identified = true;
        return item;
    }

    //新鲜的物品
    public UserItem CreateFreshItem()
    {
        UserItem item = new UserItem(this)
        {
            UniqueID = (ulong)UniqueKeyHelper.UniqueNext(),
            CurrentDura = Durability,
            MaxDura = Durability
        };

        item.UpdateItemExpiry();

        return item;
    }

    //商店物品
    public UserItem CreateShopItem()
    {
        UserItem item = new UserItem(this)
        {
            UniqueID = (ulong)UniqueKeyHelper.UniqueNext(),
            CurrentDura = Durability,
            MaxDura = Durability,
        };
        return item;
    }


    /// <summary>
    ///不同等级名称的颜色,这个就放物品里比较好吧？坑死咩。
    /// </summary>
    /// <param name="drops">是否是掉在地上的，地上的颜色和包裹栏的颜色可能不一样</param>
    /// <returns></returns>
    public Color getNameColor(bool drops = false)
    {
        switch (Grade)
        {
            case ItemGrade.None://白色
                return Color.White;
            case ItemGrade.Common://白色
                if (drops)
                {
                    return Color.White;
                }
                return Color.Yellow;
            case ItemGrade.Rare:
                return Color.DeepSkyBlue;//蓝色
            case ItemGrade.Legendary:
                return Color.DarkOrange;//橙色
            case ItemGrade.Mythical:
                return Color.DarkViolet;//紫色
            default:
                return Color.White;
        }
    }
}

/// <summary>
/// 这个是用户的物品，需要长期保存的
/// 
/// </summary>
public class UserItem
{
    //这里2个ID，一个是地图上的唯一ID，多次重启可能会重复？
    public ulong UniqueID;
    //这个是物品引用的ID
    public int ItemIndex;
    //这个是具体的物品的引用
    [JsonIgnore]
    public ItemInfo Info;
    //持久是每个装备都不一样的属性，当前持久，和最大持久放在具体的装备上
    public ushort CurrentDura, MaxDura;
    //当前数量，最大数量，这个是打包的
    public uint Count = 1, GemCount = 0;
    //这里是增加的属性
    public byte AC, MAC, DC, MC, SC, Accuracy, Agility, HP, MP, Strong, MagicResist, PoisonResist, HealthRecovery, ManaRecovery, PoisonRecovery, CriticalRate, CriticalDamage, Freezing, PoisonAttack;
    public sbyte AttackSpeed, Luck;

    public RefinedValue RefinedValue = RefinedValue.None;
    public byte RefineAdded = 0;

    public bool DuraChanged;
    public long SoulBoundId = -1;
    public bool Identified = false;
    public bool Cursed = false;

    public long WeddingRing = -1;

    public UserItem[] Slots = new UserItem[5];

    public DateTime BuybackExpiryDate;

    public ExpireInfo ExpireInfo;//到期
    public RentalInformation RentalInformation;//租借信息

    public Awake Awake = new Awake();

    //这个代表物品是极品，加了属性的
    public bool IsAdded
    {
        get
        {
            return AC != 0 || MAC != 0 || DC != 0 || MC != 0 || SC != 0 || Accuracy != 0 || Agility != 0 || HP != 0 || MP != 0 || AttackSpeed != 0 || Luck != 0 || Strong != 0 || MagicResist != 0 || PoisonResist != 0 ||
                HealthRecovery != 0 || ManaRecovery != 0 || PoisonRecovery != 0 || CriticalRate != 0 || CriticalDamage != 0 || Freezing != 0 || PoisonAttack != 0;
        }
    }

    public uint Weight
    {
        get { return Info.Type == ItemType.Amulet ? Info.Weight : Info.Weight * Count; }
    }

    public string Name
    {
        get { return Count > 1 ? string.Format("{0} ({1})", Info.Name, Count) : Info.Name; }
    }

    public string FriendlyName
    {
        get { return Count > 1 ? string.Format("{0} ({1})", Info.FriendlyName, Count) : Info.FriendlyName; }
    }

 
    public UserItem()
    {

    }

    public UserItem(ItemInfo info)
    {
        SoulBoundId = -1;
        ItemIndex = info.Index;
        Info = info;

        SetSlotSize();
    }
    public UserItem(BinaryReader reader, int version = int.MaxValue, int Customversion = int.MaxValue)
    {
        UniqueID = reader.ReadUInt64();
        ItemIndex = reader.ReadInt32();

        CurrentDura = reader.ReadUInt16();
        MaxDura = reader.ReadUInt16();

        Count = reader.ReadUInt32();

        AC = reader.ReadByte();
        MAC = reader.ReadByte();
        DC = reader.ReadByte();
        MC = reader.ReadByte();
        SC = reader.ReadByte();

        Accuracy = reader.ReadByte();
        Agility = reader.ReadByte();
        HP = reader.ReadByte();
        MP = reader.ReadByte();

        AttackSpeed = reader.ReadSByte();
        Luck = reader.ReadSByte();

        if (version <= 19) return;
        //这里要改
        SoulBoundId = reader.ReadInt64();
        byte Bools = reader.ReadByte();
        Identified = (Bools & 0x01) == 0x01;
        Cursed = (Bools & 0x02) == 0x02;
        Strong = reader.ReadByte();
        MagicResist = reader.ReadByte();
        PoisonResist = reader.ReadByte();
        HealthRecovery = reader.ReadByte();
        ManaRecovery = reader.ReadByte();
        PoisonRecovery = reader.ReadByte();
        CriticalRate = reader.ReadByte();
        CriticalDamage = reader.ReadByte();
        Freezing = reader.ReadByte();
        PoisonAttack = reader.ReadByte();


        if (version <= 31) return;

        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            if (reader.ReadBoolean()) continue;
            UserItem item = new UserItem(reader, version, Customversion);
            Slots[i] = item;
        }

        if (version <= 38) return;

        GemCount = reader.ReadUInt32();

        if (version <= 40) return;

        Awake = new Awake(reader);

        if (version <= 56) return;

        RefinedValue = (RefinedValue)reader.ReadByte();
        RefineAdded = reader.ReadByte();
        if (version < 60) return;
        WeddingRing = reader.ReadInt64();

        if (version < 65) return;

        if (reader.ReadBoolean())
            ExpireInfo = new ExpireInfo(reader, version, Customversion);

        if (version < 76)
            return;

        if (reader.ReadBoolean())
            RentalInformation = new RentalInformation(reader, version, Customversion);
    }

    /// <summary>
    /// 加载所有数据库中加载
    /// 作废，不单独加载
    /// </summary>
    /// <returns></returns>
    public static List<UserItem> loadAll()
    {
        List<UserItem> list = new List<UserItem>();
        DbDataReader read = MirRunDB.ExecuteReader("select * from UserItem");
        while (read.Read())
        {
            UserItem obj = new UserItem();
            obj.UniqueID = (ulong)read.GetInt64(read.GetOrdinal("UniqueID"));
            obj.ItemIndex = read.GetInt32(read.GetOrdinal("ItemIndex"));
            obj.CurrentDura = (ushort)read.GetInt16(read.GetOrdinal("CurrentDura"));
            obj.MaxDura = (ushort)read.GetInt16(read.GetOrdinal("MaxDura"));
            obj.Count = (uint)read.GetInt32(read.GetOrdinal("Count"));
            obj.AC = read.GetByte(read.GetOrdinal("AC"));
            obj.MAC = read.GetByte(read.GetOrdinal("MAC"));
            obj.DC = read.GetByte(read.GetOrdinal("DC"));
            obj.MC = read.GetByte(read.GetOrdinal("MC"));
            obj.SC = read.GetByte(read.GetOrdinal("SC"));

            obj.Accuracy = read.GetByte(read.GetOrdinal("Accuracy"));
            obj.Agility = read.GetByte(read.GetOrdinal("Agility"));
            obj.HP = read.GetByte(read.GetOrdinal("HP"));
            obj.MP = read.GetByte(read.GetOrdinal("MP"));
            obj.AttackSpeed = (sbyte)read.GetByte(read.GetOrdinal("AttackSpeed"));
            obj.Luck = (sbyte)read.GetByte(read.GetOrdinal("Luck"));
            obj.SoulBoundId = read.GetInt32(read.GetOrdinal("SoulBoundId"));
           
            byte Bools = read.GetByte(read.GetOrdinal("Bools"));
            obj.Identified = (Bools & 0x01) == 0x01;
            obj.Cursed = (Bools & 0x02) == 0x02;

            obj.Strong = read.GetByte(read.GetOrdinal("Strong"));
            obj.MagicResist = read.GetByte(read.GetOrdinal("MagicResist"));
            obj.PoisonResist = read.GetByte(read.GetOrdinal("PoisonResist"));
            obj.HealthRecovery = read.GetByte(read.GetOrdinal("HealthRecovery"));
            obj.ManaRecovery = read.GetByte(read.GetOrdinal("ManaRecovery"));
            obj.PoisonRecovery = read.GetByte(read.GetOrdinal("PoisonRecovery"));
            obj.CriticalRate = read.GetByte(read.GetOrdinal("CriticalRate"));
            obj.CriticalDamage = read.GetByte(read.GetOrdinal("CriticalDamage"));
            obj.Freezing = read.GetByte(read.GetOrdinal("Freezing"));
            obj.PoisonAttack = read.GetByte(read.GetOrdinal("PoisonAttack"));

 
            string _Slots = read.GetString(read.GetOrdinal("Slots"));
            //obj.Slots = UserItem.ParseUserItemIds2(_Slots);

            obj.GemCount = (uint)read.GetInt32(read.GetOrdinal("GemCount"));
            obj.Awake = JsonConvert.DeserializeObject<Awake>(read.GetString(read.GetOrdinal("Awake")));
            obj.RefinedValue = (RefinedValue)read.GetByte(read.GetOrdinal("RefinedValue"));
            obj.RefineAdded = read.GetByte(read.GetOrdinal("RefineAdded"));

            obj.WeddingRing = read.GetByte(read.GetOrdinal("WeddingRing"));
            DateTime edate = read.GetDateTime(read.GetOrdinal("ExpireInfo"));
            if (edate != null)
            {
                obj.ExpireInfo = new ExpireInfo { ExpiryDate = edate };
            }
            obj.RentalInformation=JsonConvert.DeserializeObject<RentalInformation>(read.GetString(read.GetOrdinal("RentalInformation")));

            DBObjectUtils.updateObjState(obj, obj.UniqueID);
            list.Add(obj);
        }
        return list;
    }

    //重新绑定关联，关联具体的物品
    public bool BindItem()
    {
        Info = ItemInfo.getItem(ItemIndex);
        if (Info == null)
        {
            return false;
        }
        return BindSlotItems();
    }

    public bool BindSlotItems()
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i] == null) continue;

            if (!Slots[i].BindItem()) return false;
        }
        SetSlotSize();
        return true;
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(UniqueID);
        writer.Write(ItemIndex);

        writer.Write(CurrentDura);
        writer.Write(MaxDura);

        writer.Write(Count);

        writer.Write(AC);
        writer.Write(MAC);
        writer.Write(DC);
        writer.Write(MC);
        writer.Write(SC);

        writer.Write(Accuracy);
        writer.Write(Agility);
        writer.Write(HP);
        writer.Write(MP);

        writer.Write(AttackSpeed);
        writer.Write(Luck);
        writer.Write(SoulBoundId);
        byte Bools = 0;
        if (Identified) Bools |= 0x01;
        if (Cursed) Bools |= 0x02;
        writer.Write(Bools);
        writer.Write(Strong);
        writer.Write(MagicResist);
        writer.Write(PoisonResist);
        writer.Write(HealthRecovery);
        writer.Write(ManaRecovery);
        writer.Write(PoisonRecovery);
        writer.Write(CriticalRate);
        writer.Write(CriticalDamage);
        writer.Write(Freezing);
        writer.Write(PoisonAttack);

        writer.Write(Slots.Length);
        for (int i = 0; i < Slots.Length; i++)
        {
            writer.Write(Slots[i] == null);
            if (Slots[i] == null) continue;

            Slots[i].Save(writer);
        }

        writer.Write(GemCount);


        Awake.Save(writer);

        writer.Write((byte)RefinedValue);
        writer.Write(RefineAdded);

        writer.Write(WeddingRing);

        writer.Write(ExpireInfo != null);
        ExpireInfo?.Save(writer);

        writer.Write(RentalInformation != null);
        RentalInformation?.Save(writer);

    }

    //作废，不单独保存
    //保存到数据库
    public void SaveDB()
    {
        byte state = DBObjectUtils.ObjState(this, UniqueID);
        if (state == 0)//没有改变
        {
            return;
        }
        List<SQLiteParameter> lp = new List<SQLiteParameter>();
        lp.Add(new SQLiteParameter("ItemIndex", ItemIndex));
        lp.Add(new SQLiteParameter("CurrentDura", CurrentDura));
        lp.Add(new SQLiteParameter("MaxDura", MaxDura));
        lp.Add(new SQLiteParameter("Count", Count));

        lp.Add(new SQLiteParameter("AC", AC));
        lp.Add(new SQLiteParameter("MAC", MAC));
        lp.Add(new SQLiteParameter("DC", DC));
        lp.Add(new SQLiteParameter("MC", MC));
        lp.Add(new SQLiteParameter("SC", SC));

        lp.Add(new SQLiteParameter("Accuracy", Accuracy));
        lp.Add(new SQLiteParameter("Agility", Agility));
        lp.Add(new SQLiteParameter("HP", HP));
        lp.Add(new SQLiteParameter("MP", MP));

        lp.Add(new SQLiteParameter("AttackSpeed", AttackSpeed));
        lp.Add(new SQLiteParameter("Luck", Luck));
        lp.Add(new SQLiteParameter("SoulBoundId", SoulBoundId));
        byte Bools = 0;
        if (Identified) Bools |= 0x01;
        if (Cursed) Bools |= 0x02;
        
        lp.Add(new SQLiteParameter("Bools", Bools));
        lp.Add(new SQLiteParameter("Strong", Strong));
        lp.Add(new SQLiteParameter("MagicResist", MagicResist));
        lp.Add(new SQLiteParameter("PoisonResist", PoisonResist));
        lp.Add(new SQLiteParameter("HealthRecovery", HealthRecovery));
        lp.Add(new SQLiteParameter("ManaRecovery", ManaRecovery)); 
        lp.Add(new SQLiteParameter("PoisonRecovery", PoisonRecovery));
        lp.Add(new SQLiteParameter("CriticalRate", CriticalRate));
        lp.Add(new SQLiteParameter("CriticalDamage", CriticalDamage));
        lp.Add(new SQLiteParameter("Freezing", Freezing));
        lp.Add(new SQLiteParameter("PoisonAttack", PoisonAttack));
        //lp.Add(new SQLiteParameter("Slots", UserItem.getUserItemIds(Slots)));
        lp.Add(new SQLiteParameter("GemCount", GemCount));
        lp.Add(new SQLiteParameter("Awake", JsonConvert.SerializeObject(Awake)));
        lp.Add(new SQLiteParameter("RefinedValue", (byte)RefinedValue));
        lp.Add(new SQLiteParameter("RefineAdded", RefineAdded)); 
        lp.Add(new SQLiteParameter("WeddingRing", WeddingRing));
        if (ExpireInfo != null)
        {
            lp.Add(new SQLiteParameter("ExpireInfo", ExpireInfo.ExpiryDate));
        }
      
        lp.Add(new SQLiteParameter("RentalInformation", JsonConvert.SerializeObject(RentalInformation)));

        //新增
        if (state == 1)
        {
            if (UniqueID > 0)
            {
                lp.Add(new SQLiteParameter("UniqueID", UniqueID));
            }
            string sql = "insert into UserItem" + SQLiteHelper.createInsertSql(lp.ToArray());
            MirRunDB.Execute(sql, lp.ToArray());
        }
        //修改
        if (state == 2)
        {
            string sql = "update UserItem set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where UniqueID=@UniqueID";
            lp.Add(new SQLiteParameter("UniqueID", UniqueID));
            MirRunDB.Execute(sql, lp.ToArray());
        }
        DBObjectUtils.updateObjState(this, UniqueID);
    }

    public uint Price()
    {
        if (Info == null) return 0;

        uint p = Info.Price;


        if (Info.Durability > 0)
        {
            float r = ((Info.Price / 2F) / Info.Durability);

            p = (uint)(MaxDura * r);

            if (MaxDura > 0)
                r = CurrentDura / (float)MaxDura;
            else
                r = 0;

            p = (uint)Math.Floor(p / 2F + ((p / 2F) * r) + Info.Price / 2F);
        }


        p = (uint)(p * ((AC + MAC + DC + MC + SC + Accuracy + Agility + HP + MP + AttackSpeed + Luck + Strong + MagicResist + PoisonResist + HealthRecovery + ManaRecovery + PoisonRecovery + CriticalRate + CriticalDamage + Freezing + PoisonAttack) * 0.1F + 1F));


        return p * Count;
    }
    public uint RepairPrice()
    {
        if (Info == null || Info.Durability == 0)
            return 0;

        var p = Info.Price;

        if (Info.Durability > 0)
        {
            p = (uint)Math.Floor(MaxDura * ((Info.Price / 2F) / Info.Durability) + Info.Price / 2F);
            p = (uint)(p * ((AC + MAC + DC + MC + SC + Accuracy + Agility + HP + MP + AttackSpeed + Luck + Strong + MagicResist + PoisonResist + HealthRecovery + ManaRecovery + PoisonRecovery + CriticalRate + CriticalDamage + Freezing + PoisonAttack) * 0.1F + 1F));

        }

        var cost = p * Count - Price();

        if (RentalInformation == null)
            return cost;

        return cost * 2;
    }

    public uint Quality()
    {
        uint q = (uint)(AC + MAC + DC + MC + SC + Accuracy + Agility + HP + MP + AttackSpeed + Luck + Strong + MagicResist + PoisonResist + HealthRecovery + ManaRecovery + PoisonRecovery + CriticalRate + CriticalDamage + Freezing + PoisonAttack + Awake.getAwakeLevel() + 1);

        return q;
    }

    public uint AwakeningPrice()
    {
        if (Info == null) return 0;

        uint p = 1500;

        p = (uint)((p * (1 + Awake.getAwakeLevel() * 2)) * (uint)Info.Grade);

        return p;
    }

    public uint DisassemblePrice()
    {
        if (Info == null) return 0;

        uint p = 1500 * (uint)Info.Grade;

        p = (uint)(p * ((AC + MAC + DC + MC + SC + Accuracy + Agility + HP + MP + AttackSpeed + Luck + Strong + MagicResist + PoisonResist + HealthRecovery + ManaRecovery + PoisonRecovery + CriticalRate + CriticalDamage + Freezing + PoisonAttack + Awake.getAwakeLevel()) * 0.1F + 1F));

        return p;
    }

    public uint DowngradePrice()
    {
        if (Info == null) return 0;

        uint p = 3000;

        p = (uint)((p * (1 + (Awake.getAwakeLevel() + 1) * 2)) * (uint)Info.Grade);

        return p;
    }

    public uint ResetPrice()
    {
        if (Info == null) return 0;

        uint p = 3000 * (uint)Info.Grade;

        p = (uint)(p * ((AC + MAC + DC + MC + SC + Accuracy + Agility + HP + MP + AttackSpeed + Luck + Strong + MagicResist + PoisonResist + HealthRecovery + ManaRecovery + PoisonRecovery + CriticalRate + CriticalDamage + Freezing + PoisonAttack) * 0.2F + 1F));

        return p;
    }
    public void SetSlotSize() //set slot size in db?
    {
        int amount = 0;

        switch (Info.Type)
        {
            case ItemType.Mount:
                if (Info.Shape < 7)
                    amount = 4;
                else if (Info.Shape < 12)
                    amount = 5;
                break;
            case ItemType.Weapon:
                if (Info.Shape == 49 || Info.Shape == 50)
                    amount = 5;
                break;
        }

        if (amount == Slots.Length) return;

        Array.Resize(ref Slots, amount);
    }

    public ushort Image
    {
        get
        {
            switch (Info.Type)
            {
                #region Amulet and Poison Stack Image changes
                case ItemType.Amulet:
                    if (Info.StackSize > 0)
                    {
                        switch (Info.Shape)
                        {
                            case 0: //Amulet护身符
                                if (Count >= 300) return 3662;
                                if (Count >= 200) return 3661;
                                if (Count >= 100) return 3660;
                                return 3660;
                            case 1: //Grey Poison绿毒
                                if (Count >= 150) return 3675;
                                if (Count >= 100) return 2960;
                                if (Count >= 50) return 3674;
                                return 3673;
                            case 2: //Yellow Poison黄毒
                                if (Count >= 150) return 3672;
                                if (Count >= 100) return 2961;
                                if (Count >= 50) return 3671;
                                return 3670;
                        }
                    }
                    break;
            }

            #endregion

            return Info.Image;
        }
    }
    //物品的副本
    public UserItem Clone()
    {
        UserItem item = new UserItem(Info)
        {
            UniqueID = UniqueID,
            CurrentDura = CurrentDura,
            MaxDura = MaxDura,
            Count = Count,

            AC = AC,
            MAC = MAC,
            DC = DC,
            MC = MC,
            SC = SC,
            Accuracy = Accuracy,
            Agility = Agility,
            HP = HP,
            MP = MP,

            AttackSpeed = AttackSpeed,
            Luck = Luck,

            DuraChanged = DuraChanged,
            SoulBoundId = SoulBoundId,
            Identified = Identified,
            Cursed = Cursed,
            Strong = Strong,
            MagicResist = MagicResist,
            PoisonResist = PoisonResist,
            HealthRecovery = HealthRecovery,
            ManaRecovery = ManaRecovery,
            PoisonRecovery = PoisonRecovery,
            CriticalRate = CriticalRate,
            CriticalDamage = CriticalDamage,
            Freezing = Freezing,
            PoisonAttack = PoisonAttack,

            Slots = Slots,
            Awake = Awake,

            RefinedValue = RefinedValue,
            RefineAdded = RefineAdded,

            ExpireInfo = ExpireInfo,
            RentalInformation = RentalInformation
        };

        return item;
    }


    //更新物品的期限
    //通过名字来更新的，啃爹哦,几乎不用
    public void UpdateItemExpiry()
    {
        //can't have expiry on usable items
        if (Info.Type == ItemType.Scroll || Info.Type == ItemType.Potion ||
            Info.Type == ItemType.Transform || Info.Type == ItemType.Script) return;

        ExpireInfo expiryInfo = new ExpireInfo();

        Regex r = new Regex(@"\[(.*?)\]");
        Match expiryMatch = r.Match(Info.Name);

        if (expiryMatch.Success)
        {
            string parameter = expiryMatch.Groups[1].Captures[0].Value;

            var numAlpha = new Regex("(?<Numeric>[0-9]*)(?<Alpha>[a-zA-Z]*)");
            var match = numAlpha.Match(parameter);

            string alpha = match.Groups["Alpha"].Value;
            int num = 0;

            int.TryParse(match.Groups["Numeric"].Value, out num);

            switch (alpha)
            {
                case "m":
                    expiryInfo.ExpiryDate = DateTime.Now.AddMinutes(num);
                    break;
                case "h":
                    expiryInfo.ExpiryDate = DateTime.Now.AddHours(num);
                    break;
                case "d":
                    expiryInfo.ExpiryDate = DateTime.Now.AddDays(num);
                    break;
                case "M":
                    expiryInfo.ExpiryDate = DateTime.Now.AddMonths(num);
                    break;
                case "y":
                    expiryInfo.ExpiryDate = DateTime.Now.AddYears(num);
                    break;
                default:
                    expiryInfo.ExpiryDate = DateTime.MaxValue;
                    break;
            }
            ExpireInfo = expiryInfo;
        }
    }

    //物品的极品几率
    public void UpgradeItem()
    {
        if (Info.RandomStats == null) return;
        RandomItemStat stat = Info.RandomStats;
        if ((stat.MaxDuraChance > 0) && (RandomUtils.Next(stat.MaxDuraChance) == 0))
        {
            int dura = RandomomRange(stat.MaxDuraMaxStat, stat.MaxDuraStatChance);
            MaxDura = (ushort)Math.Min(ushort.MaxValue, MaxDura + dura * 1000);
            CurrentDura = (ushort)Math.Min(ushort.MaxValue, CurrentDura + dura * 1000);
        }

        if ((stat.MaxAcChance > 0) && (RandomUtils.Next(stat.MaxAcChance) == 0)) AC = (byte)(RandomomRange(stat.MaxAcMaxStat - 1, stat.MaxAcStatChance) + 1);
        if ((stat.MaxMacChance > 0) && (RandomUtils.Next(stat.MaxMacChance) == 0)) MAC = (byte)(RandomomRange(stat.MaxMacMaxStat - 1, stat.MaxMacStatChance) + 1);
        if ((stat.MaxDcChance > 0) && (RandomUtils.Next(stat.MaxDcChance) == 0)) DC = (byte)(RandomomRange(stat.MaxDcMaxStat - 1, stat.MaxDcStatChance) + 1);
        if ((stat.MaxMcChance > 0) && (RandomUtils.Next(stat.MaxScChance) == 0)) MC = (byte)(RandomomRange(stat.MaxMcMaxStat - 1, stat.MaxMcStatChance) + 1);
        if ((stat.MaxScChance > 0) && (RandomUtils.Next(stat.MaxMcChance) == 0)) SC = (byte)(RandomomRange(stat.MaxScMaxStat - 1, stat.MaxScStatChance) + 1);
        if ((stat.AccuracyChance > 0) && (RandomUtils.Next(stat.AccuracyChance) == 0)) Accuracy = (byte)(RandomomRange(stat.AccuracyMaxStat - 1, stat.AccuracyStatChance) + 1);
        if ((stat.AgilityChance > 0) && (RandomUtils.Next(stat.AgilityChance) == 0)) Agility = (byte)(RandomomRange(stat.AgilityMaxStat - 1, stat.AgilityStatChance) + 1);
        if ((stat.HpChance > 0) && (RandomUtils.Next(stat.HpChance) == 0)) HP = (byte)(RandomomRange(stat.HpMaxStat - 1, stat.HpStatChance) + 1);
        if ((stat.MpChance > 0) && (RandomUtils.Next(stat.MpChance) == 0)) MP = (byte)(RandomomRange(stat.MpMaxStat - 1, stat.MpStatChance) + 1);
        if ((stat.StrongChance > 0) && (RandomUtils.Next(stat.StrongChance) == 0)) Strong = (byte)(RandomomRange(stat.StrongMaxStat - 1, stat.StrongStatChance) + 1);
        if ((stat.MagicResistChance > 0) && (RandomUtils.Next(stat.MagicResistChance) == 0)) MagicResist = (byte)(RandomomRange(stat.MagicResistMaxStat - 1, stat.MagicResistStatChance) + 1);
        if ((stat.PoisonResistChance > 0) && (RandomUtils.Next(stat.PoisonResistChance) == 0)) PoisonResist = (byte)(RandomomRange(stat.PoisonResistMaxStat - 1, stat.PoisonResistStatChance) + 1);
        if ((stat.HpRecovChance > 0) && (RandomUtils.Next(stat.HpRecovChance) == 0)) HealthRecovery = (byte)(RandomomRange(stat.HpRecovMaxStat - 1, stat.HpRecovStatChance) + 1);
        if ((stat.MpRecovChance > 0) && (RandomUtils.Next(stat.MpRecovChance) == 0)) ManaRecovery = (byte)(RandomomRange(stat.MpRecovMaxStat - 1, stat.MpRecovStatChance) + 1);
        if ((stat.PoisonRecovChance > 0) && (RandomUtils.Next(stat.PoisonRecovChance) == 0)) PoisonRecovery = (byte)(RandomomRange(stat.PoisonRecovMaxStat - 1, stat.PoisonRecovStatChance) + 1);
        if ((stat.CriticalRateChance > 0) && (RandomUtils.Next(stat.CriticalRateChance) == 0)) CriticalRate = (byte)(RandomomRange(stat.CriticalRateMaxStat - 1, stat.CriticalRateStatChance) + 1);
        if ((stat.CriticalDamageChance > 0) && (RandomUtils.Next(stat.CriticalDamageChance) == 0)) CriticalDamage = (byte)(RandomomRange(stat.CriticalDamageMaxStat - 1, stat.CriticalDamageStatChance) + 1);
        if ((stat.FreezeChance > 0) && (RandomUtils.Next(stat.FreezeChance) == 0)) Freezing = (byte)(RandomomRange(stat.FreezeMaxStat - 1, stat.FreezeStatChance) + 1);
        if ((stat.PoisonAttackChance > 0) && (RandomUtils.Next(stat.PoisonAttackChance) == 0)) PoisonAttack = (byte)(RandomomRange(stat.PoisonAttackMaxStat - 1, stat.PoisonAttackStatChance) + 1);
        if ((stat.AttackSpeedChance > 0) && (RandomUtils.Next(stat.AttackSpeedChance) == 0)) AttackSpeed = (sbyte)(RandomomRange(stat.AttackSpeedMaxStat - 1, stat.AttackSpeedStatChance) + 1);
        if ((stat.LuckChance > 0) && (RandomUtils.Next(stat.LuckChance) == 0)) Luck = (sbyte)(RandomomRange(stat.LuckMaxStat - 1, stat.LuckStatChance) + 1);
        if ((stat.CurseChance > 0) && (RandomUtils.Next(100) <= stat.CurseChance)) Cursed = true;
    }

    //最大增加数，增加几率1/x
    //装备极品是用这个做的做的，所以加1点属性是
    public static int RandomomRange(int count, int rate)
    {
        int x = 0;
        for (int i = 0; i < count; i++) if (RandomUtils.Next(rate) == 0) x++;
        return x;
    }
}

public class ExpireInfo
{
    public DateTime ExpiryDate;

    public ExpireInfo()
    {

    }

    public ExpireInfo(BinaryReader reader, int version = int.MaxValue, int Customversion = int.MaxValue)
    {
        ExpiryDate = DateTime.FromBinary(reader.ReadInt64());
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(ExpiryDate.ToBinary());
    }
}
