using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Server.MirDatabase;
using Server.MirObjects;
using S = ServerPackets;

namespace Server.MirEnvir
{
    //地图数据全部在这里啊。
    //地图是所有处理的入口啊
    /// <summary>
    /// 地图属性每格用一个byte表示，一个byte是8位，0-255，
    /// 最后1位表示是否可以行走(0:不能，1：可以）0或1
    /// 倒数第2位表示是否可以飞行（低0,1高）0或2
    /// 倒数第3位表示是否可以钓鱼4（0：不能，1能）0或4
    /// </summary>
    public class Map
    {
        //计算总格子数，这个格子最消耗资源了
        public static int cellcount = 0;

        //高低墙定义
        private static byte HighWall = 2;
        private static byte LowWall = 0;
        private static byte Fishing = 4;

        private static Envir Envir
        {
            get { return SMain.Envir; }
        }
        //地图数据库信息
        public MapInfo Info;
        //处理地图的线程，多个地图可能采用多线程进行处理
        public int Thread = 0;
        //地图的一些基础属
        public int Width, Height;

        //public Cell[,] Cells;//地图的格子(这个超级消耗资源，内存消耗就优化这个就好了)

        //对应格子的对象，大部分都是空的
        public List<MapObject>[,] Objects;

        public byte[,] Cells;//所有格子的属性，byte表示，


        //public Door[,] DoorIndex;//门索引
        public List<Door> Doors = new List<Door>();
        //矿区（为了节省资源，一开始不创建所有的矿区，挖的时候才创建某个矿区）
        private Dictionary<string, MineSpot> MineDic = new Dictionary<string, MineSpot>();
        //雷电时间，地火时间，空闲时间
        public long LightningTime, FireTime, InactiveTime;
        //怪物总数
        public int MonsterCount;
        //空闲总次数，一开始默认空闲100
        public uint InactiveCount=100;
        //NPC
        public List<NPCObject> NPCs = new List<NPCObject>();
        //玩家
        public List<PlayerObject> Players = new List<PlayerObject>();
        //重生信息,怪物重生啊(这里非常消耗内存啊)
        public List<MapRespawn> Respawns = new List<MapRespawn>();
        //动作列表
        public List<DelayedAction> ActionList = new List<DelayedAction>();
        //征服，战争信息
        public List<ConquestObject> Conquest = new List<ConquestObject>();
        public ConquestObject tempConquest;
        //地图加载的时候，随机产生10个可以访问的点，用于随机
        List<Point> RandomValidPoints = new List<Point>();

        public Map(MapInfo info)
        {
            Info = info;
            Thread = RandomUtils.Next(Settings.ThreadLimit);
        }

        public Door AddDoor(byte DoorIndex, Point location)
        {
            DoorIndex = (byte)(DoorIndex & 0x7F);
            for (int i = 0; i < Doors.Count; i++)
                if (Doors[i].index == DoorIndex)
                    return Doors[i];
            Door DoorInfo = new Door() { index = DoorIndex, Location = location };
            Doors.Add(DoorInfo);
            return DoorInfo;
        }
        
        public bool OpenDoor(byte DoorIndex)
        {
            for (int i = 0; i < Doors.Count; i++)
                if (Doors[i].index == DoorIndex)
                {
                    Doors[i].DoorState = 2;
                    Doors[i].LastTick = Envir.Time;
                    return true;
                }
            return false;
        }

        private byte FindType(byte[] input)
        {
            //c# custom map format
            if ((input[2] == 0x43) && (input[3] == 0x23))
            {
                return 100;
            }
            //wemade mir3 maps have no title they just start with blank bytes
            if (input[0] == 0)
                return 5;
            //shanda mir3 maps start with title: (C) SNDA, MIR3.
            if ((input[0] == 0x0F) && (input[5] == 0x53) && (input[14] == 0x33))
                return 6;

            //wemades antihack map (laby maps) title start with: Mir2 AntiHack
            if ((input[0] == 0x15) && (input[4] == 0x32) && (input[6] == 0x41) && (input[19] == 0x31))
                return 4;

            //wemades 2010 map format i guess title starts with: Map 2010 Ver 1.0
            if ((input[0] == 0x10) && (input[2] == 0x61) && (input[7] == 0x31) && (input[14] == 0x31))
                return 1;

            //shanda's 2012 format and one of shandas(wemades) older formats share same header info, only difference is the filesize
            if ((input[4] == 0x0F) && (input[18] == 0x0D) && (input[19] == 0x0A))
            {
                int W = input[0] + (input[1] << 8);
                int H = input[2] + (input[3] << 8);
                if (input.Length > (52 + (W * H * 14)))
                    return 3;
                else
                    return 2;
            }

            //3/4 heroes map format (myth/lifcos i guess)
            if ((input[0] == 0x0D) && (input[1] == 0x4C) && (input[7] == 0x20) && (input[11] == 0x6D))
                return 7;
            return 0;
        }

        private void LoadMapCellsv0(byte[] fileBytes)
        {
            int offSet = 0;
            Width = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            Height = BitConverter.ToInt16(fileBytes, offSet);
            Cells = new byte[Width, Height];
            Objects = new List<MapObject>[Width, Height];
            //DoorIndex = new Door[Width, Height];

            offSet = 52;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {//total 12
                    //默认都是可以行走的格子
                    Cells[x, y] = 1;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = HighWall; //Can Fire Over.

                    offSet += 2;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = LowWall; //Can't Fire Over.

                    offSet += 2;

                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = HighWall; //No Floor Tile.

                    //if (Cells[x, y] == null) Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };

                    offSet += 4;

                    if (fileBytes[offSet] > 0)
                    {
                        //DoorIndex[x, y] = AddDoor(fileBytes[offSet], new Point(x, y));
                        AddDoor(fileBytes[offSet], new Point(x, y));
                    }
                        

                    offSet += 3;

                    byte light = fileBytes[offSet++];

                    if (light >= 100 && light <= 119)
                        Cells[x, y]= (byte)(Cells[x, y] | Fishing);
                }
        }
        //目前使用的是在这个地图
        private void LoadMapCellsv1(byte[] fileBytes)
        {
            int offSet = 21;

            int w = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            int xor = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            int h = BitConverter.ToInt16(fileBytes, offSet);
            Width = w ^ xor;
            Height = h ^ xor;
            Cells = new byte[Width, Height];
            Objects = new List<MapObject>[Width, Height];
            //DoorIndex = new Door[Width, Height];

            offSet = 54;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    //默认都是可以行走的格子
                    Cells[x, y] = 1;
                    if (((BitConverter.ToInt32(fileBytes, offSet) ^ 0xAA38AA38) & 0x20000000) != 0)
                        Cells[x, y] = HighWall; //Can Fire Over.

                    offSet += 6;
                    if (((BitConverter.ToInt16(fileBytes, offSet) ^ xor) & 0x8000) != 0)
                        Cells[x, y] = LowWall; //No Floor Tile.

                    //if (Cells[x, y] == null) Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
                    offSet += 2;
                    if (fileBytes[offSet] > 0)
                    {
                        //DoorIndex[x, y] = AddDoor(fileBytes[offSet], new Point(x, y));
                        AddDoor(fileBytes[offSet], new Point(x, y));
                    }
                        
                    offSet += 5;

                    byte light = fileBytes[offSet++];

                    if (light >= 100 && light <= 119)
                        Cells[x, y] = (byte)(Cells[x, y] | Fishing);

                    offSet += 1;
                }
        }

        private void LoadMapCellsv2(byte[] fileBytes)
        {
            int offSet = 0;
            Width = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            Height = BitConverter.ToInt16(fileBytes, offSet);
            Cells = new byte[Width, Height];
            Objects = new List<MapObject>[Width, Height];
            //DoorIndex = new Door[Width, Height];

            offSet = 52;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {//total 14
                    //默认都是可以行走的格子
                    Cells[x, y] = 1;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = HighWall; //Can Fire Over.

                    offSet += 2;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = LowWall; //Can't Fire Over.

                    offSet += 2;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = HighWall; //No Floor Tile.

                    //if (Cells[x, y] == null) Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };

                    offSet += 2;
                    if (fileBytes[offSet] > 0)
                    {
                        //DoorIndex[x, y] = AddDoor(fileBytes[offSet], new Point(x, y));
                        AddDoor(fileBytes[offSet], new Point(x, y));
                    }
                       
                    offSet += 5;

                    byte light = fileBytes[offSet++];

                    if (light >= 100 && light <= 119)
                        Cells[x, y] = (byte)(Cells[x, y] | Fishing);

                    offSet += 2;
                }
        }

        private void LoadMapCellsv3(byte[] fileBytes)
        {
            int offSet = 0;
            Width = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            Height = BitConverter.ToInt16(fileBytes, offSet);
            Cells = new byte[Width, Height];
            Objects = new List<MapObject>[Width, Height];
            //DoorIndex = new Door[Width, Height];

            offSet = 52;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {//total 36
                    //默认都是可以行走的格子
                    Cells[x, y] = 1;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = HighWall; //Can Fire Over.

                    offSet += 2;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = LowWall; //Can't Fire Over.

                    offSet += 2;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = HighWall; //No Floor Tile.

                    //if (Cells[x, y] == null) Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
                    offSet += 2;
                    if (fileBytes[offSet] > 0)
                    {
                        //DoorIndex[x, y] = AddDoor(fileBytes[offSet], new Point(x, y));
                        AddDoor(fileBytes[offSet], new Point(x, y));
                    }
                        
                    offSet += 12;

                    byte light = fileBytes[offSet++];

                    if (light >= 100 && light <= 119)
                        Cells[x, y] = (byte)(Cells[x, y] | Fishing);

                    offSet += 17;
                }
        }

