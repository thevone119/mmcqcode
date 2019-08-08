﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ClientPackets;
using Server.MirDatabase;
using Server.MirEnvir;
using Server.MirNetwork;
using S = ServerPackets;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Server.MirObjects.Monsters;
using Newtonsoft.Json;

namespace Server.MirObjects
{
    /// <summary>
    /// 玩家各种操作的主逻辑，都在这里了，这个是核心类
    /// 登录后的玩家，都在这里
    /// 退出后，这个对象就回收了？
    /// </summary>
    public sealed class PlayerObject : MapObject
    {
        public string GMPassword = Settings.GMPassword;
        public bool IsGM, GMLogin, GMNeverDie, GMGameMaster, EnableGroupRecall, EnableGuildInvite, AllowMarriage, AllowLoverRecall, AllowMentor, HasMapShout, HasServerShout;


        public bool HasUpdatedBaseStats = true;

        public long LastRecallTime, LastRevivalTime, LastTeleportTime, LastProbeTime, MenteeEXP;
        public long LastOnlineTimeCheck;//最后检测时间
        public long LastItemSkillTime, LastSkillComm4,LastSkillComm5, LastSkillComm7;//最后检测时间



        public short Looks_Armour = 0, Looks_Weapon = -1, Looks_WeaponEffect = 0;
		public byte Looks_Wings = 0;

        public bool WarZone = false;

        public override ObjectType Race
        {
            get { return ObjectType.Player; }
        }

        public CharacterInfo Info;
        public AccountInfo Account;
        public MirConnection Connection;
        public Reporting Report;

   
        //
        //更改玩家的名字，这个是核心逻辑啊
        private String _Name = null;
        public override string Name
        {
            get {
                if(_Name==null|| _Name == String.Empty)
                {
                    
                    return Info.Name;
                }
                return _Name;
            }
            set {
                /*Check if Name exists.*/
                _Name = value;
            }
        }

        public override int CurrentMapIndex
        {
            get { return Info.CurrentMapIndex; }
            set { Info.CurrentMapIndex = value; }
        }
        public override Point CurrentLocation
        {
            get { return Info.CurrentLocation; }
            set { Info.CurrentLocation = value; }
        }
        public override MirDirection Direction
        {
            get { return Info.Direction; }
            set { Info.Direction = value; }
        }
        public override ushort Level
        {
            get { return Info.Level; }
            set { Info.Level = value; }
        }

        public override uint Health
        {
            get { return HP; }
        }
        public override uint MaxHealth
        {
            get { return MaxHP; }
        }

        public ushort HP
        {
            get { return Info.HP; }
            set { Info.HP = value; }
        }
        public ushort MP
        {
            get { return Info.MP; }
            set { Info.MP = value; }
        }

        public ushort MaxHP, MaxMP;

        //战场的攻击模式
        private WarGroup _WGroup;
        public override WarGroup WGroup
        {
            get { return _WGroup; }
            set { _WGroup = value; }
        }

        //玩家封号体系
        private PlayerTitle _playerTitle;
        public PlayerTitle playerTitle
        {
            get { return _playerTitle; }
            set { _playerTitle = value; }
        }

        //攻击模式，这个要改，增加战场攻击模式
        public override AttackMode AMode
        {
            get { return Info.AMode; }
            set { Info.AMode = value; }
        }
        public override PetMode PMode
        {
            get { return Info.PMode; }
            set { Info.PMode = value; }
        }

        public long Experience
        {
            set { Info.Experience = value; }
            get { return Info.Experience; }
        }
        public long MaxExperience;
        public byte LifeOnHit;//吸收伤害？
        public byte HpDrainRate;
        public float HpDrain = 0;

        public float ExpRateOffset = 0;

        public bool NewMail = false;

        public override int PKPoints
        {
            get { return Info.PKPoints; }
            set { Info.PKPoints = value; }
        }

        public byte Hair
        {
            get { return Info.Hair; }
            set { Info.Hair = value; }
        }
        public MirClass Class
        {
            get { return Info.Class; }
        }
        public MirGender Gender
        { get { return Info.Gender; } }

        public int BindMapIndex
        {
            get { return Info.BindMapIndex; }
            set { Info.BindMapIndex = value; }
        }
        public Point BindLocation
        {
            get { return Info.BindLocation; }
            set { Info.BindLocation = value; }
        }

        public bool RidingMount;
        public MountInfo Mount
        {
            get { return Info.Mount; }
        }
        public short MountType
        {
            get { return Mount.MountType; }
            set { Mount.MountType = value; }
        }

        public short TransformType;
        //FishingChance：上钩率 FishingProgressMax：30次尝试 FishingProgress下面的进度 
        public int FishingChance, FishingChanceCounter, FishingProgressMax, FishingProgress, FishingAutoReelChance = 0, FishingNibbleChance = 0;
        //正在钓鱼，自动消耗鱼饵，咬钩，首次咬钩
        public bool Fishing, FishingAutocast, FishFound, FishFirstFound;

        public bool CanMove
        {
            get { return !Dead && Envir.Time >= ActionTime && !Fishing && !CurrentPoison.HasFlag(PoisonType.Paralysis) && !CurrentPoison.HasFlag(PoisonType.LRParalysis) && !CurrentPoison.HasFlag(PoisonType.Frozen); }
        }
        public bool CanWalk
        {
            get { return !Dead && Envir.Time >= ActionTime && !InTrapRock && !Fishing && !CurrentPoison.HasFlag(PoisonType.Paralysis) && !CurrentPoison.HasFlag(PoisonType.LRParalysis) && !CurrentPoison.HasFlag(PoisonType.Frozen); }
        }
        public bool CanRun
        {
            get { return !Dead && Envir.Time >= ActionTime && (_stepCounter > 0 || FastRun) && (!Sneaking || ActiveSwiftFeet) && CurrentBagWeight <= MaxBagWeight && !CurrentPoison.HasFlag(PoisonType.Paralysis) && !CurrentPoison.HasFlag(PoisonType.LRParalysis) && !CurrentPoison.HasFlag(PoisonType.Frozen); }
        }
        public bool CanAttack
        {
            get
            {
                return !Dead && Envir.Time >= ActionTime && Envir.Time >= AttackTime && !CurrentPoison.HasFlag(PoisonType.Paralysis) && !CurrentPoison.HasFlag(PoisonType.LRParalysis) && !CurrentPoison.HasFlag(PoisonType.Frozen) && Mount.CanAttack && !Fishing;
            }
        }

        public bool CanRegen
        {
            get { return Envir.Time >= RegenTime && _runCounter == 0; }
        }
        private bool CanCast
        {
            get
            {
                return !Dead && Envir.Time >= ActionTime && Envir.Time >= SpellTime && !CurrentPoison.HasFlag(PoisonType.Stun) &&
                    !CurrentPoison.HasFlag(PoisonType.Paralysis) && !CurrentPoison.HasFlag(PoisonType.Frozen) && Mount.CanAttack && !Fishing;
            }
        }
        //MoveDelay 移动的延时，服务器端减少下延时，避免客户端卡顿问题
        public const long TurnDelay = 350, MoveDelay = 560, HarvestDelay = 350, RegenDelay = 10000, PotDelay = 200, HealDelay = 600, DuraDelay = 10000, VampDelay = 500, LoyaltyDelay = 1000, FishingCastDelay = 750, FishingDelay = 200, CreatureTimeLeftDelay = 1000, ItemExpireDelay = 60000, MovementDelay = 2000;
        public long ActionTime, RunTime, RegenTime, RegenItemSkillTime, PotTime, HealTime, AttackTime, StruckTime, TorchTime, DuraTime, DecreaseLoyaltyTime, IncreaseLoyaltyTime, ChatTime, ShoutTime, SpellTime, VampTime, SearchTime, FishingTime, LogTime, FishingFoundTime, CreatureTimeLeftTicker, StackingTime, ItemExpireTime, RestedTime, MovementTime;

        public byte ChatTick;

        public bool MagicShield;
        public byte MagicShieldLv;
        public long MagicShieldTime;

        public bool ElementalBarrier;
        public byte ElementalBarrierLv;
        public long ElementalBarrierTime;

        public bool HasElemental;
        public int ElementsLevel;

        public byte MagicParameter;

        private bool _concentrating;
        public bool Concentrating
        {
            get
            {
                return _concentrating;
            }
            set
            {
                if (_concentrating == value) return;
                _concentrating = value;
            }

        }
        public bool ConcentrateInterrupted;
        public long ConcentrateInterruptTime;

        public bool Stacking;

        public IntelligentCreatureType SummonedCreatureType = IntelligentCreatureType.None;
        public bool CreatureSummoned;

        public LevelEffects LevelEffects = LevelEffects.None;
        //_fishCounter:钓鱼的计数器，750毫秒变化一次的，30次就钓一下
        private int _stepCounter, _runCounter, _fishCounter, _restedCounter;

        //弓手当前的烈火阵，只有一个哦
        public MapObject ArcherTrapObject = null;

        public SpellObject[] PortalObjectsArray = new SpellObject[2];

        public NPCObject DefaultNPC
        {
            get
            {
                return SMain.Envir.DefaultNPC;
            }
        }
        //NPC控制
        public uint NPCID;
        public NPCPage NPCPage;
        public Dictionary<NPCSegment, bool> NPCSuccess = new Dictionary<NPCSegment, bool>();
        //这个是推迟处理，就是每次循环只处理一个NPC命令，只处理一行，处理完设置这个标志位，让其退出
        public bool NPCDelayed;
        public List<string> NPCSpeech = new List<string>();
        public Map NPCMoveMap;
        public Point NPCMoveCoord;
        public string NPCInputStr;

        //UserMatch：卖：true, 买：false
        public bool UserMatch;
        public string MatchName;
        public byte MatchType;
        public int PageSent;
        public List<AuctionInfo> Search = new List<AuctionInfo>();
        public List<ItemSets> ItemSets = new List<ItemSets>();
        public List<EquipmentSlot> MirSet = new List<EquipmentSlot>();

        public bool FatalSword, Slaying, TwinDrakeBlade, FlamingSword, MPEater, Hemorrhage, CounterAttack;
        public int MPEaterCount, HemorrhageAttackCount;
        public long FlamingSwordTime, CounterAttackTime;
        public bool ActiveBlizzard, ActiveReincarnation, ActiveSwiftFeet, ReincarnationReady;
        public PlayerObject ReincarnationTarget, ReincarnationHost;
        public long ReincarnationExpireTime;
        public byte Reflect;
        public bool UnlockCurse = false;
        public bool FastRun = false;
        public bool CanGainExp = true;

        //是否能创建喊
        public bool CanCreateGuild = false;
        public GuildObject MyGuild = null;//我的行会
        public Rank MyGuildRank = null;//我的行会职称
        public GuildObject PendingGuildInvite = null;
        public bool GuildNoticeChanged = true; //set to false first time client requests notice list, set to true each time someone in guild edits notice
        public bool GuildMembersChanged = true;//same as above but for members
        public bool GuildCanRequestItems = true;
        public bool RequestedGuildBuffInfo = false;
        //玩家在安全区不阻挡,可以穿人
        public override bool Blocking
        {
            get
            {
                return !Dead && !Observer && !InSafeZone;
            }
        }
        public bool AllowGroup
        {
            get { return Info.AllowGroup; }
            set { Info.AllowGroup = value; }
        }

        public bool AllowTrade
        {
            get { return Info.AllowTrade; }
            set { Info.AllowTrade = value; }
        }


        public bool GameStarted { get; set; }

        public bool HasTeleportRing, HasProtectionRing, HasRevivalRing;
        //NoDuraLoss:这个是不掉持久的意思
        public bool HasMuscleRing, HasClearRing, HasParalysisRing, HasProbeNecklace, NoDuraLoss;

        public PlayerObject MarriageProposal;
        public PlayerObject DivorceProposal;
        public PlayerObject MentorRequest;

        public PlayerObject GroupInvitation;
        public PlayerObject TradeInvitation;

        public PlayerObject TradePartner = null;
        public bool TradeLocked = false;
        public uint TradeGoldAmount = 0;

        public PlayerObject ItemRentalPartner = null;
        public UserItem ItemRentalDepositedItem = null;
        public uint ItemRentalFeeAmount = 0;
        public uint ItemRentalPeriodLength = 0;
        public bool ItemRentalFeeLocked = false;
        public bool ItemRentalItemLocked = false;

        //private long LastRankUpdate = Envir.Time;

        public List<QuestProgressInfo> CurrentQuests
        {
            get { return Info.CurrentQuests; }
        }

        public List<int> CompletedQuests
        {
            get { return Info.CompletedQuests; }
        }
        //AttackBonus：攻击加成，SkillNeckBoost：技能提升，技巧项链
        public byte AttackBonus, MineRate, GemRate, FishRate, CraftRate, SkillNeckBoost;

        //增加4个武器自带技能(其实只用到3个吧)
        public ItemSkill sk1, sk2, sk3, sk4;
        public ushort skCount;

        //是否具有某个技能
        public bool hasItemSk(ItemSkill sk)
        {
            if(sk1== sk)
            {
                return true;
            }
            if (sk2 == sk)
            {
                return true;
            }
            if (sk3 == sk)
            {
                return true;
            }
            if (sk4 == sk)
            {
                return true;
            }
            return false;
        }
        //初始化玩家
        public PlayerObject(CharacterInfo info, MirConnection connection)
        {
            if (info.Player != null)
                throw new InvalidOperationException("玩家不存在.");

            info.Player = this;
            info.Mount = new MountInfo(this);

            Connection = connection;
            Info = info;
            Account = Connection.Account;

            Report = new Reporting(this);

            if (Account.AdminAccount)
            {
                IsGM = true;
                Observer = true;//观察模式
                GMNeverDie = true;//无敌
                SMain.Enqueue(string.Format("{0} 是一个GM", Name));
            }
            //如果是第一次进入系统，则新建角色
            if ((Level == new CharacterInfo().Level) && Experience==0&& BindMapIndex==0)
            {
                NewCharacter();
            }

            if (Info.GuildIndex != -1)
            {
                MyGuild = Envir.GetGuild(Info.GuildIndex);
            }
            RefreshStats();

            if (HP == 0)
            {
                SetHP(MaxHP);
                SetMP(MaxMP);

                CurrentLocation = BindLocation;
                CurrentMapIndex = BindMapIndex;

                if (Info.PKPoints >= 200)
                {
                    Map temp = Envir.GetMapByNameAndInstance(Settings.PKTownMapName, 1);
                    Point tempLocation = new Point(Settings.PKTownPositionX, Settings.PKTownPositionY);

                    if (temp != null && temp.ValidPoint(tempLocation))
                    {
                        CurrentMapIndex = temp.Info.Index;
                        CurrentLocation = tempLocation;
                    }
                }
            }
        }
        //退出，结束游戏
        public void StopGame(byte reason)
        {
            if (Node == null) return;

            for (int i = 0; i < Pets.Count; i++)
            {
                MonsterObject pet = Pets[i];

                if (pet.Info.AI == 64)//IntelligentCreature
                {
                    //dont save Creatures they will miss alot of AI-Info when they get spawned on login
                    UnSummonIntelligentCreature(((IntelligentCreatureObject)pet).petType, false);
                    continue;
                }

                pet.Master = null;

                if (!pet.Dead)
                {
                    try
                    {
                        //契约兽不保存哦
                        if(pet.PType == PetType.Common)
                        {
                            Info.Pets.Add(new PetInfo(pet) { Time = Envir.Time });
                        }

                        Envir.MonsterCount--;
                        pet.CurrentMap.MonsterCount--;

                        pet.CurrentMap.RemoveObject(pet);
                        pet.Despawn();
                    }
                    catch
                    {
                        SMain.EnqueueDebugging(Name + " Pet logout was null on logout : " + pet != null ? pet.Name : "" + " " + pet.CurrentMap != null ? pet.CurrentMap.Info.Mcode : "");
                    }
                }
            }
            Pets.Clear();

         


            for (int i = 0; i < Info.Magics.Count; i++)
            {
                if (Envir.Time < (Info.Magics[i].CastTime + Info.Magics[i].GetDelay()))
                    Info.Magics[i].CastTime = Info.Magics[i].GetDelay() + Info.Magics[i].CastTime - Envir.Time;
                else
                    Info.Magics[i].CastTime = 0;
            }

            if (MyGuild != null) MyGuild.PlayerLogged(this, false);
            Envir.Players.Remove(this);
            CurrentMap.RemoveObject(this);
            Despawn();

            if (GroupMembers != null)
            {
                GroupMembers.Remove(this);
                RemoveGroupBuff();

                if (GroupMembers.Count > 1)
                {
                    Packet p = new S.DeleteMember { Name = Name };

                    for (int i = 0; i < GroupMembers.Count; i++)
                        GroupMembers[i].Enqueue(p);
                }
                else
                {
                    GroupMembers[0].Enqueue(new S.DeleteGroup());
                    GroupMembers[0].GroupMembers = null;
                }
                GroupMembers = null;
            }

            for (int i = 0; i < Buffs.Count; i++)
            {
                Buff buff = Buffs[i];
                if (buff.Infinite) continue;
                if (buff.Type == BuffType.Curse) continue;

                buff.Caster = null;
                if (!buff.Paused) buff.ExpireTime -= Envir.Time;

                Info.Buffs.Add(buff);
            }
            Buffs.Clear();

            for (int i = 0; i < PoisonList.Count; i++)
            {
                Poison poison = PoisonList[i];
                poison.Owner = null;
                poison.TickTime -= Envir.Time;

                Info.Poisons.Add(poison);
            }

            PoisonList.Clear();

            TradeCancel();
            CancelItemRental();
            RefineCancel();
            ItemCollectCancel();
            LogoutRelationship();
            LogoutMentor();

            string logReason = LogOutReason(reason);

            SMain.Enqueue(logReason);

            Fishing = false;

            Info.LastIP = Connection.IPAddress;
            Info.LastDate = Envir.Now;

            Report.Disconnected(logReason);
            Report.ForceSave();

            CleanUp();
        }

        private string LogOutReason(byte reason)
        {
            switch (reason)
            {
                //0-10 are 'senddisconnect to client'
                case 0:
                    return string.Format("{0} Has logged out. Reason: Server closed", Name);
                case 1:
                    return string.Format("{0} Has logged out. Reason: Double login", Name);
                case 2:
                    return string.Format("{0} Has logged out. Reason: Chat message too long", Name);
                case 3:
                    return string.Format("{0} Has logged out. Reason: Server crashed", Name);
                case 4:
                    return string.Format("{0} Has logged out. Reason: Kicked by admin", Name);
                case 5:
                    return string.Format("{0} Has logged out. Reason: Maximum connections reached", Name);
                case 10:
                    return string.Format("{0} Has logged out. Reason: Wrong client version", Name);
                case 20:
                    return string.Format("{0} Has logged out. Reason: User gone missing / disconnected", Name);
                case 21:
                    return string.Format("{0} Has logged out. Reason: Connection timed out", Name);
                case 22:
                    return string.Format("{0} Has logged out. Reason: User closed game", Name);
                case 23:
                    return string.Format("{0} Has logged out. Reason: User returned to select char", Name);
                default:
                    return string.Format("{0} Has logged out. Reason: Unknown", Name);
            }
        }

        //第一次登陆的时候，新建一个角色
        private void NewCharacter()
        {
            if (Envir.StartPoints.Count == 0) return;
            //绑定出生地
            SetBind();
            //等级设置为1
            Level = new CharacterInfo().Level;
            //随机选择一个发型
            //Hair = (byte)RandomUtils.Next(0, 9);
            Hair = 0;//固定发型，后面提供发型更改道具

            //分配初始化的装备
            for (int i = 0; i < Envir.StartItems.Count; i++)
            {
                ItemInfo info = Envir.StartItems[i];
                if (!CorrectStartItem(info)) continue;

                AddItem(info.CreateFreshItem());
            }
        }

        public long GetDelayTime(long original)
        {
            if (CurrentPoison.HasFlag(PoisonType.Slow))
            {
                return original * 2;
            }
            return original;
        }
        //死循环调用入口，这个用trycatch 包裹，因为这里非常多逻辑，非常容易发生异常
        public override void Process()
        {
            try
            {
                if (Connection == null || Node == null || Info == null) return;

                if (GroupInvitation != null && GroupInvitation.Node == null)
                    GroupInvitation = null;

                if (MagicShield && Envir.Time > MagicShieldTime)
                {
                    MagicShield = false;
                    MagicShieldLv = 0;
                    MagicShieldTime = 0;
                    CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.MagicShieldDown }, CurrentLocation);
                    RemoveBuff(BuffType.MagicShield);
                }

                if (ElementalBarrier && Envir.Time > ElementalBarrierTime)
                {
                    ElementalBarrier = false;
                    ElementalBarrierLv = 0;
                    ElementalBarrierTime = 0;
                    CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.ElementalBarrierDown }, CurrentLocation);
                }
                //离开那个范围，自动引爆烈火阵
                for (int i = 0; i <= 2; i++)//Self destruct when out of range (in this case 15 squares)
                {
                    if (ArcherTrapObject == null) continue;
                    if (FindObject(ArcherTrapObject.ObjectID, 8) != null) continue;
                    ExplosiveTrapDetonated();
                }

                if (CellTime + 700 < Envir.Time) _stepCounter = 0;

                if (Sneaking) CheckSneakRadius();

                if (FlamingSword && Envir.Time >= FlamingSwordTime)
                {
                    FlamingSword = false;
                    Enqueue(new S.SpellToggle { Spell = Spell.FlamingSword, CanUse = false });
                }

                if (CounterAttack && Envir.Time >= CounterAttackTime)
                {
                    CounterAttack = false;
                }

                if (ReincarnationReady && Envir.Time >= ReincarnationExpireTime)
                {
                    ReincarnationReady = false;
                    ActiveReincarnation = false;
                    ReincarnationTarget = null;
                    ReceiveChat("复活失败.", ChatType.System);
                }
                if ((ReincarnationReady || ActiveReincarnation) && (ReincarnationTarget == null || !ReincarnationTarget.Dead))
                {
                    ReincarnationReady = false;
                    ActiveReincarnation = false;
                    ReincarnationTarget = null;
                }

                if (Envir.Time > RunTime && _runCounter > 0)
                {
                    RunTime = Envir.Time + 1500;
                    _runCounter--;
                }

                if (Settings.RestedPeriod > 0)
                {
                    if (Envir.Time > RestedTime)
                    {
                        _restedCounter = InSafeZone ? _restedCounter + 1 : _restedCounter;

                        if (_restedCounter > 0)
                        {
                            int count = _restedCounter / (Settings.RestedPeriod * 60);

                            GiveRestedBonus(count);
                        }

                        RestedTime = Envir.Time + Settings.Second;
                    }
                }

                if (Stacking && Envir.Time > StackingTime)
                {
                    Stacking = false;

                    for (int i = 0; i < 8; i++)
                    {
                        if (Pushed(this, (MirDirection)i, 1) == 1) break;
                    }
                }

                if (NewMail)
                {
                    ReceiveChat("新邮件已经到达.", ChatType.System);

                    GetMail();
                }

                if (Account.ExpandedStorageExpiryDate < Envir.Now && Account.HasExpandedStorage)
                {
                    Account.HasExpandedStorage = false;
                    ReceiveChat("扩展存储已过期.", ChatType.System);
                    Enqueue(new S.ResizeStorage { Size = Account.Storage.Length, HasExpandedStorage = Account.HasExpandedStorage, ExpiryTime = Account.ExpandedStorageExpiryDate });
                }

                if (Envir.Time > IncreaseLoyaltyTime && Mount.HasMount)
                {
                    IncreaseLoyaltyTime = Envir.Time + (LoyaltyDelay * 60);
                    IncreaseMountLoyalty(1);
                }

                if (Envir.Time > FishingTime && Fishing)
                {
                    _fishCounter++;
                    UpdateFish();
                }

                if (Envir.Time > ItemExpireTime)
                {
                    ItemExpireTime = Envir.Time + ItemExpireDelay;

                    ProcessItems();
                }

                for (int i = Pets.Count() - 1; i >= 0; i--)
                {
                    MonsterObject pet = Pets[i];
                    if (pet.Dead) Pets.Remove(pet);
                }

  

                ProcessBuffs();
                ProcessInfiniteBuffs();
                ProcessRegen();
                ProcessPoison();

                RefreshCreaturesTimeLeft();

                UserItem item;
                if (Envir.Time > TorchTime)
                {
                    TorchTime = Envir.Time + 10000;
                    item = Info.Equipment[(int)EquipmentSlot.Torch];
                    if (item != null)
                    {
                        DamageItem(item, 5);

                        if (item.CurrentDura == 0)
                        {
                            Info.Equipment[(int)EquipmentSlot.Torch] = null;
                            Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                            RefreshStats();
                        }
                    }
                }

                if (Envir.Time > DuraTime)
                {
                    DuraTime = Envir.Time + DuraDelay;

                    for (int i = 0; i < Info.Equipment.Length; i++)
                    {
                        item = Info.Equipment[i];
                        if (item == null || !item.DuraChanged) continue; // || item.Info.Type == ItemType.Mount
                        item.DuraChanged = false;
                        Enqueue(new S.DuraChanged { UniqueID = item.UniqueID, CurrentDura = item.CurrentDura });
                    }
                }

                base.Process();
                //疲劳系统处理
                ProcessOnlineTime();
                ProcessItemSkill();
                RefreshNameColour();
                //当前是否可以免助跑
                FastRun = false;
                if (CurrentMap!=null && CurrentMap.Info!=null && CurrentMap.Info.CanFastRun)
                {
                    FastRun = CurrentMap.Info.CanFastRun;//可以免助跑
                }
            }
            catch(Exception ex)
            {
                
                SMain.Enqueue(ex);
            }
        }
        public override void SetOperateTime()
        {
            OperateTime = Envir.Time;
        }
        //处理在线时长
        //每10秒检测一次在线时长
        //在安全区不计算疲劳值
        //40级前不计算疲劳值
        private void ProcessOnlineTime()
        {
            if (Envir.Time < LastOnlineTimeCheck )
            {
                return;
            }
            if (InSafeZone)
            {
                return;
            }
            if (!Settings.openFatigue)
            {
                return;
            }
            LastOnlineTimeCheck = Envir.Time + 10000;
            if(Info.onlineDay == DateTime.Now.DayOfYear)
            {
                Info.onlineTime +=  10000;
            }
            else
            {
                Info.onlineDay = DateTime.Now.DayOfYear;
                Info.onlineTime = 0;
            }
        }

        /// <summary>
        /// 武器技能处理
        /// </summary>
        private void ProcessItemSkill()
        {
            if (Dead)
            {
                return;
            }
            if (Envir.Time < LastItemSkillTime)
            {
                return;
            }
            if (InSafeZone)
            {
                return;
            }
            LastItemSkillTime = Envir.Time + 1000;

            //噬血阵（大概3秒吸一次附近怪物的血，恢复自身）
            //加血量
            if (Envir.Time > LastSkillComm4 && hasItemSk(ItemSkill.Comm4) && RandomUtils.Next(100) < 20)
            {
                LastSkillComm4 = Envir.Time + 3000;
                int value = GetAttackPower(MinDC, MaxDC);
                int value2 = GetAttackPower(MinMC, MaxMC);
                int value3 = GetAttackPower(MinSC, MaxSC);
                if(value2> value)
                {
                    value = value2;
                }
                if (value3 > value)
                {
                    value = value3;
                }
                if (value < MaxHP / 15)
                {
                    value = MaxHP / 15;
                }
                value = value/3;
                value += skCount / 3;
                int xivalue = 0;
                CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.FlameRound }, CurrentLocation);
                List<MapObject> list = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 1);
                foreach (MapObject ob in list)
                {
                    if (ob == null)
                    {
                        continue;
                    }
                    if (ob.Race != ObjectType.Monster )
                    {
                        continue;
                    }
                    byte mon_ai = ((MonsterObject)ob).Info.AI;
                    if (mon_ai == 6 || mon_ai == 58 || mon_ai == 57)
                    {
                        continue;
                    }

                    if (!ob.IsAttackTarget(this))
                    {
                        continue;
                    }
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, ob, value, DefenceType.None,false);
                    ActionList.Add(action);
                    xivalue += value/2;
                }
     
                //吸血
                if (xivalue > 0 && HP<MaxHP && PotHealthAmount < MaxHP)
                {
                    PotHealthAmount += (ushort)xivalue;
                }
                return;
            }

            //迷幻，几率迷幻5-10秒
            if (Envir.Time > LastSkillComm5 && hasItemSk(ItemSkill.Comm5) && RandomUtils.Next(100) < 20)
            {
                CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Focus }, CurrentLocation);
                LastSkillComm5 = Envir.Time + 4000;
                List<MapObject> list = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 1);
                foreach (MapObject ob in list)
                {
                    if (ob == null)
                    {
                        continue;
                    }
                    if (ob.Race != ObjectType.Monster)
                    {
                        continue;
                    }
                    if (!ob.IsAttackTarget(this))
                    {
                        continue;
                    }
                    ((MonsterObject)ob).ShockTime = Envir.Time + RandomUtils.Next(5,10) * 1000;
                    ((MonsterObject)ob).SearchTime = ((MonsterObject)ob).ShockTime;
                    ob.Target = null;
                }
                return;
            }

            //天雷阵
            if (Envir.Time > LastSkillComm7 && hasItemSk(ItemSkill.Comm7) && RandomUtils.Next(100) < 20)
            {
                int value = GetAttackPower(MinDC, MaxDC);
                int value2 = GetAttackPower(MinMC, MaxMC);
                int value3 = GetAttackPower(MinSC, MaxSC);
                if (value2 > value)
                {
                    value = value2;
                }
                if (value3 > value)
                {
                    value = value3;
                }
                value += Level * 2;
                value += skCount;
                LastSkillComm7 = Envir.Time + 3000;
                CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.GreatFoxThunder }, CurrentLocation);
                //Broadcast(new S.ObjectEffect { ObjectID = this.ObjectID, Effect = SpellEffect.GreatFoxThunder });
                List<MapObject> list = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 2);
                foreach (MapObject ob in list)
                {
                    if (ob == null)
                    {
                        continue;
                    }
                    if (ob.Race != ObjectType.Monster)
                    {
                        continue;
                    }
                    byte mon_ai = ((MonsterObject)ob).Info.AI;

                    if (mon_ai == 6 || mon_ai==58|| mon_ai == 57)
                    {
                        continue;
                    }

                    if (!ob.IsAttackTarget(this))
                    {
                        continue;
                    }
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, ob, value, DefenceType.MAC,false);
                    ActionList.Add(action);
                }

                return;
            }

        }


        
        private void ProcessBuffs()
        {
            bool refresh = false;

            for (int i = Buffs.Count - 1; i >= 0; i--)
            {
                Buff buff = Buffs[i];

                if (Envir.Time <= buff.ExpireTime || buff.Infinite || buff.Paused) continue;

                Buffs.RemoveAt(i);
                Enqueue(new S.RemoveBuff { Type = buff.Type, ObjectID = ObjectID });

                if (buff.Visible)
                    Broadcast(new S.RemoveBuff { Type = buff.Type, ObjectID = ObjectID });

                switch (buff.Type)
                {
                    case BuffType.MoonLight:
                    case BuffType.Hiding:
                    case BuffType.DarkBody:
                        if (!HasClearRing) Hidden = false;
                        Sneaking = false;
                        for (int j = 0; j < Buffs.Count; j++)
                        {
                            switch (Buffs[j].Type)
                            {
                                case BuffType.Hiding:
                                case BuffType.MoonLight:
                                case BuffType.DarkBody:
                                    if (Buffs[j].Type != buff.Type)
                                        Buffs[j].ExpireTime = 0;
                                    break;
                            }
                        }
                        break;
                    case BuffType.Concentration:
                        ConcentrateInterrupted = false;
                        ConcentrateInterruptTime = 0;
                        Concentrating = false;
                        UpdateConcentration();
                        break;
                    case BuffType.SwiftFeet:
                        ActiveSwiftFeet = false;
                        break;
                }

                refresh = true;
            }

            if (Concentrating && !ConcentrateInterrupted && (ConcentrateInterruptTime != 0))
            {
                //check for reenable
                if (ConcentrateInterruptTime <= SMain.Envir.Time)
                {
                    ConcentrateInterruptTime = 0;
                    UpdateConcentration();//Update & send to client
                }
            }

            if (refresh) RefreshStats();
        }
        private void ProcessInfiniteBuffs()
        {
            bool hiding = false;
            bool isGM = false;
            bool mentalState = false;

            for (int i = Buffs.Count - 1; i >= 0; i--)
            {
                Buff buff = Buffs[i];

                if (!buff.Infinite) continue;

                bool removeBuff = false;

                switch (buff.Type)
                {
                    case BuffType.Hiding:
                        hiding = true;
                        if (!HasClearRing) removeBuff = true;
                        break;
                    case BuffType.MentalState:
                        mentalState = true;
                        break;
                    case BuffType.GameMaster:
                        isGM = true;
                        if (!IsGM) removeBuff = true;
                        break;
                }

                if (removeBuff)
                {
                    Buffs.RemoveAt(i);
                    Enqueue(new S.RemoveBuff { Type = buff.Type, ObjectID = ObjectID });

                    switch (buff.Type)
                    {
                        case BuffType.Hiding:
                            Hidden = false;
                            break;
                    }
                }
            }

            if (HasClearRing && !hiding)
            {
                AddBuff(new Buff { Type = BuffType.Hiding, Caster = this, ExpireTime = Envir.Time + 100, Infinite = true });
            }

            if (GetMagic(Spell.MentalState) != null && !mentalState)
            {
                AddBuff(new Buff { Type = BuffType.MentalState, Caster = this, ExpireTime = Envir.Time + 100, Values = new int[] { Info.MentalState }, Infinite = true });
            }

            if (IsGM && !isGM)
            {
                AddBuff(new Buff { Type = BuffType.GameMaster, Caster = this, ExpireTime = Envir.Time + 100, Values = new int[] { 0 }, Infinite = true, Visible = Settings.GameMasterEffect });
            }
        }
        private void ProcessRegen()
        {
            if (Dead) return;

            int healthRegen = 0, manaRegen = 0;

            //自动回血的
            if (CanRegen)
            {
                RegenTime = Envir.Time + RegenDelay;
                if (HP < MaxHP)
                {
                    healthRegen += (int)(MaxHP * 0.03F) + 1;
                    healthRegen += (int)(healthRegen * ((double)HealthRecovery / Settings.HealthRegenWeight));
                }

                if (MP < MaxMP)
                {
                    manaRegen += (int)(MaxMP * 0.03F) + 1;
                    manaRegen += (int)(manaRegen * ((double)SpellRecovery / Settings.ManaRegenWeight));
                }
            }

            //回血阵，回蓝阵，每5秒恢复
            if (Envir.Time > RegenItemSkillTime)
            {
                RegenItemSkillTime = Envir.Time + 5000;
                int skHealth = 0;
                int skMana = 0;

                if (hasItemSk(ItemSkill.Comm1) && MP < MaxMP)
                {
                    skMana += (int)(MaxMP * 0.04F) + 1;
                    skMana += (int)(skMana * ((double)SpellRecovery / Settings.ManaRegenWeight));
                }

                if (hasItemSk(ItemSkill.Comm2) && HP < MaxHP)
                {
                    skHealth += (int)(MaxHP * 0.04F) + 1;
                    skHealth += (int)(skHealth * ((double)HealthRecovery / Settings.HealthRegenWeight));
                }

                healthRegen += skHealth;
                manaRegen += skMana;
            }
                

            //喝血加的
            if (Envir.Time > PotTime)
            {
                //PotTime = Envir.Time + Math.Max(50,Math.Min(PotDelay, 600 - (Level * 10)));
                PotTime = Envir.Time + PotDelay;
                int PerTickRegen = 5 + (Level / 10);

                if (PotHealthAmount > PerTickRegen)
                {
                    healthRegen += PerTickRegen;
                    PotHealthAmount -= (ushort)PerTickRegen;
                }
                else
                {
                    healthRegen += PotHealthAmount;
                    PotHealthAmount = 0;
                }

                if (PotManaAmount > PerTickRegen)
                {
                    manaRegen += PerTickRegen;
                    PotManaAmount -= (ushort)PerTickRegen;
                }
                else
                {
                    manaRegen += PotManaAmount;
                    PotManaAmount = 0;
                }
            }

            if (Envir.Time > HealTime)
            {
                HealTime = Envir.Time + HealDelay;

                int incHeal = (Level / 10) + (HealAmount / 10);
                if (HealAmount > (5 + incHeal))
                {
                    healthRegen += (5 + incHeal);
                    HealAmount -= (ushort)Math.Min(HealAmount, 5 + incHeal);
                }
                else
                {
                    healthRegen += HealAmount;
                    HealAmount = 0;
                }
            }

            //噬血术加的血
            if (Envir.Time > VampTime)
            {
                VampTime = Envir.Time + VampDelay;
                //限制噬血术最大累计为当前最大HP
                if(VampAmount> MaxHP)
                {
                    VampAmount = MaxHP;
                }
                if (VampAmount > 10)
                {
                    healthRegen += 10;
                    VampAmount -= 10;
                }
                else
                {
                    healthRegen += VampAmount;
                    VampAmount = 0;
                }
            }

            if (healthRegen > 0) ChangeHP(healthRegen);
            if (HP == MaxHP)
            {
                PotHealthAmount = 0;
                HealAmount = 0;
            }

            if (manaRegen > 0) ChangeMP(manaRegen);
            if (MP == MaxMP) PotManaAmount = 0;
        }
        //这里有BUG，会出现数组越界
        private void ProcessPoison()
        {
            PoisonType type = PoisonType.None;
            ArmourRate = 1F;
            DamageRate = 1F;

            for (int i = PoisonList.Count - 1; i >= 0; i--)
            {
                //加多一个判断，避免数组越界啊，这里有点坑的
                if (Dead || PoisonList.Count == 0 || i>= PoisonList.Count) return;

                Poison poison = PoisonList[i];

                if (poison.Owner != null && poison.Owner.Node == null)
                {
                    PoisonList.RemoveAt(i);
                    continue;
                }

                if (Envir.Time > poison.TickTime)
                {
                    poison.Time++;
                    poison.TickTime = Envir.Time + poison.TickSpeed;

                    if (poison.Time >= poison.Duration)
                    {
                        PoisonList.RemoveAt(i);
                        //这里直接返回
                        continue;
                    }
                        

                    if (poison.PType == PoisonType.Green || poison.PType == PoisonType.Bleeding)
                    {
                        LastHitter = poison.Owner;
                        LastHitTime = Envir.Time + 10000;

                        if (poison.PType == PoisonType.Bleeding)
                        {
                            Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Bleeding, EffectType = 0 });
                        }

                        //ChangeHP(-poison.Value);
                        PoisonDamage(-poison.Value, poison.Owner);

                        if (Dead) break;
                        RegenTime = Envir.Time + RegenDelay;
                    }

                    if (poison.PType == PoisonType.DelayedExplosion)
                    {
                        if (Envir.Time > ExplosionInflictedTime) ExplosionInflictedStage++;

                        if (!ProcessDelayedExplosion(poison))
                        {
                            if (Dead) break;

                            ExplosionInflictedStage = 0;
                            ExplosionInflictedTime = 0;

                            PoisonList.RemoveAt(i);
                            continue;
                        }
                    }
                }

                switch (poison.PType)
                {
                    case PoisonType.Red:
                        ArmourRate -= 0.10F;
                        break;
                    case PoisonType.Stun:
                        DamageRate += 0.20F;
                        break;
                }
                type |= poison.PType;
                /*
                if ((int)type < (int)poison.PType)
                    type = poison.PType;
                */
            }

            if (type == CurrentPoison) return;

            Enqueue(new S.Poisoned { Poison = type });
            Broadcast(new S.ObjectPoisoned { ObjectID = ObjectID, Poison = type });

            CurrentPoison = type;
        }
        private bool ProcessDelayedExplosion(Poison poison)
        {
            if (Dead) return false;

            if (ExplosionInflictedStage == 0)
            {
                Enqueue(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 0 });
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 0 });
                return true;
            }
            if (ExplosionInflictedStage == 1)
            {
                if (Envir.Time > ExplosionInflictedTime)
                    ExplosionInflictedTime = poison.TickTime + 3000;
                Enqueue(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 1 });
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 1 });
                return true;
            }
            if (ExplosionInflictedStage == 2)
            {
                Enqueue(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 2 });
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 2 });
                if (poison.Owner != null)
                {
                    switch (poison.Owner.Race)
                    { 
                        case ObjectType.Player:
                            PlayerObject caster = (PlayerObject)poison.Owner;
                            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time, poison.Owner, caster.GetMagic(Spell.DelayedExplosion), poison.Value, this.CurrentLocation);
                            CurrentMap.ActionList.Add(action);
                            //Attacked((PlayerObject)poison.Owner, poison.Value, DefenceType.MAC, false);
                            break;
                        case ObjectType.Monster://this is in place so it could be used by mobs if one day someone chooses to
                            Attacked((MonsterObject)poison.Owner, poison.Value, DefenceType.MAC);
                            break;
                     
                    }
                    
                    LastHitter = poison.Owner;
                }
                return false;
            }
            return false;
        }

        private void ProcessItems()
        {
            for (var i = 0; i < Info.Inventory.Length; i++)
            {
                var item = Info.Inventory[i];

                if (item?.ExpireInfo?.ExpiryDate <= Envir.Now)
                {
                    ReceiveChat($"你的背包物品 {item.Info.FriendlyName} 已过期.", ChatType.Hint);
                    Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                    Info.Inventory[i] = null;

                    continue;
                }

                if (item?.RentalInformation?.RentalLocked != true ||
                    !(item?.RentalInformation?.ExpiryDate <= Envir.Now))
                    continue;

                ReceiveChat($"租赁锁已被取消 {item.Info.FriendlyName}.", ChatType.Hint);
                item.RentalInformation = null;
            }

            for (var i = 0; i < Info.Equipment.Length; i++)
            {
                var item = Info.Equipment[i];

                if (item?.ExpireInfo?.ExpiryDate <= Envir.Now)
                {
                    ReceiveChat($"你的装备 {item.Info.FriendlyName} 已过期.", ChatType.Hint);
                    Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                    Info.Equipment[i] = null;

                    continue;
                }

                if (item?.RentalInformation?.RentalLocked != true ||
                    !(item?.RentalInformation?.ExpiryDate <= Envir.Now))
                    continue;

                ReceiveChat($"租赁锁已被取消 {item.Info.FriendlyName}.", ChatType.Hint);
                item.RentalInformation = null;
            }
        }

        public override void Process(DelayedAction action)
        {
            if (action.FlaggedToRemove)
                return;

            switch (action.Type)
            {
                case DelayedType.Magic:
                    CompleteMagic(action.Params);
                    break;
                case DelayedType.Damage:
                    CompleteAttack(action.Params);
                    break;
                case DelayedType.MapMovement:
                    CompleteMapMovement(action.Params);
                    break;
                case DelayedType.Mine:
                    CompleteMine(action.Params);
                    break;
                case DelayedType.NPC:
                    CompleteNPC(action.Params);
                    break;
                case DelayedType.Poison:
                    CompletePoison(action.Params);
                    break;
                case DelayedType.DamageIndicator:
                    CompleteDamageIndicator(action.Params);
                    break;
            }
        }

        private void SetHP(ushort amount)
        {
            if (HP == amount) return;

            HP = amount <= MaxHP ? amount : MaxHP;
            HP = GMNeverDie ? MaxHP : HP;

            if (!Dead && HP == 0) Die();

            //HealthChanged = true;
            Enqueue(new S.HealthChanged { HP = HP, MP = MP });
            BroadcastHealthChange();
        }
        private void SetMP(ushort amount)
        {
            if (MP == amount) return;
            //was info.MP
            MP = amount <= MaxMP ? amount : MaxMP;
            MP = GMNeverDie ? MaxMP : MP;

            // HealthChanged = true;
            Enqueue(new S.HealthChanged { HP = HP, MP = MP });
            BroadcastHealthChange();
        }

        public void ChangeHP(int amount)
        {
            //if (amount < 0) amount = (int)(amount * PoisonRate);
            if (amount == 0)
            {
                return;
            }

            if (HasProtectionRing && MP > 0 && amount < 0)
            {
                ChangeMP(amount);
                return;
            }

            ushort value = (ushort)Math.Max(ushort.MinValue, Math.Min(MaxHP, HP + amount));

            if (value == HP) return;

            HP = value;
            HP = GMNeverDie ? MaxHP : HP;

            if (!Dead && HP == 0) Die();

            // HealthChanged = true;
            Enqueue(new S.HealthChanged { HP = HP, MP = MP });
            BroadcastHealthChange();
        }
        //use this so you can have mobs take no/reduced poison damage
        public void PoisonDamage(int amount, MapObject Attacker)
        {
            ChangeHP(amount);
        }
        public void ChangeMP(int amount)
        {
            ushort value = (ushort)Math.Max(ushort.MinValue, Math.Min(MaxMP, MP + amount));

            if (value == MP) return;

            MP = value;
            MP = GMNeverDie ? MaxMP : MP;

            // HealthChanged = true;
            Enqueue(new S.HealthChanged { HP = HP, MP = MP });
            BroadcastHealthChange();
        }
        public override void Die()
        {
            if (HasRevivalRing && Envir.Time > LastRevivalTime)
            {
                LastRevivalTime = Envir.Time + 300000;

                for (var i = (int)EquipmentSlot.RingL; i <= (int)EquipmentSlot.RingR; i++)
                {
                    var item = Info.Equipment[i];

                    if (item == null) continue;
                    if (!(item.Info.Unique.HasFlag(SpecialItemMode.Revival)) || item.CurrentDura < 1000) continue;
                    SetHP(MaxHP);
                    item.CurrentDura = (ushort)(item.CurrentDura - 1000);
                    Enqueue(new S.DuraChanged { UniqueID = item.UniqueID, CurrentDura = item.CurrentDura });
                    RefreshStats();
                    ReceiveChat("你得到了第二次生命的机会", ChatType.System);
                    return;
                }
            }

            if (LastHitter != null && LastHitter.Race == ObjectType.Player)
            {
                PlayerObject hitter = (PlayerObject)LastHitter;

                if (AtWar(hitter) || WarZone)
                {
                    hitter.ReceiveChat(string.Format("正当防卫，你受到法律保护"), ChatType.System);
                }
                else if (Envir.Time > BrownTime && PKPoints < 200)
                {
                    UserItem weapon = hitter.Info.Equipment[(byte)EquipmentSlot.Weapon];

                    hitter.PKPoints = Math.Min(int.MaxValue, LastHitter.PKPoints + 100);
                    hitter.ReceiveChat(string.Format("你谋杀了 {0}", Name), ChatType.System);
                    ReceiveChat(string.Format("你被谋杀了 {0}", LastHitter.Name), ChatType.System);

                    if (weapon != null && weapon.Luck > (Settings.MaxLuck * -1) && RandomUtils.Next(4) == 0)
                    {
                        weapon.Luck--;
                        hitter.ReceiveChat("你的武器已经被诅咒了.", ChatType.System);
                        hitter.Enqueue(new S.RefreshItem { Item = weapon });
                    }
                }
            }

            UnSummonIntelligentCreature(SummonedCreatureType);

            for (int i = Pets.Count - 1; i >= 0; i--)
            {
                if (Pets[i].Dead) continue;
                Pets[i].Die();
            }

            

            if (MagicShield)
            {
                MagicShield = false;
                CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.MagicShieldDown }, CurrentLocation);
            }
            if (ElementalBarrier)
            {
                ElementalBarrier = false;
                CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.ElementalBarrierDown }, CurrentLocation);
            }

            if (PKPoints > 200)
                RedDeathDrop(LastHitter);
            else if (!InSafeZone)
                DeathDrop(LastHitter);

            HP = 0;
            Dead = true;

            LogTime = Envir.Time;
            BrownTime = Envir.Time;

            Enqueue(new S.Death { Direction = Direction, Location = CurrentLocation });
            Broadcast(new S.ObjectDied { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });

            for (int i = 0; i < Buffs.Count; i++)
            {
                if (Buffs[i].Type == BuffType.Curse)
                {
                    Buffs.RemoveAt(i);
                    break;
                }
            }

            PoisonList.Clear();
            InTrapRock = false;

            CallDefaultNPC(DefaultNPCType.Die);

            Report.Died(CurrentMap.Info.Mcode);
            //这里加入特殊处理
            if ( CurrentMap != null && CurrentMap.mapSProcess != null)
            {
                CurrentMap.mapSProcess.PlayerDie(this);
            }
        }

        //死亡爆东西
        //这里空指针
        private void DeathDrop(MapObject killer)
        {
            var pkbodydrop = true;

            if (CurrentMap.Info.NoDropPlayer && Race == ObjectType.Player)
                return;

            if ((killer == null) || ((pkbodydrop) || (killer.Race != ObjectType.Player)))
            {
                for (var i = 0; i < Info.Equipment.Length; i++)
                {
                    var item = Info.Equipment[i];

                    if (item == null)
                        continue;

                    if (item.Info.Bind.HasFlag(BindMode.DontDeathdrop))
                        continue;

                    // TODO: Check this.
                    if (item.WeddingRing != -1)
                        continue;

                    //绑定装备不会掉落
                    if (item.SoulBoundId > 0)
                    {
                        continue;
                    }

                    if (((killer == null) || ((killer != null) && (killer.Race != ObjectType.Player))))
                    {
                        if (item.Info.Bind.HasFlag(BindMode.BreakOnDeath))
                        {
                            Info.Equipment[i] = null;
                            Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                            ReceiveChat($"你的 {item.FriendlyName} 死亡破碎了.", ChatType.System2);
                            Report.ItemChanged("Death Drop", item, item.Count, 1);
                        }
                    }
                    if (ItemSets.Any(set => set.Set == ItemSet.Spirit && !set.SetComplete))
                    {
                        if (item.Info.Set == ItemSet.Spirit)
                        {
                            Info.Equipment[i] = null;
                            Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });

                            Report.ItemChanged("Death Drop", item, item.Count, 1);
                        }
                    }

                    if (item.Count > 1)
                    {
                        var percent = RandomUtils.RandomomRange(10, 8);
                        var count = (uint)Math.Ceiling(item.Count / 10F * percent);

                        if (count > item.Count)
                            throw new ArgumentOutOfRangeException();
                        
                        var temp2 = item.Info.CreateFreshItem();
                        temp2.Count = count;

                        if (!DropItem(temp2, Settings.DropRange, true))
                            continue;

                        if (count == item.Count)
                            Info.Equipment[i] = null;

                        Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = count });
                        item.Count -= count;

                        Report.ItemChanged("Death Drop", item, count, 1);
                    }
                    else if (RandomUtils.Next(35) == 0)//这个改下死亡掉东西几率，之前是30分之1
                    {
                        if (Envir.ReturnRentalItem(item, item.RentalInformation?.OwnerName, Info))
                        {
                            Info.Equipment[i] = null;
                            Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
   
                            ReceiveChat($"你死亡并且 {item.Info.FriendlyName} 已经归还给它的主人.", ChatType.Hint);
                            Report.ItemMailed("Death Dropped Rental Item", item, 1, 1);
                            continue;
                        }
                        //爆东西，如果幻化的武器，衣服，不掉落，只爆
                        if (item.n_Image > 0)
                        {
                            item.n_Image = 0;
                            item.n_Shape = -1;
                            item.n_Effect = 0;
                            ReceiveChat($"你的装备 {item.FriendlyName} 在死亡中幻化损坏.", ChatType.System2);
                            Enqueue(new S.RefreshItem { Item = item });
                            continue;
                        }

                        if (!DropItem(item, Settings.DropRange, true))
                            continue;
                        

                        if (item.Info.GlobalDropNotify)
                            foreach (var player in Envir.Players)
                            {
                                player.ReceiveChat($"{Name} 掉落 {item.FriendlyName}.", ChatType.System2);
                            }

                        Info.Equipment[i] = null;
                        Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });

                        Report.ItemChanged("Death Drop", item, item.Count, 1);
                    }
                }

            }

            for (var i = 0; i < Info.Inventory.Length; i++)
            {
                var item = Info.Inventory[i];

                if (item == null)
                    continue;

                if (item.Info.Bind.HasFlag(BindMode.DontDeathdrop))
                    continue;

                if (item.WeddingRing != -1)
                    continue;


                //绑定装备不会掉落
                if (item.SoulBoundId > 0)
                {
                    continue;
                }

                if (item.Count > 1)
                {
                    var percent = RandomUtils.RandomomRange(10, 8);

                    if (percent == 0)
                        continue;

                    var count = (uint)Math.Ceiling(item.Count / 10F * percent);

                    if (count > item.Count)
                        throw new ArgumentOutOfRangeException();

                    var temp2 = item.Info.CreateFreshItem();
                    temp2.Count = count;

                    if (!DropItem(temp2, Settings.DropRange, true))
                        continue;

                    if (count == item.Count)
                        Info.Inventory[i] = null;

                    Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = count });
                    item.Count -= count;

                    Report.ItemChanged("DeathDrop", item, count, 1);
                }
                else if (RandomUtils.Next(10) == 0)
                {
                    if (Envir.ReturnRentalItem(item, item.RentalInformation?.OwnerName, Info))
                    {
                        Info.Inventory[i] = null;
                        Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });

                        ReceiveChat($"你死亡并且 {item.Info.FriendlyName} 已经归还给它的主人.", ChatType.Hint);
                        Report.ItemMailed("Death Dropped Rental Item", item, 1, 1);

                        continue;
                    }

                    //爆东西，如果幻化的武器，衣服，不掉落，只爆
                    if (item.n_Image > 0)
                    {
                        item.n_Image = 0;
                        item.n_Shape = -1;
                        item.n_Effect = 0;
                        ReceiveChat($"你的装备 {item.FriendlyName} 在死亡中幻化损坏.", ChatType.System2);
                        Enqueue(new S.RefreshItem { Item = item });
                        continue;
                    }

                    if (!DropItem(item, Settings.DropRange, true))
                        continue;

                    if (item.Info.GlobalDropNotify)
                        foreach (var player in Envir.Players)
                        {
                            player.ReceiveChat($"{Name} 掉落 {item.FriendlyName}.", ChatType.System2);
                        }

                    Info.Inventory[i] = null;
                    Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });

                    Report.ItemChanged("DeathDrop", item, item.Count, 1);
                }
            }

            RefreshStats();
        }
        private void RedDeathDrop(MapObject killer)
        {
            if (killer == null || killer.Race != ObjectType.Player)
            {
                for (var i = 0; i < Info.Equipment.Length; i++)
                {
                    var item = Info.Equipment[i];

                    if (item == null)
                        continue;

                    if (item.Info.Bind.HasFlag(BindMode.DontDeathdrop))
                        continue;

                    // TODO: Check this.
                    if ((item.WeddingRing != -1))
                        continue;

                    //绑定装备不会掉落
                    if (item.SoulBoundId > 0)
                    {
                        continue;
                    }

                    if (item.Info.Bind.HasFlag(BindMode.BreakOnDeath))
                    {
                        Info.Equipment[i] = null;
                        Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                        ReceiveChat($"你的 {item.FriendlyName} 已破碎.", ChatType.System2);
                        Report.ItemChanged("RedDeathDrop", item, item.Count, 1);
                    }

                    if (item.Count > 1)
                    {
                        var percent = RandomUtils.RandomomRange(10, 4);
                        var count = (uint)Math.Ceiling(item.Count / 10F * percent);

                        if (count > item.Count)
                            throw new ArgumentOutOfRangeException();

                        var temp2 = item.Info.CreateFreshItem();
                        temp2.Count = count;

                        if (!DropItem(temp2, Settings.DropRange, true))
                            continue;

                        if (count == item.Count)
                            Info.Equipment[i] = null;

                        Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = count });
                        item.Count -= count;

                        Report.ItemChanged("RedDeathDrop", item, count, 1);
                    }
                    else if (RandomUtils.Next(10) == 0)
                    {
                        if (Envir.ReturnRentalItem(item, item.RentalInformation?.OwnerName, Info))
                        {
                            Info.Equipment[i] = null;
                            Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });

                            ReceiveChat($"你死亡并且 {item.Info.FriendlyName} 已经归还给它的主人", ChatType.Hint);
                            Report.ItemMailed("Death Dropped Rental Item", item, 1, 1);

                            continue;
                        }

                        //爆东西，如果幻化的武器，衣服，不掉落，只爆
                        if (item.n_Image > 0)
                        {
                            item.n_Image = 0;
                            item.n_Shape = -1;
                            item.n_Effect = 0;
                            ReceiveChat($"你的装备 {item.FriendlyName} 在死亡中幻化损坏.", ChatType.System2);
                            Enqueue(new S.RefreshItem { Item = item });
                            continue;
                        }

                        if (!DropItem(item, Settings.DropRange, true))
                            continue;

             

                        if (item.Info.GlobalDropNotify)
                            foreach (var player in Envir.Players)
                            {
                                player.ReceiveChat($"{Name} 掉落 {item.FriendlyName}.", ChatType.System2);
                            }

                        Info.Equipment[i] = null;
                        Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });

                        Report.ItemChanged("RedDeathDrop", item, item.Count, 1);
                    }
                }

            }

            for (var i = 0; i < Info.Inventory.Length; i++)
            {
                var item = Info.Inventory[i];

                if (item == null)
                    continue;

                if (item.Info.Bind.HasFlag(BindMode.DontDeathdrop))
                    continue;

                if (item.WeddingRing != -1)
                    continue;

                //绑定装备不会掉落
                if (item.SoulBoundId > 0)
                {
                    continue;
                }
                //爆东西，如果幻化的武器，衣服，不掉落，只爆
                if (item.n_Image > 0)
                {
                    item.n_Image = 0;
                    item.n_Shape = -1;
                    item.n_Effect = 0;
                    ReceiveChat($"你的装备 {item.FriendlyName} 在死亡中幻化损坏.", ChatType.System2);
                    Enqueue(new S.RefreshItem { Item = item });
                    continue;
                }

                if (Envir.ReturnRentalItem(item, item.RentalInformation?.OwnerName, Info))
                {
                    Info.Inventory[i] = null;
                    Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });

                    ReceiveChat($"你死亡并且 {item.Info.FriendlyName} 已经归还给它的主人", ChatType.Hint);
                    Report.ItemMailed("Death Dropped Rental Item", item, 1, 1);

                    continue;
                }

                if (!DropItem(item, Settings.DropRange, true))
                    continue;

                if (item.Info.GlobalDropNotify)
                    foreach (var player in Envir.Players)
                    {
                        player.ReceiveChat($"{Name} 掉落 {item.FriendlyName}.", ChatType.System2);
                    }

                Info.Inventory[i] = null;
                Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });

                Report.ItemChanged("RedDeathDrop", item, item.Count, 1);
            }

            RefreshStats();
        }


        //杀怪处理
        public void killMon(MonsterObject mon)
        {
            if (mon == null || mon.CurrentMap == null || mon.CurrentMap != CurrentMap || mon.Info == null || mon.IsCopy)
            {
                return;
            }
            //杀怪轮回
            SaItem(mon);
            string mname = mon.Name;
            int monidx = 0;
            int killMax = 500;//最大杀怪数，大于这个数，触发BOSS
            //蜈蚣怪
            if (mname.Equals("蜈蚣") || mname.Equals("跳跳蜂") || mname.Equals("钳虫") || mname.Equals("巨型蠕虫"))
            {
                if (Info.killMon[monidx] >= killMax)
                {
                    if (RandomUtils.Next(5) == 1 && CurrentMap.ValidPoint(Functions.PointMove(CurrentLocation, Direction, 1)))
                    {
                        MonsterInfo info = Envir.GetMonsterInfo("触龙神");
                        if (info == null) return;
                        MonsterObject monster;
                        monster = MonsterObject.GetMonster(info);
                        monster.Direction = Functions.ReverseDirection(Direction);
                        monster.ActionTime = Envir.Time + 1000;
                        monster.Spawn(CurrentMap, Functions.PointMove(CurrentLocation, Direction, 1));
                        Info.killMon[monidx] = 0;
                    }
                    else
                    {
                        ReceiveChat("[触龙神]  你的杀戮已触怒了我,赶紧离开我的领地,否则我将让你后悔.", ChatType.System2);
                    }
                }
                else
                {
                    Info.killMon[monidx]++;
                    if (Info.killMon[monidx] % 10 == 0)
                    {
                        ReceiveChat(string.Format("你已累计杀死 {0} 只蜈蚣洞怪", Info.killMon[monidx]), ChatType.Hint);
                    }
                }
                return;
            }

            //野猪
            monidx = 1;
            killMax = 400;
            if (mname.Equals("黑野猪") || mname.Equals("红野猪"))
            {
                if (Info.killMon[monidx] >= killMax)
                {
                    if (RandomUtils.Next(5) == 1 && CurrentMap.ValidPoint(Functions.PointMove(CurrentLocation, Direction, 1)))
                    {
                        MonsterInfo info = Envir.GetMonsterInfo("白野猪");
                        if (info == null) return;
                        MonsterObject monster;
                        monster = MonsterObject.GetMonster(info);
                        monster.Direction = Functions.ReverseDirection(Direction);
                        monster.ActionTime = Envir.Time + 1000;
                        monster.Spawn(CurrentMap, Functions.PointMove(CurrentLocation, Direction, 1));
                        Info.killMon[monidx] = 0;
                    }
                    else
                    {
                        ReceiveChat("[白野猪]  你杀了那么多猪，完全不给老猪我面子，别跑，等我来收拾你...", ChatType.System2);
                    }
                }
                else
                {
                    Info.killMon[monidx]++;
                    if (Info.killMon[monidx] % 10 == 0)
                    {
                        ReceiveChat(string.Format("你已累计杀死 {0} 只野猪", Info.killMon[monidx]), ChatType.Hint);
                    }
                }
                return;
            }

            //牛
            monidx = 2;
            killMax = 400;
            if (mname.Equals("牛头魔") || mname.Equals("牛魔战士") || mname.Equals("牛魔斗士") || mname.Equals("牛魔侍卫") || mname.Equals("牛魔侍卫"))
            {
                if (Info.killMon[monidx] >= killMax)
                {
                    if (RandomUtils.Next(5) == 1 && CurrentMap.ValidPoint(Functions.PointMove(CurrentLocation, Direction, 1)))
                    {
                        MonsterInfo info = Envir.GetMonsterInfo("牛魔王");
                        if (info == null) return;
                        MonsterObject monster;
                        monster = MonsterObject.GetMonster(info);
                        monster.Direction = Functions.ReverseDirection(Direction);
                        monster.ActionTime = Envir.Time + 1000;
                        monster.Spawn(CurrentMap, Functions.PointMove(CurrentLocation, Direction, 1));
                        Info.killMon[monidx] = 0;
                    }
                    else
                    {
                        ReceiveChat("[牛魔王]  你是猴子派来的奸细么，打扰我的睡眠，你找死...", ChatType.System2);
                    }
                }
                else
                {
                    Info.killMon[monidx]++;
                    if (Info.killMon[monidx] % 10 == 0)
                    {
                        ReceiveChat(string.Format("你已累计杀死 {0} 只牛魔怪", Info.killMon[monidx]), ChatType.Hint);
                    }
                  
                }
                return;
            }

            //魔龙怪
            monidx = 3;
            killMax = 300;
            if (mname.Equals("魔龙破甲兵") || mname.Equals("魔龙刀兵") || mname.Equals("魔龙射手") || mname.Equals("魔龙巨蛾") || mname.Equals("魔龙力士") || mname.Equals("魔龙战将"))
            {
                if (Info.killMon[monidx] >= killMax)
                {
                    if (RandomUtils.Next(5) == 1 && CurrentMap.ValidPoint(Functions.PointMove(CurrentLocation, Direction, 1)))
                    {
                        MonsterInfo info = Envir.GetMonsterInfo("破凰魔神");
                        if (info == null) return;
                        MonsterObject monster;
                        monster = MonsterObject.GetMonster(info);
                        monster.Direction = Functions.ReverseDirection(Direction);
                        monster.ActionTime = Envir.Time + 1000;
                        monster.Spawn(CurrentMap, Functions.PointMove(CurrentLocation, Direction, 1));
                        Info.killMon[monidx] = 0;
                    }
                    else
                    {
                        ReceiveChat("[破凰魔神]  我们是神龙的后裔,你冒犯了龙族的威严,将受到龙之惩罚...", ChatType.System2);
                    }
                }
                else
                {
                    Info.killMon[monidx]++;
                    if (Info.killMon[monidx] % 10 == 0)
                    {
                        ReceiveChat(string.Format("你已累计杀死 {0} 只魔龙小怪", Info.killMon[monidx]), ChatType.Hint);
                    }
                }
                return;
            }

            //
            if (mon.MaxHP < 100 && !mon.Info.CanTreaty)
            {
                return;
            }
            int midx = mon.Info.Index;
            if (Info.killMon2.ContainsKey(midx))
            {
                Info.killMon2[midx]++;
            }
            else
            {
                Info.killMon2[midx] = 1;
            }
            //层数
            int killlev = Info.killMon2[midx] / 100;
            //每2个提醒一次
            if (Info.killMon2[midx] % 10 == 0)
            {
                ReceiveChat(string.Format("你已累计杀死 {0} 只 {1},叠加层数{2}", Info.killMon2[midx], mname, killlev), ChatType.Hint);
            }

            //黄怪3倍爆率 
            //刷黄怪
            if (Info.killMon2[midx] % 100 == 0)
            {
                //先复制怪物info
                MonsterInfo minfo = mon.Info.Clone();
                foreach (DropInfo drop in minfo.Drops)
                {
                    //每层爆率增加10%
                    drop.Chance = (drop.Chance + drop.Chance * killlev / 10.0f) * 3;
                    if (drop.Gold > 0)
                    {
                        drop.Gold = drop.Gold * 3;
                    }
                }
                //加入宝玉的爆率
                DropInfo drop2 = DropInfo.FromLine("副本_宝玉", String.Format("1/6 G_宝玉"));
                drop2.Chance = minfo.HP * 1.01 / 10000;
                drop2.Chance = drop2.Chance + drop2.Chance * killlev / 10.0f;
                
                minfo.Drops.Add(drop2);

                DropInfo drop3 = DropInfo.FromLine("副本_药水", String.Format("1/1 G_药剂3 5-8"));
                minfo.Drops.Add(drop3);
                //
                DropInfo drop4 = DropInfo.FromLine("副本_装备", String.Format("1/1 G_武器20|G_武器26|G_沃玛首饰|G_盔甲3 1-3"));
                //
                if (minfo.HP > 500)
                {
                    drop4 = DropInfo.FromLine("副本_装备", String.Format("1/1 G_武器30|G_祖玛|G_盔甲3 1-3"));
                }
                minfo.Drops.Add(drop4);
                //加入书页爆率
                DropInfo drop5 = DropInfo.FromLine("副本_书页", String.Format("1/10 G_书页残卷"));

                drop5.Chance = minfo.HP*1.01 / 6000;
                if (drop5.Chance > 0.4)
                {
                    drop5.Chance = 0.4;
                }
                minfo.Drops.Add(drop5);
                //黄怪不爆书页

                minfo.Name = minfo.Name + "[" + "精英" + "]";
                minfo.Level = (ushort)(minfo.Level + 10);
                minfo.HP = (uint)(minfo.HP * 2 + minfo.HP * killlev / 100);
                minfo.MaxAC = (ushort)(minfo.MaxAC * 4 / 3);
                minfo.MaxMAC = (ushort)(minfo.MaxAC * 4 / 3);
                minfo.MaxDC = (ushort)(minfo.MaxDC * 15 / 10);//1.5倍的基础攻击
                minfo.MaxMC = (ushort)(minfo.MaxMC * 15 / 10);//1.5倍的基础攻击
                int AttackSpeed = minfo.AttackSpeed;
                AttackSpeed = AttackSpeed - 200 - (killlev * 100);
                if (AttackSpeed < 500)
                {
                    AttackSpeed = 500;
                }
                if (AttackSpeed > 3000)
                {
                    AttackSpeed = 3000;
                }
                minfo.AttackSpeed = (ushort)AttackSpeed;
                int MoveSpeed = minfo.MoveSpeed;
                MoveSpeed = MoveSpeed - 200 - (killlev * 100);
                if (MoveSpeed < 500)
                {
                    MoveSpeed = 500;
                }
                if (MoveSpeed > 3000)
                {
                    MoveSpeed = 3000;
                }
                minfo.MoveSpeed = (ushort)MoveSpeed;


                MonsterObject monster = MonsterObject.GetMonster(minfo);
                monster.IsCopy = true;
                monster.NameColour = Color.Gold;
                monster.ChangeNameColour = Color.Gold;
                monster.RefreshAll();

                //直接刷在当前死亡怪物的位置
                if (CurrentMap == mon.CurrentMap)
                {
                    monster.SpawnNew(CurrentMap, mon.CurrentLocation);
                }
                else
                {
                    monster.SpawnNew(CurrentMap, CurrentLocation);
                }
            }
            //红怪8倍爆率
            //刷红怪
            if (Info.killMon2[midx] % 300 == 0)
            {
                //先复制怪物info
                MonsterInfo minfo = mon.Info.Clone();
                foreach (DropInfo drop in minfo.Drops)
                {
                    //每层爆率增加10%
                    drop.Chance = (drop.Chance + drop.Chance * killlev / 10.0f) * 8;
                    if (drop.Gold > 0)
                    {
                        drop.Gold = drop.Gold * 8;
                    }
                    if (drop.Gold > 50000)
                    {
                        drop.Gold = 50000;
                    }
                }
                //加入宝玉的爆率
                DropInfo drop2 = DropInfo.FromLine("副本_宝玉", String.Format("1/3 G_宝玉"));
                drop2.Chance = minfo.HP * 1.01 / 5000;
                drop2.Chance = drop2.Chance + drop2.Chance * killlev / 10.0f;

                minfo.Drops.Add(drop2);
                DropInfo drop3 = DropInfo.FromLine("副本_药水", String.Format("1/1 G_药剂3 5-8"));
                minfo.Drops.Add(drop3);
                DropInfo drop4 = DropInfo.FromLine("副本_装备", String.Format("1/1 G_武器20|G_武器26|G_沃玛首饰|G_盔甲3 2-4"));
                //
                if (minfo.HP > 500)
                {
                    drop4 = DropInfo.FromLine("副本_装备", String.Format("1/1 G_武器30|G_祖玛|G_盔甲3 2-4"));
                }
                minfo.Drops.Add(drop4);
                //加入书页爆率
                DropInfo drop5 = DropInfo.FromLine("副本_书页", String.Format("1/10 G_书页残卷"));
                drop5.Chance = minfo.HP * 1.01 / 3000;
                if (drop5.Chance > 0.8)
                {
                    drop5.Chance = 0.8;
                }
                minfo.Drops.Add(drop5);

                //兽魂丹 1/2几率出
                if (minfo.CanTreaty)
                {
                    DropInfo drop6 = DropInfo.FromLine("副本_兽丹", String.Format("1/2 兽魂丹"));
                    drop6.Chance = 0.5+ killlev / 100.0;
                    minfo.Drops.Add(drop6);
                }
                //
                minfo.Name = minfo.Name + "[" + "统领" + "]";
                minfo.Level = 99;
                minfo.HP = (uint)(minfo.HP * 6 + minfo.HP * killlev / 100);
                minfo.MaxAC = (ushort)(minfo.MaxAC * 3 / 2);
                minfo.MaxMAC = (ushort)(minfo.MaxAC * 3 / 2);
                minfo.MaxDC = (ushort)(minfo.MaxDC * 20 / 10);//2倍的基础攻击
                minfo.MaxMC = (ushort)(minfo.MaxMC * 20 / 10);//2倍的基础攻击

                int AttackSpeed = minfo.AttackSpeed;
                AttackSpeed = AttackSpeed - 200 - (killlev * 100);
                if (AttackSpeed < 500)
                {
                    AttackSpeed = 500;
                }
                if (AttackSpeed > 3000)
                {
                    AttackSpeed = 3000;
                }
                minfo.AttackSpeed = (ushort)AttackSpeed;
                int MoveSpeed = minfo.MoveSpeed;
                MoveSpeed = MoveSpeed - 200 - (killlev * 100);
                if (MoveSpeed < 500)
                {
                    MoveSpeed = 500;
                }
                if (MoveSpeed > 3000)
                {
                    MoveSpeed = 3000;
                }
                minfo.MoveSpeed = (ushort)MoveSpeed;


                MonsterObject monster = MonsterObject.GetMonster(minfo);
                monster.IsCopy = true;
                monster.NameColour = Color.Red;
                monster.ChangeNameColour = Color.Red;
                monster.RefreshAll();
                //直接刷在当前死亡怪物的位置
                if (CurrentMap == mon.CurrentMap)
                {
                    monster.SpawnNew(CurrentMap, mon.CurrentLocation);
                }
                else
                {
                    monster.SpawnNew(CurrentMap, CurrentLocation);
                }
            }
        }

        //杀怪轮回处理
        public void SaItem(MonsterObject mon)
        {
            if(Info.SaItem==null|| Info.SaItemType == 0)
            {
                return;
            }
            string mname = mon.Name;
            bool mach = false;//是否匹配杀怪
            switch (Info.SaItemType)
            {
                case 1://魔龙
                    if (mname.StartsWith("魔龙") || mname.StartsWith("火龙") || mname.Equals("破凰魔神") || mname.Equals("破天魔龙") )
                    {
                        mach = true;
                    }
                    break;

                case 2://狐狸
                    if (mname.StartsWith("狐狸") || mname.StartsWith("悲月"))
                    {
                        mach = true;
                    }
                    break;
                case 3://月氏
                    if (mname.StartsWith("月氏") || mname.Equals("鸟人像") || mname.Equals("石魔兽") )
                    {
                        mach = true;
                    }
                    break;
                case 4://洪洞
                    if (mname.StartsWith("赤血") || mname.Equals("异型多脚怪") || mname.Equals("黑暗多脚怪") || mname.Equals("怨恶"))
                    {
                        mach = true;
                    }
                    break;
                case 5://石槽
                    if ("三眼神壶|青花圣壶|灵猫斗士|火焰灵猫|长枪灵猫|铁锤猫卫|黑镐猫卫|双刃猫卫|灵猫法师|灵猫圣兽|壶中天|灵猫将军|".IndexOf(mname)!=-1)
                    {
                        mach = true;
                    }
                    break;
            }
            if (!mach)
            {
                return;
            }
            int change = 10 + Info.SaItem.samsaracount * 12;
            bool hassam = false;
            for (int i = 0; i < Info.Equipment.Length; i++)
            {
                if (Info.Equipment[i] == null || Info.Equipment[i].Info == null || Info.Equipment[i].Info.Type > ItemType.Ring)
                {
                    continue;
                }
                if (Info.SaItem != null && Info.SaItemType == 0)
                {
                    if (Info.SaItem.Info.Index == Info.Equipment[i].Info.Index)
                    {
                        hassam = true;
                        break;
                    }
                }
            }
            if (!hassam)
            {
                change = change * 2;
            }
            //爆出轮回装备,1/3的几率爆点
            if (CanGainItem(Info.SaItem, false) && RandomUtils.NextDouble() <= 1.0/change)
            {
                UserItem saitem = Info.SaItem.Clone();
                Info.SaItem = null;
                if (saitem.samsaratype == (byte)AwakeType.DC)
                {
                    saitem.samsaracount++;
                    saitem.SA_DC++;
                    if (RandomUtils.Next(3) == 1)
                    {
                        saitem.samsaracount++;
                        saitem.SA_DC++;
                    }
                }
                if (saitem.samsaratype == (byte)AwakeType.MC)
                {
                    saitem.samsaracount++;
                    saitem.SA_MC++;
                    if (RandomUtils.Next(3) == 1)
                    {
                        saitem.samsaracount++;
                        saitem.SA_MC++;
                    }
                }
                if (saitem.samsaratype == (byte)AwakeType.SC)
                {
                    saitem.samsaracount++;
                    saitem.SA_SC++;
                    if (RandomUtils.Next(3) == 1)
                    {
                        saitem.samsaracount++;
                        saitem.SA_SC++;
                    }
                }
                if (saitem.samsaratype == (byte)AwakeType.AC)
                {
                    saitem.samsaracount++;
                    saitem.SA_AC++;
                    if (RandomUtils.Next(4) == 1)
                    {
                        saitem.samsaracount++;
                        saitem.SA_AC++;
                    }
                }
                if (saitem.samsaratype == (byte)AwakeType.MAC)
                {
                    saitem.samsaracount++;
                    saitem.SA_MAC++;
                    if (RandomUtils.Next(4) == 1)
                    {
                        saitem.samsaracount++;
                        saitem.SA_MAC++;
                    }
                }
                //直接放背包，背包放不下，则爆出来
                GainItem(saitem);
                foreach (var player in Envir.Players)
                {
                    player.ReceiveChat($"恭喜[{Name}]在轮回路上，找回自己的装备 {saitem.FriendlyName}，在{CurrentMap.getTitle()}", ChatType.System2);
                }
            }
        }

        //获得经验
        public override void WinExp(uint amount, uint targetLevel = 0)
        {
            int expPoint;
            if (Info == null)
            {
                return;
            }
            if (Level < targetLevel + 8 || !Settings.ExpMobLevelDifference)
            {
                expPoint = (int)amount;
            }
            else
            {
                //如果玩家等级大于怪物等级10级，则逐级递减经验，直到大于怪物25级，就基本没经验了.
                expPoint = (int)amount - (int)Math.Round(Math.Max(amount / 10, 1) * ((double)Level - (targetLevel + 8)));
            }

            if (expPoint <= 0) expPoint = 1;
            //增加地图的经验倍率
            if (CurrentMap != null && CurrentMap.Info != null)
            {
                expPoint = (int)(expPoint * CurrentMap.Info.ExpRate); 
            }
            expPoint = (int)(expPoint * Settings.ExpRate);
            
            //party，组队经验倍数
            float[] partyExpRate = { 1.0F, 1.3F, 1.4F, 1.5F, 1.6F, 1.7F, 1.8F, 1.9F, 2F, 2.1F, 2.2F };

            if (GroupMembers != null)
            {
                int sumLevel = 0;
                int nearCount = 0;
                for (int i = 0; i < GroupMembers.Count; i++)
                {
                    PlayerObject player = GroupMembers[i];

                    if (Functions.InRange(player.CurrentLocation, CurrentLocation, Globals.DataRange * 2))
                    {
                        sumLevel += player.Level;
                        nearCount++;
                    }
                }

                if (nearCount > partyExpRate.Length) nearCount = partyExpRate.Length;

                for (int i = 0; i < GroupMembers.Count; i++)
                {
                    PlayerObject player = GroupMembers[i];
                    if (player.CurrentMap == CurrentMap &&
                        Functions.InRange(player.CurrentLocation, CurrentLocation, Globals.DataRange * 2) && !player.Dead)
                    {
                        player.GainExp((uint)((float)expPoint * partyExpRate[nearCount - 1] * (float)player.Level / (float)sumLevel));
                    }
                }
            }
            else
                GainExp((uint)expPoint);
        }

        //增益的经验
        public void GainExp(uint amount,bool EXPBonus=true)
        {
            if (!CanGainExp) return;

            if (amount == 0) return;

            //这里增加疲劳系统判定
            if (Info.Level >= 40)
            {
                //超过4个小时，经验下降30%
                if (Info.onlineTime > Settings.Hour * 4)
                {
                    amount = (uint)(amount* 0.7);
                }
                else if (Info.onlineTime > Settings.Hour * 5) //5个小时，经验下降30%
                {
                    amount = (uint)(amount * 0.6);
                }else if (Info.onlineTime > Settings.Hour * 6)//5个小时，经验下降40%
                {
                    amount = (uint)(amount * 0.5);
                }
            }
            int Leveldis = Envir.MaxLevel - Level;
            if (Leveldis > 5 && Settings.openLevelExpSup && Envir.MaxLevel >= 50)
            {
                amount= amount + (uint)(amount * Leveldis * 5 / 100);
            }
            
            //结婚加成
            if (Info.Married != 0)
            { 
                Buff buff = Buffs.Where(e => e.Type == BuffType.RelationshipEXP).FirstOrDefault();
                if(buff != null)
                {
                    CharacterInfo Lover = Envir.GetCharacterInfo(Info.Married);
                    PlayerObject player = Envir.GetPlayer(Lover.Name);
                    if (player != null && player.CurrentMap == CurrentMap && Functions.InRange(player.CurrentLocation, CurrentLocation, Globals.DataRange) && !player.Dead)
                    {
                        amount += ((amount / 100) * (uint)Settings.LoverEXPBonus);
                    }
                }
            }
            //师徒加成
            if (Info.Mentor != 0 && !Info.isMentor)
            {
                Buff buffMentor = Buffs.Where(e => e.Type == BuffType.Mentee).FirstOrDefault();
                if (buffMentor != null)
                {
                    CharacterInfo Mentor = Envir.GetCharacterInfo(Info.Mentor);
                    PlayerObject player = Envir.GetPlayer(Mentor.Name);
                    if (player != null && player.CurrentMap == CurrentMap && Functions.InRange(player.CurrentLocation, CurrentLocation, Globals.DataRange) && !player.Dead)
                    {
                        amount += ((amount / 100) * (uint)Settings.MentorExpBoost);
                    }
                }
            }
            //这个是经验卷加成
            if (ExpRateOffset > 0 && EXPBonus)
            {
                amount += (uint)(amount * (ExpRateOffset / 100));
            }

            //师徒的存储经验，就是给师傅的经验   
            if (Info.Mentor != 0 && !Info.isMentor)
            {
                MenteeEXP += (amount / 100) * Settings.MenteeExpBank;
            }
                

            Experience += amount;

            Enqueue(new S.GainExperience { Amount = amount });


            for (int i = 0; i < Pets.Count; i++)
            {
                MonsterObject monster = Pets[i];
                if (monster.CurrentMap == CurrentMap && Functions.InRange(monster.CurrentLocation, CurrentLocation, Globals.DataRange * 2) && !monster.Dead)
                    monster.PetExp(amount);
            }


            

            if (MyGuild != null)
                MyGuild.GainExp(amount);

            if (Experience < MaxExperience) return;
            if (Level >= ushort.MaxValue) return;

            //Calculate increased levels
            var experience = Experience;

            while (experience >= MaxExperience)
            {
                Level++;
                experience -= MaxExperience;

                RefreshLevelStats();

                if (Level >= ushort.MaxValue) break;
            }

            Experience = experience;

            LevelUp();

            if (IsGM) return;
            
        }
        //升级
        public void LevelUp()
        {
            RefreshStats();
            SetHP(MaxHP);
            SetMP(MaxMP);
            //
            int Leveldis = Envir.MaxLevel - Level;
            if (Leveldis > 5 && Settings.openLevelExpSup && Envir.MaxLevel>=50)
            {
                ReceiveChat(string.Format("你的等级低于最高等级{0}级，享受{1}%的等级经验补差", Leveldis, Leveldis * 5), ChatType.Hint);
            }

            CallDefaultNPC(DefaultNPCType.LevelUp);

            Enqueue(new S.LevelChanged { Level = Level, Experience = Experience, MaxExperience = MaxExperience });
            Broadcast(new S.ObjectLeveled { ObjectID = ObjectID });

            if (Info.Mentor != 0 && !Info.isMentor)
            {
                CharacterInfo Mentor = Envir.GetCharacterInfo(Info.Mentor);
                if ((Mentor != null) && ((Info.Level + Settings.MentorLevelGap) > Mentor.Level))
                    MentorBreak();
            }

            for (int i = CurrentMap.NPCs.Count - 1; i >= 0; i--)
            {
                if (Functions.InRange(CurrentMap.NPCs[i].CurrentLocation, CurrentLocation, Globals.DataRange))
                    CurrentMap.NPCs[i].CheckVisible(this);
            }
            Report.Levelled(Level);
            if (IsGM) return;
            
        }

        private static int FreeSpace(IList<UserItem> array)
        {
            int count = 0;

            for (int i = 0; i < array.Count; i++)
                if (array[i] == null) count++;

            return count;
        }

        private void AddQuestItem(UserItem item)
        {
            if (item.Info.StackSize > 1) //Stackable
            {
                for (int i = 0; i < Info.QuestInventory.Length; i++)
                {
                    UserItem temp = Info.QuestInventory[i];
                    if (temp == null || item.Info != temp.Info || temp.Count >= temp.Info.StackSize) continue;

                    if (item.Count + temp.Count <= temp.Info.StackSize)
                    {
                        temp.Count += item.Count;
                        return;
                    }
                    item.Count -= temp.Info.StackSize - temp.Count;
                    temp.Count = temp.Info.StackSize;
                }
            }

            for (int i = 0; i < Info.QuestInventory.Length; i++)
            {
                if (Info.QuestInventory[i] != null) continue;
                Info.QuestInventory[i] = item;

                return;
            }
        }

        /// <summary>
        /// 玩家获取到某个物品
        /// </summary>
        /// <param name="item"></param>
        private void AddItem(UserItem item)
        {
            //如果是可堆叠的
            if (item.Info.StackSize > 1) //Stackable
            {
                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    UserItem temp = Info.Inventory[i];
                    if (temp == null || item.Info != temp.Info || temp.Count >= temp.Info.StackSize) continue;

                    if (item.Count + temp.Count <= temp.Info.StackSize)
                    {
                        temp.Count += item.Count;
                        return;
                    }
                    
                    item.Count -= temp.Info.StackSize - temp.Count;
                    temp.Count = temp.Info.StackSize;
                }
            }
            //1-4格自动放药品
            if (item.Info.Type == ItemType.Potion || item.Info.Type == ItemType.Scroll || (item.Info.Type == ItemType.Script && item.Info.Effect == 1))
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Info.Inventory[i] != null) continue;
                    Info.Inventory[i] = item;
                    return;
                }
            }
            else if (item.Info.Type == ItemType.Amulet)//4-6格自动放护身符
            {
                for (int i = 4; i < 6; i++)
                {
                    if (Info.Inventory[i] != null) continue;
                    Info.Inventory[i] = item;
                    return;
                }
            }
            else//其他的放在包裹中
            {
                for (int i = 6; i < Info.Inventory.Length; i++)
                {
                    if (Info.Inventory[i] != null) continue;
                    Info.Inventory[i] = item;
                    return;
                }
            }
            //应该不会走到这里？
            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                if (Info.Inventory[i] != null) continue;
                Info.Inventory[i] = item;
                return;
            }
        }

        //正确的初始化装备，职业，性别
        private bool CorrectStartItem(ItemInfo info)
        {
            switch (Class)
            {
                case MirClass.Warrior:
                    if (!info.RequiredClass.HasFlag(RequiredClass.Warrior)) return false;
                    break;
                case MirClass.Wizard:
                    if (!info.RequiredClass.HasFlag(RequiredClass.Wizard)) return false;
                    break;
                case MirClass.Taoist:
                    if (!info.RequiredClass.HasFlag(RequiredClass.Taoist)) return false;
                    break;
                case MirClass.Assassin:
                    if (!info.RequiredClass.HasFlag(RequiredClass.Assassin)) return false;
                    break;
                case MirClass.Archer:
                    if (!info.RequiredClass.HasFlag(RequiredClass.Archer)) return false;
                    break;
                default:
                    return false;
            }

            switch (Gender)
            {
                case MirGender.Male:
                    if (!info.RequiredGender.HasFlag(RequiredGender.Male)) return false;
                    break;
                case MirGender.Female:
                    if (!info.RequiredGender.HasFlag(RequiredGender.Female)) return false;
                    break;
                default:
                    return false;
            }

            return true;
        }

        //检测物品是否已发送到客户端，如果没有发送过的，发送到客户端
        public void CheckItemInfo(ItemInfo info, bool dontLoop = false)
        {
            if ((dontLoop == false) && (info.ClassBased>0 | info.LevelBased>0)) //send all potential data so client can display it
            {
                for (int i = 0; i < Envir.ItemInfoList.Count; i++)
                {
                    if ((Envir.ItemInfoList[i] != info) && (ItemInfo.IsLevelBased(info,Envir.ItemInfoList[i], ushort.MaxValue) || ItemInfo.IsClassBased(info, Envir.ItemInfoList[i], this.Class)))
                        CheckItemInfo(Envir.ItemInfoList[i], true);
                }
            }

            if (Connection.SentItemInfo.Contains(info)) return;
            Enqueue(new S.NewItemInfo { Info = info });
            Connection.SentItemInfo.Add(info);
        }
        public void CheckItem(UserItem item)
        {
            CheckItemInfo(item.Info);

            for (int i = 0; i < item.Slots.Length; i++)
            {
                if (item.Slots[i] == null) continue;

                CheckItemInfo(item.Slots[i].Info);
            }
        }

        //把任务都发送到客户端，让客户端自行处理?
        public void CheckQuestInfo(QuestInfo info)
        {
            if (Connection.SentQuestInfo.Contains(info)) return;
            Enqueue(new S.NewQuestInfo { Info = info.CreateClientQuestInfo() });
            Connection.SentQuestInfo.Add(info);
        }

        public void CheckRecipeInfo(RecipeInfo info)
        {
            if (Connection.SentRecipeInfo.Contains(info)) return;

            CheckItemInfo(info.Item.Info);

            foreach (var ingredient in info.Ingredients)
            {
                CheckItemInfo(ingredient.Info);
            }

            Enqueue(new S.NewRecipeInfo { Info = info.CreateClientRecipeInfo() });
            Connection.SentRecipeInfo.Add(info);
        }

        //绑定出生地,这个是第一次登陆的时候绑定的
        private void SetBind()
        {
            SafeZoneInfo szi = Envir.StartPoints[RandomUtils.Next(Envir.StartPoints.Count)];

            BindMapIndex = szi.MapIndex;
            BindLocation = szi.Location;
        }
        //开始游戏
        public void StartGame()
        {
            Map temp = Envir.GetMap(CurrentMapIndex);

            if (temp != null && temp.Info.NoReconnect)
            {
                Map temp1 = Envir.GetMapByNameAndInstance(temp.Info.NoReconnectMap);
                if (temp1 != null)
                {
                    temp = temp1;
                    CurrentLocation = GetRandomPoint(40, 0, temp);
                }
            }

            if (temp == null || !temp.ValidPoint(CurrentLocation))
            {
                temp = Envir.GetMap(BindMapIndex);

                if (temp == null || !temp.ValidPoint(BindLocation))
                {
                    SetBind();
                    temp = Envir.GetMap(BindMapIndex);

                    if (temp == null || !temp.ValidPoint(BindLocation))
                    {
                        StartGameFailed();
                        return;
                    }
                }
                CurrentMapIndex = BindMapIndex;
                CurrentLocation = BindLocation;
            }
            temp.AddObject(this);
            CurrentMap = temp;
            Envir.Players.Add(this);

            StartGameSuccess();

            //Call Login NPC
            CallDefaultNPC(DefaultNPCType.Login);

            //Call Daily NPC
            if (Info.NewDay)
            {
                CallDefaultNPC(DefaultNPCType.Daily);
            }
        }
        private void StartGameSuccess()
        {
            Connection.Stage = GameStage.Game;
            for (int i = 0; i < Info.Magics.Count; i++)
            {
                if (Info.Magics[i].CastTime == 0) continue;
                long TimeSpend = Info.Magics[i].GetDelay() - Info.Magics[i].CastTime;
                if (TimeSpend < 0)
                {
                    Info.Magics[i].CastTime = 0; 
                    continue;
                    //avoid having server owners lower the delays and bug it :p
                }
                Info.Magics[i].CastTime = Envir.Time > TimeSpend ? Envir.Time - TimeSpend : 0;
            }
            Enqueue(new S.StartGame { Result = 4, Resolution = Settings.AllowedResolution });
            ReceiveChat("欢迎进入魔幻的传奇大陆，本服地图，怪物，装备超多，玩法多变.欢迎加QQ群670847004一起玩", ChatType.Hint);
            Info.LastLoginDate = Envir.Now;

            //这里重设下玩家的天榜属性，青铜，王者等
            if (Info != null)
            {
                Rank_Character_Info rank = Envir.getRank(2, Info.Index);
                if (rank != null)
                {
                    playerTitle = PlayerTitleUtil.getPlayerTitle(rank.rank, Info.getFb2_score());
                }
            }
            //等级补差
            int Leveldis = Envir.MaxLevel - Level;
            if (Leveldis>5 && Settings.openLevelExpSup && Envir.MaxLevel >= 50)
            {
                ReceiveChat(string.Format("你的等级低于最高等级{0}级，享受{1}%的等级经验补差", Leveldis, Leveldis*5), ChatType.Hint);
            }

            if (Settings.TestServer)
            {
                ReceiveChat("当前服务器为测试服务器.", ChatType.Hint);
                Chat("@GAMEMASTER");
            }

            if (Info.GuildIndex != -1)
            {
                //MyGuild = Envir.GetGuild(Info.GuildIndex);
                if (MyGuild == null)
                {
                    Info.GuildIndex = -1;
                    ReceiveChat("你已经离开公会.", ChatType.System);
                }
                else
                {
                    MyGuildRank = MyGuild.FindRank(Info.Name);
                    if (MyGuildRank == null)
                    {
                        MyGuild = null;
                        Info.GuildIndex = -1;
                        ReceiveChat("你已经离开公会.", ChatType.System);
                    }
                }
            }

            Spawned();

            SetLevelEffects();

            GetItemInfo();
            GetMapInfo();
            GetUserInfo();
            GetQuestInfo();
            GetRecipeInfo();

            GetCompletedQuests();

            GetMail();
            GetFriends();
            GetRelationship();

            foreach(MyMonster myMon in Info.MyMonsters)
            {
                MyMonsterUtils.RefreshMyMonLevelStats(myMon, null);
            }
            GetMyMonsters();

            if ((Info.Mentor != 0) && (Info.MentorDate.AddDays(Settings.MentorLength) < DateTime.Now))
                MentorBreak();
            else
                GetMentor();

            CheckConquest();

            GetGameShop();

            for (int i = 0; i < CurrentQuests.Count; i++)
            {
                if (CurrentQuests[i] != null)
                {
                    CurrentQuests[i].ResyncTasks();
                    SendUpdateQuest(CurrentQuests[i], QuestState.Add);
                }
            }

            Enqueue(new S.BaseStatsInfo { Stats = Settings.ClassBaseStats[(byte)Class] });
            GetObjectsPassive();
            Enqueue(new S.TimeOfDay { Lights = Envir.Lights });
            Enqueue(new S.ChangeAMode { Mode = AMode });
            //if (Class == MirClass.Wizard || Class == MirClass.Taoist)//why could an war, sin, archer not have pets?
                Enqueue(new S.ChangePMode { Mode = PMode });
            Enqueue(new S.SwitchGroup { AllowGroup = AllowGroup });

            Enqueue(new S.DefaultNPC { ObjectID = DefaultNPC.ObjectID });

            Enqueue(new S.GuildBuffList() { GuildBuffs = Settings.Guild_BuffList });
            RequestedGuildBuffInfo = true;

            if (Info.Thrusting) Enqueue(new S.SpellToggle { Spell = Spell.Thrusting, CanUse = true });
            if (Info.HalfMoon) Enqueue(new S.SpellToggle { Spell = Spell.HalfMoon, CanUse = true });
            if (Info.CrossHalfMoon) Enqueue(new S.SpellToggle { Spell = Spell.CrossHalfMoon, CanUse = true });
            if (Info.DoubleSlash) Enqueue(new S.SpellToggle { Spell = Spell.DoubleSlash, CanUse = true });

            for (int i = 0; i < Info.Pets.Count; i++)
            {
                PetInfo info = Info.Pets[i];

                MonsterObject monster = MonsterObject.GetMonster(Envir.GetMonsterInfo(info.MonsterIndex));

                if (monster == null) continue;

                monster.PetLevel = info.Level;
                monster.MaxPetLevel = info.MaxPetLevel;
                monster.PetExperience = info.Experience;

                monster.Master = this;
                Pets.Add(monster);

                monster.RefreshAll();
                if (!monster.Spawn(CurrentMap, Back))
                    monster.Spawn(CurrentMap, CurrentLocation);

                monster.SetHP(info.HP);

                if (!Settings.PetSave && !hasItemSk(ItemSkill.Wizard4))
                {
                    if (info.Time < 1 || (Envir.Time > info.Time + (Settings.PetTimeOut * Settings.Minute))) monster.Die();
                }
            }

            Info.Pets.Clear();

            for (int i = 0; i < Info.Buffs.Count; i++)
            {
                Buff buff = Info.Buffs[i];
                buff.ExpireTime += Envir.Time;
                buff.Paused = false;

                AddBuff(buff);
            }

            Info.Buffs.Clear();

            for (int i = 0; i < Info.Poisons.Count; i++)
            {
                Poison poison = Info.Poisons[i];
                poison.TickTime += Envir.Time;
                //poison.Owner = this;

                ApplyPoison(poison, poison.Owner);
            }

            Info.Poisons.Clear();

            if (MyGuild != null)
            {
                MyGuild.PlayerLogged(this, true);
                if (MyGuild.BuffList.Count > 0)
                    Enqueue(new S.GuildBuffList() { ActiveBuffs = MyGuild.BuffList});
            }

            if (InSafeZone && Info.LastDate > DateTime.MinValue)
            {
                double totalMinutes = (Envir.Now - Info.LastDate).TotalMinutes;

                _restedCounter = (int)(totalMinutes * 60);
            }

            if (Info.Mail.Count > Settings.MailCapacity)
            {
                ReceiveChat("你的邮箱溢出了.", ChatType.System);
            }

            Report.Connected(Connection.IPAddress);

            SMain.Enqueue(string.Format("{0} has connected.", Info.Name));

            


            if (IsGM) return;
            //LastRankUpdate = Envir.Time;
            

        }

        //用户刷新背包
        public void RefreshInventory()
        {
            //前面6格是按键的，不刷新
            List<UserItem> newlist = new List<UserItem>();
            for (int i=6;i< Info.Inventory.Length; i++)
            {
                if (Info.Inventory[i] != null)
                {
                    newlist.Add(Info.Inventory[i]);

                }
            }
     
            //排序
            newlist.Sort((x, y) => {
                int com = x.Info.Type.CompareTo(y.Info.Type);
                if (com == 0)
                {
                    com = x.Info.Grade.CompareTo(y.Info.Grade);
                }
                if (com == 0)
                {
                    com=x.Name.CompareTo(y.Name);
                }
                return com;
                });
    
            //重新设置背包
            for (int i = 6; i < Info.Inventory.Length; i++)
            {
                if(i - 6 < newlist.Count)
                {
                    Info.Inventory[i] = newlist[i-6];
                }
                else
                {
                    Info.Inventory[i] = null;
                }
            }
            //返回最新的用户背包
            S.UserInventory packet = new S.UserInventory
            {
                Inventory = new UserItem[Info.Inventory.Length],
            };
            Info.Inventory.CopyTo(packet.Inventory, 0);
            Enqueue(packet);
            RefreshBagWeight();
        }

        private void StartGameFailed()
        {
            Enqueue(new S.StartGame { Result = 3 });
            CleanUp();
        }

        public void SetLevelEffects()
        {
            LevelEffects = LevelEffects.None;

            if (Info.Flags[990]) LevelEffects |= LevelEffects.Mist;
            if (Info.Flags[991]) LevelEffects |= LevelEffects.RedDragon;
            if (Info.Flags[992]) LevelEffects |= LevelEffects.BlueDragon;
        }
        public void GiveRestedBonus(int count)
        {
            if (count > 0)
            {
                Buff buff = Buffs.FirstOrDefault(e => e.Type == BuffType.Rested);

                long existingTime = 0;
                if (buff != null)
                {
                    existingTime = buff.ExpireTime - Envir.Time;
                }

                long duration = ((Settings.RestedBuffLength * Settings.Minute) * count) + existingTime;
                long maxDuration = (Settings.RestedBuffLength * Settings.Minute) * Settings.RestedMaxBonus;

                if (duration > maxDuration) duration = maxDuration;

                AddBuff(new Buff { Type = BuffType.Rested, Caster = this, ExpireTime = Envir.Time + duration, Values = new int[] { Settings.RestedExpBonus } });
                _restedCounter = 0;
            }
        }

        public void Revive(uint hp, bool effect)
        {
            if (!Dead) return;

            Dead = false;
            SetHP((ushort)hp);

            CurrentMap.RemoveObject(this);
            Broadcast(new S.ObjectRemove { ObjectID = ObjectID });

            CurrentMap = this.CurrentMap;
            CurrentLocation = this.CurrentLocation;

            CurrentMap.AddObject(this);

            Enqueue(new S.MapChanged
            {
                FileName = CurrentMap.Info.FileName,
                Title = CurrentMap.getTitle(),
                MiniMap = CurrentMap.Info.MiniMap,
                BigMap = CurrentMap.Info.BigMap,
                Lights = CurrentMap.Info.Light,
                Location = CurrentLocation,
                Direction = Direction,
                MapDarkLight = CurrentMap.Info.MapDarkLight,
                Music = CurrentMap.Info.Music,
                CanFastRun = CurrentMap.Info.CanFastRun,
                DrawAnimation = CurrentMap.Info.DrawAnimation,
                SafeZones = CurrentMap.Info.SafeZones
            });

            GetObjects();

            Enqueue(new S.Revived());
            Broadcast(new S.ObjectRevived { ObjectID = ObjectID, Effect = effect });

            Fishing = false;
            Enqueue(GetFishInfo());
        }
        public void TownRevive()
        {
            if (!Dead) return;

            Map temp = Envir.GetMap(BindMapIndex);
            Point bindLocation = BindLocation;

            if (Info.PKPoints >= 200)
            {
                temp = Envir.GetMapByNameAndInstance(Settings.PKTownMapName, 1);
                bindLocation = new Point(Settings.PKTownPositionX, Settings.PKTownPositionY);

                if (temp == null)
                {
                    temp = Envir.GetMap(BindMapIndex);
                    bindLocation = BindLocation;
                }
            }

            if (temp == null || !temp.ValidPoint(bindLocation)) return;

            Dead = false;
            SetHP(MaxHP);
            SetMP(MaxMP);
            RefreshStats();

            CurrentMap.RemoveObject(this);
            Broadcast(new S.ObjectRemove { ObjectID = ObjectID });

            CurrentMap = temp;
            CurrentLocation = bindLocation;

            CurrentMap.AddObject(this);

            Enqueue(new S.MapChanged
            {
                FileName = CurrentMap.Info.FileName,
                Title = CurrentMap.getTitle(),
                MiniMap = CurrentMap.Info.MiniMap,
                BigMap = CurrentMap.Info.BigMap,
                Lights = CurrentMap.Info.Light,
                Location = CurrentLocation,
                Direction = Direction,
                MapDarkLight = CurrentMap.Info.MapDarkLight,
                Music = CurrentMap.Info.Music,
                DrawAnimation = CurrentMap.Info.DrawAnimation,
                CanFastRun = CurrentMap.Info.CanFastRun
            
            });

            GetObjects();
            Enqueue(new S.Revived());
            Broadcast(new S.ObjectRevived { ObjectID = ObjectID, Effect = true });


            InSafeZone = true;
            Fishing = false;
            Enqueue(GetFishInfo());
        }

        private void GetItemInfo()
        {
            UserItem item;
            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                item = Info.Inventory[i];
                if (item == null) continue;

                CheckItem(item);
            }

            for (int i = 0; i < Info.Equipment.Length; i++)
            {
                item = Info.Equipment[i];

                if (item == null) continue;

                CheckItem(item);
            }

            for (int i = 0; i < Info.QuestInventory.Length; i++)
            {
                item = Info.QuestInventory[i];

                if (item == null) continue;
                CheckItem(item);
            }
        }
        //返回用户信息
        private void GetUserInfo()
        {
            string guildname = MyGuild != null ? MyGuild.Name : "";
            string guildrank = MyGuild != null ? MyGuildRank.Name : "";
            S.UserInformation packet = new S.UserInformation
            {
                ObjectID = ObjectID,
                RealId = Info.Index,
                Name = PlayerTitleUtil.getPlayerTitleName(playerTitle)+Name,
                GuildName = guildname,
                GuildRank = guildrank,
                NameColour = GetNameColour(this),
                Class = Class,
                Gender = Gender,
                Level = Level,
                Location = CurrentLocation,
                Direction = Direction,
                Hair = Hair,
                HP = HP,
                MP = MP,

                Experience = Experience,
                MaxExperience = MaxExperience,

                LevelEffects = LevelEffects,

                Inventory = new UserItem[Info.Inventory.Length],
                Equipment = new UserItem[Info.Equipment.Length],
                QuestInventory = new UserItem[Info.QuestInventory.Length],
                Gold = Account.Gold,
                Credit = Account.Credit,
                HasExpandedStorage = Account.ExpandedStorageExpiryDate > Envir.Now ? true : false,
                ExpandedStorageExpiryTime = Account.ExpandedStorageExpiryDate
            };

            //Copy this method to prevent modification before sending packet information.
            for (int i = 0; i < Info.Magics.Count; i++)
                packet.Magics.Add(Info.Magics[i].CreateClientMagic());

            Info.Inventory.CopyTo(packet.Inventory, 0);
            Info.Equipment.CopyTo(packet.Equipment, 0);
            Info.QuestInventory.CopyTo(packet.QuestInventory, 0);

            //IntelligentCreature
            for (int i = 0; i < Info.IntelligentCreatures.Count; i++)
                packet.IntelligentCreatures.Add(Info.IntelligentCreatures[i].CreateClientIntelligentCreature());
            packet.SummonedCreatureType = SummonedCreatureType;
            packet.CreatureSummoned = CreatureSummoned;

            Enqueue(packet);
        }
        private void GetMapInfo()
        {
            Enqueue(new S.MapInformation
            {
                FileName = CurrentMap.Info.FileName,
                Title = CurrentMap.getTitle(),
                MiniMap = CurrentMap.Info.MiniMap,
                Lights = CurrentMap.Info.Light,
                BigMap = CurrentMap.Info.BigMap,
                Lightning = CurrentMap.Info.Lightning,
                Fire = CurrentMap.Info.Fire,
                MapDarkLight = CurrentMap.Info.MapDarkLight,
                Music = CurrentMap.Info.Music,
                CanFastRun = CurrentMap.Info.CanFastRun,
                DrawAnimation = CurrentMap.Info.DrawAnimation,
                SafeZones = CurrentMap.Info.SafeZones
            });
        }

        private void GetQuestInfo()
        {
            for (int i = 0; i < Envir.QuestInfoList.Count; i++)
            {
                CheckQuestInfo(Envir.QuestInfoList[i]);
            }
        }
        private void GetRecipeInfo()
        {
            for (int i = 0; i < Envir.RecipeInfoList.Count; i++)
            {
                CheckRecipeInfo(Envir.RecipeInfoList[i]);
            }
        }
        private void GetObjects()
        {
            for (int y = CurrentLocation.Y - Globals.DataRange; y <= CurrentLocation.Y + Globals.DataRange; y++)
            {
                if (y < 0) continue;
                if (y >= CurrentMap.Height) break;

                for (int x = CurrentLocation.X - Globals.DataRange; x <= CurrentLocation.X + Globals.DataRange; x++)
                {
                    if (x < 0) continue;
                    if (x >= CurrentMap.Width) break;
                    if (x < 0 || x >= CurrentMap.Width) continue;

                    //Cell cell = CurrentMap.GetCell(x, y);

                    if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                    for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                    {
                        MapObject ob = CurrentMap.Objects[x, y][i];

                        //if (ob.Race == ObjectType.Player && ob.Observer) continue;

                        ob.Add(this);
                    }
                }
            }
        }
        private void GetObjectsPassive()
        {
            for (int y = CurrentLocation.Y - Globals.DataRange; y <= CurrentLocation.Y + Globals.DataRange; y++)
            {
                if (y < 0) continue;
                if (y >= CurrentMap.Height) break;

                for (int x = CurrentLocation.X - Globals.DataRange; x <= CurrentLocation.X + Globals.DataRange; x++)
                {
                    if (x < 0) continue;
                    if (x >= CurrentMap.Width) break;
                    if (x < 0 || x >= CurrentMap.Width) continue;

                    //Cell cell = CurrentMap.GetCell(x, y);

                    if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                    for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                    {
                        MapObject ob = CurrentMap.Objects[x, y][i];
                        if (ob == this) continue;

                        if (ob.Race == ObjectType.Deco)
                        {
                            var tt = 0;

                            tt++;
                        }
                        //if (ob.Race == ObjectType.Player && ob.Observer) continue;
                        if (ob.Race == ObjectType.Player)
                        {
                            PlayerObject Player = (PlayerObject)ob;
                            Enqueue(Player.GetInfoEx(this));
                        }
                        else if (ob.Race == ObjectType.Spell)
                        {
                            SpellObject obSpell = (SpellObject)ob;
                            if ((obSpell.Spell != Spell.ExplosiveTrap) || (IsFriendlyTarget(obSpell.Caster)))
                                Enqueue(ob.GetInfo());
                        }
                        else if (ob.Race == ObjectType.Merchant)
                        {
                            NPCObject NPC = (NPCObject)ob;

                            NPC.CheckVisible(this);

                            if (NPC.VisibleLog[Info.Index] && NPC.Visible) Enqueue(ob.GetInfo());
                        }
                        else
                        {
                            Enqueue(ob.GetInfo());
                        }

                        if (ob.Race == ObjectType.Player || ob.Race == ObjectType.Monster)
                            ob.SendHealth(this);
                    }
                }
            }
        }

        #region Refresh Stats

        public void RefreshStats()
        {
            if (HasUpdatedBaseStats == false)
            {
                Enqueue(new S.BaseStatsInfo { Stats = Settings.ClassBaseStats[(byte)Class] });
                HasUpdatedBaseStats = true;
            }
            RefreshLevelStats();
            RefreshBagWeight();
            RefreshEquipmentStats();
            RefreshItemSetStats();
            RefreshMirSetStats();
            RefreshSkills();
            RefreshBuffs();
            RefreshStatCaps();
            RefreshMountStats();
            RefreshGuildBuffs();

            //Location Stats ?

            if (HP > MaxHP) SetHP(MaxHP);
            if (MP > MaxMP) SetMP(MaxMP);
            //这里封顶攻速，8点到顶
            AttackSpeed = 1400 - ((ASpeed * 60) + Math.Min(370, (Level * 14)));

           
            //这里提升一点攻速
            if (hasItemSk(ItemSkill.Assassin6))
            {
                if (AttackSpeed < 330) AttackSpeed = 330;
            }
            else
            {
                if (AttackSpeed < 550) AttackSpeed = 550;
            }
            //if (AttackSpeed < 450) AttackSpeed = 450;
            
        }

        //刷新等级属性数据
        private void RefreshLevelStats()
        {
            MaxExperience = Level < Settings.ExperienceList.Count ? Settings.ExperienceList[Level - 1] : 0;
            MaxHP = 0; MaxMP = 0;
            MinAC = 0; MaxAC = 0;
            MinMAC = 0; MaxMAC = 0;
            MinDC = 0; MaxDC = 0;
            MinMC = 0; MaxMC = 0;
            MinSC = 0; MaxSC = 0;

            Accuracy = Settings.ClassBaseStats[(byte)Class].StartAccuracy;
            Agility = Settings.ClassBaseStats[(byte)Class].StartAgility;
            CriticalRate = Settings.ClassBaseStats[(byte)Class].StartCriticalRate;
            CriticalDamage = Settings.ClassBaseStats[(byte)Class].StartCriticalDamage;
            //Other Stats;
            MaxBagWeight = 0;
            MaxWearWeight = 0;
            MaxHandWeight = 0;
            ASpeed = 0;
            Luck = 0;
            LifeOnHit = 0;
            HpDrainRate = 0;
            Reflect = 0;
            MagicResist = 0;
            PoisonResist = 0;
            HealthRecovery = 0;
            SpellRecovery = 0;
            PoisonRecovery = 0;
            Holy = 0;
            Freezing = 0;
            PoisonAttack = 0;

            ExpRateOffset = 0;
            ItemDropRateOffset = 0;
            MineRate = 0;
            GemRate = 0;
            FishRate = 0;
            CraftRate = 0;
            GoldDropRateOffset = 0;

            AttackBonus = 0;

            MaxHP = (ushort)Math.Min(ushort.MaxValue, 14 + (Level / Settings.ClassBaseStats[(byte)Class].HpGain + Settings.ClassBaseStats[(byte)Class].HpGainRate) * Level);

            MinAC = (ushort)Math.Min(ushort.MaxValue, Settings.ClassBaseStats[(byte)Class].MinAc > 0 ? Level / Settings.ClassBaseStats[(byte)Class].MinAc : 0);
            MaxAC = (ushort)Math.Min(ushort.MaxValue, Settings.ClassBaseStats[(byte)Class].MaxAc > 0 ? Level / Settings.ClassBaseStats[(byte)Class].MaxAc : 0);
            MinMAC = (ushort)Math.Min(ushort.MaxValue, Settings.ClassBaseStats[(byte)Class].MinMac > 0 ? Level / Settings.ClassBaseStats[(byte)Class].MinMac : 0);
            MaxMAC = (ushort)Math.Min(ushort.MaxValue, Settings.ClassBaseStats[(byte)Class].MaxMac > 0 ? Level / Settings.ClassBaseStats[(byte)Class].MaxMac : 0);
            MinDC = (ushort)Math.Min(ushort.MaxValue, Settings.ClassBaseStats[(byte)Class].MinDc > 0 ? Level / Settings.ClassBaseStats[(byte)Class].MinDc : 0);
            MaxDC = (ushort)Math.Min(ushort.MaxValue, Settings.ClassBaseStats[(byte)Class].MaxDc > 0 ? Level / Settings.ClassBaseStats[(byte)Class].MaxDc : 0);
            MinMC = (ushort)Math.Min(ushort.MaxValue, Settings.ClassBaseStats[(byte)Class].MinMc > 0 ? Level / Settings.ClassBaseStats[(byte)Class].MinMc : 0);
            MaxMC = (ushort)Math.Min(ushort.MaxValue, Settings.ClassBaseStats[(byte)Class].MaxMc > 0 ? Level / Settings.ClassBaseStats[(byte)Class].MaxMc : 0);
            MinSC = (ushort)Math.Min(ushort.MaxValue, Settings.ClassBaseStats[(byte)Class].MinSc > 0 ? Level / Settings.ClassBaseStats[(byte)Class].MinSc : 0);
            MaxSC = (ushort)Math.Min(ushort.MaxValue, Settings.ClassBaseStats[(byte)Class].MaxSc > 0 ? Level / Settings.ClassBaseStats[(byte)Class].MaxSc : 0);
            CriticalRate = (byte)Math.Min(byte.MaxValue, Settings.ClassBaseStats[(byte)Class].CritialRateGain > 0 ? CriticalRate + (Level / Settings.ClassBaseStats[(byte)Class].CritialRateGain) : CriticalRate);
            CriticalDamage = (byte)Math.Min(byte.MaxValue, Settings.ClassBaseStats[(byte)Class].CriticalDamageGain > 0 ? CriticalDamage + (Level / Settings.ClassBaseStats[(byte)Class].CriticalDamageGain) : CriticalDamage);

            MaxBagWeight = (ushort)Math.Min(ushort.MaxValue, (50 + Level / Settings.ClassBaseStats[(byte)Class].BagWeightGain * Level));
            MaxWearWeight = (ushort)Math.Min(ushort.MaxValue, 15 + Level / Settings.ClassBaseStats[(byte)Class].WearWeightGain * Level);
            MaxHandWeight = (ushort)Math.Min(ushort.MaxValue, 12 + Level / Settings.ClassBaseStats[(byte)Class].HandWeightGain * Level);
            switch (Class)
            {
                case MirClass.Warrior:
                    MaxHP = (ushort)Math.Min(ushort.MaxValue, 14 + (Level / Settings.ClassBaseStats[(byte)Class].HpGain + Settings.ClassBaseStats[(byte)Class].HpGainRate + Level / 20F) * Level);
                    MaxMP = (ushort)Math.Min(ushort.MaxValue, 11 + (Level * 3.5F) + (Level * Settings.ClassBaseStats[(byte)Class].MpGainRate));
                    break;
                case MirClass.Wizard:
                    MaxMP = (ushort)Math.Min(ushort.MaxValue, 13 + ((Level / 5F + 2F) * 2.2F * Level) + (Level * Settings.ClassBaseStats[(byte)Class].MpGainRate));
                    break;
                case MirClass.Taoist:
                    MaxMP = (ushort)Math.Min(ushort.MaxValue, (13 + Level / 8F * 2.2F * Level) + (Level * Settings.ClassBaseStats[(byte)Class].MpGainRate));
                    break;
                case MirClass.Assassin:
                    MaxMP = (ushort)Math.Min(ushort.MaxValue, (11 + Level * 5F) + (Level * Settings.ClassBaseStats[(byte)Class].MpGainRate));
                    break;
                case MirClass.Archer:
                    MaxMP = (ushort)Math.Min(ushort.MaxValue, (11 + Level * 4F) + (Level * Settings.ClassBaseStats[(byte)Class].MpGainRate));
                    break;
            }

            //这里增加玩家称号属性(隐藏属性，不发送到客户端的)
            switch (playerTitle)
            {
                case PlayerTitle.Title1:
                    MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 1);
                    MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 1);
                    MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 1);
                    break;
                case PlayerTitle.Title2:
                    MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 2);
                    MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 2);
                    MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 2);
                    break;
                case PlayerTitle.Title3:
                    MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 3);
                    MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 3);
                    MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 3);
                    Accuracy = (byte)Math.Min(byte.MaxValue, Accuracy + 1);
                    Agility = (byte)Math.Min(byte.MaxValue, Agility + 1);
                    break;
                case PlayerTitle.Title4:
                    MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 4);
                    MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 4);
                    MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 4);
                    Accuracy = (byte)Math.Min(byte.MaxValue, Accuracy + 2);
                    Agility = (byte)Math.Min(byte.MaxValue, Agility + 2);
                    break;
                case PlayerTitle.Title5:
                    MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 5);
                    MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 5);
                    MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 5);
                    Accuracy = (byte)Math.Min(byte.MaxValue, Accuracy + 2);
                    Agility = (byte)Math.Min(byte.MaxValue, Agility + 2);
                    CriticalRate = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, CriticalRate + 1)));
                    break;
                case PlayerTitle.Title6:
                    MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 5);
                    MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 5);
                    MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 5);
                    Accuracy = (byte)Math.Min(byte.MaxValue, Accuracy + 2);
                    Agility = (byte)Math.Min(byte.MaxValue, Agility + 2);
                    CriticalRate = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, CriticalRate + 1)));
                    Luck = (sbyte)Math.Max(sbyte.MinValue, (Math.Min(sbyte.MaxValue, Luck + 1)));
                    break;
                default:
                    break;
            }


        }

        //刷新背包负重
        public void RefreshBagWeight()
        {
            CurrentBagWeight = 0;

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                UserItem item = Info.Inventory[i];
                if (item != null)
                    CurrentBagWeight = (ushort)Math.Min(ushort.MaxValue, CurrentBagWeight + item.Weight);
            }
        }
        //刷新装备的状态
        private void RefreshEquipmentStats()
        {
            short OldLooks_Weapon = Looks_Weapon;
			short OldLooks_WeaponEffect = Looks_WeaponEffect;
			short OldLooks_Armour = Looks_Armour;
            short Old_MountType = MountType;
            byte OldLooks_Wings = Looks_Wings;
            byte OldLight = Light;

            Looks_Armour = 0;
            Looks_Weapon = -1;
			Looks_WeaponEffect = 0;
			Looks_Wings = 0;
            Light = 0;
            CurrentWearWeight = 0;
            CurrentHandWeight = 0;
            MountType = -1;

            HasTeleportRing = false;
            HasProtectionRing = false;
            HasRevivalRing = false;
            HasClearRing = false;
            HasMuscleRing = false;
            HasParalysisRing = false;
            HasProbeNecklace = false;
            SkillNeckBoost = 1;
            NoDuraLoss = false;
            FastRun = false;
            if (ServerConfig.runType == RunType.FastRun)
            {
                //FastRun = true;//可以免助跑
            }
            if (CurrentMap!=null && CurrentMap.Info!=null && CurrentMap.Info.CanFastRun)
            {
                FastRun = true;//可以免助跑
            }
            sk1 = 0;
            sk2 = 0;
            sk3 = 0;
            sk4 = 0;
            skCount = 0;
            //这2个是零时技能
            var skillsToAdd = new List<string>();
            var skillsToRemove = new List<string> { Settings.HealRing, Settings.FireRing, Settings.BlinkSkill  };
            short Macrate = 0, Acrate = 0, HPrate = 0, MPrate = 0;
            ItemSets.Clear();
            MirSet.Clear();

            for (int i = 0; i < Info.Equipment.Length; i++)
            {
                UserItem temp = Info.Equipment[i];
                if (temp == null) continue;
                ItemInfo RealItem = Functions.GetRealItem(temp.Info, Info.Level, Info.Class, Envir.ItemInfoList);
                if (RealItem.Type == ItemType.Weapon || RealItem.Type == ItemType.Torch)
                    CurrentHandWeight = (ushort)Math.Min(byte.MaxValue, CurrentHandWeight + temp.Weight);
                else
                    CurrentWearWeight = (ushort)Math.Min(byte.MaxValue, CurrentWearWeight + temp.Weight);

                if (temp.CurrentDura == 0 && temp.Info.Durability > 0) continue;


                MinAC = (ushort)Math.Min(ushort.MaxValue, MinAC + RealItem.MinAC + temp.Awake.getAC());
                MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + RealItem.MaxAC + temp.AC + temp.Awake.getAC() + temp.SA_AC);
                MinMAC = (ushort)Math.Min(ushort.MaxValue, MinMAC + RealItem.MinMAC + temp.Awake.getMAC());
                MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + RealItem.MaxMAC + temp.MAC + temp.Awake.getMAC() + temp.SA_MAC);

                MinDC = (ushort)Math.Min(ushort.MaxValue, MinDC + RealItem.MinDC + temp.Awake.getDC());
                MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + RealItem.MaxDC + temp.DC + temp.Awake.getDC()+ temp.SA_DC);
                MinMC = (ushort)Math.Min(ushort.MaxValue, MinMC + RealItem.MinMC + temp.Awake.getMC());
                MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + RealItem.MaxMC + temp.MC + temp.Awake.getMC()+ temp.SA_MC);
                MinSC = (ushort)Math.Min(ushort.MaxValue, MinSC + RealItem.MinSC + temp.Awake.getSC());
                MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + RealItem.MaxSC + temp.SC + temp.Awake.getSC()+ temp.SA_SC);

                Accuracy = (byte)Math.Min(byte.MaxValue, Accuracy + RealItem.Accuracy + temp.Accuracy);
                Agility = (byte)Math.Min(byte.MaxValue, Agility + RealItem.Agility + temp.Agility);

                MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + RealItem.HP + temp.HP + temp.Awake.getHPMP());
                MaxMP = (ushort)Math.Min(ushort.MaxValue, MaxMP + RealItem.MP + temp.MP + temp.Awake.getHPMP());

                ASpeed = (sbyte)Math.Max(sbyte.MinValue, (Math.Min(sbyte.MaxValue, ASpeed + temp.AttackSpeed + RealItem.AttackSpeed)));
                Luck = (sbyte)Math.Max(sbyte.MinValue, (Math.Min(sbyte.MaxValue, Luck + temp.Luck + RealItem.Luck)));

                MaxBagWeight = (ushort)Math.Max(ushort.MinValue, (Math.Min(ushort.MaxValue, MaxBagWeight + RealItem.BagWeight)));
                MaxWearWeight = (ushort)Math.Max(ushort.MinValue, (Math.Min(byte.MaxValue, MaxWearWeight + RealItem.WearWeight)));
                MaxHandWeight = (ushort)Math.Max(ushort.MinValue, (Math.Min(byte.MaxValue, MaxHandWeight + RealItem.HandWeight)));
                HPrate = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, HPrate + RealItem.HPrate));
                MPrate = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, MPrate + RealItem.MPrate));
                Acrate = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, Acrate + RealItem.MaxAcRate));
                Macrate = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, Macrate + RealItem.MaxMacRate));
                MagicResist = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, MagicResist + temp.MagicResist + RealItem.MagicResist)));
                PoisonResist = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, PoisonResist + temp.PoisonResist + RealItem.PoisonResist)));
                HealthRecovery = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, HealthRecovery + temp.HealthRecovery + RealItem.HealthRecovery)));
                SpellRecovery = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, SpellRecovery + temp.ManaRecovery + RealItem.SpellRecovery)));
                PoisonRecovery = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, PoisonRecovery + temp.PoisonRecovery + RealItem.PoisonRecovery)));
                CriticalRate = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, CriticalRate + temp.CriticalRate + RealItem.CriticalRate)));
                CriticalDamage = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, CriticalDamage + temp.CriticalDamage + RealItem.CriticalDamage)));
                Holy = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, Holy + RealItem.Holy)));
                Freezing = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, Freezing + temp.Freezing + RealItem.Freezing)));
                PoisonAttack = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, PoisonAttack + temp.PoisonAttack + RealItem.PoisonAttack)));
                Reflect = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, Reflect + RealItem.Reflect)));
                HpDrainRate = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, HpDrainRate + RealItem.HpDrainRate)));

                if (RealItem.Light > Light) Light = RealItem.Light;
                if (RealItem.Unique != SpecialItemMode.None)
                {
                    if (RealItem.Unique.HasFlag(SpecialItemMode.Paralize)) HasParalysisRing = true;
                    if (RealItem.Unique.HasFlag(SpecialItemMode.Teleport)) HasTeleportRing = true;
                    if (RealItem.Unique.HasFlag(SpecialItemMode.Clearring)) HasClearRing = true;
                    if (RealItem.Unique.HasFlag(SpecialItemMode.Protection)) HasProtectionRing = true;
                    if (RealItem.Unique.HasFlag(SpecialItemMode.Revival)) HasRevivalRing = true;
                    if (RealItem.Unique.HasFlag(SpecialItemMode.Muscle)) HasMuscleRing = true;
                    if (RealItem.Unique.HasFlag(SpecialItemMode.Flame))
                    {
                        skillsToAdd.Add(Settings.FireRing);
                        skillsToRemove.Remove(Settings.FireRing);
                    }
                    if (RealItem.Unique.HasFlag(SpecialItemMode.Healing))
                    {
                        skillsToAdd.Add(Settings.HealRing);
                        skillsToRemove.Remove(Settings.HealRing);
                    }
                    if (RealItem.Unique.HasFlag(SpecialItemMode.Probe)) HasProbeNecklace = true;
                    if (RealItem.Unique.HasFlag(SpecialItemMode.Skill)) SkillNeckBoost = 3;
                    if (RealItem.Unique.HasFlag(SpecialItemMode.NoDuraLoss)) NoDuraLoss = true;
                    if (RealItem.Unique.HasFlag(SpecialItemMode.Blink))
                    {
                        skillsToAdd.Add(Settings.BlinkSkill);
                        skillsToRemove.Remove(Settings.BlinkSkill);
                    }
                }
                if (RealItem.CanFastRun)
                {
                    FastRun = true;
                }
                //这里做武器/衣服幻化
                //SMain.Enqueue("SkillNeckBoost" + SkillNeckBoost+ "RealItem.Unique:"+ RealItem.Unique);
                if (RealItem.Type == ItemType.Armour)
                {
                    if (temp.n_Shape > 0)
                    {
                        Looks_Armour = temp.n_Shape;
                    }
                    else
                    {
                        Looks_Armour = RealItem.Shape;
                    }

                    if (temp.n_Effect > 0)
                    {
                        Looks_Wings = temp.n_Effect;
                    }
                    else
                    {
                        Looks_Wings = RealItem.Effect;
                    }
           
                }

				if (RealItem.Type == ItemType.Weapon)
				{
                    if (temp.n_Shape > 0)
                    {
                        Looks_Weapon = temp.n_Shape;
                    }
                    else
                    {
                        Looks_Weapon = RealItem.Shape;
                    }
                    if (temp.n_Effect > 0)
                    {
                        Looks_WeaponEffect = temp.n_Effect;
                    }
                    else
                    {
                        Looks_WeaponEffect = RealItem.Effect;
                    }
				}
              
                if (RealItem.Type == ItemType.Mount)
                {
                    MountType = RealItem.Shape;
                    //RealItem.Effect;
                }
                if (temp.sk1 != 0)
                {
                    sk1 = temp.sk1;
                    skCount = temp.skCount;
                }
                if (temp.sk2 != 0)
                {
                    sk2 = temp.sk2;
                }
                if (temp.sk3 != 0)
                {
                    sk3 = temp.sk3;
                }
                if (temp.sk4 != 0)
                {
                    sk4 = temp.sk4;
                }
                if (temp.hasItemSk(ItemSkill.Assassin6))
                {
                    ASpeed += 2;
                }


                if (RealItem.Set == ItemSet.None) continue;

                ItemSets itemSet = ItemSets.Where(set => set.Set == RealItem.Set && !set.Type.Contains(RealItem.Type) && !set.SetComplete).FirstOrDefault();

                if (itemSet != null)
                {
                    itemSet.Type.Add(RealItem.Type);
                    itemSet.Count++;
                }
                else
                {
                    ItemSets.Add(new ItemSets { Count = 1, Set = RealItem.Set, Type = new List<ItemType> { RealItem.Type } });
                }

                //Mir Set
                if (RealItem.Set == ItemSet.Mir)
                {
                    if (!MirSet.Contains((EquipmentSlot)i))
                        MirSet.Add((EquipmentSlot)i);
                }
            }

            MaxHP = (ushort)Math.Min(ushort.MaxValue, (((double)HPrate / 100) + 1) * MaxHP);
            MaxMP = (ushort)Math.Min(ushort.MaxValue, (((double)MPrate / 100) + 1) * MaxMP);
            MaxAC = (ushort)Math.Min(ushort.MaxValue, (((double)Acrate / 100) + 1) * MaxAC);
            MaxMAC = (ushort)Math.Min(ushort.MaxValue, (((double)Macrate / 100) + 1) * MaxMAC);

            AddTempSkills(skillsToAdd);
            RemoveTempSkills(skillsToRemove);

            if (HasMuscleRing)
            {
                MaxBagWeight = (ushort)(MaxBagWeight * 2);
                MaxWearWeight = Math.Min(ushort.MaxValue, (ushort)(MaxWearWeight * 2));
                MaxHandWeight = Math.Min(ushort.MaxValue, (ushort)(MaxHandWeight * 2));
            }
            if ((OldLooks_Armour != Looks_Armour) || (OldLooks_Weapon != Looks_Weapon) || (OldLooks_WeaponEffect != Looks_WeaponEffect) || (OldLooks_Wings != Looks_Wings) || (OldLight != Light))
            {
                Broadcast(GetUpdateInfo());

                if ((OldLooks_Weapon == 49 || OldLooks_Weapon == 50) && (Looks_Weapon != 49 && Looks_Weapon != 50))
                {
                    Enqueue(GetFishInfo());
                }
            }

            if (Old_MountType != MountType)
            {
                RefreshMount(false);
            }
        }

        private void RefreshItemSetStats()
        {
            foreach (var s in ItemSets)
            {
                if ((s.Set == ItemSet.Smash) && (s.Type.Contains(ItemType.Ring)) && (s.Type.Contains(ItemType.Bracelet)))
                    ASpeed = (sbyte)Math.Min(sbyte.MaxValue, ASpeed + 2);
                if ((s.Set == ItemSet.Purity) && (s.Type.Contains(ItemType.Ring)) && (s.Type.Contains(ItemType.Bracelet)))
                    Holy = Math.Min(byte.MaxValue, (byte)(Holy + 3));
                if ((s.Set == ItemSet.HwanDevil) && (s.Type.Contains(ItemType.Ring)) && (s.Type.Contains(ItemType.Bracelet)))
                {
                    MaxWearWeight = (ushort)Math.Min(ushort.MaxValue, MaxWearWeight + 5);
                    MaxBagWeight = (ushort)Math.Min(ushort.MaxValue, MaxBagWeight + 20);
                }

                if (!s.SetComplete) continue;
                switch (s.Set)
                {
                    case ItemSet.Mundane:
                        MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + 50);
                        break;
                    case ItemSet.NokChi:
                        MaxMP = (ushort)Math.Min(ushort.MaxValue, MaxMP + 50);
                        break;
                    case ItemSet.TaoProtect:
                        MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + 30);
                        MaxMP = (ushort)Math.Min(ushort.MaxValue, MaxMP + 30);
                        break;
                    case ItemSet.RedOrchid:
                        Accuracy = (byte)Math.Min(byte.MaxValue, Accuracy + 2);
                        HpDrainRate = (byte)Math.Min(byte.MaxValue, HpDrainRate + 10);
                        break;
                    case ItemSet.RedFlower:
                        MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + 50);
                        MaxMP = (ushort)Math.Min(ushort.MaxValue, MaxMP - 25);
                        break;
                    case ItemSet.Smash:
                        MinDC = (ushort)Math.Min(ushort.MaxValue, MinDC + 1);
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 3);
                        ASpeed = (sbyte)Math.Min(sbyte.MaxValue, ASpeed + 2);
                        break;
                    case ItemSet.HwanDevil:
                        MinMC = (ushort)Math.Min(ushort.MaxValue, MinMC + 1);
                        MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 2);
                        MaxBagWeight = (ushort)Math.Min(ushort.MaxValue, MaxBagWeight + 20);
                        MaxWearWeight = (ushort)Math.Min(ushort.MaxValue, MaxWearWeight + 5);
                        break;
                    case ItemSet.Purity:
                        MinSC = (ushort)Math.Min(ushort.MaxValue, MinSC + 1);
                        MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + 2);
                        Holy = (byte)Math.Min(ushort.MaxValue, Holy + 3);
                        break;
                    case ItemSet.FiveString:
                        MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + (((double)MaxHP / 100) * 30));
                        MinAC = (ushort)Math.Min(ushort.MaxValue, MinAC + 2);
                        MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + 2);
                        break;
                    case ItemSet.Spirit:
                        MinDC = (ushort)Math.Min(ushort.MaxValue, MinDC + 2);
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 5);
                        ASpeed = (sbyte)Math.Min(sbyte.MaxValue, ASpeed + 2);
                        break;
                    case ItemSet.Bone:
                        MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + 2);
                        MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 1);
                        MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + 1);
                        break;
                    case ItemSet.Bug:
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 1);
                        MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 1);
                        MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + 1);
                        MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + 1);
                        PoisonResist = (byte)Math.Min(byte.MaxValue, PoisonResist + 1);
                        break;
                    case ItemSet.WhiteGold:
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 2);
                        MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + 2);
                        break;
                    case ItemSet.WhiteGoldH:
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 3);
                        MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + 30);
                        ASpeed = (sbyte)Math.Min(int.MaxValue, ASpeed + 2);
                        break;
                    case ItemSet.RedJade:
                        MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 2);
                        MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + 2);
                        break;
                    case ItemSet.RedJadeH:
                        MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 2);
                        MaxMP = (ushort)Math.Min(ushort.MaxValue, MaxMP + 40);
                        Agility = (byte)Math.Min(byte.MaxValue, Agility + 2);
                        break;
                    case ItemSet.Nephrite:
                        MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + 2);
                        MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + 1);
                        MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + 1);
                        break;
                    case ItemSet.NephriteH:
                        MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + 2);
                        MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + 15);
                        MaxMP = (ushort)Math.Min(ushort.MaxValue, MaxMP + 20);
                        Holy = (byte)Math.Min(byte.MaxValue, Holy + 1);
                        Accuracy = (byte)Math.Min(byte.MaxValue, Accuracy + 1);
                        break;
                    case ItemSet.Whisker1:
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 1);
                        MaxBagWeight = (ushort)Math.Min(ushort.MaxValue, MaxBagWeight + 25);
                        break;
                    case ItemSet.Whisker2:
                        MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 1);
                        MaxBagWeight = (ushort)Math.Min(ushort.MaxValue, MaxBagWeight + 17);
                        break;
                    case ItemSet.Whisker3:
                        MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + 1);
                        MaxBagWeight = (ushort)Math.Min(ushort.MaxValue, MaxBagWeight + 17);
                        break;
                    case ItemSet.Whisker4:
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 1);
                        MaxBagWeight = (ushort)Math.Min(ushort.MaxValue, MaxBagWeight + 20);
                        break;
                    case ItemSet.Whisker5:
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 1);
                        MaxBagWeight = (ushort)Math.Min(ushort.MaxValue, MaxBagWeight + 17);
                        break;
                    case ItemSet.Hyeolryong://龙血套（5件套，改为3件套），全属性加2，血量加70
                        MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + 2);
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 2);
                        MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + 2);
                        MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + 50);
                        MaxMP = (ushort)Math.Min(ushort.MaxValue, MaxMP + 20);
                        Holy = (byte)Math.Min(byte.MaxValue, Holy + 1);
                        Accuracy = (byte)Math.Min(byte.MaxValue, Accuracy + 1);
                        break;
                    case ItemSet.Monitor://掠夺者套 5件套，全属性加2
                        MagicResist = (byte)Math.Min(byte.MaxValue, MagicResist + 1);
                        PoisonResist = (byte)Math.Min(byte.MaxValue, PoisonResist + 1);
                        MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + 2);
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 2);
                        MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 2);
                        break;
                    case ItemSet.Oppressive://狂暴套 改为3件套 5件套,暴击加1，敏捷加1，攻速加1
                        MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + 1);
                        MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + 1);
                        Agility = (byte)Math.Min(byte.MaxValue, Agility + 1);
                        ASpeed = (sbyte)Math.Min(sbyte.MaxValue, ASpeed + 1);
                        CriticalRate = (byte)Math.Min(byte.MaxValue, CriticalRate + 1);
                        break;
                    case ItemSet.Paeok://贝玉套-改成3件套了 5件套,暴击加1，准确加1，攻速加1
                        MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + 2);
                        MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + 2);
                        MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + 50);
                        Accuracy = (byte)Math.Min(byte.MaxValue, Accuracy + 1);
                        ASpeed = (sbyte)Math.Min(sbyte.MaxValue, ASpeed + 1);
                        CriticalRate = (byte)Math.Min(byte.MaxValue, CriticalRate + 1);
                        break;
                    case ItemSet.Sulgwan://黑暗套 3件套,暴击加2，准确加1，攻速加1
                        MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + 3);
                        MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + 3);
                        MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + 70);
                        Agility = (byte)Math.Min(byte.MaxValue, Agility + 2);
                        ASpeed = (sbyte)Math.Min(sbyte.MaxValue, ASpeed + 1);
                        CriticalRate = (byte)Math.Min(byte.MaxValue, CriticalRate + 2);
                        break;
                    case ItemSet.GaleWind://狂风套，加2点攻速
                        ASpeed = (sbyte)Math.Min(sbyte.MaxValue, ASpeed + 2);
                        break;
                }
            }
        }

        private void RefreshMirSetStats()
        {
            if (MirSet.Count() == 10)
            {
                MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + 1);
                MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + 1);
                MaxBagWeight = (ushort)Math.Min(ushort.MaxValue, MaxBagWeight + 70);
                Luck = (sbyte)Math.Min(sbyte.MaxValue, Luck + 2);
                ASpeed = (sbyte)Math.Min(int.MaxValue, ASpeed + 2);
                MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + 70);
                MaxMP = (ushort)Math.Min(ushort.MaxValue, MaxMP + 80);
                MagicResist = (byte)Math.Min(byte.MaxValue, MagicResist + 6);
                PoisonResist = (byte)Math.Min(byte.MaxValue, PoisonResist + 6);
            }

            if (MirSet.Contains(EquipmentSlot.RingL) && MirSet.Contains(EquipmentSlot.RingR))
            {
                MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + 1);
                MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + 1);
            }
            if (MirSet.Contains(EquipmentSlot.BraceletL) && MirSet.Contains(EquipmentSlot.BraceletR))
            {
                MinAC = (ushort)Math.Min(ushort.MaxValue, MinAC + 1);
                MinMAC = (ushort)Math.Min(ushort.MaxValue, MinMAC + 1);
            }
            if ((MirSet.Contains(EquipmentSlot.RingL) | MirSet.Contains(EquipmentSlot.RingR)) && (MirSet.Contains(EquipmentSlot.BraceletL) | MirSet.Contains(EquipmentSlot.BraceletR)) && MirSet.Contains(EquipmentSlot.Necklace))
            {
                MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + 1);
                MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + 1);
                MaxBagWeight = (ushort)Math.Min(ushort.MaxValue, MaxBagWeight + 30);
                MaxWearWeight = (ushort)Math.Min(ushort.MaxValue, MaxWearWeight + 17);
            }
            if (MirSet.Contains(EquipmentSlot.RingL) && MirSet.Contains(EquipmentSlot.RingR) && MirSet.Contains(EquipmentSlot.BraceletL) && MirSet.Contains(EquipmentSlot.BraceletR) && MirSet.Contains(EquipmentSlot.Necklace))
            {
                MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + 1);
                MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + 1);
                MaxBagWeight = (ushort)Math.Min(ushort.MaxValue, MaxBagWeight + 20);
                MaxWearWeight = (ushort)Math.Min(ushort.MaxValue, MaxWearWeight + 10);
            }
            if (MirSet.Contains(EquipmentSlot.Armour) && MirSet.Contains(EquipmentSlot.Helmet) && MirSet.Contains(EquipmentSlot.Weapon))
            {
                MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 2);
                MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 1);
                MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + 1);
                Agility = (byte)Math.Min(byte.MaxValue, Agility + 1);
            }
            if (MirSet.Contains(EquipmentSlot.Armour) && MirSet.Contains(EquipmentSlot.Boots) && MirSet.Contains(EquipmentSlot.Belt))
            {
                MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 1);
                MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 1);
                MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + 1);
                MaxHandWeight = (ushort)Math.Min(ushort.MaxValue, MaxHandWeight + 17);
            }
            if (MirSet.Contains(EquipmentSlot.Armour) && MirSet.Contains(EquipmentSlot.Boots) && MirSet.Contains(EquipmentSlot.Belt) && MirSet.Contains(EquipmentSlot.Helmet) && MirSet.Contains(EquipmentSlot.Weapon))
            {
                MinDC = (ushort)Math.Min(ushort.MaxValue, MinDC + 1);
                MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + 1);
                MinMC = (ushort)Math.Min(ushort.MaxValue, MinMC + 1);
                MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + 1);
                MinSC = (ushort)Math.Min(ushort.MaxValue, MinSC + 1);
                MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + 1);
                MaxHandWeight = (ushort)Math.Min(ushort.MaxValue, MaxHandWeight + 17);
            }
        }

        public void RefreshStatCaps()
        {
            MagicResist = Math.Min(Settings.MaxMagicResist, MagicResist);
            PoisonResist = Math.Min(Settings.MaxPoisonResist, PoisonResist);
            CriticalRate = Math.Min(Settings.MaxCriticalRate, CriticalRate);
            CriticalDamage = Math.Min(Settings.MaxCriticalDamage, CriticalDamage);
            Freezing = Math.Min(Settings.MaxFreezing, Freezing);
            PoisonAttack = Math.Min(Settings.MaxPoisonAttack, PoisonAttack);
            HealthRecovery = Math.Min(Settings.MaxHealthRegen, HealthRecovery);
            PoisonRecovery = Math.Min(Settings.MaxPoisonRecovery, PoisonRecovery);
            SpellRecovery = Math.Min(Settings.MaxManaRegen, SpellRecovery);
            HpDrainRate = Math.Min((byte)100, HpDrainRate);
        }

        //刷新坐骑的属性加成，这里要改下，不骑马也照样加属性
        public void RefreshMountStats()
        {
            if ((ServerConfig.NeedRidingMountAtt&&!RidingMount) || !Mount.HasMount) return;

            UserItem[] Slots = Mount.Slots;

            for (int i = 0; i < Slots.Length; i++)
            {
                UserItem temp = Slots[i];
                if (temp == null) continue;

                ItemInfo RealItem = Functions.GetRealItem(temp.Info, Info.Level, Info.Class, Envir.ItemInfoList);

                CurrentWearWeight = (ushort)Math.Min(ushort.MaxValue, CurrentWearWeight + temp.Weight);

                if (temp.CurrentDura == 0 && temp.Info.Durability > 0) continue;

                MinAC = (ushort)Math.Min(ushort.MaxValue, MinAC + RealItem.MinAC);
                MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + RealItem.MaxAC + temp.AC);
                MinMAC = (ushort)Math.Min(ushort.MaxValue, MinMAC + RealItem.MinMAC);
                MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + RealItem.MaxMAC + temp.MAC);

                MinDC = (ushort)Math.Min(ushort.MaxValue, MinDC + RealItem.MinDC);
                MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + RealItem.MaxDC + temp.DC);
                MinMC = (ushort)Math.Min(ushort.MaxValue, MinMC + RealItem.MinMC);
                MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + RealItem.MaxMC + temp.MC);
                MinSC = (ushort)Math.Min(ushort.MaxValue, MinSC + RealItem.MinSC);
                MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + RealItem.MaxSC + temp.SC);

                Accuracy = (byte)Math.Min(byte.MaxValue, Accuracy + RealItem.Accuracy + temp.Accuracy);
                Agility = (byte)Math.Min(byte.MaxValue, Agility + RealItem.Agility + temp.Agility);

                MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + RealItem.HP + temp.HP);
                MaxMP = (ushort)Math.Min(ushort.MaxValue, MaxMP + RealItem.MP + temp.MP);

                ASpeed = (sbyte)Math.Max(sbyte.MinValue, (Math.Min(sbyte.MaxValue, ASpeed + temp.AttackSpeed + RealItem.AttackSpeed)));
                Luck = (sbyte)Math.Max(sbyte.MinValue, (Math.Min(sbyte.MaxValue, Luck + temp.Luck + RealItem.Luck)));
            }
            
        }

        #endregion

        private void AddTempSkills(IEnumerable<string> skillsToAdd)
        {
            foreach (var skill in skillsToAdd)
            {
                Spell spelltype;
                bool hasSkill = false;

                if (!Enum.TryParse(skill, out spelltype)) return;

                for (var i = Info.Magics.Count - 1; i >= 0; i--)
                    if (Info.Magics[i].Spell == spelltype) hasSkill = true;

                if (hasSkill) continue;

                var magic = new UserMagic(spelltype) { IsTempSpell = true };
                Info.Magics.Add(magic);
                Enqueue(magic.GetInfo());
            }
        }
        private void RemoveTempSkills(IEnumerable<string> skillsToRemove)
        {
            foreach (var skill in skillsToRemove)
            {
                Spell spelltype;
                if (!Enum.TryParse(skill, out spelltype)) return;

                for (var i = Info.Magics.Count - 1; i >= 0; i--)
                {
                    if (!Info.Magics[i].IsTempSpell || Info.Magics[i].Spell != spelltype) continue;

                    Info.Magics.RemoveAt(i);
                    Enqueue(new S.RemoveMagic { PlaceId = i });
                }
            }
        }

        private void RefreshSkills()
        {
            for (int i = 0; i < Info.Magics.Count; i++)
            {
                UserMagic magic = Info.Magics[i];
                switch (magic.Spell)
                {
                    case Spell.Fencing:
                        Accuracy = (byte)Math.Min(byte.MaxValue, Accuracy + magic.Level * 3);
                        MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + (magic.Level + 1) * 3);
                        break;
                    case Spell.Slaying:
                        //SMain.Enqueue("攻杀");
                        Accuracy = (byte)Math.Min(byte.MaxValue, Accuracy + magic.Level );
                        break;
                    case Spell.FatalSword:
                        Accuracy = (byte)Math.Min(byte.MaxValue, Accuracy + magic.Level);
                        break;
                    case Spell.SpiritSword:
                        Accuracy = (byte)Math.Min(byte.MaxValue, Accuracy + magic.Level);
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + MaxSC * (magic.Level + 1) * 0.1F);
                        break;
                }
            }
        }
        private void RefreshBuffs()
        {
            short Old_TransformType = TransformType;

            TransformType = -1;

            for (int i = 0; i < Buffs.Count; i++)
            {
                Buff buff = Buffs[i];

                if (buff.Values == null || buff.Values.Length < 1 || buff.Paused) continue;

                switch (buff.Type)
                {
                    case BuffType.Haste:
                    case BuffType.Fury:
                        ASpeed = (sbyte)Math.Max(sbyte.MinValue, (Math.Min(sbyte.MaxValue, ASpeed + buff.Values[0])));
                        break;
                    case BuffType.ImmortalSkin:
                        MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + buff.Values[0]);
                        MaxDC = (ushort)Math.Max(ushort.MinValue, MaxDC - buff.Values[1]);
                        break;
                    case BuffType.SwiftFeet:
                        ActiveSwiftFeet = true;
                        break;
                    case BuffType.LightBody:
                        Agility = (byte)Math.Min(byte.MaxValue, Agility + buff.Values[0]);
                        break;
                    case BuffType.SoulShield:
                        MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + buff.Values[0]);
                        break;
                    case BuffType.BlessedArmour:
                        MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + buff.Values[0]);
                        break;
                    case BuffType.UltimateEnhancer:
                        if (Class == MirClass.Wizard || Class == MirClass.Archer)
                        {
                            MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + buff.Values[0]);
                        }
                        else if (Class == MirClass.Taoist)
                        {
                            MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + buff.Values[0]);
                        }
                        else
                        {
                            MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + buff.Values[0]);
                        }
                        break;
                    case BuffType.ProtectionField:
                        MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + buff.Values[0]);
                        break;
                    case BuffType.Rage:
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + buff.Values[0]);
                        break;
                    case BuffType.CounterAttack:
                        MinAC = (ushort)Math.Min(ushort.MaxValue, MinAC + buff.Values[0]);
                        MinMAC = (ushort)Math.Min(ushort.MaxValue, MinMAC + buff.Values[0]);
                        MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + buff.Values[0]);
                        MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + buff.Values[0]);
                        break;
                    case BuffType.Curse:

                        ushort rMaxDC = (ushort)(((int)MaxDC / 100) * buff.Values[0]);
                        ushort rMaxMC = (ushort)(((int)MaxMC / 100) * buff.Values[0]);
                        ushort rMaxSC = (ushort)(((int)MaxSC / 100) * buff.Values[0]);
                        byte rASpeed = (byte)(((int)ASpeed / 100) * buff.Values[0]);

                        MaxDC = (ushort)Math.Max(ushort.MinValue, MaxDC - rMaxDC);
                        MaxMC = (ushort)Math.Max(ushort.MinValue, MaxMC - rMaxMC);
                        MaxSC = (ushort)Math.Max(ushort.MinValue, MaxSC - rMaxSC);
                        ASpeed = (sbyte)Math.Min(sbyte.MaxValue, (Math.Max(sbyte.MinValue, ASpeed - rASpeed)));
                        break;
                    case BuffType.MagicBooster:
                        MinMC = (ushort)Math.Min(ushort.MaxValue, MinMC + buff.Values[0]);
                        MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + buff.Values[0]);
                        break;

                    case BuffType.General:
                        ExpRateOffset = (float)Math.Min(float.MaxValue, ExpRateOffset + buff.Values[0]);

                        if (buff.Values.Length > 1)
                            ItemDropRateOffset = (float)Math.Min(float.MaxValue, ItemDropRateOffset + buff.Values[1]);
                        if (buff.Values.Length > 2)
                            GoldDropRateOffset = (float)Math.Min(float.MaxValue, GoldDropRateOffset + buff.Values[2]);
                        break;
                    case BuffType.Rested:
                    case BuffType.Exp:
                        ExpRateOffset = (float)Math.Min(float.MaxValue, ExpRateOffset + buff.Values[0]);
                        break;
                    case BuffType.Drop:
                        ItemDropRateOffset = (float)Math.Min(float.MaxValue, ItemDropRateOffset + buff.Values[0]);
                        break;
                    case BuffType.Gold:
                        GoldDropRateOffset = (float)Math.Min(float.MaxValue, GoldDropRateOffset + buff.Values[0]);
                        break;
                    case BuffType.Knapsack:
                    case BuffType.BagWeight:
                        MaxBagWeight = (ushort)Math.Min(ushort.MaxValue, MaxBagWeight + buff.Values[0]);
                        break;
                    case BuffType.Transform:
                        TransformType = (short)buff.Values[0];
                        //SMain.Enqueue("时装"+ buff.Values.Length);
                        if (buff.Values.Length >= 12)
                        {
                            MinAC = (ushort)Math.Min(ushort.MaxValue, MinAC + buff.Values[1]);
                            MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + buff.Values[2]);
                            MinMAC = (ushort)Math.Min(ushort.MaxValue, MinMAC + buff.Values[3]);
                            MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + buff.Values[4]);
                            MinDC = (ushort)Math.Min(ushort.MaxValue, MinDC + buff.Values[5]);
                            MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + buff.Values[6]);
                            MinMC = (ushort)Math.Min(ushort.MaxValue, MinMC + buff.Values[7]);
                            MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + buff.Values[8]);
                            MinSC = (ushort)Math.Min(ushort.MaxValue, MinSC + buff.Values[9]);
                            MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + buff.Values[10]);

                            Luck = (sbyte)Math.Min(ushort.MaxValue, Luck + buff.Values[11]);
                        }
                        break;

                    case BuffType.Impact:
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + buff.Values[0]);
                        break;
                    case BuffType.Magic:
                        MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + buff.Values[0]);
                        break;
                    case BuffType.Taoist:
                        MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + buff.Values[0]);
                        break;
                    case BuffType.Storm:
                        ASpeed = (sbyte)Math.Max(sbyte.MinValue, (Math.Min(sbyte.MaxValue, ASpeed + buff.Values[0])));
                        break;
                    case BuffType.HealthAid:
                        MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + buff.Values[0]);
                        break;
                    case BuffType.ManaAid:
                        MaxMP = (ushort)Math.Min(ushort.MaxValue, MaxMP + buff.Values[0]);
                        break;
                    case BuffType.WonderDrug:
                        switch (buff.Values[0])
                        {
                            case 0:
                                ExpRateOffset = (float)Math.Min(float.MaxValue, ExpRateOffset + buff.Values[1]);
                                break;
                            case 1:
                                ItemDropRateOffset = (float)Math.Min(float.MaxValue, ItemDropRateOffset + buff.Values[1]);
                                break;
                            case 2:
                                MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + buff.Values[1]);
                                break;
                            case 3:
                                MaxMP = (ushort)Math.Min(ushort.MaxValue, MaxMP + buff.Values[1]);
                                break;
                            case 4:
                                MinAC = (ushort)Math.Min(ushort.MaxValue, MinAC + buff.Values[1]);
                                MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + buff.Values[1]);
                                break;
                            case 5:
                                MinMAC = (ushort)Math.Min(ushort.MaxValue, MinMAC + buff.Values[1]);
                                MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + buff.Values[1]);
                                break;
                            case 6:
                                ASpeed = (sbyte)Math.Max(sbyte.MinValue, (Math.Min(sbyte.MaxValue, ASpeed + buff.Values[1])));
                                break;
                        }
                        break;
                }
            }

            if (Old_TransformType != TransformType)
            {
                Broadcast(new S.TransformUpdate { ObjectID = ObjectID, TransformType = TransformType });
            }
        }
        public void RefreshGuildBuffs()
        {
            if (MyGuild == null) return;
            if (MyGuild.BuffList.Count == 0) return;
            for (int i = 0; i < MyGuild.BuffList.Count; i++)
            {
                GuildBuff Buff = MyGuild.BuffList[i];
                if ((Buff.Info == null) || (!Buff.Active)) continue;
                MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + Buff.Info.BuffAc);
                MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + Buff.Info.BuffMac);
                MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + Buff.Info.BuffDc);
                MaxMC = (ushort)Math.Min(ushort.MaxValue, MaxMC + Buff.Info.BuffMc);
                MaxSC = (ushort)Math.Min(ushort.MaxValue, MaxSC + Buff.Info.BuffSc);
                AttackBonus = (byte)Math.Min(byte.MaxValue, AttackBonus + Buff.Info.BuffAttack);
                MaxHP = (ushort)Math.Min(ushort.MaxValue, MaxHP + Buff.Info.BuffMaxHp);
                MaxMP = (ushort)Math.Min(ushort.MaxValue, MaxMP + Buff.Info.BuffMaxMp);
                MineRate = (byte)Math.Min(byte.MaxValue,MineRate + Buff.Info.BuffMineRate);
                GemRate = (byte)Math.Min(byte.MaxValue,GemRate + Buff.Info.BuffGemRate);
                FishRate = (byte)Math.Min(byte.MaxValue,FishRate + Buff.Info.BuffFishRate);
                ExpRateOffset = (float)Math.Min(float.MaxValue, ExpRateOffset + Buff.Info.BuffExpRate);
                CraftRate = (byte)Math.Min(byte.MaxValue, CraftRate + Buff.Info.BuffCraftRate); //needs coding
                SkillNeckBoost = (byte)Math.Min(byte.MaxValue, SkillNeckBoost + Buff.Info.BuffSkillRate);
                HealthRecovery = (byte)Math.Min(byte.MaxValue,HealthRecovery + Buff.Info.BuffHpRegen);
                SpellRecovery = (byte)Math.Min(byte.MaxValue, SpellRecovery + Buff.Info.BuffMPRegen);
                ItemDropRateOffset = (float)Math.Min(float.MaxValue, ItemDropRateOffset + Buff.Info.BuffDropRate);
                GoldDropRateOffset = (float)Math.Min(float.MaxValue, GoldDropRateOffset + Buff.Info.BuffGoldRate);
            }
        }

        public void RefreshNameColour()
        {
            Color colour = Color.White;

            if (ChangeNameColour != Color.White)
            {
                colour = ChangeNameColour;
            }
            if (PKPoints >= 200)
                colour = Color.Red;
            else if (WarZone)
            {
                if (MyGuild == null)
                    colour = Color.Green;
                else
                    colour = Color.Blue;
            }
            else if (Envir.Time < BrownTime)
            {
                //如果是战斗区域，不改变玩家颜色
                if (!CurrentMap.Info.Fight)
                {
                    colour = Color.SaddleBrown;
                }
            }
            else if (PKPoints >= 100)
            {
                colour = Color.Yellow;
            }

            if (colour == NameColour) return;


            NameColour = colour;

            //更新玩家名字颜色
            if (NameColour == Color.White)
            {
                NameColour= PlayerTitleUtil.getPlayerTitleColor(playerTitle);
            }
            if ((MyGuild == null) || (!MyGuild.IsAtWar()))
            {
                Enqueue(new S.ColourChanged { NameColour = NameColour });
            }
            BroadcastColourChange();
        }

        /// <summary>
        /// 获取玩家名字颜色，区分观察者，本体
        /// </summary>
        /// <param name="player">观察者</param>
        /// <returns></returns>
        public Color GetNameColour(PlayerObject player)
        {
            if (player == null) return NameColour;

            //在战争区域内
            //1.自己没行会，则别人看到你是绿色
            //2.自己有行会，别人没行会，则别人看到你是黄色
            //3.自己有行会，同行会的看到是蓝色
            //4.自己有行会，不同行会的看到你是黄色
            if (WarZone)
            {
                if (MyGuild == null)
                    return Color.Green;
                else
                {
                    if (player.MyGuild == null)
                        return Color.Orange;
                    if (player.MyGuild == MyGuild)
                        return Color.Blue;
                    else
                        return Color.Orange;
                }
            }
            //行会战期间
            //有行会的，并且是行会战期间，自己行会的看到是绿色，别人看到的是黄色
            if (MyGuild != null)
            {
                if (MyGuild.IsAtWar())
                {
                    if (player.MyGuild == MyGuild)
                    {
                        return Color.Blue;
                    }
                    else if (MyGuild.IsEnemy(player.MyGuild))
                    {
                        return Color.Orange;
                    }
                }
            }
            //颜色改变了，则直接别人看到你的颜色也变化了
            if (ChangeNameColour != Color.White)
            {
                return ChangeNameColour;
            }
            //
            if (NameColour == Color.White)
            {
                return PlayerTitleUtil.getPlayerTitleColor(playerTitle);
            }
            return NameColour;
        }

        public void BroadcastColourChange()
        {
            if (CurrentMap == null) return;

            for (int i = CurrentMap.Players.Count - 1; i >= 0; i--)
            {
                PlayerObject player = CurrentMap.Players[i];
                if (player == this) continue;

                if (Functions.InRange(CurrentLocation, player.CurrentLocation, Globals.DataRange))
                {
                    //这个名字颜色感觉有问题啊,应该是广播自己的颜色，给别人
                    //player.Enqueue(new S.ObjectColourChanged { ObjectID = ObjectID, NameColour = GetNameColour(player) });
                    player.Enqueue(new S.ObjectColourChanged { ObjectID = ObjectID, NameColour = GetNameColour(player) });
                }
            }
        }

        public override void BroadcastInfo()
        {
            Packet p;
            if (CurrentMap == null) return;

            for (int i = CurrentMap.Players.Count - 1; i >= 0; i--)
            {
                PlayerObject player = CurrentMap.Players[i];
                if (player == this) continue;

                if (Functions.InRange(CurrentLocation, player.CurrentLocation, Globals.DataRange))
                {
                    p = GetInfoEx(player);
                    if (p != null)
                        player.Enqueue(p);
                }
            }
        }

        //玩家发送的信息
        public void Chat(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            SMain.EnqueueChat(string.Format("{0}: {1}", Name, message));

            if (GMLogin)
            {
                if (message == GMPassword)
                {
                    IsGM = true;
                    SMain.Enqueue(string.Format("{0} is now a GM", Name));
                    ReceiveChat("你已经成为一名GM", ChatType.System);
                    //Envir.RemoveRank(Info);//remove gm chars from ranking to avoid causing bugs in rank list
                }
                else
                {
                    SMain.Enqueue(string.Format("{0} attempted a GM login", Name));
                    ReceiveChat("不正确的登录密码", ChatType.System);
                }
                GMLogin = false;
                return;
            }

            if (Info.ChatBanned)
            {
                if (Info.ChatBanExpiryDate > DateTime.Now)
                {
                    ReceiveChat("你现在被禁止聊天.", ChatType.System);
                    return;
                }

                Info.ChatBanned = false;
            }
            else
            {
                if (ChatTime > Envir.Time)
                {
                    if (ChatTick >= 5 & !IsGM)
                    {
                        Info.ChatBanned = true;
                        Info.ChatBanExpiryDate = DateTime.Now.AddMinutes(5);
                        ReceiveChat("你被禁止聊天5分钟.", ChatType.System);
                        return;
                    }

                    ChatTick++;
                }
                else
                    ChatTick = 0;

                ChatTime = Envir.Time + 2000;
            }

            string[] parts;

            message = message.Replace("$pos", Functions.PointToString(CurrentLocation));


            Packet p;
            if (message.StartsWith("/"))
            {
                //Private Message
                message = message.Remove(0, 1);
                parts = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0) return;

                PlayerObject player = Envir.GetPlayer(parts[0]);

                if (player == null)
                {
                    IntelligentCreatureObject creature = GetCreatureByName(parts[0]);
                    if (creature != null)
                    {
                        creature.ReceiveChat(message.Remove(0, parts[0].Length), ChatType.WhisperIn);
                        return;
                    }
                    ReceiveChat(string.Format("找不到 {0}.", parts[0]), ChatType.System);
                    return;
                }

                if (player.Info.Friends.Any(e => e.Info == Info && e.Blocked))
                {
                    ReceiveChat("玩家不接受你的信息.", ChatType.System);
                    return;
                }

                if (Info.Friends.Any(e => e.Info == player.Info && e.Blocked))
                {
                    ReceiveChat("不能在你的黑名单上留言.", ChatType.System);
                    return;
                }

                ReceiveChat(string.Format("/{0}", message), ChatType.WhisperOut);
                player.ReceiveChat(string.Format("{0}=>{1}", Name, message.Remove(0, parts[0].Length)), ChatType.WhisperIn);
            }
            else if (message.StartsWith("!!"))
            {
                if (GroupMembers == null) return;
                //Group
                message = String.Format("{0}:{1}", Name, message.Remove(0, 2));

                p = new S.ObjectChat { ObjectID = ObjectID, Text = message, Type = ChatType.Group };

                for (int i = 0; i < GroupMembers.Count; i++)
                    GroupMembers[i].Enqueue(p);
            }
            else if (message.StartsWith("!~"))
            {
                if (MyGuild == null) return;

                //Guild
                message = message.Remove(0, 2);
                MyGuild.SendMessage(String.Format("{0}: {1}", Name, message));

            }
            else if (message.StartsWith("!#"))
            {
                //Mentor Message
                message = message.Remove(0, 2);
                parts = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0) return;

                if (Info.Mentor == 0) return;

                CharacterInfo Mentor = Envir.GetCharacterInfo(Info.Mentor);
                PlayerObject player = Envir.GetPlayer(Mentor.Name);

                if (player == null)
                {
                    ReceiveChat(string.Format("{0} 不在线.", Mentor.Name), ChatType.System);
                    return;
                }

                ReceiveChat(string.Format("{0}: {1}", Name, message), ChatType.Mentor);
                player.ReceiveChat(string.Format("{0}: {1}", Name, message), ChatType.Mentor);
            }
            else if (message.StartsWith("!"))
            {
                //Shout
                if (Envir.Time < ShoutTime)
                {
                    ReceiveChat(string.Format("你在 {0} 秒内不能喊话.", Math.Ceiling((ShoutTime - Envir.Time) / 1000D)), ChatType.System);
                    return;
                }
                if (Level < 8 && (!HasMapShout && !HasServerShout))
                {
                    ReceiveChat("你需要8级才能喊话.", ChatType.System);
                    return;
                }

                ShoutTime = Envir.Time + 10000;
                message = String.Format("(!){0}:{1}", Name, message.Remove(0, 1));

                if (HasMapShout)
                {
                    p = new S.Chat { Message = message, Type = ChatType.Shout2 };
                    HasMapShout = false;

                    for (int i = 0; i < CurrentMap.Players.Count; i++)
                    {
                        CurrentMap.Players[i].Enqueue(p);
                    }
                    return;
                }
                else if (HasServerShout)
                {
                    p = new S.Chat { Message = message, Type = ChatType.Shout3 };
                    HasServerShout = false;

                    for (int i = 0; i < Envir.Players.Count; i++)
                    {
                        Envir.Players[i].Enqueue(p);
                    }
                    return;
                }
                else
                {
                    p = new S.Chat { Message = message, Type = ChatType.Shout };

                    //Envir.Broadcast(p);
                    for (int i = 0; i < CurrentMap.Players.Count; i++)
                    {
                        if (!Functions.InRange(CurrentLocation, CurrentMap.Players[i].CurrentLocation, Globals.DataRange * 2)) continue;
                        CurrentMap.Players[i].Enqueue(p);
                    }
                }

            }
            else if (message.StartsWith(":)"))
            {
                //Relationship Message
                message = message.Remove(0, 2);
                parts = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0) return;

                if (Info.Married == 0) return;

                CharacterInfo Lover = Envir.GetCharacterInfo(Info.Married);
                PlayerObject player = Envir.GetPlayer(Lover.Name);
            
                if (player == null)
                {
                    ReceiveChat(string.Format("{0} 不在线.", Lover.Name), ChatType.System);
                    return;
                }

                ReceiveChat(string.Format("{0}: {1}", Name, message), ChatType.Relationship);
                player.ReceiveChat(string.Format("{0}: {1}", Name, message), ChatType.Relationship);
            }
            else if (message.StartsWith("@!"))
            {
                if (!IsGM) return;

                message = String.Format("(*){0}:{1}", Name, message.Remove(0, 2));

                p = new S.Chat { Message = message, Type = ChatType.Announcement };

                Envir.Broadcast(p);
            }
            else if (message.StartsWith("@"))
            {
                //这里是特殊的客户端命令处理，这里做个转义处理
                //Command
                message = message.Remove(0, 1);
                parts = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 0) return;

                PlayerObject player;
                CharacterInfo data;
                String hintstring;
                UserItem item;

                string comand = CMDTransform.Transform(parts[0]);
                switch (comand)
                {
                    case "LOGIN":
                        GMLogin = true;
                        ReceiveChat("请输入GM密码", ChatType.Hint);
                        return;
                    case "开启怪物攻城活动":
                        if (!IsGM)
                        {
                            return;
                        }
                        if (CurrentMap.mapSProcess == null)
                        {
                            CurrentMap.mapSProcess = new MonWar();
                        }
                        return;

                    case "封停账号":
                        if (!IsGM)
                        {
                            return;
                        }
                        AccountInfo account = Envir.GetAccount(parts[1]);
                        if (account == null)
                        {
                            ReceiveChat(string.Format("找不到此账户 {0}", parts[1]), ChatType.System);
                            return;
                        }
                        account.Banned = true;
                        account.BanReason = "GM强制封停账号";
                        account.ExpiryDate = DateTime.Now.AddYears(2);
                        if (account.Connection != null)
                        {
                            account.Connection.SendDisconnect(4);
                        }
                        ReceiveChat(string.Format("已封停账号 {0}", parts[1]), ChatType.System);

                        return;

                    case "重载封IP":
                        if (!IsGM)
                        {
                            return;
                        }
                        Settings.LoadStopIp();

                        return;

                    case "恢复角色":
                        if (!IsGM)
                        {
                            return;
                        }
                        CharacterInfo characterhf = Envir.GetCharacterInfo(parts[1]);
                        if (characterhf == null)
                        {
                            ReceiveChat(string.Format("找不到角色信息 {0}", parts[1]), ChatType.System);
                        }
                        characterhf.Deleted = false;
                        ReceiveChat(string.Format("角色已恢复 {0}", parts[1]), ChatType.System);
                        return;


                    case "启用账号":
                        if (!IsGM)
                        {
                            return;
                        }
                        AccountInfo account2 = Envir.GetAccount(parts[1]);
                        if (account2 == null)
                        {
                            ReceiveChat(string.Format("找不到此账户 {0}", parts[1]), ChatType.System);
                            return;
                        }
                        account2.Banned = false;
                        ReceiveChat(string.Format("已启用账号 {0}", parts[1]), ChatType.System);


                        return;

                    case "玩家改名":
                        if (!IsGM)
                        {
                            return;
                        }
                        if (parts.Length < 3)
                        {
                            ReceiveChat(string.Format("参数过少"), ChatType.System);
                            return;
                        }
                        CharacterInfo character = Envir.GetCharacterInfo(parts[1]);
                        if (character == null)
                        {
                            character = Envir.GetCharacterInfo(ulong.Parse(parts[1]));
                        }
                        CharacterInfo character2 = Envir.GetCharacterInfo(parts[2]);
                        if (character == null)
                        {
                            ReceiveChat(string.Format("找不到此玩家 {0}", parts[1]), ChatType.System);
                            return;
                        }
                        if (character2!= null)
                        {
                            ReceiveChat(string.Format("此名称已被占用 {0}", parts[2]), ChatType.System);
                            return;
                        }
                        character.Name = parts[2];

                        ReceiveChat(string.Format("{0} 玩家名称变更为 {1}", parts[1],parts[2]), ChatType.System);


                        return;


                    case "封印能力":
                        if (!IsGM)
                        {
                            return;
                        }
                        int _WarAttackPercent = 0;
                        int.TryParse(parts[1], out _WarAttackPercent);
                        WarAttackPercent = _WarAttackPercent;
                        SMain.Enqueue("封印能力"+ WarAttackPercent);
                        return;
                    case "更改战场模式":
                        if (!IsGM)
                        {
                            return;
                        }
                        int _WGroup = 0;
                        
                        int.TryParse(parts[1], out _WGroup);
                        if (_WGroup == 1)
                        {
                            WGroup = WarGroup.GroupA;
                        }
                        else
                        {
                            WGroup = WarGroup.GroupB;
                        }
                        SMain.Enqueue("更改战场模式" + WGroup);
                        return;
                    case "改变颜色":
                        if (!IsGM)
                        {
                            return;
                        }
                        ChangeNameColour=Color.Blue;
                        SMain.Enqueue("改变颜色");
                        return;
                    case "当前名字颜色":
                        if (!IsGM)
                        {
                            return;
                        }
                        SMain.Enqueue("当前名字颜色"+ ChangeNameColour+ ",NameColour"+ NameColour);
                        return;
                    case "创建测试NPC":
                        if (!IsGM)
                        {
                            return;
                        }
                        NPCInfo _npc = Envir.GetNPCInfoByName("零时补给").Clone();
                        NPCInfo npc = new NPCInfo();
                        npc.Image = _npc.Image;
                        npc.Location = Front;
                        npc.FileName = _npc.FileName;
                        npc.Location = Front;
                        npc.Name = "零时补给";
                        NPCObject npco = new NPCObject(npc, true) { CurrentMap = CurrentMap };
                        uint oid = npco.ObjectID;
                        //npco.LoadInfo(true);
                        CurrentMap.AddObject(npco);
                        npco.Spawned();
                        npco.Call(this, "buy");
                        //Enqueue(new S.NPCUpdate { NPCID = npco.ObjectID });

                        //npco.SpawnNew(CurrentMap, Front);
                        return;
                    case "GIVEITEMSKILL":
                        if (!IsGM)
                        {
                            return;
                        }
                        byte _sk1 = 0;
                        byte.TryParse(parts[1], out _sk1);
                        foreach (UserItem titem  in Info.Equipment)
                        {
                            if(titem==null || titem.Info.Type!= ItemType.Weapon)
                            {
                                continue;
                            }
                            titem.sk1 = (ItemSkill)_sk1;
                            Enqueue(new S.RefreshItem { Item = titem });
                        }
                       
                        return;
                    case "加属性":
                        if (!IsGM)
                        {
                            return;
                        }


                        

                        return;
                    case "KILL":
                        if (!IsGM) return;

                        if (parts.Length >= 2)
                        {
                            player = Envir.GetPlayer(parts[1]);

                            if (player == null)
                            {
                                ReceiveChat(string.Format("找不到 {0}", parts[0]), ChatType.System);
                                return;
                            }
                            if (!player.GMNeverDie) player.Die();
                        }
                        else
                        {
                            if (!CurrentMap.ValidPoint(Front)) return;

                            //Cell cell = CurrentMap.GetCell(Front);

                            if (CurrentMap.Objects[Front.X, Front.Y] == null) return;

                            for (int i = 0; i < CurrentMap.Objects[Front.X, Front.Y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[Front.X, Front.Y][i];

                                switch (ob.Race)
                                {
                                    case ObjectType.Player:
                                    case ObjectType.Monster:
                                        if (ob.Dead) continue;
                                        ob.EXPOwner = this;
                                        ob.ExpireTime = Envir.Time + MonsterObject.EXPOwnerDelay;
                                        ob.Die();
                                        break;
                                    default:
                                        continue;
                                }
                            }
                        }
                        return;

                    case "KILLALL":
                        if (!IsGM) return;
                        int distance = 3;
                        if (parts.Length >= 2)
                        {
                            int.TryParse(parts[1], out distance);
                        }
                        for (int x1 = -distance; x1 < distance; x1++)
                        {
                            if (Front.X + x1 < 0 || Front.X + x1 >= CurrentMap.Width)
                            {
                                continue;
                            }
                            for (int y1 = -distance; y1 < distance; y1++)
                            {
                                if (Front.Y + y1 < 0 || Front.Y + y1 >= CurrentMap.Height)
                                {
                                    continue;
                                }
                                if (!CurrentMap.ValidPoint(Front.X + x1, Front.Y + y1))
                                {
                                    continue;
                                }
                                if (CurrentMap.Objects[Front.X + x1, Front.Y + y1] == null) continue;

                                for (int i = 0; i < CurrentMap.Objects[Front.X + x1, Front.Y + y1].Count; i++)
                                {
                                    MapObject ob = CurrentMap.Objects[Front.X + x1, Front.Y + y1][i];

                                    switch (ob.Race)
                                    {
                                        //case ObjectType.Player:
                                        case ObjectType.Monster:
                                            if (ob.Dead) continue;
                                            ob.EXPOwner = this;
                                            ob.ExpireTime = Envir.Time + MonsterObject.EXPOwnerDelay;
                                            ob.Die();
                                            break;
                                        default:
                                            continue;
                                    }
                                }
                            }
                        }

                        return;
                    case "RESTORE":
                        if (!IsGM || parts.Length < 2) return;

                        data = Envir.GetCharacterInfo(parts[1]);

                        if (data == null)
                        {
                            ReceiveChat(string.Format("玩家 {0} 查找失败", parts[1]), ChatType.System);
                            return;
                        }

                        if (!data.Deleted) return;
                        data.Deleted = false;

                        ReceiveChat(string.Format("玩家 {0} 已被恢复", data.Name), ChatType.System);
                        SMain.Enqueue(string.Format("玩家 {0} 已被恢复 {1}", data.Name, Name));

                        break;

                    case "CHANGEGENDER":
                        if (!IsGM && !Settings.TestServer) return;

                        data = parts.Length < 2 ? Info : Envir.GetCharacterInfo(parts[1]);

                        if (data == null) return;

                        switch (data.Gender)
                        {
                            case MirGender.Male:
                                data.Gender = MirGender.Female;
                                break;
                            case MirGender.Female:
                                data.Gender = MirGender.Male;
                                break;
                        }

                        ReceiveChat(string.Format("玩家 {0} 已改为 {1}", data.Name, data.Gender), ChatType.System);
                        SMain.Enqueue(string.Format("玩家 {0} 已改为 {1} by {2}", data.Name, data.Gender, Name));

                        if (data.Player != null)
                            data.Player.Connection.LogOut();

                        break;

                    case "LEVEL":
                        if ((!IsGM && !Settings.TestServer) || parts.Length < 2) return;

                        ushort level;
                        ushort old;
                        if (parts.Length >= 3)
                        {
                            if (!IsGM) return;

                            if (ushort.TryParse(parts[2], out level))
                            {
                                if (level == 0) return;
                                player = Envir.GetPlayer(parts[1]);
                                if (player == null) return;
                                old = player.Level;
                                player.Level = level;
                                player.LevelUp();

                                ReceiveChat(string.Format("玩家 {0} 等级变更为 {1} -> {2}.", player.Name, old, player.Level), ChatType.System);
                                SMain.Enqueue(string.Format("玩家 {0} 等级变更为 {1} -> {2} by {3}", player.Name, old, player.Level, Name));
                                return;
                            }
                        }
                        else
                        {
                            if (parts[1] == "-1")
                            {
                                parts[1] = ushort.MaxValue.ToString();
                            }

                            if (ushort.TryParse(parts[1], out level))
                            {
                                if (level == 0) return;
                                old = Level;
                                Level = level;
                                LevelUp();

                                ReceiveChat(string.Format("等级 {0} -> {1}.", old, Level), ChatType.System);
                                SMain.Enqueue(string.Format("Player {0} has been Leveled {1} -> {2} by {3}", Name, old, Level, Name));
                                return;
                            }
                        }

                        ReceiveChat("Could not level player", ChatType.System);
                        break;

                    case "MAKE"://GM创建一件装备,物品命令为MARK 
                        if ((!IsGM && !Settings.TestServer) || parts.Length < 2) return;
                     
                        ItemInfo iInfo = ItemInfo.getItem(parts[1]);
                        if (iInfo == null) return;
 
                        uint count = 1;
                        if (parts.Length >= 3 && !uint.TryParse(parts[2], out count))
                            count = 1;

                        var tempCount = count;

                        while (count > 0)
                        {
                            if (iInfo.StackSize >= count)
                            {
                                item = iInfo.CreateDropItem();
                                item.Count = count;

                                if (CanGainItem(item, false)) GainItem(item);

                                return;
                            }
                            item = iInfo.CreateDropItem();
                            item.Count = iInfo.StackSize;
                            count -= iInfo.StackSize;

                            if (!CanGainItem(item, false)) return;
                            GainItem(item);
                        }

                        ReceiveChat(string.Format("{0} x{1} 被创建.", iInfo.Name, tempCount), ChatType.System);
                        SMain.Enqueue(string.Format("Player {0} has attempted to Create {1} x{2}", Name, iInfo.Name, tempCount));
                        break;
                   
                    case "CLEARBUFFS":
                        foreach (var buff in Buffs)
                        {
                            buff.Infinite = false;
                            buff.ExpireTime = 0;
                        }
                        break;

                    case "CLEARBAG":
                        if (!IsGM && !Settings.TestServer) return;
                        player = this;

                        if (parts.Length >= 2)
                            player = Envir.GetPlayer(parts[1]);

                        if (player == null) return;
                        for (int i = 0; i < player.Info.Inventory.Length; i++)
                        {
                            item = player.Info.Inventory[i];
                            if (item == null) continue;

                            player.Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                            player.Info.Inventory[i] = null;
                        }
                        player.RefreshStats();
                        break;

                    case "SUPERMAN":
                        if (!IsGM && !Settings.TestServer) return;

                        GMNeverDie = !GMNeverDie;

                        hintstring = GMNeverDie ? "无敌模式." : "正常模式.";
                        ReceiveChat(hintstring, ChatType.Hint);
                        UpdateGMBuff();
                        break;

                    case "GAMEMASTER":
                        if (!IsGM && !Settings.TestServer) return;

                        GMGameMaster = !GMGameMaster;

                        hintstring = GMGameMaster ? "GameMaster Mode." : "Normal Mode.";
                        ReceiveChat(hintstring, ChatType.Hint);
                        UpdateGMBuff();
                        break;

                    case "OBSERVER":
                        if (!IsGM) return;
                        Observer = !Observer;

                        hintstring = Observer ? "观察模式." : "正常模式.";
                        ReceiveChat(hintstring, ChatType.Hint);
                        UpdateGMBuff();
                        break;
                    case "ALLOWGUILD":
                        EnableGuildInvite = !EnableGuildInvite;
                        hintstring = EnableGuildInvite ? "允许加入公会." : "拒绝加入公会.";
                        ReceiveChat(hintstring, ChatType.Hint);
                        break;
                    case "ALLOWGROUP":
                        SwitchGroup(!AllowGroup);
                        hintstring = AllowGroup ? "允许组队." : "拒绝组队.";
                        ReceiveChat(hintstring, ChatType.Hint);
                        break;
                    case "RECALL":
                        if (!IsGM) return;

                        if (parts.Length < 2) return;
                        player = Envir.GetPlayer(parts[1]);

                        if (player == null) return;

                        player.Teleport(CurrentMap, Front);
                        break;
                    case "ENABLEGROUPRECALL":
                        EnableGroupRecall = !EnableGroupRecall;
                        hintstring = EnableGroupRecall ? "开启组队召唤." : "关闭组队召唤.";
                        ReceiveChat(hintstring, ChatType.Hint);
                        break;

                    case "GROUPRECALL":
                        if (GroupMembers == null || GroupMembers[0] != this || Dead)
                            return;

                        if (CurrentMap.Info.NoRecall)
                        {
                            ReceiveChat("此地图不允许传送", ChatType.System);
                            return;
                        }

                        if (Envir.Time < LastRecallTime)
                        {
                            ReceiveChat(string.Format("你不能传送在 {0} 秒内", (LastRecallTime - Envir.Time) / 1000), ChatType.System);
                            return;
                        }

                        if (ItemSets.Any(set => set.Set == ItemSet.Recall && set.SetComplete))
                        {
                            LastRecallTime = Envir.Time + 180000;
                            for (var i = 1; i < GroupMembers.Count(); i++)
                            {
                                if (GroupMembers[i].EnableGroupRecall)
                                    GroupMembers[i].Teleport(CurrentMap, CurrentLocation);
                                else
                                    GroupMembers[i].ReceiveChat("未经你允许，有人试图传送你",
                                        ChatType.System);
                            }
                        }
                        break;
                    case "RECALLMEMBER":
                        if (GroupMembers == null || GroupMembers[0] != this)
                        {
                            ReceiveChat("你不是组长.", ChatType.System);
                            return;
                        }

                        if (Dead)
                        {
                            ReceiveChat("你不能召唤在你死的时候.", ChatType.System);
                            return;
                        }

                        if (CurrentMap.Info.NoRecall)
                        {
                            ReceiveChat("你不能召唤在此地图", ChatType.System);
                            return;
                        }

                        if (Envir.Time < LastRecallTime)
                        {
                            ReceiveChat(string.Format("你不能召唤在 {0} 秒内", (LastRecallTime - Envir.Time) / 1000), ChatType.System);
                            return;
                        }
                        if (ItemSets.Any(set => set.Set == ItemSet.Recall && set.SetComplete))
                        {
                            if (parts.Length < 2) return;
                            player = Envir.GetPlayer(parts[1]);

                            if (player == null || !IsMember(player) || this == player)
                            {
                                ReceiveChat((string.Format("玩家 {0} 找不到", parts[1])), ChatType.System);
                                return;
                            }
                            if (!player.EnableGroupRecall)
                            {
                                player.ReceiveChat("未经你允许，有人试图传送你",
                                        ChatType.System);
                                ReceiveChat((string.Format("{0} 拒绝传送", player.Name)), ChatType.System);
                                return;
                            }
                            LastRecallTime = Envir.Time + 60000;

                            if (!player.Teleport(CurrentMap, Front))
                                player.Teleport(CurrentMap, CurrentLocation);
                        }
                        else
                        {
                            ReceiveChat("没有权限.", ChatType.System);
                            return;
                        }
                        break;

                    case "RECALLLOVER":
                        if (Info.Married == 0)
                        {
                            ReceiveChat("你还没有结婚.", ChatType.System);
                            return;
                        }

                        if (Dead)
                        {
                            ReceiveChat("你不能传送，在你死的时候.", ChatType.System);
                            return;
                        }

                        if (CurrentMap.Info.NoRecall)
                        {
                            ReceiveChat("此地图不允许传送", ChatType.System);
                            return;
                        }
                        bool _temp = true;
                        if (_temp)
                        {
                            ReceiveChat("已取消夫妻传送命令，请使用夫妻召唤卷轴进行召唤.", ChatType.System);
                            return;
                        }

                        if (Info.Equipment[(int)EquipmentSlot.RingL] == null)
                        {
                            ReceiveChat("你需要戴上结婚戒指来传送.", ChatType.System);
                            return;
                        }
                        

                        //这里是什么哦 
                        if (Info.Equipment[(int)EquipmentSlot.RingL].WeddingRing == (long)Info.Married)
                        {
                            CharacterInfo Lover = Envir.GetCharacterInfo(Info.Married);

                            if (Lover == null) return;

                            player = Envir.GetPlayer(Lover.Name);

                            if (player == null)
                            {
                                ReceiveChat((string.Format("{0} 不在线.", Lover.Name)), ChatType.System);
                                return;
                            }

                            if (player.Dead)
                            {
                                ReceiveChat("你不能传送，对方已经死亡.", ChatType.System);
                                return;
                            }

                            if (player.Info.Equipment[(int)EquipmentSlot.RingL] == null)
                            {
                                player.ReceiveChat((string.Format("你需要戴上结婚戒指来传送.", Lover.Name)), ChatType.System);
                                ReceiveChat((string.Format("{0} 没有戴结婚戒指.", Lover.Name)), ChatType.System);
                                return;
                            }

                            if (player.Info.Equipment[(int)EquipmentSlot.RingL].WeddingRing != (long)player.Info.Married)
                            {
                                player.ReceiveChat((string.Format("你需要戴上结婚戒指来传送.", Lover.Name)), ChatType.System);
                                ReceiveChat((string.Format("{0} 没有戴结婚戒指.", Lover.Name)), ChatType.System);
                                return;
                            }

                            if (!player.AllowLoverRecall)
                            {
                                player.ReceiveChat("未经你允许，有人试图传送你",
                                        ChatType.System);
                                ReceiveChat((string.Format("{0} 拒绝传送.", player.Name)), ChatType.System);
                                return;
                            }

                            if ((Envir.Time < LastRecallTime) && (Envir.Time < player.LastRecallTime))
                            {
                                ReceiveChat(string.Format("你不能传送在 {0} 秒内", (LastRecallTime - Envir.Time) / 1000), ChatType.System);
                                return;
                            }

                            LastRecallTime = Envir.Time + 60000;
                            player.LastRecallTime = Envir.Time + 60000;

                            if (!player.Teleport(CurrentMap, Front))
                                player.Teleport(CurrentMap, CurrentLocation);
                        }
                        else
                        {
                            ReceiveChat("没有戴结婚戒指", ChatType.System);
                            return;
                        }
                        break;
                    case "TIME":
                        ReceiveChat(string.Format("系统时间 : {0}", DateTime.Now.ToString("hh:mm tt")), ChatType.System);
                        break;

                    case "ROLL":
                        int diceNum = RandomUtils.Next(5) + 1;

                        if (GroupMembers == null) { return; }

                        for (int i = 0; i < GroupMembers.Count; i++)
                        {
                            PlayerObject playerSend = GroupMembers[i];
                            playerSend.ReceiveChat(string.Format("{0} has rolled a {1}", Name, diceNum), ChatType.Group);
                        }
                        break;

                    case "MAP":
                        var mapName = CurrentMap.Info.Mcode;
                        var mapTitle = CurrentMap.getTitle();
                        ReceiveChat((string.Format("你当前在 {0}. 地图 ID: {1}", mapTitle, mapName)), ChatType.System);
                        break;

                    case "SAVEPLAYER":
                        if (!IsGM) return;

                        if (parts.Length < 2) return;

                        

                        break;

                    case "LOADPLAYER":
                        if (!IsGM) return;

                        if (parts.Length < 2) return;

                        
                        
                        
                      
                    break;

                    case "MOVE":
                        if (!IsGM && !HasTeleportRing && !Settings.TestServer) return;
                        if (!IsGM && CurrentMap.Info.NoPosition)
                        {
                            ReceiveChat(("你不能在这张地图上定位移动"), ChatType.System);
                            return;
                        }
                        if (Envir.Time < LastTeleportTime)
                        {
                            ReceiveChat(string.Format("你不能传送在 {0} 秒内", (LastTeleportTime - Envir.Time) / 1000), ChatType.System);
                            return;
                        }

                        int x, y;

                        if (parts.Length <= 2 || !int.TryParse(parts[1], out x) || !int.TryParse(parts[2], out y))
                        {
                            if (!IsGM)
                                LastTeleportTime = Envir.Time + 10000;
                            TeleportRandom(200, 0);
                            return;
                        }
                        if (!IsGM)
                            LastTeleportTime = Envir.Time + 10000;
                        Teleport(CurrentMap, new Point(x, y));
                        break;

                    case "MAPMOVE":
                        if ((!IsGM && !Settings.TestServer) || parts.Length < 2) return;
                        var instanceID = 1; x = 0; y = 0;

                        if (parts.Length == 3 || parts.Length == 5)
                            int.TryParse(parts[2], out instanceID);

                        if (instanceID < 1) instanceID = 1;

                        var map = SMain.Envir.GetMapByNameAndInstance(parts[1], instanceID);
                        if (map == null)
                        {
                            ReceiveChat((string.Format("地图 {0}:[{1}] 找不到", parts[1], instanceID)), ChatType.System);
                            return;
                        }

                        if (parts.Length == 4 || parts.Length == 5)
                        {
                            int.TryParse(parts[parts.Length - 2], out x);
                            int.TryParse(parts[parts.Length - 1], out y);
                        }

                        switch (parts.Length)
                        {
                            case 2:
                                ReceiveChat(TeleportRandom(200, 0, map) ? (string.Format("Moved to Map {0}", map.Info.Mcode)) :
                                    (string.Format("Failed movement to Map {0}", map.Info.Mcode)), ChatType.System);
                                break;
                            case 3:
                                ReceiveChat(TeleportRandom(200, 0, map) ? (string.Format("Moved to Map {0}:[{1}]", map.Info.Mcode, instanceID)) :
                                    (string.Format("Failed movement to Map {0}:[{1}]", map.Info.Mcode, instanceID)), ChatType.System);
                                break;
                            case 4:
                                ReceiveChat(Teleport(map, new Point(x, y)) ? (string.Format("Moved to Map {0} at {1}:{2}", map.Info.Mcode, x, y)) :
                                    (string.Format("Failed movement to Map {0} at {1}:{2}", map.Info.Mcode, x, y)), ChatType.System);
                                break;
                            case 5:
                                ReceiveChat(Teleport(map, new Point(x, y)) ? (string.Format("Moved to Map {0}:[{1}] at {2}:{3}", map.Info.Mcode, instanceID, x, y)) :
                                    (string.Format("Failed movement to Map {0}:[{1}] at {2}:{3}", map.Info.Mcode, instanceID, x, y)), ChatType.System);
                                break;
                        }
                        break;

                    case "GOTO":
                        if (!IsGM) return;

                        if (parts.Length < 2) return;
                        player = Envir.GetPlayer(parts[1]);

                        if (player == null) return;

                        Teleport(player.CurrentMap, player.CurrentLocation);
                        break;

                    case "MOB"://召唤怪物
                        if (!IsGM && !Settings.TestServer) return;
                        if (parts.Length < 2)
                        {
                            ReceiveChat("没有足够的参数产生怪物", ChatType.System);
                            return;
                        }

                        MonsterInfo mInfo = Envir.GetMonsterInfo(parts[1]);
                        if (mInfo == null)
                        {
                            ReceiveChat((string.Format("怪物 {0} 不存在", parts[1])), ChatType.System);
                            return;
                        }

                        count = 1;
                        if (parts.Length >= 3 && IsGM)
                            if (!uint.TryParse(parts[2], out count)) count = 1;

                        for (int i = 0; i < count; i++)
                        {
                            MonsterObject monster = MonsterObject.GetMonster(mInfo);
                            if (monster == null) return;
                            monster.Spawn(CurrentMap, Front);
                        }

                        ReceiveChat((string.Format("怪物 {0} x{1} 已经重生.", mInfo.Name, count)), ChatType.System);
                        break;

                    case "RECALLMOB"://召唤怪物宝宝
                        if ((!IsGM && !Settings.TestServer) || parts.Length < 2) return;

                        MonsterInfo mInfo2 = Envir.GetMonsterInfo(parts[1]);
                        if (mInfo2 == null) return;

                        count = 1;
                        byte petlevel = 0;

                        if (parts.Length > 2)
                            if (!uint.TryParse(parts[2], out count) || count > 50) count = 1;

                        if (parts.Length > 3)
                            if (!byte.TryParse(parts[3], out petlevel) || petlevel > 7) petlevel = 0;

                        if (!IsGM && Pets.Count > 4) return;

                        for (int i = 0; i < count; i++)
                        {
                            MonsterObject monster = MonsterObject.GetMonster(mInfo2);
                            if (monster == null) return;
                            monster.PetLevel = petlevel;
                            monster.Master = this;
                            monster.MaxPetLevel = 7;
                            monster.Direction = Direction;
                            monster.ActionTime = Envir.Time + 1000;
                            monster.Spawn(CurrentMap, Front);
                            Pets.Add(monster);
                        }

                        ReceiveChat((string.Format("Pet {0} x{1} has been recalled.", mInfo2.Name, count)), ChatType.System);
                        break;

                    case "RELOADDROPS"://重载爆率
                        if (!IsGM) return;
                        //重新加载所有怪物数据
                        List<MonsterInfo>  listn = MonsterInfo.loadAll();
                        Dictionary<int, MonsterInfo> dicmon = new Dictionary<int, MonsterInfo>();
                        foreach (var t in listn)
                        {
                            dicmon.Add(t.Index, t);
                        }

                        foreach (var t in Envir.MonsterInfoList)
                        {
                            if (dicmon.ContainsKey(t.Index)){
                                t.dropText = dicmon[t.Index].dropText;
                                t.LoadDrops();
                                t.LoadCommonDrops();
                            }
                        }
     
                        ReceiveChat("爆率已重载.", ChatType.Hint);
                        break;

                    case "RELOADNPCS"://重载NPC
                        if (!IsGM) return;

                        for (int i = 0; i < CurrentMap.NPCs.Count; i++)
                        {
                            CurrentMap.NPCs[i].LoadInfo(true);
                        }

                        DefaultNPC.LoadInfo(true);

                        ReceiveChat("NPC已重载.", ChatType.Hint);
                        break;

                    case "GIVEGOLD":
                        if ((!IsGM && !Settings.TestServer) || parts.Length < 2) return;

                        player = this;

                        if (parts.Length > 2)
                        {
                            if (!IsGM) return;

                            if (!uint.TryParse(parts[2], out count)) return;
                            player = Envir.GetPlayer(parts[1]);

                            if (player == null)
                            {
                                ReceiveChat(string.Format("Player {0} was not found.", parts[1]), ChatType.System);
                                return;
                            }
                        }

                        else if (!uint.TryParse(parts[1], out count)) return;

                        if (count + player.Account.Gold >= uint.MaxValue)
                            count = uint.MaxValue - player.Account.Gold;

                        player.GainGold(count);
                        SMain.Enqueue(string.Format("Player {0} has been given {1} gold", player.Name, count));
                        break;

                    case "GIVEPEARLS":
                        if ((!IsGM && !Settings.TestServer) || parts.Length < 2) return;

                        player = this;

                        if (parts.Length > 2)
                        {
                            if (!IsGM) return;

                            if (!uint.TryParse(parts[2], out count)) return;
                            player = Envir.GetPlayer(parts[1]);

                            if (player == null)
                            {
                                ReceiveChat(string.Format("Player {0} was not found.", parts[1]), ChatType.System);
                                return;
                            }
                        }

                        else if (!uint.TryParse(parts[1], out count)) return;

                        if (count + player.Info.PearlCount >= int.MaxValue)
                            count = (uint)(int.MaxValue - player.Info.PearlCount);

                        player.IntelligentCreatureGainPearls((int)count);
                        if (count > 1)
                            SMain.Enqueue(string.Format("Player {0} has been given {1} pearls", player.Name, count));
                        else
                            SMain.Enqueue(string.Format("Player {0} has been given {1} pearl", player.Name, count));
                        break;
                    case "GIVECREDIT":
                        if ((!IsGM && !Settings.TestServer) || parts.Length < 2) return;

                        player = this;

                        if (parts.Length > 2)
                        {
                            if (!IsGM) return;

                            if (!uint.TryParse(parts[2], out count)) return;
                            player = Envir.GetPlayer(parts[1]);

                            if (player == null)
                            {
                                ReceiveChat(string.Format("Player {0} was not found.", parts[1]), ChatType.System);
                                return;
                            }
                        }

                        else if (!uint.TryParse(parts[1], out count)) return;

                        if (count + player.Account.Credit >= uint.MaxValue)
                            count = uint.MaxValue - player.Account.Credit;

                        player.GainCredit(count);
                        SMain.Enqueue(string.Format("Player {0} has been given {1} credit", player.Name, count));
                        break;
                    case "GIVESKILL":
                        if ((!IsGM && !Settings.TestServer) || parts.Length < 3) return;
                        //@GIVESKILL 162 1
                        byte spellLevel = 0;

                        player = this;
                        Spell skill;

                        if (!Enum.TryParse(parts.Length > 3 ? parts[2] : parts[1], true, out skill)) return;

                        if (skill == Spell.None) return;

                        spellLevel = byte.TryParse(parts.Length > 3 ? parts[3] : parts[2], out spellLevel) ? Math.Min((byte)3, spellLevel) : (byte)0;

                        if (parts.Length > 3)
                        {
                            if (!IsGM) return;

                            player = Envir.GetPlayer(parts[1]);

                            if (player == null)
                            {
                                ReceiveChat(string.Format("Player {0} was not found.", parts[1]), ChatType.System);
                                return;
                            }
                        }

                        var magic = new UserMagic(skill) { Level = spellLevel };

                        if (player.Info.Magics.Any(e => e.Spell == skill))
                        {
                            player.Info.Magics.FirstOrDefault(e => e.Spell == skill).Level = spellLevel;
                            player.ReceiveChat(string.Format("Spell {0} changed to level {1}", skill.ToString(), spellLevel), ChatType.Hint);
                            return;
                        }
                        else
                        {
                            player.ReceiveChat(string.Format("You have learned {0} at level {1}", skill.ToString(), spellLevel), ChatType.Hint);

                            if (player != this)
                            {
                                ReceiveChat(string.Format("{0} has learned {1} at level {2}", player.Name, skill.ToString(), spellLevel), ChatType.Hint);
                            }

                            player.Info.Magics.Add(magic);
                        }

                        player.Enqueue(magic.GetInfo());
                        player.RefreshStats();
                        break;

                    case "FIND":
                        if (!IsGM && !HasProbeNecklace) return;

                        if (Envir.Time < LastProbeTime)
                        {
                            ReceiveChat(string.Format("你不能查找在 {0} 秒内", (LastProbeTime - Envir.Time) / 1000), ChatType.System);
                            return;
                        }

                        if (parts.Length < 2) return;
                        player = Envir.GetPlayer(parts[1]);

                        if (player == null)
                        {
                            ReceiveChat(parts[1] + " 不在线", ChatType.System);
                            return;
                        }
                        if (player.CurrentMap == null) return;
                        if (!IsGM)
                            LastProbeTime = Envir.Time + 180000;
                        ReceiveChat((string.Format("{0} 位于 {1} ({2},{3})", player.Name, player.CurrentMap.getTitle(), player.CurrentLocation.X, player.CurrentLocation.Y)), ChatType.System);
                        break;

                    case "LEAVEGUILD":
                        if (MyGuild == null) return;
                        if (MyGuildRank == null) return;
                        if(MyGuild.IsAtWar())
                        {
                            ReceiveChat("战时不能离开公会.", ChatType.System);
                            return;
                        }

                        MyGuild.DeleteMember(this, Name);
                        break;

                    case "CREATEGUILD":

                        if ((!IsGM && !Settings.TestServer) || parts.Length < 2) return;

                        player = parts.Length < 3 ? this : Envir.GetPlayer(parts[1]);

                        if (player == null)
                        {
                            ReceiveChat(string.Format("Player {0} was not found.", parts[1]), ChatType.System);
                            return;
                        }
                        if (player.MyGuild != null)
                        {
                            ReceiveChat(string.Format("Player {0} is already in a guild.", player.Name), ChatType.System);
                            return;
                        }

                        String gName = parts.Length < 3 ? parts[1] : parts[2];
                        if ((gName.Length < 3) || (gName.Length > 20))
                        {
                            ReceiveChat("Guildname is restricted to 3-20 characters.", ChatType.System);
                            return;
                        }
                        GuildObject guild = Envir.GetGuild(gName);
                        if (guild != null)
                        {
                            ReceiveChat(string.Format("Guild {0} already exists.", gName), ChatType.System);
                            return;
                        }
                        player.CanCreateGuild = true;
                        if (player.CreateGuild(gName))
                            ReceiveChat(string.Format("Successfully created guild {0}", gName), ChatType.System);
                        else
                            ReceiveChat("Failed to create guild", ChatType.System);
                        player.CanCreateGuild = false;
                        break;

                    case "ALLOWTRADE":
                        AllowTrade = !AllowTrade;

                        if (AllowTrade)
                            ReceiveChat("允许交易", ChatType.System);
                        else
                            ReceiveChat("拒绝交易", ChatType.System);
                        break;
                    case "MAKEITEMSRC"://物品的来源
                        if ((!IsGM && !Settings.TestServer) || parts.Length < 2) return;

                        ItemInfo makeitemInfo = ItemInfo.getItem(parts[1]);
                        if (makeitemInfo == null) return;
                        item = makeitemInfo.CreateDropItem();
                        int src_mon_idx = 0;

                        if (parts.Length >= 3 )
                        {
                            int.TryParse(parts[2], out src_mon_idx);
                        }


                        MonsterObject mob = MonsterObject.GetMonster(Envir.GetMonsterInfo(src_mon_idx));
                        if (mob == null)
                        {
                            return;
                        }

                        item.src_time = Envir.Now.ToBinary();
                        item.src_mon = mob.Name;
                        item.src_kill = "未知";
                        item.src_map = CurrentMap.getTitle();
                        item.src_mon_idx = src_mon_idx;
            
                        if (!CanGainItem(item, false)) return;
                        GainItem(item);

                        ReceiveChat(string.Format("{0} x{1} 被创建.", makeitemInfo.Name, 1), ChatType.System);
                        break;

                    case "TRIGGER":
                        if (!IsGM) return;
                        if (parts.Length < 2) return;

                        if (parts.Length >= 3)
                        {
                            player = Envir.GetPlayer(parts[2]);

                            if (player == null)
                            {
                                ReceiveChat(string.Format("玩家 {0} 找不到.", parts[2]), ChatType.System);
                                return;
                            }

                            player.CallDefaultNPC(DefaultNPCType.Trigger, parts[1]);
                            return;
                        }

                        foreach (var pl in Envir.Players)
                        {
                            pl.CallDefaultNPC(DefaultNPCType.Trigger, parts[1]);
                        }

                        break;

                    case "RIDE":
                        if (MountType > -1)
                        {
                            RidingMount = !RidingMount;

                            RefreshMount();
                        }
                        else
                            ReceiveChat("你没有坐骑……", ChatType.System);

                        ChatTime = 0;
                        break;
                    case "SETFLAG":
                        if (!IsGM && !Settings.TestServer) return;

                        if (parts.Length < 2) return;

                        int tempInt = 0;

                        if (!int.TryParse(parts[1], out tempInt)) return;

                        if (tempInt > Info.Flags.Length - 1) return;

                        Info.Flags[tempInt] = !Info.Flags[tempInt];

                        for (int f = CurrentMap.NPCs.Count - 1; f >= 0; f--)
                        {
                            if (Functions.InRange(CurrentMap.NPCs[f].CurrentLocation, CurrentLocation, Globals.DataRange))
                                CurrentMap.NPCs[f].CheckVisible(this);
                        }

                        break;

                    case "LISTFLAGS":
                        if (!IsGM && !Settings.TestServer) return;

                        for (int i = 0; i < Info.Flags.Length; i++)
                        {
                            if (Info.Flags[i] == false) continue;

                            ReceiveChat("Flag " + i, ChatType.Hint);
                        }
                        break;

                    case "CLEARFLAGS":
                        if (!IsGM && !Settings.TestServer) return;

                        player = parts.Length > 1 && IsGM ? Envir.GetPlayer(parts[1]) : this;

                        if (player == null)
                        {
                            ReceiveChat(parts[1] + " 不在线", ChatType.System);
                            return;
                        }

                        for (int i = 0; i < player.Info.Flags.Length; i++)
                        {
                            player.Info.Flags[i] = false;
                        }
                        break;
                    case "CLEARMOB":
                        if (!IsGM) return;

                        if (parts.Length > 1)
                        {
                            map = Envir.GetMapByNameAndInstance(parts[1]);

                            if (map == null) return;

                        }
                        else
                        {
                            map = CurrentMap;
                        }

                        foreach (var objs in map.Objects)
                        {
                            if (objs == null || objs == null) continue;

                            int obCount = objs.Count();

                            for (int m = 0; m < obCount; m++)
                            {
                                MapObject ob = objs[m];

                                if (ob.Race != ObjectType.Monster) continue;
                                if (ob.Dead) continue;
                                ob.Die();
                            }
                        }

                        break;

                    case "CHANGECLASS": //@changeclass [Player] [Class]
                        if (!IsGM && !Settings.TestServer) return;

                        data = parts.Length <= 2 || !IsGM ? Info : Envir.GetCharacterInfo(parts[1]);

                        if (data == null) return;

                        MirClass mirClass;

                        if (!Enum.TryParse(parts[parts.Length - 1], true, out mirClass) || data.Class == mirClass) return;

                        data.Class = mirClass;

                        ReceiveChat(string.Format("玩家 {0} 已经更改为 {1}", data.Name, data.Class), ChatType.System);
                        SMain.Enqueue(string.Format("Player {0} has been changed to {1} by {2}", data.Name, data.Class, Name));

                        if (data.Player != null)
                            data.Player.Connection.LogOut();
                        break;

                    case "DIE":
                        LastHitter = null;
                        Die();
                        break;
                    case "HAIR":
                        if (!IsGM && !Settings.TestServer) return;

                        if (parts.Length < 2)
                        {
                            Info.Hair = (byte)RandomUtils.Next(0, 9);
                        }
                        else
                        {
                            byte tempByte = 0;

                            byte.TryParse(parts[1], out tempByte);

                            Info.Hair = tempByte;
                        }
                        break;

                    case "DECO": //TEST CODE
                        if ((!IsGM && !Settings.TestServer) || parts.Length < 2) return;

                        ushort tempShort = 0;

                        ushort.TryParse(parts[1], out tempShort);

                        DecoObject decoOb = new DecoObject
                        {
                            Image = tempShort,
                            CurrentMap = CurrentMap,
                            CurrentLocation = CurrentLocation,
                        };

                        CurrentMap.AddObject(decoOb);
                        decoOb.Spawned();

                        Enqueue(decoOb.GetInfo());
                        break;

                    case "ADJUSTPKPOINT":
                        if ((!IsGM && !Settings.TestServer) || parts.Length < 2) return;

                        if (parts.Length > 2)
                        {
                            if (!IsGM) return;

                            player = Envir.GetPlayer(parts[1]);

                            if (player == null) return;


                            int.TryParse(parts[2], out tempInt);
                        }
                        else
                        {
                            player = this;
                            int.TryParse(parts[1], out tempInt);
                        }

                        player.PKPoints = tempInt;

                        break;

                    case "AWAKENING":
                        {
                            if ((!IsGM && !Settings.TestServer) || parts.Length < 3) return;

                            ItemType type;

                            if (!Enum.TryParse(parts[1], true, out type)) return;

                            AwakeType awakeType;

                            if (!Enum.TryParse(parts[2], true, out awakeType)) return;

                            foreach (UserItem temp in Info.Equipment)
                            {
                                if (temp == null) continue;

                                ItemInfo realItem = Functions.GetRealItem(temp.Info, Info.Level, Info.Class, Envir.ItemInfoList);

                                if (realItem.Type == type)
                                {
                                    Awake awake = temp.Awake;
                                    bool[] isHit;
                                    int result = awake.UpgradeAwake(temp, awakeType, out isHit);
                                    switch (result)
                                    {
                                        case -1:
                                            ReceiveChat(string.Format("{0} : 条件错误.", temp.Name), ChatType.System);
                                            break;
                                        case 0:
                                            ReceiveChat(string.Format("{0} : 升级失败.", temp.Name), ChatType.System);
                                            break;
                                        case 1:
                                            ReceiveChat(string.Format("{0} : 觉醒等级 {1}, 值 {2}~{3}.", temp.Name, awake.getAwakeLevel(), awake.getAwakeValue(), awake.getAwakeValue()), ChatType.System);
                                            p = new S.RefreshItem { Item = temp };
                                            Enqueue(p);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                        break;
                    case "REMOVEAWAKENING":
                        {
                            if ((!IsGM && !Settings.TestServer) || parts.Length < 2) return;

                            ItemType type;

                            if (!Enum.TryParse(parts[1], true, out type)) return;

                            foreach (UserItem temp in Info.Equipment)
                            {
                                if (temp == null) continue;

                                ItemInfo realItem = Functions.GetRealItem(temp.Info, Info.Level, Info.Class, Envir.ItemInfoList);

                                if (realItem.Type == type)
                                {
                                    Awake awake = temp.Awake;
                                    int result = awake.RemoveAwake();
                                    switch (result)
                                    {
                                        case 0:
                                            ReceiveChat(string.Format("{0} : 分解失败,等级0", temp.Name), ChatType.System);
                                            break;
                                        case 1:
                                            ReceiveChat(string.Format("{0} : 分解成功. 等级 {1}", temp.Name, temp.Awake.getAwakeLevel()), ChatType.System);
                                            p = new S.RefreshItem { Item = temp };
                                            Enqueue(p);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                        break;

                    case "STARTWAR":
                        if (!IsGM) return;
                        if (parts.Length < 2) return;

                        GuildObject enemyGuild = Envir.GetGuild(parts[1]);

                        if (MyGuild == null)
                        {
                            ReceiveChat("你不在公会.", ChatType.System);
                        }

                        if (MyGuild.Ranks[0] != MyGuildRank)
                        {
                            ReceiveChat("你们必须是发动战争的领导人.", ChatType.System);
                            return;
                        }

                        if (enemyGuild == null)
                        {
                            ReceiveChat(string.Format("找不到公会 {0}.", parts[1]), ChatType.System);
                            return;
                        }

                        if (MyGuild == enemyGuild)
                        {
                            ReceiveChat("不能和你自己的公会开战.", ChatType.System);
                            return;
                        }

                        if (MyGuild.WarringGuilds.Contains(enemyGuild))
                        {
                            ReceiveChat("已经和这个公会交战了.", ChatType.System);
                            return;
                        }

                        if (MyGuild.GoToWar(enemyGuild))
                        {
                            ReceiveChat(string.Format("你已经和 {0} 开启战争.", parts[1]), ChatType.System);
                            enemyGuild.SendMessage(string.Format("{0} 开启战争", MyGuild.Name), ChatType.System);
                        }

                        break;

                    case "ADDINVENTORY":
                        {
                            //扩展背包
                            int openLevel = (int)((Info.Inventory.Length - 46) / 4);
                            uint openGold = (uint)(1000000 + openLevel * 1000000);
                            if (Account.Gold >= openGold)
                            {
                                Account.Gold -= openGold;
                                Enqueue(new S.LoseGold { Gold = openGold });
                                Enqueue(new S.ResizeInventory { Size = Info.ResizeInventory() });
                                ReceiveChat("背包增加.", ChatType.System);
                            }
                            else
                            {
                                ReceiveChat("没有足够的金币.", ChatType.System);
                            }
                            ChatTime = 0;
                        }
                        break;

                    case "ADDSTORAGE":
                        {
                            TimeSpan addedTime = new TimeSpan(10, 0, 0, 0);
                            uint cost = 1000000;

                            if (Account.Gold >= cost)
                            {
                                Account.Gold -= cost;
                                Account.HasExpandedStorage = true;

                                if (Account.ExpandedStorageExpiryDate > Envir.Now)
                                {
                                    Account.ExpandedStorageExpiryDate = Account.ExpandedStorageExpiryDate + addedTime;
                                    ReceiveChat("扩展存储时间延长，到期: " + Account.ExpandedStorageExpiryDate.ToString(), ChatType.System);
                                }
                                else
                                {
                                    Account.ExpandedStorageExpiryDate = Envir.Now + addedTime;
                                    ReceiveChat("存储扩展，到期: " + Account.ExpandedStorageExpiryDate.ToString(), ChatType.System);
                                }

                                Enqueue(new S.LoseGold { Gold = cost });
                                Enqueue(new S.ResizeStorage { Size = Account.ExpandStorage(), HasExpandedStorage = Account.HasExpandedStorage, ExpiryTime = Account.ExpandedStorageExpiryDate });
                            }
                            else
                            {
                                ReceiveChat("没有足够的金币.", ChatType.System);
                            }
                            ChatTime = 0;
                        }
                        break;

                    case "INFO":
                        {
                            if (!IsGM && !Settings.TestServer) return;

                            MapObject ob = null;

                            if (parts.Length < 2)
                            {
                                Point target = Functions.PointMove(CurrentLocation, Direction, 1);
                                //Cell cell = CurrentMap.GetCell(target);

                                if (CurrentMap.Objects[target.X, target.Y] == null || CurrentMap.Objects[target.X, target.Y].Count < 1) return;

                                ob = CurrentMap.Objects[target.X, target.Y][0];
                            }
                            else
                            {
                                ob = Envir.GetPlayer(parts[1]);
                            }

                            if (ob == null) return;

                            switch (ob.Race)
                            {
                                case ObjectType.Player:
                                    PlayerObject plOb = (PlayerObject)ob;
                                    ReceiveChat("--玩家信息--", ChatType.System2);
                                    ReceiveChat(string.Format("Name : {0}, Level : {1}, X : {2}, Y : {3}", plOb.Name, plOb.Level, plOb.CurrentLocation.X, plOb.CurrentLocation.Y), ChatType.System2);
                                    break;
                                case ObjectType.Monster:
                                    MonsterObject monOb = (MonsterObject)ob;
                                    ReceiveChat("--怪物信息--", ChatType.System2);
                                    ReceiveChat(string.Format("ID : {0}, Name : {1}", monOb.Info.Index, monOb.Name), ChatType.System2);
                                    ReceiveChat(string.Format("Level : {0}, X : {1}, Y : {2}", monOb.Level, monOb.CurrentLocation.X, monOb.CurrentLocation.Y), ChatType.System2);
                                    ReceiveChat(string.Format("HP : {0}, MinDC : {1}, MaxDC : {2},MinMC:{3},MaxMC:{4}", monOb.HP, monOb.MinDC, monOb.MaxDC, monOb.MinMC, monOb.MaxMC), ChatType.System2);
                                    ReceiveChat(string.Format("MoveSpeed : {0}, AttackSpeed : {1}", monOb.MoveSpeed, monOb.AttackSpeed), ChatType.System2);
                                    ReceiveChat(string.Format("MinAC : {0}, MaxAC : {1},MinMAC:{2},MaxMAC:{3}", monOb.MinAC, monOb.MaxAC, monOb.MinMAC, monOb.MaxMAC), ChatType.System2);
                                    ReceiveChat(string.Format("Accuracy : {0}, Agility : {1}", monOb.Accuracy, monOb.Agility), ChatType.System2);

                                    break;
                                case ObjectType.Merchant:
                                    NPCObject npcOb = (NPCObject)ob;
                                    ReceiveChat("--NPC 信息--", ChatType.System2);
                                    ReceiveChat(string.Format("ID : {0}, Name : {1}", npcOb.Info.Index, npcOb.Name), ChatType.System2);
                                    ReceiveChat(string.Format("X : {0}, Y : {1}", ob.CurrentLocation.X, ob.CurrentLocation.Y), ChatType.System2);
                                    ReceiveChat(string.Format("File : {0}", npcOb.Info.FileName), ChatType.System2);
                                    break;
                            }
                        }
                        break;
                    case "MAPINFO":
                        {
                            if (!IsGM && !Settings.TestServer) return;
                            ReceiveChat("当前地图附近16格的对象数:" + CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y,16).Count, ChatType.System2);
                            ReceiveChat("当前地图怪物数:" + CurrentMap.MonsterCount, ChatType.System2);
                        }
                        break;

                    case "CLEARQUESTS":
                        if (!IsGM && !Settings.TestServer) return;

                        player = parts.Length > 1 && IsGM ? Envir.GetPlayer(parts[1]) : this;

                        if (player == null)
                        {
                            ReceiveChat(parts[1] + " 不在线", ChatType.System);
                            return;
                        }

                        foreach (var quest in player.CurrentQuests)
                        {
                            SendUpdateQuest(quest, QuestState.Remove);
                        }

                        player.CurrentQuests.Clear();

                        player.CompletedQuests.Clear();
                        player.GetCompletedQuests();

                        break;

                    case "SETQUEST":
                        if ((!IsGM && !Settings.TestServer) || parts.Length < 3) return;

                        player = parts.Length > 3 && IsGM ? Envir.GetPlayer(parts[3]) : this;

                        if (player == null)
                        {
                            ReceiveChat(parts[3] + " 不在线", ChatType.System);
                            return;
                        }

                        int questid = 0;
                        int questState = 0;

                        int.TryParse(parts[1], out questid);
                        int.TryParse(parts[2], out questState);

                        if (questid < 1) return;

                        var activeQuest = player.CurrentQuests.FirstOrDefault(e => e.Index == questid);

                        //remove from active list
                        if (activeQuest != null)
                        {
                            player.SendUpdateQuest(activeQuest, QuestState.Remove);
                            player.CurrentQuests.Remove(activeQuest);
                        }

                        switch (questState)
                        {
                            case 0: //cancel
                                if (player.CompletedQuests.Contains(questid))
                                    player.CompletedQuests.Remove(questid);
                                break;
                            case 1: //complete
                                if (!player.CompletedQuests.Contains(questid))
                                    player.CompletedQuests.Add(questid);
                                break;
                        }

                        player.GetCompletedQuests();
                        break;

                    case "TOGGLETRANSFORM":
                        Buff b = Buffs.FirstOrDefault(e => e.Type == BuffType.Transform);
                        if (b == null) return;

                        if (!b.Paused)
                        {
                            PauseBuff(b);
                        }
                        else
                        {
                            UnpauseBuff(b);
                        }

                        RefreshStats();

                        hintstring = b.Paused ? "禁用时装." : "开启时装.";
                        ReceiveChat(hintstring, ChatType.Hint);
                        break;

                    case "CREATEMAPINSTANCE": //TEST CODE
                        if (!IsGM || parts.Length < 2) return;

                        map = SMain.Envir.GetMapByNameAndInstance(parts[1]);

                        if (map == null)
                        {
                            ReceiveChat(string.Format("地图 {0} 不存在", parts[1]), ChatType.System);
                            return;
                        }
                        
                        MapInfo mapInfo = map.Info;
                        mapInfo.CreateInstance();
                        ReceiveChat(string.Format("Map instance created for map {0}", mapInfo.Mcode), ChatType.System);
                        break;
                    case "STARTCONQUEST":
                        //Needs some work, but does job for now.
                        if ((!IsGM && !Settings.TestServer) || parts.Length < 2) return;
                        int ConquestID;

                        if (parts.Length < 1)
                        {
                            ReceiveChat(string.Format("The Syntax is /StartConquest [ConquestID]"), ChatType.System);
                            return;
                        }

                        if (MyGuild == null)
                        {
                            ReceiveChat(string.Format("你需要加入一个公会来发动一场战争"), ChatType.System);
                            return;
                        }
                
                        else if (!int.TryParse(parts[1], out ConquestID)) return;

                        ConquestObject tempConq = Envir.Conquests.FirstOrDefault(t => t.Info.Index == ConquestID);

                        if (tempConq != null)
                        {
                            tempConq.StartType = ConquestType.Forced;
                            tempConq.WarIsOn = !tempConq.WarIsOn;
                            tempConq.AttackerID = MyGuild.Guildindex;
                        }
                        else return;
                        ReceiveChat(string.Format("{0} 战争开始了.", tempConq.Info.Name), ChatType.System);
                        SMain.Enqueue(string.Format("{0}战争开始了.", tempConq.Info.Name));
                        break;
                    case "RESETCONQUEST":
                        //Needs some work, but does job for now.
                        if ((!IsGM && !Settings.TestServer) || parts.Length < 2) return;
                        int ConquestNum;

                        if (parts.Length < 1)
                        {
                            ReceiveChat(string.Format("The Syntax is /ResetConquest [ConquestID]"), ChatType.System);
                            return;
                        }

                        if (MyGuild == null)
                        {
                            ReceiveChat(string.Format("你需要加入一个公会来发动一场战争"), ChatType.System);
                            return;
                        }

                        else if (!int.TryParse(parts[1], out ConquestNum)) return;

                        ConquestObject ResetConq = Envir.Conquests.FirstOrDefault(t => t.Info.Index == ConquestNum);

                        if (ResetConq != null && !ResetConq.WarIsOn)
                        {
                            ResetConq.Reset();
                        }
                        else
                        {
                            ReceiveChat("未发现征服或战争正在进行.", ChatType.System);
                            return;
                        }
                        ReceiveChat(string.Format("{0} has been reset.", ResetConq.Info.Name), ChatType.System);
                        break;
                    case "GATES":

                        if (MyGuild == null || MyGuild.Conquest == null || !MyGuildRank.Options.HasFlag(RankOptions.CanChangeRank) || MyGuild.Conquest.WarIsOn)
                        {
                            ReceiveChat(string.Format("您目前无法控制任何门."), ChatType.System);
                            return;
                        }

                        bool OpenClose = false;

                        if (parts.Length > 1)
                        {
                            string openclose = parts[1];

                            if (openclose.ToUpper() == "CLOSE") OpenClose = true;
                            else if (openclose.ToUpper() == "OPEN") OpenClose = false;
                            else
                            {
                                ReceiveChat(string.Format("You must type /Gates Open or /Gates Close."), ChatType.System);
                                return;
                            }

                            for (int i = 0; i < MyGuild.Conquest.GateList.Count; i++)
                                if (MyGuild.Conquest.GateList[i].Gate != null && !MyGuild.Conquest.GateList[i].Gate.Dead)
                                    if (OpenClose)
                                        MyGuild.Conquest.GateList[i].Gate.CloseDoor();
                                    else
                                        MyGuild.Conquest.GateList[i].Gate.OpenDoor();
                        }
                        else
                        {
                            for (int i = 0; i < MyGuild.Conquest.GateList.Count; i++)
                                if (MyGuild.Conquest.GateList[i].Gate != null && !MyGuild.Conquest.GateList[i].Gate.Dead)
                                    if (!MyGuild.Conquest.GateList[i].Gate.Closed)
                                    {
                                        MyGuild.Conquest.GateList[i].Gate.CloseDoor();
                                        OpenClose = true;
                                    }
                                    else
                                    {
                                        MyGuild.Conquest.GateList[i].Gate.OpenDoor();
                                        OpenClose = false;
                                    }
                        }

                        if (OpenClose)
                            ReceiveChat(string.Format("{0} 已经关闭.", MyGuild.Conquest.Info.Name), ChatType.System);
                        else
                            ReceiveChat(string.Format(" {0} 已经打开.", MyGuild.Conquest.Info.Name), ChatType.System);
                        break;

                    case "CHANGEFLAG":
                        if (MyGuild == null || MyGuild.Conquest == null || !MyGuildRank.Options.HasFlag(RankOptions.CanChangeRank) || MyGuild.Conquest.WarIsOn)
                        {
                            ReceiveChat(string.Format("您目前无法更改任何标志."), ChatType.System);
                            return;
                        }

                        ushort flag = (ushort)RandomUtils.Next(12);

                        if(parts.Length > 1)
                        {
                            ushort temp;

                            ushort.TryParse(parts[1], out temp);

                            if (temp <= 11) flag = temp;
                        }

                        MyGuild.FlagImage = (ushort)(1000 + flag);

                        for (int i = 0; i < MyGuild.Conquest.FlagList.Count; i++)
                        {
                            MyGuild.Conquest.FlagList[i].UpdateImage();
                        }

                        break;
                    case "CHANGEFLAGCOLOUR":
                        {
                            if (MyGuild == null || MyGuild.Conquest == null || !MyGuildRank.Options.HasFlag(RankOptions.CanChangeRank) || MyGuild.Conquest.WarIsOn)
                            {
                                ReceiveChat(string.Format("您目前无法更改任何标志."), ChatType.System);
                                return;
                            }

                            byte r1 = (byte)RandomUtils.Next(255);
                            byte g1 = (byte)RandomUtils.Next(255);
                            byte b1 = (byte)RandomUtils.Next(255);

                            if (parts.Length > 3)
                            {
                                byte.TryParse(parts[1], out r1);
                                byte.TryParse(parts[2], out g1);
                                byte.TryParse(parts[3], out b1);
                            }

                            MyGuild.FlagColour = Color.FromArgb(255, r1, g1, b1);

                            for (int i = 0; i < MyGuild.Conquest.FlagList.Count; i++)
                            {
                                MyGuild.Conquest.FlagList[i].UpdateColour();
                            }
                        }
                        break;
                    case "REVIVE":
                        if (!IsGM) return;

                        if (parts.Length < 2)
                        {
                            RefreshStats();
                            SetHP(MaxHP);
                            SetMP(MaxMP);
                            Revive(MaxHealth, true);
                        }
                        else
                        {
                            player = Envir.GetPlayer(parts[1]);
                            if (player == null) return;
                            player.Revive(MaxHealth, true);
                        }
                        break;
                    case "DELETESKILL":
                        if ((!IsGM) || parts.Length < 2) return;
                        Spell skill1;

                        if (!Enum.TryParse(parts.Length > 2 ? parts[2] : parts[1], true, out skill1)) return;

                        if (skill1 == Spell.None) return;

                        if (parts.Length > 2)
                        {
                            if (!IsGM) return;
                            player = Envir.GetPlayer(parts[1]);

                            if (player == null)
                            {
                                ReceiveChat(string.Format("玩家 {0} 查找失败!", parts[1]), ChatType.System);
                                return;
                            }
                        }
                        else
                            player = this;

                        if (player == null) return;

                        var magics = new UserMagic(skill1);
                        bool removed = false;

                        for (var i = player.Info.Magics.Count - 1; i >= 0; i--)
                        {
                            if (player.Info.Magics[i].Spell != skill1) continue;

                            player.Info.Magics.RemoveAt(i);
                            player.Enqueue(new S.RemoveMagic { PlaceId = i });
                            removed = true;
                        }

                        if (removed)
                        {
                            ReceiveChat(string.Format("你已经移除 {0} 从 {1} 玩家", skill1.ToString(), player.Name), ChatType.Hint);
                            player.ReceiveChat(string.Format("{0}已经从你身上移除.", skill1), ChatType.Hint);
                        }
                        else ReceiveChat(string.Format("无法删除技能，未找到技能"), ChatType.Hint);

                        break;
                    default:
                        break;
                }

                foreach (string command in Envir.CustomCommands)
                {
                    if (string.Compare(parts[0], command, true) != 0) continue;
                    CallDefaultNPC(DefaultNPCType.CustomCommand, parts[0]);
                }
            }
            else
            {
                message = String.Format("{0}:{1}", CurrentMap.Info.NoNames ? "?????" : Name, message);

                p = new S.ObjectChat { ObjectID = ObjectID, Text = message, Type = ChatType.Normal };

                Enqueue(p);
                Broadcast(p);
            }
        }
        //转朝向
        public void Turn(MirDirection dir)
        {
            _stepCounter = 0;

            if (CanMove)
            {
                ActionTime = Envir.Time + GetDelayTime(TurnDelay);

                Direction = dir;
                if (CheckMovement(CurrentLocation)) return;

                SafeZoneInfo szi = CurrentMap.GetSafeZone(CurrentLocation);

                if (szi != null)
                {
                    BindLocation = szi.Location;
                    BindMapIndex = CurrentMapIndex;
                    InSafeZone = true;
                }
                else
                    InSafeZone = false;

                //Cell cell = CurrentMap.GetCell(CurrentLocation);

                for (int i = 0; i < CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y].Count; i++)
                {
                    if (CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i].Race != ObjectType.Spell) continue;
                    SpellObject ob = (SpellObject)CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i];

                    ob.ProcessSpell(this);
                    //break;
                }

                if (TradePartner != null)
                    TradeCancel();

                if (ItemRentalPartner != null)
                    CancelItemRental();

                Broadcast(new S.ObjectTurn { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
            }

            Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
        }
        //采集？
        //挖东西
        public void Harvest(MirDirection dir)
        {
            if (!CanMove)
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }

            ActionTime = Envir.Time + HarvestDelay;

            Direction = dir;

            Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
            Broadcast(new S.ObjectHarvest { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });

            Point front = Front;
            bool send = false;
            for (int d = 0; d <= 1; d++)
            {
                for (int y = front.Y - d; y <= front.Y + d; y++)
                {
                    if (y < 0) continue;
                    if (y >= CurrentMap.Height) break;

                    for (int x = front.X - d; x <= front.X + d; x += Math.Abs(y - front.Y) == d ? 1 : d * 2)
                    {
                        if (x < 0) continue;
                        if (x >= CurrentMap.Width) break;
                        if (!CurrentMap.ValidPoint(x, y)) continue;

                        //Cell cell = CurrentMap.GetCell(x, y);
                        if (CurrentMap.Objects[x,y] == null) continue;

                        for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                        {
                            MapObject ob = CurrentMap.Objects[x, y][i];
                            if (ob.Race != ObjectType.Monster || !ob.Dead || ob.Harvested) continue;

                            if (ob.EXPOwner != null && ob.EXPOwner != this && !IsMember(ob))
                            {
                                send = true;
                                continue;
                            }

                            if (ob.Harvest(this)) return;
                        }
                    }
                }
            }

            if (send)
                ReceiveChat("你没有任何附近的尸体.", ChatType.System);
        }
        //步行
        public void Walk(MirDirection dir)
        {

            if (!CanMove || !CanWalk)
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }

            Point location = Functions.PointMove(CurrentLocation, dir, 1);
            //这里改下，否则可能穿人
            if (!CurrentMap.ValidPoint(location.X, location.Y))
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }

            if (!CurrentMap.CheckDoorOpen(location))
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }


            //Cell cell = CurrentMap.GetCell(location);
            if (CurrentMap.Objects[location.X, location.Y] != null)
            {
                for (int i = 0; i < CurrentMap.Objects[location.X, location.Y].Count; i++)
                {
                    MapObject ob = CurrentMap.Objects[location.X, location.Y][i];

                    if (ob.Race == ObjectType.Merchant)
                    {
                        NPCObject NPC = (NPCObject)ob;
                        if (!NPC.Visible || !NPC.VisibleLog[Info.Index]) continue;
                    }
                    else
                    {
                        //if (!ob.Blocking || ob.CellTime >= Envir.Time) continue;
                        if (!ob.Blocking) continue;
                    }
                       

                    Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                    return;
                }
            }
                

            if (Concentrating)
            {
                if (ConcentrateInterrupted)
                    ConcentrateInterruptTime = SMain.Envir.Time + 3000;// needs adjusting
                else
                {
                    ConcentrateInterruptTime = SMain.Envir.Time + 3000;// needs adjusting
                    ConcentrateInterrupted = true;
                    UpdateConcentration();//Update & send to client
                }
            }

            if (Hidden && !HasClearRing)
            {
                for (int i = 0; i < Buffs.Count; i++)
                {
                    switch (Buffs[i].Type)
                    {
                        case BuffType.Hiding:
                            Buffs[i].ExpireTime = 0;
                            break;
                    }
                }
            }

            Direction = dir;
            if (CheckMovement(location)) return;

            CurrentMap.Remove(this);
            RemoveObjects(dir, 1);

            CurrentLocation = location;
            CurrentMap.Add(this);
            AddObjects(dir, 1);

            _stepCounter++;

            SafeZoneInfo szi = CurrentMap.GetSafeZone(CurrentLocation);

            if (szi != null)
            {
                BindLocation = szi.Location;
                BindMapIndex = CurrentMapIndex;
                InSafeZone = true;
            }
            else
                InSafeZone = false;


            CheckConquest();



            CellTime = Envir.Time + 500;
            ActionTime = Envir.Time + GetDelayTime(MoveDelay);

            if (TradePartner != null)
                TradeCancel();

            if (ItemRentalPartner != null)
                CancelItemRental();

            if (RidingMount) DecreaseMountLoyalty(1);

            Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
            Broadcast(new S.ObjectWalk { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });


            //cell = CurrentMap.GetCell(CurrentLocation);

            for (int i = 0; i < CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y].Count; i++)
            {
                if (CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i].Race != ObjectType.Spell) continue;
                SpellObject ob = (SpellObject)CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i];

                ob.ProcessSpell(this);
                //break;
            }

        }
        //跑步
        public void Run(MirDirection dir)
        {
            var steps = RidingMount || ActiveSwiftFeet && !Sneaking? 3 : 2;

            if (!CanMove || !CanWalk || !CanRun)
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }

            if (Concentrating)
            {
                if (ConcentrateInterrupted)
                    ConcentrateInterruptTime = SMain.Envir.Time + 3000;// needs adjusting
                else
                {
                    ConcentrateInterruptTime = SMain.Envir.Time + 3000;// needs adjusting
                    ConcentrateInterrupted = true;
                    UpdateConcentration();//Update & send to client
                }
            }

            if (TradePartner != null)
                TradeCancel();

            if (ItemRentalPartner != null)
                CancelItemRental();

            if (Hidden && !HasClearRing && !Sneaking)
            {
                for (int i = 0; i < Buffs.Count; i++)
                {
                    switch (Buffs[i].Type)
                    {
                        case BuffType.Hiding:
                        case BuffType.MoonLight:
                        case BuffType.DarkBody:
                            Buffs[i].ExpireTime = 0;
                            break;
                    }
                }
            }

            Direction = dir;
            Point location = Functions.PointMove(CurrentLocation, dir, 1);
            for (int j = 1; j <= steps; j++)
            {
                location = Functions.PointMove(CurrentLocation, dir, j);
                //这里改下，跑步要阻挡，否则可能穿人
                if (!CurrentMap.ValidPoint(location.X, location.Y))
                {
                    Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                    return;
                }
                if (!CurrentMap.CheckDoorOpen(location))
                {
                    Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                    return;
                }
                //Cell cell = CurrentMap.GetCell(location);

                if (CurrentMap.Objects[location.X, location.Y] != null)
                {
                    for (int i = 0; i < CurrentMap.Objects[location.X, location.Y].Count; i++)
                    {
                        MapObject ob = CurrentMap.Objects[location.X, location.Y][i];

                        if (ob.Race == ObjectType.Merchant)
                        {
                            NPCObject NPC = (NPCObject)ob;
                            if (!NPC.Visible || !NPC.VisibleLog[Info.Index]) continue;
                        }
                        else
                        {
                            //if (!ob.Blocking || ob.CellTime >= Envir.Time) continue;
                            if (!ob.Blocking) continue;
                        }
                        Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                        return;
                    }

                    
                }
                if (CheckMovement(location)) return;

            }
            if (RidingMount && !Sneaking)
            {
                DecreaseMountLoyalty(2);
            }

            Direction = dir;

            CurrentMap.Remove(this);
            RemoveObjects(dir, steps);

            Point OldLocation = CurrentLocation;
            CurrentLocation = location;
            CurrentMap.Add(this);
            AddObjects(dir, steps);


            SafeZoneInfo szi = CurrentMap.GetSafeZone(CurrentLocation);

            if (szi != null)
            {
                BindLocation = szi.Location;
                BindMapIndex = CurrentMapIndex;
                InSafeZone = true;
            }
            else
                InSafeZone = false;


            CheckConquest();



            CellTime = Envir.Time + 500;
            ActionTime = Envir.Time + GetDelayTime(MoveDelay);

            if (!RidingMount)
                _runCounter++;

            if (_runCounter > 10)
            {
                _runCounter -= 8;
                ChangeHP(-1);
            }

            Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
            Broadcast(new S.ObjectRun { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });


            for (int j = 1; j <= steps; j++)
            {
                location = Functions.PointMove(OldLocation, dir, j);
                //Cell cell = CurrentMap.GetCell(location);
                if (CurrentMap.Objects[location.X, location.Y] == null) continue;
                for (int i = 0; i < CurrentMap.Objects[location.X, location.Y].Count; i++)
                {
                    if (CurrentMap.Objects[location.X, location.Y][i].Race != ObjectType.Spell) continue;
                    SpellObject ob = (SpellObject)CurrentMap.Objects[location.X, location.Y][i];

                    ob.ProcessSpell(this);
                    //break;
                }
            }

        }
        //推动
        public override int Pushed(MapObject pusher, MirDirection dir, int distance)
        {
            int result = 0;
            MirDirection reverse = Functions.ReverseDirection(dir);
            //Cell cell;
            for (int i = 0; i < distance; i++)
            {
                Point location = Functions.PointMove(CurrentLocation, dir, 1);

                if (!CurrentMap.ValidPoint(location)) return result;

                //cell = CurrentMap.GetCell(location);

                bool stop = false;
                if (CurrentMap.Objects[location.X, location.Y] != null)
                    for (int c = 0; c < CurrentMap.Objects[location.X, location.Y].Count; c++)
                    {
                        MapObject ob = CurrentMap.Objects[location.X, location.Y][c];
                        if (!ob.Blocking) continue;
                        stop = true;
                    }
                if (stop) break;

                CurrentMap.Remove(this);

                Direction = reverse;
                RemoveObjects(dir, 1);
                CurrentLocation = location;
                CurrentMap.Add(this);
                AddObjects(dir, 1);

                if (TradePartner != null)
                    TradeCancel();

                if (ItemRentalPartner != null)
                    CancelItemRental();

                Enqueue(new S.Pushed { Direction = Direction, Location = CurrentLocation });
                Broadcast(new S.ObjectPushed { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });

                result++;
            }

            if (result > 0)
            {
                if (Concentrating)
                {
                    if (ConcentrateInterrupted)
                        ConcentrateInterruptTime = SMain.Envir.Time + 3000;// needs adjusting
                    else
                    {
                        ConcentrateInterruptTime = SMain.Envir.Time + 3000;// needs adjusting
                        ConcentrateInterrupted = true;
                        UpdateConcentration();//Update & send to client
                    }
                }

                //cell = CurrentMap.GetCell(CurrentLocation);

                for (int i = 0; i < CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y].Count; i++)
                {
                    if (CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i].Race != ObjectType.Spell) continue;
                    SpellObject ob = (SpellObject)CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i];

                    ob.ProcessSpell(this);
                    //break;
                }
            }

            ActionTime = Envir.Time + 500;
            return result;
        }

        //这个好像是射手的攻击
        public void RangeAttack(MirDirection dir, Point location, uint targetID)
        {
            LogTime = Envir.Time + Globals.LogDelay;
            //SMain.Enqueue("范围攻击");
            if (Info.Equipment[(int)EquipmentSlot.Weapon] == null) return;
            ItemInfo RealItem = Functions.GetRealItem(Info.Equipment[(int)EquipmentSlot.Weapon].Info, Info.Level, Info.Class, Envir.ItemInfoList);

            if ((RealItem.Shape / 100) != 2) return;
            if (Functions.InRange(CurrentLocation, location, Globals.MaxAttackRange) == false) return;

            MapObject target = null;

            if (targetID == ObjectID)
                target = this;
            else if (targetID > 0)
                target = FindObject(targetID, 10);

            if (target != null && target.Dead) return;

            if (target != null && target.Race != ObjectType.Monster && target.Race != ObjectType.Player) return;

            Direction = dir;

            Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });

            UserMagic magic;
            Spell spell = Spell.None;
            bool Focus = false;

            if (target != null && !CanFly(target.CurrentLocation) && (Info.MentalState != 1))
            {
                target = null;
                targetID = 0;
            }

            if (target != null)
            {
                magic = GetMagic(Spell.Focus);
                int damage = GetAttackPower(MinDC, MaxDC);//这里改为物理攻击
                if (magic != null && RandomUtils.Next(5) <= magic.Level)
                {
                    Focus = true;
                    LevelMagic(magic);
                    spell = Spell.Focus;
                }

                int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);
                
                damage = (int)(damage * Math.Max(1, (distance * 0.35)));//range boost
                damage = ApplyArcherState(damage);
                int chanceToHit = 60 + (Focus ? 30 : 0) - (int)(distance * 1.5);
                int hitChance = RandomUtils.Next(100); // Randomise a number between minimum chance and 100       
                //这里MISS?
                //这里改下，弓手少MISS
                chanceToHit = 100;
                if (hitChance < chanceToHit)
                {
                    if (target.CurrentLocation != location)
                        location = target.CurrentLocation;

                    int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500 + 50; //50 MS per Step

                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, target, damage, DefenceType.ACAgility, true);
                    ActionList.Add(action);
                }
                else
                {
                    int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500 + 50; //50 MS per Step
                    //这里改下，不MISS
                    DelayedAction action = new DelayedAction(DelayedType.DamageIndicator, Envir.Time + delay, target, DamageType.Miss);
                    ActionList.Add(action);
                }
            }
            else
                targetID = 0;

            Enqueue(new S.RangeAttack { TargetID = targetID, Target = location, Spell = spell });
            Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = targetID, Target = location, Spell = spell });

            AttackTime = Envir.Time + AttackSpeed;
            if (AttackSpeed < 550)
            {
                ActionTime = Envir.Time + AttackSpeed;
            }
            else
            {
                ActionTime = Envir.Time + 550;
            }
            
            RegenTime = Envir.Time + RegenDelay;

            return;
        }

        public void Attack(MirDirection dir, Spell spell)
        {
            LogTime = Envir.Time + Globals.LogDelay;

            bool Mined = false;
            bool MoonLightAttack = false;
            bool DarkBodyAttack = false;

            if (!CanAttack)
            {
                switch (spell)
                {
                    case Spell.Slaying:
                        Slaying = false;
                        break;
                }

                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }

            if (Hidden)
            {
                for (int i = 0; i < Buffs.Count; i++)
                {
                    switch (Buffs[i].Type)
                    {
                        case BuffType.MoonLight:
                        case BuffType.DarkBody:
                            MoonLightAttack = true;
                            DarkBodyAttack = true;
                            Buffs[i].ExpireTime = 0;
                            break;
                    }
                }
            }

            byte level = 0;
            UserMagic magic;

            if (RidingMount)
            {
                spell = Spell.None;
            }

            switch (spell)
            {
                case Spell.Slaying:
                    if (!Slaying)
                        spell = Spell.None;
                    else
                    {
                        magic = GetMagic(Spell.Slaying);
                        level = magic.Level;
                    }

                    Slaying = false;
                    break;
                case Spell.DoubleSlash:
                    magic = GetMagic(spell);
                    if (magic == null || magic.Info.BaseCost + (magic.Level * magic.Info.LevelCost) > MP)
                    {
                        spell = Spell.None;
                        break;
                    }
                    level = magic.Level;
                    ChangeMP(-(magic.Info.BaseCost + magic.Level * magic.Info.LevelCost));
                    break;
                case Spell.Thrusting:
                case Spell.FlamingSword:
                    magic = GetMagic(spell);
                    if ((magic == null) || (!FlamingSword && (spell == Spell.FlamingSword)))
                    {
                        spell = Spell.None;
                        break;
                    }
                    level = magic.Level;
                    break;
                case Spell.HalfMoon:
                case Spell.CrossHalfMoon:
                    magic = GetMagic(spell);
                    if (magic == null || magic.Info.BaseCost + (magic.Level * magic.Info.LevelCost) > MP)
                    {
                        spell = Spell.None;
                        break;
                    }
                    level = magic.Level;
                    ChangeMP(-(magic.Info.BaseCost + magic.Level * magic.Info.LevelCost));
                    break;
                case Spell.TwinDrakeBlade:
                    magic = GetMagic(spell);
                    if (!TwinDrakeBlade || magic == null || magic.Info.BaseCost + magic.Level * magic.Info.LevelCost > MP)
                    {
                        spell = Spell.None;
                        break;
                    }
                    level = magic.Level;
                    ChangeMP(-(magic.Info.BaseCost + magic.Level * magic.Info.LevelCost));
                    break;
                default:
                    spell = Spell.None;
                    break;
            }


            if (!Slaying)
            {
                magic = GetMagic(Spell.Slaying);

                if (magic != null && RandomUtils.Next(12) <= magic.Level)
                {
                    Slaying = true;
                    Enqueue(new S.SpellToggle { Spell = Spell.Slaying, CanUse = Slaying });
                }
            }

            Direction = dir;

            if (RidingMount) DecreaseMountLoyalty(3);

            Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Spell = spell, Level = level });

            AttackTime = Envir.Time + AttackSpeed;
            if (AttackSpeed < 550)
            {
                ActionTime = Envir.Time + AttackSpeed;
            }
            else
            {
                ActionTime = Envir.Time + 550;
            }
            RegenTime = Envir.Time + RegenDelay;

            Point target = Functions.PointMove(CurrentLocation, dir, 1);

            //damabeBase = the original damage from your gear (+ bonus from moonlight and darkbody)
            int damageBase = GetAttackPower(MinDC, MaxDC);
            //damageFinal = the damage you're gonna do with skills added
            int damageFinal;

            if (MoonLightAttack || DarkBodyAttack)
            {
                magic = MoonLightAttack ? GetMagic(Spell.MoonLight) : GetMagic(Spell.DarkBody);

                if (magic != null)
                {
                    damageBase += magic.GetPower();
                }
            }

            if (!CurrentMap.ValidPoint(target))
            {
                switch (spell)
                {
                    case Spell.Thrusting:
                        goto Thrusting;
                    case Spell.HalfMoon:
                        goto HalfMoon;
                    case Spell.CrossHalfMoon:
                        goto CrossHalfMoon;
                    case Spell.None:
                        Mined = true;
                        goto Mining;
                }
                return;
            }

            //Cell cell = CurrentMap.GetCell(target);

            if (CurrentMap.Objects[target.X, target.Y] == null)
            {
                switch (spell)
                {
                    case Spell.Thrusting:
                        goto Thrusting;
                    case Spell.HalfMoon:
                        goto HalfMoon;
                    case Spell.CrossHalfMoon:
                        goto CrossHalfMoon;
                }
                return;
            }

            damageFinal = damageBase;//incase we're not using skills
            for (int i = 0; i < CurrentMap.Objects[target.X, target.Y].Count; i++)
            {
                MapObject ob = CurrentMap.Objects[target.X, target.Y][i];
                if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                if (!ob.IsAttackTarget(this)) continue;

                //Only undead targets
                if (ob.Undead)
                {
                    damageBase = Math.Min(int.MaxValue, damageBase + Holy);
                    damageFinal = damageBase;//incase we're not using skills
                }

                #region FatalSword
                magic = GetMagic(Spell.FatalSword);

                DefenceType defence = DefenceType.ACAgility;

                if (magic != null)
                {
                    if (FatalSword)
                    {
                        damageFinal = magic.GetDamage(damageBase);
                        LevelMagic(magic);
                        S.ObjectEffect p = new S.ObjectEffect { ObjectID = ob.ObjectID, Effect = SpellEffect.FatalSword };

                        defence = DefenceType.Agility;
                        CurrentMap.Broadcast(p, ob.CurrentLocation);

                        FatalSword = false;
                    }

                    if (!FatalSword && RandomUtils.Next(10) == 0)
                        FatalSword = true;
                }
                #endregion

                #region MPEater
                magic = GetMagic(Spell.MPEater);

                if (magic != null)
                {
                    int baseCount = 1 + Accuracy / 2;
                    int maxCount = baseCount + magic.Level * 5;
                    MPEaterCount += RandomUtils.Next(baseCount, maxCount);
                    if (MPEater)
                    {
                        LevelMagic(magic);
                        damageFinal = magic.GetDamage(damageBase);
                        defence = DefenceType.ACAgility;

                        S.ObjectEffect p = new S.ObjectEffect { ObjectID = ob.ObjectID, Effect = SpellEffect.MPEater, EffectType = ObjectID };
                        CurrentMap.Broadcast(p, ob.CurrentLocation);

                        int addMp = 5 * (magic.Level + Accuracy / 4);
                        if (hasItemSk(ItemSkill.Assassin4))
                        {
                            addMp = addMp * 15 / 10;
                        }

                        if (ob.Race == ObjectType.Player)
                        {
                            ((PlayerObject)ob).ChangeMP(-addMp);
                        }

                        ChangeMP(addMp);
                        MPEaterCount = 0;
                        MPEater = false;
                    }
                    else if (!MPEater && 100 <= MPEaterCount) MPEater = true;
                }
                #endregion

                #region Hemorrhage
                magic = GetMagic(Spell.Hemorrhage);

                if (magic != null)
                {
                    HemorrhageAttackCount += RandomUtils.Next(1, 1 + magic.Level * 2);
                    if (hasItemSk(ItemSkill.Assassin7))
                    {
                        HemorrhageAttackCount += 1;
                    }
                    if (Hemorrhage)
                    {
                        damageFinal = magic.GetDamage(damageBase);
                        LevelMagic(magic);
                        S.ObjectEffect ef = new S.ObjectEffect { ObjectID = ob.ObjectID, Effect = SpellEffect.Hemorrhage };

                        CurrentMap.Broadcast(ef, ob.CurrentLocation);

                        if (ob == null || ob.Node == null) continue;

                        long calcDuration = magic.Level * 2 + Luck / 6;

                        ob.ApplyPoison(new Poison
                        {
                            Duration = (calcDuration <= 0) ? 1 : calcDuration,
                            Owner = this,
                            PType = PoisonType.Bleeding,
                            TickSpeed = 1000,
                            Value = MaxDC + 1
                        }, this);

                        ob.OperateTime = 0;
                        HemorrhageAttackCount = 0;
                        Hemorrhage = false;
                    }
                    else if (!Hemorrhage && 55 <= HemorrhageAttackCount) Hemorrhage = true;
                }
                #endregion

                DelayedAction action;
                switch (spell)
                {
                    case Spell.Slaying:
                        magic = GetMagic(Spell.Slaying);
                        damageFinal = magic.GetDamage(damageBase);
                        //SMain.Enqueue("攻杀剑法："+ damageFinal+","+ damageBase);
                        if(hasItemSk(ItemSkill.Warrior1) && RandomUtils.Next(100) < 50)
                        {
                            defence = DefenceType.Agility;
                        }
                        
                        LevelMagic(magic);
                        break;
                    case Spell.DoubleSlash:
                        magic = GetMagic(Spell.DoubleSlash);
                        damageFinal = magic.GetDamage(damageBase);

                        if (defence == DefenceType.ACAgility) defence = DefenceType.MACAgility;

                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 400, ob, damageFinal, DefenceType.Agility, false);
                        ActionList.Add(action);
                        LevelMagic(magic);
                        break;
                    case Spell.Thrusting:
                        magic = GetMagic(Spell.Thrusting);
                        LevelMagic(magic);
                        break;
                    case Spell.HalfMoon:
                        magic = GetMagic(Spell.HalfMoon);
                        LevelMagic(magic);
                        break;
                    case Spell.CrossHalfMoon:
                        magic = GetMagic(Spell.CrossHalfMoon);
                        LevelMagic(magic);
                        break;
                    case Spell.TwinDrakeBlade://双龙，几率麻痹
                        magic = GetMagic(Spell.TwinDrakeBlade);
                        damageFinal = magic.GetDamage(damageBase);
                        if (hasItemSk(ItemSkill.Warrior4) && RandomUtils.Next(100) < 50)
                        {
                            damageFinal = damageFinal * 13 / 10;
                        }
                        TwinDrakeBlade = false;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 400, ob, damageFinal, DefenceType.Agility, false);
                        ActionList.Add(action);
                        LevelMagic(magic);

                        if ((((ob.Race != ObjectType.Player) || Settings.PvpCanResistPoison) && (RandomUtils.Next(Settings.PoisonAttackWeight) >= ob.PoisonResist)) && (ob.Level < Level + 10 && RandomUtils.Next(ob.Race == ObjectType.Player ? 40 : 20) <= magic.Level + 1))
                        {
                            ob.ApplyPoison(new Poison { PType = PoisonType.Stun, Duration = ob.Race == ObjectType.Player ? 2 : 2 + magic.Level, TickSpeed = 1000 }, this);
                            ob.Broadcast(new S.ObjectEffect { ObjectID = ob.ObjectID, Effect = SpellEffect.TwinDrakeBlade });
                        }

                        break;
                    case Spell.FlamingSword:
                        magic = GetMagic(Spell.FlamingSword);
                        damageFinal = magic.GetDamage(damageBase);
                        FlamingSword = false;
                        defence = DefenceType.AC;
                        //action = new DelayedAction(DelayedType.Damage, Envir.Time + 400, ob, damage, DefenceType.Agility, true);
                        //ActionList.Add(action);
                        LevelMagic(magic);
                        break;
                }

                //if (ob.Attacked(this, damage, defence) <= 0) break;
                action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, ob, damageFinal, defence, true);
                ActionList.Add(action);
                break;
            }

        Thrusting:
            if (spell == Spell.Thrusting)
            {
                target = Functions.PointMove(target, dir, 1);

                if (!CurrentMap.ValidPoint(target)) return;

                //cell = CurrentMap.GetCell(target);

                if (CurrentMap.Objects[target.X, target.Y] == null) return;

                for (int i = 0; i < CurrentMap.Objects[target.X, target.Y].Count; i++)
                {
                    MapObject ob = CurrentMap.Objects[target.X, target.Y][i];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;

                    magic = GetMagic(spell);
                    damageFinal = magic.GetDamage(damageBase);
                    ob.Attacked(this, damageFinal, DefenceType.Agility, false);
                    break;
                }


            }
        HalfMoon:
            if (spell == Spell.HalfMoon)
            {
                dir = Functions.PreviousDir(dir);

                magic = GetMagic(spell);
                damageFinal = magic.GetDamage(damageBase);
                if (hasItemSk(ItemSkill.Warrior2) )
                {
                    damageFinal = damageFinal*12/10;
                }
                for (int i = 0; i < 4; i++)
                {
                    target = Functions.PointMove(CurrentLocation, dir, 1);
                    dir = Functions.NextDir(dir);
                    if (target == Front) continue;

                    if (!CurrentMap.ValidPoint(target)) continue;

                    //cell = CurrentMap.GetCell(target);

                    if (CurrentMap.Objects[target.X,target.Y] == null) continue;

                    for (int o = 0; o < CurrentMap.Objects[target.X, target.Y].Count; o++)
                    {
                        MapObject ob = CurrentMap.Objects[target.X, target.Y][o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;

                        ob.Attacked(this, damageFinal, DefenceType.Agility, false);
                        break;
                    }
                }
            }

        CrossHalfMoon:
            if (spell == Spell.CrossHalfMoon)
            {
                magic = GetMagic(spell);
                damageFinal = magic.GetDamage(damageBase);
                if (hasItemSk(ItemSkill.Warrior2))
                {
                    damageFinal = damageFinal * 115 / 100;
                }
                for (int i = 0; i < 8; i++)
                {
                    target = Functions.PointMove(CurrentLocation, dir, 1);
                    dir = Functions.NextDir(dir);
                    if (target == Front) continue;

                    if (!CurrentMap.ValidPoint(target)) continue;

                    //cell = CurrentMap.GetCell(target);

                    if (CurrentMap.Objects[target.X, target.Y] == null) continue;

                    for (int o = 0; o < CurrentMap.Objects[target.X, target.Y].Count; o++)
                    {
                        MapObject ob = CurrentMap.Objects[target.X, target.Y][o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;

                        ob.Attacked(this, damageFinal, DefenceType.Agility, false);
                        break;
                    }
                }
            }

        Mining:
            if (Mined)
            {
                //挖矿逻辑处理
                if (Info.Equipment[(int)EquipmentSlot.Weapon] == null) return;
                if (!Info.Equipment[(int)EquipmentSlot.Weapon].Info.CanMine) return;
                //没有持久无法挖矿
                if (Info.Equipment[(int)EquipmentSlot.Weapon].CurrentDura == 0) return;
                MineSpot Mine = CurrentMap.getMine(target.X, target.Y);
                if ((Mine == null) || (Mine.Mine == null)) return;
                if (Mine.StonesLeft > 0)
                {
                    //这个直接就减少啊？
                    Mine.StonesLeft--;
                    if (RandomUtils.Next(100) <= (Mine.Mine.HitRate + (Info.Equipment[(int)EquipmentSlot.Weapon].Info.Accuracy + Info.Equipment[(int)EquipmentSlot.Weapon].Accuracy) * 10))
                    {
                        //create some rubble on the floor (or increase whats there)
                        SpellObject Rubble = null;
                        //Cell minecell = CurrentMap.GetCell(CurrentLocation);
                        for (int i = 0; i < CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y].Count; i++)
                        {
                            if (CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i].Race != ObjectType.Spell) continue;
                            SpellObject ob = (SpellObject)CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i];

                            if (ob.Spell != Spell.Rubble) continue;
                            Rubble = ob;
                            Rubble.ExpireTime = Envir.Time + (5 * 60 * 1000);
                            break;
                        }
                        if (Rubble == null)
                        {
                            Rubble = new SpellObject
                            {
                                Spell = Spell.Rubble,
                                Value = 1,
                                ExpireTime = Envir.Time + (5 * 60 * 1000),
                                TickSpeed = 2000,
                                Caster = null,
                                CurrentLocation = CurrentLocation,
                                CurrentMap = this.CurrentMap,
                                Direction = MirDirection.Up
                            };
                            CurrentMap.AddObject(Rubble);
                            Rubble.Spawned();
                        }
                        if (Rubble != null)
                            ActionList.Add(new DelayedAction(DelayedType.Mine, Envir.Time + 400, Rubble));
                        //check if we get a payout 核对一下我们是否得到报酬
                        if (RandomUtils.Next(100) <= (Mine.Mine.DropRate + (MineRate * 5)))
                        {
                            GetMinePayout(Mine.Mine);
                        }
                        DamageItem(Info.Equipment[(int)EquipmentSlot.Weapon], 5 + RandomUtils.Next(15));
                    }
                }
                else
                {
                    if (Envir.Time > Mine.LastRegenTick)
                    {
                        Mine.LastRegenTick = Envir.Time + Mine.Mine.SpotRegenRate * 60 * 1000;
                        Mine.StonesLeft = (byte)RandomUtils.Next(Mine.Mine.MaxStones);
                    }
                }
            }
        }

        //挖矿
        public void GetMinePayout(MineSet Mine)
        {
            if ((Mine.Drops == null) || (Mine.Drops.Count == 0)) return;
            if (FreeSpace(Info.Inventory) == 0) return;
            MineDrop minedrop = Mine.DropMine();
            if (minedrop == null)
            {
                return;
            }
            if (minedrop.getItem() != null)
            {
                UserItem item = minedrop.getItem().CreateDropItem();
                if (item.Info.Type == ItemType.Ore)
                {
                    item.CurrentDura = (ushort)Math.Min(ushort.MaxValue, (minedrop.MinDura + RandomUtils.Next(Math.Max(0, minedrop.MaxDura - minedrop.MinDura))) * 1000);
                }
                if (CheckGroupQuestItem(item)) return;

                if (CanGainItem(item, false))
                {
                    GainItem(item);
                    Report.ItemChanged("MinePayout", item, item.Count, 2);
                }
                return;
            }
        }
        //释放魔法技能
        public void Magic(Spell spell, MirDirection dir, uint targetID, Point location)
        {
            if (!CanCast)
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }

            UserMagic magic = GetMagic(spell);

            if (magic == null || magic.Info==null)
            {
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }

            if ((location.X != 0) && (location.Y != 0) && magic.Info.Range != 0 && Functions.InRange(CurrentLocation, location, magic.Info.Range) == false) return;

            if (Hidden)
            {
                for (int i = 0; i < Buffs.Count; i++)
                {
                    switch (Buffs[i].Type)
                    {
                        case BuffType.MoonLight:
                        case BuffType.DarkBody:
                            Buffs[i].ExpireTime = 0;
                            break;
                    }
                }
            }

            AttackTime = Envir.Time + MoveDelay;
            SpellTime = Envir.Time + 1800; //Spell Delay

            if (spell != Spell.ShoulderDash)
                ActionTime = Envir.Time + MoveDelay;

            LogTime = Envir.Time + Globals.LogDelay;

            long delay = magic.GetDelay();
            //施法时间限制
            if (magic != null && Envir.Time < (magic.CastTime + delay) && magic.CastTime > 0)
            {
                long needtime = ((magic.CastTime + delay) - Envir.Time) / 1000;
                if (needtime <= 0)
                {
                    needtime = 1;
                }
                ReceiveChat(string.Format("["+magic.Info.Name+"]技能释放过于频繁，请在{0}秒后再试", needtime), ChatType.System);
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }

            int cost = magic.Info.BaseCost + magic.Info.LevelCost * magic.Level;

            if (spell == Spell.Teleport || spell == Spell.Blink || spell == Spell.StormEscape)
                for (int i = 0; i < Buffs.Count; i++)
                {
                    if (Buffs[i].Type != BuffType.TemporalFlux) continue;
                    cost += (int)(MaxMP * 0.3F);
                    break;
                }

            if (Buffs.Any(e => e.Type == BuffType.MagicBooster))
            {
                UserMagic booster = GetMagic(Spell.MagicBooster);

                if (booster != null)
                {
                    int penalty = (int)Math.Round((decimal)(cost / 100) * (6 + booster.Level));
                    cost += penalty;
                }
            }

            if (cost > MP)
            {
                ReceiveChat(string.Format("[" + magic.Info.Name + "]技能无法释放,MP不足"), ChatType.System);
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
                return;
            }

            RegenTime = Envir.Time + RegenDelay;
            ChangeMP(-cost);

            Direction = dir;
            if (spell != Spell.ShoulderDash && spell != Spell.BackStep && spell != Spell.FlashDash)
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });

            MapObject target = null;

            if (targetID == ObjectID)
                target = this;
            else if (targetID > 0)
                target = FindObject(targetID, 10);

            bool cast = true;
            byte level = magic.Level;
            switch (spell)
            {
                case Spell.FireBall:
                case Spell.GreatFireBall:
                case Spell.FrostCrunch:
                    if (!Fireball(target, magic)) targetID = 0;
                    break;
                case Spell.Healing:
                    if (target == null)
                    {
                        target = this;
                        targetID = ObjectID;
                    }
                    Healing(target, magic);
                    break;
                case Spell.Repulsion:
                case Spell.EnergyRepulsor:
                case Spell.FireBurst:
                    Repulsion(magic);
                    break;
                case Spell.ElectricShock:
                    ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, target as MonsterObject));
                    break;
                case Spell.Poisoning:
                    if (!Poisoning(target, magic)) cast = false;
                    break;
                case Spell.HellFire:
                    HellFire(magic);
                    break;
                case Spell.ThunderBolt:
                    ThunderBolt(target, magic);
                    break;
                case Spell.SoulFireBall:
                    if (!SoulFireball(target, magic, out cast)) targetID = 0;
                    break;
                case Spell.SummonSkeleton:
                    SummonSkeleton(magic);
                    break;
                case Spell.Teleport:
                case Spell.Blink:
                    ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 200, magic, location));
                    break;
                case Spell.Hiding:
                    Hiding(magic);
                    break;
                case Spell.Haste:
                case Spell.LightBody:
                    ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic));
                    break;
                case Spell.Fury:
                    FurySpell(magic, out cast);
                    break;
                case Spell.ImmortalSkin:
                    ImmortalSkin(magic, out cast);
                    break;
                case Spell.FireBang:
                case Spell.IceStorm:
                    FireBang(magic, target == null ? location : target.CurrentLocation);
                    break;
                case Spell.MassHiding:
                    MassHiding(magic, target == null ? location : target.CurrentLocation, out cast);
                    break;
                case Spell.SoulShield:
                case Spell.BlessedArmour:
                    SoulShield(magic, target == null ? location : target.CurrentLocation, out cast);
                    break;
                case Spell.FireWall:
                    FireWall(magic, target == null ? location : target.CurrentLocation);
                    break;
                case Spell.Lightning:
                    Lightning(magic);
                    break;
                case Spell.HeavenlySword:
                    HeavenlySword(magic);
                    break;
                case Spell.MassHealing:
                    MassHealing(magic, target == null ? location : target.CurrentLocation);
                    break;
                case Spell.ShoulderDash:
                    ShoulderDash(magic);
                    return;
                case Spell.ThunderStorm:
                case Spell.FlameField:
                case Spell.StormEscape:
                    ThunderStorm(magic);
                    if (spell == Spell.FlameField)
                        SpellTime = Envir.Time + 2500; //Spell Delay
                    if (spell == Spell.StormEscape)
                        //Start teleport.
                        ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 750, magic, location));
                    break;
                case Spell.MagicShield:
                    ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, magic.GetPower(GetAttackPower(MinMC, MaxMC) + 15)));
                    break;
                case Spell.FlameDisruptor:
                    FlameDisruptor(target, magic);
                    break;
                case Spell.TurnUndead:
                    TurnUndead(target, magic);
                    break;
                case Spell.MagicBooster:
                    MagicBooster(magic);
                    break;
                case Spell.Vampirism:
                    Vampirism(target, magic);
                    break;
                case Spell.SummonShinsu:
                    SummonShinsu(magic);
                    break;
                case Spell.Purification:
                    if (target == null)
                    {
                        target = this;
                        targetID = ObjectID;
                    }
                    Purification(target, magic);
                    break;
                case Spell.LionRoar:
                case Spell.BattleCry:
                    CurrentMap.ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, CurrentLocation));
                    break;
                case Spell.Revelation:
                    Revelation(target, magic);
                    break;
                case Spell.PoisonCloud:
                    PoisonCloud(magic, location, out cast);
                    break;
                case Spell.Entrapment:
                    Entrapment(target, magic);
                    break;
                case Spell.BladeAvalanche:
                    BladeAvalanche(magic);
                    break;
                case Spell.SlashingBurst:
                    SlashingBurst(magic, out cast);
                    break;
                case Spell.Rage:
                    Rage(magic);
                    break;
                case Spell.Mirroring:
                    Mirroring(magic);
                    break;
                case Spell.Blizzard:
                    Blizzard(magic, target == null ? location : target.CurrentLocation, out cast);
                    break;
                case Spell.MeteorStrike:
                    MeteorStrike(magic, target == null ? location : target.CurrentLocation, out cast);
                    break;
                case Spell.IceThrust:
                    IceThrust(magic);
                    break;

                case Spell.ProtectionField:
                    ProtectionField(magic);
                    break;
                case Spell.PetEnhancer:
                    PetEnhancer(target, magic, out cast, location);
                    break;
                case Spell.HealingCircle:
                    HealingCircle(magic,target, out cast);
                    break;
                case Spell.TrapHexagon:
                    TrapHexagon(magic, target, out cast);
                    break;
                case Spell.Reincarnation://复活
                    Reincarnation(magic, target == null ? null : target as PlayerObject, out cast);
                    break;
                case Spell.Curse:
                    Curse(magic, target == null ? location : target.CurrentLocation, out cast);
                    break;
                case Spell.SummonHolyDeva:
                    SummonHolyDeva(magic);
                    break;
                case Spell.Hallucination:
                    Hallucination(target, magic);
                    break;
                case Spell.EnergyShield:
                    EnergyShield(target, magic, out cast);
                    break;
                case Spell.UltimateEnhancer:
                    UltimateEnhancer(target, magic, out cast);
                    break;
                case Spell.Plague:
                    Plague(magic, target == null ? location : target.CurrentLocation, out cast);
                    break;
                case Spell.SwiftFeet:
                    SwiftFeet(magic, out cast);
                    break;
                case Spell.MoonLight:
                    MoonLight(magic);
                    break;
                case Spell.Trap:
                    Trap(magic, target, out cast);
                    break;
                case Spell.PoisonSword:
                    PoisonSword(magic);
                    break;
                case Spell.DarkBody:
                    DarkBody(target, magic);
                    break;
                case Spell.FlashDash:
                    FlashDash(magic);
                    return;
                case Spell.CrescentSlash:
                    CrescentSlash(magic);
                    break;
                case Spell.StraightShot:
                    if (!StraightShot(target, magic)) targetID = 0;
                    break;
                case Spell.DoubleShot:
                    if (!DoubleShot(target, magic)) targetID = 0;
                    break;
                case Spell.BackStep:
                    BackStep(magic);
                    return;
                case Spell.ExplosiveTrap:
                    ExplosiveTrap(magic, Front);
                    break;
                case Spell.DelayedExplosion:
                    if (!DelayedExplosion(target, magic)) targetID = 0;
                    break;
                case Spell.Concentration:
                    Concentration(magic);
                    break;
                case Spell.ElementalShot:
                    if (!ElementalShot(target, magic)) targetID = 0;
                    break;
                case Spell.ElementalBarrier:
                    ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, magic.GetPower(GetAttackPower(MinMC, MaxMC))));
                    break;
                case Spell.BindingShot:
                    BindingShot(magic, target, out cast);
                    break;
                case Spell.SummonVampire:
                case Spell.SummonToad:
                case Spell.SummonSnakes:
                    ArcherSummon(magic, target, location);
                    break;
                case Spell.VampireShot:
                case Spell.PoisonShot:
                case Spell.CrippleShot:
                    SpecialArrowShot(target, magic);
                    break;
                case Spell.NapalmShot:
                    NapalmShot(target, magic);
                    break;
                case Spell.OneWithNature:
                    OneWithNature(target, magic);
                    break;

                //Custom Spells
                case Spell.Portal:
                    Portal(magic, location, out cast);
                    break;
                case Spell.FixedMove://定点移动，类似闪现
                    FixedMove(magic, location, out cast);
                    break;
                case Spell.EmptyDoor://虚空，虚空之门,地狱之门，一个圆圈的门
                    EmptyDoor(magic, location, out cast);
                    break;
                case Spell.MyMonsterBomb://契约兽自爆
                    MyMonsterBomb(magic, location, out cast);
                    break;
                default:
                    cast = false;
                    break;
            }

            if (cast)
            {
                magic.CastTime = Envir.Time;
            }

            Enqueue(new S.Magic { Spell = spell, TargetID = targetID, Target = location, Cast = cast, Level = level });
            Broadcast(new S.ObjectMagic { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Spell = spell, TargetID = targetID, Target = location, Cast = cast, Level = level });
        }

        #region Elemental System
        private void Concentration(UserMagic magic)
        {
            int duration = 45 + (15 * magic.Level);
            int count = Buffs.Where(x => x.Type == BuffType.Concentration).ToList().Count();
            if (count > 0) return;

            AddBuff(new Buff { Type = BuffType.Concentration, Caster = this, ExpireTime = Envir.Time + duration * 1000, Values = new int[] { magic.Level } });

            LevelMagic(magic);

            ConcentrateInterruptTime = 0;
            ConcentrateInterrupted = false;
            Concentrating = true;
            UpdateConcentration();//Update & send to client

            OperateTime = 0;
        }
        public void UpdateConcentration()
        {
            Enqueue(new S.SetConcentration { ObjectID = ObjectID, Enabled = Concentrating, Interrupted = ConcentrateInterrupted });
            Broadcast(new S.SetObjectConcentration { ObjectID = ObjectID, Enabled = Concentrating, Interrupted = ConcentrateInterrupted });
        }
        private bool ElementalShot(MapObject target, UserMagic magic)
        {
            if (HasElemental)
            {
                if (target == null || !target.IsAttackTarget(this)) return false;
                if ((Info.MentalState != 1) && !CanFly(target.CurrentLocation)) return false;

                int orbPower = GetElementalOrbPower(false);//base power + orbpower

                int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC) + orbPower);
                int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500; //50 MS per Step

                DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target);
                ActionList.Add(action);
            }
            else
            {
                ObtainElement(true);//gather orb through casting
                LevelMagic(magic);
                return false;
            }
            return true;
        }

        public void GatherElement()
        {
            UserMagic magic = GetMagic(Spell.Meditation);

            if (magic == null) return;

            int MeditationLvl = magic.Level;

            magic = GetMagic(Spell.Concentration);

            int ConcentrateLvl = magic != null ? magic.Level : -1;

            int MeditateChance = 0;
            int ConcentrateChance = 0;

            if (Concentrating && !ConcentrateInterrupted && ConcentrateLvl >= 0)
                ConcentrateChance = 1 + ConcentrateLvl;

            if (MeditationLvl >= 0)
            {
                MeditateChance = (8 - MeditationLvl);
                int rnd = RandomUtils.Next(10);
                if (rnd >= (MeditateChance - ConcentrateChance))
                {
                    ObtainElement(false);
                    LevelMagic(GetMagic(Spell.Meditation));
                }
            }
        }
        public void ObtainElement(bool cast)
        {
            int orbType = 0;
            int meditateLevel = 0;

            UserMagic spell = GetMagic(Spell.Meditation);

            if (spell == null)
            {
                ReceiveChat("技能需要气功术.", ChatType.System);
                return;
            }

            meditateLevel = spell.Level;

            int maxOrbs = (int)Settings.OrbsExpList[Settings.OrbsExpList.Count - 1];

            if (cast)
            {
                ElementsLevel = (int)Settings.OrbsExpList[0];
                orbType = 1;
                if (Settings.GatherOrbsPerLevel)//Meditation Orbs per level
                    if (meditateLevel == 3)
                    {
                        Enqueue(new S.SetElemental { ObjectID = ObjectID, Enabled = true, Value = (uint)Settings.OrbsExpList[0], ElementType = 1, ExpLast = (uint)maxOrbs });
                        Broadcast(new S.SetObjectElemental { ObjectID = ObjectID, Enabled = true, Casted = true, Value = (uint)Settings.OrbsExpList[0], ElementType = 1, ExpLast = (uint)maxOrbs });
                        ElementsLevel = (int)Settings.OrbsExpList[1];
                        orbType = 2;
                    }

                HasElemental = true;
            }
            else
            {
                HasElemental = false;
                ElementsLevel++;

                if (Settings.GatherOrbsPerLevel)//Meditation Orbs per level
                    if (ElementsLevel > Settings.OrbsExpList[GetMagic(Spell.Meditation).Level])
                    {
                        HasElemental = true;
                        ElementsLevel = (int)Settings.OrbsExpList[GetMagic(Spell.Meditation).Level];
                        return;
                    }

                if (ElementsLevel >= Settings.OrbsExpList[0]) HasElemental = true;
                for (int i = 0; i <= Settings.OrbsExpList.Count - 1; i++)
                {
                    if (Settings.OrbsExpList[i] != ElementsLevel) continue;
                    orbType = i + 1;
                    break;
                }
            }

            Enqueue(new S.SetElemental { ObjectID = ObjectID, Enabled = HasElemental, Value = (uint)ElementsLevel, ElementType = (uint)orbType, ExpLast = (uint)maxOrbs });
            Broadcast(new S.SetObjectElemental { ObjectID = ObjectID, Enabled = HasElemental, Casted = cast, Value = (uint)ElementsLevel, ElementType = (uint)orbType, ExpLast = (uint)maxOrbs });
        }

        public int GetElementalOrbCount()
        {
            int OrbCount = 0;
            for (int i = Settings.OrbsExpList.Count - 1; i >= 0; i--)
            {
                if (ElementsLevel >= Settings.OrbsExpList[i])
                {
                    OrbCount = i + 1;
                    break;
                }
            }
            return OrbCount;
        }
        public int GetElementalOrbPower(bool defensive)
        {
            if (!HasElemental) return 0;

            if (defensive)
                return (int)Settings.OrbsDefList[GetElementalOrbCount() - 1];

            if (!defensive)
                return (int)Settings.OrbsDmgList[GetElementalOrbCount() - 1];

            return 0;
        }
        #endregion

        #region Wizard Skills
        private bool Fireball(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this) || !CanFly(target.CurrentLocation)) return false;

            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));

            int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target);

            //if(magic.Info.Spell == Spell.GreatFireBall && magic.Level >= 3 && target.Race == ObjectType.Monster)
            //{
            //    List<MapObject> targets = ((MonsterObject)target).FindAllNearby(3, target.CurrentLocation);

            //    int secondaryTargetCount = targets.Count > 3 ? 3 : targets.Count;

            //    for (int i = 0; i < secondaryTargetCount; i++)
            //    {
            //        if (!target.IsAttackTarget(this)) continue;
            //        DelayedAction action2 = new DelayedAction(DelayedType.Magic, Envir.Time + delay + 200, magic, damage / 2, targets[i]);
            //        ActionList.Add(action2);

            //        Enqueue(new S.Magic { Spell = magic.Info.Spell, TargetID = targets[i].ObjectID, Target = targets[i].CurrentLocation, Cast = true, Level = magic.Level });
            //        Broadcast(new S.ObjectMagic { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Spell = magic.Info.Spell, TargetID = targets[i].ObjectID, Target = targets[i].CurrentLocation, Cast = true, Level = magic.Level });
            //    }
            //}

            ActionList.Add(action);

            return true;
        }
        private void Repulsion(UserMagic magic)
        {
            bool result = false;
            for (int d = 0; d <= 1; d++)
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

                        for (int i = 0; CurrentMap.Objects[x, y] != null && i < CurrentMap.Objects[x, y].Count; i++)
                        {
                            MapObject ob = CurrentMap.Objects[x, y][i];
                            if (ob.Race != ObjectType.Monster && ob.Race != ObjectType.Player) continue;

                            if (!ob.IsAttackTarget(this) || ob.Level >= Level) continue;

                            if (RandomUtils.Next(20) >= 6 + magic.Level * 3 + Level - ob.Level) continue;

                            int distance = 1 + Math.Max(0, magic.Level - 1) + RandomUtils.Next(2);
                            MirDirection dir = Functions.DirectionFromPoint(CurrentLocation, ob.CurrentLocation);

                            if (ob.Pushed(this, dir, distance) == 0) continue;

                            if (ob.Race == ObjectType.Player)
                            {
                                SafeZoneInfo szi = CurrentMap.GetSafeZone(ob.CurrentLocation);

                                if (szi != null)
                                {
                                    ((PlayerObject)ob).BindLocation = szi.Location;
                                    ((PlayerObject)ob).BindMapIndex = CurrentMapIndex;
                                    ob.InSafeZone = true;
                                }
                                else
                                    ob.InSafeZone = false;

                                ob.Attacked(this, magic.GetDamage(0), DefenceType.None, false);
                            }
                            result = true;
                        }
                    }
                }
            }

            if (result) LevelMagic(magic);
        }
        //诱惑之光
        private void ElectricShock(MonsterObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return;

            //契约兽不能召唤
            if (target.PType != PetType.Common)
            {
                return;
            }

            if (RandomUtils.Next(4 - magic.Level) > 0)
            {
                if (RandomUtils.Next(2) == 0) LevelMagic(magic);
                return;
            }

            LevelMagic(magic);

            if (target.Master == this)
            {
                target.ShockTime = Envir.Time + (magic.Level * 5 + 10) * 1000;
                target.Target = null;
                return;
            }

            //驯服时间延长
            if (RandomUtils.Next(2) > 0)
            {
                target.ShockTime = Envir.Time + (magic.Level * 5 + 10) * 1000;
                target.Target = null;
                return;
            }
            //等级不够，不能驯服
            if (target.Level > Level + 2 || !target.Info.CanTame) return;

            //等级没有足够高，会暴走
            if (RandomUtils.Next(Level + 20 + magic.Level * 5) <= target.Level + 10)
            {
                if (RandomUtils.Next(5) > 0 && target.Master == null)
                {
                    target.RageTime = Envir.Time + (RandomUtils.Next(20) + 10) * 1000;
                    target.Target = null;
                }
                return;
            }


            //驯服数量大于技能等级+2（最多驯服5个）
            if(getPetCount(PetType.Common)>= magic.Level + 2)
            {
                return;
            }
            //驯服几率
            int rate = (int)(target.HP / 100);
            if (rate > 15)
            {
                rate = 15;
            }
            if (rate < 2)
            {
                rate = 2;
            }
            rate *= 2;

            if(hasItemSk(ItemSkill.Wizard1) || hasItemSk(ItemSkill.Wizard4))
            {
                rate = rate / 2;
            }

            if (RandomUtils.Next(rate) != 0) return;
            //else if (RandomUtils.Next(20) == 0) target.Die();

            //如果是满血的，有5分之1的几率直接死亡，否则1/10几率直接死亡
            int dieRate = 10;
            if (target.HP== target.MaxHP)
            {
                dieRate = 5;
            }
            //
            if (RandomUtils.Next(dieRate) == 0)
            {
                target.Die();
                return;
            }



            if (target.Master != null)
            {
                target.SetHP(target.MaxHP / 10);
                target.Master.Pets.Remove(target);
            }
            else if (target.Respawn != null)
            {
                target.Respawn.Count--;
                Envir.MonsterCount--;
                CurrentMap.MonsterCount--;
                target.Respawn = null;
            }

            target.Master = this;
            //target.HealthChanged = true;
            target.BroadcastHealthChange();
            Pets.Add(target);
            target.Target = null;
            target.RageTime = 0;
            target.ShockTime = 0;
            target.OperateTime = 0;
            target.MaxPetLevel = (byte)(1 + magic.Level * 2);
            //target.TameTime = Envir.Time + (Settings.Minute * 60);

            target.Broadcast(new S.ObjectName { ObjectID = target.ObjectID, Name = target.Name });
        }

        //计算当前拥有的宠物数量
        public byte getPetCount(PetType ptype)
        {
            byte count = 0;
            if (Pets == null)
            {
                return 0;
            }
            foreach(MonsterObject p in Pets)
            {
                if (p.Dead)
                {
                    continue;
                }
                if(ptype!= PetType.All && ptype != p.PType)
                {
                    continue;
                }
                count++;
            }
            return count;
        }

        //是否具有某个契约兽技能
        public bool hasMonSk(MyMonSkill sk)
        {
            if (Pets == null)
            {
                return false;
            }
            foreach (MonsterObject p in Pets)
            {
                if (p.Dead)
                {
                    continue;
                }
                if (p.hasMonSk(sk))
                {
                    return true;
                }
            }
            return false;
        }

        private void HellFire(UserMagic magic)
        {
            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation, Direction, 4);
            CurrentMap.ActionList.Add(action);

            if (magic.Level != 3) return;

            MirDirection dir = (MirDirection)(((int)Direction + 1) % 8);
            action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation, dir, 4);
            CurrentMap.ActionList.Add(action);

            dir = (MirDirection)(((int)Direction - 1 + 8) % 8);
            action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation, dir, 4);
            CurrentMap.ActionList.Add(action);
        }
        //雷电，对不死系的伤害增加那么多？
        private void ThunderBolt(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return;

            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));

            if (target.Undead) damage = (int)(damage * 1.5F);

            if (hasItemSk(ItemSkill.Wizard2))
            {
                byte count = 0;
                List < MapObject > list= this.CurrentMap.getMapObjects(target.CurrentLocation.X, target.CurrentLocation.Y,1);
                foreach(MapObject ob in list)
                {
                    if (ob == null)
                    {
                        continue;
                    }
                    if (ob.Race!=ObjectType.Monster && ob.Race != ObjectType.Player)
                    {
                        continue;
                    }
                    if (!ob.IsAttackTarget(this))
                    {
                        continue;
                    }
                    DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, damage, ob);
                    ActionList.Add(action);
                    count++;
                    if (count > 3)
                    {
                        break;
                    }
                }
            }
            else
            {
                DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, damage, target);
                ActionList.Add(action);
            }
        }
        private void Vampirism(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return;

            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, damage, target);

            ActionList.Add(action);
        }
        private void FireBang(UserMagic magic, Point location)
        {
            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, location);
            CurrentMap.ActionList.Add(action);
        }
        //火墙
        private void FireWall(UserMagic magic, Point location)
        {
            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, location);
            CurrentMap.ActionList.Add(action);
        }
        private void Lightning(UserMagic magic)
        {
            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation, Direction);
            CurrentMap.ActionList.Add(action);
        }
        //圣言术
        private void TurnUndead(MapObject target, UserMagic magic)
        {
            //圣言术，只对不死系怪物有效
            if (target == null || target.Race != ObjectType.Monster || !target.Undead || !target.IsAttackTarget(this)) return;

            if (RandomUtils.Next(2) + Level - 1 <= target.Level)
            {
                target.Target = this;
                return;
            }

            int dif = Level - target.Level + 15;

            if (RandomUtils.Next(100) >= (magic.Level + 1 << 3) + dif)
            {
                target.Target = this;
                return;
            }

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, target);
            ActionList.Add(action);
        }
        private void FlameDisruptor(MapObject target, UserMagic magic)
        {
            if (target == null || (target.Race != ObjectType.Player && target.Race != ObjectType.Monster) || !target.IsAttackTarget(this)) return;

            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));

            if (!target.Undead) damage = (int)(damage * 1.5F);

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, damage, target);

            ActionList.Add(action);
        }
        private void ThunderStorm(UserMagic magic)
        {
            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));
            
            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation);
            CurrentMap.ActionList.Add(action);
        }
        private void Mirroring(UserMagic magic)
        {
            MonsterObject monster;
            DelayedAction action;
            for (int i = 0; i < Pets.Count; i++)
            {
                monster = Pets[i];
                if ((monster.Info.Name != Settings.CloneName) || monster.Dead) continue;
                if (monster.Node == null) continue;
                action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, monster, Front, true);
                CurrentMap.ActionList.Add(action);
                return;
            }

            MonsterInfo info = Envir.GetMonsterInfo(Settings.CloneName);
            if (info == null) return;
            if (hasItemSk(ItemSkill.Wizard5))
            {
                info = info.Clone();
                info.MaxAC = (ushort)(info.MaxAC * 2);
                info.MaxMAC = (ushort)(info.MaxMAC * 2);
                info.MaxDC = (ushort)(info.MaxDC * 2);
                info.MaxMC = (ushort)(info.MaxMC * 2);
            }

            LevelMagic(magic);

            monster = MonsterObject.GetMonster(info);
            monster.Master = this;
            monster.ActionTime = Envir.Time + 1000;
            monster.RefreshNameColour(false);

            Pets.Add(monster);

            action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, monster, Front, false);
            CurrentMap.ActionList.Add(action);
        }
        private void Blizzard(UserMagic magic, Point location, out bool cast)
        {
            cast = false;

            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));

            //冰雨几率不卡顿
            if (hasItemSk(ItemSkill.Wizard7) && RandomUtils.Next(100) < 20)
            {
                Enqueue(new S.BlizzardStopTime() { stopTime = 500 });
            }

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, location);

            ActiveBlizzard = true;
            CurrentMap.ActionList.Add(action);
            cast = true;
        }
        private void MeteorStrike(UserMagic magic, Point location, out bool cast)
        {
            cast = false;

            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));
            //火雨几率不卡顿
            if (hasItemSk(ItemSkill.Wizard7) && RandomUtils.Next(100) < 20)
            {
                Enqueue(new S.BlizzardStopTime() { stopTime=500 });
            }

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, location);

            ActiveBlizzard = true;
            CurrentMap.ActionList.Add(action);
            cast = true;
        }

        private void IceThrust(UserMagic magic)
        {
            int damageBase = GetAttackPower(MinMC, MaxMC);
            if (RandomUtils.Next(100) <= (1 + Luck))
                damageBase += damageBase;
            int damageFinish = magic.GetDamage(damageBase);

            Point location = Functions.PointMove(CurrentLocation, Direction, 1);

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 1500, this, magic, location, Direction, damageFinish, (int)(damageFinish * 0.6));

            CurrentMap.ActionList.Add(action);
        }

        private void MagicBooster(UserMagic magic)
        {
            int bonus = 6 + magic.Level * 6;

            ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, bonus));
        }

        #endregion

        #region Taoist Skills
        private void Healing(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsFriendlyTarget(this)) return;

            int health = magic.GetDamage(GetAttackPower(MinSC, MaxSC) * 2) + Level;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, health, target);

            ActionList.Add(action);
        }
        //释放施毒术
        private bool Poisoning(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return false;
            UserItem item = null;
            if (MagicParameter == 1)
            {
                item = GetPoison(1, MagicParameter);
            }
            else if (MagicParameter == 2)
            {
                item = GetPoison(1, MagicParameter);
            }
            MagicParameter = 0;
            if (item == null)
            {
                item = GetPoison(1);
            }
            if (item == null) return false;

            int power = magic.GetDamage(GetAttackPower(MinSC, MaxSC));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, power, target, item);
            ActionList.Add(action);
            ConsumeItem(item, 1);
            return true;
        }
        private bool SoulFireball(MapObject target, UserMagic magic, out bool cast)
        {
            cast = false;
            UserItem item = GetAmulet(1);
            if (item == null) return false;
            cast = true;

            if (target == null || !target.IsAttackTarget(this) || !CanFly(target.CurrentLocation)) return false;

            int damage = magic.GetDamage(GetAttackPower(MinSC, MaxSC));
            if (hasItemSk(ItemSkill.Taoist1) )
            {
                damage = damage*15/10;
            }

            int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target);

            ActionList.Add(action);
            ConsumeItem(item, 1);

            return true;
        }
        //召唤骷髅
        private void SummonSkeleton(UserMagic magic)
        {
            MonsterObject monster;
            for (int i = 0; i < Pets.Count; i++)
            {
                monster = Pets[i];
                if ((!monster.Info.Name.StartsWith(Settings.SkeletonName)) || monster.Dead) continue;
                if (monster.Node == null) continue;
                monster.ActionList.Add(new DelayedAction(DelayedType.Recall, Envir.Time + 500));
                return;
            }
            if (getPetCount(PetType.Common) > 1)
            {
                return;
            }
            //if (Pets.Where(x => x.Race == ObjectType.Monster).Count() > 1) return;

            UserItem item = GetAmulet(1);
            if (item == null) return;

            MonsterInfo info = Envir.GetMonsterInfo(Settings.SkeletonName);
            if (info == null) return;

            if (hasItemSk(ItemSkill.Taoist2))
            {
                MonsterInfo _info = Envir.GetMonsterInfo(Settings.SkeletonName +"2");

                if (_info != null)
                {
                    info = _info.Clone();
                }
                ushort _skCount = this.skCount;
                if (_skCount > 50)
                {
                    _skCount = 50;
                }
                _skCount = (ushort)(_skCount / 2);
                info.MaxMAC += _skCount;
                info.MaxAC += _skCount;
                info.MaxDC += _skCount;
            }
            LevelMagic(magic);
            ConsumeItem(item, 1);

            monster = MonsterObject.GetMonster(info);
            monster.PetLevel = magic.Level;
            monster.MaxPetLevel = (byte)(4 + magic.Level);
            if (hasItemSk(ItemSkill.Taoist2))
            {
                monster.PetLevel = monster.MaxPetLevel;
            }
            monster.Master = this;
            monster.ActionTime = Envir.Time + 1000;
            monster.RefreshNameColour(false);

            //Pets.Add(monster);

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, monster, Front);
            CurrentMap.ActionList.Add(action);
        }
        private void Purification(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsFriendlyTarget(this)) return;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, target);

            ActionList.Add(action);
        }
        private void SummonShinsu(UserMagic magic)
        {
            MonsterObject monster;
            for (int i = 0; i < Pets.Count; i++)
            {
                monster = Pets[i];
                if ((monster.Info.Name.IndexOf(Settings.ShinsuName)==-1) || monster.Dead) continue;
                if (monster.Node == null) continue;
                monster.ActionList.Add(new DelayedAction(DelayedType.Recall, Envir.Time + 500));
                return;
            }
            if (getPetCount(PetType.Common) > 1)
            {
                return;
            }
            //if (Pets.Where(x => x.Race == ObjectType.Monster).Count() > 1) return;

            UserItem item = GetAmulet(5);
            if (item == null) return;

            MonsterInfo info = Envir.GetMonsterInfo(Settings.ShinsuName);
            if (info == null) return;

            if (hasItemSk(ItemSkill.Taoist4))
            {
                info = Envir.GetMonsterInfo("变异神兽");
                info = info.Clone();
                //info.MaxMAC = (ushort)(info.MaxMAC * 2);
                //info.MaxAC = (ushort)(info.MaxAC * 2);
                //info.MaxDC = (ushort)(info.MaxDC * 2);
                //info.MaxMC = (ushort)(info.MaxMC * 2);
                //info.MaxSC = (ushort)(info.MaxSC * 2);
                //叠加层次
                ushort _skCount = this.skCount;
                if (_skCount > 50)
                {
                    _skCount = 50;
                }
                info.MaxAC += _skCount;
                info.MaxMAC += _skCount;

                info.MaxDC += _skCount;
                //info.MinDC = (ushort)(info.MaxDC / 2);

                info.MaxMC += _skCount;
                //info.MinMC = (ushort)(info.MaxMC / 2);
                info.MaxSC += _skCount;
            }


            LevelMagic(magic);
            ConsumeItem(item, 5);


            monster = MonsterObject.GetMonster(info);
            monster.PetLevel = magic.Level;
            monster.MaxPetLevel = (byte)(1 + magic.Level * 2);
            if (hasItemSk(ItemSkill.Taoist4))
            {
                monster.PetLevel = monster.MaxPetLevel;
            }
            monster.Master = this;
            monster.Direction = Direction;
            monster.ActionTime = Envir.Time + 1000;
   
            //Pets.Add(monster);

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, monster, Front);
            CurrentMap.ActionList.Add(action);
        }
        private void Hiding(UserMagic magic)
        {
            UserItem item = GetAmulet(1);
            if (item == null) return;

            ConsumeItem(item, 1);

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, GetAttackPower(MinSC, MaxSC) + (magic.Level + 1) * 5);
            ActionList.Add(action);

        }
        private void MassHiding(UserMagic magic, Point location, out bool cast)
        {
            cast = false;
            UserItem item = GetAmulet(1);
            if (item == null) return;
            cast = true;

            int delay = Functions.MaxDistance(CurrentLocation, location) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, this, magic, GetAttackPower(MinSC, MaxSC) / 2 + (magic.Level + 1) * 2, location);
            CurrentMap.ActionList.Add(action);
        }
        private void SoulShield(UserMagic magic, Point location, out bool cast)
        {
            cast = false;
            UserItem item = GetAmulet(1);
            if (item == null) return;
            cast = true;

            int delay = Functions.MaxDistance(CurrentLocation, location) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, this, magic, GetAttackPower(MinSC, MaxSC) * 2 + (magic.Level + 1) * 10, location);
            CurrentMap.ActionList.Add(action);

            ConsumeItem(item, 1);
        }
        private void MassHealing(UserMagic magic, Point location)
        {
            int value = magic.GetDamage(GetAttackPower(MinSC, MaxSC));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, value, location);
            CurrentMap.ActionList.Add(action);
        }
        private void Revelation(MapObject target, UserMagic magic)
        {
            if (target == null) return;

            int value = GetAttackPower(MinSC, MaxSC) + magic.GetPower();

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, value, target);

            ActionList.Add(action);
        }
        private void PoisonCloud(UserMagic magic, Point location, out bool cast)
        {
            cast = false;

            UserItem amulet = GetAmulet(5);
            if (amulet == null) return;

            UserItem poison = GetPoison(5, 1);
            if (poison == null) return;

            int delay = Functions.MaxDistance(CurrentLocation, location) * 50 + 500; //50 MS per Step
            int damage = magic.GetDamage(GetAttackPower(MinSC, MaxSC));
            

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, this, magic, damage, location, (byte)RandomUtils.Next(PoisonAttack+1));

            ConsumeItem(amulet, 5);
            ConsumeItem(poison, 5);

            CurrentMap.ActionList.Add(action);
            cast = true;
        }
        private void TrapHexagon(UserMagic magic, MapObject target, out bool cast)
        {
            cast = false;

            if (target == null || !target.IsAttackTarget(this) || !(target is MonsterObject)) return;
            if (target.Level > Level + 2) return;

            UserItem item = GetAmulet(1);
            Point location = target.CurrentLocation;

            if (item == null) return;

            LevelMagic(magic);
            uint duration = (uint)((magic.Level * 5 + 10) * 1000);
            int value = (int)duration;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, value, location);
            CurrentMap.ActionList.Add(action);

            ConsumeItem(item, 1);
            cast = true;
        }

        //阴阳五行阵
        private void HealingCircle(UserMagic magic, MapObject target, out bool cast)
        {
            cast = false;
            UserItem amulet = this.GetAmulet(5, 0);
            bool flag = amulet == null;
            if (!flag)
            {
                this.LevelMagic(magic);
                int damage = magic.GetDamage(base.GetAttackPower((int)this.MinSC, (int)this.MaxSC));
                DelayedAction item = new DelayedAction(DelayedType.Magic, MapObject.Envir.Time + 500L, new object[]
                {
                    this,
                    magic,
                    damage,
                    this.CurrentLocation
                });
                base.CurrentMap.ActionList.Add(item);
                this.ConsumeItem(amulet, 5);
                cast = true;
            }
        }

        private void Reincarnation(UserMagic magic, PlayerObject target, out bool cast)
        {
            cast = true;

            if (target == null || !target.Dead) return;


            // checks for amulet of revival
            UserItem item = GetAmulet(1, 3);
            if (item == null)
            {
                ReceiveChat("没有复活符，无法复活.", ChatType.System);
                return;
            }
            

            if (!ActiveReincarnation && !ReincarnationReady)
            {
                cast = false;
                int CastTime = Math.Abs(((magic.Level + 1) * 1000) - 9000);
                ExpireTime = Envir.Time + CastTime;
                ReincarnationReady = true;
                ActiveReincarnation = true;
                ReincarnationTarget = target;
                ReincarnationExpireTime = ExpireTime + 5000;

                target.ReincarnationHost = this;

                SpellObject ob = new SpellObject
                {
                    Spell = Spell.Reincarnation,
                    ExpireTime = ExpireTime,
                    TickSpeed = 1000,
                    Caster = this,
                    CurrentLocation = CurrentLocation,
                    CastLocation = CurrentLocation,
                    Show = true,
                    CurrentMap = CurrentMap,
                };
                Packet p = new S.Chat { Message = string.Format("{0} 正在尝试复活 {1}", Name, target.Name), Type = ChatType.Shout };

                for (int i = 0; i < CurrentMap.Players.Count; i++)
                {
                    if (!Functions.InRange(CurrentLocation, CurrentMap.Players[i].CurrentLocation, Globals.DataRange * 2)) continue;
                    CurrentMap.Players[i].Enqueue(p);
                }

                CurrentMap.AddObject(ob);
                ob.Spawned();
                ConsumeItem(item, 1);

                // chance of failing Reincarnation when casting
                //这里改下，增加失败几率？
                if (RandomUtils.Next(30) > (1 + magic.Level) * 10)
                {
                    return;
                }

                DelayedAction action = new DelayedAction(DelayedType.Magic, ExpireTime, magic);
                ActionList.Add(action);

                return;
            }
            return;
        }
        private void SummonHolyDeva(UserMagic magic)
        {
            MonsterObject monster;
            for (int i = 0; i < Pets.Count; i++)
            {
                monster = Pets[i];
                if ((monster.Info.Name != Settings.AngelName) || monster.Dead) continue;
                if (monster.Node == null) continue;
                monster.ActionList.Add(new DelayedAction(DelayedType.Recall, Envir.Time + 500));
                return;
            }
            if (getPetCount(PetType.Common) > 1)
            {
                return;
            }
            //if (Pets.Where(x => x.Race == ObjectType.Monster).Count() > 1) return;

            UserItem item = GetAmulet(2);
            if (item == null) return;


            MonsterInfo info = Envir.GetMonsterInfo(Settings.AngelName);
            if (info == null) return;

            LevelMagic(magic);
            ConsumeItem(item, 2);

            monster = MonsterObject.GetMonster(info);
            monster.PetLevel = magic.Level;
            monster.Master = this;
            monster.MaxPetLevel = (byte)(1 + magic.Level * 2);
            monster.Direction = Direction;
            monster.ActionTime = Envir.Time + 1000;

            //Pets.Add(monster);

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 1500, this, magic, monster, Front);
            CurrentMap.ActionList.Add(action);
        }
        private void Hallucination(MapObject target, UserMagic magic)
        {
            if (target == null || target.Race != ObjectType.Monster || !target.IsAttackTarget(this)) return;

            int damage = 0;
            int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, delay, magic, damage, target);

            ActionList.Add(action);
        }
        private void EnergyShield(MapObject target, UserMagic magic, out bool cast)
        {
            cast = false;
            
            if (target == null || !target.IsFriendlyTarget(this)) target = this; //offical is only party target

            int duration = 30 + 50 * magic.Level;
            int power = magic.GetPower(GetAttackPower(MinSC, MaxSC));
            int chance = 9 - (Luck / 3 + magic.Level);

            int[] values = { chance < 2 ? 2 : chance, power };

            switch (target.Race)
            {
                case ObjectType.Player:
                    //Only targets
                    if (target.IsFriendlyTarget(this))
                    {
                        target.AddBuff(new Buff { Type = BuffType.EnergyShield, Caster = this, ExpireTime = Envir.Time + duration * 1000, Visible = true, Values = values });
                        target.OperateTime = 0;
                        LevelMagic(magic);
                        cast = true;
                    }
                    break;
            }
        }
        private void UltimateEnhancer(MapObject target, UserMagic magic, out bool cast)
        {
            cast = false;

            if (target == null || !target.IsFriendlyTarget(this)) return;
            UserItem item = GetAmulet(1);
            if (item == null) return;

            long expiretime = GetAttackPower(MinSC, MaxSC) * 2 + (magic.Level + 1) * 10;
            int value = MaxSC >= 5 ? Math.Min(8, MaxSC / 5) : 1;

            switch (target.Race)
            {
                case ObjectType.Monster:
                case ObjectType.Player:
                    //Only targets
                    if (target.IsFriendlyTarget(this))
                    {
                        target.AddBuff(new Buff { Type = BuffType.UltimateEnhancer, Caster = this, ExpireTime = Envir.Time + expiretime * 1000, Values = new int[] { value } });
                        target.OperateTime = 0;
                        LevelMagic(magic);
                        ConsumeItem(item, 1);
                        cast = true;
                    }
                    break;
            }
        }
        //瘟疫
        private void Plague(UserMagic magic, Point location, out bool cast)
        {
            cast = false;
            UserItem item = GetAmulet(1);
            if (item == null) return;
            cast = true;

            int delay = Functions.MaxDistance(CurrentLocation, location) * 50 + 500; //50 MS per Step


            PoisonType pType = PoisonType.None;

            UserItem itemp = GetPoison(1, 1);

            if (itemp != null)
                pType = PoisonType.Green;
            else
            {
                itemp = GetPoison(1, 2);

                if (itemp != null)
                    pType = PoisonType.Red;
            }
            //上调瘟疫伤害
            int Damage = magic.GetDamage(GetAttackPower(MinSC, MaxSC));
            if (hasItemSk(ItemSkill.Taoist7))
            {
                Damage = Damage * 13 / 10;
            }
            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, this, magic, Damage, location, pType);
            CurrentMap.ActionList.Add(action);
            ConsumeItem(item, 1);
            if (itemp != null) ConsumeItem(itemp, 1);
        }
        private void Curse(UserMagic magic, Point location, out bool cast)
        {
            cast = false;
            UserItem item = GetAmulet(1);
            if (item == null) return;
            cast = true;

            ConsumeItem(item, 1);

            if (RandomUtils.Next(10 - ((magic.Level + 1) * 2)) > 2) return;

            int delay = Functions.MaxDistance(CurrentLocation, location) * 50 + 500; //50 MS per Step
            //加强一点诅咒术，之前是3级降9%，现在改为3级降17
            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, this, magic, magic.GetDamage(GetAttackPower(MinSC, MaxSC)), location, 1 + ((magic.Level + 1) * 4));
            CurrentMap.ActionList.Add(action);

        }


        private void PetEnhancer(MapObject target, UserMagic magic, out bool cast,Point p)
        {
            cast = false;
            //血龙水，如果没有目标，则目标为最靠近的宝宝
            if (target == null || target.Race != ObjectType.Monster || !target.IsFriendlyTarget(this))
            {
                int dis = 10;
                List<MapObject>  listobj = CurrentMap.getMapObjects(p.X, p.Y, 7);
                foreach(MapObject ob in listobj)
                {
                    if (ob == null || ob.Race != ObjectType.Monster || !ob.IsFriendlyTarget(this))
                    {
                        continue;
                    }
                    int _dis = Functions.MaxDistance(p, ob.CurrentLocation);
                    if(_dis< dis)
                    {
                        dis = _dis;
                        target = ob;
                    }
                }
            }

            if (target == null || target.Race != ObjectType.Monster || !target.IsFriendlyTarget(this)) return;

            int duration = GetAttackPower(MinSC, MaxSC) + magic.GetPower();

            cast = true;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, duration, target);

            ActionList.Add(action);
        }
        #endregion

        #region Warrior Skills
        private void Entrapment(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return;

            int damage = 0;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic, damage, target);

            ActionList.Add(action);
        }
        private void BladeAvalanche(UserMagic magic)
        {
            int damageBase = GetAttackPower(MinDC, MaxDC);
            if (RandomUtils.Next(0,100) <= (1+Luck)) 
                damageBase += damageBase;//crit should do something like double dmg, not double max dc dmg!
            int damageFinal = magic.GetDamage(damageBase);

            int col = 3;
            int row = 3;

            Point[] loc = new Point[col]; //0 = left 1 = center 2 = right
            loc[0] = Functions.PointMove(CurrentLocation, Functions.PreviousDir(Direction), 1);
            loc[1] = Functions.PointMove(CurrentLocation, Direction, 1);
            loc[2] = Functions.PointMove(CurrentLocation, Functions.NextDir(Direction), 1);

            for (int i = 0; i < col; i++)
            {
                Point startPoint = loc[i];
                for (int j = 0; j < row; j++)
                {
                    Point hitPoint = Functions.PointMove(startPoint, Direction, j);

                    if (!CurrentMap.ValidPoint(hitPoint)) continue;

                    //Cell cell = CurrentMap.GetCell(hitPoint);

                    if (CurrentMap.Objects[hitPoint.X, hitPoint.Y] == null) continue;

                    for (int k = 0; k < CurrentMap.Objects[hitPoint.X, hitPoint.Y].Count; k++)
                    {
                        MapObject target = CurrentMap.Objects[hitPoint.X, hitPoint.Y][k];
                        switch (target.Race)
                        {
                            case ObjectType.Monster:
                            case ObjectType.Player:
                                //Only targets
                                if (target.IsAttackTarget(this))
                                {
                                    if (target.Attacked(this, j <= 1 ? damageFinal : (int)(damageFinal * 0.6), DefenceType.MAC, false) > 0)
                                        LevelMagic(magic);
                                }
                                break;
                        }
                    }
                }
            }
        }
        private void ProtectionField(UserMagic magic)
        {
            int count = Buffs.Where(x => x.Type == BuffType.ProtectionField).ToList().Count();
            if (count > 0) return;

            int duration = 45 + (15 * magic.Level);
            int value = (int)Math.Round(MaxAC * (0.2 + (0.03 * magic.Level)));
            if (hasItemSk(ItemSkill.Warrior5))
            {
                value = value + 15;
            }
            AddBuff(new Buff { Type = BuffType.ProtectionField, Caster = this, ExpireTime = Envir.Time + duration * 1000, Values = new int[] { value } });
            OperateTime = 0;
            LevelMagic(magic);
        }
        private void Rage(UserMagic magic)
        {
            int count = Buffs.Where(x => x.Type == BuffType.Rage).ToList().Count();
            if (count > 0) return;

            int duration = 48 + (6 * magic.Level);
            int value = (int)Math.Round(MaxDC * (0.12 + (0.03 * magic.Level)));

            AddBuff(new Buff { Type = BuffType.Rage, Caster = this, ExpireTime = Envir.Time + duration * 1000, Values = new int[] { value } });
            OperateTime = 0;
            LevelMagic(magic);
        }
        //野蛮冲撞
        private void ShoulderDash(UserMagic magic)
        {
            if (InTrapRock) return;
            if (!CanWalk) return;
            ActionTime = Envir.Time + MoveDelay;

            int dist = RandomUtils.Next(2) + magic.Level + 2;
            int travel = 0;
            bool wall = true;
            Point location = CurrentLocation;
            MapObject target = null;
            for (int i = 0; i < dist; i++)
            {
                location = Functions.PointMove(location, Direction, 1);

                if (!CurrentMap.ValidPoint(location)) break;

                //Cell cell = CurrentMap.GetCell(location);

                bool blocking = false;

                if (InSafeZone) blocking = true;

                SafeZoneInfo szi = CurrentMap.GetSafeZone(location);

                if (szi != null)
                {
                    blocking = true;
                }

                if (CurrentMap.Objects[location.X, location.Y] != null)
                {
                    for (int c = CurrentMap.Objects[location.X, location.Y].Count - 1; c >= 0; c--)
                    {
                        MapObject ob = CurrentMap.Objects[location.X, location.Y][c];
                        if (!ob.Blocking) continue;
                        wall = false;
                        if (ob.Race != ObjectType.Monster && ob.Race != ObjectType.Player)
                        {
                            blocking = true;
                            break;
                        }

                        if (target == null && ob.Race == ObjectType.Player)
                            target = ob;

                        if (RandomUtils.Next(20) >= 6 + magic.Level * 3 + Level - ob.Level || !ob.IsAttackTarget(this) || ob.Level >= Level || ob.Pushed(this, Direction, 1) == 0)
                        {
                            if (target == ob)
                                target = null;
                            blocking = true;
                            break;
                        }

                        if (CurrentMap.Objects[location.X, location.Y] == null) break;

                    }
                }

                if (blocking)
                {
                    if (magic.Level != 3) break;

                    Point location2 = Functions.PointMove(location, Direction, 1);

                    if (!CurrentMap.ValidPoint(location2)) break;

                    szi = CurrentMap.GetSafeZone(location2);

                    if (szi != null)
                    {
                        break;
                    }

                    //cell = CurrentMap.GetCell(location2);

                    blocking = false;


                    if (CurrentMap.Objects[location2.X, location2.Y] != null)
                    {
                        for (int c = CurrentMap.Objects[location2.X, location2.Y].Count - 1; c >= 0; c--)
                        {
                            MapObject ob = CurrentMap.Objects[location2.X, location2.Y][c];
                            if (!ob.Blocking) continue;
                            if (ob.Race != ObjectType.Monster && ob.Race != ObjectType.Player)
                            {
                                blocking = true;
                                break;
                            }

                            if (!ob.IsAttackTarget(this) || ob.Level >= Level || ob.Pushed(this, Direction, 1) == 0)
                            {
                                blocking = true;
                                break;
                            }

                            if (CurrentMap.Objects[location2.X, location2.Y] == null) break;
                        }
                    }

                    if (blocking) break;

                    //cell = CurrentMap.GetCell(location);

                    if (CurrentMap.Objects[location.X, location.Y] != null)
                    {
                        for (int c = CurrentMap.Objects[location.X, location.Y].Count - 1; c >= 0; c--)
                        {
                            MapObject ob = CurrentMap.Objects[location.X, location.Y][c];
                            if (!ob.Blocking) continue;
                            if (ob.Race != ObjectType.Monster && ob.Race != ObjectType.Player)
                            {
                                blocking = true;
                                break;
                            }

                            if (RandomUtils.Next(20) >= 6 + magic.Level * 3 + Level - ob.Level || !ob.IsAttackTarget(this) || ob.Level >= Level || ob.Pushed(this, Direction, 1) == 0)
                            {
                                blocking = true;
                                break;
                            }

                            if (CurrentMap.Objects[location.X, location.Y] == null) break;
                        }
                    }

                    if (blocking) break;
                }

                travel++;
                CurrentMap.Remove(this);
                RemoveObjects(Direction, 1);

                CurrentLocation = location;


                

                Enqueue(new S.UserDash { Direction = Direction, Location = location });
                Broadcast(new S.ObjectDash { ObjectID = ObjectID, Direction = Direction, Location = location });

                CurrentMap.Add(this);
                AddObjects(Direction, 1);
            }

            if (travel > 0 && !wall)
            {

                if (target != null) target.Attacked(this, magic.GetDamage(0), DefenceType.None, false);
                LevelMagic(magic);
            }

            if (travel > 0)
            {
                SafeZoneInfo szi = CurrentMap.GetSafeZone(CurrentLocation);

                if (szi != null)
                {
                    BindLocation = szi.Location;
                    BindMapIndex = CurrentMapIndex;
                    InSafeZone = true;
                }
                else
                    InSafeZone = false;

                ActionTime = Envir.Time + (travel * MoveDelay / 2);

                //Cell cell = CurrentMap.GetCell(CurrentLocation);
                for (int i = 0; i < CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y].Count; i++)
                {
                    if (CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i].Race != ObjectType.Spell) continue;
                    SpellObject ob = (SpellObject)CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i];

                    if (ob.Spell != Spell.FireWall || !IsAttackTarget(ob.Caster)) continue;
                    Attacked(ob.Caster, ob.Value, DefenceType.MAC, false);
                    break;
                }
            }

            if (travel == 0 || wall && dist != travel)
            {
                if (travel > 0)
                {
                    Enqueue(new S.UserDash { Direction = Direction, Location = Front });
                    Broadcast(new S.ObjectDash { ObjectID = ObjectID, Direction = Direction, Location = Front });
                }
                else
                    Broadcast(new S.ObjectDash { ObjectID = ObjectID, Direction = Direction, Location = Front });

                Enqueue(new S.UserDashFail { Direction = Direction, Location = CurrentLocation });
                Broadcast(new S.ObjectDashFail { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
                ReceiveChat("推动力不足.", ChatType.System);
            }


            magic.CastTime = Envir.Time;
            _stepCounter = 0;
            //ActionTime = Envir.Time + GetDelayTime(MoveDelay);

            Enqueue(new S.MagicCast { Spell = magic.Spell });

            CellTime = Envir.Time + 500;
        }
        private void SlashingBurst(UserMagic magic, out bool cast)
        {
            cast = true;

            // damage
            int damageBase = GetAttackPower(MinDC, MaxDC);
            int damageFinal = magic.GetDamage(damageBase);
            if (hasItemSk(ItemSkill.Warrior7) && RandomUtils.Next(100) < 35)
            {
                damageFinal = damageFinal * 13 / 10;
            }
            // objects = this, magic, damage, currentlocation, direction, attackRange
            //加快释放时间，与距离
            //DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damageFinal, CurrentLocation, Direction, 1);
            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 400, this, magic, damageFinal, CurrentLocation, Direction, 1);
            CurrentMap.ActionList.Add(action);

            // telpo location
            Point location = Functions.PointMove(CurrentLocation, Direction, 2);

            if (!CurrentMap.ValidPoint(location)) return;

            //Cell cInfo = CurrentMap.GetCell(location);

            bool blocked = false;
            if (CurrentMap.Objects[location.X, location.Y] != null)
            {
                for (int c = 0; c < CurrentMap.Objects[location.X, location.Y].Count; c++)
                {
                    MapObject ob = CurrentMap.Objects[location.X, location.Y][c];
                    if (!ob.Blocking) continue;
                    blocked = true;
                    if ((CurrentMap.Objects[location.X, location.Y] == null) || blocked) break;
                }
            }

            // blocked telpo cancel
            if (blocked) return;

            Teleport(CurrentMap, location, false);

            //// move character
            //CurrentMap.GetCell(CurrentLocation).Remove(this);
            //RemoveObjects(Direction, 1);

            //CurrentLocation = location;

            //CurrentMap.GetCell(CurrentLocation).Add(this);
            //AddObjects(Direction, 1);

            //Enqueue(new S.UserAttackMove { Direction = Direction, Location = location });
        }
        private void FurySpell(UserMagic magic, out bool cast)
        {
            cast = true;

            ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic));
        }
        private void ImmortalSkin(UserMagic magic, out bool cast)
        {
            cast = true;

            ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 500, magic));         

        }
        private void CounterAttackCast(UserMagic magic, MapObject target)
        {
            if (target == null || magic == null) return;

            if (CounterAttack == false) return;

            int damageBase = GetAttackPower(MinDC, MaxDC);
            if (RandomUtils.Next(0, 100) <= Accuracy)
                damageBase += damageBase;//crit should do something like double dmg, not double max dc dmg!
            int damageFinal = magic.GetDamage(damageBase);


            MirDirection dir = Functions.ReverseDirection(target.Direction);
            Direction = dir;

            if (Functions.InRange(CurrentLocation, target.CurrentLocation, 1) == false) return;
            if (RandomUtils.Next(10) > magic.Level + 6) return;
            Enqueue(new S.ObjectMagic { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Spell = Spell.CounterAttack, TargetID = target.ObjectID, Target = target.CurrentLocation, Cast = true, Level = GetMagic(Spell.CounterAttack).Level, SelfBroadcast = true });
            DelayedAction action = new DelayedAction(DelayedType.Damage, AttackTime, target, damageFinal, DefenceType.AC, true);
            ActionList.Add(action);
            LevelMagic(magic);
            CounterAttack = false;
        }
        #endregion

        #region Assassin Skills

        private void HeavenlySword(UserMagic magic)
        {
            int damage = magic.GetDamage(GetAttackPower(MinDC, MaxDC));

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation, Direction);
            CurrentMap.ActionList.Add(action);
        }
        private void SwiftFeet(UserMagic magic, out bool cast)
        {
            cast = true;

            AddBuff(new Buff { Type = BuffType.SwiftFeet, Caster = this, ExpireTime = Envir.Time + 25000 + magic.Level * 5000, Values = new int[] { 1 }, Visible = true });
            LevelMagic(magic);
        }
        private void MoonLight(UserMagic magic)
        {
            for (int i = 0; i < Buffs.Count; i++)
                if (Buffs[i].Type == BuffType.MoonLight) return;

            int etime = (GetAttackPower(MinAC, MaxAC) + (magic.Level + 1) * 5) * 500;
            if (hasItemSk(ItemSkill.Assassin1))
            {
                etime = etime * 15 / 10;
            }
            AddBuff(new Buff { Type = BuffType.MoonLight, Caster = this, ExpireTime = Envir.Time + etime, Visible = true });
            LevelMagic(magic);
        }
        private void Trap(UserMagic magic, MapObject target, out bool cast)
        {
            cast = false;

            if (target == null || !target.IsAttackTarget(this) || !(target is MonsterObject)) return;
            if (target.Level >= Level + 2) return;

            Point location = target.CurrentLocation;

            LevelMagic(magic);
            uint duration = 60000;
            int value = (int)duration;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, value, location);
            CurrentMap.ActionList.Add(action);
            cast = true;
        }
        private bool PoisonSword(UserMagic magic)
        {
            UserItem item = GetPoison(1);
            if (item == null) return false;

            Point hitPoint;
            //Cell cell;
            MirDirection dir = Functions.PreviousDir(Direction);
            int power = magic.GetDamage(GetAttackPower(MinDC, MaxDC));

            for (int i = 0; i < 5; i++)
            {
                hitPoint = Functions.PointMove(CurrentLocation, dir, 1);
                dir = Functions.NextDir(dir);

                if (!CurrentMap.ValidPoint(hitPoint)) continue;
                //cell = CurrentMap.GetCell(hitPoint);

                if (CurrentMap.Objects[hitPoint.X, hitPoint.Y] == null) continue;

                for (int o = 0; o < CurrentMap.Objects[hitPoint.X, hitPoint.Y].Count; o++)
                {
                    MapObject target = CurrentMap.Objects[hitPoint.X, hitPoint.Y][o];
                    if (target.Race != ObjectType.Player && target.Race != ObjectType.Monster) continue;
                    if (target == null || !target.IsAttackTarget(this) || target.Node == null) continue;

                    target.ApplyPoison(new Poison
                    {
                        Duration = 3 + power / 10 + magic.Level * 3,
                        Owner = this,
                        PType = PoisonType.Green,
                        TickSpeed = 1000,
                        Value = power / 10 + magic.Level + 1 + RandomUtils.Next(PoisonAttack+1)
                    }, this);

                    target.OperateTime = 0;
                    break;
                }
            }

            LevelMagic(magic);
            ConsumeItem(item, 1);
            return true;
        }
        //刺客，烈火身
        private void DarkBody(MapObject target, UserMagic magic)
        {
            MonsterObject monster;
            for (int i = 0; i < Pets.Count; i++)
            {
                monster = Pets[i];
                if ((monster.Info.Name != Settings.AssassinCloneName) || monster.Dead) continue;
                if (monster.Node == null) continue;
                monster.Die();
                return;
            }

            MonsterInfo info = Envir.GetMonsterInfo(Settings.AssassinCloneName);
            if (info == null) return;

            if (target == null) return;

            LevelMagic(magic);

            monster = MonsterObject.GetMonster(info);
            monster.Master = this;
            monster.Direction = Direction;
            monster.ActionTime = Envir.Time + 500;
            monster.RefreshNameColour(false);
            monster.Target = target;
            Pets.Add(monster);


            monster.Spawn(CurrentMap, CurrentLocation);

            for (int i = 0; i < Buffs.Count; i++)
                if (Buffs[i].Type == BuffType.DarkBody) return;

            AddBuff(new Buff { Type = BuffType.DarkBody, Caster = this, ExpireTime = Envir.Time + (GetAttackPower(MinAC, MaxAC) + (magic.Level + 1) * 5) * 500, Visible = true });
            LevelMagic(magic);
        }
        private void CrescentSlash(UserMagic magic)
        {
            int damageBase = GetAttackPower(MinDC, MaxDC);
            if (RandomUtils.Next(0, 100) <= Accuracy)
                damageBase += damageBase;//crit should do something like double dmg, not double max dc dmg!
            int damageFinal = magic.GetDamage(damageBase);
            if (hasItemSk(ItemSkill.Assassin7) && RandomUtils.Next(100) < 40)
            {
                damageFinal = damageFinal*13/10;
            }
            MirDirection backDir = Functions.ReverseDirection(Direction);
            MirDirection preBackDir = Functions.PreviousDir(backDir);
            MirDirection nextBackDir = Functions.NextDir(backDir);

            for (int i = 0; i < 8; i++)
            {
                MirDirection dir = (MirDirection)i;
                Point hitPoint = Functions.PointMove(CurrentLocation, dir, 1);

                if (dir != backDir && dir != preBackDir && dir != nextBackDir)
                {

                    if (!CurrentMap.ValidPoint(hitPoint)) continue;

                    //Cell cell = CurrentMap.GetCell(hitPoint);

                    if (CurrentMap.Objects[hitPoint.X, hitPoint.Y] == null) continue;


                    for (int j = 0; j < CurrentMap.Objects[hitPoint.X, hitPoint.Y].Count; j++)
                    {
                        MapObject target = CurrentMap.Objects[hitPoint.X, hitPoint.Y][j];
                        switch (target.Race)
                        {
                            case ObjectType.Monster:
                            case ObjectType.Player:
                                //Only targets
                                if (target.IsAttackTarget(this))
                                {
                                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + AttackSpeed, target, damageFinal, DefenceType.AC, true);
                                    ActionList.Add(action);
                                }
                                break;
                        }
                    }
                    LevelMagic(magic);
                }
            }
        }
        //拔刀书，几率麻痹，玩家和BOSS麻痹几率降低点
        private void FlashDash(UserMagic magic)
        {
            bool success = false;
            ActionTime = Envir.Time;

            int travel = 0;
            bool blocked = false;
            int jumpDistance = (magic.Level <= 1) ? 0 : 1;//3 max
            Point location = CurrentLocation;
            for (int i = 0; i < jumpDistance; i++)
            {
                location = Functions.PointMove(location, Direction, 1);
                if (!CurrentMap.ValidPoint(location)) break;

                //Cell cInfo = CurrentMap.GetCell(location);
                if (CurrentMap.Objects[location.X, location.Y] != null)
                {
                    for (int c = 0; c < CurrentMap.Objects[location.X, location.Y].Count; c++)
                    {
                        MapObject ob = CurrentMap.Objects[location.X, location.Y][c];
                        if (!ob.Blocking) continue;
                        blocked = true;
                        if ((CurrentMap.Objects[location.X, location.Y] == null) || blocked) break;
                    }
                }
                if (blocked) break;
                travel++;
            }

            jumpDistance = travel;

            if (jumpDistance > 0)
            {
                location = Functions.PointMove(CurrentLocation, Direction, jumpDistance);
                CurrentMap.Remove(this);
                RemoveObjects(Direction, 1);
                CurrentLocation = location;
                CurrentMap.Add(this);
                AddObjects(Direction, 1);
                Enqueue(new S.UserDashAttack { Direction = Direction, Location = location });
                Broadcast(new S.ObjectDashAttack { ObjectID = ObjectID, Direction = Direction, Location = location, Distance = jumpDistance });
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
            }

            if (travel == 0) location = CurrentLocation;

            int attackDelay = (AttackSpeed - 120) <= 300 ? 300 : (AttackSpeed - 120);
            AttackTime = Envir.Time + attackDelay;
            SpellTime = Envir.Time + 300;

            location = Functions.PointMove(location, Direction, 1);
            if (CurrentMap.ValidPoint(location))
            {
                //Cell cInfo = CurrentMap.GetCell(location);
                if (CurrentMap.Objects[location.X, location.Y] != null)
                {
                    for (int c = 0; c < CurrentMap.Objects[location.X, location.Y].Count; c++)
                    {
                        MapObject ob = CurrentMap.Objects[location.X, location.Y][c];
                        switch (ob.Race)
                        {
                            case ObjectType.Monster:
                            case ObjectType.Player:
                                //Only targets
                                if (ob.IsAttackTarget(this))
                                {
                                    DelayedAction action = new DelayedAction(DelayedType.Damage, AttackTime, ob,magic.GetDamage(GetAttackPower(MinDC, MaxDC)), DefenceType.AC, true);
                                    ActionList.Add(action);
                                    success = true;
                                    //1万血以上的，麻痹几率降2倍
                                    int par = 15;
                                    if (ob.MaxHealth >= 10000)
                                    {
                                        par = 70; 
                                    }

                                    if ((((ob.Race != ObjectType.Player) || Settings.PvpCanResistPoison) && (RandomUtils.Next(Settings.PoisonAttackWeight) >= ob.PoisonResist)) && ( RandomUtils.Next(par) <= magic.Level + 1) )
                                    {
                                        DelayedAction pa = new DelayedAction(DelayedType.Poison, AttackTime, ob, PoisonType.Stun, SpellEffect.TwinDrakeBlade, magic.Level + 1, 1000);
                                        ActionList.Add(pa);
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            if (success) //technicaly this makes flashdash lvl when it casts rather then when it hits (it wont lvl if it's not hitting!)
                LevelMagic(magic);

            magic.CastTime = Envir.Time;
            Enqueue(new S.MagicCast { Spell = magic.Spell });
        }
        #endregion

        #region Archer Skills

        private int ApplyArcherState(int damage)
        {
            UserMagic magic = GetMagic(Spell.MentalState);
            if (magic != null)
                LevelMagic(magic);
            int dmgpenalty = 100;
            //0是全力，1是穿墙，2是团队
            switch (Info.MentalState)
            {
                case 1: //trickshot
                    dmgpenalty = 55 + (Info.MentalStateLvl * 5);
                    break;
                case 2: //group attack
                    dmgpenalty = 80;
                    break;
            }
            return (damage * dmgpenalty) / 100;
        }

        private bool StraightShot(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return false;
            if ((Info.MentalState != 1) && !CanFly(target.CurrentLocation)) return false;
            int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);
            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));
            damage = (int)(damage * Math.Max(1, (distance * 0.45)));//range boost
            damage = ApplyArcherState(damage);
            int delay = distance * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target);

            ActionList.Add(action);

            return true;
        }
        private bool DoubleShot(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return false;
            if ((Info.MentalState != 1) && !CanFly(target.CurrentLocation)) return false;
            int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);
            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));
            int damage2 = magic.GetDamage(GetAttackPower(MinDC, MaxDC));
            if(damage2 > damage)
            {
                damage = damage2;
            }
            //damage = (int)(damage * Math.Max(1, (distance * 0.25)));//range boost
            //稍微加强一点
            damage = (int)(damage * Math.Max(1, (distance * 0.30)));//range boost
            damage = ApplyArcherState(damage);
            int delay = distance * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target);

            ActionList.Add(action);

            action = new DelayedAction(DelayedType.Magic, Envir.Time + delay + 50, magic, damage, target);
            ActionList.Add(action);
            //几率追击一次伤害
            if (hasItemSk(ItemSkill.Archer7) &&  RandomUtils.Next(100) < 50)
            {
                action = new DelayedAction(DelayedType.Magic, Envir.Time + delay + 100, magic, damage, target);
                ActionList.Add(action);
            }

            return true;
        }
        private void BackStep(UserMagic magic)
        {
            ActionTime = Envir.Time;
            if (!CanWalk) return;

            int travel = 0;
            bool blocked = false;
            int jumpDistance = (magic.Level == 0) ? 1 : magic.Level;//3 max
            MirDirection jumpDir = Functions.ReverseDirection(Direction);
            Point location = CurrentLocation;
            for (int i = 0; i < jumpDistance; i++)
            {
                location = Functions.PointMove(location, jumpDir, 1);
                if (!CurrentMap.ValidPoint(location)) break;

                //Cell cInfo = CurrentMap.GetCell(location);
                if (CurrentMap.Objects[location.X, location.Y] != null)
                    for (int c = 0; c < CurrentMap.Objects[location.X, location.Y].Count; c++)
                    {
                        MapObject ob = CurrentMap.Objects[location.X, location.Y][c];
                        if (!ob.Blocking) continue;
                        blocked = true;
                        if ((CurrentMap.Objects[location.X, location.Y] == null) || blocked) break;
                    }
                if (blocked) break;
                travel++;
            }

            jumpDistance = travel;
            if (jumpDistance > 0)
            {
                for (int i = 0; i < jumpDistance; i++)
                {
                    location = Functions.PointMove(CurrentLocation, jumpDir, 1);
                    CurrentMap.Remove(this);
                    RemoveObjects(jumpDir, 1);
                    CurrentLocation = location;
                    CurrentMap.Add(this);
                    AddObjects(jumpDir, 1);
                }
                Enqueue(new S.UserBackStep { Direction = Direction, Location = location });
                Broadcast(new S.ObjectBackStep { ObjectID = ObjectID, Direction = Direction, Location = location, Distance = jumpDistance });
                LevelMagic(magic);
            }
            else
            {
                Broadcast(new S.ObjectBackStep { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Distance = jumpDistance });
                ReceiveChat("跳动力不足.", ChatType.System);
            }

            magic.CastTime = Envir.Time;
            Enqueue(new S.MagicCast { Spell = magic.Spell });

            CellTime = Envir.Time + 500;
        }
        private bool DelayedExplosion(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this) || !CanFly(target.CurrentLocation)) return false;

            int power = magic.GetDamage(GetAttackPower(MinMC, MaxMC));
            if (hasItemSk(ItemSkill.Archer2))
            {
                power = power * 13 / 10;
            }

            int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, power, target);
            ActionList.Add(action);
            return true;
        }
        //释放烈火陷阱
        private void ExplosiveTrap(UserMagic magic, Point location)
        {

            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));
            if (ArcherTrapObject == null || ArcherTrapObject.CurrentMap!= CurrentMap)
            {
                DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, location);
                CurrentMap.ActionList.Add(action);
            }
            else
            {
                //触发灵魂陷阱
                ExplosiveTrapDetonated();
            }
        }
        //触发烈火陷阱
        public void ExplosiveTrapDetonated()
        {
            if (ArcherTrapObject == null)
            {
                return;
            }
            if(ArcherTrapObject.CurrentMap!= CurrentMap)
            {
                ArcherTrapObject = null;
                return;
            }
            int damage = ((SpellObject)ArcherTrapObject).Value;
            //烈火阵 
            if (hasItemSk(ItemSkill.Archer1))
            {
                damage = damage * 13 / 10;
            }
            ((SpellObject)ArcherTrapObject).DetonateTrapNow();

            //SMain.Enqueue($"广播炸弹1x:{ArcherTrapObject.CurrentLocation.X},y:{ArcherTrapObject.CurrentLocation.Y}");
            for (int x= ArcherTrapObject.CurrentLocation.X-1;x<= ArcherTrapObject.CurrentLocation.X + 1; x++)
            {
                for (int y = ArcherTrapObject.CurrentLocation.Y - 1; y <= ArcherTrapObject.CurrentLocation.Y + 1; y++)
                {
                    if(!CurrentMap.ValidPoint(x, y)){
                        continue;
                    }
                    if(ArcherTrapObject.CurrentLocation.X!=x || ArcherTrapObject.CurrentLocation.Y != y)
                    {
                        //广播炸弹
                        SpellObject sp = new SpellObject
                        {
                            Spell = Spell.ExplosiveTrap,
                            Value = damage,
                            ExpireTime = Envir.Time + 100,
                            TickSpeed = 500,
                            Caster = this,
                            CurrentLocation = new Point(x, y),
                            CurrentMap = this.CurrentMap,
                            Direction = Direction,
                            DetonatedTrap = true
                        };
                        sp.Broadcast(sp.GetInfo());
                    }
                    //SMain.Enqueue($"广播炸弹x:{x},y:{y}");
                    List<MapObject> listobj = CurrentMap.Objects[x, y];
                    if (listobj == null)
                    {
                        continue;
                    }
                    foreach (MapObject ob in listobj)
                    {
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (ob.Dead) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, ob, damage, DefenceType.MAC, false);
                        ActionList.Add(action);
                    }
                }
            }
            ArcherTrapObject = null;
        }
        public void DoKnockback(MapObject target, UserMagic magic)//ElementalShot - knockback
        {
            //Cell cell = CurrentMap.GetCell(target.CurrentLocation);
            if (!CurrentMap.Valid(target.CurrentLocation.X, target.CurrentLocation.Y) || CurrentMap.Objects[target.CurrentLocation.X, target.CurrentLocation.Y] == null) return;

            if (target.CurrentLocation.Y < 0 || target.CurrentLocation.Y >= CurrentMap.Height || target.CurrentLocation.X < 0 || target.CurrentLocation.X >= CurrentMap.Height) return;

            if (target.Race != ObjectType.Monster && target.Race != ObjectType.Player) return;
            if (!target.IsAttackTarget(this) || target.Level >= Level) return;

            if (RandomUtils.Next(20) >= 6 + magic.Level * 3 + ElementsLevel + Level - target.Level) return;
            int distance = 1 + Math.Max(0, magic.Level - 1) + RandomUtils.Next(2);
            MirDirection dir = Functions.DirectionFromPoint(CurrentLocation, target.CurrentLocation);

            target.Pushed(this, dir, distance);
        }
        public void BindingShot(UserMagic magic, MapObject target, out bool cast)
        {
            cast = false;

            if (target == null || !target.IsAttackTarget(this) || !(target is MonsterObject)) return;
            if ((Info.MentalState != 1) && !CanFly(target.CurrentLocation)) return;
            if (target.Level > Level + 2) return;
            if (((MonsterObject)target).ShockTime >= Envir.Time) return;//Already shocked


            uint duration = (uint)((magic.Level * 5 + 10) * 1000);
            int value = (int)duration;
            int delay = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, value, target);
            ActionList.Add(action);

            cast = true;
        }
        public void SpecialArrowShot(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return;
            if ((Info.MentalState != 1) && !CanFly(target.CurrentLocation)) return;
            int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);
            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));
            if (magic.Spell != Spell.CrippleShot)
                damage = (int)(damage * Math.Max(1, (distance * 0.4)));//range boost
            damage = ApplyArcherState(damage);

            int delay = distance * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, damage, target);
            ActionList.Add(action);
        }
        public void NapalmShot(MapObject target, UserMagic magic)
        {
            if (target == null || !target.IsAttackTarget(this)) return;
            if ((Info.MentalState != 1) && !CanFly(target.CurrentLocation)) return;

            int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);
            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));
            damage = ApplyArcherState(damage);

            if (hasItemSk(ItemSkill.Archer7) && RandomUtils.Next(100) < 50)
            {
                damage = damage * 14 / 10;
            }
            int delay = distance * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, this, magic, damage, target.CurrentLocation);
            CurrentMap.ActionList.Add(action);
        }
        public void ArcherSummon(UserMagic magic, MapObject target, Point location)
        {
            if (target != null && target.IsAttackTarget(this))
                location = target.CurrentLocation;
            if (!CanFly(location)) return;

            uint duration = (uint)((magic.Level * 5 + 10) * 1000);
            int value = (int)duration;
            int delay = Functions.MaxDistance(CurrentLocation, location) * 50 + 500; //50 MS per Step

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, value, location, target);
            ActionList.Add(action);
        }

        public void OneWithNature(MapObject target, UserMagic magic)
        {
            int damage = magic.GetDamage(GetAttackPower(MinMC, MaxMC));

            if (hasItemSk(ItemSkill.Archer7) && RandomUtils.Next(100) < 35)
            {
                damage = damage * 13 / 10;
            }
            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, CurrentLocation);
            CurrentMap.ActionList.Add(action);
        }
        #endregion

        #region Custom

        private void Portal(UserMagic magic, Point location, out bool cast)
        {
            cast = false;

            if (!CurrentMap.ValidPoint(location)) return;

            if (PortalObjectsArray[1] != null && PortalObjectsArray[1].Node != null)
            {
                PortalObjectsArray[0].ExpireTime = 0;
                PortalObjectsArray[0].Process();
            }

            if (!CanFly(location)) return;

            int duration = 30 + (magic.Level * 30);
            int value = duration;
            int passthroughCount = (magic.Level * 2) - 1;

            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, value, location, passthroughCount);
            CurrentMap.ActionList.Add(action);
            cast = true;
        }

        //定点移动，闪现
        private void FixedMove(UserMagic magic, Point location, out bool cast)
        {
            cast = false;
            //如果鼠标的点不能访问，则在方向上查找可访问的点
            int maxlen = magic.Level * 2 + 5;

            Point movepoint = CurrentMap.getValidPointByLine(CurrentLocation, location, maxlen);
            //增加一次性伤害
            if (hasItemSk(ItemSkill.Assassin2))
            {
                List<MapObject> list = this.CurrentMap.getMapObjects(movepoint.X, movepoint.Y, 1);
                foreach (MapObject ob in list)
                {
                    if (ob == null)
                    {
                        continue;
                    }
                    if (ob.Race != ObjectType.Monster && ob.Race != ObjectType.Player)
                    {
                        continue;
                    }
                    if (!ob.IsAttackTarget(this))
                    {
                        continue;
                    }
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, ob, GetAttackPower(MinDC,MaxDC), DefenceType.Agility,false);
                    ActionList.Add(action);
                }
            }

            Teleport(CurrentMap, movepoint);
            cast = true;
            //要加这句，升级技能
            LevelMagic(magic);
        }



        //虚空，虚空之门,地狱之门，一个圆圈的门
        private void EmptyDoor(UserMagic magic, Point location, out bool cast)
        {
            cast = false;
            //1.查找所有的契约兽，看契约兽是否拥有此技能
            MonsterObject _pet = null;
            foreach (MonsterObject pet in Pets)
            {
                if(pet.PType!= PetType.MyMonster)
                {
                    continue;
                }
                if (!pet.hasMonSk(MyMonSkill.MyMonSK9))
                {
                    continue;
                }
                _pet = pet;
            }
            if (_pet == null|| _pet.Dead)
            {
                ReceiveChat("没有召唤相关契约兽，无法释放此技能.", ChatType.System);
                return;
            }
            if (_pet == null || _pet.myMonster==null|| _pet.myMonster.callTime<=0)
            {
                ReceiveChat("契约兽体力不足，无法释放此技能.", ChatType.System);
                return;
            }
            if(_pet.CurrentMap!= CurrentMap)
            {
                ReceiveChat("契约兽距离过远，无法释放此技能.", ChatType.System);
                return;
            }
            //扣除体力，刷新契约兽数据,几率消耗体力
            if(RandomUtils.Next(100)< 30){
                _pet.myMonster.callTime--;
                GetMyMonsters();
            }
           
            Point loc = _pet.Back;
            if (!CurrentMap.ValidPoint(loc))
            {
                loc = _pet.CurrentLocation;
            }

            //虚空之门，每4秒释放一个怪物，最多释放8个怪物
            int damage = 5 + magic.Level;
  
            DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, damage, loc, _pet.myMonster);
            CurrentMap.ActionList.Add(action);
            cast = true;
        }


        //契约兽自爆
        private void MyMonsterBomb(UserMagic magic, Point location, out bool cast)
        {
            cast = false;
            //1.查找所有的契约兽，看契约兽是否拥有此技能
            MonsterObject _pet = null;
            foreach (MonsterObject pet in Pets)
            {
                if (pet.PType != PetType.MyMonster || pet.myMonster==null)
                {
                    continue;
                }
                if (!pet.hasMonSk(MyMonSkill.MyMonSK11))
                {
                    continue;
                }
                _pet = pet;
            }
            if (_pet == null || _pet.Dead)
            {
                ReceiveChat("没有召唤相关契约兽，无法释放此技能.", ChatType.System);
                return;
            }
            if (_pet.CurrentMap != CurrentMap)
            {
                ReceiveChat("契约兽距离过远，无法释放此技能.", ChatType.System);
                return;
            }

            //计算伤害(契约兽攻击+技能等级)
            int value = (int)(_pet.HP * (magic.Level+8)/10.0);

            _pet.ApplyPoison(new Poison
            {
                Duration = RandomUtils.Next(3,7),
                Owner = this,
                PType = PoisonType.DelayedBomb,
                TickSpeed = 1000,
                Value = value + RandomUtils.Next(10, 20),
            }, this);
            _pet.Broadcast(new S.ObjectEffect { ObjectID = _pet.ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 2 });
            //Broadcast(new S.ObjectEffect { ObjectID = _pet.ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 1 });

            cast = true;
        }


        #endregion

        private void CheckSneakRadius()
        {
            if (!Sneaking) return;

            for (int y = CurrentLocation.Y - 3; y <= CurrentLocation.Y + 3; y++)
            {
                if (y < 0) continue;
                if (y >= CurrentMap.Height) break;

                for (int x = CurrentLocation.X - 3; x <= CurrentLocation.X + 3; x++)
                {
                    if (x < 0) continue;
                    if (x >= CurrentMap.Width) break;

                    //Cell cell = CurrentMap.GetCell(x, y);
                    if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                    for (int i = 0; CurrentMap.Objects[x, y] != null && i < CurrentMap.Objects[x, y].Count; i++)
                    {
                        MapObject ob = CurrentMap.Objects[x, y][i];
                        if ((ob.Race != ObjectType.Player) || ob == this) continue;

                        SneakingActive = false;
                        return;
                    }
                }
            }

            SneakingActive = true;
        }

        //技能完成，伤害要在这里加
        private void CompleteMagic(IList<object> data)
        {
            UserMagic magic = (UserMagic)data[0];
            int value;
            MapObject target;
            Point location;
            MonsterObject monster;
            switch (magic.Spell)
            {
                #region DoubleShot 连珠箭法 ,物理攻击
                case Spell.DoubleShot:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
                    if (target.Attacked(this, value, DefenceType.AC, false) > 0) LevelMagic(magic);
                    break;
                #endregion

                #region FireBall, GreatFireBall, ThunderBolt, SoulFireBall, FlameDisruptor
                case Spell.FireBall:
                case Spell.GreatFireBall:
                case Spell.ThunderBolt:
                case Spell.SoulFireBall:
                case Spell.FlameDisruptor:
                case Spell.StraightShot:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
                    if (target.Attacked(this, value, DefenceType.MAC, false) > 0) LevelMagic(magic);
                    break;

                #endregion

                #region FrostCrunch
                case Spell.FrostCrunch:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
                    if (target.Attacked(this, value, DefenceType.MAC, false) > 0)
                    {
                        if (Level + (target.Race == ObjectType.Player ? 2 : 10) >= target.Level && RandomUtils.Next(target.Race == ObjectType.Player ? 100 : 20) <= magic.Level)
                        {
                            target.ApplyPoison(new Poison
                            {
                                Owner = this,
                                Duration = target.Race == ObjectType.Player ? 4 : 5 + RandomUtils.Next(5),
                                PType = PoisonType.Slow,
                                TickSpeed = 1000,
                            }, this);
                            target.OperateTime = 0;
                        }

                        if (Level + (target.Race == ObjectType.Player ? 2 : 10) >= target.Level && RandomUtils.Next(target.Race == ObjectType.Player ? 100 : 40) <= magic.Level)
                        {
                            target.ApplyPoison(new Poison
                            {
                                Owner = this,
                                Duration = target.Race == ObjectType.Player ? 2 : 5 + RandomUtils.Next(Freezing),
                                PType = PoisonType.Frozen,
                                TickSpeed = 1000,
                            }, this);
                            target.OperateTime = 0;
                        }

                        LevelMagic(magic);
                    }
                    break;

                #endregion

                #region Vampirism

                case Spell.Vampirism:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
                    value = target.Attacked(this, value, DefenceType.MAC, false);
                    if (value == 0) return;
                    LevelMagic(magic);
                    if (VampAmount == 0) VampTime = Envir.Time + 1000;
                    VampAmount += (ushort)(value * (magic.Level + 1) * 0.25F);
                    break;

                #endregion

                #region Healing

                case Spell.Healing:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsFriendlyTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
                    if (target.Health >= target.MaxHealth) return;
                    target.HealAmount = (ushort)Math.Min(ushort.MaxValue, target.HealAmount + value);
                    target.OperateTime = 0;
                    LevelMagic(magic);
                    break;

                #endregion

                #region ElectricShock

                case Spell.ElectricShock:
                    monster = (MonsterObject)data[1];
                    if (monster == null || !monster.IsAttackTarget(this) || monster.CurrentMap != CurrentMap || monster.Node == null) return;
                    ElectricShock(monster, magic);
                    break;

                #endregion

                #region Poisoning 施毒术

                case Spell.Poisoning:
                    value = (int)data[1];
                    target = (MapObject)data[2];
                    UserItem item = (UserItem)data[3];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

                    switch (item.Info.Shape)
                    {
                        case 1://这里调下绿毒的伤害？降低点绿毒的伤害
                            int _value = value / 15 + magic.Level + 1 + RandomUtils.Next(PoisonAttack+1);
                            int _Duration = (value * 2) + ((magic.Level + 1) * 7);
                            if (hasItemSk(ItemSkill.Taoist7))
                            {
                                _value = _value * 13 / 10;
                            }

                            target.ApplyPoison(new Poison
                            {
                                Duration = _Duration,
                                Owner = this,
                                PType = PoisonType.Green,
                                TickSpeed = 2000,
                                //Value = value / 15 + magic.Level + 1 + RandomUtils.Next(PoisonAttack)
                                Value = _value
                            }, this);
                            break;
                        case 2:
                            target.ApplyPoison(new Poison
                            {
                                Duration = (value * 2) + (magic.Level + 1) * 7,
                                Owner = this,
                                PType = PoisonType.Red,
                                TickSpeed = 2000,
                            }, this);
                            break;
                    }
                    target.OperateTime = 0;

                    LevelMagic(magic);
                    break;

                #endregion

                #region StormEscape
                case Spell.StormEscape:
                    location = (Point) data[1];
                    if (CurrentMap.Info.NoTeleport)
                    {
                        ReceiveChat(("你不能在这张地图上传送"), ChatType.System);
                        return;
                    }
                    if (!CurrentMap.ValidPoint(location) || RandomUtils.Next(4) >= magic.Level + 1 || !Teleport(CurrentMap, location, false)) return;
                    CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.StormEscape }, CurrentLocation);
                    AddBuff(new Buff { Type = BuffType.TemporalFlux, Caster = this, ExpireTime = Envir.Time + 30000 });
                    LevelMagic(magic);
                    break;
                #endregion

                #region Teleport
                case Spell.Teleport:
                    Map temp = Envir.GetMap(BindMapIndex);
                    int mapSizeX = temp.Width / (magic.Level + 1);
                    int mapSizeY = temp.Height / (magic.Level + 1);

                    if (CurrentMap.Info.NoTeleport)
                    {
                        ReceiveChat(("你不能在这张地图上传送"), ChatType.System);
                        return;
                    }

                    for (int i = 0; i < 200; i++)
                    {
                        location = new Point(BindLocation.X + RandomUtils.Next(-mapSizeX, mapSizeX),
                                             BindLocation.Y + RandomUtils.Next(-mapSizeY, mapSizeY));

                        if (Teleport(temp, location)) break;
                    }

                    AddBuff(new Buff { Type = BuffType.TemporalFlux, Caster = this, ExpireTime = Envir.Time + 30000 });
                    LevelMagic(magic);

                    break;
                #endregion

                #region Blink

                case Spell.Blink:
                    {
                        location = (Point)data[1];
                        if (CurrentMap.Info.NoTeleport)
                        {
                            ReceiveChat(("你不能在这张地图上传送"), ChatType.System);
                            return;
                        }
                        if (Functions.InRange(CurrentLocation, location, magic.Info.Range) == false) return;
                        if (!CurrentMap.ValidPoint(location) || RandomUtils.Next(4) >= magic.Level + 1 || !Teleport(CurrentMap, location, false)) return;
                        CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Teleport }, CurrentLocation);
                        LevelMagic(magic);
                        AddBuff(new Buff { Type = BuffType.TemporalFlux, Caster = this, ExpireTime = Envir.Time + 30000 });
                    }
                    break;

                #endregion

                #region Hiding

                case Spell.Hiding:
                    for (int i = 0; i < Buffs.Count; i++)
                        if (Buffs[i].Type == BuffType.Hiding) return;

                    value = (int)data[1];
                    AddBuff(new Buff { Type = BuffType.Hiding, Caster = this, ExpireTime = Envir.Time + value * 1000 });
                    LevelMagic(magic);
                    break;

                #endregion

                #region Haste

                case Spell.Haste:
                    AddBuff(new Buff { Type = BuffType.Haste, Caster = this, ExpireTime = Envir.Time + (magic.Level + 1) * 30000, Values = new int[] { (magic.Level + 1) * 2 } });
                    LevelMagic(magic);
                    break;

                #endregion

                #region Fury

                case Spell.Fury:
                    AddBuff(new Buff { Type = BuffType.Fury, Caster = this, ExpireTime = Envir.Time + 60000 + magic.Level * 10000, Values = new int[] { 4 }, Visible = true });
                    LevelMagic(magic);
                    break;

                #endregion

                #region ImmortalSkin

                case Spell.ImmortalSkin:
                    int ACvalue = (int)Math.Round(MaxAC * (0.10 + (0.07 * magic.Level)));
                    int DCValue = (int)Math.Round(MaxDC * (0.05 + (0.01 * magic.Level)));
                    AddBuff(new Buff { Type = BuffType.ImmortalSkin, Caster = this, ExpireTime = Envir.Time + 60000 + magic.Level * 1000, Values = new int[] { ACvalue, DCValue }, Visible = true });
                    LevelMagic(magic);
                    break;
                #endregion

                #region LightBody

                case Spell.LightBody:
                    AddBuff(new Buff { Type = BuffType.LightBody, Caster = this, ExpireTime = Envir.Time + (magic.Level + 1) * 30000, Values = new int[] { (magic.Level + 1) * 2 } });
                    LevelMagic(magic);
                    break;

                #endregion

                #region MagicShield

                case Spell.MagicShield:

                    if (MagicShield) return;
                    MagicShield = true;
                    MagicShieldLv = magic.Level;
                    MagicShieldTime = Envir.Time + (int)data[1] * 1000;
                    CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.MagicShieldUp }, CurrentLocation);
                    AddBuff(new Buff { Type = BuffType.MagicShield, Caster = this, ExpireTime = MagicShieldTime, Values = new int[] { MagicShieldLv } });
                    LevelMagic(magic);
                    break;

                #endregion

                #region TurnUndead

                case Spell.TurnUndead:
                    monster = (MonsterObject)data[1];
                    if (monster == null || !monster.IsAttackTarget(this) || monster.CurrentMap != CurrentMap || monster.Node == null) return;
                    monster.LastHitter = this;
                    monster.LastHitTime = Envir.Time + 5000;
                    monster.EXPOwner = this;
                    monster.EXPOwnerTime = Envir.Time + 5000;
                    monster.Die();
                    LevelMagic(magic);
                    break;

                #endregion

                #region MagicBooster

                case Spell.MagicBooster:
                    value = (int)data[1];

                    AddBuff(new Buff { Type = BuffType.MagicBooster, Caster = this, ExpireTime = Envir.Time + 60000, Values = new int[] { value, 6 + magic.Level }, Visible = true });
                    LevelMagic(magic);
                    break;

                #endregion

                #region Purification

                case Spell.Purification:
                    target = (MapObject)data[1];

                    if (target == null || !target.IsFriendlyTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
                    if (RandomUtils.Next(4) > magic.Level || target.PoisonList.Count == 0) return;

                    target.ExplosionInflictedTime = 0;
                    target.ExplosionInflictedStage = 0;

                    for (int i = 0; i < target.Buffs.Count; i++)
                    {
                        if (target.Buffs[i].Type == BuffType.Curse)
                        {
                            target.Buffs.RemoveAt(i);
                            break;
                        }
                    }

                    target.PoisonList.Clear();
                    target.OperateTime = 0;

                    if (target.ObjectID == ObjectID)
                        Enqueue(new S.RemoveDelayedExplosion { ObjectID = target.ObjectID });
                    target.Broadcast(new S.RemoveDelayedExplosion { ObjectID = target.ObjectID });

                    LevelMagic(magic);
                    break;

                #endregion

                #region Revelation

                case Spell.Revelation:
                    value = (int)data[1];
                    target = (MapObject)data[2];
                    if (target == null || target.CurrentMap != CurrentMap || target.Node == null) return;
                    if (target.Race != ObjectType.Player && target.Race != ObjectType.Monster) return;
                    if (RandomUtils.Next(4) > magic.Level || Envir.Time < target.RevTime) return;

                    target.RevTime = Envir.Time + value * 1000;
                    target.OperateTime = 0;
                    target.BroadcastHealthChange();

                    LevelMagic(magic);
                    break;

                #endregion

                #region Reincarnation

                case Spell.Reincarnation:

                    if (ReincarnationReady)
                    {
                        ReincarnationTarget.Enqueue(new S.RequestReincarnation { });
                        LevelMagic(magic);
                        ReincarnationReady = false;
                    }
                    break;

                #endregion

                #region Entrapment

                case Spell.Entrapment:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null || target.Race != ObjectType.Monster ||
                        Functions.MaxDistance(CurrentLocation, target.CurrentLocation) > 7 || target.Level >= Level + 5 + RandomUtils.Next(8)) return;

                    MirDirection pulldirection = (MirDirection)((byte)(Direction - 4) % 8);
                    int pulldistance = 0;
                    if ((byte)pulldirection % 2 > 0)
                        pulldistance = Math.Max(0, Math.Min(Math.Abs(CurrentLocation.X - target.CurrentLocation.X), Math.Abs(CurrentLocation.Y - target.CurrentLocation.Y)));
                    else
                        pulldistance = pulldirection == MirDirection.Up || pulldirection == MirDirection.Down ? Math.Abs(CurrentLocation.Y - target.CurrentLocation.Y) - 2 : Math.Abs(CurrentLocation.X - target.CurrentLocation.X) - 2;

                    int levelgap = target.Race == ObjectType.Player ? Level - target.Level + 4 : Level - target.Level + 9;
                    if (RandomUtils.Next(30) >= ((magic.Level + 1) * 3) + levelgap) return;

                    int duration = target.Race == ObjectType.Player ? (int)Math.Round((magic.Level + 1) * 1.6) : (int)Math.Round((magic.Level + 1) * 0.8);
                    if (duration > 0) target.ApplyPoison(new Poison { PType = PoisonType.Paralysis, Duration = duration, TickSpeed = 1000 }, this);
                    CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = target.ObjectID, Effect = SpellEffect.Entrapment }, target.CurrentLocation);
                    if (target.Pushed(this, pulldirection, pulldistance) > 0) LevelMagic(magic);
                    break;

                #endregion

                #region Hallucination

                case Spell.Hallucination:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null ||
                        Functions.MaxDistance(CurrentLocation, target.CurrentLocation) > 7 || RandomUtils.Next(Level + 20 + magic.Level * 5) <= target.Level + 10) return;
                    item = GetAmulet(1);
                    if (item == null) return;

                    ((MonsterObject)target).HallucinationTime = Envir.Time + (RandomUtils.Next(20) + 10) * 1000;
                    target.Target = null;

                    ConsumeItem(item, 1);

                    LevelMagic(magic);
                    break;

                #endregion

                #region PetEnhancer

                case Spell.PetEnhancer:
                    value = (int)data[1];
                    target = (MonsterObject)data[2];
                    //血龙水，针对契约兽的效果减半
                    ushort tLevel = target.Level;
                    if (target.PType == PetType.MyMonster)
                    {
                        tLevel = (ushort)(tLevel / 2);
                    }
                    int dcInc = 2 + tLevel * 2;
                    int acInc = 4 + tLevel;

                    target.AddBuff(new Buff { Type = BuffType.PetEnhancer, Caster = this, ExpireTime = Envir.Time + value * 1000, Values = new int[] { dcInc, acInc }, Visible = true });
                    LevelMagic(magic);
                    break;

                #endregion

                #region ElementalBarrier, ElementalShot

                case Spell.ElementalBarrier:
                    if (ElementalBarrier) return;
                    if (!HasElemental)
                    {
                        ObtainElement(true);//gather orb through casting
                        LevelMagic(magic);
                        return;
                    }

                    int barrierPower = GetElementalOrbPower(true);//defensive orbpower
                    //destroy orbs
                    ElementsLevel = 0;
                    ObtainElement(false);
                    LevelMagic(magic);
                    //
                    ElementalBarrier = true;
                    ElementalBarrierLv = (byte)((int)magic.Level);//compensate for lower mc then wizard
                    ElementalBarrierTime = Envir.Time + ((int)data[1] + barrierPower) * 1000;
                    CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.ElementalBarrierUp }, CurrentLocation);
                    break;

                case Spell.ElementalShot:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null)
                    {
                        //destroy orbs
                        ElementsLevel = 0;
                        ObtainElement(false);//update and send to client
                        return;
                    }
                    if (target.Attacked(this, value, DefenceType.MAC, false) > 0)
                        LevelMagic(magic);
                    DoKnockback(target, magic);//ElementalShot - Knockback

                    //destroy orbs
                    ElementsLevel = 0;
                    ObtainElement(false);//update and send to client
                    break;

                #endregion

                #region DelayedExplosion

                case Spell.DelayedExplosion:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
                    if (target.Attacked(this, value, DefenceType.MAC, false) > 0) LevelMagic(magic);

                    target.ApplyPoison(new Poison
                    {
                        Duration = (value * 2) + (magic.Level + 1) * 7,
                        Owner = this,
                        PType = PoisonType.DelayedExplosion,
                        TickSpeed = 2000,
                        Value = value
                    }, this);

                    target.OperateTime = 0;
                    LevelMagic(magic);
                    break;

                #endregion

                #region BindingShot

                case Spell.BindingShot:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
                    if (((MonsterObject)target).ShockTime >= Envir.Time) return;//Already shocked

                    Point place = target.CurrentLocation;
                    MonsterObject centerTarget = null;

                    for (int y = place.Y - 1; y <= place.Y + 1; y++)
                    {
                        if (y < 0) continue;
                        if (y >= CurrentMap.Height) break;

                        for (int x = place.X - 1; x <= place.X + 1; x++)
                        {
                            if (x < 0) continue;
                            if (x >= CurrentMap.Width) break;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject targetob = CurrentMap.Objects[x, y][i];

                                if (y == place.Y && x == place.X && targetob.Race == ObjectType.Monster)
                                {
                                    centerTarget = (MonsterObject)targetob;
                                }

                                switch (targetob.Race)
                                {
                                    case ObjectType.Monster:
                                        if (targetob == null || !targetob.IsAttackTarget(this) || targetob.Node == null || targetob.Level > this.Level + 2) continue;

                                        MonsterObject mobTarget = (MonsterObject)targetob;

                                        if (centerTarget == null) centerTarget = mobTarget;

                                        mobTarget.ShockTime = Envir.Time + value;
                                        mobTarget.Target = null;
                                        break;
                                }
                            }
                        }
                    }

                    if (centerTarget == null) return;

                    //only the centertarget holds the effect
                    centerTarget.BindingShotCenter = true;
                    centerTarget.Broadcast(new S.SetBindingShot { ObjectID = centerTarget.ObjectID, Enabled = true, Value = value });

                    LevelMagic(magic);
                    break;

                #endregion

                #region VampireShot, PoisonShot, CrippleShot
                case Spell.VampireShot:
                case Spell.PoisonShot:
                case Spell.CrippleShot:
                    value = (int)data[1];
                    target = (MapObject)data[2];

                    if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
                    if (target.Attacked(this, value, DefenceType.MAC, false) == 0) return;

                    int buffTime = 5 + (5 * magic.Level);

                    bool hasVampBuff = (Buffs.Where(x => x.Type == BuffType.VampireShot).ToList().Count() > 0);
                    bool hasPoisonBuff = (Buffs.Where(x => x.Type == BuffType.PoisonShot).ToList().Count() > 0);

                    bool doVamp = false, doPoison = false;
                    if (magic.Spell == Spell.VampireShot)
                    {
                        doVamp = true;
                        if (!hasVampBuff && !hasPoisonBuff && (RandomUtils.Next(20) >= 8))//40% chance
                        {
                            AddBuff(new Buff { Type = BuffType.VampireShot, Caster = this, ExpireTime = Envir.Time + (buffTime * 1000), Values = new int[] { value }, Visible = true, ObjectID = this.ObjectID });
                            BroadcastInfo();
                        }
                    }
                    if (magic.Spell == Spell.PoisonShot)
                    {
                        doPoison = true;
                        if (!hasPoisonBuff && !hasVampBuff && (RandomUtils.Next(20) >= 8))//40% chance
                        {
                            AddBuff(new Buff { Type = BuffType.PoisonShot, Caster = this, ExpireTime = Envir.Time + (buffTime * 1000), Values = new int[] { value }, Visible = true, ObjectID = this.ObjectID });
                            BroadcastInfo();
                        }
                    }
                    if (magic.Spell == Spell.CrippleShot)
                    {
                        if (hasItemSk(ItemSkill.Archer5))
                        {
                            value = value * 13 / 10;
                        }
                        if (hasVampBuff || hasPoisonBuff)
                        {
                            place = target.CurrentLocation;
                            for (int y = place.Y - 1; y <= place.Y + 1; y++)
                            {
                                if (y < 0) continue;
                                if (y >= CurrentMap.Height) break;
                                for (int x = place.X - 1; x <= place.X + 1; x++)
                                {
                                    if (x < 0) continue;
                                    if (x >= CurrentMap.Width) break;
                                    //Cell cell = CurrentMap.GetCell(x, y);
                                    if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x, y] == null) continue;
                                    for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                                    {
                                        MapObject targetob = CurrentMap.Objects[x, y][i];
                                        if (targetob.Race != ObjectType.Monster && targetob.Race != ObjectType.Player) continue;
                                        if (targetob == null || !targetob.IsAttackTarget(this) || targetob.Node == null) continue;
                                        if (targetob.Dead) continue;

                                        if (hasVampBuff)//Vampire Effect
                                        {
                                            //cancel out buff
                                            AddBuff(new Buff { Type = BuffType.VampireShot, Caster = this, ExpireTime = Envir.Time + 1000, Values = new int[] { value }, Visible = true, ObjectID = this.ObjectID });

                                            target.Attacked(this, value, DefenceType.MAC, false);
                                            if (VampAmount == 0) VampTime = Envir.Time + 1000;
                                            VampAmount += (ushort)(value * (magic.Level + 1) * 0.25F);
                                        }
                                        if (hasPoisonBuff)//Poison Effect
                                        {
                                            //cancel out buff
                                            AddBuff(new Buff { Type = BuffType.PoisonShot, Caster = this, ExpireTime = Envir.Time + 1000, Values = new int[] { value }, Visible = true, ObjectID = this.ObjectID });
                                        
                                            targetob.ApplyPoison(new Poison
                                            {
                                                Duration = (value * 2) + (magic.Level + 1) * 7,
                                                Owner = this,
                                                PType = PoisonType.Green,
                                                TickSpeed = 2000,
                                                Value = value / 25 + magic.Level + 1 + RandomUtils.Next(PoisonAttack + 1)
                                            }, this);
                                            targetob.OperateTime = 0;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (doVamp)//Vampire Effect
                        {
                            if (VampAmount == 0) VampTime = Envir.Time + 1000;
                            VampAmount += (ushort)(value * (magic.Level + 1) * 0.25F);
                        }
                        if (doPoison)//Poison Effect
                        {
                            //弓手的毒这么猛么
                            target.ApplyPoison(new Poison
                            {
                                Duration = (value * 2) + (magic.Level + 1) * 7,
                                Owner = this,
                                PType = PoisonType.Green,
                                TickSpeed = 2000,
                                Value = value / 25 + magic.Level + 1 + RandomUtils.Next(PoisonAttack+1)
                            }, this);
                            target.OperateTime = 0;
                        }
                    }

                    LevelMagic(magic);
                    break;
                #endregion

                #region ArcherSummons
                case Spell.SummonVampire:
                case Spell.SummonToad:
                case Spell.SummonSnakes:
                    value = (int)data[1];
                    location = (Point)data[2];
                    target = (MapObject)data[3];

                    int SummonType = 0;
                    switch (magic.Spell)
                    {
                        case Spell.SummonVampire:
                            SummonType = 1;
                            break;
                        case Spell.SummonToad:
                            SummonType = 2;
                            break;
                        case Spell.SummonSnakes:
                            SummonType = 3;
                            break;
                    }
                    if (SummonType == 0) return;

                    for (int i = 0; i < Pets.Count; i++)
                    {
                        monster = Pets[i];
                        if ((monster.Info.Name != (SummonType == 1 ? Settings.VampireName : (SummonType == 2 ? Settings.ToadName : Settings.SnakeTotemName))) || monster.Dead) continue;
                        if (monster.Node == null) continue;
                        monster.ActionList.Add(new DelayedAction(DelayedType.Recall, Envir.Time + 500, target));
                        monster.Target = target;
                        return;
                    }
                    if (getPetCount(PetType.Common) > 1)
                    {
                        return;
                    }
                    //if (Pets.Where(x => x.Race == ObjectType.Monster).Count() > 1) return;

                    //left it in for future summon amulets
                    //UserItem item = GetAmulet(5);
                    //if (item == null) return;

                    MonsterInfo info = Envir.GetMonsterInfo((SummonType == 1 ? Settings.VampireName : (SummonType == 2 ? Settings.ToadName : Settings.SnakeTotemName)));
                    if (info == null) return;

                    if(SummonType==2 && hasItemSk(ItemSkill.Archer4))
                    {
                        info = info.Clone();
                        info.MaxAC = (ushort)(info.MaxAC * 2);
                        info.MaxMAC = (ushort)(info.MaxMAC * 2);
                        info.MaxDC = (ushort)(info.MaxDC * 2);
                        info.MaxMC = (ushort)(info.MaxMC * 2);
                        info.MaxSC = (ushort)(info.MaxSC * 2);
                    }

                    LevelMagic(magic);
                    //ConsumeItem(item, 5);

                    monster = MonsterObject.GetMonster(info);
                    monster.PetLevel = magic.Level;
                    monster.Master = this;
                    monster.MaxPetLevel = (byte)(1 + magic.Level * 2);
                    monster.Direction = Direction;
                    monster.ActionTime = Envir.Time + 1000;
                    monster.Target = target;
                    //默认大概10秒，每点魔法加1秒
                    //基础30秒，每级15秒，每点魔法1秒,最多存活120秒
                    int damage = GetAttackPower(MinMC, MaxMC);//根据魔法攻击计算存活时长
                    long AliveTime = ((magic.Level * 10000) + 30000 + damage);
                    if(AliveTime> Settings.Minute*2)
                    {
                        AliveTime = Settings.Minute*2;
                    }
                    //存活时间
                    if (SummonType == 1)
                        ((Monsters.VampireSpider)monster).AliveTime = Envir.Time + AliveTime*8/10;
                    if (SummonType == 2)
                        ((Monsters.SpittingToad)monster).AliveTime = Envir.Time + AliveTime;
                    if (SummonType == 3)
                        ((Monsters.SnakeTotem)monster).AliveTime = Envir.Time + AliveTime * 9 / 10;

                    //Pets.Add(monster);
               
                    DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 500, this, magic, monster, location);
                    CurrentMap.ActionList.Add(action);
                    break;
                #endregion

                case Spell.FixedMove:
                    LevelMagic(magic);
                    break;

            }


        }
        private void CompleteMine(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            if (target == null) return;
            target.Broadcast(new S.MapEffect { Effect = SpellEffect.Mine, Location = target.CurrentLocation, Value = (byte)Direction });
            //target.Broadcast(new S.ObjectEffect { ObjectID = target.ObjectID, Effect = SpellEffect.Mine });
            if ((byte)target.Direction < 6)
                target.Direction++;
            target.Broadcast(target.GetInfo());
        }
        private void CompleteAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];
            bool damageWeapon = (bool)data[3];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            if (target.Attacked(this, damage, defence, damageWeapon) <= 0) return;

            //Level Fencing / SpiritSword
            foreach (UserMagic magic in Info.Magics)
            {
                switch (magic.Spell)
                {
                    case Spell.Fencing:
                    case Spell.SpiritSword:
                        LevelMagic(magic);
                        break;
                }
            }
        }
        private void CompleteDamageIndicator(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            DamageType type = (DamageType)data[1];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            target.BroadcastDamageIndicator(type);
        }
        //完成NPC的事件
        private void CompleteNPC(IList<object> data)
        {
            uint npcid = (uint)data[0];
            string page = (string)data[1];

            if (data.Count > 3)
            {
                Map map = (Map)data[2];
                Point coords = (Point)data[3];

                Teleport(map, coords);
            }

            NPCDelayed = true;

            if (page.Length > 0)
            {
                if (npcid == DefaultNPC.ObjectID)
                {
                    DefaultNPC.Call(this, page.ToUpper());
                }
                else
                {
                    NPCObject obj = SMain.Envir.Objects.FirstOrDefault(x => x.ObjectID == npcid) as NPCObject;

                    if (obj != null)
                        obj.Call(this, page);
                }

                CallNPCNextPage();
            }
        }
        private void CompletePoison(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            PoisonType pt = (PoisonType)data[1];
            SpellEffect sp = (SpellEffect)data[2];
            int duration = (int)data[3];
            int tickSpeed = (int)data[4];

            if (target == null) return;

            target.ApplyPoison(new Poison { PType = pt, Duration = duration, TickSpeed = tickSpeed }, this);
            target.Broadcast(new S.ObjectEffect { ObjectID = target.ObjectID, Effect = sp });
        }
        //取符
        private UserItem GetAmulet(int count, int shape = 0)
        {
            //先从装备栏取。
            for (int i = 0; i < Info.Equipment.Length; i++)
            {
                UserItem item = Info.Equipment[i];
                if (item != null && item.Info.Type == ItemType.Amulet && item.Info.Shape == shape && item.Count >= count)
                    return item;
            }
            //取不到则从背包取
            for(int i=0;i< Info.Inventory.Length; i++)
            {
                UserItem item = Info.Inventory[i];
                if (item != null && item.Info.Type == ItemType.Amulet && item.Info.Shape == shape && item.Count >= count)
                    return item;
            }
            return null;
        }
        //取毒Info.Shape,0:红绿都可以，1：绿毒 2：黄毒
        private UserItem GetPoison(int count, byte shape = 0)
        {
            for (int i = 0; i < Info.Equipment.Length; i++)
            {
                UserItem item = Info.Equipment[i];
                if (item != null && item.Info.Type == ItemType.Amulet && item.Count >= count)
                {
                    if (shape == 0)
                    {
                        if (item.Info.Shape == 1 || item.Info.Shape == 2)
                            return item;
                    }
                    else
                    {
                        if (item.Info.Shape == shape)
                            return item;
                    }
                }
            }
            //取不到则从背包取
            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                UserItem item = Info.Inventory[i];
                if (item != null && item.Info.Type == ItemType.Amulet && item.Count >= count)
                {
                    if (shape == 0)
                    {
                        if (item.Info.Shape == 1 || item.Info.Shape == 2)
                            return item;
                    }
                    else
                    {
                        if (item.Info.Shape == shape)
                            return item;
                    }
                }
            }

            return null;
        }
        private UserItem GetBait(int count)
        {
            UserItem item = Info.Equipment[(int)EquipmentSlot.Weapon];
            if (item == null || item.Info.Type != ItemType.Weapon || (item.Info.Shape != 49 && item.Info.Shape != 50)) return null;

            UserItem bait = item.Slots[(int)FishingSlot.Bait];

            if (bait == null || bait.Count < count) return null;

            return bait;
        }

        private UserItem GetFishingItem(FishingSlot type)
        {
            UserItem item = Info.Equipment[(int)EquipmentSlot.Weapon];
            if (item == null || item.Info.Type != ItemType.Weapon || (item.Info.Shape != 49 && item.Info.Shape != 50)) return null;

            UserItem fishingItem = item.Slots[(int)type];

            if (fishingItem == null) return null;

            return fishingItem;
        }
        private void DeleteFishingItem(FishingSlot type)
        {
            UserItem item = Info.Equipment[(int)EquipmentSlot.Weapon];
            if (item == null || item.Info.Type != ItemType.Weapon || (item.Info.Shape != 49 && item.Info.Shape != 50)) return;

            UserItem slotItem = Info.Equipment[(int)EquipmentSlot.Weapon].Slots[(int)type];

            Enqueue(new S.DeleteItem { UniqueID = slotItem.UniqueID, Count = 1 });
            Info.Equipment[(int)EquipmentSlot.Weapon].Slots[(int)type] = null;

            Report.ItemChanged("FishingConsumable", slotItem, 1, 1);
        }
        private void DamagedFishingItem(FishingSlot type, int lossDura)
        {
            UserItem item = GetFishingItem(type);

            if (item != null)
            {
                if (item.CurrentDura <= 0)
                {

                    DeleteFishingItem(type);
                }
                else
                {
                    DamageItem(item, lossDura, true);
                }
            }
        }

        public UserMagic GetMagic(Spell spell)
        {
            for (int i = 0; i < Info.Magics.Count; i++)
            {
                UserMagic magic = Info.Magics[i];
                if (magic.Spell != spell) continue;
                return magic;
            }

            return null;
        }

        public void LevelMagic(UserMagic magic)
        {
            ushort exp = (ushort)(RandomUtils.Next(3) + 1);

            if ((Settings.MentorSkillBoost) && (Info.Mentor != 0) && (Info.isMentor))
            {
                Buff buff = Buffs.Where(e => e.Type == BuffType.Mentee).FirstOrDefault();
                if (buff != null)
                {
                    CharacterInfo Mentor = Envir.GetCharacterInfo(Info.Mentor);
                    PlayerObject player = Envir.GetPlayer(Mentor.Name);
                    if (player.CurrentMap == CurrentMap && Functions.InRange(player.CurrentLocation, CurrentLocation, Globals.DataRange) && !player.Dead)
                        if (SkillNeckBoost == 1) exp *= 2;
                }
            }

            exp *= SkillNeckBoost;
            exp *= Settings.SkillRate;


            if (Level == 65535) exp = byte.MaxValue;

            int oldLevel = magic.Level;

            switch (magic.Level)
            {
                case 0:
                    if (Level < magic.Info.Level1)
                        return;

                    magic.Experience += exp;
                    if (magic.Experience >= magic.Info.Need1)
                    {
                        magic.Level++;
                        magic.Experience = (ushort)(magic.Experience - magic.Info.Need1);
                        RefreshStats();
                    }
                    break;
                case 1:
                    if (Level < magic.Info.Level2)
                        return;

                    magic.Experience += exp;
                    if (magic.Experience >= magic.Info.Need2)
                    {
                        magic.Level++;
                        magic.Experience = (ushort)(magic.Experience - magic.Info.Need2);
                        RefreshStats();
                    }
                    break;
                case 2:
                    if (Level < magic.Info.Level3)
                        return;

                    magic.Experience += exp;
                    if (magic.Experience >= magic.Info.Need3)
                    {
                        magic.Level++;
                        magic.Experience = 0;
                        RefreshStats();
                    }
                    break;
                default:
                    return;
            }

            if (oldLevel != magic.Level)
            {
                long delay = magic.GetDelay();
                Enqueue(new S.MagicDelay { Spell = magic.Spell, Delay = delay });
            }

            Enqueue(new S.MagicLeveled { Spell = magic.Spell, Level = magic.Level, Experience = magic.Experience });

        }
        //玩家移动到某个点，进行检查
        public bool CheckMovement(Point location)
        {
            if (Envir.Time < MovementTime) return false;

            //脚本触发，触发默认NPC
            //Script triggered coords
            for (int s = 0; s < CurrentMap.Info.ActiveCoords.Count; s++)
            {
                Point activeCoord = CurrentMap.Info.ActiveCoords[s];

                if (activeCoord != location) continue;

                CallDefaultNPC(DefaultNPCType.MapCoord, CurrentMap.Info.Mcode, activeCoord.X, activeCoord.Y);
            }

            //地图移动
            //Map movements
            for (int i = 0; i < CurrentMap.Info.Movements.Count; i++)
            {
                MovementInfo info = CurrentMap.Info.Movements[i];

                if (info.Source != location) continue;
                //必须有僵尸洞
                if (info.NeedHole)
                {
                    //Cell cell = CurrentMap.GetCell(location);

                    if (CurrentMap.Objects[location.X, location.Y] == null ||
                        CurrentMap.Objects[location.X, location.Y].Where(ob => ob.Race == ObjectType.Spell).All(ob => ((SpellObject)ob).Spell != Spell.DigOutZombie))
                        continue;
                }
                //被行会占领的，必须归属行会才可以
                if (info.ConquestIndex > 0)
                {
                    if (MyGuild == null || MyGuild.Conquest == null) continue;
                    if (MyGuild.Conquest.Info.Index != info.ConquestIndex) continue;
                }

                if (info.NeedMove) //use with ENTERMAP npc command
                {
                    NPCMoveMap = Envir.GetMap(info.MapIndex);
                    NPCMoveCoord = info.Destination;
                    continue;
                }

                Map temp = Envir.GetMap(info.MapIndex);

                if (temp == null || !temp.ValidPoint(info.Destination)|| !temp.MapOpen) continue;

                CurrentMap.RemoveObject(this);
                Broadcast(new S.ObjectRemove { ObjectID = ObjectID });

                CompleteMapMovement(temp, info.Destination, CurrentMap, CurrentLocation);
                return true;
            }

            return false;
        }
        //完成地图的转移
        private void CompleteMapMovement(params object[] data)
        {
            if (this == null) return;
            Map temp = (Map)data[0];
            Point destination = (Point)data[1];
            Map checkmap = (Map)data[2];
            Point checklocation = (Point)data[3];

            if (CurrentMap != checkmap || CurrentLocation != checklocation) return;

            bool mapChanged = temp != CurrentMap;

            CurrentMap = temp;
            CurrentLocation = destination;

            CurrentMap.AddObject(this);

            MovementTime = Envir.Time + MovementDelay;

            Enqueue(new S.MapChanged
            {
                FileName = CurrentMap.Info.FileName,
                Title = CurrentMap.getTitle(),
                MiniMap = CurrentMap.Info.MiniMap,
                BigMap = CurrentMap.Info.BigMap,
                Lights = CurrentMap.Info.Light,
                Location = CurrentLocation,
                Direction = Direction,
                MapDarkLight = CurrentMap.Info.MapDarkLight,
                Music = CurrentMap.Info.Music,
                DrawAnimation = CurrentMap.Info.DrawAnimation,
                CanFastRun = CurrentMap.Info.CanFastRun,
                SafeZones = CurrentMap.Info.SafeZones
            });

            if (RidingMount) RefreshMount();

            GetObjects();

            SafeZoneInfo szi = CurrentMap.GetSafeZone(CurrentLocation);

            if (szi != null)
            {
                BindLocation = szi.Location;
                BindMapIndex = CurrentMapIndex;
                InSafeZone = true;
            }
            else
                InSafeZone = false;

            if (mapChanged)
            {
                CallDefaultNPC(DefaultNPCType.MapEnter, CurrentMap.Info.Mcode);
            }

            if (Info.Married != 0)
            {
                CharacterInfo Lover = Envir.GetCharacterInfo(Info.Married);
                PlayerObject player = Envir.GetPlayer(Lover.Name);

                if (player != null) player.GetRelationship(false);
            }

            CheckConquest(true);
        }


        //传送到某个地图
        public override bool Teleport(Map temp, Point location, bool effects = true, byte effectnumber = 0)
        {
            if (!temp.MapOpen)
            {
                return false;
            }
            Map oldMap = CurrentMap;
            Point oldLocation = CurrentLocation;

            bool mapChanged = temp != oldMap;

            if (!base.Teleport(temp, location, effects)) return false;

            Enqueue(new S.MapChanged
            {
                FileName = CurrentMap.Info.FileName,
                Title = CurrentMap.getTitle(),
                MiniMap = CurrentMap.Info.MiniMap,
                BigMap = CurrentMap.Info.BigMap,
                Lights = CurrentMap.Info.Light,
                Location = CurrentLocation,
                Direction = Direction,
                MapDarkLight = CurrentMap.Info.MapDarkLight,
                Music = CurrentMap.Info.Music,
                DrawAnimation = CurrentMap.Info.DrawAnimation,
                CanFastRun = CurrentMap.Info.CanFastRun,
                SafeZones = CurrentMap.Info.SafeZones
            });

            if (effects) Enqueue(new S.ObjectTeleportIn { ObjectID = ObjectID, Type = effectnumber });

            //Cancel actions
            if (TradePartner != null)
                TradeCancel();

            if (ItemRentalPartner != null)
                CancelItemRental();

            if (RidingMount) RefreshMount();
            if (ActiveBlizzard) ActiveBlizzard = false;

            GetObjectsPassive();

            SafeZoneInfo szi = CurrentMap.GetSafeZone(CurrentLocation);

            if (szi != null)
            {
                BindLocation = szi.Location;
                BindMapIndex = CurrentMapIndex;
                InSafeZone = true;
            }
            else
                InSafeZone = false;

            CheckConquest();

            Fishing = false;
            Enqueue(GetFishInfo());

            if (mapChanged)
            {
                CallDefaultNPC(DefaultNPCType.MapEnter, CurrentMap.Info.Mcode);

                if (Info.Married != 0)
                {
                    CharacterInfo Lover = Envir.GetCharacterInfo(Info.Married);
                    PlayerObject player = Envir.GetPlayer(Lover.Name);

                    if (player != null) player.GetRelationship(false);
                }
            }

            if (CheckStacked())
            {
                StackingTime = Envir.Time + 1000;
                Stacking = true;
            }

            Report.MapChange("Teleported", oldMap.Info, CurrentMap.Info);

            return true;
        }
        //这个是地牢逃脱
        public bool TeleportEscape(int attempts)
        {
            Map temp = Envir.GetMap(BindMapIndex);

            for (int i = 0; i < attempts; i++)
            {
                Point location = temp.RandomValidPoint();
                if (Teleport(temp, location)) return true;
            }

            return false;
        }

        //这个是回城
        public void TeleportBackHome()
        {
            if (!Teleport(Envir.GetMap(BindMapIndex), BindLocation))
            {
                //如果回城有问题，就当做地牢吧
                if (!TeleportEscape(20))
                {
                    return;
                }
            }
            return;
        }

        private Packet GetMountInfo()
        {
            return new S.MountUpdate
            {
                ObjectID = ObjectID,
                RidingMount = RidingMount,
                MountType = MountType
            };
        }
        private Packet GetUpdateInfo()
        {
            UpdateConcentration();
            return new S.PlayerUpdate
            {
                ObjectID = ObjectID,
                Weapon = Looks_Weapon,
				WeaponEffect = Looks_WeaponEffect,
				Armour = Looks_Armour,
                Light = Light,
                WingEffect = Looks_Wings
            };
        }

        public override Packet GetInfo()
        {
            //should never use this but i leave it in for safety
            if (Observer) return null;

            string gName = "";
            string conquest = "";
            if (MyGuild != null)
            {
                gName = MyGuild.Name;
                if (MyGuild.Conquest != null)
                {
                    conquest = "[" + MyGuild.Conquest.Info.Name + "]";
                    gName = gName + conquest;
                }
                    
            }

            return new S.ObjectPlayer
            {
                ObjectID = ObjectID,
                Name = CurrentMap.Info.NoNames ? "?????" : PlayerTitleUtil.getPlayerTitleName(playerTitle)+Name,
                NameColour = NameColour,
                GuildName = CurrentMap.Info.NoNames ? "?????" : gName,
                GuildRankName = CurrentMap.Info.NoNames ? "?????" : MyGuildRank != null ? MyGuildRank.Name : "",
                Class = Class,
                Gender = Gender,
                Level = Level,
                Location = CurrentLocation,
                Direction = Direction,
                Hair = Hair,
                Weapon = Looks_Weapon,
				WeaponEffect = Looks_WeaponEffect,
				Armour = Looks_Armour,
                Light = Light,
                Poison = CurrentPoison,
                Dead = Dead,
                Hidden = Hidden,
                Effect = MagicShield ? SpellEffect.MagicShieldUp : ElementalBarrier ? SpellEffect.ElementalBarrierUp : SpellEffect.None,
                WingEffect = Looks_Wings,
                MountType = MountType,
                RidingMount = RidingMount,
                Fishing = Fishing,

                TransformType = TransformType,

                ElementOrbEffect = (uint)GetElementalOrbCount(),
                ElementOrbLvl = (uint)ElementsLevel,
                ElementOrbMax = (uint)Settings.OrbsExpList[Settings.OrbsExpList.Count - 1],

                Buffs = Buffs.Where(d => d.Visible).Select(e => e.Type).ToList(),

                LevelEffects = LevelEffects
            };
        }

        public Packet GetInfoEx(PlayerObject player)
        {
            var p = (S.ObjectPlayer)GetInfo();

            if (p != null)
            {
                //他妈的这里有毛病吧，其实没毛病
                //p.NameColour = GetNameColour(player);
                p.NameColour = GetNameColour(player);
            }

            return p;
        }

        public override bool IsAttackTarget(PlayerObject attacker)
        {
            if (attacker == null || attacker.Node == null) return false;
            //加入战场攻击模式
            if (WGroup != WarGroup.None)
            {
                if (WGroup != attacker.WGroup)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            
            if (Dead || InSafeZone || attacker.InSafeZone || attacker == this || GMGameMaster) return false;
            if (CurrentMap.Info.NoFight) return false;

            switch (attacker.AMode)
            {
                case AttackMode.All:
                    return true;
                case AttackMode.Group:
                    return GroupMembers == null || !GroupMembers.Contains(attacker);
                case AttackMode.Guild:
                    return MyGuild == null || MyGuild != attacker.MyGuild;
                case AttackMode.EnemyGuild:
                    return MyGuild != null && MyGuild.IsEnemy(attacker.MyGuild);
                case AttackMode.Peace:
                    return false;
                case AttackMode.RedBrown:
                    return PKPoints >= 200 || Envir.Time < BrownTime;
            }

            return true;
        }
        public override bool IsAttackTarget(MonsterObject attacker)
        {
            if (attacker == null || attacker.Node == null) return false;
            if (Dead || attacker.Master == this || GMGameMaster) return false;
            //加入战场攻击模式
            if (WGroup != WarGroup.None)
            {
                if (WGroup != attacker.WGroup)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            if (attacker.Info.AI == 6 || attacker.Info.AI == 58) return PKPoints >= 200;
            if (attacker.Master == null) return true;
            if (InSafeZone || attacker.InSafeZone || attacker.Master.InSafeZone) return false;

            if (LastHitter != attacker.Master && attacker.Master.LastHitter != this)
            {
                bool target = false;

                for (int i = 0; i < attacker.Master.Pets.Count; i++)
                {
                    if (attacker.Master.Pets[i].Target != this) continue;

                    target = true;
                    break;
                }

                if (!target)
                    return false;
            }

            switch (attacker.Master.AMode)
            {
                case AttackMode.All:
                    return true;
                case AttackMode.Group:
                    return GroupMembers == null || !GroupMembers.Contains(attacker.Master);
                case AttackMode.Guild:
                    return true;
                case AttackMode.EnemyGuild:
                    return false;
                case AttackMode.Peace:
                    return false;
                case AttackMode.RedBrown:
                    return PKPoints >= 200 || Envir.Time < BrownTime;
            }

            return true;

        }
        public override bool IsFriendlyTarget(PlayerObject ally)
        {
            if (ally == this|| ally==null) return true;
            //加入战场攻击模式
            if (WGroup != WarGroup.None)
            {
                if (WGroup != ally.WGroup)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            switch (ally.AMode)
            {
                case AttackMode.Group:
                    return GroupMembers != null && GroupMembers.Contains(ally);
                case AttackMode.RedBrown:
                    return PKPoints < 200 & Envir.Time > BrownTime;
                case AttackMode.Guild:
                    return MyGuild != null && MyGuild == ally.MyGuild;
                case AttackMode.EnemyGuild:
                    return true;
            }
            return true;
        }
        public override bool IsFriendlyTarget(MonsterObject ally)
        {
            if (ally.Race != ObjectType.Monster) return false;
            //加入战场攻击模式
            if (WGroup != WarGroup.None)
            {
                if (WGroup != ally.WGroup)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            if (ally.Master == null) return false;

            switch (ally.Master.Race)
            {
                case ObjectType.Player:
                    if (!ally.Master.IsFriendlyTarget(this)) return false;
                    break;
                case ObjectType.Monster:
                    return false;
            }

            return true;
        }

        public override int Attacked(PlayerObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            if (hasItemSk(ItemSkill.Warrior7) && RandomUtils.Next(100) < 25)
            {
                BroadcastDamageIndicator(DamageType.Miss);
                return 0;
            }
            //
            

            int armour = 0;

            for (int i = 0; i < Buffs.Count; i++)
            {
                switch (Buffs[i].Type)
                {
                    case BuffType.MoonLight:
                    case BuffType.DarkBody:
                        Buffs[i].ExpireTime = 0;
                        break;
                    case BuffType.EnergyShield:
                        int rate = Buffs[i].Values[0];

                        if (RandomUtils.Next(rate) == 0)
                        {
                        if (HP + ( (ushort)Buffs[i].Values[1] ) >= MaxHP)
                                SetHP(MaxHP);
                            else
                                ChangeHP(Buffs[i].Values[1]);
                        }
                        break;
                }
            }

            switch (type)
            {
                case DefenceType.ACAgility:
                    if (RandomUtils.Next(Agility + 1) > attacker.Accuracy)
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        return 0;
                    }
                    armour = GetDefencePower(MinAC, MaxAC);
                    break;
                case DefenceType.AC:
                    armour = GetDefencePower(MinAC, MaxAC);
                    break;
                case DefenceType.MACAgility:
                    if ((Settings.PvpCanResistMagic) && (RandomUtils.Next(Settings.MagicResistWeight) < MagicResist))
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        return 0;
                    }
                    if (RandomUtils.Next(Agility + 1) > attacker.Accuracy)
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        return 0;
                    }
                    armour = GetDefencePower(MinMAC, MaxMAC);
                    break;
                case DefenceType.MAC:
                    if ((Settings.PvpCanResistMagic) && (RandomUtils.Next(Settings.MagicResistWeight) < MagicResist))
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        return 0;
                    }
                    armour = GetDefencePower(MinMAC, MaxMAC);
                    break;
                case DefenceType.Agility:
                    if (RandomUtils.Next(Agility + 1) > attacker.Accuracy)
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        return 0;
                    }
                    break;
            }

            armour = (int)Math.Max(int.MinValue, (Math.Min(int.MaxValue, (decimal)(armour * ArmourRate))));
            damage = (int)Math.Max(int.MinValue, (Math.Min(int.MaxValue, (decimal)(damage * DamageRate))));

            if (damageWeapon)
                attacker.DamageWeapon();

            damage += attacker.AttackBonus;

            if (RandomUtils.Next(100) < Reflect)
            {
                if (attacker.IsAttackTarget(this))
                {
                    attacker.Attacked(this, damage, type, false);
                    CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Reflect }, CurrentLocation);
                }
                return 0;
            }

            if (MagicShield)
                damage -= damage * (MagicShieldLv + 2) / 10;

            if (ElementalBarrier)
                damage -= damage * (ElementalBarrierLv + 1) / 10;

            if (armour >= damage)
            {
                BroadcastDamageIndicator(DamageType.Miss);
                return 0;
            }

            if ((attacker.CriticalRate * Settings.CriticalRateWeight) > RandomUtils.Next(100))
            {
                CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Critical }, CurrentLocation);
                damage = Math.Min(int.MaxValue, damage + (int)Math.Floor(damage * (((double)attacker.CriticalDamage / (double)Settings.CriticalDamageWeight) * 10)));
                BroadcastDamageIndicator(DamageType.Critical);
            }

            if (MagicShield)
            {
                MagicShieldTime -= (damage - armour) * 60;
                AddBuff(new Buff { Type = BuffType.MagicShield, Caster = this, ExpireTime = MagicShieldTime, Values = new int[] { MagicShieldLv } });
            }

            ElementalBarrierTime -= (damage - armour) * 60;

            if (attacker.LifeOnHit > 0)
                attacker.ChangeHP(attacker.LifeOnHit);

            if (attacker.HpDrainRate > 0)
            {
                attacker.HpDrain += Math.Max(0, ((float)(damage - armour) / 100) * attacker.HpDrainRate);
                if (attacker.HpDrain > 2)
                {
                    int HpGain = (int)Math.Floor(attacker.HpDrain);
                    attacker.ChangeHP(HpGain);
                    attacker.HpDrain -= HpGain;

                }
            }

            for (int i = PoisonList.Count - 1; i >= 0; i--)
            {
                if (PoisonList[i].PType != PoisonType.LRParalysis) continue;

                PoisonList.RemoveAt(i);
                OperateTime = 0;
            }


            LastHitter = attacker;
            LastHitTime = Envir.Time + 10000;
            RegenTime = Envir.Time + RegenDelay;
            LogTime = Envir.Time + Globals.LogDelay;

            if (Envir.Time > BrownTime && PKPoints < 200 && !AtWar(attacker))
                attacker.BrownTime = Envir.Time + Settings.Minute;

            ushort LevelOffset = (byte)(Level > attacker.Level ? 0 : Math.Min(10, attacker.Level - Level));

            if (attacker.HasParalysisRing && type != DefenceType.MAC && type != DefenceType.MACAgility && 1 == RandomUtils.Next(1, 15))
            {
                ApplyPoison(new Poison { PType = PoisonType.Paralysis, Duration = 5, TickSpeed = 1000 }, attacker);
            }
            if ((attacker.Freezing > 0) && (Settings.PvpCanFreeze) && type != DefenceType.MAC && type != DefenceType.MACAgility)
            {
                if ((RandomUtils.Next(Settings.FreezingAttackWeight) < attacker.Freezing) && (RandomUtils.Next(LevelOffset) == 0))
                    ApplyPoison(new Poison { PType = PoisonType.Slow, Duration = Math.Min(10, (3 + RandomUtils.Next(attacker.Freezing))), TickSpeed = 1000 }, attacker);
            }

            if (attacker.PoisonAttack > 0 && type != DefenceType.MAC && type != DefenceType.MACAgility)
            {
                if ((RandomUtils.Next(Settings.PoisonAttackWeight) < attacker.PoisonAttack) && (RandomUtils.Next(LevelOffset) == 0))
                    ApplyPoison(new Poison { PType = PoisonType.Green, Duration = 5, TickSpeed = 1000, Value = Math.Min(10, 3 + RandomUtils.Next(attacker.PoisonAttack)) }, attacker);
            }

            attacker.GatherElement();

            DamageDura();
            ActiveBlizzard = false;
            ActiveReincarnation = false;

            CounterAttackCast(GetMagic(Spell.CounterAttack), LastHitter);

            Enqueue(new S.Struck { AttackerID = attacker.ObjectID });
            Broadcast(new S.ObjectStruck { ObjectID = ObjectID, AttackerID = attacker.ObjectID, Direction = Direction, Location = CurrentLocation });

            BroadcastDamageIndicator(DamageType.Hit, armour - damage);

            //
            int loseHp = damage - armour;
            if (loseHp < 0)
            {
                return 0;
            }
            //契约兽承受伤害
            if (Pets != null)
            {
                foreach (MonsterObject p in Pets)
                {
                    if (p.Dead)
                    {
                        continue;
                    }
                    if (p.hasMonSk(MyMonSkill.MyMonSK1))
                    {
                        int mhp = loseHp * 25 / 100;
                        p.ChangeHP(-(mhp));
                        loseHp = loseHp - mhp;
                        break;
                    }
                }
            }

            ChangeHP(-loseHp);
            return loseHp;
        }
        public override int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility)
        {
            if (hasItemSk(ItemSkill.Warrior7) && RandomUtils.Next(100) < 20)
            {
                BroadcastDamageIndicator(DamageType.Miss);
                return 0;
            }

            int armour = 0;

                for (int i = 0; i < Buffs.Count; i++)
                {
                    switch (Buffs[i].Type)
                    {
                        case BuffType.MoonLight:
                        case BuffType.DarkBody:
                            Buffs[i].ExpireTime = 0;
                            break;
                        case BuffType.EnergyShield:
                            int rate = Buffs[i].Values[0];

                            if (RandomUtils.Next(rate < 2 ? 2 : rate) == 0)
                            {
                                if (HP + ((ushort)Buffs[i].Values[1]) >= MaxHP)
                                    SetHP(MaxHP);
                                else
                                    ChangeHP(Buffs[i].Values[1]);
                            }
                            break;
                    }
                }

            switch (type)
            {
                case DefenceType.ACAgility:
                    if (RandomUtils.Next(Agility + 1) > attacker.Accuracy)
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        return 0;
                    }
                    armour = GetDefencePower(MinAC, MaxAC);
                    break;
                case DefenceType.AC:
                    armour = GetDefencePower(MinAC, MaxAC);
                    break;
                case DefenceType.MACAgility:
                    if (RandomUtils.Next(Settings.MagicResistWeight) < MagicResist)
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        return 0;
                    }
                    if (RandomUtils.Next(Agility + 1) > attacker.Accuracy)
                    {
                        return 0;
                    }
                    armour = GetDefencePower(MinMAC, MaxMAC);
                    break;
                case DefenceType.MAC:
                    if (RandomUtils.Next(Settings.MagicResistWeight) < MagicResist)
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        return 0;
                    }
                    armour = GetDefencePower(MinMAC, MaxMAC);
                    break;
                case DefenceType.Agility:
                    if (RandomUtils.Next(Agility + 1) > attacker.Accuracy)
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        return 0;
                    }
                    break;
            }

            if (RandomUtils.Next(100) < Reflect)
            {
                if (attacker.IsAttackTarget(this))
                {
                    attacker.Attacked(this, damage, type, false);
                    CurrentMap.Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Reflect }, CurrentLocation);
                }
                return 0;
            }

            armour = (int)Math.Max(int.MinValue, (Math.Min(int.MaxValue, (decimal)(armour * ArmourRate))));
            damage = (int)Math.Max(int.MinValue, (Math.Min(int.MaxValue, (decimal)(damage * DamageRate))));

            if (MagicShield)
                damage -= damage * (MagicShieldLv + 2) / 10;

            if (ElementalBarrier)
                damage -= damage * (ElementalBarrierLv + 1) / 10;

            if (armour >= damage)
            {
                BroadcastDamageIndicator(DamageType.Miss);
                return 0;
            }

            if (MagicShield)
            {
                MagicShieldTime -= (damage - armour) * 60;
                AddBuff(new Buff { Type = BuffType.MagicShield, Caster = this, ExpireTime = MagicShieldTime, Values = new int[] { MagicShieldLv } });
            }

            ElementalBarrierTime -= (damage - armour) * 60;

            for (int i = PoisonList.Count - 1; i >= 0; i--)
            {
                if (PoisonList[i].PType != PoisonType.LRParalysis) continue;

                PoisonList.RemoveAt(i);
                OperateTime = 0;
            }

            LastHitter = attacker.Master ?? attacker;
            LastHitTime = Envir.Time + 10000;
            RegenTime = Envir.Time + RegenDelay;
            LogTime = Envir.Time + Globals.LogDelay;

            DamageDura();
            ActiveBlizzard = false;
            ActiveReincarnation = false;

            CounterAttackCast(GetMagic(Spell.CounterAttack), LastHitter);

            if (StruckTime < Envir.Time)
            {
                Enqueue(new S.Struck { AttackerID = attacker.ObjectID });
                Broadcast(new S.ObjectStruck { ObjectID = ObjectID, AttackerID = attacker.ObjectID, Direction = Direction, Location = CurrentLocation });
                StruckTime = Envir.Time + 500;
            }

            BroadcastDamageIndicator(DamageType.Hit, armour - damage);


            //
            int loseHp = damage - armour;
            if (loseHp < 0)
            {
                return 0;
            }
            //契约兽承受伤害
            if (Pets != null)
            {
                foreach (MonsterObject p in Pets)
                {
                    if (p.Dead)
                    {
                        continue;
                    }
                    if (p.hasMonSk(MyMonSkill.MyMonSK1))
                    {
                        int mhp = loseHp * 30 / 100;
                        p.ChangeHP(-(mhp));
                        loseHp = loseHp - mhp;
                        break;
                    }
                }
            }
            
            ChangeHP(-loseHp);
            return loseHp;
        }
        public override int Struck(int damage, DefenceType type = DefenceType.ACAgility)
        {
            if (hasItemSk(ItemSkill.Warrior7) && RandomUtils.Next(100) < 20)
            {
                return 0;
            }

            int armour = 0;
            if (Hidden)
            {
                for (int i = 0; i < Buffs.Count; i++)
                {
                    switch (Buffs[i].Type)
                    {
                        case BuffType.MoonLight:
                        case BuffType.DarkBody:
                            Buffs[i].ExpireTime = 0;
                            break;
                    }
                }
            }

            switch (type)
            {
                case DefenceType.ACAgility:
                    armour = GetDefencePower(MinAC, MaxAC);
                    break;
                case DefenceType.AC:
                    armour = GetDefencePower(MinAC, MaxAC);
                    break;
                case DefenceType.MACAgility:
                    armour = GetDefencePower(MinMAC, MaxMAC);
                    break;
                case DefenceType.MAC:
                    armour = GetDefencePower(MinMAC, MaxMAC);
                    break;
                case DefenceType.Agility:
                    break;
            }

            armour = (int)Math.Max(int.MinValue, (Math.Min(int.MaxValue, (decimal)(armour * ArmourRate))));
            damage = (int)Math.Max(int.MinValue, (Math.Min(int.MaxValue, (decimal)(damage * DamageRate))));

            if (MagicShield)
                damage -= damage * (MagicShieldLv + 2) / 10;

            if (ElementalBarrier)
                damage -= damage * (ElementalBarrierLv + 1) / 10;

            if (armour >= damage) return 0;

            if (MagicShield)
            {
                MagicShieldTime -= (damage - armour) * 60;
                AddBuff(new Buff { Type = BuffType.MagicShield, Caster = this, ExpireTime = MagicShieldTime, Values = new int[] { MagicShieldLv } });
            }

            ElementalBarrierTime -= (damage - armour) * 60;
            RegenTime = Envir.Time + RegenDelay;
            LogTime = Envir.Time + Globals.LogDelay;

            DamageDura();
            ActiveBlizzard = false;
            ActiveReincarnation = false;
            Enqueue(new S.Struck { AttackerID = 0 });
            Broadcast(new S.ObjectStruck { ObjectID = ObjectID, AttackerID = 0, Direction = Direction, Location = CurrentLocation });

            ChangeHP(armour - damage);
            return damage - armour;
        }
        public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true)
        {
            if ((Caster != null) && (!NoResist))
                if (((Caster.Race != ObjectType.Player) || Settings.PvpCanResistPoison) && (RandomUtils.Next(Settings.PoisonResistWeight) < PoisonResist))
                    return;

            if (!ignoreDefence && (p.PType == PoisonType.Green))
            {
                int armour = GetDefencePower(MinMAC, MaxMAC);

                if (p.Value < armour)
                    p.PType = PoisonType.None;
                else
                    p.Value -= armour;
            }

            if (p.Owner != null && p.Owner.Race == ObjectType.Player && Envir.Time > BrownTime && PKPoints < 200)
                p.Owner.BrownTime = Envir.Time + Settings.Minute;

            if ((p.PType == PoisonType.Green) || (p.PType == PoisonType.Red)) p.Duration = Math.Max(0, p.Duration - PoisonRecovery);
            if (p.Duration == 0) return;
            if (p.PType == PoisonType.None) return;

            for (int i = 0; i < PoisonList.Count; i++)
            {
                if (PoisonList[i].PType != p.PType) continue;
                if ((PoisonList[i].PType == PoisonType.Green) && (PoisonList[i].Value > p.Value)) return;//cant cast weak poison to cancel out strong poison无法施放弱毒以消除强毒
                if ((PoisonList[i].PType != PoisonType.Green) && ((PoisonList[i].Duration - PoisonList[i].Time) > p.Duration)) return;//cant cast 1 second poison to make a 1minute poison go away!
                if ((PoisonList[i].PType == PoisonType.Frozen) || (PoisonList[i].PType == PoisonType.Slow) || (PoisonList[i].PType == PoisonType.Paralysis) || (PoisonList[i].PType == PoisonType.LRParalysis)) return;//prevents mobs from being perma frozen/slowed
                if (p.PType == PoisonType.DelayedExplosion) return;
                ReceiveChat("你中毒了.", ChatType.System2);
                //毒覆盖，不叠加
                PoisonList[i] = p;
                return;
            }

            if (p.PType == PoisonType.DelayedExplosion)
            {
                ExplosionInflictedTime = Envir.Time + 4000;
                Enqueue(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion });
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion });
                ReceiveChat("你中毒了.", ChatType.System);
            }
            else
                ReceiveChat("你中毒了.", ChatType.System2);

            PoisonList.Add(p);
        }

        public override void AddBuff(Buff b)
        {
            if (Buffs.Any(d => d.Infinite && d.Type == b.Type)) return; //cant overwrite infinite buff with regular buff

            base.AddBuff(b);

            string caster = b.Caster != null ? b.Caster.Name : string.Empty;

            if (b.Values == null) b.Values = new int[1];

            S.AddBuff addBuff = new S.AddBuff { Type = b.Type, Caster = caster, Expire = b.ExpireTime - Envir.Time, Values = b.Values, Infinite = b.Infinite, ObjectID = ObjectID, Visible = b.Visible };
            Enqueue(addBuff);

            if (b.Visible) Broadcast(addBuff);

            RefreshStats();
        }
        public void PauseBuff(Buff b)
        {
            if (b.Paused) return;

            b.ExpireTime = b.ExpireTime - Envir.Time;
            b.Paused = true;
            Enqueue(new S.RemoveBuff { Type = b.Type, ObjectID = ObjectID });
        }
        public void UnpauseBuff(Buff b)
        {
            if (!b.Paused) return;

            b.ExpireTime = b.ExpireTime + Envir.Time;
            b.Paused = false;
            Enqueue(new S.AddBuff { Type = b.Type, Caster = Name, Expire = b.ExpireTime - Envir.Time, Values = b.Values, Infinite = b.Infinite, ObjectID = ObjectID, Visible = b.Visible });
        }

        public void EquipSlotItem(MirGridType grid, ulong id, int to, MirGridType gridTo)
        {
            S.EquipSlotItem p = new S.EquipSlotItem { Grid = grid, UniqueID = id, To = to, GridTo = gridTo, Success = false };

            UserItem Item = null;

            switch (gridTo)
            {
                case MirGridType.Mount:
                    Item = Info.Equipment[(int)EquipmentSlot.Mount];
                    break;
                case MirGridType.Fishing:
                    Item = Info.Equipment[(int)EquipmentSlot.Weapon];
                    break;
                default:
                    Enqueue(p);
                    return;
            }

            if (Item == null || Item.Slots == null)
            {
                Enqueue(p);
                return;
            }

            if (gridTo == MirGridType.Fishing && (Item.Info.Shape != 49 && Item.Info.Shape != 50))
            {
                Enqueue(p);
                return;
            }

            if (to < 0 || to >= Item.Slots.Length)
            {
                Enqueue(p);
                return;
            }

            if (Item.Slots[to] != null)
            {
                Enqueue(p);
                return;
            }

            UserItem[] array;
            switch (grid)
            {
                case MirGridType.Inventory:
                    array = Info.Inventory;
                    break;
                case MirGridType.Storage:
                    if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.StorageKey, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Enqueue(p);
                        return;
                    }
                    NPCObject ob = null;
                    for (int i = 0; i < CurrentMap.NPCs.Count; i++)
                    {
                        if (CurrentMap.NPCs[i].ObjectID != NPCID) continue;
                        ob = CurrentMap.NPCs[i];
                        break;
                    }

                    if (ob == null || !Functions.InRange(ob.CurrentLocation, CurrentLocation, Globals.DataRange))
                    {
                        Enqueue(p);
                        return;
                    }
                    
                    if (Info.Equipment[to] != null &&
                        Info.Equipment[to].Info.Bind.HasFlag(BindMode.DontStore))
                    {
                        Enqueue(p);
                        return;
                    }
                    array = Account.Storage;
                    break;
                default:
                    Enqueue(p);
                    return;
            }


            int index = -1;
            UserItem temp = null;

            for (int i = 0; i < array.Length; i++)
            {
                temp = array[i];
                if (temp == null || temp.UniqueID != id) continue;
                index = i;
                break;
            }

            if (temp == null || index == -1)
            {
                Enqueue(p);
                return;
            }

            if ((temp.SoulBoundId != -1) && (temp.SoulBoundId != (long)Info.Index))
            {
                Enqueue(p);
                return;
            }


            if (CanUseItem(temp))
            {
                if (temp.Info.NeedIdentify && !temp.Identified)
                {
                    temp.Identified = true;
                    Enqueue(new S.RefreshItem { Item = temp });
                }
                //if ((temp.Info.BindOnEquip) && (temp.SoulBoundId == -1))
                //{
                //    temp.SoulBoundId = Info.Index;
                //    Enqueue(new S.RefreshItem { Item = temp });
                //}
                //if (UnlockCurse && Info.Equipment[to].Cursed)
                //    UnlockCurse = false;

                Item.Slots[to] = temp;
                array[index] = null;

                p.Success = true;
                Enqueue(p);
                RefreshStats();

                Report.ItemMoved("EquipSlotItem", temp, grid, gridTo, index, to);

                return;
            }

            Enqueue(p);
        }
        public void RemoveItem(MirGridType grid, ulong id, int to)
        {
            S.RemoveItem p = new S.RemoveItem { Grid = grid, UniqueID = id, To = to, Success = false };
            UserItem[] array;
            switch (grid)
            {
                case MirGridType.Inventory:
                    array = Info.Inventory;
                    break;
                case MirGridType.Storage:
                    if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.StorageKey, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Enqueue(p);
                        return;
                    }
                    NPCObject ob = null;
                    for (int i = 0; i < CurrentMap.NPCs.Count; i++)
                    {
                        if (CurrentMap.NPCs[i].ObjectID != NPCID) continue;
                        ob = CurrentMap.NPCs[i];
                        break;
                    }

                    if (ob == null || !Functions.InRange(ob.CurrentLocation, CurrentLocation, Globals.DataRange))
                    {
                        Enqueue(p);
                        return;
                    }
                    array = Account.Storage;
                    break;
                default:
                    Enqueue(p);
                    return;
            }

            if (to < 0 || to >= array.Length) return;

            UserItem temp = null;
            int index = -1;

            for (int i = 0; i < Info.Equipment.Length; i++)
            {
                temp = Info.Equipment[i];
                if (temp == null || temp.UniqueID != id) continue;
                index = i;
                break;
            }

            if (temp == null || index == -1)
            {
                Enqueue(p);
                return;
            }

            if (temp.Cursed && !UnlockCurse)
            {
                Enqueue(p);
                return;
            }

            if (temp.WeddingRing != -1)
            {
                Enqueue(p);
                return;
            }

            if (!CanRemoveItem(grid, temp)) return;

            if (temp.Cursed)
                UnlockCurse = false;

            if (array[to] == null)
            {
                Info.Equipment[index] = null;

                array[to] = temp;
                p.Success = true;
                Enqueue(p);
                RefreshStats();
                Broadcast(GetUpdateInfo());

                Report.ItemMoved("RemoveItem", temp, MirGridType.Equipment, grid, index, to);

                return;
            }

            Enqueue(p);
        }
        public void RemoveSlotItem(MirGridType grid, ulong id, int to, MirGridType gridTo)
        {
            S.RemoveSlotItem p = new S.RemoveSlotItem { Grid = grid, UniqueID = id, To = to, GridTo = gridTo, Success = false };
            UserItem[] array;
            switch (gridTo)
            {
                case MirGridType.Inventory:
                    array = Info.Inventory;
                    break;
                case MirGridType.Storage:
                    if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.StorageKey, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Enqueue(p);
                        return;
                    }
                    NPCObject ob = null;
                    for (int i = 0; i < CurrentMap.NPCs.Count; i++)
                    {
                        if (CurrentMap.NPCs[i].ObjectID != NPCID) continue;
                        ob = CurrentMap.NPCs[i];
                        break;
                    }

                    if (ob == null || !Functions.InRange(ob.CurrentLocation, CurrentLocation, Globals.DataRange))
                    {
                        Enqueue(p);
                        return;
                    }
                    array = Account.Storage;
                    break;
                default:
                    Enqueue(p);
                    return;
            }

            if (to < 0 || to >= array.Length) return;

            UserItem temp = null;
            UserItem slotTemp = null;
            int index = -1;

            switch (grid)
            {
                case MirGridType.Mount:
                    temp = Info.Equipment[(int)EquipmentSlot.Mount];
                    break;
                case MirGridType.Fishing:
                    temp = Info.Equipment[(int)EquipmentSlot.Weapon];
                    break;
                default:
                    Enqueue(p);
                    return;
            }

            if (temp == null || temp.Slots == null)
            {
                Enqueue(p);
                return;
            }

            if (grid == MirGridType.Fishing && (temp.Info.Shape != 49 && temp.Info.Shape != 50))
            {
                Enqueue(p);
                return;
            }

            for (int i = 0; i < temp.Slots.Length; i++)
            {
                slotTemp = temp.Slots[i];
                if (slotTemp == null || slotTemp.UniqueID != id) continue;
                index = i;
                break;
            }

            if (slotTemp == null || index == -1)
            {
                Enqueue(p);
                return;
            }

            if (slotTemp.Cursed && !UnlockCurse)
            {
                Enqueue(p);
                return;
            }

            if (slotTemp.WeddingRing != -1)
            {
                Enqueue(p);
                return;
            }

            if (!CanRemoveItem(gridTo, slotTemp)) return;

            temp.Slots[index] = null;

            if (slotTemp.Cursed)
                UnlockCurse = false;

            if (array[to] == null)
            {
                array[to] = slotTemp;
                p.Success = true;
                Enqueue(p);
                RefreshStats();
                Broadcast(GetUpdateInfo());

                Report.ItemMoved("RemoveSlotItem", temp, grid, gridTo, index, to);

                return;
            }

            Enqueue(p);
        }
        public void MoveItem(MirGridType grid, int from, int to)
        {
            S.MoveItem p = new S.MoveItem { Grid = grid, From = from, To = to, Success = false };
            UserItem[] array;
            switch (grid)
            {
                case MirGridType.Inventory:
                    array = Info.Inventory;
                    break;
                case MirGridType.Storage:
                    if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.StorageKey, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Enqueue(p);
                        return;
                    }
                    NPCObject ob = null;
                    for (int i = 0; i < CurrentMap.NPCs.Count; i++)
                    {
                        if (CurrentMap.NPCs[i].ObjectID != NPCID) continue;
                        ob = CurrentMap.NPCs[i];
                        break;
                    }

                    if (ob == null || !Functions.InRange(ob.CurrentLocation, CurrentLocation, Globals.DataRange))
                    {
                        Enqueue(p);
                        return;
                    }
                    array = Account.Storage;
                    break;
                case MirGridType.Trade:
                    array = Info.Trade;
                    TradeItem();
                    break;
                case MirGridType.Refine:
                    array = Info.Refine;
                    break;
                default:
                    Enqueue(p);
                    return;
            }

            if (from >= 0 && to >= 0 && from < array.Length && to < array.Length)
            {
                if (array[from] == null)
                {
                    Report.ItemError("MoveItem", grid, grid, from, to);
                    ReceiveChat("物品移动错误 - 请报告你移动的物品和时间", ChatType.System);
                    Enqueue(p);
                    return;
                }

                UserItem i = array[to];
                array[to] = array[from];

                Report.ItemMoved("MoveItem", array[to], grid, grid, from, to);

                array[from] = i;

                Report.ItemMoved("MoveItem", array[from], grid, grid, to, from);
                
                p.Success = true;
                Enqueue(p);
                return;
            }

            Enqueue(p);
        }
        public void StoreItem(int from, int to)
        {
            S.StoreItem p = new S.StoreItem { From = from, To = to, Success = false };

            if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.StorageKey, StringComparison.CurrentCultureIgnoreCase))
            {
                Enqueue(p);
                return;
            }
            NPCObject ob = null;
            for (int i = 0; i < CurrentMap.NPCs.Count; i++)
            {
                if (CurrentMap.NPCs[i].ObjectID != NPCID) continue;
                ob = CurrentMap.NPCs[i];
                break;
            }

            if (ob == null || !Functions.InRange(ob.CurrentLocation, CurrentLocation, Globals.DataRange))
            {
                Enqueue(p);
                return;
            }


            if (from < 0 || from >= Info.Inventory.Length)
            {
                Enqueue(p);
                return;
            }

            if (to < 0 || to >= Account.Storage.Length)
            {
                Enqueue(p);
                return;
            }

            UserItem temp = Info.Inventory[from];

            if (temp == null)
            {
                Enqueue(p);
                return;
            }

            if (temp.Info.Bind.HasFlag(BindMode.DontStore))
            {
                Enqueue(p);
                return;
            }

            if (temp.RentalInformation != null && temp.RentalInformation.BindingFlags.HasFlag(BindMode.DontStore))
            {
                Enqueue(p);
                return;
            }

            if (Account.Storage[to] == null)
            {
                Account.Storage[to] = temp;
                Info.Inventory[from] = null;
                RefreshBagWeight();

                Report.ItemMoved("StoreItem", temp, MirGridType.Inventory, MirGridType.Storage, from, to);

                p.Success = true;
                Enqueue(p);
                return;
            }
            Enqueue(p);
        }
        public void TakeBackItem(int from, int to)
        {
            S.TakeBackItem p = new S.TakeBackItem { From = from, To = to, Success = false };

            if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.StorageKey, StringComparison.CurrentCultureIgnoreCase))
            {
                Enqueue(p);
                return;
            }
            NPCObject ob = null;
            for (int i = 0; i < CurrentMap.NPCs.Count; i++)
            {
                if (CurrentMap.NPCs[i].ObjectID != NPCID) continue;
                ob = CurrentMap.NPCs[i];
                break;
            }

            if (ob == null || !Functions.InRange(ob.CurrentLocation, CurrentLocation, Globals.DataRange))
            {
                Enqueue(p);
                return;
            }


            if (from < 0 || from >= Account.Storage.Length)
            {
                Enqueue(p);
                return;
            }

            if (to < 0 || to >= Info.Inventory.Length)
            {
                Enqueue(p);
                return;
            }

            UserItem temp = Account.Storage[from];

            if (temp == null)
            {
                Enqueue(p);
                return;
            }

            if (temp.Weight + CurrentBagWeight > MaxBagWeight)
            {
                ReceiveChat("太重了.", ChatType.System);
                Enqueue(p);
                return;
            }

            if (Info.Inventory[to] == null)
            {
                Info.Inventory[to] = temp;
                Account.Storage[from] = null;

                Report.ItemMoved("TakeBackStoreItem", temp, MirGridType.Storage, MirGridType.Inventory, from, to);

                p.Success = true;
                RefreshBagWeight();
                Enqueue(p);

                return;
            }
            Enqueue(p);
        }
        public void EquipItem(MirGridType grid, ulong id, int to)
        {
            S.EquipItem p = new S.EquipItem { Grid = grid, UniqueID = id, To = to, Success = false };

            if (Fishing)
            {
                Enqueue(p);
                return;
            }

            if (to < 0 || to >= Info.Equipment.Length)
            {
                Enqueue(p);
                return;
            }

            UserItem[] array;
            switch (grid)
            {
                case MirGridType.Inventory:
                    array = Info.Inventory;
                    break;
                case MirGridType.Storage:
                    if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.StorageKey, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Enqueue(p);
                        return;
                    }
                    NPCObject ob = null;
                    for (int i = 0; i < CurrentMap.NPCs.Count; i++)
                    {
                        if (CurrentMap.NPCs[i].ObjectID != NPCID) continue;
                        ob = CurrentMap.NPCs[i];
                        break;
                    }

                    if (ob == null || !Functions.InRange(ob.CurrentLocation, CurrentLocation, Globals.DataRange))
                    {
                        Enqueue(p);
                        return;
                    }
                    array = Account.Storage;
                    break;
                default:
                    Enqueue(p);
                    return;
            }


            int index = -1;
            UserItem temp = null;

            for (int i = 0; i < array.Length; i++)
            {
                temp = array[i];
                if (temp == null || temp.UniqueID != id) continue;
                index = i;
                break;
            }

            if (temp == null || index == -1)
            {
                Enqueue(p);
                return;
            }
            if ((Info.Equipment[to] != null) && (Info.Equipment[to].Cursed) && (!UnlockCurse))
            {
                Enqueue(p);
                return;
            }

            if ((temp.SoulBoundId != -1) && (temp.SoulBoundId != (long)Info.Index))
            {
                Enqueue(p);
                return;
            }

            if (Info.Equipment[to] != null)
                if (Info.Equipment[to].WeddingRing != -1)
                {
                    Enqueue(p);
                    return;
                }


            if (CanEquipItem(temp, to))
            {
                if (temp.Info.NeedIdentify && !temp.Identified)
                {
                    temp.Identified = true;
                    Enqueue(new S.RefreshItem { Item = temp });
                }
                if ((temp.Info.Bind.HasFlag(BindMode.BindOnEquip)) && (temp.SoulBoundId == -1))
                {
                    temp.SoulBoundId = (long)Info.Index;
                    Enqueue(new S.RefreshItem { Item = temp });
                }

                if ((Info.Equipment[to] != null) && (Info.Equipment[to].Cursed) && (UnlockCurse))
                    UnlockCurse = false;

                array[index] = Info.Equipment[to];

                Report.ItemMoved("RemoveItem", temp, MirGridType.Equipment, grid, to, index);

                Info.Equipment[to] = temp;

                Report.ItemMoved("EquipItem", temp, grid, MirGridType.Equipment, index, to);

                p.Success = true;
                Enqueue(p);
                RefreshStats();

                //Broadcast(GetUpdateInfo());
                return;
            }
            Enqueue(p);
        }

        //使用某个物品
        public void UseItem(ulong id)
        {
            S.UseItem p = new S.UseItem { UniqueID = id, Success = false };

            UserItem item = null;
            int index = -1;

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                item = Info.Inventory[i];
                if (item == null || item.UniqueID != id) continue;
                index = i;
                break;
            }

            if (item == null || index == -1 || !CanUseItem(item))
            {
                Enqueue(p);
                return;
            }

            if (Dead && !(item.Info.Type == ItemType.Scroll && item.Info.Shape == 6))
            {
                Enqueue(p);
                return;
            }

            switch (item.Info.Type)
            {
                case ItemType.Potion://药水
                    switch (item.Info.Shape)
                    {
                        case 0: //NormalPotion(普通药水)
                            PotHealthAmount = (ushort)Math.Min(ushort.MaxValue, PotHealthAmount + item.Info.HP);
                            PotManaAmount = (ushort)Math.Min(ushort.MaxValue, PotManaAmount + item.Info.MP);
                            break;
                        case 1: //SunPotion（快速恢复药）
                            ChangeHP(item.Info.HP);
                            ChangeMP(item.Info.MP);
                            break;
                        case 2: //MysteryWater
                            if (UnlockCurse)
                            {
                                ReceiveChat("你已经可以解除诅咒的物品.", ChatType.Hint);
                                Enqueue(p);
                                return;
                            }
                            ReceiveChat("现在你可以解除诅咒的物品.", ChatType.Hint);
                            UnlockCurse = true;
                            break;
                        case 3: //Buff
                            int time = item.Info.Durability;

                            if ((item.Info.MaxDC + item.DC) > 0)
                                AddBuff(new Buff { Type = BuffType.Impact, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.MaxDC + item.DC } });

                            if ((item.Info.MaxMC + item.MC) > 0)
                                AddBuff(new Buff { Type = BuffType.Magic, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.MaxMC + item.MC } });

                            if ((item.Info.MaxSC + item.SC) > 0)
                                AddBuff(new Buff { Type = BuffType.Taoist, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.MaxSC + item.SC } });

                            if ((item.Info.AttackSpeed + item.AttackSpeed) > 0)
                                AddBuff(new Buff { Type = BuffType.Storm, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.AttackSpeed + item.AttackSpeed } });

                            if ((item.Info.HP + item.HP) > 0)
                                AddBuff(new Buff { Type = BuffType.HealthAid, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.HP + item.HP } });

                            if ((item.Info.MP + item.MP) > 0)
                                AddBuff(new Buff { Type = BuffType.ManaAid, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.MP + item.MP } });

                            if ((item.Info.MaxAC + item.AC) > 0)
                                AddBuff(new Buff { Type = BuffType.Defence, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.MaxAC + item.AC } });

                            if ((item.Info.MaxMAC + item.MAC) > 0)
                                AddBuff(new Buff { Type = BuffType.MagicDefence, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.MaxMAC + item.MAC } });
                            break;
                        case 4: //Exp
                            time = item.Info.Durability;

                            AddBuff(new Buff { Type = BuffType.Exp, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.Luck + item.Luck } });
                            break;
                    }
                    break;
                case ItemType.Scroll://（卷轴）
                    UserItem temp;
                    switch (item.Info.Shape)
                    {
                        case 0: //DE 地牢
                            if (!TeleportEscape(20))
                            {
                                Enqueue(p);
                                return;
                            }
                            break;
                        case 1: //TT 回城
                            if (!Teleport(Envir.GetMap(BindMapIndex), BindLocation))
                            {
                                //如果回城有问题，就当做地牢吧
                                if (!TeleportEscape(20))
                                {
                                    Enqueue(p);
                                    return;
                                }
                            }
                            break;
                        case 2: //RT 随机
                            if (!TeleportRandom(200, item.Info.Durability))
                            {
                                Enqueue(p);
                                return;
                            }
                            break;
                        case 3: //BenedictionOil 祝福油
                            if (!TryLuckWeapon())
                            {
                                Enqueue(p);
                                return;
                            }
                            break;
                        case 4: //RepairOil
                            temp = Info.Equipment[(int)EquipmentSlot.Weapon];
                            if (temp == null || temp.MaxDura == temp.CurrentDura)
                            {
                                Enqueue(p);
                                return;
                            }
                            if (temp.Info.Bind.HasFlag(BindMode.DontRepair))
                            {
                                Enqueue(p);
                                return;
                            }
                            temp.MaxDura = (ushort)Math.Max(0, temp.MaxDura - Math.Min(5000, temp.MaxDura - temp.CurrentDura) / 30);

                            temp.CurrentDura = (ushort)Math.Min(temp.MaxDura, temp.CurrentDura + 5000);
                            temp.DuraChanged = false;

                            ReceiveChat("你的武器已部分修理过", ChatType.Hint);
                            Enqueue(new S.ItemRepaired { UniqueID = temp.UniqueID, MaxDura = temp.MaxDura, CurrentDura = temp.CurrentDura });
                            break;
                        case 5: //WarGodOil
                            temp = Info.Equipment[(int)EquipmentSlot.Weapon];
                            if (temp == null || temp.MaxDura == temp.CurrentDura)
                            {
                                Enqueue(p);
                                return;
                            }
                            if (temp.Info.Bind.HasFlag(BindMode.DontRepair) || (temp.Info.Bind.HasFlag(BindMode.NoSRepair)))
                            {
                                Enqueue(p);
                                return;
                            }
                            temp.CurrentDura = temp.MaxDura;
                            temp.DuraChanged = false;

                            ReceiveChat("你的武器已经完全修好了", ChatType.Hint);
                            Enqueue(new S.ItemRepaired { UniqueID = temp.UniqueID, MaxDura = temp.MaxDura, CurrentDura = temp.CurrentDura });
                            break;
                        case 6: //ResurrectionScroll(复活卷轴)
                            if (Dead)
                            {
                                MP = MaxMP;
                                Revive(MaxHealth, true);
                            }
                            break;
                        case 7: //CreditScroll(增加元宝的卷轴)
                            if (item.Info.Price > 0)
                            {
                                GainCredit(item.Info.Price);
                                ReceiveChat(String.Format("{0} 元宝已添加到您的帐户", item.Info.Price), ChatType.Hint);
                            }
                            break;
                        case 8: //MapShoutScroll(地图喊话卷轴)
                            HasMapShout = true;
                            ReceiveChat("你已经在你的当前地图上得到一个免费的喊话，用!命令进行喊话", ChatType.Hint);
                            break;
                        case 9://ServerShoutScroll(全服喊话卷轴)
                            HasServerShout = true;
                            ReceiveChat("你在服务器上得到一个免费的喊话，用!命令进行喊话", ChatType.Hint);
                            break;
                        case 10://GuildSkillScroll(行会技能卷轴)
                            MyGuild.NewBuff(item.Info.Effect, false);
                            break;
                        case 11://HomeTeleport(领地回城卷，行会回城卷)
                            if (MyGuild != null && MyGuild.Conquest != null && !MyGuild.Conquest.WarIsOn && MyGuild.Conquest.PalaceMap != null && !TeleportRandom(200, 0, MyGuild.Conquest.PalaceMap))
                            {
                                Enqueue(p);
                                return;
                            }
                            break;
                        case 12://LotteryTicket(彩票) - 计算下逻辑     10000金币一张，     200分之一中得100万。                                                                         
                            if (RandomUtils.Next(item.Info.Effect * 32) == 1) // 1st prize : 1,000,000
                            {
                                ReceiveChat("你得了一等奖！获得1,000,000金币", ChatType.Hint);
                                GainGold(1000000);
                            }
                            else if (RandomUtils.Next(item.Info.Effect * 16) == 1)  // 2nd prize : 200,000
                            {
                                ReceiveChat("你得了二等奖! 获得 200,000 金币", ChatType.Hint);
                                GainGold(200000);
                            }
                            else if (RandomUtils.Next(item.Info.Effect * 8) == 1)  // 3rd prize : 100,000
                            {
                                ReceiveChat("你得了三等奖! 获得 100,000 金币", ChatType.Hint);
                                GainGold(100000);
                            }
                            else if (RandomUtils.Next(item.Info.Effect * 4) == 1) // 4th prize : 10,000
                            {
                                ReceiveChat("你得了四等奖! 获得 10,000 金币", ChatType.Hint);
                                GainGold(10000);
                            }
                            else if (RandomUtils.Next(item.Info.Effect * 2) == 1)  // 5th prize : 1,000
                            {
                                ReceiveChat("你得了五等奖! 获得 1,000 金币", ChatType.Hint);
                                GainGold(1000);
                            }
                            else if (RandomUtils.Next(item.Info.Effect) == 1)  // 6th prize 500
                            {
                                ReceiveChat("你得了六等奖! 获得 500 金币", ChatType.Hint);
                                GainGold(500);
                            }
                            else
                            {
                                ReceiveChat("你没有中奖.", ChatType.Hint);
                            }
                            break;
                        case 101: //装备全修复，修复神油
                            foreach(UserItem _temp  in Info.Equipment)
                            {
                                if (_temp == null || _temp.MaxDura == _temp.CurrentDura)
                                {
                                    continue;
                                }
                                if (_temp.Info.Bind.HasFlag(BindMode.DontRepair) || (_temp.Info.Bind.HasFlag(BindMode.NoSRepair)))
                                {
                                    continue;
                                }
                                _temp.CurrentDura = _temp.MaxDura;
                                _temp.DuraChanged = false;
                                Enqueue(new S.ItemRepaired { UniqueID = _temp.UniqueID, MaxDura = _temp.MaxDura, CurrentDura = _temp.CurrentDura });
                            }
                            ReceiveChat("你的装备已经完全修好了", ChatType.Hint);
                            break;
                    }
                    break;
                case ItemType.Book:
                    UserMagic magic = new UserMagic((Spell)item.Info.Shape);
                    if (magic.Info == null)
                    {
                        Enqueue(p);
                        return;
                    }
                    //满级不能吃书
                    magic = GetMagic((Spell)item.Info.Shape);
                    if (magic != null && magic.Level == 3)
                    {
                        Enqueue(p);
                        return;
                    }
                    if (magic == null)
                    {
                        magic = new UserMagic((Spell)item.Info.Shape);
                        Info.Magics.Add(magic);
                        Enqueue(magic.GetInfo());
                    }
                    else
                    {
                        magic.Level++;
                        magic.Experience = 0;
                        Enqueue(new S.MagicLeveled { Spell = magic.Spell, Level = magic.Level, Experience = magic.Experience });
                    }
                    RefreshStats();
                    break;
                case ItemType.Script:
                    //特殊卷轴
                    if (item.Info.Shape > 100)
                    {
                        //时空卷轴
                        if (item.Info.Shape == 101)
                        {
                            //未记录，这里针对不允许夫妻传送的地图，不允许记录
                            if (string.IsNullOrEmpty(item.spRecord))
                            {
                                if (CurrentMap.Info.NoRecall)
                                {
                                    ReceiveChat("当前地图无法使用此卷轴", ChatType.System);
                                    Enqueue(p);
                                    return;
                                }
                                item.spRecord = CurrentMapIndex + ","+CurrentLocation.X + ","+ CurrentLocation.Y;
                                item.spInfo = "时空卷轴已记录&^&"+"记录点:"+CurrentMap.getTitle() + "("+ CurrentLocation.X+","+ CurrentLocation.Y + ")";
                                Enqueue(p);
                                //这里返回最新的物品信息
                                //返回最新的用户背包
                                S.UserInventory packet = new S.UserInventory
                                {
                                    Inventory = new UserItem[Info.Inventory.Length],
                                };
                                Info.Inventory.CopyTo(packet.Inventory, 0);
                                Enqueue(packet);
                                return;
                            }
                            else//已记录
                            {
                                //限制下使用的期限。5分钟内只能使用1次。
                                int lasttime = Info.getTempValue("Script101");
                                if (lasttime > 0 && (Envir.Time / 1000 - lasttime) < 60 * 5)
                                {
                                    ReceiveChat($"不能频繁使用此卷轴在{60 * 5 -(Envir.Time / 1000 - lasttime)}秒内", ChatType.System);
                                    Enqueue(p);
                                    return;
                                }


                                string[] vars = item.spRecord.Split(',');
                                if (vars.Length != 3)
                                {
                                    Enqueue(p);
                                    return;
                                }
                                Map moveMap = Envir.GetMap(int.Parse(vars[0]));
                                if (moveMap == null|| moveMap.Info==null)
                                {
                                    Enqueue(p);
                                    return;
                                }
                                if (Level < moveMap.Info.minLevel)
                                {
                                    ReceiveChat($"您的等级低于{moveMap.Info.minLevel}级，无法传送到此地图", ChatType.System);
                                    Enqueue(p);
                                    return;
                                }
                                if (moveMap.Info.enterGold > 0){
                                    if (Account.Gold < moveMap.Info.enterGold)
                                    {
                                        ReceiveChat($"当前传送地图需要收取{moveMap.Info.enterGold}金币，您的金币不足", ChatType.System);
                                        Enqueue(p);
                                        return;
                                    }
                                    else
                                    {
                                        LoseGold((uint)moveMap.Info.enterGold);
                                        ReceiveChat($"当前传送地图需要收取{moveMap.Info.enterGold}金币，已从你账户中扣除", ChatType.System);
                                    }
                                }
                                


                                Point movePoint = new Point(int.Parse(vars[1]), int.Parse(vars[2]));
                                if (item.CurrentDura >= 1000)
                                {
                                    item.CurrentDura = (ushort)(item.CurrentDura - 1000);
                                    Teleport(moveMap, movePoint);
                                    Info.putTempValue("Script101", (int)Envir.Time/1000);
                                    if (item.CurrentDura >= 1000)
                                    {
                                        Enqueue(new S.ItemRepaired { UniqueID = item.UniqueID, MaxDura = item.MaxDura, CurrentDura = item.CurrentDura });
                                        Enqueue(p);
                                        return;
                                    }
                                }
                            }
                        }
                        //夫妻召唤卷
                        if (item.Info.Shape == 102)
                        {
                            if (Info.Married == 0)
                            {
                                ReceiveChat("你还没有结婚.", ChatType.System);
                                Enqueue(p);
                                return;
                            }

                            if (Dead)
                            {
                                ReceiveChat("你不能传送，在你死的时候.", ChatType.System);
                                Enqueue(p);
                                return;
                            }

                            if (CurrentMap.Info.NoRecall)
                            {
                                ReceiveChat("此地图不允许传送", ChatType.System);
                                Enqueue(p);
                                return;
                            }
                            CharacterInfo Lover = Envir.GetCharacterInfo(Info.Married);

                            if (Lover == null) return;

                            PlayerObject player = Envir.GetPlayer(Lover.Name);

                            if (player == null)
                            {
                                ReceiveChat((string.Format("{0} 不在线.", Lover.Name)), ChatType.System);
                                Enqueue(p);
                                return;
                            }

                            if (player.Dead)
                            {
                                ReceiveChat("你不能传送，对方已经死亡.", ChatType.System);
                                Enqueue(p);
                                return;
                            }

                            if (Math.Abs(player.Level - Level) >  5)
                            {
                                ReceiveChat("双方等级差距过大，无法传送", ChatType.System);
                                Enqueue(p);
                                return;
                            }
                            if (!player.AllowLoverRecall)
                            {
                                player.ReceiveChat("未经你允许，有人试图传送你",
                                        ChatType.System);
                                ReceiveChat((string.Format("{0} 拒绝传送.", player.Name)), ChatType.System);
                                Enqueue(p);
                                return;
                            }

                            if ((Envir.Time < LastRecallTime) && (Envir.Time < player.LastRecallTime))
                            {
                                ReceiveChat(string.Format("你不能传送在 {0} 秒内", (LastRecallTime - Envir.Time) / 1000), ChatType.System);
                                Enqueue(p);
                                return;
                            }

                            if (player.Level < CurrentMap.Info.minLevel)
                            {
                                ReceiveChat($"对方等级低于{CurrentMap.Info.minLevel}级，无法传送到此地图", ChatType.System);
                                Enqueue(p);
                                return;
                            }

                            if (CurrentMap.Info.enterGold > 0)
                            {
                                if (Account.Gold < CurrentMap.Info.enterGold)
                                {
                                    ReceiveChat($"当前传送地图需要收取{CurrentMap.Info.enterGold}金币，您的金币不足", ChatType.System);
                                    Enqueue(p);
                                    return;
                                }
                                else
                                {
                                    LoseGold((uint)CurrentMap.Info.enterGold);
                                    ReceiveChat($"当前传送地图需要收取{CurrentMap.Info.enterGold}金币，已从你账户中扣除", ChatType.System);
                                }
                            }

                            LastRecallTime = Envir.Time + 60000;
                            player.LastRecallTime = Envir.Time + 60000;

                            if (item.CurrentDura >= 1000)
                            {
                                item.CurrentDura = (ushort)(item.CurrentDura - 1000);
                                if (!player.Teleport(CurrentMap, Front))
                                {
                                    player.Teleport(CurrentMap, CurrentLocation);
                                }
                                if (item.CurrentDura >= 1000)
                                {
                                    Enqueue(new S.ItemRepaired { UniqueID = item.UniqueID, MaxDura = item.MaxDura, CurrentDura = item.CurrentDura });
                                    Enqueue(p);
                                    return;
                                }
                            }
                        }

                        //徒弟召唤卷
                        if (item.Info.Shape == 103)
                        {
                            if (Info.Mentor == 0)
                            {
                                ReceiveChat("你还没有徒弟.", ChatType.System);
                                Enqueue(p);
                                return;
                            }

                            if (Dead)
                            {
                                ReceiveChat("你不能传送，在你死的时候.", ChatType.System);
                                Enqueue(p);
                                return;
                            }

                            if (CurrentMap.Info.NoRecall)
                            {
                                ReceiveChat("此地图不允许传送", ChatType.System);
                                Enqueue(p);
                                return;
                            }
                            CharacterInfo Mentor = Envir.GetCharacterInfo(Info.Mentor);

                            if (Mentor == null) return;

                            PlayerObject player = Envir.GetPlayer(Mentor.Name);

                            if (player == null)
                            {
                                ReceiveChat((string.Format("{0} 不在线.", Mentor.Name)), ChatType.System);
                                Enqueue(p);
                                return;
                            }

                            if (player.Dead)
                            {
                                ReceiveChat("你不能传送，对方已经死亡.", ChatType.System);
                                Enqueue(p);
                                return;
                            }

                           
                           

                            if ((Envir.Time < LastRecallTime) && (Envir.Time < player.LastRecallTime))
                            {
                                ReceiveChat(string.Format("你不能传送在 {0} 秒内", (LastRecallTime - Envir.Time) / 1000), ChatType.System);
                                Enqueue(p);
                                return;
                            }

                            if (player.Level < CurrentMap.Info.minLevel)
                            {
                                ReceiveChat($"对方等级低于{CurrentMap.Info.minLevel}级，无法传送到此地图", ChatType.System);
                                Enqueue(p);
                                return;
                            }

                            if (CurrentMap.Info.enterGold > 0)
                            {
                                if (Account.Gold < CurrentMap.Info.enterGold)
                                {
                                    ReceiveChat($"当前传送地图需要收取{CurrentMap.Info.enterGold}金币，您的金币不足", ChatType.System);
                                    Enqueue(p);
                                    return;
                                }
                                else
                                {
                                    LoseGold((uint)CurrentMap.Info.enterGold);
                                    ReceiveChat($"当前传送地图需要收取{CurrentMap.Info.enterGold}金币，已从你账户中扣除", ChatType.System);
                                }
                            }

                            LastRecallTime = Envir.Time + 60000;
                            player.LastRecallTime = Envir.Time + 60000;

                            if (item.CurrentDura >= 1000)
                            {
                                item.CurrentDura = (ushort)(item.CurrentDura - 1000);
                                if (!player.Teleport(CurrentMap, Front))
                                {
                                    player.Teleport(CurrentMap, CurrentLocation);
                                }
                                if (item.CurrentDura >= 1000)
                                {
                                    Enqueue(new S.ItemRepaired { UniqueID = item.UniqueID, MaxDura = item.MaxDura, CurrentDura = item.CurrentDura });
                                    Enqueue(p);
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        CallDefaultNPC(DefaultNPCType.UseItem, item.Info.Shape);
                    }
                    
                    break;
                case ItemType.Food://食物，给坐骑吃
                    temp = Info.Equipment[(int)EquipmentSlot.Mount];
                    if (temp == null || temp.MaxDura == temp.CurrentDura)
                    {
                        Enqueue(p);
                        return;
                    }

                    switch (item.Info.Shape)
                    {
                        case 0:
                            temp.MaxDura = (ushort)Math.Max(0, temp.MaxDura - Math.Min(1000, temp.MaxDura - (temp.CurrentDura / 30)));
                            break;
                        case 1:
                            break;
                    }

                    temp.CurrentDura = (ushort)Math.Min(temp.MaxDura, temp.CurrentDura + item.CurrentDura);
                    temp.DuraChanged = false;

                    ReceiveChat("你的坐骑已经恢复了一些忠诚.", ChatType.Hint);
                    Enqueue(new S.ItemRepaired { UniqueID = temp.UniqueID, MaxDura = temp.MaxDura, CurrentDura = temp.CurrentDura });

                    RefreshStats();
                    break;
                case ItemType.Pets:
                    if (item.Info.Shape >= 20)
                    {
                        switch (item.Info.Shape)
                        {
                            case 20://Mirror
                                Enqueue(new S.IntelligentCreatureEnableRename());
                                break;
                            case 21://BlackStone黑色灵物石
                                if (item.Count > 1) item.Count--;
                                else Info.Inventory[index] = null;
                                RefreshBagWeight();
                                p.Success = true;
                                Enqueue(p);
                                BlackstoneRewardItem();
                                return;
                            case 22://Nuts
                                if (CreatureSummoned)
                                    for (int i = 0; i < Pets.Count; i++)
                                    {
                                        if (Pets[i].Info.AI != 64) continue;
                                        if (((IntelligentCreatureObject)Pets[i]).petType != SummonedCreatureType) continue;
                                        ((IntelligentCreatureObject)Pets[i]).maintainfoodTime = item.Info.Effect * Settings.Hour / 1000;
                                        UpdateCreatureMaintainFoodTime(SummonedCreatureType, 0);
                                        break;
                                    }
                                break;
                            case 23://FairyMoss, FreshwaterClam, Mackerel, Cherry
                                if (CreatureSummoned)
                                    for (int i = 0; i < Pets.Count; i++)
                                    {
                                        if (Pets[i].Info.AI != 64) continue;
                                        if (((IntelligentCreatureObject)Pets[i]).petType != SummonedCreatureType) continue;
                                        if (((IntelligentCreatureObject)Pets[i]).Fullness < 10000)
                                            ((IntelligentCreatureObject)Pets[i]).IncreaseFullness(item.Info.Effect * 100);
                                        break;
                                    }
                                break;
                            case 24://WonderPill
                                if (CreatureSummoned)
                                    for (int i = 0; i < Pets.Count; i++)
                                    {
                                        if (Pets[i].Info.AI != 64) continue;
                                        if (((IntelligentCreatureObject)Pets[i]).petType != SummonedCreatureType) continue;
                                        if (((IntelligentCreatureObject)Pets[i]).Fullness == 0)
                                            ((IntelligentCreatureObject)Pets[i]).IncreaseFullness(100);
                                        break;
                                    }
                                break;
                            case 25://Strongbox
                                byte boxtype = item.Info.Effect;
                                if (item.Count > 1) item.Count--;
                                else Info.Inventory[index] = null;
                                RefreshBagWeight();
                                p.Success = true;
                                Enqueue(p);
                                StrongboxRewardItem(boxtype);
                                return;
                            case 26://Wonderdrugs
                                int time = item.Info.Durability;
                                switch (item.Info.Effect)
                                {
                                    case 0://exp low/med/high
                                        AddBuff(new Buff { Type = BuffType.WonderDrug, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.Effect, item.Info.Luck + item.Luck } });
                                        break;
                                    case 1://drop low/med/high
                                        AddBuff(new Buff { Type = BuffType.WonderDrug, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.Effect, item.Info.Luck + item.Luck } });
                                        break;
                                    case 2://hp low/med/high
                                        AddBuff(new Buff { Type = BuffType.WonderDrug, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.Effect, item.Info.HP + item.HP } });
                                        break;
                                    case 3://mp low/med/high
                                        AddBuff(new Buff { Type = BuffType.WonderDrug, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.Effect, item.Info.MP + item.MP } });
                                        break;
                                    case 4://ac-ac low/med/high
                                        AddBuff(new Buff { Type = BuffType.WonderDrug, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.Effect, item.Info.MaxAC + item.AC } });
                                        break;
                                    case 5://mac-mac low/med/high
                                        AddBuff(new Buff { Type = BuffType.WonderDrug, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.Effect, item.Info.MaxMAC + item.MAC } });
                                        break;
                                    case 6://speed low/med/high
                                        AddBuff(new Buff { Type = BuffType.WonderDrug, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.Effect, item.Info.AttackSpeed + item.AttackSpeed } });
                                        break;
                                }
                                break;
                            case 27://FortuneCookies
                                break;
                            case 28://Knapsack
                                time = item.Info.Durability;
                                AddBuff(new Buff { Type = BuffType.Knapsack, Caster = this, ExpireTime = Envir.Time + time * Settings.Minute, Values = new int[] { item.Info.Luck + item.Luck } });
                                break;
                        }
                    }
                    else
                    {
                        int slotIndex = Info.IntelligentCreatures.Count;
                        UserIntelligentCreature petInfo = new UserIntelligentCreature((IntelligentCreatureType)item.Info.Shape, slotIndex, item.Info.Effect);
                        if (Info.CheckHasIntelligentCreature((IntelligentCreatureType)item.Info.Shape))
                        {
                            ReceiveChat("你已经拥有了这个宠物.", ChatType.Hint);
                            petInfo = null;
                        }

                        if (petInfo == null || slotIndex >= 10)
                        {
                            Enqueue(p);
                            return;
                        }

                        ReceiveChat("获得一种新宠物{" + petInfo.CustomName + "}.", ChatType.Hint);

                        Info.IntelligentCreatures.Add(petInfo);
                        Enqueue(petInfo.GetInfo());
                    }
                    break;
                case ItemType.Transform: //Transforms,时装,这里改下，需要增加相关的时装属性啊

                    int tTime = item.Info.Durability;
                    int tType = item.Info.Shape;

                    AddBuff(new Buff { Type = BuffType.Transform, Caster = this, ExpireTime = Envir.Time + tTime * 1000, Values = new int[] { tType, item.Info.MinAC, item.Info.MaxAC, item.Info.MinMAC, item.Info.MaxMAC, item.Info.MinDC, item.Info.MaxDC, item.Info.MinMC, item.Info.MaxMC, item.Info.MinSC, item.Info.MaxSC, item.Info.Luck } });
                    break;

                case ItemType.SkinWeapon: //武器变幻
                     //查找武器
                    if (Info.Equipment[(int)EquipmentSlot.Weapon] == null)
                    {
                        ReceiveChat($"没有武器，无法对武器进行形态变幻", ChatType.System);
                        return;
                    }
                    if (item.Info.Image > 0)
                    {
                        Info.Equipment[(int)EquipmentSlot.Weapon].n_Image = item.Info.Image;
                    }
                    if (item.Info.Shape > -1)
                    {
                        Info.Equipment[(int)EquipmentSlot.Weapon].n_Shape = item.Info.Shape;
                    }
                    if (item.Info.Effect > 0)
                    {
                        Info.Equipment[(int)EquipmentSlot.Weapon].n_Effect = item.Info.Effect;
                    }
                    Enqueue(new S.RefreshItem { Item = Info.Equipment[(int)EquipmentSlot.Weapon] });
                    //AddBuff(new Buff { Type = BuffType.Transform, Caster = this, ExpireTime = Envir.Time + tTime * 1000, Values = new int[] { tType, item.Info.MinAC, item.Info.MaxAC, item.Info.MinMAC, item.Info.MaxMAC, item.Info.MinDC, item.Info.MaxDC, item.Info.MinMC, item.Info.MaxMC, item.Info.MinSC, item.Info.MaxSC, item.Info.Luck } });
                    break;

                case ItemType.SkinArmour: //衣服变幻
                     //查找衣服
                    if (Info.Equipment[(int)EquipmentSlot.Armour] == null)
                    {
                        ReceiveChat($"没有衣服，无法对衣服进行形态变幻", ChatType.System);
                        return;
                    }
                    if (item.Info.Image > 0)
                    {
                        Info.Equipment[(int)EquipmentSlot.Armour].n_Image = item.Info.Image;
                    }
                    if (item.Info.Shape > 0)
                    {
                        Info.Equipment[(int)EquipmentSlot.Armour].n_Shape = item.Info.Shape;
                    }
                    if (item.Info.Effect > 0)
                    {
                        Info.Equipment[(int)EquipmentSlot.Armour].n_Effect = item.Info.Effect;
                    }
                    Enqueue(new S.RefreshItem { Item = Info.Equipment[(int)EquipmentSlot.Armour] });
                    //AddBuff(new Buff { Type = BuffType.Transform, Caster = this, ExpireTime = Envir.Time + tTime * 1000, Values = new int[] { tType, item.Info.MinAC, item.Info.MaxAC, item.Info.MinMAC, item.Info.MaxMAC, item.Info.MinDC, item.Info.MaxDC, item.Info.MinMC, item.Info.MaxMC, item.Info.MinSC, item.Info.MaxSC, item.Info.Luck } });
                    break;
                default:
                    return;
            }

            if (item.Count > 1) item.Count--;
            else Info.Inventory[index] = null;
            RefreshBagWeight();

            Report.ItemChanged("UseItem", item, 1, 1);

            p.Success = true;
            Enqueue(p);
        }
        //拆分物品
        public void SplitItem(MirGridType grid, ulong id, uint count)
        {
            S.SplitItem1 p = new S.SplitItem1 { Grid = grid, UniqueID = id, Count = count, Success = false };
            UserItem[] array;
            switch (grid)
            {
                case MirGridType.Inventory:
                    array = Info.Inventory;
                    break;
                case MirGridType.Storage:
                    if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.StorageKey, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Enqueue(p);
                        return;
                    }
                    NPCObject ob = null;
                    for (int i = 0; i < CurrentMap.NPCs.Count; i++)
                    {
                        if (CurrentMap.NPCs[i].ObjectID != NPCID) continue;
                        ob = CurrentMap.NPCs[i];
                        break;
                    }

                    if (ob == null || !Functions.InRange(ob.CurrentLocation, CurrentLocation, Globals.DataRange))
                    {
                        Enqueue(p);
                        return;
                    }
                    array = Account.Storage;
                    break;
                default:
                    Enqueue(p);
                    return;
            }

            UserItem temp = null;


            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null || array[i].UniqueID != id) continue;
                temp = array[i];
                break;
            }

            if (temp == null || count >= temp.Count || FreeSpace(array) == 0)
            {
                Enqueue(p);
                return;
            }

            temp.Count -= count;

            temp = temp.Info.CreateFreshItem();
            temp.Count = count;

            p.Success = true;
            Enqueue(p);
            Enqueue(new S.SplitItem { Item = temp, Grid = grid });

            if (grid == MirGridType.Inventory && (temp.Info.Type == ItemType.Potion || temp.Info.Type == ItemType.Scroll || temp.Info.Type == ItemType.Amulet || (temp.Info.Type == ItemType.Script && temp.Info.Effect == 1)))
            {
                if (temp.Info.Type == ItemType.Potion || temp.Info.Type == ItemType.Scroll || (temp.Info.Type == ItemType.Script && temp.Info.Effect == 1))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (array[i] != null) continue;
                        array[i] = temp;
                        RefreshBagWeight();
                        return;
                    }
                }
                else if (temp.Info.Type == ItemType.Amulet)
                {
                    for (int i = 4; i < 6; i++)
                    {
                        if (array[i] != null) continue;
                        array[i] = temp;
                        RefreshBagWeight();
                        return;
                    }
                }
            }

            for (int i = 6; i < array.Length; i++)
            {
                if (array[i] != null) continue;
                array[i] = temp;
                RefreshBagWeight();
                return;
            }

            for (int i = 0; i < 6; i++)
            {
                if (array[i] != null) continue;
                array[i] = temp;
                RefreshBagWeight();
                return;
            }
        }
        //合并物品
        public void MergeItem(MirGridType gridFrom, MirGridType gridTo, ulong fromID, ulong toID)
        {
            S.MergeItem p = new S.MergeItem { GridFrom = gridFrom, GridTo = gridTo, IDFrom = fromID, IDTo = toID, Success = false };

            UserItem[] arrayFrom;

            switch (gridFrom)
            {
                case MirGridType.Inventory:
                    arrayFrom = Info.Inventory;
                    break;
                case MirGridType.Storage:
                    if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.StorageKey, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Enqueue(p);
                        return;
                    }
                    NPCObject ob = null;
                    for (int i = 0; i < CurrentMap.NPCs.Count; i++)
                    {
                        if (CurrentMap.NPCs[i].ObjectID != NPCID) continue;
                        ob = CurrentMap.NPCs[i];
                        break;
                    }

                    if (ob == null || !Functions.InRange(ob.CurrentLocation, CurrentLocation, Globals.DataRange))
                    {
                        Enqueue(p);
                        return;
                    }
                    arrayFrom = Account.Storage;
                    break;
                case MirGridType.Equipment:
                    arrayFrom = Info.Equipment;
                    break;
                case MirGridType.Fishing:
                    if (Info.Equipment[(int)EquipmentSlot.Weapon] == null || (Info.Equipment[(int)EquipmentSlot.Weapon].Info.Shape != 49 && Info.Equipment[(int)EquipmentSlot.Weapon].Info.Shape != 50))
                    {
                        Enqueue(p);
                        return;
                    }
                    arrayFrom = Info.Equipment[(int)EquipmentSlot.Weapon].Slots;
                    break;
                default:
                    Enqueue(p);
                    return;
            }

            UserItem[] arrayTo;
            switch (gridTo)
            {
                case MirGridType.Inventory:
                    arrayTo = Info.Inventory;
                    break;
                case MirGridType.Storage:
                    if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.StorageKey, StringComparison.CurrentCultureIgnoreCase))
                    {
                        Enqueue(p);
                        return;
                    }
                    NPCObject ob = null;
                    for (int i = 0; i < CurrentMap.NPCs.Count; i++)
                    {
                        if (CurrentMap.NPCs[i].ObjectID != NPCID) continue;
                        ob = CurrentMap.NPCs[i];
                        break;
                    }

                    if (ob == null || !Functions.InRange(ob.CurrentLocation, CurrentLocation, Globals.DataRange))
                    {
                        Enqueue(p);
                        return;
                    }
                    arrayTo = Account.Storage;
                    break;
                case MirGridType.Equipment:
                    arrayTo = Info.Equipment;
                    break;
                case MirGridType.Fishing:
                    if (Info.Equipment[(int)EquipmentSlot.Weapon] == null || (Info.Equipment[(int)EquipmentSlot.Weapon].Info.Shape != 49 && Info.Equipment[(int)EquipmentSlot.Weapon].Info.Shape != 50))
                    {
                        Enqueue(p);
                        return;
                    }
                    arrayTo = Info.Equipment[(int)EquipmentSlot.Weapon].Slots;
                    break;
                default:
                    Enqueue(p);
                    return;
            }

            UserItem tempFrom = null;
            int index = -1;

            for (int i = 0; i < arrayFrom.Length; i++)
            {
                if (arrayFrom[i] == null || arrayFrom[i].UniqueID != fromID) continue;
                index = i;
                tempFrom = arrayFrom[i];
                break;
            }

            if (tempFrom == null || tempFrom.Info.StackSize == 1 || index == -1)
            {
                Enqueue(p);
                return;
            }


            UserItem tempTo = null;

            for (int i = 0; i < arrayTo.Length; i++)
            {
                if (arrayTo[i] == null || arrayTo[i].UniqueID != toID) continue;
                tempTo = arrayTo[i];
                break;
            }

            if (tempTo == null || tempTo.Info != tempFrom.Info || tempTo.Count == tempTo.Info.StackSize)
            {
                Enqueue(p);
                return;
            }

            if (tempTo.Info.Type != ItemType.Amulet && (gridFrom == MirGridType.Equipment || gridTo == MirGridType.Equipment))
            {
                Enqueue(p);
                return;
            }

            if(tempTo.Info.Type != ItemType.Bait && (gridFrom == MirGridType.Fishing || gridTo == MirGridType.Fishing))
            {
                Enqueue(p);
                return;
            }

            if (tempFrom.Count <= tempTo.Info.StackSize - tempTo.Count)
            {
                tempTo.Count += tempFrom.Count;
                arrayFrom[index] = null;
            }
            else
            {
                tempFrom.Count -= tempTo.Info.StackSize - tempTo.Count;
                tempTo.Count = tempTo.Info.StackSize;
            }

            TradeUnlock();

            p.Success = true;
            Enqueue(p);
            RefreshStats();
        }

        //玩家对契约兽的操作
        //操作 1:改名 2：召唤 3：释放，解雇，4：转移 5：喂养
        public void MyMonsterOperation(ulong monidx, byte operation, string parameter1, string parameter2)
        {
            if (Dead)
            {
                return;
            }
            if (Info.MyMonsters == null)
            {
                ReceiveChat("找不到该契约兽.", ChatType.Hint);
                return;
            }
            MyMonster mon = null;
            for (int i = 0; i < Info.MyMonsters.Length; i++)
            {
                if (Info.MyMonsters[i] == null || Info.MyMonsters[i].idx!= monidx)
                {
                    continue;
                }
                mon = Info.MyMonsters[i];
            }
            if (mon == null)
            {
                ReceiveChat("找不到该契约兽.", ChatType.Hint);
                return;
            }

            //改名
            if (operation == 1)
            {
                if(parameter1==null|| parameter1.Length<2|| parameter1.Length > 6)
                {
                    ReceiveChat("契约兽名称不符合规则（2-6个字的名称）.", ChatType.Hint);
                    GetMyMonsters();
                    return;
                }
                //判断是否有契约兽改名卡
                bool haska = false;
                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    if (Info.Inventory[i]!=null && Info.Inventory[i].Name == "契约兽改名卡")
                    {
                        Enqueue(new S.DeleteItem { UniqueID = Info.Inventory[i].UniqueID, Count = 1 });
                        Info.Inventory[i] = null;
                        haska = true;
                        break;
                    }
                }
                if (!haska)
                {
                    ReceiveChat("没有【契约兽改名卡】无法改名.", ChatType.Hint);
                    GetMyMonsters();
                    return;
                }
                //item merged ok
                RefreshBagWeight();
                mon.rMonName = parameter1;

                ReceiveChat("契约兽名字已变更，下次召唤生效.", ChatType.Hint);
                GetMyMonsters();
            }

            //召唤，这里改下
            if (operation == 2)
            {
                //同一个契约兽，只能召唤一个
                foreach (MonsterObject pet in Pets)
                {
                    if (pet == null || pet.Dead || pet.myMonster == null)
                    {
                        continue;
                    }
                    if (pet.myMonster.idx == mon.idx)
                    {
                        pet.PetRecall();
                        return;
                    }
                }

                int mcount = 1;
                if (Level >= 50)
                {
                    mcount = 2;
                }
                if (Level >= 60)
                {
                    mcount = 3;
                }
                if (Level >= 63)
                {
                    mcount = 4;
                }
                if (Level >= 65)
                {
                    mcount = 5;
                }

                if (getPetCount(PetType.MyMonster) >= mcount)
                {
                    ReceiveChat($"当前等级最多召唤{mcount}个契约兽.", ChatType.Hint);
                    return;
                }

                MonsterInfo _info = Envir.GetMonsterInfo(mon.MonIndex);
                if (_info == null)
                {
                    ReceiveChat("找不到当前契约兽.", ChatType.Hint);
                    return;
                }

                if (!mon.ReCall())
                {
                    ReceiveChat($"[{mon.getName()}]  体力不足，无法出战...", ChatType.System);
                    return;
                }

                MonsterInfo minfo = _info.Clone();
                MonsterObject monster = MonsterObject.GetMonster(minfo);
                monster.PetLevel = (byte)(mon.MonLevel/10+1);
                if (monster.PetLevel > 7)
                {
                    monster.PetLevel = 7;
                }
                monster.MaxPetLevel = (byte)(mon.MonLevel / 10 + 1);
                if (monster.MaxPetLevel > 7)
                {
                    monster.MaxPetLevel = 7;
                }
                monster.Master = this;
                monster.PType = PetType.MyMonster;
                monster.myMonster = mon;
               
                monster.ActionTime = Envir.Time + 1000;
                MyMonsterUtils.RefreshMyMonLevelStats(mon, monster);
                monster.RefreshNameColour(false);

                if (CurrentMap.ValidPoint(Front))
                {
                    monster.Spawn(CurrentMap, Front);
                }
                else
                {
                    monster.Spawn(CurrentMap, CurrentLocation);
                }
                Pets.Add(monster);
                GetMyMonsters();
            }

            //解雇
            if (operation == 3)
            {
                //如果已存在契约兽，则契约兽死亡
                foreach (MonsterObject pet in Pets)
                {
                    if (pet == null || pet.Dead || pet.myMonster == null)
                    {
                        continue;
                    }
                    if (pet.myMonster.idx == mon.idx)
                    {
                        pet.Die();
                    }
                }

                for (int i = 0; i < Info.MyMonsters.Length; i++)
                {
                    if (Info.MyMonsters[i] == null || Info.MyMonsters[i].idx != monidx)
                    {
                        continue;
                    }
                    Info.MyMonsters[i]=null;
                }
                GetMyMonsters();
            }


            //转赠送
            if (operation == 4)
            {
                //1.判断玩家是否在线
                PlayerObject p = Envir.GetPlayer(parameter1);
                if (p == null || p.Dead)
                {
                    ReceiveChat($"玩家 {parameter1} 不在线.", ChatType.Hint);
                    return;
                }

                int kaidx = -1;
                //2.判断是否有契约兽改名卡
                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    if (Info.Inventory[i] != null && Info.Inventory[i].Name == "契约兽转赠卡")
                    {
                        kaidx = i;
                        break;
                    }
                }
                if (kaidx==-1)
                {
                    ReceiveChat("没有【契约兽转赠卡】无法转赠.", ChatType.Hint);
                    return;
                }
                //3.判断玩家是否有位置
                //判断契约兽是否签约满了
                int monIdx = -1;
                for (int i = 0; i < p.Info.MyMonsters.Length; i++)
                {
                    if (p.Info.MyMonsters[i] == null)
                    {
                        monIdx = i;
                        break;
                    }
                }
                if (monIdx == -1)
                {
                    ReceiveChat($"玩家 {parameter1 }的契约兽已满，无法转赠.", ChatType.Hint);
                    return;
                }

                //如果契约兽已经召唤，则契约兽死亡
                foreach (MonsterObject pet in Pets)
                {
                    if (pet == null || pet.Dead || pet.myMonster == null)
                    {
                        continue;
                    }
                    if (pet.myMonster.idx == mon.idx)
                    {
                        pet.Die();
                    }
                }

                //删除卡
                Enqueue(new S.DeleteItem { UniqueID = Info.Inventory[kaidx].UniqueID, Count = 1 });
                Info.Inventory[kaidx] = null;
                //删除契约兽
                for (int i = 0; i < Info.MyMonsters.Length; i++)
                {
                    if (Info.MyMonsters[i] == null || Info.MyMonsters[i].idx != monidx)
                    {
                        continue;
                    }
                    p.Info.MyMonsters[monIdx] = Info.MyMonsters[i].Clone();
                    Info.MyMonsters[i] = null;
                }
                GetMyMonsters();
                p.GetMyMonsters();
                
                ReceiveChat($"契约兽 {mon.MonName} 已赠送给玩家 {parameter1} .", ChatType.Hint);
                p.ReceiveChat($"收到来自玩家 {Name} 赠送的契约兽  {mon.MonName} .", ChatType.Hint);

            }

            //喂食
            if (operation == 5)
            {
                ulong itemid = ulong.Parse(parameter1);
                //
                UserItem temp = null;
                int index = -1;
                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    temp = Info.Inventory[i];
                    if (temp == null || temp.UniqueID != itemid) continue;
                    index = i;
                    break;
                }
                if (index == -1)
                {
                    ReceiveChat("当前物品不存在，请刷新背包.", ChatType.System2);
                    //Enqueue(p);
                    return;
                }
         
                //灵兽仙桃
                if (temp.Info.Name == "灵兽仙桃")
                {
                    if (mon.callTime > mon.MonLevel * 5)
                    {
                        ReceiveChat($"[{mon.getName()}]  主人，我已吃饱了，吃不下了.当前等级体力上限{mon.MonLevel * 3}", ChatType.System);
                        return;
                    }
                    mon.callTime += (byte)temp.Count;

                    S.DeleteItem p = new S.DeleteItem { UniqueID = itemid, Count = temp.Count };
                    Info.Inventory[index] = null;
                    RefreshBagWeight();
                    Enqueue(p);
                    GetMyMonsters();
                    return;
                }

                //重置成长
                //吞噬本体兽丹进行重置属性
                if (temp.Info.Name == "兽魂丹" && mon.MonIndex == temp.src_mon_idx)
                {
                    MonsterObject mob = MonsterObject.GetMonster(Envir.GetMonsterInfo(temp.src_mon_idx));
                    if (mob == null || mob.Info == null)
                    {
                        ReceiveChat("当前兽丹无效.", ChatType.System2);
                        return;
                    }

                    mon.RestartUp(mob.Info.UpChance);

                    ReceiveChat($"[{mon.getName()}] 已获得成长属性重置", ChatType.System2);
                    MyMonsterUtils.RefreshMyMonLevelStats(mon, null);

                    S.DeleteItem p = new S.DeleteItem { UniqueID = itemid, Count = temp.Count };
                    Info.Inventory[index] = null;
                    RefreshBagWeight();
                    Enqueue(p);
                    GetMyMonsters();
                    return;
                }

                //重置成长
                if (temp.Info.Name == "灵兽洗髓丹")
                {
                    MonsterInfo mobinfo = Envir.GetMonsterInfo(mon.MonIndex);
                    if (mobinfo == null )
                    {
                        ReceiveChat("当前契约兽无效.", ChatType.System2);
                        return;
                    }

                    mon.RestartUp(mobinfo.UpChance);

                    ReceiveChat($"[{mon.getName()}] 已获得成长属性重置", ChatType.System2);
                    MyMonsterUtils.RefreshMyMonLevelStats(mon, null);

                    S.DeleteItem p = new S.DeleteItem { UniqueID = itemid, Count = temp.Count };
                    Info.Inventory[index] = null;
                    RefreshBagWeight();
                    Enqueue(p);
                    GetMyMonsters();
                    return;
                }


                //进化
                if (temp.Info.Name == "兽魂丹")
                {
                    if (mon.callTime < 30)
                    {
                        ReceiveChat($"[{mon.getName()}]  体力不足，无法吞食此兽丹进行进化,进化需要30体力", ChatType.System);
                        return;
                    }
                    //判断兽丹是否有效
                    if (temp.src_mon_idx == 0)
                    {
                        ReceiveChat("当前兽丹无效.", ChatType.System2);
                        return;
                    }

                    if(mon.UpMonIndex == temp.src_mon_idx)
                    {
                        ReceiveChat("当前兽丹已进化过，无需重复进化.", ChatType.System2);
                        return;
                    }

   
                    MonsterObject mob = MonsterObject.GetMonster(Envir.GetMonsterInfo(temp.src_mon_idx));
                    if (mob == null|| mob.Info==null)
                    {
                        ReceiveChat("当前兽丹无效.", ChatType.System2);
                        return;
                    }


                    if (!mob.Info.CanTreaty)
                    {
                        ReceiveChat("当前兽丹无效.", ChatType.System2);
                        return;
                    }



                    mon.callTime = mon.callTime - 30;
                    mon.UpMonIndex = temp.src_mon_idx;
                    mon.UpMonName = mob.Name;

                    MyMonsterUtils.RefreshMyMonLevelStats(mon, null);
                    ReceiveChat($"当前契约兽已进化为 {mob.Name}", ChatType.Hint);

                    S.DeleteItem p = new S.DeleteItem { UniqueID = itemid, Count = temp.Count };
                    Info.Inventory[index] = null;
                    RefreshBagWeight();
                    Enqueue(p);
                    GetMyMonsters();
                    return;
                }

                //领悟技能1
                if (temp.Info.Name.StartsWith("书页残卷"))
                {
                    byte monlevel = 0;
                    string levelname = "初级技能";
                    int callTime = 0;
                    if (temp.Count == 2)
                    {
                        monlevel = 40;
                        callTime = 1;
                        levelname = "初级技能";
                    }
                    if (temp.Count == 5)
                    {
                        monlevel = 45;
                        callTime = 2;
                        levelname = "中级技能";
                    }
                    if (temp.Count == 10)
                    {
                        monlevel = 50;
                        callTime = 3;
                        levelname = "高级技能";
                    }
                    if (monlevel == 0)
                    {
                        ReceiveChat($"[{mon.getName()}] 我只吃2/5/10几种数量的书页，分别领悟初/中/高级技能.", ChatType.System);
                        return;
                    }

                    if (mon.MonLevel < monlevel)
                    {
                        ReceiveChat($"[{mon.getName()}] 我需要{monlevel}级才能领悟{levelname}哦.", ChatType.System);
                        return;
                    }

                    if (mon.callTime < callTime)
                    {
                        ReceiveChat($"[{mon.getName()}] 体力不足，读不下书哦.给点仙桃我吃吧...", ChatType.System);
                        return;
                    }
                    mon.callTime -= callTime;

                    MyMonSkillBean sk = MyMonSkillBean.RefreshSkill(monlevel, mon.skill1, mon.skill2, mon.skCount);
                    if (sk == null)
                    {
                        ReceiveChat($"[{mon.getName()}] 技能领悟失败.", ChatType.System);
                        return;
                    }
                    mon.skill1 = sk.skid;
                    mon.skillname1 = sk.skname;
                    mon.skCount++;
                    ReceiveChat($"[{mon.getName()}] 我已成功领悟技能[{sk.skname}].", ChatType.System);
                    MyMonsterUtils.RefreshMyMonLevelStats(mon, null);
                    //如果契约兽已经召唤，则更新契约兽
                    if (Pets != null)
                    {
                        //查找玩家的宠物，看有没此契约兽
                        for (int i = 0; i < Pets.Count; i++)
                        {
                            MonsterObject pet = Pets[i];
                            if (pet == null || pet.Dead || pet.myMonster == null || pet.myMonster.idx != mon.idx)
                            {
                                continue;
                            }
                            MyMonsterUtils.RefreshMyMonLevelStats(mon, pet);
                        }
                    }
                   
                    //删除背包物品
                    S.DeleteItem p = new S.DeleteItem { UniqueID = itemid, Count = temp.Count };
                    Info.Inventory[index] = null;
                    RefreshBagWeight();
                    Enqueue(p);
                    GetMyMonsters();
                    return;
                }

                //领悟技能2
                if (temp.Info.Name == "灵兽开智丹")
                {
                    byte monlevel = 55;

                    if (mon.MonLevel < monlevel)
                    {
                        ReceiveChat($"[{mon.getName()}] 我需要{monlevel}级才能开启第二智力哦.", ChatType.System);
                        return;
                    }
                    if (mon.skill1 <= 0)
                    {
                        ReceiveChat($"[{mon.getName()}] 我第一技能还没领悟呢，先给我吃点书页积累些智力吧.", ChatType.System);
                        return;
                    }
                    //首次开启第2技能，有成功率
                    if (mon.skill2 <= 0)
                    {
                        if (RandomUtils.Next(100) > temp.Info.Strong)
                        {
                            //删除背包物品
                            S.DeleteItem pt = new S.DeleteItem { UniqueID = itemid, Count = temp.Count };
                            Info.Inventory[index] = null;
                            RefreshBagWeight();
                            Enqueue(pt);
                            ReceiveChat($"[{mon.getName()}] 主人，我太笨了，无法领悟第二技能呢，再给我吃一些开智丹吧.", ChatType.System);
                            return;
                        }
                    }

                   


                    MyMonSkillBean sk = MyMonSkillBean.RefreshSkill(monlevel, mon.skill1, mon.skill2, mon.skCount);
                    if (sk == null)
                    {
                        ReceiveChat($"[{mon.getName()}] 技能领悟失败.", ChatType.System);
                        return;
                    }
                    mon.skill2 = sk.skid;
                    mon.skillname2 = sk.skname;
                    mon.skCount++;
                    ReceiveChat($"[{mon.getName()}] 我已成功领悟技能[{sk.skname}].", ChatType.System);
                    MyMonsterUtils.RefreshMyMonLevelStats(mon, null);
                    //如果契约兽已经召唤，则更新契约兽
                    if (Pets != null)
                    {
                        //查找玩家的宠物，看有没此契约兽
                        for (int i = 0; i < Pets.Count; i++)
                        {
                            MonsterObject pet = Pets[i];
                            if (pet == null || pet.Dead || pet.myMonster == null || pet.myMonster.idx != mon.idx)
                            {
                                continue;
                            }
                            MyMonsterUtils.RefreshMyMonLevelStats(mon, pet);
                        }
                    }

                    //删除背包物品
                    S.DeleteItem p = new S.DeleteItem { UniqueID = itemid, Count = temp.Count };
                    Info.Inventory[index] = null;
                    RefreshBagWeight();
                    Enqueue(p);
                    GetMyMonsters();
                    return;
                }


                //吞噬万物，除了书页
                if (mon.hasMonSk(MyMonSkill.MyMonSK6))
                {
                    //吃装备，3倍的经验获得
                    uint exp = temp.Info.Price * temp.Count * 3;
                    exp = exp * (uint)Settings.LevelGoldExpList[mon.MonLevel];

                    MyMonsterUtils.MyMonExp(mon, exp, this, null);
                    //删除背包物品
                    S.DeleteItem p = new S.DeleteItem { UniqueID = itemid, Count = temp.Count };
                    Info.Inventory[index] = null;
                    RefreshBagWeight();
                    Enqueue(p);

                    //这里给主人增加经验/金币
                    if (mon.canDevour((int)temp.Info.Price))
                    {
                        uint pexp = temp.Info.Price;
                        if (pexp > 10000 * 10)
                        {
                            pexp = 10000 * 10;
                        }

                        pexp = pexp * (uint)Settings.LevelGoldExpList[Level]* temp.Count;
                        GainExp(pexp);
                    }
                    else
                    {
                        WinGold(temp.SellPrice()/2);
                    }
                    return;
                }

                //吃装备
                //只吃比自己低10级的装备
                //如果装备不是要去等级的，就可以吃，否则只吃比自己低10级的装备了
                if (temp.isEquip())
                {
                    if (mon.MonLevel >= Level + 3)
                    {
                        ReceiveChat($"[{mon.getName()}] 主人你的等级太低了，我已无法成长，你要快点升级啊.", ChatType.System);
                        return;
                    }
                    if (temp.Info.eatLevel < mon.MonLevel - 15)
                    {
                        ReceiveChat($"[{mon.getName()}] 此物品太低级了，我可吃不下.", ChatType.System);
                        return;
                    }
                    else
                    {
                        //吃装备，双倍的经验获得
                        uint exp = temp.Info.Price * temp.Count * 3;
                        exp = exp * (uint)Settings.LevelGoldExpList[Level];

                        MyMonsterUtils.MyMonExp(mon, exp,this,null);
                        //删除背包物品
                        S.DeleteItem p = new S.DeleteItem { UniqueID = itemid, Count = temp.Count };
                        Info.Inventory[index] = null;
                        RefreshBagWeight();
                        Enqueue(p);
                        return;
                    }
                }

                ReceiveChat($"[{mon.getName()}] 这是什么啊,不吃...不吃...", ChatType.System);
                return;
            }



        }

        /// <summary>
        /// 针对其他的砸蛋
        /// </summary>
        /// <param name="fromID"></param>
        /// <param name="toID"></param>
        public void CombineOtherItem(ulong fromID, ulong toID)
        {
            S.CombineItem p = new S.CombineItem { IDFrom = fromID, IDTo = toID, Success = false };
            UserItem[] array = Info.Inventory;//背包的物品
            UserItem tempFrom = null;
            UserItem tempTo = null;
            int indexFrom = -1;
            int indexTo = -1;
            //死了不允许处理
            if (Dead)
            {
                Enqueue(p);
                return;
            }

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null || array[i].UniqueID != fromID) continue;
                indexFrom = i;
                tempFrom = array[i];
                break;
            }

            if (tempFrom == null || indexFrom == -1)
            {
                Enqueue(p);
                return;
            }

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null || array[i].UniqueID != toID) continue;
                indexTo = i;
                tempTo = array[i];
                break;
            }

            if (tempTo == null || indexTo == -1)
            {
                Enqueue(p);
                return;
            }
            //砸蛋，开兽丹
            if(tempFrom.Info.Type == ItemType.Nothing && tempFrom.Info.Shape==11 && tempTo.Info.Type== ItemType.MonsterDan)
            {
                //判断兽丹是否有效
                if (tempTo.src_mon_idx == 0)
                {
                    ReceiveChat("当前兽丹无效.", ChatType.Hint);
                    Enqueue(p);
                    return;
                }

                MonsterObject mob = MonsterObject.GetMonster(Envir.GetMonsterInfo(tempTo.src_mon_idx));
                if (mob == null)
                {
                    ReceiveChat("当前兽丹对应的怪物无效.", ChatType.Hint);
                    Enqueue(p);
                    return;
                }
                if (!mob.Info.CanTreaty)
                {
                    ReceiveChat("当前兽丹对应的怪物无效.", ChatType.Hint);
                    Enqueue(p);
                    return;
                }

                //判断契约兽是否签约满了
                int monIdx = -1;
                for(int i=0;i< Info.MyMonsters.Length; i++)
                {
                    if (Info.MyMonsters[i] == null)
                    {
                        monIdx = i;
                        break;
                    }
                }
                if (monIdx == -1)
                {
                    ReceiveChat("您的契约兽已满，无法再次进行签约.", ChatType.Hint);
                    Enqueue(p);
                    return;
                }


                if (RandomUtils.Next(100) < tempFrom.Info.Strong)
                {
                    //成功
                    if (tempFrom.Count > 1)
                    {
                        tempFrom.Count--;
                        Enqueue(new S.RefreshItem { Item = tempFrom });
                    }
                    else {
                        Info.Inventory[indexFrom] = null;
                    }

                    Info.Inventory[indexTo] = null;
                    p.Destroy = true;

                    Report.ItemCombined("CombineItem", tempFrom, tempTo, indexFrom, indexTo, MirGridType.Inventory);

                    //item merged ok
                    TradeUnlock();
                    //签约灵兽
                    Info.MyMonsters[monIdx] = new MyMonster(tempTo.src_mon_idx, (ushort)mob.Info.Image, mob.Name, mob.Info.UpChance);
                    //重置契约兽各种属性
                    MyMonsterUtils.RefreshMyMonLevelStats(Info.MyMonsters[monIdx], null);

                    p.Success = true;
                    Enqueue(p);
                    GetMyMonsters();
                    ReceiveChat("签约灵兽成功.", ChatType.Hint);


                    return;
                }
                else
                {
                    //失败
                    if (tempFrom.Count > 1)
                    {
                        tempFrom.Count--;
                        Enqueue(new S.RefreshItem { Item = tempFrom });
                    }
                    else {
                        Info.Inventory[indexFrom] = null;
                    }

                    Info.Inventory[indexTo] = null;
                    p.Destroy = true;

                    Report.ItemCombined("CombineItem", tempFrom, tempTo, indexFrom, indexTo, MirGridType.Inventory);

                    //item merged ok
                    TradeUnlock();

                    p.Success = true;
                    Enqueue(p);
                    ReceiveChat("签约灵兽失败.", ChatType.System2);
                    return;
                }
            }

            //返回信息
            Enqueue(p);
        }

        //合并物品？
        //把背包的某个物品放到某个装备上。
        //比如特殊修理
        //比如装备升级
        public void CombineItem(ulong fromID, ulong toID)
        {
            S.CombineItem p = new S.CombineItem { IDFrom = fromID, IDTo = toID, Success = false };

            UserItem[] array = Info.Inventory;//背包的物品
            UserItem tempFrom = null;
            UserItem tempTo = null;
            int indexFrom = -1;
            int indexTo = -1;
            //死了不允许处理
            if (Dead)
            {
                Enqueue(p);
                return;
            }

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null || array[i].UniqueID != fromID) continue;
                indexFrom = i;
                tempFrom = array[i];
                break;
            }

            if (tempFrom == null || indexFrom == -1)
            {
                Enqueue(p);
                return;
            }

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == null || array[i].UniqueID != toID) continue;
                indexTo = i;
                tempTo = array[i];
                break;
            }

            if (tempTo == null || indexTo == -1)
            {
                Enqueue(p);
                return;
            }

            //必须使宝石类的才可以
            if (tempFrom.Info.Type != ItemType.Gem)
            {
                CombineOtherItem(fromID,toID);
                return;
            }

            //必须使是装备才可以
            if ((byte)tempTo.Info.Type < 1 || (byte)tempTo.Info.Type > 11)
            {
                CombineOtherItem(fromID, toID);
                return;
            }

            bool canRepair = false, canUpgrade = false;

            switch (tempFrom.Info.Shape)
            {
                case 1: //BoneHammer
                case 2: //SewingSupplies
                case 5: //SpecialHammer
                case 6: //SpecialSewingSupplies  特殊修理

                    if (tempTo.Info.Bind.HasFlag(BindMode.DontRepair))
                    {
                        Enqueue(p);
                        return;
                    }

                    switch (tempTo.Info.Type)
                    {
                        case ItemType.Weapon:
                        case ItemType.Necklace:
                        case ItemType.Ring:
                        case ItemType.Bracelet:
                            if (tempFrom.Info.Shape == 1 || tempFrom.Info.Shape == 5)
                                canRepair = true;
                            break;
                        case ItemType.Armour:
                        case ItemType.Helmet:
                        case ItemType.Boots:
                        case ItemType.Belt:
                            if (tempFrom.Info.Shape == 2 || tempFrom.Info.Shape == 6)
                                canRepair = true;
                            break;
                        default:
                            canRepair = false;
                            break;
                    }

                    if (canRepair != true)
                    {
                        Enqueue(p);
                        return;
                    }

                    if (tempTo.CurrentDura == tempTo.MaxDura)
                    {
                        ReceiveChat("物品不需要修理.", ChatType.Hint);
                        Enqueue(p);
                        return;
                    }
                    break;
                case 3: //gems
                case 4: //orbs，宝玉砸装备系统
                    if (tempTo.Info.Bind.HasFlag(BindMode.DontUpgrade) || tempTo.Info.Unique != SpecialItemMode.None)
                    {
                        Enqueue(p);
                        return;
                    }
                    //如果是租借的，并且不能升级的
                    if (tempTo.RentalInformation != null && tempTo.RentalInformation.BindingFlags.HasFlag(BindMode.DontUpgrade))
                    {
                        Enqueue(p);
                        return;
                    }

                    if ((tempTo.GemCount >= tempFrom.Info.CriticalDamage) || (GetCurrentStatCount(tempFrom, tempTo) >= tempFrom.Info.HpDrainRate))
                    {
                        ReceiveChat("物品已达到最大加强", ChatType.Hint);
                        Enqueue(p);
                        return;
                    }

                    if (ServerConfig.openMaxGem && tempTo.GemCount >= tempTo.MaxGem)
                    {
                        ReceiveChat("物品已达到最大加强", ChatType.Hint);
                        Enqueue(p);
                        return;
                    }

                    int successchance = tempFrom.Info.Reflect;

                    // Gem is only affected by the stat applied.
                    // Drop rate per gem won't work if gems add more than 1 stat, i.e. DC + 2 per gem.
                    if (Settings.GemStatIndependent)
                    {
                        StatType GemType = GetGemType(tempFrom);

                        switch (GemType)
                        {
                            case StatType.AC:
                                successchance *= (int)tempTo.AC;
                                break;

                            case StatType.MAC:
                                successchance *= (int)tempTo.MAC;
                                break;

                            case StatType.DC:
                                successchance *= (int)tempTo.DC;
                                break;

                            case StatType.MC:
                                successchance *= (int)tempTo.MC;
                                break;

                            case StatType.SC:
                                successchance *= (int)tempTo.SC;
                                break;

                            case StatType.ASpeed:
                                successchance *= (int)tempTo.AttackSpeed;
                                break;

                            case StatType.Accuracy:
                                successchance *= (int)tempTo.Accuracy;
                                break;

                            case StatType.Agility:
                                successchance *= (int)tempTo.Agility;
                                break;

                            case StatType.Freezing:
                                successchance *= (int)tempTo.Freezing;
                                break;

                            case StatType.PoisonAttack:
                                successchance *= (int)tempTo.PoisonAttack;
                                break;

                            case StatType.MagicResist:
                                successchance *= (int)tempTo.MagicResist;
                                break;

                            case StatType.PoisonResist:
                                successchance *= (int)tempTo.PoisonResist;
                                break;

                            // These attributes may not work as more than 1 stat is
                            // added per gem, i.e + 40 HP.

                            case StatType.HP:
                                successchance *= (int)tempTo.HP;
                                break;

                            case StatType.MP:
                                successchance *= (int)tempTo.MP;
                                break;

                            case StatType.HP_Regen:
                                successchance *= (int)tempTo.HealthRecovery;
                                break;
                                
                            // I don't know if this conflicts with benes.
                            case StatType.Luck:
                                successchance *= (int)tempTo.Luck;
                                break;

                            case StatType.Strong:
                                successchance *= (int)tempTo.Strong;
                                break;

                            case StatType.PoisonRegen:
                                successchance *= (int)tempTo.PoisonRecovery;
                                break;


                            /*
                                 Currently not supported.
                                 Missing item definitions.

                                 case StatType.HP_Precent:
                                 case StatType.MP_Precent:
                                 case StatType.MP_Regen:
                                 case StatType.Holy:
                                 case StatType.Durability:

                            */
                            default:
                                successchance *= (int)tempTo.GemCount;
                                break;

                        }
                    }
                    // Gem is affected by the total added stats on the item.
                    else
                    {
                        successchance *= (int)tempTo.GemCount;
                    }

                    successchance = successchance >= tempFrom.Info.CriticalRate ? 0 : (tempFrom.Info.CriticalRate - successchance) + (GemRate * 5);

                    //成功率下调10%
                    //successchance = successchance - 10;
                    //保底成功率10%
                    if (successchance < 10)
                    {
                        successchance = 10;
                    }
                    //最高70
                    if (successchance > 70)
                    {
                        successchance = 70;
                    }
                    byte quality = tempTo.quality;
                    if(tempTo.Info.Type!= ItemType.Weapon)
                    {
                        quality = (byte)(quality * 2);
                    }

                    //check if combine will succeed
                    bool succeeded = RandomUtils.Next(100) < successchance + quality;
                    //随机失败，品质好的失败几率小一点
                    if (succeeded && RandomUtils.Next(4 + tempTo.quality) == 1)
                    {
                        succeeded = false;
                    }
                    canUpgrade = true;

                    byte itemType = (byte)tempTo.Info.Type;

                    if (!ValidGemForItem(tempFrom, itemType))
                    {
                        ReceiveChat("无效组合", ChatType.Hint);
                        Enqueue(p);
                        return;
                    }

                    if ((tempFrom.Info.MaxDC + tempFrom.DC) > 0)
                    {
                        if (succeeded) tempTo.DC = (byte)Math.Min(byte.MaxValue, tempTo.DC + tempFrom.Info.MaxDC + tempFrom.DC);
                    }

                    else if ((tempFrom.Info.MaxMC + tempFrom.MC) > 0)
                    {
                        if (succeeded) tempTo.MC = (byte)Math.Min(byte.MaxValue, tempTo.MC + tempFrom.Info.MaxMC + tempFrom.MC);
                    }

                    else if ((tempFrom.Info.MaxSC + tempFrom.SC) > 0)
                    {
                        if (succeeded) tempTo.SC = (byte)Math.Min(byte.MaxValue, tempTo.SC + tempFrom.Info.MaxSC + tempFrom.SC);
                    }

                    else if ((tempFrom.Info.MaxAC + tempFrom.AC) > 0)
                    {
                        if (succeeded) tempTo.AC = (byte)Math.Min(byte.MaxValue, tempTo.AC + tempFrom.Info.MaxAC + tempFrom.AC);
                    }

                    else if ((tempFrom.Info.MaxMAC + tempFrom.MAC) > 0)
                    {
                        if (succeeded) tempTo.MAC = (byte)Math.Min(byte.MaxValue, tempTo.MAC + tempFrom.Info.MaxMAC + tempFrom.MAC);
                    }

                    else if ((tempFrom.Info.Durability) > 0)
                    {
                        if (succeeded) tempTo.MaxDura = (ushort)Math.Min(ushort.MaxValue, tempTo.MaxDura + tempFrom.MaxDura);
                    }

                    else if ((tempFrom.Info.AttackSpeed + tempFrom.AttackSpeed) > 0)
                    {
                        if (succeeded) tempTo.AttackSpeed = (sbyte)Math.Max(sbyte.MinValue, (Math.Min(sbyte.MaxValue, tempTo.AttackSpeed + tempFrom.Info.AttackSpeed + tempFrom.AttackSpeed)));
                    }

                    else if ((tempFrom.Info.Agility + tempFrom.Agility) > 0)
                    {
                        if (succeeded) tempTo.Agility = (byte)Math.Min(byte.MaxValue, tempFrom.Info.Agility + tempTo.Agility + tempFrom.Agility);
                    }

                    else if ((tempFrom.Info.Accuracy + tempFrom.Accuracy) > 0)
                    {
                        if (succeeded) tempTo.Accuracy = (byte)Math.Min(byte.MaxValue, tempFrom.Info.Accuracy + tempTo.Accuracy + tempFrom.Accuracy);
                    }

                    else if ((tempFrom.Info.PoisonAttack + tempFrom.PoisonAttack) > 0)
                    {
                        if (succeeded) tempTo.PoisonAttack = (byte)Math.Min(byte.MaxValue, tempFrom.Info.PoisonAttack + tempTo.PoisonAttack + tempFrom.PoisonAttack);
                    }

                    else if ((tempFrom.Info.Freezing + tempFrom.Freezing) > 0)
                    {
                        if (succeeded) tempTo.Freezing = (byte)Math.Min(byte.MaxValue, tempFrom.Info.Freezing + tempTo.Freezing + tempFrom.Freezing);
                    }

                    else if ((tempFrom.Info.MagicResist + tempFrom.MagicResist) > 0)
                    {
                        if (succeeded) tempTo.MagicResist = (byte)Math.Min(byte.MaxValue, tempFrom.Info.MagicResist + tempTo.MagicResist + tempFrom.MagicResist);
                    }
                    else if ((tempFrom.Info.PoisonResist + tempFrom.PoisonResist) > 0)
                    {
                        if (succeeded) tempTo.PoisonResist = (byte)Math.Min(byte.MaxValue, tempFrom.Info.PoisonResist + tempTo.PoisonResist + tempFrom.PoisonResist);
                    }
                    else if ((tempFrom.Info.Luck + tempFrom.Luck) > 0)
                    {
                        if (succeeded) tempTo.Luck = (sbyte)Math.Min(sbyte.MaxValue, tempFrom.Info.Luck + tempTo.Luck + tempFrom.Luck);
                    }
                    else
                    {
                        ReceiveChat("无法组合这些物品.", ChatType.Hint);
                        Enqueue(p);
                        return;
                    }

                    if (!succeeded)
                    {
                        //3分之1的几率碎
                        if ((tempFrom.Info.Shape == 3) && (RandomUtils.Next(tempTo.quality+3) == 1) )
                        {
                            //item destroyed
                            ReceiveChat("物品已经损坏.", ChatType.Hint);
                            Report.ItemChanged("CombineItem (Item Destroyed)", Info.Inventory[indexTo], 1, 1);

                            Info.Inventory[indexTo] = null;
                            p.Destroy = true;
                        }
                        else
                        {
                            //upgrade has no effect
                            ReceiveChat("升级没有效果.", ChatType.Hint);
                        }

                        canUpgrade = false;
                    }
                    else
                    {
                        tempTo.GemCount++;
                    }
                    break;
               case 11://混沌石升级系统
                    if (tempTo.Info.Bind.HasFlag(BindMode.DontUpgrade) || tempTo.Info.Unique != SpecialItemMode.None)
                    {
                        Enqueue(p);
                        return;
                    }
                    if (tempTo.RentalInformation != null && tempTo.RentalInformation.BindingFlags.HasFlag(BindMode.DontUpgrade))
                    {
                        Enqueue(p);
                        return;
                    }
                    if((tempTo.quality >= 5 && tempTo.Info.Type != ItemType.Weapon) || (tempTo.quality >= 10 && tempTo.Info.Type == ItemType.Weapon))
                    {
                        ReceiveChat("物品已达到最大强化", ChatType.Hint);
                        Enqueue(p);
                        return;
                    }
                    byte type2 = (byte)tempTo.Info.Type;
                    if (!ValidGemForItem(tempFrom, type2))
                    {
                        this.ReceiveChat("当前装备无法使用", ChatType.Hint);
                        Enqueue(p);
                        return;
                    }
                    //升级成功率75
                    int change = 75;
                    if (tempTo.Info.Type == ItemType.Weapon)
                    {
                        change -= (int)(tempTo.quality * 7);
                    }
                    else
                    {
                        change -= (int)(tempTo.quality * 15);
                    }
                    if (change<5)
                    {
                        change = 5;
                    }
                    if (change>70)
                    {
                        change = 70;
                    }
                    bool suss2 = RandomUtils.Next(100) < change;
                    if (suss2)
                    {
                        tempTo.quality += 1;
                        if (RandomUtils.Next(100) < 70)
                        {
                            tempTo.spiritual += 1;
                        }
                        canUpgrade = true;
                    }
                    else
                    {
                        this.ReceiveChat("升级没有效果.", ChatType.Hint);
                        canUpgrade = false;
                    }
                    break;
                case 12://品质，灵性清洗石
                    if (tempTo.Info.Bind.HasFlag(BindMode.DontUpgrade) || tempTo.Info.Unique != SpecialItemMode.None)
                    {
                        Enqueue(p);
                        return;
                    }
                    if (tempTo.RentalInformation != null && tempTo.RentalInformation.BindingFlags.HasFlag(BindMode.DontUpgrade))
                    {
                        Enqueue(p);
                        return;
                    }
                    if (tempTo.quality <= 0)
                    {
                        ReceiveChat("当前装备无需清洗", ChatType.Hint);
                        Enqueue(p);
                        return;
                    }
                    if (tempTo.Count >1 )
                    {
                        ReceiveChat("当前装备无法清洗", ChatType.Hint);
                        Enqueue(p);
                        return;
                    }

                    tempTo.quality = 0;
                    tempTo.spiritual = 0;
                    tempTo.samsaracount = 0;
                    tempTo.samsaratype = 0;
                    tempTo.SA_AC = 0;
                    tempTo.SA_MAC = 0;
                    tempTo.SA_DC = 0;
                    tempTo.SA_MC = 0;
                    tempTo.SA_SC = 0;
                    canUpgrade = true;
                    ReceiveChat("装备已清洗.", ChatType.Hint);
                    Enqueue(new S.ItemUpgraded { Item = tempTo });
                    if (tempFrom.Count > 1)
                    {
                        tempFrom.Count--;
                        Enqueue(new S.RefreshItem { Item = tempFrom });
                    }
                    else {
                        Info.Inventory[indexFrom] = null;
                    }
                    TradeUnlock();
                    p.Success = true;
                    Enqueue(p);
                    return;
                case 13://所有属性全部清洗
                    if (tempTo.Info.Bind.HasFlag(BindMode.DontUpgrade) || tempTo.Info.Unique != SpecialItemMode.None)
                    {
                        Enqueue(p);
                        return;
                    }
                    if (tempTo.RentalInformation != null && tempTo.RentalInformation.BindingFlags.HasFlag(BindMode.DontUpgrade))
                    {
                        Enqueue(p);
                        return;
                    }
                    if (tempTo.Count > 1)
                    {
                        ReceiveChat("当前装备无法清洗", ChatType.Hint);
                        Enqueue(p);
                        return;
                    }

                    tempTo.quality = 0;
                    tempTo.spiritual = 0;
                    tempTo.samsaracount = 0;
                    tempTo.samsaratype = 0;
                    tempTo.SA_AC = 0;
                    tempTo.SA_MAC = 0;
                    tempTo.SA_DC = 0;
                    tempTo.SA_MC = 0;
                    tempTo.SA_SC = 0;
                    tempTo.AC = tempTo.MAC = tempTo.DC = tempTo.MC = tempTo.SC = tempTo.Accuracy = tempTo.Agility = tempTo.HP = tempTo.MP = tempTo.Strong = tempTo.MagicResist = tempTo.PoisonResist = tempTo.HealthRecovery = tempTo.ManaRecovery = tempTo.PoisonRecovery = tempTo.CriticalRate = tempTo.CriticalDamage = tempTo.Freezing = tempTo.PoisonAttack = 0;
                    tempTo.AttackSpeed = 0;
                    tempTo.GemCount = 0;
                    tempTo.RefineTime = 0;
                    canUpgrade = true;
                    ReceiveChat("装备已清洗.", ChatType.Hint);
                    Enqueue(new S.ItemUpgraded { Item = tempTo });
                    if (tempFrom.Count > 1)
                    {
                        tempFrom.Count--;
                        Enqueue(new S.RefreshItem { Item = tempFrom });
                    }
                    else {
                        Info.Inventory[indexFrom] = null;
                    }
                    TradeUnlock();
                    p.Success = true;
                    Enqueue(p);
                    return;
                default:
                    Enqueue(p);
                    return;
            }


            //刷新包裹
            RefreshBagWeight();

            if (canRepair && Info.Inventory[indexTo] != null)
            {
                switch (tempTo.Info.Shape)
                {
                    case 1:
                    case 2:
                        {
                            tempTo.MaxDura = (ushort)Math.Max(0, Math.Min(tempTo.MaxDura, tempTo.MaxDura - 100 * RandomUtils.Next(10)));
                        }
                        break;
                    default:
                        break;
                }
                tempTo.CurrentDura = tempTo.MaxDura;
                tempTo.DuraChanged = false;

                ReceiveChat("物品已修复.", ChatType.Hint);
                Enqueue(new S.ItemRepaired { UniqueID = tempTo.UniqueID, MaxDura = tempTo.MaxDura, CurrentDura = tempTo.CurrentDura });
            }

            if (canUpgrade && Info.Inventory[indexTo] != null)
            {
               
                ReceiveChat("物品已升级.", ChatType.Hint);
                Enqueue(new S.ItemUpgraded { Item = tempTo });
            }

            if (tempFrom.Count > 1)
            {
                tempFrom.Count--;
                Enqueue(new S.RefreshItem { Item = tempFrom });
            }
            else {
                Info.Inventory[indexFrom] = null;
            }

            Report.ItemCombined("CombineItem", tempFrom, tempTo, indexFrom, indexTo, MirGridType.Inventory);

            //item merged ok
            TradeUnlock();

            p.Success = true;
            Enqueue(p);
        }
        private bool ValidGemForItem(UserItem Gem, byte itemtype)
        {
            switch (itemtype)
            {
                case 1: //weapon
                    if (Gem.Info.Unique.HasFlag(SpecialItemMode.Paralize))
                        return true;
                    break;
                case 2: //Armour
                    if (Gem.Info.Unique.HasFlag(SpecialItemMode.Teleport))
                        return true;
                    break;
                case 4: //Helmet
                    if (Gem.Info.Unique.HasFlag(SpecialItemMode.Clearring))
                        return true;
                    break;
                case 5: //necklace
                    if (Gem.Info.Unique.HasFlag(SpecialItemMode.Protection))
                        return true;
                    break;
                case 6: //bracelet
                    if (Gem.Info.Unique.HasFlag(SpecialItemMode.Revival))
                        return true;
                    break;
                case 7: //ring
                    if (Gem.Info.Unique.HasFlag(SpecialItemMode.Muscle))
                        return true;
                    break;
                case 8: //amulet
                    if (Gem.Info.Unique.HasFlag(SpecialItemMode.Flame))
                        return true;
                    break;
                case 9://belt
                    if (Gem.Info.Unique.HasFlag(SpecialItemMode.Healing))
                        return true;
                    break;
                case 10: //boots
                    if (Gem.Info.Unique.HasFlag(SpecialItemMode.Probe))
                        return true;
                    break;
                case 11: //stone
                    if (Gem.Info.Unique.HasFlag(SpecialItemMode.Skill))
                        return true;
                    break;
                case 12:///torch
                    if (Gem.Info.Unique.HasFlag(SpecialItemMode.NoDuraLoss))
                        return true;
                    break;
            }
            return false;
        }
        //Gems granting multiple stat types are not compatiable with this method.
        private StatType GetGemType(UserItem gem)
        {
            if ((gem.Info.MaxDC + gem.DC) > 0)
                return StatType.DC;

            else if ((gem.Info.MaxMC + gem.MC) > 0)
                return StatType.MC;

            else if ((gem.Info.MaxSC + gem.SC) > 0)
                return StatType.SC;

            else if ((gem.Info.MaxAC + gem.AC) > 0)
                return StatType.AC;

            else if ((gem.Info.MaxMAC + gem.MAC) > 0)
                return StatType.MAC;

            else if ((gem.Info.Durability) > 0)
                return StatType.Durability;

            else if ((gem.Info.AttackSpeed + gem.AttackSpeed) > 0)
                return StatType.ASpeed;

            else if ((gem.Info.Agility + gem.Agility) > 0)
                return StatType.Agility;

            else if ((gem.Info.Accuracy + gem.Accuracy) > 0)
                return StatType.Accuracy;

            else if ((gem.Info.PoisonAttack + gem.PoisonAttack) > 0)
                return StatType.PoisonAttack;

            else if ((gem.Info.Freezing + gem.Freezing) > 0)
                return StatType.Freezing;

            else if ((gem.Info.MagicResist + gem.MagicResist) > 0)
                return StatType.MagicResist;

            else if ((gem.Info.PoisonResist + gem.PoisonResist) > 0)
                return StatType.PoisonResist;

            else if ((gem.Info.Luck + gem.Luck) > 0)
                return StatType.Luck;

            else if ((gem.Info.PoisonRecovery + gem.PoisonRecovery) > 0)
                return StatType.PoisonRegen;

            else if ((gem.Info.HP + gem.HP) > 0)
                return StatType.HP;

            else if ((gem.Info.MP + gem.MP) > 0)
                return StatType.MP;

            else if ((gem.Info.HealthRecovery + gem.HealthRecovery) > 0)
                return StatType.HP_Regen;

            // These may be incomplete. Item definitions may be missing?

            else if ((gem.Info.HPrate) > 0)
                return StatType.HP_Percent;

            else if ((gem.Info.MPrate) > 0)
                return StatType.MP_Percent;

            else if ((gem.Info.SpellRecovery) > 0)
                return StatType.MP_Regen;

            else if ((gem.Info.Holy) > 0)
                return StatType.Holy;

            else if ((gem.Info.Strong + gem.Strong) > 0)
                return StatType.Strong;

            else if (gem.Info.HPrate > 0)
                return StatType.HP_Regen;

            else
                return StatType.Unknown;
        }
        //Gems granting multiple stat types are not compatible with this method.
        private int GetCurrentStatCount(UserItem gem, UserItem item)
        {
            if ((gem.Info.MaxDC + gem.DC) > 0)
                return item.DC;

            else if ((gem.Info.MaxMC + gem.MC) > 0)
                return item.MC;

            else if ((gem.Info.MaxSC + gem.SC) > 0)
                return item.SC;

            else if ((gem.Info.MaxAC + gem.AC) > 0)
                return item.AC;

            else if ((gem.Info.MaxMAC + gem.MAC) > 0)
                return item.MAC;

            else if ((gem.Info.Durability) > 0)
                return item.Info.Durability > item.MaxDura ? 0 : ((item.MaxDura - item.Info.Durability) / 1000);

            else if ((gem.Info.AttackSpeed + gem.AttackSpeed) > 0)
                return item.AttackSpeed;

            else if ((gem.Info.Agility + gem.Agility) > 0)
                return item.Agility;

            else if ((gem.Info.Accuracy + gem.Accuracy) > 0)
                return item.Accuracy;

            else if ((gem.Info.PoisonAttack + gem.PoisonAttack) > 0)
                return item.PoisonAttack;

            else if ((gem.Info.Freezing + gem.Freezing) > 0)
                return item.Freezing;

            else if ((gem.Info.MagicResist + gem.MagicResist) > 0)
                return item.MagicResist;

            else if ((gem.Info.PoisonResist + gem.PoisonResist) > 0)
                return item.PoisonResist;

            else if ((gem.Info.Luck + gem.Luck) > 0)
                return item.Luck;

            else if ((gem.Info.PoisonRecovery + gem.PoisonRecovery) > 0)
                return item.PoisonRecovery;

            else if ((gem.Info.HP + gem.HP) > 0)
                return item.HP;

            else if ((gem.Info.MP + gem.MP) > 0)
                return item.MP;

            else if ((gem.Info.HealthRecovery + gem.HealthRecovery) > 0)
                return item.HealthRecovery;

            // Definitions are missing for these.
            /*
            else if ((gem.Info.HPrate) > 0)
                return item.h

            else if ((gem.Info.MPrate) > 0)
                return 

            else if ((gem.Info.SpellRecovery) > 0)
                return 

            else if ((gem.Info.Holy) > 0)
                return 

            else if ((gem.Info.Strong + gem.Strong) > 0)
                return 

            else if (gem.Info.HPrate > 0)
                return
            */
            return 0;
        }
        public void DropItem(ulong id, uint count)
        {
            S.DropItem p = new S.DropItem { UniqueID = id, Count = count, Success = false };
            if (Dead)
            {
                Enqueue(p);
                return;
            }

            if (CurrentMap.Info.NoThrowItem)
            {
                ReceiveChat("您不能在此地图上丢弃物品", ChatType.System);
                Enqueue(p);
                return;
            }

            UserItem temp = null;
            int index = -1;

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                temp = Info.Inventory[i];
                if (temp == null || temp.UniqueID != id) continue;
                index = i;
                break;
            }

            if (temp == null || index == -1 || count > temp.Count)
            {
                Enqueue(p);
                return;
            }

            if (temp.Info.Bind.HasFlag(BindMode.DontDrop))
            {
                Enqueue(p);
                return;
            }

            if (temp.RentalInformation != null && temp.RentalInformation.BindingFlags.HasFlag(BindMode.DontDrop))
            {
                Enqueue(p);
                return;
            }

            if (temp.Count == count)
            {
                if (!temp.Info.Bind.HasFlag(BindMode.DestroyOnDrop))
                    if (!DropItem(temp))
                    {
                        Enqueue(p);
                        return;
                    }
                Info.Inventory[index] = null;
            }
            else
            {
                UserItem temp2 = temp.Info.CreateFreshItem();
                temp2.Count = count;
                if (!temp.Info.Bind.HasFlag(BindMode.DestroyOnDrop))
                    if (!DropItem(temp2))
                    {
                        Enqueue(p);
                        return;
                    }
                temp.Count -= count;
            }
            p.Success = true;
            Enqueue(p);
            RefreshBagWeight();

            Report.ItemChanged("DropItem", temp, count, 1);
        }
        public void DropGold(uint gold)
        {
            if (Account.Gold < gold) return;

            ItemObject ob = new ItemObject(this, gold);

            if (!ob.Drop(5)) return;
            Account.Gold -= gold;
            Enqueue(new S.LoseGold { Gold = gold });
        }
        //捡取物品
        public void PickUp()
        {
            if (Dead)
            {
                //Send Fail
                return;
            }

            //Cell cell = CurrentMap.GetCell(CurrentLocation);

            bool sendFail = false;

            for (int i = 0; i < CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y].Count; i++)
            {
                MapObject ob = CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i];

                if (ob.Race != ObjectType.Item) continue;
                //加入战场捡取
                if(ob.Owner!=null && ob.Owner.WGroup != WGroup)
                {
                    sendFail = true;
                    continue;
                }
                if (ob.Owner != null && ob.Owner != this && !IsGroupMember(ob.Owner) && WGroup== WarGroup.None) //Or Group member.
                {
                    sendFail = true;
                    continue;
                }
                ItemObject item = (ItemObject)ob;

                if (item.Item != null)
                {
                    if (!CanGainItem(item.Item)) continue;

                    if (item.Item.Info.ShowGroupPickup && IsGroupMember(this))
                        for (int j = 0; j < GroupMembers.Count; j++)
                            GroupMembers[j].ReceiveChat(Name + " 发现: {" + item.Item.Name + "}",
                                ChatType.System);

                    GainItem(item.Item);

                    Report.ItemChanged("PickUpItem", item.Item, item.Item.Count, 2);

                    CurrentMap.RemoveObject(ob);
                    ob.Despawn();

                    return;
                }

                if (!CanGainGold(item.Gold)) continue;

                GainGold(item.Gold);
                CurrentMap.RemoveObject(ob);
                ob.Despawn();
                return;
            }

            if (sendFail)
                ReceiveChat("一定时间内不能捡取.", ChatType.System);

        }

        private bool IsGroupMember(MapObject player)
        {
            if (player.Race != ObjectType.Player) return false;
            return GroupMembers != null && GroupMembers.Contains(player);
        }

        public override bool CanGainGold(uint gold)
        {
            return (UInt64)gold + Account.Gold <= uint.MaxValue;
        }
        public override void WinGold(uint gold)
        {
            if (GroupMembers == null)
            {
                GainGold(gold);
                return;
            }

            uint count = 0;

            for (int i = 0; i < GroupMembers.Count; i++)
            {
                PlayerObject player = GroupMembers[i];
                if (player.CurrentMap == CurrentMap && Functions.InRange(player.CurrentLocation, CurrentLocation, Globals.DataRange) && !player.Dead)
                    count++;
            }

            if (count == 0 || count > gold)
            {
                GainGold(gold);
                return;
            }
            gold = gold / count;

            for (int i = 0; i < GroupMembers.Count; i++)
            {
                PlayerObject player = GroupMembers[i];
                if (player.CurrentMap == CurrentMap && Functions.InRange(player.CurrentLocation, CurrentLocation, Globals.DataRange) && !player.Dead)
                    player.GainGold(gold);
            }
        }
        //得到金币
        public void GainGold(uint gold)
        {
            if (gold == 0) return;

            if (((UInt64)Account.Gold + gold) > uint.MaxValue)
                gold = uint.MaxValue - Account.Gold;

            Account.Gold += gold;

            Enqueue(new S.GainedGold { Gold = gold });
        }
        //得到元宝
        public void GainCredit(uint credit)
        {
            if (credit == 0) return;

            if (((UInt64)Account.Credit + credit) > uint.MaxValue)
                credit = uint.MaxValue - Account.Credit;

            Account.Credit += credit;

            Enqueue(new S.GainedCredit { Credit = credit });
        }

        //失去金币
        public void LoseGold(uint gold)
        {
            if (gold == 0) return;
            if(Account.Gold< gold)
            {
                gold = Account.Gold;
            }
            Account.Gold -= gold;
            Enqueue(new S.LoseGold { Gold = gold });
        }

        //失去元宝
        public void LoseCredit(uint credit)
        {
            if (credit == 0) return;
            if (Account.Credit < credit)
            {
                credit = Account.Credit;
            }
            Account.Credit -= credit;
            Enqueue(new S.LoseCredit { Credit = credit });
        }

        //检测是否能携带此物品，检测负重，检测包裹
        public bool CanGainItem(UserItem item, bool useWeight = true)
        {
            if (item.Info.Type == ItemType.Amulet)
            {
                if (FreeSpace(Info.Inventory) > 0 && (CurrentBagWeight + item.Weight <= MaxBagWeight || !useWeight)) return true;

                uint count = item.Count;

                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    UserItem bagItem = Info.Inventory[i];

                    if (bagItem == null || bagItem.Info != item.Info) continue;

                    if (bagItem.Count + count <= bagItem.Info.StackSize) return true;

                    count -= bagItem.Info.StackSize - bagItem.Count;
                }

                return false;
            }

            if (useWeight && CurrentBagWeight + (item.Weight) > MaxBagWeight) return false;

            if (FreeSpace(Info.Inventory) > 0) return true;

            if (item.Info.StackSize > 1)
            {
                uint count = item.Count;

                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    UserItem bagItem = Info.Inventory[i];

                    if (bagItem.Info != item.Info) continue;

                    if (bagItem.Count + count <= bagItem.Info.StackSize) return true;

                    count -= bagItem.Info.StackSize - bagItem.Count;
                }
            }

            return false;
        }
        public bool CanGainItems(UserItem[] items)
        {
            int itemCount = items.Count(e => e != null);
            uint itemWeight = 0;
            uint stackOffset = 0;

            if (itemCount < 1) return true;

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null) continue;

                itemWeight += items[i].Weight;

                if (items[i].Info.StackSize > 1)
                {
                    uint count = items[i].Count;

                    for (int u = 0; u < Info.Inventory.Length; u++)
                    {
                        UserItem bagItem = Info.Inventory[u];

                        if (bagItem == null || bagItem.Info != items[i].Info) continue;

                        if (bagItem.Count + count > bagItem.Info.StackSize) stackOffset++;

                        break;
                    }
                }
            }

            if (CurrentBagWeight + (itemWeight) > MaxBagWeight) return false;
            if (FreeSpace(Info.Inventory) < itemCount + stackOffset) return false;

            return true;
        }

        public void GainItem(UserItem item)
        {
            //CheckItemInfo(item.Info);
            CheckItem(item);

            UserItem clonedItem = item.Clone();

            Enqueue(new S.GainedItem { Item = clonedItem }); //Cloned because we are probably going to change the amount.

            AddItem(item);
            RefreshBagWeight();

        }
        public void GainItemMail(UserItem item, int reason)
        {
            string sender = "Bichon Administrator";
            string message = "You have been automatically sent an item \r\ndue to the following reason.\r\n";

            switch (reason)
            {
                case 1:
                    message = "Could not return item to bag after trade.";
                    break;
                case 2:
                    message = "Your loaned item has been returned.";
                    break;
                default:
                    message = "No reason provided.";
                    break;
            }

            //sent from player
            MailInfo mail = new MailInfo(Info.Index)
            {
                Sender = sender,
                Message = message
            };

            mail.Items.Add(item);

            mail.Send();
        }

        private bool DropItem(UserItem item, int range = 1, bool DeathDrop = false)
        {
            ItemObject ob = new ItemObject(this, item, DeathDrop);

            if (!ob.Drop(range)) return false;

            if (item.Info.Type == ItemType.Meat)
                item.CurrentDura = (ushort)Math.Max(0, item.CurrentDura - 2000);

            return true;
        }

        private bool CanUseItem(UserItem item)
        {
            if (item == null) return false;

            switch (Gender)
            {
                case MirGender.Male:
                    if (!item.Info.RequiredGender.HasFlag(RequiredGender.Male))
                    {
                        ReceiveChat("你不是女人.", ChatType.System);
                        return false;
                    }
                    break;
                case MirGender.Female:
                    if (!item.Info.RequiredGender.HasFlag(RequiredGender.Female))
                    {
                        ReceiveChat("你不是男的.", ChatType.System);
                        return false;
                    }
                    break;
            }

            switch (Class)
            {
                case MirClass.Warrior:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Warrior))
                    {
                        ReceiveChat("战士不能使用此物品.", ChatType.System);
                        return false;
                    }
                    break;
                case MirClass.Wizard:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Wizard))
                    {
                        ReceiveChat("法师不能使用此物品.", ChatType.System);
                        return false;
                    }
                    break;
                case MirClass.Taoist:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Taoist))
                    {
                        ReceiveChat("道士不能使用此物品.", ChatType.System);
                        return false;
                    }
                    break;
                case MirClass.Assassin:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Assassin))
                    {
                        ReceiveChat("刺客不能使用此物品.", ChatType.System);
                        return false;
                    }
                    break;
                case MirClass.Archer:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Archer))
                    {
                        ReceiveChat("弓箭手不能使用此物品.", ChatType.System);
                        return false;
                    }
                    break;
            }

            switch (item.Info.RequiredType)
            {
                case RequiredType.Level:
                    if (Level < item.Info.RequiredAmount)
                    {
                        ReceiveChat("你的等级不够.", ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MaxAC:
                    if (MaxAC < item.Info.RequiredAmount)
                    {
                        ReceiveChat("你的防御不够.", ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MaxMAC:
                    if (MaxMAC < item.Info.RequiredAmount)
                    {
                        ReceiveChat("你的魔御不够.", ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MaxDC:
                    if (MaxDC < item.Info.RequiredAmount)
                    {
                        ReceiveChat("你的攻击不够.", ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MaxMC:
                    if (MaxMC < item.Info.RequiredAmount)
                    {
                        ReceiveChat("你的魔法不够.", ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MaxSC:
                    if (MaxSC < item.Info.RequiredAmount)
                    {
                        ReceiveChat("你的道术不够.", ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MaxLevel:
                    if (Level > item.Info.RequiredAmount)
                    {
                        ReceiveChat("你已经超过了最高等级.", ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MinAC:
                    if (MinAC < item.Info.RequiredAmount)
                    {
                        ReceiveChat("你没有足够的基础防御.", ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MinMAC:
                    if (MinMAC < item.Info.RequiredAmount)
                    {
                        ReceiveChat("你没有足够的基础魔御.", ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MinDC:
                    if (MinDC < item.Info.RequiredAmount)
                    {
                        ReceiveChat("你没有足够的基础攻击.", ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MinMC:
                    if (MinMC < item.Info.RequiredAmount)
                    {
                        ReceiveChat("你没有足够的基础魔法.", ChatType.System);
                        return false;
                    }
                    break;
                case RequiredType.MinSC:
                    if (MinSC < item.Info.RequiredAmount)
                    {
                        ReceiveChat("你没有足够的基础道术.", ChatType.System);
                        return false;
                    }
                    break;
            }

            switch (item.Info.Type)
            {
                case ItemType.Scroll:
                    switch (item.Info.Shape)
                    {
                        case 0:
                            if (CurrentMap.Info.NoEscape)
                            {
                                ReceiveChat("你不能在这里使用地牢逃脱卷", ChatType.System);
                                return false;
                            }
                            break;
                        case 2:
                            if (CurrentMap.Info.NoRandom)
                            {
                                ReceiveChat("你不能在这里使用随机传送卷", ChatType.System);
                                return false;
                            }
                            break;
                        case 6:
                            if (!Dead)
                            {
                                ReceiveChat("你不能在未死亡的时候使用复活卷轴", ChatType.Hint);
                                return false;
                            }
                            break;
                        case 10:
                            {
                                int skillId = item.Info.Effect;

                                if (MyGuild == null)
                                {
                                    ReceiveChat("你必须加入公会才能使用这个技能", ChatType.Hint);
                                    return false;
                                }
                                if (MyGuildRank != MyGuild.Ranks[0])
                                {
                                    ReceiveChat("你必须是公会领袖才能使用这个技能", ChatType.Hint);
                                    return false;
                                }
                                GuildBuffInfo buffInfo = Envir.FindGuildBuffInfo(skillId);

                                if (buffInfo == null) return false;

                                if (MyGuild.BuffList.Any(e => e.Info.Id == skillId))
                                {
                                    ReceiveChat("你的公会已经有这个技能了", ChatType.Hint);
                                    return false;
                                }
                            }
                            break;
                    }
                    break;
                case ItemType.Potion:
                    if (CurrentMap.Info.NoDrug)
                    {
                        ReceiveChat("你不能在这里使用药剂", ChatType.System);
                        return false;
                    }
                    break;

                case ItemType.Book:
                    //吃书
                    UserMagic magic = GetMagic((Spell)item.Info.Shape);
                    if (magic == null)
                    {
                        return true;
                    }
                    if (magic.Level == 3)
                    {
                        return false;
                    }
                    //等级不够，不能吃书
                    switch (magic.Level)
                    {
                        case 0:
                            if (Level < magic.Info.Level1)
                            {
                                return false;
                            }
                            break;
                        case 1:
                            if (Level < magic.Info.Level2)
                            {
                                return false;
                            }
                            break;
                        case 2:
                            if (Level < magic.Info.Level3)
                            {
                                return false;
                            }
                            break;
                        default:
                            return false;
                    }
                    break;
                case ItemType.Saddle:
                case ItemType.Ribbon:
                case ItemType.Bells:
                case ItemType.Mask:
                case ItemType.Reins:
                    if (Info.Equipment[(int)EquipmentSlot.Mount] == null)
                    {
                        ReceiveChat("只能与坐骑一起使用", ChatType.System);
                        return false;
                    }
                    break;
                case ItemType.Hook:
                case ItemType.Float:
                case ItemType.Bait:
                case ItemType.Finder:
                case ItemType.Reel:
                    if (Info.Equipment[(int)EquipmentSlot.Weapon] == null ||
                        (Info.Equipment[(int)EquipmentSlot.Weapon].Info.Shape != 49 && Info.Equipment[(int)EquipmentSlot.Weapon].Info.Shape != 50))
                    {
                        ReceiveChat("只能与鱼竿一起使用", ChatType.System);
                        return false;
                    }
                    break;
                case ItemType.Pets:
                    switch (item.Info.Shape)
                    {
                        case 20://mirror rename creature
                            if (Info.IntelligentCreatures.Count == 0) return false;
                            break;
                        case 21://creature stone
                            break;
                        case 22://nuts maintain food levels
                            if (!CreatureSummoned)
                            {
                                ReceiveChat("只能与召唤的生物一起使用", ChatType.System);
                                return false;
                            }
                            break;
                        case 23://basic creature food
                            if (!CreatureSummoned)
                            {
                                ReceiveChat("只能与召唤的生物一起使用", ChatType.System);
                                return false;
                            }
                            else
                            {
                                for (int i = 0; i < Pets.Count; i++)
                                {
                                    if (Pets[i].Info.AI != 64) continue;
                                    if (((IntelligentCreatureObject)Pets[i]).petType != SummonedCreatureType) continue;


                                    if (((IntelligentCreatureObject)Pets[i]).Fullness > 9900)
                                    {
                                        ReceiveChat(((IntelligentCreatureObject)Pets[i]).Name + " 不饥饿", ChatType.System);
                                        return false;
                                    }
                                    return true;
                                }
                                return false;
                            }
                        case 24://wonderpill vitalize creature
                            if (!CreatureSummoned)
                            {
                                ReceiveChat("只能与召唤的生物一起使用", ChatType.System);
                                return false;
                            }
                            else
                            {
                                for (int i = 0; i < Pets.Count; i++)
                                {
                                    if (Pets[i].Info.AI != 64) continue;
                                    if (((IntelligentCreatureObject)Pets[i]).petType != SummonedCreatureType) continue;


                                    if (((IntelligentCreatureObject)Pets[i]).Fullness > 0)
                                    {
                                        ReceiveChat(((IntelligentCreatureObject)Pets[i]).Name + " 不需要活力化", ChatType.System);
                                        return false;
                                    }
                                    return true;
                                }
                                return false;
                            }
                        case 25://Strongbox
                            break;
                        case 26://Wonderdrugs
                            break;
                        case 27://Fortunecookies
                            break;
                    }
                    break;
            }

            if (RidingMount && item.Info.Type != ItemType.Scroll && item.Info.Type != ItemType.Potion)
            {
                return false;
            }

            //if (item.Info.Type == ItemType.Book)
            //    for (int i = 0; i < Info.Magics.Count; i++)
            //        if (Info.Magics[i].Spell == (Spell)item.Info.Shape) return false;

            return true;
        }
        //是否可以装备某个物品
        private bool CanEquipItem(UserItem item, int slot)
        {
            switch ((EquipmentSlot)slot)
            {
                case EquipmentSlot.Weapon:
                    if (item.Info.Type != ItemType.Weapon)
                        return false;
                    break;
                case EquipmentSlot.Armour:
                    if (item.Info.Type != ItemType.Armour)
                        return false;
                    break;
                case EquipmentSlot.Helmet:
                    if (item.Info.Type != ItemType.Helmet)
                        return false;
                    break;
                case EquipmentSlot.Torch:
                    if (item.Info.Type != ItemType.Torch)
                        return false;
                    break;
                case EquipmentSlot.Necklace:
                    if (item.Info.Type != ItemType.Necklace)
                        return false;
                    break;
                case EquipmentSlot.BraceletL:
                    if (item.Info.Type != ItemType.Bracelet)
                        return false;
                    break;
                case EquipmentSlot.BraceletR:
                    if (item.Info.Type != ItemType.Bracelet && item.Info.Type != ItemType.Amulet)
                        return false;
                    break;
                case EquipmentSlot.RingL:
                case EquipmentSlot.RingR:
                    if (item.Info.Type != ItemType.Ring)
                        return false;
                    break;
                case EquipmentSlot.Amulet:
                    if (item.Info.Type != ItemType.Amulet)// || item.Info.Shape == 0
                        return false;
                    break;
                case EquipmentSlot.Boots:
                    if (item.Info.Type != ItemType.Boots)
                        return false;
                    break;
                case EquipmentSlot.Belt:
                    if (item.Info.Type != ItemType.Belt)
                        return false;
                    break;
                case EquipmentSlot.Stone:
                    if (item.Info.Type != ItemType.Stone)
                        return false;
                    break;
                case EquipmentSlot.Mount:
                    if (item.Info.Type != ItemType.Mount)
                        return false;
                    break;
                default:
                    return false;
            }


            switch (Gender)
            {
                case MirGender.Male:
                    if (!item.Info.RequiredGender.HasFlag(RequiredGender.Male))
                        return false;
                    break;
                case MirGender.Female:
                    if (!item.Info.RequiredGender.HasFlag(RequiredGender.Female))
                        return false;
                    break;
            }


            switch (Class)
            {
                case MirClass.Warrior:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Warrior))
                        return false;
                    break;
                case MirClass.Wizard:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Wizard))
                        return false;
                    break;
                case MirClass.Taoist:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Taoist))
                        return false;
                    break;
                case MirClass.Assassin:
                    if (!item.Info.RequiredClass.HasFlag(RequiredClass.Assassin))
                        return false;
                    break;
            }

            switch (item.Info.RequiredType)
            {
                case RequiredType.Level:
                    if (Level < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MaxAC:
                    if (MaxAC < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MaxMAC:
                    if (MaxMAC < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MaxDC:
                    if (MaxDC < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MaxMC:
                    if (MaxMC < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MaxSC:
                    if (MaxSC < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MaxLevel:
                    if (Level > item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MinAC:
                    if (MinAC < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MinMAC:
                    if (MinMAC < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MinDC:
                    if (MinDC < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MinMC:
                    if (MinMC < item.Info.RequiredAmount)
                        return false;
                    break;
                case RequiredType.MinSC:
                    if (MinSC < item.Info.RequiredAmount)
                        return false;
                    break;
            }

            if (item.Info.Type == ItemType.Weapon || item.Info.Type == ItemType.Torch)
            {
                if (item.Weight - (Info.Equipment[slot] != null ? Info.Equipment[slot].Weight : 0) + CurrentHandWeight > MaxHandWeight)
                    return false;
            }
            else
                if (item.Weight - (Info.Equipment[slot] != null ? Info.Equipment[slot].Weight : 0) + CurrentWearWeight > MaxWearWeight)
                    return false;

            if (RidingMount && item.Info.Type != ItemType.Torch)
            {
                return false;
            }

            return true;
        }
        private bool CanRemoveItem(MirGridType grid, UserItem item)
        {
            //Item  Stuck

            UserItem[] array;
            switch (grid)
            {
                case MirGridType.Inventory:
                    array = Info.Inventory;
                    break;
                case MirGridType.Storage:
                    array = Account.Storage;
                    break;
                default:
                    return false;
            }

            if (RidingMount && item.Info.Type != ItemType.Torch)
            {
                return false;
            }

            return FreeSpace(array) > 0;
        }

        public bool CheckQuestItem(UserItem uItem, uint count)
        {
            foreach (var item in Info.QuestInventory.Where(item => item != null && item.Info == uItem.Info))
            {
                if (count > item.Count)
                {
                    count -= item.Count;
                    continue;
                }

                if (count > item.Count) continue;
                count = 0;
                break;
            }

            return count <= 0;
        }
        public bool CanGainQuestItem(UserItem item)
        {
            if (FreeSpace(Info.QuestInventory) > 0) return true;

            if (item.Info.StackSize > 1)
            {
                uint count = item.Count;

                for (int i = 0; i < Info.QuestInventory.Length; i++)
                {
                    UserItem bagItem = Info.QuestInventory[i];

                    if (bagItem.Info != item.Info) continue;

                    if (bagItem.Count + count <= bagItem.Info.StackSize) return true;

                    count -= bagItem.Info.StackSize - bagItem.Count;
                }
            }

            ReceiveChat("你不能携带更多的任务.", ChatType.System);

            return false;
        }
        public void GainQuestItem(UserItem item)
        {
            CheckItem(item);

            UserItem clonedItem = item.Clone();

            Enqueue(new S.GainedQuestItem { Item = clonedItem });

            AddQuestItem(item);


        }
        public void TakeQuestItem(ItemInfo uItem, uint count)
        {
            for (int o = 0; o < Info.QuestInventory.Length; o++)
            {
                UserItem item = Info.QuestInventory[o];
                if (item == null) continue;
                if (item.Info != uItem) continue;

                if (count > item.Count)
                {
                    Enqueue(new S.DeleteQuestItem { UniqueID = item.UniqueID, Count = item.Count });
                    Info.QuestInventory[o] = null;

                    count -= item.Count;
                    continue;
                }

                Enqueue(new S.DeleteQuestItem { UniqueID = item.UniqueID, Count = count });

                if (count == item.Count)
                    Info.QuestInventory[o] = null;
                else
                    item.Count -= count;
                break;
            }
        }

        private void DamageDura()
        {
            if (!NoDuraLoss)
                for (int i = 0; i < Info.Equipment.Length; i++) DamageItem(Info.Equipment[i], RandomUtils.Next(1) + 1);
        }
        public void DamageWeapon()
        {
            if (!NoDuraLoss)
                DamageItem(Info.Equipment[(int)EquipmentSlot.Weapon], RandomUtils.Next(3) + 1);
        }
        //损坏物品，降低持久
        private void DamageItem(UserItem item, int amount, bool isChanged = false)
        {
            if (item == null || item.CurrentDura == 0 || item.Info.Type == ItemType.Amulet) return;
            if ((item.WeddingRing == (long)Info.Married) && (Info.Equipment[(int)EquipmentSlot.RingL].UniqueID == item.UniqueID)) return;
            if (item.Info.Strong > 0) amount = Math.Max(1, amount - item.Info.Strong);
            item.CurrentDura = (ushort)Math.Max(ushort.MinValue, item.CurrentDura - amount);
            item.DuraChanged = true;

            if (item.CurrentDura > 0 && isChanged != true) return;
            Enqueue(new S.DuraChanged { UniqueID = item.UniqueID, CurrentDura = item.CurrentDura });

            item.DuraChanged = false;
            RefreshStats();
        }
        private void ConsumeItem(UserItem item, uint cost)
        {
            item.Count -= cost;
            Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = cost });


            if (item.Count != 0) return;

            for (int i = 0; i < Info.Equipment.Length; i++)
            {
                if (Info.Equipment[i] != null && Info.Equipment[i].Slots.Length > 0)
                {
                    for (int j = 0; j < Info.Equipment[i].Slots.Length; j++)
                    {
                        if (Info.Equipment[i].Slots[j] != item) continue;
                        Info.Equipment[i].Slots[j] = null;
                        return;
                    }
                }

                if (Info.Equipment[i] != item) continue;
                Info.Equipment[i] = null;

                return;
            }

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                if (Info.Inventory[i] != item) continue;
                Info.Inventory[i] = null;
                return;
            }
            //Item not found
        }

        private bool TryLuckWeapon()
        {
            var item = Info.Equipment[(int)EquipmentSlot.Weapon];

            if (item == null || item.Luck >= 7)
                return false;

            if (item.Info.Bind.HasFlag(BindMode.DontUpgrade))
                return false;

            if (item.RentalInformation != null && item.RentalInformation.BindingFlags.HasFlag(BindMode.DontUpgrade))
                return false;

            if (item.Luck > (Settings.MaxLuck * -1) && RandomUtils.Next(20) == 0)
            {
                Luck--;
                item.Luck--;
                Enqueue(new S.RefreshItem { Item = item });
                ReceiveChat("你的武器被诅咒了.", ChatType.System);
            }
            else if (item.Luck <= 0 || RandomUtils.Next(8 * item.Luck) == 0)
            {
                Luck++;
                item.Luck++;
                Enqueue(new S.RefreshItem { Item = item });
                ReceiveChat("你的武器幸运增加了.", ChatType.Hint);
            }
            else
            {
                ReceiveChat("无效.", ChatType.Hint);
            }

            return true;
        }

        public void RequestUserName(ulong id)
        {
            CharacterInfo Character = Envir.GetCharacterInfo(id);
            if (Character != null)
            {
                Enqueue(new S.UserName { Id = Character.Index, Name = Character.Name });
            }
        }
        public void RequestChatItem(ulong id)
        {
            //Enqueue(new S.ChatItemStats { ChatItemId = id, Stats = whatever });
        }
        //查看玩家装备
        public void Inspect(uint id)
        {
            if (ObjectID == id) return;

            PlayerObject player = CurrentMap.Players.SingleOrDefault(x => x.ObjectID == id || x.Pets.Count(y => y.ObjectID == id && y is Monsters.HumanWizard) > 0);

            if (player == null) return;
            Inspect(player.Info.Index);
        }
        //查看玩家装备
        public void Inspect(ulong id)
        {
            //加入逻辑，如果当前地图不显示玩家名称，则不允许查看装备
            if (CurrentMap.Info.NoNames)
            {
                return;
            }
            if (ObjectID == id) return;
            CharacterInfo player = Envir.GetCharacterInfo(id);
            if (player == null) return;
            CharacterInfo Lover = null;
            string loverName = "";
            if (player.Married != 0) Lover = Envir.GetCharacterInfo(player.Married);

            if (Lover != null)
                loverName = Lover.Name;

            for (int i = 0; i < player.Equipment.Length; i++)
            {
                UserItem u = player.Equipment[i];
                if (u == null) continue;

                CheckItem(u);
            }
            string guildname = "";
            string guildrank = "";
            GuildObject Guild = null;
            Rank GuildRank = null;
            if (player.GuildIndex != -1)
            {
                Guild = Envir.GetGuild(player.GuildIndex);
                if (Guild != null)
                {
                    GuildRank = Guild.FindRank(player.Name);
                    if (GuildRank == null)
                        Guild = null;
                    else
                    {
                        guildname = Guild.Name;
                        guildrank = GuildRank.Name;
                    }
                }
            }
            Enqueue(new S.PlayerInspect
            {
                Name = player.Name,
                Equipment = player.Equipment,
                GuildName = guildname,
                GuildRank = guildrank,
                Hair = player.Hair,
                Gender = player.Gender,
                Class = player.Class,
                Level = player.Level,
                LoverName = loverName
            });
        }
        public void RemoveObjects(MirDirection dir, int count)
        {
            switch (dir)
            {
                case MirDirection.Up:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.UpRight:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Remove(this);
                            }
                        }
                    }

                    //Left Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange - count; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Right:
                    //Left Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.DownRight:
                    //Top Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Remove(this);
                            }
                        }
                    }

                    //Left Block
                    for (int a = -Globals.DataRange + count; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Down:
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                           // Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.DownLeft:
                    //Top Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Remove(this);
                            }
                        }
                    }

                    //Right Block
                    for (int a = -Globals.DataRange + count; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Left:
                    for (int a = -Globals.DataRange; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
                case MirDirection.UpLeft:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Remove(this);
                            }
                        }
                    }

                    //Right Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange - count; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Remove(this);
                            }
                        }
                    }
                    break;
            }
        }
        public void AddObjects(MirDirection dir, int count)
        {
            switch (dir)
            {
                case MirDirection.Up:
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.UpRight:
                    //Top Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Add(this);
                            }
                        }
                    }

                    //Right Block
                    for (int a = -Globals.DataRange + count; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Right:
                    for (int a = -Globals.DataRange; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.DownRight:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Add(this);
                            }
                        }
                    }

                    //Right Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange - count; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X + Globals.DataRange - b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Down:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.DownLeft:
                    //Bottom Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y + Globals.DataRange - a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Add(this);
                            }
                        }
                    }

                    //Left Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange - count; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.Left:
                    //Left Block
                    for (int a = -Globals.DataRange; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                           // Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
                case MirDirection.UpLeft:
                    //Top Block
                    for (int a = 0; a < count; a++)
                    {
                        int y = CurrentLocation.Y - Globals.DataRange + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = -Globals.DataRange; b <= Globals.DataRange; b++)
                        {
                            int x = CurrentLocation.X + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Add(this);
                            }
                        }
                    }

                    //Left Block
                    for (int a = -Globals.DataRange + count; a <= Globals.DataRange; a++)
                    {
                        int y = CurrentLocation.Y + a;
                        if (y < 0 || y >= CurrentMap.Height) continue;

                        for (int b = 0; b < count; b++)
                        {
                            int x = CurrentLocation.X - Globals.DataRange + b;
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                ob.Add(this);
                            }
                        }
                    }
                    break;
            }
        }
        public override void Remove(PlayerObject player)
        {
            if (player == this) return;

            base.Remove(player);
            Enqueue(new S.ObjectRemove { ObjectID = player.ObjectID });
        }
        public override void Add(PlayerObject player)
        {
            if (player == this) return;

            //base.Add(player);
            Enqueue(player.GetInfoEx(this));
            player.Enqueue(GetInfoEx(player));

            player.SendHealth(this);
            SendHealth(player);
        }
        public override void Remove(MonsterObject monster)
        {
            Enqueue(new S.ObjectRemove { ObjectID = monster.ObjectID });
        }
        public override void Add(MonsterObject monster)
        {
            Enqueue(monster.GetInfo());

            monster.SendHealth(this);
        }
        public override void SendHealth(PlayerObject player)
        {
            if (!player.IsMember(this) && Envir.Time > RevTime) return;
            byte time = Math.Min(byte.MaxValue, (byte)Math.Max(5, (RevTime - Envir.Time) / 1000));
            player.Enqueue(new S.ObjectHealth { ObjectID = ObjectID, HP = this.HP,MaxHP=this.MaxHP, Expire = time });
        }

        //这里是发送给客户端的文字消息，这个可以做拦截转意处理
        public override void ReceiveChat(string text, ChatType type)
        {
            Enqueue(new S.Chat { Message = text, Type = type });

            Report.ChatMessage(text);
        }

        public void ReceiveOutputMessage(string text, OutputMessageType type)
        {
            Enqueue(new S.SendOutputMessage { Message = text, Type = type });
        }

        private void CleanUp()
        {
            Connection.Player = null;
            Info.Player = null;
            Info.Mount = null;
            Connection = null;
            Account = null;
            Info = null;
        }

        public void Enqueue(Packet p)
        {
            if (Connection == null) return;
            Connection.Enqueue(p);
        }

        public void SpellToggle(Spell spell, bool use)
        {
            UserMagic magic;

            magic = GetMagic(spell);
            if (magic == null) return;

            int cost;
            switch (spell)
            {
                case Spell.Thrusting:
                    Info.Thrusting = use;
                    break;
                case Spell.HalfMoon:
                    Info.HalfMoon = use;
                    break;
                case Spell.CrossHalfMoon:
                    Info.CrossHalfMoon = use;
                    break;
                case Spell.DoubleSlash:
                    Info.DoubleSlash = use;
                    break;
                case Spell.TwinDrakeBlade:
                    if (TwinDrakeBlade) return;
                    magic = GetMagic(spell);
                    if (magic == null) return;
                    cost = magic.Info.BaseCost + magic.Level * magic.Info.LevelCost;
                    if (cost >= MP) return;

                    TwinDrakeBlade = true;
                    ChangeMP(-cost);

                    Enqueue(new S.ObjectMagic { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Spell = spell });
                    break;
                case Spell.FlamingSword://烈火，10秒一次
                    if (FlamingSword || Envir.Time < FlamingSwordTime) return;
                    magic = GetMagic(spell);
                    if (magic == null) return;
                    cost = magic.Info.BaseCost + magic.Level * magic.Info.LevelCost;
                    if (cost >= MP) return;

                    FlamingSword = true;
                    FlamingSwordTime = Envir.Time + 10000;
                    Enqueue(new S.SpellToggle { Spell = Spell.FlamingSword, CanUse = true });
                    ChangeMP(-cost);
                    break;
                case Spell.CounterAttack:
                    if (CounterAttack || Envir.Time < CounterAttackTime) return;
                    magic = GetMagic(spell);
                    if (magic == null) return;
                    cost = magic.Info.BaseCost + magic.Level * magic.Info.LevelCost;
                    if (cost >= MP) return;

                    CounterAttack = true;
                    CounterAttackTime = Envir.Time + 7000;
                    AddBuff(new Buff { Type = BuffType.CounterAttack, Caster = this, ExpireTime = CounterAttackTime, Values = new int[] { 11 + magic.Level * 3 }, Visible = true });
                    ChangeMP(-cost);
                    break;
                case Spell.MentalState:
                    Info.MentalState = (byte)((Info.MentalState + 1) % 3);
                    for (int i = 0; i < Buffs.Count; i++)
                    {
                        if (Buffs[i].Type == BuffType.MentalState)
                        {
                            Buffs[i].Values[0] = Info.MentalState;
                            S.AddBuff addBuff = new S.AddBuff { Type = Buffs[i].Type, Caster = Buffs[i].Caster.Name, Expire = Buffs[i].ExpireTime - Envir.Time, Values = Buffs[i].Values, Infinite = Buffs[i].Infinite, ObjectID = ObjectID, Visible = Buffs[i].Visible };
                            Enqueue(addBuff);
                            break;
                        }
                    }
                    break;
            }
        }

        private void UpdateGMBuff()
        {
            if (!IsGM) return;
            for (int i = 0; i < Buffs.Count; i++)
            {
                if (Buffs[i].Type == BuffType.GameMaster)
                {
                    GMOptions options = GMOptions.None;

                    if (GMGameMaster) options |= GMOptions.GameMaster;
                    if (GMNeverDie) options |= GMOptions.Superman;
                    if (Observer) options |= GMOptions.Observer;

                    Buffs[i].Values[0] = (byte)options;
                    Enqueue(new S.AddBuff { Type = Buffs[i].Type, Caster = Buffs[i].Caster.Name, Expire = Buffs[i].ExpireTime - Envir.Time, Values = Buffs[i].Values, Infinite = Buffs[i].Infinite, ObjectID = ObjectID, Visible = Buffs[i].Visible });
                    break;
                }
            }
        }

        #region NPC
        public void CallDefaultNPC(DefaultNPCType type, params object[] value)
        {
            string key = string.Empty;

            switch (type)
            {
                case DefaultNPCType.Login:
                    key = "Login";
                    break;
                case DefaultNPCType.UseItem:
                    if (value.Length < 1) return;
                    key = string.Format("UseItem({0})", value[0]);
                    break;
                case DefaultNPCType.Trigger:
                    if (value.Length < 1) return;
                    key = string.Format("Trigger({0})", value[0]);
                    break;
                case DefaultNPCType.MapCoord:
                    if (value.Length < 3) return;
                    key = string.Format("MapCoord({0},{1},{2})", value[0], value[1], value[2]);
                    break;
                case DefaultNPCType.MapEnter:
                    if (value.Length < 1) return;
                    key = string.Format("MapEnter({0})", value[0]);
                    break;
                case DefaultNPCType.Die:
                    key = "Die";
                    break;
                case DefaultNPCType.LevelUp:
                    key = "LevelUp";
                    break;
                case DefaultNPCType.CustomCommand:
                    if (value.Length < 1) return;
                    key = string.Format("CustomCommand({0})", value[0]);
                    break;
                case DefaultNPCType.OnAcceptQuest:
                    if (value.Length < 1) return;
                    key = string.Format("OnAcceptQuest({0})", value[0]);
                    break;
                case DefaultNPCType.OnFinishQuest:
                    if (value.Length < 1) return;
                    key = string.Format("OnFinishQuest({0})", value[0]);
                    break;
                case DefaultNPCType.Daily:
                    key = "Daily";
                    Info.NewDay = false;
                    break;
                case DefaultNPCType.TalkMonster:
                    if (value.Length < 1) return;
                    key = string.Format("TalkMonster({0})", value[0]);
                    break;
            }

            key = string.Format("[@_{0}]", key);

            DelayedAction action = new DelayedAction(DelayedType.NPC, SMain.Envir.Time + 0, DefaultNPC.ObjectID, key);
            ActionList.Add(action);

            Enqueue(new S.NPCUpdate { NPCID = DefaultNPC.ObjectID });
        }

        public void CallDefaultNPC(uint objectID, string key)
        {
            if (DefaultNPC == null) return;
            DefaultNPC.Call(this, key.ToUpper());
            CallNPCNextPage();
            return;
        }
        //玩家触发NPC
        public void CallNPC(uint objectID, string key)
        {
            if (Dead) return;

            for (int i = 0; i < CurrentMap.NPCs.Count; i++)
            {
                NPCObject ob = CurrentMap.NPCs[i];
                if (ob.ObjectID != objectID) continue;

                if (!Functions.InRange(ob.CurrentLocation, CurrentLocation, Globals.DataRange)) return;
                ob.CheckVisible(this);

                if (!ob.VisibleLog[Info.Index] || !ob.Visible) return;

                ob.Call(this, key.ToUpper());
                break;
            }

            CallNPCNextPage();
        }
        //直接触发下一页？
        private void CallNPCNextPage()
        {
            //process any new npc calls immediately
            for (int i = 0; i < ActionList.Count; i++)
            {
                if (ActionList[i].Type != DelayedType.NPC || ActionList[i].Time != -1) continue;
                var action = ActionList[i];

                ActionList.RemoveAt(i);

                CompleteNPC(action.Params);
            }
        }

       public void TalkMonster(uint objectID)
        {
            TalkingMonster talkMonster = FindObject(objectID, Globals.DataRange) as TalkingMonster;

            if (talkMonster == null) return;

            talkMonster.TalkingObjects.Add(this);

            CallDefaultNPC(DefaultNPCType.TalkMonster, talkMonster.Info.Name);
        }

        //购买物品
        public void BuyItem(ulong index, uint count, PanelType type)
        {
            if (Dead) return;

            if (NPCPage == null ||
                !(String.Equals(NPCPage.Key, NPCObject.BuySellKey, StringComparison.CurrentCultureIgnoreCase) ||
                String.Equals(NPCPage.Key, NPCObject.SecondBuyKey, StringComparison.CurrentCultureIgnoreCase) ||
                String.Equals(NPCPage.Key, NPCObject.BuyKey, StringComparison.CurrentCultureIgnoreCase) ||
                String.Equals(NPCPage.Key, NPCObject.BuyBackKey, StringComparison.CurrentCultureIgnoreCase) ||
                String.Equals(NPCPage.Key, NPCObject.BuyUsedKey, StringComparison.CurrentCultureIgnoreCase) ||
                String.Equals(NPCPage.Key, NPCObject.PearlBuyKey, StringComparison.CurrentCultureIgnoreCase)))
            {
                return;
            } 
                    

            for (int i = 0; i < CurrentMap.NPCs.Count; i++)
            {
                NPCObject ob = CurrentMap.NPCs[i];
                if (ob.ObjectID != NPCID) continue;

                if (type == PanelType.Buy)
                {
                    ob.Buy(this, index, count);
                }
            }
        }
        public void CraftItem(ulong index, uint count, int[] slots)
        {
            if (Dead) return;

            if (NPCPage == null) return;

            for (int i = 0; i < CurrentMap.NPCs.Count; i++)
            {
                NPCObject ob = CurrentMap.NPCs[i];
                if (ob.ObjectID != NPCID) continue;

                ob.Craft(this, index, count, slots);
            }
        }

        //卖物品
        public void SellItem(ulong uniqueID, uint count)
        {
            S.SellItem p = new S.SellItem { UniqueID = uniqueID, Count = count };
            if (Dead || count == 0)
            {
                Enqueue(p);
                return;
            }

            if (NPCPage == null || !(String.Equals(NPCPage.Key, NPCObject.BuySellKey, StringComparison.CurrentCultureIgnoreCase) || String.Equals(NPCPage.Key, NPCObject.SellKey, StringComparison.CurrentCultureIgnoreCase)))
            {
                Enqueue(p);
                return;
            }

            for (int n = 0; n < CurrentMap.NPCs.Count; n++)
            {
                NPCObject ob = CurrentMap.NPCs[n];
                if (ob.ObjectID != NPCID) continue;

                UserItem temp = null;
                int index = -1;

                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    temp = Info.Inventory[i];
                    if (temp == null || temp.UniqueID != uniqueID) continue;
                    index = i;
                    break;
                }

                if (temp == null || index == -1 || count > temp.Count)
                {
                    Enqueue(p);
                    return;
                }

                if (temp.Info.Bind.HasFlag(BindMode.DontSell))
                {
                    Enqueue(p);
                    return;
                }

                if (temp.RentalInformation != null && temp.RentalInformation.BindingFlags.HasFlag(BindMode.DontSell))
                {
                    Enqueue(p);
                    return;
                }

                if (ob.Types.Count != 0 && !ob.Types.Contains(temp.Info.Type))
                {
                    ReceiveChat("此处无法卖此物品"+ ob.Types.Count, ChatType.System);
                    ReceiveChat("此处无法卖此物品" + temp.Info.Type, ChatType.System);
                    Enqueue(p);
                    return;
                }

                if (temp.Info.StackSize > 1 && count != temp.Count)
                {
                    UserItem item = temp.Info.CreateFreshItem();
                    item.Count = count;

                    if (item.SellPrice() / 2 + Account.Gold > uint.MaxValue)
                    {
                        Enqueue(p);
                        return;
                    }

                    temp.Count -= count;
                    temp = item;
                }
                else Info.Inventory[index] = null;

                ob.Sell(this, temp);
                p.Success = true;
                Enqueue(p);
                GainGold(temp.SellPrice() / 2);
                RefreshBagWeight();

                return;
            }



            Enqueue(p);
        }
        public void RepairItem(ulong uniqueID, bool special = false)
        {
            Enqueue(new S.RepairItem { UniqueID = uniqueID });

            if (Dead) return;

            if (NPCPage == null || (!String.Equals(NPCPage.Key, NPCObject.RepairKey, StringComparison.CurrentCultureIgnoreCase) && !special) || (!String.Equals(NPCPage.Key, NPCObject.SRepairKey, StringComparison.CurrentCultureIgnoreCase) && special)) return;

            for (int n = 0; n < CurrentMap.NPCs.Count; n++)
            {
                NPCObject ob = CurrentMap.NPCs[n];
                if (ob.ObjectID != NPCID) continue;

                UserItem temp = null;
                int index = -1;

                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    temp = Info.Inventory[i];
                    if (temp == null || temp.UniqueID != uniqueID) continue;
                    index = i;
                    break;
                }

                if (temp == null || index == -1) return;

                if ((temp.Info.Bind.HasFlag(BindMode.DontRepair)) || (temp.Info.Bind.HasFlag(BindMode.NoSRepair) && special))
                {
                    ReceiveChat("无法修复此物品.", ChatType.System);
                    return;
                }

                if (ob.Types.Count != 0 && !ob.Types.Contains(temp.Info.Type))
                {
                    ReceiveChat("此处无法修复此物品.", ChatType.System);
                    return;
                }

                uint cost = (uint)(temp.RepairPrice() * ob.PriceRate(this));

                uint baseCost = (uint)(temp.RepairPrice() * ob.PriceRate(this, true));

                if (cost > Account.Gold) return;

                Account.Gold -= cost;
                Enqueue(new S.LoseGold { Gold = cost });
                if (ob.Conq != null) ob.Conq.GoldStorage += (cost - baseCost);

                if (!special) temp.MaxDura = (ushort)Math.Max(0, temp.MaxDura - (temp.MaxDura - temp.CurrentDura) / 30);

                temp.CurrentDura = temp.MaxDura;
                temp.DuraChanged = false;

                Enqueue(new S.ItemRepaired { UniqueID = uniqueID, MaxDura = temp.MaxDura, CurrentDura = temp.CurrentDura });
                return;
            }
        }
        public void SendStorage()
        {
            if (Connection.StorageSent) return;
            Connection.StorageSent = true;

            for (int i = 0; i < Account.Storage.Length; i++)
            {
                UserItem item = Account.Storage[i];
                if (item == null) continue;
                //CheckItemInfo(item.Info);
                CheckItem(item);
            }

            Enqueue(new S.UserStorage { Storage = Account.Storage }); // Should be no alter before being sent.
        }

        #endregion

        #region Consignment 集市，寄卖系统

        //寄卖物品
        public void ConsignItem(ulong uniqueID, uint GoldPrice,uint CreditPrice)
        {
            S.ConsignItem p = new S.ConsignItem { UniqueID = uniqueID };
            if(CreditPrice==0 && GoldPrice == 0)
            {
                p.msg = "寄卖物品价格不能为0";
                Enqueue(p);
                return;
            }
   
            if (Dead)
            {
                p.msg = "死亡不能寄卖物品";
                Enqueue(p);
                return;
            }

            if (NPCPage == null || (!String.Equals(NPCPage.Key, NPCObject.ConsignKey, StringComparison.CurrentCultureIgnoreCase))
                && (!String.Equals(NPCPage.Key, NPCObject.ConsignCreditKey, StringComparison.CurrentCultureIgnoreCase))
                && (!String.Equals(NPCPage.Key, NPCObject.ConsignDoulbeKey, StringComparison.CurrentCultureIgnoreCase))
                )
            {
                p.msg = "寄卖出错";
                Enqueue(p);
                return;
            }

            if (Account.Gold < Globals.ConsignmentCost)
            {
                p.msg = "金币不足，不能寄卖物品";
                Enqueue(p);
                return;
            }
            //寄售物品数限制
            S.NPCMarket _m = AuctionInfo.SearchPage(Info.Index,0, string.Empty, true, 0, (int)Globals.MaxConsignmentCount+5);
            if(_m.Listings!=null && _m.Listings.Count >= Globals.MaxConsignmentCount)
            {
                p.msg = "最多只能寄售"+Globals.MaxConsignmentCount+"件物品";
                Enqueue(p);
                return;
            }
           

            for (int n = 0; n < CurrentMap.NPCs.Count; n++)
            {
                NPCObject ob = CurrentMap.NPCs[n];
                if (ob.ObjectID != NPCID) continue;

                UserItem temp = null;
                int index = -1;

                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    temp = Info.Inventory[i];
                    if (temp == null || temp.UniqueID != uniqueID) continue;
                    index = i;
                    break;
                }

                if (temp == null || index == -1)
                {
                    p.msg = "你的背包中没有找到需要寄卖的物品";
                    Enqueue(p);
                    return;
                }

                if (temp.Info.Bind.HasFlag(BindMode.DontSell))
                {
                    p.msg = "此物品为非卖品";
                    Enqueue(p);
                    return;
                }

                if (temp.RentalInformation != null && temp.RentalInformation.BindingFlags.HasFlag(BindMode.DontSell))
                {
                    p.msg = "此物品为非卖品";
                    Enqueue(p);
                    return;
                }

                //Check Max Consignment.

                AuctionInfo auction = new AuctionInfo
                {
                    AuctionID = (ulong)UniqueKeyHelper.UniqueNext(),
                    CharacterIndex = Info.Index,
                    AccountIndex = Account.Index,
                    ConsignmentDate = Envir.Now,
                    Seller = Info.Name,
                    Item = temp,
                    GoldPrice = GoldPrice,
                    CreditPrice= CreditPrice
                };
                AuctionInfo.add(auction);

                p.Success = true;
                Enqueue(p);

                Report.ItemChanged("ConsignItem", temp, temp.Count, 1);

                Info.Inventory[index] = null;
                Account.Gold -= Globals.ConsignmentCost;
                Enqueue(new S.LoseGold { Gold = Globals.ConsignmentCost });
                RefreshBagWeight();
            }
            p.msg = "你离NPC太远";
            Enqueue(p);
        }

        //集市查询
        public void MarketSearch(string match,byte itemtype, int page=0)
        {
            if (Dead || Envir.Time < SearchTime) return;
            //定制市场，去掉这个
            //if (NPCPage == null || (!String.Equals(NPCPage.Key, NPCObject.MarketKey, StringComparison.CurrentCultureIgnoreCase) && !String.Equals(NPCPage.Key, NPCObject.ConsignmentsKey, StringComparison.CurrentCultureIgnoreCase))) return;
            SearchTime = Envir.Time + Globals.SearchDelay;
            MatchName = match;
            MatchType = itemtype;
            S.NPCMarket NPCMarket = AuctionInfo.SearchPage(Info.Index, itemtype, match, UserMatch, page,8);
            for (int i = 0; i < NPCMarket.Listings.Count; i++)
            {
                //CheckItemInfo(listings[i].Item.Info);
                CheckItem(NPCMarket.Listings[i].Item);
            }
            Enqueue(NPCMarket);
        }
        
        //市场买东西
        //这个是玩家市场
        public void MarketBuy(ulong auctionID,byte payType)
        {
            if (Dead)
            {
                Enqueue(new S.MarketFail { Reason = 0 });
                return;
            }
            //这个是干嘛哦，总是有问题哦,这个是市场定制的，不用判断脚本了,注释掉
            if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.MarketKey, StringComparison.CurrentCultureIgnoreCase))
            {
                //Enqueue(new S.MarketFail { Reason = 1 });
                //return;
            }

            for (int n = 0; n < CurrentMap.NPCs.Count; n++)
            {
                NPCObject ob = CurrentMap.NPCs[n];
                if (ob.ObjectID != NPCID) continue;
                AuctionInfo auction = AuctionInfo.get(auctionID);
                if (auction == null|| auction.isdel)
                {
                    Enqueue(new S.MarketFail { Reason = 9 });
                    return;
                }

                if (auction.Sold)
                {
                    Enqueue(new S.MarketFail { Reason = 2 });
                    return;
                }

                if (auction.Expired)
                {
                    Enqueue(new S.MarketFail { Reason = 3 });
                    return;
                }

                if (payType==0 && auction.GoldPrice > Account.Gold)
                {
                    Enqueue(new S.MarketFail { Reason = 4 });
                    return;
                }
                if (payType == 1 && auction.CreditPrice > Account.Credit)
                {
                    Enqueue(new S.MarketFail { Reason = 4 });
                    return;
                }

                if (!CanGainItem(auction.Item))
                {
                    Enqueue(new S.MarketFail { Reason = 5 });
                    return;
                }
                if (auction.AccountIndex==Account.Index)
                {
                    Enqueue(new S.MarketFail { Reason = 6 });
                    return;
                }



                //买家减钱
                auction.Sold = true;
                auction.isdel = true;//删除掉
                if (payType == 0)
                {
                    Account.Gold -= auction.GoldPrice;
                    Enqueue(new S.LoseGold { Gold = auction.GoldPrice });
                    //卖家加钱
                    Report.ItemChanged("BuyMarketItem", auction.Item, auction.Item.Count, 2);
                    uint gold = (uint)Math.Max(0, auction.GoldPrice - auction.GoldPrice * Globals.Commission);
                    AccountInfo Seller = Envir.GetAccount(auction.AccountIndex);
                    Seller.Gold += gold;
                    if (Seller != null)
                    {
                        Envir.MessageAccount(Seller, string.Format("你寄售的物品 {0} 已卖出，获得{1:#,##0} 金币(手续费：{2:#,##0})", auction.Item.FriendlyName, auction.GoldPrice, (uint)(auction.GoldPrice * Globals.Commission)), ChatType.Hint);
                    }
                    //刷新卖家的账户信息
                    if (Seller.Connection != null)
                    {
                        Seller.Connection.RefreshUserGold();
                    }
                    Enqueue(new S.MarketSuccess { Message = string.Format("你购买 {0} 花费 {1:#,##0} 金币", auction.Item.FriendlyName, auction.GoldPrice) });
                    MarketSearch(MatchName, MatchType);
                    GainItem(auction.Item);
                }
                if (payType == 1)
                {
                    Account.Credit -= auction.CreditPrice;
                    Enqueue(new S.LoseCredit { Credit = auction.CreditPrice });
                    //卖家加钱
                    Report.ItemChanged("BuyMarketItem", auction.Item, auction.Item.Count, 2);
                    uint gold = (uint)Math.Max(0, auction.CreditPrice - auction.CreditPrice * Globals.Commission);
                    AccountInfo Seller = Envir.GetAccount(auction.AccountIndex);
                    Seller.Credit += gold;
                    if (Seller != null)
                    {
                        Envir.MessageAccount(Seller, string.Format("你寄售的物品 {0} 已卖出，获得{1:#,##0} 元宝(手续费：{2:#,##0})", auction.Item.FriendlyName, auction.CreditPrice, (uint)(auction.CreditPrice * Globals.Commission)), ChatType.Hint);
                    }
                    //刷新卖家的账户信息
                    if (Seller.Connection != null)
                    {
                        Seller.Connection.RefreshUserGold();
                    }
                    Enqueue(new S.MarketSuccess { Message = string.Format("你购买 {0} 花费 {1:#,##0} 元宝", auction.Item.FriendlyName, auction.CreditPrice) });
                    MarketSearch(MatchName, MatchType);
                    GainItem(auction.Item);
                }
                return;
            }
            Enqueue(new S.MarketFail { Reason = 7 });
        }
        //集市下架物品
        public void MarketGetBack(ulong auctionID)
        {
            if (Dead)
            {
                Enqueue(new S.MarketFail { Reason = 0 });
                return;
            }
            //这个没用，注释掉
            if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.ConsignmentsKey, StringComparison.CurrentCultureIgnoreCase))
            {
                //Enqueue(new S.MarketFail { Reason = 1 });
                //return;
            }

            for (int n = 0; n < CurrentMap.NPCs.Count; n++)
            {
                NPCObject ob = CurrentMap.NPCs[n];
                if (ob.ObjectID != NPCID) continue;
                AuctionInfo auction = AuctionInfo.get(auctionID);
                if (auction == null)
                {
                    Enqueue(new S.MarketFail { Reason = 2 });
                    return;
                }
                if (!CanGainItem(auction.Item))
                {
                    Enqueue(new S.MarketFail { Reason = 5 });
                    return;
                }
                GainItem(auction.Item);
                auction.isdel = true;
                MarketSearch(MatchName, MatchType);
                return;
            }

            Enqueue(new S.MarketFail { Reason = 7 });
        }

        #endregion

        #region Awakening

        public void Awakening(ulong UniqueID, AwakeType type)
        {
            if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.AwakeningKey, StringComparison.CurrentCultureIgnoreCase))
                return;

            if (type == AwakeType.None) return;

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                UserItem item = Info.Inventory[i];
                if (item == null || item.UniqueID != UniqueID) continue;

                Awake awake = item.Awake;

                if (item.Info.Bind.HasFlag(BindMode.DontUpgrade))
                {
                    Enqueue(new S.Awakening { result = -1, removeID = -1 });
                    return;
                }

                if (item.RentalInformation != null && item.RentalInformation.BindingFlags.HasFlag(BindMode.DontUpgrade))
                {
                    Enqueue(new S.Awakening { result = -1, removeID = -1 });
                    return;
                }

                if (!item.Info.CanAwakening)
                {
                    Enqueue(new S.Awakening { result = -1, removeID = -1 });
                    return;
                }

                if (awake.IsMaxLevel())
                {
                    Enqueue(new S.Awakening { result = -2, removeID = -1 });
                    return;
                }

                if (Info.AccountInfo.Gold < item.AwakeningPrice())
                {
                    Enqueue(new S.Awakening { result = -3, removeID = -1 });
                    return;
                }

                if (HasAwakeningNeedMaterials(item, type))
                {
                    Info.AccountInfo.Gold -= item.AwakeningPrice();
                    Enqueue(new S.LoseGold { Gold = item.AwakeningPrice() });

                    bool[] isHit;

                    switch (awake.UpgradeAwake(item, type, out isHit))
                    {
                        case -1:
                            Enqueue(new S.Awakening { result = -1, removeID = -1 });
                            break;
                        case 0:
                            AwakeningEffect(false, isHit);
                            Info.Inventory[i] = null;
                            Enqueue(new S.Awakening { result = 0, removeID = (long)item.UniqueID });
                            break;
                        case 1:
                            Enqueue(new S.RefreshItem { Item = item });
                            AwakeningEffect(true, isHit);
                            Enqueue(new S.Awakening { result = 1, removeID = -1 });
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void DowngradeAwakening(ulong UniqueID)
        {
            if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.DowngradeKey, StringComparison.CurrentCultureIgnoreCase))
                return;

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                UserItem item = Info.Inventory[i];
                if (item != null)
                {
                    if (item.UniqueID == UniqueID)
                    {
                        if (item.RentalInformation != null)
                        {
                            ReceiveChat($"不能降级 {item.FriendlyName} 因为它属于 {item.RentalInformation.OwnerName}", ChatType.System);
                            return;
                        }

                        if (Info.AccountInfo.Gold >= item.DowngradePrice())
                        {
                            Info.AccountInfo.Gold -= item.DowngradePrice();
                            Enqueue(new S.LoseGold { Gold = item.DowngradePrice() });

                            Awake awake = item.Awake;
                            int result = awake.RemoveAwake();
                            switch (result)
                            {
                                case 0:
                                    ReceiveChat(string.Format("{0} :删除失败等级 0", item.Name), ChatType.System);
                                    break;
                                case 1:
                                    ushort maxDura = (RandomUtils.Next(20) == 0) ? (ushort)(item.MaxDura - 1000) : item.MaxDura;
                                    if (maxDura < 1000) maxDura = 1000;

                                    Info.Inventory[i].CurrentDura = (Info.Inventory[i].CurrentDura >= maxDura) ? maxDura : Info.Inventory[i].CurrentDura;
                                    Info.Inventory[i].MaxDura = maxDura;
                                    ReceiveChat(string.Format("{0} : 删除成功. 等级 {1}", item.Name, item.Awake.getAwakeLevel()), ChatType.System);
                                    Enqueue(new S.RefreshItem { Item = item });
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }

        //分解物品
        public void DisassembleItem(ulong UniqueID)
        {
            if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.DisassembleKey, StringComparison.CurrentCultureIgnoreCase))
                return;

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                UserItem item = Info.Inventory[i];

                if (item == null || item.UniqueID != UniqueID)
                    continue;

                if (item.Info.Bind.HasFlag(BindMode.UnableToDisassemble))
                {
                    ReceiveChat($"无法拆解 {item.FriendlyName}", ChatType.System);
                    return;
                }

                if (item.RentalInformation != null && item.RentalInformation.BindingFlags.HasFlag(BindMode.UnableToDisassemble))
                {
                    ReceiveChat($"无法拆解 {item.FriendlyName} 因为它属于 {item.RentalInformation.OwnerName}", ChatType.System);
                    return;
                }

                if (Info.AccountInfo.Gold >= item.DisassemblePrice())
                {
                    List<ItemInfo> dropList = new List<ItemInfo>();
                    foreach (DropInfo drop in Envir.AwakeningDrops)
                    {
                        foreach(ItemInfo dinfo in drop.ItemList)
                        {
                            if (dinfo.Grade == item.Info.Grade - 1 ||
                            dinfo.Grade == item.Info.Grade + 1)
                            {
                                if (drop.isDrop())
                                {
                                    dropList.Add(dinfo);
                                }
                            }

                            if (dinfo.Grade == item.Info.Grade)
                            {
                                dropList.Add(dinfo);
                            }
                        }
                    }

                    if (dropList.Count == 0) continue;

                    UserItem gainItem = dropList[RandomUtils.Next(dropList.Count)].CreateDropItem();
                    if (gainItem == null) continue;
                    gainItem.Count = (uint)RandomUtils.Next((int)((((int)item.Info.Grade * item.Info.RequiredAmount) / 10) + item.AddedVue+1));
                    if (gainItem.Count < 1) gainItem.Count = 1;

                    GainItem(gainItem);

                    Enqueue(new S.LoseGold { Gold = item.DisassemblePrice() });
                    Info.AccountInfo.Gold -= item.DisassemblePrice();

                    Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                    Info.Inventory[i] = null;
                }
            }
        }

        public void ResetAddedItem(ulong UniqueID)
        {
            if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.ResetKey, StringComparison.CurrentCultureIgnoreCase))
                return;

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                UserItem item = Info.Inventory[i];
                if (item != null)
                {
                    if (item.UniqueID == UniqueID)
                    {
                        if (item.RentalInformation != null)
                        {
                            ReceiveChat($"无法重置 {item.FriendlyName} 因为它属于 {item.RentalInformation.OwnerName}", ChatType.System);
                            return;
                        }

                        if (Info.AccountInfo.Gold >= item.ResetPrice())
                        {
                            Info.AccountInfo.Gold -= item.ResetPrice();
                            Enqueue(new S.LoseGold { Gold = item.ResetPrice() });

                            UserItem newItem = new UserItem(item.Info);

                            ushort maxDura = (RandomUtils.Next(20) == 0) ? (ushort)(item.MaxDura - 1000) : item.MaxDura;
                            if (maxDura < 1000) maxDura = 1000;

                            newItem.UniqueID = item.UniqueID;
                            newItem.ItemIndex = item.ItemIndex;
                            newItem.CurrentDura = (item.CurrentDura >= maxDura) ? maxDura : item.CurrentDura;
                            newItem.MaxDura = maxDura;
                            newItem.Count = item.Count;
                            newItem.Slots = item.Slots;
                            newItem.Awake = item.Awake;

                            Info.Inventory[i] = newItem;

                            Enqueue(new S.RefreshItem { Item = Info.Inventory[i] });
                        }
                    }
                }
            }
        }

        public void AwakeningNeedMaterials(ulong UniqueID, AwakeType type)
        {
            if (type == AwakeType.None) return;

            foreach (UserItem item in Info.Inventory)
            {
                if (item != null)
                {
                    if (item.UniqueID == UniqueID)
                    {
                        Awake awake = item.Awake;

                        byte[] materialCount = new byte[2];
                        int idx = 0;
                        foreach (List<byte> material in Awake.AwakeMaterials[(int)type - 1])
                        {
                            byte materialRate = (byte)(Awake.AwakeMaterialRate[(int)item.Info.Grade - 1] * (float)awake.getAwakeLevel());
                            materialCount[idx] = material[(int)item.Info.Grade - 1];
                            materialCount[idx] += materialRate;
                            idx++;
                        }

                        ItemInfo[] materials = new ItemInfo[2];

                        foreach (ItemInfo info in Envir.ItemInfoList)
                        {
                            if (item.Info.Grade == info.Grade &&
                                info.Type == ItemType.Awakening)
                            {
                                if (info.Shape == (short)type - 1)
                                {
                                    materials[0] = info;
                                }
                                else if (info.Shape == 100)
                                {
                                    materials[1] = info;
                                }
                            }
                        }

                        Enqueue(new S.AwakeningNeedMaterials { Materials = materials, MaterialsCount = materialCount });
                        break;
                    }
                }
            }
        }

        public void AwakeningEffect(bool isSuccess, bool[] isHit)
        {
            for (int i = 0; i < 5; i++)
            {
                Enqueue(new S.ObjectEffect { ObjectID = ObjectID, Effect = isHit[i] ? SpellEffect.AwakeningHit : SpellEffect.AwakeningMiss, EffectType = 0, DelayTime = (uint)(i * 500) });
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = isHit[i] ? SpellEffect.AwakeningHit : SpellEffect.AwakeningMiss, EffectType = 0, DelayTime = (uint)(i * 500) });
            }

            Enqueue(new S.ObjectEffect { ObjectID = ObjectID, Effect = isSuccess ? SpellEffect.AwakeningSuccess : SpellEffect.AwakeningFail, EffectType = 0, DelayTime = 2500 });
            Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = isSuccess ? SpellEffect.AwakeningSuccess : SpellEffect.AwakeningFail, EffectType = 0, DelayTime = 2500 });
        }

        public bool HasAwakeningNeedMaterials(UserItem item, AwakeType type)
        {
            Awake awake = item.Awake;

            byte[] materialCount = new byte[2];

            int idx = 0;
            foreach (List<byte> material in Awake.AwakeMaterials[(int)type - 1])
            {
                byte materialRate = (byte)(Awake.AwakeMaterialRate[(int)item.Info.Grade - 1] * (float)awake.getAwakeLevel());
                materialCount[idx] = material[(int)item.Info.Grade - 1];
                materialCount[idx] += materialRate;
                idx++;
            }

            byte[] currentCount = new byte[2] { 0, 0 };

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                UserItem materialItem = Info.Inventory[i];
                if (materialItem != null)
                {
                    if (materialItem.Info.Grade == item.Info.Grade &&
                        materialItem.Info.Type == ItemType.Awakening)
                    {
                        if (materialItem.Info.Shape == ((int)type - 1) &&
                            materialCount[0] - currentCount[0] != 0)
                        {
                            if (materialItem.Count <= materialCount[0] - currentCount[0])
                            {
                                currentCount[0] += (byte)materialItem.Count;
                            }
                            else if (materialItem.Count > materialCount[0] - currentCount[0])
                            {
                                currentCount[0] = (byte)(materialCount[0] - currentCount[0]);
                            }
                        }
                        else if (materialItem.Info.Shape == 100 &&
                            materialCount[1] - currentCount[1] != 0)
                        {
                            if (materialItem.Count <= materialCount[1] - currentCount[1])
                            {
                                currentCount[1] += (byte)materialItem.Count;
                            }
                            else if (materialItem.Count > materialCount[1] - currentCount[1])
                            {
                                currentCount[1] = (byte)(materialCount[1] - currentCount[1]);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < materialCount.Length; i++)
            {
                if (materialCount[i] != currentCount[i])
                {
                    Enqueue(new S.Awakening { result = -4, removeID = -1 });
                    return false;
                }
            }

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                if (Info.Inventory[i] != null)
                {
                    if (Info.Inventory[i].Info.Grade == item.Info.Grade &&
                        Info.Inventory[i].Info.Type == ItemType.Awakening)
                    {
                        if (Info.Inventory[i].Info.Shape == ((int)type - 1) &&
                            currentCount[0] > 0)
                        {
                            if (Info.Inventory[i].Count <= currentCount[0])
                            {
                                Enqueue(new S.DeleteItem { UniqueID = Info.Inventory[i].UniqueID, Count = Info.Inventory[i].Count });
                                currentCount[0] -= (byte)Info.Inventory[i].Count;
                                Info.Inventory[i] = null;
                            }
                            else if (Info.Inventory[i].Count > currentCount[0])
                            {
                                Enqueue(new S.DeleteItem { UniqueID = Info.Inventory[i].UniqueID, Count = (uint)currentCount[0] });
                                Info.Inventory[i].Count -= currentCount[0];
                                currentCount[0] = 0;
                            }
                        }
                        else if (Info.Inventory[i].Info.Shape == 100 &&
                            currentCount[1] > 0)
                        {
                            if (Info.Inventory[i].Count <= currentCount[1])
                            {
                                Enqueue(new S.DeleteItem { UniqueID = Info.Inventory[i].UniqueID, Count = Info.Inventory[i].Count });
                                currentCount[1] -= (byte)Info.Inventory[i].Count;
                                Info.Inventory[i] = null;
                            }
                            else if (Info.Inventory[i].Count > currentCount[1])
                            {
                                Enqueue(new S.DeleteItem { UniqueID = Info.Inventory[i].UniqueID, Count = (uint)currentCount[1] });
                                Info.Inventory[i].Count -= currentCount[1];
                                currentCount[1] = 0;
                            }
                        }
                    }
                }
            }
            return true;
        }

        #endregion

        #region Groups
        //允许组队开关
        public void SwitchGroup(bool allow)
        {
            Enqueue(new S.SwitchGroup { AllowGroup = allow });

            if (AllowGroup == allow) return;
            AllowGroup = allow;

            if (AllowGroup || GroupMembers == null) return;

            RemoveGroupBuff();

            GroupMembers.Remove(this);
            Enqueue(new S.DeleteGroup());

            if (GroupMembers.Count > 1)
            {
                Packet p = new S.DeleteMember { Name = Name };

                for (int i = 0; i < GroupMembers.Count; i++)
                    GroupMembers[i].Enqueue(p);
            }
            else
            {
                GroupMembers[0].Enqueue(new S.DeleteGroup());
                GroupMembers[0].GroupMembers = null;
            }
            GroupMembers = null;
        }

        public void RemoveGroupBuff()
        {
            for (int i = 0; i < Buffs.Count; i++)
            {
                Buff buff = Buffs[i];

                if (buff.Type == BuffType.RelationshipEXP)
                {
                    CharacterInfo Lover = Envir.GetCharacterInfo(Info.Married);

                    if (Lover == null) continue;

                    PlayerObject LoverP = Envir.GetPlayer(Lover.Name);

                    RemoveBuff(BuffType.RelationshipEXP);

                    if (LoverP != null)
                    {
                        LoverP.RemoveBuff(BuffType.RelationshipEXP);
                    }
                }
                else if (buff.Type == BuffType.Mentee || buff.Type == BuffType.Mentor)
                {
                    CharacterInfo Mentor = Envir.GetCharacterInfo(Info.Mentor);

                    if (Mentor == null) continue;

                    PlayerObject MentorP = Envir.GetPlayer(Mentor.Name);

                    RemoveBuff(buff.Type);

                    if (MentorP != null)
                    {
                        MentorP.RemoveBuff(buff.Type == BuffType.Mentee ? BuffType.Mentor : BuffType.Mentee);
                    }
                }
            }
        }
        public void AddMember(string name)
        {
            if (GroupMembers != null && GroupMembers[0] != this)
            {
                ReceiveChat("你不是组长.", ChatType.System);
                return;
            }

            if (GroupMembers != null && GroupMembers.Count >= Globals.MaxGroup)
            {
                ReceiveChat("您的组已经拥有成员的最大数量.", ChatType.System);
                return;
            }

            PlayerObject player = Envir.GetPlayer(name);

            if (player == null)
            {
                ReceiveChat(name + " 找不到.", ChatType.System);
                return;
            }
            if (player == this)
            {
                ReceiveChat("无法添加自己.", ChatType.System);
                return;
            }

            if (!player.AllowGroup)
            {
                ReceiveChat(name + " 不允许组队.可输入@允许组队 命令开启", ChatType.System);
                return;
            }

            if (player.GroupMembers != null)
            {
                ReceiveChat(name + " 已经在另一组队.", ChatType.System);
                return;
            }

            if (player.GroupInvitation != null)
            {
                ReceiveChat(name + " 已经接收到来自另一个玩家的邀请.", ChatType.System);
                return;
            }

            SwitchGroup(true);
            player.Enqueue(new S.GroupInvite { Name = Name });
            player.GroupInvitation = this;

        }
        public void DelMember(string name)
        {
            if (GroupMembers == null)
            {
                ReceiveChat("你在组队里.", ChatType.System);
                return;
            }
            if (GroupMembers[0] != this)
            {
                ReceiveChat("你不是组长.", ChatType.System);
                return;
            }

            PlayerObject player = null;

            for (int i = 0; i < GroupMembers.Count; i++)
            {
                if (String.Compare(GroupMembers[i].Name, name, StringComparison.OrdinalIgnoreCase) != 0) continue;
                player = GroupMembers[i];
                break;
            }


            if (player == null)
            {
                ReceiveChat(name + " 不在你的小组里.", ChatType.System);
                return;
            }

            player.RemoveGroupBuff();

            GroupMembers.Remove(player);
            player.Enqueue(new S.DeleteGroup());

            if (GroupMembers.Count > 1)
            {
                Packet p = new S.DeleteMember { Name = player.Name };

                for (int i = 0; i < GroupMembers.Count; i++)
                    GroupMembers[i].Enqueue(p);
            }
            else
            {
                GroupMembers[0].Enqueue(new S.DeleteGroup());
                GroupMembers[0].GroupMembers = null;
            }
            player.GroupMembers = null;
        }
        public void GroupInvite(bool accept)
        {
            if (GroupInvitation == null)
            {
                ReceiveChat("你没有被邀请到一个组队.", ChatType.System);
                return;
            }

            if (!accept)
            {
                GroupInvitation.ReceiveChat(Name + " 谢绝组队邀请.", ChatType.System);
                GroupInvitation = null;
                return;
            }

            if (GroupMembers != null)
            {
                ReceiveChat(string.Format("不能再加入{0} 分组", GroupInvitation.Name), ChatType.System);
                GroupInvitation = null;
                return;
            }

            if (GroupInvitation.GroupMembers != null && GroupInvitation.GroupMembers[0] != GroupInvitation)
            {
                ReceiveChat(GroupInvitation.Name + " 不再是组长.", ChatType.System);
                GroupInvitation = null;
                return;
            }

            if (GroupInvitation.GroupMembers != null && GroupInvitation.GroupMembers.Count >= Globals.MaxGroup)
            {
                ReceiveChat(GroupInvitation.Name + " 组队已经拥有成员的最大数目.", ChatType.System);
                GroupInvitation = null;
                return;
            }
            if (!GroupInvitation.AllowGroup)
            {
                ReceiveChat(GroupInvitation.Name + " 不允许组队.", ChatType.System);
                GroupInvitation = null;
                return;
            }
            if (GroupInvitation.Node == null)
            {
                ReceiveChat(GroupInvitation.Name + " 不再在线.", ChatType.System);
                GroupInvitation = null;
                return;
            }

            if (GroupInvitation.GroupMembers == null)
            {
                GroupInvitation.GroupMembers = new List<PlayerObject> { GroupInvitation };
                GroupInvitation.Enqueue(new S.AddMember { Name = GroupInvitation.Name });
            }

            Packet p = new S.AddMember { Name = Name };
            GroupMembers = GroupInvitation.GroupMembers;
            GroupInvitation = null;

            for (int i = 0; i < GroupMembers.Count; i++)
            {
                PlayerObject member = GroupMembers[i];

                member.Enqueue(p);
                Enqueue(new S.AddMember { Name = member.Name });

                if (CurrentMap != member.CurrentMap || !Functions.InRange(CurrentLocation, member.CurrentLocation, Globals.DataRange)) continue;

                byte time = Math.Min(byte.MaxValue, (byte)Math.Max(5, (RevTime - Envir.Time) / 1000));

                member.Enqueue(new S.ObjectHealth { ObjectID = ObjectID, HP = this.HP, MaxHP = this.MaxHP, Expire = time });
                Enqueue(new S.ObjectHealth { ObjectID = member.ObjectID, HP = this.HP, MaxHP = this.MaxHP, Expire = time });

                for (int j = 0; j < member.Pets.Count; j++)
                {
                    MonsterObject pet = member.Pets[j];

                    Enqueue(new S.ObjectHealth { ObjectID = pet.ObjectID, HP = this.HP, MaxHP = this.MaxHP, Expire = time });
                }
            }

            GroupMembers.Add(this);

            //Adding Buff on for marriage
            if (GroupMembers != null)
            for (int i = 0; i < GroupMembers.Count; i++)
            {
                PlayerObject player = GroupMembers[i];
                    if (Info.Married == player.Info.Index)
                    {
                        AddBuff(new Buff { Type = BuffType.RelationshipEXP, Caster = player, ExpireTime = Envir.Time * 1000, Infinite = true, Values = new int[] { Settings.LoverEXPBonus } });
                        player.AddBuff(new Buff { Type = BuffType.RelationshipEXP, Caster = this, ExpireTime = Envir.Time * 1000, Infinite = true, Values = new int[] { Settings.LoverEXPBonus } });
                    }
                    if (Info.Mentor == player.Info.Index)
                    {
                        if (Info.isMentor)
                        {
                            player.AddBuff(new Buff { Type = BuffType.Mentee, Caster = player, ExpireTime = Envir.Time * 1000, Infinite = true, Values = new int[] { Settings.MentorExpBoost } });
                            AddBuff(new Buff { Type = BuffType.Mentor, Caster = this, ExpireTime = Envir.Time * 1000, Infinite = true, Values = new int[] { Settings.MentorDamageBoost } });
                        }
                        else
                        {
                            AddBuff(new Buff { Type = BuffType.Mentee, Caster = player, ExpireTime = Envir.Time * 1000, Infinite = true, Values = new int[] { Settings.MentorExpBoost } });
                            player.AddBuff(new Buff { Type = BuffType.Mentor, Caster = this, ExpireTime = Envir.Time * 1000, Infinite = true, Values = new int[] { Settings.MentorDamageBoost } });
                        }
                    }
            }

            

            for (int j = 0; j < Pets.Count; j++)
                Pets[j].BroadcastHealthChange();

            Enqueue(p);
        }

        #endregion

        #region Guilds

        public void CreateNewbieGuild(string GuildName)
        {
            if (Envir.GetGuild(GuildName) != null) return;
            //make the guild
            GuildObject guild = new GuildObject(this, GuildName) { Guildindex = UniqueKeyHelper.UniqueNext() };
            guild.Ranks[0].Members.Clear();
            guild.Membercount--;
            Envir.GuildList.Add(guild);
        }
        public bool CreateGuild(string GuildName)
        {
            if ((MyGuild != null) || (Info.GuildIndex != -1)) return false;
            if (Envir.GetGuild(GuildName) != null) return false;
            if (Info.Level < Settings.Guild_RequiredLevel)
            {
                ReceiveChat(String.Format("您的级别不够高，不能创建一个公会: {0}", Settings.Guild_RequiredLevel), ChatType.System);
                return false;
            }
            //check if we have the required items
            for (int i = 0; i < Settings.Guild_CreationCostList.Count; i++)
            {
                ItemVolume Required = Settings.Guild_CreationCostList[i];
                if (Required.Item == null)
                {
                    if (Info.AccountInfo.Gold < Required.Amount)
                    {
                        ReceiveChat(String.Format("金币不足. 创建公会要求 {0} 金币.", Required.Amount), ChatType.System);
                        return false;
                    }
                }
                else
                {
                    uint count = Required.Amount;
                    foreach (var item in Info.Inventory.Where(item => item != null && item.Info == Required.Item))
                    {
                        if ((Required.Item.Type == ItemType.Ore) && (item.CurrentDura / 1000 > Required.Amount))
                        {
                            count = 0;
                            break;
                        }
                        if (item.Count > count)
                            count = 0;
                        else
                            count = count - item.Count;
                        if (count == 0) break;
                    }
                    if (count != 0)
                    {
                        if (Required.Amount == 1)
                            ReceiveChat(String.Format("创建公会需要 {0} .", Required.Item.Name), ChatType.System);
                        else
                        {
                            if (Required.Item.Type == ItemType.Ore)
                                ReceiveChat(string.Format("{0} 纯度为 {1} 才能建立公会.", Required.Item.Name, Required.Amount / 1000), ChatType.System);
                            else
                                ReceiveChat(string.Format("{0}不足, 你需要{1}才能建立公会.", Required.Item.Name, Required.Amount), ChatType.System);
                        }
                        return false;
                    }
                }
            }
            //take the required items
            for (int i = 0; i < Settings.Guild_CreationCostList.Count; i++)
            {
                ItemVolume Required = Settings.Guild_CreationCostList[i];
                if (Required.Item == null)
                {
                    if (Info.AccountInfo.Gold >= Required.Amount)
                    {
                        Info.AccountInfo.Gold -= Required.Amount;
                        Enqueue(new S.LoseGold { Gold = Required.Amount });
                    }
                }
                else
                {
                    uint count = Required.Amount;
                    for (int o = 0; o < Info.Inventory.Length; o++)
                    {
                        UserItem item = Info.Inventory[o];
                        if (item == null) continue;
                        if (item.Info != Required.Item) continue;

                        if ((Required.Item.Type == ItemType.Ore) && (item.CurrentDura / 1000 > Required.Amount))
                        {
                            Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                            Info.Inventory[o] = null;
                            break;
                        }
                        if (count > item.Count)
                        {
                            Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                            Info.Inventory[o] = null;
                            count -= item.Count;
                            continue;
                        }

                        Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = count });
                        if (count == item.Count)
                            Info.Inventory[o] = null;
                        else
                            item.Count -= count;
                        break;
                    }
                }
            }
            RefreshStats();
            //make the guild
            GuildObject guild = new GuildObject(this, GuildName) { Guildindex = UniqueKeyHelper.UniqueNext() };
            Envir.GuildList.Add(guild);

            Info.GuildIndex = guild.Guildindex;
            SMain.Enqueue("新建行会:"+ Info.GuildIndex);
            MyGuild = guild;
            MyGuildRank = guild.FindRank(Name);
            GuildMembersChanged = true;
            GuildNoticeChanged = true;
            GuildCanRequestItems = true;
            //tell us we now have a guild
            BroadcastInfo();
            MyGuild.SendGuildStatus(this);
            return true;
        }
        public void EditGuildMember(string Name, string RankName, byte RankIndex, byte ChangeType)
        {
            if ((MyGuild == null) || (MyGuildRank == null))
            {
                ReceiveChat("你不在公会里!", ChatType.System);
                return;
            }
            switch (ChangeType)
            {
                case 0: //add member
                    if (!MyGuildRank.Options.HasFlag(RankOptions.CanRecruit))
                    {
                        ReceiveChat("不允许你招募新成员!", ChatType.System);
                        return;
                    }
                    if (Name == "") return;
                    PlayerObject player = Envir.GetPlayer(Name);
                    if (player == null)
                    {
                        ReceiveChat(String.Format("{0} 不在线!", Name), ChatType.System);
                        return;
                    }
                    if ((player.MyGuild != null) || (player.MyGuildRank != null) || (player.Info.GuildIndex != -1))
                    {
                        ReceiveChat(String.Format("{0} 已经在公会里!", Name), ChatType.System);
                        return;
                    }
                    if (!player.EnableGuildInvite)
                    {
                        ReceiveChat(String.Format("{0} 禁用公会邀请!对方可输入@允许加入公会 命令开启", Name), ChatType.System);
                        return;
                    }
                    if (player.PendingGuildInvite != null)
                    {
                        ReceiveChat(string.Format("{0} 已经有公会邀请待审.", Name), ChatType.System);
                        return;
                    }

                    if (MyGuild.IsAtWar())
                    {
                        ReceiveChat("战时不能招收成员.", ChatType.System);
                        return;
                    }

                    player.Enqueue(new S.GuildInvite { Name = MyGuild.Name });
                    player.PendingGuildInvite = MyGuild;
                    break;
                case 1: //delete member
                    if (!MyGuildRank.Options.HasFlag(RankOptions.CanKick))
                    {
                        ReceiveChat("不允许删除成员!", ChatType.System);
                        return;
                    }
                    if (Name == "") return;

                    if (!MyGuild.DeleteMember(this, Name))
                    {
                        return;
                    }
                    break;
                case 2: //promote member (and it'll auto create a new rank at bottom if the index > total ranks!)
                    if (!MyGuildRank.Options.HasFlag(RankOptions.CanChangeRank))
                    {
                        ReceiveChat("不允许更改其他成员级别!", ChatType.System);
                        return;
                    }
                    if (Name == "") return;
                    MyGuild.ChangeRank(this, Name, RankIndex, RankName);
                    break;
                case 3: //change rank name
                    if (!MyGuildRank.Options.HasFlag(RankOptions.CanChangeRank))
                    {
                        ReceiveChat("不允许你变换等级!", ChatType.System);
                        return;
                    }
                    if ((RankName == "") || (RankName.Length < 3))
                    {
                        ReceiveChat("名称过短!", ChatType.System);
                        return;
                    }
                    if (RankName.Contains("\\") || RankName.Length > 20)
                    {
                        return;
                    }
                    if (!MyGuild.ChangeRankName(this, RankName, RankIndex))
                        return;
                    break;
                case 4: //new rank
                    if (!MyGuildRank.Options.HasFlag(RankOptions.CanChangeRank))
                    {
                        ReceiveChat("不允许你变换等级!", ChatType.System);
                        return;
                    }
                    if (MyGuild.Ranks.Count > 254)
                    {
                        ReceiveChat("职称超过最大限制.", ChatType.System);
                        return;
                    }
                    MyGuild.NewRank(this);
                    break;
                case 5: //change rank setting
                    if (!MyGuildRank.Options.HasFlag(RankOptions.CanChangeRank))
                    {
                        ReceiveChat("不允许你变换等级!", ChatType.System);
                        return;
                    }
                    int temp;

                    if (!int.TryParse(RankName, out temp))
                    {
                        return;
                    }
                    MyGuild.ChangeRankOption(this, RankIndex, temp, Name);
                    break;
            }
        }
        public void EditGuildNotice(List<string> notice)
        {
            if ((MyGuild == null) || (MyGuildRank == null))
            {
                ReceiveChat("你不在公会里!", ChatType.System);
                return;
            }
            if (!MyGuildRank.Options.HasFlag(RankOptions.CanChangeNotice))
            {

                ReceiveChat("不允许更改公会通知!", ChatType.System);
                return;
            }
            if (notice.Count > 200)
            {
                ReceiveChat("公会公告不能超过200行!", ChatType.System);
                return;
            }
            MyGuild.NewNotice(notice);
        }
        public void GuildInvite(bool accept)
        {
            if (PendingGuildInvite == null)
            {
                ReceiveChat("你没有被邀请去公会.", ChatType.System);
                return;
            }
            if (!accept) return;
            if (!PendingGuildInvite.HasRoom())
            {
                ReceiveChat(String.Format("{0} 已满.", PendingGuildInvite.Name), ChatType.System);
                return;
            }
            PendingGuildInvite.NewMember(this);
            Info.GuildIndex = PendingGuildInvite.Guildindex;
            MyGuild = PendingGuildInvite;
            MyGuildRank = PendingGuildInvite.FindRank(Name);
            GuildMembersChanged = true;
            GuildNoticeChanged = true;
            //tell us we now have a guild
            BroadcastInfo();
            MyGuild.SendGuildStatus(this);
            PendingGuildInvite = null;
            EnableGuildInvite = false;
            GuildCanRequestItems = true;
            //refresh guildbuffs
            RefreshStats();
            if (MyGuild.BuffList.Count > 0)
                Enqueue(new S.GuildBuffList() { ActiveBuffs = MyGuild.BuffList});
        }
        public void RequestGuildInfo(byte Type)
        {
            if (MyGuild == null) return;
            if (MyGuildRank == null) return;
            switch (Type)
            {
                case 0://notice
                    if (GuildNoticeChanged)
                        Enqueue(new S.GuildNoticeChange() { notice = MyGuild.Notice });
                    GuildNoticeChanged = false;
                    break;
                case 1://memberlist
                    if (GuildMembersChanged)
                        Enqueue(new S.GuildMemberChange() { Status = 255, Ranks = MyGuild.Ranks });
                    break;
            }
        }
        public void GuildNameReturn(string Name)
        {
            if (Name == "") CanCreateGuild = false;
            if (!CanCreateGuild) return;
            if ((Name.Length < 3) || (Name.Length > 20))
            {
                ReceiveChat("公会名称太长.", ChatType.System);
                CanCreateGuild = false;
                return;
            }
            if (Name.Contains('\\'))
            {
                CanCreateGuild = false;
                return;
            }
            if (MyGuild != null)
            {
                ReceiveChat("你已经是公会的一份子了.", ChatType.System);
                CanCreateGuild = false;
                return;
            }
            GuildObject guild = Envir.GetGuild(Name);
            if (guild != null)
            {
                ReceiveChat(string.Format("公会 {0} 已存在.", Name), ChatType.System);
                CanCreateGuild = false;
                return;
            }

            CreateGuild(Name);
            CanCreateGuild = false;
        }
        public void GuildStorageGoldChange(Byte Type, uint Amount)
        {
            if ((MyGuild == null) || (MyGuildRank == null))
            {
                ReceiveChat("你不是公会的一份子.", ChatType.System);
                return;
            }

            if (!InSafeZone)
            {
                ReceiveChat("不能在安全区域外使用公会存储.", ChatType.System);
                return;
            }

            if (Type == 0)//donate
            {
                if (Account.Gold < Amount)
                {
                    ReceiveChat("金币不足.", ChatType.System);
                    return;
                }
                if ((MyGuild.Gold + (UInt64)Amount) > uint.MaxValue)
                {
                    ReceiveChat("公会金币达到上限.", ChatType.System);
                    return;
                }
                Account.Gold -= Amount;
                MyGuild.Gold += Amount;
                Enqueue(new S.LoseGold { Gold = Amount });
                MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 0, Name = Info.Name, Amount = Amount });
                MyGuild.NeedSave = true;
            }
            else
            {
                if (MyGuild.Gold < Amount)
                {
                    ReceiveChat("金币不足.", ChatType.System);
                    return;
                }
                if (!CanGainGold(Amount))
                {
                    ReceiveChat("金币达到上限.", ChatType.System);
                    return;
                }
                if (MyGuildRank.Index != 0)
                {
                    ReceiveChat("排名不足.", ChatType.System);
                    return;
                }

                MyGuild.Gold -= Amount;
                GainGold(Amount);
                MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 1, Name = Info.Name, Amount = Amount });
                MyGuild.NeedSave = true;
            }
        }
        public void GuildStorageItemChange(Byte Type, int from, int to)
        {
            S.GuildStorageItemChange p = new S.GuildStorageItemChange { Type = (byte)(3 + Type), From = from, To = to };
            if ((MyGuild == null) || (MyGuildRank == null))
            {
                Enqueue(p);
                ReceiveChat("你不是公会的一份子.", ChatType.System);
                return;
            }

            if (!InSafeZone && Type != 3)
            {
                Enqueue(p);
                ReceiveChat("不能在安全区域外使用公会存储.", ChatType.System);
                return;
            }

            switch (Type)
            {
                case 0://store
                    if (!MyGuildRank.Options.HasFlag(RankOptions.CanStoreItem))
                    {
                        Enqueue(p);
                        ReceiveChat("您没有在公会存储中存储物品的权限.", ChatType.System);
                        return;
                    }
                    if (from < 0 || from >= Info.Inventory.Length)
                    {
                        Enqueue(p);
                        return;
                    }
                    if (to < 0 || to >= MyGuild.StoredItems.Length)
                    {
                        Enqueue(p);
                        return;
                    }
                    if (Info.Inventory[from] == null)
                    {
                        Enqueue(p);
                        return;
                    }
                    if (Info.Inventory[from].Info.Bind.HasFlag(BindMode.DontStore))
                    {
                        Enqueue(p);
                        return;
                    }
                    if (Info.Inventory[from].RentalInformation != null && Info.Inventory[from].RentalInformation.BindingFlags.HasFlag(BindMode.DontStore))
                    {
                        Enqueue(p);
                        return;
                    }
                    if (MyGuild.StoredItems[to] != null)
                    {
                        ReceiveChat("目标槽不空.", ChatType.System);
                        Enqueue(p);
                        return;
                    }
                    MyGuild.StoredItems[to] = new GuildStorageItem() { Item = Info.Inventory[from], UserId = (long)Info.Index };
                    Info.Inventory[from] = null;
                    RefreshBagWeight();
                    MyGuild.SendItemInfo(MyGuild.StoredItems[to].Item);
                    MyGuild.SendServerPacket(new S.GuildStorageItemChange() { Type = 0, User = Info.Index, Item = MyGuild.StoredItems[to], To = to, From = from });
                    MyGuild.NeedSave = true;
                    break;
                case 1://retrieve
                    if (!MyGuildRank.Options.HasFlag(RankOptions.CanRetrieveItem))
                    {

                        ReceiveChat("您没有在公会存储中存储物品的权限.", ChatType.System);
                        return;
                    }
                    if (from < 0 || from >= MyGuild.StoredItems.Length)
                    {
                        Enqueue(p);
                        return;
                    }
                    if (to < 0 || to >= Info.Inventory.Length)
                    {
                        Enqueue(p);
                        return;
                    }
                    if (Info.Inventory[to] != null)
                    {
                        ReceiveChat("目标槽不空.", ChatType.System);
                        Enqueue(p);
                        return;
                    }
                    if (MyGuild.StoredItems[from] == null)
                    {
                        Enqueue(p);
                        return;
                    }
                    if (MaxBagWeight < CurrentBagWeight + MyGuild.StoredItems[from].Item.Weight)
                    {
                        ReceiveChat("过重无法检索物品.", ChatType.System);
                        Enqueue(p);
                        return;
                    }
                    if (MyGuild.StoredItems[from].Item.Info.Bind.HasFlag(BindMode.DontStore))
                    {
                        Enqueue(p);
                        return;
                    }
                    Info.Inventory[to] = MyGuild.StoredItems[from].Item;
                    MyGuild.StoredItems[from] = null;
                    MyGuild.SendServerPacket(new S.GuildStorageItemChange() { Type = 1, User = Info.Index, To = to, From = from });
                    RefreshBagWeight();
                    MyGuild.NeedSave = true;
                    break;
                case 2: // Move Item
                    GuildStorageItem q = null;
                    if (!MyGuildRank.Options.HasFlag(RankOptions.CanStoreItem))
                    {
                        Enqueue(p);
                        ReceiveChat("您没有在公会存储中存储物品的权限.", ChatType.System);
                        return;
                    }
                    if (from < 0 || from >= MyGuild.StoredItems.Length)
                    {
                        Enqueue(p);
                        return;
                    }
                    if (to < 0 || to >= MyGuild.StoredItems.Length)
                    {
                        Enqueue(p);
                        return;
                    }
                    if (MyGuild.StoredItems[from] == null)
                    {
                        Enqueue(p);
                        return;
                    }
                    if (MyGuild.StoredItems[from].Item.Info.Bind.HasFlag(BindMode.DontStore))
                    {
                        Enqueue(p);
                        return;
                    }
                    if (MyGuild.StoredItems[to] != null)
                    {
                        q = MyGuild.StoredItems[to];
                    }
                    MyGuild.StoredItems[to] = MyGuild.StoredItems[from];
                    if (q != null) MyGuild.StoredItems[from] = q;
                    else MyGuild.StoredItems[from] = null;

                    MyGuild.SendItemInfo(MyGuild.StoredItems[to].Item);

                    if (MyGuild.StoredItems[from] != null) MyGuild.SendItemInfo(MyGuild.StoredItems[from].Item);

                    MyGuild.SendServerPacket(new S.GuildStorageItemChange() { Type = 2, User = Info.Index, Item = MyGuild.StoredItems[to], To = to, From = from });
                    MyGuild.NeedSave = true;
                    break;
                case 3://request list
                    if (!GuildCanRequestItems) return;
                    GuildCanRequestItems = false;
                    for (int i = 0; i < MyGuild.StoredItems.Length; i++)
                    {
                        if (MyGuild.StoredItems[i] == null) continue;
                        UserItem item = MyGuild.StoredItems[i].Item;
                        if (item == null) continue;
                        //CheckItemInfo(item.Info);
                        CheckItem(item);
                    }
                    Enqueue(new S.GuildStorageList() { Items = MyGuild.StoredItems });
                    break;
            }

        }
        public void GuildWarReturn(string Name)
        {
            if (MyGuild == null || MyGuildRank != MyGuild.Ranks[0]) return;

            GuildObject enemyGuild = Envir.GetGuild(Name);

            if (enemyGuild == null)
            {
                ReceiveChat(string.Format("找不到公会 {0}.", Name), ChatType.System);
                return;
            }

            if (MyGuild == enemyGuild)
            {
                ReceiveChat("不能与你自己的公会打仗.", ChatType.System);
                return;
            }

            if (MyGuild.WarringGuilds.Contains(enemyGuild))
            {
                ReceiveChat("已经与这个公会交战.", ChatType.System);
                return;
            }

            if (MyGuild.Gold < Settings.Guild_WarCost)
            {
                ReceiveChat("公会银行资金不足.", ChatType.System);
                return;
            }

            if (MyGuild.GoToWar(enemyGuild))
            {
                ReceiveChat(string.Format("你发动了一场战争 {0}.", Name), ChatType.System);
                enemyGuild.SendMessage(string.Format("{0} 发动了一场战争", MyGuild.Name), ChatType.System);

                MyGuild.Gold -= Settings.Guild_WarCost;
                MyGuild.SendServerPacket(new S.GuildStorageGoldChange() { Type = 2, Name = Info.Name, Amount = Settings.Guild_WarCost });
            }
        }

        public bool AtWar(PlayerObject attacker)
        {
            if (CurrentMap.Info.Fight) return true;

            if (MyGuild == null) return false;

            if (attacker == null || attacker.MyGuild == null) return false;

            if (!MyGuild.WarringGuilds.Contains(attacker.MyGuild)) return false;

            return true;
        }

        public void GuildBuffUpdate(byte Type, int Id)
        {
            if (MyGuild == null) return;
            if (MyGuildRank == null) return;
            if (Id < 0) return;
            switch (Type)
            {
                case 0://request info list
                    if (RequestedGuildBuffInfo) return;
                    Enqueue(new S.GuildBuffList() { GuildBuffs = Settings.Guild_BuffList });
                    break;
                case 1://buy the buff
                    if (!MyGuildRank.Options.HasFlag(RankOptions.CanActivateBuff))
                    {
                        ReceiveChat("你没有正确的公会等级.", ChatType.System);
                        return;
                    }
                    GuildBuffInfo BuffInfo = Envir.FindGuildBuffInfo(Id);
                    if (BuffInfo == null)
                    {
                        ReceiveChat("Buff 不存在.", ChatType.System);
                        return;
                    }
                    if (MyGuild.GetBuff(Id) != null)
                    {
                        ReceiveChat("Buff 已经获得.", ChatType.System);
                        return;
                    }
                    if ((MyGuild.Level < BuffInfo.LevelRequirement) || (MyGuild.SparePoints < BuffInfo.PointsRequirement)) return;//client checks this so it shouldnt be possible without a moded client :p
                    MyGuild.NewBuff(Id);
                    break;
                case 2://activate the buff
                    if (!MyGuildRank.Options.HasFlag(RankOptions.CanActivateBuff))
                    {
                        ReceiveChat("你没有正确的公会等级.", ChatType.System);
                        return;
                    }
                    GuildBuff Buff = MyGuild.GetBuff(Id);
                    if (Buff == null)
                    {
                        ReceiveChat("未获得的buff.", ChatType.System);
                        return;
                    }
                    if ((MyGuild.Gold < Buff.Info.ActivationCost) || (Buff.Active)) return;
                    MyGuild.ActivateBuff(Id);
                    break;
            }
        }

        #endregion

        #region Trading

        public void DepositTradeItem(int from, int to)
        {
            S.DepositTradeItem p = new S.DepositTradeItem { From = from, To = to, Success = false };

            if (from < 0 || from >= Info.Inventory.Length)
            {
                Enqueue(p);
                return;
            }

            if (to < 0 || to >= Info.Trade.Length)
            {
                Enqueue(p);
                return;
            }

            UserItem temp = Info.Inventory[from];

            if (temp == null)
            {
                Enqueue(p);
                return;
            }

            if (temp.Info.Bind.HasFlag(BindMode.DontTrade))
            {
                Enqueue(p);
                return;
            }

            if (temp.RentalInformation != null && temp.RentalInformation.BindingFlags.HasFlag(BindMode.DontTrade))
            {
                Enqueue(p);
                return;
            }

            if (Info.Trade[to] == null)
            {
                Info.Trade[to] = temp;
                Info.Inventory[from] = null;
                RefreshBagWeight();
                TradeItem();

                Report.ItemMoved("DepositTradeItem", temp, MirGridType.Inventory, MirGridType.Trade, from, to);
                
                p.Success = true;
                Enqueue(p);
                return;
            }
            Enqueue(p);

        }
        public void RetrieveTradeItem(int from, int to)
        {
            S.RetrieveTradeItem p = new S.RetrieveTradeItem { From = from, To = to, Success = false };

            if (from < 0 || from >= Info.Trade.Length)
            {
                Enqueue(p);
                return;
            }

            if (to < 0 || to >= Info.Inventory.Length)
            {
                Enqueue(p);
                return;
            }

            UserItem temp = Info.Trade[from];

            if (temp == null)
            {
                Enqueue(p);
                return;
            }

            if (temp.Weight + CurrentBagWeight > MaxBagWeight)
            {
                ReceiveChat("太重不能取回.", ChatType.System);
                Enqueue(p);
                return;
            }

            if (Info.Inventory[to] == null)
            {
                Info.Inventory[to] = temp;
                Info.Trade[from] = null;

                p.Success = true;
                RefreshBagWeight();
                TradeItem();

                Report.ItemMoved("RetrieveTradeItem", temp, MirGridType.Trade, MirGridType.Inventory, from, to);
            }

            Enqueue(p);
        }

        public void TradeRequest()
        {
            if (TradePartner != null)
            {
                ReceiveChat("你已经在交易了.", ChatType.System);
                return;
            }

            Point target = Functions.PointMove(CurrentLocation, Direction, 1);
            //Cell cell = CurrentMap.GetCell(target);
            PlayerObject player = null;

            if (CurrentMap.Objects[target.X, target.Y] == null || CurrentMap.Objects[target.X, target.Y].Count < 1) return;

            for (int i = 0; i < CurrentMap.Objects[target.X, target.Y].Count; i++)
            {
                MapObject ob = CurrentMap.Objects[target.X, target.Y][i];
                if (ob.Race != ObjectType.Player) continue;

                player = Envir.GetPlayer(ob.Name);
            }

            if (player == null)
            {
                ReceiveChat(string.Format("你必须面对某人进行交易."), ChatType.System);
                return;
            }

            if (player != null)
            {
                if (!Functions.FacingEachOther(Direction, CurrentLocation, player.Direction, player.CurrentLocation))
                {
                    ReceiveChat(string.Format("你必须面对某人进行交易."), ChatType.System);
                    return;
                }

                if (player == this)
                {
                    ReceiveChat("你不能与你自己交易.", ChatType.System);
                    return;
                }

                if (player.Dead || Dead)
                {
                    ReceiveChat("死后不能交易", ChatType.System);
                    return;
                }

                if (player.TradeInvitation != null)
                {
                    ReceiveChat(string.Format("玩家 {0} 已经有交易邀请了.", player.Info.Name), ChatType.System);
                    return;
                }

                if (!player.AllowTrade)
                {
                    ReceiveChat(string.Format("玩家 {0} 目前不允许交易.对方输入@允许交易，即可开启", player.Info.Name), ChatType.System);
                    return;
                }

                if (!Functions.InRange(player.CurrentLocation, CurrentLocation, Globals.DataRange) || player.CurrentMap != CurrentMap)
                {
                    ReceiveChat(string.Format("玩家 {0} 不在交易范围内.", player.Info.Name), ChatType.System);
                    return;
                }

                if (player.TradePartner != null)
                {
                    ReceiveChat(string.Format("玩家 {0} 正在交易.", player.Info.Name), ChatType.System);
                    return;
                }

                player.TradeInvitation = this;
                player.Enqueue(new S.TradeRequest { Name = Info.Name });
            }
        }
        public void TradeReply(bool accept)
        {
            if (TradeInvitation == null || TradeInvitation.Info == null)
            {
                TradeInvitation = null;
                return;
            }

            if (!accept)
            {
                TradeInvitation.ReceiveChat(string.Format("玩家 {0} 拒绝交易.", Info.Name), ChatType.System);
                TradeInvitation = null;
                return;
            }

            if (TradePartner != null)
            {
                ReceiveChat("你已经在交易了.", ChatType.System);
                TradeInvitation = null;
                return;
            }

            if (TradeInvitation.TradePartner != null)
            {
                ReceiveChat(string.Format("玩家 {0} 正在交易.", TradeInvitation.Info.Name), ChatType.System);
                TradeInvitation = null;
                return;
            }

            TradePartner = TradeInvitation;
            TradeInvitation.TradePartner = this;
            TradeInvitation = null;

            Enqueue(new S.TradeAccept { Name = TradePartner.Info.Name });
            TradePartner.Enqueue(new S.TradeAccept { Name = Info.Name });
        }
        public void TradeGold(uint amount)
        {
            TradeUnlock();

            if (TradePartner == null) return;

            if (Account.Gold < amount)
            {
                return;
            }

            TradeGoldAmount += amount;
            Account.Gold -= amount;

            Enqueue(new S.LoseGold { Gold = amount });
            TradePartner.Enqueue(new S.TradeGold { Amount = TradeGoldAmount });
        }
        public void TradeItem()
        {
            TradeUnlock();

            if (TradePartner == null) return;

            for (int i = 0; i < Info.Trade.Length; i++)
            {
                UserItem u = Info.Trade[i];
                if (u == null) continue;

                //TradePartner.CheckItemInfo(u.Info);
                TradePartner.CheckItem(u);
            }

            TradePartner.Enqueue(new S.TradeItem { TradeItems = Info.Trade });
        }

        //解除交易锁定
        public void TradeUnlock()
        {
            TradeLocked = false;

            if (TradePartner != null)
            {
                TradePartner.TradeLocked = false;
            }
        }

        //交易确认
        public void TradeConfirm(bool confirm)
        {
            if(!confirm)
            {
                TradeLocked = false;
                return;
            }

            if (TradePartner == null)
            {
                TradeCancel();
                return;
            }

            if (!Functions.InRange(TradePartner.CurrentLocation, CurrentLocation, Globals.DataRange) || TradePartner.CurrentMap != CurrentMap ||
                !Functions.FacingEachOther(Direction, CurrentLocation, TradePartner.Direction, TradePartner.CurrentLocation))
            {
                TradeCancel();
                return;
            }

            TradeLocked = true;

            if (TradeLocked && !TradePartner.TradeLocked)
            {
                TradePartner.ReceiveChat(string.Format("玩家 {0} 等待您确认交易.", Info.Name), ChatType.System);
            }

            if (!TradeLocked || !TradePartner.TradeLocked) return;

            PlayerObject[] TradePair = new PlayerObject[2] { TradePartner, this };

            bool CanTrade = true;
            UserItem u;

            //check if both people can accept the others items
            for (int p = 0; p < 2; p++)
            {
                int o = p == 0 ? 1 : 0;

                if (!TradePair[o].CanGainItems(TradePair[p].Info.Trade))
                {
                    CanTrade = false;
                    TradePair[p].ReceiveChat("交易伙伴不能接受所有物品.", ChatType.System);
                    TradePair[p].Enqueue(new S.TradeCancel { Unlock = true });

                    TradePair[o].ReceiveChat("无法接受所有物品.", ChatType.System);
                    TradePair[o].Enqueue(new S.TradeCancel { Unlock = true });

                    return;
                }

                if (!TradePair[o].CanGainGold(TradePair[p].TradeGoldAmount))
                {
                    CanTrade = false;
                    TradePair[p].ReceiveChat("交易伙伴不能再接受金币了.", ChatType.System);
                    TradePair[p].Enqueue(new S.TradeCancel { Unlock = true });

                    TradePair[o].ReceiveChat("无法接受更多的金币.", ChatType.System);
                    TradePair[o].Enqueue(new S.TradeCancel { Unlock = true });

                    return;
                }
            }

            //swap items
            if (CanTrade)
            {
                for (int p = 0; p < 2; p++)
                {
                    int o = p == 0 ? 1 : 0;

                    for (int i = 0; i < TradePair[p].Info.Trade.Length; i++)
                    {
                        u = TradePair[p].Info.Trade[i];

                        if (u == null) continue;

                        TradePair[o].GainItem(u);
                        TradePair[p].Info.Trade[i] = null;

                        Report.ItemMoved("TradeConfirm", u, MirGridType.Trade, MirGridType.Inventory, i, -99, string.Format("Trade from {0} to {1}", TradePair[p].Name, TradePair[o].Name));
                    }

                    if (TradePair[p].TradeGoldAmount > 0)
                    {
                        Report.GoldChanged("TradeConfirm", TradePair[p].TradeGoldAmount, true, string.Format("Trade from {0} to {1}", TradePair[p].Name, TradePair[o].Name));

                        TradePair[o].GainGold(TradePair[p].TradeGoldAmount);
                        TradePair[p].TradeGoldAmount = 0;
                    }

                    TradePair[p].ReceiveChat("交易成功.", ChatType.System);
                    TradePair[p].Enqueue(new S.TradeConfirm());

                    TradePair[p].TradeLocked = false;
                    TradePair[p].TradePartner = null;
                }
            }
        }
        //交易取消
        public void TradeCancel()
        {
            TradeUnlock();

            if (TradePartner == null)
            {
                return;
            }

            PlayerObject[] TradePair = new PlayerObject[2] { TradePartner, this };

            for (int p = 0; p < 2; p++)
            {
                if (TradePair[p] != null)
                {
                    for (int t = 0; t < TradePair[p].Info.Trade.Length; t++)
                    {
                        UserItem temp = TradePair[p].Info.Trade[t];

                        if (temp == null) continue;

                        if(FreeSpace(TradePair[p].Info.Inventory) < 1)
                        {
                            TradePair[p].GainItemMail(temp, 1);
                            Report.ItemMailed("TradeCancel", temp, temp.Count, 1);

                            TradePair[p].Enqueue(new S.DeleteItem { UniqueID = temp.UniqueID, Count = temp.Count });
                            TradePair[p].Info.Trade[t] = null;
                            continue;
                        }

                        for (int i = 0; i < TradePair[p].Info.Inventory.Length; i++)
                        {
                            if (TradePair[p].Info.Inventory[i] != null) continue;

                            //Put item back in inventory
                            if (TradePair[p].CanGainItem(temp))
                            {
                                TradePair[p].RetrieveTradeItem(t, i);
                            }
                            else //Send item to mailbox if it can no longer be stored
                            {
                                TradePair[p].GainItemMail(temp, 1);
                                Report.ItemMailed("TradeCancel", temp, temp.Count, 1);

                                TradePair[p].Enqueue(new S.DeleteItem { UniqueID = temp.UniqueID, Count = temp.Count });
                            }

                            TradePair[p].Info.Trade[t] = null;

                            break;
                        }
                    }

                    //Put back deposited gold
                    if (TradePair[p].TradeGoldAmount > 0)
                    {
                        Report.GoldChanged("TradeCancel", TradePair[p].TradeGoldAmount, false);

                        TradePair[p].GainGold(TradePair[p].TradeGoldAmount);
                        TradePair[p].TradeGoldAmount = 0;
                    }

                    TradePair[p].TradeLocked = false;
                    TradePair[p].TradePartner = null;

                    TradePair[p].Enqueue(new S.TradeCancel { Unlock = false });
                }
            }
        }

        #endregion

        #region Mounts

        public void RefreshMount(bool refreshStats = true)
        {
            if (RidingMount)
            {
                if (MountType < 0)
                {
                    RidingMount = false;
                }
                else if (!Mount.CanRide)
                {
                    RidingMount = false;
                    ReceiveChat("你必须骑上马鞍才能骑上你的坐骑", ChatType.System);
                }
                else if (!Mount.CanMapRide)
                {
                    RidingMount = false;
                    ReceiveChat("你不能骑马在这张地图上", ChatType.System);
                }
                else if (!Mount.CanDungeonRide)
                {
                    RidingMount = false;
                    ReceiveChat("你不能没有缰绳骑马", ChatType.System);
                }
            }
            else
            {
                RidingMount = false;
            }

            if(refreshStats)
                RefreshStats();

            Broadcast(GetMountInfo());
            Enqueue(GetMountInfo());
        }
        public void IncreaseMountLoyalty(int amount)
        {
            UserItem item = Info.Equipment[(int)EquipmentSlot.Mount];
            if (item != null && item.CurrentDura < item.MaxDura)
            {
                item.CurrentDura = (ushort)Math.Min(item.MaxDura, item.CurrentDura + amount);
                item.DuraChanged = false;
                Enqueue(new S.ItemRepaired { UniqueID = item.UniqueID, MaxDura = item.MaxDura, CurrentDura = item.CurrentDura });
            }
        }
        public void DecreaseMountLoyalty(int amount)
        {
            if (Envir.Time > DecreaseLoyaltyTime)
            {
                DecreaseLoyaltyTime = Envir.Time + (Mount.SlowLoyalty ? (LoyaltyDelay * 2) : LoyaltyDelay);
                UserItem item = Info.Equipment[(int)EquipmentSlot.Mount];
                if (item != null && item.CurrentDura > 0)
                {
                    DamageItem(item, amount);

                    if (item.CurrentDura == 0)
                    {
                        RefreshMount();
                    }
                }
            }
        }

        #endregion

        #region Fishing 钓鱼系统

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cast">是否投递</param>
        /// <param name="cancel">是否取消</param>
        public void FishingCast(bool cast, bool cancel = false)
        {
            UserItem rod = Info.Equipment[(int)EquipmentSlot.Weapon];

            byte flexibilityStat = 0;//柔韧度
            sbyte successStat = 0;
            //咬口几率
            byte nibbleMin = 0, nibbleMax = 0;

            byte failedAddSuccessMin = 0, failedAddSuccessMax = 0;
            FishingProgressMax = Settings.FishingAttempts;//30;

            if (rod == null || (rod.Info.Shape != 49 && rod.Info.Shape != 50) || rod.CurrentDura <= 0)
            {
                Fishing = false;
                return;
            }

            Point fishingPoint = Functions.PointMove(CurrentLocation, Direction, 3);

            if (fishingPoint.X < 0 || fishingPoint.Y < 0 || CurrentMap.Width < fishingPoint.X || CurrentMap.Height < fishingPoint.Y)
            {
                Fishing = false;
                return;
            }

            //Cell fishingCell = CurrentMap.Cells[fishingPoint.X, fishingPoint.Y];

            if (!CurrentMap.CanFishing(fishingPoint.X, fishingPoint.Y))
            {
                Fishing = false;
                return;
            }

            flexibilityStat = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, flexibilityStat + rod.Info.CriticalRate)));
            successStat = (sbyte)Math.Max(sbyte.MinValue, (Math.Min(sbyte.MaxValue, successStat + rod.Info.MaxAC)));

            if (cast)
            {
                DamageItem(rod, 1, true);
            }

            UserItem hook = rod.Slots[(int)FishingSlot.Hook];

            if (hook == null)
            {
                ReceiveChat("你需要一个钩子.", ChatType.System);
                return;
            }
            else
            {
                DamagedFishingItem(FishingSlot.Hook, 1);
            }

            foreach (UserItem temp in rod.Slots)
            {
                if (temp == null) continue;

                ItemInfo realItem = Functions.GetRealItem(temp.Info, Info.Level, Info.Class, Envir.ItemInfoList);

                switch (realItem.Type)
                {
                    case ItemType.Hook:
                        {
                            flexibilityStat = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, flexibilityStat + temp.CriticalRate + realItem.CriticalRate)));
                        }
                        break;
                    case ItemType.Float:
                        {
                            nibbleMin = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, nibbleMin + realItem.MinAC)));
                            nibbleMax = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, nibbleMax + realItem.MaxAC)));
                        }
                        break;
                    case ItemType.Bait:
                        {
                            successStat = (sbyte)Math.Max(sbyte.MinValue, (Math.Min(sbyte.MaxValue, successStat + realItem.MaxAC)));
                        }
                        break;
                    case ItemType.Finder:
                        {
                            failedAddSuccessMin = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, failedAddSuccessMin + realItem.MinAC)));
                            failedAddSuccessMax = (byte)Math.Max(byte.MinValue, (Math.Min(byte.MaxValue, failedAddSuccessMax + realItem.MaxAC)));
                        }
                        break;
                    case ItemType.Reel:
                        {
                            FishingAutoReelChance = (sbyte)Math.Max(sbyte.MinValue, (Math.Min(sbyte.MaxValue, FishingAutoReelChance + realItem.MaxMAC)));
                            successStat = (sbyte)Math.Max(sbyte.MinValue, (Math.Min(sbyte.MaxValue, successStat + realItem.MaxAC)));
                        }
                        break;
                    default:
                        break;
                }
            }
            FishingNibbleChance = 5 + RandomUtils.Next(nibbleMin, nibbleMax);

            if (cast) FishingChance = Settings.FishingSuccessStart + (int)successStat + (FishingChanceCounter != 0 ? RandomUtils.Next(failedAddSuccessMin, failedAddSuccessMax) : 0) + (FishingChanceCounter * Settings.FishingSuccessMultiplier); //10 //10
            if (FishingChanceCounter != 0) DamagedFishingItem(FishingSlot.Finder, 1);
            FishingChance += FishRate * 5;

            FishingChance = Math.Min(100, Math.Max(0, FishingChance));
            FishingNibbleChance = Math.Min(100, Math.Max(0, FishingNibbleChance));
            FishingAutoReelChance = Math.Min(100, Math.Max(0, FishingAutoReelChance));

            FishingTime = Envir.Time + FishingCastDelay + Settings.FishingDelay;

            if (cast)
            {
                if (Fishing) return;

                _fishCounter = 0;
                FishFound = false;

                UserItem item = GetBait(1);

                if (item == null)
                {
                    ReceiveChat("你需要诱饵.", ChatType.System);
                    return;
                }

                ConsumeItem(item, 1);
                Fishing = true;
            }
            else
            {
                if (!Fishing)
                {
                    Enqueue(GetFishInfo());
                    return;
                }
                //改下这里实现自动钓哦
                Fishing = false;

                if (FishingProgress > 99)
                {
                    FishingChanceCounter++;
                }



                if (FishFound)
                {
                    int getChance = FishingChance + RandomUtils.Next(10, 24) + (FishingProgress > 50 ? flexibilityStat / 2 : 0);
                    getChance = Math.Min(100, Math.Max(0, getChance));
                    if (RandomUtils.Next(0, 100) <= getChance)
                    {
                        FishingChanceCounter = 0;
                       
                        UserItem dropItem = null;
                        //foreach (DropInfo drop in Envir.FishingDrops.Where(x => x.Type == fishingCell.FishingAttribute))
                        foreach (DropInfo drop in Envir.FishingDrops)
                        {
                            float DropRate = 1;
                            if (EXPOwner != null && EXPOwner.ItemDropRateOffset > 0)
                            {
                                DropRate = DropRate * (1 + EXPOwner.ItemDropRateOffset / 100.0f);
                            }
                            if(CurrentMap!=null && CurrentMap.Info != null)
                            {
                                DropRate = DropRate * CurrentMap.Info.DropRate;
                            }
              
                            if (!drop.isDrop(DropRate))
                            {
                                continue;
                            }
                            ItemInfo dropitem = drop.DropItem();
                            if (dropitem != null)
                            {
                                dropItem = dropitem.CreateDropItem();
                                break;
                            }
                        }

                        if (dropItem == null)
                            ReceiveChat("你的鱼逃走了!", ChatType.System);
                        else if (FreeSpace(Info.Inventory) < 1)
                            ReceiveChat("你的袋子里没有足够的空间.", ChatType.System);
                        else
                        {
                            GainItem(dropItem);
                            Report.ItemChanged("FishedItem", dropItem, dropItem.Count, 2);
                        }
                        //1.百分之1几率出现生物（元宝钓竿的情况下）
                        //1.86%几率多角虫（一个小时4个左右）（120发）
                        //2.6%几率出现小财神龟（1-2个金条）（1400发）
                        //3.3%几率出现经验卷(2-3个50%的，1/5几率100%的，大概200-300块可以出一张100的卷)（3333发）
                        //4.2%财富龟（1金砖 1金条 1/5金盒，大概200-300块可以出一张金盒）（5000发）
                        //5.3%幸运龟(5-10个神珠，1/22几率出幸运项链，这样大概要花2000-3000才可能出到一条项链）（3333发）
                        //随机产生怪物在身后，不管有没钓到
                        if (RandomUtils.Next(1000) <15)
                        {
                            MonsterObject mob = null;
                            int rd = RandomUtils.Next(1000);
                            if (rd < 860)//86%
                            {
                                SMain.Enqueue("钓到 巨型多角虫");
                                mob = MonsterObject.GetMonster(Envir.GetMonsterInfo("巨型多角虫"));
                            }
                            else if (rd < 920)//6%
                            {
                                SMain.Enqueue("钓到 幸运龟");
                                mob = MonsterObject.GetMonster(Envir.GetMonsterInfo("幸运龟"));
                            }
                            else if (rd < 950)//3%
                            {
                                SMain.Enqueue("钓到 绿毛龟");//
                                mob = MonsterObject.GetMonster(Envir.GetMonsterInfo("绿毛龟"));
                            }
                            else if (rd < 985) //2 %
                            {
                                SMain.Enqueue("钓到 小财神龟");
                                mob = MonsterObject.GetMonster(Envir.GetMonsterInfo("小财神龟"));
                            }
                            else
                            {
                                SMain.Enqueue("钓到 财神龟");
                                mob = MonsterObject.GetMonster(Envir.GetMonsterInfo("财神龟"));
                            }
                            if (mob != null)
                            {
                                //默认是0.022f;降低恢复速度
                                mob.HealthScale = 0.001f;
                                if (CurrentMap.ValidPoint(Back))
                                {
                                    mob.Spawn(CurrentMap, Back);
                                }
                                else
                                {
                                    mob.Spawn(CurrentMap, CurrentMap.RandomValidPoint(CurrentLocation.X, CurrentLocation.Y,2));
                                }
                            }
                        }
                        //这个有65%的几率，每次大概6000金币,收益一半，那就是3000金币，这个其实2000比较合理
                        if (Level < Envir.MaxLevel)
                        {
                            uint gexp = 3000;
                            gexp = gexp * (uint)Settings.LevelGoldExpList[Level];
                            GainExp(gexp);
                        }
                        else
                        {
                            ReceiveChat("你的等级过高，钓鱼没有经验收益!", ChatType.Hint);
                        }

                        DamagedFishingItem(FishingSlot.Reel, 1);
                        //改下这里实现自动钓哦
                        cancel = false;

                    }
                    else
                        ReceiveChat("你的鱼逃走了!", ChatType.System);
                }

                FishFound = false;
                FishFirstFound = false;
            }

            Enqueue(GetFishInfo());
            Broadcast(GetFishInfo());

            if (FishingAutocast && !cast && !cancel)
            {
                FishingTime = Envir.Time + (FishingCastDelay * 2);
                FishingFoundTime = Envir.Time;
                FishingAutoReelChance = 0;
                FishingNibbleChance = 0;
                FishFirstFound = false;

                FishingCast(true);
            }
        }

        /// <summary>
        /// 是否自动消耗
        /// </summary>
        /// <param name="autoCast"></param>
        public void FishingChangeAutocast(bool autoCast)
        {
            UserItem rod = Info.Equipment[(int)EquipmentSlot.Weapon];

            if (rod == null || (rod.Info.Shape != 49 && rod.Info.Shape != 50)) return;

            UserItem reel = rod.Slots[(int)FishingSlot.Reel];

            if (reel == null)
            {
                FishingAutocast = false;
                return;
            }

            FishingAutocast = autoCast;
        }
        //更新客户端的钓鱼效果
        public void UpdateFish()
        {
            if (FishFound != true && FishFirstFound != true)
            {
                FishFound = RandomUtils.Next(0, 100) <= FishingNibbleChance;
                FishingFoundTime = FishFound ? Envir.Time + 3000 : Envir.Time;

                if (FishFound)
                {
                    FishFirstFound = true;
                    DamagedFishingItem(FishingSlot.Float, 1);
                }
            }
            else
            {
                if (FishingAutoReelChance != 0 && RandomUtils.Next(0, 100) <= FishingAutoReelChance)
                {
                    FishingCast(false);
                }
            }

            if (FishingFoundTime < Envir.Time)
                FishFound = false;

            FishingTime = Envir.Time + FishingDelay;
            Enqueue(GetFishInfo());

            if (FishingProgress > 100)
            {
                FishingCast(false);
            }
        }
        Packet GetFishInfo()
        {
            FishingProgress = _fishCounter > 0 ? (int)(((decimal)_fishCounter / FishingProgressMax) * 100) : 0;

            return new S.FishingUpdate
            {
                ObjectID = ObjectID,
                Fishing = Fishing,
                ProgressPercent = FishingProgress,
                FishingPoint = Functions.PointMove(CurrentLocation, Direction, 3),
                ChancePercent = FishingChance,
                FoundFish = FishFound
            };
        }

        #endregion

        #region Quests
        //接受任务
        public void AcceptQuest(int index)
        {
            bool canAccept = true;

            if (CurrentQuests.Exists(e => e.Info.Index == index)) return; //e.Info.NpcIndex == npcIndex && 

            QuestInfo info = Envir.QuestInfoList.FirstOrDefault(d => d.Index == index);

            NPCObject npc = null;

            for (int i = CurrentMap.NPCs.Count - 1; i >= 0; i--)
            {
                if (CurrentMap.NPCs[i].ObjectID != info.NpcIndex) continue;

                if (!Functions.InRange(CurrentMap.NPCs[i].CurrentLocation, CurrentLocation, Globals.DataRange)) break;
                npc = CurrentMap.NPCs[i];
                break;
            }
            if (npc == null || !npc.VisibleLog[Info.Index] || !npc.Visible) return;

            if (!info.CanAccept(this))
            {
                canAccept = false;
            }

            if (CurrentQuests.Count >= Globals.MaxConcurrentQuests)
            {
                ReceiveChat("已获得的最大任务量.", ChatType.System);
                return;
            }

            if (CompletedQuests.Contains(index))
            {
                ReceiveChat("任务已经完成.", ChatType.System);
                return;
            }

            //检查以前的链接任务是否已完成
            //check previous chained quests have been completed
            QuestInfo tempInfo = info;
            while (tempInfo != null && tempInfo.RequiredQuest != 0)
            {
                if (!CompletedQuests.Contains(tempInfo.RequiredQuest))
                {
                    canAccept = false;
                    break;
                }

                tempInfo = Envir.QuestInfoList.FirstOrDefault(d => d.Index == tempInfo.RequiredQuest);
            }

            if (!canAccept)
            {
                ReceiveChat("无法接受任务.", ChatType.System);
                return;
            }

            //给玩家任务道具，让玩家进行押运之类的。
            if (info.CarryItems.Count > 0)
            {
                foreach (QuestItemTask carryItem in info.CarryItems)
                {
                    uint count = carryItem.Count;

                    while (count > 0)
                    {
                        UserItem item = carryItem.Item.CreateFreshItem();

                        if (item.Info.StackSize > count)
                        {
                            item.Count = count;
                            count = 0;
                        }
                        else
                        {
                            count -= item.Info.StackSize;
                            item.Count = item.Info.StackSize;
                        }

                        if (!CanGainQuestItem(item))
                        {
                            RecalculateQuestBag();
                            return;
                        }

                        GainQuestItem(item);

                        Report.ItemChanged("AcceptQuest", item, item.Count, 2);
                    }
                }
            }

            QuestProgressInfo quest = new QuestProgressInfo(index) { StartDateTime = DateTime.Now };

            CurrentQuests.Add(quest);
            SendUpdateQuest(quest, QuestState.Add, true);

            CallDefaultNPC(DefaultNPCType.OnAcceptQuest, index);
        }
        //完成某个任务
        public void FinishQuest(int questIndex, int selectedItemIndex = -1)
        {
            QuestProgressInfo quest = CurrentQuests.FirstOrDefault(e => e.Info.Index == questIndex);

            if (quest == null || !quest.Completed) return;

            NPCObject npc = null;

            for (int i = CurrentMap.NPCs.Count - 1; i >= 0; i--)
            {
                if (CurrentMap.NPCs[i].ObjectID != quest.Info.FinishNpcIndex) continue;

                if (!Functions.InRange(CurrentMap.NPCs[i].CurrentLocation, CurrentLocation, Globals.DataRange)) break;
                npc = CurrentMap.NPCs[i];
                break;
            }
            if (npc == null || !npc.VisibleLog[Info.Index] || !npc.Visible) return;

            List<UserItem> rewardItems = new List<UserItem>();

            foreach (var reward in quest.Info.FixedRewards)
            {
                uint count = reward.Count;

                UserItem rewardItem;

                while (count > 0)
                {
                    rewardItem = reward.Item.CreateFreshItem();
                    if (reward.Item.StackSize >= count)
                    {
                        rewardItem.Count = count;
                        count = 0;
                    }
                    else
                    {
                        rewardItem.Count = reward.Item.StackSize;
                        count -= reward.Item.StackSize;
                    }

                    rewardItems.Add(rewardItem);
                }
            }

            if (selectedItemIndex >= 0)
            {
                for (int i = 0; i < quest.Info.SelectRewards.Count; i++)
                {
                    if (selectedItemIndex != i) continue;

                    uint count = quest.Info.SelectRewards[i].Count;
                    UserItem rewardItem;

                    while (count > 0)
                    {
                        rewardItem = quest.Info.SelectRewards[i].Item.CreateFreshItem();
                        if (quest.Info.SelectRewards[i].Item.StackSize >= count)
                        {
                            rewardItem.Count = count;
                            count = 0;
                        }
                        else
                        {
                            rewardItem.Count = quest.Info.SelectRewards[i].Item.StackSize;
                            count -= quest.Info.SelectRewards[i].Item.StackSize;
                        }

                        rewardItems.Add(rewardItem);
                    }
                }
            }

            if (!CanGainItems(rewardItems.ToArray()))
            {
                ReceiveChat("背包满时不能交任务.", ChatType.System);
                return;
            }
            //可以无限重复做的任务，不放入完成列表
            if (quest.Info.Type != QuestType.Repeatable)
            {
                Info.CompletedQuests.Add(quest.Index);
                GetCompletedQuests();
            }

            CurrentQuests.Remove(quest);
            SendUpdateQuest(quest, QuestState.Remove);

            if (quest.Info.CarryItems.Count > 0)
            {
                foreach (QuestItemTask carryItem in quest.Info.CarryItems)
                {
                    TakeQuestItem(carryItem.Item, carryItem.Count);
                }
            }

            foreach (QuestItemTask iTask in quest.Info.ItemTasks)
            {
                TakeQuestItem(iTask.Item, Convert.ToUInt32(iTask.Count));
            }

            foreach (UserItem item in rewardItems)
            {
                GainItem(item);
            }

            RecalculateQuestBag();

            GainGold(quest.Info.GoldReward);
            GainExp(quest.Info.ExpReward);
            GainCredit(quest.Info.CreditReward);

            CallDefaultNPC(DefaultNPCType.OnFinishQuest, questIndex);
        }
        //放弃任务
        public void AbandonQuest(int questIndex)
        {
            QuestProgressInfo quest = CurrentQuests.FirstOrDefault(e => e.Info.Index == questIndex);

            if (quest == null) return;

            CurrentQuests.Remove(quest);
            SendUpdateQuest(quest, QuestState.Remove);

            RecalculateQuestBag();
        }
        //共享任务
        public void ShareQuest(int questIndex)
        {
            bool shared = false;
            if (GroupMembers != null)
            {
                foreach (PlayerObject player in GroupMembers.
                    Where(player => player.CurrentMap == CurrentMap &&
                        Functions.InRange(player.CurrentLocation, CurrentLocation, Globals.DataRange) &&
                        !player.Dead && player != this))
                {
                    player.Enqueue(new S.ShareQuest { QuestIndex = questIndex, SharerName = Name });
                    shared = true;
                }
            }

            if (!shared)
            {
                ReceiveChat("任务不能与任何人分享.", ChatType.System);
            }
        }

        //检测怪物杀死任务
        public void CheckGroupQuestKill(MonsterInfo mInfo)
        {
            if (Info == null)
            {
                return;
            }
            if (GroupMembers != null)
            {
                foreach (PlayerObject player in GroupMembers.
                    Where(player => player.CurrentMap == CurrentMap &&
                        Functions.InRange(player.CurrentLocation, CurrentLocation, Globals.DataRange) &&
                        !player.Dead))
                {
                    player.CheckNeedQuestKill(mInfo);
                }
            }
            else
                CheckNeedQuestKill(mInfo);
        }
        //任务
        public bool CheckGroupQuestItem(UserItem item, bool gainItem = true)
        {
            bool itemCollected = false;

            if (GroupMembers != null)
            {
                foreach (PlayerObject player in GroupMembers.
                    Where(player => player.CurrentMap == CurrentMap &&
                        Functions.InRange(player.CurrentLocation, CurrentLocation, Globals.DataRange) &&
                        !player.Dead))
                {
                    if (player.CheckNeedQuestItem(item, gainItem))
                    {
                        itemCollected = true;
                        player.Report.ItemChanged("WinQuestItem", item, item.Count, 2);
                    }
                }
            }
            else
            {
                if (CheckNeedQuestItem(item, gainItem))
                {
                    itemCollected = true;
                    Report.ItemChanged("WinQuestItem", item, item.Count, 2);
                }
            }

            return itemCollected;
        }
        //任务
        public bool CheckNeedQuestItem(UserItem item, bool gainItem = true)
        {
            foreach (QuestProgressInfo quest in CurrentQuests.
                Where(e => e.ItemTaskCount.Count > 0).
                Where(e => e.NeedItem(item.Info)).
                Where(e => CanGainQuestItem(item)))
            {
                if (gainItem)
                {
                    GainQuestItem(item);
                    quest.ProcessItem(Info.QuestInventory);

                    Enqueue(new S.SendOutputMessage { Message = string.Format("你已找到 {0}.", item.FriendlyName), Type = OutputMessageType.Quest });

                    SendUpdateQuest(quest, QuestState.Update);

                    Report.ItemChanged("WinQuestItem", item, item.Count, 2);
                }
                return true;
            }

            return false;
        }
        public bool CheckNeedQuestFlag(int flagNumber)
        {
            foreach (QuestProgressInfo quest in CurrentQuests.
                Where(e => e.FlagTaskSet.Count > 0).
                Where(e => e.NeedFlag(flagNumber)))
            {
                quest.ProcessFlag(Info.Flags);

                //Enqueue(new S.SendOutputMessage { Message = string.Format("Location visited."), Type = OutputMessageType.Quest });

                SendUpdateQuest(quest, QuestState.Update);
                return true;
            }

            return false;
        }
        //任务
        public void CheckNeedQuestKill(MonsterInfo mInfo)
        {
            foreach (QuestProgressInfo quest in CurrentQuests.
                    Where(e => e.KillTaskCount.Count > 0).
                    Where(quest => quest.NeedKill(mInfo)))
            {
                quest.ProcessKill(mInfo);

                Enqueue(new S.SendOutputMessage { Message = string.Format("You killed {0}.", mInfo.GameName), Type = OutputMessageType.Quest });

                SendUpdateQuest(quest, QuestState.Update);
            }
        }

        public void RecalculateQuestBag()
        {
            for (int i = Info.QuestInventory.Length - 1; i >= 0; i--)
            {
                UserItem itm = Info.QuestInventory[i];

                if (itm == null) continue;

                bool itemRequired = false;

                foreach (QuestProgressInfo quest in CurrentQuests)
                {
                    foreach (QuestItemTask task in quest.Info.ItemTasks)
                    {
                        if (task.Item == itm.Info)
                        {
                            itemRequired = true;
                            break;
                        }
                    }
                }

                if (!itemRequired)
                {
                    Info.QuestInventory[i] = null;
                    Enqueue(new S.DeleteQuestItem { UniqueID = itm.UniqueID, Count = itm.Count });
                }
            }
        }

        public void SendUpdateQuest(QuestProgressInfo quest, QuestState state, bool trackQuest = false)
        {
            quest.CheckCompleted();

            Enqueue(new S.ChangeQuest
            {
                Quest = quest.CreateClientQuestProgress(),
                QuestState = state,
                TrackQuest = trackQuest
            });
        }

        public void GetCompletedQuests()
        {
            Enqueue(new S.CompleteQuest
            {
                CompletedQuests = CompletedQuests
            });
        }

        #endregion

        #region Mail

        public void SendMail(string name, string message)
        {
            CharacterInfo player = Envir.GetCharacterInfo(name);

            if (player == null)
            {
                ReceiveChat(string.Format("找不到玩家 {0}", name), ChatType.System);
                return;
            }

            if (player.Friends.Any(e => e.Info == Info && e.Blocked))
            {
                ReceiveChat("玩家不接受你的邮件.", ChatType.System);
                return;
            }

            if (Info.Friends.Any(e => e.Info == player && e.Blocked))
            {
                ReceiveChat("当你在黑名单上时不能邮寄玩家.", ChatType.System);
                return;
            }

            //sent from player
            MailInfo mail = new MailInfo(player.Index, true)
            {
                Sender = Info.Name,
                Message = message,
                Gold = 0
            };

            mail.Send();
        }

        public void SendMail(string name, string message, uint gold, ulong[] items, bool stamped)
        {
            CharacterInfo player = Envir.GetCharacterInfo(name);

            if (player == null)
            {
                ReceiveChat(string.Format("找不到玩家 {0}", name), ChatType.System);
                return;
            }
            if(message==null|| message.Length == 0)
            {
                message = "邮件请在安全区收取";
            }

            bool hasStamp = false;
            uint totalGold = 0;
            uint parcelCost = GetMailCost(items, gold, stamped);

            totalGold = gold + parcelCost;

            if (Account.Gold < totalGold || Account.Gold < gold || gold > totalGold)
            {
                Enqueue(new S.MailSent { Result = -1 });
                return;
            }

            //Validate user has stamp
            if (stamped)
            {
                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    UserItem item = Info.Inventory[i];

                    if (item == null || item.Info.Type != ItemType.Nothing || item.Info.Shape != 1 || item.Count < 1) continue;

                    hasStamp = true;

                    if (item.Count > 1) item.Count--;
                    else Info.Inventory[i] = null;

                    Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = 1 });
                    break;
                }
            }

            List<UserItem> giftItems = new List<UserItem>();

            for (int j = 0; j < (hasStamp ? 5 : 1); j++)
            {
                if (items[j] < 1) continue;

                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    UserItem item = Info.Inventory[i];

                    if (item == null || items[j] != item.UniqueID) continue;

                    if(item.Info.Bind.HasFlag(BindMode.DontTrade))
                    {
                        ReceiveChat(string.Format("{0} 无法邮寄", item.FriendlyName), ChatType.System);
                        return;
                    }

                    if (item.RentalInformation != null && item.RentalInformation.BindingFlags.HasFlag(BindMode.DontTrade))
                    {
                        ReceiveChat(string.Format("{0} 无法邮寄", item.FriendlyName), ChatType.System);
                        return;
                    }

                    giftItems.Add(item);

                    Info.Inventory[i] = null;
                    Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                }
            }

            if (totalGold > 0)
            {
                Account.Gold -= totalGold;
                Enqueue(new S.LoseGold { Gold = totalGold });
            }

            //Create parcel
            MailInfo mail = new MailInfo(player.Index, true)
            {
                MailID = (ulong)UniqueKeyHelper.UniqueNext(),
                Sender = Info.Name,
                Message = message,
                Gold = gold,
                Items = giftItems
            };

            mail.Send();

            Enqueue(new S.MailSent { Result = 1 });
        }

        public void ReadMail(ulong mailID)
        {
            MailInfo mail = Info.Mail.SingleOrDefault(e => e.MailID == mailID);

            if (mail == null) return;

            mail.DateOpened = DateTime.Now;

            GetMail();
        }

        public void CollectMail(ulong mailID)
        {
            MailInfo mail = Info.Mail.SingleOrDefault(e => e.MailID == mailID);

            if (mail == null) return;

            if (!mail.Collected)
            {
                ReceiveChat("邮件必须在仓库收取.", ChatType.System);
                return;
            }

            if (!InSafeZone)
            {
                if (CurrentMap != null && CurrentMap.Info!=null && CurrentMap.Info.SafeZones.Count==0)
                {
                    ReceiveChat("邮件请在安全区收取.", ChatType.System);
                    return;
                }
            }
           
            


            if (mail.Items.Count > 0)
            {
                if (!CanGainItems(mail.Items.ToArray()))
                {
                    ReceiveChat("包满时不能收取物品.", ChatType.System);
                    return;
                }

                for (int i = 0; i < mail.Items.Count; i++)
                {
                    GainItem(mail.Items[i]);
                }
            }

            if (mail.Gold > 0)
            {
                uint count = mail.Gold;

                if (count + Account.Gold >= uint.MaxValue)
                    count = uint.MaxValue - Account.Gold;

                GainGold(count);
            }

            mail.Items = new List<UserItem>();
            mail.Gold = 0;

            mail.Collected = true;

            Enqueue(new S.ParcelCollected { Result = 1 });

            GetMail();
        }

        public void DeleteMail(ulong mailID)
        {
            MailInfo mail = Info.Mail.SingleOrDefault(e => e.MailID == mailID);

            if (mail == null) return;

            Info.Mail.Remove(mail);

            GetMail();
        }

        public void LockMail(ulong mailID, bool lockMail)
        {
            MailInfo mail = Info.Mail.SingleOrDefault(e => e.MailID == mailID);

            if (mail == null) return;

            mail.Locked = lockMail;

            GetMail();
        }

        public uint GetMailCost(ulong[] items, uint gold, bool stamped)
        {
            uint cost = 0;

            if (!Settings.MailFreeWithStamp || !stamped)
            {
                if (gold > 0 && Settings.MailCostPer1KGold > 0)
                {
                    cost += (uint)Math.Floor((decimal)gold / 1000) * Settings.MailCostPer1KGold;
                }

                if (items != null && items.Length > 0 && Settings.MailItemInsurancePercentage > 0)
                {
                    for (int j = 0; j < (stamped ? 5 : 1); j++)
                    {
                        if (items[j] < 1) continue;

                        for (int i = 0; i < Info.Inventory.Length; i++)
                        {
                            UserItem item = Info.Inventory[i];

                            if (item == null || items[j] != item.UniqueID) continue;

                            cost += (uint)Math.Floor((double)item.Price() / 100 * Settings.MailItemInsurancePercentage);
                        }
                    }
                }
            }


            return cost;
        }

        public void GetMail()
        {
            List<ClientMail> mail = new List<ClientMail>();

            int start = (Info.Mail.Count - Settings.MailCapacity) > 0 ? (Info.Mail.Count - (int)Settings.MailCapacity) : 0;

            for (int i = start; i < Info.Mail.Count; i++)
            {
                foreach (UserItem itm in Info.Mail[i].Items)
                {
                    CheckItem(itm);
                }

                mail.Add(Info.Mail[i].CreateClientMail());
            }

            //foreach (MailInfo m in Info.Mail)
            //{
            //    foreach (UserItem itm in m.Items)
            //    {
            //        CheckItem(itm);
            //    }

            //    mail.Add(m.CreateClientMail());
            //}

            NewMail = false;

            Enqueue(new S.ReceiveMail { Mail = mail });
        }

        public int GetMailAwaitingCollectionAmount()
        {
            int count = 0;
            for (int i = 0; i < Info.Mail.Count; i++)
            {
                if (!Info.Mail[i].Collected) count++;
            }

            return count;
        }

        #endregion

        #region IntelligentCreatures 智能生物？

        public void SummonIntelligentCreature(IntelligentCreatureType pType)
        {
            if (pType == IntelligentCreatureType.None) return;

            if (Dead) return;

            if (CreatureSummoned == true || SummonedCreatureType != IntelligentCreatureType.None) return;

            for (int i = 0; i < Info.IntelligentCreatures.Count; i++)
            {
                if (Info.IntelligentCreatures[i].PetType != pType) continue;

                MonsterInfo mInfo = Envir.GetMonsterInfo(Settings.IntelligentCreatureNameList[(byte)pType]);
                if (mInfo == null) return;

                byte petlevel = 0;//for future use

                MonsterObject monster = MonsterObject.GetMonster(mInfo);
                if (monster == null) return;
                monster.PetLevel = petlevel;
                monster.Master = this;
                monster.MaxPetLevel = 7;
                monster.Direction = Direction;
                monster.ActionTime = Envir.Time + 1000;
                ((IntelligentCreatureObject)monster).CustomName = Info.IntelligentCreatures[i].CustomName;
                ((IntelligentCreatureObject)monster).CreatureRules = new IntelligentCreatureRules
                {
                    MinimalFullness = Info.IntelligentCreatures[i].Info.MinimalFullness,
                    MousePickupEnabled = Info.IntelligentCreatures[i].Info.MousePickupEnabled,
                    MousePickupRange = Info.IntelligentCreatures[i].Info.MousePickupRange,
                    AutoPickupEnabled = Info.IntelligentCreatures[i].Info.AutoPickupEnabled,
                    AutoPickupRange = Info.IntelligentCreatures[i].Info.AutoPickupRange,
                    SemiAutoPickupEnabled = Info.IntelligentCreatures[i].Info.SemiAutoPickupEnabled,
                    SemiAutoPickupRange = Info.IntelligentCreatures[i].Info.SemiAutoPickupRange,
                    CanProduceBlackStone = Info.IntelligentCreatures[i].Info.CanProduceBlackStone
                };
                ((IntelligentCreatureObject)monster).ItemFilter = Info.IntelligentCreatures[i].Filter;
                ((IntelligentCreatureObject)monster).CurrentPickupMode = Info.IntelligentCreatures[i].petMode;
                ((IntelligentCreatureObject)monster).Fullness = Info.IntelligentCreatures[i].Fullness;
                ((IntelligentCreatureObject)monster).blackstoneTime = Info.IntelligentCreatures[i].BlackstoneTime;
                ((IntelligentCreatureObject)monster).maintainfoodTime = Info.IntelligentCreatures[i].MaintainFoodTime;

                if (!CurrentMap.ValidPoint(Front)) return;
                monster.Spawn(CurrentMap, Front);
                Pets.Add(monster);//make a new creaturelist ? 

                CreatureSummoned = true;
                SummonedCreatureType = pType;

                ReceiveChat((string.Format("生物 {0} 已被召唤.", Info.IntelligentCreatures[i].CustomName)), ChatType.System);
                break;
            }
            //update client
            GetCreaturesInfo();
        }
        public void UnSummonIntelligentCreature(IntelligentCreatureType pType, bool doUpdate = true)
        {
            if (pType == IntelligentCreatureType.None) return;

            for (int i = 0; i < Pets.Count; i++)
            {
                if (Pets[i].Info.AI != 64) continue;
                if (((IntelligentCreatureObject)Pets[i]).petType != pType) continue;

                if (doUpdate) ReceiveChat((string.Format("生物 {0}被解雇了.", ((IntelligentCreatureObject)Pets[i]).CustomName)), ChatType.System);

                Pets[i].Die();

                CreatureSummoned = false;
                SummonedCreatureType = IntelligentCreatureType.None;
                break;
            }
            //update client
            if (doUpdate) GetCreaturesInfo();
        }
        public void ReleaseIntelligentCreature(IntelligentCreatureType pType, bool doUpdate = true)
        {
            if (pType == IntelligentCreatureType.None) return;

            //remove creature
            for (int i = 0; i < Info.IntelligentCreatures.Count; i++)
            {
                if (Info.IntelligentCreatures[i].PetType != pType) continue;

                if (doUpdate) ReceiveChat((string.Format("生物 {0} 已被释放.", Info.IntelligentCreatures[i].CustomName)), ChatType.System);

                Info.IntelligentCreatures.Remove(Info.IntelligentCreatures[i]);
                break;
            }

            //re-arange slots
            for (int i = 0; i < Info.IntelligentCreatures.Count; i++)
                Info.IntelligentCreatures[i].SlotIndex = i;

            //update client
            if (doUpdate) GetCreaturesInfo();
        }

        public void UpdateSummonedCreature(IntelligentCreatureType pType)
        {
            if (pType == IntelligentCreatureType.None) return;

            UserIntelligentCreature creatureInfo = null;
            for (int i = 0; i < Info.IntelligentCreatures.Count; i++)
            {
                if (Info.IntelligentCreatures[i].PetType != pType) continue;

                creatureInfo = Info.IntelligentCreatures[i];
                break;
            }
            if (creatureInfo == null) return;

            for (int i = 0; i < Pets.Count; i++)
            {
                if (Pets[i].Info.AI != 64) continue;
                if (((IntelligentCreatureObject)Pets[i]).petType != pType) continue;

                ((IntelligentCreatureObject)Pets[i]).CustomName = creatureInfo.CustomName;
                ((IntelligentCreatureObject)Pets[i]).ItemFilter = creatureInfo.Filter;
                ((IntelligentCreatureObject)Pets[i]).CurrentPickupMode = creatureInfo.petMode;
                break;
            }
        }
        public void UpdateCreatureFullness(IntelligentCreatureType pType, int fullness)
        {
            if (pType == IntelligentCreatureType.None) return;

            for (int i = 0; i < Info.IntelligentCreatures.Count; i++)
            {
                if (Info.IntelligentCreatures[i].PetType != pType) continue;
                Info.IntelligentCreatures[i].Fullness = fullness;
                break;
            }

            //update client
            //GetCreaturesInfo();
        }
        public void UpdateCreatureBlackstoneTime(IntelligentCreatureType pType, long blackstonetime)
        {
            if (pType == IntelligentCreatureType.None) return;

            for (int i = 0; i < Info.IntelligentCreatures.Count; i++)
            {
                if (Info.IntelligentCreatures[i].PetType != pType) continue;
                Info.IntelligentCreatures[i].BlackstoneTime = blackstonetime;
                break;
            }

            //update client
            //GetCreaturesInfo();
        }
        public void UpdateCreatureMaintainFoodTime(IntelligentCreatureType pType, long maintainfoodtime)
        {
            if (pType == IntelligentCreatureType.None) return;

            for (int i = 0; i < Info.IntelligentCreatures.Count; i++)
            {
                if (Info.IntelligentCreatures[i].PetType != pType) continue;
                Info.IntelligentCreatures[i].MaintainFoodTime = maintainfoodtime;
                break;
            }

            //update client
            //GetCreaturesInfo();
        }

        public void RefreshCreaturesTimeLeft()
        {
            if (Envir.Time > CreatureTimeLeftTicker)
            {
                //Make sure summoned vars are in correct state
                RefreshCreatureSummoned();

                //ExpireTime
                List<int> releasedPets = new List<int>();
                CreatureTimeLeftTicker = Envir.Time + CreatureTimeLeftDelay;
                for (int i = 0; i < Info.IntelligentCreatures.Count; i++)
                {
                    if (Info.IntelligentCreatures[i].ExpireTime == -9999) continue;//permanent
                    Info.IntelligentCreatures[i].ExpireTime = Info.IntelligentCreatures[i].ExpireTime - 1;
                    if (Info.IntelligentCreatures[i].ExpireTime <= 0)
                    {
                        Info.IntelligentCreatures[i].ExpireTime = 0;
                        if (CreatureSummoned && SummonedCreatureType == Info.IntelligentCreatures[i].PetType)
                            UnSummonIntelligentCreature(SummonedCreatureType, false);//unsummon creature
                        releasedPets.Add(i);
                    }
                }
                for (int i = (releasedPets.Count - 1); i >= 0; i--)//start with largest value
                {
                    ReceiveChat((string.Format("宠物 {0} 已过期.", Info.IntelligentCreatures[releasedPets[i]].CustomName)), ChatType.System);
                    ReleaseIntelligentCreature(Info.IntelligentCreatures[releasedPets[i]].PetType, false);//release creature
                }

                if (CreatureSummoned && SummonedCreatureType != IntelligentCreatureType.None)
                {
                    for (int i = 0; i < Pets.Count; i++)
                    {
                        if (Pets[i].Info.AI != 64) continue;
                        if (((IntelligentCreatureObject)Pets[i]).petType != SummonedCreatureType) continue;

                        ((IntelligentCreatureObject)Pets[i]).ProcessBlackStoneProduction();
                        ((IntelligentCreatureObject)Pets[i]).ProcessMaintainFoodBuff();
                        break;
                    }
                }

                //update client
                GetCreaturesInfo();
            }
        }
        public void RefreshCreatureSummoned()
        {
            if (SummonedCreatureType == IntelligentCreatureType.None || !CreatureSummoned)
            {
                //make sure both are in the unsummoned state
                CreatureSummoned = false;
                SummonedCreatureType = IntelligentCreatureType.None;
                return;
            }
            bool petFound = false;
            for (int i = 0; i < Pets.Count; i++)
            {
                if (Pets[i].Info.AI != 64) continue;
                if (((IntelligentCreatureObject)Pets[i]).petType != SummonedCreatureType) continue;
                petFound = true;
                break;
            }
            if (!petFound)
            {
                SMain.EnqueueDebugging(string.Format("{0}: SummonedCreature no longer exists?!?. {1}", Name, SummonedCreatureType.ToString()));
                CreatureSummoned = false;
                SummonedCreatureType = IntelligentCreatureType.None;
            }
        }

        public void IntelligentCreaturePickup(bool mousemode, Point atlocation)
        {
            if (!CreatureSummoned) return;

            for (int i = 0; i < Pets.Count; i++)
            {
                if (Pets[i].Info.AI != 64) continue;
                if (((IntelligentCreatureObject)Pets[i]).petType != SummonedCreatureType) continue;

                //((IntelligentCreatureObject)Pets[i]).MouseLocation = atlocation;
                ((IntelligentCreatureObject)Pets[i]).ManualPickup(mousemode, atlocation);
                break;
            }
        }

        public void IntelligentCreatureGainPearls(int amount)
        {
            Info.PearlCount += amount;
            if (Info.PearlCount > int.MaxValue) Info.PearlCount = int.MaxValue;
        }

        public void IntelligentCreatureLosePearls(int amount)
        {
            Info.PearlCount -= amount;
            if (Info.PearlCount < 0) Info.PearlCount = 0;
        }

        public void IntelligentCreatureProducePearl()
        {
            Info.PearlCount++;
        }
        public bool IntelligentCreatureProduceBlackStone()
        {
            ItemInfo iInfo = ItemInfo.getItem(Settings.CreatureBlackStoneName);
            if (iInfo == null) return false;

            UserItem item = iInfo.CreateDropItem();
            item.Count = 1;

            if (!CanGainItem(item, false)) return false;

            GainItem(item);
            return true;
        }

        public void IntelligentCreatureSay(IntelligentCreatureType pType, string message)
        {
            if (!CreatureSummoned || message == "") return;
            if (pType != SummonedCreatureType) return;

            for (int i = 0; i < Pets.Count; i++)
            {
                if (Pets[i].Info.AI != 64) continue;
                if (((IntelligentCreatureObject)Pets[i]).petType != pType) continue;

                Enqueue(new S.ObjectChat { ObjectID = Pets[i].ObjectID, Text = message, Type = ChatType.Normal });
                return;
            }
        }

        public void StrongboxRewardItem(int boxtype)
        {
            UserItem dropItem = null;
            foreach (DropInfo drop in Envir.StrongboxDrops)
            {
                if (drop.Gold>0||!drop.isDrop())
                {
                    continue;
                }
                //只开一个出来
                ItemInfo di = drop.DropItem();
                if(di == null )
                {
                    return;
                }
                dropItem = di.CreateFreshItem();
            }

            if (dropItem == null)
            {
                ReceiveChat("什么也找不到.", ChatType.System);
                return;
            }

            if (dropItem.Info.Type == ItemType.Pets && dropItem.Info.Shape == 26)
            {
                dropItem = CreateDynamicWonderDrug(boxtype, dropItem);
            }
            else
            {
                dropItem = dropItem.Info.CreateDropItem();
            }
                

            if (FreeSpace(Info.Inventory) < 1)
            {
                ReceiveChat("背包空间不足.", ChatType.System);
                return;
            }

            if (dropItem != null) GainItem(dropItem);
        }

        public void BlackstoneRewardItem()
        {
            UserItem dropItem = null;
            foreach (DropInfo drop in Envir.BlackstoneDrops)
            {
                if (drop.Gold > 0 || !drop.isDrop())
                {
                    continue;
                }
                //只开一个出来
                ItemInfo di = drop.DropItem();
                if (di == null)
                {
                    return;
                }
                dropItem = di.CreateFreshItem();
            }
            if (FreeSpace(Info.Inventory) < 1)
            {
                ReceiveChat("背包空间不足.", ChatType.System);
                return;
            }
            if (dropItem != null) GainItem(dropItem);
        }

        private UserItem CreateDynamicWonderDrug(int boxtype, UserItem dropitem)
        {
            dropitem.CurrentDura = (ushort)1;//* 3600
            switch ((int)dropitem.Info.Effect)
            {
                case 0://exp low/med/high
                    dropitem.Luck = (sbyte)5;
                    if (boxtype > 0) dropitem.Luck = (sbyte)10;
                    if (boxtype > 1) dropitem.Luck = (sbyte)20;
                    break;
                case 1://drop low/med/high
                    dropitem.Luck = (sbyte)10;
                    if (boxtype > 0) dropitem.Luck = (sbyte)20;
                    if (boxtype > 1) dropitem.Luck = (sbyte)50;
                    break;
                case 2://hp low/med/high
                    dropitem.HP = (byte)50;
                    if (boxtype > 0) dropitem.HP = (byte)100;
                    if (boxtype > 1) dropitem.HP = (byte)200;
                    break;
                case 3://mp low/med/high
                    dropitem.MP = (byte)50;
                    if (boxtype > 0) dropitem.MP = (byte)100;
                    if (boxtype > 1) dropitem.MP = (byte)200;
                    break;
                case 4://ac low/med/high
                    dropitem.AC = (byte)1;
                    if (boxtype > 0) dropitem.AC = (byte)3;
                    if (boxtype > 1) dropitem.AC = (byte)5;
                    break;
                case 5://amc low/med/high
                    dropitem.MAC = (byte)1;
                    if (boxtype > 0) dropitem.MAC = (byte)3;
                    if (boxtype > 1) dropitem.MAC = (byte)5;
                    break;
                case 6://speed low/med/high
                    dropitem.AttackSpeed = (sbyte)2;
                    if (boxtype > 0) dropitem.AttackSpeed = (sbyte)3;
                    if (boxtype > 1) dropitem.AttackSpeed = (sbyte)4;
                    break;
            }
            //string dbg = String.Format(" Img: {0} Effect: {1} Dura: {2} Exp: {3} Drop: {3} HP: {4} MP: {5} AC: {6} MAC: {7} ASpeed: {8} BagWeight: {9}", dropitem.Image, dropitem.Info.Effect, dropitem.CurrentDura, dropitem.Luck, dropitem.HP, dropitem.MP, dropitem.AC, dropitem.MAC, dropitem.AttackSpeed, dropitem.Luck);
            //ReceiveChat(dropitem.Name + dbg, ChatType.System);
            return dropitem;
        }

        private IntelligentCreatureObject GetCreatureByName(string creaturename)
        {
            if (!CreatureSummoned || creaturename == "") return null;
            if (SummonedCreatureType == IntelligentCreatureType.None) return null;

            for (int i = 0; i < Pets.Count; i++)
            {
                if (Pets[i].Info.AI != 64) continue;
                if (((IntelligentCreatureObject)Pets[i]).petType != SummonedCreatureType) continue;

                return ((IntelligentCreatureObject)Pets[i]);
            }
            return null;
        }

        private string CreateTimeString(double secs)
        {
            TimeSpan t = TimeSpan.FromSeconds(secs);
            string answer;
            if (t.TotalMinutes < 1.0)
            {
                answer = string.Format("{0}s", t.Seconds);
            }
            else if (t.TotalHours < 1.0)
            {
                answer = string.Format("{0}m", t.Minutes);
            }
            else if (t.TotalDays < 1.0)
            {
                answer = string.Format("{0}h {1:D2}m", (int)t.TotalHours, t.Minutes);
            }
            else // t.TotalDays >= 1.0
            {
                answer = string.Format("{0}d {1}h {2:D2}m", (int)t.TotalDays, (int)t.Hours, t.Minutes);
            }
            return answer;
        }

        private void GetCreaturesInfo()
        {
            S.UpdateIntelligentCreatureList packet = new S.UpdateIntelligentCreatureList
            {
                CreatureSummoned = CreatureSummoned,
                SummonedCreatureType = SummonedCreatureType,
                PearlCount = Info.PearlCount,
            };

            for (int i = 0; i < Info.IntelligentCreatures.Count; i++)
                packet.CreatureList.Add(Info.IntelligentCreatures[i].CreateClientIntelligentCreature());

            Enqueue(packet);
        }


        #endregion

        #region Friends 好友系统

        public void AddFriend(string name, bool blocked = false)
        {
            CharacterInfo info = Envir.GetCharacterInfo(name);

            if (info == null)
            {
                ReceiveChat("玩家不存在", ChatType.System);
                return;
            }

            if (Name == name)
            {
                ReceiveChat("无法添加自己", ChatType.System);
                return;
            }

            if (Info.Friends.Any(e => e.Index == info.Index))
            {
                ReceiveChat("玩家已经加入", ChatType.System);
                return;
            }

            FriendInfo friend = new FriendInfo(info, blocked);

            Info.Friends.Add(friend);

            GetFriends();
        }

        public void RemoveFriend(ulong index)
        {
            FriendInfo friend = Info.Friends.FirstOrDefault(e => e.Index == index);

            if (friend == null)
            {
                return;
            }

            Info.Friends.Remove(friend);

            GetFriends();
        }

        public void AddMemo(ulong index, string memo)
        {
            if (memo.Length > 200) return;

            FriendInfo friend = Info.Friends.FirstOrDefault(e => e.Index == index);

            if (friend == null)
            {
                return;
            }

            friend.Memo = memo;

            GetFriends();
        }

        public void GetFriends()
        {
            List<ClientFriend> friends = new List<ClientFriend>();

            foreach (FriendInfo friend in Info.Friends)
            {
                if (friend.CreateClientFriend() != null)
                {
                    friends.Add(friend.CreateClientFriend());
                }
            }

            Enqueue(new S.FriendUpdate { Friends = friends });
        }

        #endregion

        #region ItemCollect 物品收集
        //寄存物品到NPC处
        public void DepositItemCollect(int from, int to)
        {

            S.DepositItemCollect p = new S.DepositItemCollect { From = from, To = to, Success = false };
            if (NPCPage == null || !NPCPage.Key.StartsWith(NPCObject.ItemCollectKey0.Substring(0, NPCObject.ItemCollectKey0.Length - 2), StringComparison.CurrentCultureIgnoreCase))
            {
                Enqueue(p);
                return;
            }
            NPCObject ob = null;
            for (int i = 0; i < CurrentMap.NPCs.Count; i++)
            {
                if (CurrentMap.NPCs[i].ObjectID != NPCID) continue;
                ob = CurrentMap.NPCs[i];
                break;
            }

            if (ob == null || !Functions.InRange(ob.CurrentLocation, CurrentLocation, Globals.DataRange))
            {
                Enqueue(p);
                return;
            }


            if (from < 0 || from >= Info.Inventory.Length)
            {
                Enqueue(p);
                return;
            }

            if (to < 0 || to >= Info.ItemCollect.Length)
            {
                Enqueue(p);
                return;
            }

            UserItem temp = Info.Inventory[from];

            if (temp == null)
            {
                Enqueue(p);
                return;
            }

            if (Info.ItemCollect[to] == null)
            {
                Info.ItemCollect[to] = temp;
                Info.Inventory[from] = null;
                RefreshBagWeight();

                Report.ItemMoved("DepositItemCollect", temp, MirGridType.Inventory, MirGridType.ItemCollect, from, to);

                p.Success = true;
                Enqueue(p);
                return;
            }
            Enqueue(p);

        }

        //取消物品收集，归还物品给玩家
        public void ItemCollectCancel()
        {
            for (int t = 0; t < Info.ItemCollect.Length; t++)
            {
                UserItem temp = Info.ItemCollect[t];

                if (temp == null) continue;
                bool Retrieve = false;//是否已归还

                for (int i = 6; i < Info.Inventory.Length; i++)
                {
                    if (Info.Inventory[i] != null) continue;
                    RetrieveItemCollect(t, i);
                    Retrieve = true;
                    Info.ItemCollect[t] = null;
                    Enqueue(new S.DeleteItem { UniqueID = temp.UniqueID, Count = temp.Count });
                    break;
                }


                if (!Retrieve)
                {
                    if (DropItem(temp, Settings.DropRange))
                    {
                        Enqueue(new S.DeleteItem { UniqueID = temp.UniqueID, Count = temp.Count });
                        Info.ItemCollect[t] = null;
                    }
                }
            }
        }
        //归还物品
        public void RetrieveItemCollect(int from, int to)
        {
            S.RetrieveItemCollect p = new S.RetrieveItemCollect { From = from, To = to, Success = false };

            if (from < 0 || from >= Info.ItemCollect.Length)
            {
                Enqueue(p);
                return;
            }

            if (to < 0 || to >= Info.Inventory.Length)
            {
                Enqueue(p);
                return;
            }

            UserItem temp = Info.ItemCollect[from];

            if (temp == null)
            {
                Enqueue(p);
                return;
            }

            if (temp.Weight + CurrentBagWeight > MaxBagWeight)
            {
                //ReceiveChat("太重不能获取.", ChatType.System);
                //Enqueue(p);
                //return;
            }

            if (Info.Inventory[to] == null)
            {
                Info.Inventory[to] = temp;
                Info.ItemCollect[from] = null;

                Report.ItemMoved("RetrieveItemCollect", temp, MirGridType.ItemCollect, MirGridType.Inventory, from, to);

                p.Success = true;
                RefreshBagWeight();
                Enqueue(p);

                return;
            }
            Enqueue(p);
        }
        //确认收集物品
        public void ConfirmItemCollect(byte type)
        {
            S.ConfirmItemCollect p = new S.ConfirmItemCollect { Success = false };
            if (Dead) return;
            //SMain.Enqueue("装备收集的后续处理");
            //0:装备熔炼，1：装备合成 2：装备轮回 3：装备回收 4.
            if (type == 0)
            {
                //装备熔炼,武器最大加10，饰品最大加5
                int count = 0;
                int MaxQuality = Settings.MaxQuality;//最大品质，首饰5，武器10
                UserItem[] temps = new UserItem[2];
                for (int t = 0; t < Info.ItemCollect.Length; t++)
                {
                    UserItem temp = Info.ItemCollect[t];
                    if (temp == null) continue;
                    count++;
                    if (count > 2)
                    {
                        break;
                    }
                    temps[count - 1] = temp;
                }
                if (count < 2)
                {
                    ReceiveChat("请放入2件相同的装备..", ChatType.System);
                    return;
                }
                if (count > 2)
                {
                    ReceiveChat("你放入的装备过多，请放入2件相同的装备.", ChatType.System);
                    return;
                }
                if (temps[0].ItemIndex != temps[1].ItemIndex)
                {
                    ReceiveChat("请放入2件相同的装备.", ChatType.System);
                    return;
                }
                if (temps[0].Info.Type != ItemType.Weapon && temps[0].Info.Type != ItemType.Armour && temps[0].Info.Type != ItemType.Helmet && temps[0].Info.Type != ItemType.Necklace
                    && temps[0].Info.Type != ItemType.Bracelet && temps[0].Info.Type != ItemType.Ring && temps[0].Info.Type != ItemType.Belt && temps[0].Info.Type != ItemType.Boots
                    )
                {
                    ReceiveChat("请放入武器，盔甲，首饰，鞋子，腰带等装备.", ChatType.System);
                    return;
                }

                if (temps[0].Info.Grade == ItemGrade.Ancient)
                {
                    //ReceiveChat("当前装备为远古时期遗留的先天秘宝，无法进行熔炼.", ChatType.System);
                    //return;
                }

                if (temps[0].Info.Type == ItemType.Weapon)
                {
                    MaxQuality = MaxQuality * 2;
                }



                int max = -1;
                //取出品质，灵性较大的装备
                if (temps[0].quality + temps[0].spiritual > temps[1].quality + temps[1].spiritual)
                {
                    max = 0;
                }
                if (temps[0].quality + temps[0].spiritual < temps[1].quality + temps[1].spiritual)
                {
                    max = 1;
                }
                //2件一样，则取宝石数多的
                if (max == -1 && ServerConfig.openMaxGem)
                {
                    if (temps[0].MaxGem > temps[1].MaxGem)
                    {
                        max = 0;
                    }
                    if (temps[0].MaxGem < temps[1].MaxGem)
                    {
                        max = 1;
                    }
                }
                //2件一样，则取增加属性多的
                if (max == -1)
                {
                    if (temps[0].AddedVue > temps[1].AddedVue)
                    {
                        max = 0;
                    }
                    else
                    {
                        max = 1;
                    }
                }
                //判断是否达到最大可熔炼值
                if (temps[max].quality >= MaxQuality)
                {
                    ReceiveChat(String.Format("当前装备品质已满，无法再次熔炼."), ChatType.System);
                    return;
                }

                //检查费用(封顶10万)
                uint cost = (uint)((temps[0].Price()) * 2);
                if (cost > 100000)
                {
                    cost = 100000;
                }
                if (cost > Account.Gold)
                {
                    ReceiveChat(String.Format("你没有足够的金币来熔炼装备."), ChatType.System);
                    return;
                }
                LoseGold(cost);

               

                //删除装备
                for (int t = 0; t < Info.ItemCollect.Length; t++)
                {
                    Info.ItemCollect[t] = null;
                }
                //删除装备
                Enqueue(new S.DeleteItem { UniqueID = temps[0].UniqueID, Count = temps[0].Count });
                Enqueue(new S.DeleteItem { UniqueID = temps[1].UniqueID, Count = temps[1].Count });
                //50%的几率加品质，30的几率增加灵性，20的几率什么也不加
                temps[max].quality++;
                ReceiveChat(String.Format("恭喜你，您熔炼的装备增加了1点品质."), ChatType.System);
                int rd = RandomUtils.Next(100);
                if (rd < 70)
                {
                    temps[max].spiritual++;
                    ReceiveChat(String.Format("恭喜你，您熔炼的装备增加了1点灵性."), ChatType.System);
                }

                p.Success = true;
                Enqueue(p);
                //获得装备
                GainItem(temps[max]);
            }
            //装备合成
            if (type == 1)
            {
                //装备合成（3合1，目前只做宝石哈）,这个考虑下，估计放工艺锻造那边比较好
                // int count = 0;
                //装备合成
                int count = 0;
                UserItem[] temps = new UserItem[2];
                string erroritem = "";
                for (int t = 0; t < Info.ItemCollect.Length; t++)
                {
                    UserItem temp = Info.ItemCollect[t];
                    if (temp == null) continue;
                    count++;
                    if (temp.Info.Type != ItemType.Stone)
                    {
                        erroritem += temp.Name + ",";
                    }
                    if (count > 2)
                    {
                        break;
                    }
                    temps[count - 1] = temp;
                }
                if (erroritem.Length > 2)
                {
                    ReceiveChat("目前只支持宝石合成，以下装备请取回...", ChatType.System);
                    ReceiveChat(erroritem, ChatType.System);
                    return;
                }
                if (count < 2)
                {
                    ReceiveChat("请放入2件相同的宝石..", ChatType.System);
                    return;
                }
                if (count > 2)
                {
                    ReceiveChat("你放入的宝石过多，请放入2件宝石的装备.", ChatType.System);
                    return;
                }
                if (temps[0].ItemIndex != temps[1].ItemIndex)
                {
                    ReceiveChat("请放入2件相同的宝石.", ChatType.System);
                    return;
                }
                if (temps[0].Name.Contains("完美"))
                {
                    ReceiveChat("当前宝石已是完美宝石，无法再合成.", ChatType.System);
                    return;
                }
                //取下一级别宝石
                ItemInfo item = ItemInfo.getItem(temps[0].ItemIndex+1);
                if (item == null|| !item.GroupName.StartsWith("宝石"))
                {
                    ReceiveChat("合成失败，无法合成此类宝石.", ChatType.System);
                    return;
                }

                //检查费用(封顶10万)
                uint cost = (uint)((temps[0].Price()) * 2);
                if (cost > 200000)
                {
                    cost = 200000;
                }
                if (cost > Account.Gold)
                {
                    ReceiveChat(String.Format("你没有足够的金币进行宝石合成."), ChatType.System);
                    return;
                }
                LoseGold(cost);

                //删除装备
                for (int t = 0; t < Info.ItemCollect.Length; t++)
                {
                    Info.ItemCollect[t] = null;
                }
                //删除装备
                Enqueue(new S.DeleteItem { UniqueID = temps[0].UniqueID, Count = temps[0].Count });
                Enqueue(new S.DeleteItem { UniqueID = temps[1].UniqueID, Count = temps[1].Count });
                p.Success = true;
                Enqueue(p);
                //获得装备
                ReceiveChat("合成成功..", ChatType.System);
                GainItem(item.CreateFreshItem());
            }
            //装备轮回
            if (type == 2)
            {
                if (Info.SaItem != null)
                {
                    ReceiveChat("您当前已有一件装备在轮回中,先去轮回中寻找您的装备吧", ChatType.System);
                    return;
                }
                //装备轮回，
                int count = 0;
                UserItem[] temps = new UserItem[2];
                for (int t = 0; t < Info.ItemCollect.Length; t++)
                {
                    UserItem temp = Info.ItemCollect[t];
                    if (temp == null) continue;
                    count++;
                    if (count > 2)
                    {
                        break;
                    }
                    if (temp.spiritual > 0 && temp.spiritual>temp.samsaracount)
                    {
                        temps[0] = temp;
                        continue;
                    }
                    if(temp.Info.Type == ItemType.Quest && temp.Info.Shape>=1 && temp.Info.Shape <= 5)
                    {
                        temps[1] = temp;
                    }
                }
                if (count < 2)
                {
                    ReceiveChat("请放入需要轮回的装备和轮回符纸..", ChatType.System);
                    return;
                }
                if (count > 2)
                {
                    ReceiveChat("你放入的装备过多，请放入需要轮回的装备和轮回符纸.", ChatType.System);
                    return;
                }
                if (temps[0] == null)
                {
                    ReceiveChat("请放入具有灵性，并且轮回属性未满的装备..", ChatType.System);
                    return;
                }
                if (temps[1] == null)
                {
                    ReceiveChat("请放入轮回符纸，轮回符纸可以护送您的装备进入轮回中..", ChatType.System);
                    return;
                }
                //衣服，头盔只能放防御类符纸
                //temps[0].Info.Type != ItemType.Armour && temps[0].Info.Type != ItemType.Helmet 
                if((temps[0].Info.Type== ItemType.Armour|| temps[0].Info.Type== ItemType.Helmet) && (temps[1].Info.Shape !=  (byte)AwakeType.AC && temps[1].Info.Shape != (byte)AwakeType.MAC))
                {
                    ReceiveChat("衣服，头盔只能放防御类符纸", ChatType.System);
                    return;
                }

                //武器，项链，只能放攻击类符纸
                if ((temps[0].Info.Type == ItemType.Weapon || temps[0].Info.Type == ItemType.Necklace) && (temps[1].Info.Shape == (byte)AwakeType.AC || temps[1].Info.Shape == (byte)AwakeType.MAC))
                {
                    ReceiveChat("武器，项链，只能放攻击类符纸", ChatType.System);
                    return;
                }


                //每日轮回次数限制(重启清空这个次数限制)
                string satimekey = "SA_ITEM_TIME_" + Envir.Now.DayOfYear;
                int satime = Info.getTempValue(satimekey);
                if (satime >= 3)
                {
                    ReceiveChat("每日最多进行3次轮回...", ChatType.System);
                    return;
                }
                Info.putTempValue(satimekey, satime + 1);
                

                //删除装备
                for (int t = 0; t < Info.ItemCollect.Length; t++)
                {
                    Info.ItemCollect[t] = null;
                }
                //删除装备
                Enqueue(new S.DeleteItem { UniqueID = temps[0].UniqueID, Count = temps[0].Count });
                Enqueue(new S.DeleteItem { UniqueID = temps[1].UniqueID, Count = temps[1].Count });
                p.Success = true;
                Enqueue(p);
                //获得装备
                temps[0].samsaratype = (byte)temps[1].Info.Shape;
                Info.SaItem = temps[0];
                //轮回类型分类
                if (RandomUtils.Next(100) < 30)
                {
                    Info.SaItemType = 0;
                }
                else
                {
                    Info.SaItemType = (byte)RandomUtils.Next(1, 6);
                }

                //地图没开的，不投放
                Map lunhmap = null;
                if (Info.SaItemType == 2)
                {
                    lunhmap = Envir.GetMapByNameAndInstance("Fox01");
                    if (lunhmap == null || !lunhmap.MapOpen || Level < lunhmap.Info.minLevel)
                    {
                        Info.SaItemType = 0;
                    }
                }
                
                if (Info.SaItemType == 3)
                {
                    lunhmap = Envir.GetMapByNameAndInstance("gumi101");
                    if (lunhmap == null || !lunhmap.MapOpen || Level < lunhmap.Info.minLevel)
                    {
                        Info.SaItemType = 0;
                    }
                }
                if (Info.SaItemType == 4)
                {
                    lunhmap = Envir.GetMapByNameAndInstance("R01");
                    if (lunhmap == null || !lunhmap.MapOpen|| Level < lunhmap.Info.minLevel)
                    {
                        Info.SaItemType = 0;
                    }
                }
                if (Info.SaItemType == 5 )
                {
                    lunhmap = Envir.GetMapByNameAndInstance("UMM");
                    if (lunhmap == null || !lunhmap.MapOpen || Level < lunhmap.Info.minLevel)
                    {
                        Info.SaItemType = 0;
                    }
                }

                switch (Info.SaItemType)
                {
                    case 0:
                        ReceiveChat("您的装备已投入[轮回地狱]中，如果您有机会再获得它，它将突破自我..", ChatType.System);
                        break;

                    case 1:
                        ReceiveChat("您的装备已投入 [魔龙] 中进行轮回，如果您有机会再获得它，它将突破自我..", ChatType.System);
                        break;

                    case 2:
                        ReceiveChat("您的装备已投入 [狐狸] 中进行轮回，如果您有机会再获得它，它将突破自我..", ChatType.System);
                        break;
                    case 3:
                        ReceiveChat("您的装备已投入 [月氏] 中进行轮回，如果您有机会再获得它，它将突破自我..", ChatType.System);
                        break;
                    case 4:
                        ReceiveChat("您的装备已投入 [洪洞] 中进行轮回，如果您有机会再获得它，它将突破自我..", ChatType.System);
                        break;
                    case 5:
                        ReceiveChat("您的装备已投入 [石槽] 中进行轮回，如果您有机会再获得它，它将突破自我..", ChatType.System);
                        break;
                }

                //GainItem(temps[max]);
            }
            //装备回收,只开放祖玛套，沃玛套回收
            if (type == 3)
            {
                //有人到达45级，自动开放回收
                if (Envir.MaxLevel < 45)
                {
                    //ReceiveChat(String.Format("当前还未开放回收功能"), ChatType.System);
                    //return;
                }

                if (Level < 20)
                {
                    ReceiveChat(String.Format("等级低于20级，不能使用回收功能"), ChatType.System);
                    return;
                }
            
                //45级后，3级的等级保护
                int maxLevel = 45;
                if(maxLevel < Envir.MaxLevel - 3)
                {
                    maxLevel = Envir.MaxLevel - 3;
                }
                if (Level > maxLevel && Level < 55)
                {
                    //ReceiveChat(String.Format("您的等级过高，目前高于{0}级不能使用回收功能，请支援下新人吧...", maxLevel), ChatType.System);
                    //return;
                }
                //可回收装备列表
                int cancount = 0;
                string erroritem = "";
                uint rmoney = 0;
                for (int t = 0; t < Info.ItemCollect.Length; t++)
                {
                    UserItem temp = Info.ItemCollect[t];
                    if (temp == null) continue;
                    if(temp.Info.Type > ItemType.Stone )
                    {
                        erroritem += temp.Name + ",";
                        continue;
                    }
                    bool canRecovery = false;
                    if (temp.Info.Set > 0)
                    {
                        //canRecovery = true;
                    }
                    //1.必须是装备，2.装备等级大于等于30级，3.装备等级和玩家等级匹配
                    if (temp.isEquip() && temp.Info.eatLevel>=30 )
                    {
                        canRecovery = true;
                        rmoney = temp.Info.Price * temp.Count;
                        if (temp.quality > 0)
                        {
                            rmoney = rmoney * temp.quality;
                        }
                    }

                    if (temp.Info.RequiredAmount > 30)
                    {
                        //canRecovery = true;
                    }
                    if (!canRecovery)
                    {
                        erroritem += temp.Name + ",";
                    }
                    else
                    {
                        cancount++;
                    }
                }
                if (erroritem.Length > 2)
                {
                    ReceiveChat("以下装备不符合回收要求，请取回...", ChatType.System);
                    ReceiveChat(erroritem, ChatType.System);
                    return;
                }

                if (!Info.RecoveryMoney((int)rmoney, false))
                {
                    ReceiveChat("达到当日可回收上限，请次日再来回收...", ChatType.System);
                    return;
                }
                //加到上限中
                Info.RecoveryMoney((int)rmoney, true);
                p.Success = true;
                Enqueue(p);
                for (int t = 0; t < Info.ItemCollect.Length; t++)
                {
                    UserItem temp = Info.ItemCollect[t];
                    if (temp == null) continue;

                    uint exp = temp.Info.Price * temp.Count * (uint)Settings.LevelGoldExpList[Level];
                    int expPoint = 0;
                    if (Level < temp.Info.eatLevel + 8 || !Settings.ExpMobLevelDifference)
                    {
                        expPoint = (int)exp;
                    }
                    else
                    {
                        //如果玩家等级大于怪物等级10级，则逐级递减经验，直到大于怪物25级，就基本没经验了.
                        expPoint = (int)exp - (int)Math.Round(Math.Max(exp / 10, 1) * ((double)Level - (temp.Info.eatLevel + 8)));
                    }
                    if(expPoint < exp / 4)
                    {
                        expPoint = (int)exp / 4;
                    }
                    //品质影响回收
                    if (temp.quality > 0)
                    {
                        expPoint = expPoint * temp.quality;
                    }

                    GainExp((uint)expPoint);
                    //移除装备
                    Info.ItemCollect[t] = null;
                    Enqueue(new S.DeleteItem { UniqueID = temp.UniqueID, Count = temp.Count });
                }
            }

            //装备分解
            if (type == 4)
            {
                string erroritem = "";
                uint cost = 0;
                for (int t = 0; t < Info.ItemCollect.Length; t++)
                {
                    UserItem temp = Info.ItemCollect[t];
                    if (temp == null) continue;
                    if (temp.Info.MaxMaterial < 1)
                    {
                        erroritem += temp.Name + ",";
                        continue;
                    }
                    cost += temp.Price();
                }
                if (erroritem.Length > 2)
                {
                    ReceiveChat("以下装备不符合分解要求，请取回...", ChatType.System);
                    ReceiveChat(erroritem, ChatType.System);
                    return;
                }
                p.Success = true;
                Enqueue(p);
                //检查费用
                if (cost > Account.Gold)
                {
                    ReceiveChat(String.Format("你没有足够的金币来进行装备分解."), ChatType.System);
                    return;
                }
                LoseGold(cost);

                int st_count = 0;//获得的原石数量
                for (int t = 0; t < Info.ItemCollect.Length; t++)
                {
                    UserItem temp = Info.ItemCollect[t];
                    if (temp == null||temp.Info==null) continue;
                    st_count+=RandomUtils.Next(temp.Info.MinMaterial, temp.Info.MaxMaterial + 1);
                    //移除装备
                    Info.ItemCollect[t] = null;
                    Enqueue(new S.DeleteItem { UniqueID = temp.UniqueID, Count = temp.Count });
                }
                if (st_count > 0)
                {
                    ReceiveChat($"分解装备成功，你获得{st_count}个混沌原石", ChatType.System);
                    for(int i=0;i< st_count; i++)
                    {
                        GainItem(ItemInfo.getItem("混沌原石").CreateFreshItem());
                    }
                }
                else
                {
                    ReceiveChat("分解装备失败，你什么也没获得", ChatType.System);
                }
            }
        }

        #endregion

        #region Refining 武器升级

        public void DepositRefineItem(int from, int to)
        {

            S.DepositRefineItem p = new S.DepositRefineItem { From = from, To = to, Success = false };

            if (NPCPage == null || !String.Equals(NPCPage.Key, NPCObject.RefineKey, StringComparison.CurrentCultureIgnoreCase))
            {
                Enqueue(p);
                return;
            }
            NPCObject ob = null;
            for (int i = 0; i < CurrentMap.NPCs.Count; i++)
            {
                if (CurrentMap.NPCs[i].ObjectID != NPCID) continue;
                ob = CurrentMap.NPCs[i];
                break;
            }

            if (ob == null || !Functions.InRange(ob.CurrentLocation, CurrentLocation, Globals.DataRange))
            {
                Enqueue(p);
                return;
            }


            if (from < 0 || from >= Info.Inventory.Length)
            {
                Enqueue(p);
                return;
            }

            if (to < 0 || to >= Info.Refine.Length)
            {
                Enqueue(p);
                return;
            }

            UserItem temp = Info.Inventory[from];

            if (temp == null)
            {
                Enqueue(p);
                return;
            }

            if (Info.Refine[to] == null)
            {
                Info.Refine[to] = temp;
                Info.Inventory[from] = null;
                RefreshBagWeight();

                Report.ItemMoved("DepositRefineItems", temp, MirGridType.Inventory, MirGridType.Refine, from, to);

                p.Success = true;
                Enqueue(p);
                return;
            }
            Enqueue(p);

        }
        public void RetrieveRefineItem(int from, int to)
        {
            S.RetrieveRefineItem p = new S.RetrieveRefineItem { From = from, To = to, Success = false };

            if (from < 0 || from >= Info.Refine.Length)
            {
                Enqueue(p);
                return;
            }

            if (to < 0 || to >= Info.Inventory.Length)
            {
                Enqueue(p);
                return;
            }

            UserItem temp = Info.Refine[from];

            if (temp == null)
            {
                Enqueue(p);
                return;
            }

            if (temp.Weight + CurrentBagWeight > MaxBagWeight)
            {
                ReceiveChat("太重不能获取.", ChatType.System);
                Enqueue(p);
                return;
            }

            if (Info.Inventory[to] == null)
            {
                Info.Inventory[to] = temp;
                Info.Refine[from] = null;

                Report.ItemMoved("TakeBackRefineItems", temp, MirGridType.Refine, MirGridType.Inventory, from, to);

                p.Success = true;
                RefreshBagWeight();
                Enqueue(p);

                return;
            }
            Enqueue(p);
        }
        public void RefineCancel()
        {
            for (int t = 0; t < Info.Refine.Length; t++)
            {
                UserItem temp = Info.Refine[t];

                if (temp == null) continue;

                for (int i = 0; i < Info.Inventory.Length; i++)
                {
                    if (Info.Inventory[i] != null) continue;

                    //Put item back in inventory
                    if (CanGainItem(temp))
                    {
                        RetrieveRefineItem(t, i);
                    }
                    else //Drop item on floor if it can no longer be stored
                    {
                        if (DropItem(temp, Settings.DropRange))
                        {
                            Enqueue(new S.DeleteItem { UniqueID = temp.UniqueID, Count = temp.Count });
                        }
                    }

                    Info.Refine[t] = null;

                    break;
                }
            }
        }

        //装备升级，目前只开放武器的升级
        public void RefineItem(ulong uniqueID)
        {
            Enqueue(new S.RepairItem { UniqueID = uniqueID }); //CHECK THIS.

            if (Dead) return;

            if (NPCPage == null || (!String.Equals(NPCPage.Key, NPCObject.RefineKey, StringComparison.CurrentCultureIgnoreCase))) return;

            int index = -1;

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                if (Info.Inventory[i] == null || Info.Inventory[i].UniqueID != uniqueID) continue;
                index = i;
                break;
            }

            if (index == -1) return;

            if (Info.Inventory[index].RefineAdded != 0)
            {
                ReceiveChat(String.Format("{0} 需要检查之前，你可以尝试重新提炼它.", Info.Inventory[index].FriendlyName), ChatType.System);
                return;
            }

            if ((Info.Inventory[index].Info.Type != ItemType.Weapon) && (Settings.OnlyRefineWeapon))
            {
                ReceiveChat(String.Format("{0} 无法提炼.", Info.Inventory[index].FriendlyName), ChatType.System);
                return;
            }

            if (Info.Inventory[index].Info.Bind.HasFlag(BindMode.DontUpgrade))
            {
                ReceiveChat(String.Format("{0} 无法提炼.", Info.Inventory[index].FriendlyName), ChatType.System);
                return;
            }

            if (Info.Inventory[index].RentalInformation != null && Info.Inventory[index].RentalInformation.BindingFlags.HasFlag(BindMode.DontUpgrade))
            {
                ReceiveChat(String.Format("{0} 无法提炼.", Info.Inventory[index].FriendlyName), ChatType.System);
                return;
            }


            if (index == -1) return;




            //CHECK GOLD HERE 费用
            uint cost = (uint)((Info.Inventory[index].Info.RequiredAmount * 10) * Settings.RefineCost);

            if (cost > Account.Gold)
            {
                ReceiveChat(String.Format("你没有足够的金币来提炼你的 {0}.", Info.Inventory[index].FriendlyName), ChatType.System);
                return;
            }

            Account.Gold -= cost;
            Enqueue(new S.LoseGold { Gold = cost });

            //START OF FORMULA

            Info.CurrentRefine = Info.Inventory[index];
            Info.Inventory[index] = null;
            Info.CollectTime = (Envir.Time + (Settings.RefineTime * Settings.Minute));
            Enqueue(new S.RefineItem { UniqueID = uniqueID });


            short OrePurity = 0;//矿石的总持久
            byte OreAmount = 0;//矿石的数量
            byte ItemAmount = 0;//饰品的总数
            short TotalDC = 0;//饰品的总攻击
            short TotalMC = 0;//饰品的总魔法
            short TotalSC = 0;//饰品的总道术
            short RequiredLevel = 0;//饰品的等级
            short Durability = 0;//饰品的最大持久
            short CurrentDura = 0;//饰品的当前持久
            //short AddedStats = 0;//加星数，增加的攻击，魔法，道术的数量
            UserItem Ingredient;

            for (int i = 0; i < Info.Refine.Length; i++)
            {
                Ingredient = Info.Refine[i];

                if (Ingredient == null) continue;
                if (Ingredient.Info.Type == ItemType.Weapon)
                {
                    Info.Refine[i] = null;
                    continue;
                }

                if ((Ingredient.Info.MaxDC > 0) || (Ingredient.Info.MaxMC > 0) || (Ingredient.Info.MaxSC > 0))
                {
                    TotalDC += (short)(Ingredient.Info.MinDC + Ingredient.Info.MaxDC + Ingredient.DC);
                    TotalMC += (short)(Ingredient.Info.MinMC + Ingredient.Info.MaxMC + Ingredient.MC);
                    TotalSC += (short)(Ingredient.Info.MinSC + Ingredient.Info.MaxSC + Ingredient.SC);
                    RequiredLevel += Ingredient.Info.RequiredAmount;
                    if (Math.Round(Ingredient.MaxDura / 1000M) == Math.Round(Ingredient.Info.Durability / 1000M)) Durability++;
                    if (Math.Round(Ingredient.CurrentDura / 1000M) == Math.Round(Ingredient.MaxDura / 1000M)) CurrentDura++;
                    ItemAmount++;
                }

                if (Ingredient.Info.FriendlyName == Settings.RefineOreName)
                {
                    OrePurity += (short)Math.Round(Ingredient.CurrentDura / 1000M);
                    OreAmount++;
                }

                Info.Refine[i] = null;
            }
            //饰品错误，直接失败
            if ((TotalDC == 0) && (TotalMC == 0) && (TotalSC == 0))
            {
                Info.CurrentRefine.RefinedValue = RefinedValue.None;
                Info.CurrentRefine.RefineAdded = Settings.RefineIncrease;
                if (Settings.RefineTime == 0)
                {
                    CollectRefine();
                }
                else
                {
                    ReceiveChat(String.Format("{0} 正在被提炼, 请在 {1} 分钟后取回.", Info.CurrentRefine.FriendlyName, Settings.RefineTime), ChatType.System);
                }
                return;
            }
            //没有矿石，直接失败
            if (OreAmount == 0)
            {
                Info.CurrentRefine.RefinedValue = RefinedValue.None;
                Info.CurrentRefine.RefineAdded = Settings.RefineIncrease;
                if (Settings.RefineTime == 0)
                {
                    CollectRefine();
                }
                else
                {
                    ReceiveChat(String.Format("{0} 正在被提炼, 请在 {1} 分钟后取回.", Info.CurrentRefine.FriendlyName, Settings.RefineTime), ChatType.System);
                }
                return;
            }


            short RefineStat = 0;//饰品的总攻击/魔法/道术

            if ((TotalDC > TotalMC) && (TotalDC > TotalSC))
            {
                Info.CurrentRefine.RefinedValue = RefinedValue.DC;
                RefineStat = TotalDC;
            }

            if ((TotalMC > TotalDC) && (TotalMC > TotalSC))
            {
                Info.CurrentRefine.RefinedValue = RefinedValue.MC;
                RefineStat = TotalMC;
            }

            if ((TotalSC > TotalDC) && (TotalSC > TotalMC))
            {
                Info.CurrentRefine.RefinedValue = RefinedValue.SC;
                RefineStat = TotalSC;
            }

            Info.CurrentRefine.RefineAdded = Settings.RefineIncrease;

            //饰品占比30%
            int ItemSuccess = 0; //Chance out of 35%

            ItemSuccess += (RefineStat * 5) - Info.CurrentRefine.Info.RequiredAmount;
            ItemSuccess += 5;
            if (ItemSuccess > 10) ItemSuccess = 10;
            if (ItemSuccess < 0) ItemSuccess = 0; //10%


            if ((RequiredLevel / ItemAmount) > (Info.CurrentRefine.Info.RequiredAmount - 5)) ItemSuccess += 10; //20%
            if (Durability == ItemAmount) ItemSuccess += 10; //30%
            //if (CurrentDura == ItemAmount) ItemSuccess += 5; //35%

            //矿石占比30%
            int OreSuccess = 0; //Chance out of 35%

            if (OreAmount >= ItemAmount) OreSuccess += 15; //15%
            if ((OrePurity / OreAmount) >= (RefineStat / ItemAmount)) OreSuccess += 15; //30%
            //if (OrePurity == RefineStat) OreSuccess += 5; //35%

            int LuckSuccess = 0; //Chance out of 10%
            LuckSuccess = (Info.CurrentRefine.Luck + 5);
            if (LuckSuccess > 10) LuckSuccess = 10;
            if (LuckSuccess < 0) LuckSuccess = 0;


            int BaseSuccess = Settings.RefineBaseChance; //20% as standard


            int SuccessChance = (ItemSuccess + OreSuccess + LuckSuccess + BaseSuccess + Info.CurrentRefine.quality);//

            byte AddedStats = (byte)(Info.CurrentRefine.DC + Info.CurrentRefine.MC + Info.CurrentRefine.SC);
            //次数不受其他任何条件干扰
            if(AddedStats-5 > Info.CurrentRefine.RefineTime)
            {
                //Info.CurrentRefine.RefineTime = (byte)(AddedStats - 5);
            }

            AddedStats = Info.CurrentRefine.RefineTime;
            if (Info.CurrentRefine.Info.Type == ItemType.Weapon) AddedStats = (byte)(AddedStats * Settings.RefineWepStatReduce);
            else AddedStats = (byte)(AddedStats * Settings.RefineItemStatReduce);
            if (AddedStats > 70) AddedStats = 70;

            SuccessChance -= AddedStats;

            //保底等于基础成功率10%
            if (SuccessChance < 10)
            {
                SuccessChance = 10;
            }
            //升级失败
            if (RandomUtils.Next(1, 100) > SuccessChance)
            {
                Info.CurrentRefine.RefinedValue = RefinedValue.None;
            }
            else//成功(升级次数加1)
            {
                Info.CurrentRefine.RefineTime++;
                //爆点数(爆点不算入次数)
                if (RandomUtils.Next(1, 100) < Settings.RefineCritChance)
                {
                    Info.CurrentRefine.RefineAdded = (byte)(Info.CurrentRefine.RefineAdded * Settings.RefineCritIncrease);

                    //Info.CurrentRefine.RefineTime++;
                }
            }
            
            //END OF FORMULA (SET REFINEDVALUE TO REFINEDVALUE.NONE) REFINEADDED SHOULD BE > 0

            if (Settings.RefineTime == 0)
            {
                CollectRefine();
            }
            else
            {
                ReceiveChat(String.Format("{0} 正在被提炼, 请在 {1} 分钟后取回..", Info.CurrentRefine.FriendlyName, Settings.RefineTime), ChatType.System);
            }
        }
        //取回升级的物品
        public void CollectRefine()
        {
            S.NPCCollectRefine p = new S.NPCCollectRefine { Success = false };

            if (Info.CurrentRefine == null)
            {
                ReceiveChat("您目前没有提炼任何物品.", ChatType.System);
                Enqueue(p);
                return;
            }

            if (Info.CollectTime > Envir.Time)
            {
                ReceiveChat(string.Format("{0} 正在被提炼, 请在 {1} 分钟后取回.", Info.CurrentRefine.FriendlyName, ((Info.CollectTime - Envir.Time) / Settings.Minute)), ChatType.System);
                Enqueue(p);
                return;
            }


            if (Info.CurrentRefine.Info.Weight + CurrentBagWeight > MaxBagWeight)
            {
                ReceiveChat(string.Format("{0} 太重不能取回，在减少你的背包重量后再试一次.", Info.CurrentRefine.FriendlyName), ChatType.System);
                Enqueue(p);
                return;
            }

            int index = -1;

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                if (Info.Inventory[i] != null) continue;
                index = i;
                break;
            }

            if (index == -1)
            {
                ReceiveChat("你的背包没有足够的空间, 腾出一些空间再试一次.", ChatType.System);
                Enqueue(p);
                return;
            }

            ReceiveChat(String.Format("你的物品已经归还给你."), ChatType.System);
            p.Success = true;

            GainItem(Info.CurrentRefine);

            Info.CurrentRefine = null;
            Info.CollectTime = 0;
            Enqueue(p);
        }
        //检查升级的物品是否升级成功
        public void CheckRefine(ulong uniqueID)
        {
            //Enqueue(new S.RepairItem { UniqueID = uniqueID });

            if (Dead) return;

            if (NPCPage == null || (!String.Equals(NPCPage.Key, NPCObject.RefineCheckKey, StringComparison.CurrentCultureIgnoreCase))) return;

            UserItem temp = null;

            int index = -1;

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                temp = Info.Inventory[i];
                if (temp == null || temp.UniqueID != uniqueID) continue;
                index = i;
                break;
            }

            if (index == -1) return;

            if (Info.Inventory[index].RefineAdded == 0)
            {
                ReceiveChat(String.Format("{0} 不需要检查，因为它还没有被精炼.", Info.Inventory[index].FriendlyName), ChatType.System);
                return;
            }

            
            if ((Info.Inventory[index].RefinedValue == RefinedValue.DC) && (Info.Inventory[index].RefineAdded > 0))
            {
                ReceiveChat(String.Format("祝贺你, 你的物品 {0} 获得 +{1} 攻击.", Info.Inventory[index].FriendlyName, Info.Inventory[index].RefineAdded), ChatType.System);
                Info.Inventory[index].DC = (byte)Math.Min(byte.MaxValue, Info.Inventory[index].DC + Info.Inventory[index].RefineAdded);
                Info.Inventory[index].RefineAdded = 0;
                Info.Inventory[index].RefinedValue = RefinedValue.None;
            }
            else if ((Info.Inventory[index].RefinedValue == RefinedValue.MC) && (Info.Inventory[index].RefineAdded > 0))
            {
                ReceiveChat(String.Format("祝贺你, 你的物品 {0} 获得 +{1} 魔法.", Info.Inventory[index].FriendlyName, Info.Inventory[index].RefineAdded), ChatType.System);
                Info.Inventory[index].MC = (byte)Math.Min(byte.MaxValue, Info.Inventory[index].MC + Info.Inventory[index].RefineAdded);
                Info.Inventory[index].RefineAdded = 0;
                Info.Inventory[index].RefinedValue = RefinedValue.None;
            }
            else if ((Info.Inventory[index].RefinedValue == RefinedValue.SC) && (Info.Inventory[index].RefineAdded > 0))
            {
                ReceiveChat(String.Format("祝贺你, 你的物品 {0} 获得 +{1} 道术.", Info.Inventory[index].FriendlyName, Info.Inventory[index].RefineAdded), ChatType.System);
                Info.Inventory[index].SC = (byte)Math.Min(byte.MaxValue, Info.Inventory[index].SC + Info.Inventory[index].RefineAdded);
                Info.Inventory[index].RefineAdded = 0;
                Info.Inventory[index].RefinedValue = RefinedValue.None;
            }
            else if ((Info.Inventory[index].RefinedValue == RefinedValue.None) && (Info.Inventory[index].RefineAdded > 0))
            {
                if(Info.Inventory[index].RefineTime< Info.Inventory[index].quality)
                {
                    ReceiveChat(String.Format("非常遗憾, 你的物品 {0} 升级失败.", Info.Inventory[index].FriendlyName), ChatType.System);
                    Info.Inventory[index].RefineAdded = 0;
                    Info.Inventory[index].RefinedValue = RefinedValue.None;
                }
                else
                {
                    ReceiveChat(String.Format("的物品 {0} 在检查中粉碎.", Info.Inventory[index].FriendlyName), ChatType.System);
                    Enqueue(new S.RefineItem { UniqueID = Info.Inventory[index].UniqueID });
                    Info.Inventory[index] = null;
                    return;
                }
            }

            Enqueue(new S.ItemUpgraded { Item = Info.Inventory[index] });
            return;
        }

        #endregion 

        #region Relationship 关系，结婚
        //离婚
        public void NPCDivorce()
        {
            if (Info.Married == 0)
            {
                ReceiveChat(string.Format("你还没结婚."), ChatType.System);
                return;
            }

            CharacterInfo Lover = Envir.GetCharacterInfo(Info.Married);
            PlayerObject Player = Envir.GetPlayer(Lover.Name);

            Buff buff = Buffs.Where(e => e.Type == BuffType.RelationshipEXP).FirstOrDefault();
            if (buff != null)
            {
                RemoveBuff(BuffType.RelationshipEXP);
                Player.RemoveBuff(BuffType.RelationshipEXP);
            }

            Info.Married = 0;
            Info.MarriedDate = DateTime.Now;

            if (Info.Equipment[(int)EquipmentSlot.RingL] != null)
            {
                Info.Equipment[(int)EquipmentSlot.RingL].WeddingRing = -1;
                Enqueue(new S.RefreshItem { Item = Info.Equipment[(int)EquipmentSlot.RingL] });
            }


            GetRelationship(false);
            
            Lover.Married = 0;
            Lover.MarriedDate = DateTime.Now;
            if (Lover.Equipment[(int)EquipmentSlot.RingL] != null)
                Lover.Equipment[(int)EquipmentSlot.RingL].WeddingRing = -1;

            if (Player != null)
            {
                Player.GetRelationship(false);
                Player.ReceiveChat(string.Format("你刚刚离婚了"), ChatType.System);
                if (Player.Info.Equipment[(int)EquipmentSlot.RingL] != null)
                    Player.Enqueue(new S.RefreshItem { Item = Player.Info.Equipment[(int)EquipmentSlot.RingL] });
            }
        }

        public bool CheckMakeWeddingRing()
        {
            if (Info.Married == 0)
            {
                ReceiveChat(string.Format("你需要结婚才能戴结婚戒指."), ChatType.System);
                return false;
            }

            if (Info.Equipment[(int)EquipmentSlot.RingL] == null)
            {
                ReceiveChat(string.Format("你需要在左手手指上戴结婚戒指."), ChatType.System);
                return false;
            }

            if (Info.Equipment[(int)EquipmentSlot.RingL].WeddingRing != -1)
            {
                ReceiveChat(string.Format("你已经戴结婚戒指了."), ChatType.System);
                return false;
            }

            if (Info.Equipment[(int)EquipmentSlot.RingL].Info.Bind.HasFlag(BindMode.NoWeddingRing))
            {
                ReceiveChat(string.Format("不能使用这种类型的戒指."), ChatType.System);
                return false;
            }

            return true;
        }
        //制作结婚戒指
        public void MakeWeddingRing()
        {
            if (CheckMakeWeddingRing())
            {
                Info.Equipment[(int)EquipmentSlot.RingL].WeddingRing = (long)Info.Married;
                Enqueue(new S.RefreshItem { Item = Info.Equipment[(int)EquipmentSlot.RingL] });
            }
        }
        //替换结婚戒指
        public void ReplaceWeddingRing(ulong uniqueID)
        {
            if (Dead) return;

            if (NPCPage == null || (!String.Equals(NPCPage.Key, NPCObject.ReplaceWedRingKey, StringComparison.CurrentCultureIgnoreCase))) return;

            UserItem temp = null;
            UserItem CurrentRing = Info.Equipment[(int)EquipmentSlot.RingL];

            if (CurrentRing == null || CurrentRing.WeddingRing == -1)
            {
                //ReceiveChat(string.Format("你没有戴戒指来升级."), ChatType.System);
                //return;
            }

 
            int index = -1;

            for (int i = 0; i < Info.Inventory.Length; i++)
            {
                temp = Info.Inventory[i];
                if (temp == null || temp.UniqueID != uniqueID) continue;
                index = i;
                break;
            }

            if (index == -1)
            {
                ReceiveChat(string.Format("找不到合适的戒指."), ChatType.System);
                return;
            }

                

            temp = Info.Inventory[index];


            if (temp.Info.Type != ItemType.Ring)
            {
                ReceiveChat(string.Format("你不能用这个物品来代替结婚戒指."), ChatType.System);
                return;
            }

            if (!CanEquipItem(temp, (int)EquipmentSlot.RingL))
            {
                ReceiveChat(string.Format("当前戒指你无法使用."), ChatType.System);
                return;
            }

            if (temp.Info.Bind.HasFlag(BindMode.NoWeddingRing))
            {
                ReceiveChat(string.Format("不能使用这种类型的戒指."), ChatType.System);
                return;
            }

            uint cost = (uint)((Info.Inventory[index].Info.RequiredAmount * 10) * Settings.ReplaceWedRingCost);

            if (cost > Account.Gold)
            {
                ReceiveChat(String.Format("你没有足够的金币来代替你的结婚戒指."), ChatType.System);
                return;
            }

            Account.Gold -= cost;
            Enqueue(new S.LoseGold { Gold = cost });


            temp.WeddingRing = (long)Info.Married;
            if (CurrentRing != null)
            {
                CurrentRing.WeddingRing = -1;
                Info.Inventory[index] = CurrentRing;
            }
            

            Info.Equipment[(int)EquipmentSlot.RingL] = temp;
           

            Enqueue(new S.EquipItem { Grid = MirGridType.Inventory, UniqueID = temp.UniqueID, To = (int)EquipmentSlot.RingL, Success = true });

            Enqueue(new S.RefreshItem { Item = Info.Inventory[index] });
            Enqueue(new S.RefreshItem { Item = Info.Equipment[(int)EquipmentSlot.RingL] });

        }

        //求婚
        public void MarriageRequest()
        {

            if (Info.Married != 0)
            {
                ReceiveChat(string.Format("你已经结婚了."), ChatType.System);
                return;
            }

            if (Info.MarriedDate.AddDays(Settings.MarriageCooldown) > DateTime.Now)
            {
                ReceiveChat(string.Format("你再也不能结婚了, 离婚后有 {0} 天的冷却时间.", Settings.MarriageCooldown), ChatType.System);
                return;
            }

            if (Info.Level < Settings.MarriageLevelRequired)
            {
                ReceiveChat(string.Format("你至少要有到达等级 {0} 才能结婚.", Settings.MarriageLevelRequired), ChatType.System);
                return;
            }

            Point target = Functions.PointMove(CurrentLocation, Direction, 1);
            //Cell cell = CurrentMap.GetCell(target);
            PlayerObject player = null;

            if (CurrentMap.Objects[target.X, target.Y] == null || CurrentMap.Objects[target.X, target.Y].Count < 1)
            {
                ReceiveChat(string.Format("你需要面对对方才能结婚."), ChatType.System);
                return;
            }

            int playcount = 0;
            for (int i = 0; i < CurrentMap.Objects[target.X, target.Y].Count; i++)
            {
                MapObject ob = CurrentMap.Objects[target.X, target.Y][i];
                if (ob.Race != ObjectType.Player) continue;

                player = Envir.GetPlayer(ob.Name);
                playcount++;
            }
            if (playcount != 1)
            {
                ReceiveChat(string.Format("你需要面对对方才能结婚."), ChatType.System);
                return;
            }



            if (player != null)
            {
                if (!Functions.FacingEachOther(Direction, CurrentLocation, player.Direction, player.CurrentLocation))
                {
                    ReceiveChat(string.Format("你需要面对对方才能结婚."), ChatType.System);
                    return;
                }

                if (player.Level < Settings.MarriageLevelRequired)
                {
                    ReceiveChat(string.Format("你的爱人至少需要 {0} 级，才能结婚.", Settings.MarriageLevelRequired), ChatType.System);
                    return;
                }

                if (player.Info.MarriedDate.AddDays(Settings.MarriageCooldown) > DateTime.Now)
                {
                    ReceiveChat(string.Format("{0} 再也不能结婚了, 离婚后有{1}天的冷却时间 ", player.Name, Settings.MarriageCooldown), ChatType.System);
                    return;
                }

                if (!player.AllowMarriage)
                {
                    ReceiveChat("你想提出的人不允许结婚.", ChatType.System);
                    return;
                }

                if (player == this)
                {
                    ReceiveChat("你不能嫁给自己.", ChatType.System);
                    return;
                }

                if (player.Dead || Dead)
                {
                    ReceiveChat("你不能和死去的玩家结婚.", ChatType.System);
                    return;
                }

                if (player.MarriageProposal != null)
                {
                    ReceiveChat(string.Format("{0} 已经发送了结婚请求.", player.Info.Name), ChatType.System);
                    return;
                }

                if (!Functions.InRange(player.CurrentLocation, CurrentLocation, Globals.DataRange) || player.CurrentMap != CurrentMap)
                {
                    ReceiveChat(string.Format("{0} 不在结婚范围内.", player.Info.Name), ChatType.System);
                    return;
                }

                if (player.Info.Married != 0)
                {
                    ReceiveChat(string.Format("{0} 已经结婚了.", player.Info.Name), ChatType.System);
                    return;
                }
                if (player.Info.Gender == Info.Gender)
                {
                    ReceiveChat("对不起，目前不允许同性结婚.", ChatType.System);
                    return;
                }

                player.MarriageProposal = this;
                player.Enqueue(new S.MarriageRequest { Name = Info.Name });
            }
            else
            {
                ReceiveChat(string.Format("你需要面对对方进行求婚."), ChatType.System);
                return;
            }
        }
        //求婚允许，拒绝
        public void MarriageReply(bool accept)
        {
            if (MarriageProposal == null || MarriageProposal.Info == null)
            {
                MarriageProposal = null;
                return;
            }

            if (!accept)
            {
                MarriageProposal.ReceiveChat(string.Format("{0} 拒绝嫁给你.", Info.Name), ChatType.System);
                MarriageProposal = null;
                return;
            }

            if (Info.Married != 0)
            {
                ReceiveChat("你已经结婚了.", ChatType.System);
                MarriageProposal = null;
                return;
            }

            if (MarriageProposal.Info.Married != 0)
            {
                ReceiveChat(string.Format("{0} 已经结婚了.", MarriageProposal.Info.Name), ChatType.System);
                MarriageProposal = null;
                return;
            }


            MarriageProposal.Info.Married = Info.Index;
            MarriageProposal.Info.MarriedDate = DateTime.Now;

            Info.Married = MarriageProposal.Info.Index;
            Info.MarriedDate = DateTime.Now;

            GetRelationship(false);
            MarriageProposal.GetRelationship(false);

            MarriageProposal.ReceiveChat(string.Format("恭喜你，你和{0}结婚了 .", Info.Name), ChatType.System);
            ReceiveChat(String.Format("恭喜你，你和{0}结婚了 .", MarriageProposal.Info.Name), ChatType.System);

            MarriageProposal = null;
        }
        //离婚请求
        public void DivorceRequest()
        {

            if (Info.Married == 0)
            {
                ReceiveChat(string.Format("你还没结婚."), ChatType.System);
                return;
            }


            Point target = Functions.PointMove(CurrentLocation, Direction, 1);
            //Cell cell = CurrentMap.GetCell(target);
            PlayerObject player = null;

            if (CurrentMap.Objects[target.X, target.Y] == null || CurrentMap.Objects[target.X, target.Y].Count < 1) return;

            for (int i = 0; i < CurrentMap.Objects[target.X, target.Y].Count; i++)
            {
                MapObject ob = CurrentMap.Objects[target.X, target.Y][i];
                if (ob.Race != ObjectType.Player) continue;

                player = Envir.GetPlayer(ob.Name);
            }

            if (player == null)
            {
                ReceiveChat(string.Format("你需要面对你的爱人和他离婚。"), ChatType.System);
                return;
            }

            if (player != null)
            {
                if (!Functions.FacingEachOther(Direction, CurrentLocation, player.Direction, player.CurrentLocation))
                {
                    ReceiveChat(string.Format("你需要面对你的爱人和他离婚."), ChatType.System);
                    return;
                }

                if (player == this)
                {
                    ReceiveChat("你不能离婚.", ChatType.System);
                    return;
                }

                if (player.Dead || Dead)
                {
                    ReceiveChat("你不能和死去的混蛋离婚.", ChatType.System); //GOT TO HERE, NEED TO KEEP WORKING ON IT.
                    return;
                }

                if (player.Info.Index != Info.Married)
                {
                    ReceiveChat(string.Format("你还没有和 {0} 结婚", player.Info.Name), ChatType.System);
                    return;
                }

                if (!Functions.InRange(player.CurrentLocation, CurrentLocation, Globals.DataRange) || player.CurrentMap != CurrentMap)
                {
                    ReceiveChat(string.Format("{0} 不在离婚可操作范围内.", player.Info.Name), ChatType.System);
                    return;
                }

                player.DivorceProposal = this;
                player.Enqueue(new S.DivorceRequest { Name = Info.Name });
            }
            else
            {
                ReceiveChat(string.Format("你需要面对你的爱人和他离婚."), ChatType.System);
                return;
            }
        }
        //离婚，拒绝，同意
        public void DivorceReply(bool accept)
        {
            if (DivorceProposal == null || DivorceProposal.Info == null)
            {
                DivorceProposal = null;
                return;
            }

            if (!accept)
            {
                DivorceProposal.ReceiveChat(string.Format("{0} 拒绝和你离婚.", Info.Name), ChatType.System);
                DivorceProposal = null;
                return;
            }

            if (Info.Married == 0)
            {
                ReceiveChat("你没有结婚所以你不需要离婚.", ChatType.System);
                DivorceProposal = null;
                return;
            }

            Buff buff = Buffs.Where(e => e.Type == BuffType.RelationshipEXP).FirstOrDefault();
            if (buff != null)
            {
                RemoveBuff(BuffType.RelationshipEXP);
                DivorceProposal.RemoveBuff(BuffType.RelationshipEXP);
            }

            DivorceProposal.Info.Married = 0;
            DivorceProposal.Info.MarriedDate = DateTime.Now;
            if (DivorceProposal.Info.Equipment[(int)EquipmentSlot.RingL] != null)
            {
                DivorceProposal.Info.Equipment[(int)EquipmentSlot.RingL].WeddingRing = -1;
                DivorceProposal.Enqueue(new S.RefreshItem { Item = DivorceProposal.Info.Equipment[(int)EquipmentSlot.RingL] });
            }

            Info.Married = 0;
            Info.MarriedDate = DateTime.Now;
            if (Info.Equipment[(int)EquipmentSlot.RingL] != null)
            {
                Info.Equipment[(int)EquipmentSlot.RingL].WeddingRing = -1;
                Enqueue(new S.RefreshItem { Item = Info.Equipment[(int)EquipmentSlot.RingL] });
            }


            DivorceProposal.ReceiveChat(string.Format("你现在离婚了", Info.Name), ChatType.System);
            ReceiveChat("你现在离婚了", ChatType.System);

            GetRelationship(false);
            DivorceProposal.GetRelationship(false);
            DivorceProposal = null;
        }

        //更新契约兽
        public void GetMyMonsters()
        {
            //这里做个处理，排序处理，前面的不能为空哦
            S.MyMonstersPackets monpacket = new S.MyMonstersPackets { MyMonsters = Info.MyMonsters };
            Enqueue(monpacket);
        }

        public void GetRelationship(bool CheckOnline = true)
        {
            if (Info.Married == 0)
            {
                Enqueue(new S.LoverUpdate { Name = "", Date = Info.MarriedDate, MapName = "", MarriedDays = 0 });
            }
            else
            {
                CharacterInfo Lover = Envir.GetCharacterInfo(Info.Married);

                PlayerObject player = Envir.GetPlayer(Lover.Name);

                if (player == null)
                    Enqueue(new S.LoverUpdate { Name = Lover.Name, Date = Info.MarriedDate, MapName = "", MarriedDays = (short)(DateTime.Now - Info.MarriedDate).TotalDays });
                else
                {
                    Enqueue(new S.LoverUpdate { Name = Lover.Name, Date = Info.MarriedDate, MapName = player.CurrentMap.getTitle(), MarriedDays = (short)(DateTime.Now - Info.MarriedDate).TotalDays });
                    if (CheckOnline)
                    {
                        player.GetRelationship(false);
                        player.ReceiveChat(String.Format("{0} 已经在线.", Info.Name), ChatType.System);
                    }
                }
            }
        }
        public void LogoutRelationship()
        {
            if (Info.Married == 0) return;
            CharacterInfo Lover = Envir.GetCharacterInfo(Info.Married);

            if (Lover == null)
            {
                SMain.EnqueueDebugging(Name + " 已婚却找不到结婚证 " + Info.Married);
                return;
            }

            PlayerObject player = Envir.GetPlayer(Lover.Name);
            if (player != null)
            {
                player.Enqueue(new S.LoverUpdate { Name = Info.Name, Date = player.Info.MarriedDate, MapName = "", MarriedDays = (short)(DateTime.Now - Info.MarriedDate).TotalDays });
                player.ReceiveChat(String.Format("{0} 已经离线.", Info.Name), ChatType.System);
            }
        }

        #endregion

        #region Mentorship 师徒

        //取消拜师
        public void MentorBreak(bool Force = false)
        {
            if (Info.Mentor == 0)
            {
                ReceiveChat("您当前没有可取消的师傅", ChatType.System);
                return;
            }
            CharacterInfo Mentor = Envir.GetCharacterInfo(Info.Mentor);
            PlayerObject Player = Envir.GetPlayer(Mentor.Name);

            if (Force)
            {
                Info.MentorDate = DateTime.Now.AddDays(Settings.MentorLength);
                ReceiveChat(String.Format("你还需要等待{0}天，才可以重新寻找徒弟.", Settings.MentorLength), ChatType.System);
            }
            else
                ReceiveChat("你的导师资格已经过期了.", ChatType.System);

            if (Info.isMentor)
            {
                RemoveBuff(BuffType.Mentor);

                if (Player != null)
                {
                    Info.MentorExp += Player.MenteeEXP;
                    Player.MenteeEXP = 0;
                    Player.RemoveBuff(BuffType.Mentee);
                }
            }
            else
            {
                RemoveBuff(BuffType.Mentee);

                if (Player != null)
                {
                    Mentor.MentorExp += MenteeEXP;
                    MenteeEXP = 0;
                    Player.RemoveBuff(BuffType.Mentor);
                }
            }

            Info.Mentor = 0;
            GetMentor(false);
            

            if (Info.isMentor && Info.MentorExp > 0)
            {
                GainExp((uint)Info.MentorExp);
                Info.MentorExp = 0;
            }
            

            Mentor.Mentor = 0;
            

            if (Player != null)
            {
                Player.ReceiveChat("你的导师资格已经过期了.", ChatType.System);
                Player.GetMentor(false);
                if (Mentor.isMentor && Mentor.MentorExp > 0)
                {
                    Player.GainExp((uint)Mentor.MentorExp);
                    Info.MentorExp = 0;
                }
            }
            else
            {
                if (Mentor.isMentor && Mentor.MentorExp > 0)
                {
                    Mentor.Experience += Mentor.MentorExp;
                    Mentor.MentorExp = 0;
                }
            }

            Info.isMentor = false;
            Mentor.isMentor = false;
            Info.MentorExp = 0;
            Mentor.MentorExp = 0;
        }
        //拜师
        public void AddMentor(string Name)
        {

            if (Info.Mentor != 0)
            {
                ReceiveChat("你已经有师傅了.", ChatType.System);
                return;
            }

            if (Info.Name == Name)
            {
                ReceiveChat("你不能败自己为师.", ChatType.System);
                return;
            }

            if (Info.MentorDate > DateTime.Now)
            {
                ReceiveChat("你还不能寻找新的师傅.", ChatType.System);
                return;
            }

            PlayerObject Mentor = Envir.GetPlayer(Name);

            if (Mentor == null)
            {
                ReceiveChat(String.Format("当前名字找不到任何人 {0}.", Name), ChatType.System);
            }
            else
            {
                Mentor.MentorRequest = null;

                if (!Mentor.AllowMentor)
                {
                    ReceiveChat(String.Format("{0} 不允许拜师请求.", Mentor.Info.Name), ChatType.System);
                    return;
                }

                if (Mentor.Info.MentorDate > DateTime.Now)
                {
                    ReceiveChat(String.Format("{0} 还不能开始另一个指导.", Mentor.Info.Name), ChatType.System);
                    return;
                }

                if (Mentor.Info.Mentor != 0)
                {
                    ReceiveChat(String.Format("{0} 已经是师傅了.", Mentor.Info.Name), ChatType.System);
                    return;
                }

                if (Info.Class != Mentor.Info.Class)
                {
                    //ReceiveChat("你只能同职业的人为师傅.", ChatType.System);
                    //return;
                }
                if ((Info.Level + Settings.MentorLevelGap) > Mentor.Level)
                {
                    ReceiveChat(String.Format("您只能拜高于你 {0} 级的人为师.", Settings.MentorLevelGap), ChatType.System);
                    return;
                }

                Mentor.MentorRequest = this;
                Mentor.Enqueue(new S.MentorRequest { Name = Info.Name, Level = Info.Level });
                ReceiveChat(String.Format("拜师请求已发送."), ChatType.System);
            }

        }

        public void MentorReply(bool accept)
        {
            if (MentorRequest == null || MentorRequest.Info == null)
            {
                MentorRequest = null;
                return;
            }

            if (!accept)
            {
                MentorRequest.ReceiveChat(string.Format("{0} 拒绝做你师傅.", Info.Name), ChatType.System);
                MentorRequest = null;
                return;
            }

            if (Info.Mentor != 0)
            {
                ReceiveChat("你已经有了一个徒弟.", ChatType.System);
                return;
            }

            PlayerObject Student = Envir.GetPlayer(MentorRequest.Info.Name);
            MentorRequest = null;

            if (Student == null)
            {
                ReceiveChat(String.Format("{0} 不在线.", Student.Name), ChatType.System);
                return;
            }
            else
            {
                if (Student.Info.Mentor != 0)
                {
                    ReceiveChat(String.Format("{0} 已经有师傅了.", Student.Info.Name), ChatType.System);
                    return;
                }
                if (Info.Class != Student.Info.Class)
                {
                    //ReceiveChat("你只能指导同一个职业的人.", ChatType.System);
                    //return;
                }
                if ((Info.Level - Settings.MentorLevelGap) < Student.Level)
                {
                    ReceiveChat(String.Format("你只能指导比你低 {0} 级的人.", Settings.MentorLevelGap), ChatType.System);
                    return;
                }

                Student.Info.Mentor = Info.Index;
                Student.Info.isMentor = false;
                Info.Mentor = Student.Info.Index;
                Info.isMentor = true;
                Student.Info.MentorDate = DateTime.Now;
                Info.MentorDate = DateTime.Now;

                ReceiveChat(String.Format("你现在是{0}师傅 .", Student.Info.Name), ChatType.System);
                Student.ReceiveChat(String.Format("你现在正在被{0}指导 .", Info.Name), ChatType.System);
                GetMentor(false);
                Student.GetMentor(false);
            }
        }

        public void GetMentor(bool CheckOnline = true)
        {
            if (Info.Mentor == 0)
            {
                Enqueue(new S.MentorUpdate { Name = "", Level = 0, Online = false, MenteeEXP = 0 });
            }
            else
            {
                CharacterInfo Mentor = Envir.GetCharacterInfo(Info.Mentor);

                PlayerObject player = Envir.GetPlayer(Mentor.Name);

                if (player == null)
                    Enqueue(new S.MentorUpdate { Name = Mentor.Name, Level = Mentor.Level, Online = false, MenteeEXP = Info.MentorExp });
                else
                {
                    Enqueue(new S.MentorUpdate { Name = Mentor.Name, Level = Mentor.Level, Online = true, MenteeEXP = Info.MentorExp });
                    if (CheckOnline)
                    {
                        player.GetMentor(false);
                        player.ReceiveChat(String.Format("{0} 已经在线.", Info.Name), ChatType.System);
                    }
                }
            }
        }

        public void LogoutMentor()
        {
            if (Info.Mentor == 0) return;

            CharacterInfo Mentor = Envir.GetCharacterInfo(Info.Mentor);

            if (Mentor == null)
            {
                SMain.EnqueueDebugging(Name + " 是导师，但找不到导师ID " + Info.Mentor);
                return;
            }

            PlayerObject player = Envir.GetPlayer(Mentor.Name);

            if (!Info.isMentor)
            {
                Mentor.MentorExp += MenteeEXP;
            }

            if (player != null)
            {
                player.Enqueue(new S.MentorUpdate { Name = Info.Name, Level = Info.Level, Online = false, MenteeEXP = Mentor.MentorExp });
                player.ReceiveChat(String.Format("{0} 已经下线.", Info.Name), ChatType.System);
            }
        }

        #endregion

        #region Gameshop
        //刷新库存
        public void GameShopStock(GameShopItem item)
        {
            //购买数
            int purchased;
            //剩余
            int StockLevel;

            if (item.iStock) //Invididual Stock,角色的库存
            {
                Info.GSpurchases.TryGetValue(item.Info.Index, out purchased);
            }
            else //Server Stock，服务器的库存
            {
                Envir.GameshopLog.TryGetValue(item.Info.Index, out purchased);
            }

            if (item.Stock - purchased >= 0)
            {
                StockLevel = item.Stock - purchased;
                Enqueue(new S.GameShopStock { GIndex = item.Info.Index, StockLevel = StockLevel });
            }
        }
        //商城购买东西
        public void GameshopBuy(int GIndex,byte payType, byte Quantity)
        {
            if (Quantity < 1 || Quantity > 99) return;

            List<GameShopItem> shopList = Envir.GameShopList;
            GameShopItem Product = null;
            int purchased;
            //库存是否足够
            bool stockAvailable = false;

            bool canAfford = false;
            uint CreditCost =0;//花费的元宝
            uint GoldCost = 0;//花费的金币

            List<UserItem> mailItems = new List<UserItem>();

            for (int i = 0; i < shopList.Count; i++)
            {
                if (shopList[i].GIndex == GIndex)
                {
                    Product = shopList[i];
                    break;
                }
            }

            if (Product == null)
            {
                ReceiveChat("你想买一个不在商店里的东西.", ChatType.System);
                SMain.EnqueueDebugging(Info.Name + " 试图购买不存在的东西.");
                return;
            }
            //一次最多只能买5个
            if (((decimal)(Quantity * Product.Count) / Product.Info.StackSize) > 5) return;

            if (Product.Stock != 0)
            {

                if (Product.iStock) //Invididual Stock
                {
                    Info.GSpurchases.TryGetValue(Product.Info.Index, out purchased);
                }
                else //Server Stock
                {
                    Envir.GameshopLog.TryGetValue(Product.Info.Index, out purchased);
                }

                if (Product.Stock - purchased - Quantity >= 0)
                {
                    stockAvailable = true;
                }
                else
                {
                    ReceiveChat("库存不足.", ChatType.System);
                    GameShopStock(Product);
                    SMain.EnqueueDebugging(Info.Name + " 正在试图购买 " + Product.Info.FriendlyName + " x " + Quantity + " - 库存不足.");
                    return;
                }
            }
            else
            {
                stockAvailable = true;
            }
            if (!stockAvailable)
            {
                return;
            }

            //这个是时间库存，1个小时左右，只能卖多少
            if (Product.TimeStock != 0)
            {
                if(Envir.Time > Product.lastBuyTime)
                {
                    Product.lastBuyTime = Envir.Time + 1000 * 60 * RandomUtils.Next(50, 60);
                    Product.TimeStockCount = Quantity;
                }
                else
                {
                    if (Product.TimeStockCount+ Quantity > Product.TimeStock)
                    {
                        ReceiveChat("库存不足.", ChatType.System);
                        return;
                    }
                    Product.TimeStockCount += Quantity;
                }
            }
                

            SMain.EnqueueDebugging(Info.Name + " is trying to buy " + Product.Info.FriendlyName + " x " + Quantity + " - Stock is available");
            //金币购买
            if (payType == 0)
            {
                GoldCost = Product.GoldPrice * Quantity;
                if (Account.Gold >= GoldCost && GoldCost>0)
                {
                    canAfford = true;
                }
            }
            if (payType == 1)
            {
                CreditCost = Product.CreditPrice * Quantity;
                if (Account.Credit >= CreditCost && CreditCost>0)
                {
                    canAfford = true;
                }
            }
            if (!canAfford)
            {
                ReceiveChat("你没有足够的货币购买此商品.", ChatType.System);
                SMain.EnqueueDebugging(Info.Name + " is trying to buy " + Product.Info.FriendlyName + " x " + Quantity + " - not enough currency.");
                return;
            }
            //创建物品，并判断背包空间是否充足
            //物品合并，拆包等处理
            uint quantity = (Quantity * Product.Count);

            if (Product.Info.StackSize <= 1 || quantity == 1)
            {
                for (int i = 0; i < Quantity; i++)
                {
                    UserItem mailItem = Product.Info.CreateFreshItem();

                    mailItems.Add(mailItem);
                }
            }
            else
            {
                //可能创建多个物品
                while (quantity > 0)
                {
                    UserItem mailItem = Product.Info.CreateFreshItem();
                    mailItem.Count = 0;
                    for (int i = 0; i < mailItem.Info.StackSize; i++)
                    {
                        mailItem.Count++;
                        quantity--;
                        if (quantity == 0) break;
                    }
                    if (mailItem.Count == 0) break;
                    mailItems.Add(mailItem);
                }
            }
            //发到背包
            if (Product.acceptType == 0)
            {
                if (!CanGainItems(mailItems.ToArray()))
                {
                    ReceiveChat("您的背包空间或负重不足，购买失败，请清空背包后再购买", ChatType.Hint);
                    return;
                }
                foreach(UserItem item in mailItems)
                {
                    GainItem(item);
                }
            }
            //发送邮件
            if (Product.acceptType == 1)
            {
                MailInfo mail = new MailInfo(Info.Index)
                {
                    MailID = (ulong)UniqueKeyHelper.UniqueNext(),
                    Sender = "商店",
                    Message = "谢谢你从游戏商店购买商品，您的商品在附件上（邮件可去邮局收取）.",
                    Items = mailItems,
                };
                mail.Send();

                SMain.EnqueueDebugging(Info.Name + " is trying to buy " + Product.Info.FriendlyName + " x " + Quantity + " - Purchases Sent!");
                ReceiveChat("您的购买物品已发送到您的邮箱.请注意查收", ChatType.Hint);
            }


            //足够支付
            SMain.EnqueueDebugging(Info.Name + " is trying to buy " + Product.Info.FriendlyName + " x " + Quantity + " - Has enough currency.");
            //从账户中扣取金额
            Account.Gold -= GoldCost;
            Account.Credit -= CreditCost;
            SMain.Envir.AddDropGold += GoldCost;
            Report.GoldChanged("GameShop", GoldCost, true, Product.Info.FriendlyName);
            Report.CreditChanged("GameShop", CreditCost, true, Product.Info.FriendlyName);
            //发送扣除金额
            if (GoldCost != 0) Enqueue(new S.LoseGold { Gold = GoldCost });
            if (CreditCost != 0) Enqueue(new S.LoseCredit { Credit = CreditCost });
            //库存处理
            int Purchased;
            if (Product.iStock && Product.Stock != 0)
            {
                Info.GSpurchases.TryGetValue(Product.Info.Index, out Purchased);
                if (Purchased == 0)
                {
                    Info.GSpurchases[Product.GIndex] = Quantity;
                }
                else
                {
                    Info.GSpurchases[Product.GIndex] += Quantity;
                }
            }

            Purchased = 0;

            Envir.GameshopLog.TryGetValue(Product.Info.Index, out Purchased);
            if (Purchased == 0)
            {
                Envir.GameshopLog[Product.GIndex] = Quantity;
            }
            else
            {
                Envir.GameshopLog[Product.GIndex] += Quantity;
            }

            if (Product.Stock != 0) GameShopStock(Product);

            Report.ItemGSBought("GameShop", Product, Quantity, CreditCost, GoldCost);

        }
            
        public void GetGameShop()
        {
            int purchased;
            GameShopItem item = new GameShopItem();
            int StockLevel;

            for (int i = 0; i < Envir.GameShopList.Count; i++)
            {
                item = Envir.GameShopList[i];

                if (item.Stock != 0)
                {
                    if (item.iStock) //Individual Stock
                    {
                        Info.GSpurchases.TryGetValue(item.Info.Index, out purchased);
                    }
                    else //Server Stock
                    {
                        Envir.GameshopLog.TryGetValue(item.Info.Index, out purchased);
                    }

                    if (item.Stock - purchased >= 0)
                    {
                        StockLevel = item.Stock - purchased;
                        Enqueue(new S.GameShopInfo { Item = item, StockLevel = StockLevel });
                    }
                }
                else
                {
                    Enqueue(new S.GameShopInfo { Item = item, StockLevel = item.Stock });
                }  
            }
        }

        #endregion

        #region ConquestWall
        public void CheckConquest(bool checkPalace = false)
        {
            if (CurrentMap.tempConquest == null && CurrentMap.Conquest != null)
            {
                ConquestObject swi = CurrentMap.GetConquest(CurrentLocation);
                if (swi != null)
                    EnterSabuk();
                else
                    LeaveSabuk();
            }
            else if (CurrentMap.tempConquest != null)
            {
                if (checkPalace && CurrentMap.Info.Index == CurrentMap.tempConquest.PalaceMap.Info.Index && CurrentMap.tempConquest.GameType == ConquestGame.CapturePalace)
                    CurrentMap.tempConquest.TakeConquest(this);

                EnterSabuk();
            }
        }
        public void EnterSabuk()
        {
            if (WarZone) return;
            WarZone = true;
            RefreshNameColour();
        }

        public void LeaveSabuk()
        {
            if (!WarZone) return;
            WarZone = false;
            RefreshNameColour();
        }
        #endregion

        private long[,] LastRankRequest = new long[6,3];

        //获取排行榜数据
        //30秒才允许查询一次
        public void GetRanking(byte RankType, byte RankType2)
        {
            if (RankType > 6) return;
            if ((LastRankRequest[RankType, RankType2] != 0) && ((LastRankRequest[RankType, RankType2] + 30 * 1000) > Envir.Time)) return;
            LastRankRequest[RankType, RankType2] = Envir.Time;
            Enqueue(new S.Rankings { Listings = Envir.RankClass[RankType, RankType2], RankType = RankType, RankType2 = RankType2 });
        }

        public void Opendoor(byte Doorindex)
        {
            //todo: add check for sw doors
            if (CurrentMap.OpenDoor(Doorindex))
            {
                Enqueue(new S.Opendoor() { DoorIndex = Doorindex });
                Broadcast(new S.Opendoor() { DoorIndex = Doorindex });
            }
        }

        public void GetRentedItems()
        {
            Enqueue(new S.GetRentedItems { RentedItems = Info.RentedItems });
        }

        public void ItemRentalRequest()
        {
            if (Dead)
            {
                ReceiveChat("死时不能租用物品.", ChatType.System);
                return;
            }

            if (ItemRentalPartner != null)
            {
                ReceiveChat("你已经租用了一个物品给另一个玩家.", ChatType.System);
                return;
            }

            var targetPosition = Functions.PointMove(CurrentLocation, Direction, 1);
            //var targetCell = CurrentMap.GetCell(targetPosition);
            PlayerObject targetPlayer = null;

            if (CurrentMap.Objects[targetPosition.X, targetPosition.Y] == null || CurrentMap.Objects[targetPosition.X, targetPosition.Y].Count < 1)
                return;

            foreach (var mapObject in CurrentMap.Objects[targetPosition.X, targetPosition.Y])
            {
                if (mapObject.Race != ObjectType.Player)
                    continue;

                targetPlayer = Envir.GetPlayer(mapObject.Name);
            }

            if (targetPlayer == null)
            {
                ReceiveChat("租用物品需要面对玩家.", ChatType.System);
                return;
            }

            if (Info.RentedItems.Count >= 3)
            {
                ReceiveChat("一次不能租用超过3件物品.", ChatType.System);
                return;
            }

            if (targetPlayer.Info.HasRentedItem)
            {
                ReceiveChat($"{targetPlayer.Name} 此时无法再租用物品.", ChatType.System);
                return;
            }

            if (!Functions.FacingEachOther(Direction, CurrentLocation, targetPlayer.Direction,
                targetPlayer.CurrentLocation))
            {
                ReceiveChat("租用物品需要面对玩家.", ChatType.System);
                return;
            }

            if (targetPlayer == this)
            {
                ReceiveChat("你无法向自己租借物品.", ChatType.System);
                return;
            }

            if (targetPlayer.Dead)
            {
                ReceiveChat($"无法租用物品 {targetPlayer.Name} 死的时候.", ChatType.System);
                return;
            }

            if (!Functions.InRange(targetPlayer.CurrentLocation, CurrentLocation, Globals.DataRange)
                || targetPlayer.CurrentMap != CurrentMap)
            {
                ReceiveChat($"{targetPlayer.Name} 不在范围内", ChatType.System);
                return;
            }

            if (targetPlayer.ItemRentalPartner != null)
            {
                ReceiveChat($"{targetPlayer.Name} 当前忙，请再试一次.", ChatType.System);
                return;
            }

            ItemRentalPartner = targetPlayer;
            targetPlayer.ItemRentalPartner = this;

            Enqueue(new S.ItemRentalRequest { Name = targetPlayer.Name, Renting = false });
            ItemRentalPartner.Enqueue(new S.ItemRentalRequest { Name = Name, Renting = true });
        }

        public void SetItemRentalFee(uint amount)
        {
            if (ItemRentalFeeLocked)
                return;

            if (Account.Gold < amount)
                return;

            if (ItemRentalPartner == null)
                return;

            ItemRentalFeeAmount += amount;
            Account.Gold -= amount;

            Enqueue(new S.LoseGold { Gold = amount });
            ItemRentalPartner.Enqueue(new S.ItemRentalFee { Amount = amount });
        }

        public void SetItemRentalPeriodLength(uint days)
        {
            if (ItemRentalItemLocked)
                return;

            if (ItemRentalPartner == null)
                return;

            ItemRentalPeriodLength = days;
            ItemRentalPartner.Enqueue(new S.ItemRentalPeriod { Days = days });
        }

        public void DepositRentalItem(int from, int to)
        {
            var packet = new S.DepositRentalItem { From = from, To = to, Success = false };

            if (ItemRentalItemLocked)
            {
                Enqueue(packet);
                return;
            }

            if (from < 0 || from >= Info.Inventory.Length)
            {
                Enqueue(packet);
                return;
            }

            // TODO: Change this check.
            if (to < 0 || to >= 1)
            {
                Enqueue(packet);
                return;
            }

            var item = Info.Inventory[from];

            if (item == null)
            {
                Enqueue(packet);
                return;
            }

            if (item.RentalInformation?.RentalLocked == true)
            {
                ReceiveChat($"无法出租 {item.FriendlyName} 直到 {item.RentalInformation.ExpiryDate}", ChatType.System);
                Enqueue(packet);
                return;
            }

            if (item.Info.Bind.HasFlag(BindMode.UnableToRent))
            {
                ReceiveChat($"无法出租 {item.FriendlyName}", ChatType.System);
                Enqueue(packet);
                return;
            }

            if (item.RentalInformation != null && item.RentalInformation.BindingFlags.HasFlag(BindMode.UnableToRent))
            {
                ReceiveChat($"无法出租 {item.FriendlyName} 因为它属于 {item.RentalInformation.OwnerName}", ChatType.System);
                Enqueue(packet);
                return;
            }

            if (ItemRentalDepositedItem == null)
            {
                ItemRentalDepositedItem = item;
                Info.Inventory[from] = null;

                packet.Success = true;
                RefreshBagWeight();
                UpdateRentalItem();
                Report.ItemMoved("DepositRentalItem", item, MirGridType.Inventory, MirGridType.Renting, from, to);
            }

            Enqueue(packet);
        }

        public void RetrieveRentalItem(int from, int to)
        {
            var packet = new S.RetrieveRentalItem { From = from, To = to, Success = false };

            // TODO: Change this check.
            if (from < 0 || from >= 1)
            {
                Enqueue(packet);
                return;
            }

            if (to < 0 || to >= Info.Inventory.Length)
            {
                Enqueue(packet);
                return;
            }

            var item = ItemRentalDepositedItem;

            if (item == null)
            {
                Enqueue(packet);
                return;
            }

            if (item.Weight + CurrentBagWeight > MaxBagWeight)
            {
                ReceiveChat("物品太重无法获取.", ChatType.System);
                Enqueue(packet);
                return;
            }

            if (Info.Inventory[to] == null)
            {
                Info.Inventory[to] = item;
                ItemRentalDepositedItem = null;

                packet.Success = true;
                RefreshBagWeight();
                UpdateRentalItem();
                Report.ItemMoved("RetrieveRentalItem", item, MirGridType.Renting, MirGridType.Inventory, from, to);
            }

            Enqueue(packet);
        }

        private void UpdateRentalItem()
        {
            if (ItemRentalPartner == null)
                return;

            if (ItemRentalDepositedItem != null)
                ItemRentalPartner.CheckItem(ItemRentalDepositedItem);

            ItemRentalPartner.Enqueue(new S.UpdateRentalItem { LoanItem = ItemRentalDepositedItem });
        }

        public void CancelItemRental()
        {
            if (ItemRentalPartner == null)
                return;

            ItemRentalRemoveLocks();

            var rentalPair = new []  {
                ItemRentalPartner,
                this
            };

            for (var i = 0; i < 2; i++)
            {
                if (rentalPair[i] == null)
                    continue;

                if (rentalPair[i].ItemRentalDepositedItem != null)
                {
                    var item = rentalPair[i].ItemRentalDepositedItem;

                    if (FreeSpace(rentalPair[i].Info.Inventory) < 1)
                    {
                        rentalPair[i].GainItemMail(item, 1);
                        rentalPair[i].Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                        rentalPair[i].ItemRentalDepositedItem = null;

                        Report.ItemMailed("Cancel Item Rental", item, item.Count, 1);

                        continue;
                    }

                    for (var j = 0; j < rentalPair[i].Info.Inventory.Length; j++)
                    {
                        if (rentalPair[i].Info.Inventory[j] != null)
                            continue;

                        if (rentalPair[i].CanGainItem(item))
                            rentalPair[i].RetrieveRentalItem(0, j);
                        else
                        {
                            rentalPair[i].GainItemMail(item, 1);
                            rentalPair[i].Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });

                            Report.ItemMailed("Cancel Item Rental", item, item.Count, 1);
                        }

                        rentalPair[i].ItemRentalDepositedItem = null;

                        break;
                    }
                }
 
                if (rentalPair[i].ItemRentalFeeAmount > 0)
                {
                    rentalPair[i].GainGold(rentalPair[i].ItemRentalFeeAmount);
                    rentalPair[i].ItemRentalFeeAmount = 0;

                    Report.GoldChanged("CancelItemRental", rentalPair[i].ItemRentalFeeAmount, false);
                }

                rentalPair[i].ItemRentalPartner = null;
                rentalPair[i].Enqueue(new S.CancelItemRental());
            }
        }

        public void ItemRentalLockFee()
        {
            S.ItemRentalLock p = new S.ItemRentalLock { Success = false, GoldLocked = false, ItemLocked = false };

            if (ItemRentalFeeAmount > 0)
            {
                ItemRentalFeeLocked = true;
                p.GoldLocked = true;
                p.Success = true;

                ItemRentalPartner.Enqueue(new S.ItemRentalPartnerLock { GoldLocked = ItemRentalFeeLocked });
            }

            if (ItemRentalFeeLocked && ItemRentalPartner.ItemRentalItemLocked)
                ItemRentalPartner.Enqueue(new S.CanConfirmItemRental());
            else if (ItemRentalFeeLocked && !ItemRentalPartner.ItemRentalItemLocked)
                ItemRentalPartner.ReceiveChat($"{Name}已锁定租金.", ChatType.System);

            Enqueue(p);
        }

        public void ItemRentalLockItem()
        {
            S.ItemRentalLock p = new S.ItemRentalLock { Success = false, GoldLocked = false, ItemLocked = false };

            if (ItemRentalDepositedItem != null)
            {
                ItemRentalItemLocked = true;
                p.ItemLocked = true;
                p.Success = true;

                ItemRentalPartner.Enqueue(new S.ItemRentalPartnerLock { ItemLocked = ItemRentalItemLocked });
            }

            if (ItemRentalItemLocked && ItemRentalPartner.ItemRentalFeeLocked)
                Enqueue(new S.CanConfirmItemRental());
            else if (ItemRentalItemLocked && !ItemRentalPartner.ItemRentalFeeLocked)
                ItemRentalPartner.ReceiveChat($"{Name}已锁定租赁物品.", ChatType.System);


            Enqueue(p);
        }

        private void ItemRentalRemoveLocks()
        {
            ItemRentalFeeLocked = false;
            ItemRentalItemLocked = false;

            if (ItemRentalPartner == null)
                return;

            ItemRentalPartner.ItemRentalFeeLocked = false;
            ItemRentalPartner.ItemRentalItemLocked = false;
        }

        public void ConfirmItemRental()
        {
            if (ItemRentalPartner == null)
            {
                CancelItemRental();
                return;
            }

            if (Info.RentedItems.Count >= 3)
            {
                CancelItemRental();
                return;
            }

            if (ItemRentalPartner.Info.HasRentedItem)
            {
                CancelItemRental();
                return;
            }

            if (ItemRentalDepositedItem == null)
                return;

            if (ItemRentalPartner.ItemRentalFeeAmount <= 0)
                return;

            if (ItemRentalDepositedItem.Info.Bind.HasFlag(BindMode.UnableToRent))
                return;

            if (ItemRentalDepositedItem.RentalInformation != null &&
                ItemRentalDepositedItem.RentalInformation.BindingFlags.HasFlag(BindMode.UnableToRent))
                return;

            if (!Functions.InRange(ItemRentalPartner.CurrentLocation, CurrentLocation, Globals.DataRange)
                || ItemRentalPartner.CurrentMap != CurrentMap || !Functions.FacingEachOther(Direction, CurrentLocation,
                    ItemRentalPartner.Direction, ItemRentalPartner.CurrentLocation))
            {
                CancelItemRental();
                return;
            }

            if (!ItemRentalItemLocked && !ItemRentalPartner.ItemRentalFeeLocked)
                return;

            if (!ItemRentalPartner.CanGainItem(ItemRentalDepositedItem))
            {
                ReceiveChat($"{ItemRentalPartner.Name} 无法获取物品.", ChatType.System);
                Enqueue(new S.CancelItemRental());

                ItemRentalPartner.ReceiveChat("无法获取租赁物品.", ChatType.System);
                ItemRentalPartner.Enqueue(new S.CancelItemRental());

                return;
            }

            if (!CanGainGold(ItemRentalPartner.ItemRentalFeeAmount))
            {
                ReceiveChat("你再也收不到金币了.", ChatType.System);
                Enqueue(new S.CancelItemRental());

                ItemRentalPartner.ReceiveChat($"{Name} 再也收不到金币.", ChatType.System);
                ItemRentalPartner.Enqueue(new S.CancelItemRental());

                return;
            }

            var item = ItemRentalDepositedItem;
            item.RentalInformation = new RentalInformation
            {
                OwnerName = Name,
                ExpiryDate = DateTime.Now.AddDays(ItemRentalPeriodLength),
                BindingFlags = BindMode.DontDrop | BindMode.DontStore | BindMode.DontSell | BindMode.DontTrade | BindMode.UnableToRent | BindMode.DontUpgrade | BindMode.UnableToDisassemble
            };

            var itemRentalInformation = new ItemRentalInformation
            {
                ItemId = item.UniqueID,
                ItemName = item.FriendlyName,
                RentingPlayerName = ItemRentalPartner.Name,
                ItemReturnDate = item.RentalInformation.ExpiryDate,
                
            };

            Info.RentedItems.Add(itemRentalInformation);
            ItemRentalDepositedItem = null;

            ItemRentalPartner.GainItem(item);
            ItemRentalPartner.Info.HasRentedItem = true;
            ItemRentalPartner.ReceiveChat($"你租了 {item.FriendlyName} 从 {Name} 直到 {item.RentalInformation.ExpiryDate}", ChatType.System);

            GainGold(ItemRentalPartner.ItemRentalFeeAmount);
            ReceiveChat($"收到 {ItemRentalPartner.ItemRentalFeeAmount} 租金.", ChatType.System);
            ItemRentalPartner.ItemRentalFeeAmount = 0;

            Enqueue(new S.ConfirmItemRental());
            ItemRentalPartner.Enqueue(new S.ConfirmItemRental());

            ItemRentalRemoveLocks();

            ItemRentalPartner.ItemRentalPartner = null;
            ItemRentalPartner = null;
        }
    }
}

