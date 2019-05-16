using Server.MirDatabase;
using Server.MirObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using S = ServerPackets;

namespace Server.MirEnvir
{
    //玩家战役处理
    //1v1,2v2 3v3 处理
    public class PlayerPK : MapSpecialProcess
    {
        //自读属性
        private static readonly Point pointA =new Point(38,40), pointB = new Point(9,12);//双方集合点
        //最长战斗时间，30分钟，30分钟无法分胜负，直接结束
        private static readonly long MAX_PK_TIME = 1000 * 60 * 30;
        //最长等待对手时间
        private static readonly long MAX_SIGN_TIME = 1000 * 60 * 5;


        //lastPtime最后处理时间
        private long lastPtime;

        //PK开始时间
        private long SignStartTime,PKstartTime;

        //所有的参战成员
        public static List<PlayerPKMember> allMember = new List<PlayerPKMember>();
        //warType 1V1  2V2  3V3 5V5
        public static byte warType, costType;
        public static int fb_step=0;//0：等待中，1：已发起 2：已应战 3：战斗中

        //各种处理
        public override void Process(Map map)
        {
            //500毫秒处理一次哦
            if (lastPtime > Envir.Time)
            {
                return;
            }
            lastPtime = Envir.Time + 500;
            //
            if (allMember.Count == 0)
            {
                fb_step = 0;
                return;
            }
            //已有人发起挑战，则判断5分钟内是否有人应战，无人应战，则结束
            if (fb_step == 1)
            {
                if (SignStartTime == 0)
                {
                    SignStartTime = Envir.Time;
                }
                //无人应战，取消
                if (Envir.Time- SignStartTime> MAX_SIGN_TIME)
                {
                    //发送给报名玩家
                    foreach (var player in Envir.Players)
                    {
                        if (player == null || player.Dead)
                        {
                            continue;
                        }
                        PlayerPKMember pkm = getPKMember(player.Info.Index);
                        if (pkm == null)
                        {
                            continue;
                        }
                        player.ReceiveChat($"PK挑战赛无人敢于接受挑战，自动取消PK挑战赛", ChatType.System2);
                    }
                    allMember.Clear();
                    fb_step = 0;
                    map.mapSProcess = new PlayerPK();
                }
            }

            //完成配对，传送双入场
            if (fb_step == 2)
            {
                startPK(map);
                PKstartTime = Envir.Time;
                fb_step = 3;
            }

            //循环检测双方胜负
            if (fb_step == 3)
            {
                PKProcess(map);
            }

            //结束PK判断胜负
            if (fb_step == 4)
            {
                PKEndProcess(map);
                allMember.Clear();
                fb_step = 0;
                map.mapSProcess = new PlayerPK();
            }

        }

     

        //开始进行PK赛
        //1.检测费用，扣除费用
        //2.传送进入PK场
        private void startPK(Map map)
        {
            //这里检测下费用，并扣除费用
            //判断双方玩家是否都在线
            //费用是否足够
            bool online = true;
            bool hascost = true;
            foreach (PlayerPKMember m in allMember)
            {
                bool has = false;
                foreach (var player in Envir.Players)
                {
                    if (player == null || player.Dead)
                    {
                        continue;
                    }
                    if (player.Info.Index == m.gid)
                    {
                        has = true;
                        if (m.mainUser)
                        {
                            if (player.Account.Gold < costGold())
                            {
                                hascost = false;
                            }
                            if (player.Account.Credit < costCredit())
                            {
                                hascost = false;
                            }
                        }
                    }
                }
                if (!has)
                {
                    online = false;
                    break;
                }
            }

            //不在线，或者费用不足，均取消
            if (!online)
            {
                allMember.Clear();
                fb_step = 0;
                return;
            }
            if (!hascost)
            {
                allMember.Clear();
                fb_step = 0;
                return;
            }
            //传送双方入场
            //扣除费用，并传送双方入场
            foreach (var player in Envir.Players)
            {
                foreach (PlayerPKMember m in allMember)
                {
                    if (m.gid != player.Info.Index)
                    {
                        continue;
                    }
                    if (m.mainUser)
                    {
                        player.LoseCredit((uint)costCredit());
                        player.LoseGold((uint)costGold());
                        SMain.EnqueueDebugging($"{player.Name}参加PK赛扣除费用,金币{costGold()},元宝{costCredit()}");
                    }
                    if (m.WGroup == WarGroup.GroupA)
                    {
                        player.Teleport(map, pointA);
                    }
                    else
                    {
                        player.Teleport(map, pointB);
                    }
                    break;
                }
            }
        }


