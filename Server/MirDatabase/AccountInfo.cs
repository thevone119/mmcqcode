using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Server.MirNetwork;
using Server.MirEnvir;
using C = ClientPackets;
using Newtonsoft.Json;
using System.Data.SQLite;
using System.Data.Common;

namespace Server.MirDatabase
{
    //账号信息
    //看怎么和平台整合才可以
    //账号应该独立，不要和具体游戏的内容捆绑。
    //把仓库，金币，信用等游戏内的信息，迁移到角色上去。
    public class AccountInfo
    {
        //缓存数据
        private static Dictionary<string, AccountInfo> dbtemp = new Dictionary<string, AccountInfo>();

        public ulong Index;

        public string AccountID = string.Empty;
        public string Password = string.Empty;

        public string UserName = string.Empty;
        public DateTime BirthDate;
        public string SecretQuestion = string.Empty;
        public string SecretAnswer = string.Empty;
        public string EMailAddress = string.Empty;

        public string CreationIP = string.Empty;
        public DateTime CreationDate;

        public bool Banned;
        public string BanReason = string.Empty;
        public DateTime ExpiryDate;
        public int WrongPasswordCount;

        public string LastIP = string.Empty;
        public DateTime LastDate;

        [JsonIgnore]
        public List<CharacterInfo> Characters = new List<CharacterInfo>();
        //这个是仓库物品存储，仓库关联到账号上，不关联到角色，这样多个角色间可以共享仓库
        //默认这个共享只能存储80个物品，可以扩展仓库，扩展仓库后可以存储160个物品
        public UserItem[] Storage = new UserItem[80];
        public bool HasExpandedStorage;
        public DateTime ExpandedStorageExpiryDate;
        public uint Gold;//金币,金币是账号上的金币，多角色共享
        public uint Credit;//积分，信用,也可称作元宝

        public ListViewItem ListItem;
        public MirConnection Connection;

        [JsonIgnore]
        public LinkedList<AuctionInfo> Auctions = new LinkedList<AuctionInfo>();
        public bool AdminAccount;

        public AccountInfo()
        {

        }
        public AccountInfo(C.NewAccount p)
        {
            AccountID = p.AccountID;
            Password = p.Password;
            UserName = p.UserName;
            SecretQuestion = p.SecretQuestion;
            SecretAnswer = p.SecretAnswer;
            EMailAddress = p.EMailAddress;

            BirthDate = p.BirthDate;
            CreationDate = SMain.Envir.Now;
        }
        




