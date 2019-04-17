using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using Server.MirDatabase;
using Server.MirNetwork;
using Server.MirObjects;
using S = ServerPackets;

namespace Server.MirEnvir
{
    //这个是线程代理吧
    public class MobThread
    {
        public int Id = 0;
        public long LastRunTime = 0;
        public long StartTime = 0;
        public long EndTime = 0;
        public int LastCount = 0;//执行次数
        public LinkedList<MapObject> ObjectsList = new LinkedList<MapObject>();
        public LinkedListNode<MapObject> current = null;
        public Boolean Stop = false;
    }



    public class Envir
    {
        //锁
        public static object AccountLock = new object();
        public static object LoadLock = new object();
        //相关配置
        public const int Version = 77;
        public const int CustomVersion = 0;
        //public const string DatabasePath = @".\Server.MirDB";
        public const string AccountPath = @".\Server.MirADB";
        public const string BackUpPath = @".\Back Up\";
        public bool ResetGS = false;//是否清除游戏日志
        //正则
        private static readonly Regex AccountIDReg, PasswordReg, EMailReg;
        //版本
        public static int LoadVersion;
        public static int LoadCustomVersion;
        //时间计数器
        private readonly DateTime _startTime = DateTime.Now;
        public readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        public long Time { get; private set; }
        public RespawnTimer RespawnTick = new RespawnTimer();
        private static List<string> DisabledCharNames = new List<string>();

        public DateTime Now
        {
            get { return _startTime.AddMilliseconds(Time); }
        }

        public bool Running { get; private set; }

        //对象ID?全局对象ID，自增
        public uint ObjectID
        {
            get { return (uint)UniqueKeyHelper.NextInt(); }
        }
        //玩家数
        public static int _playerCount;
        public int PlayerCount
        {
            get { return Players.Count; }
        }


        //线程
        private Thread _thread;
        private TcpListener _listener;
        //这几个是干嘛的，状态端口？监听状态的么，先关闭试下
        private bool StatusPortEnabled = false;
        public List<MirStatusConnection> StatusConnections = new List<MirStatusConnection>();
        private TcpListener _StatusPort;
        private int _sessionID;
        public List<MirConnection> Connections = new List<MirConnection>();

        //服务器数据
        //Server DB
        //public int MapIndex, ItemIndex, MonsterIndex, NPCIndex, QuestIndex, GameshopIndex, ConquestIndex, RespawnIndex;
        public List<MapInfo> MapInfoList = new List<MapInfo>();
        public List<ItemInfo> ItemInfoList = new List<ItemInfo>();
        public List<MonsterInfo> MonsterInfoList = new List<MonsterInfo>();
        public List<MagicInfo> MagicInfoList = new List<MagicInfo>();
        public List<NPCInfo> NPCInfoList = new List<NPCInfo>();
        public DragonInfo DragonInfo = new DragonInfo();//这个不知道干嘛的，先不用
        public List<QuestInfo> QuestInfoList = new List<QuestInfo>();
        public List<GameShopItem> GameShopList = new List<GameShopItem>();
        public List<RecipeInfo> RecipeInfoList = new List<RecipeInfo>();//食物，目前没用吧
        //这个是服务器的物品的库存
        public Dictionary<int, int> GameshopLog = new Dictionary<int, int>();

        //用户数据,这里的数据ID，全部都使用随机的整数ID，避免后续的合区等数据重复
        //User DB
        //public int NextAccountID, NextCharacterID;
        // public ulong NextUserItemID, NextAuctionID, NextMailID;
        //账号信息
        public List<AccountInfo> AccountList = new List<AccountInfo>();
        //角色信息
        public List<CharacterInfo> CharacterList = new List<CharacterInfo>();

        //public int GuildCount, NextGuildID;
        //行会
        public List<GuildObject> GuildList = new List<GuildObject>();

        //在线信息
        //Live Info
        public List<Map> MapList = new List<Map>();
        public List<SafeZoneInfo> StartPoints = new List<SafeZoneInfo>();
        public List<ItemInfo> StartItems = new List<ItemInfo>();
        //这个是在线的邮件，被接收了之后，就到了角色下了，就不在这里在线邮件列表了
        public List<MailInfo> Mail = new List<MailInfo>();
        //在线玩家
        public List<PlayerObject> Players = new List<PlayerObject>();
        public bool Saving = false;
        public LightSetting Lights;
        public LinkedList<MapObject> Objects = new LinkedList<MapObject>();

        public List<ConquestInfo> ConquestInfos = new List<ConquestInfo>();
        public List<ConquestObject> Conquests = new List<ConquestObject>();


        //多线程参数
        //multithread vars
        readonly object _locker = new object();
        public MobThread[] MobThreads = new MobThread[Settings.ThreadLimit];
        private Thread[] MobThreading = new Thread[Settings.ThreadLimit];
        //自定义命令，通过默认NPC添加
        public List<string> CustomCommands = new List<string>();
        public Dragon DragonSystem;
        //这3个是什么啊？默认NPC,怪物NPC，机器人NPC?
        public NPCObject DefaultNPC;
        public NPCObject MonsterNPC;
        public NPCObject RobotNPC;
        //这几个是不同的爆率？钓鱼几率，觉醒几率，箱子几率，矿石几率？
        public List<DropInfo> FishingDrops = new List<DropInfo>();
        public List<DropInfo> AwakeningDrops = new List<DropInfo>();
        public List<DropInfo> StrongboxDrops = new List<DropInfo>();
        public List<DropInfo> BlackstoneDrops = new List<DropInfo>();
        //行会战争
        public List<GuildAtWar> GuildsAtWar = new List<GuildAtWar>();

        //地图重生(这个是把重生信息保存起来，避免重启的时候，怪物的刷新时间变化了？)
        public List<MapRespawn> SavedSpawns = new List<MapRespawn>();

        //排行榜,排行榜其实定期缓存就行了？10分钟循环计算一次不就行了？靠了
        //只显示TOP10
        //public List<Rank_Character_Info> RankTop = new List<Rank_Character_Info>();
        public List<Rank_Character_Info>[,] RankClass = new List<Rank_Character_Info>[6,3];
        //public int[] RankBottomLevel = new int[6];
        //最高等级
        public int MaxLevel = 0;

