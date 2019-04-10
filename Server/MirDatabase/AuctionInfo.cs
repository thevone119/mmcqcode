using Newtonsoft.Json;
using Server.MirEnvir;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using S = ServerPackets;

namespace Server.MirDatabase
{   
    //物品拍卖,寄卖物品
    //改造这个类，实现分页查询等功能，把外部的功能移除，集合到这里来
    public class AuctionInfo
    {
        private static LinkedList<AuctionInfo> listAll = new LinkedList<AuctionInfo>();
        private static long lastCheckExpired;//最后校验过期的时间，过期时间1分钟校验一次
        //主键ID
        public ulong AuctionID;
      
        public UserItem Item;
        public DateTime ConsignmentDate;
        //这2个价格，只有一个有效哦,必须要有一个为0
        public uint GoldPrice;//金币价格，
        public uint CreditPrice;//元宝价格
        //关联的账号ID
        public ulong AccountIndex;
        //关联的角色ID
        public ulong CharacterIndex;
        //卖家的姓名
        public string Seller;

        //过期？已卖？删除，如果是删除状态的，保存的时候，要删除数据库中的数据
        public bool Expired, Sold,isdel;

        public AuctionInfo()
        {
            
        }


        /// <summary>
        /// 加载所有数据库中加载
        /// </summary>
        /// <returns></returns>
        public static void loadAll()
        {
            MirRunDB.Execute("delete from AuctionInfo where isdel=1");

            DbDataReader read = MirRunDB.ExecuteReader("select * from AuctionInfo where isdel=0");
            while (read.Read())
            {
                AuctionInfo obj = new AuctionInfo();

                obj.AuctionID = (ulong)read.GetInt64(read.GetOrdinal("AuctionID"));
                obj.GoldPrice = (uint)read.GetInt32(read.GetOrdinal("GoldPrice"));
                obj.CreditPrice = (uint)read.GetInt32(read.GetOrdinal("CreditPrice"));
                obj.CharacterIndex = (ulong)read.GetInt64(read.GetOrdinal("CharacterIndex"));
                obj.AccountIndex = (ulong)read.GetInt64(read.GetOrdinal("AccountIndex"));
                obj.Seller = read.GetString(read.GetOrdinal("Seller"));
                
                obj.Item = JsonConvert.DeserializeObject<UserItem>(read.GetString(read.GetOrdinal("UserItem")));
                if (!obj.Item.BindItem())
                {
                    obj.Item = null;
                    continue;
                }

                obj.ConsignmentDate = read.GetDateTime(read.GetOrdinal("ConsignmentDate"));
                obj.Expired = read.GetBoolean(read.GetOrdinal("Expired"));
                obj.Sold = read.GetBoolean(read.GetOrdinal("Sold"));
                obj.isdel = read.GetBoolean(read.GetOrdinal("isdel"));

                DBObjectUtils.updateObjState(obj, obj.AuctionID);
                listAll.AddLast(obj);
            }
        }

        //增加一条数据
        public static void add(AuctionInfo info)
        {
            listAll.AddFirst(info);
        }


        //保存所有的数据
        public static void SaveAll()
        {
            foreach (AuctionInfo info in listAll)
            {
                info.SaveDB();
            }
        }

