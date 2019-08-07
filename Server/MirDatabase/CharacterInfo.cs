using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Server.MirEnvir;
using Server.MirNetwork;
using Server.MirObjects;
using System.Windows.Forms;
using System.Data.SQLite;
using Newtonsoft.Json;
using System.Data.Common;

namespace Server.MirDatabase
{   //角色信息
    public class CharacterInfo
    {
        public ulong accIndex;//账号的索引，ID,增加的关联字段
        public ulong Index;
        public string Name;
        public ushort Level=1;//默认等级
        public MirClass Class;
        public MirGender Gender;
        public byte Hair;//发型
        public long GuildIndex = -1;

        public string CreationIP;
        public DateTime CreationDate;

        //这里判断游戏是否禁止开始
        public bool Banned;
        public string BanReason = string.Empty;
        public DateTime ExpiryDate;

        public bool ChatBanned;
        public DateTime ChatBanExpiryDate;

        public string LastIP = string.Empty;
        public DateTime LastDate;//最后退出游戏的时间
        public DateTime LastLoginDate;//最后登录的时间

        public bool Deleted;
        public DateTime DeleteDate;

        public ListViewItem ListItem;

        //Marriage
        public ulong Married = 0;
        public DateTime MarriedDate;

        //Mentor
        public ulong Mentor = 0;
        public DateTime MentorDate;
        public bool isMentor;
        public long MentorExp = 0;

        //Location
        public int CurrentMapIndex;
        public Point CurrentLocation;
        public MirDirection Direction;
        public int BindMapIndex;
        public Point BindLocation;

        public ushort HP, MP;
        public long Experience;
        public AttackMode AMode;
        public PetMode PMode;
        public bool AllowGroup;
        public bool AllowTrade=true;//是否允许交易，默认是允许的

        public int PKPoints;

        public bool NewDay;

        public bool Thrusting, HalfMoon, CrossHalfMoon;
        public bool DoubleSlash;
        public byte MentalState;
        public byte MentalStateLvl;
        //背包（包括背包的40格+下面的6个按键格,其中0-5是下面的6格按键），装备，交易，仓库，精炼(改善，这个不知道是扩展背包还是什么)
        public UserItem[] Inventory = new UserItem[46], Equipment = new UserItem[14], Trade = new UserItem[10], QuestInventory = new UserItem[40], Refine = new UserItem[16];

        public UserItem[] ItemCollect = new UserItem[10];//这个是收集的客户端提交上来的物品

        //契约兽，10个契约兽
        public MyMonster[] MyMonsters = new MyMonster[10];//10个契约兽


        public List<ItemRentalInformation> RentedItems = new List<ItemRentalInformation>();
        public List<ItemRentalInformation> RentedItemsToRemove = new List<ItemRentalInformation>();
        public bool HasRentedItem;
        public UserItem CurrentRefine = null;//当前升级物品，目前只有武器
        public UserItem SaItem = null;//当前轮回物品，几乎所有装备都支持轮回
        public byte SaItemType;//轮回类型 0：默认轮回副本轮回 1：魔龙轮回 2：狐狸轮回 3：月氏轮回
        public long CollectTime = 0;
        public List<UserMagic> Magics = new List<UserMagic>();
        public List<PetInfo> Pets = new List<PetInfo>();
        public List<Buff> Buffs = new List<Buff>();
        //这个应该没用了？
        public List<Poison> Poisons = new List<Poison>();

        public List<MailInfo> Mail = new List<MailInfo>();
        public List<FriendInfo> Friends = new List<FriendInfo>();

        //IntelligentCreature
        public List<UserIntelligentCreature> IntelligentCreatures = new List<UserIntelligentCreature>();
        public int PearlCount;

        public List<QuestProgressInfo> CurrentQuests = new List<QuestProgressInfo>();
        public List<int> CompletedQuests = new List<int>();

        //这个是各种标志位，主要做任务标识的
        public bool[] Flags = new bool[Globals.FlagIndexCount];
        //账号信息
        [JsonIgnore]
        public AccountInfo AccountInfo;
        //玩家
        [JsonIgnore]
        public PlayerObject Player;
        [JsonIgnore]
        public MountInfo Mount;

        //这个记录的是当前角色拥有某件物品的数量(主要是商城的中的物品)
        public Dictionary<int, int> GSpurchases = new Dictionary<int, int>();
        //这个作废掉
        public int[] Rank = new int[2];//dont save this in db!(and dont send it to clients :p)

        public int[] killMon = new int[10];//杀怪记录
        public Dictionary<int, int> killMon2 = new Dictionary<int, int>();//杀怪记录,所有怪物的记录哦

        //这个记录各种手工实现的逻辑(这个是保存到数据库的)
        public Dictionary<string, int> saveKey = new Dictionary<string, int>();
        //这个记录各种手工实现的逻辑（这个是零时的，重启后无效的）
        public Dictionary<string, int> tempKey = new Dictionary<string, int>();

        //角色在线时长
        public uint onlineTime = 0;//在线时长，每天的
        public int onlineDay = 0;//当前日期

        public uint offlineTime = 0;//累计的（这个不存储到数据库）

        //记录地狱副本的层级，得分，总用时,创建时间（日）
        private int fb1_level;
        private int fb1_score;
        private int fb1_usetime;
        private int fb1_createday;
        //增加天榜积分(5天掉100分)，积分规则，每赢1场，增加100分，输1场掉100分,
        //如果积分大于500，则赢一场增加90分，如果积分大于1000，则赢1场增加80分，积分大于1500，则赢1场增加70积分
        private int fb2_score;
        private int fb2_addscore;//这个是累计积分，不会掉的，可以用累计积分兑换物品
        private int fb2_createday;

