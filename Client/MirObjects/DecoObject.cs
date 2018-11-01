using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Client.MirGraphics;
using Client.MirScenes;
using S = ServerPackets;

namespace Client.MirObjects
{
    /// <summary>
    /// 这个是小动物，小房子等的图片而已,感觉好像没什么鸟用
    /// 应该是NPC对话框中出现的图片吧？
    /// 目前还不知道有什么用
    /// 但是这个不属于对话框的？是显示在地图上的么？什么时候有这个东西哦？
    /// </summary>
    class DecoObject : MapObject
    {
        public override ObjectType Race
        {
            get { return ObjectType.Deco; }
        }

        public override bool Blocking
        {
            get { return false; }
        }

        public ushort Image;

        public DecoObject(uint objectID)
            : base(objectID)
        {
        }

        public void Load(S.ObjectDeco info)
        {
            CurrentLocation = info.Location;
            MapLocation = info.Location;
            GameScene.Scene.MapControl.AddObject(this);
            Image = info.Image;

            BodyLibrary = Libraries.Deco;
        }
        public override void Process()
        {
            DrawLocation = new Point((CurrentLocation.X - User.Movement.X + MapControl.OffSetX) * MapControl.CellWidth, (CurrentLocation.Y - User.Movement.Y + MapControl.OffSetY) * MapControl.CellHeight);
            DrawLocation.Offset(GlobalDisplayLocationOffset);
            DrawLocation.Offset(User.OffSetMove);
        }

        public override void Draw()
        {
            BodyLibrary.Draw(Image, DrawLocation, DrawColour, true);
        }

        public override bool MouseOver(Point p)
        {
            return false;
        }

        public override void DrawBehindEffects(bool effectsEnabled)
        {
        }

        public override void DrawEffects(bool effectsEnabled)
        {
        }
    }
}