        //查询分页,page从0开始
        //UserMatch：卖：true, 买：false
        public static S.NPCMarket SearchPage(ulong CharacterIndex,byte itemtype,string MatchName, bool UserMatch,int page=0,int pagesize=8)
        {
            if (MatchName != null)
            {
                MatchName = MatchName.Replace(" ", "");
            }
            //1分钟才校验一次是否过期
            if (SMain.Envir.Time > lastCheckExpired)
            {
                lastCheckExpired = SMain.Envir.Time + 1000 * 60;
                DateTime Now = SMain.Envir.Now;
                foreach (AuctionInfo info in listAll)
                {
                    if (info.isdel)
                    {
                        continue;
                    }
                    //判断是否过期
                    if (Now >= info.ConsignmentDate.AddDays(Globals.ConsignmentLength) && !info.Sold)
                    {
                        info.Expired = true;
                    }
                }
            }


            //第一步，先查询出所有符合的，最多查询10页，100条符合的
            IList<AuctionInfo> listSearch = new List<AuctionInfo>();
            DateTime now = DateTime.Now;
            foreach (AuctionInfo info in listAll)
            {
                if (info.isdel)
                {
                    continue;
                }
                if (UserMatch)
                {
                    if(info.CharacterIndex!= CharacterIndex)
                    {
                        continue;
                    }
                    if (listSearch.Count >= Globals.MaxConsignmentCount)
                    {
                        break;
                    }
                }
                else
                {
                    if (listSearch.Count >= 100)
                    {
                        break;
                    }
                    if (info.Expired || info.Sold)
                    {
                        continue;
                    }
                }
                if(!string.IsNullOrEmpty(MatchName)&& !info.Item.Name.Contains(MatchName))
                {
                    continue;
                }
                if (itemtype > 0&& itemtype<100&& itemtype != (byte)info.Item.Info.Type)
                {
                    continue;
                }
                if (itemtype == 100 && ((byte)info.Item.Info.Type<=20&& (byte)info.Item.Info.Type > 0))
                {
                    continue;
                }
                listSearch.Add(info);
            }
            //取分页内容
            List<ClientAuction> listings = new List<ClientAuction>();
            for (int i = 0; i < pagesize; i++)
            {
                if (i + page * pagesize >= listSearch.Count) break;
                listings.Add(listSearch[i + page * pagesize].CreateClientAuction(UserMatch));
            }
            return new S.NPCMarket { Listings = listings, cpage= page,pageCount = (listSearch.Count - 1) / pagesize + 1, UserMode = UserMatch };
        }

        //取某件商品
        public static AuctionInfo get(ulong AuctionID)
        {
            foreach (AuctionInfo info in listAll)
            {
                if(info.AuctionID== AuctionID && !info.isdel)
                {
                    return info;
                }
            }
            return null;
        }
        //保存到数据库
        public void SaveDB()
        {
            byte state = DBObjectUtils.ObjState(this, AuctionID);
            if (state == 0)//没有改变
            {
                return;
            }
            List<SQLiteParameter> lp = new List<SQLiteParameter>();
            lp.Add(new SQLiteParameter("ConsignmentDate", ConsignmentDate));
            lp.Add(new SQLiteParameter("GoldPrice", GoldPrice));
            lp.Add(new SQLiteParameter("CreditPrice", CreditPrice));
            lp.Add(new SQLiteParameter("CharacterIndex", CharacterIndex));
            lp.Add(new SQLiteParameter("Expired", Expired));
            lp.Add(new SQLiteParameter("Sold", Sold));
            lp.Add(new SQLiteParameter("isdel", isdel));
            lp.Add(new SQLiteParameter("Seller", Seller));

            lp.Add(new SQLiteParameter("AccountIndex", AccountIndex));
            lp.Add(new SQLiteParameter("UserItem", JsonConvert.SerializeObject(Item)));
            //新增
            if (state == 1)
            {
                if (AuctionID > 0)
                {
                    lp.Add(new SQLiteParameter("AuctionID", AuctionID));
                }
                string sql = "insert into AuctionInfo" + SQLiteHelper.createInsertSql(lp.ToArray());
                MirRunDB.Execute(sql, lp.ToArray());
            }
            //修改
            if (state == 2)
            {
                string sql = "update AuctionInfo set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where AuctionID=@AuctionID";
                lp.Add(new SQLiteParameter("AuctionID", AuctionID));
                MirRunDB.Execute(sql, lp.ToArray());
            }
            DBObjectUtils.updateObjState(this, AuctionID);
        }

        public ClientAuction CreateClientAuction(bool userMatch)
        {
            return new ClientAuction
                {
                    AuctionID = AuctionID,
                    Item = Item,
                    Seller = Seller,
                    Expired= Expired,
                    Sold= Sold,
                    GoldPrice = GoldPrice,
                    CreditPrice = CreditPrice,
                    ConsignmentDate = ConsignmentDate,
                };
        }
    }
}
