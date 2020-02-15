using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
/// <summary>
/// 定义所有的客户端的包
/// </summary>
namespace ClientPackets
{
    public sealed class ClientVersion : Packet
    {
        public override short Index
        {
            get { return (short)ClientPacketIds.ClientVersion; }
        }

        public byte[] VersionHash;

        protected override void ReadPacket(BinaryReader reader)
        {
            VersionHash = reader.ReadBytes(reader.ReadInt32());
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(VersionHash.Length);
            writer.Write(VersionHash);
        }
    }
    public sealed class Disconnect : Packet
    {
        public override short Index
        {
            get { return (short)ClientPacketIds.Disconnect; }
        }
        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    public sealed class KeepAlive : Packet
    {
        public override short Index
        {
            get { return (short)ClientPacketIds.KeepAlive; }
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
    public sealed class NewAccount: Packet
    {
        public override short Index
        {
            get { return (short)ClientPacketIds.NewAccount; }
        }

        public string AccountID = string.Empty;
        public string Password = string.Empty;
        public DateTime BirthDate;
        public string UserName = string.Empty;
        public string SecretQuestion = string.Empty;
        public string SecretAnswer = string.Empty;
        public string EMailAddress = string.Empty;

        protected override void ReadPacket(BinaryReader reader)
        {
            AccountID = reader.ReadString();
            Password = reader.ReadString();
            BirthDate = DateTime.FromBinary(reader.ReadInt64());
            UserName = reader.ReadString();
            SecretQuestion = reader.ReadString();
            SecretAnswer = reader.ReadString();
            EMailAddress = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AccountID);
            writer.Write(Password);
            writer.Write(BirthDate.ToBinary());
            writer.Write(UserName);
            writer.Write(SecretQuestion);
            writer.Write(SecretAnswer);
            writer.Write(EMailAddress);
        }
    }
    public sealed class ChangePassword: Packet
    {
        public override short Index
        {
            get { return (short)ClientPacketIds.ChangePassword; }
        }

        public string AccountID = string.Empty;
        public string CurrentPassword = string.Empty;
        public string NewPassword = string.Empty;

        protected override void ReadPacket(BinaryReader reader)
        {
            AccountID = reader.ReadString();
            CurrentPassword = reader.ReadString();
            NewPassword = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AccountID);
            writer.Write(CurrentPassword);
            writer.Write(NewPassword);
        }
    }

    //这里做一些登录限制，限制客户端的物理地址
    public sealed class Login : Packet
    {
        public override short Index
        {
            get { return (short)ClientPacketIds.Login; }
        }

        public string AccountID = string.Empty;
        public string Password = string.Empty;
        //这里传入客户端信息
        public string ClientInfo = string.Empty;

