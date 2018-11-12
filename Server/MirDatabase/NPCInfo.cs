using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Server.MirEnvir;
using System.Data.SQLite;
using System.Data.Common;
using Newtonsoft.Json;

namespace Server.MirDatabase
{
    /// <summary>
    /// NPC信息
    /// </summary>
    public class NPCInfo
    {
        public int Index;

        public string FileName = string.Empty, Name = string.Empty;

        public int MapIndex;
        public Point Location;
        public ushort Rate = 100;
        public ushort Image;
        public Color Colour;

        public bool TimeVisible = false;
        public byte HourStart = 0;
        public byte MinuteStart = 0;
        public byte HourEnd = 0;
        public byte MinuteEnd = 1;
        public short MinLev = 0;
        public short MaxLev = 0;
        public string DayofWeek = "";
        public string ClassRequired = "";
        public bool Sabuk = false;
        public int FlagNeeded = 0;
        public int Conquest;

        //以下为非数据库字段
        public bool IsDefault, IsRobot;

        //这2个也加入到数据库中？
        public List<int> CollectQuestIndexes = new List<int>();
        public List<int> FinishQuestIndexes = new List<int>();
        
        public NPCInfo()
        { }

        /// <summary>
        /// 加载所有数据
        /// </summary>
        /// <returns></returns>
        public static List<NPCInfo> loadAll()
        {
            List<NPCInfo> list = new List<NPCInfo>();
            DbDataReader read = MirConfigDB.ExecuteReader("select * from NPCInfo");

            while (read.Read())
            {
                NPCInfo obj = new NPCInfo();
                if (read.IsDBNull(read.GetOrdinal("Name")))
                {
                    continue;
                }
                obj.Name = read.GetString(read.GetOrdinal("Name"));
                if (obj.Name == null)
                {
                    continue;
                }
                obj.Index = read.GetInt32(read.GetOrdinal("Idx"));
                obj.FileName = read.GetString(read.GetOrdinal("FileName"));

                obj.MapIndex = read.GetInt16(read.GetOrdinal("MapIndex"));
                obj.Location = new Point(read.GetInt16(read.GetOrdinal("Location_X")), read.GetInt16(read.GetOrdinal("Location_Y")));

                obj.Rate = (ushort)read.GetInt16(read.GetOrdinal("Rate"));
                obj.Image = (ushort)read.GetInt16(read.GetOrdinal("Image"));

                obj.TimeVisible = read.GetBoolean(read.GetOrdinal("TimeVisible"));
                obj.HourStart = read.GetByte(read.GetOrdinal("HourStart"));
                obj.MinuteStart = read.GetByte(read.GetOrdinal("MinuteStart"));
                obj.HourEnd = read.GetByte(read.GetOrdinal("HourEnd"));
                obj.MinuteEnd = read.GetByte(read.GetOrdinal("MinuteEnd"));
                obj.MinLev = (short)read.GetInt16(read.GetOrdinal("MinLev"));
                obj.MaxLev = (short)read.GetInt16(read.GetOrdinal("MaxLev"));
                obj.DayofWeek = read.GetString(read.GetOrdinal("DayofWeek"));
                obj.ClassRequired = read.GetString(read.GetOrdinal("ClassRequired"));
                obj.Conquest = read.GetInt32(read.GetOrdinal("Conquest"));
                obj.FlagNeeded = read.GetInt32(read.GetOrdinal("FlagNeeded"));

                obj.CollectQuestIndexes= JsonConvert.DeserializeObject< List< int >>(read.GetString(read.GetOrdinal("CollectQuestIndexes")));
                obj.FinishQuestIndexes = JsonConvert.DeserializeObject<List<int>>(read.GetString(read.GetOrdinal("FinishQuestIndexes")));



                DBObjectUtils.updateObjState(obj, obj.Index);
                list.Add(obj);
            }
            return list;
        }

        public NPCInfo(BinaryReader reader)
        {
            if (Envir.LoadVersion > 33)
            {
                Index = reader.ReadInt32();
                MapIndex = reader.ReadInt32();

                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    CollectQuestIndexes.Add(reader.ReadInt32());

                count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    FinishQuestIndexes.Add(reader.ReadInt32());
            }

            FileName = reader.ReadString();
            Name = reader.ReadString();

            Location = new Point(reader.ReadInt32(), reader.ReadInt32());

            if (Envir.LoadVersion >= 72)
            {
                Image = reader.ReadUInt16();
            }
            else
            {
                Image = reader.ReadByte();
            }
            
            Rate = reader.ReadUInt16();

            if (Envir.LoadVersion >= 64)
            {
                TimeVisible = reader.ReadBoolean();
                HourStart = reader.ReadByte();
                MinuteStart = reader.ReadByte();
                HourEnd = reader.ReadByte();
                MinuteEnd = reader.ReadByte();
                MinLev = reader.ReadInt16();
                MaxLev = reader.ReadInt16();
                DayofWeek = reader.ReadString();
                ClassRequired = reader.ReadString();
                if (Envir.LoadVersion >= 66)
                    Conquest = reader.ReadInt32();
                else
                    Sabuk = reader.ReadBoolean();
                FlagNeeded = reader.ReadInt32();
            }
        }
        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(MapIndex);