        /// <summary>
        /// pk赛循环检测处理
        /// </summary>
        /// <param name="map"></param>
        private void PKProcess(Map map)
        {
            //如果时间到了
            if(Envir.Time - PKstartTime > MAX_PK_TIME)
            {
                fb_step = 4;
                return;
            }

            //判断当前地图下玩家死亡数，如果对方全部死亡，则结束
            bool hasA=false, hasB=false;
            foreach(PlayerObject p in map.Players)
            {
                if (p == null || p.Dead)
                {
                    continue;
                }
                PlayerPKMember m = getPKMember(p.Info.Index);
                if (m == null)
                {
                    continue;
                }
                if(m.WGroup== WarGroup.GroupA)
                {
                    hasA = true;
                }
                if (m.WGroup == WarGroup.GroupB)
                {
                    hasB = true;
                }
            }
            if(!hasA || !hasB)
            {
                fb_step = 4;
                return;
            }
        }

        /// <summary>
        /// pk赛结束，判断胜负关系
        /// 并传送出PK场地
        /// </summary>
        /// <param name="map"></param>
        private void PKEndProcess(Map map)
        {
            //地图中的玩家，都传送出去哦
            List<PlayerObject> mappls = new List<PlayerObject>();
            //判断当前地图下A,B组的玩家数，那个组的玩家数多，哪个组就获胜。获胜一方可获得奖励
            int liveA = 0, liveB = 0;
            foreach (PlayerObject p in map.Players)
            {
                if (p == null || p.Dead)
                {
                    continue;
                }
                mappls.Add(p);
                PlayerPKMember m = getPKMember(p.Info.Index);
                if (m == null)
                {
                    continue;
                }
                if (m.WGroup == WarGroup.GroupA)
                {
                    liveA++;
                }
                if (m.WGroup == WarGroup.GroupB)
                {
                    liveB++;
                }
            }
            //地图中的玩家全部传送出去
            foreach (PlayerObject p in mappls)
            {
                p.TeleportBackHome();
            }

            //A胜利
            if (liveA > liveB)
            {
                foreach (PlayerPKMember m in allMember)
                {
                    if(m.WGroup== WarGroup.GroupA)
                    {
                        m.WinState = 1;
                    }
                    else
                    {
                        m.WinState = 2;
                    }
                }
            }
            //B胜利
            if (liveA < liveB)
            {
                foreach (PlayerPKMember m in allMember)
                {
                    if (m.WGroup == WarGroup.GroupA)
                    {
                        m.WinState = 2;
                    }
                    else
                    {
                        m.WinState = 1;
                    }
                }
            }
            //平局
            if (liveA == liveB)
            {
                foreach (PlayerPKMember m in allMember)
                {
                    m.WinState = 3;
                }
            }
            int wGold = WinGold();
            int wCredit = WinCredit();
            //发奖励，发消息
            foreach (PlayerObject p in Envir.Players)
            {
                if (p == null)
                {
                    continue;
                }
                PlayerPKMember pkm = getPKMember(p.Info.Index);
                if (pkm == null)
                {
                    continue;
                }
                if (pkm.WinState == 1)
                {
                    if (pkm.mainUser)
                    {
                        p.ReceiveChat($"PK挑战赛取得胜利，获得:{wCredit}元宝,{wGold}金币", ChatType.System2);
                        p.GainGold((uint)wGold);
                        p.GainCredit((uint)wCredit);
                        SMain.EnqueueDebugging($"{p.Name} PK挑战赛取得胜利,获得:{wCredit}元宝,{wGold}金币");
                    }
                    else
                    {
                        p.ReceiveChat($"PK挑战赛取得胜利,领队获得相应奖励", ChatType.System2);
                    }
                }
                if (pkm.WinState == 2)
                {
                    p.ReceiveChat($"PK挑战赛失败，继续努力...", ChatType.System2);
                }
                if (pkm.WinState == 3)
                {
                    if (pkm.mainUser)
                    {
                        p.ReceiveChat($"PK挑战平局，获得:{wCredit / 2}元宝,{wGold / 2}金币", ChatType.System2);
                        p.GainGold((uint)wGold / 2);
                        p.GainCredit((uint)wCredit / 2);
                        SMain.EnqueueDebugging($"{p.Name} PK挑战平局，获得:{wCredit / 2}元宝,{wGold / 2}金币");
                    }
                    else
                    {
                        p.ReceiveChat($"PK挑战平局,领队获得相应奖励", ChatType.System2);
                    }
                }
                allMember.Remove(pkm);
            }

            //离线发奖励
            if (allMember.Count > 0)
            {
                for (int i = SMain.Envir.CharacterList.Count - 1; i >= 0; i--)
                {
                    CharacterInfo cinfo = SMain.Envir.CharacterList[i];
                    PlayerPKMember pkm = getPKMember(cinfo.Index);
                    if (pkm == null)
                    {
                        continue;
                    }
                    if (pkm.WinState == 1)
                    {
                        if (pkm.mainUser)
                        {
                            cinfo.AccountInfo.Gold += (uint)wGold;
                            cinfo.AccountInfo.Credit += (uint)wCredit;
                            SMain.EnqueueDebugging($"{cinfo.Name} (离线)PK挑战赛取得胜利,获得:{wCredit}元宝,{wGold}金币");
                        }
                    }
                    //平局
                    if (pkm.WinState == 3)
                    {
                        if (pkm.mainUser)
                        {
                            cinfo.AccountInfo.Gold += (uint)wGold/2;
                            cinfo.AccountInfo.Credit += (uint)wCredit/2;
                            SMain.EnqueueDebugging($"{cinfo.Name}(离线) PK挑战平局，获得:{wCredit / 2}元宝,{wGold / 2}金币");
                        }
                    }
                    allMember.Remove(pkm);
                }
            }
        }

