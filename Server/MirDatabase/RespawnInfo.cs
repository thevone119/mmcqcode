using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Server.MirEnvir;

namespace Server.MirDatabase
{
    //这个是怪物重生信息
    //关联地图id,关联怪物ID
    //怪物的刷新信息
    public class RespawnInfo
    {
        public int MonsterIndex;
        public Point Location;
        //总数，传播范围，延时，随机延时
        public ushort Count, Spread, Delay, RandomDelay;
        //这个是朝向么？朝向都给固定了?坑爹么
        public byte Direction;
        //这个是怪物的巡逻路线
        public string RoutePath = string.Empty;
        //这个是ID
        public int RespawnIndex;
        //是否保存重生的时间
        public bool SaveRespawnTime = false;
        //不知道干嘛用的哦，重生的间隔计数？
        public ushort RespawnTicks; //leave 0 if not using this system!

        public RespawnInfo()
        {

        }
        

        public static RespawnInfo FromText(string text)
        {
            string[] data = text.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

            if (data.Length < 7) return null;

            RespawnInfo info = new RespawnInfo();

            int x,y ;

            if (!int.TryParse(data[0], out info.MonsterIndex)) return null;
            if (!int.TryParse(data[1], out x)) return null;
            if (!int.TryParse(data[2], out y)) return null;

            info.Location = new Point(x, y);

            if (!ushort.TryParse(data[3], out info.Count)) return null;
            if (!ushort.TryParse(data[4], out info.Spread)) return null;
            if (!ushort.TryParse(data[5], out info.Delay)) return null;
            if (!byte.TryParse(data[6], out info.Direction)) return null;
            if (!ushort.TryParse(data[7], out info.RandomDelay)) return null;
            //if (!int.TryParse(data[8], out info.RespawnIndex)) return null;
            if (!bool.TryParse(data[9], out info.SaveRespawnTime)) return null;
            if (!ushort.TryParse(data[10], out info.RespawnTicks)) return null;

            return info;
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(MonsterIndex);

            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(Count);
            writer.Write(Spread);

            writer.Write(Delay);
            writer.Write(Direction);

            writer.Write(RoutePath);

            writer.Write(RandomDelay);
            writer.Write(RespawnIndex);
            writer.Write(SaveRespawnTime);
            writer.Write(RespawnTicks);
        }

        public override string ToString()
        {
            return string.Format("Monster: {0} - {1} - {2} - {3} - {4} - {5} - {6} - {7} - {8} - {9}", MonsterIndex, Functions.PointToString(Location), Count, Spread, Delay, Direction, RandomDelay, RespawnIndex, SaveRespawnTime, RespawnTicks);
        }
    }
    //线路？怎么有个延时
    //这个是一些护卫的巡逻路径
    public class RouteInfo
    {
        public Point Location;
        public int Delay;//延迟

        public static RouteInfo FromText(string text)
        {
            string[] data = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (data.Length < 2) return null;

            RouteInfo info = new RouteInfo();

            int x, y;

            if (!int.TryParse(data[0], out x)) return null;
            if (!int.TryParse(data[1], out y)) return null;

            info.Location = new Point(x, y);

            if (data.Length <= 2) return info;

            return !int.TryParse(data[2], out info.Delay) ? info : info;
        }
    }
}