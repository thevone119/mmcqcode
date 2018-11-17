using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace Server.MirDatabase
{   ///物品拍卖
    public class AuctionInfo
    {
        public ulong AuctionID; 

        public UserItem Item;
        public DateTime ConsignmentDate;
        public uint Price;
        //关联的角色ID
        public ulong CharacterIndex;
        [JsonIgnore]
        public CharacterInfo CharacterInfo;
        //过期？已卖？
        public bool Expired, Sold;

        public AuctionInfo()
        {
            
        }


        /// <summary>
        /// 加载所有数据库中加载
        /// </summary>
        /// <returns></returns>
        public static LinkedList<AuctionInfo> loadAll()
        {
            LinkedList<AuctionInfo> list = new LinkedList<AuctionInfo>();
            DbDataReader read = MirRunDB.ExecuteReader("select * from AuctionInfo");
            while (read.Read())
            {
                AuctionInfo obj = new AuctionInfo();

                obj.AuctionID = (ulong)read.GetInt64(read.GetOrdinal("AuctionID"));
                obj.Price = (uint)read.GetInt32(read.GetOrdinal("Price"));
                obj.CharacterIndex = (ulong)read.GetInt64(read.GetOrdinal("CharacterIndex"));
                obj.Item = JsonConvert.DeserializeObject<UserItem>(read.GetString(read.GetOrdinal("UserItem")));
                if (!obj.Item.BindItem())
                {
                    obj.Item = null;
                    continue;
                }

                obj.ConsignmentDate = read.GetDateTime(read.GetOrdinal("ConsignmentDate"));
                obj.Expired = read.GetBoolean(read.GetOrdinal("Expired"));
                obj.Sold = read.GetBoolean(read.GetOrdinal("Sold"));
               
                DBObjectUtils.updateObjState(obj, obj.AuctionID);
                list.AddLast(obj);
            }
            return list;
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
            lp.Add(new SQLiteParameter("Price", Price));
            lp.Add(new SQLiteParameter("CharacterIndex", CharacterIndex));
            lp.Add(new SQLiteParameter("Expired", Expired));
            lp.Add(new SQLiteParameter("Sold", Sold));
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
                    Seller = userMatch ? (Sold ? "Sold" : (Expired ? "Expired" : "For Sale")) : CharacterInfo.Name,
                    Price = Price,
                    ConsignmentDate = ConsignmentDate,
                };
        }
    }
}