        /// <summary>
        /// PK战发起
        /// </summary>
        /// <param name="p"></param>
        /// <param name="warType">1:1v1</param>
        /// <param name="costType">1-4</param>
        /// <returns></returns>
        public static string signUp(PlayerObject p, byte _warType, byte _costType)
        {
            if (fb_step != 0)
            {
                return "当前已有PK赛正在进行中...无法发起PK挑战";
            }
            //判断组队成员个数是否符合要求
            warType = _warType;
            costType = _costType;
            if (warType > 1)
            {
                if(p.GroupMembers==null|| p.GroupMembers.Count!= warType)
                {
                    return $"请保证组有且只有{warType}个成员，否则无法发起此类挑战";
                }
            }
            //判断费用是否足够
            String coststr = "";
            int _costGold = costGold(), _costCredit= costCredit();

            if (_costGold > 0)
            {
                coststr = _costGold / 10000 + "万金币";
            }
            if (_costCredit > 0)
            {
                coststr = _costCredit / 10000 + "万元宝";
            }

            if (p.Account.Gold< _costGold + 50000)
            {
                return $"您的金币数不足，小于({_costGold + 50000})金币，无法发起挑战";
            }

            if (p.Account.Credit < _costCredit)
            {
                return $"您的元宝数不足，小于({_costCredit})元宝，无法发起挑战";
            }
            //如果费用足够，则发起挑战
            //先扣除5万金币报名费用
            p.LoseGold(50000);
            //
            if (warType == 1)
            {
                PlayerPKMember m = new PlayerPKMember();
                m.WGroup = WarGroup.GroupA;
                m.gid = p.Info.Index;
                m.mainUser = true;
                allMember.Add(m);
            }
            else
            {
                foreach(PlayerObject pk in p.GroupMembers)
                {
                    PlayerPKMember m = new PlayerPKMember();
                    m.WGroup = WarGroup.GroupA;
                    m.gid = pk.Info.Index;
                    if(m.gid == p.Info.Index)
                    {
                        m.mainUser = true;
                    }
                    allMember.Add(m);
                }
            }


            //发送全服的公告
            foreach (var player in Envir.Players)
            {
                if (player == null || player.Dead)
                {
                    continue;
                }
                player.ReceiveChat($"{p.Name} 发起 {warType}V{warType} {coststr} PK挑战赛，谁不服，来战...", ChatType.Announcement);
            }

            fb_step = 1;
            return "PK挑战已发起,等待对手应战，请保持金币/元宝充足，否则视为弃权,5分钟内无人接受挑战则自动取消";
        }


        /// <summary>
        /// 取消发起
        /// </summary>
        /// <param name="p"></param>
        /// <param name="warType">1:1v1</param>
        /// <param name="costType">1-4</param>
        /// <returns></returns>
        public static string cancelSign(PlayerObject p)
        {
            if (allMember.Count == 0)
            {
                return "当前没有真正进行的PK赛，无法取消";
            }
            //是否A对的主人
            bool ismain = false;
            foreach (PlayerPKMember pk in allMember)
            {
                if (pk.mainUser && pk.gid == p.Info.Index && pk.WGroup == WarGroup.GroupA)
                {
                    ismain = true;
                }
            }
            if (!ismain)
            {
                return "您不是PK赛的发起人，无法取消";
            }
            allMember.Clear();
            fb_step = 0;
            return "已取消PK赛...";
        }