        private void LoadMapCellsv4(byte[] fileBytes)
        {
            int offSet = 31;
            int w = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            int xor = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            int h = BitConverter.ToInt16(fileBytes, offSet);
            Width = w ^ xor;
            Height = h ^ xor;
            Cells = new byte[Width, Height];
            Objects = new List<MapObject>[Width, Height];
            //DoorIndex = new Door[Width, Height];

            offSet = 64;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {//total 12
                 //默认都是可以行走的格子
                    Cells[x, y] = 1;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = HighWall; //Can Fire Over.

                    offSet += 2;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = LowWall; //Can't Fire Over.

                    //if (Cells[x, y] == null) Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
                    offSet += 4;
                    if (fileBytes[offSet] > 0)
                    {
                        AddDoor(fileBytes[offSet], new Point(x, y));
                        //DoorIndex[x, y] = AddDoor(fileBytes[offSet], new Point(x, y));
                    }
                        
                    offSet += 6;
                }
        }

        private void LoadMapCellsv5(byte[] fileBytes)
        {
            int offSet = 22;
            Width = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            Height = BitConverter.ToInt16(fileBytes, offSet);
            Cells = new byte[Width, Height];
            Objects = new List<MapObject>[Width, Height];
            //DoorIndex = new Door[Width, Height];

            offSet = 28 + (3 * ((Width / 2) + (Width % 2)) * (Height / 2));
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {//total 14
                    //默认都是可以行走的格子
                    Cells[x, y] = 1;
                    if ((fileBytes[offSet] & 0x01) != 1)
                        Cells[x, y] = HighWall;
                    else if ((fileBytes[offSet] & 0x02) != 2)
                        Cells[x, y] = LowWall;
                    //else
                        //Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
                    offSet += 13;

                    byte light = fileBytes[offSet++];

                    if (light >= 100 && light <= 119)
                        Cells[x, y] = (byte)(Cells[x, y] | Fishing);
                }
        }

        private void LoadMapCellsv6(byte[] fileBytes)
        {
            int offSet = 16;
            Width = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 2;
            Height = BitConverter.ToInt16(fileBytes, offSet);
            Cells = new byte[Width, Height];
            Objects = new List<MapObject>[Width, Height];
            //DoorIndex = new Door[Width, Height];

            offSet = 40;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {//total 20
                 //默认都是可以行走的格子
                    Cells[x, y] = 1;
                    if ((fileBytes[offSet] & 0x01) != 1)
                        Cells[x, y] = HighWall;
                    else if ((fileBytes[offSet] & 0x02) != 2)
                        Cells[x, y] = LowWall;
                    //else
                        //Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
                    offSet += 20;
                }
        }

        private void LoadMapCellsv7(byte[] fileBytes)
        {
            int offSet = 21;
            Width = BitConverter.ToInt16(fileBytes, offSet);
            offSet += 4;
            Height = BitConverter.ToInt16(fileBytes, offSet);
            Cells = new byte[Width, Height];
            Objects = new List<MapObject>[Width, Height];
            //DoorIndex = new Door[Width, Height];

            offSet = 54;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {//total 15
                    //默认都是可以行走的格子
                    Cells[x, y] = 1;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = HighWall; //Can Fire Over.
                    offSet += 6;
                    if ((BitConverter.ToInt16(fileBytes, offSet) & 0x8000) != 0)
                        Cells[x, y] = LowWall; //Can't Fire Over.
                    //offSet += 2;
                    //if (Cells[x, y] == null) Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
                    offSet += 2;
                    if (fileBytes[offSet] > 0)
                    {
                        //DoorIndex[x, y] = AddDoor(fileBytes[offSet], new Point(x, y));
                        AddDoor(fileBytes[offSet], new Point(x, y));
                    }
                      
                    offSet += 4;

                    byte light = fileBytes[offSet++];

                    if (light >= 100 && light <= 119)
                        Cells[x, y] = (byte)(Cells[x, y] | Fishing);

                    offSet += 2;
                }
        }

