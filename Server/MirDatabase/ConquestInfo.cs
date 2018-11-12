using Newtonsoft.Json;
using Server.MirEnvir;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Text;

namespace Server.MirDatabase
{   
    /// <summary>
    /// 占领》？
    /// 是行会占领么？
    /// 整个行会战斗用这个数据啊。
    /// 不是，这个是领地的意思，可以配置一块领地出来，这个不属于行会的领导，可以配置相关的城墙，门，射手，国王等信息
    /// 这个没什么必要，后期可能单独在土城配置一块区域？
    /// </summary>
    public class ConquestInfo
    {
        public int Index;
        public bool FullMap;
        public Point Location;
        public ushort Size;
        public string Name;
        public int MapIndex;
        public int PalaceIndex;
        public List<int> ExtraMaps = new List<int>();
        public List<ConquestArcherInfo> ConquestGuards = new List<ConquestArcherInfo>();
        public List<ConquestGateInfo> ConquestGates = new List<ConquestGateInfo>();
        public List<ConquestWallInfo> ConquestWalls = new List<ConquestWallInfo>();
        public List<ConquestSiegeInfo> ConquestSieges = new List<ConquestSiegeInfo>();
        public List<ConquestFlagInfo> ConquestFlags = new List<ConquestFlagInfo>();

        public int GuardIndex;
        public int GateIndex;
        public int WallIndex;
        public int SiegeIndex;
        public int FlagIndex;

        public byte StartHour = 0;
        public int WarLength = 60;

        private int counter;

        public ConquestType Type = ConquestType.Request;
        public ConquestGame Game = ConquestGame.CapturePalace;

        //星期1-7
        public bool Monday;
        public bool Tuesday;
        public bool Wednesday;
        public bool Thursday;
        public bool Friday;
        public bool Saturday;
        public bool Sunday;

        //King of the hill
        public Point KingLocation;
        public ushort KingSize;

        //Control points
        public List<ConquestFlagInfo> ControlPoints = new List<ConquestFlagInfo>();
        public int ControlPointIndex;

        public ConquestInfo()
        {

        }

        /// <summary>
        /// 加载所有数据
        /// </summary>
        /// <returns></returns>
        public static List<ConquestInfo> loadAll()
        {
            List<ConquestInfo> list = new List<ConquestInfo>();
            DbDataReader read = MirConfigDB.ExecuteReader("select * from ConquestInfo");

            while (read.Read())
            {
                ConquestInfo obj = new ConquestInfo();
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
                obj.FullMap = read.GetBoolean(read.GetOrdinal("FullMap"));
                
                obj.Location = new Point(read.GetInt16(read.GetOrdinal("Location_X")), read.GetInt16(read.GetOrdinal("Location_Y")));

                obj.Size = (ushort)read.GetInt16(read.GetOrdinal("Size"));
                obj.MapIndex = read.GetInt32(read.GetOrdinal("MapIndex"));


                obj.PalaceIndex = read.GetInt32(read.GetOrdinal("PalaceIndex"));
                obj.GuardIndex = read.GetInt32(read.GetOrdinal("GuardIndex"));
                obj.GateIndex = read.GetInt32(read.GetOrdinal("GateIndex"));
                obj.WallIndex = read.GetInt32(read.GetOrdinal("WallIndex"));
                obj.SiegeIndex = read.GetInt32(read.GetOrdinal("SiegeIndex"));
                obj.FlagIndex = read.GetInt32(read.GetOrdinal("FlagIndex"));
                obj.StartHour = read.GetByte(read.GetOrdinal("StartHour"));
                obj.WarLength = read.GetInt32(read.GetOrdinal("WarLength"));
                obj.Type = (ConquestType)read.GetByte(read.GetOrdinal("Type"));
                obj.Game = (ConquestGame)read.GetByte(read.GetOrdinal("Game"));
                obj.Monday = read.GetBoolean(read.GetOrdinal("Monday"));
                obj.Tuesday = read.GetBoolean(read.GetOrdinal("Tuesday"));
                obj.Wednesday = read.GetBoolean(read.GetOrdinal("Wednesday"));
                obj.Thursday = read.GetBoolean(read.GetOrdinal("Thursday"));
                obj.Friday = read.GetBoolean(read.GetOrdinal("Friday"));
                obj.Saturday = read.GetBoolean(read.GetOrdinal("Saturday"));
                obj.Sunday = read.GetBoolean(read.GetOrdinal("Sunday"));

                obj.KingLocation = new Point(read.GetInt16(read.GetOrdinal("KingLocation_X")), read.GetInt16(read.GetOrdinal("KingLocation_Y")));
                obj.KingSize= (ushort)read.GetInt32(read.GetOrdinal("KingSize"));
                obj.ControlPointIndex = read.GetInt32(read.GetOrdinal("ControlPointIndex"));
                obj.ConquestGuards = JsonConvert.DeserializeObject<List<ConquestArcherInfo>>(read.GetString(read.GetOrdinal("ConquestGuards")));
                obj.ExtraMaps = JsonConvert.DeserializeObject<List<int>>(read.GetString(read.GetOrdinal("ExtraMaps")));
                obj.ConquestGates = JsonConvert.DeserializeObject<List<ConquestGateInfo>>(read.GetString(read.GetOrdinal("ConquestGates")));
                obj.ConquestWalls = JsonConvert.DeserializeObject<List<ConquestWallInfo>>(read.GetString(read.GetOrdinal("ConquestWalls")));
                obj.ConquestSieges = JsonConvert.DeserializeObject<List<ConquestSiegeInfo>>(read.GetString(read.GetOrdinal("ConquestSieges")));
                obj.ConquestFlags = JsonConvert.DeserializeObject<List<ConquestFlagInfo>>(read.GetString(read.GetOrdinal("ConquestFlags")));
                obj.ControlPoints = JsonConvert.DeserializeObject<List<ConquestFlagInfo>>(read.GetString(read.GetOrdinal("ControlPoints")));

                DBObjectUtils.updateObjState(obj, obj.Index);
                list.Add(obj);
            }
            return list;
        }

