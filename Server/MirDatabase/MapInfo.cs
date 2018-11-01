using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Server.MirEnvir;
using System.Data.SQLite;
using System.Text;
using Newtonsoft.Json;

namespace Server.MirDatabase
{
    /// <summary>
    /// 地图信息
    /// </summary>
    public class MapInfo
    {
        //索引
        public int Index;
        //文件名，地图名
        public string FileName = string.Empty, Title = string.Empty;
        //小地图，大地图，地图背景音乐
        public ushort MiniMap, BigMap, Music;
        //灯光
        public LightSetting Light;
        //
        public byte MapDarkLight = 0, MineIndex = 0;

        //各种属性配置，是否随机，是否传送，是否可以PK，是否掉落物品，是否可骑马等
        public bool NoTeleport, NoReconnect, NoRandom, NoEscape, NoRecall, NoDrug, NoPosition, NoFight,
            NoThrowItem, NoDropPlayer, NoDropMonster, NoNames, NoMount, NeedBridle, Fight, NeedHole, Fire, Lightning;

        public string NoReconnectMap = string.Empty;
        public int FireDamage, LightningDamage;
        //安全区域
        public List<SafeZoneInfo> SafeZones = new List<SafeZoneInfo>();
        //这个是传送门么？
        public List<MovementInfo> Movements = new List<MovementInfo>();
        //重生信息，是怪物重生吧
        public List<RespawnInfo> Respawns = new List<RespawnInfo>();
        //NPC
        public List<NPCInfo> NPCs = new List<NPCInfo>();
        //挖矿区域
        public List<MineZone> MineZones = new List<MineZone>();
        //活动的坐标？非数据库字段
        public List<Point> ActiveCoords = new List<Point>();
        //实例包裹
        public InstanceInfo Instance;

        public MapInfo()
        {

        }

        public MapInfo(BinaryReader reader)
        {
            Index = reader.ReadInt32();
            FileName = reader.ReadString();
            Title = reader.ReadString();
            MiniMap = reader.ReadUInt16();
            Light = (LightSetting) reader.ReadByte();

            if (Envir.LoadVersion >= 3) BigMap = reader.ReadUInt16();

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                SafeZones.Add(new SafeZoneInfo(reader) { Info = this });

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                Respawns.Add(new RespawnInfo(reader, Envir.LoadVersion, Envir.LoadCustomVersion));

            if (Envir.LoadVersion <= 33)
            {
                count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    NPCs.Add(new NPCInfo(reader));
            }

            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                Movements.Add(new MovementInfo(reader));

            if (Envir.LoadVersion < 14) return;

            NoTeleport = reader.ReadBoolean();
            NoReconnect = reader.ReadBoolean();
            NoReconnectMap = reader.ReadString();
            NoRandom = reader.ReadBoolean();
            NoEscape = reader.ReadBoolean();
            NoRecall = reader.ReadBoolean();
            NoDrug = reader.ReadBoolean();
            NoPosition = reader.ReadBoolean();
            NoThrowItem = reader.ReadBoolean();
            NoDropPlayer = reader.ReadBoolean();
            NoDropMonster = reader.ReadBoolean();
            NoNames = reader.ReadBoolean();
            Fight = reader.ReadBoolean();
            if (Envir.LoadVersion == 14) NeedHole = reader.ReadBoolean();
            Fire = reader.ReadBoolean();
            FireDamage = reader.ReadInt32();
            Lightning = reader.ReadBoolean();
            LightningDamage = reader.ReadInt32();
            if (Envir.LoadVersion < 23) return;
            MapDarkLight = reader.ReadByte();
            if (Envir.LoadVersion < 26) return;
            count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
                MineZones.Add(new MineZone(reader));
            if (Envir.LoadVersion < 27) return;
            MineIndex = reader.ReadByte();

            if (Envir.LoadVersion < 33) return;
            NoMount = reader.ReadBoolean();
            NeedBridle = reader.ReadBoolean();

            if (Envir.LoadVersion < 42) return;
            NoFight = reader.ReadBoolean();

            if (Envir.LoadVersion < 53) return;
                Music = reader.ReadUInt16(); 

        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Index);
            writer.Write(FileName);
            writer.Write(Title);
            writer.Write(MiniMap);
            writer.Write((byte)Light);
            writer.Write(BigMap);
            writer.Write(SafeZones.Count);

            for (int i = 0; i < SafeZones.Count; i++)
                SafeZones[i].Save(writer);

            writer.Write(Respawns.Count);
            for (int i = 0; i < Respawns.Count; i++)
                Respawns[i].Save(writer);

