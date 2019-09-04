using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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

//这个应该是NPC面板类型了，客户端的NPC交易面板的类型弹出面板的类型
public enum PanelType : byte
{
    Buy = 0,//买
    Sell,//卖
    Repair,//修理
    SpecialRepair,//特殊修理
    Consign,//委托，寄售
    Craft,//制作
    Refine,//提炼
    CheckRefine,//检查精炼
    Disassemble,//拆卸
    Downgrade,//降级
    Reset,//重置
    CollectRefine,//取回武器
    ReplaceWedRing,//打造结婚戒指
    ConsignCredit,//委托，寄售元宝
    ConsignDoulbe,//寄售，委托，支持同时输入金币和元宝
}

//混合模式,d3d中的混合模式
public enum BlendMode : sbyte
{
    NONE = -1,
    NORMAL = 0,
    LIGHT = 1,
    LIGHTINV = 2,
    INVNORMAL = 3,
    INVLIGHT = 4,
    INVLIGHTINV = 5,//闯入
    INVCOLOR = 6,//使颜色变深
    INVBACKGROUND = 7//INVBACKGROUND
}

public enum DamageType : byte
{
    Hit = 0,//击中
    Miss = 1,//miss
    Critical = 2//暴击？
}

[Flags]
public enum GMOptions : byte
{
    None = 0,
    GameMaster = 0x0001,
    Observer = 0x0002,
    Superman = 0x0004
}
//觉醒物品的类型？就是觉醒什么属性的意思
public enum AwakeType : byte
{
    None = 0,
    DC,
    MC,
    SC,
    AC,
    MAC,
    HPMP,
}
//这个应该是3中不同的效果？不知道什么效果，在客户端只会持续5秒的效果
[Flags]
public enum LevelEffects : byte
{
    None = 0,
    Mist = 0x0001,//半透明，隐身效果
    RedDragon = 0x0002,//红毒效果？
    BlueDragon = 0x0004//蓝色？
}

public enum OutputMessageType : byte
{
    Normal, 
    Quest,
    Guild
}

//物品等级，这个是什么鸟等级，这个主要是影响颜色的
//白-浅蓝LightSkyBlue 绿limegreen--紫mediumorchid-金gold-粉Violet
//1普通（白色）2高级（蓝DeepSkyBlue/LightSkyBlue）3稀有（绿limegreen/DarkOrange）4传说（紫mediumorchid） 5神器（金gold） 6史诗（粉Violet/DarkViolet）
public enum ItemGrade : byte
{
    None = 0,//白色0+
    Common = 1,//普通 白色10+
    HighLevel=2,//高级20+
    Rare = 3,//稀有30+
    Legendary = 4,//传说40+
    Mythical = 5,//神话50+
    Ancient = 6,//特殊，可变形的装备,远古
    Epic = 7,//最后的2件装备
}

//这个是装备的属性类型
public enum StatType : byte
{
    AC = 0,
    MAC = 1,
    DC = 2,
    MC = 3,
    SC = 4,
    HP = 5,
    MP = 6,
    HP_Percent = 7,
    MP_Percent = 8,
    HP_Regen = 9,
    MP_Regen = 10,
    ASpeed = 11,
    Luck = 12,
    Strong = 13,
    Accuracy = 14,
    Agility = 15,
    MagicResist = 16,
    PoisonResist = 17,
    PoisonAttack = 18,
    PoisonRegen = 19,
    Freezing = 20,
    Holy = 21,
    Durability = 22,
    Unknown = 23
}
public enum RefinedValue : byte
{
    None = 0,
    DC = 1,
    MC = 2,
    SC = 3,
}
//任务类型
public enum QuestType : byte
{
    General = 0,//常规任务
    Daily = 1,//每日任务
    Repeatable = 2,//可重复做的任务，无限重复做的任务
    Story = 3//故事
}

public enum QuestIcon : byte
{
    None = 0,
    QuestionWhite = 1,
    ExclamationYellow = 2,
    QuestionYellow = 3,
    ExclamationBlue = 5,
    QuestionBlue = 6,
    ExclamationGreen = 52,
    QuestionGreen = 53
}

public enum QuestState : byte
{
    Add,
    Update,
    Remove
}

public enum DefaultNPCType : byte
{
    Login,
    LevelUp,
    UseItem,
    MapCoord,
    MapEnter,
    Die,
    Trigger,
    CustomCommand,
    OnAcceptQuest,
    OnFinishQuest,
    Daily,
    TalkMonster
}
//智能生物类型？不知道干嘛的
public enum IntelligentCreatureType : byte
{
    None = 99,
    BabyPig = 0,
    Chick = 1,
    Kitten = 2,
    BabySkeleton = 3,
    Baekdon = 4,
    Wimaen = 5,
    BlackKitten = 6,
    BabyDragon = 7,
    OlympicFlame = 8,
    BabySnowMan = 9,
    Frog = 10,
    BabyMonkey = 11,
    AngryBird = 12,
    Foxey = 13,
}

//1 blank mob files
//7 mob frames not added
//2 blank frame sets (92, 173)
//4 mob frames duplicate of other frame sets

//TODO: add 2 missing frames in to blank frames, remove 2 duplicate frames (leaving no blanks and 2 duplicates)
//各种怪物的定义？感觉怪物不用定义，直接数据库配置比较好，全部采用服务器的比较好
public enum Monster : ushort
{
    Guard = 0,//警卫
    TaoistGuard = 1,
    Guard2 = 2,
    Hen = 3,
    Deer = 4,
    Scarecrow = 5,
    HookingCat = 6,
    RakingCat = 7,
    Yob = 8,
    Oma = 9,
    CannibalPlant = 10,
    ForestYeti = 11,
    SpittingSpider = 12,
    ChestnutTree = 13,
    EbonyTree = 14,
    LargeMushroom = 15,
    CherryTree = 16,
    OmaFighter = 17,
    OmaWarrior = 18,
    CaveBat = 19,
    CaveMaggot = 20,
    Scorpion = 21,
    Skeleton = 22,
    BoneFighter = 23,
    AxeSkeleton = 24,
    BoneWarrior = 25,
    BoneElite = 26,
    Dung = 27,
    Dark = 28,
    WoomaSoldier = 29,
    WoomaFighter = 30,
    WoomaWarrior = 31,
    FlamingWooma = 32,
    WoomaGuardian = 33,
    WoomaTaurus = 34,
    WhimperingBee = 35,
    GiantWorm = 36,
    Centipede = 37,
    BlackMaggot = 38,
    Tongs = 39,
    EvilTongs = 40,
    EvilCentipede = 41,
    BugBat = 42,
    BugBatMaggot = 43,
    WedgeMoth = 44,
    RedBoar = 45,
    BlackBoar = 46,
    SnakeScorpion = 47,
    WhiteBoar = 48,
    EvilSnake = 49,
    BombSpider = 50,
    RootSpider = 51,
    SpiderBat = 52,
    VenomSpider = 53,
    GangSpider = 54,
    GreatSpider = 55,
    LureSpider = 56,
    BigApe = 57,
    EvilApe = 58,
    GrayEvilApe = 59,
    RedEvilApe = 60,
    CrystalSpider = 61,
    RedMoonEvil = 62,
    BigRat = 63,
    ZumaArcher = 64,
    ZumaStatue = 65,
    ZumaGuardian = 66,
    RedThunderZuma = 67,
    ZumaTaurus = 68,
    DigOutZombie = 69,
    ClZombie = 70,
    NdZombie = 71,
    CrawlerZombie = 72,
    ShamanZombie = 73,
    Ghoul = 74,
    KingScorpion = 75,
    KingHog = 76,
    DarkDevil = 77,
    BoneFamiliar = 78,
    Shinsu = 79,
    Shinsu1 = 80,
    SpiderFrog = 81,
    HoroBlaster = 82,
    BlueHoroBlaster = 83,
    KekTal = 84,
    VioletKekTal = 85,
    Khazard = 86,
    RoninGhoul = 87,
    ToxicGhoul = 88,
    BoneCaptain = 89,
    BoneSpearman = 90,
    BoneBlademan = 91,
    BoneArcher = 92,
    BoneLord = 93,
    Minotaur = 94,
    IceMinotaur = 95,
    ElectricMinotaur = 96,
    WindMinotaur = 97,
    FireMinotaur = 98,
    RightGuard = 99,
    LeftGuard = 100,
    MinotaurKing = 101,
    FrostTiger = 102,
    Sheep = 103,
    Wolf = 104,
    ShellNipper = 105,
    Keratoid = 106,
    GiantKeratoid = 107,
    SkyStinger = 108,
    SandWorm = 109,
    VisceralWorm = 110,
    RedSnake = 111,
    TigerSnake = 112,
    Yimoogi = 113,
    GiantWhiteSnake = 114,
    BlueSnake = 115,
    YellowSnake = 116,
    HolyDeva = 117,
    AxeOma = 118,
    SwordOma = 119,
    CrossbowOma = 120,
    WingedOma = 121,
    FlailOma = 122,
    OmaGuard = 123,
    YinDevilNode = 124,
    YangDevilNode = 125,
    OmaKing = 126,
    BlackFoxman = 127,
    RedFoxman = 128,
    WhiteFoxman = 129,
    TrapRock = 130,
    GuardianRock = 131,
    ThunderElement = 132,
    CloudElement = 133,
    GreatFoxSpirit = 134,
    HedgeKekTal = 135,
    BigHedgeKekTal = 136,
    RedFrogSpider = 137,
    BrownFrogSpider = 138,
    ArcherGuard = 139,
    KatanaGuard = 140,
    ArcherGuard2 = 141,
    Pig = 142,
    Bull = 143,
    Bush = 144,
    ChristmasTree = 145,
    HighAssassin = 146,
    DarkDustPile = 147,
    DarkBrownWolf = 148,
    Football = 149, 
    GingerBreadman = 150,
    HalloweenScythe = 151,
    GhastlyLeecher = 152,
    CyanoGhast = 153,//这个怪物的AI有问题啊
    MutatedManworm = 154,
    CrazyManworm = 155,
    MudPile = 156,
    TailedLion = 157,
    Behemoth = 158,//done BOSS
    DarkDevourer = 159,//done
    PoisonHugger = 160,//done
    Hugger = 161,//done
    MutatedHugger = 162,//done
    DreamDevourer = 163,//done
    Treasurebox = 164,//done
    SnowPile = 165,//done
    Snowman = 166,//done
    SnowTree = 167,//done
    GiantEgg = 168,//done
    RedTurtle = 169,//done
    GreenTurtle = 170,//done
    BlueTurtle = 171,//done
        Catapult = 172, //not added frames //special 3 states in 1 
        SabukWallSection = 173, //not added frames
        NammandWallSection = 174, //not added frames
        SiegeRepairman = 175, //not added frames
    BlueSanta = 176,//done
    BattleStandard = 177,//done
    //ArcherGuard2 = 178,//done
    RedYimoogi = 179,//done
    LionRiderMale = 180, //frames not added
    LionRiderFemale = 181, //frames not added
    Tornado = 182,//done
    FlameTiger = 183,//done
    WingedTigerLord = 184,//done BOSS
    TowerTurtle = 185,//done
    FinialTurtle = 186,//done
    TurtleKing = 187,//done BOSS
    DarkTurtle = 188,//done
    LightTurtle = 189,//done  
    DarkSwordOma = 190,//done
    DarkAxeOma = 191,//done
    DarkCrossbowOma = 192,//done
    DarkWingedOma = 193,//done
    BoneWhoo = 194,//done
    DarkSpider = 195,//done
    ViscusWorm = 196,//done
    ViscusCrawler = 197,//done
    CrawlerLave = 198,//done
    DarkYob = 199,//done

    FlamingMutant = 200,//FINISH
    StoningStatue = 201,//FINISH BOSS
    FlyingStatue = 202,//FINISH
    ValeBat = 203,//done
    Weaver = 204,//done
    VenomWeaver = 205,//done
    CrackingWeaver = 206,//done
    ArmingWeaver = 207,//done
    CrystalWeaver = 208,//done
    FrozenZumaStatue = 209,//done
    FrozenZumaGuardian = 210,//done
    FrozenRedZuma = 211,//done
    GreaterWeaver = 212,//done
    SpiderWarrior = 213,//done
    SpiderBarbarian = 214,//done
    HellSlasher = 215,//done
    HellPirate = 216,//done
    HellCannibal = 217,//done
    HellKeeper = 218, //done BOSS
    HellBolt = 219, //done
    WitchDoctor = 220,//done
    ManectricHammer = 221,//done
    ManectricClub = 222,//done
    ManectricClaw = 223,//done
    ManectricStaff = 224,//done
    NamelessGhost = 225,//done
    DarkGhost = 226,//done
    ChaosGhost = 227,//done
    ManectricBlest = 228,//done
    ManectricKing = 229,//done
    FrozenDoor = 230,//done
    IcePillar = 231,//done
    FrostYeti = 232,//done
    ManectricSlave = 233,//done
    TrollHammer = 234,//done
    TrollBomber = 235,//done
    TrollStoner = 236,//done
    TrollKing = 237,//done BOSS
    FlameSpear = 238,//done
    FlameMage = 239,//done
    FlameScythe = 240,//done
    FlameAssassin = 241,//done
    FlameQueen = 242, //finish BOSS
    HellKnight1 = 243,//done
    HellKnight2 = 244,//done
    HellKnight3 = 245,//done
    HellKnight4 = 246,//done
    HellLord = 247,//done BOSS
    WaterGuard = 248,//done
    IceGuard = 249,
    ElementGuard = 250,
    DemonGuard = 251,
    KingGuard = 252,
    Snake10 = 253,//done
    Snake11 = 254,//done
    Snake12 = 255,//done
    Snake13 = 256,//done
    Snake14 = 257,//done
    Snake15 = 258,//done
    Snake16 = 259,//done
    Snake17 = 260,//done

    DeathCrawler = 261,
    BurningZombie = 262,
    MudZombie = 263,
    FrozenZombie = 264,
    UndeadWolf = 265,
    Demonwolf = 266,
    WhiteMammoth = 267,
    DarkBeast = 268,
    LightBeast = 269,
    BloodBaboon = 270,
    HardenRhino = 271,
    AncientBringer = 272,
    FightingCat = 273,
    FireCat = 274,
    CatWidow = 275,
    StainHammerCat = 276,
    BlackHammerCat = 277,
    StrayCat = 278,
    CatShaman = 279,
    Jar1 = 280,
    Jar2 = 281,
    SeedingsGeneral = 282,
    RestlessJar = 283,
    GeneralJinmYo = 284,
    Bunny = 285,
    Tucson = 286,
    TucsonFighter = 287,
    TucsonMage = 288,
    TucsonWarrior = 289,
    Armadillo = 290,
    ArmadilloElder = 291,
    TucsonEgg = 292,
    PlaguedTucson = 293,
    SandSnail = 294,
    CannibalTentacles = 295,
    TucsonGeneral = 296,
    GasToad = 297,
    Mantis = 298,
    SwampWarrior = 299,

    AssassinBird = 300,
    RhinoWarrior = 301,
    RhinoPriest = 302,
    SwampSlime = 303,
    RockGuard = 304,
    MudWarrior = 305,
    SmallPot = 306,
    TreeQueen = 307,
    ShellFighter = 308,
    DarkBaboon = 309,
    TwinHeadBeast = 310,
    OmaCannibal = 311,
    OmaBlest = 312,
    OmaSlasher = 313,
    OmaAssassin = 314,
    OmaMage = 315,
    OmaWitchDoctor = 316,
    LightningBead = 317,
    HealingBead = 318,
    PowerUpBead = 319,
    DarkOmaKing = 320,
    CaveMage = 321,
    Mandrill = 322,
    PlagueCrab = 323,
    CreeperPlant = 324,
    FloatingWraith = 325,
    ArmedPlant = 326,
    AvengerPlant = 327,
    Nadz = 328,
    AvengingSpirit = 329,
    AvengingWarrior = 330,
    AxePlant = 331,
    WoodBox = 332,
    ClawBeast = 333,
    KillerPlant = 334,
    SackWarrior = 335,
    WereTiger = 336,
    KingHydrax = 337,
    Hydrax = 338,
    HornedMage = 339,
    Basiloid = 340,
    HornedArcher = 341,
    ColdArcher = 342,
    HornedWarrior = 343,
    FloatingRock = 344,
    ScalyBeast = 345,
    HornedSorceror = 346,
    BoulderSpirit = 347,
    HornedCommander = 348,
    MoonStone = 349,

    SunStone = 350,
    LightningStone = 351,
    Turtlegrass = 352,
    Mantree = 353,
    Bear = 354,
    Leopard = 355,
    ChieftainArcher = 356,
    ChieftainSword = 357,
    StoningSpider = 358, //Archer Spell mob (not yet coded)
    VampireSpider = 359, //Archer Spell mob
    SpittingToad = 360, //Archer Spell mob
    SnakeTotem = 361, //Archer Spell mob
    CharmedSnake = 362, //Archer Spell mob
    FrozenSoldier = 363,
    FrozenFighter = 364,
    FrozenArcher = 365,
    FrozenKnight = 366,
    FrozenGolem = 367,
    IcePhantom = 368,
    SnowWolf = 369,
    SnowWolfKing = 370,
    WaterDragon = 371,
    BlackTortoise = 372,
    Manticore = 373,
    DragonWarrior = 374,
    DragonArcher = 375,
    Kirin = 376,
    Guard3 = 377,
    ArcherGuard3 = 378,
    Bunny2 = 379,
    FrozenMiner = 380,
    FrozenAxeman = 381,
    FrozenMagician = 382,
    SnowYeti = 383,
    IceCrystalSoldier = 384,
    DarkWraith = 385,
    DarkSpirit = 386,
    CrystalBeast = 387,
    RedOrb = 388,
    BlueOrb = 389,
    YellowOrb = 390,
    GreenOrb = 391,
    WhiteOrb = 392,
    FatalLotus = 393,
    AntCommander = 394,
    CargoBoxwithlogo = 395,
    Doe = 396,
    Reindeer = 397, //frames not added
    AngryReindeer = 398,
    CargoBox = 399,
    
    Ram1 = 400,
    Ram2 = 401,
    Kite = 402,

    //这里增加雪域怪物
    Monster403 = 403,
    Monster404 = 404,
    Monster405 = 405,
    Monster406 = 406,
    Monster407 = 407,
    Monster408 = 408,
    Monster409 = 409,
    Monster410 = 410,
    Monster411 = 411,
    Monster412 = 412,
    Monster413 = 413,
    Monster414 = 414,
    Monster415 = 415,
    Monster416 = 416,
    Monster417 = 417,
    Monster418 = 418,
    Monster419 = 419,
    Monster420 = 420,
    Monster421 = 421,
    Monster422 = 422,
    Monster423 = 423,
    Monster424 = 424,
    Monster425 = 425,
    Monster426 = 426,
    Monster427 = 427,
    Monster428 = 428,
    Monster429 = 429,
    Monster430 = 430,
    Monster431 = 431,
    Monster432 = 432,
    Monster433 = 433,
    Monster434 = 434,
    Monster435 = 435,
    Monster436 = 436,
    Monster437 = 437,
    Monster438 = 438,
    Monster439 = 439,
    Monster440 = 440,
    Monster441 = 441,
    Monster442 = 442,
    Monster443 = 443,
    Monster444 = 444,
    Monster445 = 445,
    Monster446 = 446,
    Monster447 = 447,
    Monster448 = 448,
    Monster449 = 449,
    Monster450 = 450,
    Monster451 = 451,
    Monster452 = 452,
    Monster453 = 453,
    Monster454 = 454,
    Monster455 = 455,
    Monster456 = 456,
    Monster457 = 457,
    Monster458 = 458,


    EvilMir = 900,
    EvilMirBody = 901,
    DragonStatue = 902,
    HellBomb1 = 903,
    HellBomb2 = 904,
    HellBomb3 = 905,

    SabukGate = 950,
    PalaceWallLeft = 951,
    PalaceWall1 = 952,
    PalaceWall2 = 953,
    GiGateSouth = 954,
    GiGateEast = 955,
    GiGateWest = 956,
    SSabukWall1 = 957,
    SSabukWall2 = 958,
    SSabukWall3 = 959,

    BabyPig = 10000,//Permanent
    Chick = 10001,//Special
    Kitten = 10002,//Permanent
    BabySkeleton = 10003,//Special
    Baekdon = 10004,//Special
    Wimaen = 10005,//Event
    BlackKitten = 10006,//unknown
    BabyDragon = 10007,//unknown
    OlympicFlame = 10008,//unknown
    BabySnowMan = 10009,//unknown
    Frog = 10010,//unknown
    BabyMonkey = 10011,//unknown
    AngryBird = 10012,
    Foxey = 10013,

    //怪物466的另外一个副本
    Monster20446 = 20446,
}

//所有的动作定义
public enum MirAction : byte
{
    Standing,//站立
    Walking,//行走
    Running,//跑步，玩家才有
    Pushed,//被推动,针对人物的后退动作，怪物的话，没有，怪物都是直接移动到那边去的
    DashL,//短跑
    DashR,
    DashFail,
    Stance,//站立
    Stance2,//站立2
    Attack1,//攻击
    Attack2,
    Attack3,
    Attack4,
    Attack5,
    AttackRange1,//范围攻击
    AttackRange2,
    AttackRange3,
    Special,
    Struck,//被攻击
    Harvest,//收获，这个是针对玩家的
    Spell,//施法
    Die,//正死亡
    Dead,//已死亡
    Skeleton,//骨架
    Show,//显示,如石化的祖玛怪物的激活，如神兽的激活（变大）等
    Hide,//隐身
    Stoned,//石化,比如祖玛卫士等石化效果
    Appear,//显现，出现，主要针对召唤骷髅，召唤神兽，需要显示出来的.
    Revive,//复活，这个大部分的都是和Die（正死亡）的动作是一样的
    SitDown,//坐下
    Mine,//挖矿
    Sneek,//蛇形走位，针对刺客
    DashAttack,//跑动攻击，针对刺客
    Lunge,//弓步

    WalkingBow,
    RunningBow,
    Jump,

    MountStanding,
    MountWalking,
    MountRunning,
    MountStruck,
    MountAttack,

    FishingCast,
    FishingWait,
    FishingReel,

    Standing2,//另外一种形态下的站立
    Walking2,//另外一种形态下的站立
    Struck2,//另外一种形态被攻击
}

public enum CellAttribute : byte
{
    Walk = 0,
    HighWall = 1,
    LowWall = 2,
}
//灯光
public enum LightSetting : byte
{
    Normal = 0,//正常
    Dawn = 1,//黎明
    Day = 2,//白天
    Evening = 3,//傍晚
    Night = 4//夜间
}

//灯光类型
public enum LightType : byte
{
    Normal = 0,//正常的灯光
    indoor = 1,//室内灯光(洞内才区分灯光)
    none = 2,//没有灯光
}

//跑步类型
public enum RunType : byte
{
    Normal = 0,//正常跑步，需要起步
    FastRun = 1,//不需要起步，直接跑步
}

//退出游戏类型，是否需要等待脱战才可以退出,这个主要逻辑是服务器端的逻辑
public enum ExitGameType : byte
{
    Normal = 0,//需要脱战才可以退出
    noWait = 1,//不需要起步，直接跑步
}