        //获取副本1的最终积分
        public int getFb1_score()
        {
            int day = SMain.Envir.Now.DayOfYear;
            if(fb1_createday> day)
            {
                fb1_createday = day-100;
            }
            int _fb1_score = fb1_score - (day - fb1_createday-3) * 100;
            if (_fb1_score> fb1_score)
            {
                return fb1_score;
            }
            if (_fb1_score < 0)
            {
                return 0;
            }
            return _fb1_score;
        }

        public void setFb1_score(int _score)
        {
            fb1_createday = SMain.Envir.Now.DayOfYear;
            fb1_score = _score;
        }

        //获取副本2的最终积分
        public int getFb2_score()
        {
            int day = SMain.Envir.Now.DayOfYear;
            if (fb2_createday > day)
            {
                fb2_createday = day - 100;
            }
            int _fb2_score = fb2_score - (day - fb2_createday - 5) * 100;
            if (_fb2_score > fb2_score)
            {
                return fb2_score;
            }
            if (_fb2_score < 0)
            {
                return 0;
            }
            return _fb2_score;
        }


        /// <summary>
        /// 追加积分，这里可能是负数的
        /// </summary>
        /// <param name="_score"></param>
        public void addFb2_score(int _score)
        {
            int curr_score = getFb2_score();
            curr_score += _score;
            if (curr_score < 0)
            {
                curr_score = 0;
            }
            fb2_createday = SMain.Envir.Now.DayOfYear;
            fb2_score = curr_score;
            if (_score > 0)
            {
                fb2_addscore += _score;
            }
        }


        public CharacterInfo()
        {

        }

