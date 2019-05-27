using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Server.MirDatabase;
using Server.MirEnvir;
using Server.MirObjects.Monsters;
using S = ServerPackets;

namespace Server.MirObjects
{
    /// <summary>
    /// 怪物对象
    /// </summary>
    public class MonsterObject : MapObject
    {
        public static MonsterObject GetMonster(MonsterInfo info)
        {
            if (info == null) return null;
           
            switch (info.AI)
            {
                case 1:
                case 2://鸡,鹿
                    return new Deer(info);
                case 3://龙蛇宝箱,树
                    return new Tree(info);
                case 4://毒蜘蛛
                    return new SpittingSpider(info);
                case 5://食人花
                    return new CannibalPlant(info);
                case 6://带刀护卫
                    return new Guard(info);
                case 7://洞蛆,楔蛾
                    return new CaveMaggot(info);
                case 8://掷斧骷髅,黑暗战士,祖玛弓箭手
                    return new AxeSkeleton(info);
                case 9://狼,蝎子
                    return new HarvestMonster(info);
                case 10://火焰沃玛
                    return new FlamingWooma(info);
                case 11://沃玛教主
                    return new WoomaTaurus(info);
                case 12://角蝇
                    return new BugBagMaggot(info);
                case 13://赤月恶魔
                    return new RedMoonEvil(info);
                case 14://触龙神
                    return new EvilCentipede(info);
                case 15://祖玛雕像
                    return new ZumaMonster(info);
                case 16://祖玛赤雷
                    return new RedThunderZuma(info);
                case 17://祖玛教主
                    return new ZumaTaurus(info);
                case 18://神兽
                    return new Shinsu(info);
                case 19://虹魔蝎卫
                    return new KingScorpion(info);
                case 20://虹魔教主
                    return new DarkDevil(info);
                case 21://肉食性食尸鬼(未知是什么)
                    return new IncarnatedGhoul(info);
                case 22://祖玛教主_封魔
                    return new IncarnatedZT(info);
                case 23://商店护卫,变异骷髅
                    return new BoneFamiliar(info);
                case 24://僧侣僵尸
                    return new DigOutZombie(info);
                case 25://腐肉僵尸
                    return new RevivingZombie(info);
                case 26://雷电僵尸
                    return new ShamanZombie(info);
                case 27://狂热血蜥蜴,蚂蚁司令官
                    return new Khazard(info);
                case 28://恶灵尸王
                    return new ToxicGhoul(info);
                case 29://骷髅长枪兵
                    return new BoneSpearman(info);
                case 30://黄泉教主
                    return new BoneLord(info);
                case 31://牛魔法师
                    return new RightGuard(info);
                case 32://牛魔祭祀
                    return new LeftGuard(info);
                case 33://牛魔王
                    return new MinotaurKing(info);
                case 34://幻影寒虎
                    return new FrostTiger(info);
                case 35://沙虫
                    return new SandWorm(info);
                case 36://浮龙金蛇
                    return new Yimoogi(info);
                case 37://神石毒魔蛛1
                    return new CrystalSpider(info);
                case 38://精灵
                    return new HolyDeva(info);
                case 39://幻影蜘蛛
                    return new RootSpider(info);
                case 40://爆裂蜘蛛
                    return new BombSpider(info);
                case 41:
                case 42://镇魂石
                    return new YinDevilNode(info);
                case 43://破凰魔神,火龙教主？
                    return new OmaKing(info);
                case 44://狐狸战士
                    return new BlackFoxman(info);
                case 45://狐狸法师
                    return new RedFoxman(info);
                case 46://狐狸道士
                    return new WhiteFoxman(info);
                case 47://悲月魂石
                    return new TrapRock(info);
                case 48://九尾魂石
                    return new GuardianRock(info);
                case 49://闪电元素
                    return new ThunderElement(info);
                case 50://悲月天珠
                    return new GreatFoxSpirit(info);
                case 51://悲月刺蛙
                    return new HedgeKekTal(info);
                case 52://破天魔龙
                    return new EvilMir(info);
                case 53://破天魔龙
                    return new EvilMirBody(info);
                case 54://火龙守护兽
                    return new DragonStatue(info);
                case 55://分身
                    return new HumanWizard(info);
                case 56://练功师
                    return new Trainer(info);
                case 57://弓箭护卫
                    return new TownArcher(info);
                case 58://大刀护卫
                    return new Guard(info);
                case 59://刺客分身
                    return new HumanAssassin(info);
                case 60://召唤蜘蛛
                    return new VampireSpider(info);
                case 61://召唤蛤蟆
                    return new SpittingToad(info);
                case 62://召唤图腾
                    return new SnakeTotem(info);
                case 63://鬼魅蛇
                    return new CharmedSnake(info);
                case 64://宝贝猪,小鸡
                    return new IntelligentCreatureObject(info);
                case 65://赤血利刃
                    return new MutatedManworm(info);
                case 66://赤血狂魔
                    return new CrazyManworm(info);
                case 67://黑暗多脚怪
                    return new DarkDevourer(info);
                case 68://足球
                    return new Football(info);
                case 69://紫电小蜘蛛
                    return new PoisonHugger(info);
                case 70://剧毒小蜘蛛
                    return new Hugger(info);
                case 71://怨恶
                    return new Behemoth(info);
                case 72://幽冥龟
                    return new FinialTurtle(info);
                case 73://大龟王
                    return new TurtleKing(info);
                case 74://光明龟
                    return new LightTurtle(info);
                case 75://溶混鬼
                    return new WitchDoctor(info);
                case 76://弯刀流魂
                    return new HellSlasher(info);
                case 77://拔舌流魂
                    return new HellPirate(info);
                case 78://吞魂鬼
                    return new HellCannibal(info);
                case 79://地狱守门人
                    return new HellKeeper(info);
                case 80://守卫弓手
                    return new ConquestArcher(info);
                case 81://大门
                    return new Gate(info);
                case 82://城堡Gi西
                    return new Wall(info);
                case 83://风暴战士
                    return new Tornado(info);
                case 84://野兽王
                    return new WingedTigerLord(info);

                case 86://冰狱战将
                    return new ManectricClaw(info);
                case 87://冰狱天将
                    return new ManectricBlest(info);
                case 88://冰狱魔王
                    return new ManectricKing(info);
                case 89://冰柱
                    return new IcePillar(info);
                case 90://地狱炮兵
                    return new TrollBomber(info);
                case 91://地狱统领
                    return new TrollKing(info);
                case 92://地狱长矛鬼
                    return new FlameSpear(info);
                case 93://地狱魔焰鬼
                    return new FlameMage(info);
                case 94://地狱巨镰鬼
                    return new FlameScythe(info);
                case 95://地狱双刃鬼
                    return new FlameAssassin(info);
                case 96://地狱将军
                    return new FlameQueen(info);
                case 97://寒冰守护神,紫电守护神
                    return new HellKnight(info);
                case 98://炎魔太子
                    return new HellLord(info);
                case 99://寒冰球
                    return new HellBomb(info);
                case 100://
                    return new VenomSpider(info);
                case 101:
                    return new Jar2(info);
                case 102:
                    return new RestlessJar(info);
                case 103://赤血鬼魂
                    return new CyanoGhast(info);
                case 104://阳龙王
                    return new ChieftainSword(info);
                case 105://暴雪僵尸
                    return new FrozenZombie(info);
                case 106://火焰僵尸
                    return new BurningZombie(info);
                case 107://DarkBeast LightBeast 暗黑剑齿虎 光明剑齿虎 2种攻击
                    return new DarkBeast(info);
                case 108://WhiteMammoth 猛犸象,普通攻击和蹲地板
                    return new WhiteMammoth(info);
                case 109://HardenRhino 铁甲犀牛
                    return new HardenRhino(info);
                case 110://Demonwolf 赤炎狼 1近身普攻，3格内喷火 火焰灵猫 共用
                    return new Demonwolf(info);
                case 111://BloodBaboon 血狒狒
                    return new BloodBaboon(info);
                case 112://DeathCrawler 死灵
                    return new DeathCrawler(info);
                case 113://AncientBringer 丹墨
                    return new AncientBringer(info);
                case 114://CatWidow 长枪灵猫
                    return new CatWidow(info);
                case 115://StainHammerCat 铁锤猫卫
                    return new StainHammerCat(info);
                case 116://BlackHammerCat 黑镐猫卫
                    return new BlackHammerCat(info);
                case 117://StrayCat 双刃猫卫
                    return new StrayCat(info);
                case 118://CatShaman 灵猫法师
                    return new CatShaman(info);
                case 119://SeedingsGeneral 灵猫圣兽
                    return new SeedingsGeneral(info);
                case 120://SeedingsGeneral 灵猫将军
                    return new GeneralJinmYo(info);
                case 122://GasToad 神气蛤蟆
                    return new GasToad(info);
                case 123://Mantis 螳螂
                    return new Mantis(info);
                case 124://SwampWarrior 神殿树人
                    return new SwampWarrior(info);
                case 125://SwampWarrior 神殿刺鸟
                    return new AssassinBird(info);
                case 126://RhinoWarrior 犀牛勇士
                    return new RhinoWarrior(info);
                case 127://RhinoPriest 犀牛牧师
                    return new RhinoPriest(info);
                case 128://SwampSlime 泥战士
                    return new SwampSlime(info);
                case 129://RockGuard 石巨人
                    return new RockGuard(info);
                case 130://MudWarrior 泥土巨人
                    return new MudWarrior(info);
                case 131://SmallPot 小如来
                    return new SmallPot(info);
                case 132://TreeQueen 树王
                    return new TreeQueen(info);
                case 133://ShellFighter 斗争者
                    return new ShellFighter(info);
                case 134://黑暗的沸沸 
                    return new DarkBaboon(info);
                case 135://双头兽 
                    return new TwinHeadBeast(info);
                case 136://奥玛食人族 
                    return new OmaCannibal(info);
                case 137://奥玛祝福 普通攻击，砸地板 
                    return new OmaBlest(info);
                case 138://奥玛斧头兵 破防
                    return new OmaSlasher(info);
                case 139://奥玛刺客 闪现近身，破防，攻击完，又随机闪开
                    return new OmaAssassin(info);
                case 140://奥玛法师，随机闪开
                    return new OmaMage(info);
                case 141://奥玛巫医，3种攻击手段
                    return new OmaWitchDoctor(info);
                case 142://长鼻猴 普通攻击 攻击并净化，回血
                    return new Mandrill(info);
                case 143://瘟疫蟹 雷电攻击
                    return new PlagueCrab(info);
                case 144://攀缘花 
                    return new CreeperPlant(info);
                case 145://幽灵射手 
                    return new FloatingWraith(info);
                case 146://幽灵厨子 破防 
                    return new ArmedPlant(info);
                case 147://淹死的奴隶 
                    return new Nadz(info);
                case 148://复仇的恶灵 
                    return new AvengingSpirit(info);
                case 149://复仇的勇士 
                    return new AvengingWarrior(info);
                case 150://ClawBeast 水手长 
                    return new ClawBeast(info);
                case 151://WoodBox 爆炸箱子 
                    return new WoodBox(info);
                case 152://KillerPlant 黑暗船长 
                    return new KillerPlant(info);
                case 153://FrozenFighter 雪原战士 
                    return new FrozenFighter(info);
                case 154://FrozenKnight 雪原勇士 
                    return new FrozenKnight(info);
                case 155://FrozenGolem 雪原鬼尊 
                    return new FrozenGolem(info);
                case 156://IcePhantom 雪原恶鬼 
                    return new IcePhantom(info);
                case 157://SnowWolf 雪原冰狼 
                    return new SnowWolf(info);
                case 158://SnowWolfKing 雪太狼 
                    return new SnowWolfKing(info);
                case 159://FrozenMiner 冰魄矿工 
                    return new FrozenMiner(info);
                case 160://FrozenAxeman 冰魄斧兵 
                    return new FrozenAxeman(info);
                case 161://FrozenMagician 冰魄法师 
                    return new FrozenMagician(info);
                case 162://SnowYeti 冰魄雪人 
                    return new SnowYeti(info);
                case 163://IceCrystalSoldier 冰晶战士 
                    return new IceCrystalSoldier(info);
                case 164://DarkWraith 暗黑战士 
                    return new DarkWraith(info);
                case 165://DarkSpirit 幽灵战士 
                    return new DarkSpirit(info);
                case 166://CrystalBeast 水晶兽 冰雪守护神 
                    return new CrystalBeast(info);
                case 168://Monster403 紫花仙子
                    return new Monster403(info);
                case 169://Monster404 冰焰鼠
                    return new Monster404(info);
                case 170://Monster405 冰蜗牛
                    return new Monster405(info);
                case 171://Monster406 冰宫战士
                    return new Monster406(info);
                case 172://Monster407 冰宫射手
                    return new Monster407(info);
                case 173://Monster408 冰宫卫士
                    return new Monster408(info);
                case 174://Monster409 虹花仙子
                    return new Monster409(info);
                case 175://Monster410 冰宫鼠卫
                    return new Monster410(info);
                case 176://Monster411 冰宫骑士
                    return new Monster411(info);
                case 177://Monster412 冰宫刀卫
                    return new Monster412(info);
                case 178://Monster413 冰宫护法
                    return new Monster413(info);
                case 179://Monster414 冰宫画卷
                    return new Monster414(info);
                case 180://Monster415 冰宫画卷
                    return new Monster415(info);
                case 181://Monster416 冰宫画卷
                    return new Monster416(info);
                case 182://Monster417 冰宫画卷
                    return new Monster417(info);
                case 183://Monster418 冰宫学者
                    return new Monster418(info);
                case 184://Monster419 冰宫巫师
                    return new Monster419(info);
                case 185://Monster420 冰宫祭师
                    return new Monster420(info);
                case 186://Monster421 冰雪女皇
                    return new Monster421(info);
                //unfinished
                case 253://鸟人像？
                    return new FlamingMutant(info);
                case 254:
                    return new StoningStatue(info);
                //unfinished END


                case 200://custom
                    return new Runaway(info);
                case 201://custom
                    return new TalkingMonster(info);
                case 202://弓箭护卫（战场中的弓箭护卫）
                    return new WarTownArcher(info);
                case 210://custom
                    return new FlameTiger(info);
                case 255://custom
                    return new TestAttackMon(info);
               
                    

                default:
                    return new MonsterObject(info);
            }
        }