[Obfuscation(Feature = "renaming", Exclude = true)]
public enum MirGender : byte
{
    Male = 0,
    Female = 1
}
//职业，这个后面要加职业,加武僧
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum MirClass : byte
{
    Warrior = 0,//战士
    Wizard = 1,//法师
    Taoist = 2,//道士
    Assassin = 3,//刺客
    Archer = 4,//弓箭手
    Monk = 5//武僧，
}
//方向
public enum MirDirection : byte
{
    Up = 0,//上
    UpRight = 1,//右上
    Right = 2,//右
    DownRight = 3,//右下
    Down = 4,//下
    DownLeft = 5,//左下
    Left = 6,//左
    UpLeft = 7//左上
}
//对象类型?这个不太对哦，不知道是什么
public enum ObjectType : byte
{
    None= 0,
    Player = 1,//玩家
    Item = 2,//物品
    Merchant = 3,//商人
    Spell = 4,//咒语
    Monster = 5,//怪物
    Deco = 6,//这个是NCP相关的？
    Creature = 7//生物,这个应该是非战斗宠物
}
//聊天的类型，字体类型
public enum ChatType : byte
{
    Normal = 0,//正常
    Shout = 1,//叫喊
    System = 2,//系统
    Hint = 3,//这个是操作的提醒，比如允许天人合一，允许组队等
    Announcement = 4,//公告
    Group = 5,//组内聊天
    WhisperIn = 6,//耳语
    WhisperOut = 7,//耳语
    Guild = 8,//行会
    Trainer = 9,
    LevelUp = 10,
    System2 = 11,//红色底纹
    Relationship = 12,
    Mentor = 13,
    Shout2 = 14,
    Shout3 = 15
}
//物品分类？
//这里的分类过于复杂了，应该只分以下几类即可？
//1.装备(装备是要细分不同的装备的)
//2.药水(可以直接使用的,包括随机，回城等，这种用脚本处理)
//3.材料(各种任务材料，用于任务的材料,不能直接使用，提供给NPC脚本使用)
public enum ItemType : byte
{
    Nothing = 0,
    Weapon = 1,//武器
    Armour = 2,//盔甲
    Helmet = 4, //头盔
    Necklace = 5,//项链
    Bracelet = 6,//手链
    Ring = 7,//戒指
    Amulet = 8,//护身符
    Belt = 9,//腰带
    Boots = 10,//靴子
    Stone = 11,//石头
    Torch = 12,//火把
    Potion = 13,//药剂(药水)
    Ore = 14,//矿石
    Meat = 15,//肉
    CraftingMaterial = 16,//工艺材料
    Scroll = 17,//纸张(卷轴，随机，回城等)
    Gem = 18,//软玉，石头材料？
    Mount = 19,//坐骑
    Book = 20,//书
    Script = 21,//手稿，脚本，特殊卷轴等
    Reins = 22,//缰绳
    Bells = 23,//数据库中对应4个铃铛，这个是给宠物使用的吧
    Saddle = 24,//鞍座
    Ribbon = 25,//蝴蝶结的饰品
    Mask = 26,//面具（数据库中对应的面具应该是给宠物使用的面具吧）,目前这个面积好像是没用的
    Food = 27,//食物(生肉，精肉)，给坐骑吃
    Hook = 28,//钩子，钓鱼用的钩子
    Float = 29,//浮标
    Bait = 30,//诱饵
    Finder = 31,//鱼群探测器
    Reel = 32,//鱼竿上的转盘，收鱼线用的
    Fish = 33,//鱼
    Quest = 34,//任务
	Awakening = 35,//觉醒
    Pets = 36,//宠物
    Transform = 37,//外观（时装吧）
    SkinWeapon = 38,//武器变幻
    SkinArmour = 39,//衣服变幻
    MonsterDan = 40,//兽魂丹

}
//格子类型，就是各种交易框的类型咯？
public enum MirGridType : byte
{
    None = 0,
    Inventory = 1,//库存(F9角色的背包)
    Equipment = 2,//设备(这个是角色的装备栏)
    Trade = 3,//贸易(角色的交易栏)
    Storage = 4,//保管部(这个是账号的仓库，多角色共享的)
    BuyBack = 5,//回购
    DropPanel = 6,//跌落面板？
    Inspect = 7,//检查
    TrustMerchant = 8,//信任商人
    GuildStorage = 9,//行会仓库
    GuestTrade = 10,//客人交易？
    Mount = 11,//安装
    Fishing = 12,//垂钓
    QuestInventory = 13,//查询库存
    AwakenItem = 14,//唤醒物品
    Mail = 15,//邮件
    Refine = 16,//精炼
    Renting = 17,//租赁
    GuestRenting = 18,
    Craft = 19,//工艺，制作
    ItemCollect = 20//收集物品
}
//装备物品
public enum EquipmentSlot : byte
{
    Weapon = 0,//武器
    Armour = 1,//衣服
    Helmet = 2,//头盔
    Torch = 3,//火把
    Necklace = 4,//项链
    BraceletL = 5,//手镯
    BraceletR = 6,
    RingL = 7,//戒指
    RingR = 8,
    Amulet = 9,//毒符
    Belt = 10,//腰带
    Boots = 11,//鞋子
    Stone = 12,//宝石
    Mount = 13//坐骑
}
//安装，组装物品
public enum MountSlot : byte
{
    Reins = 0,//绳子，捆绑？
    Bells = 1,
    Saddle = 2,//马鞍 
    Ribbon = 3,//色带
    Mask = 4//面具
}
//钓具
public enum FishingSlot : byte
{
    Hook = 0,//钩子
    Float = 1,//浮标
    Bait = 2,//诱饵
    Finder = 3,//取景器，鱼竿？
    Reel = 4//绕在卷轴上的线,鱼线？
}

//战场分组
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum WarGroup : byte
{
    None = 0,//默认，非战场模式,没有战场分组
    GroupA = 1,//战场编组1
    GroupB = 2,//战场编组2
}


//玩家封号(6级封号)
//1.青铜 2.白银 3.黄金 4.铂金 5.钻石 6.王者
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum PlayerTitle : byte
{
    None = 0,//默认，无封号
    Title1 = 1,//青铜
    Title2 = 2,//白银
    Title3 = 3,//黄金
    Title4 = 4,//帕金
    Title5 = 5,//钻石
    Title6 = 6,//王者
}

//攻击模式
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum AttackMode : byte
{
    Peace = 0,
    Group = 1,
    Guild = 2,
    EnemyGuild = 3,
    RedBrown = 4,
    All = 5
}
//宠物攻击模式
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum PetMode : byte
{
    Both = 0,
    MoveOnly = 1,
    AttackOnly = 2,
    None = 3,
}


//宠物类型
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum PetType : byte
{
    Common = 0,//普通，法师召唤的，道士召唤的宝宝
    CreaturePet = 1,//非战斗宠物
    MyMonster = 2,//契约兽
    ChildTempMonster = 3,//契约兽的子兽，零时的

    //通用技能，契约兽带的技能，虚空之门，
    //虚空之门，每3秒召唤一个怪物，召唤30秒，30秒内如果被打死，则不能停止召唤
    //随机召唤10个怪物为其战斗5分钟，5分钟后，怪物重返虚空,超过叛变时间，直接死亡
    TempMonster = 4,



    All =11,//包括所有
}
//毒药类型
[Flags]
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum PoisonType : ushort
{
    None = 0,
    Green = 1,
    Red = 2,
    Slow = 4,//减速
    Frozen = 8,//冰冻，不能动了
    Stun = 16,//打昏
    Paralysis = 32,//麻痹
    DelayedExplosion = 64,//延期爆炸
    Bleeding = 128,//流血
    LRParalysis = 256,//解毒？
    DelayedBomb = 512,//延期自爆（契约兽的自爆）
}
//物品的绑定类型，搞那么多干毛，所有物品都一样就行了啊。靠了
[Flags]
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum BindMode : short
{
    none = 0,
    DontDeathdrop = 1,//0x0001 死亡不掉落
    DontDrop = 2,//0x0002 不掉落，不可丢弃？
    DontSell = 4,//0x0004 不能卖
    DontStore = 8,//0x0008 不能存储
    DontTrade = 16,//0x0010 不能交易
    DontRepair = 32,//0x0020 不能修理
    DontUpgrade = 64,//0x0040 不能升级
    DestroyOnDrop = 128,//0x0080 丢弃销毁
    BreakOnDeath = 256,//0x0100 死亡销毁
    BindOnEquip = 512,//0x0200 绑定装备
    NoSRepair = 1024,//0x0400 不能特殊修理
    NoWeddingRing = 2048,//0x0800 不是婚戒指,结婚不能带
    UnableToRent = 4096,//不能出租
    UnableToDisassemble = 8192//不能分解
}
//特殊物品的模式,搞那么多干毛，所有物品都一样就行了啊。靠了
[Flags]
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum SpecialItemMode : short
{
    None = 0,
    Paralize = 0x0001,//麻痹
    Teleport = 0x0002,//传送，传送戒指么
    Clearring = 0x0004,//隐身戒指
    Protection = 0x0008,//护身么？
    Revival = 0x0010,//复活？
    Muscle = 0x0020,//超负载戒指
    Flame = 0x0040,//火焰戒指？
    Healing = 0x0080,//治疗戒指？
    Probe = 0x0100,//探测
    Skill = 0x0200,//技巧（修炼技能加速）
    NoDuraLoss = 0x0400,//持久不掉落 坚硬戒指
    Blink = 0x800,//这个是针对技能的，目前还没有装备
}

//职业限制其实都可以不用管哦,当然，技能是要分职业的，其他的都没有必要分了，
[Flags]
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum RequiredClass : byte
{
    All=0,
    Warrior = 1,//战
    Wizard = 2,//法
    Taoist = 4,//道
    Assassin = 8,//刺
    Archer = 16,//弓
    Monk=32,//和尚
    WarWizTao = Warrior | Wizard | Taoist,//战法道 7
    None = WarWizTao | Assassin | Archer| Monk,//全职业 63
}

//性别限制，也可以不用管，喜欢穿女的就穿女的嘛，有个毛问题
[Flags]
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum RequiredGender : byte
{
    Male = 1,
    Female = 2,
    None = Male | Female
}

//这些限制倒是有必要的哦
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum RequiredType : byte
{
    Level = 0,
    MaxAC = 1,//防御
    MaxMAC = 2,//魔御
    MaxDC = 3,//攻击
    MaxMC = 4,//魔法
    MaxSC = 5,//道术
    MaxLevel = 6,//最大等级
    MinAC = 7,
    MinMAC = 8,
    MinDC = 9,
    MinMC = 10,
    MinSC = 11,
}
//这个是干毛的啊
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum ItemSet : byte
{
    None = 0,
    Spirit = 1,//祈祷套
    Recall = 2,//记忆套，可以召唤
    RedOrchid = 3,//虹膜套
    RedFlower = 4,//魔血套
    Smash = 5,//破碎套
    HwanDevil = 6,//幻魔套
    Purity = 7,//灵玉套
    FiveString = 8,//五玄套
    Mundane = 9,//平凡套
    NokChi = 10,//诺卡套
    TaoProtect = 11,//守护套
    Mir = 12,//天龙套
    Bone = 13,//龙骨套
    Bug = 14,//邪恶套
    WhiteGold = 15,//雷霆套
    WhiteGoldH = 16,//真雷霆套
    RedJade = 17,//烈焰套
    RedJadeH = 18,//真烈焰套
    Nephrite = 19,//光芒套
    NephriteH = 20,//真光芒套
    Whisker1 = 21,//勇气套
    Whisker2 = 22,//勇气套
    Whisker3 = 23,//勇气套
    Whisker4 = 24,//勇气套
    Whisker5 = 25,//勇气套
    Hyeolryong = 26,//血龙套（改为3件套）
    Monitor = 27,//掠夺者套(5件套改成3件套)
    Oppressive = 28,//狂暴套(5件套改成3件套)
    Paeok = 29,//贝玉套(5件套改成3件套)
    Sulgwan = 30,//黑暗套(5件套改成3件套)

    //以下是自行添加得
    GaleWind =51,//狂风套
    DragonWhistle = 52,//龙啸套

    //这里增加首饰套，从沃玛套到金龙套，狂龙套

}
//魔法技能?魔法效果？
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum Spell : byte
{
    None = 0,

    //Warrior
    Fencing = 1,//基本剑法
    Slaying = 2,//攻杀剑法
    Thrusting = 3,//刺杀
    HalfMoon = 4,//半月
    ShoulderDash = 5,//野蛮冲撞
    TwinDrakeBlade = 6,//雷霆剑法 双龙斩
    Entrapment = 7,//擒龙手
    FlamingSword = 8,//烈火剑法
    LionRoar = 9,//狮子吼
    CrossHalfMoon = 10,//狂风斩
    BladeAvalanche = 11,//空破斩
    ProtectionField = 12,//护身气幕
    Rage = 13,//剑气爆
    CounterAttack = 14,//天务
    SlashingBurst = 15,//逐日剑法
    Fury = 16,//血龙剑法
    ImmortalSkin = 17,//金刚不坏

    //Wizard
    FireBall = 31,//火球术
    Repulsion = 32,//抗拒火环
    ElectricShock = 33,//诱惑之光
    GreatFireBall = 34,//大火球
    HellFire = 35,//地狱火
    ThunderBolt = 36,//雷电术
    Teleport = 37,//瞬息移动
    FireBang = 38,//爆裂火焰
    FireWall = 39,//这个是火墙？火墙要去除暴击，或者暴击率降低50%以上
    Lightning = 40,//疾光电影
    FrostCrunch = 41,//寒冰掌
    ThunderStorm = 42,//地狱雷光(不死系怪物伤害高，普通怪物伤害很低)
    MagicShield = 43,//魔法盾
    TurnUndead = 44,//圣言术（只对不死系怪物有效）
    Vampirism = 45,//噬血术
    IceStorm = 46,//冰咆哮
    FlameDisruptor = 47,//灭天火
    Mirroring = 48,//分身术
    FlameField = 49,//火龙气焰
    Blizzard = 50,//这个是什么技能，像下雨
    MagicBooster = 51,//深延术
    MeteorStrike = 52,//流星火雨,释放时间过长，5段伤害，改为4段
    IceThrust = 53,//冰焰术
    FastMove = 54,//移形换位
    StormEscape = 55,//风暴传送，风雷术

    //Taoist
    Healing = 61,//治愈术
    SpiritSword = 62,//精神力战法
    Poisoning = 63,//施毒术
    SoulFireBall = 64,//火符
    SummonSkeleton = 65,//召唤骷髅
    Hiding = 67,//隐身术
    MassHiding = 68,//集体隐身术
    SoulShield = 69,//幽灵盾
    Revelation = 70,//心灵启示
    BlessedArmour = 71,//神圣战甲术
    EnergyRepulsor = 72,//气功波
    TrapHexagon = 73,//困魔咒吧？
    Purification = 74,//净化术
    MassHealing = 75,//群体治愈术
    Hallucination = 76,//迷魂术
    UltimateEnhancer = 77,//无极真气
    SummonShinsu = 78,//召唤神兽
    Reincarnation = 79,//回生术
    SummonHolyDeva = 80,//召唤月灵
    Curse = 81,//诅咒术
    Plague = 82,//瘟疫，调整下伤害
    PoisonCloud = 83,//群毒，毒云
    EnergyShield = 84,//阴阳法环
    PetEnhancer = 85,//血龙水
    HealingCircle = 86,//五行阵？

    //Assassin
    FatalSword= 91,//基本刺术
    DoubleSlash = 92,//风剑术
    Haste = 93,//体迅风
    FlashDash = 94,//拔刀术
    LightBody = 95,//风身术
    HeavenlySword = 96,//炎龙波
    FireBurst = 97,//旋风击
    Trap = 98,//灵魂陷阱
    PoisonSword = 99,//猛毒剑气
    MoonLight = 100,//月影术
    MPEater = 101,//真气调息
    SwiftFeet = 102,//轻身步
    DarkBody = 103,//烈火身
    Hemorrhage = 104,//血风击
    CrescentSlash = 105,//火镰狂舞
    MoonMist = 106,//这个还没实现，月色迷雾，应该是使得场景变得迷幻的技能

    //Archer
    Focus = 121,//基本箭法
    StraightShot = 122,//落日箭法,魔法
    DoubleShot = 123,//连珠箭法 改为物理，魔法适配的攻击，防御算物理
    ExplosiveTrap = 124,//烈火陷阱（这里有问题）
    DelayedExplosion = 125,//爆裂箭法
    Meditation = 126,//气功术
    BackStep = 127,//风弹步
    ElementalShot = 128,//蓄力一击
    Concentration= 129,//气流术
    Stonetrap = 130,//这个应该是地柱钉
    ElementalBarrier = 131,//金刚术
    SummonVampire = 132,//蛛魔阱
    VampireShot = 133,//噬血箭法
    SummonToad = 134,//痹魔阱
    PoisonShot = 135,//剧毒箭法
    CrippleShot = 136,//邪魔箭法
    SummonSnakes = 137,//蛇柱阱
    NapalmShot = 138,//火龙箭法
    OneWithNature = 139,//血龙闪
    BindingShot = 140,//天罗地网
    MentalState = 141,//弓箭精通

    //Custom，自定义
    Blink = 151,
    Portal = 152,
    BattleCry = 153,


    //定点移动，类似闪现
    FixedMove=161,
    //虚空，虚空之门,地狱之门，一个圆圈的门
    EmptyDoor = 162,
    //契约兽自爆
    MyMonsterBomb = 163,



    //Map Events,这几个是什么，好像没放在魔法技能表中,是地图的事件么？
    DigOutZombie = 200,//这个是僵尸爬出来，在地图上产生的洞
    Rubble = 201,//挖矿，土翻转出来
    MapLightning = 202,//地图上的闪电
    MapLava = 203,//地图上的熔岩
    MapQuake1 = 204,//地突
    MapQuake2 = 205,//地突
    MonKITO = 206,//鬼头2.5秒后自爆
    MonFireCircle = 207//鬼圈 6秒后消失
}


//释放各种技能特效，针对玩家，怪物
//部分通用技能，部分怪物带的技能，都在这里
public enum SpellEffect : byte
{
    None,
    FatalSword,//基本刺术的效果
    Teleport,//传送效果
    Healing,//治疗效果
    RedMoonEvil,//赤月恶魔的地钉
    TwinDrakeBlade,//双龙，特效
    MagicShieldUp,//魔法盾
    MagicShieldDown,//魔法盾
    GreatFoxSpirit,//悲月天珠的特效
    Entrapment,//擒龙手特效
    Reflect,//反伤？
    Critical,
    Mine,
    ElementalBarrierUp,//金刚盾
    ElementalBarrierDown,//金刚盾
    DelayedExplosion,//延迟炸弹特效
    MPEater,//真气调息,吸收蓝的效果
    Hemorrhage,//血风击
    Bleeding,//流血效果
    AwakeningSuccess,
    AwakeningFail,
    AwakeningMiss,
    AwakeningHit,
    StormEscape,//风暴传送，风雷术
    TurtleKing,//大龟王的特效
    Behemoth,//怨恶的特效
    Stunned,//击晕特效
    IcePillar,//冰柱效果
    TreeQueen,//树的女王的树钉
    GreatFoxThunder,//悲月的雷电特效
    Focus,//基本箭法，聚集，焦点，噬血等
    FlameRound,//火焰环绕
    PoisonRain,//毒雨
    DelayedBomb,//自爆效果，爆炸效果（契约兽的自爆）
}

public enum BuffType : byte
{
    None = 0,

    //魔法
    //magics
    TemporalFlux,//瞬移？
    Hiding,//隐藏
    Haste,//匆忙,体 迅 风，刺客技能
    SwiftFeet,//轻盈，增加移动速度
    Fury,//愤怒,增加攻击速度
    SoulShield,//灵魂盾牌
    BlessedArmour,//美甲
    LightBody,//光体
    UltimateEnhancer,//终极增强者
    ProtectionField,
    Rage,
    Curse,//诅咒
    MoonLight,
    DarkBody,
    Concentration,
    VampireShot,//噬血状态，噬血箭法
    PoisonShot,
    CounterAttack,
    MentalState,
    EnergyShield,
    MagicBooster,
    PetEnhancer,//血龙水
    ImmortalSkin,
    MagicShield,//魔法盾

    //特殊
    //special
    GameMaster = 100,//游戏玩家
    General,//一般
    Exp,//经验加成
    Drop,//掉落
    Gold,//金币？
    BagWeight,//负重？
    Transform,//改变,时装
    RelationshipEXP,//结婚经验
    Mentee,
    Mentor,//
    Guild,//行会
    Prison,//监狱，
    Rested,

    //统计
    //stats
    Impact = 200,//冲击
    Magic,//
    Taoist,
    Storm,
    HealthAid,
    ManaAid,
    Defence,
    MagicDefence,
    WonderDrug,
    Knapsack
}

public enum DefenceType : byte
{
    ACAgility,//防御，敏捷
    AC,//防御
    MACAgility,//魔御，敏捷
    MAC,//魔法防御
    Agility,//敏捷
    Repulsion,//反伤
    None
}

public enum ServerPacketIds : short
{
    Connected,
    ClientVersion,
    Disconnect,
    KeepAlive,
    NewAccount,
    ChangePassword,
    ChangePasswordBanned,
    Login,
    LoginBanned,
    LoginSuccess,
    NewCharacter,
    NewCharacterSuccess,
    DeleteCharacter,
    DeleteCharacterSuccess,
    StartGame,
    StartGameBanned,
    StartGameDelay,
    MapInformation,
    UserInformation,
    UserLocation,
    ObjectPlayer,
    ObjectRemove,
    ObjectTurn,
    ObjectWalk,
    ObjectRun,
    Chat,
    ObjectChat,
    NewItemInfo,
    MoveItem,
    EquipItem,
    MergeItem,
    RemoveItem,
    RemoveSlotItem,
    TakeBackItem,
    StoreItem,
    SplitItem,
    SplitItem1,
    DepositRefineItem,
    RetrieveRefineItem,
    RefineCancel,
    RefineItem,
    DepositTradeItem,
    RetrieveTradeItem,
    UseItem,
    DropItem,
    PlayerUpdate,
    PlayerInspect,
    LogOutSuccess,
    LogOutFailed,
    TimeOfDay,
    ChangeAMode,
    ChangePMode,
    ObjectItem,
    ObjectGold,
    GainedItem,
    GainedGold,
    LoseGold,
    GainedCredit,
    LoseCredit,
    ObjectMonster,
    ObjectAttack,
    Struck,
    ObjectStruck,
    DamageIndicator,
    DuraChanged,
    HealthChanged,
    DeleteItem,
    Death,
    ObjectDied,
    ColourChanged,
    ObjectColourChanged,
    ObjectGuildNameChanged,
    GainExperience,
    LevelChanged,
    ObjectLeveled,
    ObjectHarvest,
    ObjectHarvested,
    ObjectNpc,
    NPCResponse,
    ObjectHide,
    ObjectShow,
    Poisoned,
    ObjectPoisoned,
    MapChanged,
    ObjectTeleportOut,
    ObjectTeleportIn,
    TeleportIn,
    NPCGoods,
    NPCSell,
    NPCRepair,
    NPCSRepair,
    NPCRefine,
    NPCCheckRefine,
    NPCCollectRefine,
    NPCReplaceWedRing,
    NPCStorage,
    SellItem,
    CraftItem,
    RepairItem,
    ItemRepaired,
    NewMagic,
    RemoveMagic,
    MagicLeveled,
    Magic,
    MagicDelay,
    MagicCast,
    ObjectMagic,
    ObjectEffect,
    RangeAttack,
    Pushed,
    ObjectPushed,
    ObjectName,
    UserStorage,
    SwitchGroup,
    DeleteGroup,
    DeleteMember,
    GroupInvite,
    AddMember,
    Revived,
    ObjectRevived,
    SpellToggle,
    ObjectHealth,
    MapEffect,
    ObjectRangeAttack,
    AddBuff,
    RemoveBuff,
    ObjectHidden,
    RefreshItem,
    ObjectSpell,
    UserDash,
    ObjectDash,
    UserDashFail,
    ObjectDashFail,
    NPCConsign,
    NPCMarket,
    ConsignItem,
    MarketFail,
    MarketSuccess,
    ObjectSitDown,
    InTrapRock,
    BaseStatsInfo,
    UserName,
    ChatItemStats,
    GuildNoticeChange,
    GuildMemberChange,
    GuildStatus,
    GuildInvite,
    GuildExpGain,
    GuildNameRequest,
    GuildStorageGoldChange,
    GuildStorageItemChange,
    GuildStorageList,
    GuildRequestWar,
    DefaultNPC,
    NPCUpdate,
    NPCImageUpdate,
    MarriageRequest,
    DivorceRequest,
    MentorRequest,
    TradeRequest,
    TradeAccept,
    TradeGold,
    TradeItem,
    TradeConfirm,
    TradeCancel,
    MountUpdate,
    EquipSlotItem,
    FishingUpdate,
    ChangeQuest,
    CompleteQuest,
    ShareQuest,
    NewQuestInfo,
    GainedQuestItem,
    DeleteQuestItem,
    CancelReincarnation,
    RequestReincarnation,
    UserBackStep,
    ObjectBackStep,
    UserDashAttack,
    ObjectDashAttack,
    UserAttackMove,
    CombineItem,
    ItemUpgraded,
    SetConcentration,
    SetObjectConcentration,
    SetElemental,
    SetObjectElemental,
    RemoveDelayedExplosion,
    ObjectDeco,
    ObjectSneaking,
    ObjectLevelEffects,
    SetBindingShot,
    SendOutputMessage,

    NPCAwakening,
    NPCDisassemble,
    NPCDowngrade,
    NPCReset,
    AwakeningNeedMaterials,
    AwakeningLockedItem,
    Awakening,

    ReceiveMail,
    MailLockedItem,
    MailSendRequest,
    MailSent,
    ParcelCollected,
    MailCost,
	ResizeInventory,
    ResizeStorage,
    NewIntelligentCreature,
    UpdateIntelligentCreatureList,
    IntelligentCreatureEnableRename,
    IntelligentCreaturePickup,
    NPCPearlGoods,

    TransformUpdate,
    FriendUpdate,
    LoverUpdate,
    MentorUpdate,
    GuildBuffList,
    NPCRequestInput,
    GameShopInfo,
    GameShopStock,
    Rankings,
    Opendoor,

    GetRentedItems,
    ItemRentalRequest,
    ItemRentalFee,
    ItemRentalPeriod,
    DepositRentalItem,
    RetrieveRentalItem,
    UpdateRentalItem,
    CancelItemRental,
    ItemRentalLock,
    ItemRentalPartnerLock,
    CanConfirmItemRental,
    ConfirmItemRental,
    NewRecipeInfo,


    RechargeLink,
    RechargeResult,
    UserGold,
    UserInventory,
    NPCItemCollect,
    RetrieveItemCollect,
    DepositItemCollect,
    ItemCollectCancel,
    ConfirmItemCollect,
    NPCConsignCredit,
    NPCConsignDoulbe,
    ObjectMonsterChange,
    BlizzardStopTime,
    MyMonstersPackets,
    MyMonstersExpUpdate,
}

public enum ClientPacketIds : short
{
    ClientVersion,
    Disconnect,
    KeepAlive,
    NewAccount,
    ChangePassword,
    Login,
    NewCharacter,
    DeleteCharacter,
    StartGame,
    LogOut,
    Turn,
    Walk,
    Run,
    Chat,
    MoveItem,
    StoreItem,
    TakeBackItem,
    MergeItem,
    EquipItem,
    RemoveItem,
    RemoveSlotItem,
    SplitItem,
    UseItem,
    DropItem,
    DepositRefineItem,
    RetrieveRefineItem,
    RefineCancel,
    RefineItem,
    CheckRefine,
    ReplaceWedRing,
    DepositTradeItem,
    RetrieveTradeItem,
    DropGold,
    PickUp,
    Inspect,
    ChangeAMode,
    ChangePMode,
    ChangeTrade,
    Attack,
    RangeAttack,
    Harvest,
    CallNPC,
    TalkMonsterNPC,
    BuyItem,
    SellItem,
    CraftItem,
    RepairItem,
    BuyItemBack,
    SRepairItem,
    MagicKey,
    Magic,
    SwitchGroup,
    AddMember,
    DellMember,
    GroupInvite,
    TownRevive,
    SpellToggle,
    ConsignItem,
    MarketSearch,