        protected override void ReadPacket(BinaryReader reader)
        {
            AccountID = reader.ReadString();
            Password = reader.ReadString();
            ClientInfo = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AccountID);
            writer.Write(Password);
            writer.Write(ClientInfo);
        }
    }
    public sealed class NewCharacter : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.NewCharacter; } }

        public string Name = string.Empty;
        public MirGender Gender;
        public MirClass Class;
        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
            Gender = (MirGender)reader.ReadByte();
            Class = (MirClass)reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write((byte)Gender);
            writer.Write((byte)Class);
        }
    }
    public sealed class DeleteCharacter : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.DeleteCharacter; } }

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
    //刷新用户的金币
    public sealed class RefreshUserGold : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RefreshUserGold; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    

    public sealed class StartGame : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.StartGame; } }

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
    public sealed class LogOut : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.LogOut; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }

    //新加充值接口,充值元宝
    public sealed class RechargeCredit : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RechargeCredit; } }

        public byte pay_type;//1：支付宝；2：微信支付
        public uint price;//充值金额(多少元)

        protected override void ReadPacket(BinaryReader reader)
        {
            pay_type = reader.ReadByte();
            price = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(pay_type);
            writer.Write(price);
        }
    }

    //新加充值完成接口,告诉服务器已经完成充值
    public sealed class RechargeEnd : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RechargeEnd; } }
        public long oid;//订单ID

        protected override void ReadPacket(BinaryReader reader)
        {
            oid = reader.ReadInt64();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(oid);
        }
    }


    public sealed class Turn : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.Turn; } }

        public MirDirection Direction;

        protected override void ReadPacket(BinaryReader reader)
        {
            Direction = (MirDirection)reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Direction);
        }
    }
    public sealed class Walk : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.Walk; } }

        public MirDirection Direction;
        protected override void ReadPacket(BinaryReader reader)
        {
            Direction = (MirDirection)reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Direction);
        }
    }
    public sealed class Run : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.Run; } }

        public MirDirection Direction;
        protected override void ReadPacket(BinaryReader reader)
        {
            Direction = (MirDirection)reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Direction);
        }
    }
    public sealed class Chat : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.Chat; } }

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
    public sealed class MoveItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.MoveItem; } }

        public MirGridType Grid;
        public int From, To;
        protected override void ReadPacket(BinaryReader reader)
        {
            Grid = (MirGridType)reader.ReadByte();
            From = reader.ReadInt32();
            To = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Grid);
            writer.Write(From);
            writer.Write(To);
        }
    }
    public sealed class StoreItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.StoreItem; } }

        public int From, To;
        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
        }
    }

    public sealed class DepositRefineItem : Packet
    {

        public override short Index { get { return (short)ClientPacketIds.DepositRefineItem; } }

        public int From, To;
        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
        }
    }

    public sealed class RetrieveRefineItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RetrieveRefineItem; } }

        public int From, To;
        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
        }
    }

    public sealed class RefineCancel : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RefineCancel; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    //放入物品
    public sealed class DepositItemCollect : Packet
    {

        public override short Index { get { return (short)ClientPacketIds.DepositItemCollect; } }

        public int From, To;
        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
        }
    }
    //取回物品
    public sealed class RetrieveItemCollect : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RetrieveItemCollect; } }

        public int From, To;
        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
        }
    }


    //物品收集取消
    public sealed class ItemCollectCancel : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ItemCollectCancel; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }

    //确认收集
    public sealed class ConfirmItemCollect : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ConfirmItemCollect; } }

        public byte type;

        protected override void ReadPacket(BinaryReader reader)
        {
            type = reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(type);
        }
    }

    public sealed class RefineItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RefineItem; } }

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

    public sealed class CheckRefine : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.CheckRefine; } }

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

    public sealed class ReplaceWedRing : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ReplaceWedRing; } }

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
        public override short Index { get { return (short)ClientPacketIds.DepositTradeItem; } }

        public int From, To;
        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
        }
    }

    public sealed class RetrieveTradeItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RetrieveTradeItem; } }

        public int From, To;
        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
        }
    }
    public sealed class TakeBackItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.TakeBackItem; } }

        public int From, To;
        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
        }
    }
    public sealed class MergeItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.MergeItem; } }

        public MirGridType GridFrom, GridTo;
        public ulong IDFrom, IDTo;
        protected override void ReadPacket(BinaryReader reader)
        {
            GridFrom = (MirGridType)reader.ReadByte();
            GridTo = (MirGridType)reader.ReadByte();
            IDFrom = reader.ReadUInt64();
            IDTo = reader.ReadUInt64();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)GridFrom);
            writer.Write((byte)GridTo);
            writer.Write(IDFrom);
            writer.Write(IDTo);
        }
    }
    public sealed class EquipItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.EquipItem; } }

        public MirGridType Grid;
        public ulong UniqueID;
        public int To;

        protected override void ReadPacket(BinaryReader reader)
        {
            Grid = (MirGridType)reader.ReadByte();
            UniqueID = reader.ReadUInt64();
            To = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Grid);
            writer.Write(UniqueID);
            writer.Write(To);
        }
    }
    public sealed class RemoveItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RemoveItem; } }

        public MirGridType Grid;
        public ulong UniqueID;
        public int To;

        protected override void ReadPacket(BinaryReader reader)
        {
            Grid = (MirGridType)reader.ReadByte();
            UniqueID = reader.ReadUInt64();
            To = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Grid);
            writer.Write(UniqueID);
            writer.Write(To);
        }
    }
    public sealed class RemoveSlotItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RemoveSlotItem; } }

        public MirGridType Grid;
        public MirGridType GridTo;
        public ulong UniqueID;
        public int To;

        protected override void ReadPacket(BinaryReader reader)
        {
            Grid = (MirGridType)reader.ReadByte();
            GridTo = (MirGridType)reader.ReadByte();
            UniqueID = reader.ReadUInt64();
            To = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Grid);
            writer.Write((byte)GridTo);
            writer.Write(UniqueID);
            writer.Write(To);
        }
    }
    public sealed class SplitItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.SplitItem; } }

        public MirGridType Grid;
        public ulong UniqueID;
        public uint Count;

        protected override void ReadPacket(BinaryReader reader)
        {
            Grid = (MirGridType)reader.ReadByte();
            UniqueID = reader.ReadUInt64();
            Count = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Grid);
            writer.Write(UniqueID);
            writer.Write(Count);
        }
    }
    public sealed class UseItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.UseItem; } }

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
    public sealed class DropItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.DropItem; } }

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
    public sealed class DropGold : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.DropGold; } }

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
    public sealed class PickUp : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.PickUp; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    //查看玩家装备
    public sealed class Inspect : Packet
    {
        public override short Index
        {
            get { return (short)ClientPacketIds.Inspect; }
        }

        public uint ObjectID;
        public bool Ranking = false;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Ranking = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Ranking);
        }
    }
    public sealed class ChangeAMode : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ChangeAMode; } }

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
    public sealed class ChangePMode : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ChangePMode; } }

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
    public sealed class ChangeTrade : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ChangeTrade; } }

        public bool AllowTrade;

        protected override void ReadPacket(BinaryReader reader)
        {
            AllowTrade = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AllowTrade);
        }
    }
    //攻击
    public sealed class Attack : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.Attack; } }

        public MirDirection Direction;
        public Spell Spell;

        protected override void ReadPacket(BinaryReader reader)
        {
            Direction = (MirDirection) reader.ReadByte();
            Spell = (Spell) reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Direction);
            writer.Write((byte)Spell);
        }
    }

    //范围攻击
    public sealed class RangeAttack : Packet //ArcherTest
    {
        public override short Index { get { return (short)ClientPacketIds.RangeAttack; } }

        public MirDirection Direction;//攻击方向
        public Point Location;//当前位置
        public uint TargetID;//目标ID
        public Point TargetLocation;//目标点

        protected override void ReadPacket(BinaryReader reader)
        {
            Direction = (MirDirection)reader.ReadByte();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            TargetID = reader.ReadUInt32();
            TargetLocation = new Point(reader.ReadInt32(), reader.ReadInt32());
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Direction);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(TargetID);
            writer.Write(TargetLocation.X);
            writer.Write(TargetLocation.Y);
        }
    }
    //收割，采集药材？
    public sealed class Harvest : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.Harvest; } }

        public MirDirection Direction;
        protected override void ReadPacket(BinaryReader reader)
        {
            Direction = (MirDirection)reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Direction);
        }
    }
    //呼叫NPC
    public sealed class CallNPC : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.CallNPC; } }

        public uint ObjectID;
        public string Key = string.Empty;

        protected override void ReadPacket(BinaryReader reader)
        {
            ObjectID = reader.ReadUInt32();
            Key = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(Key);
        }
    }
    //对话怪物NPC?
    public sealed class TalkMonsterNPC : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.TalkMonsterNPC; } }

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
    //购买物品
    public sealed class BuyItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.BuyItem; } }

        public ulong ItemIndex;
        public uint Count;
        public PanelType Type;

        protected override void ReadPacket(BinaryReader reader)
        {
            ItemIndex = reader.ReadUInt64();
            Count = reader.ReadUInt32();
            Type = (PanelType)reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ItemIndex);
            writer.Write(Count);
            writer.Write((byte)Type);
        }
    }
    //卖
    public sealed class SellItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.SellItem; } }

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
    //制作
    public sealed class CraftItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.CraftItem; } }

        public ulong UniqueID;
        public uint Count;
        public int[] Slots;

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
            Count = reader.ReadUInt32();
            Slots = new int[reader.ReadInt32()];

            for (int i = 0; i < Slots.Length; i++)
            {
                Slots[i] = reader.ReadInt32();
            }
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
            writer.Write(Count);
            writer.Write(Slots.Length);

            for (int i = 0; i < Slots.Length; i++)
            {
                writer.Write(Slots[i]);
            }
        }
    }
    //修理
    public sealed class RepairItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RepairItem; } }

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

    public sealed class BuyItemBack : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.BuyItemBack; } }

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
    public sealed class SRepairItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.SRepairItem; } }

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
    //魔法键？
    public sealed class MagicKey : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.MagicKey; } }

        public Spell Spell;
        public byte Key;

        protected override void ReadPacket(BinaryReader reader)
        {
            Spell = (Spell) reader.ReadByte();
            Key = reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte) Spell);
            writer.Write(Key);
        }
    }

    //魔法参数，魔法释放的前置
    //目前用来切换红绿毒
    public sealed class MagicParameter : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.MagicParameter; } }

        public byte Parameter;//1.绿毒，2红毒

        protected override void ReadPacket(BinaryReader reader)
        {
            Parameter = reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Parameter);
        }
    }


    //释放魔法？
    public sealed class Magic : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.Magic; } }

        public Spell Spell;
        public MirDirection Direction;
        public uint TargetID;
        public Point Location;

        protected override void ReadPacket(BinaryReader reader)
        {
            Spell = (Spell) reader.ReadByte();
            Direction = (MirDirection)reader.ReadByte();
            TargetID = reader.ReadUInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte) Spell);
            writer.Write((byte)Direction);
            writer.Write(TargetID);
            writer.Write(Location.X);
            writer.Write(Location.Y);
        }
    }
    //开关组队
    public sealed class SwitchGroup : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.SwitchGroup; } }

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
    //添加成员
    public sealed class AddMember : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.AddMember; } }

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
    //删除成员
    public sealed class DelMember : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.DellMember; } }

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
    //组队邀请？是否邀请组队？
    public sealed class GroupInvite : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.GroupInvite; } }

        public bool AcceptInvite;
        protected override void ReadPacket(BinaryReader reader)
        {
            AcceptInvite = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AcceptInvite);
        }
    }
    //结婚请求
    public sealed class MarriageRequest : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.MarriageRequest; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    //同意，拒绝结婚
    public sealed class MarriageReply : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.MarriageReply; } }

        public bool AcceptInvite;
        protected override void ReadPacket(BinaryReader reader)
        {
            AcceptInvite = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AcceptInvite);
        }
    }
    //改变结婚？
    public sealed class ChangeMarriage : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ChangeMarriage; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    //离婚请求
    public sealed class DivorceRequest : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.DivorceRequest; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    //是否同意离婚
    public sealed class DivorceReply : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.DivorceReply; } }

        public bool AcceptInvite;
        protected override void ReadPacket(BinaryReader reader)
        {
            AcceptInvite = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AcceptInvite);
        }
    }
    //添加老师？
    public sealed class AddMentor : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.AddMentor; } }

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
    //老师同意？
    public sealed class MentorReply : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.MentorReply; } }

        public bool AcceptInvite;
        protected override void ReadPacket(BinaryReader reader)
        {
            AcceptInvite = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AcceptInvite);
        }
    }
    //允许
    public sealed class AllowMentor : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.AllowMentor; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    //取消
    public sealed class CancelMentor : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.CancelMentor; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }

    //交易答复
    public sealed class TradeReply : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.TradeReply; } }

        public bool AcceptInvite;
        protected override void ReadPacket(BinaryReader reader)
        {
            AcceptInvite = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AcceptInvite);
        }
    }
    //交易请求
    public sealed class TradeRequest : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.TradeRequest; } }

        protected override void ReadPacket(BinaryReader reader)
        {  }

        protected override void WritePacket(BinaryWriter writer)
        { }
    }
    //交易金币
    public sealed class TradeGold : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.TradeGold; } }

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
    //交易确认
    public sealed class TradeConfirm : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.TradeConfirm; } }

        public bool Locked;
        protected override void ReadPacket(BinaryReader reader)
        {
            Locked = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Locked);
        }
    }
    //交易取消
    public sealed class TradeCancel : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.TradeCancel; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    //重生
    public sealed class TownRevive : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.TownRevive; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    //魔法切换？
    public sealed class SpellToggle : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.SpellToggle; } }
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
    //寄售物品
    public sealed class ConsignItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ConsignItem; } }

        public ulong UniqueID;
        public uint GoldPrice = 0;//金币价格，如果金币价格是0，则只能元宝购买
        public uint CreditPrice = 0;//元宝价格

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
            GoldPrice = reader.ReadUInt32();
            CreditPrice = reader.ReadUInt32();
            
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
            writer.Write(GoldPrice);
            writer.Write(CreditPrice);
        }
    }
    //查询市场
    public sealed class MarketSearch : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.MarketSearch; } }

        public string Match = string.Empty;
        public byte itemtype;
        public int Page;
        protected override void ReadPacket(BinaryReader reader)
        {
            Match = reader.ReadString();
            itemtype = reader.ReadByte();
            Page = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Match);
            writer.Write(itemtype);
            writer.Write(Page);
        }
    }
    
    //市场买
    public sealed class MarketBuy : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.MarketBuy; } }

        public ulong AuctionID;
        public byte payType;//支付类型 0：金币 1：元宝

        protected override void ReadPacket(BinaryReader reader)
        {
            AuctionID = reader.ReadUInt64();
            payType = reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AuctionID);
            writer.Write(payType);
        }
    }
    //市场下架？
    public sealed class MarketGetBack : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.MarketGetBack; } }

        public ulong AuctionID;

        protected override void ReadPacket(BinaryReader reader)
        {
            AuctionID = reader.ReadUInt64();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AuctionID);
        }
    }
    //查询用户名？
    public sealed class RequestUserName : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RequestUserName; } }

        public ulong UserID;

        protected override void ReadPacket(BinaryReader reader)
        {
            UserID = reader.ReadUInt64();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UserID);
        }
    }
    //查询物品信息
    public sealed class RequestChatItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RequestChatItem; } }

        public ulong ChatItemID;

        protected override void ReadPacket(BinaryReader reader)
        {
            ChatItemID = reader.ReadUInt64();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ChatItemID);
        }
    }
    //编辑行会成员
    public sealed class EditGuildMember : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.EditGuildMember; } }

        public byte ChangeType = 0;
        public byte RankIndex = 0;
        public string Name = "";
        public string RankName = "";

        protected override void ReadPacket(BinaryReader reader)
        {
            ChangeType = reader.ReadByte();
            RankIndex = reader.ReadByte();
            Name = reader.ReadString();
            RankName = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(ChangeType);
            writer.Write(RankIndex);
            writer.Write(Name);
            writer.Write(RankName);
        }
    }
    //编辑行会公共
    public sealed class EditGuildNotice : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.EditGuildNotice; } }

        public List<string> notice = new List<string>();

        protected override void ReadPacket(BinaryReader reader)
        {
            int LineCount = reader.ReadInt32();
            for (int i = 0; i < LineCount; i++)
                notice.Add(reader.ReadString());
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(notice.Count);
            for (int i = 0; i < notice.Count; i++)
                writer.Write(notice[i]);
        }
    }
    //行会邀请
    public sealed class GuildInvite : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.GuildInvite; } }

        public bool AcceptInvite;
        protected override void ReadPacket(BinaryReader reader)
        {
            AcceptInvite = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AcceptInvite);
        }
    }
    //申请行会信息？
    public sealed class RequestGuildInfo : Packet
    {
        public override short Index
        {
            get { return (short)ClientPacketIds.RequestGuildInfo; } 
        }
        public byte Type;
        protected override void ReadPacket(BinaryReader reader)
        {
            Type = reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Type);
        }
    }
    //行会名称返回？
    public sealed class GuildNameReturn : Packet
    {
        public override short Index
        {
            get { return (short)ClientPacketIds.GuildNameReturn; }
        }
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
    //行会战争返回？
    public sealed class GuildWarReturn : Packet
    {
        public override short Index
        {
            get { return (short)ClientPacketIds.GuildWarReturn; }
        }
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
    //改变行会存储金币
    public sealed class GuildStorageGoldChange: Packet
    {
        public override short Index
        {
            get { return (short)ClientPacketIds.GuildStorageGoldChange; }
        }
        public byte Type = 0;
        public uint Amount = 0;        
        protected override void ReadPacket(BinaryReader reader)
        {
            Type = reader.ReadByte();
            Amount = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Type);
            writer.Write(Amount);
        }
    }
    //改变行会存储物品
    public sealed class GuildStorageItemChange: Packet
    {
        public override short Index
        {
            get { return (short)ClientPacketIds.GuildStorageItemChange; }
        }
        public byte Type = 0;
        public int From, To;
        protected override void ReadPacket(BinaryReader reader)
        {
            Type = reader.ReadByte();
            From = reader.ReadInt32();
            To = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Type);
            writer.Write(From);
            writer.Write(To);
        }
    }
    //装备物品
    public sealed class EquipSlotItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.EquipSlotItem; } }

        public MirGridType Grid;
        public ulong UniqueID;
        public int To;
        public MirGridType GridTo;

        protected override void ReadPacket(BinaryReader reader)
        {
            Grid = (MirGridType)reader.ReadByte();
            UniqueID = reader.ReadUInt64();
            To = reader.ReadInt32();
            GridTo = (MirGridType)reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write((byte)Grid);
            writer.Write(UniqueID);
            writer.Write(To);
            writer.Write((byte)GridTo);
        }
    }
    //钓鱼，投递
    public sealed class FishingCast : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.FishingCast; } }

        public bool CastOut;//是否投递，如果不是投递，就是拉钩

        protected override void ReadPacket(BinaryReader reader)
        {
            CastOut = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(CastOut);
        }
    }
    //钓鱼，改变自动投递
    public sealed class FishingChangeAutocast : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.FishingChangeAutocast; } }

        public bool AutoCast;

        protected override void ReadPacket(BinaryReader reader)
        {
            AutoCast = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(AutoCast);
        }
    }
    //接受任务
    public sealed class AcceptQuest : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.AcceptQuest; } }

        public uint NPCIndex;
        public int QuestIndex;

        protected override void ReadPacket(BinaryReader reader)
        {
            NPCIndex = reader.ReadUInt32();
            QuestIndex = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(NPCIndex);
            writer.Write(QuestIndex);
        }
    }
    //完成任务
    public sealed class FinishQuest : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.FinishQuest; } }

        public int QuestIndex;
        public int SelectedItemIndex;

        protected override void ReadPacket(BinaryReader reader)
        {
            QuestIndex = reader.ReadInt32();
            SelectedItemIndex = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(QuestIndex);
            writer.Write(SelectedItemIndex);
        }
    }
    //放弃任务
    public sealed class AbandonQuest : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.AbandonQuest; } }

        public int QuestIndex;

        protected override void ReadPacket(BinaryReader reader)
        {
            QuestIndex = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(QuestIndex);
        }
    }
    //分享任务
    public sealed class ShareQuest : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ShareQuest; } }

        public int QuestIndex;

        protected override void ReadPacket(BinaryReader reader)
        {
            QuestIndex = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(QuestIndex);
        }
    }
    //接受转生？专职？
    public sealed class AcceptReincarnation : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.AcceptReincarnation; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    //取消转职？
    public sealed class CancelReincarnation : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.CancelReincarnation; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }
        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }
    //合并物品？
    public sealed class CombineItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.CombineItem; } }

        public ulong IDFrom, IDTo;
        protected override void ReadPacket(BinaryReader reader)
        {
            IDFrom = reader.ReadUInt64();
            IDTo = reader.ReadUInt64();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(IDFrom);
            writer.Write(IDTo);
        }
    }
    //设置物品激活？
    public sealed class SetConcentration : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.SetConcentration; } }

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
    //觉醒物品需要的属性
    public sealed class AwakeningNeedMaterials : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.AwakeningNeedMaterials; } }

        public ulong UniqueID;
        public AwakeType Type;

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
            Type = (AwakeType)reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
            writer.Write((byte)Type);
        }
    }
    //觉醒锁住物品
    public sealed class AwakeningLockedItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.AwakeningLockedItem; } }

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
    //觉醒某个位置的物品？
    public sealed class Awakening : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.Awakening; } }

        public ulong UniqueID;
        public AwakeType Type;
        public uint PositionIdx;

        protected override void ReadPacket(BinaryReader reader)
        {
            UniqueID = reader.ReadUInt64();
            Type = (AwakeType)reader.ReadByte();
            PositionIdx = reader.ReadUInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(UniqueID);
            writer.Write((byte)Type);
            writer.Write(PositionIdx);
        }
    }
    //拆解物品
    public sealed class DisassembleItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.DisassembleItem; } }

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
    //降低觉醒
    public sealed class DowngradeAwakening : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.DowngradeAwakening; } }

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
    //重新添加物品
    public sealed class ResetAddedItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ResetAddedItem; } }

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
    //发送邮件
    public sealed class SendMail : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.SendMail; } }

        public string Name;
        public string Message;
        public uint Gold;
        public ulong[] ItemsIdx = new ulong[5];
        public bool Stamped;

        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
            Message = reader.ReadString();
            Gold = reader.ReadUInt32();

            for (int i = 0; i < 5; i++)
            {
                ItemsIdx[i] = reader.ReadUInt64();
            }

            Stamped = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Message);
            writer.Write(Gold);

            for (int i = 0; i < 5; i++)
            {
                writer.Write(ItemsIdx[i]);
            }

            writer.Write(Stamped);
        }
    }
    //读取邮件
    public sealed class ReadMail : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ReadMail; } }

        public ulong MailID;

        protected override void ReadPacket(BinaryReader reader)
        {
            MailID = reader.ReadUInt64();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(MailID);
        }
    }
    //整理包裹，收邮件？
    public sealed class CollectParcel : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.CollectParcel; } }

        public ulong MailID;

        protected override void ReadPacket(BinaryReader reader)
        {
            MailID = reader.ReadUInt64();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(MailID);
        }
    }
    //删除邮件
    public sealed class DeleteMail : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.DeleteMail; } }

        public ulong MailID;

        protected override void ReadPacket(BinaryReader reader)
        {
            MailID = reader.ReadUInt64();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(MailID);
        }
    }
    //锁定邮件
    public sealed class LockMail : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.LockMail; } }

        public ulong MailID;
        public bool Lock;

        protected override void ReadPacket(BinaryReader reader)
        {
            MailID = reader.ReadUInt64();
            Lock = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(MailID);
            writer.Write(Lock);
        }
    }
    //锁定邮件物品
    public sealed class MailLockedItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.MailLockedItem; } }

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
    //邮件费用
    public sealed class MailCost : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.MailCost; } }

        public uint Gold;
        public ulong[] ItemsIdx = new ulong[5];
        public bool Stamped;

        protected override void ReadPacket(BinaryReader reader)
        {
            Gold = reader.ReadUInt32();

            for (int i = 0; i < 5; i++)
            {
                ItemsIdx[i] = reader.ReadUInt64();
            }

            Stamped = reader.ReadBoolean();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Gold);

            for (int i = 0; i < 5; i++)
            {
                writer.Write(ItemsIdx[i]);
            }

            writer.Write(Stamped);
        }
    }

    //契约兽的操作
    public sealed class MyMonsterOperation : Packet//MyMonsterOperation
    {
        public override short Index { get { return (short)ClientPacketIds.MyMonsterOperation; } }

        public ulong monidx;//契约兽的ID
        public byte operation;//操作 1:改名 2：召唤 3：释放，解雇，4：转移 5:喂食物

        public string parameter1 = string.Empty;
        public string parameter2 = string.Empty;

        protected override void ReadPacket(BinaryReader reader)
        {
            monidx = reader.ReadUInt64();
            operation = reader.ReadByte();
            parameter1 = reader.ReadString();
            parameter2 = reader.ReadString();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(monidx);
            writer.Write(operation);
            writer.Write(parameter1);
            writer.Write(parameter2);
        }
    }

    
    //
    public sealed class UpdateIntelligentCreature : Packet//IntelligentCreature
    {
        public override short Index { get { return (short)ClientPacketIds.UpdateIntelligentCreature; } }


        public ClientIntelligentCreature Creature;
        public bool SummonMe = false;
        public bool UnSummonMe = false;
        public bool ReleaseMe = false;

        protected override void ReadPacket(BinaryReader reader)
        {
            Creature = new ClientIntelligentCreature(reader);
            SummonMe = reader.ReadBoolean();
            UnSummonMe = reader.ReadBoolean();
            ReleaseMe = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            Creature.Save(writer);
            writer.Write(SummonMe);
            writer.Write(UnSummonMe);
            writer.Write(ReleaseMe);
        }
    }
    //智能选取？
    public sealed class IntelligentCreaturePickup : Packet//IntelligentCreature
    {
        public override short Index { get { return (short)ClientPacketIds.IntelligentCreaturePickup; } }

        public bool MouseMode = false;
        public Point Location = new Point(0,0);

        protected override void ReadPacket(BinaryReader reader)
        {
            MouseMode = reader.ReadBoolean();
            Location.X = reader.ReadInt32();
            Location.Y = reader.ReadInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(MouseMode);
            writer.Write(Location.X);
            writer.Write(Location.Y);
        }
    }

    //添加朋友
    public sealed class AddFriend : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.AddFriend; } }

        public string Name;
        public bool Blocked;

        protected override void ReadPacket(BinaryReader reader)
        {
            Name = reader.ReadString();
            Blocked = reader.ReadBoolean();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Blocked);
        }
    }
    //删除朋友
    public sealed class RemoveFriend : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RemoveFriend; } }

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
    //刷新朋友
    public sealed class RefreshFriends : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RefreshFriends; } }

        protected override void ReadPacket(BinaryReader reader)
        {
        }

        protected override void WritePacket(BinaryWriter writer)
        {
        }
    }

    //添加备注
    public sealed class AddMemo : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.AddMemo; } }

        public ulong CharacterIndex;
        public string Memo;

        protected override void ReadPacket(BinaryReader reader)
        {
            CharacterIndex = (ulong)reader.ReadInt64();
            Memo = reader.ReadString();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(CharacterIndex);
            writer.Write(Memo);
        }
    }
    //更新行会
    public sealed class GuildBuffUpdate : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.GuildBuffUpdate; } }

        public byte Action = 0; //0 = request list, 1 = request a buff to be enabled, 2 = request a buff to be activated
        public int Id;

        protected override void ReadPacket(BinaryReader reader)
        {
            Action = reader.ReadByte();
            Id = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Action);
            writer.Write(Id);
        }
    }
    //商店买东西
    //这里要扩展下，支持使用元宝购买还是使用金币购买
    public sealed class GameshopBuy : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.GameshopBuy; } }

        public int GIndex;//商城物品ID
        public byte payType;//支付类型 0：金币 1：元宝
        public byte Quantity;//购买的数量

        protected override void ReadPacket(BinaryReader reader)
        {
            GIndex = reader.ReadInt32();
            payType = reader.ReadByte();
            Quantity = reader.ReadByte();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(GIndex);
            writer.Write(payType);
            writer.Write(Quantity);
        }
    }
    //NPC确认输入
    public sealed class NPCConfirmInput : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.NPCConfirmInput; } }

        public uint NPCID;
        public string PageName;
        public string Value;

        protected override void ReadPacket(BinaryReader reader)
        {
            NPCID = reader.ReadUInt32();
            PageName = reader.ReadString();
            Value = reader.ReadString();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(NPCID);
            writer.Write(PageName);
            writer.Write(Value);
        }
    }
    //问题报告，截图报告？
    public sealed class ReportIssue : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ReportIssue; } }

        public byte[] Image;
        public int ImageSize;
        public int ImageChunk;

        public string Message;

        protected override void ReadPacket(BinaryReader reader)
        {
            Image = reader.ReadBytes(reader.ReadInt32());
            ImageSize = reader.ReadInt32();
            ImageChunk = reader.ReadInt32();
        }
        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(Image.Length);
            writer.Write(Image);
            writer.Write(ImageSize);
            writer.Write(ImageChunk);
        }
    }
    //获取排行榜？
    public sealed class GetRanking : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.GetRanking; } }
        public byte RankIndex;//职业
        public byte RankType;//榜单类型 0：人 1：地 2：天

        protected override void ReadPacket(BinaryReader reader)
        {
            RankIndex = reader.ReadByte();
            RankType = reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(RankIndex);
            writer.Write(RankType);
        }
    }
    //开门
    public sealed class Opendoor : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.Opendoor; } }
        public byte DoorIndex;

        protected override void ReadPacket(BinaryReader reader)
        {
            DoorIndex = reader.ReadByte();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(DoorIndex);
        }
    }
    //获取XX物品
    public sealed class GetRentedItems : Packet
    {
        public override short Index
        {
            get
            {
                return (short)ClientPacketIds.GetRentedItems;
            }
        }

        protected override void ReadPacket(BinaryReader reader)
        { }

        protected override void WritePacket(BinaryWriter writer)
        { }
    }
    //请求什么物品？
    public sealed class ItemRentalRequest : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ItemRentalRequest; } }

        protected override void ReadPacket(BinaryReader reader)
        { }

        protected override void WritePacket(BinaryWriter writer)
        { }
    }
    //物品的费用
    public sealed class ItemRentalFee : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ItemRentalFee; } }

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
    //物品的时限
    public sealed class ItemRentalPeriod : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ItemRentalPeriod; } }

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
    //寄存，归还物品?
    public sealed class DepositRentalItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.DepositRentalItem; } }

        public int From, To;

        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
        }
    }

    public sealed class RetrieveRentalItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RetrieveRentalItem; } }

        public int From, To;

        protected override void ReadPacket(BinaryReader reader)
        {
            From = reader.ReadInt32();
            To = reader.ReadInt32();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(From);
            writer.Write(To);
        }
    }

    public sealed class CancelItemRental : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.CancelItemRental; } }

        protected override void ReadPacket(BinaryReader reader)
        { }

        protected override void WritePacket(BinaryWriter writer)
        { }
    }

    public sealed class ItemRentalLockFee : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ItemRentalLockFee; } }

        protected override void ReadPacket(BinaryReader reader)
        { }

        protected override void WritePacket(BinaryWriter writer)
        { }
    }

    public sealed class ItemRentalLockItem : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ItemRentalLockItem; } }

        protected override void ReadPacket(BinaryReader reader)
        { }

        protected override void WritePacket(BinaryWriter writer)
        { }
    }

    public sealed class ConfirmItemRental : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.ConfirmItemRental; } }

        protected override void ReadPacket(BinaryReader reader)
        { }

        protected override void WritePacket(BinaryWriter writer)
        { }
    }

    //刷新背包
    public sealed class RefreshInventory : Packet
    {
        public override short Index { get { return (short)ClientPacketIds.RefreshInventory; } }

        protected override void ReadPacket(BinaryReader reader)
        { }

        protected override void WritePacket(BinaryWriter writer)
        { }
    }

    public sealed class CheckCode : Packet
    {
        public override short Index
        {
            get { return (short)ClientPacketIds.CheckCode; }
        }
        public string code;

        protected override void ReadPacket(BinaryReader reader)
        {
            code = reader.ReadString();
        }

        protected override void WritePacket(BinaryWriter writer)
        {
            writer.Write(code);
        }
    }
    


}