        private void LoadMapCellsV100(byte[] Bytes)
        {
            int offset = 4;
            if ((Bytes[0] != 1) || (Bytes[1] != 0)) return;//only support version 1 atm
            Width = BitConverter.ToInt16(Bytes, offset);
            offset += 2;
            Height = BitConverter.ToInt16(Bytes, offset);
            Cells = new byte[Width, Height];
            Objects = new List<MapObject>[Width, Height];
            //DoorIndex = new Door[Width, Height];

            offset = 8;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    //默认都是可以行走的格子
                    Cells[x, y] = 1;
                    offset += 2;
                    if ((BitConverter.ToInt32(Bytes, offset) & 0x20000000) != 0)
                        Cells[x, y] = HighWall; //Can Fire Over.
                    offset += 10;
                    if ((BitConverter.ToInt16(Bytes, offset) & 0x8000) != 0)
                        Cells[x, y] = LowWall; //Can't Fire Over.

                    //if (Cells[x, y] == null) Cells[x, y] = new Cell { Attribute = CellAttribute.Walk };
                    offset += 2;
                    if (Bytes[offset] > 0)
                    {
                        //DoorIndex[x, y] = AddDoor(Bytes[offset], new Point(x, y));
                        AddDoor(Bytes[offset], new Point(x, y));
                    }
                    
                    offset += 11;

                    byte light = Bytes[offset++];

                    if (light >= 100 && light <= 119)
                    {
                        //Cells[x, y].FishingAttribute = (sbyte)(light - 100);
                        Cells[x, y] = (byte)(Cells[x, y] | Fishing);
                    }
                }
                
        }
        //加载地图信息
        public bool Load()
        {
            try
            {
                string fileName = Path.Combine(Settings.MapPath, Info.FileName + ".map");
                if (File.Exists(fileName))
                {
                    byte[] fileBytes = File.ReadAllBytes(fileName);
                    switch(FindType(fileBytes))
                    {
                        case 0:
                            LoadMapCellsv0(fileBytes);
                            break;
                        case 1:
                            LoadMapCellsv1(fileBytes);
                            break;
                        case 2:
                            LoadMapCellsv2(fileBytes);
                            break;
                        case 3:
                            LoadMapCellsv3(fileBytes);
                            break;
                        case 4:
                            LoadMapCellsv4(fileBytes);
                            break;
                        case 5:
                            LoadMapCellsv5(fileBytes);
                            break;
                        case 6:
                            LoadMapCellsv6(fileBytes);
                            break;
                        case 7:
                            LoadMapCellsv7(fileBytes);
                            break;
                        case 100:
                            LoadMapCellsV100(fileBytes);
                            break;
                    }

                    cellcount += Width * Height;

                    for (int i = 0; i < Info.Respawns.Count; i++)
                    {
                        MapRespawn info = new MapRespawn(Info.Respawns[i],this);
                        if (info.Monster == null) continue;
                        Respawns.Add(info);

                        if ((info.Info.SaveRespawnTime) && (info.Info.RespawnTicks != 0))
                        {
                            SMain.Envir.SavedSpawns.Add(info);
                        }
                    }


                    for (int i = 0; i < Info.NPCs.Count; i++)
                    {
                        NPCInfo info = Info.NPCs[i];
                        if (!ValidPoint(info.Location)) continue;

                        AddObject(new NPCObject(info) {CurrentMap = this});
                    }

                    for (int i = 0; i < Info.SafeZones.Count; i++)
                    {
                        CreateSafeZone(Info.SafeZones[i]);
                    }
                    loadRandomValidPoints();
                    return true;
                }
            }
            catch (Exception ex)
            {
                SMain.Enqueue(ex);
            }

            SMain.Enqueue("Failed to Load Map: " + Info.Mcode);

            return false;
        }

        //随机加载10个可以访问的点
        private void loadRandomValidPoints()
        {
            for(int i = 0; i < 500; i++)
            {
                int x = RandomUtils.Next(Width);
                int y = RandomUtils.Next(Height);
                if (Valid(x, y))
                {
                    RandomValidPoints.Add(new Point(x, y));
                    if (RandomValidPoints.Count > 10)
                    {
                        break;
                    }
                }
            }
        }

        //随机返回一个可以访问的点
        public Point RandomValidPoint()
        {
            //先随机10次，没有找到，则直接从固定的10个点中找
            for (int i = 0; i < 10; i++)
            {
                int x = RandomUtils.Next(Width);
                int y = RandomUtils.Next(Height);
                if (Valid(x, y))
                {
                    return new Point(x, y);
                }
            }
            if (RandomValidPoints.Count > 0)
            {
                return RandomValidPoints[RandomUtils.Next(RandomValidPoints.Count)];
            }
            return Point.Empty;
        }

        //在2个点之间找出一个可以访问的点
        //用于随机闪现
        public Point getValidPointByLine(Point p1, Point p2,int maxlen)
        {
            Point retp = Point.Empty;
            //查找最大范围内所有的点
            for (int x= p1.X- maxlen;x< p1.X + maxlen; x++)
            {
                for(int y=p1.Y- maxlen;y<p1.Y+ maxlen; y++)
                {
                    if (!Valid(x, y))
                    {
                        continue;
                    }
                    int _Distance = Math.Abs(x - p2.X)+ Math.Abs(y - p2.Y);
                    int Distance = Math.Abs(retp.X - p2.X)+ Math.Abs(retp.Y - p2.Y);
                    if(_Distance< Distance)
                    {
                        retp = new Point(x,y);
                    }
                }
            }
            if (retp.IsEmpty)
            {
                return p1;
            }
            return retp;
        }


        //某个点是否可以访问，其实就是是否可以行走
        public bool Valid(int x,int y)
        {
            if(x >= Width || y >= Height || x<0 || y<0)
            {
                return false;
            }
            return (Cells[x, y] & 1)==1;
        }

        //判断某个点是否可以钓鱼
        public bool CanFishing(int x, int y)
        {
            if (x >= Width || y >= Height || x < 0 || y < 0)
            {
                return false;
            }
            return (Cells[x, y] & 4) == 4;
        }

        public bool ValidPoint(Point location)
        {
            return ValidPoint(location.X, location.Y);
        }
        public bool ValidPoint(int x, int y)
        {
            return Valid(x,y);
        }


        public void Add(MapObject mapObject)
        {
            Add(mapObject.CurrentLocation.X, mapObject.CurrentLocation.Y, mapObject);
        }
        //添加对象进地图(地图的格子中)
        public void Add(int x,int y,MapObject mapObject)
        {
            if (Objects[x, y] == null)
            {
                Objects[x, y]= new List<MapObject>();
            }
            Objects[x, y].Add(mapObject);
        }


        public void Remove(MapObject mapObject)
        {
            Remove(mapObject.CurrentLocation.X, mapObject.CurrentLocation.Y, mapObject);
        }

        //移除对象
        public void Remove(int x, int y, MapObject mapObject)
        {
            if (Objects[x, y] == null)
            {
                return;
            }
            Objects[x, y].Remove(mapObject);
            if (Objects[x, y].Count == 0)
            {
                Objects[x, y] = null;
            }
        }
        //获取地图某个格子的对象列表
        public List<MapObject> getMapObjects(int x,int y)
        {
            return Objects[x, y];
        }
        //获取地图某个区域的对象列表
        public List<MapObject> getMapObjects(int x, int y,int Range)
        {
            List<MapObject> list = new List<MapObject>();
            for(int x1=x- Range; x1 <= x + Range; x1++)
            {
                if (x1 < 0 || x1 >= Width)
                {
                    continue;
                }
                for (int y1 = y - Range; y1 <= y + Range; y1++)
                {
                    if (y1 < 0 || y1 >= Height || Objects[x1, y1]==null)
                    {
                        continue;
                    }
                    list.AddRange(Objects[x1, y1]);
                }
            }
            return list;
        }

        //获取某个点的矿区
        public MineSpot getMine(int x,int y)
        {
            MineSpot mspot = null;
            if (Info.MineIndex==0 && Info.MineZones.Count == 0)
            {
                return null;
            }
            string key = x + "_" + y;
            if (MineDic.ContainsKey(key))
            {
                return MineDic[key];
            }
            //这里创建某个矿区点
            int MineIndex = Info.MineIndex;

            foreach(MineZone mz in Info.MineZones)
            {
                if (mz.inMineZone(x, y))
                {
                    MineIndex = mz.Mine;
                }
            }
            if (MineIndex > 0)
            {
                mspot = new MineSpot() { Mine = Settings.MineSetList[MineIndex - 1] };
                MineDic.Add(key, mspot);
            }
            return mspot;
        }
        //安全区
        private void CreateSafeZone(SafeZoneInfo info)
        {
            //安全区的边框
            if (Settings.SafeZoneBorder)
            {
                for (int y = info.Location.Y - info.Size; y <= info.Location.Y + info.Size; y++)
                {
                    if (y < 0) continue;
                    if (y >= Height) break;
                    for (int x = info.Location.X - info.Size; x <= info.Location.X + info.Size; x += Math.Abs(y - info.Location.Y) == info.Size ? 1 : info.Size * 2)
                    {
                        if (x < 0) continue;
                        if (x >= Width) break;
                        if (!Valid(x, y)) continue;

                        SpellObject spell = new SpellObject
                        {
                            ExpireTime = long.MaxValue,
                            Spell = Spell.TrapHexagon,
                            TickSpeed = int.MaxValue,
                            CurrentLocation = new Point(x, y),
                            CurrentMap = this,
                            Decoration = true
                        };
                        Add(x,y, spell);

                        spell.Spawned();
                    }
                }
            }
            //安全区的治疗效果
            if (Settings.SafeZoneHealing)
            {
                for (int y = info.Location.Y - info.Size; y <= info.Location.Y + info.Size; y++)
                {
                    if (y < 0) continue;
                    if (y >= Height) break;
                    for (int x = info.Location.X - info.Size; x <= info.Location.X + info.Size; x++)
                    {
                        if (x < 0) continue;
                        if (x >= Width) break;
                        if (!Valid(x, y)) continue;

                        SpellObject spell = new SpellObject
                            {
                                ExpireTime = long.MaxValue,
                                Value = 25,
                                TickSpeed = 2000,
                                Spell = Spell.Healing,
                                CurrentLocation = new Point(x, y),
                                CurrentMap = this
                            };

                        Add(x, y,spell);

                        spell.Spawned();
                    }
                }
            }


        }

        //不知道这是干嘛
        public bool CheckDoorOpen(Point location)
        {
            for (int i = 0; i < Doors.Count; i++)
            {
                if(Doors[i].Location== location && Doors[i].DoorState!=2)
                {
                    return false;
                }
            }
            //不存在则返回真？
            //if (DoorIndex[location.X, location.Y] == null) return true;
            //if (DoorIndex[location.X, location.Y].DoorState != 2) return false;
            return true;
        }

        //死循环调用入口
        public void Process()
        {
            try
            {
                ProcessRespawns();
                //处理门的开关？，这里好像只处理关门，不处理开门
                //process doors
                for (int i = 0; i < Doors.Count; i++)
                {
                    if ((Doors[i].DoorState == 2) && (Doors[i].LastTick + 5000 < Envir.Time))
                    {
                        Doors[i].DoorState = 0;
                        //broadcast that door is closed
                        Broadcast(new S.Opendoor() { DoorIndex = Doors[i].index, Close = true }, Doors[i].Location);

                    }
                }

                //闪电？部分地图有闪电？
                //如果有闪电，3-15秒随机产生一个闪电
                //整个地图的几乎所有玩家都会看到？那如果很多玩家，就会产生非常多的闪电哦。
                if ((Info.Lightning) && Envir.Time > LightningTime)
                {
                    LightningTime = Envir.Time + RandomUtils.Next(3000, 15000);
                    for (int i = Players.Count - 1; i >= 0; i--)
                    {
                        PlayerObject player = Players[i];
                        Point Location;
                        if (RandomUtils.Next(4) == 0)
                        {
                            Location = player.CurrentLocation;
                        }
                        else
                            Location = new Point(player.CurrentLocation.X - 10 + RandomUtils.Next(20), player.CurrentLocation.Y - 10 + RandomUtils.Next(20));

                        if (!ValidPoint(Location)) continue;

                        SpellObject Lightning = null;
                        Lightning = new SpellObject
                        {
                            Spell = Spell.MapLightning,
                            Value = RandomUtils.Next(Info.LightningDamage),
                            ExpireTime = Envir.Time + (1000),
                            TickSpeed = 500,
                            Caster = null,
                            CurrentLocation = Location,
                            CurrentMap = this,
                            Direction = MirDirection.Up
                        };
                        AddObject(Lightning);
                        Lightning.Spawned();
                    }
                }

                //地图火灾，熔岩伤害
                if ((Info.Fire) && Envir.Time > FireTime)
                {
                    FireTime = Envir.Time + RandomUtils.Next(3000, 15000);
                    for (int i = Players.Count - 1; i >= 0; i--)
                    {
                        PlayerObject player = Players[i];
                        Point Location;
                        if (RandomUtils.Next(4) == 0)
                        {
                            Location = player.CurrentLocation;
                        }
                        else
                            Location = new Point(player.CurrentLocation.X - 10 + RandomUtils.Next(20), player.CurrentLocation.Y - 10 + RandomUtils.Next(20));

                        if (!ValidPoint(Location)) continue;

                        SpellObject Lightning = null;
                        Lightning = new SpellObject
                        {
                            Spell = Spell.MapLava,
                            Value = RandomUtils.Next(Info.FireDamage),
                            ExpireTime = Envir.Time + (1000),
                            TickSpeed = 500,
                            Caster = null,
                            CurrentLocation = Location,
                            CurrentMap = this,
                            Direction = MirDirection.Up
                        };
                        AddObject(Lightning);
                        Lightning.Spawned();
                    }
                }
                //处理各种动作，处理完一个删除一个
                for (int i = 0; i < ActionList.Count; i++)
                {
                    if (Envir.Time < ActionList[i].Time) continue;
                    Process(ActionList[i]);
                    ActionList.RemoveAt(i);
                }
                //计算空闲数,某个地图，如果X分钟都没有玩家，则停止这个地图的一些功能
                if (InactiveTime < Envir.Time)
                {
                    if (!Players.Any())
                    {
                        //
                        InactiveTime = Envir.Time + Settings.Minute;
                        //InactiveTime = Envir.Time + Settings.Second;
                        InactiveCount++;
                    }
                    else
                    {
                        InactiveCount = 0;
                    }
                }
            }
            catch(Exception ex)
            {
                SMain.Enqueue(ex);
            }
        }

        //处理重生
        //地图的重生？
        //算法要进行优化，否则比较消耗资源
        private void ProcessRespawns()
        {
            bool Success = true;
            for (int i = 0; i < Respawns.Count; i++)
            {
                MapRespawn respawn = Respawns[i];
                if ((respawn.Info.RespawnTicks != 0) && (Envir.RespawnTick.CurrentTickcounter < respawn.NextSpawnTick)) continue;
                if ((respawn.Info.RespawnTicks == 0) && (Envir.Time < respawn.RespawnTime)) continue;
                //小于等于2个的，一般都是大BOSS，不做倍率调整哦.
                int markCount = respawn.Info.Count;
                if (markCount > 2)
                {
                    if (Info != null)
                    {
                        markCount = (int)(markCount * Info.MonsterRate);
                    }
                    markCount = (int)(markCount * Settings.MonsterRate);
                }

                if (respawn.Count < (markCount))
                {
                    int count = (int)(markCount) - respawn.Count;
                    for (int c = 0; c < count; c++)
                    {
                        Success = respawn.Spawn();
                    }
                }
                if (Success)
                {
                    respawn.ErrorCount = 0;
                    long delay = Math.Max(1, respawn.Info.Delay - respawn.Info.RandomDelay + RandomUtils.Next(respawn.Info.RandomDelay * 2));
                    respawn.RespawnTime = Envir.Time + (delay * Settings.Minute);
                    if (respawn.Info.RespawnTicks != 0)
                    {
                        respawn.NextSpawnTick = Envir.RespawnTick.CurrentTickcounter + (ulong)respawn.Info.RespawnTicks;
                        if (respawn.NextSpawnTick > long.MaxValue)//since nextspawntick is ulong this simple thing allows an easy way of preventing the counter from overflowing
                            respawn.NextSpawnTick -= long.MaxValue;
                    }
                }
                else
                {
                    respawn.RespawnTime = Envir.Time + 1 * Settings.Minute; // each time it fails to spawn, give it a 1 minute cooldown
                    if (respawn.ErrorCount < 5)
                        respawn.ErrorCount++;
                    else
                    {
                        if (respawn.ErrorCount == 5)
                        {
                            respawn.ErrorCount++;

                            File.AppendAllText(@".\SpawnErrors.txt",
                                String.Format("[{5}]Failed to spawn: mapindex: {0} ,mob info: index: {1} spawncoords ({2}:{3}) range {4}", respawn.Map.Info.Index, respawn.Info.MonsterIndex, respawn.Info.Location.X, respawn.Info.Location.Y, respawn.Info.Spread, DateTime.Now)
                                       + Environment.NewLine);
                            //*/
                        }

                    }
                }
            }
        }

        //处理动作
        public void Process(DelayedAction action)
        {
            switch (action.Type)
            {
                case DelayedType.Magic:
                    CompleteMagic(action.Params);
                    break;
                case DelayedType.Spawn:
                    MapObject obj = (MapObject)action.Params[0];

                    switch(obj.Race)
                    {
                        case ObjectType.Monster:
                            {
                                MonsterObject mob = (MonsterObject)action.Params[0];
                                mob.Spawn(this, (Point)action.Params[1]);
                                if (action.Params.Length > 2) ((MonsterObject)action.Params[2]).SlaveList.Add(mob);
                            }
                            break;
                        case ObjectType.Spell:
                            {
                                SpellObject spell = (SpellObject)action.Params[0];
                                AddObject(spell);
                                spell.Spawned();
                            }
                            break;
                    }
                    break;
            }
        }

        //完成魔法
        //用户释放魔法技能
        private void CompleteMagic(IList<object> data)
        {
            bool train = false;
            PlayerObject player = (PlayerObject)data[0];
            UserMagic magic = (UserMagic)data[1];

            if (player == null || player.Info == null) return;

            int value, value2;
            Point location;
            //Cell cell;
            
            MirDirection dir;
            MonsterObject monster;
            Point front;
            switch (magic.Spell)
            {

                #region HellFire

                case Spell.HellFire:
                    value = (int)data[2];
                    dir = (MirDirection)data[4];
                    location = Functions.PointMove((Point)data[3], dir, 1);
                    int count = (int)data[5] - 1;

                    if (!ValidPoint(location)) return;

                    if (count > 0)
                    {
                        DelayedAction action = new DelayedAction(DelayedType.Magic, Envir.Time + 100, player, magic, value, location, dir, count);
                        ActionList.Add(action);
                    }
                    
                    //cell = GetCell(location);

                    if (Objects[location.X, location.Y] == null) return;


                    for (int i = 0; i < Objects[location.X, location.Y].Count; i++)
                    {
                        MapObject target = Objects[location.X, location.Y][i];
                        switch (target.Race)
                        {
                            case ObjectType.Monster:
                            case ObjectType.Player:
                                //Only targets
                                if (target.IsAttackTarget(player))
                                {
                                    if (target.Attacked(player, value, DefenceType.MAC, false) > 0)
                                        player.LevelMagic(magic);
                                    return;
                                }
                                break;
                        }
                    }
                    break;

                #endregion

                #region SummonSkeleton, SummonShinsu, SummonHolyDeva, ArcherSummons

                case Spell.SummonSkeleton:
                case Spell.SummonShinsu:
                case Spell.SummonHolyDeva:
                case Spell.SummonVampire:
                case Spell.SummonToad:
                case Spell.SummonSnakes:
                    monster = (MonsterObject)data[2];
                    front = (Point)data[3];

                    if (monster.Master.Dead) return;

                    if (ValidPoint(front))
                        monster.Spawn(this, front);
                    else
                        monster.Spawn(player.CurrentMap, player.CurrentLocation);

                    monster.Master.Pets.Add(monster);
                    break;

                #endregion

                #region FireBang, IceStorm

                case Spell.IceStorm:
                case Spell.FireBang:
                    value = (int)data[2];
                    location = (Point)data[3];

                    for (int y = location.Y - 1; y <= location.Y + 1; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 1; x <= location.X + 1; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y) || Objects[x, y] == null) continue;

                            for (int i = 0; i < Objects[x, y].Count; i++)
                            {
                                MapObject target = Objects[x, y][i];
                                switch (target.Race)
                                {
                                    case ObjectType.Monster:
                                    case ObjectType.Player:
                                        //Only targets
                                        if (target.IsAttackTarget(player))
                                        {
                                            if (target.Attacked(player, value, DefenceType.MAC, false) > 0)
                                                train = true;
                                        }
                                        break;
                                }
                            }

                        }

                    }

                    break;

                #endregion

                #region MassHiding

                case Spell.MassHiding:
                    value = (int)data[2];
                    location = (Point)data[3];

                    for (int y = location.Y - 1; y <= location.Y + 1; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 1; x <= location.X + 1; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y) || Objects[x, y] == null) continue;

                            for (int i = 0; i < Objects[x, y].Count; i++)
                            {
                                MapObject target = Objects[x, y][i];
                                switch (target.Race)
                                {
                                    case ObjectType.Monster:
                                    case ObjectType.Player:
                                        //Only targets
                                        if (target.IsFriendlyTarget(player))
                                        {
                                            for (int b = 0; b < target.Buffs.Count; b++)
                                                if (target.Buffs[b].Type == BuffType.Hiding) return;

                                            target.AddBuff(new Buff { Type = BuffType.Hiding, Caster = player, ExpireTime = Envir.Time + value * 1000 });
                                            target.OperateTime = 0;
                                            train = true;
                                        }
                                        break;
                                }
                            }

                        }

                    }

                    break;

                #endregion

                #region SoulShield, BlessedArmour

                case Spell.SoulShield:
                case Spell.BlessedArmour:
                    value = (int)data[2];
                    location = (Point)data[3];
                    BuffType type = magic.Spell == Spell.SoulShield ? BuffType.SoulShield : BuffType.BlessedArmour;

                    for (int y = location.Y - 3; y <= location.Y + 3; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 3; x <= location.X + 3; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y) || Objects[x, y] == null) continue;

                            for (int i = 0; i < Objects[x, y].Count; i++)
                            {
                                MapObject target = Objects[x, y][i];
                                switch (target.Race)
                                {
                                    case ObjectType.Monster:
                                    case ObjectType.Player:
                                        //Only targets
                                        if (target.IsFriendlyTarget(player))
                                        {
                                            target.AddBuff(new Buff { Type = type, Caster = player, ExpireTime = Envir.Time + value * 1000, Values = new int[]{ target.Level / 7 + 4 } });
                                            target.OperateTime = 0;
                                            train = true;
                                        }
                                        break;
                                }
                            }

                        }

                    }

                    break;

                #endregion

                #region FireWall

                case Spell.FireWall:
                    value = (int)data[2];
                    location = (Point)data[3];

                    player.LevelMagic(magic);

                    if (ValidPoint(location))
                    {
                        //cell = GetCell(location);

                        bool cast = true;
                        if (Objects[location.X, location.Y] != null)
                            for (int o = 0; o < Objects[location.X, location.Y].Count; o++)
                            {
                                MapObject target = Objects[location.X, location.Y][o];
                                if (target.Race != ObjectType.Spell || ((SpellObject)target).Spell != Spell.FireWall) continue;

                                cast = false;
                                break;
                            }

                        if (cast)
                        {
                            SpellObject ob = new SpellObject
                                {
                                    Spell = Spell.FireWall,
                                    Value = value,
                                    ExpireTime = Envir.Time + (10 + value / 2) * 1000,
                                    TickSpeed = 2000,
                                    Caster = player,
                                    CurrentLocation = location,
                                    CurrentMap = this,
                                };
                            AddObject(ob);
                            ob.Spawned();
                        }
                    }

                    dir = MirDirection.Up;
                    for (int i = 0; i < 4; i++)
                    {
                        location = Functions.PointMove((Point)data[3], dir, 1);
                        dir += 2;

                        if (!ValidPoint(location)) continue;

                        //cell = GetCell(location);
                        bool cast = true;

                        if (Objects[location.X, location.Y] != null)
                            for (int o = 0; o < Objects[location.X, location.Y].Count; o++)
                            {
                                MapObject target = Objects[location.X, location.Y][o];
                                if (target.Race != ObjectType.Spell || ((SpellObject)target).Spell != Spell.FireWall) continue;

                                cast = false;
                                break;
                            }

                        if (!cast) continue;

                        SpellObject ob = new SpellObject
                        {
                            Spell = Spell.FireWall,
                            Value = value,
                            ExpireTime = Envir.Time + (10 + value / 2) * 1000,
                            TickSpeed = 2000,
                            Caster = player,
                            CurrentLocation = location,
                            CurrentMap = this,
                        };
                        AddObject(ob);
                        ob.Spawned();
                    }

                    break;

                #endregion

                #region Lightning

                case Spell.Lightning:
                    value = (int)data[2];
                    location = (Point)data[3];
                    dir = (MirDirection)data[4];

                    for (int i = 0; i < 6; i++)
                    {
                        location = Functions.PointMove(location, dir, 1);

                        if (!ValidPoint(location)) continue;

                        //cell = GetCell(location);

                        if (Objects[location.X, location.Y] == null) continue;

                        for (int o = 0; o < Objects[location.X, location.Y].Count; o++)
                        {
                            MapObject target = Objects[location.X, location.Y][o];
                            if (target.Race != ObjectType.Player && target.Race != ObjectType.Monster) continue;

                            if (!target.IsAttackTarget(player)) continue;
                            if (target.Attacked(player, value, DefenceType.MAC, false) > 0)
                                train = true;
                            break;
                        }
                    }

                    break;

                #endregion

                #region HeavenlySword

                case Spell.HeavenlySword:
                    value = (int)data[2];
                    location = (Point)data[3];
                    dir = (MirDirection)data[4];

                    for (int i = 0; i < 3; i++)
                    {
                        location = Functions.PointMove(location, dir, 1);

                        if (!ValidPoint(location)) continue;

                        //cell = GetCell(location);

                        if (Objects[location.X, location.Y] == null) continue;

                        for (int o = 0; o < Objects[location.X, location.Y].Count; o++)
                        {
                            MapObject target = Objects[location.X, location.Y][o];
                            if (target.Race != ObjectType.Player && target.Race != ObjectType.Monster) continue;

                            if (!target.IsAttackTarget(player)) continue;
                            if (target.Attacked(player, value, DefenceType.MAC, false) > 0)
                                train = true;
                            break;
                        }
                    }

                    break;

                #endregion

                #region MassHealing

                case Spell.MassHealing:
                    value = (int)data[2];
                    location = (Point)data[3];

                    for (int y = location.Y - 1; y <= location.Y + 1; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 1; x <= location.X + 1; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y) || Objects[x, y] == null) continue;

                            for (int i = 0; i < Objects[x, y].Count; i++)
                            {
                                MapObject target = Objects[x, y][i];
                                switch (target.Race)
                                {
                                    case ObjectType.Monster:
                                    case ObjectType.Player:
                                        //Only targets
                                        if (target.IsFriendlyTarget(player))
                                        {
                                            if (target.Health >= target.MaxHealth) continue;
                                            target.HealAmount = (ushort)Math.Min(ushort.MaxValue, target.HealAmount + value);
                                            target.OperateTime = 0;
                                            train = true;
                                        }
                                        break;
                                }
                            }

                        }

                    }

                    break;

                #endregion

                #region ThunderStorm

                case Spell.ThunderStorm:
                case Spell.FlameField:
                case Spell.NapalmShot:
                case Spell.StormEscape:
                    value = (int)data[2];
                    location = (Point)data[3];
                    for (int y = location.Y - 2; y <= location.Y + 2; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 2; x <= location.X + 2; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y) || Objects[x, y] == null) continue;

                            for (int i = 0; i < Objects[x, y].Count; i++)
                            {
                                MapObject target = Objects[x, y][i];
                                switch (target.Race)
                                {
                                    case ObjectType.Monster:
                                    case ObjectType.Player:
                                        //Only targets
                                        if (!target.IsAttackTarget(player)) break;

                                        if (target.Attacked(player, magic.Spell == Spell.ThunderStorm && !target.Undead ? value / 10 : value, DefenceType.MAC, false) <= 0)
                                        {
                                            if (target.Undead)
                                            {
                                                target.ApplyPoison(new Poison { PType = PoisonType.Stun, Duration = magic.Level + 2, TickSpeed = 1000 }, player);
                                            }
                                            break;
                                        }

                                        train = true;
                                        break;
                                }
                            }

                        }
                    }

                    break;

                #endregion

                #region LionRoar

                case Spell.LionRoar:
                    location = (Point)data[2];

                    for (int y = location.Y - 2; y <= location.Y + 2; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 2; x <= location.X + 2; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y) || Objects[x, y] == null) continue;

                            for (int i = 0; i < Objects[x, y].Count; i++)
                            {
                                MapObject target = Objects[x, y][i];
                                if (target.Race != ObjectType.Monster) continue;
                                //Only targets
                                if (!target.IsAttackTarget(player) || player.Level + 3 < target.Level) continue;
                                target.ApplyPoison(new Poison { PType = PoisonType.LRParalysis, Duration = magic.Level + 2, TickSpeed = 1000 }, player);
                                target.OperateTime = 0;
                                train = true;
                            }

                        }

                    }

                    break;

                #endregion

                #region PoisonCloud

                case Spell.PoisonCloud:
                    value = (int)data[2];
                    location = (Point)data[3];
                    byte bonusdmg = (byte)data[4];
                    train = true;
                    bool show = true;

                    for (int y = location.Y - 1; y <= location.Y + 1; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 1; x <= location.X + 1; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y)) continue;

                            bool cast = true;
                            if (Objects[x, y] != null)
                                for (int o = 0; o < Objects[x, y].Count; o++)
                                {
                                    MapObject target = Objects[x, y][o];
                                    if (target.Race != ObjectType.Spell || ((SpellObject)target).Spell != Spell.PoisonCloud) continue;

                                    cast = false;
                                    break;
                                }

                            if (!cast) continue;

                            SpellObject ob = new SpellObject
                                {
                                    Spell = Spell.PoisonCloud,
                                    Value = value + bonusdmg,
                                    ExpireTime = Envir.Time + 6000,
                                    TickSpeed = 1000,
                                    Caster = player,
                                    CurrentLocation = new Point(x, y),
                                    CastLocation = location,
                                    Show = show,
                                    CurrentMap = this,
                                };

                            show = false;

                            AddObject(ob);
                            ob.Spawned();
                        }
                    } 

                    break;

                #endregion

                #region IceThrust

                case Spell.IceThrust:
                    {
                        location = (Point)data[2];
                        MirDirection direction = (MirDirection)data[3];

                        int nearDamage = (int)data[4];
                        int farDamage = (int)data[5];

                        int col = 3;
                        int row = 3;

                        Point[] loc = new Point[col]; //0 = left 1 = center 2 = right
                        loc[0] = Functions.PointMove(location, Functions.PreviousDir(direction), 1);
                        loc[1] = Functions.PointMove(location, direction, 1);
                        loc[2] = Functions.PointMove(location, Functions.NextDir(direction), 1);

                        for (int i = 0; i < col; i++)
                        {
                            Point startPoint = loc[i];
                            for (int j = 0; j < row; j++)
                            {
                                Point hitPoint = Functions.PointMove(startPoint, direction, j);

                                if (!ValidPoint(hitPoint)) continue;

                                //cell = GetCell(hitPoint);

                                if (Objects[hitPoint.X, hitPoint.Y] == null) continue;

                                for (int k = 0; k < Objects[hitPoint.X, hitPoint.Y].Count; k++)
                                {
                                    MapObject target = Objects[hitPoint.X, hitPoint.Y][k];
                                    switch (target.Race)
                                    {
                                        case ObjectType.Monster:
                                        case ObjectType.Player:
                                            if (target.IsAttackTarget(player))
                                            {
                                                //Only targets
                                                if (target.Attacked(player, j <= 1 ? nearDamage : farDamage, DefenceType.MAC, false) > 0)
                                                {
                                                    if (player.Level + (target.Race == ObjectType.Player ? 2 : 10) >= target.Level && RandomUtils.Next(target.Race == ObjectType.Player ? 100 : 20) <= magic.Level)
                                                    {
                                                        target.ApplyPoison(new Poison
                                                        {
                                                            Owner = player,
                                                            Duration = target.Race == ObjectType.Player ? 4 : 5 + RandomUtils.Next(5),
                                                            PType = PoisonType.Slow,
                                                            TickSpeed = 1000,
                                                        }, player);
                                                        target.OperateTime = 0;
                                                    }

                                                    if (player.Level + (target.Race == ObjectType.Player ? 2 : 10) >= target.Level && RandomUtils.Next(target.Race == ObjectType.Player ? 100 : 40) <= magic.Level)
                                                    {
                                                        target.ApplyPoison(new Poison
                                                        {
                                                            Owner = player,
                                                            Duration = target.Race == ObjectType.Player ? 2 : 5 + RandomUtils.Next(player.Freezing),
                                                            PType = PoisonType.Frozen,
                                                            TickSpeed = 1000,
                                                        }, player);
                                                        target.OperateTime = 0;
                                                    }

                                                    train = true;
                                                }
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }

                    break;

                #endregion

                #region SlashingBurst

                case Spell.SlashingBurst:
                    value = (int)data[2];
                    location = (Point)data[3];
                    dir = (MirDirection)data[4];
                    count = (int)data[5];

                    for (int i = 0; i < count; i++)
                    {
                        location = Functions.PointMove(location, dir, 1);

                        if (!ValidPoint(location)) continue;

                        //cell = GetCell(location);

                        if (Objects[location.X, location.Y] == null) continue;

                        for (int o = 0; o < Objects[location.X, location.Y].Count; o++)
                        {
                            MapObject target = Objects[location.X, location.Y][o];
                            if (target.Race != ObjectType.Player && target.Race != ObjectType.Monster) continue;

                            if (!target.IsAttackTarget(player)) continue;
                            if (target.Attacked(player, value, DefenceType.AC, false) > 0)
                                train = true;
                            break;
                        }
                    }
                    break;

                #endregion

                #region Mirroring

                case Spell.Mirroring:
                    monster = (MonsterObject)data[2];
                    front = (Point)data[3];
                    bool finish = (bool)data[4];

                    if (finish)
                    {
                        monster.Die();
                        return;
                    };

                    if (ValidPoint(front))
                        monster.Spawn(this, front);
                    else
                        monster.Spawn(player.CurrentMap, player.CurrentLocation);
                    break;

                #endregion

                #region Blizzard

                case Spell.Blizzard:
                    value = (int)data[2];
                    location = (Point)data[3];

                    train = true;
                    show = true;

                    for (int y = location.Y - 2; y <= location.Y + 2; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 2; x <= location.X + 2; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y)) continue;

                            bool cast = true;
                            if (Objects[x, y] != null)
                                for (int o = 0; o < Objects[x, y].Count; o++)
                                {
                                    MapObject target = Objects[x, y][o];
                                    if (target.Race != ObjectType.Spell || ((SpellObject) target).Spell != Spell.Blizzard) continue;

                                    cast = false;
                                    break;
                                }

                            if (!cast) continue;

                            SpellObject ob = new SpellObject
                                {
                                    Spell = Spell.Blizzard,
                                    Value = value,
                                    ExpireTime = Envir.Time + 3000,
                                    TickSpeed = 440,
                                    Caster = player,
                                    CurrentLocation = new Point(x, y),
                                    CastLocation = location,
                                    Show = show,
                                    CurrentMap = this,
                                    StartTime = Envir.Time + 800,
                                };

                            show = false;

                            AddObject(ob);
                            ob.Spawned();
                        }
                    } 

                    break;

                #endregion

                #region MeteorStrike

                case Spell.MeteorStrike:
                    value = (int)data[2];
                    location = (Point)data[3];

                    train = true;
                    show = true;

                    for (int y = location.Y - 2; y <= location.Y + 2; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 2; x <= location.X + 2; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y)) continue;

                            bool cast = true;
                            if (Objects[x, y] != null)
                                for (int o = 0; o < Objects[x, y].Count; o++)
                                {
                                    MapObject target = Objects[x, y][o];
                                    if (target.Race != ObjectType.Spell || ((SpellObject)target).Spell != Spell.MeteorStrike) continue;

                                    cast = false;
                                    break;
                                }

                            if (!cast) continue;

                            SpellObject ob = new SpellObject
                            {
                                Spell = Spell.MeteorStrike,
                                Value = value,
                                ExpireTime = Envir.Time + 3000,
                                TickSpeed = 440,
                                Caster = player,
                                CurrentLocation = new Point(x, y),
                                CastLocation = location,
                                Show = show,
                                CurrentMap = this,
                                StartTime = Envir.Time + 800,
                            };

                            show = false;

                            AddObject(ob);
                            ob.Spawned();
                        }
                    }

                    break;

                #endregion

                #region TrapHexagon

                case Spell.TrapHexagon:
                    value = (int)data[2];
                    location = (Point)data[3];

                    MonsterObject centerTarget = null;

                    for (int y = location.Y - 1; y <= location.Y + 1; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 1; x <= location.X + 1; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y) || Objects[x, y] == null) continue;

                            for (int i = 0; i < Objects[x, y].Count; i++)
                            {
                                MapObject target = Objects[x, y][i];

                                if (y == location.Y && x == location.X && target.Race == ObjectType.Monster)
                                {
                                    centerTarget = (MonsterObject)target;
                                }
                                
                                switch (target.Race)
                                {
                                    case ObjectType.Monster:
                                        if (target == null || !target.IsAttackTarget(player) || target.Node == null || target.Level > player.Level + 2) continue;

                                        MonsterObject mobTarget = (MonsterObject)target;

                                        if (centerTarget == null) centerTarget = mobTarget;

                                        mobTarget.ShockTime = Envir.Time + value;
                                        mobTarget.Target = null;
                                        break;
                                }
                            }

                        }
                    }

                    if (centerTarget == null) return;

                    for (byte i = 0; i < 8; i += 2)
                    {
                        Point startpoint = Functions.PointMove(location, (MirDirection)i, 2);
                        for (byte j = 0; j <= 4; j += 4)
                        {
                            MirDirection spawndirection = i == 0 || i == 4 ? MirDirection.Right : MirDirection.Up;
                            Point spawnpoint = Functions.PointMove(startpoint, spawndirection + j, 1);
                            if (spawnpoint.X <= 0 || spawnpoint.X > centerTarget.CurrentMap.Width) continue;
                            if (spawnpoint.Y <= 0 || spawnpoint.Y > centerTarget.CurrentMap.Height) continue;
                            SpellObject ob = new SpellObject
                            {
                                Spell = Spell.TrapHexagon,
                                ExpireTime = Envir.Time + value,
                                TickSpeed = 100,
                                Caster = player,
                                CurrentLocation = spawnpoint,
                                CastLocation = location,
                                CurrentMap = centerTarget.CurrentMap,
                                Target = centerTarget,
                            };

                            centerTarget.CurrentMap.AddObject(ob);
                            ob.Spawned();
                        }
                    }

                    train = true;

                    break;

                #endregion

                #region Curse

                case Spell.Curse:
                    value = (int)data[2];
                    location = (Point)data[3];
                    value2 = (int)data[4];
                    type = BuffType.Curse;

                    for (int y = location.Y - 3; y <= location.Y + 3; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 3; x <= location.X + 3; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y) || Objects[x, y] == null) continue;

                            for (int i = 0; i < Objects[x, y].Count; i++)
                            {
                                MapObject target = Objects[x, y][i];
                                switch (target.Race)
                                {
                                    case ObjectType.Monster:
                                    case ObjectType.Player:

                                        if (RandomUtils.Next(10) >= 4) continue;

                                        //Only targets
                                        if (target.IsAttackTarget(player))
                                        {
                                            target.ApplyPoison(new Poison { PType = PoisonType.Slow, Duration = value, TickSpeed = 1000, Value = value2 }, player);
                                            target.AddBuff(new Buff { Type = type, Caster = player, ExpireTime = Envir.Time + value * 1000, Values = new int[]{ value2 } });
                                            target.OperateTime = 0;
                                            train = true;
                                        }
                                        break;
                                }
                            }

                        }

                    }

                    break;

                #endregion

                #region ExplosiveTrap

                case Spell.ExplosiveTrap:
                    value = (int)data[2];
                    front = (Point)data[3];
                    int trapID = (int)data[4];

                    if (ValidPoint(front))
                    {
                        //cell = GetCell(front);

                        bool cast = true;
                        if (Objects[front.X, front.Y] != null)
                            for (int o = 0; o < Objects[front.X, front.Y].Count; o++)
                            {
                                MapObject target = Objects[front.X, front.Y][o];
                                if (target.Race != ObjectType.Spell || (((SpellObject)target).Spell != Spell.FireWall && ((SpellObject)target).Spell != Spell.ExplosiveTrap)) continue;

                                cast = false;
                                break;
                            }

                        if (cast)
                        {
                            player.LevelMagic(magic);
                            System.Drawing.Point[] Traps = new Point[3];
                            Traps[0] = front;
                            Traps[1] = Functions.Left(front, player.Direction);
                            Traps[2] = Functions.Right(front, player.Direction);
                            for (int i = 0; i <= 2; i++)
                            {
                                SpellObject ob = new SpellObject
                                {
                                    Spell = Spell.ExplosiveTrap,
                                    Value = value,
                                    ExpireTime = Envir.Time + (10 + value / 2) * 1000,
                                    TickSpeed = 500,
                                    Caster = player,
                                    CurrentLocation = Traps[i],
                                    CurrentMap = this,
                                    ExplosiveTrapID = trapID,
                                    ExplosiveTrapCount = i
                                };
                                AddObject(ob);
                                ob.Spawned();
                                player.ArcherTrapObjectsArray[trapID, i] = ob;
                            }
                        }
                    }
                    break;

                #endregion

                #region Plague

                case Spell.Plague:
                    value = (int)data[2];
                    location = (Point)data[3];

                    for (int y = location.Y - 3; y <= location.Y + 3; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 3; x <= location.X + 3; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y) || Objects[x, y] == null) continue;

                            for (int i = 0; i < Objects[x, y].Count; i++)
                            {
                                MapObject target = Objects[x, y][i];
                                switch (target.Race)
                                {
                                    case ObjectType.Monster:
                                    case ObjectType.Player:
                                        //Only targets
                                        if (target.IsAttackTarget(player))
                                        {
                                            int chance = RandomUtils.Next(15);
                                            PoisonType poison;
                                            if (new int[] { 0, 1, 3 }.Contains(chance)) //3 in 15 chances it'll slow
                                                poison = PoisonType.Slow;
                                            else if (new int[] { 3, 4 }.Contains(value)) //2 in 15 chances it'll freeze
                                                poison = PoisonType.Frozen;
                                            else if (new int[] { 5, 6, 7, 8, 9 }.Contains(value)) //5 in 15 chances it'll red/green
                                                poison = (PoisonType)data[4];
                                            else //5 in 15 chances it'll do nothing
                                                poison = PoisonType.None;

                                            int tempValue = 0;

                                            if (poison == PoisonType.Green)
                                            {
                                                tempValue = value / 15 + magic.Level + 1;
                                            }
                                            else
                                            {
                                                tempValue = value + (magic.Level + 1) * 2;
                                            }

                                            if (poison != PoisonType.None)
                                            {
                                                target.ApplyPoison(new Poison { PType = poison, Duration = (2 * (magic.Level + 1)) + (value / 10), TickSpeed = 1000, Value = tempValue, Owner = player }, player, false, false);
                                            }
                                            
                                            if (target.Race == ObjectType.Player)
                                            {
                                                PlayerObject tempOb = (PlayerObject)target;

                                                tempOb.ChangeMP(-tempValue);
                                            }

                                            train = true;
                                        }
                                        break;
                                }
                            }

                        }

                    }

                    break;

                #endregion

                #region Trap

                case Spell.Trap:
                    value = (int)data[2];
                    location = (Point)data[3];
                    MonsterObject selectTarget = null;

                    if (!ValidPoint(location)) break;

                    //cell = GetCell(location);

                    if (!ValidPoint(location) || Objects[location.X, location.Y] == null) break;

                    for (int i = 0; i < Objects[location.X, location.Y].Count; i++)
                    {
                        MapObject target = Objects[location.X, location.Y][i];
                        if (target.Race == ObjectType.Monster)
                        {
                            selectTarget = (MonsterObject)target;

                            if (selectTarget == null || !selectTarget.IsAttackTarget(player) || selectTarget.Node == null || selectTarget.Level >= player.Level + 2) continue;
                            selectTarget.ShockTime = Envir.Time + value;
                            selectTarget.Target = null;
                            break;
                        }
                    }

                    if (selectTarget == null) return;

                    if (location.X <= 0 || location.X > selectTarget.CurrentMap.Width) break;
                    if (location.Y <= 0 || location.Y > selectTarget.CurrentMap.Height) break;
                    SpellObject spellOb = new SpellObject
                    {
                        Spell = Spell.Trap,
                        ExpireTime = Envir.Time + value,
                        TickSpeed = 100,
                        Caster = player,
                        CurrentLocation = location,
                        CastLocation = location,
                        CurrentMap = selectTarget.CurrentMap,
                        Target = selectTarget,
                    };

                    selectTarget.CurrentMap.AddObject(spellOb);
                    spellOb.Spawned();

                    train = true;
                    break;

                #endregion

                #region OneWithNature

                case Spell.OneWithNature:
                    value = (int)data[2];
                    location = (Point)data[3];

                    bool hasVampBuff = (player.Buffs.Where(ex => ex.Type == BuffType.VampireShot).ToList().Count() > 0);
                    bool hasPoisonBuff = (player.Buffs.Where(ex => ex.Type == BuffType.PoisonShot).ToList().Count() > 0);

                    for (int y = location.Y - 2; y <= location.Y + 2; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 2; x <= location.X + 2; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y) || Objects[x,y] == null) continue;

                            for (int i = 0; i < Objects[x, y].Count; i++)
                            {
                                MapObject target = Objects[x, y][i];
                                switch (target.Race)
                                {
                                    case ObjectType.Monster:
                                    case ObjectType.Player:
                                        //Only targets
                                        if (!target.IsAttackTarget(player) || target.Dead) break;

                                        //knockback
                                        //int distance = 1 + Math.Max(0, magic.Level - 1) + RandomUtils.Next(2);
                                        //dir = Functions.DirectionFromPoint(location, target.CurrentLocation);
                                        //if(target.Level < player.Level)
                                        //    target.Pushed(player, dir, distance);// <--crashes server somehow?

                                        if (target.Attacked(player, value, DefenceType.MAC, false) <= 0) break;

                                        if (hasVampBuff)//Vampire Effect
                                        {
                                            if (player.VampAmount == 0) player.VampTime = Envir.Time + 1000;
                                            player.VampAmount += (ushort)(value * (magic.Level + 1) * 0.25F);
                                        }
                                        if (hasPoisonBuff)//Poison Effect
                                        {
                                            target.ApplyPoison(new Poison
                                            {
                                                Duration = (value * 2) + (magic.Level + 1) * 7,
                                                Owner = player,
                                                PType = PoisonType.Green,
                                                TickSpeed = 2000,
                                                Value = value / 15 + magic.Level + 1 + RandomUtils.Next(player.PoisonAttack)
                                            }, player);
                                            target.OperateTime = 0;
                                        }
                                        train = true;
                                        break;
                                }
                            }

                        }
                    }

                    if (hasVampBuff)//Vampire Effect
                    {
                        //cancel out buff
                        player.AddBuff(new Buff { Type = BuffType.VampireShot, Caster = player, ExpireTime = Envir.Time + 1000, Values = new int[]{ value }, Visible = true, ObjectID = player.ObjectID });
                    }
                    if (hasPoisonBuff)//Poison Effect
                    {
                        //cancel out buff
                        player.AddBuff(new Buff { Type = BuffType.PoisonShot, Caster = player, ExpireTime = Envir.Time + 1000, Values = new int[]{ value }, Visible = true, ObjectID = player.ObjectID });
                    }
                    break;

                #endregion

                #region Portal

                case Spell.Portal:                  
                    value = (int)data[2];
                    location = (Point)data[3];
                    value2 = (int)data[4];

                    spellOb = new SpellObject
                    {
                        Spell = Spell.Portal,
                        Value = value2,
                        ExpireTime = Envir.Time + value * 1000,
                        TickSpeed = 2000,
                        Caster = player,
                        CurrentLocation = location,
                        CurrentMap = this,
                    };

                    if (player.PortalObjectsArray[0] == null)
                    {
                        player.PortalObjectsArray[0] = spellOb;
                    }
                    else
                    {
                        player.PortalObjectsArray[1] = spellOb;
                        player.PortalObjectsArray[1].ExitMap = player.PortalObjectsArray[0].CurrentMap;
                        player.PortalObjectsArray[1].ExitCoord = player.PortalObjectsArray[0].CurrentLocation;

                        player.PortalObjectsArray[0].ExitMap = player.PortalObjectsArray[1].CurrentMap;
                        player.PortalObjectsArray[0].ExitCoord = player.PortalObjectsArray[1].CurrentLocation;
                    }

                    AddObject(spellOb);
                    spellOb.Spawned();
                    train = true;
                    break;

                #endregion

                #region DelayedExplosion

                case Spell.DelayedExplosion:
                    value = (int)data[2];
                    location = (Point)data[3];

                    for (int y = location.Y - 1; y <= location.Y + 1; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 1; x <= location.X + 1; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y) || Objects[x,y] == null) continue;

                            for (int i = 0; i < Objects[x, y].Count; i++)
                            {
                                MapObject target = Objects[x, y][i];
                                switch (target.Race)
                                {
                                    case ObjectType.Monster:
                                    case ObjectType.Player:
                                        //Only targets
                                        if (target.IsAttackTarget(player))
                                        {
                                            if (target.Attacked(player, value, DefenceType.MAC, false) > 0)
                                                train = false;//wouldnt want to make the skill give twice the points
                                        }
                                        break;
                                }
                            }

                        }

                    }

                    break;

                #endregion

                #region BattleCry

                case Spell.BattleCry:
                    location = (Point)data[2];

                    for (int y = location.Y - 2; y <= location.Y + 2; y++)
                    {
                        if (y < 0) continue;
                        if (y >= Height) break;

                        for (int x = location.X - 2; x <= location.X + 2; x++)
                        {
                            if (x < 0) continue;
                            if (x >= Width) break;

                            //cell = GetCell(x, y);

                            if (!Valid(x,y) || Objects[x,y] == null) continue;

                            for (int i = 0; i < Objects[x, y].Count; i++)
                            {
                                MapObject target = Objects[x, y][i];
                                if (target.Race != ObjectType.Monster) continue;

                                if (magic.Level == 0)
                                {
                                    if (RandomUtils.Next(60) >= 4) continue;
                                }
                                else if (magic.Level == 1)
                                {
                                    if (RandomUtils.Next(45) >= 3) continue;
                                }
                                else if (magic.Level == 2)
                                {
                                    if (RandomUtils.Next(30) >= 2) continue;
                                }
                                else if (magic.Level == 3)
                                {
                                    if (RandomUtils.Next(15) >= 1) continue;
                                }

                                if (((MonsterObject)target).Info.CoolEye == 100) continue;
                                target.Target = player;
                                target.OperateTime = 0;
                                train = true;
                            }
                        }
                    }
                    break;

                    #endregion
            }

            if (train)
                player.LevelMagic(magic);

        }

        public void AddObject(MapObject ob)
        {
            if (ob.Race == ObjectType.Player)
            {
                Players.Add((PlayerObject)ob);
                InactiveTime = Envir.Time;
            }
            if (ob.Race == ObjectType.Merchant)
                NPCs.Add((NPCObject)ob);
            Add(ob.CurrentLocation.X, ob.CurrentLocation.Y, ob);
        }

        public void RemoveObject(MapObject ob)
        {
            if (ob.Race == ObjectType.Player) Players.Remove((PlayerObject)ob);
            if (ob.Race == ObjectType.Merchant) NPCs.Remove((NPCObject)ob);
            Remove(ob.CurrentLocation.X, ob.CurrentLocation.Y, ob);
            //GetCell(ob.CurrentLocation).Remove(ob);
        }


        public SafeZoneInfo GetSafeZone(Point location)
        {
            for (int i = 0; i < Info.SafeZones.Count; i++)
            {
                SafeZoneInfo szi = Info.SafeZones[i];
                if (Functions.InRange(szi.Location, location, szi.Size))
                    return szi;
            }
            return null;
        }

        public ConquestObject GetConquest(Point location)
        {
            for (int i = 0; i < Conquest.Count; i++)
            {
                ConquestObject swi = Conquest[i];

                if ((swi.Info.FullMap || Functions.InRange(swi.Info.Location, location, swi.Info.Size)) && swi.WarIsOn)
                    return swi;
            }
            return null;
        }

        //public ConquestObject GetInnerConquest(Map map, Point location)
        //{
        //    for (int i = 0; i < Conquest.Count; i++)
        //    {
        //        ConquestObject swi = Conquest[i];
        //        if (map.Info.Index != swi.Info.MapIndex) continue;

        //        if (Functions.InRange(swi.Info.KingLocation, location, swi.Info.KingSize) && swi.WarIsOn)
        //            return swi;
        //    }
        //    return null;
        //}

        public void Broadcast(Packet p, Point location)
        {
            if (p == null) return;

            for (int i = Players.Count - 1; i >= 0; i--)
            {
                PlayerObject player = Players[i];

                if (Functions.InRange(location, player.CurrentLocation, Globals.DataRange))
                    player.Enqueue(p);
                    
            }
        }

        public void BroadcastNPC(Packet p, Point location)
        {
            if (p == null) return;

            for (int i = Players.Count - 1; i >= 0; i--)
            {
                PlayerObject player = Players[i];

                if (Functions.InRange(location, player.CurrentLocation, Globals.DataRange))
                    player.Enqueue(p);

            }
        }


        public void Broadcast(Packet p, Point location, PlayerObject Player)
        {
            if (p == null) return;

            if (Functions.InRange(location, Player.CurrentLocation, Globals.DataRange))
            {
                Player.Enqueue(p);
            }    
        }

        public bool Inactive(int count = 5)
        {
            //temporary test for server speed. Stop certain processes if no players.
            if (InactiveCount > count) return true;

            return false;
        }
    }


    //怪物重生的实现
    public class MapRespawn
    {
        public RespawnInfo Info;//刷怪信息
        public MonsterInfo Monster;//怪物信息
        public Map Map;//刷怪地图
        public int Count;//当前的怪物数(每次刷怪都是总数减去当前的怪物数)

        public long RespawnTime;//刷怪时间
        public ulong NextSpawnTick;//下次刷怪间隔
        public byte ErrorCount = 0;//错误数,刷怪刷不到具体的位置，则错误

        public List<RouteInfo> Route;
        //刷怪的区域点，一开始的时候就固化了。默认初始双倍的点
        private List<Point> Locs = new List<Point>();//
        private int _minx, _maxx, _miny, _maxy;

        public MapRespawn(RespawnInfo info, Map map)
        {
            Info = info;
            Map = map;
            Monster = SMain.Envir.GetMonsterInfo(info.MonsterIndex);
            LoadRoutes();
            initRespawnRegion();
        }

        //初始化刷怪区域
        //这里看下还怎么优化
        private void initRespawnRegion()
        {
            if(Map==null)
            {
                return;
            }
            //刷怪区域
            _minx = Info.Location.X - Info.Spread;
            _maxx = Info.Location.X + Info.Spread;
            _miny = Info.Location.Y - Info.Spread;
            _maxy = Info.Location.Y + Info.Spread;
            if (_minx < 0) _minx = 0;
            if (_maxx < 0) _maxx = 0;
            if (_miny < 0) _miny = 0;
            if (_maxy < 0) _maxy = 0;
            if (_minx >= Map.Width) _minx = Map.Width-1;
            if (_maxx >= Map.Width) _maxx = Map.Width-1;
            if (_miny >= Map.Height) _miny = Map.Height-1;
            if (_maxy >= Map.Height) _maxy = Map.Height-1;
            //如果区域比较小，小于20*20，直接从点中取
            if(Math.Abs(_maxx- _minx)* Math.Abs(_maxy - _miny) < 500)
            {
                List<Point> listp = new List<Point>();
                for (int x = _minx; x <= _maxx; x++)
                {
                    for (int y = _miny; y <= _maxy; y++)
                    {
                        if (Map.Valid(x, y))
                        {
                            listp.Add(new Point(x, y));
                        }
                    }
                }
                //默认记录1倍的刷新点
                for (int i = 0; i < listp.Count && i < Info.Count; i++)
                {
                    Point p = listp[RandomUtils.Next(listp.Count)];
                    listp.Remove(p);
                    Locs.Add(p);
                }
            }
            else
            {
                //随机抽取点
                int maxcount = Info.Spread * Info.Spread / 2;
                if (maxcount > 500)
                {
                    maxcount = 500;
                }
                for(int i=0;i< maxcount; i++)
                {
                    Point p = new Point(RandomUtils.Next(_minx, _maxx + 1), RandomUtils.Next(_miny, _maxy + 1));
                    if (Map.Valid(p.X,p.Y))
                    {
                        Locs.Add(p);
                        if(Locs.Count> Info.Count*2)
                        {
                            break;
                        }
                    }
                }
            }

        }

        //怪物的产生
        public bool Spawn()
        {
            MonsterObject ob = MonsterObject.GetMonster(Monster);
            if (ob == null|| Locs.Count==0) return true;
            bool isSpawn = false;
            //5次随机重生，如果5次都没有随机到点，则从列表中取点
            for (int i = 0; i < 5; i++)
            {
                Point p = new Point(RandomUtils.Next(_minx, _maxx + 1), RandomUtils.Next(_miny, _maxy + 1));
                if (Map.Valid(p.X, p.Y))
                {
                    ob.CurrentLocation = p;
                    isSpawn = true;
                    break;
                }
            }

            if (!isSpawn)
            {
                ob.CurrentLocation = Locs[RandomUtils.Next(Locs.Count)];
            }

            Map.AddObject(ob);
            ob.CurrentMap = Map;
            ob.Respawn = this;
            if (Route.Count > 0)
                ob.Route.AddRange(Route);

            ob.RefreshAll();
            ob.SetHP(ob.MaxHP);

            ob.Spawned();
            Count++;
            Map.MonsterCount++;
            SMain.Envir.MonsterCount++;
            return true;
        }

        //加载怪物自动行走路径，针对大刀守卫
        //后续可以扩展到其他智能的怪物哦
        public void LoadRoutes()
        {
            Route = new List<RouteInfo>();

            if (string.IsNullOrEmpty(Info.RoutePath)) return;

            string fileName = Path.Combine(Settings.RoutePath, Info.RoutePath + ".txt");

            if (!File.Exists(fileName)) return;

            List<string> lines = File.ReadAllLines(fileName).ToList();

            foreach (string line in lines)
            {
                RouteInfo info = RouteInfo.FromText(line);

                if (info == null) continue;

                Route.Add(info);
            }
        }
    }
}