    MarketBuy,
    MarketGetBack,
    RequestUserName,
    RequestChatItem,
    EditGuildMember,
    EditGuildNotice,
    GuildInvite,
    GuildNameReturn,
    RequestGuildInfo,
    GuildStorageGoldChange,
    GuildStorageItemChange,
    GuildWarReturn,
    MarriageRequest,
    MarriageReply,
    ChangeMarriage,
    DivorceRequest,
    DivorceReply,
    AddMentor,
    MentorReply,
    AllowMentor,
    CancelMentor,
    TradeRequest,
    TradeReply,
    TradeGold,
    TradeConfirm,
    TradeCancel,
    EquipSlotItem,
    FishingCast,
    FishingChangeAutocast,
    AcceptQuest,
    FinishQuest,
    AbandonQuest,
    ShareQuest,

    AcceptReincarnation,
    CancelReincarnation,
    CombineItem,

    SetConcentration,
    AwakeningNeedMaterials,
    AwakeningLockedItem,
    Awakening,
    DisassembleItem,
    DowngradeAwakening,
    ResetAddedItem,

    SendMail,
    ReadMail,
    CollectParcel,
    DeleteMail,
    LockMail,
    MailLockedItem,
    MailCost,

    UpdateIntelligentCreature,
    IntelligentCreaturePickup,

    AddFriend,
    RemoveFriend,
    RefreshFriends,
    AddMemo,
    GuildBuffUpdate,
    NPCConfirmInput,
    GameshopBuy,

    ReportIssue,
    GetRanking,
    Opendoor,

    GetRentedItems,
    ItemRentalRequest,
    ItemRentalFee,
    ItemRentalPeriod,
    DepositRentalItem,
    RetrieveRentalItem,
    CancelItemRental,
    ItemRentalLockFee,
    ItemRentalLockItem,
    ConfirmItemRental,
    RechargeCredit,//充值元宝
    RefreshUserGold,//刷新用户账户
    RechargeEnd,//支付完成
    RefreshInventory,//刷新背包
    DepositItemCollect,//物品收集，放入物品
    RetrieveItemCollect,//物品收集，取回物品
    ItemCollectCancel,//物品收集，取消
    ConfirmItemCollect,//确认收集物品
    MagicParameter,//魔法参数，魔法释放的前置
    MyMonsterOperation,//契约兽操作
}

public enum ConquestType : byte
{
    Request = 0,
    Auto = 1,
    Forced = 2,
}

public enum ConquestGame : byte
{
    CapturePalace = 0,
    KingOfHill = 1,
    Random = 2,
    Classic = 3,
    ControlPoints = 4
}

public class InIReader
{
    #region Fields
    private readonly List<string> _contents;
    private readonly string _fileName;
    #endregion

    #region Constructor
    public InIReader(string fileName)
    {
        _fileName = fileName;

        _contents = new List<string>();
        try
        {
            if (File.Exists(_fileName))
                _contents.AddRange(File.ReadAllLines(_fileName, EncodingType.GetType(_fileName)));
        }
        catch
        {
        }
    }
    #endregion

    #region Functions
    public string FindValue(string section, string key)
    {
        for (int a = 0; a < _contents.Count; a++)
            if (String.CompareOrdinal(_contents[a], "[" + section + "]") == 0)
                for (int b = a + 1; b < _contents.Count; b++)
                    if (String.CompareOrdinal(_contents[b].Split('=')[0], key) == 0)
                        return _contents[b].Split('=')[1];
                    else if (_contents[b].StartsWith("[") && _contents[b].EndsWith("]"))
                        return null;
        return null;
    }

    private int FindIndex(string section, string key)
    {
        for (int a = 0; a < _contents.Count; a++)
            if (String.CompareOrdinal(_contents[a], "[" + section + "]") == 0)
                for (int b = a + 1; b < _contents.Count; b++)
                    if (String.CompareOrdinal(_contents[b].Split('=')[0], key) == 0)
                        return b;
                    else if (_contents[b].StartsWith("[") && _contents[b].EndsWith("]"))
                    {
                        _contents.Insert(b - 1, key + "=");
                        return b - 1;
                    }
                    else if (_contents.Count - 1 == b)
                    {
                        _contents.Add(key + "=");
                        return _contents.Count - 1;
                    }
        if (_contents.Count > 0)
            _contents.Add("");

        _contents.Add("[" + section + "]");
        _contents.Add(key + "=");
        return _contents.Count - 1;
    }

    public void Save()
    {
        try
        {
            File.WriteAllLines(_fileName, _contents);
        }
        catch
        {
        }
    }
    #endregion

    #region Read
    public bool ReadBoolean(string section, string key, bool Default)
    {
        bool result;

        if (!bool.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }

        return result;
    }

    public byte ReadByte(string section, string key, byte Default)
    {
        byte result;

        if (!byte.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }


        return result;
    }

    public sbyte ReadSByte(string section, string key, sbyte Default)
    {
        sbyte result;

        if (!sbyte.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }


        return result;
    }

    public ushort ReadUInt16(string section, string key, ushort Default)
    {
        ushort result;

        if (!ushort.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }


        return result;
    }

    public short ReadInt16(string section, string key, short Default)
    {
        short result;

        if (!short.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }


        return result;
    }

    public uint ReadUInt32(string section, string key, uint Default)
    {
        uint result;

        if (!uint.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }

        return result;
    }

    public int ReadInt32(string section, string key, int Default)
    {
        int result;

        if (!int.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }

        return result;
    }

    public ulong ReadUInt64(string section, string key, ulong Default)
    {
        ulong result;

        if (!ulong.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }

        return result;
    }

    public long ReadInt64(string section, string key, long Default)
    {
        long result;

        if (!long.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }


        return result;
    }

    public float ReadSingle(string section, string key, float Default)
    {
        float result;

        if (!float.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }

        return result;
    }

    public double ReadDouble(string section, string key, double Default)
    {
        double result;

        if (!double.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }

        return result;
    }

    public decimal ReadDecimal(string section, string key, decimal Default)
    {
        decimal result;

        if (!decimal.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }

        return result;
    }

    public string ReadString(string section, string key, string Default)
    {
        string result = FindValue(section, key);

        if (string.IsNullOrEmpty(result))
        {
            result = Default;
            Write(section, key, Default);
        }

        return result;
    }

    public char ReadChar(string section, string key, char Default)
    {
        char result;

        if (!char.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }

        return result;
    }

    public Point ReadPoint(string section, string key, Point Default)
    {
        string temp = FindValue(section, key);
        int tempX, tempY;
        if (temp == null || !int.TryParse(temp.Split(',')[0], out tempX))
        {
            Write(section, key, Default);
            return Default;
        }
        if (!int.TryParse(temp.Split(',')[1], out tempY))
        {
            Write(section, key, Default);
            return Default;
        }

        return new Point(tempX, tempY);
    }

    public Size ReadSize(string section, string key, Size Default)
    {
        string temp = FindValue(section, key);
        int tempX, tempY;
        if (!int.TryParse(temp.Split(',')[0], out tempX))
        {
            Write(section, key, Default);
            return Default;
        }
        if (!int.TryParse(temp.Split(',')[1], out tempY))
        {
            Write(section, key, Default);
            return Default;
        }

        return new Size(tempX, tempY);
    }

    public TimeSpan ReadTimeSpan(string section, string key, TimeSpan Default)
    {
        TimeSpan result;

        if (!TimeSpan.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }


        return result;
    }

    public float ReadFloat(string section, string key, float Default)
    {
        float result;

        if (!float.TryParse(FindValue(section, key), out result))
        {
            result = Default;
            Write(section, key, Default);
        }

        return result;
    }
    #endregion

    #region Write
    public void Write(string section, string key, bool value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }

    public void Write(string section, string key, byte value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }

    public void Write(string section, string key, sbyte value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }

    public void Write(string section, string key, ushort value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }

    public void Write(string section, string key, short value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }

    public void Write(string section, string key, uint value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }

    public void Write(string section, string key, int value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }

    public void Write(string section, string key, ulong value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }

    public void Write(string section, string key, long value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }

    public void Write(string section, string key, float value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }

    public void Write(string section, string key, double value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }

    public void Write(string section, string key, decimal value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }

    public void Write(string section, string key, string value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }

    public void Write(string section, string key, char value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }

    public void Write(string section, string key, Point value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value.X + "," + value.Y;
        Save();
    }

    public void Write(string section, string key, Size value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value.Width + "," + value.Height;
        Save();
    }

    public void Write(string section, string key, TimeSpan value)
    {
        _contents[FindIndex(section, key)] = key + "=" + value;
        Save();
    }
    #endregion
}

public static class Globals
{
    public const int
        MinAccountIDLength = 3,
        MaxAccountIDLength = 15,

        MinPasswordLength = 5,
        MaxPasswordLength = 15,

        //角色名称最小长度，最大长度，最多建立多少个角色
        MinCharacterNameLength = 2,
        MaxCharacterNameLength = 10,
        MaxCharacterCount = 4,

        MaxChatLength = 80,

        MaxGroup = 15,//最大组队人数
        
        MaxAttackRange = 9,

        MaxDragonLevel = 13,

        FlagIndexCount = 1999,

        MaxConcurrentQuests = 20,

        LogDelay = 5000,//这个影响到游戏退出的时间

        DataRange = 16;//Was 24,数据处理的范围，人物16格内

    //这个是寄卖物品的费率，成功收取的佣金3%的佣金
    public static float Commission = 0.03F;
    //搜素物品的最小时间间隔，小于这个间隔，不处理
    public const uint SearchDelay = 500,
                      ConsignmentLength = 7,//寄卖物品的期限7天
                      ConsignmentCost = 1000,//寄卖物品的费用，每次寄卖，都要收取的费用
                      MinConsignment = 1000,//寄卖的最小金额
                      MaxConsignment = 50000000,//寄卖物品的最大金额
                      MaxConsignmentCount = 20;//最多寄卖数量

    //充值获得元宝数
    public const uint Recharge10 = 11000, Recharge20 = 23000, Recharge50 = 60000, Recharge100 = 130000;



}

public static class Functions
{
    public static bool CompareBytes(byte[] a, byte[] b)
    {
        if (a == b) return true;

        if (a == null || b == null || a.Length != b.Length) return false;

        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;

        return true;
    }

    public static bool TryParse(string s, out Point temp)
    {
        temp = Point.Empty;
        int tempX, tempY;
        if (String.IsNullOrWhiteSpace(s)) return false;

        string[] data = s.Split(',');
        if (data.Length <= 1) return false;

        if (!Int32.TryParse(data[0], out tempX))
            return false;

        if (!Int32.TryParse(data[1], out tempY))
            return false;

        temp = new Point(tempX, tempY);
        return true;
    }
    public static Point Subtract(this Point p1, Point p2)
    {
        return new Point(p1.X - p2.X, p1.Y - p2.Y);
    }
    public static Point Subtract(this Point p1, int x, int y)
    {
        return new Point(p1.X - x, p1.Y - y);
    }
    public static Point Add(this Point p1, Point p2)
    {
        return new Point(p1.X + p2.X, p1.Y + p2.Y);
    }
    public static Point Add(this Point p1, int x, int y)
    {
        return new Point(p1.X + x, p1.Y + y);
    }
    public static string PointToString(Point p)
    {
        return String.Format("{0}, {1}", p.X, p.Y);
    }
    public static bool InRange(Point a, Point b, int i)
    {
        return Math.Abs(a.X - b.X) <= i && Math.Abs(a.Y - b.Y) <= i;
    }

    //是否在线性攻击范围8个方向的攻击范围
    public static bool InAttackRange(Point a, Point b, int i)
    {
        if(a.X==b.X || a.Y == b.Y)
        {
            return Math.Abs(a.X - b.X) <= i && Math.Abs(a.Y - b.Y) <= i;
        }
        else
        {
            return Math.Abs(a.X - b.X) <= i && Math.Abs(a.Y - b.Y) <= i && Math.Abs(a.X - b.X)== Math.Abs(a.Y - b.Y);
        }
    }

    public static bool FacingEachOther(MirDirection dirA, Point pointA, MirDirection dirB, Point pointB)
    {
        if (dirA == DirectionFromPoint(pointA, pointB) && dirB == DirectionFromPoint(pointB, pointA))
        {
            return true;
        }

        return false;
    }

    public static string PrintTimeSpanFromSeconds(double secs, bool accurate = true)
    {
        TimeSpan t = TimeSpan.FromSeconds(secs);
        string answer;
        if (t.TotalMinutes < 1.0)
        {
            answer = string.Format("{0}秒", t.Seconds);
        }
        else if (t.TotalHours < 1.0)
        {
            answer = accurate ? string.Format("{0}分 {1:D2}秒", t.Minutes, t.Seconds) : string.Format("{0}分", t.Minutes);
        }
        else if (t.TotalDays < 1.0)
        {
            answer = accurate ? string.Format("{0}时 {1:D2}分 {2:D2}秒", (int)t.Hours, t.Minutes, t.Seconds) : string.Format("{0}时 {1:D2}分", (int)t.TotalHours, t.Minutes);
        }
        else // more than 1 day
        {
            answer = accurate ? string.Format("{0}天 {1:D2}时 {2:D2}分 {3:D2}秒", (int)t.Days, (int)t.Hours, t.Minutes, t.Seconds) : string.Format("{0}天 {1}时 {2:D2}分", (int)t.TotalDays, (int)t.Hours, t.Minutes);
        }

        return answer;
    }

    public static string PrintTimeSpanFromMilliSeconds(double milliSeconds)
    {
        TimeSpan t = TimeSpan.FromMilliseconds(milliSeconds);
        string answer;
        if (t.TotalMinutes < 1.0)
        {
            answer = string.Format("{0}.{1}秒", t.Seconds, (decimal)(t.Milliseconds / 100));
        }
        else if (t.TotalHours < 1.0)
        {
            answer = string.Format("{0}分 {1:D2}秒", (int)t.TotalMinutes, t.Seconds);
        }
        else if (t.TotalDays < 1.0)
        {
            answer = string.Format("{0}时 {1:D2}分 {2:D2}秒", (int)t.TotalHours, t.Minutes, t.Seconds);
        }
        else
        {
            answer = string.Format("{0}天 {1}时 {2:D2}分 {3:D2}秒", (int)t.Days, (int)t.Hours, t.Minutes, t.Seconds);
        }

        return answer;
    }

    public static MirDirection PreviousDir(MirDirection d)
    {
        switch (d)
        {
            case MirDirection.Up:
                return MirDirection.UpLeft;
            case MirDirection.UpRight:
                return MirDirection.Up;
            case MirDirection.Right:
                return MirDirection.UpRight;
            case MirDirection.DownRight:
                return MirDirection.Right;
            case MirDirection.Down:
                return MirDirection.DownRight;
            case MirDirection.DownLeft:
                return MirDirection.Down;
            case MirDirection.Left:
                return MirDirection.DownLeft;
            case MirDirection.UpLeft:
                return MirDirection.Left;
            default: return d;
        }
    }
    public static MirDirection NextDir(MirDirection d)
    {
        switch (d)
        {
            case MirDirection.Up:
                return MirDirection.UpRight;
            case MirDirection.UpRight:
                return MirDirection.Right;
            case MirDirection.Right:
                return MirDirection.DownRight;
            case MirDirection.DownRight:
                return MirDirection.Down;
            case MirDirection.Down:
                return MirDirection.DownLeft;
            case MirDirection.DownLeft:
                return MirDirection.Left;
            case MirDirection.Left:
                return MirDirection.UpLeft;
            case MirDirection.UpLeft:
                return MirDirection.Up;
            default: return d;
        }
    }
    public static MirDirection DirectionFromPoint(Point source, Point dest)
    {
        if (source.X < dest.X)
        {
            if (source.Y < dest.Y)
                return MirDirection.DownRight;
            if (source.Y > dest.Y)
                return MirDirection.UpRight;
            return MirDirection.Right;
        }

        if (source.X > dest.X)
        {
            if (source.Y < dest.Y)
                return MirDirection.DownLeft;
            if (source.Y > dest.Y)
                return MirDirection.UpLeft;
            return MirDirection.Left;
        }

        return source.Y < dest.Y ? MirDirection.Down : MirDirection.Up;
    }



    public static Size Add(this Size p1, Size p2)
    {
        return new Size(p1.Width + p2.Width, p1.Height + p2.Height);
    }
    public static Size Add(this Size p1, int width, int height)
    {
        return new Size(p1.Width + width, p1.Height + height);
    }

    public static Point PointMove(Point p, MirDirection d, int i)
    {
        switch (d)
        {
            case MirDirection.Up:
                p.Offset(0, -i);
                break;
            case MirDirection.UpRight:
                p.Offset(i, -i);
                break;
            case MirDirection.Right:
                p.Offset(i, 0);
                break;
            case MirDirection.DownRight:
                p.Offset(i, i);
                break;
            case MirDirection.Down:
                p.Offset(0, i);
                break;
            case MirDirection.DownLeft:
                p.Offset(-i, i);
                break;
            case MirDirection.Left:
                p.Offset(-i, 0);
                break;
            case MirDirection.UpLeft:
                p.Offset(-i, -i);
                break;
        }
        return p;
    }
    public static Point Left(Point p, MirDirection d)
    {
        switch (d)
        {
            case MirDirection.Up:
                p.Offset(-1, 0);
                break;
            case MirDirection.UpRight:
                p.Offset(-1, -1);
                break;
            case MirDirection.Right:
                p.Offset(0, -1);
                break;
            case MirDirection.DownRight:
                p.Offset(1, -1);
                break;
            case MirDirection.Down:
                p.Offset(1, 0);
                break;
            case MirDirection.DownLeft:
                p.Offset(1, 1);
                break;
            case MirDirection.Left:
                p.Offset(0, 1);
                break;
            case MirDirection.UpLeft:
                p.Offset(-1, 1);
                break;
        }
        return p;
    }

    public static Point Right(Point p, MirDirection d)
    {
        switch (d)
        {
            case MirDirection.Up:
                p.Offset(1, 0);
                break;
            case MirDirection.UpRight:
                p.Offset(1, 1);
                break;
            case MirDirection.Right:
                p.Offset(0, 1);
                break;
            case MirDirection.DownRight:
                p.Offset(-1, 1);
                break;
            case MirDirection.Down:
                p.Offset(-1, 0);
                break;
            case MirDirection.DownLeft:
                p.Offset(-1,-1);
                break;
            case MirDirection.Left:
                p.Offset(0, -1);
                break;
            case MirDirection.UpLeft:
                p.Offset(1, -1);
                break;
        }
        return p;
    }
    
    //2点间最大距离，可以当做2点间的距离
    public static int MaxDistance(Point p1, Point p2)
    {
        return Math.Max(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
    }

    public static MirDirection ReverseDirection(MirDirection dir)
    {
        switch (dir)
        {
            case MirDirection.Up:
                return MirDirection.Down;
            case MirDirection.UpRight:
                return MirDirection.DownLeft;
            case MirDirection.Right:
                return MirDirection.Left;
            case MirDirection.DownRight:
                return MirDirection.UpLeft;
            case MirDirection.Down:
                return MirDirection.Up;
            case MirDirection.DownLeft:
                return MirDirection.UpRight;
            case MirDirection.Left:
                return MirDirection.Right;
            case MirDirection.UpLeft:
                return MirDirection.DownRight;
            default:
                return dir;
        }
    }
    //
    public static ItemInfo GetRealItem(ItemInfo Origin, ushort Level, MirClass job, List<ItemInfo> ItemList)
    {
        if (Origin.ClassBased>0 && Origin.LevelBased>0)
            return GetClassAndLevelBasedItem(Origin, job, Level, ItemList);
        if (Origin.ClassBased>0)
            return GetClassBasedItem(Origin, job, ItemList);
        if (Origin.LevelBased>0)
            return GetLevelBasedItem(Origin, Level, ItemList);
        return Origin;
    }
    public static ItemInfo GetLevelBasedItem(ItemInfo Origin, ushort level, List<ItemInfo> ItemList)
    {
        ItemInfo output = Origin;
        for (int i = 0; i < ItemList.Count; i++)
        {
            ItemInfo info = ItemList[i];
            if (ItemInfo.IsLevelBased(Origin,info, level))
            {
                if (output.RequiredAmount < info.RequiredAmount)
                    output = info;
            }
        }
        return output;
    }
    public static ItemInfo GetClassBasedItem(ItemInfo Origin, MirClass job, List<ItemInfo> ItemList)
    {
        for (int i = 0; i < ItemList.Count; i++)
        {
            ItemInfo info = ItemList[i];
            if (ItemInfo.IsClassBased(Origin,info, job))
            {
                return info;
            }
        }
        return Origin;
    }

    public static ItemInfo GetClassAndLevelBasedItem(ItemInfo Origin, MirClass job, ushort level, List<ItemInfo> ItemList)
    {
        ItemInfo output = Origin;
        for (int i = 0; i < ItemList.Count; i++)
        {
            ItemInfo info = ItemList[i];
            if (ItemInfo.IsClassBased(Origin,info, job) && ItemInfo.IsLevelBased(Origin,info, level))
            {
                if (output.RequiredAmount < info.RequiredAmount)
                    output = info;
            }
        }
        return output;
    }

    public static string StringOverLines(string line, int maxWordsPerLine, int maxLettersPerLine)
    {
        string newString = string.Empty;

        string[] words = line.Split(' ');

        int lineLength = 0;

        for (int i = 0; i < words.Length; i++)
        {
            lineLength += words[i].Length + 1;

            newString += words[i] + " ";
            if (i > 0 && i % maxWordsPerLine == 0 && lineLength > maxLettersPerLine)
            {
                lineLength = 0;
                newString += "\r\n";
            }
        }

        return newString;
    }

    public static byte[] ImageToByteArray(Image imageIn)
    {
        MemoryStream ms = new MemoryStream();
        imageIn.Save(ms, ImageFormat.Gif);
        return ms.ToArray();
    }

    public static Image ByteArrayToImage(byte[] byteArrayIn)
    {
        MemoryStream ms = new MemoryStream(byteArrayIn);
        Image returnImage = Image.FromStream(ms);
        return returnImage;
    }

    public static IEnumerable<byte[]> SplitArray(byte[] value, int bufferLength)
    {
        int countOfArray = value.Length / bufferLength;
        if (value.Length % bufferLength > 0)
            countOfArray++;
        for (int i = 0; i < countOfArray; i++)
        {
            yield return value.Skip(i * bufferLength).Take(bufferLength).ToArray();
        }
    }

    public static byte[] CombineArray(List<byte[]> arrays)
    {
        byte[] rv = new byte[arrays.Sum(x => x.Length)];
        int offset = 0;
        foreach (byte[] array in arrays)
        {
            System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
            offset += array.Length;
        }
        return rv;
    }
}
//选择的职业
public class SelectInfo
{
    public ulong Index;
    public string Name = string.Empty;
    public ushort Level;
    public MirClass Class;
    public MirGender Gender;
    public DateTime LastAccess;
    
        public SelectInfo()
        { }
        public SelectInfo(BinaryReader reader)
        {
            Index = (ulong)reader.ReadInt64();
            Name = reader.ReadString();
            Level = reader.ReadUInt16();
            Class = (MirClass)reader.ReadByte();
            Gender = (MirGender)reader.ReadByte();
            LastAccess = DateTime.FromBinary(reader.ReadInt64());
        }
        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(Name);
            writer.Write(Level);
            writer.Write((byte)Class);
            writer.Write((byte)Gender);
            writer.Write(LastAccess.ToBinary());
        }
}

public class RentalInformation
{
    public string OwnerName;
    public BindMode BindingFlags = BindMode.none;
    public DateTime ExpiryDate;
    public bool RentalLocked;

    public RentalInformation()
    { }

    public RentalInformation(BinaryReader reader, int version = int.MaxValue, int CustomVersion = int.MaxValue)
    {
        OwnerName = reader.ReadString();
        BindingFlags = (BindMode)reader.ReadInt16();
        ExpiryDate = DateTime.FromBinary(reader.ReadInt64());
        RentalLocked = reader.ReadBoolean();
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(OwnerName);
        writer.Write((short)BindingFlags);
        writer.Write(ExpiryDate.ToBinary());
        writer.Write(RentalLocked);
    }
}

/// <summary>
/// 商店物品
/// 
/// </summary>
public class GameShopItem
{
    public int ItemIndex;//物品ID
    public int GIndex;//商品ID
    [JsonIgnore]
    public ItemInfo Info;//物品实体
    public uint GoldPrice = 0;//金币价格，如果金币价格是0，则只能元宝购买
    public uint CreditPrice = 0;//元宝价格
    public uint Count = 1;//这个是商品保护物品的数量，不是购买数量哦,本身包含的物品数量
    public string Class = "";//职业
    public string Category = "";//分类，药水，毒符，坐骑，时装等
    public int Stock = 0;//库存，0代表无限库存
    public bool iStock = false;//是否内部库存，如果是内部库存的意思是限制每个角色只能买多少个，否则是所有人的库存
    public int TimeStock = 0;//时间库存，每个小时左右，产生多少个。
    public bool Deal = false;//推荐
    public bool TopItem = false;//热销
    public DateTime Date;//判断新品