        //静态加载一些规则，正则
        static Envir()
        {
            //正则初始化
            AccountIDReg =
                new Regex(@"^[A-Za-z0-9]{" + Globals.MinAccountIDLength + "," + Globals.MaxAccountIDLength + "}$");
            PasswordReg =
                new Regex(@"^[A-Za-z0-9]{" + Globals.MinPasswordLength + "," + Globals.MaxPasswordLength + "}$");
            EMailReg = new Regex(@"\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*");
            
            //过滤字符串？
            string path = Path.Combine(Settings.EnvirPath, "DisabledChars.txt");
            DisabledCharNames.Clear();
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "");
            }
            else
            {
                string[] lines = File.ReadAllLines(path, EncodingType.GetType(path));

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith(";") || string.IsNullOrWhiteSpace(lines[i])) continue;
                    DisabledCharNames.Add(lines[i].ToUpper());
                }
            }
        }

        public static int LastCount = 0, LastRealCount = 0;

        public static long LastRunTime = 0;
        public int MonsterCount;

        private long warTime, mailTime, guildTime, conquestTime, rentalItemsTime;
        private int DailyTime = DateTime.Now.Day;

        private bool MagicExists(Spell spell)
        {
            for (int i = 0; i < MagicInfoList.Count; i++)
            {
                if (MagicInfoList[i].Spell == spell) return true;
            }
            return false;
        }

        //获取魔法信息
        public MagicInfo getMagicInfo(Spell spell)
        {
            for (int i = 0; i < MagicInfoList.Count; i++)
            {
                if (MagicInfoList[i].Spell == spell) return MagicInfoList[i];
            }
            return null;
        }


        //初始化魔法信息（在数据库读取多好啊，靠了）
        private void FillMagicInfoList()
        {
            MagicInfoList = MagicInfo.loadAll();
        }
        //判断是否可以开启服务，判断怪物信息是否存在
        private string CanStartEnvir()
        {
            if (Settings.EnforceDBChecks)
            {
                if (StartPoints.Count == 0) return "Cannot start server without start points";

                if (GetMonsterInfo(Settings.SkeletonName, true) == null) return "Cannot start server without mob: " + Settings.SkeletonName;
                if (GetMonsterInfo(Settings.ShinsuName, true) == null) return "Cannot start server without mob: " + Settings.ShinsuName;
                if (GetMonsterInfo(Settings.BugBatName, true) == null) return "Cannot start server without mob: " + Settings.BugBatName;
                if (GetMonsterInfo(Settings.Zuma1, true) == null) return "Cannot start server without mob: " + Settings.Zuma1;
                if (GetMonsterInfo(Settings.Zuma2, true) == null) return "Cannot start server without mob: " + Settings.Zuma2;
                if (GetMonsterInfo(Settings.Zuma3, true) == null) return "Cannot start server without mob: " + Settings.Zuma3;
                if (GetMonsterInfo(Settings.Zuma4, true) == null) return "Cannot start server without mob: " + Settings.Zuma4;
                if (GetMonsterInfo(Settings.Zuma5, true) == null) return "Cannot start server without mob: " + Settings.Zuma5;
                if (GetMonsterInfo(Settings.Zuma6, true) == null) return "Cannot start server without mob: " + Settings.Zuma6;
                if (GetMonsterInfo(Settings.Zuma7, true) == null) return "Cannot start server without mob: " + Settings.Zuma7;
                //if (GetMonsterInfo(Settings.Turtle1, true) == null) return "Cannot start server without mob: " + Settings.Turtle1;
                //if (GetMonsterInfo(Settings.Turtle2, true) == null) return "Cannot start server without mob: " + Settings.Turtle2;
                //if (GetMonsterInfo(Settings.Turtle3, true) == null) return "Cannot start server without mob: " + Settings.Turtle3;
                //if (GetMonsterInfo(Settings.Turtle4, true) == null) return "Cannot start server without mob: " + Settings.Turtle4;
                //if (GetMonsterInfo(Settings.Turtle5, true) == null) return "Cannot start server without mob: " + Settings.Turtle5;
                if (GetMonsterInfo(Settings.BoneMonster1, true) == null) return "Cannot start server without mob: " + Settings.BoneMonster1;
                if (GetMonsterInfo(Settings.BoneMonster2, true) == null) return "Cannot start server without mob: " + Settings.BoneMonster2;
                if (GetMonsterInfo(Settings.BoneMonster3, true) == null) return "Cannot start server without mob: " + Settings.BoneMonster3;
                if (GetMonsterInfo(Settings.BoneMonster4, true) == null) return "Cannot start server without mob: " + Settings.BoneMonster4;
                //if (GetMonsterInfo(Settings.BehemothMonster1, true) == null) return "Cannot start server without mob: " + Settings.BehemothMonster1;
                //if (GetMonsterInfo(Settings.BehemothMonster2, true) == null) return "Cannot start server without mob: " + Settings.BehemothMonster2;
                //if (GetMonsterInfo(Settings.BehemothMonster3, true) == null) return "Cannot start server without mob: " + Settings.BehemothMonster3;
                //if (GetMonsterInfo(Settings.HellKnight1, true) == null) return "Cannot start server without mob: " + Settings.HellKnight1;
                //if (GetMonsterInfo(Settings.HellKnight2, true) == null) return "Cannot start server without mob: " + Settings.HellKnight2;
                //if (GetMonsterInfo(Settings.HellKnight3, true) == null) return "Cannot start server without mob: " + Settings.HellKnight3;
                //if (GetMonsterInfo(Settings.HellKnight4, true) == null) return "Cannot start server without mob: " + Settings.HellKnight4;
                //if (GetMonsterInfo(Settings.HellBomb1, true) == null) return "Cannot start server without mob: " + Settings.HellBomb1;
                //if (GetMonsterInfo(Settings.HellBomb2, true) == null) return "Cannot start server without mob: " + Settings.HellBomb2;
                //if (GetMonsterInfo(Settings.HellBomb3, true) == null) return "Cannot start server without mob: " + Settings.HellBomb3;
                if (GetMonsterInfo(Settings.WhiteSnake, true) == null) return "Cannot start server without mob: " + Settings.WhiteSnake;
                if (GetMonsterInfo(Settings.AngelName, true) == null) return "Cannot start server without mob: " + Settings.AngelName;
                if (GetMonsterInfo(Settings.BombSpiderName, true) == null) return "Cannot start server without mob: " + Settings.BombSpiderName;
                if (GetMonsterInfo(Settings.CloneName, true) == null) return "Cannot start server without mob: " + Settings.CloneName;
                if (GetMonsterInfo(Settings.AssassinCloneName, true) == null) return "Cannot start server without mob: " + Settings.AssassinCloneName;
                if (GetMonsterInfo(Settings.VampireName, true) == null) return "Cannot start server without mob: " + Settings.VampireName;
                if (GetMonsterInfo(Settings.ToadName, true) == null) return "Cannot start server without mob: " + Settings.ToadName;
                if (GetMonsterInfo(Settings.SnakeTotemName, true) == null) return "Cannot start server without mob: " + Settings.SnakeTotemName;
                if (GetMonsterInfo(Settings.FishingMonster, true) == null) return "Cannot start server without mob: " + Settings.FishingMonster;

                if (ItemInfo.getItem(Settings.RefineOreName) == null) return "Cannot start server without item: " + Settings.RefineOreName;
            }

            //add intelligent creature checks?

            return "true";
        }

  

        //工作线程,此方法采用线程调度
        //只调用一次，通过这个方法开启其他线程循环
        private void WorkLoop()
        {
            try
            {
                Time = Stopwatch.ElapsedMilliseconds;

                long conTime = Time;
                long saveTime = Time + Settings.SaveDelay * Settings.Minute;
                long userTime = Time + Settings.Minute * 5;
                long SpawnTime = Time;
                long processTime = Time + 1000;
                long StartTime = Time;

                int processCount = 0;
                int processRealCount = 0;

                LinkedListNode<MapObject> current = null;

                if (Settings.Multithreaded)
                {
                    for (int j = 0; j < MobThreads.Length; j++)
                    {
                        MobThreads[j] = new MobThread();
                        MobThreads[j].Id = j;
                    }
                }

                StartEnvir();
                string canstartserver = CanStartEnvir();
                if (canstartserver != "true")
                {
                    SMain.Enqueue(canstartserver);
                    StopEnvir();
                    _thread = null;
                    Stop();
                    return;
                }
                //开启线程调用，第0个不管？第0个是在主线程处理的
                if (Settings.Multithreaded)
                {
                    for (int j = 0; j < MobThreads.Length; j++)
                    {
                        MobThread Info = MobThreads[j];
                        if (j > 0) //dont start up 0 
                        {
                            MobThreading[j] = new Thread(() => ThreadLoop(Info));
                            MobThreading[j].IsBackground = true;
                            MobThreading[j].Start();
                        }
                    }
                }
                //开启网络
                StartNetwork();

                try
                {
                    //这里死循环，每秒循环一次？
                    while (Running)
                    {
                        long lasttime = Time;//上次时间
                        Time = Stopwatch.ElapsedMilliseconds;//当前时间
                        long usertime = Time - lasttime;//消耗的时间
                        //20-50毫秒执行一次就可以了。
                        if (usertime < Settings.RefreshDelay)
                        {
                            Thread.Sleep ((int)(Settings.RefreshDelay - usertime));
                        }
                        Time = Stopwatch.ElapsedMilliseconds;//当前时间

                        if (Time >= processTime)
                        {
                            LastCount = processCount;
                            LastRealCount = processRealCount;
                            processCount = 0;
                            processRealCount = 0;
                            processTime = Time + 1000;
                        }

                        //支付处理
                        PayOrder.Process();

                        //网络处理，应该放到单独的网络处理线程里比较好？
                        if (conTime != Time)
                        {
                            conTime = Time;

                            AdjustLights();
                            //这里是对所有连接进行处理
                            lock (Connections)
                            {
                                for (int i = Connections.Count - 1; i >= 0; i--)
                                {
                                    Connections[i].Process();
                                }
                            }
                            //这个是对所有连接状态进行处理
                            lock (StatusConnections)
                            {
                                for (int i = StatusConnections.Count - 1; i >= 0; i--)
                                {
                                    StatusConnections[i].Process();
                                }
                            }
                        }


                        if (current == null)
                            current = Objects.First;

                        if (current == Objects.First)
                        {
                            LastRunTime = Time - StartTime;
                            StartTime = Time;
                        }

                        //多线程进行处理
                        if (Settings.Multithreaded)
                        {
                            //这个是对非主线程的线程进行处理(,如果stop==ture,则再次开启)
                            for (int j = 1; j < MobThreads.Length; j++)
                            {
                                MobThread Info = MobThreads[j];
                                processCount += Info.LastCount;
                                Info.LastCount = 0;
                                if (Info.Stop == true)
                                {
                                    Info.EndTime = Time + 20;
                                    Info.Stop = false;
                                }
                            }
                            //线程锁？
                            lock (_locker)
                            {
                                Monitor.PulseAll(_locker);         // changing a blocking condition. (this makes the threads wake up!)
                            }
                            //run the first loop in the main thread so the main thread automaticaly 'halts' untill the other threads are finished
                            ThreadLoop(MobThreads[0]);
                            processCount += MobThreads[0].LastCount;
                            MobThreads[0].LastCount = 0;
                        }
                        //这个是否放在地图处理里比较好呢(20毫秒内，对所有的MapObject处理一次)
                        Boolean TheEnd = false;
                        long Start = Stopwatch.ElapsedMilliseconds;
                        while ((!TheEnd))
                        {
                            //每10次才取一次时间，避免频繁取时间，消耗CPU
                            if (processCount  % 100 == 0)
                            {
                                if(Stopwatch.ElapsedMilliseconds - Start > 20)
                                {
                                    TheEnd = true;
                                    break;
                                }
                            }
                            
                            if (current == null)
                            {
                                TheEnd = true;
                                break;
                            }
                            else
                            {
                                LinkedListNode<MapObject> next = current.Next;
                                if (!Settings.Multithreaded || ((current.Value.Race != ObjectType.Monster) || (current.Value.Master != null)))
                                {
                                    if (Time > current.Value.OperateTime)
                                    {

                                        current.Value.Process();
                                        current.Value.SetOperateTime();
                                    }
                                    processCount++;
                                }
                                current = next;
                            }
                        }

                        //地图处理
                        for (int i = 0; i < MapList.Count; i++)
                        {
                            MapList[i].Process();
                        }
                            

                        //后面这些都是写无关紧要的处理了

                        if (DragonSystem != null) DragonSystem.Process();

                        Process();

                        //每5分钟保存一次数据(数据的保存，放单独的线程进行处理比较好)
                        if (Time >= saveTime)
                        {
                            saveTime = Time + Settings.SaveDelay * Settings.Minute;
                            SaveAccounts();
                            //SaveGuilds();
                            SaveGoods();
                            SaveConquests();
                        }

                        //每10分钟发送在线人数的广播？
                        if (Time >= userTime)
                        {
                            userTime = Time + Settings.Minute * 10;
                            //小于50个人不发送
                            if (Players.Count > 50)
                            {
                                userTime = Time + Settings.Minute * 5;
                                //Broadcast(new S.Chat{Message = string.Format("当前在线人数: {0}", Players.Count + Players.Count/2),Type = ChatType.Hint});
                            }
                        }

                        //这几个不知道干嘛的，每10秒进行重生处理？
                        if (Time >= SpawnTime)
                        {
                            SpawnTime = Time + (Settings.Second * 10);//technicaly this limits the respawn tick code to a minimum of 10 second each but lets assume it's not meant to be this accurate
                            SMain.Envir.RespawnTick.Process();
                        }

                        //   if (Players.Count == 0) Thread.Sleep(1);
                        //   GC.Collect();


                    }

                }
                catch (Exception ex)
                {
                    SMain.Enqueue(ex);

                    lock (Connections)
                    {
                        for (int i = Connections.Count - 1; i >= 0; i--)
                            Connections[i].SendDisconnect(3);
                    }

                    // Get stack trace for the exception with source file information
                    var st = new StackTrace(ex, true);
                    // Get the top stack frame
                    var frame = st.GetFrame(0);
                    // Get the line number from the stack frame
                    var line = frame.GetFileLineNumber();

                    File.AppendAllText(@".\Error.txt",
                                           string.Format("[{0}] {1} at line {2}{3}", Now, ex, line, Environment.NewLine));
                }
                //结束前要保存数据
                StopNetwork();
                StopEnvir();
                SaveAccounts();
                //SaveGuilds();
                SaveConquests(true);

            }
            catch (Exception ex)
            {
                // Get stack trace for the exception with source file information
                var st = new StackTrace(ex, true);
                // Get the top stack frame
                var frame = st.GetFrame(0);
                // Get the line number from the stack frame
                var line = frame.GetFileLineNumber();

                SMain.Enqueue("[outer workloop error]" + ex);
                File.AppendAllText(@".\Error.txt",
                                       string.Format("[{0}] {1} at line {2}{3}", Now, ex, line, Environment.NewLine));
            }
            _thread = null;

        }
        //线程调用
        private void ThreadLoop(MobThread Info)
        {
            Info.Stop = false;
            long starttime = Time;
            try
            {

                bool stopping = false;
                if (Info.current == null)
                    Info.current = Info.ObjectsList.First;
                stopping = Info.current == null;
                //while (stopping == false)
                while (Running)
                {
                    if (Info.current == null)
                        Info.current = Info.ObjectsList.First;
                    else
                    {
                        LinkedListNode<MapObject> next = Info.current.Next;
                        LastCount++;
                        //if we reach the end of our list > go back to the top (since we are running threaded, we dont want the system to sit there for xxms doing nothing)
                        if (Info.current == Info.ObjectsList.Last)
                        {
                            next = Info.ObjectsList.First;
                            Info.LastRunTime = (Info.LastRunTime + (Time - Info.StartTime)) / 2;
                            //Info.LastRunTime = (Time - Info.StartTime) /*> 0 ? (Time - Info.StartTime) : Info.LastRunTime */;
                            Info.StartTime = Time;
                        }
                        if (Time > Info.current.Value.OperateTime)
                        {
                            if (Info.current.Value.Master == null)//since we are running multithreaded, dont allow pets to be processed (unless you constantly move pets into their map appropriate thead)
                            {
                                Info.current.Value.Process();
                                Info.current.Value.SetOperateTime();
                            }
                        }
                        Info.current = next;
                    }
                    //if it's the main thread > make it loop till the subthreads are done, else make it stop after 'endtime'
                    if (Info.Id == 0)//主线程,只要有一个非主线程完成了，他就退出。
                    {
                        stopping = true;
                        for (int x = 1; x < MobThreads.Length; x++)
                            if (MobThreads[x].Stop == false)
                                stopping = false;
                        if (stopping)
                        {
                            Info.Stop = stopping;
                            return;
                        }
                    }
                    else//非主线程，只要时间到了，就释放锁，让主线程继续执行
                    {
                        if (LastCount % 100 == 0)
                        {
                            if ((Stopwatch.ElapsedMilliseconds > Info.EndTime) && Running)
                            {
                                Info.Stop = true;
                                lock (_locker)
                                {
                                    while (Info.Stop) Monitor.Wait(_locker);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is ThreadInterruptedException) return;
                SMain.Enqueue(ex);

                File.AppendAllText(@".\Error.txt",
                                       string.Format("[{0}] {1}{2}", Now, ex, Environment.NewLine));
            }
            //Info.Stop = true;
        }
        //灯光控制，白天，黑夜控制，这个改掉，总返回白天吧
        private void AdjustLights()
        {
            LightSetting oldLights = Lights;

            int hours = (Now.Hour * 2) % 24;
            if (hours == 6 || hours == 7)
                Lights = LightSetting.Dawn;
            else if (hours >= 8 && hours <= 15)
                Lights = LightSetting.Day;
            else if (hours == 16 || hours == 17)
                Lights = LightSetting.Evening;
            else
                Lights = LightSetting.Night;

            //这个总是设置为白天
            if (ServerConfig.lightType != LightType.none)
            {
                Lights = LightSetting.Day;
            }
            if (oldLights == Lights) return;

            Broadcast(new S.TimeOfDay { Lights = Lights });
        }

        public void Process()
        {
            //if we get to a new day : reset daily's
            if (Now.Day != DailyTime)
            {
                DailyTime = Now.Day;
                ProcessNewDay();
            }
            //每分钟检测行会战是否结束
            if (Time >= warTime)
            {
                for (int i = GuildsAtWar.Count - 1; i >= 0; i--)
                {
                    GuildsAtWar[i].TimeRemaining -= Settings.Minute;

                    if (GuildsAtWar[i].TimeRemaining < 0)
                    {
                        GuildsAtWar[i].EndWar();
                        GuildsAtWar.RemoveAt(i);
                    }
                }

                warTime = Time + Settings.Minute;
            }
            //每分钟检测邮件
            if (Time >= mailTime)
            {
                for (int i = Mail.Count - 1; i >= 0; i--)
                {
                    MailInfo mail = Mail[i];

                    if (mail.Receive())
                    {
                        //collected mail ok
                    }
                }

                mailTime = Time + (Settings.Minute * 1);
            }
            //每分钟检测行会信息
            if (Time >= guildTime)
            {
                guildTime = Time + (Settings.Minute);
                for (int i = 0; i < GuildList.Count; i++)
                {
                    GuildList[i].Process();
                }
            }
            //每分钟检测是否占领？行会攻城，占领10秒？
            if (Time >= conquestTime)
            {
                conquestTime = Time + (Settings.Second * 10);
                for (int i = 0; i < Conquests.Count; i++)
                    Conquests[i].Process();
            }
            //每5分钟执行的都放这里吧
            if (Time >= rentalItemsTime)
            {
                rentalItemsTime = Time + Settings.Minute * 5;
                ProcessRentedItems();
                //这里放下刷新排行榜
                LoadRank();
                CMDTransform.reLoad();
            }

        }

        public void Broadcast(Packet p)
        {
            for (int i = 0; i < Players.Count; i++) Players[i].Enqueue(p);
        }

        public void RequiresBaseStatUpdate()
        {
            for (int i = 0; i < Players.Count; i++) Players[i].HasUpdatedBaseStats = false;
        }

        public void SaveDB()
        {
            Thread t = new Thread(() => SaveDBThread());
            t.IsBackground = true;
            t.Start();
        }
        public void SaveDBThread()
        {
            try
            {
                SMain.Enqueue("SaveDBThread start");
                MirConfigDB.BeginTransaction();
                //地图数据
                for (int i = 0; i < MapInfoList.Count; i++)
                {
                    MapInfoList[i].SaveDB();
                }
                //物品数据
                for (int i = 0; i < ItemInfoList.Count; i++)
                {
                    ItemInfoList[i].SaveDB();
                }
                //怪物数据
                for (int i = 0; i < MonsterInfoList.Count; i++)
                {
                    MonsterInfoList[i].SaveDB();
                }
                //NPC数据
                for (int i = 0; i < NPCInfoList.Count; i++)
                {
                    NPCInfoList[i].SaveDB();
                }
                //任务数据
                for (int i = 0; i < QuestInfoList.Count; i++)
                {
                    QuestInfoList[i].SaveDB();
                }
                //魔法技能数据
                for (int i = 0; i < MagicInfoList.Count; i++)
                {
                    MagicInfoList[i].SaveDB();
                }
                //商店数据
                for (int i = 0; i < GameShopList.Count; i++)
                {
                    GameShopList[i].SaveDB();
                }
                //领地数据
                for (int i = 0; i < ConquestInfos.Count; i++)
                {
                    ConquestInfos[i].SaveDB();
                }
            }
            catch (Exception ex)
            {
                SMain.Enqueue(ex);
            }
            MirConfigDB.Commit();
            SMain.Enqueue("SaveDBThread end");
        }
        public void SaveAccounts()
        {
            Thread t = new Thread(() => SaveAccountsThread());
            t.IsBackground = true;
            t.Start();
        }
        public void SaveAccountsThread()
        {
            try
            {
                SMain.Enqueue("SaveAccountsThread start!");
                MirRunDB.BeginTransaction();
                for (int i = 0; i < AccountList.Count; i++)
                {
                    AccountList[i].SaveDB();
                }
                for (int i = 0; i < CharacterList.Count; i++)
                {
                    CharacterList[i].SaveDB();
                }
                AuctionInfo.SaveAll();
                foreach (MailInfo mail in Mail)
                {
                    mail.SaveDB();
                }
                PayOrder.SaveAll();
                //
                SaveGuilds();
            }
            catch (Exception ex)
            {
                SMain.Enqueue(ex);
            }
            MirRunDB.Commit();
            SMain.Enqueue("SaveAccountsThread end!");
        }

        private void SaveGuilds()
        {
            for(int i=0;i< GuildList.Count; i++)
            {
                GuildList[i].SaveDB();
            }
        }
       

        private void SaveGoods(bool forced = false)
        {
            if (!Directory.Exists(Settings.GoodsPath)) Directory.CreateDirectory(Settings.GoodsPath);

            for (int i = 0; i < MapList.Count; i++)
            {
                Map map = MapList[i];

                if (map.NPCs.Count < 1) continue;

                for (int j = 0; j < map.NPCs.Count; j++)
                {
                    NPCObject npc = map.NPCs[j];

                    if (forced)
                    {
                        npc.ProcessGoods(forced);
                    }

                    if (!npc.NeedSave) continue;

                    string path = Settings.GoodsPath + npc.Info.Index.ToString() + ".msdn";

                    MemoryStream mStream = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(mStream);
                    int Temp = 9999;
                    writer.Write(Temp);
                    writer.Write(Version);
                    writer.Write(CustomVersion);
                    writer.Write(npc.UsedGoods.Count);

                    for (int k = 0; k < npc.UsedGoods.Count; k++)
                    {
                        npc.UsedGoods[k].Save(writer);
                    }

                    FileStream fStream = new FileStream(path, FileMode.Create);
                    byte[] data = mStream.ToArray();
                    fStream.BeginWrite(data, 0, data.Length, EndSaveGoodsAsync, fStream);
                }
            }
        }
        private void EndSaveGoodsAsync(IAsyncResult result)
        {
            try
            {
                FileStream fStream = result.AsyncState as FileStream;
                if (fStream != null)
                {
                    string oldfilename = fStream.Name.Substring(0, fStream.Name.Length - 1);
                    string newfilename = fStream.Name;
                    fStream.EndWrite(result);
                    fStream.Dispose();
                    if (File.Exists(oldfilename))
                        File.Move(oldfilename, oldfilename + "o");
                    File.Move(newfilename, oldfilename);
                    if (File.Exists(oldfilename + "o"))
                        File.Delete(oldfilename + "o");
                }
            }
            catch (Exception)
            {
            }
        }

        private void SaveConquests(bool forced = false)
        {
            if (!Directory.Exists(Settings.ConquestsPath)) Directory.CreateDirectory(Settings.ConquestsPath);
            for (int i = 0; i < Conquests.Count; i++)
            {
                if (Conquests[i].NeedSave || forced)
                {
                    Conquests[i].NeedSave = false;
                    MemoryStream mStream = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(mStream);
                    Conquests[i].Save(writer);
                    FileStream fStream = new FileStream(Settings.ConquestsPath + Conquests[i].Info.Index.ToString() + ".mcdn", FileMode.Create);
                    byte[] data = mStream.ToArray();
                    fStream.BeginWrite(data, 0, data.Length, EndSaveConquestsAsync, fStream);
                }
            }
        }
        private void EndSaveConquestsAsync(IAsyncResult result)
        {
            FileStream fStream = result.AsyncState as FileStream;
            try
            {
                if (fStream != null)
                {
                    string oldfilename = fStream.Name.Substring(0, fStream.Name.Length - 1);
                    string newfilename = fStream.Name;
                    fStream.EndWrite(result);
                    fStream.Dispose();
                    if (File.Exists(oldfilename))
                        File.Move(oldfilename, oldfilename + "o");
                    File.Move(newfilename, oldfilename);
                    if (File.Exists(oldfilename + "o"))
                        File.Delete(oldfilename + "o");
                }
            }
            catch (Exception)
            {

            }
        }

        
        //初始化装载数据
        public void LoadDB()
        {
            lock (LoadLock)
            {
                //地图数据
                MapInfoList.Clear();
                MapInfoList = MapInfo.loadAll();
                //物品数据
                ItemInfoList.Clear();
                ItemInfoList = ItemInfo.loadAll();
                for (int i = 0; i < ItemInfoList.Count; i++)
                {
                    if ((ItemInfoList[i] != null) && (ItemInfoList[i].RandomStatsId < Settings.RandomItemStatsList.Count))
                    {
                        ItemInfoList[i].RandomStats = Settings.RandomItemStatsList[ItemInfoList[i].RandomStatsId];
                    }
                }
                //怪物数据
                MonsterInfoList.Clear();
                MonsterInfoList = MonsterInfo.loadAll();
                //NPC数据
                NPCInfoList.Clear();
                NPCInfoList = NPCInfo.loadAll();

                //任务数据
                QuestInfoList.Clear();
                QuestInfoList = QuestInfo.loadAll();

                //龙数据？
                DragonInfo = new DragonInfo();

                //魔法技能数据
                MagicInfoList.Clear();
                MagicInfoList = MagicInfo.loadAll();

                //商店数据
                GameShopList.Clear();
                GameShopList = GameShopItem.loadAll();
                //领地数据
                ConquestInfos = ConquestInfo.loadAll();

               

                Settings.LinkGuildCreationItems(ItemInfoList);
            }

        }
        //加载账号信息
        //这个是加载所有的账号哦
        public void LoadAccounts()
        {
            //reset ranking
            lock (LoadLock)
            {
                //加载账号，角色
                AccountList.Clear();
                CharacterList.Clear();
                Mail.Clear();
                AccountList = AccountInfo.loadAll();
                CharacterList = CharacterInfo.loadAll();
                AuctionInfo.loadAll();
                Mail = MailInfo.loadAll();


                for (int a = 0; a < AccountList.Count; a++)
                {
                    for (int ac = 0; ac < CharacterList.Count; ac++)
                    {
                        if (CharacterList[ac].accIndex == AccountList[a].Index)
                        {
                            CharacterList[ac].AccountInfo = AccountList[a];
                            AccountList[a].Characters.Add(CharacterList[ac]);
                        }
                    }
                }
         
                if (ResetGS) ClearGameshopLog();

                //加载支付数据
                //支付数据
                PayOrder.loadAll();
            }
        }

        //加载行会信息
        public void LoadGuilds()
        {
            lock (LoadLock)
            {
                //int count = 0;
                GuildList.Clear();
                GuildList = GuildObject.loadAll();
                SMain.Enqueue("加载公会信息:"+ GuildList.Count);
            }
        }

        //加载排行榜
        //每5分钟加载刷新一次
        //只排前20
        public void LoadRank()
        {
            for (int i = 0; i < RankClass.GetLength(0); i++)
            {
                for (int j = 0; j < RankClass.GetLength(1); j++)
                {
                    if(RankClass[i, j] != null)
                    {
                        RankClass[i, j].Clear();
                    }
                    else
                    {
                        RankClass[i, j] = new List<Rank_Character_Info>();
                    }
                }
            }
            //地榜得分保留3天，超过3天每天降100分
            for (int ac = 0; ac < CharacterList.Count; ac++)
            {

            }
            int day = Now.DayOfYear;

            List<Rank_Character_Info> all0 = new List<Rank_Character_Info>();//人榜
            List<Rank_Character_Info> all1 = new List<Rank_Character_Info>();//地帮
            //在内存中计算排行榜
            for (int ac = 0; ac < CharacterList.Count; ac++)
            {
                if (CharacterList[ac].Deleted)
                {
                    continue;
                }
                if(CharacterList[ac].AccountInfo!=null && CharacterList[ac].AccountInfo.AdminAccount)
                {
                    continue;
                }
                //地榜得分保留3天，超过3天每天降100分
                if (CharacterList[ac].fb1_createday == 0|| CharacterList[ac].fb1_createday> day)
                {
                    CharacterList[ac].fb1_createday = day;
                }
                if (CharacterList[ac].fb1_score > 0)
                {
                    if (day - CharacterList[ac].fb1_createday > 2)
                    {
                        CharacterList[ac].fb1_score = CharacterList[ac].fb1_score - 100;
                        CharacterList[ac].fb1_createday = day - 2;
                    }
                }
                
                if (CharacterList[ac].fb1_score < 0)
                {
                    CharacterList[ac].fb1_score = 0;
                }



                Rank_Character_Info r0 = new Rank_Character_Info() { Class = CharacterList[ac].Class, Name = CharacterList[ac].Name, CharacterId= CharacterList[ac].Index, level = CharacterList[ac].Level, Experience = CharacterList[ac].Experience };
                Rank_Character_Info r1 = new Rank_Character_Info() { Class = CharacterList[ac].Class, Name = CharacterList[ac].Name, CharacterId = CharacterList[ac].Index, level = CharacterList[ac].fb1_score };
                if (CharacterList[ac].Player != null)
                {
                    r0.PlayerId = CharacterList[ac].Player.ObjectID;
                    r1.PlayerId = CharacterList[ac].Player.ObjectID;
                }
                all0.Add(r0);
                all1.Add(r1);
            }
            //先排序
            all0.Sort(delegate (Rank_Character_Info p2, Rank_Character_Info p1) {
                if (p1.level.CompareTo(p2.level) == 0)
                {
                    return p1.Experience.CompareTo(p2.Experience);
                }
                return p1.level.CompareTo(p2.level);
            });
            all1.Sort(delegate (Rank_Character_Info p2, Rank_Character_Info p1) { return p1.level.CompareTo(p2.level); });

            if (all0.Count > 0)
            {
                MaxLevel = all0[0].level;
            }
            //放入各自的榜单
            for (int i = 0; i < RankClass.GetLength(0); i++)
            {
                for (int j = 0; j < RankClass.GetLength(1); j++)
                {
                    if (j == 0)//人榜
                    {
                        for(int c = 0;  c< all0.Count; c++)
                        {
                            if(RankClass[i, j].Count >= 20)
                            {
                                break;
                            }
                            if(i== 0)
                            {
                                RankClass[i, j].Add(all0[c]);
                            }
                            else if (i == ((byte)all0[c].Class + 1))
                            {
                                RankClass[i, j].Add(all0[c]);
                            }
                        }
                    }
                    if (j == 1)//地榜
                    {
                        for (int c = 0; c < all1.Count; c++)
                        {
                            if (RankClass[i, j].Count >= 20)
                            {
                                break;
                            }
                            if (i == 0)
                            {
                                RankClass[i, j].Add(all1[c]);
                            }
                            else if (i == ((byte)all1[c].Class + 1))
                            {
                                RankClass[i, j].Add(all1[c]);
                            }
                        }
                    }
                    if (j == 2)//天榜
                    {
                        
                    }
                }
            }

        }

        //加载钓鱼，掉落物品
        public void LoadFishingDrops()
        {
            FishingDrops.Clear();

            for (byte i = 0; i <= 19; i++)
            {
                string path = Path.Combine(Settings.DropPath, Settings.FishingDropFilename + ".txt");

                path = path.Replace("00", i.ToString("D2"));

                if (!File.Exists(path) && i < 2)
                {
                    FileStream newfile = File.Create(path);
                    newfile.Close();
                }

                if (!File.Exists(path)) continue;

                string[] lines = File.ReadAllLines(path, EncodingType.GetType(path));

                for (int j = 0; j < lines.Length; j++)
                {
                    if (lines[j].StartsWith(";") || string.IsNullOrWhiteSpace(lines[j])) continue;

                    DropInfo drop = DropInfo.FromLine("",lines[j]);
                    if (drop == null)
                    {
                        SMain.Enqueue(string.Format("Could not load fishing drop: {0}", lines[j]));
                        continue;
                    }

                    drop.Type = i;

                    FishingDrops.Add(drop);
                }

                FishingDrops.Sort((drop1, drop2) =>
                {
                    if (drop1.Chance > 0 && drop2.Chance == 0)
                        return 1;
                    if (drop1.Chance == 0 && drop2.Chance > 0)
                        return -1;

                    return 0;
                });
            }
        }

        public void LoadAwakeningMaterials()
        {
            AwakeningDrops.Clear();

            string path = Path.Combine(Settings.DropPath, Settings.AwakeningDropFilename + ".txt");

            if (!File.Exists(path))
            {
                FileStream newfile = File.Create(path);
                newfile.Close();

            }

            string[] lines = File.ReadAllLines(path, EncodingType.GetType(path));

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(";") || string.IsNullOrWhiteSpace(lines[i])) continue;

                DropInfo drop = DropInfo.FromLine("",lines[i]);
                if (drop == null)
                {
                    SMain.Enqueue(string.Format("Could not load Awakening drop: {0}", lines[i]));
                    continue;
                }

                AwakeningDrops.Add(drop);
            }

            AwakeningDrops.Sort((drop1, drop2) =>
            {
                if (drop1.Chance > 0 && drop2.Chance == 0)
                    return 1;
                if (drop1.Chance == 0 && drop2.Chance > 0)
                    return -1;

                return 0;
            });
        }
        //宝盒爆率?
        public void LoadStrongBoxDrops()
        {
            StrongboxDrops.Clear();

            string path = Path.Combine(Settings.DropPath, Settings.StrongboxDropFilename + ".txt");

            if (!File.Exists(path))
            {
                FileStream newfile = File.Create(path);
                newfile.Close();
            }

            string[] lines = File.ReadAllLines(path, EncodingType.GetType(path));

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(";") || string.IsNullOrWhiteSpace(lines[i])) continue;

                DropInfo drop = DropInfo.FromLine("",lines[i]);
                if (drop == null)
                {
                    SMain.Enqueue(string.Format("Could not load strongbox drop: {0}", lines[i]));
                    continue;
                }

                StrongboxDrops.Add(drop);
            }

            StrongboxDrops.Sort((drop1, drop2) =>
            {
                if (drop1.Chance > 0 && drop2.Chance == 0)
                    return 1;
                if (drop1.Chance == 0 && drop2.Chance > 0)
                    return -1;

                return 0;
            });
        }
        //黑石？
        public void LoadBlackStoneDrops()
        {
            BlackstoneDrops.Clear();

            string path = Path.Combine(Settings.DropPath, Settings.BlackstoneDropFilename + ".txt");

            if (!File.Exists(path))
            {
                FileStream newfile = File.Create(path);
                newfile.Close();

            }

            string[] lines = File.ReadAllLines(path, EncodingType.GetType(path));

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(";") || string.IsNullOrWhiteSpace(lines[i])) continue;

                DropInfo drop = DropInfo.FromLine("",lines[i]);
                if (drop == null)
                {
                    SMain.Enqueue(string.Format("Could not load blackstone drop: {0}", lines[i]));
                    continue;
                }

                BlackstoneDrops.Add(drop);
            }

            BlackstoneDrops.Sort((drop1, drop2) =>
            {
                if (drop1.Chance > 0 && drop2.Chance == 0)
                    return 1;
                if (drop1.Chance == 0 && drop2.Chance > 0)
                    return -1;

                return 0;
            });
        }
        //占领，城池？
        public void LoadConquests()
        {
            lock (LoadLock)
            {
                int count = 0;

                Conquests.Clear();

                ConquestObject newConquest;
                Map tempMap;
                ConquestArcherObject tempArcher;
                ConquestGateObject tempGate;
                ConquestWallObject tempWall;
                ConquestSiegeObject tempSiege;


                for (int i = 0; i < ConquestInfos.Count; i++)
                {
                    newConquest = null;
                    tempMap = GetMap(ConquestInfos[i].MapIndex);

                    if (tempMap == null) continue;

                    if (File.Exists(Settings.ConquestsPath + ConquestInfos[i].Index.ToString() + ".mcd"))
                    {
                        using (FileStream stream = File.OpenRead(Settings.ConquestsPath + ConquestInfos[i].Index.ToString() + ".mcd"))
                        using (BinaryReader reader = new BinaryReader(stream))
                            newConquest = new ConquestObject(reader) { Info = ConquestInfos[i], ConquestMap = tempMap };

                        for (int k = 0; k < GuildList.Count; k++)
                        {
                            if (newConquest.Owner == GuildList[k].Guildindex)
                            {
                                newConquest.Guild = GuildList[k];
                                GuildList[k].Conquest = newConquest;
                            }
                        }

                        Conquests.Add(newConquest);
                        tempMap.Conquest.Add(newConquest);
                        count++;
                    }
                    else
                    {
                        newConquest = new ConquestObject { Info = ConquestInfos[i], NeedSave = true, ConquestMap = tempMap };

                        Conquests.Add(newConquest);
                        tempMap.Conquest.Add(newConquest);
                    }

                    //Bind Info to Saved Archer objects or create new objects
                    for (int j = 0; j < ConquestInfos[i].ConquestGuards.Count; j++)
                    {
                        tempArcher = newConquest.ArcherList.FirstOrDefault(x => x.Index == ConquestInfos[i].ConquestGuards[j].Index);

                        if (tempArcher != null)
                        {
                            tempArcher.Info = ConquestInfos[i].ConquestGuards[j];
                            tempArcher.Conquest = newConquest;
                        }
                        else
                        {
                            newConquest.ArcherList.Add(new ConquestArcherObject { Info = ConquestInfos[i].ConquestGuards[j], Alive = true, Index = ConquestInfos[i].ConquestGuards[j].Index, Conquest = newConquest });
                        }
                    }

                    //Remove archers that have been removed from DB
                    for (int j = 0; j < newConquest.ArcherList.Count; j++)
                    {
                        if (newConquest.ArcherList[j].Info == null)
                            newConquest.ArcherList.Remove(newConquest.ArcherList[j]);
                    }

                    //Bind Info to Saved Gate objects or create new objects
                    for (int j = 0; j < ConquestInfos[i].ConquestGates.Count; j++)
                    {
                        tempGate = newConquest.GateList.FirstOrDefault(x => x.Index == ConquestInfos[i].ConquestGates[j].Index);

                        if (tempGate != null)
                        {
                            tempGate.Info = ConquestInfos[i].ConquestGates[j];
                            tempGate.Conquest = newConquest;
                        }
                        else
                        {
                            newConquest.GateList.Add(new ConquestGateObject { Info = ConquestInfos[i].ConquestGates[j], Health = uint.MaxValue, Index = ConquestInfos[i].ConquestGates[j].Index, Conquest = newConquest });
                        }
                    }

                    //Bind Info to Saved Flag objects or create new objects
                    for (int j = 0; j < ConquestInfos[i].ConquestFlags.Count; j++)
                    {
                        newConquest.FlagList.Add(new ConquestFlagObject { Info = ConquestInfos[i].ConquestFlags[j], Index = ConquestInfos[i].ConquestFlags[j].Index, Conquest = newConquest });
                    }

                    //Remove Gates that have been removed from DB
                    for (int j = 0; j < newConquest.GateList.Count; j++)
                    {
                        if (newConquest.GateList[j].Info == null)
                            newConquest.GateList.Remove(newConquest.GateList[j]);
                    }

                    //Bind Info to Saved Wall objects or create new objects
                    for (int j = 0; j < ConquestInfos[i].ConquestWalls.Count; j++)
                    {
                        tempWall = newConquest.WallList.FirstOrDefault(x => x.Index == ConquestInfos[i].ConquestWalls[j].Index);

                        if (tempWall != null)
                        {
                            tempWall.Info = ConquestInfos[i].ConquestWalls[j];
                            tempWall.Conquest = newConquest;
                        }
                        else
                        {
                            newConquest.WallList.Add(new ConquestWallObject { Info = ConquestInfos[i].ConquestWalls[j], Index = ConquestInfos[i].ConquestWalls[j].Index, Health = uint.MaxValue, Conquest = newConquest });
                        }
                    }

                    //Remove Walls that have been removed from DB
                    for (int j = 0; j < newConquest.WallList.Count; j++)
                    {
                        if (newConquest.WallList[j].Info == null)
                            newConquest.WallList.Remove(newConquest.WallList[j]);
                    }


                    //Bind Info to Saved Siege objects or create new objects
                    for (int j = 0; j < ConquestInfos[i].ConquestSieges.Count; j++)
                    {
                        tempSiege = newConquest.SiegeList.FirstOrDefault(x => x.Index == ConquestInfos[i].ConquestSieges[j].Index);

                        if (tempSiege != null)
                        {
                            tempSiege.Info = ConquestInfos[i].ConquestSieges[j];
                            tempSiege.Conquest = newConquest;
                        }
                        else
                        {
                            newConquest.SiegeList.Add(new ConquestSiegeObject { Info = ConquestInfos[i].ConquestSieges[j], Index = ConquestInfos[i].ConquestSieges[j].Index, Health = uint.MaxValue, Conquest = newConquest });
                        }
                    }

                    //Remove Siege that have been removed from DB
                    for (int j = 0; j < newConquest.SiegeList.Count; j++)
                    {
                        if (newConquest.SiegeList[j].Info == null)
                            newConquest.SiegeList.Remove(newConquest.SiegeList[j]);
                    }

                    //Bind Info to Saved Flag objects or create new objects
                    for (int j = 0; j < ConquestInfos[i].ControlPoints.Count; j++)
                    {
                        ConquestFlagObject cp = null;
                        newConquest.ControlPoints.Add(cp = new ConquestFlagObject { Info = ConquestInfos[i].ControlPoints[j], Index = ConquestInfos[i].ControlPoints[j].Index, Conquest = newConquest }, new Dictionary<GuildObject, int>());

                        cp.Spawn();
                    }


                    newConquest.LoadArchers();
                    newConquest.LoadGates();
                    newConquest.LoadWalls();
                    newConquest.LoadSieges();
                    newConquest.LoadFlags();
                    newConquest.LoadNPCs();
                }
            }
        }

 

        //线程启动
        public void Start()
        {
            if (Running || _thread != null) return;

            Running = true;

            _thread = new Thread(WorkLoop) { IsBackground = true };
            _thread.Start();
        }


        public void Stop()
        {
            Running = false;

            //这里保存下数据才可以吧

            lock (_locker)
            {
                Monitor.PulseAll(_locker);         // changing a blocking condition. (this makes the threads wake up!)
            }

            //simply intterupt all the mob threads if they are running (will give an invisible error on them but fastest way of getting rid of them on shutdowns)
            for (int i = 1; i < MobThreading.Length; i++)
            {
                if (MobThreads[i] != null)
                    MobThreads[i].EndTime = Time + 9999;
                if ((MobThreading[i] != null) &&
                    (MobThreading[i].ThreadState != System.Threading.ThreadState.Stopped) && (MobThreading[i].ThreadState != System.Threading.ThreadState.Unstarted))
                {
                    MobThreading[i].Interrupt();
                }
            }


            while (_thread != null)
                Thread.Sleep(10);
        }

        public void Reboot()
        {
            (new Thread(() =>
            {
                SMain.Enqueue("Server rebooting...");
                Stop();
                Start();
            })).Start();
        }

        private void StartEnvir()
        {
            Players.Clear();
            StartPoints.Clear();
            StartItems.Clear();
            MapList.Clear();
            GameshopLog.Clear();
            CustomCommands.Clear();
            MonsterCount = 0;

            LoadDB();

            RecipeInfoList.Clear();
            foreach (var recipe in Directory.GetFiles(Settings.RecipePath, "*.txt")
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .ToArray())
                RecipeInfoList.Add(new RecipeInfo(recipe));

            SMain.Enqueue(string.Format("{0} Recipes loaded.", RecipeInfoList.Count));

            for (int i = 0; i < MapInfoList.Count; i++)
                MapInfoList[i].CreateMap();
            SMain.Enqueue(string.Format("{0} Maps Loaded.", MapInfoList.Count));
            SMain.Enqueue(string.Format("{0} Maps cellCount.", Map.cellcount));

            for (int i = 0; i < ItemInfoList.Count; i++)
            {
                if (ItemInfoList[i].StartItem)
                    StartItems.Add(ItemInfoList[i]);
            }

            for (int i = 0; i < MonsterInfoList.Count; i++)
            {
                MonsterInfoList[i].LoadDrops();
                MonsterInfoList[i].LoadCommonDrops();
            }
                

            LoadFishingDrops();
            LoadAwakeningMaterials();
            LoadStrongBoxDrops();
            LoadBlackStoneDrops();
            SMain.Enqueue("Drops Loaded.");

            if (DragonInfo.Enabled)
            {
                DragonSystem = new Dragon(DragonInfo);
                if (DragonSystem != null)
                {
                    if (DragonSystem.Load()) DragonSystem.Info.LoadDrops();
                }

                SMain.Enqueue("Dragon Loaded.");
            }

            DefaultNPC = new NPCObject(new NPCInfo() { Name = "DefaultNPC", FileName = Settings.DefaultNPCFilename, IsDefault = true });
            MonsterNPC = new NPCObject(new NPCInfo() { Name = "MonsterNPC", FileName = Settings.MonsterNPCFilename, IsDefault = true });
            RobotNPC = new NPCObject(new NPCInfo() { Name = "RobotNPC", FileName = Settings.RobotNPCFilename, IsDefault = true, IsRobot = true });

            SMain.Enqueue("Envir Started.");
        }

        //启动网络监听,这里开启2个端口的网络监听？
        private void StartNetwork()
        {
            Connections.Clear();

            LoadAccounts();

            LoadGuilds();

            LoadConquests();

            _listener = new TcpListener(IPAddress.Parse(Settings.IPAddress), Settings.Port);
            _listener.Start();
            _listener.BeginAcceptTcpClient(Connection, null);

            if (StatusPortEnabled)
            {
                _StatusPort = new TcpListener(IPAddress.Parse(Settings.IPAddress), 3000);
                _StatusPort.Start();
                _StatusPort.BeginAcceptTcpClient(StatusConnection, null);
            }
            SMain.Enqueue("Network Started.");

            //FixGuilds();
        }

        private void StopEnvir()
        {
            SaveGoods(true);

            MapList.Clear();
            StartPoints.Clear();
            StartItems.Clear();
            Objects.Clear();
            Players.Clear();

            CleanUp();

            GC.Collect();

            SMain.Enqueue("Envir Stopped.");
        }
        private void StopNetwork()
        {
            _listener.Stop();
            lock (Connections)
            {
                for (int i = Connections.Count - 1; i >= 0; i--)
                    Connections[i].SendDisconnect(0);
            }

            if (StatusPortEnabled)
            {
                _StatusPort.Stop();
                for (int i = StatusConnections.Count - 1; i >= 0; i--)
                    StatusConnections[i].SendDisconnect();
            }

            long expire = Time + 5000;

            while (Connections.Count != 0 && Stopwatch.ElapsedMilliseconds < expire)
            {
                Time = Stopwatch.ElapsedMilliseconds;

                for (int i = Connections.Count - 1; i >= 0; i--)
                    Connections[i].Process();

                Thread.Sleep(1);
            }


            Connections.Clear();

            expire = Time + 10000;
            while (StatusConnections.Count != 0 && Stopwatch.ElapsedMilliseconds < expire)
            {
                Time = Stopwatch.ElapsedMilliseconds;

                for (int i = StatusConnections.Count - 1; i >= 0; i--)
                    StatusConnections[i].Process();

                Thread.Sleep(1);
            }


            StatusConnections.Clear();
            SMain.Enqueue("Network Stopped.");
        }

        private void CleanUp()
        {
            for (int i = 0; i < CharacterList.Count; i++)
            {
                CharacterInfo info = CharacterList[i];

                if (info.Deleted)
                {
                    #region Mentor Cleanup
                    if (info.Mentor > 0)
                    {
                        CharacterInfo Mentor = GetCharacterInfo(info.Mentor);

                        if (Mentor != null)
                        {
                            Mentor.Mentor = 0;
                            Mentor.MentorExp = 0;
                            Mentor.isMentor = false;
                        }

                        info.Mentor = 0;
                        info.MentorExp = 0;
                        info.isMentor = false;
                    }
                    #endregion

                    #region Marriage Cleanup
                    if (info.Married > 0)
                    {
                        CharacterInfo Lover = GetCharacterInfo(info.Married);

                        info.Married = 0;
                        info.MarriedDate = DateTime.Now;

                        Lover.Married = 0;
                        Lover.MarriedDate = DateTime.Now;
                        if (Lover.Equipment[(int)EquipmentSlot.RingL] != null)
                            Lover.Equipment[(int)EquipmentSlot.RingL].WeddingRing = -1;
                    }
                    #endregion

                    if (info.DeleteDate < DateTime.Now.AddDays(-7))
                    {
                        //delete char from db
                    }
                }

                if (info.Mail.Count > Settings.MailCapacity)
                {
                    for (int j = (info.Mail.Count - 1 - (int)Settings.MailCapacity); j >= 0; j--)
                    {
                        if (info.Mail[j].DateOpened > DateTime.Now && info.Mail[j].Collected && info.Mail[j].Items.Count == 0 && info.Mail[j].Gold == 0)
                        {
                            info.Mail.Remove(info.Mail[j]);
                        }
                    }
                }
            }
        }

        private void Connection(IAsyncResult result)
        {
            if (!Running || !_listener.Server.IsBound) return;

            try
            {
                TcpClient tempTcpClient = _listener.EndAcceptTcpClient(result);
                lock (Connections)
                    Connections.Add(new MirConnection(++_sessionID, tempTcpClient));
            }
            catch (Exception ex)
            {
                SMain.Enqueue(ex);
            }
            finally
            {
                while (Connections.Count >= Settings.MaxUser)
                    Thread.Sleep(1);

                if (Running && _listener.Server.IsBound)
                    _listener.BeginAcceptTcpClient(Connection, null);
            }
        }

        private void StatusConnection(IAsyncResult result)
        {
            if (!Running || !_StatusPort.Server.IsBound) return;

            try
            {
                TcpClient tempTcpClient = _StatusPort.EndAcceptTcpClient(result);
                lock (StatusConnections)
                    StatusConnections.Add(new MirStatusConnection(tempTcpClient));
            }
            catch (Exception ex)
            {
                SMain.Enqueue(ex);
            }
            finally
            {
                while (StatusConnections.Count >= 5) //dont allow to many status port connections it's just an abuse thing
                    Thread.Sleep(1);

                if (Running && _StatusPort.Server.IsBound)
                    _StatusPort.BeginAcceptTcpClient(StatusConnection, null);
            }
        }

        public void NewAccount(ClientPackets.NewAccount p, MirConnection c)
        {
            if (!Settings.AllowNewAccount)
            {
                c.Enqueue(new ServerPackets.NewAccount { Result = 0 });
                return;
            }

            if (!AccountIDReg.IsMatch(p.AccountID))
            {
                c.Enqueue(new ServerPackets.NewAccount { Result = 1 });
                return;
            }

            if (!PasswordReg.IsMatch(p.Password))
            {
                c.Enqueue(new ServerPackets.NewAccount { Result = 2 });
                return;
            }
            if (!string.IsNullOrWhiteSpace(p.EMailAddress) && !EMailReg.IsMatch(p.EMailAddress) ||
                p.EMailAddress.Length > 50)
            {
                c.Enqueue(new ServerPackets.NewAccount { Result = 3 });
                return;
            }

            if (!string.IsNullOrWhiteSpace(p.UserName) && p.UserName.Length > 20)
            {
                c.Enqueue(new ServerPackets.NewAccount { Result = 4 });
                return;
            }

            if (!string.IsNullOrWhiteSpace(p.SecretQuestion) && p.SecretQuestion.Length > 30)
            {
                c.Enqueue(new ServerPackets.NewAccount { Result = 5 });
                return;
            }

            if (!string.IsNullOrWhiteSpace(p.SecretAnswer) && p.SecretAnswer.Length > 30)
            {
                c.Enqueue(new ServerPackets.NewAccount { Result = 6 });
                return;
            }

            lock (AccountLock)
            {
                if (AccountExists(p.AccountID))
                {
                    c.Enqueue(new ServerPackets.NewAccount { Result = 7 });
                    return;
                }

                AccountList.Add(new AccountInfo(p) { Index = (ulong)UniqueKeyHelper.UniqueNext(), CreationIP = c.IPAddress });


                c.Enqueue(new ServerPackets.NewAccount { Result = 8 });
            }
        }
        public void ChangePassword(ClientPackets.ChangePassword p, MirConnection c)
        {
            if (!Settings.AllowChangePassword)
            {
                c.Enqueue(new ServerPackets.ChangePassword { Result = 0 });
                return;
            }

            if (!AccountIDReg.IsMatch(p.AccountID))
            {
                c.Enqueue(new ServerPackets.ChangePassword { Result = 1 });
                return;
            }

            if (!PasswordReg.IsMatch(p.CurrentPassword))
            {
                c.Enqueue(new ServerPackets.ChangePassword { Result = 2 });
                return;
            }

            if (!PasswordReg.IsMatch(p.NewPassword))
            {
                c.Enqueue(new ServerPackets.ChangePassword { Result = 3 });
                return;
            }

            AccountInfo account = GetAccount(p.AccountID);

            if (account == null)
            {
                c.Enqueue(new ServerPackets.ChangePassword { Result = 4 });
                return;
            }

            if (account.Banned)
            {
                if (account.ExpiryDate > Now)
                {
                    c.Enqueue(new ServerPackets.ChangePasswordBanned { Reason = account.BanReason, ExpiryDate = account.ExpiryDate });
                    return;
                }
                account.Banned = false;
            }
            account.BanReason = string.Empty;
            account.ExpiryDate = DateTime.MinValue;

            if (String.CompareOrdinal(account.Password, p.CurrentPassword) != 0)
            {
                c.Enqueue(new ServerPackets.ChangePassword { Result = 5 });
                return;
            }

            account.Password = p.NewPassword;
            c.Enqueue(new ServerPackets.ChangePassword { Result = 6 });
        }
        public void Login(ClientPackets.Login p, MirConnection c)
        {
            if (!Settings.AllowLogin)
            {
                c.Enqueue(new ServerPackets.Login { Result = 0 });
                return;
            }

            if (!AccountIDReg.IsMatch(p.AccountID))
            {
                c.Enqueue(new ServerPackets.Login { Result = 1 });
                return;
            }

            if (!PasswordReg.IsMatch(p.Password))
            {
                c.Enqueue(new ServerPackets.Login { Result = 2 });
                return;
            }
            AccountInfo account = GetAccount(p.AccountID);

            if (account == null)
            {
                c.Enqueue(new ServerPackets.Login { Result = 3 });
                return;
            }

            if (account.Banned)
            {
                if (account.ExpiryDate > DateTime.Now)
                {
                    c.Enqueue(new ServerPackets.LoginBanned
                    {
                        Reason = account.BanReason,
                        ExpiryDate = account.ExpiryDate
                    });
                    return;
                }
                account.Banned = false;
            }
            account.BanReason = string.Empty;
            account.ExpiryDate = DateTime.MinValue;


            if (String.CompareOrdinal(account.Password, p.Password) != 0)
            {
                if (account.WrongPasswordCount++ >= 5)
                {
                    account.Banned = true;
                    account.BanReason = "错误登录次数太多.";
                    account.ExpiryDate = DateTime.Now.AddMinutes(2);

                    c.Enqueue(new ServerPackets.LoginBanned
                    {
                        Reason = account.BanReason,
                        ExpiryDate = account.ExpiryDate
                    });
                    return;
                }

                c.Enqueue(new ServerPackets.Login { Result = 4 });
                return;
            }
            account.WrongPasswordCount = 0;

            lock (AccountLock)
            {
                if (account.Connection != null)
                    account.Connection.SendDisconnect(1);

                account.Connection = c;
            }

            c.Account = account;
            c.Stage = GameStage.Select;

            account.LastDate = Now;
            account.LastIP = c.IPAddress;

            SMain.Enqueue(account.Connection.SessionID + ", " + account.Connection.IPAddress + ", User logged in.");
            c.Enqueue(new ServerPackets.LoginSuccess { Characters = account.GetSelectInfo()});
            
        }
        public void NewCharacter(ClientPackets.NewCharacter p, MirConnection c, bool IsGm)
        {
            if (!Settings.AllowNewCharacter)
            {
                c.Enqueue(new ServerPackets.NewCharacter { Result = 0 });
                return;
            }

            if (p.Name == null || p.Name.Length < Globals.MinCharacterNameLength || p.Name.Length > Globals.MaxCharacterNameLength)
            {
                c.Enqueue(new ServerPackets.NewCharacter { Result = 1 });
                return;
            }

            if ((!IsGm) && (DisabledCharNames.Contains(p.Name.ToUpper())))
            {
                c.Enqueue(new ServerPackets.NewCharacter { Result = 1 });
                return;
            }

            if (p.Gender != MirGender.Male && p.Gender != MirGender.Female)
            {
                c.Enqueue(new ServerPackets.NewCharacter { Result = 2 });
                return;
            }

            if (p.Class != MirClass.Warrior && p.Class != MirClass.Wizard && p.Class != MirClass.Taoist &&
                p.Class != MirClass.Assassin && p.Class != MirClass.Archer)
            {
                c.Enqueue(new ServerPackets.NewCharacter { Result = 3 });
                return;
            }

            if ((p.Class == MirClass.Assassin && !Settings.AllowCreateAssassin) ||
                (p.Class == MirClass.Archer && !Settings.AllowCreateArcher))
            {
                c.Enqueue(new ServerPackets.NewCharacter { Result = 3 });
                return;
            }

            int count = 0;

            for (int i = 0; i < c.Account.Characters.Count; i++)
            {
                if (c.Account.Characters[i].Deleted) continue;

                if (++count >= Globals.MaxCharacterCount)
                {
                    c.Enqueue(new ServerPackets.NewCharacter { Result = 4 });
                    return;
                }
            }

            lock (AccountLock)
            {
                if (CharacterExists(p.Name))
                {
                    c.Enqueue(new ServerPackets.NewCharacter { Result = 5 });
                    return;
                }

                CharacterInfo info = new CharacterInfo(p, c) { Index = (ulong)UniqueKeyHelper.UniqueNext(), AccountInfo = c.Account };

                c.Account.Characters.Add(info);
                CharacterList.Add(info);

                c.Enqueue(new ServerPackets.NewCharacterSuccess { CharInfo = info.ToSelectInfo() });
            }
        }

        public bool AccountExists(string accountID)
        {
            for (int i = 0; i < AccountList.Count; i++)
                if (String.Compare(AccountList[i].AccountID, accountID, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;

            return false;
        }
        public bool CharacterExists(string name)
        {
            for (int i = 0; i < CharacterList.Count; i++)
                if (String.Compare(CharacterList[i].Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;

            return false;
        }

        private AccountInfo GetAccount(string accountID)
        {
            for (int i = 0; i < AccountList.Count; i++)
                if (String.Compare(AccountList[i].AccountID, accountID, StringComparison.OrdinalIgnoreCase) == 0)
                    return AccountList[i];

            return null;
        }

        public AccountInfo GetAccount(ulong AccountId)
        {
            for (int i = 0; i < AccountList.Count; i++)
                if (AccountList[i].Index == AccountId)
                    return AccountList[i];

            return null;
        }

        public List<AccountInfo> MatchAccounts(string accountID, bool match = false)
        {
            if (string.IsNullOrEmpty(accountID)) return new List<AccountInfo>(AccountList);

            List<AccountInfo> list = new List<AccountInfo>();

            for (int i = 0; i < AccountList.Count; i++)
            {
                if (match)
                {
                    if (AccountList[i].AccountID.Equals(accountID, StringComparison.OrdinalIgnoreCase))
                        list.Add(AccountList[i]);
                }
                else
                {
                    if (AccountList[i].AccountID.IndexOf(accountID, StringComparison.OrdinalIgnoreCase) >= 0)
                        list.Add(AccountList[i]);
                }
            }

            return list;
        }

        public List<AccountInfo> MatchAccountsByPlayer(string playerName, bool match = false)
        {
            if (string.IsNullOrEmpty(playerName)) return new List<AccountInfo>(AccountList);

            List<AccountInfo> list = new List<AccountInfo>();

            for (int i = 0; i < AccountList.Count; i++)
            {
                for (int j = 0; j < AccountList[i].Characters.Count; j++)
                {
                    if (match)
                    {
                        if (AccountList[i].Characters[j].Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                            list.Add(AccountList[i]);
                    }
                    else
                    {
                        if (AccountList[i].Characters[j].Name.IndexOf(playerName, StringComparison.OrdinalIgnoreCase) >= 0)
                            list.Add(AccountList[i]);
                    }
                }
            }

            return list;
        }

        public void CreateAccountInfo()
        {
            AccountList.Add(new AccountInfo { Index = (ulong)UniqueKeyHelper.UniqueNext() });
        }
        public void CreateMapInfo()
        {
            MapInfoList.Add(new MapInfo { Index = (int)DBObjectUtils.getObjNextId(new MapInfo()) });
        }
        public void CreateItemInfo(ItemType type = ItemType.Nothing)
        {
            //SMain.Enqueue("getObjNextId:"+ DBObjectUtils.getObjNextId(new ItemInfo()));
            ItemInfoList.Add(new ItemInfo { Index = (int)DBObjectUtils.getObjNextId(new ItemInfo()), Type = type, RandomStatsId = 255 });
        }
        public void CreateMonsterInfo()
        {
            MonsterInfoList.Add(new MonsterInfo { Index = (int)DBObjectUtils.getObjNextId(new MonsterInfo()) });
        }
        public void CreateNPCInfo()
        {
            NPCInfoList.Add(new NPCInfo { Index = (int)DBObjectUtils.getObjNextId(new NPCInfo()) });
        }
        public void CreateQuestInfo()
        {
            QuestInfoList.Add(new QuestInfo { Index = (int)DBObjectUtils.getObjNextId(new QuestInfo()) });
        }

        public void AddToGameShop(ItemInfo Info)
        {
            GameShopList.Add(new GameShopItem { GIndex = (int)DBObjectUtils.getObjNextId(new GameShopItem()), GoldPrice = (uint)(1000 * Settings.CredxGold), CreditPrice = 1000, ItemIndex = Info.Index, Info = Info, Date = DateTime.Now, Class = "All", Category = Info.Type.ToString() });
        }

        public void Remove(MapInfo info)
        {
            MapInfoList.Remove(info);
            //Desync all objects\
        }
        public void Remove(ItemInfo info)
        {
            ItemInfoList.Remove(info);
        }
        public void Remove(MonsterInfo info)
        {
            MonsterInfoList.Remove(info);
            //Desync all objects\
        }
        public void Remove(NPCInfo info)
        {
            NPCInfoList.Remove(info);
            //Desync all objects\
        }
        public void Remove(QuestInfo info)
        {
            QuestInfoList.Remove(info);
            //Desync all objects\
        }

        public void Remove(GameShopItem info)
        {
            GameShopList.Remove(info);

            if (GameShopList.Count == 0)
            {
                //GameshopIndex = 0;
            }

            //Desync all objects\
        }


        public bool BindQuest(QuestProgressInfo quest)
        {
            for (int i = 0; i < QuestInfoList.Count; i++)
            {
                QuestInfo info = QuestInfoList[i];
                if (info.Index != quest.Index) continue;
                quest.Info = info;
                return true;
            }
            return false;
        }

        public Map GetMap(int index)
        {
            return MapList.FirstOrDefault(t => t.Info.Index == index);
        }


        //根据地图名称查找地图，创建副本
        public Map GetMapByNameCopy(string name,int fb_id)
        {
            List<Map> list = new List<Map>();
            foreach (Map m in MapList)
            {
                if (String.Equals(m.Info.Mcode, name, StringComparison.CurrentCultureIgnoreCase))
                {
                    list.Add(m);
                }
            }
            if (list.Count == 0)
            {
                return null;
            }
            Map retm = null;
            //创建一个副本
            if (list.Count == 1)
            {
                retm = list[0].Info.CreateInstance();
                SMain.Enqueue("创建地图副本：" + fb_id+",idx:" + list.Count );
                retm.mapSProcess = FBMap.getInstance(fb_id);
                return retm;
            }
            //第一个不算副本哈
            for(int i=1;i< list.Count; i++)
            {
                if (list[i].InactiveCount > 5)
                {
                    retm = list[i];
                    retm.Clear();
                    //SMain.Enqueue("创建地图副本：" + fb_id + ",idx:" + i);
                    retm.mapSProcess = FBMap.getInstance(fb_id);
                    return retm;
                }
            }
            //
            retm = list[0].Info.CreateInstance();
            SMain.Enqueue("创建地图副本：" + fb_id + ",idx:" + list.Count);
            retm.mapSProcess = FBMap.getInstance(fb_id);
            return retm;
        }


        //根据地图名称查找地图
        public Map GetMapByNameAndInstance(string name, int instanceValue = 0)
        {
            if (instanceValue < 0) instanceValue = 0;
            if (instanceValue > 0) instanceValue--;
            List<Map> list = new List<Map>();
            foreach(Map m in MapList)
            {
                if(String.Equals(m.Info.Mcode, name, StringComparison.CurrentCultureIgnoreCase))
                {
                    list.Add(m);
                }
            }
            
            if (instanceValue < list.Count)
            {
                return list[instanceValue];
            }
  
            return null;
        }

        //这个没用到，还好
        public MapObject GetObject(uint objectID)
        {
            return Objects.FirstOrDefault(e => e.ObjectID == objectID);
        }
        //这个只有游戏开始的时候调用一下，还好
        public MonsterInfo GetMonsterInfo(int index)
        {
            for (int i = 0; i < MonsterInfoList.Count; i++)
                if (MonsterInfoList[i].Index == index) return MonsterInfoList[i];

            return null;
        }

        public MonsterInfo GetMonsterInfoByImage(Monster Image)
        {
            for (int i = 0; i < MonsterInfoList.Count; i++)
                if (MonsterInfoList[i].Image == Image) return MonsterInfoList[i];

            return null;
        }

        //通过NPC名称，查找NPC
        public NPCInfo GetNPCInfoByName(string name)
        {
            foreach (NPCInfo n in NPCInfoList)
            {
                if (String.Equals(n.Name, name, StringComparison.CurrentCultureIgnoreCase))
                {
                    return n;
                }
            }
            return null;
        }
        //没用到
        public NPCObject GetNPC(string name)
        {
            return MapList.SelectMany(t1 => t1.NPCs.Where(t => t.Info.Name == name)).FirstOrDefault();
        }
        /*
        public MonsterInfo GetMonsterInfo(string name)
        {
            for (int i = 0; i < MonsterInfoList.Count; i++)
            {
                MonsterInfo info = MonsterInfoList[i];
                //if (info.Name != name && !info.Name.Replace(" ", "").StartsWith(name, StringComparison.OrdinalIgnoreCase)) continue;
                if (String.Compare(info.Name, name, StringComparison.OrdinalIgnoreCase) != 0 && String.Compare(info.Name.Replace(" ", ""), name.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) != 0) continue;
                return info;
            }
            return null;
        }
        */

        public MonsterInfo GetMonsterInfo(string name, bool Strict = false)
        {
            //先查找完全相同的
            for (int i = 0; i < MonsterInfoList.Count; i++)
            {
                MonsterInfo info = MonsterInfoList[i];
                if (info.Name != name) continue;
                return info;
            }
            //找不到再查找类似名称的
            for (int i = 0; i < MonsterInfoList.Count; i++)
            {
                MonsterInfo info = MonsterInfoList[i];
                if (info.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase)) continue;
                return info;
            }
            return null;
        }
        public PlayerObject GetPlayer(string name)
        {
            for (int i = 0; i < Players.Count; i++)
                if (String.Compare(Players[i].Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                    return Players[i];

            return null;
        }
        public PlayerObject GetPlayer(uint PlayerId)
        {
            for (int i = 0; i < Players.Count; i++)
                if (Players[i].Info.Index == PlayerId)
                    return Players[i];

            return null;
        }
        public CharacterInfo GetCharacterInfo(string name)
        {
            for (int i = 0; i < CharacterList.Count; i++)
                if (String.Compare(CharacterList[i].Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                    return CharacterList[i];

            return null;
        }

        public CharacterInfo GetCharacterInfo(ulong index)
        {
            for (int i = 0; i < CharacterList.Count; i++)
                if (CharacterList[i].Index == index)
                    return CharacterList[i];

            return null;
        }


        public QuestInfo GetQuestInfo(int index)
        {
            return QuestInfoList.FirstOrDefault(info => info.Index == index);
        }

        public ItemInfo GetBook(short Skill)
        {
            for (int i = 0; i < ItemInfoList.Count; i++)
            {
                ItemInfo info = ItemInfoList[i];
                if ((info.Type != ItemType.Book) || (info.Shape != Skill)) continue;
                return info;
            }
            return null;
        }

        //这里循环所有账号不好吧。循环所有连接好一点？
        public void MessageAccount(AccountInfo account, string message, ChatType type)
        {
            if (account == null) return;
            if (account.Characters == null) return;

            for (int i = 0; i < account.Characters.Count; i++)
            {
                if (account.Characters[i].Player == null) continue;
                account.Characters[i].Player.ReceiveChat(message, type);
                return;
            }
        }

        public GuildObject GetGuild(string name)
        {
            for (int i = 0; i < GuildList.Count; i++)
            {
                if (String.Compare(GuildList[i].Name.Replace(" ", ""), name, StringComparison.OrdinalIgnoreCase) != 0) continue;
                return GuildList[i];
            }
            return null;
        }
        public GuildObject GetGuild(long index)
        {
            for (int i = 0; i < GuildList.Count; i++)
                if (GuildList[i].Guildindex == index)
                    return GuildList[i];
            return null;
        }

        public void ProcessNewDay()
        {
            foreach (CharacterInfo c in CharacterList)
            {
                ClearDailyQuests(c);

                c.NewDay = true;

                if (c.Player != null)
                {
                    c.Player.CallDefaultNPC(DefaultNPCType.Daily);
                }
            }
        }

        private void ProcessRentedItems()
        {
            foreach (var characterInfo in CharacterList)
            {
                if (characterInfo.RentedItems.Count <= 0)
                    continue;

                foreach (var rentedItemInfo in characterInfo.RentedItems)
                {
                    if (rentedItemInfo.ItemReturnDate >= Now)
                        continue;

                    var rentingPlayer = GetCharacterInfo(rentedItemInfo.RentingPlayerName);

                    for (var i = 0; i < rentingPlayer.Inventory.Length; i++)
                    {
                        if (rentedItemInfo.ItemId != rentingPlayer?.Inventory[i]?.UniqueID)
                            continue;

                        var item = rentingPlayer.Inventory[i];

                        if (item?.RentalInformation == null)
                            continue;

                        if (Now <= item.RentalInformation.ExpiryDate)
                            continue;

                        ReturnRentalItem(item, item.RentalInformation.OwnerName, rentingPlayer, false);
                        rentingPlayer.Inventory[i] = null;
                        rentingPlayer.HasRentedItem = false;

                        if (rentingPlayer.Player == null)
                            continue;

                        rentingPlayer.Player.ReceiveChat($"{item.Info.FriendlyName} has just expired from your inventory.", ChatType.Hint);
                        rentingPlayer.Player.Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                        rentingPlayer.Player.RefreshStats();
                    }

                    for (var i = 0; i < rentingPlayer.Equipment.Length; i++)
                    {
                        var item = rentingPlayer.Equipment[i];

                        if (item?.RentalInformation == null)
                            continue;

                        if (Now <= item.RentalInformation.ExpiryDate)
                            continue;

                        ReturnRentalItem(item, item.RentalInformation.OwnerName, rentingPlayer, false);
                        rentingPlayer.Equipment[i] = null;
                        rentingPlayer.HasRentedItem = false;

                        if (rentingPlayer.Player == null)
                            continue;

                        rentingPlayer.Player.ReceiveChat($"{item.Info.FriendlyName} has just expired from your inventory.", ChatType.Hint);
                        rentingPlayer.Player.Enqueue(new S.DeleteItem { UniqueID = item.UniqueID, Count = item.Count });
                        rentingPlayer.Player.RefreshStats();
                    }
                }
            }

            foreach (var characterInfo in CharacterList)
            {
                if (characterInfo.RentedItemsToRemove.Count <= 0)
                    continue;

                foreach (var rentalInformationToRemove in characterInfo.RentedItemsToRemove)
                    characterInfo.RentedItems.Remove(rentalInformationToRemove);

                characterInfo.RentedItemsToRemove.Clear();
            }
        }

        public bool ReturnRentalItem(UserItem rentedItem, string ownerName, CharacterInfo rentingCharacterInfo, bool removeNow = true)
        {
            if (rentedItem.RentalInformation == null)
                return false;

            var owner = GetCharacterInfo(ownerName);
            var returnItems = new List<UserItem>();

            foreach (var rentalInformation in owner.RentedItems)
                if (rentalInformation.ItemId == rentedItem.UniqueID)
                    owner.RentedItemsToRemove.Add(rentalInformation);

            rentedItem.RentalInformation.BindingFlags = BindMode.none;
            rentedItem.RentalInformation.RentalLocked = true;
            rentedItem.RentalInformation.ExpiryDate = rentedItem.RentalInformation.ExpiryDate.AddDays(1);

            returnItems.Add(rentedItem);

            var mail = new MailInfo(owner.Index, true)
            {
                Sender = rentingCharacterInfo.Name,
                Message = rentedItem.Info.FriendlyName,
                Items = returnItems
            };

            mail.Send();

            if (removeNow)
            {
                foreach (var rentalInformationToRemove in owner.RentedItemsToRemove)
                    owner.RentedItems.Remove(rentalInformationToRemove);

                owner.RentedItemsToRemove.Clear();
            }

            return true;
        }

        private void ClearDailyQuests(CharacterInfo info)
        {
            foreach (var quest in QuestInfoList)
            {
                if (quest.Type != QuestType.Daily) continue;

                for (int i = 0; i < info.CompletedQuests.Count; i++)
                {
                    if (info.CompletedQuests[i] != quest.Index) continue;

                    info.CompletedQuests.RemoveAt(i);
                }
            }

            if (info.Player != null)
            {
                info.Player.GetCompletedQuests();
            }
        }

        public GuildBuffInfo FindGuildBuffInfo(int Id)
        {
            for (int i = 0; i < Settings.Guild_BuffList.Count; i++)
                if (Settings.Guild_BuffList[i].Id == Id)
                    return Settings.Guild_BuffList[i];
            return null;
        }

        public void ClearGameshopLog()
        {
            SMain.Envir.GameshopLog.Clear();

            for (int i = 0; i < AccountList.Count; i++)
            {
                for (int f = 0; f < AccountList[i].Characters.Count; f++)
                {
                    AccountList[i].Characters[f].GSpurchases.Clear();
                }
            }

            ResetGS = false;
            SMain.Enqueue("Gameshop Purchase Logs Cleared.");

        }

    }
}

