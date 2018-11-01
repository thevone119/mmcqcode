using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Server.MirEnvir;
using System.Data.SQLite;

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
            StringBuilder sb = new StringBuilder();
            List<SQLiteParameter> lp = new List<SQLiteParameter>();
            sb.Append("update NPCInfo set ");

            sb.Append(" MapIndex=@MapIndex, "); lp.Add(new SQLiteParameter("MapIndex", MapIndex));
            sb.Append(" FileName=@FileName, "); lp.Add(new SQLiteParameter("FileName", FileName));
            sb.Append(" Name=@Name, "); lp.Add(new SQLiteParameter("Name", Name));
            sb.Append(" Location_X=@Location_X, "); lp.Add(new SQLiteParameter("Location_X", Location.X));
            sb.Append(" Location_Y=@Location_Y, "); lp.Add(new SQLiteParameter("Location_Y", Location.Y));
            sb.Append(" Image=@Image, "); lp.Add(new SQLiteParameter("Image", Image));
            sb.Append(" Rate=@Rate, "); lp.Add(new SQLiteParameter("Rate", Rate));

            sb.Append(" TimeVisible=@TimeVisible, "); lp.Add(new SQLiteParameter("TimeVisible", TimeVisible));
            sb.Append(" HourStart=@HourStart, "); lp.Add(new SQLiteParameter("HourStart", HourStart));
            sb.Append(" MinuteStart=@MinuteStart, "); lp.Add(new SQLiteParameter("MinuteStart", MinuteStart));
            sb.Append(" HourEnd=@HourEnd, "); lp.Add(new SQLiteParameter("HourEnd", HourEnd));
            sb.Append(" MinuteEnd=@MinuteEnd, "); lp.Add(new SQLiteParameter("MinuteEnd", MinuteEnd));

            sb.Append(" MinLev=@MinLev, "); lp.Add(new SQLiteParameter("MinLev", MinLev));
            sb.Append(" MaxLev=@MaxLev, "); lp.Add(new SQLiteParameter("MaxLev", MaxLev));

            sb.Append(" DayofWeek=@DayofWeek, "); lp.Add(new SQLiteParameter("DayofWeek", DayofWeek));
            sb.Append(" ClassRequired=@ClassRequired, "); lp.Add(new SQLiteParameter("ClassRequired", ClassRequired));
            sb.Append(" Conquest=@Conquest, "); lp.Add(new SQLiteParameter("Conquest", Conquest));
            sb.Append(" FlagNeeded=@FlagNeeded, "); lp.Add(new SQLiteParameter("FlagNeeded", FlagNeeded));

            List<string> newList = CollectQuestIndexes.ConvertAll<string>(x => x.ToString());
            string cqidexs = string.Join(",", newList.ToArray());
            sb.Append(" CollectQuestIndexes=@CollectQuestIndexes, "); lp.Add(new SQLiteParameter("CollectQuestIndexes", cqidexs));
            newList = FinishQuestIndexes.ConvertAll<string>(x => x.ToString());
            string fqidexs = string.Join(",", newList.ToArray());
            sb.Append(" FinishQuestIndexes=@FinishQuestIndexes "); lp.Add(new SQLiteParameter("FinishQuestIndexes", fqidexs));
  
            sb.Append(" where  Idx=@Idx "); lp.Add(new SQLiteParameter("Idx", Index));
            //执行更新
            int ucount = MirConfigDB.Execute(sb.ToString(), lp.ToArray());

            //没有得更新，则执行插入
            if (ucount <= 0)
            {
                sb.Clear();
                lp.Clear();
                sb.Append("insert into NPCInfo(Idx,MapIndex,FileName,Name,Location_X,Location_Y,Image,Rate,TimeVisible,HourStart,MinuteStart,HourEnd,MinuteEnd,MinLev,MaxLev,DayofWeek,ClassRequired,Conquest,FlagNeeded) values(@Idx,@MapIndex,@FileName,@Name,@Location_X,@Location_Y,@Image,@Rate,@TimeVisible,@HourStart,@MinuteStart,@HourEnd,@MinuteEnd,@MinLev,@MaxLev,@DayofWeek,@ClassRequired,@Conquest,@FlagNeeded) ");

                lp.Add(new SQLiteParameter("Idx", Index));
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
               

                //执行插入
                MirConfigDB.Execute(sb.ToString(), lp.ToArray());
            }
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

            info.Index = ++SMain.EditEnvir.NPCIndex;
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