    public byte acceptType = 0;//接收类型0：默认（背包接收） 1：邮件接收

    public long lastBuyTime = 0;
    public long TimeStockCount = 0;


    public GameShopItem()
    {

    }


    //重新绑定关联，关联具体的物品
    public bool BindItem()
    {
        Info = ItemInfo.getItem(ItemIndex);
        if (Info != null)
        {
            return true;
        }
        return false;
    }


    public GameShopItem(BinaryReader reader, int version = int.MaxValue, int Customversion = int.MaxValue)
    {
        ItemIndex = reader.ReadInt32();
        GIndex = reader.ReadInt32();
        GoldPrice = reader.ReadUInt32();
        CreditPrice = reader.ReadUInt32();
        Count = reader.ReadUInt32();
        Class = reader.ReadString();
        Category = reader.ReadString();
        Stock = reader.ReadInt32();
        iStock = reader.ReadBoolean();
        Deal = reader.ReadBoolean();
        TopItem = reader.ReadBoolean();
        Date = DateTime.FromBinary(reader.ReadInt64());
    }

    public GameShopItem(BinaryReader reader, bool packet = false)
    {
        ItemIndex = reader.ReadInt32();
        GIndex = reader.ReadInt32();
        Info = new ItemInfo(reader);
        GoldPrice = reader.ReadUInt32();
        CreditPrice = reader.ReadUInt32();
        Count = reader.ReadUInt32();
        Class = reader.ReadString();
        Category = reader.ReadString();
        Stock = reader.ReadInt32();
        iStock = reader.ReadBoolean();
        Deal = reader.ReadBoolean();
        TopItem = reader.ReadBoolean();
        Date = DateTime.FromBinary(reader.ReadInt64());
    }

    public void Save(BinaryWriter writer, bool packet = false)
    {
        writer.Write(ItemIndex);
        writer.Write(GIndex);
        if (packet) Info.Save(writer);
        writer.Write(GoldPrice);
        writer.Write(CreditPrice);
        writer.Write(Count);
        writer.Write(Class);
        writer.Write(Category);
        writer.Write(Stock);
        writer.Write(iStock);
        writer.Write(Deal);
        writer.Write(TopItem);
        
        writer.Write(Date.ToBinary());

        SaveDB();
    }

    /// <summary>
    /// 加载所有数据
    /// </summary>
    /// <returns></returns>
    public static List<GameShopItem> loadAll()
    {
        List<GameShopItem> list = new List<GameShopItem>();
        DbDataReader read = MirConfigDB.ExecuteReader("select * from GameShopItem where Deal=0");

        while (read.Read())
        {
            GameShopItem obj = new GameShopItem();
  
            obj.GIndex = read.GetInt32(read.GetOrdinal("GIndex"));
            obj.ItemIndex = read.GetInt32(read.GetOrdinal("ItemIndex"));
            obj.Info = ItemInfo.getItem(obj.ItemIndex);
            if (obj.Info == null)
            {
                continue;
            }


            obj.GoldPrice = (uint)read.GetInt32(read.GetOrdinal("GoldPrice"));
            obj.CreditPrice = (uint)read.GetInt32(read.GetOrdinal("CreditPrice"));
            obj.Count = (uint)read.GetInt32(read.GetOrdinal("Count"));
            obj.Class = read.GetString(read.GetOrdinal("Class"));
            obj.Category = read.GetString(read.GetOrdinal("Category"));
            obj.Stock = read.GetInt32(read.GetOrdinal("Stock"));
            obj.iStock = read.GetBoolean(read.GetOrdinal("iStock"));
            obj.Deal = read.GetBoolean(read.GetOrdinal("Deal"));
            obj.TopItem = read.GetBoolean(read.GetOrdinal("TopItem"));
            obj.Date = read.GetDateTime(read.GetOrdinal("endDate"));
            obj.acceptType= read.GetByte(read.GetOrdinal("acceptType"));
            obj.TimeStock = read.GetInt32(read.GetOrdinal("TimeStock"));
            DBObjectUtils.updateObjState(obj, obj.GIndex);
            list.Add(obj);
        }
        return list;
    }

    //保存到数据库
    public void SaveDB()
    {
        byte state = DBObjectUtils.ObjState(this, GIndex);
        if (state == 0)//没有改变
        {
            return;
        }
        List<SQLiteParameter> lp = new List<SQLiteParameter>();
        lp.Add(new SQLiteParameter("ItemIndex", ItemIndex));
        lp.Add(new SQLiteParameter("ItemIdx", Info.Index));
        lp.Add(new SQLiteParameter("GoldPrice", GoldPrice));
        lp.Add(new SQLiteParameter("CreditPrice", CreditPrice));
        lp.Add(new SQLiteParameter("Count", Count));
        lp.Add(new SQLiteParameter("Class", Class));
        lp.Add(new SQLiteParameter("Category", Category));

        lp.Add(new SQLiteParameter("Stock", Stock));
        lp.Add(new SQLiteParameter("iStock", iStock));

        lp.Add(new SQLiteParameter("Deal", Deal));
        lp.Add(new SQLiteParameter("TopItem", TopItem));
        lp.Add(new SQLiteParameter("endDate", Date));
        lp.Add(new SQLiteParameter("acceptType", acceptType));
        //新增
        if (state == 1)
        {
            if (GIndex > 0)
            {
                lp.Add(new SQLiteParameter("GIndex", GIndex));
            }
            string sql = "insert into GameShopItem" + SQLiteHelper.createInsertSql(lp.ToArray()); ;
            MirConfigDB.Execute(sql, lp.ToArray());
        }
        //修改
        if (state == 2)
        {
            string sql = "update GameShopItem set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where GIndex=@GIndex";
            lp.Add(new SQLiteParameter("GIndex", GIndex));
            MirConfigDB.Execute(sql, lp.ToArray());
        }
        DBObjectUtils.updateObjState(this, GIndex);

       
    }


    public override string ToString()
    {
        return string.Format("{0}: {1}", GIndex, Info.Name);
    }

}

public class Awake
{
    //Awake Option,装备觉醒的配置，成功率70%
    public static byte AwakeSuccessRate = 70;
    public static byte AwakeHitRate = 70;
    public static int MaxAwakeLevel = 5;
    public static byte Awake_WeaponRate = 1;
    public static byte Awake_HelmetRate = 1;
    public static byte Awake_ArmorRate = 5;
    public static byte AwakeChanceMin = 1;
    public static float[] AwakeMaterialRate = new float[4] { 1.0F, 1.0F, 1.0F, 1.0F };
    public static byte[] AwakeChanceMax = new byte[4] { 1, 2, 3, 4 };
    public static List<List<byte>[]> AwakeMaterials = new List<List<byte>[]>();

    public AwakeType type;
    List<byte> listAwake = new List<byte>();

    public Awake(BinaryReader reader)
    {
        type = (AwakeType)reader.ReadByte();
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            listAwake.Add(reader.ReadByte());
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)type);
        writer.Write(listAwake.Count);
        foreach (byte value in listAwake)
        {
            writer.Write(value);
        }
    }

    public Awake()
    {
        type = AwakeType.None;
    }

    public bool IsMaxLevel() { return listAwake.Count == Awake.MaxAwakeLevel; }

    public int getAwakeLevel() { return listAwake.Count; }

    public byte getAwakeValue()
    {
        byte total = 0;

        foreach (byte value in listAwake)
        {
            total += value;
        }

        return total;
    }

    public bool CheckAwakening(UserItem item, AwakeType type)
    {
        if (item.Info.Bind.HasFlag(BindMode.DontUpgrade))
            return false;

        if (item.Info.CanAwakening != true)
            return false;

        if (item.Info.Grade == ItemGrade.None)
            return false;

        if (IsMaxLevel()) return false;

        if (this.type == AwakeType.None)
        {
            if (item.Info.Type == ItemType.Weapon)
            {
                if (type == AwakeType.DC ||
                    type == AwakeType.MC ||
                    type == AwakeType.SC)
                {
                    this.type = type;
                    return true;
                }
                else
                    return false;
            }
            else if (item.Info.Type == ItemType.Helmet)
            {
                if (type == AwakeType.AC ||
                    type == AwakeType.MAC)
                {
                    this.type = type;
                    return true;
                }
                else
                    return false;
            }
            else if (item.Info.Type == ItemType.Armour)
            {
                if (type == AwakeType.HPMP)
                {
                    this.type = type;
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }
        else
        {
            if (this.type == type)
                return true;
            else
                return false;
        }
    }

    public int UpgradeAwake(UserItem item, AwakeType type, out bool[] isHit)
    {
        //return -1 condition error, -1 = dont upgrade, 0 = failed, 1 = Succeed,  
        isHit = null;
        if (CheckAwakening(item, type) != true)
            return -1;

        Random rand = new Random(DateTime.Now.Millisecond);

        if (rand.Next(0, 100) <= AwakeSuccessRate)
        {
            isHit = Awakening(item);
            return 1;
        }
        else
        {
            int idx;
            isHit = makeHit(1, out idx);
            return 0;
        }
    }

    public int RemoveAwake()
    {
        if (listAwake.Count > 0)
        {
            listAwake.Remove(listAwake[listAwake.Count - 1]);

            if (listAwake.Count == 0)
                type = AwakeType.None;

            return 1;
        }
        else
        {
            type = AwakeType.None;
            return 0;
        }
    }

    public int getAwakeLevelValue(int i) { return listAwake[i]; }

    public byte getDC() { return (type == AwakeType.DC ? getAwakeValue() : (byte)0); }
    public byte getMC() { return (type == AwakeType.MC ? getAwakeValue() : (byte)0); }
    public byte getSC() { return (type == AwakeType.SC ? getAwakeValue() : (byte)0); }
    public byte getAC() { return (type == AwakeType.AC ? getAwakeValue() : (byte)0); }
    public byte getMAC() { return (type == AwakeType.MAC ? getAwakeValue() : (byte)0); }
    public byte getHPMP() { return (type == AwakeType.HPMP ? getAwakeValue() : (byte)0); }

    private bool[] makeHit(int maxValue, out int makeValue)
    {
        float stepValue = (float)maxValue / 5.0f;
        float totalValue = 0.0f;
        bool[] isHit = new bool[5];
        Random rand = new Random(DateTime.Now.Millisecond);

        for (int i = 0; i < 5; i++)
        {
            if (rand.Next(0, 100) < AwakeHitRate)
            {
                totalValue += stepValue;
                isHit[i] = true;
            }
            else
            {
                isHit[i] = false;
            }
        }

        makeValue = totalValue <= 1.0f ? 1 : (int)totalValue;
        return isHit;
    }

    private bool[] Awakening(UserItem item)
    {
        int minValue = AwakeChanceMin;
        int maxValue = (AwakeChanceMax[(int)item.Info.Grade - 1] < AwakeChanceMin) ? AwakeChanceMin : AwakeChanceMax[(int)item.Info.Grade - 1];

        int result;

        bool[] returnValue = makeHit(maxValue, out result);

        switch (item.Info.Type)
        {
            case ItemType.Weapon:
                result *= (int)Awake_WeaponRate;
                break;
            case ItemType.Armour:
                result *= (int)Awake_ArmorRate;
                break;
            case ItemType.Helmet:
                result *= (int)Awake_HelmetRate;
                break;
            default:
                result = 0;
                break;
        }

        listAwake.Add((byte)result);

        return returnValue;
    }
}

public class ClientMagic
{
    public string Name;//加入一个名字
    public Spell Spell;
    //基础花费，等级花费
    public byte BaseCost, LevelCost, Icon;
    public byte Level1, Level2, Level3;
    public ushort Need1, Need2, Need3;

    public byte Level, Key, Range;
    public ushort Experience;

    public bool IsTempSpell;
    //释放时间，延期
    public long CastTime, Delay;

    public ClientMagic()
    {
    }

    public ClientMagic(BinaryReader reader)
    {
        Spell = (Spell)reader.ReadByte();
        Name = reader.ReadString();
        BaseCost = reader.ReadByte();
        LevelCost = reader.ReadByte();
        Icon = reader.ReadByte();
        Level1 = reader.ReadByte();
        Level2 = reader.ReadByte();
        Level3 = reader.ReadByte();
        Need1 = reader.ReadUInt16();
        Need2 = reader.ReadUInt16();
        Need3 = reader.ReadUInt16();

        Level = reader.ReadByte();
        Key = reader.ReadByte();
        Experience = reader.ReadUInt16();

        Delay = reader.ReadInt64();

        Range = reader.ReadByte();
        CastTime = reader.ReadInt64();
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)Spell);
        writer.Write(Name);
        writer.Write(BaseCost);
        writer.Write(LevelCost);
        writer.Write(Icon);
        writer.Write(Level1);
        writer.Write(Level2);
        writer.Write(Level3);
        writer.Write(Need1);
        writer.Write(Need2);
        writer.Write(Need3);

        writer.Write(Level);
        writer.Write(Key);
        writer.Write(Experience);

        writer.Write(Delay);

        writer.Write(Range);
        writer.Write(CastTime);
    }
   
}
//寄卖物品
public class ClientAuction
{
    public ulong AuctionID;
    public UserItem Item;
    public string Seller = string.Empty;//卖家
    public uint GoldPrice = 0;//金币价格，如果金币价格是0，则只能元宝购买
    //public uint Price;
    //这里要加个元宝价格
    public uint CreditPrice = 0;//元宝价格
   
    public DateTime ConsignmentDate;

    //添加以下2个字段
    //过期？已卖？
    public bool Expired, Sold;



    public ClientAuction()
    {
        
    }
    public ClientAuction(BinaryReader reader)
    {
        AuctionID = reader.ReadUInt64();
        Item = new UserItem(reader);
        Seller = reader.ReadString();

        GoldPrice = reader.ReadUInt32();
        CreditPrice = reader.ReadUInt32();
        ConsignmentDate = DateTime.FromBinary(reader.ReadInt64());
        Expired = reader.ReadBoolean();
        Sold = reader.ReadBoolean();
    }
    public void Save(BinaryWriter writer)
    {
        writer.Write(AuctionID);
        Item.Save(writer);
        writer.Write(Seller);
        writer.Write(GoldPrice);
        writer.Write(CreditPrice);
        writer.Write(ConsignmentDate.ToBinary());
        writer.Write(Expired);
        writer.Write(Sold);
    }
}

//客户端的任务信息
public class ClientQuestInfo
{
    public int Index;

    public uint NPCIndex;

    public string Name, Group;
    public List<string> Description = new List<string>();
    public List<string> TaskDescription = new List<string>();
    public List<string> CompletionDescription = new List<string>(); 

    public int MinLevelNeeded, MaxLevelNeeded;
    public int QuestNeeded;
    public RequiredClass ClassNeeded;

    public QuestType Type;

    public uint RewardGold;
    public uint RewardExp;
    public uint RewardCredit;
    public List<QuestItemReward> RewardsFixedItem = new List<QuestItemReward>();
    public List<QuestItemReward> RewardsSelectItem = new List<QuestItemReward>();

    public uint FinishNPCIndex;

    public bool SameFinishNPC
    {
        get { return NPCIndex == FinishNPCIndex; }
    }

    public ClientQuestInfo() { }

    public ClientQuestInfo(BinaryReader reader)
    {
        Index = reader.ReadInt32();
        NPCIndex = reader.ReadUInt32();
        Name = reader.ReadString();
        Group = reader.ReadString();

        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
            Description.Add(reader.ReadString());

        count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
            TaskDescription.Add(reader.ReadString());

        count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
            CompletionDescription.Add(reader.ReadString());

        MinLevelNeeded = reader.ReadInt32();
        MaxLevelNeeded = reader.ReadInt32();
        QuestNeeded = reader.ReadInt32();
        ClassNeeded = (RequiredClass)reader.ReadByte();
        Type = (QuestType)reader.ReadByte();
        RewardGold = reader.ReadUInt32();
        RewardExp = reader.ReadUInt32();
        RewardCredit = reader.ReadUInt32();

        count = reader.ReadInt32();

        for (int i = 0; i < count; i++ )
            RewardsFixedItem.Add(new QuestItemReward(reader));

        count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
            RewardsSelectItem.Add(new QuestItemReward(reader));

        FinishNPCIndex = reader.ReadUInt32();
    }
    public void Save(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(NPCIndex);
        writer.Write(Name);
        writer.Write(Group);

        writer.Write(Description.Count);
        for (int i = 0; i < Description.Count; i++)
            writer.Write(Description[i]);

        writer.Write(TaskDescription.Count);
        for (int i = 0; i < TaskDescription.Count; i++)
            writer.Write(TaskDescription[i]);

        writer.Write(CompletionDescription.Count);
        for (int i = 0; i < CompletionDescription.Count; i++)
            writer.Write(CompletionDescription[i]);

        writer.Write(MinLevelNeeded);
        writer.Write(MaxLevelNeeded);
        writer.Write(QuestNeeded);
        writer.Write((byte)ClassNeeded);
        writer.Write((byte)Type);
        writer.Write(RewardGold);
        writer.Write(RewardExp);
        writer.Write(RewardCredit);

        writer.Write(RewardsFixedItem.Count);

        for (int i = 0; i < RewardsFixedItem.Count; i++)
            RewardsFixedItem[i].Save(writer);

        writer.Write(RewardsSelectItem.Count);

        for (int i = 0; i < RewardsSelectItem.Count; i++)
            RewardsSelectItem[i].Save(writer);

        writer.Write(FinishNPCIndex);
    }

    public QuestIcon GetQuestIcon(bool taken = false, bool completed = false)
    {
        QuestIcon icon = QuestIcon.None;

        switch (Type)
        {
            case QuestType.General:
            case QuestType.Repeatable:
                if (completed)
                    icon = QuestIcon.QuestionYellow;
                else if (taken)
                    icon = QuestIcon.QuestionWhite;
                else
                    icon = QuestIcon.ExclamationYellow;
                break;
            case QuestType.Daily:
                if (completed)
                    icon = QuestIcon.QuestionBlue;
                else if (taken)
                    icon = QuestIcon.QuestionWhite;
                else
                    icon = QuestIcon.ExclamationBlue;
                break;
            case QuestType.Story:
                if (completed)
                    icon = QuestIcon.QuestionGreen;
                else if (taken)
                    icon = QuestIcon.QuestionWhite;
                else
                    icon = QuestIcon.ExclamationGreen;
                break;
        }

        return icon;
    }
}

public class ClientQuestProgress
{
    public int Id;

    public ClientQuestInfo QuestInfo;

    public List<string> TaskList = new List<string>();

    public bool Taken;
    public bool Completed;
    public bool New;

    public QuestIcon Icon
    {
        get 
        {
            return QuestInfo.GetQuestIcon(Taken, Completed); 
        }
    }

    public ClientQuestProgress(){ }

    public ClientQuestProgress(BinaryReader reader)
    {
        Id = reader.ReadInt32();

        int count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
            TaskList.Add(reader.ReadString());

        Taken = reader.ReadBoolean();
        Completed = reader.ReadBoolean();
        New = reader.ReadBoolean();
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(Id);

        writer.Write(TaskList.Count);

        for (int i = 0; i < TaskList.Count; i++)
            writer.Write(TaskList[i]);

        writer.Write(Taken);
        writer.Write(Completed);
        writer.Write(New);
    }
}

public class QuestItemReward
{
    public ItemInfo Item;
    public uint Count = 1;

    public QuestItemReward() { }

    public QuestItemReward(BinaryReader reader)
    {
        Item = new ItemInfo(reader);
        Count = reader.ReadUInt32();
    }

    public void Save(BinaryWriter writer)
    {
        Item.Save(writer);
        writer.Write(Count);
    }
}

public class ClientMail
{
    public ulong MailID;
    public string SenderName;
    public string Message;
    public bool Opened, Locked, CanReply, Collected;

    public DateTime DateSent;

    public uint Gold;
    public List<UserItem> Items = new List<UserItem>();

    public ClientMail() { }

    public ClientMail(BinaryReader reader)
    {
        MailID = reader.ReadUInt64();
        SenderName = reader.ReadString();
        Message = reader.ReadString();
        Opened = reader.ReadBoolean();
        Locked = reader.ReadBoolean();
        CanReply = reader.ReadBoolean();
        Collected = reader.ReadBoolean();

        DateSent = DateTime.FromBinary(reader.ReadInt64());

        Gold = reader.ReadUInt32();
        int count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
            Items.Add(new UserItem(reader));
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(MailID);
        writer.Write(SenderName);
        writer.Write(Message);
        writer.Write(Opened);
        writer.Write(Locked);
        writer.Write(CanReply);
        writer.Write(Collected);

        writer.Write(DateSent.ToBinary());

        writer.Write(Gold);
        writer.Write(Items.Count);

        for (int i = 0; i < Items.Count; i++)
            Items[i].Save(writer);
    }
}

public class ClientFriend
{
    public ulong Index;
    public string Name;
    public string Memo = "";
    public bool Blocked;

    public bool Online;

    public ClientFriend() { }

    public ClientFriend(BinaryReader reader)
    {
        Index = (ulong)reader.ReadInt64();
        Name = reader.ReadString();
        Memo = reader.ReadString();
        Blocked = reader.ReadBoolean();

        Online = reader.ReadBoolean();
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(Index);
        writer.Write(Name);
        writer.Write(Memo);
        writer.Write(Blocked);

        writer.Write(Online);
    }
}


public enum IntelligentCreaturePickupMode : byte
{
    Automatic = 0,
    SemiAutomatic = 1,
}

public class IntelligentCreatureRules
{
    public int MinimalFullness = 1;

    public bool MousePickupEnabled = false;
    public int MousePickupRange = 0;
    public bool AutoPickupEnabled = false;
    public int AutoPickupRange = 0;
    public bool SemiAutoPickupEnabled = false;
    public int SemiAutoPickupRange = 0;

    public bool CanProduceBlackStone = false;

    public string Info = "";
    public string Info1 = "";
    public string Info2 = "";

    public IntelligentCreatureRules()
    {
    }

    public IntelligentCreatureRules(BinaryReader reader)
    {
        MinimalFullness = reader.ReadInt32();
        MousePickupEnabled = reader.ReadBoolean();
        MousePickupRange = reader.ReadInt32();
        AutoPickupEnabled = reader.ReadBoolean();
        AutoPickupRange = reader.ReadInt32();
        SemiAutoPickupEnabled = reader.ReadBoolean();
        SemiAutoPickupRange = reader.ReadInt32();

        CanProduceBlackStone = reader.ReadBoolean();

        Info = reader.ReadString();
        Info1 = reader.ReadString();
        Info2 = reader.ReadString();
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(MinimalFullness);
        writer.Write(MousePickupEnabled);
        writer.Write(MousePickupRange);
        writer.Write(AutoPickupEnabled);
        writer.Write(AutoPickupRange);
        writer.Write(SemiAutoPickupEnabled);
        writer.Write(SemiAutoPickupRange);

        writer.Write(CanProduceBlackStone);

        writer.Write(Info);
        writer.Write(Info1);
        writer.Write(Info2);
    }
}

public class IntelligentCreatureItemFilter
{
    public bool PetPickupAll = true;
    public bool PetPickupGold = false;
    public bool PetPickupWeapons = false;
    public bool PetPickupArmours = false;
    public bool PetPickupHelmets = false;
    public bool PetPickupBoots = false;
    public bool PetPickupBelts = false;
    public bool PetPickupAccessories = false;
    public bool PetPickupOthers = false;

    public ItemGrade PickupGrade = ItemGrade.None;

    public IntelligentCreatureItemFilter()
    {
    }

    public void SetItemFilter(int idx)
    {
        switch (idx)
        {
            case 0://all items
                PetPickupAll = true;
                PetPickupGold = false;
                PetPickupWeapons = false;
                PetPickupArmours = false;
                PetPickupHelmets = false;
                PetPickupBoots = false;
                PetPickupBelts = false;
                PetPickupAccessories = false;
                PetPickupOthers = false;
                break;
            case 1://gold
                PetPickupAll = false;
                PetPickupGold = !PetPickupGold;
                break;
            case 2://weapons
                PetPickupAll = false;
                PetPickupWeapons = !PetPickupWeapons;
                break;
            case 3://armours
                PetPickupAll = false;
                PetPickupArmours = !PetPickupArmours;
                break;
            case 4://helmets
                PetPickupAll = false;
                PetPickupHelmets = !PetPickupHelmets;
                break;
            case 5://boots
                PetPickupAll = false;
                PetPickupBoots = !PetPickupBoots;
                break;
            case 6://belts
                PetPickupAll = false;
                PetPickupBelts = !PetPickupBelts;
                break;
            case 7://jewelry
                PetPickupAll = false;
                PetPickupAccessories = !PetPickupAccessories;
                break;
            case 8://others
                PetPickupAll = false;
                PetPickupOthers = !PetPickupOthers;
                break;
        }
        if (PetPickupGold && PetPickupWeapons && PetPickupArmours && PetPickupHelmets && PetPickupBoots && PetPickupBelts && PetPickupAccessories && PetPickupOthers)
        {
            PetPickupAll = true;
            PetPickupGold = false;
            PetPickupWeapons = false;
            PetPickupArmours = false;
            PetPickupHelmets = false;
            PetPickupBoots = false;
            PetPickupBelts = false;
            PetPickupAccessories = false;
            PetPickupOthers = false;
        }
        else
            if (!PetPickupGold && !PetPickupWeapons && !PetPickupArmours && !PetPickupHelmets && !PetPickupBoots && !PetPickupBelts && !PetPickupAccessories && !PetPickupOthers)
            {
                PetPickupAll = true;
            }
    }

