using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Server.MirDatabase
{
    //安全区域
    public class SafeZoneInfo
    {
        //安全区位置
        public Point Location;
        //大小
        public ushort Size;
        //是否开始坐标（这个应该没什么用？应该记录是否回城位置）
        public bool StartPoint;
        //地图引用
        [JsonIgnore]
        public MapInfo Info;

        public SafeZoneInfo()
        {

        }

        public SafeZoneInfo(BinaryReader reader)
        {
            Location = new Point(reader.ReadInt32(), reader.ReadInt32());
            Size = reader.ReadUInt16();
            StartPoint = reader.ReadBoolean();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Location.X);
            writer.Write(Location.Y);
            writer.Write(Size);
            writer.Write(StartPoint);
        }

        public override string ToString()
        {
            return string.Format("Map: {0}- {1}", Functions.PointToString(Location), StartPoint);
        }
    }
}
