﻿﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
/// <summary>
/// /定义所有服务器端的包
/// </summary>
namespace ServerPackets
{
    //心跳包
    public sealed class KeepAlive : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.KeepAlive; }
        }
        public long Time;

        protected override void ReadPacket(BinaryReader reader)
        {
            Time = reader.ReadInt64();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Time);
        }
    }
    //连接
    public sealed class Connected : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.Connected; }
        }

        protected override void ReadPacket(BinaryReader reader)
        {
        }

        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    //客户端版本
    public sealed class ClientVersion : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ClientVersion; }
        }

        public byte Result;
        /*
         * 0: Wrong Version
         * 1: Correct Version
         */

        protected override void ReadPacket(BinaryReader reader)
        {
            Result = reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Result);
        }
    }
    //断开连接
    public sealed class Disconnect : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.Disconnect; }
        }

        public byte Reason;

        /*
         * 0: Server Closing.
         * 1: Another User.
         * 2: Packet Error.
         * 3: Server Crashed.
         */

        protected override void ReadPacket(BinaryReader reader)
        {
            Reason = reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Reason);
        }
    }
    //新账号
    public sealed class NewAccount : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.NewAccount; }
        }

        public byte Result;
        /*
         * 0: Disabled
         * 1: Bad AccountID
         * 2: Bad Password
         * 3: Bad Email
         * 4: Bad Name
         * 5: Bad Question
         * 6: Bad Answer
         * 7: Account Exists.
         * 8: Success
         */

        protected override void ReadPacket(BinaryReader reader)
        {
            Result = reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Result);
        }
    }

    //修改密码
    public sealed class ChangePassword : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ChangePassword; }
        }

        public byte Result;
        /*
         * 0: Disabled
         * 1: Bad AccountID
         * 2: Bad Current Password
         * 3: Bad New Password
         * 4: Account Not Exist
         * 5: Wrong Password
         * 6: Success
         */

        protected override void ReadPacket(BinaryReader reader)
        {
            Result = reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Result);
        }
    }
    //修改密码
    public sealed class ChangePasswordBanned : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ChangePasswordBanned; }
        }

        public string Reason = string.Empty;
        public DateTime ExpiryDate;

        protected override void ReadPacket(BinaryReader reader)
        {
            Reason = reader.ReadString();
            ExpiryDate = DateTime.FromBinary(reader.ReadInt64());
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Reason);
            writer.Write(ExpiryDate.ToBinary());
        }
    }
    //登录
    public sealed class Login : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.Login; }
        }

        public byte Result;
        /*
         * 0: Disabled
         * 1: Bad AccountID
         * 2: Bad Password
         * 3: Account Not Exist
         * 4: Wrong Password
         */

        protected override void ReadPacket(BinaryReader reader)
        {
            Result = reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Result);
        }
    }
    //禁止登录
    public sealed class LoginBanned : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.LoginBanned; }
        }

        public string Reason = string.Empty;
        public DateTime ExpiryDate;

        protected override void ReadPacket(BinaryReader reader)
        {
            Reason = reader.ReadString();
            ExpiryDate = DateTime.FromBinary(reader.ReadInt64());
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Reason);
            writer.Write(ExpiryDate.ToBinary());
        }
    }
    //登录成功
    public sealed class LoginSuccess : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.LoginSuccess; }
        }


        public List<SelectInfo> Characters = new List<SelectInfo>();

        protected override void ReadPacket(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                Characters.Add(new SelectInfo(reader));
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Characters.Count);
            for (int i = 0; i < Characters.Count; i++)
                Characters[i].Save(writer);
        }
    }
    //新角色
    public sealed class NewCharacter : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.NewCharacter; }
        }

        /*
         * 0: Disabled.
         * 1: Bad Character Name
         * 2: Bad Gender
         * 3: Bad Class
         * 4: Max Characters
         * 5: Character Exists.
         * */
        public byte Result;

        protected override void ReadPacket(BinaryReader reader)
        {
            Result = reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Result);
        }
    }
    //返回充值链接
    public sealed class RechargeLink : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.RechargeLink; } }
        public long orderid;
        public uint money = 10;//当前选择的金额
        public byte payType = 1;//支付方式1：支付宝；2：微信支付 
        public string ret_Link;//信息支付2维码的信息
        public string query_Link;//查询结果的url，客户端自行循环查询结果，查询到结果，通知服务器，服务器再次发起查询确认支付结果

        protected override void ReadPacket(BinaryReader reader)
        {
            orderid = reader.ReadInt64();
            payType = reader.ReadByte();
            money = reader.ReadUInt32();
            ret_Link = reader.ReadString();
            query_Link = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(orderid);
            writer.Write(payType);
            writer.Write(money);
            writer.Write(ret_Link);
            writer.Write(query_Link);
        }
    }
    //返回充值结果
    public sealed class RechargeResult : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.RechargeResult; } }

        public byte pay_state;//1：支付成功，2：支付失败
        public uint money = 0;//当前选择的金额
        public uint addCredit = 0;//当前选择的金额
        

        protected override void ReadPacket(BinaryReader reader)
        {
            pay_state = reader.ReadByte();
            money = reader.ReadUInt32();
            addCredit = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(pay_state);
            writer.Write(money);
            writer.Write(addCredit);
        }
    }
    //创建新角色成功
    public sealed class NewCharacterSuccess : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.NewCharacterSuccess; }
        }

        public SelectInfo CharInfo;

        protected override void ReadPacket(BinaryReader reader)
        {
            CharInfo = new SelectInfo(reader);
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            CharInfo.Save(writer);
        }
    }
    //删除角色
    public sealed class DeleteCharacter : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.DeleteCharacter; }
        }

        public byte Result;

        /*
         * 0: Disabled.
         * 1: Character Not Found
         * */

        protected override void ReadPacket(BinaryReader reader)
        {
            Result = reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Result);
        }
    }
    //删除角色成功
    public sealed class DeleteCharacterSuccess : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.DeleteCharacterSuccess; }
        }

        public ulong CharacterIndex;

        protected override void ReadPacket(BinaryReader reader)
        {
            CharacterIndex = (ulong)reader.ReadInt64();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(CharacterIndex);
        }
    }
    //开始游戏
    public sealed class StartGame : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.StartGame; }
        }

        public byte Result;
        public int Resolution;

        /*
         * 0: Disabled.
         * 1: Not logged in
         * 2: Character not found.
         * 3: Start Game Error
         * */

        protected override void ReadPacket(BinaryReader reader)
        {
            Result = reader.ReadByte();
            Resolution = reader.ReadInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Result);
            writer.Write(Resolution);
        }
    }
    //禁止开始游戏
    public sealed class StartGameBanned : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.StartGameBanned; }
        }

        public string Reason = string.Empty;
        public DateTime ExpiryDate;

        protected override void ReadPacket(BinaryReader reader)
        {
            Reason = reader.ReadString();
            ExpiryDate = DateTime.FromBinary(reader.ReadInt64());
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Reason);
            writer.Write(ExpiryDate.ToBinary());
        }
    }
    //开始游戏延时
    public sealed class StartGameDelay : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.StartGameDelay; }
        }

        public long Milliseconds;

        protected override void ReadPacket(BinaryReader reader)
        {
            Milliseconds = reader.ReadInt64();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Milliseconds);
        }

    }
    //返回地图信息？
    public sealed class MapInformation : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.MapInformation; }
        }

        public string FileName = string.Empty;
        public string Title = string.Empty;
        public ushort MiniMap, BigMap, Music;
        public LightSetting Lights;
        public bool Lightning, Fire;
        public byte MapDarkLight;
        public bool CanFastRun;
        //地图信息增加安全区,用于客户端判断是否在安全区，在安全区则可以穿人，穿怪
        //安全区域
        public List<SafeZoneInfo> SafeZones = new List<SafeZoneInfo>();



        protected override void ReadPacket(BinaryReader reader)
        {
            FileName = reader.ReadString();
            Title = reader.ReadString();
            MiniMap = reader.ReadUInt16();
            BigMap = reader.ReadUInt16();
            Lights = (LightSetting)reader.ReadByte();
            byte bools = reader.ReadByte();
            if ((bools & 0x01) == 0x01) Lightning = true;
            if ((bools & 0x02) == 0x02) Fire = true;
            MapDarkLight = reader.ReadByte();
            Music = reader.ReadUInt16();
            CanFastRun = reader.ReadBoolean();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                SafeZones.Add(new SafeZoneInfo(reader));
            }
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(FileName);
            writer.Write(Title);
            writer.Write(MiniMap);
            writer.Write(BigMap);
            writer.Write((byte)Lights);
            byte bools = 0;
            bools |= (byte)(Lightning ? 0x01 : 0);
            bools |= (byte)(Fire ? 0x02 : 0);
            writer.Write(bools);
            writer.Write(MapDarkLight);
            writer.Write(Music);
            writer.Write(CanFastRun);
            writer.Write(SafeZones.Count);
            for (int i = 0; i < SafeZones.Count; i++)
            {
                SafeZones[i].Save(writer);
            }
        }
    }

    //返回用户信息(登录后，第一次返回完整的信息)
    public sealed class UserInformation : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.UserInformation; }
        }

        public uint ObjectID;
        public ulong RealId;
        public string Name = string.Empty;
        public string GuildName = string.Empty;
        public string GuildRank = string.Empty;
        public Color NameColour;
        public MirClass Class;
        public MirGender Gender;
        public ushort Level;
        public Point Location;
        public MirDirection Direction;
        public byte Hair;
        public ushort HP, MP;
        public long Experience, MaxExperience;

        public LevelEffects LevelEffects;

        public UserItem[] Inventory, Equipment, QuestInventory;
        public uint Gold, Credit;

        public bool HasExpandedStorage;
        public DateTime ExpandedStorageExpiryTime;

        public List<ClientMagic> Magics = new List<ClientMagic>();

        public List<ClientIntelligentCreature> IntelligentCreatures = new List<ClientIntelligentCreature>();//IntelligentCreature
        public IntelligentCreatureType SummonedCreatureType = IntelligentCreatureType.None;//IntelligentCreature
        public bool CreatureSummoned;//IntelligentCreature



        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            RealId = reader.ReadUInt64();
            Name = reader.ReadString();
            GuildName = reader.ReadString();
            GuildRank = reader.ReadString();
            NameColour = Color.FromArgb(reader.ReadInt32());
            Class = (MirClass)reader.ReadByte();
            Gender = (MirGender)reader.ReadByte();
            Level = reader.ReadUInt16();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
            Hair = reader.ReadByte();
            HP = reader.ReadUInt16();
            MP = reader.ReadUInt16();

            Experience = reader.ReadInt64();
            MaxExperience = reader.ReadInt64();

            LevelEffects = (LevelEffects)reader.ReadByte();

            if (reader.ReadBoolean())
            {
                Inventory = new UserItem[reader.ReadInt32()];
                for (int i = 0; i < Inventory.Length; i++)
                {
                    if (!reader.ReadBoolean()) continue;
                    Inventory[i] = new UserItem(reader);
                }
            }

            if (reader.ReadBoolean())
            {
                Equipment = new UserItem[reader.ReadInt32()];
                for (int i = 0; i < Equipment.Length; i++)
                {
                    if (!reader.ReadBoolean()) continue;
                    Equipment[i] = new UserItem(reader);
                }
            }

            if (reader.ReadBoolean())
            {
                QuestInventory = new UserItem[reader.ReadInt32()];
                for (int i = 0; i < QuestInventory.Length; i++)
                {
                    if (!reader.ReadBoolean()) continue;
                    QuestInventory[i] = new UserItem(reader);
                }
            }

            Gold = reader.ReadUInt32();
            Credit = reader.ReadUInt32();

            HasExpandedStorage = reader.ReadBoolean();
            ExpandedStorageExpiryTime = DateTime.FromBinary(reader.ReadInt64());

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
                Magics.Add(new ClientMagic(reader));

            //IntelligentCreature
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                IntelligentCreatures.Add(new ClientIntelligentCreature(reader));
            SummonedCreatureType = (IntelligentCreatureType)reader.ReadByte();
            CreatureSummoned = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(RealId);
            writer.Write(Name);
            writer.Write(GuildName);
            writer.Write(GuildRank);
            writer.Write(NameColour.ToArgb());
            writer.Write((byte)Class);
            writer.Write((byte)Gender);
            writer.Write(Level);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
            writer.Write(Hair);
            writer.Write(HP);
            writer.Write(MP);

            writer.Write(Experience);
            writer.Write(MaxExperience);

            writer.Write((byte)LevelEffects);

            writer.Write(Inventory != null);
            if (Inventory != null)
            {
                writer.Write(Inventory.Length);
                for (int i = 0; i < Inventory.Length; i++)
                {
                    writer.Write(Inventory[i] != null);
                    if (Inventory[i] == null) continue;

                    Inventory[i].Save(writer);
                }

            }

            writer.Write(Equipment != null);
            if (Equipment != null)
            {
                writer.Write(Equipment.Length);
                for (int i = 0; i < Equipment.Length; i++)
                {
                    writer.Write(Equipment[i] != null);
                    if (Equipment[i] == null) continue;

                    Equipment[i].Save(writer);
                }
            }

            writer.Write(QuestInventory != null);
            if (QuestInventory != null)
            {
                writer.Write(QuestInventory.Length);
                for (int i = 0; i < QuestInventory.Length; i++)
                {
                    writer.Write(QuestInventory[i] != null);
                    if (QuestInventory[i] == null) continue;

                    QuestInventory[i].Save(writer);
                }
            }

            writer.Write(Gold);
            writer.Write(Credit);

            writer.Write(HasExpandedStorage);
            writer.Write(ExpandedStorageExpiryTime.ToBinary());

            writer.Write(Magics.Count);
            for (int i = 0; i < Magics.Count; i++)
                Magics[i].Save(writer);

            //IntelligentCreature
            writer.Write(IntelligentCreatures.Count);
            for (int i = 0; i < IntelligentCreatures.Count; i++)
                IntelligentCreatures[i].Save(writer);
            writer.Write((byte)SummonedCreatureType);
            writer.Write(CreatureSummoned);
        }
    }

    /// <summary>
    /// 我的契约兽信息返回
    /// 返回所有的契约兽
    /// </summary>
    public sealed class MyMonstersPackets : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.MyMonstersPackets; }
        }

        public MyMonster[] MyMonsters;//10个契约兽

        protected override void ReadPacket(BinaryReader reader)
        {
            if (reader.ReadBoolean())
            {
                MyMonsters = new MyMonster[reader.ReadInt32()];
                for (int i = 0; i < MyMonsters.Length; i++)
                {
                    if (!reader.ReadBoolean()) continue;
                    MyMonsters[i] = new MyMonster(reader);
                }
            }
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(MyMonsters != null);
            if (MyMonsters != null)
            {
                writer.Write(MyMonsters.Length);
                for (int i = 0; i < MyMonsters.Length; i++)
                {
                    writer.Write(MyMonsters[i] != null);
                    if (MyMonsters[i] == null) continue;

                    MyMonsters[i].Save(writer);
                }
            }
        }
    }

    /// <summary>
    /// 契约兽信息更新，更新经验
    /// </summary>
    public sealed class MyMonstersExpUpdate : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.MyMonstersExpUpdate; }
        }

        public ulong monidx;//契约兽的ID
        public byte MonLevel;

        public ulong currExp;//当前等级累计经验
        public ulong maxExp;//当前等级要求经验




        protected override void ReadPacket(BinaryReader reader)
        {
            monidx = reader.ReadUInt64();
            MonLevel = reader.ReadByte();
            currExp = reader.ReadUInt64();
            maxExp = reader.ReadUInt64();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(monidx);
            writer.Write(MonLevel);
            writer.Write(currExp);
            writer.Write(maxExp);
        }


    }

    




    //返回用户的最新的背包,刷新用户背包的时候返回最新的背包信息
    public sealed class UserInventory : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.UserInventory; }
        }

        public UserItem[] Inventory;
        protected override void ReadPacket(BinaryReader reader)
        {
            if (reader.ReadBoolean())
            {
                Inventory = new UserItem[reader.ReadInt32()];
                for (int i = 0; i < Inventory.Length; i++)
                {
                    if (!reader.ReadBoolean()) continue;
                    Inventory[i] = new UserItem(reader);
                }
            }
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Inventory != null);
            if (Inventory != null)
            {
                writer.Write(Inventory.Length);
                for (int i = 0; i < Inventory.Length; i++)
                {
                    writer.Write(Inventory[i] != null);
                    if (Inventory[i] == null) continue;

                    Inventory[i].Save(writer);
                }
            }
        }
    }

    //返回用户的金币，金币和元宝
    //当前的金币，元宝
    public sealed class UserGold : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.UserGold; }
        }

        public uint Gold;//金币,金币是账号上的金币，多角色共享
        public uint Credit;//积分，信用,也可称作元宝


        protected override void ReadPacket(BinaryReader reader)
        {
            Gold = reader.ReadUInt32();
            Credit = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Gold);
            writer.Write(Credit);
        }
    }

    //返回用户位置？
    public sealed class UserLocation : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.UserLocation; }
        }

        public Point Location;
        public MirDirection Direction;


        protected override void ReadPacket(BinaryReader reader)
        {
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }
    //返回玩家信息
    public sealed class ObjectPlayer : Packet
    {

        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectPlayer; }
        }

        public uint ObjectID;
        public string Name = string.Empty;
        public string GuildName = string.Empty;
        public string GuildRankName = string.Empty;
        public Color NameColour;
        public MirClass Class;
        public MirGender Gender;
        public ushort Level;
        public Point Location;
        public MirDirection Direction;
        public byte Hair;//头发
        public byte Light;//发光？
		public short Weapon, WeaponEffect, Armour;//武器，武器效果，衣服，这里可以做 幻化衣服，武器，武器特效等
        public PoisonType Poison;//中毒类型
        public bool Dead, Hidden;//死，隐身？
        public SpellEffect Effect;//魔法效果
        public byte WingEffect;//
        public bool Extra;

        public short MountType;
        public bool RidingMount;
        public bool Fishing;

        public short TransformType;

        public uint ElementOrbEffect;
        public uint ElementOrbLvl;
        public uint ElementOrbMax;

        public LevelEffects LevelEffects;

        public List<BuffType> Buffs = new List<BuffType>();

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Name = reader.ReadString();
            GuildName = reader.ReadString();
            GuildRankName = reader.ReadString();
            NameColour = Color.FromArgb(reader.ReadInt32());
            Class = (MirClass)reader.ReadByte();
            Gender = (MirGender)reader.ReadByte();
            Level = reader.ReadUInt16();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
            Hair = reader.ReadByte();
            Light = reader.ReadByte();
            Weapon = reader.ReadInt16();
			WeaponEffect = reader.ReadInt16();
			Armour = reader.ReadInt16();
            Poison = (PoisonType)reader.ReadUInt16();
            Dead = reader.ReadBoolean();
            Hidden = reader.ReadBoolean();
            Effect = (SpellEffect)reader.ReadByte();
            WingEffect = reader.ReadByte();
            Extra = reader.ReadBoolean();
            MountType = reader.ReadInt16();
            RidingMount = reader.ReadBoolean();
            Fishing = reader.ReadBoolean();

            TransformType = reader.ReadInt16();

            ElementOrbEffect = reader.ReadUInt32();
            ElementOrbLvl = reader.ReadUInt32();
            ElementOrbMax = reader.ReadUInt32();

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Buffs.Add((BuffType)reader.ReadByte());
            }

            LevelEffects = (LevelEffects)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Name);
            writer.Write(GuildName);
            writer.Write(GuildRankName);
            writer.Write(NameColour.ToArgb());
            writer.Write((byte)Class);
            writer.Write((byte)Gender);
            writer.Write(Level);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
            writer.Write(Hair);
            writer.Write(Light);
            writer.Write(Weapon);
			writer.Write(WeaponEffect);
			writer.Write(Armour);
            writer.Write((ushort)Poison);
            writer.Write(Dead);
            writer.Write(Hidden);
            writer.Write((byte)Effect);
            writer.Write(WingEffect);
            writer.Write(Extra);
            writer.Write(MountType);
            writer.Write(RidingMount);
            writer.Write(Fishing);

            writer.Write(TransformType);

            writer.Write(ElementOrbEffect);
            writer.Write(ElementOrbLvl);
            writer.Write(ElementOrbMax);

            writer.Write(Buffs.Count);
            for (int i = 0; i < Buffs.Count; i++)
            {
                writer.Write((byte)Buffs[i]);
            }

            writer.Write((byte)LevelEffects);
        }
    }
    //删除物品？
    public sealed class ObjectRemove : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectRemove; }
        }

        public uint ObjectID;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
        }
    }
    //物品旋转？
    public sealed class ObjectTurn : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectTurn; }
        }

        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }
    //步行
    public sealed class ObjectWalk : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectWalk; }
        }

        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }
    //跑动
    public sealed class ObjectRun : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectRun; }
        }

        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }
    //聊天,消息
    public sealed class Chat : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.Chat; }
        }

        public string Message = string.Empty;
        public ChatType Type;

        protected override void ReadPacket(BinaryReader reader)
        {
            Message = reader.ReadString();
            Type = (ChatType)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Message);
            writer.Write((byte)Type);
        }
    }
    //对象说话
    public sealed class ObjectChat : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectChat; }
        }

        public uint ObjectID;
        public string Text = string.Empty;
        public ChatType Type;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Text = reader.ReadString();
            Type = (ChatType)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Text);
            writer.Write((byte)Type);
        }
    }
    //新的物品？
    public sealed class NewItemInfo : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.NewItemInfo; }
        }

        public ItemInfo Info;

        protected override void ReadPacket(BinaryReader reader)
        {
            Info = new ItemInfo(reader);
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            Info.Save(writer);
        }
    }
    //移动物品？
    public sealed class MoveItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.MoveItem; }
        }

        public MirGridType Grid;
        public int From, To;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            Grid = (MirGridType)reader.ReadByte();
            From = reader.ReadInt32();
            To = reader.ReadInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Grid);
            writer.Write(From);
            writer.Write(To);
            writer.Write(Success);
        }
    }
    //装备物品
    public sealed class EquipItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.EquipItem; }
        }

        public MirGridType Grid;
        public ulong UniqueID;
        public int To;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            Grid = (MirGridType)reader.ReadByte();
            UniqueID = reader.ReadUInt64();
            To = reader.ReadInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Grid);
            writer.Write(UniqueID);
            writer.Write(To);
            writer.Write(Success);
        }
    }
    //合并物品
    public sealed class MergeItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.MergeItem; }
        }

        public MirGridType GridFrom, GridTo;
        public ulong IDFrom, IDTo;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            GridFrom = (MirGridType)reader.ReadByte();
            GridTo = (MirGridType)reader.ReadByte();
            IDFrom = reader.ReadUInt64();
            IDTo = reader.ReadUInt64();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)GridFrom);
            writer.Write((byte)GridTo);
            writer.Write(IDFrom);
            writer.Write(IDTo);
            writer.Write(Success);
        }
    }
    //删除物品
    public sealed class RemoveItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.RemoveItem; }
        }

        public MirGridType Grid;
        public ulong UniqueID;
        public int To;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            Grid = (MirGridType)reader.ReadByte();
            UniqueID = reader.ReadUInt64();
            To = reader.ReadInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Grid);
            writer.Write(UniqueID);
            writer.Write(To);
            writer.Write(Success);
        }
    }
    //删除物品
    public sealed class RemoveSlotItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.RemoveSlotItem; }
        }

        public MirGridType Grid;
        public MirGridType GridTo;
        public ulong UniqueID;
        public int To;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            Grid = (MirGridType)reader.ReadByte();
            GridTo = (MirGridType)reader.ReadByte();
            UniqueID = reader.ReadUInt64();
            To = reader.ReadInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Grid);
            writer.Write((byte)GridTo);
            writer.Write(UniqueID);
            writer.Write(To);
            writer.Write(Success);
        }
    }
    //收回物品
    public sealed class TakeBackItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.TakeBackItem; }
        }

        public int From, To;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
            writer.Write(Success);
        }
    }
    //存储物品
    public sealed class StoreItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.StoreItem; }
        }

        public int From, To;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
            writer.Write(Success);
        }
    }

    public sealed class DepositRefineItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.DepositRefineItem; }
        }

        public int From, To;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
            writer.Write(Success);
        }
    }

    public sealed class RetrieveRefineItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.RetrieveRefineItem; }
        }

        public int From, To;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
            writer.Write(Success);
        }
    }

    public sealed class RefineCancel : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.RefineCancel; }
        }

        public bool Unlock;
        protected override void ReadPacket(BinaryReader reader)
        {
            Unlock = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Unlock);
        }
    }

    public sealed class RefineItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.RefineItem; }
        }

        public ulong UniqueID;

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
        }
    }

    public sealed class DepositTradeItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.DepositTradeItem; }
        }

        public int From, To;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
            writer.Write(Success);
        }
    }
    public sealed class RetrieveTradeItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.RetrieveTradeItem; }
        }

        public int From, To;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
            writer.Write(Success);
        }
    }
    public sealed class SplitItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.SplitItem; }
        }

        public UserItem Item;
        public MirGridType Grid;

        protected override void ReadPacket(BinaryReader reader)
        {
            if (reader.ReadBoolean())
                Item = new UserItem(reader);

            Grid = (MirGridType)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Item != null);
            if (Item != null) Item.Save(writer);
            writer.Write((byte)Grid);
        }
    }
    public sealed class SplitItem1 : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.SplitItem1; }
        }

        public MirGridType Grid;
        public ulong UniqueID;
        public uint Count;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            Grid = (MirGridType)reader.ReadByte();
            UniqueID = reader.ReadUInt64();
            Count = reader.ReadUInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Grid);
            writer.Write(UniqueID);
            writer.Write(Count);
            writer.Write(Success);
        }
    }
    //使用物品
    public sealed class UseItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.UseItem; }
        }

        public ulong UniqueID;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
            writer.Write(Success);
        }
    }
    //丢弃物品
    public sealed class DropItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.DropItem; }
        }

        public ulong UniqueID;
        public uint Count;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
            Count = reader.ReadUInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
            writer.Write(Count);
            writer.Write(Success);
        }
    }
    //更新玩家信息（外观）
    public sealed class PlayerUpdate : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.PlayerUpdate; }
        }

        public uint ObjectID;
        public byte Light;
		public short Weapon, WeaponEffect, Armour;
		public byte WingEffect;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();

            Light = reader.ReadByte();
            Weapon = reader.ReadInt16();
			WeaponEffect = reader.ReadInt16();
			Armour = reader.ReadInt16();
            WingEffect = reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);

            writer.Write(Light);
            writer.Write(Weapon);
			writer.Write(WeaponEffect);
			writer.Write(Armour);
            writer.Write(WingEffect);
        }
    }
    //查看玩家信息（完整信息）?
    public sealed class PlayerInspect : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.PlayerInspect; }
        }

        public string Name = string.Empty;
        public string GuildName = string.Empty;
        public string GuildRank = string.Empty;
        public UserItem[] Equipment;
        public MirClass Class;
        public MirGender Gender;
        public byte Hair;
        public ushort Level;
        public string LoverName;

        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
            GuildName = reader.ReadString();
            GuildRank = reader.ReadString();
            Equipment = new UserItem[reader.ReadInt32()];
            for (int i = 0; i < Equipment.Length; i++)
            {
                if (reader.ReadBoolean())
                    Equipment[i] = new UserItem(reader);
            }

            Class = (MirClass)reader.ReadByte();
            Gender = (MirGender)reader.ReadByte();
            Hair = reader.ReadByte();
            Level = reader.ReadUInt16();
            LoverName = reader.ReadString();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(GuildName);
            writer.Write(GuildRank);
            writer.Write(Equipment.Length);
            for (int i = 0; i < Equipment.Length; i++)
            {
                UserItem T = Equipment[i];
                writer.Write(T != null);
                if (T != null) T.Save(writer);
            }

            writer.Write((byte)Class);
            writer.Write((byte)Gender);
            writer.Write(Hair);
            writer.Write(Level);
            writer.Write(LoverName);

        }
    }
    //结婚
    public sealed class MarriageRequest : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.MarriageRequest; } }

        public string Name;
        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
        }
    }
    //离婚
    public sealed class DivorceRequest : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.DivorceRequest; } }

        public string Name;
        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
        }
    }
    //师徒
    public sealed class MentorRequest : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.MentorRequest; } }

        public string Name;
        public ushort Level;

        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
            Level = reader.ReadUInt16();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Level);
        }
    }
    //交易
    public sealed class TradeRequest : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.TradeRequest; } }

        public string Name;
        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
        }
    }
    public sealed class TradeAccept : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.TradeAccept; } }

        public string Name;

        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
        }
    }
    public sealed class TradeGold : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.TradeGold; }
        }

        public uint Amount;

        protected override void ReadPacket(BinaryReader reader)
        {
            Amount = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Amount);
        }
    }
    //交易物品
    public sealed class TradeItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.TradeItem; }
        }

        public UserItem[] TradeItems;

        protected override void ReadPacket(BinaryReader reader)
        {
            TradeItems = new UserItem[reader.ReadInt32()];
            for (int i = 0; i < TradeItems.Length; i++)
            {
                if (reader.ReadBoolean())
                    TradeItems[i] = new UserItem(reader);
            }
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(TradeItems.Length);
            for (int i = 0; i < TradeItems.Length; i++)
            {
                UserItem T = TradeItems[i];
                writer.Write(T != null);
                if (T != null) T.Save(writer);
            }
        }
    }
    //交易确认
    public sealed class TradeConfirm : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.TradeConfirm; }
        }

        protected override void ReadPacket(BinaryReader reader)
        {
        }

        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    public sealed class TradeCancel : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.TradeCancel; }
        }

        public bool Unlock;
        protected override void ReadPacket(BinaryReader reader)
        {
            Unlock = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Unlock);
        }
    }
    //退出成功
    public sealed class LogOutSuccess : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.LogOutSuccess; }
        }

        public List<SelectInfo> Characters = new List<SelectInfo>();

        protected override void ReadPacket(BinaryReader reader)
        {

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
                Characters.Add(new SelectInfo(reader));
        }

        protected override void WritePacket(BinaryWriter writer)
        {

            writer.Write(Characters.Count);

            for (int i = 0; i < Characters.Count; i++)
                Characters[i].Save(writer);
        }
    }
    public sealed class LogOutFailed : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.LogOutFailed; }
        }

        protected override void ReadPacket(BinaryReader reader)
        {
        }

        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }

    //时间,这个真没必要，直接心跳返回服务器的时间给客户端，客户端自己计算即可哦
    //这样控制也可以
    public sealed class TimeOfDay : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.TimeOfDay; }
        }

        public LightSetting Lights;

        protected override void ReadPacket(BinaryReader reader)
        {
            Lights = (LightSetting)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Lights);
        }
    }
    //更改攻击类型
    public sealed class ChangeAMode : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ChangeAMode; }
        }

        public AttackMode Mode;

        protected override void ReadPacket(BinaryReader reader)
        {
            Mode = (AttackMode)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Mode);
        }
    }
    //宠物攻击类型
    public sealed class ChangePMode : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ChangePMode; }
        }

        public PetMode Mode;

        protected override void ReadPacket(BinaryReader reader)
        {
            Mode = (PetMode)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Mode);
        }
    }
    //这是掉落物品么？名称可以去掉，名称同意开始的时候全部传过来,减少流量
    //物品的颜色是服务器端控制的
    public sealed class ObjectItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectItem; }
        }

        public uint ObjectID;
        public string Name = string.Empty;
        public Color NameColour;
        public Point Location;
        public ushort Image;
        public ItemGrade grade;


        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Name = reader.ReadString();
            NameColour = Color.FromArgb(reader.ReadInt32());
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Image = reader.ReadUInt16();
            grade = (ItemGrade)reader.ReadByte();
		}

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Name);
            writer.Write(NameColour.ToArgb());
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(Image);
            writer.Write((byte)grade);
		}
    }
    //掉落金币？
    public sealed class ObjectGold : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectGold; }
        }

        public uint ObjectID;
        public uint Gold;
        public Point Location;


        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Gold = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Gold);
            writer.Write(Location.X);
            writer.Write(Location.Y);
        }
    }
    //获得物品？
    public sealed class GainedItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.GainedItem; }
        }

        public UserItem Item;

        protected override void ReadPacket(BinaryReader reader)
        {
            Item = new UserItem(reader);
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            Item.Save(writer);
        }
    }
    //获得金币
    public sealed class GainedGold : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.GainedGold; }
        }

        public uint Gold;

        protected override void ReadPacket(BinaryReader reader)
        {
            Gold = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Gold);
        }
    }
    //丢失金币
    public sealed class LoseGold : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.LoseGold; }
        }

        public uint Gold;

        protected override void ReadPacket(BinaryReader reader)
        {
            Gold = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Gold);
        }
    }
    public sealed class GainedCredit : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.GainedCredit; }
        }

        public uint Credit;

        protected override void ReadPacket(BinaryReader reader)
        {
            Credit = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Credit);
        }
    }
    public sealed class LoseCredit : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.LoseCredit; }
        }

        public uint Credit;

        protected override void ReadPacket(BinaryReader reader)
        {
            Credit = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Credit);
        }
    }
    //怪物？如果是怪物的话，就应该把怪物通用的信息，如名称，名称颜色等通用信息一开始就初始化在客户端，不要服务器每次都传给客户端的哦。
    public sealed class ObjectMonster : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectMonster; }
        }

        public uint ObjectID;
        public string Name = string.Empty;
        public Color NameColour;
        public Point Location;
        public Monster Image;
        public MirDirection Direction;
        public byte Effect, AI, Light;
        public bool Dead, Skeleton;
        public PoisonType Poison;
        public bool Hidden, Extra;
        public byte ExtraByte;//扩展字段，状态等
        public long ShockTime;
        public bool BindingShotCenter;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Name = reader.ReadString();
            NameColour = Color.FromArgb(reader.ReadInt32());
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Image = (Monster)reader.ReadUInt16();
            Direction = (MirDirection)reader.ReadByte();
            Effect = reader.ReadByte();
            AI = reader.ReadByte();
            Light = reader.ReadByte();
            Dead = reader.ReadBoolean();
            Skeleton = reader.ReadBoolean();
            Poison = (PoisonType)reader.ReadUInt16();
            Hidden = reader.ReadBoolean();
            ShockTime = reader.ReadInt64();
            BindingShotCenter = reader.ReadBoolean();
            Extra = reader.ReadBoolean();
            ExtraByte = reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Name);
            writer.Write(NameColour.ToArgb());
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((ushort)Image);
            writer.Write((byte)Direction);
            writer.Write(Effect);
            writer.Write(AI);
            writer.Write(Light);
            writer.Write(Dead);
            writer.Write(Skeleton);
            writer.Write((ushort)Poison);
            writer.Write(Hidden);
            writer.Write(ShockTime);
            writer.Write(BindingShotCenter);
            writer.Write(Extra);
            writer.Write((byte)ExtraByte);
        }

    }

    //怪物的变化，状态变化，用这个好一点。
    //这里改变，不改变位置，朝向这些，避免冲突哦
    public sealed class ObjectMonsterChange : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectMonsterChange; }
        }

        public uint ObjectID;
        public string Name = string.Empty;
        public Color NameColour;
        public Monster Image;
        public byte Effect, AI, Light;
        public bool Dead, Skeleton;
        public PoisonType Poison;
        public bool Hidden, Extra;
        public byte ExtraByte;//扩展字段，状态等
        public long ShockTime;
        public bool BindingShotCenter;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Name = reader.ReadString();
            NameColour = Color.FromArgb(reader.ReadInt32());
            Image = (Monster)reader.ReadUInt16();
            Effect = reader.ReadByte();
            AI = reader.ReadByte();
            Light = reader.ReadByte();
            Dead = reader.ReadBoolean();
            Skeleton = reader.ReadBoolean();
            Poison = (PoisonType)reader.ReadUInt16();
            Hidden = reader.ReadBoolean();
            ShockTime = reader.ReadInt64();
            BindingShotCenter = reader.ReadBoolean();
            Extra = reader.ReadBoolean();
            ExtraByte = reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Name);
            writer.Write(NameColour.ToArgb());
            writer.Write((ushort)Image);
            writer.Write(Effect);
            writer.Write(AI);
            writer.Write(Light);
            writer.Write(Dead);
            writer.Write(Skeleton);
            writer.Write((ushort)Poison);
            writer.Write(Hidden);
            writer.Write(ShockTime);
            writer.Write(BindingShotCenter);
            writer.Write(Extra);
            writer.Write((byte)ExtraByte);
        }
    }
    
    //冰雨，火雨攻击时间缩短
    public sealed class BlizzardStopTime : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.BlizzardStopTime; }
        }

        public int stopTime;

        protected override void ReadPacket(BinaryReader reader)
        {
            stopTime = reader.ReadInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(stopTime);
        }
    }
    

    //怪物攻击
    public sealed class ObjectAttack : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectAttack; }
        }

        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;
        public Spell Spell;
        public byte Level;
        public byte Type;//0 1 2 3,决定客户端怪物采用的攻击方式 攻击1-4

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
            Spell = (Spell)reader.ReadByte();
            Level = reader.ReadByte();
            Type = reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
            writer.Write((byte)Spell);
            writer.Write(Level);
            writer.Write(Type);
        }
    }
    //击中
    public sealed class Struck : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.Struck; }
        }

        public uint AttackerID;

        protected override void ReadPacket(BinaryReader reader)
        {
            AttackerID = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AttackerID);
        }
    }
    public sealed class ObjectStruck : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectStruck; }
        }

        public uint ObjectID;
        public uint AttackerID;
        public Point Location;
        public MirDirection Direction;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            AttackerID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(AttackerID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }
    //伤害显示
    public sealed class DamageIndicator : Packet
    {
        public int Damage;
        public DamageType Type;
        public uint ObjectID;

        public override short Index
        {
            get { return (short)ServerPacketIds.DamageIndicator; }
        }

        protected override void ReadPacket(BinaryReader reader)
        {
            Damage = reader.ReadInt32();
            Type = (DamageType)reader.ReadByte();
            ObjectID = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Damage);
            writer.Write((byte)Type);
            writer.Write(ObjectID);
        }
    }
    //状态变化
    public sealed class DuraChanged : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.DuraChanged; }
        }

        public ulong UniqueID;
        public ushort CurrentDura;

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
            CurrentDura = reader.ReadUInt16();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
            writer.Write(CurrentDura);
        }
    }
    //血量变化,主要针对本玩家的
    public sealed class HealthChanged : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.HealthChanged; }
        }

        public ushort HP, MP;

        protected override void ReadPacket(BinaryReader reader)
        {
            HP = reader.ReadUInt16();
            MP = reader.ReadUInt16();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(HP);
            writer.Write(MP);
        }
    }
    //删除物品
    public sealed class DeleteItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.DeleteItem; }
        }

        public ulong UniqueID;
        public uint Count;

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
            Count = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
            writer.Write(Count);
        }
    }
    //死亡
    public sealed class Death : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.Death; }
        }

        public Point Location;
        public MirDirection Direction;

        protected override void ReadPacket(BinaryReader reader)
        {
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }
    //对象死亡
    public sealed class ObjectDied : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectDied; }
        }

        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;
        public byte Type;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
            Type = reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
            writer.Write(Type);
        }
    }
    //名字颜色变化
    public sealed class ColourChanged : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ColourChanged; }
        }

        public Color NameColour;

        protected override void ReadPacket(BinaryReader reader)
        {
            NameColour = Color.FromArgb(reader.ReadInt32());
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(NameColour.ToArgb());
        }
    }
    //对象颜色变化
    public sealed class ObjectColourChanged : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectColourChanged; }
        }

        public uint ObjectID;
        public Color NameColour;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            NameColour = Color.FromArgb(reader.ReadInt32());
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(NameColour.ToArgb());
        }
    }

    //行会颜色变化,改名了?
    public sealed class ObjectGuildNameChanged : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectGuildNameChanged; }
        }

        public uint ObjectID;
        public string GuildName;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            GuildName = reader.ReadString();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(GuildName);
        }
    }
    public sealed class GainExperience : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.GainExperience; }
        }

        public uint Amount;

        protected override void ReadPacket(BinaryReader reader)
        {
            Amount = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Amount);
        }
    }

    //等级变化
    public sealed class LevelChanged : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.LevelChanged; }
        }

        public ushort Level;
        public long Experience, MaxExperience;

        protected override void ReadPacket(BinaryReader reader)
        {
            Level = reader.ReadUInt16();
            Experience = reader.ReadInt64();
            MaxExperience = reader.ReadInt64();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Level);
            writer.Write(Experience);
            writer.Write(MaxExperience);
        }
    }
    //对象调整？
    public sealed class ObjectLeveled : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectLeveled; }
        }

        public uint ObjectID;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
        }
    }
    //对象收货？对象采集?
    public sealed class ObjectHarvest : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectHarvest; }
        }

        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }
    //
    public sealed class ObjectHarvested : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectHarvested; }
        }

        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }

    }
    //NPC
    public sealed class ObjectNPC : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectNpc; }
        }

        public uint ObjectID;
        public string Name = string.Empty;

        public Color NameColour;
        public ushort Image;
        public Color Colour;
        public Point Location;
        public MirDirection Direction;
        public List<int> QuestIDs = new List<int>();

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Name = reader.ReadString();
            NameColour = Color.FromArgb(reader.ReadInt32());
            Image = reader.ReadUInt16();
            Colour = Color.FromArgb(reader.ReadInt32());
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();

            int count = reader.ReadInt32();

            for (var i = 0; i < count; i++)
                QuestIDs.Add(reader.ReadInt32());
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Name);
            writer.Write(NameColour.ToArgb());
            writer.Write(Image);
            writer.Write(Colour.ToArgb());
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);

            writer.Write(QuestIDs.Count);

            for (int i = 0; i < QuestIDs.Count; i++)
                writer.Write(QuestIDs[i]);
        }
    }
    //NPC信息的返回
    public sealed class NPCResponse : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCResponse; } }

        public List<string> Page;

        protected override void ReadPacket(BinaryReader reader)
        {
            Page = new List<string>();

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
                Page.Add(reader.ReadString());
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Page.Count);

            for (int i = 0; i < Page.Count; i++)
                writer.Write(Page[i]);
        }
    }
    public sealed class ObjectHide : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ObjectHide; } }

        public uint ObjectID;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
        }
    }
    public sealed class ObjectShow : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ObjectShow; } }

        public uint ObjectID;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
        }
    }
    public sealed class Poisoned : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.Poisoned; } }

        public PoisonType Poison;

        protected override void ReadPacket(BinaryReader reader)
        {
            Poison = (PoisonType)reader.ReadUInt16();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((ushort)Poison);
        }
    }
    public sealed class ObjectPoisoned : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ObjectPoisoned; } }

        public uint ObjectID;
        public PoisonType Poison;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Poison = (PoisonType)reader.ReadUInt16();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write((ushort)Poison);
        }
    }
    //改变地图？
    public sealed class MapChanged : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.MapChanged; }
        }

        public string FileName = string.Empty;
        public string Title = string.Empty;
        public ushort MiniMap, BigMap, Music;
        public LightSetting Lights;
        public Point Location;
        public MirDirection Direction;
        public byte MapDarkLight;
        public bool CanFastRun;
        //地图信息增加安全区,用于客户端判断是否在安全区，在安全区则可以穿人，穿怪
        //安全区域
        public List<SafeZoneInfo> SafeZones = new List<SafeZoneInfo>();

        protected override void ReadPacket(BinaryReader reader)
        {
            FileName = reader.ReadString();
            Title = reader.ReadString();
            MiniMap = reader.ReadUInt16();
            BigMap = reader.ReadUInt16();
            Lights = (LightSetting)reader.ReadByte();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
            MapDarkLight = reader.ReadByte();
            Music = reader.ReadUInt16();
            CanFastRun = reader.ReadBoolean();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                SafeZones.Add(new SafeZoneInfo(reader));
            }
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(FileName);
            writer.Write(Title);
            writer.Write(MiniMap);
            writer.Write(BigMap);
            writer.Write((byte)Lights);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
            writer.Write(MapDarkLight);
            writer.Write(Music);
            writer.Write(CanFastRun);
            writer.Write(SafeZones.Count);
            for (int i = 0; i < SafeZones.Count; i++)
            {
                SafeZones[i].Save(writer);
            }
        }
    }
    public sealed class ObjectTeleportOut : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ObjectTeleportOut; } }

        public uint ObjectID;
        public byte Type;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Type = reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Type);
        }
    }
    public sealed class ObjectTeleportIn : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ObjectTeleportIn; } }

        public uint ObjectID;
        public byte Type;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Type = reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Type);
        }
    }
    public sealed class TeleportIn : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.TeleportIn; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    public sealed class NPCGoods : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCGoods; } }

        public List<UserItem> List = new List<UserItem>();
        public float Rate;
        public PanelType Type;

        protected override void ReadPacket(BinaryReader reader)
        {
            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
                List.Add(new UserItem(reader));

            Rate = reader.ReadSingle();
            Type = (PanelType)reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(List.Count);

            for (int i = 0; i < List.Count; i++)
                List[i].Save(writer);

            writer.Write(Rate);
            writer.Write((byte)Type);
        }
    }
    public sealed class NPCSell : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCSell; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    public sealed class NPCRepair : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCRepair; } }
        public float Rate;

        protected override void ReadPacket(BinaryReader reader)
        {
            Rate = reader.ReadSingle();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Rate);
        }
    }
    public sealed class NPCSRepair : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCSRepair; } }

        public float Rate;

        protected override void ReadPacket(BinaryReader reader)
        {
            Rate = reader.ReadSingle();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Rate);
        }
    }

    //寄存物品到NPC处
    public sealed class DepositItemCollect : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.DepositItemCollect; }
        }

        public int From, To;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
            writer.Write(Success);
        }
    }

    //NPC收集物品
    public sealed class NPCItemCollect : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCItemCollect; } }

        public float Rate;
        public byte type;//0：装备熔炼 1：装备合成

        protected override void ReadPacket(BinaryReader reader)
        {
            Rate = reader.ReadSingle();
            type = reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Rate);
            writer.Write(type);
        }
    }
    //NPC归还收集的物品
    public sealed class RetrieveItemCollect : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.RetrieveItemCollect; }
        }

        public int From, To;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
            writer.Write(Success);
        }
    }
    //取消
    public sealed class ItemCollectCancel : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ItemCollectCancel; }
        }

        public bool Unlock;
        protected override void ReadPacket(BinaryReader reader)
        {
            Unlock = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Unlock);
        }
    }
    
    //确认升级
    public sealed class ConfirmItemCollect : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ConfirmItemCollect; }
        }

        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Success);
        }
    }


    public sealed class NPCRefine : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCRefine; } }

        public float Rate;
        public bool Refining;

        protected override void ReadPacket(BinaryReader reader)
        {
            Rate = reader.ReadSingle();
            Refining = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Rate);
            writer.Write(Refining);
        }
    }


    public sealed class NPCCheckRefine : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCCheckRefine; } }


        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }

    public sealed class NPCCollectRefine : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCCollectRefine; } }

        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            Success = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Success);
        }
    }
    //打造结婚戒指
    public sealed class NPCReplaceWedRing : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCReplaceWedRing; } }

        public float Rate;

        protected override void ReadPacket(BinaryReader reader)
        {
            Rate = reader.ReadSingle();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Rate);
        }
    }

    public sealed class NPCStorage : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCStorage; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    public sealed class SellItem : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.SellItem; } }

        public ulong UniqueID;
        public uint Count;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
            Count = reader.ReadUInt32();
            Success = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
            writer.Write(Count);
            writer.Write(Success);
        }
    }
    public sealed class RepairItem : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.RepairItem; } }

        public ulong UniqueID;

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
        }
    }
    public sealed class ItemRepaired : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ItemRepaired; } }

        public ulong UniqueID;
        public ushort MaxDura, CurrentDura;

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
            MaxDura = reader.ReadUInt16();
            CurrentDura = reader.ReadUInt16();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
            writer.Write(MaxDura);
            writer.Write(CurrentDura);
        }
    }

    //学了新的魔法技能
    public sealed class NewMagic : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.NewMagic; }
        }

        public ClientMagic Magic;
        protected override void ReadPacket(BinaryReader reader)
        {
            Magic = new ClientMagic(reader);
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            Magic.Save(writer);
        }
    }
    public sealed class RemoveMagic : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.RemoveMagic; }
        }

        public int PlaceId;
        protected override void ReadPacket(BinaryReader reader)
        {
            PlaceId = reader.ReadInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(PlaceId);
        }

    }
    //技能升级，升经验
    public sealed class MagicLeveled : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.MagicLeveled; }
        }

        public Spell Spell;
        public byte Level;
        public ushort Experience;

        protected override void ReadPacket(BinaryReader reader)
        {
            Spell = (Spell)reader.ReadByte();
            Level = reader.ReadByte();
            Experience = reader.ReadUInt16();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Spell);
            writer.Write(Level);
            writer.Write(Experience);
        }
    }
    //在某个目标释放魔法
    public sealed class Magic : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.Magic; } }

        public Spell Spell;
        public uint TargetID;
        public Point Target;
        public bool Cast;
        public byte Level;

        protected override void ReadPacket(BinaryReader reader)
        {
            Spell = (Spell)reader.ReadByte();
            TargetID = reader.ReadUInt32();
            Target = new Point(reader.ReadInt32(), reader.ReadInt32());
            Cast = reader.ReadBoolean();
            Level = reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Spell);
            writer.Write(TargetID);
            writer.Write(Target.X);
            writer.Write(Target.Y);
            writer.Write(Cast);
            writer.Write(Level);
        }
    }
    public sealed class MagicDelay : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.MagicDelay; } }

        public Spell Spell;
        public long Delay;

        protected override void ReadPacket(BinaryReader reader)
        {
            Spell = (Spell)reader.ReadByte();
            Delay = reader.ReadInt64();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Spell);
            writer.Write(Delay);
        }
    }
    public sealed class MagicCast : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.MagicCast; } }

        public Spell Spell;

        protected override void ReadPacket(BinaryReader reader)
        {
            Spell = (Spell)reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Spell);
        }
    }
    //玩家释放魔法
    public sealed class ObjectMagic : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ObjectMagic; } }

        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;

        public Spell Spell;
        public uint TargetID;
        public Point Target;
        public bool Cast;
        public byte Level;
        public bool SelfBroadcast = false;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();

            Spell = (Spell)reader.ReadByte();
            TargetID = reader.ReadUInt32();
            Target = new Point(reader.ReadInt32(), reader.ReadInt32());
            Cast = reader.ReadBoolean();
            Level = reader.ReadByte();
            SelfBroadcast = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);

            writer.Write((byte)Spell);
            writer.Write(TargetID);
            writer.Write(Target.X);
            writer.Write(Target.Y);
            writer.Write(Cast);
            writer.Write(Level);
            writer.Write(SelfBroadcast);
        }
    }



    public sealed class ObjectEffect : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ObjectEffect; } }

        public uint ObjectID;
        public SpellEffect Effect;
        public uint EffectType;
        public uint DelayTime = 0;
        public uint Time = 0;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Effect = (SpellEffect)reader.ReadByte();
            EffectType = reader.ReadUInt32();
            DelayTime = reader.ReadUInt32();
            Time = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write((byte)Effect);
            writer.Write(EffectType);
            writer.Write(DelayTime);
            writer.Write(Time);
        }
    }
    public sealed class RangeAttack : Packet //ArcherTest
    {
        public override short Index { get { return (short)ServerPacketIds.RangeAttack; } }

        public uint TargetID;
        public Point Target;
        public Spell Spell;

        protected override void ReadPacket(BinaryReader reader)
        {
            TargetID = reader.ReadUInt32();
            Target = new Point(reader.ReadInt32(), reader.ReadInt32());
            Spell = (Spell)reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(TargetID);
            writer.Write(Target.X);
            writer.Write(Target.Y);
            writer.Write((byte)Spell);
        }
    }
    //向某个方向推动
    public sealed class Pushed : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.Pushed; }
        }

        public Point Location;
        public MirDirection Direction;


        protected override void ReadPacket(BinaryReader reader)
        {
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }
    //被推动
    public sealed class ObjectPushed : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectPushed; }
        }

        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }
    //对象的名称
    public sealed class ObjectName : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ObjectName; } }

        public uint ObjectID;
        public string Name = string.Empty;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Name = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Name);
        }
    }
    //仓库存储的物品
    public sealed class UserStorage : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.UserStorage; } }

        public UserItem[] Storage;

        protected override void ReadPacket(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return;

            Storage = new UserItem[reader.ReadInt32()];
            for (int i = 0; i < Storage.Length; i++)
            {
                if (!reader.ReadBoolean()) continue;
                Storage[i] = new UserItem(reader);
            }
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Storage != null);
            if (Storage == null) return;

            writer.Write(Storage.Length);
            for (int i = 0; i < Storage.Length; i++)
            {
                writer.Write(Storage[i] != null);
                if (Storage[i] == null) continue;

                Storage[i].Save(writer);
            }
        }
    }
    public sealed class SwitchGroup : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.SwitchGroup; } }

        public bool AllowGroup;
        protected override void ReadPacket(BinaryReader reader)
        {
            AllowGroup = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AllowGroup);
        }
    }
    public sealed class DeleteGroup : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.DeleteGroup; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    public sealed class DeleteMember : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.DeleteMember; } }

        public string Name = string.Empty;
        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
        }
    }
    public sealed class GroupInvite : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.GroupInvite; } }

        public string Name = string.Empty;
        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
        }
    }
    public sealed class AddMember : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.AddMember; } }

        public string Name = string.Empty;
        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
        }
    }
    public sealed class Revived : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.Revived; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    public sealed class ObjectRevived : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ObjectRevived; } }
        public uint ObjectID;
        public bool Effect;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Effect = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Effect);
        }
    }
    public sealed class SpellToggle : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.SpellToggle; } }
        public Spell Spell;
        public bool CanUse;

        protected override void ReadPacket(BinaryReader reader)
        {
            Spell = (Spell)reader.ReadByte();
            CanUse = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Spell);
            writer.Write(CanUse);
        }
    }
    //对象的血量
    public sealed class ObjectHealth : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ObjectHealth; } }
        public uint ObjectID;
        public uint HP,MaxHP;//具体的血量
        public byte  Expire;//这个好像没什么用吧

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            HP = reader.ReadUInt32();
            MaxHP= reader.ReadUInt32();
            Expire = reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(HP);
            writer.Write(MaxHP);
            writer.Write(Expire);
        }
    }
    //地图上的魔法效果
    public sealed class MapEffect : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.MapEffect; } }

        public Point Location;
        public SpellEffect Effect;
        public byte Value;

        protected override void ReadPacket(BinaryReader reader)
        {
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Effect = (SpellEffect)reader.ReadByte();
            Value = reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Effect);
            writer.Write(Value);
        }
    }
    //对象范围攻击
    public sealed class ObjectRangeAttack : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectRangeAttack; }
        }

        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;
        public uint TargetID;
        public Point Target;
        public byte Type;//0 1 2 客户端怪物采用的范围攻击方式 1 2 3
        public Spell Spell;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
            TargetID = reader.ReadUInt32();
            Target = new Point(reader.ReadInt32(), reader.ReadInt32());
            Type = reader.ReadByte();
            Spell = (Spell)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
            writer.Write(TargetID);
            writer.Write(Target.X);
            writer.Write(Target.Y);
            writer.Write(Type);
            writer.Write((byte)Spell);
        }
    }
    public sealed class AddBuff : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.AddBuff; } }

        public BuffType Type;
        public string Caster = string.Empty;
        public uint ObjectID;
        public bool Visible;
        public long Expire;
        public int[] Values;
        public bool Infinite;

        protected override void ReadPacket(BinaryReader reader)
        {
            Type = (BuffType)reader.ReadByte();
            Caster = reader.ReadString();
            Visible = reader.ReadBoolean();
            ObjectID = reader.ReadUInt32();
            Expire = reader.ReadInt64();

            Values = new int[reader.ReadInt32()];
            for (int i = 0; i < Values.Length; i++)
            {
                Values[i] = reader.ReadInt32();
            }

            Infinite = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            writer.Write(Caster);
            writer.Write(Visible);
            writer.Write(ObjectID);
            writer.Write(Expire);

            writer.Write(Values.Length);
            for (int i = 0; i < Values.Length; i++)
            {
                writer.Write(Values[i]);
            }

            writer.Write(Infinite);
        }
    }
    public sealed class RemoveBuff : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.RemoveBuff; } }

        public BuffType Type;
        public uint ObjectID;

        protected override void ReadPacket(BinaryReader reader)
        {
            Type = (BuffType)reader.ReadByte();
            ObjectID = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            writer.Write(ObjectID);
        }
    }
    public sealed class ObjectHidden : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ObjectHidden; } }
        public uint ObjectID;
        public bool Hidden;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Hidden = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Hidden);
        }
    }
    public sealed class RefreshItem : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.RefreshItem; } }
        public UserItem Item;
        protected override void ReadPacket(BinaryReader reader)
        {
            Item = new UserItem(reader);
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            Item.Save(writer);
        }
    }
    public sealed class ObjectSpell : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectSpell; }
        }

        public uint ObjectID;
        public Point Location;
        public Spell Spell;
        public MirDirection Direction;
        public bool Param;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Spell = (Spell)reader.ReadByte();
            Direction = (MirDirection)reader.ReadByte();
            Param = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Spell);
            writer.Write((byte)Direction);
            writer.Write(Param);
        }
    }
    public sealed class UserDash : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.UserDash; }
        }

        public Point Location;
        public MirDirection Direction;


        protected override void ReadPacket(BinaryReader reader)
        {
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }
    public sealed class ObjectDash : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectDash; }
        }

        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;


        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }
    public sealed class UserDashFail : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.UserDashFail; }
        }

        public Point Location;
        public MirDirection Direction;


        protected override void ReadPacket(BinaryReader reader)
        {
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }
    public sealed class ObjectDashFail : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectDashFail; }
        }

        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;


        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }
    public sealed class RemoveDelayedExplosion : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.RemoveDelayedExplosion; } }

        public uint ObjectID;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
        }
    }
    //NPC寄售，金币
    public sealed class NPCConsign : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCConsign; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    //NPC寄售，元宝
    public sealed class NPCConsignCredit : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCConsignCredit; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    //NPC寄售，金币+元宝
    public sealed class NPCConsignDoulbe : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCConsignDoulbe; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }


    //返回市场的内容
    public sealed class NPCMarket : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCMarket; } }

        public List<ClientAuction> Listings = new List<ClientAuction>();
        public int cpage;//当前页数
        public int pageCount;//总页数
        public bool UserMode; //UserMode：卖：true, 买：false

        protected override void ReadPacket(BinaryReader reader)
        {
            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
                Listings.Add(new ClientAuction(reader));

            cpage = reader.ReadInt32();
            pageCount = reader.ReadInt32();
            UserMode = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Listings.Count);

            for (int i = 0; i < Listings.Count; i++)
                Listings[i].Save(writer);
            writer.Write(cpage);
            writer.Write(pageCount);
            writer.Write(UserMode);
        }
    }
   
    //寄卖物品
    public sealed class ConsignItem : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ConsignItem; } }

        public ulong UniqueID;
        public bool Success;
        //增加返回信息，给客户端提醒
        public string msg= "寄卖出错";

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
            Success = reader.ReadBoolean();
            msg = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
            writer.Write(Success);
            writer.Write(msg);
        }
    }
    public sealed class MarketFail : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.MarketFail; } }

        public byte Reason;

        /*
         * 0: Dead.
         * 1: Not talking to TrustMerchant.
         * 2: Already Sold.
         * 3: Expired.
         * 4: Not enough Gold.
         * 5: Too heavy or not enough bag space.
         * 6: You cannot buy your own items.
         * 7: Trust Merchant is too far.
         * 8: Too much Gold.
         */

        protected override void ReadPacket(BinaryReader reader)
        {
            Reason = reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Reason);
        }
    }
    public sealed class MarketSuccess : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.MarketSuccess; } }

        public string Message = string.Empty;

        protected override void ReadPacket(BinaryReader reader)
        {
            Message = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Message);
        }
    }
    public sealed class ObjectSitDown : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ObjectSitDown; } }
        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;
        public bool Sitting;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
            Sitting = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
            writer.Write(Sitting);
        }
    }
    public sealed class InTrapRock : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.InTrapRock; } }
        public bool Trapped;

        protected override void ReadPacket(BinaryReader reader)
        {
            Trapped = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Trapped);
        }
    }
    public sealed class BaseStatsInfo : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.BaseStatsInfo; }
        }

        public BaseStats Stats;

        protected override void ReadPacket(BinaryReader reader)
        {
            Stats = new BaseStats(reader);
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            Stats.Save(writer);
        }
    }

    public sealed class UserName : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.UserName; } }
        public ulong Id;
        public string Name;
        protected override void ReadPacket(BinaryReader reader)
        {
            Id = reader.ReadUInt64();
            Name = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Name);
        }
    }
    public sealed class ChatItemStats : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ChatItemStats; } }
        public ulong ChatItemId;
        public UserItem Stats;
        protected override void ReadPacket(BinaryReader reader)
        {
            ChatItemId = reader.ReadUInt64();
            Stats = new UserItem(reader);
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ChatItemId);
            if (Stats != null) Stats.Save(writer);
        }
    }

    public sealed class GuildNoticeChange : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.GuildNoticeChange; }
        }
        public int update = 0;
        public List<string> notice = new List<string>();
        protected override void ReadPacket(BinaryReader reader)
        {
            update = reader.ReadInt32();
            for (int i = 0; i < update; i++)
                notice.Add(reader.ReadString());
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            if (update < 0)
            {
                writer.Write(update);
                return;
            }
            writer.Write(notice.Count);
            for (int i = 0; i < notice.Count; i++)
                writer.Write(notice[i]);
        }
    }

    public sealed class GuildMemberChange : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.GuildMemberChange; }
        }
        public string Name = string.Empty;
        public byte Status = 0;
        public byte RankIndex = 0;
        public List<Rank> Ranks = new List<Rank>();
        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
            RankIndex = reader.ReadByte();
            Status = reader.ReadByte();
            if (Status > 5)
            {
                int rankcount = reader.ReadInt32();
                for (int i = 0; i < rankcount; i++)
                    Ranks.Add(new Rank(reader));
            }
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(RankIndex);
            writer.Write(Status);
            if (Status > 5)
            {
                writer.Write(Ranks.Count);
                for (int i = 0; i < Ranks.Count; i++)
                    Ranks[i].Save(writer);
            }
        }
    }

    public sealed class GuildStatus : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.GuildStatus; }
        }
        public string GuildName = string.Empty;
        public string GuildRankName = string.Empty;
        public byte Level;
        public long Experience;
        public long MaxExperience;
        public uint Gold;
        public byte SparePoints;
        public int MemberCount;
        public int MaxMembers;
        public bool Voting;
        public byte ItemCount;
        public byte BuffCount;
        public RankOptions MyOptions;
        public int MyRankId;

        protected override void ReadPacket(BinaryReader reader)
        {
            GuildName = reader.ReadString();
            GuildRankName = reader.ReadString();
            Level = reader.ReadByte();
            Experience = reader.ReadInt64();
            MaxExperience = reader.ReadInt64();
            Gold = reader.ReadUInt32();
            SparePoints = reader.ReadByte();
            MemberCount = reader.ReadInt32();
            MaxMembers = reader.ReadInt32();
            Voting = reader.ReadBoolean();
            ItemCount = reader.ReadByte();
            BuffCount = reader.ReadByte();
            MyOptions = (RankOptions)reader.ReadByte();
            MyRankId = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(GuildName);
            writer.Write(GuildRankName);
            writer.Write(Level);
            writer.Write(Experience);
            writer.Write(MaxExperience);
            writer.Write(Gold);
            writer.Write(SparePoints);
            writer.Write(MemberCount);
            writer.Write(MaxMembers);
            writer.Write(Voting);
            writer.Write(ItemCount);
            writer.Write(BuffCount);
            writer.Write((byte)MyOptions);
            writer.Write(MyRankId);
        }
    }
    public sealed class GuildInvite : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.GuildInvite; } }

        public string Name = string.Empty;
        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
        }
    }
    public sealed class GuildExpGain : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.GuildExpGain; } }

        public uint Amount = 0;
        protected override void ReadPacket(BinaryReader reader)
        {
            Amount = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Amount);
        }
    }
    public sealed class GuildNameRequest : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.GuildNameRequest; } }
        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }

    public sealed class GuildStorageGoldChange : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.GuildStorageGoldChange; } }
        public uint Amount = 0;
        public byte Type = 0;
        public string Name = string.Empty;

        protected override void ReadPacket(BinaryReader reader)
        {
            Amount = reader.ReadUInt32();
            Type = reader.ReadByte();
            Name = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Amount);
            writer.Write(Type);
            writer.Write(Name);
        }
    }
    public sealed class GuildStorageItemChange : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.GuildStorageItemChange; } }
        public ulong User = 0;
        public byte Type = 0;
        public int To = 0;
        public int From = 0;
        public GuildStorageItem Item = null;
        protected override void ReadPacket(BinaryReader reader)
        {
            Type = reader.ReadByte();
            To = reader.ReadInt32();
            From = reader.ReadInt32();
            User = (ulong)reader.ReadInt64();
            if (!reader.ReadBoolean()) return;
            Item = new GuildStorageItem();
            Item.UserId = reader.ReadInt64();
            Item.Item = new UserItem(reader);
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Type);
            writer.Write(To);
            writer.Write(From);
            writer.Write(User);
            writer.Write(Item != null);
            if (Item == null) return;
            writer.Write(Item.UserId);
            Item.Item.Save(writer);
        }
    }
    public sealed class GuildStorageList : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.GuildStorageList; }
        }
        public GuildStorageItem[] Items;
        protected override void ReadPacket(BinaryReader reader)
        {
            Items = new GuildStorageItem[reader.ReadInt32()];
            for (int i = 0; i < Items.Length; i++)
            {
                if (reader.ReadBoolean() == true)
                    Items[i] = new GuildStorageItem(reader);
            }
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Items.Length);
            for (int i = 0; i < Items.Length; i++)
            {
                writer.Write(Items[i] != null);
                if (Items[i] != null)
                    Items[i].save(writer);
            }
        }

    }
    public sealed class GuildRequestWar : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.GuildRequestWar; } }
        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    public sealed class DefaultNPC : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.DefaultNPC; } }

        public uint ObjectID;
        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
        }
    }

    public sealed class NPCUpdate : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCUpdate; } }

        public uint NPCID;

        protected override void ReadPacket(BinaryReader reader)
        {
            NPCID = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(NPCID);
        }
    }


    public sealed class NPCImageUpdate : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCImageUpdate; } }

        public long ObjectID;
        public ushort Image;
        public Color Colour;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadInt64();
            Image = reader.ReadUInt16();
            Colour = Color.FromArgb(reader.ReadInt32());
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Image);
            writer.Write(Colour.ToArgb());
        }
    }
    public sealed class MountUpdate : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.MountUpdate; } }

        public long ObjectID;
        public short MountType;
        public bool RidingMount;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadInt64();
            MountType = reader.ReadInt16();
            RidingMount = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(MountType);
            writer.Write(RidingMount);
        }
    }

    public sealed class TransformUpdate : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.TransformUpdate; } }

        public long ObjectID;
        public short TransformType;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadInt64();
            TransformType = reader.ReadInt16();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(TransformType);
        }
    }

    public sealed class EquipSlotItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.EquipSlotItem; }
        }

        public MirGridType Grid;
        public ulong UniqueID;
        public int To;
        public bool Success;
        public MirGridType GridTo;

        protected override void ReadPacket(BinaryReader reader)
        {
            Grid = (MirGridType)reader.ReadByte();
            UniqueID = reader.ReadUInt64();
            To = reader.ReadInt32();
            GridTo = (MirGridType)reader.ReadByte();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Grid);
            writer.Write(UniqueID);
            writer.Write(To);
            writer.Write((byte)GridTo);
            writer.Write(Success);
        }
    }

    public sealed class FishingUpdate : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.FishingUpdate; } }

        public long ObjectID;
        public bool Fishing;//是否正在钓鱼，如果是否，则客户端关闭钓鱼状态窗口
        public int ProgressPercent;
        public int ChancePercent;
        public Point FishingPoint;
        public bool FoundFish;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadInt64();
            Fishing = reader.ReadBoolean();
            ProgressPercent = reader.ReadInt32();
            ChancePercent = reader.ReadInt32();
            FishingPoint = new Point(reader.ReadInt32(), reader.ReadInt32());
            FoundFish = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Fishing);
            writer.Write(ProgressPercent);
            writer.Write(ChancePercent);
            writer.Write(FishingPoint.X);
            writer.Write(FishingPoint.Y);
            writer.Write(FoundFish);
        }
    }

    //public sealed class UpdateQuests : Packet
    //{
    //    public override short Index
    //    {
    //        get { return (short)ServerPacketIds.UpdateQuests; }
    //    }

    //    public List<ClientQuestProgress> CurrentQuests = new List<ClientQuestProgress>();
    //    public List<int> CompletedQuests = new List<int>();

    //    protected override void ReadPacket(BinaryReader reader)
    //    {
    //        int count = reader.ReadInt32();
    //        for (var i = 0; i < count; i++)
    //            CurrentQuests.Add(new ClientQuestProgress(reader));

    //        count = reader.ReadInt32();
    //        for (var i = 0; i < count; i++)
    //            CompletedQuests.Add(reader.ReadInt32());
    //    }
    //    protected override void WritePacket(BinaryWriter writer)
    //    {
    //        writer.Write(CurrentQuests.Count);
    //        foreach (var q in CurrentQuests)
    //            q.Save(writer);

    //        writer.Write(CompletedQuests.Count);
    //        foreach (int q in CompletedQuests)
    //            writer.Write(q);
    //    }
    //}


    public sealed class ChangeQuest : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ChangeQuest; }
        }

        public ClientQuestProgress Quest = new ClientQuestProgress();
        public QuestState QuestState;
        public bool TrackQuest;

        protected override void ReadPacket(BinaryReader reader)
        {
            Quest = new ClientQuestProgress(reader);
            QuestState = (QuestState)reader.ReadByte();
            TrackQuest = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            Quest.Save(writer);
            writer.Write((byte)QuestState);
            writer.Write(TrackQuest);
        }
    }

    public sealed class CompleteQuest : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.CompleteQuest; }
        }

        public List<int> CompletedQuests = new List<int>();

        protected override void ReadPacket(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
                CompletedQuests.Add(reader.ReadInt32());
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(CompletedQuests.Count);
            foreach (int q in CompletedQuests)
                writer.Write(q);
        }
    }

    public sealed class ShareQuest : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ShareQuest; }
        }

        public int QuestIndex;
        public string SharerName;

        protected override void ReadPacket(BinaryReader reader)
        {
            QuestIndex = reader.ReadInt32();
            SharerName = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(QuestIndex);
            writer.Write(SharerName);
        }
    }


    public sealed class NewQuestInfo : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.NewQuestInfo; }
        }

        public ClientQuestInfo Info;

        protected override void ReadPacket(BinaryReader reader)
        {
            Info = new ClientQuestInfo(reader);
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            Info.Save(writer);
        }
    }

    public sealed class GainedQuestItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.GainedQuestItem; }
        }

        public UserItem Item;

        protected override void ReadPacket(BinaryReader reader)
        {
            Item = new UserItem(reader);
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            Item.Save(writer);
        }
    }

    public sealed class DeleteQuestItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.DeleteQuestItem; }
        }

        public ulong UniqueID;
        public uint Count;

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
            Count = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
            writer.Write(Count);
        }
    }

    public sealed class GameShopInfo : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.GameShopInfo; }
        }

        public GameShopItem Item;
        public int StockLevel;

        protected override void ReadPacket(BinaryReader reader)
        {
            Item = new GameShopItem(reader, true);
            StockLevel = reader.ReadInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            Item.Save(writer, true);
            writer.Write(StockLevel);
        }
    }
    //刷新商品的库存
    public sealed class GameShopStock : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.GameShopStock; }
        }

        public int GIndex;
        public int StockLevel;

        protected override void ReadPacket(BinaryReader reader)
        {
            GIndex = reader.ReadInt32();
            StockLevel = reader.ReadInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(GIndex);
            writer.Write(StockLevel);
        }
    }

    public sealed class CancelReincarnation : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.CancelReincarnation; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }

    public sealed class RequestReincarnation : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.RequestReincarnation; } }


        protected override void ReadPacket(BinaryReader reader)
        {
        }

        protected override void WritePacket(BinaryWriter writer)
        {
        }

    }

    public sealed class UserBackStep : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.UserBackStep; }
        }

        public Point Location;
        public MirDirection Direction;


        protected override void ReadPacket(BinaryReader reader)
        {
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }

    public sealed class ObjectBackStep : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectBackStep; }
        }

        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;
        public int Distance;


        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
            Distance = reader.ReadInt16();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
            writer.Write(Distance);
        }
    }

    public sealed class UserDashAttack : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.UserDashAttack; }
        }

        public Point Location;
        public MirDirection Direction;

        protected override void ReadPacket(BinaryReader reader)
        {
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }

    public sealed class ObjectDashAttack : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectDashAttack; }
        }

        public uint ObjectID;
        public Point Location;
        public MirDirection Direction;
        public int Distance;


        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
            Distance = reader.ReadInt16();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
            writer.Write(Distance);
        }
    }

    public sealed class UserAttackMove : Packet//warrior skill - SlashingBurst move packet 
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.UserAttackMove; }
        }


        public Point Location;
        public MirDirection Direction;

        protected override void ReadPacket(BinaryReader reader)
        {
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Direction = (MirDirection)reader.ReadByte();
        }


        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write((byte)Direction);
        }
    }

    public sealed class CombineItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.CombineItem; }
        }

        public ulong IDFrom, IDTo;
        public bool Success;
        public bool Destroy;

        protected override void ReadPacket(BinaryReader reader)
        {
            IDFrom = reader.ReadUInt64();
            IDTo = reader.ReadUInt64();
            Success = reader.ReadBoolean();
            Destroy = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(IDFrom);
            writer.Write(IDTo);
            writer.Write(Success);
            writer.Write(Destroy);
        }
    }

    public sealed class ItemUpgraded : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ItemUpgraded; }
        }

        public UserItem Item;

        protected override void ReadPacket(BinaryReader reader)
        {
            Item = new UserItem(reader);
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            Item.Save(writer);
        }
    }

    public sealed class SetConcentration : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.SetConcentration; } }

        public uint ObjectID;
        public bool Enabled;
        public bool Interrupted;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Enabled = reader.ReadBoolean();
            Interrupted = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Enabled);
            writer.Write(Interrupted);
        }
    }
    public sealed class SetObjectConcentration : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.SetObjectConcentration; } }

        public uint ObjectID;
        public bool Enabled;
        public bool Interrupted;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Enabled = reader.ReadBoolean();
            Interrupted = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Enabled);
            writer.Write(Interrupted);
        }
    }
    public sealed class SetElemental : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.SetElemental; } }

        public uint ObjectID;
        public bool Enabled;
        public uint Value;
        public uint ElementType;
        public uint ExpLast;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Enabled = reader.ReadBoolean();
            Value = reader.ReadUInt32();
            ElementType = reader.ReadUInt32();
            ExpLast = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Enabled);
            writer.Write(Value);
            writer.Write(ElementType);
            writer.Write(ExpLast);
        }
    }
    public sealed class SetObjectElemental : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.SetObjectElemental; } }

        public uint ObjectID;
        public bool Enabled;
        public bool Casted;
        public uint Value;
        public uint ElementType;
        public uint ExpLast;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Enabled = reader.ReadBoolean();
            Casted = reader.ReadBoolean();
            Value = reader.ReadUInt32();
            ElementType = reader.ReadUInt32();
            ExpLast = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Enabled);
            writer.Write(Casted);
            writer.Write(Value);
            writer.Write(ElementType);
            writer.Write(ExpLast);
        }
    }

    public sealed class ObjectDeco : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ObjectDeco; }
        }

        public uint ObjectID;
        public Point Location;
        public ushort Image;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Image = reader.ReadUInt16();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(Image);
        }
    }
    public sealed class ObjectSneaking : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ObjectSneaking; } }
        public uint ObjectID;
        public bool SneakingActive;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            SneakingActive = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(SneakingActive);
        }
    }

    public sealed class ObjectLevelEffects : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ObjectLevelEffects; } }

        public uint ObjectID;
        public LevelEffects LevelEffects;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            LevelEffects = (LevelEffects)reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write((byte)LevelEffects);
        }
    }

    public sealed class SetBindingShot : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.SetBindingShot; } }

        public uint ObjectID;
        public bool Enabled;
        public long Value;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Enabled = reader.ReadBoolean();
            Value = reader.ReadInt64();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Enabled);
            writer.Write(Value);
        }
    }

    public sealed class SendOutputMessage : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.SendOutputMessage; } }

        public string Message;
        public OutputMessageType Type;

        protected override void ReadPacket(BinaryReader reader)
        {
            Message = reader.ReadString();
            Type = (OutputMessageType)reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Message);
            writer.Write((byte)Type);
        }
    }
    public sealed class NPCAwakening : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCAwakening; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    public sealed class NPCDisassemble : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCDisassemble; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    public sealed class NPCDowngrade : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCDowngrade; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    public sealed class NPCReset : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCReset; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    //觉醒需要的材料
    public sealed class AwakeningNeedMaterials : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.AwakeningNeedMaterials; } }

        public ItemInfo[] Materials;
        public byte[] MaterialsCount;

        protected override void ReadPacket(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return;

            int count = reader.ReadInt32();
            Materials = new ItemInfo[count];
            MaterialsCount = new byte[count];

            for (int i = 0; i < Materials.Length; i++)
            {
                if (!reader.ReadBoolean()) continue;
                Materials[i] = new ItemInfo(reader);
                MaterialsCount[i] = reader.ReadByte();
            }
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Materials != null);
            if (Materials == null) return;

            writer.Write(Materials.Length);
            for (int i = 0; i < Materials.Length; i++)
            {
                writer.Write(Materials[i] != null);
                if (Materials[i] == null) continue;

                Materials[i].Save(writer);
                writer.Write(MaterialsCount[i]);
            }
        }
    }

    public sealed class AwakeningLockedItem : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.AwakeningLockedItem; } }

        public ulong UniqueID;
        public bool Locked;

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
            Locked = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
            writer.Write(Locked);
        }
    }

    public sealed class Awakening : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.Awakening; } }

        public int result;
        public long removeID = -1;

        protected override void ReadPacket(BinaryReader reader)
        {
            result = reader.ReadInt32();
            removeID = reader.ReadInt64();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(result);
            writer.Write(removeID);
        }
    }

    public sealed class ReceiveMail : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ReceiveMail; }
        }

        public List<ClientMail> Mail = new List<ClientMail>();

        protected override void ReadPacket(BinaryReader reader)
        {
            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
                Mail.Add(new ClientMail(reader));
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Mail.Count);

            for (int i = 0; i < Mail.Count; i++)
                Mail[i].Save(writer);
        }
    }
    public sealed class MailLockedItem : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.MailLockedItem; } }

        public ulong UniqueID;
        public bool Locked;

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
            Locked = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
            writer.Write(Locked);
        }
    }

    public sealed class MailSent : Packet
    {
        public sbyte Result;

        public override short Index
        {
            get { return (short)ServerPacketIds.MailSent; }
        }

        protected override void ReadPacket(BinaryReader reader)
        {
            Result = reader.ReadSByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Result);
        }
    }

    public sealed class MailSendRequest : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.MailSendRequest; }
        }

        protected override void ReadPacket(BinaryReader reader)
        {
        }

        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }

    public sealed class ParcelCollected : Packet
    {
        public sbyte Result;

        public override short Index
        {
            get { return (short)ServerPacketIds.ParcelCollected; }
        }

        protected override void ReadPacket(BinaryReader reader)
        {
            Result = reader.ReadSByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Result);
        }
    }

    public sealed class MailCost : Packet
    {
        public uint Cost;

        public override short Index
        {
            get { return (short)ServerPacketIds.MailCost; }
        }

        protected override void ReadPacket(BinaryReader reader)
        {
            Cost = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Cost);
        }
    }

    public sealed class ResizeInventory : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ResizeInventory; } }

        public int Size;

        protected override void ReadPacket(BinaryReader reader)
        {
            Size = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Size);
        }
    }

    public sealed class ResizeStorage : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ResizeStorage; } }

        public int Size;
        public bool HasExpandedStorage;
        public DateTime ExpiryTime;

        protected override void ReadPacket(BinaryReader reader)
        {
            Size = reader.ReadInt32();
            HasExpandedStorage = reader.ReadBoolean();
            ExpiryTime = DateTime.FromBinary(reader.ReadInt64());
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Size);
            writer.Write(HasExpandedStorage);
            writer.Write(ExpiryTime.ToBinary());
        }
    }

    public sealed class NewIntelligentCreature : Packet//IntelligentCreature
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.NewIntelligentCreature; }
        }

        public ClientIntelligentCreature Creature;
        protected override void ReadPacket(BinaryReader reader)
        {
            Creature = new ClientIntelligentCreature(reader);
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            Creature.Save(writer);
        }
    }
    //这个是干嘛，拼命的发送这个包
    public sealed class UpdateIntelligentCreatureList : Packet//IntelligentCreature
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.UpdateIntelligentCreatureList; }
        }

        public List<ClientIntelligentCreature> CreatureList = new List<ClientIntelligentCreature>();
        public bool CreatureSummoned = false;
        public IntelligentCreatureType SummonedCreatureType = IntelligentCreatureType.None;
        public int PearlCount = 0;

        protected override void ReadPacket(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                CreatureList.Add(new ClientIntelligentCreature(reader));
            CreatureSummoned = reader.ReadBoolean();
            SummonedCreatureType = (IntelligentCreatureType)reader.ReadByte();
            PearlCount = reader.ReadInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(CreatureList.Count);
            for (int i = 0; i < CreatureList.Count; i++)
                CreatureList[i].Save(writer);
            writer.Write(CreatureSummoned);
            writer.Write((byte)SummonedCreatureType);
            writer.Write(PearlCount);
        }
    }

    public sealed class IntelligentCreatureEnableRename : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.IntelligentCreatureEnableRename; }
        }

        protected override void ReadPacket(BinaryReader reader)
        {
        }

        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }

    public sealed class IntelligentCreaturePickup : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.IntelligentCreaturePickup; }
        }

        public uint ObjectID;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
        }
    }

    public sealed class NPCPearlGoods : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCPearlGoods; } }

        public List<UserItem> List = new List<UserItem>();
        public float Rate;

        protected override void ReadPacket(BinaryReader reader)
        {
            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
                List.Add(new UserItem(reader));

            Rate = reader.ReadSingle();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(List.Count);

            for (int i = 0; i < List.Count; i++)
                List[i].Save(writer);

            writer.Write(Rate);
        }
    }

    public sealed class FriendUpdate : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.FriendUpdate; }
        }

        public List<ClientFriend> Friends = new List<ClientFriend>();

        protected override void ReadPacket(BinaryReader reader)
        {
            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
                Friends.Add(new ClientFriend(reader));
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Friends.Count);

            for (int i = 0; i < Friends.Count; i++)
                Friends[i].Save(writer);
        }
    }

    public sealed class GuildBuffList : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.GuildBuffList; } }

        public byte Remove = 0;
        public List<GuildBuff> ActiveBuffs = new List<GuildBuff>();
        public List<GuildBuffInfo> GuildBuffs = new List<GuildBuffInfo>();

        protected override void ReadPacket(BinaryReader reader)
        {
            Remove = reader.ReadByte();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                ActiveBuffs.Add(new GuildBuff(reader));
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                GuildBuffs.Add(new GuildBuffInfo(reader));
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Remove);
            writer.Write(ActiveBuffs.Count);
            for (int i = 0; i < ActiveBuffs.Count; i++)
                ActiveBuffs[i].Save(writer);
            writer.Write(GuildBuffs.Count);
            for (int i = 0; i < GuildBuffs.Count; i++)
                GuildBuffs[i].Save(writer);
        }
    }
    public sealed class LoverUpdate : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.LoverUpdate; }
        }

        public string Name;
        public DateTime Date;
        public string MapName;
        public short MarriedDays;

        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
            Date = DateTime.FromBinary(reader.ReadInt64());
            MapName = reader.ReadString();
            MarriedDays = reader.ReadInt16();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Date.ToBinary());
            writer.Write(MapName);
            writer.Write(MarriedDays);
        }
    }

    public sealed class MentorUpdate : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.MentorUpdate; }
        }

        public string Name;
        public ushort Level;
        public bool Online;
        public long MenteeEXP;

        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
            Level = reader.ReadUInt16();
            Online = reader.ReadBoolean();
            MenteeEXP = reader.ReadInt64();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Level);
            writer.Write(Online);
            writer.Write(MenteeEXP);
        }
    }

    public sealed class NPCRequestInput : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.NPCRequestInput; } }

        public uint NPCID;
        public string PageName;

        protected override void ReadPacket(BinaryReader reader)
        {
            NPCID = reader.ReadUInt32();
            PageName = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(NPCID);
            writer.Write(PageName);
        }
    }
    //服务器返回的排行信息
    public sealed class Rankings : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.Rankings; } }

        public byte RankType = 0;
        public byte RankType2 = 0;
        //public int MyRank = 0;//当前角色的排行，这个在客户端自己计算（服务器端不管这个了）
        public List<Rank_Character_Info> Listings = new List<Rank_Character_Info>();

        protected override void ReadPacket(BinaryReader reader)
        {
            RankType = reader.ReadByte();
            RankType2 = reader.ReadByte();
            //MyRank = reader.ReadInt32();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Listings.Add(new Rank_Character_Info(reader));
            }
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(RankType);
            writer.Write(RankType2);
            //writer.Write(MyRank);
            writer.Write(Listings.Count);
            for (int i = 0; i < Listings.Count; i++)
                Listings[i].Save(writer);
        }
    }

    public sealed class Opendoor : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.Opendoor; } }

        public bool Close = false;
        public byte DoorIndex;

        protected override void ReadPacket(BinaryReader reader)
        {
            DoorIndex = reader.ReadByte();
            Close = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(DoorIndex);
            writer.Write(Close);
        }
    }

    public sealed class GetRentedItems : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.GetRentedItems; }
        }

        public List<ItemRentalInformation> RentedItems = new List<ItemRentalInformation>();

        protected override void ReadPacket(BinaryReader reader)
        {
            var count = reader.ReadInt32();

            for (var i = 0; i < count; i++)
                RentedItems.Add(new ItemRentalInformation(reader));
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(RentedItems.Count);

            foreach (var rentedItemInformation in RentedItems)
                rentedItemInformation.Save(writer);
        }
    }

    public sealed class ItemRentalRequest : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.ItemRentalRequest; } }

        public string Name;
        public bool Renting;

        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
            Renting = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Renting);
        }
    }

    public sealed class ItemRentalFee : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ItemRentalFee; }
        }

        public uint Amount;

        protected override void ReadPacket(BinaryReader reader)
        {
            Amount = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Amount);
        }
    }

    public sealed class ItemRentalPeriod : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ItemRentalPeriod; }
        }

        public uint Days;

        protected override void ReadPacket(BinaryReader reader)
        {
            Days = reader.ReadUInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Days);
        }
    }

    public sealed class DepositRentalItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.DepositRentalItem; }
        }

        public int From, To;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
            writer.Write(Success);
        }
    }

    public sealed class RetrieveRentalItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.RetrieveRentalItem; }
        }

        public int From, To;
        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
            Success = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
            writer.Write(Success);
        }
    }

    public sealed class UpdateRentalItem : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.UpdateRentalItem; }
        }

        public bool HasData;
        public UserItem LoanItem;

        protected override void ReadPacket(BinaryReader reader)
        {
            HasData = reader.ReadBoolean();

            if (HasData)
                LoanItem = new UserItem(reader); 
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(LoanItem != null);

            if (LoanItem != null)
                LoanItem.Save(writer);
        }
    }

    public sealed class CancelItemRental : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.CancelItemRental; }
        }

        protected override void ReadPacket(BinaryReader reader)
        { }

        protected override void WritePacket(BinaryWriter writer)
        { }
    }

    public sealed class ItemRentalLock : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ItemRentalLock; }
        }

        public bool Success;
        public bool GoldLocked;
        public bool ItemLocked;

        protected override void ReadPacket(BinaryReader reader)
        {
            Success = reader.ReadBoolean();
            GoldLocked = reader.ReadBoolean();
            ItemLocked = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Success);
            writer.Write(GoldLocked);
            writer.Write(ItemLocked);
        }
    }

    public sealed class ItemRentalPartnerLock : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ItemRentalPartnerLock; }
        }

        public bool GoldLocked;
        public bool ItemLocked;

        protected override void ReadPacket(BinaryReader reader)
        {
            GoldLocked = reader.ReadBoolean();
            ItemLocked = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(GoldLocked);
            writer.Write(ItemLocked);
        }
    }

    public sealed class CanConfirmItemRental : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.CanConfirmItemRental; }
        }

        protected override void ReadPacket(BinaryReader reader)
        { }

        protected override void WritePacket(BinaryWriter writer)
        { }
    }

    public sealed class ConfirmItemRental : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.ConfirmItemRental; }
        }

        protected override void ReadPacket(BinaryReader reader)
        { }

        protected override void WritePacket(BinaryWriter writer)
        { }
    }


    public sealed class NewRecipeInfo : Packet
    {
        public override short Index
        {
            get { return (short)ServerPacketIds.NewRecipeInfo; }
        }

        public ClientRecipeInfo Info;

        protected override void ReadPacket(BinaryReader reader)
        {
            Info = new ClientRecipeInfo(reader);
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            Info.Save(writer);
        }
    }
    public sealed class CraftItem : Packet
    {
        public override short Index { get { return (short)ServerPacketIds.CraftItem; } }

        public bool Success;

        protected override void ReadPacket(BinaryReader reader)
        {
            Success = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Success);
        }
    }
}