    public IntelligentCreatureItemFilter(BinaryReader reader)
    {
        PetPickupAll = reader.ReadBoolean();
        PetPickupGold = reader.ReadBoolean();
        PetPickupWeapons = reader.ReadBoolean();
        PetPickupArmours = reader.ReadBoolean();
        PetPickupHelmets = reader.ReadBoolean();
        PetPickupBoots = reader.ReadBoolean();
        PetPickupBelts = reader.ReadBoolean();
        PetPickupAccessories = reader.ReadBoolean();
        PetPickupOthers = reader.ReadBoolean();
        //PickupGrade = (ItemGrade)reader.ReadByte();
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(PetPickupAll);
        writer.Write(PetPickupGold);
        writer.Write(PetPickupWeapons);
        writer.Write(PetPickupArmours);
        writer.Write(PetPickupHelmets);
        writer.Write(PetPickupBoots);
        writer.Write(PetPickupBelts);
        writer.Write(PetPickupAccessories);
        writer.Write(PetPickupOthers);
        //writer.Write((byte)PickupGrade);
    }
}

public class ClientIntelligentCreature
{
    public IntelligentCreatureType PetType;
    public int Icon;

    public string CustomName;
    public int Fullness;
    public int SlotIndex;
    public long ExpireTime;//in days
    public long BlackstoneTime;
    public long MaintainFoodTime;

    public IntelligentCreaturePickupMode petMode = IntelligentCreaturePickupMode.SemiAutomatic;

    public IntelligentCreatureRules CreatureRules;
    public IntelligentCreatureItemFilter Filter;


    public ClientIntelligentCreature()
    {
    }

    public ClientIntelligentCreature(BinaryReader reader)
    {
        PetType = (IntelligentCreatureType)reader.ReadByte();
        Icon = reader.ReadInt32();

        CustomName = reader.ReadString();
        Fullness = reader.ReadInt32();
        SlotIndex = reader.ReadInt32();
        ExpireTime = reader.ReadInt64();
        BlackstoneTime = reader.ReadInt64();

        petMode = (IntelligentCreaturePickupMode)reader.ReadByte();

        CreatureRules = new IntelligentCreatureRules(reader);
        Filter = new IntelligentCreatureItemFilter(reader);
        Filter.PickupGrade = (ItemGrade)reader.ReadByte();
        MaintainFoodTime = reader.ReadInt64();
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)PetType);
        writer.Write(Icon);

        writer.Write(CustomName);
        writer.Write(Fullness);
        writer.Write(SlotIndex);
        writer.Write(ExpireTime);
        writer.Write(BlackstoneTime);

        writer.Write((byte)petMode);

        CreatureRules.Save(writer);
        Filter.Save(writer);
        writer.Write((byte)Filter.PickupGrade);
        writer.Write(MaintainFoodTime);
    }
}

//数据包，这里做一个加密
public abstract class Packet
{
    public static bool IsServer;

    public abstract short Index { get; }

    public static Packet ReceivePacket(byte[] rawBytes, out byte[] extra)
    {
        //残留数据等于当前数据
        extra = rawBytes;
        Packet p;

        if (rawBytes.Length < 4) return null; //| 2Bytes: Packet Size | 2Bytes: Packet ID |

        int length = (rawBytes[1] << 8) + rawBytes[0];

        //如果接受的数据，不完整，则等待下次接受，完整后才处理
        if (length > rawBytes.Length || length < 2) return null;

        using (MemoryStream stream = new MemoryStream(rawBytes, 2, length - 2))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            short id = reader.ReadInt16();
            try
            {
                p = (IsServer ? GetClientPacket(id) : GetServerPacket(id));
                if (p == null) return null;
                p.ReadPacket(reader);
            }
            catch
            {
                Console.WriteLine("数据包异常结束:" + id);
                return null;
                //return new C.Disconnect();
            }
        }
        //扩展字段，多余的数据？多余的数据通过引用返回
        extra = new byte[rawBytes.Length - length];
        Buffer.BlockCopy(rawBytes, length, extra, 0, rawBytes.Length - length);

        return p;
    }

    //获取数据包，加密
    public IEnumerable<byte> GetPacketBytes()
    {
        if (Index < 0) return new byte[0];

        byte[] data;

        using (MemoryStream stream = new MemoryStream())
        {
            stream.SetLength(2);
            stream.Seek(2, SeekOrigin.Begin);
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(Index);
                WritePacket(writer);
                stream.Seek(0, SeekOrigin.Begin);
                writer.Write((short)stream.Length);
                stream.Seek(0, SeekOrigin.Begin);

                data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
            }
        }
        //这里增加数据包的加密处理（根据Index进行加密）

        return data;
    }

    protected abstract void ReadPacket(BinaryReader reader);
    protected abstract void WritePacket(BinaryWriter writer);

    private static Packet GetClientPacket(short index)
    {
        switch (index)
        {
            case (short)ClientPacketIds.ClientVersion:
                return new C.ClientVersion();
            case (short)ClientPacketIds.Disconnect:
                return new C.Disconnect();
            case (short)ClientPacketIds.KeepAlive:
                return new C.KeepAlive();
            case (short)ClientPacketIds.NewAccount:
                return new C.NewAccount();
            case (short)ClientPacketIds.ChangePassword:
                return new C.ChangePassword();
            case (short)ClientPacketIds.Login:
                return new C.Login();
            case (short)ClientPacketIds.NewCharacter:
                return new C.NewCharacter();
            case (short)ClientPacketIds.DeleteCharacter:
                return new C.DeleteCharacter();
            case (short)ClientPacketIds.StartGame:
                return new C.StartGame();
            case (short)ClientPacketIds.LogOut:
                return new C.LogOut();
            case (short)ClientPacketIds.Turn:
                return new C.Turn();
            case (short)ClientPacketIds.Walk:
                return new C.Walk();
            case (short)ClientPacketIds.Run:
                return new C.Run();
            case (short)ClientPacketIds.Chat:
                return new C.Chat();
            case (short)ClientPacketIds.MoveItem:
                return new C.MoveItem();
            case (short)ClientPacketIds.StoreItem:
                return new C.StoreItem();
            case (short)ClientPacketIds.TakeBackItem:
                return new C.TakeBackItem();
            case (short)ClientPacketIds.MergeItem:
                return new C.MergeItem();
            case (short)ClientPacketIds.EquipItem:
                return new C.EquipItem();
            case (short)ClientPacketIds.RemoveItem:
                return new C.RemoveItem();
            case (short)ClientPacketIds.RemoveSlotItem:
                return new C.RemoveSlotItem();
            case (short)ClientPacketIds.SplitItem:
                return new C.SplitItem();
            case (short)ClientPacketIds.UseItem:
                return new C.UseItem();
            case (short)ClientPacketIds.DropItem:
                return new C.DropItem();
            case (short)ClientPacketIds.DepositRefineItem:
                return new C.DepositRefineItem();
            case (short)ClientPacketIds.RetrieveRefineItem:
                return new C.RetrieveRefineItem();
            case (short)ClientPacketIds.RefineCancel:
                return new C.RefineCancel();
            case (short)ClientPacketIds.RefineItem:
                return new C.RefineItem();
            case (short)ClientPacketIds.CheckRefine:
                return new C.CheckRefine();
            case (short)ClientPacketIds.ReplaceWedRing:
                return new C.ReplaceWedRing();
            case (short)ClientPacketIds.DepositTradeItem:
                return new C.DepositTradeItem();
            case (short)ClientPacketIds.RetrieveTradeItem:
                return new C.RetrieveTradeItem();
            case (short)ClientPacketIds.DropGold:
                return new C.DropGold();
            case (short)ClientPacketIds.PickUp:
                return new C.PickUp();
            case (short)ClientPacketIds.Inspect:
                return new C.Inspect();
            case (short)ClientPacketIds.ChangeAMode:
                return new C.ChangeAMode();
            case (short)ClientPacketIds.ChangePMode:
                return new C.ChangePMode();
            case (short)ClientPacketIds.ChangeTrade:
                return new C.ChangeTrade();
            case (short)ClientPacketIds.Attack:
                return new C.Attack();
            case (short)ClientPacketIds.RangeAttack:
                return new C.RangeAttack();
            case (short)ClientPacketIds.Harvest:
                return new C.Harvest();
            case (short)ClientPacketIds.CallNPC:
                return new C.CallNPC();
            case (short)ClientPacketIds.TalkMonsterNPC:
                return new C.TalkMonsterNPC();
            case (short)ClientPacketIds.BuyItem:
                return new C.BuyItem();
            case (short)ClientPacketIds.SellItem:
                return new C.SellItem();
            case (short)ClientPacketIds.CraftItem:
                return new C.CraftItem();
            case (short)ClientPacketIds.RepairItem:
                return new C.RepairItem();
            case (short)ClientPacketIds.BuyItemBack:
                return new C.BuyItemBack();
            case (short)ClientPacketIds.SRepairItem:
                return new C.SRepairItem();
            case (short)ClientPacketIds.MagicKey:
                return new C.MagicKey();
            case (short)ClientPacketIds.Magic:
                return new C.Magic();
            case (short)ClientPacketIds.SwitchGroup:
                return new C.SwitchGroup();
            case (short)ClientPacketIds.AddMember:
                return new C.AddMember();
            case (short)ClientPacketIds.DellMember:
                return new C.DelMember();
            case (short)ClientPacketIds.GroupInvite:
                return new C.GroupInvite();
            case (short)ClientPacketIds.TownRevive:
                return new C.TownRevive();
            case (short)ClientPacketIds.SpellToggle:
                return new C.SpellToggle();
            case (short)ClientPacketIds.ConsignItem:
                return new C.ConsignItem();
            case (short)ClientPacketIds.MarketSearch:
                return new C.MarketSearch();
            case (short)ClientPacketIds.MarketBuy:
                return new C.MarketBuy();
            case (short)ClientPacketIds.MarketGetBack:
                return new C.MarketGetBack();
            case (short)ClientPacketIds.RequestUserName:
                return new C.RequestUserName();
            case (short)ClientPacketIds.RequestChatItem:
                return new C.RequestChatItem();
            case (short)ClientPacketIds.EditGuildMember:
                return new C.EditGuildMember();
            case (short)ClientPacketIds.EditGuildNotice:
                return new C.EditGuildNotice();
            case (short)ClientPacketIds.GuildInvite:
                return new C.GuildInvite();
            case (short)ClientPacketIds.GuildNameReturn:
                return new C.GuildNameReturn();
            case (short)ClientPacketIds.RequestGuildInfo:
                return new C.RequestGuildInfo();
            case (short)ClientPacketIds.GuildStorageGoldChange:
                return new C.GuildStorageGoldChange();
            case (short)ClientPacketIds.GuildStorageItemChange:
                return new C.GuildStorageItemChange();
            case (short)ClientPacketIds.GuildWarReturn:
                return new C.GuildWarReturn();
            case (short)ClientPacketIds.MarriageRequest:
                return new C.MarriageRequest();
            case (short)ClientPacketIds.MarriageReply:
                return new C.MarriageReply();
            case (short)ClientPacketIds.ChangeMarriage:
                return new C.ChangeMarriage();
            case (short)ClientPacketIds.DivorceRequest:
                return new C.DivorceRequest();
            case (short)ClientPacketIds.DivorceReply:
                return new C.DivorceReply();
            case (short)ClientPacketIds.AddMentor:
                return new C.AddMentor();
            case (short)ClientPacketIds.MentorReply:
                return new C.MentorReply();
            case (short)ClientPacketIds.AllowMentor:
                return new C.AllowMentor();
            case (short)ClientPacketIds.CancelMentor:
                return new C.CancelMentor();
            case (short)ClientPacketIds.TradeRequest:
                return new C.TradeRequest();
            case (short)ClientPacketIds.TradeReply:
                return new C.TradeReply();
            case (short)ClientPacketIds.TradeGold:
                return new C.TradeGold();
            case (short)ClientPacketIds.TradeConfirm:
                return new C.TradeConfirm();
            case (short)ClientPacketIds.TradeCancel:
                return new C.TradeCancel();
            case (short)ClientPacketIds.EquipSlotItem:
                return new C.EquipSlotItem();
            case (short)ClientPacketIds.FishingCast:
                return new C.FishingCast();
            case (short)ClientPacketIds.FishingChangeAutocast:
                return new C.FishingChangeAutocast();
            case (short)ClientPacketIds.AcceptQuest:
                return new C.AcceptQuest();
            case (short)ClientPacketIds.FinishQuest:
                return new C.FinishQuest();
            case (short)ClientPacketIds.AbandonQuest:
                return new C.AbandonQuest();
            case (short)ClientPacketIds.ShareQuest:
                return new C.ShareQuest();
            case (short)ClientPacketIds.AcceptReincarnation:
                return new C.AcceptReincarnation();
            case (short)ClientPacketIds.CancelReincarnation:
                return new C.CancelReincarnation();
            case (short)ClientPacketIds.CombineItem:
                return new C.CombineItem();
            case (short)ClientPacketIds.SetConcentration:
                return new C.SetConcentration();
			case (short)ClientPacketIds.AwakeningNeedMaterials:
                return new C.AwakeningNeedMaterials();
            case (short)ClientPacketIds.AwakeningLockedItem:
                return new C.AwakeningLockedItem();
            case (short)ClientPacketIds.Awakening:
                return new C.Awakening();
            case (short)ClientPacketIds.DisassembleItem:
                return new C.DisassembleItem();
            case (short)ClientPacketIds.DowngradeAwakening:
                return new C.DowngradeAwakening();
            case (short)ClientPacketIds.ResetAddedItem:
                return new C.ResetAddedItem();
            case (short)ClientPacketIds.SendMail:
                return new C.SendMail();
            case (short)ClientPacketIds.ReadMail:
                return new C.ReadMail();
            case (short)ClientPacketIds.CollectParcel:
                return new C.CollectParcel();
            case (short)ClientPacketIds.DeleteMail:
                return new C.DeleteMail();
            case (short)ClientPacketIds.LockMail:
                return new C.LockMail();
            case (short)ClientPacketIds.MailLockedItem:
                return new C.MailLockedItem();
            case (short)ClientPacketIds.MailCost:
                return new C.MailCost();
            case (short)ClientPacketIds.UpdateIntelligentCreature://IntelligentCreature
                return new C.UpdateIntelligentCreature();
            case (short)ClientPacketIds.IntelligentCreaturePickup://IntelligentCreature
                return new C.IntelligentCreaturePickup();
            case (short)ClientPacketIds.AddFriend:
                return new C.AddFriend();
            case (short)ClientPacketIds.RemoveFriend:
                return new C.RemoveFriend();
            case (short)ClientPacketIds.RefreshFriends:
                return new C.RefreshFriends();
            case (short)ClientPacketIds.AddMemo:
                return new C.AddMemo();
            case (short)ClientPacketIds.GuildBuffUpdate:
                return new C.GuildBuffUpdate();
            case (short)ClientPacketIds.GameshopBuy:
                return new C.GameshopBuy();
            case (short)ClientPacketIds.NPCConfirmInput:
                return new C.NPCConfirmInput();
            case (short)ClientPacketIds.ReportIssue:
                return new C.ReportIssue();
            case (short)ClientPacketIds.GetRanking:
                return new C.GetRanking();
            case (short)ClientPacketIds.Opendoor:
                return new C.Opendoor();
            case (short)ClientPacketIds.GetRentedItems:
                return new C.GetRentedItems();
            case (short)ClientPacketIds.ItemRentalRequest:
                return new C.ItemRentalRequest();
            case (short)ClientPacketIds.ItemRentalFee:
                return new C.ItemRentalFee();
            case (short)ClientPacketIds.ItemRentalPeriod:
                return new C.ItemRentalPeriod();
            case (short)ClientPacketIds.DepositRentalItem:
                return new C.DepositRentalItem();
            case (short)ClientPacketIds.RetrieveRentalItem:
                return new C.RetrieveRentalItem();
            case (short)ClientPacketIds.CancelItemRental:
                return new C.CancelItemRental();
            case (short)ClientPacketIds.ItemRentalLockFee:
                return new C.ItemRentalLockFee();
            case (short)ClientPacketIds.ItemRentalLockItem:
                return new C.ItemRentalLockItem();
            case (short)ClientPacketIds.ConfirmItemRental:
                return new C.ConfirmItemRental();
            case (short)ClientPacketIds.RefreshUserGold:
                return new C.RefreshUserGold();
            case (short)ClientPacketIds.RechargeCredit:
                return new C.RechargeCredit();
            case (short)ClientPacketIds.RechargeEnd:
                return new C.RechargeEnd();
            case (short)ClientPacketIds.RefreshInventory:
                return new C.RefreshInventory();
            case (short)ClientPacketIds.DepositItemCollect:
                return new C.DepositItemCollect();
            case (short)ClientPacketIds.RetrieveItemCollect:
                return new C.RetrieveItemCollect();
            case (short)ClientPacketIds.ItemCollectCancel:
                return new C.ItemCollectCancel();
            case (short)ClientPacketIds.ConfirmItemCollect:
                return new C.ConfirmItemCollect();
            case (short)ClientPacketIds.MagicParameter:
                return new C.MagicParameter();
            case (short)ClientPacketIds.MyMonsterOperation:
                return new C.MyMonsterOperation();
                
            default:
                return null;
        }

    }
    public static Packet GetServerPacket(short index)
    {
        switch (index)
        {
            case (short)ServerPacketIds.Connected:
                return new S.Connected();
            case (short)ServerPacketIds.ClientVersion:
                return new S.ClientVersion();
            case (short)ServerPacketIds.Disconnect:
                return new S.Disconnect();
            case (short)ServerPacketIds.KeepAlive:
                return new S.KeepAlive();
            case (short)ServerPacketIds.NewAccount:
                return new S.NewAccount();
            case (short)ServerPacketIds.ChangePassword:
                return new S.ChangePassword();
            case (short)ServerPacketIds.ChangePasswordBanned:
                return new S.ChangePasswordBanned();
            case (short)ServerPacketIds.Login:
                return new S.Login();
            case (short)ServerPacketIds.LoginBanned:
                return new S.LoginBanned();
            case (short)ServerPacketIds.LoginSuccess:
                return new S.LoginSuccess();
            case (short)ServerPacketIds.NewCharacter:
                return new S.NewCharacter();
            case (short)ServerPacketIds.NewCharacterSuccess:
                return new S.NewCharacterSuccess();
            case (short)ServerPacketIds.DeleteCharacter:
                return new S.DeleteCharacter();
            case (short)ServerPacketIds.DeleteCharacterSuccess:
                return new S.DeleteCharacterSuccess();
            case (short)ServerPacketIds.StartGame:
                return new S.StartGame();
            case (short)ServerPacketIds.StartGameBanned:
                return new S.StartGameBanned();
            case (short)ServerPacketIds.StartGameDelay:
                return new S.StartGameDelay();       
            case (short)ServerPacketIds.MapInformation:
                return new S.MapInformation();
            case (short)ServerPacketIds.UserInformation:
                return new S.UserInformation();
            case (short)ServerPacketIds.UserLocation:
                return new S.UserLocation();
            case (short)ServerPacketIds.ObjectPlayer:
                return new S.ObjectPlayer();
            case (short)ServerPacketIds.ObjectRemove:
                return new S.ObjectRemove();
            case (short)ServerPacketIds.ObjectTurn:
                return new S.ObjectTurn();
            case (short)ServerPacketIds.ObjectWalk:
                return new S.ObjectWalk();
            case (short)ServerPacketIds.ObjectRun:
                return new S.ObjectRun();
            case (short)ServerPacketIds.Chat:
                return new S.Chat();
            case (short)ServerPacketIds.ObjectChat:
                return new S.ObjectChat();
            case (short)ServerPacketIds.NewItemInfo:
                return new S.NewItemInfo();
            case (short)ServerPacketIds.MoveItem:
                return new S.MoveItem();
            case (short)ServerPacketIds.EquipItem:
                return new S.EquipItem();
            case (short)ServerPacketIds.MergeItem:
                return new S.MergeItem();
            case (short)ServerPacketIds.RemoveItem:
                return new S.RemoveItem();
            case (short)ServerPacketIds.RemoveSlotItem:
                return new S.RemoveSlotItem();
            case (short)ServerPacketIds.TakeBackItem:
                return new S.TakeBackItem();
            case (short)ServerPacketIds.StoreItem:
                return new S.StoreItem();
            case (short)ServerPacketIds.DepositRefineItem:
                return new S.DepositRefineItem();
            case (short)ServerPacketIds.RetrieveRefineItem:
                return new S.RetrieveRefineItem();
            case (short)ServerPacketIds.RefineItem:
                return new S.RefineItem();
            case (short)ServerPacketIds.DepositTradeItem:
                return new S.DepositTradeItem();
            case (short)ServerPacketIds.RetrieveTradeItem:
                return new S.RetrieveTradeItem();
            case (short)ServerPacketIds.SplitItem:
                return new S.SplitItem();
            case (short)ServerPacketIds.SplitItem1:
                return new S.SplitItem1();
            case (short)ServerPacketIds.UseItem:
                return new S.UseItem();
            case (short)ServerPacketIds.DropItem:
                return new S.DropItem();
            case (short)ServerPacketIds.PlayerUpdate:
                return new S.PlayerUpdate();
            case (short)ServerPacketIds.PlayerInspect:
                return new S.PlayerInspect();
            case (short)ServerPacketIds.LogOutSuccess:
                return new S.LogOutSuccess();
            case (short)ServerPacketIds.LogOutFailed:
                return new S.LogOutFailed();
            case (short)ServerPacketIds.TimeOfDay:
                return new S.TimeOfDay();
            case (short)ServerPacketIds.ChangeAMode:
                return new S.ChangeAMode();
            case (short)ServerPacketIds.ChangePMode:
                return new S.ChangePMode();
            case (short)ServerPacketIds.ObjectItem:
                return new S.ObjectItem();
            case (short)ServerPacketIds.ObjectGold:
                return new S.ObjectGold();
            case (short)ServerPacketIds.GainedItem:
                return new S.GainedItem();
            case (short)ServerPacketIds.GainedGold:
                return new S.GainedGold();
            case (short)ServerPacketIds.LoseGold:
                return new S.LoseGold();
            case (short)ServerPacketIds.GainedCredit:
                return new S.GainedCredit();
            case (short)ServerPacketIds.LoseCredit:
                return new S.LoseCredit();
            case (short)ServerPacketIds.ObjectMonster:
                return new S.ObjectMonster();
            case (short)ServerPacketIds.ObjectAttack:
                return new S.ObjectAttack();
            case (short)ServerPacketIds.Struck:
                return new S.Struck();
            case (short)ServerPacketIds.DamageIndicator:
                return new S.DamageIndicator();
            case (short)ServerPacketIds.ObjectStruck:
                return new S.ObjectStruck();
            case (short)ServerPacketIds.DuraChanged:
                return new S.DuraChanged();
            case (short)ServerPacketIds.HealthChanged:
                return new S.HealthChanged();
            case (short)ServerPacketIds.DeleteItem:
                return new S.DeleteItem();
            case (short)ServerPacketIds.Death:
                return new S.Death();
            case (short)ServerPacketIds.ObjectDied:
                return new S.ObjectDied();
            case (short)ServerPacketIds.ColourChanged:
                return new S.ColourChanged();
            case (short)ServerPacketIds.ObjectColourChanged:
                return new S.ObjectColourChanged();
            case (short)ServerPacketIds.ObjectGuildNameChanged:
                return new S.ObjectGuildNameChanged();
            case (short)ServerPacketIds.GainExperience:
                return new S.GainExperience();
            case (short)ServerPacketIds.LevelChanged:
                return new S.LevelChanged();
            case (short)ServerPacketIds.ObjectLeveled:
                return new S.ObjectLeveled();
            case (short)ServerPacketIds.ObjectHarvest:
                return new S.ObjectHarvest();
            case (short)ServerPacketIds.ObjectHarvested:
                return new S.ObjectHarvested();
            case (short)ServerPacketIds.ObjectNpc:
                return new S.ObjectNPC();
            case (short)ServerPacketIds.NPCResponse:
                return new S.NPCResponse();
            case (short)ServerPacketIds.ObjectHide:
                return new S.ObjectHide();
            case (short)ServerPacketIds.ObjectShow:
                return new S.ObjectShow();
            case (short)ServerPacketIds.Poisoned:
                return new S.Poisoned();
            case (short)ServerPacketIds.ObjectPoisoned:
                return new S.ObjectPoisoned();
            case (short)ServerPacketIds.MapChanged:
                return new S.MapChanged();
            case (short)ServerPacketIds.ObjectTeleportOut:
                return new S.ObjectTeleportOut();
            case (short)ServerPacketIds.ObjectTeleportIn:
                return new S.ObjectTeleportIn();
            case (short)ServerPacketIds.TeleportIn:
                return new S.TeleportIn();
            case (short)ServerPacketIds.NPCGoods:
                return new S.NPCGoods();
            case (short)ServerPacketIds.NPCSell:
                return new S.NPCSell();
            case (short)ServerPacketIds.NPCRepair:
                return new S.NPCRepair();
            case (short)ServerPacketIds.NPCSRepair: 
                return new S.NPCSRepair();
            case (short)ServerPacketIds.NPCRefine:
                return new S.NPCRefine();
            case (short)ServerPacketIds.NPCCheckRefine:
                return new S.NPCCheckRefine();
            case (short)ServerPacketIds.NPCCollectRefine:
                return new S.NPCCollectRefine();
            case (short)ServerPacketIds.NPCReplaceWedRing:
                return new S.NPCReplaceWedRing();
            case (short)ServerPacketIds.NPCStorage:
                return new S.NPCStorage();
            case (short)ServerPacketIds.SellItem:
                return new S.SellItem();
            case (short)ServerPacketIds.CraftItem:
                return new S.CraftItem();
            case (short)ServerPacketIds.RepairItem:
                return new S.RepairItem();
            case (short)ServerPacketIds.ItemRepaired:
                return new S.ItemRepaired();
            case (short)ServerPacketIds.NewMagic:
                return new S.NewMagic();
            case (short)ServerPacketIds.MagicLeveled:
                return new S.MagicLeveled();
            case (short)ServerPacketIds.Magic:
                return new S.Magic();
            case (short)ServerPacketIds.MagicDelay:
                return new S.MagicDelay();
            case (short)ServerPacketIds.MagicCast:
                return new S.MagicCast();
            case (short)ServerPacketIds.ObjectMagic:
                return new S.ObjectMagic();
            case (short)ServerPacketIds.ObjectEffect:
                return new S.ObjectEffect();
            case (short)ServerPacketIds.RangeAttack:
                return new S.RangeAttack();
            case (short)ServerPacketIds.Pushed:
                return new S.Pushed();
            case (short)ServerPacketIds.ObjectPushed:
                return new S.ObjectPushed();
            case (short)ServerPacketIds.ObjectName:
                return new S.ObjectName();
            case (short)ServerPacketIds.UserStorage:
                return new S.UserStorage();
            case (short)ServerPacketIds.SwitchGroup:
                return new S.SwitchGroup();
            case (short)ServerPacketIds.DeleteGroup:
                return new S.DeleteGroup();
            case (short)ServerPacketIds.DeleteMember:
                return new S.DeleteMember();
            case (short)ServerPacketIds.GroupInvite:
                return new S.GroupInvite();
            case (short)ServerPacketIds.AddMember:
                return new S.AddMember();
            case (short)ServerPacketIds.Revived:
                return new S.Revived();
            case (short)ServerPacketIds.ObjectRevived:
                return new S.ObjectRevived();
            case (short)ServerPacketIds.SpellToggle:
                return new S.SpellToggle();
            case (short)ServerPacketIds.ObjectHealth:
                return new S.ObjectHealth();
            case (short)ServerPacketIds.MapEffect:
                return new S.MapEffect();
            case (short)ServerPacketIds.ObjectRangeAttack:
                return new S.ObjectRangeAttack();
            case (short)ServerPacketIds.AddBuff:
                return new S.AddBuff();
            case (short)ServerPacketIds.RemoveBuff:
                return new S.RemoveBuff();
            case (short)ServerPacketIds.ObjectHidden:
                return new S.ObjectHidden();
            case (short)ServerPacketIds.RefreshItem:
                return new S.RefreshItem();
            case (short)ServerPacketIds.ObjectSpell:
                return new S.ObjectSpell();
            case (short)ServerPacketIds.UserDash:
                return new S.UserDash();
            case (short)ServerPacketIds.ObjectDash:
                return new S.ObjectDash();
            case (short)ServerPacketIds.UserDashFail:
                return new S.UserDashFail();
            case (short)ServerPacketIds.ObjectDashFail:
                return new S.ObjectDashFail();
            case (short)ServerPacketIds.NPCConsign:
                return new S.NPCConsign();
            case (short)ServerPacketIds.NPCConsignCredit:
                return new S.NPCConsignCredit();
            case (short)ServerPacketIds.NPCConsignDoulbe:
                return new S.NPCConsignDoulbe();
            case (short)ServerPacketIds.NPCMarket:
                return new S.NPCMarket();
            case (short)ServerPacketIds.ObjectMonsterChange:
                return new S.ObjectMonsterChange();
            case (short)ServerPacketIds.BlizzardStopTime:
                return new S.BlizzardStopTime();
            case (short)ServerPacketIds.MyMonstersPackets:
                return new S.MyMonstersPackets();
            case (short)ServerPacketIds.MyMonstersExpUpdate:
                return new S.MyMonstersExpUpdate();
                
            case (short)ServerPacketIds.ConsignItem:
                return new S.ConsignItem();
            case (short)ServerPacketIds.MarketFail:
                return new S.MarketFail();
            case (short)ServerPacketIds.MarketSuccess:
                return new S.MarketSuccess();
            case (short)ServerPacketIds.ObjectSitDown:
                return new S.ObjectSitDown();
            case (short)ServerPacketIds.InTrapRock:
                return new S.InTrapRock();
            case (short)ServerPacketIds.RemoveMagic:
                return new S.RemoveMagic();
            case (short)ServerPacketIds.BaseStatsInfo:
                return new S.BaseStatsInfo();
            case (short)ServerPacketIds.UserName:
                return new S.UserName();
            case (short)ServerPacketIds.ChatItemStats:
                return new S.ChatItemStats();
            case (short)ServerPacketIds.GuildMemberChange:
                return new S.GuildMemberChange();
            case (short)ServerPacketIds.GuildNoticeChange:
                return new S.GuildNoticeChange();
            case (short)ServerPacketIds.GuildStatus:
                return new S.GuildStatus();
            case (short)ServerPacketIds.GuildInvite:
                return new S.GuildInvite();
            case (short)ServerPacketIds.GuildExpGain:
                return new S.GuildExpGain();
            case (short)ServerPacketIds.GuildNameRequest:
                return new S.GuildNameRequest();
            case (short)ServerPacketIds.GuildStorageGoldChange:
                return new S.GuildStorageGoldChange();
            case (short)ServerPacketIds.GuildStorageItemChange:
                return new S.GuildStorageItemChange();
            case (short)ServerPacketIds.GuildStorageList:
                return new S.GuildStorageList();
            case (short)ServerPacketIds.GuildRequestWar:
                return new S.GuildRequestWar();
            case (short)ServerPacketIds.DefaultNPC:
                return new S.DefaultNPC();
            case (short)ServerPacketIds.NPCUpdate:
                return new S.NPCUpdate();
            case (short)ServerPacketIds.NPCImageUpdate:
                return new S.NPCImageUpdate();
            case (short)ServerPacketIds.MarriageRequest:
                return new S.MarriageRequest();
            case (short)ServerPacketIds.DivorceRequest:
                return new S.DivorceRequest();
            case (short)ServerPacketIds.MentorRequest:
                return new S.MentorRequest();
            case (short)ServerPacketIds.TradeRequest:
                return new S.TradeRequest();
            case (short)ServerPacketIds.TradeAccept:
                return new S.TradeAccept();
            case (short)ServerPacketIds.TradeGold:
                return new S.TradeGold();
            case (short)ServerPacketIds.TradeItem:
                return new S.TradeItem();
            case (short)ServerPacketIds.TradeConfirm:
                return new S.TradeConfirm();
            case (short)ServerPacketIds.TradeCancel:
                return new S.TradeCancel();
            case (short)ServerPacketIds.MountUpdate:
                return new S.MountUpdate();
            case (short)ServerPacketIds.TransformUpdate:
                return new S.TransformUpdate();
            case (short)ServerPacketIds.EquipSlotItem:
                return new S.EquipSlotItem();
            case (short)ServerPacketIds.FishingUpdate:
                return new S.FishingUpdate();
            case (short)ServerPacketIds.ChangeQuest:
                return new S.ChangeQuest();
            case (short)ServerPacketIds.CompleteQuest:
                return new S.CompleteQuest();
            case (short)ServerPacketIds.ShareQuest:
                return new S.ShareQuest();
            case (short)ServerPacketIds.NewQuestInfo:
                return new S.NewQuestInfo();
            case (short)ServerPacketIds.GainedQuestItem:
                return new S.GainedQuestItem();
            case (short)ServerPacketIds.DeleteQuestItem:
                return new S.DeleteQuestItem();
            case (short)ServerPacketIds.CancelReincarnation:
                return new S.CancelReincarnation();
            case (short)ServerPacketIds.RequestReincarnation:
                return new S.RequestReincarnation();
            case (short)ServerPacketIds.UserBackStep:
                return new S.UserBackStep();
            case (short)ServerPacketIds.ObjectBackStep:
                return new S.ObjectBackStep();
            case (short)ServerPacketIds.UserDashAttack:
                return new S.UserDashAttack();
            case (short)ServerPacketIds.ObjectDashAttack:
                return new S.ObjectDashAttack();
            case (short)ServerPacketIds.UserAttackMove://Warrior Skill - SlashingBurst
                return new S.UserAttackMove();
            case (short)ServerPacketIds.CombineItem:
                return new S.CombineItem();
            case (short)ServerPacketIds.ItemUpgraded:
                return new S.ItemUpgraded();
            case (short)ServerPacketIds.SetConcentration:
                return new S.SetConcentration();
            case (short)ServerPacketIds.SetObjectConcentration:
                return new S.SetObjectConcentration();
            case (short)ServerPacketIds.SetElemental:
                return new S.SetElemental();
            case (short)ServerPacketIds.SetObjectElemental:
                return new S.SetObjectElemental();
            case (short)ServerPacketIds.RemoveDelayedExplosion:
                return new S.RemoveDelayedExplosion();
            case (short)ServerPacketIds.ObjectDeco:
                return new S.ObjectDeco();
            case (short)ServerPacketIds.ObjectSneaking:
                return new S.ObjectSneaking();
            case (short)ServerPacketIds.ObjectLevelEffects:
                return new S.ObjectLevelEffects();
            case (short)ServerPacketIds.SetBindingShot:
                return new S.SetBindingShot();
            case (short)ServerPacketIds.SendOutputMessage:
                return new S.SendOutputMessage();
			case (short)ServerPacketIds.NPCAwakening:
                return new S.NPCAwakening();
            case (short)ServerPacketIds.NPCDisassemble:
                return new S.NPCDisassemble();
            case (short)ServerPacketIds.NPCDowngrade:
                return new S.NPCDowngrade();
            case (short)ServerPacketIds.NPCReset:
                return new S.NPCReset();
            case (short)ServerPacketIds.AwakeningNeedMaterials:
                return new S.AwakeningNeedMaterials();
            case (short)ServerPacketIds.AwakeningLockedItem:
                return new S.AwakeningLockedItem();
            case (short)ServerPacketIds.Awakening:
                return new S.Awakening();
            case (short)ServerPacketIds.ReceiveMail:
                return new S.ReceiveMail();
            case (short)ServerPacketIds.MailLockedItem:
                return new S.MailLockedItem();
            case (short)ServerPacketIds.MailSent:
                return new S.MailSent();
            case (short)ServerPacketIds.MailSendRequest:
                return new S.MailSendRequest();
            case (short)ServerPacketIds.ParcelCollected:
                return new S.ParcelCollected();
            case (short)ServerPacketIds.MailCost:
                return new S.MailCost();
			case (short)ServerPacketIds.ResizeInventory:
                return new S.ResizeInventory();
            case (short)ServerPacketIds.ResizeStorage:
                return new S.ResizeStorage();
            case (short)ServerPacketIds.NewIntelligentCreature:
                return new S.NewIntelligentCreature();
            case (short)ServerPacketIds.UpdateIntelligentCreatureList:
                return new S.UpdateIntelligentCreatureList();
            case (short)ServerPacketIds.IntelligentCreatureEnableRename:
                return new S.IntelligentCreatureEnableRename();
            case (short)ServerPacketIds.IntelligentCreaturePickup:
                return new S.IntelligentCreaturePickup();
            case (short)ServerPacketIds.NPCPearlGoods:
                return new S.NPCPearlGoods();
            case (short)ServerPacketIds.FriendUpdate:
                return new S.FriendUpdate();
            case (short)ServerPacketIds.LoverUpdate:
                return new S.LoverUpdate();
            case (short)ServerPacketIds.MentorUpdate:
                return new S.MentorUpdate();
            case (short)ServerPacketIds.GuildBuffList:
                return new S.GuildBuffList();
            case (short)ServerPacketIds.GameShopInfo:
                return new S.GameShopInfo();
            case (short)ServerPacketIds.GameShopStock:
                return new S.GameShopStock();
            case (short)ServerPacketIds.NPCRequestInput:
                return new S.NPCRequestInput();
            case (short)ServerPacketIds.Rankings:
                return new S.Rankings();
            case (short)ServerPacketIds.Opendoor:
                return new S.Opendoor();
            case (short)ServerPacketIds.GetRentedItems:
                return new S.GetRentedItems();
            case (short)ServerPacketIds.ItemRentalRequest:
                return new S.ItemRentalRequest();
            case (short)ServerPacketIds.ItemRentalFee:
                return new S.ItemRentalFee();
            case (short)ServerPacketIds.ItemRentalPeriod:
                return new S.ItemRentalPeriod();
            case (short)ServerPacketIds.DepositRentalItem:
                return new S.DepositRentalItem();
            case (short)ServerPacketIds.RetrieveRentalItem:
                return new S.RetrieveRentalItem();
            case (short)ServerPacketIds.UpdateRentalItem:
                return new S.UpdateRentalItem();
            case (short)ServerPacketIds.CancelItemRental:
                return new S.CancelItemRental();
            case (short)ServerPacketIds.ItemRentalLock:
                return new S.ItemRentalLock();
            case (short)ServerPacketIds.ItemRentalPartnerLock:
                return new S.ItemRentalPartnerLock();
            case (short)ServerPacketIds.CanConfirmItemRental:
                return new S.CanConfirmItemRental();
            case (short)ServerPacketIds.ConfirmItemRental:
                return new S.ConfirmItemRental();
            case (short)ServerPacketIds.NewRecipeInfo:
                return new S.NewRecipeInfo();
            case (short)ServerPacketIds.RechargeLink:
                return new S.RechargeLink();
            case (short)ServerPacketIds.RechargeResult:
                return new S.RechargeResult();
            case (short)ServerPacketIds.UserGold:
                return new S.UserGold();
            case (short)ServerPacketIds.UserInventory:
                return new S.UserInventory();
            case (short)ServerPacketIds.NPCItemCollect:
                return new S.NPCItemCollect();
            case (short)ServerPacketIds.RetrieveItemCollect:
                return new S.RetrieveItemCollect();
            case (short)ServerPacketIds.DepositItemCollect:
                return new S.DepositItemCollect();
            case (short)ServerPacketIds.ItemCollectCancel:
                return new S.ItemCollectCancel();
            case (short)ServerPacketIds.ConfirmItemCollect:
                return new S.ConfirmItemCollect();
                
            default:
                return null;
        }
    }
}