        public CharacterInfo(ClientPackets.NewCharacter p, MirConnection c)
        {
            Name = p.Name;
            Class = p.Class;
            Gender = p.Gender;

            CreationIP = c.IPAddress;
            CreationDate = SMain.Envir.Now;
        }
        
    
        /// <summary>
        /// 查询所有角色信息
        /// </summary>
        /// <returns></returns>
        public static List<CharacterInfo> loadAll()
        {
            List<CharacterInfo> list = new List<CharacterInfo>();
            DbDataReader read = MirRunDB.ExecuteReader("select * from CharacterInfo ");
            while (read.Read())
            {
                CharacterInfo obj = new CharacterInfo();
                obj.Index= (ulong)read.GetInt64(read.GetOrdinal("Idx"));
                obj.accIndex = (ulong)read.GetInt64(read.GetOrdinal("accIndex"));
                obj.Name = read.GetString(read.GetOrdinal("Name"));
                obj.Level = (ushort)read.GetInt16(read.GetOrdinal("Level"));
                obj.Class = (MirClass)read.GetInt16(read.GetOrdinal("Class"));
                obj.Gender = (MirGender)read.GetInt16(read.GetOrdinal("Gender"));

                obj.Hair = read.GetByte(read.GetOrdinal("Hair"));
                obj.CreationIP = read.GetString(read.GetOrdinal("CreationIP"));
                obj.CreationDate = read.GetDateTime(read.GetOrdinal("CreationDate"));
                obj.Banned = read.GetBoolean(read.GetOrdinal("Banned"));
                obj.BanReason = read.GetString(read.GetOrdinal("BanReason"));
                obj.ExpiryDate = read.GetDateTime(read.GetOrdinal("ExpiryDate"));
                obj.LastIP = read.GetString(read.GetOrdinal("LastIP"));
                obj.LastDate = read.GetDateTime(read.GetOrdinal("LastDate"));
                obj.Deleted = read.GetBoolean(read.GetOrdinal("Deleted"));
                obj.DeleteDate = read.GetDateTime(read.GetOrdinal("DeleteDate"));
                obj.CurrentMapIndex = read.GetInt32(read.GetOrdinal("CurrentMapIndex"));
                obj.CurrentLocation = new Point(read.GetInt32(read.GetOrdinal("CurrentLocation_X")), read.GetInt32(read.GetOrdinal("CurrentLocation_Y")));
                obj.Direction = (MirDirection)read.GetByte(read.GetOrdinal("Direction"));
                obj.BindMapIndex = read.GetInt32(read.GetOrdinal("BindMapIndex"));
                obj.BindLocation = new Point(read.GetInt32(read.GetOrdinal("BindLocation_X")), read.GetInt32(read.GetOrdinal("BindLocation_Y")));

                obj.HP = (ushort)read.GetInt32(read.GetOrdinal("HP"));
                obj.MP = (ushort)read.GetInt32(read.GetOrdinal("MP"));
                obj.Experience = read.GetInt64(read.GetOrdinal("Experience"));
                obj.AMode = (AttackMode)read.GetByte(read.GetOrdinal("AMode"));

                
                obj.PMode = (PetMode)read.GetByte(read.GetOrdinal("PMode"));
                obj.PKPoints = read.GetInt32(read.GetOrdinal("PKPoints"));
                //扩展背包的容量
                int Inventory_len = read.GetInt32(read.GetOrdinal("Inventory_len"));
                obj.Inventory = JsonConvert.DeserializeObject<UserItem[]>(read.GetString(read.GetOrdinal("Inventory")));
                Array.Resize(ref obj.Inventory, Inventory_len);
                for (int i = 0; i < obj.Inventory.Length; i++)
                {
                    if (obj.Inventory[i] != null)
                    {
                        //全部取消结婚戒指
                        obj.Inventory[i].WeddingRing = -1;
                        if (!obj.Inventory[i].BindItem())
                        {
                            obj.Inventory[i] = null;
                        }
                    }
                }
                //装备栏
                obj.Equipment = JsonConvert.DeserializeObject<UserItem[]>(read.GetString(read.GetOrdinal("Equipment")));
                for (int i = 0; i < obj.Equipment.Length; i++)
                {
                    if (obj.Equipment[i] != null)
                    {
                        //全部取消结婚戒指
                        obj.Equipment[i].WeddingRing = -1;
                        if (!obj.Equipment[i].BindItem())
                        {
                            obj.Equipment[i] = null;
                        }
                    }
                }
                //仓库
                obj.QuestInventory = JsonConvert.DeserializeObject<UserItem[]>(read.GetString(read.GetOrdinal("QuestInventory")));
                for (int i = 0; i < obj.QuestInventory.Length; i++)
                {
                    if (obj.QuestInventory[i] != null)
                    {
                        if (!obj.QuestInventory[i].BindItem())
                        {
                            obj.QuestInventory[i] = null;
                        }
                    }
                }
                //收集上来的临时装备，需要归还给玩家的
                if (!read.IsDBNull(read.GetOrdinal("ItemCollect")))
                {
                    obj.ItemCollect = JsonConvert.DeserializeObject<UserItem[]>(read.GetString(read.GetOrdinal("ItemCollect")));
                    for (int i = 0; i < obj.ItemCollect.Length; i++)
                    {
                        if (obj.ItemCollect[i] != null)
                        {
                            if (!obj.ItemCollect[i].BindItem())
                            {
                                obj.ItemCollect[i] = null;
                            }
                        }
                    }
                }

                //契约兽
                if (!read.IsDBNull(read.GetOrdinal("MyMonsters")))
                {
                    obj.MyMonsters = JsonConvert.DeserializeObject<MyMonster[]>(read.GetString(read.GetOrdinal("MyMonsters")));
                }
                

                //魔法技能
                obj.Magics = JsonConvert.DeserializeObject<List<UserMagic>>(read.GetString(read.GetOrdinal("Magics")));
                for (int i = 0; i < obj.Magics.Count; i++)
                {
                    if (!obj.Magics[i].BindInfo())
                    {
                        obj.Magics.RemoveAt(i);
                        i--;
                        continue;
                    }
                    obj.Magics[i].CastTime = 0;
                }

                
                obj.Thrusting = read.GetBoolean(read.GetOrdinal("Thrusting"));//开启刺杀
                //obj.Thrusting = true;//默认都是开启的,这个不行哦，别的职业不能开启
                obj.HalfMoon = read.GetBoolean(read.GetOrdinal("HalfMoon"));
                obj.CrossHalfMoon = read.GetBoolean(read.GetOrdinal("CrossHalfMoon"));
                obj.DoubleSlash = read.GetBoolean(read.GetOrdinal("DoubleSlash"));
                obj.MentalState = read.GetByte(read.GetOrdinal("MentalState"));
 
                obj.Pets = JsonConvert.DeserializeObject<List<PetInfo>>(read.GetString(read.GetOrdinal("Pets")));
                obj.AllowGroup = read.GetBoolean(read.GetOrdinal("AllowGroup"));
                obj.Flags = JsonConvert.DeserializeObject<bool[]>(read.GetString(read.GetOrdinal("Flags")));

                obj.GuildIndex = read.GetInt64(read.GetOrdinal("GuildIndex"));
                obj.AllowTrade = read.GetBoolean(read.GetOrdinal("AllowTrade"));
                if (!read.IsDBNull(read.GetOrdinal("CurrentQuests")))
                {
                    obj.CurrentQuests = JsonConvert.DeserializeObject<List<QuestProgressInfo>>(read.GetString(read.GetOrdinal("CurrentQuests")));
                    if (obj.CurrentQuests != null)
                    {
                        for (int i = 0; i < obj.CurrentQuests.Count; i++)
                        {
                            if (!SMain.Envir.BindQuest(obj.CurrentQuests[i]))
                            {
                                obj.CurrentQuests.Remove(obj.CurrentQuests[i]);
                            }
                        }
                    }
                }
                
                

                obj.Buffs = JsonConvert.DeserializeObject<List<Buff>>(read.GetString(read.GetOrdinal("Buffs")));
                if (!read.IsDBNull(read.GetOrdinal("Mail")))
                {
                    obj.Mail = JsonConvert.DeserializeObject<List<MailInfo>>(read.GetString(read.GetOrdinal("Mail")));
                    for (int i = 0; i < obj.Mail.Count; i++)
                    {
                        obj.Mail[i].BindItems();
                    }
                }

                if (!read.IsDBNull(read.GetOrdinal("IntelligentCreatures")))
                {
                    obj.IntelligentCreatures = JsonConvert.DeserializeObject<List<UserIntelligentCreature>>(read.GetString(read.GetOrdinal("IntelligentCreatures")));
                }
              
                obj.PearlCount = read.GetInt32(read.GetOrdinal("PearlCount"));
                obj.CompletedQuests = JsonConvert.DeserializeObject<List<int>>(read.GetString(read.GetOrdinal("CompletedQuests")));

           
               
                
                //武器升级物品
                obj.CurrentRefine = JsonConvert.DeserializeObject<UserItem>(read.GetString(read.GetOrdinal("CurrentRefine")));
                if (obj.CurrentRefine != null)
                {
                    if (!obj.CurrentRefine.BindItem())
                    {
                        obj.CurrentRefine = null;
                    }
                }
                if (!read.IsDBNull(read.GetOrdinal("SaItem")))
                {
                    obj.SaItem = JsonConvert.DeserializeObject<UserItem>(read.GetString(read.GetOrdinal("SaItem")));
                    if (obj.SaItem != null)
                    {
                        if (!obj.SaItem.BindItem())
                        {
                            obj.SaItem = null;
                        }
                    }
                }
                if (!read.IsDBNull(read.GetOrdinal("SaItemType")))
                {
                    obj.SaItemType = read.GetByte(read.GetOrdinal("SaItemType"));
                }
                

                if (!read.IsDBNull(read.GetOrdinal("killMon2")))
                {
                    obj.killMon2 = JsonConvert.DeserializeObject<Dictionary<int, int>>(read.GetString(read.GetOrdinal("killMon2")));
                }

                
                obj.CollectTime = read.GetInt64(read.GetOrdinal("CollectTime"));
                //这里的时间要重置的
                obj.CollectTime += SMain.Envir.Time;

                if (!read.IsDBNull(read.GetOrdinal("Friends")))
                {
                    obj.Friends = JsonConvert.DeserializeObject<List<FriendInfo>>(read.GetString(read.GetOrdinal("Friends")));
                }
                   

                obj.RentedItems = JsonConvert.DeserializeObject<List<ItemRentalInformation>>(read.GetString(read.GetOrdinal("RentedItems")));
                obj.HasRentedItem = read.GetBoolean(read.GetOrdinal("HasRentedItem"));
                obj.Married = (ulong)read.GetInt64(read.GetOrdinal("Married"));
                obj.MarriedDate = read.GetDateTime(read.GetOrdinal("MarriedDate"));
                obj.Mentor = (ulong)read.GetInt64(read.GetOrdinal("Mentor"));
                obj.MentorDate = read.GetDateTime(read.GetOrdinal("MentorDate"));
                obj.isMentor = read.GetBoolean(read.GetOrdinal("isMentor"));
                obj.MentorExp = (long)read.GetInt64(read.GetOrdinal("MentorExp"));
                obj.GSpurchases = JsonConvert.DeserializeObject<Dictionary<int, int>>(read.GetString(read.GetOrdinal("GSpurchases")));
                //添加杀怪记录
                if (!read.IsDBNull(read.GetOrdinal("killMon")))
                {
                    string killMons = read.GetString(read.GetOrdinal("killMon"));
                    if(killMons!=null && killMons.Length > 5)
                    {
                        obj.killMon = JsonConvert.DeserializeObject<int[]>(killMons);
                    }
                }
                //这个是各种key记录
                if (!read.IsDBNull(read.GetOrdinal("saveKey")))
                {
                    string saveKey = read.GetString(read.GetOrdinal("saveKey"));
                    if (saveKey != null && saveKey.Length > 5)
                    {
                        obj.saveKey = JsonConvert.DeserializeObject<Dictionary<string, int>>(saveKey);
                    }
                }

                obj.fb1_level = read.GetInt32(read.GetOrdinal("fb1_level"));
                obj.fb1_score = read.GetInt32(read.GetOrdinal("fb1_score"));
                obj.fb1_usetime = read.GetInt32(read.GetOrdinal("fb1_usetime"));
                obj.fb1_createday = read.GetInt32(read.GetOrdinal("fb1_createday"));

                obj.fb2_score = read.GetInt32(read.GetOrdinal("fb2_score"));
                obj.fb2_addscore = read.GetInt32(read.GetOrdinal("fb2_addscore"));
                
                obj.fb2_createday = read.GetInt32(read.GetOrdinal("fb2_createday"));
                
                //添加2个字段
                //obj.onlineTime = (uint)read.GetInt32(read.GetOrdinal("onlineTime"));
                //obj.onlineDay = read.GetInt32(read.GetOrdinal("onlineDay"));
                //
                obj.RetrieveItem();
                DBObjectUtils.updateObjState(obj, obj.Index);
                list.Add(obj);
            }
            return list;
        }