            writer.Write(Movements.Count);
            for (int i = 0; i < Movements.Count; i++)
                Movements[i].Save(writer);

            writer.Write(NoTeleport);
            writer.Write(NoReconnect);
            writer.Write(NoReconnectMap);
            writer.Write(NoRandom);
            writer.Write(NoEscape);
            writer.Write(NoRecall);
            writer.Write(NoDrug);
            writer.Write(NoPosition);
            writer.Write(NoThrowItem);
            writer.Write(NoDropPlayer);
            writer.Write(NoDropMonster);
            writer.Write(NoNames);
            writer.Write(Fight);
            writer.Write(Fire);
            writer.Write(FireDamage);
            writer.Write(Lightning);
            writer.Write(LightningDamage);
            writer.Write(MapDarkLight);
            writer.Write(MineZones.Count);
            for (int i = 0; i < MineZones.Count; i++)
                MineZones[i].Save(writer);
            writer.Write(MineIndex);

            writer.Write(NoMount);
            writer.Write(NeedBridle);

            writer.Write(NoFight);

            writer.Write(Music);
            SaveDB();
        }

        //保存到数据库
        public void SaveDB()
        {
            StringBuilder sb = new StringBuilder();
            List<SQLiteParameter> lp = new List<SQLiteParameter>();
            sb.Append("update MapInfo set ");

            sb.Append(" FileName=@FileName, "); lp.Add(new SQLiteParameter("FileName", FileName));
            sb.Append(" Title=@Title, "); lp.Add(new SQLiteParameter("Title", Title));
            sb.Append(" MiniMap=@MiniMap, "); lp.Add(new SQLiteParameter("MiniMap", MiniMap));
            sb.Append(" Light=@Light, "); lp.Add(new SQLiteParameter("Light", Light));
            sb.Append(" BigMap=@BigMap, "); lp.Add(new SQLiteParameter("BigMap", BigMap));
            sb.Append(" NoTeleport=@NoTeleport, "); lp.Add(new SQLiteParameter("NoTeleport", NoTeleport));
            sb.Append(" NoReconnect=@NoReconnect, "); lp.Add(new SQLiteParameter("NoReconnect", NoReconnect));
            sb.Append(" NoReconnectMap=@NoReconnectMap, "); lp.Add(new SQLiteParameter("NoReconnectMap", NoReconnectMap));
            sb.Append(" NoRandom=@NoRandom, "); lp.Add(new SQLiteParameter("NoRandom", NoRandom));
            sb.Append(" NoEscape=@NoEscape, "); lp.Add(new SQLiteParameter("NoEscape", NoEscape));
            sb.Append(" NoRecall=@NoRecall, "); lp.Add(new SQLiteParameter("NoRecall", NoRecall));
            sb.Append(" NoDrug=@NoDrug, "); lp.Add(new SQLiteParameter("NoDrug", NoDrug));
            sb.Append(" NoPosition=@NoPosition, "); lp.Add(new SQLiteParameter("NoPosition", NoPosition));
            sb.Append(" NoThrowItem=@NoThrowItem, "); lp.Add(new SQLiteParameter("NoThrowItem", NoThrowItem));
            sb.Append(" NoDropPlayer=@NoDropPlayer, "); lp.Add(new SQLiteParameter("NoDropPlayer", NoDropPlayer));
            sb.Append(" NoDropMonster=@NoDropMonster, "); lp.Add(new SQLiteParameter("NoDropMonster", NoDropMonster));
            sb.Append(" NoNames=@NoNames, "); lp.Add(new SQLiteParameter("NoNames", NoNames));
            sb.Append(" Fight=@Fight, "); lp.Add(new SQLiteParameter("Fight", Fight));
            sb.Append(" Fire=@Fire, "); lp.Add(new SQLiteParameter("Fire", Fire));
            sb.Append(" FireDamage=@FireDamage, "); lp.Add(new SQLiteParameter("FireDamage", FireDamage));
            sb.Append(" Lightning=@Lightning, "); lp.Add(new SQLiteParameter("Lightning", Lightning));
            sb.Append(" LightningDamage=@LightningDamage, "); lp.Add(new SQLiteParameter("LightningDamage", LightningDamage));
            sb.Append(" MapDarkLight=@MapDarkLight, "); lp.Add(new SQLiteParameter("MapDarkLight", MapDarkLight));
            sb.Append(" MineIndex=@MineIndex, "); lp.Add(new SQLiteParameter("MineIndex", MineIndex));
            sb.Append(" NoMount=@NoMount, "); lp.Add(new SQLiteParameter("NoMount", NoMount));
            sb.Append(" NeedBridle=@NeedBridle, "); lp.Add(new SQLiteParameter("NeedBridle", NeedBridle));
            sb.Append(" NoFight=@NoFight, "); lp.Add(new SQLiteParameter("NoFight", NoFight));
            sb.Append(" Music=@Music, "); lp.Add(new SQLiteParameter("Music", Music));

        
          
            sb.Append(" SafeZones=@SafeZones, "); lp.Add(new SQLiteParameter("SafeZones", JsonConvert.SerializeObject(SafeZones)));
            sb.Append(" Movements=@Movements, "); lp.Add(new SQLiteParameter("Movements", JsonConvert.SerializeObject(Movements)));
            sb.Append(" Respawns=@Respawns, "); lp.Add(new SQLiteParameter("Respawns", JsonConvert.SerializeObject(Respawns)));
            sb.Append(" MineZones=@MineZones "); lp.Add(new SQLiteParameter("MineZones", JsonConvert.SerializeObject(MineZones)));

            sb.Append(" where Idx=@Idx "); lp.Add(new SQLiteParameter("Idx", Index));


            //执行更新
            int ucount = MirConfigDB.Execute(sb.ToString(), lp.ToArray());

            //没有得更新，则执行插入
            if (ucount <= 0)
            {
                sb.Clear();
                sb.Append("insert into MapInfo(Idx,FileName,Title,MiniMap,Light,BigMap,NoTeleport,NoReconnect,NoReconnectMap,NoRandom,NoEscape,NoRecall,NoDrug,NoPosition,NoThrowItem,NoDropPlayer,NoDropMonster,NoNames,Fight,Fire,FireDamage,Lightning,LightningDamage,MapDarkLight,MineIndex,NoMount,NeedBridle,NoFight,Music,SafeZones,Movements,Respawns,MineZones) values(@Idx,@FileName,@Title,@MiniMap,@Light,@BigMap,@NoTeleport,@NoReconnect,@NoReconnectMap,@NoRandom,@NoEscape,@NoRecall,@NoDrug,@NoPosition,@NoThrowItem,@NoDropPlayer,@NoDropMonster,@NoNames,@Fight,@Fire,@FireDamage,@Lightning,@LightningDamage,@MapDarkLight,@MineIndex,@NoMount,@NeedBridle,@NoFight,@Music,@SafeZones,@Movements,@Respawns,@MineZones) ");
                //执行插入
                MirConfigDB.Execute(sb.ToString(), lp.ToArray());
            }
        }