        public ConquestInfo(BinaryReader reader)
        {
            Index = reader.ReadInt32();

            if(Envir.LoadVersion > 73)
            {
                FullMap = reader.ReadBoolean();
            }

            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Size = reader.ReadUInt16();
            Name = reader.ReadString();
            MapIndex = reader.ReadInt32();
            PalaceIndex = reader.ReadInt32();
            GuardIndex = reader.ReadInt32();
            GateIndex = reader.ReadInt32();
            WallIndex = reader.ReadInt32();
            SiegeIndex = reader.ReadInt32();

            if (Envir.LoadVersion > 72)
            {
                FlagIndex = reader.ReadInt32();
            }

            counter = reader.ReadInt32();
            for (int i = 0; i < counter; i++)
            {
                ConquestGuards.Add(new ConquestArcherInfo(reader));
            }
            counter = reader.ReadInt32();
            for (int i = 0; i < counter; i++)
            {
                ExtraMaps.Add(reader.ReadInt32());
            }
            counter = reader.ReadInt32();
            for (int i = 0; i < counter; i++)
            {
                ConquestGates.Add(new ConquestGateInfo(reader));
            }
            counter = reader.ReadInt32();
            for (int i = 0; i < counter; i++)
            {
                ConquestWalls.Add(new ConquestWallInfo(reader));
            }
            counter = reader.ReadInt32();
            for (int i = 0; i < counter; i++)
            {
                ConquestSieges.Add(new ConquestSiegeInfo(reader));
            }

            if (Envir.LoadVersion > 72)
            {
                counter = reader.ReadInt32();
                for (int i = 0; i < counter; i++)
                {
                    ConquestFlags.Add(new ConquestFlagInfo(reader));
                }
            }

            StartHour = reader.ReadByte();
            WarLength = reader.ReadInt32();
            Type = (ConquestType)reader.ReadByte();
            Game = (ConquestGame)reader.ReadByte();

            Monday = reader.ReadBoolean();
            Tuesday = reader.ReadBoolean();
            Wednesday = reader.ReadBoolean();
            Thursday = reader.ReadBoolean();
            Friday = reader.ReadBoolean();
            Saturday = reader.ReadBoolean();
            Sunday = reader.ReadBoolean();

            KingLocation = new Point(reader.ReadInt32(), reader.ReadInt32());
            KingSize = reader.ReadUInt16();

            if (Envir.LoadVersion > 74)
            {
                ControlPointIndex = reader.ReadInt32();
                counter = reader.ReadInt32();
                for (int i = 0; i < counter; i++)
                {
                    ControlPoints.Add(new ConquestFlagInfo(reader));
                }
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(FullMap);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(Size);
            writer.Write(Name);
            writer.Write(MapIndex);
            writer.Write(PalaceIndex);
            writer.Write(GuardIndex);
            writer.Write(GateIndex);
            writer.Write(WallIndex);
            writer.Write(SiegeIndex);
            writer.Write(FlagIndex);

            writer.Write(ConquestGuards.Count);
            for (int i = 0; i < ConquestGuards.Count; i++)
            {
                ConquestGuards[i].Save(writer);
            }
            writer.Write(ExtraMaps.Count);
            for (int i = 0; i < ExtraMaps.Count; i++)
            {
                writer.Write(ExtraMaps[i]);
            }
            writer.Write(ConquestGates.Count);
            for (int i = 0; i < ConquestGates.Count; i++)
            {
                ConquestGates[i].Save(writer);
            }
            writer.Write(ConquestWalls.Count);
            for (int i = 0; i < ConquestWalls.Count; i++)
            {
                ConquestWalls[i].Save(writer);
            }
            writer.Write(ConquestSieges.Count);
            for (int i = 0; i < ConquestSieges.Count; i++)
            {
                ConquestSieges[i].Save(writer);
            }

            writer.Write(ConquestFlags.Count);
            for (int i = 0; i < ConquestFlags.Count; i++)
            {
                ConquestFlags[i].Save(writer);
            }
            writer.Write(StartHour);
            writer.Write(WarLength);
            writer.Write((byte)Type);
            writer.Write((byte)Game);

            writer.Write(Monday);
            writer.Write(Tuesday);
            writer.Write(Wednesday);
            writer.Write(Thursday);
            writer.Write(Friday);
            writer.Write(Saturday);
            writer.Write(Sunday);

            writer.Write(KingLocation.X);
            writer.Write(KingLocation.Y);
            writer.Write(KingSize);

            writer.Write(ControlPointIndex);
            writer.Write(ControlPoints.Count);
            for (int i = 0; i < ControlPoints.Count; i++)
            {
                ControlPoints[i].Save(writer);
            }
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
            List<SQLiteParameter> lp = new List<SQLiteParameter>();
            lp.Add(new SQLiteParameter("FullMap", FullMap));
            lp.Add(new SQLiteParameter("Location_X", Location.X));
            lp.Add(new SQLiteParameter("Location_Y", Location.Y));
            lp.Add(new SQLiteParameter("Size", Size));
            lp.Add(new SQLiteParameter("Name", Name));
            lp.Add(new SQLiteParameter("MapIndex", MapIndex));
            lp.Add(new SQLiteParameter("PalaceIndex", PalaceIndex));
            lp.Add(new SQLiteParameter("GuardIndex", GuardIndex));
            lp.Add(new SQLiteParameter("GateIndex", GateIndex));
            lp.Add(new SQLiteParameter("WallIndex", WallIndex));
            lp.Add(new SQLiteParameter("SiegeIndex", SiegeIndex));
            lp.Add(new SQLiteParameter("FlagIndex", FlagIndex));
            lp.Add(new SQLiteParameter("StartHour", StartHour));
            lp.Add(new SQLiteParameter("WarLength", WarLength));
            lp.Add(new SQLiteParameter("Type", Type));
            lp.Add(new SQLiteParameter("Game", Game));
            lp.Add(new SQLiteParameter("Monday", Monday));
            lp.Add(new SQLiteParameter("Tuesday", Tuesday));
            lp.Add(new SQLiteParameter("Wednesday", Wednesday));
            lp.Add(new SQLiteParameter("Thursday", Thursday));
            lp.Add(new SQLiteParameter("Friday", Friday));
            lp.Add(new SQLiteParameter("Saturday", Saturday));
            lp.Add(new SQLiteParameter("Sunday", Sunday));
            lp.Add(new SQLiteParameter("KingLocation_X", KingLocation.X));
            lp.Add(new SQLiteParameter("KingLocation_Y", KingLocation.Y));
            lp.Add(new SQLiteParameter("KingSize", KingSize));
            lp.Add(new SQLiteParameter("ControlPointIndex", ControlPointIndex));
            //以下都是集合
            lp.Add(new SQLiteParameter("ConquestGuards", JsonConvert.SerializeObject(ConquestGuards)));
            lp.Add(new SQLiteParameter("ExtraMaps", JsonConvert.SerializeObject(ExtraMaps)));
            lp.Add(new SQLiteParameter("ConquestGates", JsonConvert.SerializeObject(ConquestGates)));
            lp.Add(new SQLiteParameter("ConquestWalls", JsonConvert.SerializeObject(ConquestWalls)));
            lp.Add(new SQLiteParameter("ConquestSieges", JsonConvert.SerializeObject(ConquestSieges)));
            lp.Add(new SQLiteParameter("ConquestFlags", JsonConvert.SerializeObject(ConquestFlags)));
            lp.Add(new SQLiteParameter("ControlPoints", JsonConvert.SerializeObject(ControlPoints)));

            //新增
            if (state == 1)
            {
                if (Index > 0)
                {
                    lp.Add(new SQLiteParameter("Idx", Index));
                }
                string sql = "insert into ConquestInfo" + SQLiteHelper.createInsertSql(lp.ToArray()); ;
                MirConfigDB.Execute(sql, lp.ToArray());
            }
            //修改
            if (state == 2)
            {
                string sql = "update ConquestInfo set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where Idx=@Idx";
                lp.Add(new SQLiteParameter("Idx", Index));
                MirConfigDB.Execute(sql, lp.ToArray());
            }
            
            DBObjectUtils.updateObjState(this, Index);
        }

        public override string ToString()
        {
            return string.Format("{0}- {1}", Index, Name);
        }
    }