        //归还物品，针对之前被放入收集窗口的装备，重新归还给到账号
        //重启的时候要做一次归还处理
        public void RetrieveItem()
        {
            //精炼物品归还
            for(int i=0;i < ItemCollect.Length; i++)
            {
                if (ItemCollect[i] != null)
                {
                    for(int j=0;j< Inventory.Length; j++)
                    {
                        if (Inventory[j] == null)
                        {
                            Inventory[j] = ItemCollect[i];
                            ItemCollect[i] = null;
                            break;
                        }
                    }
                }
            }
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
            lp.Add(new SQLiteParameter("accIndex", AccountInfo.Index));
            lp.Add(new SQLiteParameter("Level", Level));
            lp.Add(new SQLiteParameter("Class", (byte)Class));
            lp.Add(new SQLiteParameter("Gender", (byte)Gender));
            lp.Add(new SQLiteParameter("Hair", Hair));
            lp.Add(new SQLiteParameter("CreationIP", CreationIP));
            lp.Add(new SQLiteParameter("CreationDate", CreationDate));
            lp.Add(new SQLiteParameter("Banned", Banned));
            lp.Add(new SQLiteParameter("BanReason", BanReason));
            lp.Add(new SQLiteParameter("ExpiryDate", ExpiryDate));
            lp.Add(new SQLiteParameter("LastIP", LastIP));
            lp.Add(new SQLiteParameter("LastDate", LastDate));
            lp.Add(new SQLiteParameter("Deleted", Deleted));
            lp.Add(new SQLiteParameter("DeleteDate", DeleteDate));
            lp.Add(new SQLiteParameter("CurrentMapIndex", CurrentMapIndex));
            lp.Add(new SQLiteParameter("CurrentLocation_X", CurrentLocation.X));
            lp.Add(new SQLiteParameter("CurrentLocation_Y", CurrentLocation.Y));
            lp.Add(new SQLiteParameter("Direction", Direction));
            lp.Add(new SQLiteParameter("BindMapIndex", BindMapIndex));
            lp.Add(new SQLiteParameter("BindLocation_X", BindLocation.X));
            lp.Add(new SQLiteParameter("BindLocation_Y", BindLocation.Y));
            lp.Add(new SQLiteParameter("HP", HP));
            lp.Add(new SQLiteParameter("MP", MP));
            lp.Add(new SQLiteParameter("Experience", Experience));
            lp.Add(new SQLiteParameter("AMode", (byte)AMode));

            lp.Add(new SQLiteParameter("PMode", (byte)PMode));
            lp.Add(new SQLiteParameter("PKPoints", PKPoints));
            
            lp.Add(new SQLiteParameter("Inventory", JsonConvert.SerializeObject(Inventory)));
            lp.Add(new SQLiteParameter("Inventory_len", Inventory.Length));//背包的容量
            lp.Add(new SQLiteParameter("Equipment", JsonConvert.SerializeObject(Equipment)));
            lp.Add(new SQLiteParameter("QuestInventory", JsonConvert.SerializeObject(QuestInventory)));
            lp.Add(new SQLiteParameter("Magics", JsonConvert.SerializeObject(Magics)));

            lp.Add(new SQLiteParameter("Thrusting", Thrusting));
            lp.Add(new SQLiteParameter("HalfMoon", HalfMoon));
            lp.Add(new SQLiteParameter("CrossHalfMoon", CrossHalfMoon));
            lp.Add(new SQLiteParameter("DoubleSlash", DoubleSlash));
            lp.Add(new SQLiteParameter("MentalState", MentalState));

            lp.Add(new SQLiteParameter("Pets", JsonConvert.SerializeObject(Pets)));
   
            lp.Add(new SQLiteParameter("AllowGroup", AllowGroup));

            lp.Add(new SQLiteParameter("Flags", JsonConvert.SerializeObject(Flags)));

            lp.Add(new SQLiteParameter("GuildIndex", GuildIndex));
            lp.Add(new SQLiteParameter("AllowTrade", AllowTrade));
            lp.Add(new SQLiteParameter("CurrentQuests", JsonConvert.SerializeObject(CurrentQuests)));
            lp.Add(new SQLiteParameter("Buffs", JsonConvert.SerializeObject(Buffs)));
            lp.Add(new SQLiteParameter("Mail", JsonConvert.SerializeObject(Mail)));

            lp.Add(new SQLiteParameter("IntelligentCreatures", JsonConvert.SerializeObject(IntelligentCreatures)));
            
            lp.Add(new SQLiteParameter("PearlCount", PearlCount));
            lp.Add(new SQLiteParameter("CompletedQuests", JsonConvert.SerializeObject(CompletedQuests)));
      
            

            lp.Add(new SQLiteParameter("CurrentRefine", JsonConvert.SerializeObject(CurrentRefine)));

            if ((CollectTime - SMain.Envir.Time) < 0)
                CollectTime = 0;
            else
                CollectTime = CollectTime - SMain.Envir.Time;


            lp.Add(new SQLiteParameter("CollectTime", CollectTime));
            lp.Add(new SQLiteParameter("Friends", JsonConvert.SerializeObject(Friends)));

            lp.Add(new SQLiteParameter("RentedItems", JsonConvert.SerializeObject(RentedItems)));
            lp.Add(new SQLiteParameter("ItemCollect", JsonConvert.SerializeObject(ItemCollect)));
            
            lp.Add(new SQLiteParameter("HasRentedItem", HasRentedItem));
            lp.Add(new SQLiteParameter("Married", Married));
            lp.Add(new SQLiteParameter("MarriedDate", MarriedDate));
            lp.Add(new SQLiteParameter("Mentor", Mentor));
            lp.Add(new SQLiteParameter("MentorDate", MentorDate));
            lp.Add(new SQLiteParameter("isMentor", isMentor));
            lp.Add(new SQLiteParameter("MentorExp", MentorExp));
            lp.Add(new SQLiteParameter("GSpurchases", JsonConvert.SerializeObject(GSpurchases)));
            lp.Add(new SQLiteParameter("killMon", JsonConvert.SerializeObject(killMon)));
            lp.Add(new SQLiteParameter("saveKey", JsonConvert.SerializeObject(saveKey)));
            lp.Add(new SQLiteParameter("SaItem", JsonConvert.SerializeObject(SaItem)));
            lp.Add(new SQLiteParameter("SaItemType", SaItemType));//轮回物品的类型
            
            lp.Add(new SQLiteParameter("fb1_level", fb1_level));
            lp.Add(new SQLiteParameter("fb1_score", fb1_score));
            lp.Add(new SQLiteParameter("fb1_usetime", fb1_usetime));
            lp.Add(new SQLiteParameter("fb1_createday", fb1_createday));

            lp.Add(new SQLiteParameter("fb2_score", fb2_score));
            lp.Add(new SQLiteParameter("fb2_createday", fb2_createday));
            lp.Add(new SQLiteParameter("fb2_addscore", fb2_addscore));
            
            lp.Add(new SQLiteParameter("killMon2", JsonConvert.SerializeObject(killMon2)));
            //契约兽
            lp.Add(new SQLiteParameter("MyMonsters", JsonConvert.SerializeObject(MyMonsters)));
            

            //lp.Add(new SQLiteParameter("onlineTime", onlineTime));
            //lp.Add(new SQLiteParameter("onlineDay", onlineDay));
            //新增
            if (state == 1)
            {
                if (Index > 0)
                {
                    lp.Add(new SQLiteParameter("Idx", Index));
                }
                string sql = "insert into CharacterInfo" + SQLiteHelper.createInsertSql(lp.ToArray());
                MirRunDB.Execute(sql, lp.ToArray());
            }
            //修改
            if (state == 2)
            {
                string sql = "update CharacterInfo set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where Idx=@Idx";
                lp.Add(new SQLiteParameter("Idx", Index));
                MirRunDB.Execute(sql, lp.ToArray());
            }
            DBObjectUtils.updateObjState(this, Index);
        }