        public void CreateMap()
        {
            for (int j = 0; j < SMain.Envir.NPCInfoList.Count; j++)
            {
                if (SMain.Envir.NPCInfoList[j].MapIndex != Index) continue;

                NPCs.Add(SMain.Envir.NPCInfoList[j]);
            }

            Map map = new Map(this);

            if (!map.Load()) return;

            SMain.Envir.MapList.Add(map);

            if (Instance == null)
            {
                Instance = new InstanceInfo(this, map);
            }

            for (int i = 0; i < SafeZones.Count; i++)
                if (SafeZones[i].StartPoint)
                    SMain.Envir.StartPoints.Add(SafeZones[i]);
        }

        public void CreateInstance()
        {
            if (Instance.MapList.Count == 0) return;

            Map map = new Map(this);
            if (!map.Load()) return;

            SMain.Envir.MapList.Add(map);

            Instance.AddMap(map);
        }

        public void CreateSafeZone()
        {
            SafeZones.Add(new SafeZoneInfo { Info = this });
        }

        public void CreateRespawnInfo()
        {
            Respawns.Add(new RespawnInfo { RespawnIndex = ++SMain.EditEnvir.RespawnIndex });
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Index, Title);
        }

        public void CreateNPCInfo()
        {
            NPCs.Add(new NPCInfo());
        }

        public void CreateMovementInfo()
        {
            Movements.Add(new MovementInfo());
        }

        public static void FromText(string text)
        {
            string[] data = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (data.Length < 8) return;

            MapInfo info = new MapInfo {FileName = data[0], Title = data[1]};


            if (!ushort.TryParse(data[2], out info.MiniMap)) return;

            if (!Enum.TryParse(data[3], out info.Light)) return;
            int sziCount, miCount, riCount, npcCount;

            if (!int.TryParse(data[4], out sziCount)) return;
            if (!int.TryParse(data[5], out miCount)) return;
            if (!int.TryParse(data[6], out riCount)) return;
            if (!int.TryParse(data[7], out npcCount)) return;


            int start = 8;

            for (int i = 0; i < sziCount; i++)
            {
                SafeZoneInfo temp = new SafeZoneInfo { Info = info };
                int x, y;

                if (!int.TryParse(data[start + (i * 4)], out x)) return;
                if (!int.TryParse(data[start + 1 + (i * 4)], out y)) return;
                if (!ushort.TryParse(data[start + 2 + (i * 4)], out temp.Size)) return;
                if (!bool.TryParse(data[start + 3 + (i * 4)], out temp.StartPoint)) return;

                temp.Location = new Point(x, y);
                info.SafeZones.Add(temp);
            }
            start += sziCount * 4;



            for (int i = 0; i < miCount; i++)
            {
                MovementInfo temp = new MovementInfo();
                int x, y;

                if (!int.TryParse(data[start + (i * 5)], out x)) return;
                if (!int.TryParse(data[start + 1 + (i * 5)], out y)) return;
                temp.Source = new Point(x, y);

                if (!int.TryParse(data[start + 2 + (i * 5)], out temp.MapIndex)) return;

                if (!int.TryParse(data[start + 3 + (i * 5)], out x)) return;
                if (!int.TryParse(data[start + 4 + (i * 5)], out y)) return;
                temp.Destination = new Point(x, y);

                info.Movements.Add(temp);
            }
            start += miCount * 5;


            for (int i = 0; i < riCount; i++)
            {
                RespawnInfo temp = new RespawnInfo();
                int x, y;

                if (!int.TryParse(data[start + (i * 7)], out temp.MonsterIndex)) return;
                if (!int.TryParse(data[start + 1 + (i * 7)], out x)) return;
                if (!int.TryParse(data[start + 2 + (i * 7)], out y)) return;

                temp.Location = new Point(x, y);

                if (!ushort.TryParse(data[start + 3 + (i * 7)], out temp.Count)) return;
                if (!ushort.TryParse(data[start + 4 + (i * 7)], out temp.Spread)) return;
                if (!ushort.TryParse(data[start + 5 + (i * 7)], out temp.Delay)) return;
                if (!byte.TryParse(data[start + 6 + (i * 7)], out temp.Direction)) return;
                if (!int.TryParse(data[start + 7 + (i * 7)], out temp.RespawnIndex)) return;
                if (!bool.TryParse(data[start + 8 + (i * 7)], out temp.SaveRespawnTime)) return;
                if (!ushort.TryParse(data[start + 9 + (i * 7)], out temp.RespawnTicks)) return;

                info.Respawns.Add(temp);
            }
            start += riCount * 7;


            for (int i = 0; i < npcCount; i++)
            {
                NPCInfo temp = new NPCInfo { FileName = data[start + (i * 6)], Name = data[start + 1 + (i * 6)] };
                int x, y;

                if (!int.TryParse(data[start + 2 + (i * 6)], out x)) return;
                if (!int.TryParse(data[start + 3 + (i * 6)], out y)) return;

                temp.Location = new Point(x, y);

                if (!ushort.TryParse(data[start + 4 + (i * 6)], out temp.Rate)) return;
                if (!ushort.TryParse(data[start + 5 + (i * 6)], out temp.Image)) return;

                info.NPCs.Add(temp);
            }



            info.Index = ++SMain.EditEnvir.MapIndex;
            SMain.EditEnvir.MapInfoList.Add(info);
        }
    }

    //这个是地图的包裹器吧，把数据库中的地图和实际地图包裹么？
    //如果所有当前地图都满了，从这里创建新的实例
    //当实例为空时销毁地图-图中的进程循环还是在这里？
    //改变NPC设备移动并创建下一个可用实例
    //这个是干毛啊
    //这个是副本么？
    public class InstanceInfo
    {
        //Constants
        public int PlayerCap = 2;
        public int MaxInstanceCount = 10;

        //
        public MapInfo MapInfo;
        public List<Map> MapList = new List<Map>();

        /*
         Notes
         Create new instance from here if all current maps are full
         Destroy maps when instance is empty - process loop in map or here?
         Change NPC INSTANCEMOVE to move and create next available instance

        */

        public InstanceInfo(MapInfo mapInfo, Map map)
        {
            MapInfo = mapInfo;
            AddMap(map);
        }

        public void AddMap(Map map)
        {
            MapList.Add(map);
        }

        public void RemoveMap(Map map)
        {
            MapList.Remove(map);
        }

        //获取第一个可用的实例
        public Map GetFirstAvailableInstance()
        {
            for (int i = 0; i < MapList.Count; i++)
            {
                Map m = MapList[i];

                if (m.Players.Count < PlayerCap) return m;
            }

            return null;
        }

        public void CreateNewInstance()
        {
            MapInfo.CreateInstance();
        }
    }
}