    //围攻？
    public class ConquestSiegeInfo
    {
        public int Index;
        public Point Location;
        public int MobIndex;
        public string Name;
        public uint RepairCost;

        public ConquestSiegeInfo()
        {

        }

        public ConquestSiegeInfo(BinaryReader reader)
        {
            Index = reader.ReadInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            MobIndex = reader.ReadInt32();
            Name = reader.ReadString();
            RepairCost = reader.ReadUInt32();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(MobIndex);
            writer.Write(Name);
            writer.Write(RepairCost);
        }

        public override string ToString()
        {
            return string.Format("{0} - {1} ({2})", Index, Name, Location);
        }


    }
    //城墙？
    public class ConquestWallInfo
    {
        public int Index;
        public Point Location;
        public int MobIndex;
        public string Name;
        public uint RepairCost;

        public ConquestWallInfo()
        {

        }

        public ConquestWallInfo(BinaryReader reader)
        {
            Index = reader.ReadInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            MobIndex = reader.ReadInt32();
            Name = reader.ReadString();
            RepairCost = reader.ReadUInt32();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(MobIndex);
            writer.Write(Name);
            writer.Write(RepairCost);
        }

        public override string ToString()
        {
            return string.Format("{0} - {1} ({2})", Index, Name, Location);
        }


    }