        //获取零时值,默认0
        public int getTempValue(string key)
        {
            if (tempKey.ContainsKey(key))
            {
                return tempKey[key];
            }
            else
            {
                return 0;
            }
        }

        //更新零时值
        public void putTempValue(string key ,int val)
        {
            if (tempKey.ContainsKey(key))
            {
                tempKey[key]=val;
            }
            else
            {
                tempKey.Add(key,val);
            }
        }

        //获取存储值,默认0
        public int getSaveValue(string key)
        {
            if (saveKey.ContainsKey(key))
            {
                return saveKey[key];
            }
            else
            {
                return 0;
            }
        }

        //更新存储值
        public void putSaveValue(string key, int val)
        {
            if (saveKey.ContainsKey(key))
            {
                saveKey[key] = val;
            }
            else
            {
                saveKey.Add(key, val);
            }
        }

        public ListViewItem CreateListView()
        {
            if (ListItem != null)
                ListItem.Remove();

            ListItem = new ListViewItem(Index.ToString()) { Tag = this };

            ListItem.SubItems.Add(Name);
            ListItem.SubItems.Add(Level.ToString());
            ListItem.SubItems.Add(Class.ToString());
            ListItem.SubItems.Add(Gender.ToString());

            return ListItem;
        }

        public SelectInfo ToSelectInfo()
        {
            return new SelectInfo
                {
                    Index = Index,
                    Name = Name,
                    Level = Level,
                    Class = Class,
                    Gender = Gender,
                    LastAccess = LastDate
                };
        }