        public override ObjectType Race
        {
            get { return ObjectType.Monster; }
        }

        public MonsterInfo Info;
        public MapRespawn Respawn;

        //允许更改怪物名称
        private string _name = null;
        public override string Name
        {
            get {
                if (_name != null)
                {
                    return _name;
                }
                return Master == null ? Info.GameName : string.Format("{0}({1})", Info.GameName, Master.Name);
            }
            set { _name = value; }
        }

        public override int CurrentMapIndex { get; set; }
        public override Point CurrentLocation { get; set; }
        public override sealed MirDirection Direction { get; set; }

        //允许更改怪物等级
        private ushort _Level;
        public override ushort Level
        {
            get {
                if (_Level > 0)
                {
                    return _Level;
                }
                return Info.Level;
            }
            set {
                _Level=value;
            }
        }


        //战场的攻击模式
        private WarGroup _WGroup;
        public override sealed WarGroup WGroup
        {
            get {
                if (Master != null)
                {
                    return Master.WGroup;
                }
                return _WGroup;
            }
            set { _WGroup = value; }
        }


        public override sealed AttackMode AMode
        {
            get
            {
                return base.AMode;
            }
            set
            {
                base.AMode = value;
            }
        }
        public override sealed PetMode PMode
        {
            get
            {
                return base.PMode;
            }
            set
            {
                base.PMode = value;
            }
        }

        public override uint Health
        {
            get { return HP; }
        }

        public override uint MaxHealth
        {
            get { return MaxHP; }
        }

        public uint HP, MaxHP;
        public ushort MoveSpeed;

        //怪物的经验，经验这里太乱了，先根据血量,防御，攻击等计算经验
        //血量在1-1.5倍之间，在100-1万之间
        //
        public virtual uint Experience 
        { 
            get {
                //根据怪物的血量，敏捷，防御，攻击等属性计算怪物的最终经验值
                uint cc = (uint)(Math.Min(Math.Max(0,Info.Agility - 10),20) * 5 + Math.Max(Info.MaxAC-5,0) * 2 + Math.Max(Info.MaxMAC-5,0) * 2 + Math.Max(Info.MaxDC,Info.MaxMC) * 2);
                if(cc> Info.HP*2)
                {
                    cc = Info.HP * 2;
                }
                cc = cc + Info.HP;
                if (cc > 10000)
                {
                    return cc * 3/2;
                }
                return (uint)(cc / 10000.0 * 0.5 * cc) + cc;
            } 
        }
        public int DeadDelay
        {
            get
            {
                switch (Info.AI)
                {
                    case 81:
                    case 82:
                        return int.MaxValue;//这个是城墙，默认不清除,这个有问题啊.城墙需要等玩家自行修复
                    case 252:
                        return 5000;
                    default:
                        return 180000;//默认3分钟清除尸体
                }
            }
        }
        //RegenDelay生命恢复时间10秒恢复一次
        public const int RegenDelay = 10000, EXPOwnerDelay = 5000, RoamDelay = 1000, HealDelay = 600, RevivalDelay = 2000;

        //怪物到处搜索，走动的时间，这个针对怪物攻城后，修改这个时间，让怪物快点搜索
        public int SearchDelay = 3000;
        public long ActionTime, MoveTime, AttackTime, RegenTime, DeadTime, SearchTime, RoamTime, HealTime;
        public long ShockTime, RageTime, HallucinationTime;
        public bool BindingShotCenter, PoisonStopRegen = true;

        public float HealthScale = 0.022F;//生命恢复的比例,外部可以更改这个值

        public byte PetLevel;
        public uint PetExperience;
        public byte MaxPetLevel;
        public long TameTime;//怪物的叛变时间,目前的BB不会叛变？

        public int RoutePoint;
        public bool Waiting;

        //是否副本怪物
        public bool IsCopy = false;
        //这个是怪物的奴隶
        public List<MonsterObject> SlaveList = new List<MonsterObject>();
        public List<RouteInfo> Route = new List<RouteInfo>();

        public override bool Blocking
        {
            get
            {
                return !Dead && !InSafeZone;
                //return false;
            }
        }
        protected virtual bool CanRegen
        {
            get { return Envir.Time >= RegenTime; }
        }
        //是否能移动
        //这里加入数据库的配置
        protected virtual bool CanMove
        {
            get
            {
                if(!Dead && Envir.Time > MoveTime && Envir.Time > ActionTime && Envir.Time > ShockTime &&
                       (Master == null || Master.PMode == PetMode.MoveOnly || Master.PMode == PetMode.Both) && !CurrentPoison.HasFlag(PoisonType.Paralysis)
                       && !CurrentPoison.HasFlag(PoisonType.LRParalysis) && !CurrentPoison.HasFlag(PoisonType.Stun) && !CurrentPoison.HasFlag(PoisonType.Frozen))
                {
                    return Info.CanMove;
                }
                else
                {
                    return false;
                }
            }
        }
        protected virtual bool CanAttack
        {
            get
            {
                return !Dead && Envir.Time > AttackTime && Envir.Time > ActionTime &&
                     (Master == null || Master.PMode == PetMode.AttackOnly || Master.PMode == PetMode.Both || !CurrentMap.Info.NoFight) && !CurrentPoison.HasFlag(PoisonType.Paralysis)
                       && !CurrentPoison.HasFlag(PoisonType.LRParalysis) && !CurrentPoison.HasFlag(PoisonType.Stun) && !CurrentPoison.HasFlag(PoisonType.Frozen);
            }
        }

        protected internal MonsterObject(MonsterInfo info)
        {
            Info = info;

            Undead = Info.Undead;
            AutoRev = info.AutoRev;
            CoolEye = info.CoolEye > RandomUtils.Next(100);
            Direction = (MirDirection)RandomUtils.Next(8);

            AMode = AttackMode.All;
            PMode = PetMode.Both;

            RegenTime = RandomUtils.Next(RegenDelay) + Envir.Time;
            SearchTime = RandomUtils.Next(SearchDelay) + Envir.Time;
            RoamTime = RandomUtils.Next(RoamDelay) + Envir.Time;
        }

        //这个是召唤一个新的怪物，更改了怪物的各种属性的，不用
        public bool SpawnNew(Map temp, Point location)
        {
            CurrentMap = temp;
            if(location== Point.Empty)
            {
                CurrentLocation = temp.RandomValidPoint();
            }
            else
            {
                CurrentLocation = location;
            }
           

            CurrentMap.AddObject(this);

            //RefreshAll();
            SetHP(MaxHP);

            Spawned();
            Envir.MonsterCount++;
            CurrentMap.MonsterCount++;
            return true;
        }

