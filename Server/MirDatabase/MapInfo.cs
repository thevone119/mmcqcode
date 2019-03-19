using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Server.MirEnvir;
using System.Data.SQLite;
using System.Text;
using Newtonsoft.Json;
using System.Data.Common;

namespace Server.MirDatabase
{
    /// <summary>
    /// ��ͼ��Ϣ
    /// </summary>
    public class MapInfo
    {
        //����
        public int Index;
        //�ļ�������ͼcode,��ͼ��
        public string FileName = string.Empty, Mcode = string.Empty, Title = string.Empty;
        //С��ͼ�����ͼ����ͼ��������
        public ushort MiniMap, BigMap, Music;
        //�ƹ�
        public LightSetting Light;
        //
        public byte MapDarkLight = 0, MineIndex = 0;

        //�����������ã��Ƿ�������Ƿ��ͣ��Ƿ����PK���Ƿ������Ʒ���Ƿ�������
        public bool NoTeleport, NoReconnect, NoRandom, NoEscape, NoRecall, NoDrug, NoPosition, NoFight,
            NoThrowItem, NoDropPlayer, NoDropMonster, NoNames, NoMount, NeedBridle, Fight, NeedHole, Fire, Lightning;

        public string NoReconnectMap = string.Empty;
        public int FireDamage, LightningDamage;
        //��ͼ�ı��ʣ����鱶�ʣ�ˢ�ֱ���
        public float DropRate = 1F, ExpRate = 1F, MonsterRate = 1.0F;
        //��ȫ����
        public List<SafeZoneInfo> SafeZones = new List<SafeZoneInfo>();
        //����Ǵ�����ô��
        public List<MovementInfo> Movements = new List<MovementInfo>();
        //������Ϣ���ǹ���������
        public List<RespawnInfo> Respawns = new List<RespawnInfo>();
        //NPC
        public List<NPCInfo> NPCs = new List<NPCInfo>();
        //�ڿ�����
        public List<MineZone> MineZones = new List<MineZone>();
        //������ꣿ�����ݿ��ֶΣ�����ĵ㣬��ͨ��Ĭ��NPC����ӽ����ģ����ȥ����Щ�㣬�򴥷�Ĭ��NPC�¼�Ŷ��
        public List<Point> ActiveCoords = new List<Point>();

        //ʵ������
        //[JsonIgnore]
        //public InstanceInfo Instance;

        public MapInfo()
        {

        }