        public bool CheckHasIntelligentCreature(IntelligentCreatureType petType)
        {
            for (int i = 0; i < IntelligentCreatures.Count; i++)
                if (IntelligentCreatures[i].PetType == petType) return true;
            return false;
        }
        public int ResizeInventory()
        {
            if (Inventory.Length >= 86) return Inventory.Length;

            if (Inventory.Length == 46)
                Array.Resize(ref Inventory, Inventory.Length + 8);
            else
                Array.Resize(ref Inventory, Inventory.Length + 4);

            return Inventory.Length;
        }

        //计算获取当前玩家的离线经验
        public uint OffLineExp()
        {



            return 0;
        }
        


    }
    //宠物
    public class PetInfo
    {
        public int MonsterIndex;
        public uint HP, Experience;
        public byte Level, MaxPetLevel;

        public long Time;

        public PetInfo()
        {

        }

        public PetInfo(MonsterObject ob)
        {
            MonsterIndex = ob.Info.Index;
            HP = ob.HP;
            Experience = ob.PetExperience;
            Level = ob.PetLevel;
            MaxPetLevel = ob.MaxPetLevel;
        }

        public PetInfo(BinaryReader reader)
        {
            MonsterIndex = reader.ReadInt32();
            if (MonsterIndex == 271) MonsterIndex = 275;
            HP = reader.ReadUInt32();
            Experience = reader.ReadUInt32();
            Level = reader.ReadByte();
            MaxPetLevel = reader.ReadByte();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(MonsterIndex);
            writer.Write(HP);
            writer.Write(Experience);
            writer.Write(Level);
            writer.Write(MaxPetLevel);
        }
    }
    //坐骑
    public class MountInfo
    {
        public PlayerObject Player;
        public short MountType = -1;

        public bool CanRide
        {
            get { return HasMount && Slots[(int)MountSlot.Saddle] != null; }
        }
        public bool CanMapRide
        {
            get { return HasMount && !Player.CurrentMap.Info.NoMount; }
        }
        public bool CanDungeonRide
        {
            get { return HasMount && CanMapRide && (!Player.CurrentMap.Info.NeedBridle || Slots[(int)MountSlot.Reins] != null); }
        }
        public bool CanAttack
        {
            get { return HasMount && Slots[(int)MountSlot.Bells] != null || !RidingMount; }
        }
        public bool SlowLoyalty
        {
            get { return HasMount && Slots[(int)MountSlot.Ribbon] != null; }
        }

        public bool HasMount
        {
            get { return Player.Info.Equipment[(int)EquipmentSlot.Mount] != null; }
        }

        private bool RidingMount
        {
            get { return Player.RidingMount; }
            set { Player.RidingMount = value; }
        }

        public UserItem[] Slots
        {
            get { return Player.Info.Equipment[(int)EquipmentSlot.Mount].Slots; }
        }


        public MountInfo(PlayerObject ob)
        {
            Player = ob;
        }
    }
    //好友
    public class FriendInfo
    {
        public ulong Index;

        [JsonIgnore]
        private CharacterInfo _Info;
        [JsonIgnore]
        public CharacterInfo Info
        {
            get 
            {
                if (_Info == null) 
                    _Info = SMain.Envir.GetCharacterInfo(Index);

                return _Info;
            }
        }

        public bool Blocked;
        public string Memo;

        public FriendInfo()
        {

        }

        public FriendInfo(CharacterInfo info, bool blocked) 
        {
            Index = info.Index;
            Blocked = blocked;
            Memo = "";
        }

        public FriendInfo(BinaryReader reader)
        {
            Index = (ulong)reader.ReadInt32();
            Blocked = reader.ReadBoolean();
            Memo = reader.ReadString();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(Blocked);
            writer.Write(Memo);
        }

        public ClientFriend CreateClientFriend()
        {
            if (Info == null)
            {
                return null;
            }
            return new ClientFriend()
            {
                Index = Index,
                Name = Info.Name,
                Blocked = Blocked,
                Memo = Memo,
                Online = Info.Player != null && Info.Player.Node != null
            };
        }
    }

    public class IntelligentCreatureInfo
    {
        public static List<IntelligentCreatureInfo> Creatures = new List<IntelligentCreatureInfo>();

