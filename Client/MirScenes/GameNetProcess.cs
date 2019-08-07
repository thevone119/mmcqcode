using Client.MirObjects;
using Client.MirScenes.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using S = ServerPackets;

namespace Client.MirScenes
{
    //想抽出来，算了,本来接受数据就要对场景进行各种更新
    //因为要和场景中的各种数据打交道，因此放在场景中更好吧。
    class GameNetProcess
    {
        public static GameScene Scene;

        //返回心跳包
        private void KeepAlive(S.KeepAlive p)
        {
            if (p.Time == 0) return;
            Scene.PingTime = (CMain.Time - p.Time);//这个时间不准吧
        }

        //服务器端返回的地图信息
        private void MapInformation(S.MapInformation p)
        {
            if (Scene.MapControl != null && !Scene.MapControl.IsDisposed)
                Scene.MapControl.Dispose();
            Scene.MapControl = new MapControl { FileName = Path.Combine(Settings.MapPath, p.FileName + ".map"), Title = p.Title, MiniMap = p.MiniMap, BigMap = p.BigMap, Lights = p.Lights, Lightning = p.Lightning, Fire = p.Fire, MapDarkLight = p.MapDarkLight, Music = p.Music, SafeZones=p.SafeZones, CanFastRun = p.CanFastRun, DrawAnimation = p.DrawAnimation };
            MirLog.info("DrawAnimation:"+ Scene.MapControl.DrawAnimation);
            Scene.MapControl.LoadMap();
            Scene.InsertControl(0, Scene.MapControl);
        }
        //服务器端返回用户信息
        private void UserInformation(S.UserInformation p)
        {
            GameScene.User = new UserObject(p.ObjectID);
            GameScene.User.Load(p);
            Scene.MainDialog.PModeLabel.Visible = GameScene.User.Class == MirClass.Wizard || GameScene.User.Class == MirClass.Taoist;
            GameScene.Gold = p.Gold;
            GameScene.Credit = p.Credit;

            Scene.InventoryDialog.RefreshInventory();
            foreach (SkillBarDialog Bar in Scene.SkillBarDialogs)
                Bar.Update();
        }
    }
}
