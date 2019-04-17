using System;
using System.Collections.Generic;
using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using Server.MirObjects.Monsters;
using S = ServerPackets;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Server.MirObjects
{
    /// <summary>
    /// 地图对象的抽象类，包括地图上的怪物，地图上的掉落物品，地图上的魔法？
    /// 这个感觉要优化才可以，把太多的东西放在这里类里了。
    /// 这个类是造成服务器内存很大的主要原因
    /// 
    /// </summary>
    public abstract class MapObject
    {
        protected static Envir Envir
        {
            get { return SMain.Envir; }
        }
        //这个只是临时的ID吧。
        public readonly uint ObjectID = SMain.Envir.ObjectID;

        public abstract ObjectType Race { get; }

        public abstract string Name { get; set; }

        public long ExplosionInflictedTime;
        public int ExplosionInflictedStage;

        private int SpawnThread;

        //Position
        private Map _currentMap;
        public Map CurrentMap
        {
            set
            {
                _currentMap = value;
                CurrentMapIndex = _currentMap != null ? _currentMap.Info.Index : 0;
            }
            get { return _currentMap; }
        }

        public abstract int CurrentMapIndex { get; set; }
        public abstract Point CurrentLocation { get; set; }
        public abstract MirDirection Direction { get; set; }

        public abstract ushort Level { get; set; }

        public abstract uint Health { get; }
        public abstract uint MaxHealth { get; }
        public byte PercentHealth
        {
            get { return (byte) (Health/(float) MaxHealth*100); }

        }

        public ushort MinAC, MaxAC, MinMAC, MaxMAC;
        public ushort MinDC, MaxDC, MinMC, MaxMC, MinSC, MaxSC;
        //精确，敏捷，灯光
        public byte Accuracy, Agility, Light;
        public sbyte ASpeed, Luck;
        public int AttackSpeed;

        public ushort CurrentHandWeight,
                   MaxHandWeight,
                   CurrentWearWeight,
                   MaxWearWeight;

        public ushort CurrentBagWeight,
                      MaxBagWeight;
        //CriticalRate:暴击率
        //CriticalDamage:暴击伤害
        public byte MagicResist, PoisonResist, HealthRecovery, SpellRecovery, PoisonRecovery, CriticalRate, CriticalDamage, Holy, Freezing, PoisonAttack;

        public long CellTime, BrownTime, PKPointTime, LastHitTime, EXPOwnerTime;
        public Color NameColour = Color.White;
        //增加一个字段，可以更改怪物名称
        public Color ChangeNameColour = Color.White;

        public bool Dead, Undead, Harvested, AutoRev;

        public List<KeyValuePair<string, string>> NPCVar = new List<KeyValuePair<string, string>>();

        public virtual int PKPoints { get; set; }

        public ushort PotHealthAmount, PotManaAmount, HealAmount, VampAmount;
        //public bool HealthChanged;
        //这2个是增加爆率，数值为百分比，比如30，就是增加百分之30的的爆率
        public float ItemDropRateOffset = 0, GoldDropRateOffset = 0;

        public bool CoolEye;
        private bool _hidden;
        
        public bool Hidden
        {
            get
            {
                return _hidden;
            }
            set
            {
                if (_hidden == value) return;
                _hidden = value;
                CurrentMap.Broadcast(new S.ObjectHidden {ObjectID = ObjectID, Hidden = value}, CurrentLocation);
            }
        }

        private bool _observer;
        public bool Observer
        {
            get
            {
                return _observer;
            }
            set
            {
                if (_observer == value) return;
                _observer = value;
                if (!_observer)
                    BroadcastInfo();
                else
                    Broadcast(new S.ObjectRemove { ObjectID = ObjectID });
            }
        }

        #region Sneaking
        private bool _sneakingActive;
        public bool SneakingActive
        {
            get { return _sneakingActive; }
            set
            {
                if (_sneakingActive == value) return;
                _sneakingActive = value;

                Observer = _sneakingActive;

                //CurrentMap.Broadcast(new S.ObjectSneaking { ObjectID = ObjectID, SneakingActive = value }, CurrentLocation);
            }
        }

        private bool _sneaking;
        public bool Sneaking
        {
            get { return _sneaking; }
            set { _sneaking = value; SneakingActive = value; }
        }
        #endregion

        public MapObject _target;
        public virtual MapObject Target
        {
            get { return _target; }
            set
            {
                if (_target == value) return;
                _target = value;
            }

        }
        //主人，最后攻击人，经验拥有者，拥有者
        public MapObject Master, LastHitter, EXPOwner, Owner;
        public long ExpireTime, OwnerTime, OperateTime;
        public int OperateDelay = 100;

        public List<MonsterObject> Pets = new List<MonsterObject>();
        public List<Buff> Buffs = new List<Buff>();

        public List<PlayerObject> GroupMembers;//组队成员，包含自己

        public virtual AttackMode AMode { get; set; }

        public virtual PetMode PMode { get; set; }
        public bool InSafeZone;
        //防御率，伤害率
        public float ArmourRate, DamageRate; //recieved not given 未赋值

        public List<Poison> PoisonList = new List<Poison>();
        public PoisonType CurrentPoison = PoisonType.None;
        public List<DelayedAction> ActionList = new List<DelayedAction>();

        //这个是引用到全局的哦？好多嵌套引用啊。坑
        public LinkedListNode<MapObject> Node;
        public LinkedListNode<MapObject> NodeThreaded;
        public long RevTime;

        public virtual bool Blocking
        {
            get {
                if (InSafeZone)
                {
                    return false;
                }
                return true;
            }
        }

        public Point Front
        {
            get { return Functions.PointMove(CurrentLocation, Direction, 1); }

        }
        public Point Back
        {
            get { return Functions.PointMove(CurrentLocation, Direction, -1); }

        }
        
        //增加一个方法，方便查看状态
        public string toString()
        {
            //用反射
            return ReflectionUtils.ObjectToString(this);
        }

        //死循环调用入口
        public virtual void Process()
        {
            //这里是入口调用，里面的逻辑太多，经常会出现空指针异常。
            //这里用异常捕获处理下，避免出现异常的时候，服务器奔溃
            try
            {
                if (Master != null && Master.Node == null) Master = null;
                if (LastHitter != null && LastHitter.Node == null) LastHitter = null;
                if (EXPOwner != null && EXPOwner.Node == null) EXPOwner = null;
                if (Target != null && (Target.Node == null || Target.Dead)) Target = null;
                if (Owner != null && Owner.Node == null) Owner = null;

                if (Envir.Time > PKPointTime && PKPoints > 0)
                {
                    PKPointTime = Envir.Time + Settings.PKDelay * Settings.Second;
                    PKPoints--;
                }

                if (LastHitter != null && Envir.Time > LastHitTime)
                    LastHitter = null;


                if (EXPOwner != null && Envir.Time > EXPOwnerTime)
                {
                    EXPOwner = null;
                }

                for (int i = 0; i < ActionList.Count; i++)
                {
                    if (Envir.Time < ActionList[i].Time) continue;
                    Process(ActionList[i]);
                    ActionList.RemoveAt(i);
                }
            }
            catch(Exception e)
            {
                SMain.Enqueue(e);
            }
        }

        public abstract void SetOperateTime();
        //获取攻击
        public int GetAttackPower(int min, int max)
        {
            if (min < 0) min = 0;
            if (min > max) max = min;
            //幸运与诅咒计算,多少几率打出最大，最小伤害
            if (Luck > 0)
            {
                if (Luck > RandomUtils.Next(Settings.MaxLuck))
                    return max;
            }
            else if (Luck < 0)
            {
                if (Luck < -RandomUtils.Next(Settings.MaxLuck))
                    return min;
            }

            return RandomUtils.Next(min, max + 1);
        }
        //获取防御
        public int GetDefencePower(int min, int max)
        {
            if (min < 0) min = 0;
            if (min > max) max = min;

            return RandomUtils.Next(min, max + 1);
        }

        public virtual void Remove(PlayerObject player)
        {
            player.Enqueue(new S.ObjectRemove {ObjectID = ObjectID});
        }
        public virtual void Add(PlayerObject player)
        {
            if (Race == ObjectType.Merchant)
            {
                NPCObject NPC = (NPCObject)this;
                NPC.CheckVisible(player, true);
                return;
            }

            player.Enqueue(GetInfo());

            //if (Race == ObjectType.Player)
            //{
            //    PlayerObject me = (PlayerObject)this;
            //    player.Enqueue(me.GetInfoEx(player));
            //}
            //else
            //{
            //    player.Enqueue(GetInfo());
            //}
        }
        public virtual void Remove(MonsterObject monster)
        {

        }
        public virtual void Add(MonsterObject monster)
        {

        }

        public abstract void Process(DelayedAction action);


        public bool CanFly(Point target)
        {
            Point location = CurrentLocation;
            while (location != target)
            {
                MirDirection dir = Functions.DirectionFromPoint(location, target);

                location = Functions.PointMove(location, dir, 1);

                if (location.X < 0 || location.Y < 0 || location.X >= CurrentMap.Width || location.Y >= CurrentMap.Height) return false;

                if (!CurrentMap.ValidPoint(location)) return false;

            }

            return true;
        }
        //生产，产生
        public virtual void Spawned()
        {
            Node = Envir.Objects.AddLast(this);
            if ((Race == ObjectType.Monster) && Settings.Multithreaded)
            {
                SpawnThread = CurrentMap.Thread;
                NodeThreaded = Envir.MobThreads[SpawnThread].ObjectsList.AddLast(this);
            }
            OperateTime = Envir.Time + RandomUtils.Next(OperateDelay);

            InSafeZone = CurrentMap != null && CurrentMap.GetSafeZone(CurrentLocation) != null;
            BroadcastInfo();
            BroadcastHealthChange();
        }
        //清除对象，清除尸体
        public virtual void Despawn()
        {
            Broadcast(new S.ObjectRemove {ObjectID = ObjectID});
            Envir.Objects.Remove(Node);
            if (Settings.Multithreaded && (Race == ObjectType.Monster))
            {
                Envir.MobThreads[SpawnThread].ObjectsList.Remove(NodeThreaded);
            }            

            ActionList.Clear();

            for (int i = Pets.Count - 1; i >= 0; i--)
                Pets[i].Die();

            Node = null;
        }

        //查找某个对象，这个效率好低哦
        public MapObject FindObject(uint targetID, int dist)
        {
            for (int d = 0; d <= dist; d++)
            {
                for (int y = CurrentLocation.Y - d; y <= CurrentLocation.Y + d; y++)
                {
                    if (y < 0) continue;
                    if (y >= CurrentMap.Height) break;

                    for (int x = CurrentLocation.X - d; x <= CurrentLocation.X + d; x += Math.Abs(y - CurrentLocation.Y) == d ? 1 : d * 2)
                    {
                        if (x < 0) continue;
                        if (x >= CurrentMap.Width) break;

                        //Cell cell = CurrentMap.GetCell(x, y);
                        if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                        for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                        {
                            MapObject ob = CurrentMap.Objects[x, y][i];
                            if (ob.ObjectID != targetID) continue;

                            return ob;
                        }
                    }
                }
            }
            return null;
        }

        //发送广播
        //附近16*16格的玩家接受到广播
        public virtual void Broadcast(Packet p)
        {
            if (p == null || CurrentMap == null) return;

            for (int i = CurrentMap.Players.Count - 1; i >= 0; i--)
            {
                PlayerObject player = CurrentMap.Players[i];
                if (player == this) continue;

                if (Functions.InRange(CurrentLocation, player.CurrentLocation, Globals.DataRange))
                    player.Enqueue(p);
            }
        }

        public virtual void BroadcastInfo()
        {
            Broadcast(GetInfo());
            return;
        } 

        public abstract bool IsAttackTarget(PlayerObject attacker);
        public abstract bool IsAttackTarget(MonsterObject attacker);
        public abstract int Attacked(PlayerObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true);
        public abstract int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility);

        public abstract int Struck(int damage, DefenceType type = DefenceType.ACAgility);

        public abstract bool IsFriendlyTarget(PlayerObject ally);
        public abstract bool IsFriendlyTarget(MonsterObject ally);

        public abstract void ReceiveChat(string text, ChatType type);

        public abstract Packet GetInfo();

        public virtual void WinExp(uint amount, uint targetLevel = 0)
        {


        }

        public virtual bool CanGainGold(uint gold)
        {
            return false;
        }
        public virtual void WinGold(uint gold)
        {

        }

        public virtual bool Harvest(PlayerObject player) { return false; }

        public abstract void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true);

        public virtual void AddBuff(Buff b)
        {
            switch (b.Type)
            {
                case BuffType.MoonLight:
                case BuffType.Hiding:
                case BuffType.DarkBody:
                    Hidden = true;

                    if (b.Type == BuffType.MoonLight || b.Type == BuffType.DarkBody) Sneaking = true;

                    for (int y = CurrentLocation.Y - Globals.DataRange; y <= CurrentLocation.Y + Globals.DataRange; y++)
                    {
                        if (y < 0) continue;
                        if (y >= CurrentMap.Height) break;

                        for (int x = CurrentLocation.X - Globals.DataRange; x <= CurrentLocation.X + Globals.DataRange; x++)
                        {
                            if (x < 0) continue;
                            if (x >= CurrentMap.Width) break;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            ///Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Monster) continue;

                                if (ob.Target == this && (!ob.CoolEye || ob.Level < Level)) ob.Target = null;
                            }
                        }
                    }
                    break;
            }


            for (int i = 0; i < Buffs.Count; i++)
            {
                if (Buffs[i].Type != b.Type) continue;

                Buffs[i] = b;
                Buffs[i].Paused = false;
                return;
            }

            Buffs.Add(b);
        }
        public void RemoveBuff(BuffType b)
        {
            for (int i = 0; i < Buffs.Count; i++)
            {
                if (Buffs[i].Type != b) continue;

                Buffs[i].Infinite = false;
                Buffs[i].ExpireTime = Envir.Time;
            }
        }
        //检测当前位置有没有其他的玩家，怪物，如果有，就走动一下哦
        public bool CheckStacked()
        {
            //Cell cell = CurrentMap.GetCell(CurrentLocation);

            if (CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y] != null)
                for (int i = 0; i < CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y].Count; i++)
                {
                    MapObject ob = CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i];
                    if (ob == this || !ob.Blocking) continue;
                    return true;
                }

            return false;
        }

        public virtual bool Teleport(Map temp, Point location, bool effects = true, byte effectnumber = 0)
        {
            if (temp == null || !temp.ValidPoint(location)) return false;

            CurrentMap.RemoveObject(this);
            if (effects) Broadcast(new S.ObjectTeleportOut {ObjectID = ObjectID, Type = effectnumber});
            Broadcast(new S.ObjectRemove {ObjectID = ObjectID});
            
            CurrentMap = temp;
            CurrentLocation = location;

            InTrapRock = false;

            CurrentMap.AddObject(this);
            BroadcastInfo();

            if (effects) Broadcast(new S.ObjectTeleportIn { ObjectID = ObjectID, Type = effectnumber });
            
            BroadcastHealthChange();
            
            return true;
        }

        //随机传送
        public virtual bool TeleportRandom(int attempts, int distance, Map map = null)
        {
            if (map == null) map = CurrentMap;
            if (map.Cells == null) return false;
            Point p = map.RandomValidPoint();
            if(p.IsEmpty)
            {
                return false;
            }
            return Teleport(map, p);
        }

        public Point GetRandomPoint(int attempts, int distance, Map map)
        {
            byte edgeoffset = 0;

            if (map.Width < 150)
            {
                if (map.Height < 30) edgeoffset = 2;
                else edgeoffset = 20;
            }

            for (int i = 0; i < attempts; i++)
            {
                Point location;

                if (distance <= 0)
                    location = new Point(edgeoffset + RandomUtils.Next(map.Width - edgeoffset), edgeoffset + RandomUtils.Next(map.Height - edgeoffset)); //Can adjust Random Range...
                else
                    location = new Point(CurrentLocation.X + RandomUtils.Next(-distance, distance + 1),
                                         CurrentLocation.Y + RandomUtils.Next(-distance, distance + 1));


                if (map.ValidPoint(location)) return location;
            }

            return new Point(0, 0);
        }

        //对附近的发送广播
        //比如随机飞走了
        //比如掉血了
        //这里目前之显示组队玩家的血量，修改下所有玩家的血量都显示
        public void BroadcastHealthChange()
        {
 
            if (Race != ObjectType.Player && Race != ObjectType.Monster) return;
           
            byte time = Math.Min(byte.MaxValue, (byte)Math.Max(5, (RevTime - Envir.Time) / 1000));
            Packet p = new S.ObjectHealth { ObjectID = ObjectID, HP = this.Health,MaxHP=this.MaxHealth ,Expire = time };

            if (Envir.Time < RevTime)
            {
                CurrentMap.Broadcast(p, CurrentLocation);
                return;
            }
         
            if (Race == ObjectType.Monster && !AutoRev && Master == null) return;
   
            if (Race == ObjectType.Player)
            {
                if (AutoRev)
                {
                    Broadcast(p);
                    return;
                }

                if (GroupMembers != null) //Send HP to group
                {
                    for (int i = 0; i < GroupMembers.Count; i++)
                    {
                        PlayerObject member = GroupMembers[i];

                        if (this == member) continue;
                        if (member.CurrentMap != CurrentMap || !Functions.InRange(member.CurrentLocation, CurrentLocation, Globals.DataRange)) continue;
                        member.Enqueue(p);
                    }
                }

                return;
            }

            if (Master != null && Master.Race == ObjectType.Player)
            {
                PlayerObject player = (PlayerObject)Master;

                player.Enqueue(p);

                if (player.GroupMembers != null) //Send pet HP to group
                {
                    for (int i = 0; i < player.GroupMembers.Count; i++)
                    {
                        PlayerObject member = player.GroupMembers[i];

                        if (player == member) continue;

                        if (member.CurrentMap != CurrentMap || !Functions.InRange(member.CurrentLocation, CurrentLocation, Globals.DataRange)) continue;
                        member.Enqueue(p);
                    }
                }
            }


            if (EXPOwner != null && EXPOwner.Race == ObjectType.Player)
            {
                PlayerObject player = (PlayerObject)EXPOwner;

                if (player.IsMember(Master)) return;
                
                player.Enqueue(p);

                if (player.GroupMembers != null)
                {
                    for (int i = 0; i < player.GroupMembers.Count; i++)
                    {
                        PlayerObject member = player.GroupMembers[i];

                        if (player == member) continue;
                        if (member.CurrentMap != CurrentMap || !Functions.InRange(member.CurrentLocation, CurrentLocation, Globals.DataRange)) continue;
                        member.Enqueue(p);
                    }
                }
            }

        }

        public void BroadcastDamageIndicator(DamageType type, int damage = 0)
        {
            Packet p = new S.DamageIndicator { ObjectID = ObjectID, Damage = damage, Type = type };

            if (Race == ObjectType.Player)
            {
                PlayerObject player = (PlayerObject)this;
                player.Enqueue(p);
            }
            Broadcast(p);
        }

        public abstract void Die();
        public abstract int Pushed(MapObject pusher, MirDirection dir, int distance);

        public bool IsMember(MapObject member)
        {
            if (member == this) return true;
            if (GroupMembers == null || member == null) return false;

            for (int i = 0; i < GroupMembers.Count; i++)
                if (GroupMembers[i] == member) return true;

            return false;
        }

        public abstract void SendHealth(PlayerObject player);

        public bool InTrapRock
        {
            set
            {
                if (this is PlayerObject)
                {
                    PlayerObject player = (PlayerObject)this;
                    player.Enqueue(new S.InTrapRock { Trapped = value });
                }
            }
            get
            {
                Point checklocation;

                for (int i = 0; i <= 6; i += 2)
                {
                    checklocation = Functions.PointMove(CurrentLocation, (MirDirection)i, 1);

                    if (checklocation.X < 0) continue;
                    if (checklocation.X >= CurrentMap.Width) continue;
                    if (checklocation.Y < 0) continue;
                    if (checklocation.Y >= CurrentMap.Height) continue;

                    //Cell cell = CurrentMap.GetCell(checklocation.X, checklocation.Y);
                    if (!CurrentMap.Valid(checklocation.X, checklocation.Y) || CurrentMap.Objects[checklocation.X, checklocation.Y] == null) continue;

                    for (int j = 0; j < CurrentMap.Objects[checklocation.X, checklocation.Y].Count; j++)
                    {
                        MapObject ob = CurrentMap.Objects[checklocation.X, checklocation.Y][j];
                        switch (ob.Race)
                        {
                            case ObjectType.Monster:
                                if (ob is TrapRock)
                                {
                                    TrapRock rock = (TrapRock)ob;
                                    if (rock.Dead) continue;
                                    if (rock.Target != this) continue;
                                    if (!rock.Visible) continue;
                                }
                                else continue;

                                return true;
                            default:
                                continue;
                        }
                    }
                }
                return false;
            }
        }

    }

    public class Poison
    {
        public MapObject Owner;
        public PoisonType PType;
        public int Value;
        public long Duration, Time, TickTime, TickSpeed;

        public Poison() { }

        public Poison(BinaryReader reader)
        {
            Owner = null;
            PType = (PoisonType)reader.ReadByte();
            Value = reader.ReadInt32();
            Duration = reader.ReadInt64();
            Time = reader.ReadInt64();
            TickTime = reader.ReadInt64();
            TickSpeed = reader.ReadInt64();
        }
    }

    public class Buff
    {
        public BuffType Type;

        [JsonIgnore]
        public MapObject Caster;
        public bool Visible;
        public uint ObjectID;
        public long ExpireTime;
        public int[] Values;
        public bool Infinite;

        public bool RealTime;
        public DateTime RealTimeExpire;

        public bool Paused;//Buff是否暂停

        public Buff() { }

        public Buff(BinaryReader reader)
        {
            Type = (BuffType)reader.ReadByte();
            Caster = null;
            Visible = reader.ReadBoolean();
            ObjectID = reader.ReadUInt32();
            ExpireTime = reader.ReadInt64();

            if (Envir.LoadVersion < 56)
            {
                Values = new int[] { reader.ReadInt32() };
            }
            else
            {
                Values = new int[reader.ReadInt32()];

                for (int i = 0; i < Values.Length; i++)
                {
                    Values[i] = reader.ReadInt32();
                }
            }

            Infinite = reader.ReadBoolean();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            writer.Write(Visible);
            writer.Write(ObjectID);
            writer.Write(ExpireTime);

            writer.Write(Values.Length);
            for (int i = 0; i < Values.Length; i++)
            {
                writer.Write(Values[i]);
            }

            writer.Write(Infinite);
        }
    }
}