        public static IntelligentCreatureInfo BabyPig,
                                                Chick,
                                                Kitten,
                                                BabySkeleton,
                                                Baekdon,
                                                Wimaen,
                                                BlackKitten,
                                                BabyDragon,
                                                OlympicFlame,
                                                BabySnowMan,
                                                Frog,
                                                Monkey,
                                                AngryBird,
                                                Foxey;

        public IntelligentCreatureType PetType;

        public int Icon;
        public int MinimalFullness = 1000;

        public bool MousePickupEnabled = false;
        public int MousePickupRange = 0;
        public bool AutoPickupEnabled = false;
        public int AutoPickupRange = 0;
        public bool SemiAutoPickupEnabled = false;
        public int SemiAutoPickupRange = 0;

        public bool CanProduceBlackStone = false;

        public string Info = "";
        public string Info1 = "Unable to produce BlackStones.";
        public string Info2 = "Can produce Pearls, used to buy Creature items.";

        static IntelligentCreatureInfo()
        {
            BabyPig = new IntelligentCreatureInfo { PetType = IntelligentCreatureType.BabyPig, Icon = 500, SemiAutoPickupEnabled = true, SemiAutoPickupRange = 3, MinimalFullness = 4000, Info = "Can pickup items (3x3 semi-auto)." };
            Chick = new IntelligentCreatureInfo { PetType = IntelligentCreatureType.Chick, Icon = 501, MousePickupEnabled = true, MousePickupRange = 11, AutoPickupEnabled = true, AutoPickupRange = 7, SemiAutoPickupEnabled = true, SemiAutoPickupRange = 7, CanProduceBlackStone = true, Info = "Can pickup items (7x7 auto/semi-auto, 11x11 mouse).", Info1 = "Can produce BlackStones." };
            Kitten = new IntelligentCreatureInfo { PetType = IntelligentCreatureType.Kitten, Icon = 502, SemiAutoPickupEnabled = true, SemiAutoPickupRange = 3, MinimalFullness = 6000, Info = "Can pickup items (5x5 semi-auto)." };
            BabySkeleton = new IntelligentCreatureInfo { PetType = IntelligentCreatureType.BabySkeleton, Icon = 503, MousePickupEnabled = true, MousePickupRange = 11, AutoPickupEnabled = true, AutoPickupRange = 7, SemiAutoPickupEnabled = true, SemiAutoPickupRange = 7, CanProduceBlackStone = true, Info = "Can pickup items (7x7 auto/semi-auto, 11x11 mouse).", Info1 = "Can produce BlackStones." };
            Baekdon = new IntelligentCreatureInfo { PetType = IntelligentCreatureType.Baekdon, Icon = 504, MousePickupEnabled = true, MousePickupRange = 11, AutoPickupEnabled = true, AutoPickupRange = 7, SemiAutoPickupEnabled = true, SemiAutoPickupRange = 7, CanProduceBlackStone = true, Info = "Can pickup items (7x7 auto/semi-auto, 11x11 mouse).", Info1 = "Can produce BlackStones." };
            Wimaen = new IntelligentCreatureInfo { PetType = IntelligentCreatureType.Wimaen, Icon = 505, MousePickupEnabled = true, MousePickupRange = 7, AutoPickupEnabled = true, AutoPickupRange = 5, SemiAutoPickupEnabled = true, SemiAutoPickupRange = 5, MinimalFullness = 5000, Info = "Can pickup items (5x5 auto/semi-auto, 7x7 mouse)." };
            BlackKitten = new IntelligentCreatureInfo { PetType = IntelligentCreatureType.BlackKitten, Icon = 506, MousePickupEnabled = true, MousePickupRange = 7, AutoPickupEnabled = true, AutoPickupRange = 5, SemiAutoPickupEnabled = true, SemiAutoPickupRange = 5, MinimalFullness = 5000, Info = "Can pickup items (5x5 auto/semi-auto, 7x7 mouse)." };
            BabyDragon = new IntelligentCreatureInfo { PetType = IntelligentCreatureType.BabyDragon, Icon = 507, MousePickupEnabled = true, MousePickupRange = 7, AutoPickupEnabled = true, AutoPickupRange = 5, SemiAutoPickupEnabled = true, SemiAutoPickupRange = 5, MinimalFullness = 7000, Info = "Can pickup items (5x5 auto/semi-auto, 7x7 mouse)." };
            OlympicFlame = new IntelligentCreatureInfo { PetType = IntelligentCreatureType.OlympicFlame, Icon = 508, MousePickupEnabled = true, MousePickupRange = 11, AutoPickupEnabled = true, AutoPickupRange = 11, SemiAutoPickupEnabled = true, SemiAutoPickupRange = 11, CanProduceBlackStone = true, Info = "Can pickup items (11x11 auto/semi-auto, 11x11 mouse).", Info1 = "Can produce BlackStones." };
            BabySnowMan = new IntelligentCreatureInfo { PetType = IntelligentCreatureType.BabySnowMan, Icon = 509, MousePickupEnabled = true, MousePickupRange = 11, AutoPickupEnabled = true, AutoPickupRange = 11, SemiAutoPickupEnabled = true, SemiAutoPickupRange = 11, CanProduceBlackStone = true, Info = "Can pickup items (11x11 auto/semi-auto, 11x11 mouse).", Info1 = "Can produce BlackStones." };
            Frog = new IntelligentCreatureInfo { PetType = IntelligentCreatureType.Frog, Icon = 510, MousePickupEnabled = true, MousePickupRange = 11, AutoPickupEnabled = true, AutoPickupRange = 11, SemiAutoPickupEnabled = true, SemiAutoPickupRange = 11, CanProduceBlackStone = true, Info = "Can pickup items (11x11 auto/semi-auto, 11x11 mouse).", Info1 = "Can produce BlackStones." };
            Monkey = new IntelligentCreatureInfo { PetType = IntelligentCreatureType.BabyMonkey, Icon = 511, MousePickupEnabled = true, MousePickupRange = 11, AutoPickupEnabled = true, AutoPickupRange = 11, SemiAutoPickupEnabled = true, SemiAutoPickupRange = 11, CanProduceBlackStone = true, Info = "Can pickup items (11x11 auto/semi-auto, 11x11 mouse).", Info1 = "Can produce BlackStones." };
            AngryBird = new IntelligentCreatureInfo { PetType = IntelligentCreatureType.AngryBird, Icon = 512, MousePickupEnabled = true, MousePickupRange = 11, AutoPickupEnabled = true, AutoPickupRange = 11, SemiAutoPickupEnabled = true, SemiAutoPickupRange = 11, CanProduceBlackStone = true, Info = "Can pickup items (11x11 auto/semi-auto, 11x11 mouse).", Info1 = "Can produce BlackStones." };
            Foxey = new IntelligentCreatureInfo { PetType = IntelligentCreatureType.Foxey, Icon = 513, MousePickupEnabled = true, MousePickupRange = 11, AutoPickupEnabled = true, AutoPickupRange = 11, SemiAutoPickupEnabled = true, SemiAutoPickupRange = 11, CanProduceBlackStone = true, Info = "Can pickup items (11x11 auto/semi-auto, 11x11 mouse).", Info1 = "Can produce BlackStones." };
        }