//基础属性，各职业的成长属性
public class BaseStats
{
    public float HpGain, HpGainRate, MpGainRate, BagWeightGain, WearWeightGain, HandWeightGain;
    public byte MinAc, MaxAc, MinMac, MaxMac, MinDc, MaxDc, MinMc, MaxMc, MinSc, MaxSc, StartAgility, StartAccuracy, StartCriticalRate, StartCriticalDamage, CritialRateGain, CriticalDamageGain;

    public BaseStats(MirClass Job)
    {
        switch (Job)
        {
            case MirClass.Warrior:
                HpGain = 4F;
                HpGainRate = 4.5F;
                MpGainRate = 0;
                BagWeightGain = 3F;
                WearWeightGain = 20F;
                HandWeightGain = 13F;
                MinAc = 0;
                MaxAc = 7;
                MinMac = 0;
                MaxMac = 0;
                MinDc = 5;
                MaxDc = 5;
                MinMc = 0;
                MaxMc = 0;
                MinSc = 0;
                MaxSc = 0;
                StartAgility = 15;//敏捷
                StartAccuracy = 5;//精确
                StartCriticalRate = 5;
                StartCriticalDamage = 2;
                CritialRateGain = 0;
                CriticalDamageGain = 0;
                break;
            case MirClass.Wizard:
                HpGain = 15F;
                HpGainRate = 1.8F;
                MpGainRate = 0;
                BagWeightGain = 5F;
                WearWeightGain = 100F;
                HandWeightGain = 90F;
                MinAc = 0;
                MaxAc = 0;
                MinMac = 0;
                MaxMac = 0;
                MinDc = 7;
                MaxDc = 7;
                MinMc = 7;
                MaxMc = 7;
                MinSc = 0;
                MaxSc = 0;
                StartAgility = 15;//敏捷
                StartAccuracy = 5;
                StartCriticalRate = 5;
                StartCriticalDamage = 4;
                CritialRateGain = 0;
                CriticalDamageGain = 0;
                break;
            case MirClass.Taoist:
                HpGain = 6F;
                HpGainRate = 2.5F;
                MpGainRate = 0;
                BagWeightGain = 4F;
                WearWeightGain = 50F;
                HandWeightGain = 42F;
                MinAc = 0;
                MaxAc = 0;
                MinMac = 12;
                MaxMac = 6;
                MinDc = 7;
                MaxDc = 7;
                MinMc = 0;
                MaxMc = 0;
                MinSc = 7;
                MaxSc = 7;
                StartAgility = 18;//敏捷
                StartAccuracy = 5;
                StartCriticalRate = 5;
                StartCriticalDamage = 4;
                CritialRateGain = 0;
                CriticalDamageGain = 0;
                break;
            case MirClass.Assassin:
                HpGain = 4F;
                HpGainRate = 3.25F;
                MpGainRate = 0;
                BagWeightGain = 3.5F;
                WearWeightGain = 33F;
                HandWeightGain = 30F;
                MinAc = 0;
                MaxAc = 0;
                MinMac = 0;
                MaxMac = 0;
                MinDc = 8;
                MaxDc = 8;
                MinMc = 0;
                MaxMc = 0;
                MinSc = 0;
                MaxSc = 0;
                StartAgility = 20;//敏捷
                StartAccuracy = 6;//原来是5，调整为6
                StartCriticalRate = 5;
                StartCriticalDamage = 4;
                CritialRateGain = 0;
                CriticalDamageGain = 0;
                break;
            case MirClass.Archer:
                HpGain = 4F;
                HpGainRate = 3.25F;
                MpGainRate = 0;
                BagWeightGain = 4F; //done
                WearWeightGain = 33F;
                HandWeightGain = 30F;
                MinAc = 0;
                MaxAc = 0;
                MinMac = 0;
                MaxMac = 0;
                MinDc = 8;
                MaxDc = 8;
                MinMc = 8;
                MaxMc = 8;
                MinSc = 0;
                MaxSc = 0;
                StartAgility = 15;//敏捷
                StartAccuracy = 11;//原来是8，调整为11
                StartCriticalRate = 5;
                StartCriticalDamage = 4;
                CritialRateGain = 0;
                CriticalDamageGain = 0;
                break;
        }
    }
    public BaseStats(BinaryReader reader)
    {
        HpGain = reader.ReadSingle();
        HpGainRate = reader.ReadSingle();
        MpGainRate = reader.ReadSingle();
        MinAc = reader.ReadByte();
        MaxAc = reader.ReadByte();
        MinMac = reader.ReadByte();
        MaxMac = reader.ReadByte();
        MinDc = reader.ReadByte();
        MaxDc = reader.ReadByte();
        MinMc = reader.ReadByte();
        MaxMc = reader.ReadByte();
        MinSc = reader.ReadByte();
        MaxSc = reader.ReadByte();
        StartAccuracy = reader.ReadByte();
        StartAgility = reader.ReadByte();
        StartCriticalRate = reader.ReadByte();
        StartCriticalDamage = reader.ReadByte();
        CritialRateGain = reader.ReadByte();
        CriticalDamageGain = reader.ReadByte();
        BagWeightGain = reader.ReadSingle();
        WearWeightGain = reader.ReadSingle();
        HandWeightGain = reader.ReadSingle();
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(HpGain);
        writer.Write(HpGainRate);
        writer.Write(MpGainRate);
        writer.Write(MinAc);
        writer.Write(MaxAc);
        writer.Write(MinMac);
        writer.Write(MaxMac);
        writer.Write(MinDc);
        writer.Write(MaxDc);
        writer.Write(MinMc);
        writer.Write(MaxMc);
        writer.Write(MinSc);
        writer.Write(MaxSc);
        writer.Write(StartAccuracy);
        writer.Write(StartAgility);
        writer.Write(StartCriticalRate);
        writer.Write(StartCriticalDamage);
        writer.Write(CritialRateGain);
        writer.Write(CriticalDamageGain);
        writer.Write(BagWeightGain);
        writer.Write(WearWeightGain);
        writer.Write(HandWeightGain);
    }
}
//装备随机加的属性配置
//MaxDcChance:最大几率，1/X,一般是, MaxDcStatChance, MaxDcMaxStat
public class RandomItemStat
{
    public byte MaxDuraChance, MaxDuraStatChance, MaxDuraMaxStat;
    public byte MaxAcChance, MaxAcStatChance, MaxAcMaxStat, MaxMacChance, MaxMacStatChance, MaxMacMaxStat, MaxDcChance, MaxDcStatChance, MaxDcMaxStat, MaxMcChance, MaxMcStatChance, MaxMcMaxStat, MaxScChance, MaxScStatChance, MaxScMaxStat;
    public byte AccuracyChance, AccuracyStatChance, AccuracyMaxStat, AgilityChance, AgilityStatChance, AgilityMaxStat, HpChance, HpStatChance, HpMaxStat, MpChance, MpStatChance, MpMaxStat, StrongChance, StrongStatChance, StrongMaxStat;
    public byte MagicResistChance, MagicResistStatChance, MagicResistMaxStat, PoisonResistChance, PoisonResistStatChance, PoisonResistMaxStat;
    public byte HpRecovChance, HpRecovStatChance, HpRecovMaxStat, MpRecovChance, MpRecovStatChance, MpRecovMaxStat, PoisonRecovChance, PoisonRecovStatChance, PoisonRecovMaxStat;
    public byte CriticalRateChance, CriticalRateStatChance, CriticalRateMaxStat, CriticalDamageChance, CriticalDamageStatChance, CriticalDamageMaxStat;
    public byte FreezeChance, FreezeStatChance, FreezeMaxStat, PoisonAttackChance, PoisonAttackStatChance, PoisonAttackMaxStat;
    public byte AttackSpeedChance, AttackSpeedStatChance, AttackSpeedMaxStat, LuckChance, LuckStatChance, LuckMaxStat;
    public byte CurseChance;

    public RandomItemStat(ItemType Type = ItemType.Book)
    {
        switch (Type)
        {
            case ItemType.Weapon:
                SetWeapon();
                break;
            case ItemType.Armour:
                SetArmour();
                break;
            case ItemType.Helmet:
                SetHelmet();
                break;
            case ItemType.Belt:
            case ItemType.Boots:
                SetBeltBoots();
                break;
            case ItemType.Necklace:
                SetNecklace();
                break;
            case ItemType.Bracelet:
                SetBracelet();
                break;
            case ItemType.Ring:
                SetRing();
                break; 
            case ItemType.Mount:
                SetMount();
                break;
        }
    }

    public void SetWeapon()
    {
        MaxDuraChance = 2;
        MaxDuraStatChance = 13;
        MaxDuraMaxStat = 13;
        
        MaxDcChance = 15;
        MaxDcStatChance = 15;
        MaxDcMaxStat = 13;

        MaxMcChance = 20;
        MaxMcStatChance = 15;
        MaxMcMaxStat = 13;

        MaxScChance = 20;
        MaxScStatChance = 15;
        MaxScMaxStat = 13;

        AttackSpeedChance = 60;
        AttackSpeedStatChance = 30;
        AttackSpeedMaxStat = 3;
        
        StrongChance = 24;
        StrongStatChance = 20;
        StrongMaxStat = 2;

        AccuracyChance = 30;
        AccuracyStatChance = 20;
        AccuracyMaxStat = 2;
    }
    public void SetArmour()
    {
        MaxDuraChance = 2;
        MaxDuraStatChance = 10;
        MaxDuraMaxStat = 3;

        MaxAcChance = 30;
        MaxAcStatChance = 15;
        MaxAcMaxStat = 7;

        MaxMacChance = 30;
        MaxMacStatChance = 15;
        MaxMacMaxStat = 7;

        MaxDcChance = 40;
        MaxDcStatChance = 20;
        MaxDcMaxStat = 7;

        MaxMcChance = 40;
        MaxMcStatChance = 20;
        MaxMcMaxStat = 7;

        MaxScChance = 40;
        MaxScStatChance = 20;
        MaxScMaxStat = 7;

    }
    public void SetHelmet()
    {
        MaxDuraChance = 2;
        MaxDuraStatChance = 10;
        MaxDuraMaxStat = 3;

        MaxAcChance = 30;
        MaxAcStatChance = 15;
        MaxAcMaxStat = 7;

        MaxMacChance = 30;
        MaxMacStatChance = 15;
        MaxMacMaxStat = 7;

        MaxDcChance = 40;
        MaxDcStatChance = 20;
        MaxDcMaxStat = 7;

        MaxMcChance = 40;
        MaxMcStatChance = 20;
        MaxMcMaxStat = 7;

        MaxScChance = 40;
        MaxScStatChance = 20;
        MaxScMaxStat = 7;
    }
    public void SetBeltBoots()
    {
        MaxDuraChance = 2;
        MaxDuraStatChance = 10;
        MaxDuraMaxStat = 3;

        MaxAcChance = 30;
        MaxAcStatChance = 30;
        MaxAcMaxStat = 3;

        MaxMacChance = 30;
        MaxMacStatChance = 30;
        MaxMacMaxStat = 3;

        MaxDcChance = 30;
        MaxDcStatChance = 30;
        MaxDcMaxStat = 3;

        MaxMcChance = 30;
        MaxMcStatChance = 30;
        MaxMcMaxStat = 3;

        MaxScChance = 30;
        MaxScStatChance = 30;
        MaxScMaxStat = 3;

        AgilityChance = 60;
        AgilityStatChance = 30;
        AgilityMaxStat = 3;
    }
    public void SetNecklace()
    {
        MaxDuraChance = 2;
        MaxDuraStatChance = 10;
        MaxDuraMaxStat = 3;

        MaxDcChance = 15;
        MaxDcStatChance = 30;
        MaxDcMaxStat = 7;

        MaxMcChance = 15;
        MaxMcStatChance = 30;
        MaxMcMaxStat = 7;

        MaxScChance = 15;
        MaxScStatChance = 30;
        MaxScMaxStat = 7;

        AccuracyChance = 60;
        AccuracyStatChance = 30;
        AccuracyMaxStat = 7;

        AgilityChance = 60;
        AgilityStatChance = 30;
        AgilityMaxStat = 7;
    }
    public void SetBracelet()
    {
        MaxDuraChance = 2;
        MaxDuraStatChance = 10;
        MaxDuraMaxStat = 3;

        MaxAcChance = 20;
        MaxAcStatChance = 30;
        MaxAcMaxStat = 6;

        MaxMacChance = 20;
        MaxMacStatChance = 30;
        MaxMacMaxStat = 6;

        MaxDcChance = 30;
        MaxDcStatChance = 30;
        MaxDcMaxStat = 6;

        MaxMcChance = 30;
        MaxMcStatChance = 30;
        MaxMcMaxStat = 6;

        MaxScChance = 30;
        MaxScStatChance = 30;
        MaxScMaxStat = 6;
    }
    public void SetRing()
    {
        MaxDuraChance = 2;
        MaxDuraStatChance = 10;
        MaxDuraMaxStat = 3;

        MaxAcChance = 25;
        MaxAcStatChance = 20;
        MaxAcMaxStat = 6;

        MaxMacChance = 25;
        MaxMacStatChance = 20;
        MaxMacMaxStat = 6;

        MaxDcChance = 15;
        MaxDcStatChance = 30;
        MaxDcMaxStat = 6;

        MaxMcChance = 15;
        MaxMcStatChance = 30;
        MaxMcMaxStat = 6;

        MaxScChance = 15;
        MaxScStatChance = 30;
        MaxScMaxStat = 6;
    }