        //随机刷到地图某个位置
        public bool Spawn(Map temp)
        {
            return Spawn(temp, temp.RandomValidPoint());
        }
        //这个重生是在当前位置重生，如僵尸的复活
        //如怪物召唤小怪，也是通过这个方法进行召唤的
        public bool Spawn(Map temp, Point location)
        {
            if (!temp.ValidPoint(location)) return false;

            CurrentMap = temp;
            CurrentLocation = location;

            CurrentMap.AddObject(this);

            RefreshAll();
            SetHP(MaxHP);

            Spawned();
            Envir.MonsterCount++;
            CurrentMap.MonsterCount++;
            return true;
        }
        //怪物重生(这个是根据配置进行刷怪)
        //这个迁移到MapRespawn类中
        public bool Spawn_back(MapRespawn respawn)
        {
            Respawn = respawn;

            if (Respawn.Map == null) return false;
    
            for (int i = 0; i < 10; i++)
            {
                CurrentLocation = new Point(Respawn.Info.Location.X + RandomUtils.Next(-Respawn.Info.Spread, Respawn.Info.Spread + 1),
                                            Respawn.Info.Location.Y + RandomUtils.Next(-Respawn.Info.Spread, Respawn.Info.Spread + 1));

                if (!respawn.Map.ValidPoint(CurrentLocation)) continue;

                respawn.Map.AddObject(this);

                CurrentMap = respawn.Map;

                if (Respawn.Route.Count > 0)
                    Route.AddRange(Respawn.Route);

                RefreshAll();
                SetHP(MaxHP);

                Spawned();
                Respawn.Count++;
                respawn.Map.MonsterCount++;
                Envir.MonsterCount++;
                return true;
            }
            return false;
        }

        public override void Spawned()
        {
            base.Spawned();
            ActionTime = Envir.Time + 2000;
            if (Info.HasSpawnScript && (SMain.Envir.MonsterNPC != null))
            {
                SMain.Envir.MonsterNPC.Call(this,string.Format("[@_SPAWN({0})]", Info.Index));
            }
        }

        protected virtual void RefreshBase()
        {
            MaxHP = Info.HP;
            MinAC = Info.MinAC;
            MaxAC = Info.MaxAC;
            MinMAC = Info.MinMAC;
            MaxMAC = Info.MaxMAC;
            MinDC = Info.MinDC;
            MaxDC = Info.MaxDC;
            MinMC = Info.MinMC;
            MaxMC = Info.MaxMC;
            MinSC = Info.MinSC;
            MaxSC = Info.MaxSC;
            Accuracy = Info.Accuracy;
            Agility = Info.Agility;

            MoveSpeed = Info.MoveSpeed;
            AttackSpeed = Info.AttackSpeed;
        }

        //这里更改宠物的不同等级的血量
        //血量计算，每级增加初始血量的20%，递增模式
        //道士的宝宝血量成长快，法师等宝宝血量每级固定加20
        public virtual void RefreshAll()
        {
            RefreshBase();
                MinAC = (ushort)Math.Min(ushort.MaxValue, MinAC + PetLevel * 2);
                MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + PetLevel * 2);
                MinMAC = (ushort)Math.Min(ushort.MaxValue, MinMAC + PetLevel * 2);
                MaxMAC = (ushort)Math.Min(ushort.MaxValue, MaxMAC + PetLevel * 2);
                MinDC = (ushort)Math.Min(ushort.MaxValue, MinDC *1.0* (10 + PetLevel/2.0) / 10.0);//攻击成长，最高成长1.35倍攻击
                MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC *1.0* (10 + PetLevel/2.0) / 10.0);//攻击成长，最高成长1.35倍攻击

            if (Info.Name.StartsWith(Settings.SkeletonName) ||Info.Name.IndexOf(Settings.ShinsuName)!=-1 ||Info.Name == Settings.AngelName) 
            {
                MoveSpeed = (ushort)Math.Min(ushort.MaxValue, (Math.Max(ushort.MinValue, MoveSpeed - MaxPetLevel * 130)));
                AttackSpeed = (ushort)Math.Min(ushort.MaxValue, (Math.Max(ushort.MinValue, AttackSpeed - MaxPetLevel * 70)));
                MaxHP = getPetUp(PetLevel, MaxHP, 2);
            }
            else
            {
                MaxHP = (uint)Math.Min(uint.MaxValue, MaxHP + PetLevel * 20);
            }
            if (MoveSpeed < 400) MoveSpeed = 400;
            if (AttackSpeed < 400) AttackSpeed = 400;