        /// <summary>
        /// PK战参加
        /// </summary>
        /// <param name="p"></param>
        /// <param name="warType">1:1v1</param>
        /// <param name="costType">1-4</param>
        /// <returns></returns>
        public static string JoinIn(PlayerObject p)
        {
            //0：等待中，1：已发起 2：已应战 3：战斗中
            if (fb_step==0)
            {
                return "当前没有玩家发起PK挑战，无法参加PK赛";
            }
            if (fb_step > 1)
            {
                return "当前已有PK挑战赛正在进行，请等待下场PK赛";
            }
            //判断成员是否符合
            if (warType > 1)
            {
                if (p.GroupMembers == null || p.GroupMembers.Count != warType)
                {
                    return $"请保证组有且只有{warType}个成员，否则参加当前的PK赛";
                }
            }
            //判断费用是否足够
            int _costGold = costGold(), _costCredit = costCredit();

            if (p.Account.Gold < _costGold)
            {
                return $"您的金币数不足，小于({_costGold })金币，无法参加PK赛";
            }

            if (p.Account.Credit < _costCredit)
            {
                return $"您的元宝数不足，小于({_costCredit})元宝，无法参加PK赛";
            }

            //判断是否已在参战方
            if (getPKMember(p.Info.Index) != null)
            {
                return $"您已属于参战成员,无法应战";
            }
            //


            //对战成立,把双方队伍传入
            if (warType == 1)
            {
                PlayerPKMember m = new PlayerPKMember();
                m.WGroup = WarGroup.GroupB;
                m.gid = p.Info.Index;
                m.mainUser = true;
                allMember.Add(m);
            }
            else
            {
                foreach (PlayerObject pk in p.GroupMembers)
                {
                    if (getPKMember(pk.Info.Index) != null)
                    {
                        return $"您的成员中存在发起挑战的成员,无法应战";
                    }
                    PlayerPKMember m = new PlayerPKMember();
                    m.WGroup = WarGroup.GroupB;
                    m.gid = pk.Info.Index;
                    if (m.gid == p.Info.Index)
                    {
                        m.mainUser = true;
                    }
                    allMember.Add(m);
                }
            }
            //判断双方玩家是否都在线
            //费用是否足够
            bool online = true;
            bool hascost = true;
            foreach(PlayerPKMember m in allMember)
            {
                bool has = false;
                foreach (var player in Envir.Players)
                {
                    if (player == null || player.Dead)
                    {
                        continue;
                    }
                    if (player.Info.Index == m.gid)
                    {
                        has = true;
                        if (m.mainUser)
                        {
                            if (player.Account.Gold < _costGold)
                            {
                                hascost = false;
                            }
                            if (player.Account.Credit < _costCredit)
                            {
                                hascost = false;
                            }
                        }
                    }
                }
                if (!has)
                {
                    online = false;
                    break;
                }
            }

            //不在线，或者费用不足，均取消
            if (!online)
            {
                allMember.Clear();
                fb_step = 0;
                return "PK赛发起方不在线，PK赛取消";
            }
            if (!hascost)
            {
                allMember.Clear();
                fb_step = 0;
                return "PK赛发起方费用不足，PK赛取消";
            }



            //发送全服的公告
            foreach (var player in Envir.Players)
            {
                if (player == null || player.Dead)
                {
                    continue;
                }
                player.ReceiveChat($"{p.Name} 参加 {warType}V{warType} PK挑战赛，勇气可嘉...", ChatType.Announcement);
            }
            fb_step = 2;

            return "参加PK赛成功，马上进入PK赛场...";
        }

       
        public static int costGold()
        {
            switch (costType)
            {
                case 1:
                    return 10000 * 10;
                case 2:
                    return 10000 * 100;
                case 3:
                    return 0;
                case 4:
                    return 0;
            }
            return 0;
        }


        public static int costCredit()
        {
            switch (costType)
            {
                case 1:
                    return 0;
                case 2:
                    return 0;
                case 3:
                    return 10000 * 1;
                case 4:
                    return 10000 * 10;
            }
            return 0;
        }

        //胜利后获得金币
        public static int WinGold()
        {
            int g = costGold();
            if (g == 0)
            {
                return 0;
            }
            return g + g * 8 / 10;
        }
        //胜利后获得元宝
        public static int WinCredit()
        {
            int g = costCredit();
            if (g == 0)
            {
                return 0;
            }
            return g + g * 8 / 10;
        }


        public static PlayerPKMember getPKMember(ulong gid)
        {
            foreach(PlayerPKMember p in allMember)
            {
                if(p.gid== gid)
                {
                    return p;
                }
            }
            return null;
        }




    }

    //PK赛的玩家，成员
    public class PlayerPKMember
    {
        //玩家角色id
        public ulong gid;

        public bool mainUser;//是否发起人

        public byte costType;//费用类型

        //最后胜负关系 0：未知  1：胜利 2：失败 3：平局
        public byte WinState;

        //归属队伍
        public WarGroup WGroup;

    }
}