    //行会战的门？
    public class ConquestGateInfo
    {
        public int Index;
        public Point Location;//位置
        public int MobIndex;
        public string Name;//名称
        public uint RepairCost;//修理费用

        public ConquestGateInfo()
        {

        }

        public ConquestGateInfo(BinaryReader reader)
        {
            Index = reader.ReadInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            MobIndex = reader.ReadInt32();
            Name = reader.ReadString();
            RepairCost = reader.ReadUInt32();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(MobIndex);
            writer.Write(Name);
            writer.Write(RepairCost);
        }

        public override string ToString()
        {
            return string.Format("{0} - {1} ({2})", Index, Name, Location);
        }


    }
    //行会战的射手信息
    public class ConquestArcherInfo
    {
        public int Index;
        public Point Location;
        public int MobIndex;
        public string Name;
        public uint RepairCost;

        public ConquestArcherInfo()
        {

        }

        public ConquestArcherInfo(BinaryReader reader)
        {
            Index = reader.ReadInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            MobIndex = reader.ReadInt32();
            Name = reader.ReadString();
            RepairCost = reader.ReadUInt32();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(MobIndex);
            writer.Write(Name);
            writer.Write(RepairCost);
        }

        public override string ToString()
        {
            return string.Format("{0} - {1} ({2})", Index, Name, Location);
        }


    }

    public class ConquestFlagInfo
    {
        public int Index;
        public Point Location;
        public string Name;
        public string FileName = string.Empty;

        public ConquestFlagInfo()
        {

        }

        public ConquestFlagInfo(BinaryReader reader)
        {
            Index = reader.ReadInt32();
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Name = reader.ReadString();
            FileName = reader.ReadString();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(Name);
            writer.Write(FileName);
        }

        public override string ToString()
        {
            return string.Format("{0} - {1} ({2})", Index, Name, Location);
        }
    }
}