    public void SetMount()
    {
        SetRing();
    }
}

public class ChatItem
{
    public long RecievedTick = 0;
    public ulong ID = 0;
    public UserItem ItemStats;
}

public class UserId
{
    public ulong Id = 0;
    public string UserName = "";
}

#region ItemSets

/// <summary>
/// 物品的集合,打包么,比如药水
/// </summary>
public class ItemSets
{
    public ItemSet Set;
    public List<ItemType> Type;
    //这个是套装最少需要的数量
    private byte Amount
    {
        get
        {
            switch (Set)
            {
                case ItemSet.Mundane:
                case ItemSet.NokChi:
                case ItemSet.TaoProtect:
                case ItemSet.Whisker1:
                case ItemSet.Whisker2:
                case ItemSet.Whisker3:
                case ItemSet.Whisker4:
                case ItemSet.Whisker5:
                case ItemSet.GaleWind:
                    return 2;
                case ItemSet.RedOrchid:
                case ItemSet.RedFlower:
                case ItemSet.Smash:
                case ItemSet.HwanDevil:
                case ItemSet.Purity:
                case ItemSet.FiveString:
                case ItemSet.Bone:
                case ItemSet.Bug:
                case ItemSet.Monitor:
                case ItemSet.Oppressive:
                case ItemSet.Paeok:
                case ItemSet.Sulgwan:
                case ItemSet.Hyeolryong:
                    return 3;
                case ItemSet.Recall:
                    return 4;
                case ItemSet.Spirit:
                case ItemSet.WhiteGold:
                case ItemSet.WhiteGoldH:
                case ItemSet.RedJade:
                case ItemSet.RedJadeH:
                case ItemSet.Nephrite:
                case ItemSet.NephriteH:
                    return 5;
                case ItemSet.DragonWhistle:
                    return 6;
                default:
                    return 2;
            }
        }
    }
    public byte Count;
    public bool SetComplete
    {
        get
        {
            return Count >= Amount;
        }
    }
}

#endregion

//各种矿石的设置
//包括矿石的爆率的设置
//BlackIronOre黑铁矿，GoldOre金矿，CopperOre铜矿，SilverOre银矿
//PlatinumOre铂金矿,RubyOre红宝石，NephriteOre软玉，AmethystOre紫水晶
#region "Mine Related"
public class MineSet
{
    public string Name = string.Empty;
    public byte SpotRegenRate = 5;//矿石再生时间默认5分钟
    public byte MaxStones = 160;//最大产生矿的数量，也就是每5分钟最多产生0-160块矿石，挖完了，就等下一个5分钟了.
    public byte HitRate= 25;//这个是挖到土的几率
    public byte DropRate = 10;//这个是掉的几率

    public List<MineDrop> Drops = new List<MineDrop>();


    public MineSet(byte MineType = 0)
    {
        switch (MineType)
        {
            case 1:

                Drops.Add(new MineDrop(){ItemName = "GoldOre", MinDura = 3, MaxDura = 16,});
                Drops.Add(new MineDrop() { ItemName = "SilverOre", MinDura = 3, MaxDura = 16,  });
                Drops.Add(new MineDrop() { ItemName = "CopperOre",  MinDura = 3, MaxDura = 16,  });
                Drops.Add(new MineDrop() { ItemName = "BlackIronOre", MinDura = 3, MaxDura = 16,});
                break;
            case 2:

                Drops.Add(new MineDrop(){ItemName = "PlatinumOre",  MinDura = 3, MaxDura = 16});
                Drops.Add(new MineDrop() { ItemName = "RubyOre",  MinDura = 3, MaxDura = 16,});
                Drops.Add(new MineDrop() { ItemName = "NephriteOre",  MinDura = 3, MaxDura = 16  });
                Drops.Add(new MineDrop() { ItemName = "AmethystOre",  MinDura = 3, MaxDura = 16 });
                break;
        }
    }

    //爆出其中一种矿
    public MineDrop DropMine()
    {
        int change = 0;
        for(int i=0;i< Drops.Count; i++)
        {
            change += Drops[i].DropRate;
        }
        if (change == 0)
        {
            return null;
        }
        int _change = RandomUtils.Next(change);
        change = 0;
        for (int i = 0; i < Drops.Count; i++)
        {
            change += Drops[i].DropRate;
            if (_change < change)
            {
                return Drops[i];
            }
        }
        return null;
    }


}

public class MineSpot
{
    public byte StonesLeft = 0;
    public long LastRegenTick = 0;
    public MineSet Mine;
}
//挖矿的爆率
public class MineDrop
{
    public string ItemName;
    private ItemInfo _Item;
    public byte MinDura = 1;
    public byte MaxDura = 10;
    public byte DropRate = 10;

    public ItemInfo getItem()
    {
        if (_Item == null)
        {
            _Item = ItemInfo.getItem(ItemName);
        }
        return _Item;
    }
}
//矿区,挖矿区域？
public class MineZone
{
    public byte Mine;
    public Point Location;
    public ushort Size;

    public MineZone()
    {
    }

    public MineZone(BinaryReader reader)
    {
        Location = new Point(reader.ReadInt32(), reader.ReadInt32());
        Size = reader.ReadUInt16();
        Mine = reader.ReadByte();
    }

    //是否在矿区内
    public bool inMineZone(int x,int y)
    {
        if (Math.Abs(x - Location.X) <= Size && Math.Abs(y - Location.Y)<= Size)
        {
            return true;
        }
        return false;
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(Location.X);
        writer.Write(Location.Y);
        writer.Write(Size);
        writer.Write(Mine);
    }
    public override string ToString()
    {
        return string.Format("Mine: {0}- {1}", Functions.PointToString(Location), Mine);
    }
}
#endregion

#region "Guild Related"
public class ItemVolume
{
    public ItemInfo Item;
    public string ItemName;
    public uint Amount;
}
//等级，军阶，行会成员的等级，不同等级的权限不一样
public class Rank
{
    public List<GuildMember> Members = new List<GuildMember>();

    public string Name = "";
    public int Index = 0;
    public RankOptions Options = (RankOptions)0;
    public Rank() 
    {
    }
    public Rank(BinaryReader reader, bool Offline = false)
    {
        Name = reader.ReadString();
        Options = (RankOptions)reader.ReadByte();
        if (!Offline)
            Index = reader.ReadInt32();
        int Membercount = reader.ReadInt32();
        for (int j = 0; j < Membercount; j++)
            Members.Add(new GuildMember(reader, Offline));
    }
    public void Save(BinaryWriter writer, bool Save = false)
    {
        writer.Write(Name);
        writer.Write((byte)Options);
        if (!Save)
            writer.Write(Index);
        writer.Write(Members.Count);
        for (int j = 0; j < Members.Count; j++)
            Members[j].save(writer);
    }
}

public class GuildStorageItem
{
    public UserItem Item;
    public long UserId = 0;
    public GuildStorageItem()
    {
    }
    public GuildStorageItem(BinaryReader reader)
    {
        Item = new UserItem(reader);
        UserId = reader.ReadInt64();
    }
    public void save(BinaryWriter writer)
    {
        Item.Save(writer);
        writer.Write(UserId);
    }
}

public class GuildMember
{
    public string name = "";
    public ulong Id;
    [JsonIgnore]
    public object Player;
    public DateTime LastLogin;
    public bool hasvoted;
    public bool Online;//成员是否在线，这个有问题，显示的是全部在线

    public GuildMember()
    {}
    public GuildMember(BinaryReader reader, bool Offline = false)
    {
        name = reader.ReadString();
        Id = (ulong)reader.ReadInt64();
        LastLogin = DateTime.FromBinary(reader.ReadInt64());
        hasvoted = reader.ReadBoolean();
        Online = reader.ReadBoolean();
        Online = Offline ? false: Online;
    }
    public void save(BinaryWriter writer)
    {
        writer.Write(name);
        writer.Write(Id);
        writer.Write(LastLogin.ToBinary());
        writer.Write(hasvoted);
        writer.Write(Online);
    }
}

[Flags]
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum RankOptions : byte
{
    CanChangeRank = 1,
    CanRecruit = 2,
    CanKick = 4,
    CanStoreItem = 8,
    CanRetrieveItem = 16,
    CanAlterAlliance = 32,
    CanChangeNotice = 64,
    CanActivateBuff = 128
}

public class GuildBuffInfo
{
    public int Id;
    public int Icon = 0;
    public string name = "";
    public byte LevelRequirement;
    public byte PointsRequirement = 1;
    public int TimeLimit;
    public int ActivationCost;
    public byte BuffAc;
    public byte BuffMac;
    public byte BuffDc;
    public byte BuffMc;
    public byte BuffSc;
    public byte BuffAttack;
    public int  BuffMaxHp;
    public int  BuffMaxMp;
    public byte BuffMineRate;
    public byte BuffGemRate;
    public byte BuffFishRate;
    public byte BuffExpRate;
    public byte BuffCraftRate;
    public byte BuffSkillRate;
    public byte BuffHpRegen;
    public byte BuffMPRegen;
    
    public byte BuffDropRate;
    public byte BuffGoldRate;

    public GuildBuffInfo()
    {

    }

    public GuildBuffInfo(BinaryReader reader)
    {
        Id = reader.ReadInt32();
        Icon = reader.ReadInt32();
        name = reader.ReadString();
        LevelRequirement = reader.ReadByte();
        PointsRequirement = reader.ReadByte();
        TimeLimit = reader.ReadInt32();
        ActivationCost = reader.ReadInt32();
        BuffAc = reader.ReadByte();
        BuffMac = reader.ReadByte();
        BuffDc = reader.ReadByte();
        BuffMc = reader.ReadByte();
        BuffSc = reader.ReadByte();
        BuffMaxHp = reader.ReadInt32();
        BuffMaxMp = reader.ReadInt32();
        BuffMineRate = reader.ReadByte();
        BuffGemRate = reader.ReadByte();
        BuffFishRate = reader.ReadByte();
        BuffExpRate = reader.ReadByte();
        BuffCraftRate = reader.ReadByte();
        BuffSkillRate = reader.ReadByte();
        BuffHpRegen = reader.ReadByte();
        BuffMPRegen = reader.ReadByte();
        BuffAttack = reader.ReadByte();
        BuffDropRate = reader.ReadByte();
        BuffGoldRate = reader.ReadByte();
    }

    public GuildBuffInfo(InIReader reader, int i)
    {
        Id  = reader.ReadInt32("Buff-" + i.ToString(), "Id",0);
        Icon = reader.ReadInt32("Buff-" + i.ToString(), "Icon", 0);
        name = reader.ReadString("Buff-" + i.ToString(), "Name","");
        LevelRequirement = reader.ReadByte("Buff-" + i.ToString(), "LevelReq",0);
        PointsRequirement = reader.ReadByte("Buff-" + i.ToString(), "PointsReq",1);
        TimeLimit = reader.ReadInt32("Buff-" + i.ToString(), "TimeLimit",0);;
        ActivationCost = reader.ReadInt32("Buff-" + i.ToString(), "ActivationCost",0);
        BuffAc = reader.ReadByte("Buff-" + i.ToString(), "BuffAc",0);
        BuffMac = reader.ReadByte("Buff-" + i.ToString(), "BuffMAC",0);
        BuffDc = reader.ReadByte("Buff-" + i.ToString(), "BuffDc",0);
        BuffMc = reader.ReadByte("Buff-" + i.ToString(), "BuffMc",0);
        BuffSc = reader.ReadByte("Buff-" + i.ToString(), "BuffSc",0);
        BuffMaxHp = reader.ReadInt32("Buff-" + i.ToString(), "BuffMaxHp",0);
        BuffMaxMp = reader.ReadInt32("Buff-" + i.ToString(), "BuffMaxMp",0);
        BuffMineRate = reader.ReadByte("Buff-" + i.ToString(), "BuffMineRate",0);
        BuffGemRate = reader.ReadByte("Buff-" + i.ToString(), "BuffGemRate",0);
        BuffFishRate = reader.ReadByte("Buff-" + i.ToString(), "BuffFishRate",0);
        BuffExpRate = reader.ReadByte("Buff-" + i.ToString(), "BuffExpRate",0);
        BuffCraftRate = reader.ReadByte("Buff-" + i.ToString(), "BuffCraftRate",0);
        BuffSkillRate = reader.ReadByte("Buff-" + i.ToString(), "BuffSkillRate",0);
        BuffHpRegen = reader.ReadByte("Buff-" + i.ToString(), "BuffHpRegen",0);
        BuffMPRegen = reader.ReadByte("Buff-" + i.ToString(), "BuffMpRegen",0);
        BuffAttack = reader.ReadByte("Buff-" + i.ToString(), "BuffAttack",0);
        BuffDropRate = reader.ReadByte("Buff-" + i.ToString(), "BuffDropRate",0);
        BuffGoldRate = reader.ReadByte("Buff-" + i.ToString(), "BuffGoldRate",0);
    }

    public void Save(InIReader reader, int i)
    {
        reader.Write("Buff-" + i.ToString(), "Id", Id);
        reader.Write("Buff-" + i.ToString(), "Icon", Icon);
        reader.Write("Buff-" + i.ToString(), "Name", name);
        reader.Write("Buff-" + i.ToString(), "LevelReq", LevelRequirement);
        reader.Write("Buff-" + i.ToString(), "PointsReq", PointsRequirement);
        reader.Write("Buff-" + i.ToString(), "TimeLimit", TimeLimit); ;
        reader.Write("Buff-" + i.ToString(), "ActivationCost", ActivationCost); ;
        reader.Write("Buff-" + i.ToString(), "BuffAc", BuffAc); ;
        reader.Write("Buff-" + i.ToString(), "BuffMAC", BuffMac); ;
        reader.Write("Buff-" + i.ToString(), "BuffDc", BuffDc); ;
        reader.Write("Buff-" + i.ToString(), "BuffMc", BuffMc); ;
        reader.Write("Buff-" + i.ToString(), "BuffSc", BuffSc); ;
        reader.Write("Buff-" + i.ToString(), "BuffMaxHp", BuffMaxHp); ;
        reader.Write("Buff-" + i.ToString(), "BuffMaxMp", BuffMaxMp); ;
        reader.Write("Buff-" + i.ToString(), "BuffMineRate", BuffMineRate); ;
        reader.Write("Buff-" + i.ToString(), "BuffGemRate", BuffGemRate); ;
        reader.Write("Buff-" + i.ToString(), "BuffFishRate", BuffFishRate); ;
        reader.Write("Buff-" + i.ToString(), "BuffExpRate", BuffExpRate); ;
        reader.Write("Buff-" + i.ToString(), "BuffCraftRate", BuffCraftRate); ;
        reader.Write("Buff-" + i.ToString(), "BuffSkillRate", BuffSkillRate); ;
        reader.Write("Buff-" + i.ToString(), "BuffHpRegen", BuffHpRegen); ;
        reader.Write("Buff-" + i.ToString(), "BuffMpRegen", BuffMPRegen); ;
        reader.Write("Buff-" + i.ToString(), "BuffAttack", BuffAttack); ;
        reader.Write("Buff-" + i.ToString(), "BuffDropRate", BuffDropRate); ;
        reader.Write("Buff-" + i.ToString(), "BuffGoldRate", BuffGoldRate); ;
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(Id);
        writer.Write(Icon);
        writer.Write(name);
        writer.Write(LevelRequirement);
        writer.Write(PointsRequirement);
        writer.Write(TimeLimit);
        writer.Write(ActivationCost);
        writer.Write(BuffAc);
        writer.Write(BuffMac);
        writer.Write(BuffDc);
        writer.Write(BuffMc);
        writer.Write(BuffSc);
        writer.Write(BuffMaxHp);
        writer.Write(BuffMaxMp);
        writer.Write(BuffMineRate);
        writer.Write(BuffGemRate);
        writer.Write(BuffFishRate);
        writer.Write(BuffExpRate);
        writer.Write(BuffCraftRate);
        writer.Write(BuffSkillRate);
        writer.Write(BuffHpRegen);
        writer.Write(BuffMPRegen);
        writer.Write(BuffAttack);
        writer.Write(BuffDropRate);
        writer.Write(BuffGoldRate);
    }

    public override string ToString()
    {
        return string.Format("{0}: {1}", Id, name);
    }

    public string ShowStats()
    {
        string text = string.Empty;

        //text = name + "\n";
        if (BuffAc > 0)
        {
            text += LanguageUtils.Format("防御提升: 0-{0}.", BuffAc);
            if (text != "") text += "\n";
        }
        if (BuffMac > 0)
        {
            text += LanguageUtils.Format("魔御提升: 0-{0}.", BuffMac);
            if (text != "") text += "\n";
        }
        if (BuffDc > 0)
        {
            text += LanguageUtils.Format("攻击提升: 0-{0}.", BuffDc);
            if (text != "") text += "\n";
        }
        if (BuffMc > 0)
        {
            text += LanguageUtils.Format("魔法提升: 0-{0}.", BuffMc);
            if (text != "") text += "\n";
        }
        if (BuffSc > 0)
        {
            text += LanguageUtils.Format("道术提升: 0-{0}.", BuffSc);
            if (text != "") text += "\n";
        }
        if (BuffMaxHp > 0)
        {
            text += LanguageUtils.Format("HP提升: {0}.", BuffMaxHp);
            if (text != "") text += "\n";
        }
        if (BuffMaxMp > 0)
        {
            text += LanguageUtils.Format("MP提升: {0}.", BuffMaxMp);
            if (text != "") text += "\n";
        }
        if (BuffHpRegen > 0)
        {
            text += LanguageUtils.Format("生命恢复提升: {0}.", BuffHpRegen);
            if (text != "") text += "\n";
        }
        if (BuffMPRegen > 0)
        {
            text += LanguageUtils.Format("法力恢复提升: {0}.", BuffMPRegen);
            if (text != "") text += "\n";
        }
        if (BuffMineRate > 0)
        {
            text += LanguageUtils.Format("挖矿成功率提升: {0}%.", BuffMineRate * 5);
            if (text != "") text += "\n";
        }
        if (BuffGemRate > 0)
        {
            text += LanguageUtils.Format("宝玉成功率提升: {0}%.", BuffGemRate * 5);
            if (text != "") text += "\n";
        }
        if (BuffFishRate > 0)
        {
            text += LanguageUtils.Format("钓鱼成功率提升: {0}%.", BuffFishRate * 5);
            if (text != "") text += "\n";
        }
        if (BuffExpRate > 0)
        {
            text += LanguageUtils.Format("经验提升: {0}%.", BuffExpRate);
            if (text != "") text += "\n";
        }
        if (BuffCraftRate > 0)
        {
            text += LanguageUtils.Format("工艺制作提升: {0}%.", BuffCraftRate * 5);
            if (text != "") text += "\n";
        }
        if (BuffSkillRate > 0)
        {
            text += LanguageUtils.Format("技能修炼提升: {0}.", BuffSkillRate);
            if (text != "") text += "\n";
        }
        if (BuffAttack > 0)
        {
            text += LanguageUtils.Format("伤害提升: {0}.", BuffAttack);
            if (text != "") text += "\n";
        }
        if (BuffDropRate > 0)
        {
            text += LanguageUtils.Format("爆率提升: {0}%.", BuffDropRate);
            if (text != "") text += "\n";
        }
        if (BuffGoldRate > 0)
        {
            text += LanguageUtils.Format("金币获取提升: 0-{0}.", BuffGoldRate);
            if (text != "") text += "\n";
        }


        return text;
    }
}

public class GuildBuff
{
    public int Id;
    public GuildBuffInfo Info;
    public bool Active = false;
    public int ActiveTimeRemaining;

    public bool UsingGuildSkillIcon
    {
        get { return Info != null && Info.Icon < 1000; }
    }

    public GuildBuff()
    {
    }

    public GuildBuff(BinaryReader reader)
    {
        Id = reader.ReadInt32();
        Active = reader.ReadBoolean();
        ActiveTimeRemaining = reader.ReadInt32();
    }
    public void Save(BinaryWriter writer)
    {
        writer.Write(Id);
        writer.Write(Active);
        writer.Write(ActiveTimeRemaining);
    }

    public string PrintTimeSpan(double secs)
    {
        TimeSpan t = TimeSpan.FromMinutes(secs);
        string answer;
        if (t.TotalMinutes < 1.0)
        {
            answer = string.Format("{0}s", t.Seconds);
        }
        else if (t.TotalHours < 1.0)
        {
            answer = string.Format("{0}ms", t.Minutes);
        }
        else // more than 1 hour
        {
            answer = string.Format("{0}h {1:D2}m ", (int)t.TotalHours, t.Minutes);
        }

        return answer;
    }
    
    public string ShowStats()
    {
        if (Info == null) return "";
        return Info.ShowStats();
    }

}

//outdated but cant delete it or old db's wont load
public class GuildBuffOld
{
    public GuildBuffOld()
    { }
    public GuildBuffOld(BinaryReader reader)
    {
        reader.ReadByte();
        reader.ReadInt64();
    }
}

#endregion

#region Ranking Pete107|Petesn00beh 15/1/2016
//等级排名信息？
public class Rank_Character_Info
{
    public ulong PlayerId;
    public ulong CharacterId;//这个是角色ID
    public string Name;
    public MirClass Class;
    public int level;//人榜：等级，地榜：评分，天榜：评分
    public int rank;//排名，不过不传到客户端
    //经验
    public long Experience;//clients shouldnt care about this only server
    public object info;//again only keep this on server!

    public Rank_Character_Info()
    {

    }
    public Rank_Character_Info(BinaryReader reader)
    {
        //rank = reader.ReadInt32();
        PlayerId = (ulong)reader.ReadInt64();
        Name = reader.ReadString();
        level = reader.ReadInt32();
        Class = (MirClass)reader.ReadByte();

    }
    public void Save(BinaryWriter writer)
    {
        //writer.Write(rank);
        writer.Write(PlayerId);
        writer.Write(Name);
        writer.Write(level);
        writer.Write((byte)Class);
    }
}
#endregion
//门,这个服务器端其实不用管？
//不对，这个门不是普通的门，而是某个传送点的开发，关闭时间？
public class Door
{
    public byte index;
    public byte DoorState;//0: closed, 1: opening, 2: open, 3: closing
    public byte ImageIndex;
    public long LastTick;//最后的期限，超过最后期限的5秒就关闭了
    public Point Location;
}

public class ItemRentalInformation
{
    public ulong ItemId;
    public string ItemName;
    public string RentingPlayerName;
    public DateTime ItemReturnDate;

    public ItemRentalInformation()
    { }

    public ItemRentalInformation(BinaryReader reader)
    {
        ItemId = reader.ReadUInt64();
        ItemName = reader.ReadString();
        RentingPlayerName = reader.ReadString();
        ItemReturnDate = DateTime.FromBinary(reader.ReadInt64());
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(ItemId);
        writer.Write(ItemName);
        writer.Write(RentingPlayerName);
        writer.Write(ItemReturnDate.ToBinary());
    }
}

public class ClientRecipeInfo
{
    public UserItem Item;
    public List<UserItem> Ingredients = new List<UserItem>();

    public ClientRecipeInfo()
    {

    }

    public ClientRecipeInfo(BinaryReader reader)
    {
        Item = new UserItem(reader);

        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            Ingredients.Add(new UserItem(reader));
        }
    }

    public void Save(BinaryWriter writer)
    {
        Item.Save(writer);

        writer.Write(Ingredients.Count);
        foreach (var ingredient in Ingredients)
        {
            ingredient.Save(writer);
        }
    }
}