            RefreshBuffs();
        }

        //宝宝成长算法,主要用于血量成长
        private uint getPetUp(int level, uint basepar, int ratio)
        {
            if (level <= 0)
            {
                return basepar;
            }
            uint currsta = basepar;
            for (int i = 1; i <= level; i++)
            {
                uint cz = (uint)(basepar * ratio/10 * (i + 1));//这个是成长
                currsta = currsta + cz;
            }
            return currsta;
        }

        protected virtual void RefreshBuffs()
        {
            for (int i = 0; i < Buffs.Count; i++)
            {
                Buff buff = Buffs[i];

                if (buff.Values == null || buff.Values.Length < 1) continue;

                switch (buff.Type)
                {
                    case BuffType.Haste:
                        ASpeed = (sbyte)Math.Max(sbyte.MinValue, (Math.Min(sbyte.MaxValue, ASpeed + buff.Values[0])));
                        break;
                    case BuffType.SwiftFeet:
                        MoveSpeed = (ushort)Math.Max(ushort.MinValue, MoveSpeed + 100 * buff.Values[0]);
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
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + buff.Values[0]);
                        break;
                    case BuffType.Curse:
                        ushort rMaxDC = (ushort)(((int)MaxDC / 100) * buff.Values[0]);
                        ushort rMaxMC = (ushort)(((int)MaxMC / 100) * buff.Values[0]);
                        ushort rMaxSC = (ushort)(((int)MaxSC / 100) * buff.Values[0]);
                        sbyte rASpeed = (sbyte)(((int)ASpeed / 100) * buff.Values[0]);
                        ushort rMSpeed = (ushort)((MoveSpeed / 100) * buff.Values[0]);

                        MaxDC = (ushort)Math.Max(ushort.MinValue, MaxDC - rMaxDC);
                        MaxMC = (ushort)Math.Max(ushort.MinValue, MaxMC - rMaxMC);
                        MaxSC = (ushort)Math.Max(ushort.MinValue, MaxSC - rMaxSC);
                        ASpeed = (sbyte)Math.Min(sbyte.MaxValue, (Math.Max(sbyte.MinValue, ASpeed - rASpeed)));
                        MoveSpeed = (ushort)Math.Max(ushort.MinValue, MoveSpeed - rMSpeed);
                        break;

                    case BuffType.PetEnhancer:
                        MinDC = (ushort)Math.Min(ushort.MaxValue, MinDC + buff.Values[0]);
                        MaxDC = (ushort)Math.Min(ushort.MaxValue, MaxDC + buff.Values[0]);
                        MinAC = (ushort)Math.Min(ushort.MaxValue, MinAC + buff.Values[1]);
                        MaxAC = (ushort)Math.Min(ushort.MaxValue, MaxAC + buff.Values[1]);
                        break;
                }

            }
        }
        public void RefreshNameColour(bool send = true)
        {
            if (ShockTime < Envir.Time) BindingShotCenter = false;

            Color colour = Color.White;
            if(ChangeNameColour!= Color.White)
            {
                colour = ChangeNameColour;
            }
            switch (PetLevel)
            {
                case 1:
                    colour = Color.Aqua;
                    break;
                case 2:
                    colour = Color.Aquamarine;
                    break;
                case 3:
                    colour = Color.LightSeaGreen;
                    break;
                case 4:
                    colour = Color.SlateBlue;
                    break;
                case 5:
                    colour = Color.SteelBlue;
                    break;
                case 6:
                    colour = Color.Blue;
                    break;
                case 7:
                    colour = Color.Navy;
                    break;
            }

            if (Envir.Time < ShockTime)
                colour = Color.Peru;
            else if (Envir.Time < RageTime)
                colour = Color.Red;
            else if (Envir.Time < HallucinationTime)
                colour = Color.MediumOrchid;

            if (colour == NameColour || !send) return;

            NameColour = colour;

            Broadcast(new S.ObjectColourChanged { ObjectID = ObjectID, NameColour = NameColour });
        }

        public void SetHP(uint amount)
        {
            if (HP == amount) return;

            HP = amount <= MaxHP ? amount : MaxHP;

            if (!Dead && HP == 0) Die();

            //  HealthChanged = true;
            BroadcastHealthChange();
        }
        public virtual void ChangeHP(int amount)
        {

            uint value = (uint)Math.Max(uint.MinValue, Math.Min(MaxHP, HP + amount));

            if (value == HP) return;

            HP = value;

            if (!Dead && HP == 0) Die();

           // HealthChanged = true;
            BroadcastHealthChange();
        }

        //use this so you can have mobs take no/reduced poison damage
        public virtual void PoisonDamage(int amount, MapObject Attacker)
        {
            ChangeHP(amount);
        }


        public override bool Teleport(Map temp, Point location, bool effects = true, byte effectnumber = 0)
        {
            if (temp == null || !temp.ValidPoint(location)) return false;

            CurrentMap.RemoveObject(this);
            if (effects) Broadcast(new S.ObjectTeleportOut { ObjectID = ObjectID, Type = effectnumber });
            Broadcast(new S.ObjectRemove { ObjectID = ObjectID });
            
            CurrentMap.MonsterCount--;

            CurrentMap = temp;
            CurrentLocation = location;
            
            CurrentMap.MonsterCount++;

            InTrapRock = false;

            CurrentMap.AddObject(this);
            BroadcastInfo();

            if (effects) Broadcast(new S.ObjectTeleportIn { ObjectID = ObjectID, Type = effectnumber });

            BroadcastHealthChange();

            return true;
        }

        //怪物死亡
        public override void Die()
        {
            try
            {
                if (Dead) return;

                HP = 0;
                Dead = true;

                DeadTime = Envir.Time + DeadDelay;

                Broadcast(new S.ObjectDied { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
                //触发死亡脚本
                if (Info != null && Info.HasDieScript && (SMain.Envir.MonsterNPC != null))
                {
                    SMain.Envir.MonsterNPC.Call(this, string.Format("[@_DIE({0})]", Info.Index));
                }

                //经验拥有者
                if (EXPOwner != null && Master == null && EXPOwner.Race == ObjectType.Player)
                {
                    EXPOwner.WinExp(Experience, Level);

                    PlayerObject playerObj = (PlayerObject)EXPOwner;
                    playerObj.CheckGroupQuestKill(Info);
                    if (!IsCopy)
                    {
                        playerObj.killMon(this);
                    }
                }
                //如果是副本怪物，则调用副本处理方法
                if (IsCopy && CurrentMap != null && CurrentMap.mapSProcess != null)
                {
                    CurrentMap.mapSProcess.monDie(this);
                }


                if (Respawn != null)
                    Respawn.Count--;

                //没有主人，并且有经验拥有者才爆东西
                if (Master == null && EXPOwner != null)
                    Drop();

                Master = null;

                PoisonList.Clear();
                Envir.MonsterCount--;
                //这里之前会空指针，坑死人咩
                if (CurrentMap != null)
                {
                    CurrentMap.MonsterCount--;
                }
            }
            catch(Exception e)
            {
                SMain.Enqueue(e);
            }
        }
        //复活
        public void Revive(uint hp, bool effect)
        {
            if (!Dead) return;

            SetHP(hp);

            Dead = false;
            ActionTime = Envir.Time + RevivalDelay;

            Broadcast(new S.ObjectRevived { ObjectID = ObjectID, Effect = effect });

            if (Respawn != null)
                Respawn.Count++;

            Envir.MonsterCount++;
            CurrentMap.MonsterCount++;
        }

        //被推动
        public override int Pushed(MapObject pusher, MirDirection dir, int distance)
        {
            if (!Info.CanPush) return 0;
            //if (!CanMove) return 0; //stops mobs that can't move (like cannibalplants) from being pushed

            int result = 0;
            MirDirection reverse = Functions.ReverseDirection(dir);
            for (int i = 0; i < distance; i++)
            {
                Point location = Functions.PointMove(CurrentLocation, dir, 1);

                if (!CurrentMap.ValidPoint(location)) return result;

                //Cell cell = CurrentMap.GetCell(location);

                bool stop = false;
                if (CurrentMap.Objects[location.X, location.Y] != null)
                    for (int c = 0; c < CurrentMap.Objects[location.X, location.Y].Count; c++)
                    {
                        MapObject ob = CurrentMap.Objects[location.X, location.Y][c];
                        if (!ob.Blocking) continue;
                        stop = true;
                    }
                if (stop) break;

                CurrentMap.Remove(CurrentLocation.X, CurrentLocation.Y,this);

                Direction = reverse;
                RemoveObjects(dir, 1);
                CurrentLocation = location;
                //CurrentMap.GetCell(CurrentLocation).Add(this);
                CurrentMap.Add(CurrentLocation.X, CurrentLocation.Y, this);
                AddObjects(dir, 1);

                Broadcast(new S.ObjectPushed { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });

                result++;
            }

            ActionTime = Envir.Time + 300 * result;
            MoveTime = Envir.Time + 500 * result;

            if (result > 0)
            {
                //Cell cell = CurrentMap.GetCell(CurrentLocation);

                for (int i = 0; i < CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y].Count; i++)
                {
                    if (CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i].Race != ObjectType.Spell) continue;
                    SpellObject ob = (SpellObject)CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i];

                    ob.ProcessSpell(this);
                    //break;
                }
            }

            return result;
        }
        //怪物掉落物品
        protected virtual void Drop()
        {
            for (int i = 0; i < Info.Drops.Count; i++)
            {
                DropInfo drop = Info.Drops[i];
                float DropRate = 1;
                float addGold = 0;

                if (EXPOwner != null)
                {
                    if(EXPOwner.ItemDropRateOffset > 0)
                    {
                        DropRate = DropRate * (1 + EXPOwner.ItemDropRateOffset / 100.0f);
                    }
                    if (EXPOwner.GoldDropRateOffset > 0)
                    {
                        addGold = drop.Gold * (EXPOwner.GoldDropRateOffset / 100.0f);
                    }
                }
              
                
                if (CurrentMap != null && CurrentMap.Info != null)
                {
                    DropRate = DropRate * CurrentMap.Info.DropRate;
                }
                if (!drop.isDrop(DropRate))
                {
                    continue;
                }
                //掉落金币
                if (drop.Gold > 0)
                {
                    uint gold = drop.DropGold(addGold);
                    if (gold <= 0) continue;
                    if (!DropGold((uint)gold)) return;
                }
                else
                {
                    //掉落物品
                    List<ItemInfo> dropItems = drop.DropItems();
                    if (dropItems == null || dropItems.Count==0)
                    {
                        continue;
                    }
                    foreach(ItemInfo ditem in dropItems)
                    {
                        UserItem item = ditem.CreateDropItem();
                        if (item == null) continue;
                        if (EXPOwner != null && EXPOwner.Race == ObjectType.Player)
                        {
                            PlayerObject ob = (PlayerObject)EXPOwner;

                            if (ob.CheckGroupQuestItem(item))
                            {
                                continue;
                            }
                        }

                        if (drop.QuestRequired) continue;
                        //增加物品的来源
                        if (EXPOwner != null && ditem.StackSize==1)
                        {
                            item.src_time = Envir.Now.ToBinary();
                            item.src_mon = this.Name;
                            item.src_kill = EXPOwner.Name;
                            item.src_map = CurrentMap.getTitle();
                        }
                        if (!DropItem(item)) return;
                    }
                }
            }
        }
        //掉落物品处理
        protected virtual bool DropItem(UserItem item)
        {
            if (CurrentMap.Info.NoDropMonster)
                return false;

            ItemObject ob = new ItemObject(this, item)
            {
                Owner = EXPOwner,
                OwnerTime = Envir.Time + Settings.Minute,
            };
            //如果物品需要通知，则发送通知
            if (item.Info.GlobalDropNotify)
            {
                foreach (var player in Envir.Players)
                {
                    player.ReceiveChat($"{Name} 掉落 {item.FriendlyName}，在{CurrentMap.getTitle()}", ChatType.System2);
                }
            }
            return ob.Drop(Settings.DropRange);
        }

        protected virtual bool DropGold(uint gold)
        {
            if (EXPOwner != null && EXPOwner.CanGainGold(gold) && !Settings.DropGold)
            {
                EXPOwner.WinGold(gold);
                return true;
            }

            uint count = gold / Settings.MaxDropGold == 0 ? 1 : gold / Settings.MaxDropGold + 1;
            for (int i = 0; i < count; i++)
            {
                ItemObject ob = new ItemObject(this, i != count - 1 ? Settings.MaxDropGold : gold % Settings.MaxDropGold)
                {
                    Owner = EXPOwner,
                    OwnerTime = Envir.Time + Settings.Minute,
                };

                ob.Drop(Settings.DropRange);
            }

            return true;
        }
        //死循环调用入口，这个用trycatch 包裹，因为这里非常多逻辑，非常容易发生异常
        //死循环处理
        public override void Process()
        {
            try
            {
                base.Process();

                RefreshNameColour();

                if (Target != null && (Target.CurrentMap != CurrentMap || !Target.IsAttackTarget(this) || !Functions.InRange(CurrentLocation, Target.CurrentLocation, Globals.DataRange)))
                    Target = null;

                for (int i = SlaveList.Count - 1; i >= 0; i--)
                    if (SlaveList[i].Dead || SlaveList[i].Node == null)
                        SlaveList.RemoveAt(i);

                if (Dead && Envir.Time >= DeadTime)
                {
                    CurrentMap.RemoveObject(this);
                    if (Master != null)
                    {
                        Master.Pets.Remove(this);
                        Master = null;
                    }

                    Despawn();
                    return;
                }

                if (Master != null && TameTime > 0 && Envir.Time >= TameTime)
                {
                    Master.Pets.Remove(this);
                    Master = null;
                    Broadcast(new S.ObjectName { ObjectID = ObjectID, Name = Name });
                }

                ProcessAI();

                ProcessBuffs();
                ProcessRegen();
                ProcessPoison();

            }
            catch (Exception e)
            {
                SMain.Enqueue(e);
            }
            

             /*if (!HealthChanged) return;
            HealthChanged = false;
            
            BroadcastHealthChange();*/
        }

        //这个是干嘛的，设置操作的时间？
        public override void SetOperateTime()
        {
            long time = Envir.Time + 2000;

            if (DeadTime < time && DeadTime > Envir.Time)
                time = DeadTime;

            if (OwnerTime < time && OwnerTime > Envir.Time)
                time = OwnerTime;

            if (ExpireTime < time && ExpireTime > Envir.Time)
                time = ExpireTime;

            if (PKPointTime < time && PKPointTime > Envir.Time)
                time = PKPointTime;

            if (LastHitTime < time && LastHitTime > Envir.Time)
                time = LastHitTime;

            if (EXPOwnerTime < time && EXPOwnerTime > Envir.Time)
                time = EXPOwnerTime;

            if (SearchTime < time && SearchTime > Envir.Time)
                time = SearchTime;

            if (RoamTime < time && RoamTime > Envir.Time)
                time = RoamTime;


            if (ShockTime < time && ShockTime > Envir.Time)
                time = ShockTime;

            if (RegenTime < time && RegenTime > Envir.Time && Health < MaxHealth)
                time = RegenTime;

            if (RageTime < time && RageTime > Envir.Time)
                time = RageTime;

            if (HallucinationTime < time && HallucinationTime > Envir.Time)
                time = HallucinationTime;

            if (ActionTime < time && ActionTime > Envir.Time)
                time = ActionTime;

            if (MoveTime < time && MoveTime > Envir.Time)
                time = MoveTime;

            if (AttackTime < time && AttackTime > Envir.Time)
                time = AttackTime;

            if (HealTime < time && HealTime > Envir.Time && HealAmount > 0)
                time = HealTime;

            if (BrownTime < time && BrownTime > Envir.Time)
                time = BrownTime;

            for (int i = 0; i < ActionList.Count; i++)
            {
                if (ActionList[i].Time >= time && ActionList[i].Time > Envir.Time) continue;
                time = ActionList[i].Time;
            }

            for (int i = 0; i < PoisonList.Count; i++)
            {
                if (PoisonList[i].TickTime >= time && PoisonList[i].TickTime > Envir.Time) continue;
                time = PoisonList[i].TickTime;
            }

            for (int i = 0; i < Buffs.Count; i++)
            {
                if (Buffs[i].ExpireTime >= time && Buffs[i].ExpireTime > Envir.Time) continue;
                time = Buffs[i].ExpireTime;
            }


            if (OperateTime <= Envir.Time || time < OperateTime)
                OperateTime = time;
        }

        public override void Process(DelayedAction action)
        {
            switch (action.Type)
            {
                case DelayedType.Damage:
                    CompleteAttack(action.Params);
                    break;
                case DelayedType.RangeDamage:
                    CompleteRangeAttack(action.Params);
                    break;
                case DelayedType.Die:
                    CompleteDeath(action.Params);
                    break;
                case DelayedType.MapMovement:
                    CompleteMapMovement(action.Params);
                    break;
                case DelayedType.Recall:
                    PetRecall();
                    break;
            }
        }

        public void PetRecall()
        {
            if (Master == null) return;
            if (!Teleport(Master.CurrentMap, Master.Back))
                Teleport(Master.CurrentMap, Master.CurrentLocation);
        }
        protected virtual void CompleteAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            target.Attacked(this, damage, defence);
        }

        protected virtual void CompleteRangeAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            target.Attacked(this, damage, defence);
        }

        protected virtual void CompleteDeath(IList<object> data)
        {
            throw new NotImplementedException();
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
            
            CurrentMap.RemoveObject(this);
            CurrentMap.MonsterCount--;
            Broadcast(new S.ObjectRemove { ObjectID = ObjectID });

            CurrentMap = temp;
            CurrentLocation = destination;
            //
            CurrentMap.AddObject(this);
            CurrentMap.MonsterCount++;
            InTrapRock = false;
            BroadcastInfo();

            //if (effects) Broadcast(new S.ObjectTeleportIn { ObjectID = ObjectID, Type = effectnumber });

        }

        protected virtual void ProcessRegen()
        {
            if (Dead) return;

            int healthRegen = 0;
            //这个是怪物的生命恢复
            if (CanRegen)
            {
                RegenTime = Envir.Time + RegenDelay;


                if (HP < MaxHP)
                    healthRegen += (int)(MaxHP * HealthScale) + 1;
            }


            if (Envir.Time > HealTime)
            {
                HealTime = Envir.Time + HealDelay;

                if (HealAmount > 5)
                {
                    healthRegen += 5;
                    HealAmount -= 5;
                }
                else
                {
                    healthRegen += HealAmount;
                    HealAmount = 0;
                }
            }

            if (healthRegen > 0) ChangeHP(healthRegen);
            if (HP == MaxHP) HealAmount = 0;
        }
        protected virtual void ProcessPoison()
        {
            PoisonType type = PoisonType.None;
            ArmourRate = 1F;
            DamageRate = 1F;

            for (int i = PoisonList.Count - 1; i >= 0; i--)
            {
                //加多一个判断，避免数组越界啊，这里有点坑的
                if (Dead || PoisonList.Count==0) return;

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

                    if (poison.Time > poison.Duration)
                    {
                        PoisonList.RemoveAt(i);
                        //这里直接返回
                        continue;
                    }
                        
                    //绿毒拉仇恨太猛了，看要不要改
                    if (poison.PType == PoisonType.Green || poison.PType == PoisonType.Bleeding)
                    {
                        if (EXPOwner == null || EXPOwner.Dead)
                        {
                            EXPOwner = poison.Owner;
                            EXPOwnerTime = Envir.Time + EXPOwnerDelay;
                        }
                        else if (EXPOwner == poison.Owner)
                        {
                            //这里可以改成前面5下掉血才拉仇恨的
                            EXPOwnerTime = Envir.Time + EXPOwnerDelay;
                        }
                           

                        if (poison.PType == PoisonType.Bleeding)
                        {
                            Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Bleeding, EffectType = 0 });
                        }

                        //ChangeHP(-poison.Value);
                        PoisonDamage(-poison.Value, poison.Owner);
                        if (PoisonStopRegen)
                            RegenTime = Envir.Time + RegenDelay;
                    }

                    if (poison.PType == PoisonType.DelayedExplosion)
                    {
                        if (Envir.Time > ExplosionInflictedTime) ExplosionInflictedStage++;

                        if (!ProcessDelayedExplosion(poison))
                        {
                            ExplosionInflictedStage = 0;
                            ExplosionInflictedTime = 0;

                            if (Dead) break; //temp to stop crashing

                            PoisonList.RemoveAt(i);
                            continue;
                        }
                    }
                }
                //中毒护甲减半？
                switch (poison.PType)
                {
                    case PoisonType.Red:
                        ArmourRate -= 0.5F;
                        break;
                    case PoisonType.Stun:
                        DamageRate += 0.5F;
                        break;
                    case PoisonType.Slow:
                        MoveSpeed += 100;
                        AttackSpeed += 100;
 
                        if (poison.Time >= poison.Duration)
                        {
                            MoveSpeed = Info.MoveSpeed;
                            AttackSpeed = Info.AttackSpeed;
                            //这里增加代码，解决道士宝宝中毒后无法回复攻速，移速
                            if (Info.Name == Settings.SkeletonName || Info.Name == Settings.ShinsuName || Info.Name == Settings.AngelName)
                            {
                                MoveSpeed = (ushort)Math.Min(ushort.MaxValue, (Math.Max(ushort.MinValue, MoveSpeed - MaxPetLevel * 130)));
                                AttackSpeed = (ushort)Math.Min(ushort.MaxValue, (Math.Max(ushort.MinValue, AttackSpeed - MaxPetLevel * 70)));
                            }
                        }
                        break;
                }
                type |= poison.PType;
                /*
                if ((int)type < (int)poison.PType)
                    type = poison.PType;
                 */
            }

            
            if (type == CurrentPoison) return;

            CurrentPoison = type;
            Broadcast(new S.ObjectPoisoned { ObjectID = ObjectID, Poison = type });
        }

        private bool ProcessDelayedExplosion(Poison poison)
        {
            if (Dead) return false;

            if (ExplosionInflictedStage == 0)
            {
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 0 });
                return true;
            }
            if (ExplosionInflictedStage == 1)
            {
                if (Envir.Time > ExplosionInflictedTime)
                    ExplosionInflictedTime = poison.TickTime + 3000;
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion, EffectType = 1 });
                return true;
            }
            if (ExplosionInflictedStage == 2)
            {
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


        private void ProcessBuffs()
        {
            bool refresh = false;
            for (int i = Buffs.Count - 1; i >= 0; i--)
            {
                Buff buff = Buffs[i];

                if (Envir.Time <= buff.ExpireTime) continue;

                Buffs.RemoveAt(i);

                switch (buff.Type)
                {
                    case BuffType.MoonLight:
                    case BuffType.Hiding:
                    case BuffType.DarkBody:
                        Hidden = false;
                        break;
                }

                refresh = true;
            }

            if (refresh) RefreshAll();
        }
        protected virtual void ProcessAI()
        {
            if (Dead) return;

            if (Master != null)
            {
                if ((Master.PMode == PetMode.Both || Master.PMode == PetMode.MoveOnly))
                {
                    if (!Functions.InRange(CurrentLocation, Master.CurrentLocation, Globals.DataRange) || CurrentMap != Master.CurrentMap)
                        PetRecall();
                }

                if (Master.PMode == PetMode.MoveOnly || Master.PMode == PetMode.None)
                    Target = null;
            }
            //搜索目标，3秒搜索一次
            ProcessSearch();
            //到处漫游
            ProcessRoam();
            //处理攻击目标，如果有的话
            ProcessTarget();
        }
        //怪物到处搜索
        protected virtual void ProcessSearch()
        {
            if (Envir.Time < SearchTime) return;
            if (Master != null && (Master.PMode == PetMode.MoveOnly || Master.PMode == PetMode.None)) return;

            SearchTime = Envir.Time + SearchDelay;

            if (CurrentMap.Inactive(5)) return;

            //Stacking or Infront of master - Move
            bool stacking = CheckStacked();

            if (CanMove && ((Master != null && Master.Front == CurrentLocation) || stacking))
            {
                //Walk Randomly
                if (!Walk(Direction))
                {
                    MirDirection dir = Direction;

                    switch (RandomUtils.Next(3)) // favour Clockwise
                    {
                        case 0:
                            for (int i = 0; i < 7; i++)
                            {
                                dir = Functions.NextDir(dir);

                                if (Walk(dir))
                                    break;
                            }
                            break;
                        default:
                            for (int i = 0; i < 7; i++)
                            {
                                dir = Functions.PreviousDir(dir);

                                if (Walk(dir))
                                    break;
                            }
                            break;
                    }
                }
            }

            if (Target == null || RandomUtils.Next(3) == 0)
                FindTarget();
        }

        //怪物到处走动，漫游
        protected virtual void ProcessRoam()
        {
            if (Target != null || Envir.Time < RoamTime) return;

            if (ProcessRoute()) return;

            if (CurrentMap.Inactive(5)) return;

            if (Master != null)
            {
                MoveTo(Master.Back);
                return;
            }

            RoamTime = Envir.Time + RoamDelay;
            if (RandomUtils.Next(10) != 0) return;

            switch (RandomUtils.Next(3)) //Face Walk
            {
                case 0:
                    Turn((MirDirection)RandomUtils.Next(8));
                    break;
                default:
                    Walk(Direction);
                    break;
            }
        }
        protected virtual void ProcessTarget()
        {
            if (Target == null || !CanAttack) return;

            if (InAttackRange())
            {
                Attack();
                if (Target==null||Target.Dead)
                    FindTarget();

                return;
            }

            if (Envir.Time < ShockTime)
            {
                Target = null;
                return;
            }
            
            MoveTo(Target.CurrentLocation);
        }
        protected virtual bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;

            return Target.CurrentLocation != CurrentLocation && Functions.InRange(CurrentLocation, Target.CurrentLocation, 1);
        }
        //这个是怪物的仇恨？
        //这个循环很坑啊,这里比较耗时的，看下是否优化，如何优化哦
        protected virtual void FindTarget()
        {
            //if (CurrentMap.Players.Count < 1) return;
            Map Current = CurrentMap;

            for (int d = 0; d <= Info.ViewRange; d++)
            {
                for (int y = CurrentLocation.Y - d; y <= CurrentLocation.Y + d; y++)
                {
                    if (y < 0) continue;
                    if (y >= Current.Height) break;

                    for (int x = CurrentLocation.X - d; x <= CurrentLocation.X + d; x += Math.Abs(y - CurrentLocation.Y) == d ? 1 : d*2)
                    {
                        if (x < 0) continue;
                        if (x >= Current.Width) break;
                        //Cell cell = Current.Cells[x, y];
                        if (Current.Objects[x,y] == null || !Current.Valid(x,y)) continue;
                        for (int i = 0; i < Current.Objects[x, y].Count; i++)
                        {
                            MapObject ob = Current.Objects[x, y][i];
                            switch (ob.Race)
                            {
                                case ObjectType.Monster:
                                    if (!ob.IsAttackTarget(this)) continue;
                                    if (ob.Hidden && (!CoolEye || Level < ob.Level)) continue;
                                    if (this is TrapRock && ob.InTrapRock) continue;
                                    Target = ob;
                                    return;
                                case ObjectType.Player:
                                    PlayerObject playerob = (PlayerObject)ob;
                                    if (!ob.IsAttackTarget(this)) continue;
                                    if (playerob.GMGameMaster || ob.Hidden && (!CoolEye || Level < ob.Level) || Envir.Time < HallucinationTime) continue;
                                    //GM观察模式，不拉仇恨
                                    if (playerob.IsGM && playerob.Observer) continue;

                                    Target = ob;

                                    if (Master != null)
                                    {
                                        for (int j = 0; j < playerob.Pets.Count; j++)
                                        {
                                            MonsterObject pet = playerob.Pets[j];

                                            if (!pet.IsAttackTarget(this)) continue;
                                            Target = pet;
                                            break;
                                        }
                                    }
                                    return;
                                default:
                                    continue;
                            }
                        }
                    }
                }
            }
        }

        protected virtual bool ProcessRoute()
        {
            if (Route.Count < 1) return false;

            RoamTime = Envir.Time + 500;

            if (CurrentLocation == Route[RoutePoint].Location)
            {
                if (Route[RoutePoint].Delay > 0 && !Waiting)
                {
                    Waiting = true;
                    RoamTime = Envir.Time + RoamDelay + Route[RoutePoint].Delay;
                    return true;
                }

                Waiting = false;
                RoutePoint++;
            }

            if (RoutePoint > Route.Count - 1) RoutePoint = 0;

            if (!CurrentMap.ValidPoint(Route[RoutePoint].Location)) return true;

            MoveTo(Route[RoutePoint].Location);

            return true;
        }
        //向某个方向移动
        protected virtual void MoveTo(Point location)
        {
            if (CurrentLocation == location) return;

            bool inRange = Functions.InRange(location, CurrentLocation, 1);
            //如果只是1格，直接走过去
            if (inRange)
            {
                if (!CurrentMap.EmptyPoint(location.X, location.Y)) return;
            }

            MirDirection dir = Functions.DirectionFromPoint(CurrentLocation, location);
            //向某个方向走
            if (Walk(dir)) return;
            //方向走不通，则随机向一个方向走
            switch (RandomUtils.Next(2)) //No favour
            {
                case 0:
                    for (int i = 0; i < 7; i++)
                    {
                        dir = Functions.NextDir(dir);

                        if (Walk(dir))
                            return;
                    }
                    break;
                default:
                    for (int i = 0; i < 7; i++)
                    {
                        dir = Functions.PreviousDir(dir);

                        if (Walk(dir))
                            return;
                    }
                    break;
            }
        }

        //向某个方向移动
        //如果不能移动，则穿墙飞过去
        public  void MoveAndFly(Point location)
        {
            if (CurrentLocation == location) return;

            bool inRange = Functions.InRange(location, CurrentLocation, 1);
            //如果只是1格，直接走过去
            if (inRange)
            {
                if (!CurrentMap.EmptyPoint(location.X, location.Y)) return;
            }

            MirDirection dir = Functions.DirectionFromPoint(CurrentLocation, location);
            //向某个方向走
            if (Walk(dir)) return;

            //方向走不通，10分之1几率飞过去，避免卡死在那里
            if (RandomUtils.Next(5) == 1)
            {
                for (int i = 2; i < 10; i++)
                {
                    Point mp = Functions.PointMove(CurrentLocation, dir, i);
                    if (!CurrentMap.EmptyPoint(mp.X, mp.Y)) continue;
                    Teleport(CurrentMap, mp);
                    return;
                }
            }

            switch (RandomUtils.Next(2)) //No favour
            {
                case 0:
                    for (int i = 0; i < 7; i++)
                    {
                        dir = Functions.NextDir(dir);

                        if (Walk(dir))
                            return;
                    }
                    break;
                default:
                    for (int i = 0; i < 7; i++)
                    {
                        dir = Functions.PreviousDir(dir);

                        if (Walk(dir))
                            return;
                    }
                    break;
            }
        }

        public virtual void Turn(MirDirection dir)
        {
            if (!CanMove) return;

            Direction = dir;
                
            InSafeZone = CurrentMap.GetSafeZone(CurrentLocation) != null;


            //Cell cell = CurrentMap.GetCell(CurrentLocation);

            for (int i = 0; i < CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y].Count; i++)
            {
                if (CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i].Race != ObjectType.Spell) continue;
                SpellObject ob = (SpellObject)CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i];

                ob.ProcessSpell(this);
                //break;
            }


            Broadcast(new S.ObjectTurn { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
        }


        public virtual bool Walk(MirDirection dir) 
        {
            if (!CanMove) return false;

            Point location = Functions.PointMove(CurrentLocation, dir, 1);

            if (!CurrentMap.ValidPoint(location)) return false;

            //Cell cell = CurrentMap.GetCell(location);

            if (CurrentMap.Objects[location.X, location.Y] != null)
            {
                for (int i = 0; i < CurrentMap.Objects[location.X, location.Y].Count; i++)
                {
                    MapObject ob = CurrentMap.Objects[location.X, location.Y][i];
                    if (!ob.Blocking || Race == ObjectType.Creature) continue;

                    return false;
                }
            }
           
            CurrentMap.Remove(CurrentLocation.X, CurrentLocation.Y,this);

            Direction = dir;
            RemoveObjects(dir, 1);
            CurrentLocation = location;
            CurrentMap.Add(this);
            AddObjects(dir, 1);

            if (Hidden)
            {
                Hidden = false;

                for (int i = 0; i < Buffs.Count; i++)
                {
                    if (Buffs[i].Type != BuffType.Hiding) continue;

                    Buffs[i].ExpireTime = 0;
                    break;
                }
            }


            CellTime = Envir.Time + 500;
            ActionTime = Envir.Time + 300;
            MoveTime = Envir.Time + MoveSpeed;
            if (MoveTime > AttackTime)
                AttackTime = MoveTime;

            InSafeZone = CurrentMap.GetSafeZone(CurrentLocation) != null;

            Broadcast(new S.ObjectWalk { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });


            //cell = CurrentMap.GetCell(CurrentLocation);

            for (int i = 0; i < CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y].Count; i++)
            {
                if (CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i].Race != ObjectType.Spell) continue;
                SpellObject ob = (SpellObject)CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i];

                ob.ProcessSpell(this);
                //break;
            }

            return true;
        }
        //改写下这里，支持多种攻击手段
        protected virtual void Attack()
        {
            if (BindingShotCenter) ReleaseBindingShot();

            ShockTime = 0;
            
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }


            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });


            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;

            int damage = GetAttackPower(MinDC, MaxDC);

            if (damage == 0) return;

            Target.Attacked(this, damage);
        }

        public void ReleaseBindingShot()
        {
            if (!BindingShotCenter) return;

            ShockTime = 0;
            Broadcast(GetInfo());//update clients in range (remove effect)
            BindingShotCenter = false;

            //the centertarget is escaped so make all shocked mobs awake (3x3 from center)
            Point place = CurrentLocation;
            for (int y = place.Y - 1; y <= place.Y + 1; y++)
            {
                if (y < 0) continue;
                if (y >= CurrentMap.Height) break;

                for (int x = place.X - 1; x <= place.X + 1; x++)
                {
                    if (x < 0) continue;
                    if (x >= CurrentMap.Width) break;

                    //Cell cell = CurrentMap.GetCell(x, y);
                    if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x,y] == null) continue;

                    for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                    {
                        MapObject targetob = CurrentMap.Objects[x, y][i];
                        if (targetob == null || targetob.Node == null || targetob.Race != ObjectType.Monster) continue;
                        if (((MonsterObject)targetob).ShockTime == 0) continue;

                        //each centerTarget has its own effect which needs to be cleared when no longer shocked
                        if (((MonsterObject)targetob).BindingShotCenter) ((MonsterObject)targetob).ReleaseBindingShot();
                        else ((MonsterObject)targetob).ShockTime = 0;

                        break;
                    }
                }
            }
        }

        public bool FindNearby(int distance)
        {
            for (int d = 0; d <= distance; d++)
            {
                for (int y = CurrentLocation.Y - d; y <= CurrentLocation.Y + d; y++)
                {
                    if (y < 0) continue;
                    if (y >= CurrentMap.Height) break;

                    for (int x = CurrentLocation.X - d; x <= CurrentLocation.X + d; x += Math.Abs(y - CurrentLocation.Y) == d ? 1 : d * 2)
                    {
                        if (x < 0) continue;
                        if (x >= CurrentMap.Width) break;
                        if (!CurrentMap.ValidPoint(x, y)) continue;
                       // Cell cell = CurrentMap.GetCell(x, y);
                        if (CurrentMap.Objects[x,y] == null) continue;

                        for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                        {
                            MapObject ob = CurrentMap.Objects[x, y][i];
                            switch (ob.Race)
                            {
                                case ObjectType.Monster:
                                case ObjectType.Player:
                                    if (!ob.IsAttackTarget(this)) continue;
                                    if (ob.Hidden && (!CoolEye || Level < ob.Level)) continue;
                                    if (ob.Race == ObjectType.Player)
                                    {
                                        PlayerObject player = ((PlayerObject)ob);
                                        if (player.GMGameMaster) continue;
                                    }
                                    return true;
                                default:
                                    continue;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public List<MapObject> FindFriendsNearby(int distance)
        {
            List<MapObject> Friends = new List<MapObject>();
            for (int d = 0; d <= distance; d++)
            {
                for (int y = CurrentLocation.Y - d; y <= CurrentLocation.Y + d; y++)
                {
                    if (y < 0) continue;
                    if (y >= CurrentMap.Height) break;

                    for (int x = CurrentLocation.X - d; x <= CurrentLocation.X + d; x += Math.Abs(y - CurrentLocation.Y) == d ? 1 : d * 2)
                    {
                        if (x < 0) continue;
                        if (x >= CurrentMap.Width) break;
                        if (!CurrentMap.ValidPoint(x, y)) continue;
                        //Cell cell = CurrentMap.GetCell(x, y);
                        if (CurrentMap.Objects[x,y] == null) continue;

                        for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                        {
                            MapObject ob = CurrentMap.Objects[x, y][i];
                            switch (ob.Race)
                            {
                                case ObjectType.Monster:
                                case ObjectType.Player:
                                    if (ob == this || ob.Dead) continue;
                                    if (ob.IsAttackTarget(this)) continue;
                                    if (ob.Race == ObjectType.Player)
                                    {
                                        PlayerObject player = ((PlayerObject)ob);
                                        if (player.GMGameMaster) continue;
                                    }
                                    Friends.Add(ob);
                                    break;
                                default:
                                    continue;
                            }
                        }
                    }
                }
            }

            return Friends;
        }

        public List<MapObject> FindAllNearby(int dist, Point location, bool needSight = true)
        {
            List<MapObject> targets = new List<MapObject>();
            for (int d = 0; d <= dist; d++)
            {
                for (int y = location.Y - d; y <= location.Y + d; y++)
                {
                    if (y < 0) continue;
                    if (y >= CurrentMap.Height) break;

                    for (int x = location.X - d; x <= location.X + d; x += Math.Abs(y - location.Y) == d ? 1 : d * 2)
                    {
                        if (x < 0) continue;
                        if (x >= CurrentMap.Width) break;

                        //Cell cell = CurrentMap.GetCell(x, y);
                        if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                        for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                        {
                            MapObject ob = CurrentMap.Objects[x, y][i];
                            switch (ob.Race)
                            {
                                case ObjectType.Monster:
                                case ObjectType.Player:
                                    targets.Add(ob);
                                    continue;
                                default:
                                    continue;
                            }
                        }
                    }
                }
            }
            return targets;
        }

        protected List<MapObject> FindAllTargets(int dist, Point location, bool needSight = true)
        {
            List<MapObject> targets = new List<MapObject>();
            for (int d = 0; d <= dist; d++)
            {
                for (int y = location.Y - d; y <= location.Y + d; y++)
                {
                    if (y < 0) continue;
                    if (y >= CurrentMap.Height) break;

                    for (int x = location.X - d; x <= location.X + d; x += Math.Abs(y - location.Y) == d ? 1 : d * 2)
                    {
                        if (x < 0) continue;
                        if (x >= CurrentMap.Width) break;                    

                        //Cell cell = CurrentMap.GetCell(x, y);
                        if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x, y] == null) continue;

                        for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                        {
                            MapObject ob = CurrentMap.Objects[x, y][i];
                            switch (ob.Race)
                            {
                                case ObjectType.Monster:
                                case ObjectType.Player:
                                    if (!ob.IsAttackTarget(this)) continue;
                                    if (ob.Hidden && (!CoolEye || Level < ob.Level) && needSight) continue;
                                    if (ob.Race == ObjectType.Player)
                                    {
                                        PlayerObject player = ((PlayerObject)ob);
                                        if (player.GMGameMaster) continue;
                                    }
                                    targets.Add(ob);
                                    continue;
                                default:
                                    continue;
                            }
                        }
                    }
                }
            }
            return targets;
        }
        //附近的玩家是否当前怪物的攻击目标
        public override bool IsAttackTarget(PlayerObject attacker)
        {
            if (attacker == null || attacker.Node == null) return false;
            if (Dead) return false;
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
            if (Master == null) return true;
            if (attacker.AMode == AttackMode.Peace) return false;
            if (Master == attacker) return attacker.AMode == AttackMode.All;
            if (Master.Race == ObjectType.Player && (attacker.InSafeZone || InSafeZone)) return false;

            switch (attacker.AMode)
            {
                case AttackMode.Group:
                    return Master.GroupMembers == null || !Master.GroupMembers.Contains(attacker);
                case AttackMode.Guild:
                    {
                        if (!(Master is PlayerObject)) return false;
                        PlayerObject master = (PlayerObject)Master;
                        return master.MyGuild == null || master.MyGuild != attacker.MyGuild;
                    }
                case AttackMode.EnemyGuild:
                    {
                        if (!(Master is PlayerObject)) return false;
                        PlayerObject master = (PlayerObject)Master;
                        return (master.MyGuild != null && attacker.MyGuild != null) && master.MyGuild.IsEnemy(attacker.MyGuild);
                    }
                case AttackMode.RedBrown:
                    return Master.PKPoints >= 200 || Envir.Time < Master.BrownTime;
                default:
                    return true;
            }
        }
        //附近的怪物是否当前怪物的攻击目标
        public override bool IsAttackTarget(MonsterObject attacker)
        {
            if (attacker == null || attacker.Node == null) return false;
            if (Dead || attacker == this) return false;
            if (attacker.Race == ObjectType.Creature) return false;
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
            //护卫
            if (attacker.Info.AI == 6) // Guard
            {
                if (Info.AI != 1 && Info.AI != 2 && Info.AI != 3 && (Master == null || Master.PKPoints >= 200)) //Not Dear/Hen/Tree/Pets or Red Master 
                    return true;
            }
            else if (attacker.Info.AI == 58) // Tao Guard - attacks Pets
            {
                if (Info.AI != 1 && Info.AI != 2 && Info.AI != 3) //Not Dear/Hen/Tree
                    return true;
            }
            else if (Master != null) //Pet Attacked
            {
                if (attacker.Master == null) //Wild Monster
                    return true;
                
                //Pet Vs Pet
                if (Master == attacker.Master)
                    return false;

                if (Envir.Time < ShockTime) //Shocked
                    return false;

                if (Master.Race == ObjectType.Player && attacker.Master.Race == ObjectType.Player && (Master.InSafeZone || attacker.Master.InSafeZone)) return false;

                switch (attacker.Master.AMode)
                {
                    case AttackMode.Group:
                        if (Master.GroupMembers != null && Master.GroupMembers.Contains((PlayerObject)attacker.Master)) return false;
                        break;
                    case AttackMode.Guild:
                        break;
                    case AttackMode.EnemyGuild:
                        break;
                    case AttackMode.RedBrown:
                        if (attacker.Master.PKPoints < 200 || Envir.Time > attacker.Master.BrownTime) return false;
                        break;
                    case AttackMode.Peace:
                        return false;
                }

                for (int i = 0; i < Master.Pets.Count; i++)
                    if (Master.Pets[i].EXPOwner == attacker.Master) return true;

                for (int i = 0; i < attacker.Master.Pets.Count; i++)
                {
                    MonsterObject ob = attacker.Master.Pets[i];
                    if (ob == Target || ob.Target == this) return true;
                }

                return Master.LastHitter == attacker.Master;
            }
            else if (attacker.Master != null) //Pet Attacking Wild Monster
            {
                if (Envir.Time < ShockTime) //Shocked
                    return false;

                for (int i = 0; i < attacker.Master.Pets.Count; i++)
                {
                    MonsterObject ob = attacker.Master.Pets[i];
                    if (ob == Target || ob.Target == this) return true;
                }

                if (Target == attacker.Master)
                    return true;
            }

            if (Envir.Time < attacker.HallucinationTime) return true;

            return Envir.Time < attacker.RageTime;
        }
        public override bool IsFriendlyTarget(PlayerObject ally)
        {
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
            if (Master == null) return false;
            if (Master == ally) return true;

            switch (ally.AMode)
            {
                case AttackMode.Group:
                    return Master.GroupMembers != null && Master.GroupMembers.Contains(ally);
                case AttackMode.Guild:
                    return false;
                case AttackMode.EnemyGuild:
                    return true;
                case AttackMode.RedBrown:
                    return Master.PKPoints < 200 & Envir.Time > Master.BrownTime;
            }
            return true;
        }

        public override bool IsFriendlyTarget(MonsterObject ally)
        {
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
            if (Master != null) return false;
            if (ally.Race != ObjectType.Monster) return false;
            if (ally.Master != null) return false;

            return true;
        }
        //被玩家攻击
        //damageWeapon：是否损坏持久
        public override int Attacked(PlayerObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            if (Target == null && attacker.IsAttackTarget(this))
            {
                Target = attacker;
            }

            int armour = 0;

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
                    if (RandomUtils.Next(Agility + 1) > attacker.Accuracy)
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        return 0;
                    }
                    armour = GetDefencePower(MinMAC, MaxMAC);
                    break;
                case DefenceType.MAC:
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
            {
                attacker.DamageWeapon();
            }
            damage += attacker.AttackBonus;
            //护甲高于伤害，则miss
            if (armour >= damage)
            {
                BroadcastDamageIndicator(DamageType.Miss);
                return 0;
            }
       
            //暴击
            if ((attacker.CriticalRate * Settings.CriticalRateWeight) > RandomUtils.Next(100))
            {
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.Critical });
                damage = Math.Min(int.MaxValue, damage + (int)Math.Floor(damage * (((double)attacker.CriticalDamage / (double)Settings.CriticalDamageWeight) * 10)));
                BroadcastDamageIndicator(DamageType.Critical);
            }
            //吸收伤害么？
            if (attacker.LifeOnHit > 0)
                attacker.ChangeHP(attacker.LifeOnHit);

            if (Target != this && attacker.IsAttackTarget(this))
            {
                if (attacker.Info.MentalState == 2)
                {
                    if (Functions.MaxDistance(CurrentLocation, attacker.CurrentLocation) < (8 - attacker.Info.MentalStateLvl))
                        Target = attacker;
                }
                else
                {
                    //这个仇恨系统看要不要改哟，2分之1的几率拉走仇恨吧
                    Target = attacker;
                }
            }

            if (BindingShotCenter) ReleaseBindingShot();
            ShockTime = 0;

            for (int i = PoisonList.Count - 1; i >= 0; i--)
            {
                if (PoisonList[i].PType != PoisonType.LRParalysis) continue;

                PoisonList.RemoveAt(i);
                OperateTime = 0;
            }

            if (Master != null && Master != attacker)
                if (Envir.Time > Master.BrownTime && Master.PKPoints < 200)
                    attacker.BrownTime = Envir.Time + Settings.Minute;

            if (EXPOwner == null || EXPOwner.Dead)
                EXPOwner = attacker;

            if (EXPOwner == attacker)
            {
                EXPOwnerTime = Envir.Time + EXPOwnerDelay;
            }
               

            ushort LevelOffset = (ushort)(Level > attacker.Level ? 0 : Math.Min(10, attacker.Level - Level));

            if (attacker.HasParalysisRing && type != DefenceType.MAC && type != DefenceType.MACAgility && 1 == RandomUtils.Next(1, 15))
            {
                ApplyPoison(new Poison { PType = PoisonType.Paralysis, Duration = 5, TickSpeed = 1000 }, attacker);
            }
            
            if (attacker.Freezing > 0 && type != DefenceType.MAC && type != DefenceType.MACAgility)
            {
                if ((RandomUtils.Next(Settings.FreezingAttackWeight) < attacker.Freezing) && (RandomUtils.Next(LevelOffset) == 0))
                    ApplyPoison(new Poison { PType = PoisonType.Slow, Duration = Math.Min(10, (3 + RandomUtils.Next(attacker.Freezing))), TickSpeed = 1000 }, attacker);
            }

            if (attacker.PoisonAttack > 0 && type != DefenceType.MAC && type != DefenceType.MACAgility)
            {
                if ((RandomUtils.Next(Settings.PoisonAttackWeight) < attacker.PoisonAttack) && (RandomUtils.Next(LevelOffset) == 0))
                    ApplyPoison(new Poison { PType = PoisonType.Green, Duration = 5, TickSpeed = 1000, Value = Math.Min(10, 3 + RandomUtils.Next(attacker.PoisonAttack)) }, attacker);
            }

            Broadcast(new S.ObjectStruck { ObjectID = ObjectID, AttackerID = attacker.ObjectID, Direction = Direction, Location = CurrentLocation });

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

            attacker.GatherElement();

            if (attacker.Info.Mentor != 0 && attacker.Info.isMentor)
            {
                Buff buff = attacker.Buffs.Where(e => e.Type == BuffType.Mentor).FirstOrDefault();
                if (buff != null)
                {
                    CharacterInfo Mentee = Envir.GetCharacterInfo(attacker.Info.Mentor);
                    PlayerObject player = Envir.GetPlayer(Mentee.Name);
                    if (player.CurrentMap == attacker.CurrentMap && Functions.InRange(player.CurrentLocation, attacker.CurrentLocation, Globals.DataRange) && !player.Dead)
                    {
                        damage += ((damage / 100) * Settings.MentorDamageBoost);
                    }
                }
            }

            if (Master != null && Master != attacker && Master.Race == ObjectType.Player && Envir.Time > Master.BrownTime && Master.PKPoints < 200 && !((PlayerObject)Master).AtWar(attacker))
            {
                attacker.BrownTime = Envir.Time + Settings.Minute;
            }

            for (int i = 0; i < attacker.Pets.Count; i++)
            {
                MonsterObject ob = attacker.Pets[i];

                if (IsAttackTarget(ob) && (ob.Target == null)) ob.Target = this;
            }

            BroadcastDamageIndicator(DamageType.Hit, armour - damage);

            ChangeHP(armour - damage);
            return damage - armour;
        }
        //怪物被攻击
        public override int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility)
        {
            if (Target == null && attacker.IsAttackTarget(this))
                Target = attacker;

            int armour = 0;

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
                    if (RandomUtils.Next(Agility + 1) > attacker.Accuracy)
                    {
                        BroadcastDamageIndicator(DamageType.Miss);
                        return 0;
                    }
                    armour = GetDefencePower(MinMAC, MaxMAC);
                    break;
                case DefenceType.MAC:
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

            if (armour >= damage)
            {
                BroadcastDamageIndicator(DamageType.Miss);
                return 0;
            }

            if (Target != this && attacker.IsAttackTarget(this))
                Target = attacker;

            if (BindingShotCenter) ReleaseBindingShot();
            ShockTime = 0;

            for (int i = PoisonList.Count - 1; i >= 0; i--)
            {
                if (PoisonList[i].PType != PoisonType.LRParalysis) continue;

                PoisonList.RemoveAt(i);
                OperateTime = 0;
            }

            if (attacker.Info.AI == 6 || attacker.Info.AI == 58)
                EXPOwner = null;

            else if (attacker.Master != null)
            {
                if (attacker.CurrentMap != attacker.Master.CurrentMap || !Functions.InRange(attacker.CurrentLocation, attacker.Master.CurrentLocation, Globals.DataRange))
                    EXPOwner = null;
                else
                {

                    if (EXPOwner == null || EXPOwner.Dead)
                        EXPOwner = attacker.Master;

                    if (EXPOwner == attacker.Master)
                    {
                        EXPOwnerTime = Envir.Time + EXPOwnerDelay;
                    }
                        
                }

            }

            Broadcast(new S.ObjectStruck { ObjectID = ObjectID, AttackerID = attacker.ObjectID, Direction = Direction, Location = CurrentLocation });

            BroadcastDamageIndicator(DamageType.Hit, armour - damage);

            ChangeHP(armour - damage);
            return damage - armour;
        }
        //被击中
        public override int Struck(int damage, DefenceType type = DefenceType.ACAgility)
        {
            int armour = 0;

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

            if (armour >= damage) return 0;
            Broadcast(new S.ObjectStruck { ObjectID = ObjectID, AttackerID = 0, Direction = Direction, Location = CurrentLocation });

            ChangeHP(armour - damage);
            return damage - armour;
        }

        public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true)
        {
            if (p.Owner != null && p.Owner.IsAttackTarget(this))
                Target = p.Owner;

            if (Master != null && p.Owner != null && p.Owner.Race == ObjectType.Player && p.Owner != Master)
            {
                if (Envir.Time > Master.BrownTime && Master.PKPoints < 200)
                    p.Owner.BrownTime = Envir.Time + Settings.Minute;
            }

            if (!ignoreDefence && (p.PType == PoisonType.Green))
            {
                int armour = GetDefencePower(MinMAC, MaxMAC);

                if (p.Value < armour)
                    p.PType = PoisonType.None;
                else
                    p.Value -= armour;
            }

            if (p.PType == PoisonType.None) return;
            //毒免疫
            if(Info.PoisonImmune>0)
            {
                if ((Info.PoisonImmune  & (int)p.PType) == (int)p.PType)
                {
                    return;
                }
            }

            for (int i = 0; i < PoisonList.Count; i++)
            {
                if (PoisonList[i].PType != p.PType) continue;
                if ((PoisonList[i].PType == PoisonType.Green) && (PoisonList[i].Value > p.Value)) return;//cant cast weak poison to cancel out strong poison
                if ((PoisonList[i].PType != PoisonType.Green) && ((PoisonList[i].Duration - PoisonList[i].Time) > p.Duration)) return;//cant cast 1 second poison to make a 1minute poison go away!
                if (p.PType == PoisonType.DelayedExplosion) return;
                if ((PoisonList[i].PType == PoisonType.Frozen) || (PoisonList[i].PType == PoisonType.Slow) || (PoisonList[i].PType == PoisonType.Paralysis) || (PoisonList[i].PType == PoisonType.LRParalysis)) return;//prevents mobs from being perma frozen/slowed
                //毒覆盖，不叠加
                PoisonList[i] = p;
                return;
            }

            if (p.PType == PoisonType.DelayedExplosion)
            {
                ExplosionInflictedTime = Envir.Time + 4000;
                Broadcast(new S.ObjectEffect { ObjectID = ObjectID, Effect = SpellEffect.DelayedExplosion });
            }

            PoisonList.Add(p);
        }
        public override void AddBuff(Buff b)
        {
            if (Buffs.Any(d => d.Infinite && d.Type == b.Type)) return; //cant overwrite infinite buff with regular buff

            string caster = b.Caster != null ? b.Caster.Name : string.Empty;

            if (b.Values == null) b.Values = new int[1];

            S.AddBuff addBuff = new S.AddBuff { Type = b.Type, Caster = caster, Expire = b.ExpireTime - Envir.Time, Values = b.Values, Infinite = b.Infinite, ObjectID = ObjectID, Visible = b.Visible };

            if (b.Visible) Broadcast(addBuff);

            base.AddBuff(b);
            RefreshAll();
        }
        
        public override Packet GetInfo()
        {
            return new S.ObjectMonster
                {
                    ObjectID = ObjectID,
                    Name = Name,
                    NameColour = NameColour,
                    Location = CurrentLocation,
                    Image = Info.Image,
                    Direction = Direction,
                    Effect = Info.Effect,
                    AI = Info.AI,
                    Light = Info.Light,
                    Dead = Dead,
                    Skeleton = Harvested,
                    Poison = CurrentPoison,
                    Hidden = Hidden,
                    ShockTime = (ShockTime > 0 ? ShockTime - Envir.Time : 0),
                    BindingShotCenter = BindingShotCenter
                };
        }

        public override void ReceiveChat(string text, ChatType type)
        {
            throw new NotSupportedException();
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

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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
                            if (x < 0 || x >= CurrentMap.Width) continue;

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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

                            if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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
                                if (ob.Race != ObjectType.Player) continue;
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
                                if (ob.Race != ObjectType.Player) continue;
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
                                if (ob.Race != ObjectType.Player) continue;
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
                                if (ob.Race != ObjectType.Player) continue;
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

                           // Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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
                                if (ob.Race != ObjectType.Player) continue;
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

                            //Cell cell = CurrentMap.GetCell(x, y);

                            if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;

                            for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                            {
                                MapObject ob = CurrentMap.Objects[x, y][i];
                                if (ob.Race != ObjectType.Player) continue;
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
                                if (ob.Race != ObjectType.Player) continue;
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
                                if (ob.Race != ObjectType.Player) continue;
                                ob.Add(this);
                            }
                        }
                    }
                    break;
            }
        }

        public override void Add(PlayerObject player)
        {
            player.Enqueue(GetInfo());
            SendHealth(player);
        }

        //经验拥有者才可以看到血
        //这里改下，大家都可以看到怪物的血
        public override void SendHealth(PlayerObject player)
        {
            //不显示血
            if (!player.IsMember(Master) && !(player.IsMember(EXPOwner) && AutoRev) && Envir.Time > RevTime) return;

            if (!AutoRev && Envir.Time > RevTime)
            {
                return;
            }
            byte time = Math.Min(byte.MaxValue, (byte) Math.Max(5, (RevTime - Envir.Time)/1000));
            
            player.Enqueue(new S.ObjectHealth { ObjectID = ObjectID, HP = this.HP,MaxHP=this.MaxHP, Expire = time });
        }

        //宠物获得经验，并升级
        //宠物升级经验要求，基数，每级要求杀10个怪
        //逐级加5，就是7级就是35+10，就是要杀45个怪
        public void PetExp(uint amount)
        {
            if (PetLevel >= MaxPetLevel) return;
            //
            //if (Info.Name == Settings.SkeletonName || Info.Name == Settings.ShinsuName || Info.Name == Settings.AngelName)
            //    amount *= 3;
            //PetExperience += amount;
            //每个怪都增加1点经验
            PetExperience++;
            int needExperience = PetLevel * 5 + 10;
            if(PetExperience< needExperience)
            {
                return;
            }
            //if (PetExperience < (PetLevel + 1)*20000) return;
            //PetExperience = (uint) (PetExperience - ((PetLevel + 1)*20000));
            PetExperience = 0;
            PetLevel++;
            RefreshAll();
            OperateTime = 0;
            BroadcastHealthChange();
        }
        public override void Despawn()
        {
            SlaveList.Clear();
            base.Despawn();
        }

    }

    /// <summary>
    /// 采用怪物对象包裹器，包裹怪物
    /// 这里只记录简单的怪物的位置，血量，仇恨等个性东西，其他的都用具体的怪物属性
    /// 采用怪物属性引用的方式，避免大量刷怪占用的内存属性
    /// </summary>
    public class MonsterObjectInstance
    {

    }
}
