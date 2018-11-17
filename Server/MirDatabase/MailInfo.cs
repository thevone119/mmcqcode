using Newtonsoft.Json;
using Server.MirDatabase;
using Server.MirObjects;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace Server.MirEnvir
{
    //邮件信息
    public class MailInfo
    {
        public ulong MailID;

        public string Sender;

        public ulong RecipientIndex;

        [JsonIgnore]
        public CharacterInfo RecipientInfo;

        public string Message = string.Empty;
        public uint Gold = 0;

        public List<UserItem> Items = new List<UserItem>();

        public DateTime DateSent, DateOpened;

        public bool Sent
        {
            get { return DateSent > DateTime.MinValue; }
        }

        public bool Opened
        {
            get { return DateOpened > DateTime.MinValue; }
        }

        public bool Locked;

        public bool Collected;

        public bool Parcel //parcel if item contains gold or items.
        {
            get { return Gold > 0 || Items.Count > 0; }
        }

        public bool CanReply;

        public MailInfo()
        {

        }

        public MailInfo(ulong recipientIndex, bool canReply = false)
        {
            MailID = (ulong)UniqueKeyHelper.UniqueNext();
            RecipientIndex = recipientIndex;

            CanReply = canReply;
        }

        public MailInfo(BinaryReader reader, int version, int customversion)
        {
            MailID = reader.ReadUInt64();
            Sender = reader.ReadString();
            RecipientIndex = (ulong)reader.ReadInt32();
            Message = reader.ReadString();
            Gold = reader.ReadUInt32();

            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                UserItem item = new UserItem(reader, version, customversion);
                if (item.BindItem())
                    Items.Add(item);
            }

            DateSent = DateTime.FromBinary(reader.ReadInt64());
            DateOpened = DateTime.FromBinary(reader.ReadInt64());

            Locked = reader.ReadBoolean();
            Collected = reader.ReadBoolean();
            CanReply = reader.ReadBoolean();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(MailID);
            writer.Write(Sender);
            writer.Write(RecipientIndex);
            writer.Write(Message);
            writer.Write(Gold);

            writer.Write(Items.Count);
            for (int i = 0; i < Items.Count; i++)
                Items[i].Save(writer);

            writer.Write(DateSent.ToBinary());
            writer.Write(DateOpened.ToBinary());

            writer.Write(Locked);
            writer.Write(Collected);
            writer.Write(CanReply);
        }

        public void BindItems()
        {
            for (int i = 0; i < Items.Count && i >= 0; i++)
            {
                if (!Items[i].BindItem())
                {
                    Items.RemoveAt(i);
                    i--;
                }
            }
        }


        /// <summary>
        /// 加载所有数据库中加载
        /// </summary>
        /// <returns></returns>
        public static List<MailInfo> loadAll()
        {
            List<MailInfo> list = new List<MailInfo>();
            DbDataReader read = MirRunDB.ExecuteReader("select * from MailInfo");
            while (read.Read())
            {
                MailInfo obj = new MailInfo();

                obj.MailID = (ulong)read.GetInt64(read.GetOrdinal("MailID"));
                obj.Sender = read.GetString(read.GetOrdinal("Sender"));
                obj.RecipientIndex = (ulong)read.GetInt64(read.GetOrdinal("RecipientIndex"));
                obj.Message = read.GetString(read.GetOrdinal("Message"));
                obj.Gold = (uint)read.GetInt32(read.GetOrdinal("Gold"));
                obj.DateSent = read.GetDateTime(read.GetOrdinal("Gold"));
                obj.DateOpened = read.GetDateTime(read.GetOrdinal("DateOpened"));
                obj.Locked = read.GetBoolean(read.GetOrdinal("Locked"));
                obj.Collected = read.GetBoolean(read.GetOrdinal("Collected"));
                obj.CanReply = read.GetBoolean(read.GetOrdinal("CanReply"));
                obj.CanReply = read.GetBoolean(read.GetOrdinal("CanReply"));

                obj.Items = JsonConvert.DeserializeObject<List<UserItem>>(read.GetString(read.GetOrdinal("Items")));
                obj.BindItems();


                DBObjectUtils.updateObjState(obj, obj.MailID);
                list.Add(obj);
            }
            return list;
        }

        //保存到数据库
        public void SaveDB()
        {
            byte state = DBObjectUtils.ObjState(this, MailID);
            if (state == 0)//没有改变
            {
                return;
            }
            List<SQLiteParameter> lp = new List<SQLiteParameter>();
            lp.Add(new SQLiteParameter("Sender", Sender));
            lp.Add(new SQLiteParameter("RecipientIndex", RecipientIndex));
            lp.Add(new SQLiteParameter("Message", Message));
            lp.Add(new SQLiteParameter("Gold", Gold));
            lp.Add(new SQLiteParameter("DateSent", DateSent));
            lp.Add(new SQLiteParameter("DateOpened", DateOpened));
            lp.Add(new SQLiteParameter("Locked", Locked));
            lp.Add(new SQLiteParameter("Collected", Collected));
            lp.Add(new SQLiteParameter("CanReply", CanReply));

            lp.Add(new SQLiteParameter("Items", JsonConvert.SerializeObject(Items)));
            //新增
            if (state == 1)
            {
                if (MailID > 0)
                {
                    lp.Add(new SQLiteParameter("MailID", MailID));
                }
                string sql = "insert into MailInfo" + SQLiteHelper.createInsertSql(lp.ToArray());
                MirRunDB.Execute(sql, lp.ToArray());
            }
            //修改
            if (state == 2)
            {
                string sql = "update MailInfo set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where MailID=@MailID";
                lp.Add(new SQLiteParameter("MailID", MailID));
                MirRunDB.Execute(sql, lp.ToArray());
            }
            DBObjectUtils.updateObjState(this, MailID);
        }
        //发送邮件
        public void Send()
        {
            if (Sent) return;

            Collected = true;

            if (Parcel)
            {
                if(Items.Count > 0 && Gold > 0)
                {
                    if(!Settings.MailAutoSendGold || !Settings.MailAutoSendItems)
                    {
                        Collected = false;
                    }
                }
                if(Items.Count > 0)
                {
                    if (!Settings.MailAutoSendItems)
                    {
                        Collected = false;
                    }
                }
                else
                {
                    if (!Settings.MailAutoSendGold)
                    {
                        Collected = false;
                    }
                }
            }

            if (SMain.Envir.Mail.Contains(this)) return;

            SMain.Envir.Mail.Add(this); //add to postbox

            DateSent = DateTime.Now;
        }
        //接受邮件
        public bool Receive()
        {
            if (!Sent) return false; //mail not sent yet

            if (RecipientInfo == null)
            {
                RecipientInfo = SMain.Envir.GetCharacterInfo(RecipientIndex);

                if (RecipientInfo == null) return false;
            }

            RecipientInfo.Mail.Add(this); //add to players inbox
            
            if(RecipientInfo.Player != null)
            {
                RecipientInfo.Player.NewMail = true; //notify player of new mail  --check in player process
            }

            SMain.Envir.Mail.Remove(this); //remove from postbox

            return true;
        }

        public ClientMail CreateClientMail()
        {
            return new ClientMail
            {
                MailID = MailID,
                SenderName = Sender,
                Message = Message,
                Locked = Locked,
                CanReply = CanReply,
                Gold = Gold,
                Items = Items,
                Opened = Opened,
                Collected = Collected,
                DateSent = DateSent
            };
        }
    }

    // player bool NewMail (process in envir loop) - send all mail on login

    // Send mail from player (auto from player)
    // Send mail from Envir (mir administrator)
}