        /// <summary>
        /// ������������
        /// </summary>
        /// <returns></returns>
        public static List<MapInfo> loadAll()
        {
            List<MapInfo> list = new List<MapInfo>();
            DbDataReader read = MirConfigDB.ExecuteReader("select * from MapInfo where isdel=0");

            while (read.Read())
            {
                MapInfo obj = new MapInfo();
                if (read.IsDBNull(read.GetOrdinal("FileName")))
                {
                    continue;
                }
                obj.FileName = read.GetString(read.GetOrdinal("FileName"));
                if (obj.FileName == null)
                {
                    continue;
                }
                obj.Mcode = read.GetString(read.GetOrdinal("Mcode"));
                
                obj.Index = read.GetInt32(read.GetOrdinal("Idx"));
                obj.Title = read.GetString(read.GetOrdinal("Title"));

                obj.MiniMap = (ushort)read.GetInt16(read.GetOrdinal("MiniMap"));
                obj.Light = (LightSetting)read.GetByte(read.GetOrdinal("Light"));
                obj.BigMap = (ushort)read.GetInt16(read.GetOrdinal("BigMap"));

                obj.DropRate = read.GetFloat(read.GetOrdinal("DropRate"));
                obj.ExpRate = read.GetFloat(read.GetOrdinal("ExpRate"));
                obj.MonsterRate = read.GetFloat(read.GetOrdinal("MonsterRate"));


                obj.NoTeleport = read.GetBoolean(read.GetOrdinal("NoTeleport"));
                obj.NoReconnect = read.GetBoolean(read.GetOrdinal("NoReconnect"));
                obj.NoReconnectMap = read.GetString(read.GetOrdinal("NoReconnectMap"));
                obj.NoRandom = read.GetBoolean(read.GetOrdinal("NoRandom"));
                obj.NoEscape = read.GetBoolean(read.GetOrdinal("NoEscape"));
                obj.NoRecall = read.GetBoolean(read.GetOrdinal("NoRecall"));
                obj.NoDrug = read.GetBoolean(read.GetOrdinal("NoDrug"));
                obj.NoPosition = read.GetBoolean(read.GetOrdinal("NoPosition"));
                obj.NoThrowItem = read.GetBoolean(read.GetOrdinal("NoThrowItem"));
                obj.NoDropPlayer = read.GetBoolean(read.GetOrdinal("NoDropPlayer"));
                obj.NoDropMonster = read.GetBoolean(read.GetOrdinal("NoDropMonster"));
                obj.NoNames = read.GetBoolean(read.GetOrdinal("NoNames"));
                obj.Fight = read.GetBoolean(read.GetOrdinal("Fight"));
                obj.Fire = read.GetBoolean(read.GetOrdinal("Fire"));
                obj.FireDamage = read.GetInt32(read.GetOrdinal("FireDamage"));
                obj.Lightning = read.GetBoolean(read.GetOrdinal("Lightning"));
                obj.LightningDamage = read.GetInt32(read.GetOrdinal("LightningDamage"));
                obj.MapDarkLight = read.GetByte(read.GetOrdinal("MapDarkLight"));
                obj.MineIndex = read.GetByte(read.GetOrdinal("MineIndex"));
                obj.NoMount = read.GetBoolean(read.GetOrdinal("NoMount"));
                obj.NeedBridle = read.GetBoolean(read.GetOrdinal("NeedBridle"));
                obj.NoFight = read.GetBoolean(read.GetOrdinal("NoFight"));
                obj.Music = (ushort)read.GetInt16(read.GetOrdinal("Music"));

                obj.SafeZones = JsonConvert.DeserializeObject<List<SafeZoneInfo>>(read.GetString(read.GetOrdinal("SafeZones")));
                //��������
                if (obj.SafeZones != null)
                {
                    for(int i=0;i< obj.SafeZones.Count; i++)
                    {
                        obj.SafeZones[i].MapIndex = obj.Index;
                    }
                }
                else
                {
                    obj.SafeZones = new List<SafeZoneInfo>();
                }

                obj.Movements = JsonConvert.DeserializeObject<List<MovementInfo>>(read.GetString(read.GetOrdinal("Movements")));
                obj.Respawns = JsonConvert.DeserializeObject<List<RespawnInfo>>(read.GetString(read.GetOrdinal("Respawns")));
                obj.MineZones = JsonConvert.DeserializeObject<List<MineZone>>(read.GetString(read.GetOrdinal("MineZones")));
                
                DBObjectUtils.updateObjState(obj, obj.Index);
                list.Add(obj);
            }
            return list;
        }

      
        //���浽���ݿ�
        public void SaveDB()
        {
            byte state = DBObjectUtils.ObjState(this, Index);
            if (state == 0)//û�иı�
            {
                return;
            }
            SMain.Enqueue("MapInfo change state:"+ state);
            List<SQLiteParameter> lp = new List<SQLiteParameter>();
            lp.Add(new SQLiteParameter("Mcode", Mcode));
            lp.Add(new SQLiteParameter("FileName", FileName));
            lp.Add(new SQLiteParameter("Title", Title));
            lp.Add(new SQLiteParameter("MiniMap", MiniMap));
            lp.Add(new SQLiteParameter("Light", Light));
            lp.Add(new SQLiteParameter("BigMap", BigMap));
            lp.Add(new SQLiteParameter("NoTeleport", NoTeleport));
            lp.Add(new SQLiteParameter("NoReconnect", NoReconnect));
            lp.Add(new SQLiteParameter("NoReconnectMap", NoReconnectMap));
            lp.Add(new SQLiteParameter("NoRandom", NoRandom));
            lp.Add(new SQLiteParameter("NoEscape", NoEscape));
            lp.Add(new SQLiteParameter("NoRecall", NoRecall));
            lp.Add(new SQLiteParameter("NoDrug", NoDrug));
            lp.Add(new SQLiteParameter("NoPosition", NoPosition));
            lp.Add(new SQLiteParameter("NoThrowItem", NoThrowItem));
            lp.Add(new SQLiteParameter("NoDropPlayer", NoDropPlayer));
            lp.Add(new SQLiteParameter("NoDropMonster", NoDropMonster));
            lp.Add(new SQLiteParameter("NoNames", NoNames));
            lp.Add(new SQLiteParameter("Fight", Fight));
            lp.Add(new SQLiteParameter("Fire", Fire));
            lp.Add(new SQLiteParameter("FireDamage", FireDamage));
            lp.Add(new SQLiteParameter("Lightning", Lightning));
            lp.Add(new SQLiteParameter("LightningDamage", LightningDamage));
            lp.Add(new SQLiteParameter("MapDarkLight", MapDarkLight));
            lp.Add(new SQLiteParameter("MineIndex", MineIndex));
            lp.Add(new SQLiteParameter("NoMount", NoMount));
            lp.Add(new SQLiteParameter("NeedBridle", NeedBridle));
            lp.Add(new SQLiteParameter("NoFight", NoFight));
            lp.Add(new SQLiteParameter("Music", Music));

            lp.Add(new SQLiteParameter("DropRate", DropRate));
            lp.Add(new SQLiteParameter("ExpRate", ExpRate));
            lp.Add(new SQLiteParameter("MonsterRate", MonsterRate));

            lp.Add(new SQLiteParameter("SafeZones", JsonConvert.SerializeObject(SafeZones)));
            lp.Add(new SQLiteParameter("Movements", JsonConvert.SerializeObject(Movements)));
            lp.Add(new SQLiteParameter("Respawns", JsonConvert.SerializeObject(Respawns)));
            lp.Add(new SQLiteParameter("MineZones", JsonConvert.SerializeObject(MineZones)));

            //����
            if (state == 1)
            {
                if (Index > 0)
                {
                    lp.Add(new SQLiteParameter("Idx", Index));
                }
                string sql = "insert into MapInfo" + SQLiteHelper.createInsertSql(lp.ToArray()); 
                MirConfigDB.Execute(sql, lp.ToArray());
            }
            //�޸�
            if (state == 2)
            {
                string sql = "update MapInfo set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where Idx=@Idx";
                lp.Add(new SQLiteParameter("Idx", Index));
                MirConfigDB.Execute(sql, lp.ToArray());
            }
            DBObjectUtils.updateObjState(this, Index);
        }