        public IntelligentCreatureInfo()
        {
            Creatures.Add(this);
        }

        public static IntelligentCreatureInfo GetCreatureInfo(IntelligentCreatureType petType)
        {
            for (int i = 0; i < Creatures.Count; i++)
            {
                IntelligentCreatureInfo info = Creatures[i];
                if (info.PetType != petType) continue;
                return info;
            }
            return null;
        }
    }
    public class UserIntelligentCreature
    {
        public IntelligentCreatureType PetType;
        public IntelligentCreatureInfo Info;
        public IntelligentCreatureItemFilter Filter;

        public IntelligentCreaturePickupMode petMode = IntelligentCreaturePickupMode.SemiAutomatic;

        public string CustomName;
        public int Fullness;
        public int SlotIndex;
        public long ExpireTime = -9999;//
        public long BlackstoneTime = 0;
        public long MaintainFoodTime = 0;

        public UserIntelligentCreature()
        {

        }

        public UserIntelligentCreature(IntelligentCreatureType creatureType, int slot, byte effect = 0)
        {
            PetType = creatureType;
            Info = IntelligentCreatureInfo.GetCreatureInfo(PetType);
            CustomName = Settings.IntelligentCreatureNameList[(byte)PetType];
            Fullness = 7500;//starts at 75% food
            SlotIndex = slot;

            if (effect > 0) ExpireTime = effect * 86400;//effect holds the amount in days
            else ExpireTime = -9999;//permanent

            BlackstoneTime = 0;
            MaintainFoodTime = 0;

            Filter = new IntelligentCreatureItemFilter();
        }

        public UserIntelligentCreature(BinaryReader reader)
        {
            PetType = (IntelligentCreatureType)reader.ReadByte();
            Info = IntelligentCreatureInfo.GetCreatureInfo(PetType);

            CustomName = reader.ReadString();
            Fullness = reader.ReadInt32();
            SlotIndex = reader.ReadInt32();
            ExpireTime = reader.ReadInt64();
            BlackstoneTime = reader.ReadInt64();

            petMode = (IntelligentCreaturePickupMode)reader.ReadByte();

            Filter = new IntelligentCreatureItemFilter(reader);
            if (Envir.LoadVersion > 48)
            {
                Filter.PickupGrade = (ItemGrade)reader.ReadByte();
                
                MaintainFoodTime = reader.ReadInt64();//maintain food buff
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write((byte)PetType);

            writer.Write(CustomName);
            writer.Write(Fullness);
            writer.Write(SlotIndex);
            writer.Write(ExpireTime);
            writer.Write(BlackstoneTime);

            writer.Write((byte)petMode);

            Filter.Save(writer);
            writer.Write((byte)Filter.PickupGrade);//since Envir.Version 49

            writer.Write(MaintainFoodTime);//maintain food buff

        }

        public Packet GetInfo()
        {
            return new ServerPackets.NewIntelligentCreature
            {
                Creature = CreateClientIntelligentCreature()
            };
        }

        public ClientIntelligentCreature CreateClientIntelligentCreature()
        {
            return new ClientIntelligentCreature
            {
                PetType = PetType,
                Icon = Info.Icon,
                CustomName = CustomName,
                Fullness = Fullness,
                SlotIndex = SlotIndex,
                ExpireTime = ExpireTime,
                BlackstoneTime = BlackstoneTime,
                MaintainFoodTime = MaintainFoodTime,

                petMode = petMode,

                CreatureRules = new IntelligentCreatureRules
                {
                    MinimalFullness = Info.MinimalFullness,
                    MousePickupEnabled = Info.MousePickupEnabled,
                    MousePickupRange = Info.MousePickupRange,
                    AutoPickupEnabled = Info.AutoPickupEnabled,
                    AutoPickupRange = Info.AutoPickupRange,
                    SemiAutoPickupEnabled = Info.SemiAutoPickupEnabled,
                    SemiAutoPickupRange = Info.SemiAutoPickupRange,
                    CanProduceBlackStone = Info.CanProduceBlackStone,
                    Info = Info.Info,
                    Info1 = Info.Info1,
                    Info2 = Info.Info2
                },

                Filter = Filter
            };
        }
    }
}