        /// <summary>
        /// 根据账号ID查找账号
        /// </summary>
        /// <param name="AccountID"></param>
        /// <returns></returns>
        public static List<AccountInfo> loadAll()
        {
            List<AccountInfo> list = new List<AccountInfo>();
            DbDataReader read = MirRunDB.ExecuteReader("select * from AccountInfo ");
            if (read.Read())
            {
                AccountInfo obj = new AccountInfo();
                obj.Index = (ulong)read.GetInt64(read.GetOrdinal("Idx"));
                obj.AccountID = read.GetString(read.GetOrdinal("AccountID"));
                obj.Password = read.GetString(read.GetOrdinal("Password"));
                obj.UserName = read.GetString(read.GetOrdinal("UserName"));
                obj.BirthDate = read.GetDateTime(read.GetOrdinal("BirthDate"));
                obj.SecretQuestion = read.GetString(read.GetOrdinal("SecretQuestion"));
                obj.SecretAnswer = read.GetString(read.GetOrdinal("SecretAnswer"));
                obj.EMailAddress = read.GetString(read.GetOrdinal("EMailAddress"));

                obj.CreationIP = read.GetString(read.GetOrdinal("CreationIP"));
                obj.CreationDate = read.GetDateTime(read.GetOrdinal("CreationDate"));

                obj.Banned = read.GetBoolean(read.GetOrdinal("Banned"));
                obj.BanReason = read.GetString(read.GetOrdinal("BanReason"));
                obj.ExpiryDate = read.GetDateTime(read.GetOrdinal("ExpiryDate"));

                obj.LastIP = read.GetString(read.GetOrdinal("LastIP"));
                obj.LastDate = read.GetDateTime(read.GetOrdinal("LastDate"));


                obj.HasExpandedStorage = read.GetBoolean(read.GetOrdinal("HasExpandedStorage"));
                obj.ExpandedStorageExpiryDate = read.GetDateTime(read.GetOrdinal("ExpandedStorageExpiryDate"));

                obj.Gold = (uint)read.GetInt32(read.GetOrdinal("Gold"));
                obj.Credit = (uint)read.GetInt32(read.GetOrdinal("Credit"));
                obj.AdminAccount = read.GetBoolean(read.GetOrdinal("AdminAccount"));

                obj.Storage = JsonConvert.DeserializeObject<UserItem[]>(read.GetString(read.GetOrdinal("Storage")));
                if (obj.Storage != null)
                {
                    for(int i=0;i< obj.Storage.Length; i++)
                    {
                        if (obj.Storage[i] != null)
                        {
                            obj.Storage[i].BindItem();
                        }
                    }
                }
                DBObjectUtils.updateObjState(obj, obj.Index);
                list.Add(obj);
            }
            return list;
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
            lp.Add(new SQLiteParameter("AccountID", AccountID));
            lp.Add(new SQLiteParameter("Password", Password));
            lp.Add(new SQLiteParameter("UserName", UserName));
            lp.Add(new SQLiteParameter("BirthDate", BirthDate));
            lp.Add(new SQLiteParameter("SecretQuestion", SecretQuestion));
            lp.Add(new SQLiteParameter("SecretAnswer", SecretAnswer));
            lp.Add(new SQLiteParameter("EMailAddress", EMailAddress));
            lp.Add(new SQLiteParameter("CreationIP", CreationIP));
            lp.Add(new SQLiteParameter("CreationDate", CreationDate));
            lp.Add(new SQLiteParameter("ExpiryDate", ExpiryDate));
            lp.Add(new SQLiteParameter("Banned", Banned));
            lp.Add(new SQLiteParameter("BanReason", BanReason));
            lp.Add(new SQLiteParameter("ExpiryDate", ExpiryDate));
            lp.Add(new SQLiteParameter("LastIP", LastIP));
            lp.Add(new SQLiteParameter("LastDate", LastDate));
            lp.Add(new SQLiteParameter("HasExpandedStorage", HasExpandedStorage));
            lp.Add(new SQLiteParameter("ExpandedStorageExpiryDate", ExpandedStorageExpiryDate));
            lp.Add(new SQLiteParameter("Gold", Gold));
            lp.Add(new SQLiteParameter("Credit", Credit));
            lp.Add(new SQLiteParameter("AdminAccount", AdminAccount));
            lp.Add(new SQLiteParameter("Storage", JsonConvert.SerializeObject(Storage)));

            //新增
            if (state == 1)
            {
                if (Index > 0)
                {
                    lp.Add(new SQLiteParameter("Idx", Index));
                }
                string sql = "insert into AccountInfo" + SQLiteHelper.createInsertSql(lp.ToArray());
                MirRunDB.Execute(sql, lp.ToArray());
            }
            //修改
            if (state == 2)
            {
                string sql = "update AccountInfo set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where Idx=@Idx";
                lp.Add(new SQLiteParameter("Idx", Index));
                MirRunDB.Execute(sql, lp.ToArray());
            }
            DBObjectUtils.updateObjState(this, Index);
        }

        public ListViewItem CreateListView()
        {
            if (ListItem != null)
                ListItem.Remove();


            ListItem = new ListViewItem(Index.ToString()) {Tag = this};

            ListItem.SubItems.Add(AccountID);
            ListItem.SubItems.Add(Password);
            ListItem.SubItems.Add(UserName);
            ListItem.SubItems.Add(AdminAccount.ToString());
            ListItem.SubItems.Add(Banned.ToString());
            ListItem.SubItems.Add(BanReason);
            ListItem.SubItems.Add(ExpiryDate.ToString());

            return ListItem;
        }

        public void Update()
        {
            if (ListItem == null) return;

            ListItem.SubItems[0].Text = Index.ToString();
            ListItem.SubItems[1].Text = AccountID;
            ListItem.SubItems[2].Text = Password;
            ListItem.SubItems[3].Text = UserName;
            ListItem.SubItems[4].Text = AdminAccount.ToString();
            ListItem.SubItems[5].Text = Banned.ToString();
            ListItem.SubItems[6].Text = BanReason;
            ListItem.SubItems[7].Text = ExpiryDate.ToString();
        }

        public List<SelectInfo> GetSelectInfo()
        {
            List<SelectInfo> list = new List<SelectInfo>();

            for (int i = 0; i < Characters.Count; i++)
            {
                if (Characters[i].Deleted) continue;
                list.Add(Characters[i].ToSelectInfo());
                if (list.Count >= Globals.MaxCharacterCount) break;
            }

            return list;
        }

        public int ExpandStorage()
        {
            if (Storage.Length == 80)
                Array.Resize(ref Storage, Storage.Length + 80);

            return Storage.Length;
        }
    }
}