            writer.Write(CollectQuestIndexes.Count());
            for (int i = 0; i < CollectQuestIndexes.Count; i++)
                writer.Write(CollectQuestIndexes[i]);

            writer.Write(FinishQuestIndexes.Count());
            for (int i = 0; i < FinishQuestIndexes.Count; i++)
                writer.Write(FinishQuestIndexes[i]);

            writer.Write(FileName);
            writer.Write(Name);

            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(Image);
            writer.Write(Rate);

            writer.Write(TimeVisible);
            writer.Write(HourStart);
            writer.Write(MinuteStart);
            writer.Write(HourEnd);
            writer.Write(MinuteEnd);
            writer.Write(MinLev);
            writer.Write(MaxLev);
            writer.Write(DayofWeek);
            writer.Write(ClassRequired);
            writer.Write(Conquest);
            writer.Write(FlagNeeded);
            SaveDB();
        }

        //保存到数据库
        public void SaveDB()
        {
            byte state = DBObjectUtils.ObjState(this, Index);
            if (state == 0)//没有改变
            {
                return;
            }
            SMain.Enqueue("NPCInfo change state:" + state);
            List<SQLiteParameter> lp = new List<SQLiteParameter>();
            lp.Add(new SQLiteParameter("MapIndex", MapIndex));
            lp.Add(new SQLiteParameter("FileName", FileName));
            lp.Add(new SQLiteParameter("Name", Name));
            lp.Add(new SQLiteParameter("Location_X", Location.X));
            lp.Add(new SQLiteParameter("Location_Y", Location.Y));
            lp.Add(new SQLiteParameter("Image", Image));
            lp.Add(new SQLiteParameter("Rate", Rate));

            lp.Add(new SQLiteParameter("TimeVisible", TimeVisible));
            lp.Add(new SQLiteParameter("HourStart", HourStart));
            lp.Add(new SQLiteParameter("MinuteStart", MinuteStart));
            lp.Add(new SQLiteParameter("HourEnd", HourEnd));
            lp.Add(new SQLiteParameter("MinuteEnd", MinuteEnd));

            lp.Add(new SQLiteParameter("MinLev", MinLev));
            lp.Add(new SQLiteParameter("MaxLev", MaxLev));

            lp.Add(new SQLiteParameter("DayofWeek", DayofWeek));
            lp.Add(new SQLiteParameter("ClassRequired", ClassRequired));
            lp.Add(new SQLiteParameter("Conquest", Conquest));
            lp.Add(new SQLiteParameter("FlagNeeded", FlagNeeded));

            lp.Add(new SQLiteParameter("CollectQuestIndexes", JsonConvert.SerializeObject(CollectQuestIndexes)));
            lp.Add(new SQLiteParameter("FinishQuestIndexes", JsonConvert.SerializeObject(FinishQuestIndexes)));

            //新增
            if (state == 1)
            {
                if (Index > 0)
                {
                    lp.Add(new SQLiteParameter("Idx", Index));
                }
                string sql = "insert into NPCInfo" + SQLiteHelper.createInsertSql(lp.ToArray()); ;
                MirConfigDB.Execute(sql, lp.ToArray());
            }
            //修改
            if (state == 2)
            {
                string sql = "update NPCInfo set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where Idx=@Idx";
                lp.Add(new SQLiteParameter("Idx", Index));
                MirConfigDB.Execute(sql, lp.ToArray());
            }
            
            DBObjectUtils.updateObjState(this, Index);

        }

        public static void FromText(string text)
        {
            string[] data = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (data.Length < 6) return;

            NPCInfo info = new NPCInfo { Name = data[0] };

            int x, y;

            info.FileName = data[0];
            info.MapIndex = SMain.EditEnvir.MapInfoList.Where(d => d.FileName == data[1]).FirstOrDefault().Index;

            if (!int.TryParse(data[2], out x)) return;
            if (!int.TryParse(data[3], out y)) return;

            info.Location = new Point(x, y);

            info.Name = data[4];

            if (!ushort.TryParse(data[5], out info.Image)) return;
            if (!ushort.TryParse(data[6], out info.Rate)) return;

            info.Index = DBObjectUtils.getObjNextId(info);
            SMain.EditEnvir.NPCInfoList.Add(info);
        }
        public string ToText()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6}",
                FileName, SMain.EditEnvir.MapInfoList.Where(d => d.Index == MapIndex).FirstOrDefault().FileName, Location.X, Location.Y, Name, Image, Rate);
        }

        public override string ToString()
        {
            return string.Format("{0}:   {1}", FileName, Functions.PointToString(Location));
        }
    }
}