        //������ͼ��Ϣ(�ǳ������ڴ�)
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
            /**
            if (Instance == null)
            {
                Instance = new InstanceInfo(this, map);
            }
            **/
            for (int i = 0; i < SafeZones.Count; i++)
                if (SafeZones[i].StartPoint)
                    SMain.Envir.StartPoints.Add(SafeZones[i]);
        }
        //����һ���µ�ʵ��������
        public Map CreateInstance()
        {
            //if (Instance.MapList.Count == 0) return;

            Map map = new Map(this);
            if (!map.Load())
            {
                return null;
            }
            SMain.Envir.MapList.Add(map);
            return map;
            //Instance.AddMap(map);
        }

        public void CreateSafeZone()
        {
            SafeZones.Add(new SafeZoneInfo { MapIndex = this.Index });
        }

        public void CreateRespawnInfo()
        {
            Respawns.Add(new RespawnInfo());
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
                SafeZoneInfo temp = new SafeZoneInfo { MapIndex = info.Index };
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
                //if (!int.TryParse(data[start + 7 + (i * 7)], out temp.RespawnIndex)) return;
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



            info.Index = (int)DBObjectUtils.getObjNextId(info);
            SMain.EditEnvir.MapInfoList.Add(info);
        }
    }

    //����ǵ�ͼ�İ������ɣ������ݿ��еĵ�ͼ��ʵ�ʵ�ͼ����ô��
    //������е�ǰ��ͼ�����ˣ������ﴴ���µ�ʵ��
    //��ʵ��Ϊ��ʱ���ٵ�ͼ-ͼ�еĽ���ѭ�����������
    //�ı�NPC�豸�ƶ���������һ������ʵ��
    //����Ǹ�ë��
    //����Ǹ���ô��
    public class InstanceInfo
    {
        //Constants ������
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
            ������е�ǰӳ�䶼��������Ӵ˴�������ʵ��
            ��ʵ��Ϊ��ʱ����ӳ��-��ӳ���л����ڴ˴�ѭ����
            ��npc instance move����Ϊmove��������һ������ʵ��
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

        //��ȡ��һ�����õ�ʵ��
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