//增加一个安全区，用于客户端在安全区进行穿人
//安全区域
public class SafeZoneInfo
{
    //安全区位置
    public Point Location;
    //大小
    public ushort Size;
    //是否开始点，新生点（服务器才使用）
    public bool StartPoint;
    //地图引用（服务器才使用）
    public int MapIndex;
    //安全区归属的战场分组，分组一样的可以进行治疗
    public WarGroup WGroup;//战场分组，不在同一

    public SafeZoneInfo()
    {

    }

    public SafeZoneInfo(BinaryReader reader)
    {
        Location = new Point(reader.ReadInt32(), reader.ReadInt32());
        Size = reader.ReadUInt16();
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(Location.X);
        writer.Write(Location.Y);
        writer.Write(Size);
    }

    public override string ToString()
    {
        return string.Format("Map: {0}- {1}", Functions.PointToString(Location), StartPoint);
    }
}

//装备自带技能
[Flags]
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum ItemSkill : byte
{
    None = 0,
    Warrior1 = 11,//战
    Warrior2 = 12,//战
    //Warrior3 = 13,//战
    Warrior4 = 14,//战
    Warrior5 = 15,//战
    //Warrior6 = 16,//战
    Warrior7 = 17,//战
    Wizard1 = 21,//法
    Wizard2 = 22,//法
    //Wizard3 = 23,//法
    Wizard4 = 24,//法
    Wizard5 = 25,//法
    //Wizard6 = 26,//法
    Wizard7 = 27,//法
    Taoist1 = 31,//道
    Taoist2 = 32,//道
    //Taoist3 = 33,//道
    Taoist4 = 34,//道
    Taoist5 = 35,//道
    //Taoist6 = 36,//道
    Taoist7 = 37,//道
    Assassin1 = 41,//刺
    Assassin2 = 42,//刺
    //Assassin3 = 43,//刺
    Assassin4 = 44,//刺
    Assassin5 = 45,//刺
    Assassin6 = 46,//刺
    Assassin7 = 47,//刺
    Archer1 = 51,//弓
    Archer2 = 52,//弓
    //Archer3 = 53,//弓
    Archer4 = 54,//弓
    Archer5 = 55,//弓
    //Archer6 = 56,//弓
    Archer7 = 57,//弓
    Comm1 = 1,//通用
    Comm2 = 2,//通用
    //Comm3 = 3,//通用
    Comm4 = 4,//通用
    Comm5 = 5,//通用
    //Comm6 = 6,//通用
    Comm7 = 7,//通用
}

//装备自带技能
public class ItemSkillBean
{
    public ItemSkill skid;//技能ID
    public string skname;//技能名称
    public string skmemo;//技能描述
    public RequiredClass reqcls;//职业
    public byte change;//几率

    public byte level;//级别，阵法的级别 1:初级 2：中级 3：高级 

    public ItemGrade Grade;//品级，只有大于或等于当前品级的才可以刷出来

    public static List<ItemSkillBean> list = new List<ItemSkillBean>();

    public ItemSkillBean(ItemSkill skid, string skname, string skmemo, RequiredClass reqcls, byte change, ItemGrade Grade, byte level)
    {
        this.skid = skid;
        this.skname = skname;
        this.skmemo = skmemo;
        this.reqcls = reqcls;
        this.reqcls = reqcls;
        this.change = change;
        this.Grade = Grade;
        this.level = level;
    }

    public static void init()
    {
        if(list==null|| list.Count == 0)
        {
            list.Add(new ItemSkillBean(ItemSkill.Warrior1, "破山阵", "攻杀几率破防", RequiredClass.Warrior,40, ItemGrade.None,1));
            list.Add(new ItemSkillBean(ItemSkill.Warrior2, "半月阵", "提升半月弯刀,狂风斩的伤害", RequiredClass.Warrior, 40, ItemGrade.None,1));
            list.Add(new ItemSkillBean(ItemSkill.Warrior4, "雷霆阵", "雷霆剑法几率增加伤害", RequiredClass.Warrior,15, ItemGrade.HighLevel,2));
            list.Add(new ItemSkillBean(ItemSkill.Warrior5, "金刚阵", "护身气幕防御", RequiredClass.Warrior,15, ItemGrade.HighLevel,2));
            list.Add(new ItemSkillBean(ItemSkill.Warrior7, "天神阵", "几率免疫攻击，日闪几率增加伤害", RequiredClass.Warrior,5, ItemGrade.Legendary,3));

            list.Add(new ItemSkillBean(ItemSkill.Wizard1, "迷惑阵", "强化诱惑之光，诱惑宝宝成功率提升", RequiredClass.Wizard, 40, ItemGrade.None,1));
            list.Add(new ItemSkillBean(ItemSkill.Wizard2, "天罚阵", "雷电术可同时攻击3个目标", RequiredClass.Wizard, 40, ItemGrade.None,1));
            list.Add(new ItemSkillBean(ItemSkill.Wizard4, "统治阵", "强化诱惑之光，提升成功率,上线宝宝不死亡", RequiredClass.Wizard,15, ItemGrade.HighLevel,2));
            list.Add(new ItemSkillBean(ItemSkill.Wizard5, "分身阵", "强化分身，分身攻击力等于本体攻击力", RequiredClass.Wizard,15, ItemGrade.HighLevel,2));
            list.Add(new ItemSkillBean(ItemSkill.Wizard7, "法神阵", "强化火墙/火雨/冰雨技能,火墙压制怪物回血", RequiredClass.Wizard,5, ItemGrade.Legendary,3));

            list.Add(new ItemSkillBean(ItemSkill.Taoist1, "符咒阵", "感悟火符真谛使得火符威力增加", RequiredClass.Taoist, 40, ItemGrade.None,1));
            list.Add(new ItemSkillBean(ItemSkill.Taoist2, "骷髅阵", "强化骷髅，召唤出强化骷髅为你做战", RequiredClass.Taoist, 40, ItemGrade.None,1));
            list.Add(new ItemSkillBean(ItemSkill.Taoist4, "圣兽阵", "强化神兽，召唤出强化神兽为你做战", RequiredClass.Taoist,15, ItemGrade.HighLevel,2));
            list.Add(new ItemSkillBean(ItemSkill.Taoist5, "厚土阵", "魔，防技能魔防提升", RequiredClass.Taoist,15, ItemGrade.HighLevel,2));
            list.Add(new ItemSkillBean(ItemSkill.Taoist7, "道尊阵", "施毒术/毒云/瘟疫伤害增加", RequiredClass.Taoist,5, ItemGrade.Legendary,3));

            list.Add(new ItemSkillBean(ItemSkill.Assassin1, "月隐阵", "增加月隐术隐身时间", RequiredClass.Assassin, 40, ItemGrade.None,1));
            list.Add(new ItemSkillBean(ItemSkill.Assassin2, "鬼灵阵", "鬼灵步在瞬移过程中对附近目标造成伤害", RequiredClass.Assassin, 40, ItemGrade.None,1));
            list.Add(new ItemSkillBean(ItemSkill.Assassin4, "真气阵", "真气调息-吸蓝效率提升", RequiredClass.Assassin, 15, ItemGrade.HighLevel,2));
            list.Add(new ItemSkillBean(ItemSkill.Assassin5, "幻像阵", "烈火身，几率增加烈火身伤害量", RequiredClass.Assassin, 15, ItemGrade.HighLevel,2));
            list.Add(new ItemSkillBean(ItemSkill.Assassin6, "狂风阵", "增加攻速，同时攻速上限提升", RequiredClass.Assassin, 15, ItemGrade.HighLevel,2));
            list.Add(new ItemSkillBean(ItemSkill.Assassin7, "刺皇阵", "血风触发几率提升，火镰狂舞几率增加伤害", RequiredClass.Assassin, 5, ItemGrade.Legendary,3));

            list.Add(new ItemSkillBean(ItemSkill.Archer1, "烈火阵", "烈火陷阱伤害提升", RequiredClass.Archer, 40, ItemGrade.None,1));
            list.Add(new ItemSkillBean(ItemSkill.Archer2, "爆裂阵", "爆裂箭伤害提升", RequiredClass.Archer, 40, ItemGrade.None,1));
            list.Add(new ItemSkillBean(ItemSkill.Archer4, "痹魔阵", "召唤蛤蟆攻防提升", RequiredClass.Archer, 15, ItemGrade.HighLevel,2));
            list.Add(new ItemSkillBean(ItemSkill.Archer5, "邪魔阵", "邪魔箭伤害提升", RequiredClass.Archer, 15, ItemGrade.HighLevel,2));
            list.Add(new ItemSkillBean(ItemSkill.Archer7, "箭神阵", "连珠箭法/火龙箭法/血龙闪几率增加伤害", RequiredClass.Archer, 5, ItemGrade.Legendary,3));

            list.Add(new ItemSkillBean(ItemSkill.Comm1, "聚灵阵", "回蓝效率提升", RequiredClass.All, 40, ItemGrade.None,1));
            list.Add(new ItemSkillBean(ItemSkill.Comm2, "龙血阵", "回血效率提升", RequiredClass.All, 40, ItemGrade.None,1));
            list.Add(new ItemSkillBean(ItemSkill.Comm4, "噬血阵", "几率吸收周围怪物的血量，回复自身", RequiredClass.All, 15, ItemGrade.HighLevel,2));
            list.Add(new ItemSkillBean(ItemSkill.Comm5, "迷幻阵", "几率使得周围怪物进入迷幻状态", RequiredClass.All, 15, ItemGrade.HighLevel,2));
            list.Add(new ItemSkillBean(ItemSkill.Comm7, "天雷阵", "几率触发雷阵，对身边怪物进行雷阵攻击", RequiredClass.All, 4, ItemGrade.Legendary,3));
        }
    }

    public static ItemSkillBean get(ItemSkill skid)
    {
        init();
        foreach(ItemSkillBean k in list)
        {
            if (k.skid == skid)
            {
                return k;
            }
        }
        return null;
    }

    //刷武器的阵法
    //刷前2阵法
    public static bool RefreshWeaponSkill(UserItem item,MirClass pclass,byte rtype,byte maxlevel)
    {
        init();
        int change = item.spiritual * 10;
        if (change <= 5)
        {
            change = 5;
        }
        //每2层增加1点成功率
        change += item.skCount / 2;
        //失败几率
        if (RandomUtils.Next(90)>= change && rtype==0)
        {
            return false;
        }

        //先擦除原装备阵法,不擦除
        if (rtype == 0)
        {
            //item.sk1 = 0;
            //item.sk2 = 0;
        }
        if (rtype == 1)
        {
            //item.sk1 = 0;
        }
        if (rtype == 2)
        {
            //item.sk2 = 0;
        }
        if (rtype == 3)
        {
            //item.sk3 = 0;
        }
        if (rtype == 4)
        {
            //item.sk4 = 0;
        }

        List<ItemSkillBean> listc = new List<ItemSkillBean>();
        foreach(ItemSkillBean sk in list)
        {
            //级别限制
            if(sk.Grade> item.Info.Grade)
            {
                continue;
            }

            if (sk.level > maxlevel)
            {
                continue;
            }

            //已有的阵法不刷
            if (item.hasItemSk(sk.skid))
            {
                continue;
            }

            //通用阵法
            if(sk.reqcls == RequiredClass.All)
            {
                listc.Add(sk);
                continue;
            }
            //职业阵法
            switch (pclass)
            {
                case MirClass.Warrior:
                    if (sk.reqcls.HasFlag(RequiredClass.Warrior))
                    {
                        listc.Add(sk);
                    }
                    break;
                case MirClass.Wizard:
                    if (sk.reqcls.HasFlag(RequiredClass.Wizard))
                    {
                        listc.Add(sk);
                    }
                    break;
                case MirClass.Taoist:
                    if (sk.reqcls.HasFlag(RequiredClass.Taoist))
                    {
                        listc.Add(sk);
                    }
                    break;
                case MirClass.Assassin:
                    if (sk.reqcls.HasFlag(RequiredClass.Assassin))
                    {
                        listc.Add(sk);
                    }
                    break;
                case MirClass.Archer:
                    if (sk.reqcls.HasFlag(RequiredClass.Archer))
                    {
                        listc.Add(sk);
                    }
                    break;
            }
        }
    
        //最终的阵法放这里存储
        List<ItemSkillBean> listr2 = new List<ItemSkillBean>();
        //随机取一次
        int count = 0;
        foreach (ItemSkillBean sk in listc)
        {
            count += sk.change;
        }
        if (RandomUtils.Next(100) < change)
        {
            int rd = RandomUtils.Next(count);
            count = 0;
            foreach (ItemSkillBean sk in listc)
            {
                count += sk.change;
                if(rd< count)
                {
                    listr2.Add(sk);
                    listc.Remove(sk);
                    break;
                }
            }
        }
        if (rtype == 0)
        {
            //随机取第2次
            count = 0;
            foreach (ItemSkillBean sk in listc)
            {
                count += sk.change;
            }
            if (RandomUtils.Next(100) < change)
            {
                int rd = RandomUtils.Next(count);
                count = 0;
                foreach (ItemSkillBean sk in listc)
                {
                    count += sk.change;
                    if (rd < count)
                    {
                        listr2.Add(sk);
                        break;
                    }
                }
            }
        }
       
        if (listr2.Count > 0)
        {
            //封印层数增加
            item.skCount++;
            if (rtype == 0)
            {
                for (int i = 0; i < listr2.Count; i++)
                {
                    if (i == 0)
                    {
                        item.sk1 = listr2[i].skid;
                    }
                    if (i == 1)
                    {
                        item.sk2 = listr2[i].skid;
                    }
                }
            }
            if (rtype == 1)
            {
                item.sk1 = listr2[0].skid;
            }
            if (rtype == 2)
            {
                item.sk2 = listr2[0].skid;
            }
            if (rtype == 3)
            {
                item.sk3 = listr2[0].skid;
            }
            if (rtype == 4)
            {
                item.sk4 = listr2[0].skid;
            }
            return true;
        }
        else
        {
            return false;
        }
    }
}


//契约兽技能
[Flags]
[Obfuscation(Feature = "renaming", Exclude = true)]
public enum MyMonSkill : byte
{
    None = 0,
    MyMonSK1 = 1,//战
    MyMonSK2 = 2,//战
    MyMonSK3 = 3,//战
    MyMonSK4 = 4,//战
    MyMonSK5 = 5,//战
    MyMonSK6 = 6,//战
    MyMonSK7 = 7,//战
    MyMonSK8 = 8,//战
    MyMonSK9 = 9,//虚空
    MyMonSK11=11,//自爆
}

//契约兽技能
public class MyMonSkillBean
{
    public MyMonSkill skid;//技能ID
    public string skname;//技能名称
    public string skmemo;//技能描述
    public int change;//几率
    public byte MonLevel;//怪物等级要求


    public static List<MyMonSkillBean> list = new List<MyMonSkillBean>();

    public MyMonSkillBean(MyMonSkill skid, string skname, string skmemo, int change,  byte MonLevel)
    {
        this.skid = skid;
        this.skname = skname;
        this.skmemo = skmemo;
        this.change = change;
        this.MonLevel = MonLevel;
    }

    public static void init()
    {
        if (list == null || list.Count == 0)
        {
            list.Add(new MyMonSkillBean(MyMonSkill.MyMonSK1, "护主", "主人受到的伤害部分转接给契约兽", 160, 40));
            list.Add(new MyMonSkillBean(MyMonSkill.MyMonSK2, "金刚", "大幅提升双抗，降低攻击", 320, 40));
            list.Add(new MyMonSkillBean(MyMonSkill.MyMonSK3, "兽血", "血量提升，血量恢复提升", 320, 40));
            list.Add(new MyMonSkillBean(MyMonSkill.MyMonSK11, "自爆", "引爆自身，对身边怪物造成一次性伤害", 160, 40));
            list.Add(new MyMonSkillBean(MyMonSkill.MyMonSK4, "狂暴", "攻速，移速提升", 160, 45));
            list.Add(new MyMonSkillBean(MyMonSkill.MyMonSK5, "矫捷", "敏捷，准确提升", 160, 45));
            list.Add(new MyMonSkillBean(MyMonSkill.MyMonSK6, "吞噬", "吞噬万物，转化为经验反馈给主人", 30, 45));
            list.Add(new MyMonSkillBean(MyMonSkill.MyMonSK7, "天雷", "几率触发天雷对契约兽身旁怪物造成雷电攻击", 20, 50));
            list.Add(new MyMonSkillBean(MyMonSkill.MyMonSK8, "毒雨", "几率触发毒雨对契约兽身旁怪物造成毒雨攻击", 8, 50));
            list.Add(new MyMonSkillBean(MyMonSkill.MyMonSK9, "虚空", "消耗体力，召唤虚空生物，为其战斗", 8, 50));
        }
    }

    public static MyMonSkillBean get(MyMonSkill skid)
    {
        init();
        foreach (MyMonSkillBean k in list)
        {
            if (k.skid == skid)
            {
                return k;
            }
        }
        return null;
    }

    //刷武器的阵法
    //刷前2阵法
    public static MyMonSkillBean RefreshSkill(byte MonLevel, MyMonSkill ExcSkill1, MyMonSkill ExcSkill2,int skCount)
    {
        init();
        skCount = skCount / 2;
        List<MyMonSkillBean> listc = new List<MyMonSkillBean>();
        foreach (MyMonSkillBean sk in list)
        {
            //级别限制
            if (sk.MonLevel > MonLevel)
            {
                continue;
            }
            if(sk.skid == ExcSkill1 || sk.skid==ExcSkill2)
            {
                continue;
            }
            listc.Add(sk);
        }

        //随机取一次
        int count = 0;
        foreach (MyMonSkillBean sk in listc)
        {
            count += sk.change + skCount;
        }
        int rd = RandomUtils.Next(count);
        count = 0;
        foreach (MyMonSkillBean sk in listc)
        {
            count += sk.change + skCount;
            if (rd < count)
            {
                return sk;
            }
        }
        return null;
        
    }
}

/// <summary>
/// 玩家称号处理
/// </summary>
public class PlayerTitleUtil
{

    public static string getPlayerTitleName(PlayerTitle pt)
    {
        switch (pt)
        {
            case PlayerTitle.None:
                return "";
            case PlayerTitle.Title1:
                return "[青铜]";
            case PlayerTitle.Title2:
                return "[白银]";
            case PlayerTitle.Title3:
                return "[黄金]";
            case PlayerTitle.Title4:
                return "[铂金]";
            case PlayerTitle.Title5:
                return "[钻石]";
            case PlayerTitle.Title6:
                return "[王者]";
            default:
                return "";
        }
    }

    public static Color getPlayerTitleColor(PlayerTitle pt)
    {
        switch (pt)
        {
            case PlayerTitle.None:
                return Color.White;
            case PlayerTitle.Title1:
                return Color.LightSkyBlue;//蓝色
            case PlayerTitle.Title2:
                return Color.DeepSkyBlue;//蓝色
            case PlayerTitle.Title3:
                return Color.DarkOrange;//紫
            case PlayerTitle.Title4:
                return Color.Gold;//金
            case PlayerTitle.Title5:
                return Color.LimeGreen;//绿
            case PlayerTitle.Title6:
                return Color.Violet;//粉
            default:
                return Color.White;
        }
    }

    //根据积分，排名等获得相应的职称
    public static PlayerTitle getPlayerTitle(int ranking,int score)
    {
        //只有前20
        if (ranking > 20 || score<100)
        {
            return PlayerTitle.None;
        }
        PlayerTitle ret = PlayerTitle.None;
        //根据积分先排名
        if (score < 300)
        {
            ret = PlayerTitle.Title1;
        }else if(score < 500)
        {
            ret = PlayerTitle.Title2;
        }
        else if (score < 800)
        {
            ret = PlayerTitle.Title3;
        }
        else if (score < 1000)
        {
            ret = PlayerTitle.Title4;
        }
        else if (score < 1300)
        {
            ret = PlayerTitle.Title5;
        }
        else
        {
            ret = PlayerTitle.Title6;
        }
        //最多3个王者
        if (ranking > 3 && ret== PlayerTitle.Title6)
        {
            ret = PlayerTitle.Title5;
        }
        return ret;
    }

}


//针对各种定义的名字进行转换处理
public class NameChange
{
    public static string getRequiredClass(RequiredClass clas)
    {
        switch (clas)
        {
            case RequiredClass.Warrior:
                return "战士";
            case RequiredClass.Wizard:
                return "法师";
            case RequiredClass.Taoist:
                return "道士";
            case RequiredClass.Assassin:
                return "刺客";
            case RequiredClass.Archer:
                return "弓箭手";
            case RequiredClass.WarWizTao:
                return "战法道";
            case RequiredClass.None:
                return "无";
            case RequiredClass.All:
                return "无";
        }
        return "无";
    }

    public static string getItemGrade(ItemGrade clas)
    {
        //List<object> clist = new List<object>();
        switch (clas)
        {
            case ItemGrade.Common:
                return "普通";
            case ItemGrade.HighLevel:
                return "高级";
            case ItemGrade.Rare:
                return "稀有";
            case ItemGrade.Legendary:
                return "传说";
            case ItemGrade.Mythical:
                return "神话";
            case ItemGrade.Ancient:
                return "远古";
            case ItemGrade.Epic:
                return "史诗";
            case ItemGrade.None:
                return "无";
        }
        return "无";
    }
    ////1普通（白色）2高级（蓝DeepSkyBlue/LightSkyBlue）3稀有（绿limegreen/DarkOrange）4传说（紫MediumOrchid） 5神器（金gold） 6史诗（粉Violet/DarkViolet）
    public static Color getItemGradeNameColor(ItemGrade clas)
    {
        switch (clas)
        {
            case ItemGrade.None://白色
                return Color.White;
            case ItemGrade.Common://白色
                return Color.White;
            case ItemGrade.HighLevel:
                return Color.LightSkyBlue;//蓝色
            case ItemGrade.Rare:
                return Color.DeepSkyBlue;//蓝色
            case ItemGrade.Legendary:
                return Color.DarkOrange;//紫
            case ItemGrade.Mythical:
                return Color.Gold;//金
            case ItemGrade.Ancient:
                return Color.LimeGreen;//绿
            case ItemGrade.Epic:
                return Color.Violet;//粉
            default:
                return Color.White;
        }
        //return Color.White;
    }

    public static string getItemType(ItemType clas)
    {
        //List<object> clist = new List<object>();
        switch (clas)
        {
            case ItemType.Nothing:
                return "";
            case ItemType.Weapon:
                return "武器";
            case ItemType.Armour:
                return "盔甲";
            case ItemType.Helmet:
                return "头盔";
            case ItemType.Necklace:
                return "项链";
            case ItemType.Bracelet:
                return "手镯";
            case ItemType.Ring:
                return "戒指";
            case ItemType.Amulet:
                return "毒符";
            case ItemType.Belt:
                return "腰带";
            case ItemType.Boots:
                return "靴子";
            case ItemType.Stone:
                return "宝石";
            case ItemType.Torch:
                return "火把";
            case ItemType.Potion:
                return "药剂";
            case ItemType.Ore:
                return "矿石";
            case ItemType.Meat:
                return "肉";
            case ItemType.CraftingMaterial:
                return "合成材料";
            case ItemType.Scroll:
                return "卷轴";
            case ItemType.Gem:
                return "玉石";
            case ItemType.Mount:
                return "坐骑";
            case ItemType.Book:
                return "书";
            case ItemType.Script:
                return "手稿";
            case ItemType.Reins:
                return "缰绳";
            case ItemType.Bells:
                return "铃铛";
            case ItemType.Saddle:
                return "马鞍";
            case ItemType.Ribbon:
                return "缎带";
            case ItemType.Mask:
                return "面具";
            case ItemType.Food:
                return "食物";
            case ItemType.Hook:
                return "鱼钩";
            case ItemType.Float:
                return "鱼漂";
            case ItemType.Bait:
                return "鱼饵";
            case ItemType.Finder:
                return "探测器";
            case ItemType.Reel:
                return "绕线器";
            case ItemType.Fish:
                return "鱼";
            case ItemType.Quest:
                return "任务物品";
            case ItemType.Awakening:
                return "觉醒物品";
            case ItemType.Pets:
                return "宠物";
            case ItemType.Transform:
                return "时装";
            case ItemType.SkinWeapon:
                return "武器变幻";
            case ItemType.SkinArmour:
                return "衣服变幻";
            case ItemType.MonsterDan:
                return "兽魂丹";
                
        }
        return "";
    }

    public static string getMirClass(MirClass clas)
    {
        //List<object> clist = new List<object>();
        switch (clas)
        {
            case MirClass.Warrior:
                return "战士";
            case MirClass.Wizard:
                return "法师";
            case MirClass.Taoist:
                return "道士";
            case MirClass.Assassin:
                return "刺客";
            case MirClass.Archer:
                return "弓箭手";
            case MirClass.Monk:
                return "武僧";
        }
        return "无";
    }


}


/// <summary>
/// 服务器的配置，这个后面需要从服务器端读取
/// 服务器端和客户端的通用配置哦
/// </summary>
public class ServerConfig
{
    private static readonly InIReader Reader = new InIReader(@".\Configs\ServerClientCommon.ini");

    public static LightType lightType = LightType.Normal;//
    public static RunType runType = RunType.Normal;//
    public static ExitGameType exitGameType = ExitGameType.noWait;
    public static bool NeedRidingMountAtt = false;//是否需要骑马才有属性加成

    public static bool openMaxGem = true;//是否宝石上限

    //版本号，通过版本号传送客户端数据，版本号不同的可以兼容数据传送
    public static int Version = 100;

    public static void Load()
    {
        //General
        openMaxGem = Reader.ReadBoolean("General", "openMaxGem", openMaxGem);


    }
   

}