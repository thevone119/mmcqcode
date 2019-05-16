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
    //军团战役
    //军团PK场。
    //1.活动每天晚上8点报名，报名后8点10分统计
    //2.每天晚上9点报名，匹配战役
    //
    public class GroupWar : MapSpecialProcess
    {
        //刷怪,3种怪物
        public static string fbboss1 = "夜火红猪";//每批都刷
        public static string fbboss2 = "夜火白猪";//2批一刷
        public static string fbboss3 = "夜火战车";//3批一刷


        //比奇安全区点
        private static Point safeBiqi = new Point(333, 265);

        //安全区域2个点
        private static Point safePointA = new Point(77, 22),safePointB = new Point(20, 79);
        //4个角落点(顺时针)
        private static Point nookPoint1 = new Point(50, 10), nookPoint2 = new Point(93, 52), nookPoint3 = new Point(48, 93), nookPoint4 = new Point(8, 52);
        //2个小BOSS点（上下房间，以及中间的点）
        private static Point monPointUpper = new Point(24, 22), monPointCenter = new Point(50, 50), monPointLower = new Point(81, 80);



        //最后处理时间
        private long lastPtime;

        private long HuodongStartTime;//战斗开始时间
        private long WarStartTime;//战斗开始时间



        private int mon_step;//刷怪副本的进度，每5分钟一个进度
        private long nextMonTime;//下一次刷怪时间
        private long nextTimePercent=0;//时间进度，每次1分钟，解除1点封印


        // (静态的，不重置)
        private static int War_Day=-1;//战斗的天
        private static byte _warType;//当前的战役类型 1匹配   2军团
        private static int fb_step;//副本执行阶段,不同阶段，不一样的0：未开始/已经结束

        //战役时间处理,warHour：活动开始整点，MaxBaoMingTime：最大报名时间（10分钟），MaxWarTime：最大的战斗时间（45分钟）
        //怪物刷新间隔MonRefreshInterval，5分钟刷一批
        private static int warHour = 22, MaxBaoMingTime = 1000*60*2, MaxWarTime = 1000*60*5, MonRefreshInterval=1000*60*1,minWarPlayCount=2;


        //各种处理
        public override void Process(Map map)
        {
            if (!Settings.openGroupWar)
            {
                return;
            }
            //500毫秒处理一次哦
            if (lastPtime > Envir.Time)
            {
                return;
            }
            lastPtime = Envir.Time + 500;
            //非战斗过程,把地图的玩家全部传送出去
            if (fb_step != 5 && map.Players.Count > 0)
            {
                List<PlayerObject> pls = new List<PlayerObject>();

                foreach (var player in map.Players)
                {
                    pls.Add(player);
                }
                Map bqimap = Envir.GetMapByNameAndInstance("0");
                foreach (var player in pls)
                {
                    player.ChangeNameColour = Color.White;
                    player.WarAttackPercent = 100;
                    player.WGroup = WarGroup.None;
                    player.Teleport(bqimap, safeBiqi);
                }
            }


            //判断当前时间是否在8点10分-9点之间
            DateTime now = Envir.Now;
            int day = now.DayOfYear;
            //1.如果是20点，并且活动未开始，则开始活动，重置各种数据
            if (day!= War_Day && fb_step == 0 && now.Hour == warHour)
            {
                War_Day = day;
                fb_step = 1;
                mon_step = 0;
                nextMonTime = 0;
                nextTimePercent = 0;
                WarStartTime = 0;
                HuodongStartTime = Envir.Time;//活动开始时间
                return;
            }

            //未开始/已结束，则直接返回
            if(fb_step == 0)
            {
                return;
            }

            //2. 8点,发送全服公告，活动开始接受报名
            if (fb_step == 1)
            {
                //小于5人，直接结束
                if (Envir.Players.Count < 5)
                {
                    //lastPtime = Envir.Time + 1000 * 60 * 60;
                    //fb_step = 0;
                    //return;
                }

                foreach (var player in Envir.Players)
                {
                    player.ReceiveChat($"封神战役现已开始接受报名，各位勇士想要封神的，可来报名参赛", ChatType.System2);
                    player.ReceiveChat($"封神战役现已开始接受报名，各位勇士想要封神的，可来报名参赛", ChatType.System2);
                }
                fb_step = 2;
                SMain.Enqueue( $" 封神战役开启...");
                //这里清理怪物
                map.ClearMonster();
                //3小时内，安全区无法自动治疗
                //map.SafeZoneHealingTime = Envir.Time + 1000 * 60 * 60 * 3;
                lastPtime = Envir.Time + 1000*30;
                return;
            }
            //报名阶段
            if (fb_step == 2)
            {
                long starttime = HuodongStartTime + MaxBaoMingTime - Envir.Time;
                if (starttime>0)
                {
                    foreach (var player in Envir.Players)
                    {
                        player.ReceiveChat($"封神战役现已开始接受报名，各位勇士想要封神的，可来报名参赛,报名剩余时间{starttime / 1000}秒", ChatType.System2);
                    }
                    lastPtime = Envir.Time + 1000 * 30;
                    return;
                }
                else
                {
                    GroupWarPlayer.clearHuodongPlayer();
                    MatchPlayer(map);
                    //清理报名玩家
                    GroupWarPlayer.clearBaoming(day);

                    if (GroupWarPlayer.getHuodongPlayerCount() == 0)
                    {
                        foreach (var player in Envir.Players)
                        {
                            player.ReceiveChat($"封神战役由于报名玩家数较少，今日封神战役取消", ChatType.System2);
                        }
                        fb_step = 0;
                        return;
                    }
                    //计算对赌
                    GroupWarPlayer.countBet();
                    //扣除相应的金币，元宝
                    foreach (var player in Envir.Players)
                    {
                        GroupWarPlayer gp = GroupWarPlayer.getHuodongPlayer(player.Info.Index);
                        //活动玩家扣除费用
                        if (gp != null)
                        {
                            uint deGold = (uint)(gp.deGold + 10 * 10000);
                            uint deCredit = (uint)gp.deCredit;
                            //扣除玩家金币
                            player.LoseGold((uint)(gp.deGold + 10 * 10000));
                            //扣除玩家元宝
                            player.LoseCredit((uint)gp.deCredit);
                            player.ReceiveChat($"报名成功，匹配战役开始，扣除金币{deGold},扣除元宝{deCredit},开始你的战斗吧...", ChatType.System2);
                            //这里记录下玩家的金币，元宝扣除情况，否则出问题了，查不了记录麻烦
                            SMain.EnqueueDebugging(player.Name + $" 报名成功，匹配战役开始，扣除金币{deGold},扣除元宝{deCredit},开始你的战斗吧...");
                            //记录下，避免重启之类的，赌注丢失？
                            player.Info.putSaveValue("GROUP_WAR_GOLD", (int)deGold);
                            player.Info.putSaveValue("GROUP_WAR_CREDIT", (int)deCredit);

                        }
                        else
                        {
                            player.ReceiveChat($"封神战役已经匹配完成，马上进入封神之战", ChatType.System2);
                            player.ReceiveChat($"封神战役已经匹配完成，马上进入封神之战", ChatType.System2);
                        }
                    }
                    fb_step = 3;
                    return;
                }
            }



            //3. 清理战场
            if (fb_step == 3)
            {
                fb_step = 4;
                return;
            }


            //4. 传送玩家入场，划分A,B队，封印玩家的能力
            if (fb_step == 4)
            {
                WarStartTime = Envir.Time;
                StartProcess(map);
                fb_step = 5;
                return;
            }

            //5.战斗过程中（持续40分钟，每分钟解除2%的封印）
            //每分钟刷新一批怪物，按照玩家数刷新，杀死每个怪物解封2%的攻击
            //5分 10分 15分 20分，每5分钟在中间3个位置各刷一个小BOSS，杀死的解封3%攻击，几率爆
            //30分，40分各刷一个大BOSS，杀死的全体队员解封5%的攻击，几率爆神器
            if (fb_step == 5)
            {

                WarProcess(map);
                //fb_step = 5;
                return;
            }

            //结束战役，退出地图
            if (fb_step == 6)
            {
                WarEnd(map);
                //重置
                fb_step = 0;
                map.mapSProcess = new GroupWar();
                return;
            }
        }

        /// <summary>
        /// 匹配玩家进入战役
        /// 玩家匹配全部在这里
        /// </summary>
        /// <param name="map"></param>
        private void MatchPlayer(Map map)
        {
            DateTime now = Envir.Now;
            int day = now.DayOfYear;
            //当前报名的 取出当天报名的玩家
            List<GroupWarPlayer> listpp1 = GroupWarPlayer.getBaoming(day);
            //匹配的玩家
            List<PlayerObject> listpp2 = new List<PlayerObject>();
            //匹配军团的玩家
            List<List<PlayerObject>> listpp3 = new List<List<PlayerObject>>();

            //没有玩家报名，直接结束
            if (listpp1.Count == 0)
            {
                fb_step = 0;
                return;
            }
            //看下是匹配玩家还是匹配军团


            //安全区的点

            Point biqi = new Point(330,268);
            //2.取比奇安全区的玩家
            foreach (var player in Envir.Players)
            {
                
                //死亡或者小于35级的，不能参加
                if (player.Dead|| player.Level<35|| player.Level<Envir.MaxLevel-10)
                {
                    continue;
                }
                //不在安全区的不算
                if (player.CurrentMap == null || player.CurrentMap.Info == null  || !player.InSafeZone)
                {
                    //continue;
                }
                
                //不在比奇安全区15格以内的不算
                if(player.CurrentMap.Info.Mcode != "0"||!Functions.InRange(biqi, player.CurrentLocation, 15))
                {
                    //continue;
                }


                
                //匹配上面的报名人
                //匹配模式
                if (_warType == 1)
                {
                    foreach (GroupWarPlayer t in listpp1)
                    {
                       
                        if (t.gid == player.Info.Index)
                        {
                            //判断金币元宝是否充足
                            if(t.checkMoney(player))
                            {
                                listpp2.Add(player);
                            }
                            break;
                        }
                    }
                }
                //军团战模式
                if (_warType == 2)
                {
                    if (player.MyGuild == null)
                    {
                        continue;
                    }
                    foreach (GroupWarPlayer t in listpp1)
                    {
                        if(t.gid== player.Info.Index)
                        {
                            if (!t.checkMoney(player))
                            {
                                break;
                            }
                            bool has = false;
                            foreach(List<PlayerObject> l in listpp3)
                            {
                                if(l[0].MyGuild.Guildindex== player.MyGuild.Guildindex)
                                {
                                    l.Add(player);
                                    has = true;
                                    break;
                                }
                            }
                            if (!has)
                            {
                                List<PlayerObject> l = new List<PlayerObject>();
                                l.Add(player);
                                listpp3.Add(l);
                            }
                            break;
                        }
                    }
                }
            }

            //匹配模式
            if (_warType == 1)
            {
                if (listpp2.Count < minWarPlayCount)
                {
                    fb_step = 0;
                    return;
                }
                listpp2.Sort((a, b) => -a.Level.CompareTo(b.Level));//等级降序排列
                //如果是单数，则剔除最后一名
                if (listpp2.Count % 2 == 1)
                {
                    listpp2.RemoveAt(listpp2.Count-1);
                }
                //这里按职业，等级，积分进行匹配,
                //1.职业优先原则，积分/100+等级
                listpp2.Sort(delegate (PlayerObject p1, PlayerObject p2) {
                    if (p1.Class == p2.Class)
                    {
                        int s1 = p1.Level + p1.Info.getFb2_score() / 100;
                        int s2 = p2.Level + p2.Info.getFb2_score() / 100;
                        return s2.CompareTo(s1);
                    }
                    else
                    {
                        return p2.Class.CompareTo(p1.Class);
                    }
                });
                //分配队伍
                for(int i=0;i< listpp2.Count; i++)
                {
                    if ( i%2== 0)
                    {
                        GroupWarPlayer.addHuodongPlayer(WarGroup.GroupA, listpp2[i].Info.Index);
                    }
                    else
                    {
                        GroupWarPlayer.addHuodongPlayer(WarGroup.GroupB, listpp2[i].Info.Index);
                    }
                }
            }
            //军团战模式
            //2.计算等级最接近的5个级别差的玩家
            if (_warType == 2)
            {
                //这里，如果只有一个队伍，直接退出
                if (listpp3.Count <= 1)
                {
                    fb_step = 0;
                    return;
                }
                listpp3.Sort((a, b) => -a.Count.CompareTo(b.Count));//降序排列

                //如果多过3个队伍，只保留3个队伍
                for(int i = 0; i < 20; i++)
                {
                    if (listpp3.Count > 3)
                    {
                        listpp3.RemoveAt(listpp3.Count-1);
                    }
                    else
                    {
                        break;
                    }
                }
                //3个队伍，如果第3个队伍人数少于第2个队伍的2/3人数，则直接剔除第3个队伍
                if (listpp3.Count > 2)
                {
                    if(listpp3[2].Count< listpp3[2].Count * 2 / 3|| listpp3[2].Count<2)
                    {
                        listpp3.RemoveAt(2);
                    }
                }
                //3个队伍，随机剔除1个
                if (listpp3.Count > 2)
                {
                    listpp3.RemoveAt(RandomUtils.Next(1, 3));
                }

                //如果是2个队伍，则直接匹配上，摘取2个队伍中的最高等级的相同人数
                if (listpp3.Count == 2)
                {
                    int minp = 20;
                    foreach (List<PlayerObject> l in listpp3)
                    {
                        l.Sort((a, b) => -a.Level.CompareTo(b.Level));//降序排列
                        if (l.Count < minp)
                        {
                            minp = l.Count;
                        }
                    }
                    //队伍少于2个人，直接结束
                    if (minp < minWarPlayCount/2)
                    {
                        fb_step = 0;
                        return;
                    }
                    //抽取前面几个进入匹配队伍
                    int idx = 0;
                    foreach (List<PlayerObject> l in listpp3)
                    {
                        for (int i = 0; i < minp; i++)
                        {
                            if (idx == 0)
                            {
                                GroupWarPlayer.addHuodongPlayer(WarGroup.GroupA, l[i].Info.Index);
                            }
                            else
                            {
                                GroupWarPlayer.addHuodongPlayer(WarGroup.GroupB, l[i].Info.Index);
                            }
                        }
                        idx++;
                    }
                }
            }
        }


        /// <summary>
        /// 开始战役
        /// 传送玩家入场，划分A,B队，封印玩家的能力
        /// </summary>
        /// <param name="map"></param>
        private void StartProcess(Map map)
        {
            foreach (var player in Envir.Players)
            {
                //A队
                GroupWarPlayer warp = GroupWarPlayer.getHuodongPlayer(player.Info.Index);
                if (warp == null)
                {
                    continue;
                }
                if (warp.WGroup == WarGroup.GroupA)
                {
                    player.Teleport(map, safePointA);
                    player.ChangeNameColour = warp.NameColour;
                    //player.NameColour = warp.NameColour;
                    //player.RefreshNameColour();
                    player.WarAttackPercent = warp.WarAttackPercent;
                    player.WGroup = warp.WGroup;
                }
                //B对
                if (warp.WGroup == WarGroup.GroupB)
                {
                    player.Teleport(map, safePointB);
                    player.ChangeNameColour = warp.NameColour;
                    //player.NameColour = warp.NameColour;
                    //player.RefreshNameColour();
                    player.WarAttackPercent = warp.WarAttackPercent;
                    player.WGroup = warp.WGroup;
                }
            }
        }

        /// <summary>
        ///4.战斗过程中（持续50分钟，每分钟解除1%的封印）
        //每分钟刷新一批怪物，按照玩家数刷新，杀死每个怪物解封1%的攻击
        //5分 10分 15分 20分，每5分钟在中间3个位置各刷一个小BOSS，杀死的解封3%攻击，几率爆
        //30分，40分各刷一个大BOSS，杀死的全体队员解封5%的攻击，几率爆神器
        /// </summary>
        /// <param name="map"></param>
        private void WarProcess(Map map)
        {
            //检测是否结束,时间到了，结束
            if (Envir.Time - WarStartTime > MaxWarTime)
            {
                fb_step = 6;
                return;
            }
            //每个人6条命
            int maxKill = GroupWarPlayer.getHuodongPlayerCount()  * 3;
            //杀敌数到了，结束
            if (GroupWarPlayer.GroupAkill >= maxKill|| GroupWarPlayer.GroupBkill >= maxKill)
            {
                fb_step = 6;
                return;
            }

            //超过一半玩家认输，直接输
            if (GroupWarPlayer.renshuEnd())
            {
                fb_step = 6;
                return;
            }


            //刷怪处理
            RefreshFBMon(map);
            //一直检查玩家的模式，自动切换模式,包括能力值
            ChangePaly(map);
            //安全区域处理，自动伤害敌对玩家
            SafeAreaProcess(map);
        }

        /// <summary>
        /// 安全区域排除对方
        /// </summary>
        /// <param name="map"></param>
        private void SafeAreaProcess(Map map)
        {
            //伤害系数随着时间递减（不递减了）
            List<MapObject> listA = map.getMapObjects(safePointA.X, safePointA.Y, 4);
            int count = 0;
            foreach(MapObject ob in listA)
            {
                if(ob==null || ob.Race!= ObjectType.Player)
                {
                    continue;
                }
               if(ob.WGroup== WarGroup.GroupA)
                {
                    continue;
                }
                int sh = (int)ob.MaxHealth/5;
                ob.Struck(sh, DefenceType.None);
                //最多伤害2个玩家
                if (count++>1)
                {
                    break;
                }
            }

            List<MapObject> listB = map.getMapObjects(safePointB.X, safePointB.Y, 4);
            count = 0;
            foreach (MapObject ob in listB)
            {
                if (ob == null || ob.Race != ObjectType.Player)
                {
                    continue;
                }
                if (ob.WGroup == WarGroup.GroupB)
                {
                    continue;
                }
                int sh = (int)ob.MaxHealth / 5;
                ob.Struck(sh, DefenceType.None);
                //最多伤害2个玩家
                if (count++ > 1)
                {
                    break;
                }
            }
        }


        //一直检查玩家的能力，每分钟更新一次玩家的能力+2解封速度
        private void ChangePaly(Map map)
        {
            if (nextTimePercent == 0)
            {
                nextTimePercent = Envir.Time + 1000 * 60;
            }
            //每分钟解除2点封印
            if(Envir.Time> nextTimePercent)
            {
                nextTimePercent = Envir.Time + 1000 * 60;
                GroupWarPlayer.addHuodongTimeWarAttackPercent(2);
                sendHuodongPercent(WarGroup.None);
            }

            foreach (var player in map.Players)
            {
                if (player == null)
                {
                    continue;
                }
                //A队
                GroupWarPlayer warp = GroupWarPlayer.getHuodongPlayer(player.Info.Index);
                if (warp == null)
                {
                    continue;
                }
                if (warp.WGroup == WarGroup.GroupA)
                {
                    //player.Teleport(map, safePoint1);
                    player.ChangeNameColour = warp.NameColour;
                    player.WarAttackPercent = warp.WarAttackPercent;
                    player.WGroup = warp.WGroup;
                }
                //B对
                if (warp.WGroup == WarGroup.GroupB)
                {
                    //player.Teleport(map, safePoint2);
                    player.ChangeNameColour = warp.NameColour;
                    player.WarAttackPercent = warp.WarAttackPercent;
                    player.WGroup = warp.WGroup;
                }
            }
        }


        /// <summary>
        /// 刷怪处理（刷10次怪，每次格6分钟）
        /// 分阶段刷怪
        /// 1
        /// 2
        /// 3
        /// 4
        /// 5
        /// 6
        /// 7
        /// 8
        /// 9
        /// 10
        /// </summary>
        /// <param name="map"></param>
        /// <param name="SP"></param>
        private void RefreshFBMon(Map map)
        {
            if(Envir.Time < nextMonTime)
            {
                return;
            }
            //每5分钟刷一批怪
            nextMonTime = Envir.Time + MonRefreshInterval;
            mon_step++;
            //1.小怪是每次都刷，刷在4个角落里,根据玩家数刷，4个玩家则刷4个，8个玩家则刷8个
            MonsterInfo info = getFBMon(1);
            int pcount = GroupWarPlayer.getHuodongPlayerCount() / 4;
            if (pcount < 1)
            {
                pcount = 1;
            }
            for (int i = 0; i < pcount; i++)
            {
                for(int j = 0; j < 4; j++)
                {
                    MonsterObject monster = MonsterObject.GetMonster(info);
                    if (monster != null)
                    {
                        monster.IsCopy = true;
                        monster.RefreshAll();
                        switch (j)
                        {
                            case 0:
                                monster.SpawnNew(map, nookPoint1);
                                break;
                            case 1:
                                monster.SpawnNew(map, nookPoint2);
                                break;
                            case 2:
                                monster.SpawnNew(map, nookPoint3);
                                break;
                            case 3:
                                monster.SpawnNew(map, nookPoint4);
                                break;
                        }
                    }
                }
            }
            //2批一刷
            if (mon_step % 2 == 0)
            {
                for(int i = 0; i < 3; i++)
                {
                    MonsterInfo info2 = getFBMon(2);
                    MonsterObject monster2 = MonsterObject.GetMonster(info2);
                    if (monster2 != null)
                    {
                        monster2.IsCopy = true;
                        monster2.RefreshAll();
                        switch (i)
                        {
                            case 0:
                                monster2.SpawnNew(map, monPointUpper);
                                break;
                            case 1:
                                monster2.SpawnNew(map, monPointCenter);
                                break;
                            case 2:
                                monster2.SpawnNew(map, monPointLower);
                                break;
                        }
                    }
                }
            }

            //3批一刷
            if (mon_step % 3 == 0)
            {
                MonsterInfo info3 = getFBMon(3);
                MonsterObject monster3 = MonsterObject.GetMonster(info3);
                if (monster3 != null)
                {
                    monster3.IsCopy = true;
                    monster3.RefreshAll();
                    monster3.SpawnNew(map, monPointCenter);
                }
            }


            switch (mon_step)
            {
                case 1:
                    break;
                case 2:
                    //MonsterInfo info2 = getFBMon(2);

                    break; 
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                case 6:
                    break;
                case 7:
                    break;
                case 8:
                    break;
                case 9:
                    break;
                case 10:
                    break;
            }
        }

        //怪物类型
        private MonsterInfo getFBMon(byte montype)
        {
            MonsterInfo info = null;
            switch (montype)
            {
                case 1:
                    info = Envir.GetMonsterInfo(fbboss1);
                    info = info.Clone();
                    break;
                case 2:
                    info = Envir.GetMonsterInfo(fbboss2);
                    info = info.Clone();
                    break;
                case 3:
                    info = Envir.GetMonsterInfo(fbboss3);
                    info = info.Clone();
                    break;
            }
            return info;
        }

        /// <summary>
        /// 战役结束处理
        /// 包括积分处理
        /// </summary>
        /// <param name="map"></param>
        private void WarEnd(Map map)
        {
            //判断胜负关系，1A组胜利，2，B组胜利
            GroupWarPlayer.countWin();
            //玩家恢复模式，攻击，传送回比奇
            Map bqimap = Envir.GetMapByNameAndInstance("0");
            foreach (var player in Envir.Players)
            {
                GroupWarPlayer warp = GroupWarPlayer.getHuodongPlayer(player.Info.Index);
                if (warp == null)
                {
                    continue;
                }
                player.ChangeNameColour = Color.White;
                player.WarAttackPercent = 100;
                player.WGroup = WarGroup.None;
                player.Teleport(bqimap, safeBiqi);
                //发放奖励
                if (warp.WinState==1)
                {
                    int winCredit = 0, winGold = 0;
                    //胜利，发放胜利奖励
                    if (warp.WinCredit > 0)
                    {
                        winCredit = (warp.WinCredit * 8 / 10 + warp.deCredit);
                    }
                    if (warp.WinGold > 0)
                    {
                        winGold = (warp.WinGold * 8 / 10 + warp.deGold);
                    }
                    //增加积分
                    int fb2_score = player.Info.getFb2_score();
                    int add_score = 0;
                    if (fb2_score < 500)
                    {
                        add_score = 100;
                    }else if(fb2_score < 800)
                    {
                        add_score = 90;
                    }
                    else if (fb2_score < 1000)
                    {
                        add_score = 80;
                    }
                    else
                    {
                        add_score = 70;
                    }
                    player.ReceiveChat($"恭喜你在战役中取得胜利,杀数:{warp.killuser},比分：黄队({GroupWarPlayer.GroupAkill})蓝队({GroupWarPlayer.GroupBkill})", ChatType.Announcement);
                    player.ReceiveChat($"战役胜利获得:{winCredit}元宝,{winGold}金币,{warp.WinExp}经验,{add_score}积分", ChatType.System2);
                    player.GainCredit((uint)winCredit);
                    player.GainGold((uint)winGold);
                    player.GainExp((uint)warp.WinExp,true);
                    player.Info.addFb2_score(add_score);
                    SMain.EnqueueDebugging(player.Name + $" 在战役中取得胜利，获得:{winCredit}元宝,{winGold}金币,{warp.WinExp}经验,{add_score}积分");
                }
                else
                {
                    //失败
                    player.ReceiveChat($"战役失败，比分：黄队({GroupWarPlayer.GroupAkill})蓝队({GroupWarPlayer.GroupBkill})，基于您的团队贡献，获得{warp.LoseExp}经验,失去战役100积分", ChatType.System2);
                    player.GainExp((uint)warp.LoseExp, false);
                    player.Info.addFb2_score(-100);
                }
                //记录下，重置赌注哦
                player.Info.putSaveValue("GROUP_WAR_GOLD", (int)0);
                player.Info.putSaveValue("GROUP_WAR_CREDIT", (int)0);
                //删除活动人物
                GroupWarPlayer.delHuodongPlayer(player.Info.Index);
            }

            //有部分活动玩家中途退场的，直接更改角色信息
            if (GroupWarPlayer.getHuodongPlayerCount() > 0)
            {
                for (int i = SMain.Envir.CharacterList.Count - 1; i >= 0; i--)
                {
                    CharacterInfo cinfo = SMain.Envir.CharacterList[i];
                    GroupWarPlayer warp = GroupWarPlayer.getHuodongPlayer(cinfo.Index);
                    if (warp == null)
                    {
                        continue;
                    }
                    cinfo.CurrentMapIndex = bqimap.Info.Index;
                    cinfo.CurrentLocation = safeBiqi;
                    //
                    if (warp.WinState == 1)
                    {
                        int winCredit = 0, winGold = 0;
                        //胜利，发放胜利奖励
                        if (warp.WinCredit > 0)
                        {
                            winCredit = (warp.WinCredit * 8 / 10 + warp.deCredit);
                        }
                        if (warp.WinGold > 0)
                        {
                            winGold = (warp.WinGold * 8 / 10 + warp.deGold);
                        }
                        cinfo.AccountInfo.Gold += (uint)winGold;
                        cinfo.AccountInfo.Credit += (uint)winCredit;
                        cinfo.addFb2_score(50);//离线了，只加50分
                        SMain.EnqueueDebugging(cinfo.Name + $" （离线）在战役中取得胜利，获得:{winCredit}元宝,{winGold}金币");
                        //如果离线了，没有经验获得哦
                        //player.GainExp((uint)warp.WinExp, false);
                    }
                    else
                    {
                        //输得，直接扣100分
                        cinfo.addFb2_score(-100);
                    }
                    cinfo.putSaveValue("GROUP_WAR_GOLD", (int)0);
                    cinfo.putSaveValue("GROUP_WAR_CREDIT", (int)0);
                    GroupWarPlayer.delHuodongPlayer(cinfo.Index);
                }
            }
        }
        






        //副本怪物死亡，怪物死亡会调用这个方法
        public override void monDie(MonsterObject mon)
        {
            if (mon.EXPOwner == null|| mon.EXPOwner.Race != ObjectType.Player)
            {
                return;
            }
            PlayerObject kmp = (PlayerObject)mon.EXPOwner;
            kmp.ReceiveChat($"你杀死一个怪物获得相应能力解封", ChatType.System2);
            GroupWarPlayer wp = GroupWarPlayer.getHuodongPlayer(kmp.Info.Index);
            if (wp == null)
            {
                return;
            }
            if (mon.Name == fbboss1)
            {
                wp.killmon1++;
            }
            if (mon.Name == fbboss2)
            {
                wp.killmon2++;
                GroupWarPlayer.addHuodongTeamWarAttackPercent(wp.WGroup,2);
            }
            if (mon.Name == fbboss3)
            {
                wp.killmon3++;
                GroupWarPlayer.addHuodongTeamWarAttackPercent(wp.WGroup, 3);
            }
            //发送进度
            sendHuodongPercent(wp.WGroup);
        }

        //玩家死亡
        //玩家被杀死
        public override void PlayerDie(PlayerObject player)
        {
            if(player==null|| player.LastHitter==null|| player.LastHitter.Race!= ObjectType.Player)
            {
                return;
            }

            //
            GroupWarPlayer diep = GroupWarPlayer.getHuodongPlayer(player.Info.Index);
            if (diep != null)
            {
                diep.dietime++;
            }

            PlayerObject hitter = (PlayerObject)player.LastHitter;
            GroupWarPlayer gp = GroupWarPlayer.getHuodongPlayer(hitter.Info.Index);
            if (gp != null)
            {
                hitter.ReceiveChat($"你杀死一个敌人获得相应能力解封", ChatType.System2);
                gp.killuser++;
                if(gp.WGroup== WarGroup.GroupA)
                {
                    GroupWarPlayer.GroupAkill++;
                }
                if (gp.WGroup == WarGroup.GroupB)
                {
                    GroupWarPlayer.GroupBkill++;
                }
                sendHuodongPercent(WarGroup.None);
            }
        }


        //发送活动进度
        private void sendHuodongPercent(WarGroup wgroup)
        {
            foreach (var player in Envir.Players)
            {
                if(player==null|| player.Dead)
                {
                    continue;
                }
                GroupWarPlayer warp = GroupWarPlayer.getHuodongPlayer(player.Info.Index);
                if (warp == null|| (warp.WGroup != wgroup && wgroup!= WarGroup.None))
                {
                    continue;
                }
                player.ReceiveChat($"当前解封能力:{warp.WarAttackPercent}%,杀数:{warp.killuser},比分：黄队({GroupWarPlayer.GroupAkill})蓝队({GroupWarPlayer.GroupBkill})", ChatType.Announcement);
            }
        }

        //战役玩家报名
        public static string signUp(PlayerObject p ,byte warType,byte costType)
        {
            DateTime now = Envir.Now;
            int day = now.DayOfYear;
            if (!Settings.openGroupWar)
            {
                return $"封神战役未开放，不接受报名";
            }
            //等级要求不够，不能报名
            if (p.Level < 35 || p.Level < Envir.MaxLevel - 10)
            {
                return $"您的等级过低，先去练练再来吧";
            }
            GroupWarPlayer gt = new GroupWarPlayer();
            gt.gid = p.Info.Index;
            gt.cday = day;
            gt.warType = warType;
            gt.costType = costType;

            //已经报名的，直接返回
            if (GroupWarPlayer.hasBaoming(gt))
            {
                return "之前已报名，无需重复报名";
            }

            //时间段不对的，直接返回
            if (now.Hour != warHour)
            {
                return $"当前时间不接受报名";
            }
            if (fb_step!=2)
            {
                return $"当前时间不接受报名";
            }
            //时间和战役类型不匹配的，不能报名
            //双号，只允许军团战役，单号只允许匹配战役
            if (now.Day % 2 == 0)
            {
                
                if (warType!=2)
                {
                    return "当前日期只接受军团战役报名";
                }
            }
            else
            {
                if (warType != 1 )
                {
                    return "当前日期只接受匹配战役报名";
                }
            }

            switch (warType)
            {
                case 0:

                    return "报名失败，请稍候再试";
                case 1:
                    gt.gid = (ulong)p.Info.Index;
                    break;
                case 2:
                    //检测是否有公会
                    if (p.MyGuild == null)
                    {
                        return "报名失败，必须加入公会才能报名此战役";
                    }
                    gt.gid = p.Info.Index;
                    break;
                default:
                    return "报名失败，请稍候再试";
            }
            GroupWarPlayer.addBaoming(gt);
            _warType = warType;
            return "报名成功，请保持身上金币/元宝充足，否则视为弃权";
        }

        /// <summary>
        /// 取消报名
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static string cancelSign(PlayerObject p)
        {
            DateTime now = Envir.Now;
            int day = now.DayOfYear;
            GroupWarPlayer gt = new GroupWarPlayer();
            gt.gid = p.Info.Index;
            gt.cday = day;
            //已经报名的，直接返回
            if (!GroupWarPlayer.hasBaoming(gt))
            {
                return "你还没有报名";
            }
            GroupWarPlayer.cancelBaoming(gt);
            return "已取消报名";
        }

        /// <summary>
        /// 战役认输，投降
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static string WarSurrender(PlayerObject p)
        {
            DateTime now = Envir.Now;
            int day = now.DayOfYear;

            GroupWarPlayer gw = GroupWarPlayer.getHuodongPlayer(p.Info.Index);
            gw.WinState = 2;
            //GroupWarPlayer.delHuodongPlayer(p.Info.Index);
            Map bqimap = Envir.GetMapByNameAndInstance("0");
            p.ChangeNameColour = Color.White;
            p.WarAttackPercent = 100;
            p.WGroup = WarGroup.None;
            p.Teleport(bqimap, safeBiqi);
            return "你在封神战役中认输，将失去参战资格";
        }
        

    }






    /// <summary>
    /// 战役成员，AB队伍成员
    /// </summary>
    public class GroupWarPlayer
    {
        //活动报名列表
        private static List<GroupWarPlayer> baoming = new List<GroupWarPlayer>();
        //活动参赛列表
        private static List<GroupWarPlayer> huodong = new List<GroupWarPlayer>();

        //AB队的累计杀敌数
        public static int GroupAkill = 0;
        public static int GroupBkill = 0;


        //添加报名
        public static void addBaoming(GroupWarPlayer gw)
        {
            if (!hasBaoming(gw))
            {
                baoming.Add(gw);
            }
        }

        //取消报名
        public static void cancelBaoming(GroupWarPlayer gw)
        {
            foreach (GroupWarPlayer g in baoming)
            {
                if (g.cday == gw.cday && g.gid == gw.gid)
                {
                    baoming.Remove(g);
                    return;
                }
            }
        }

        //是否已存在报名
        public static bool hasBaoming(GroupWarPlayer gw)
        {
            foreach (GroupWarPlayer g in baoming)
            {
                if (g.cday == gw.cday && g.gid == gw.gid)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 清理报名玩家，除了某天之外的都清理掉哦
        /// </summary>
        /// <param name="day"></param>
        public static void clearBaoming(int day)
        {
            List<GroupWarPlayer> clearl = new List<GroupWarPlayer>();
            foreach (GroupWarPlayer t in baoming)
            {
                if (t.cday == day)
                {
                    clearl.Add(t);
                }
            }
            foreach (GroupWarPlayer t in clearl)
            {
                baoming.Remove(t);
            }
        }

        /// <summary>
        /// 获取报名列表，只取某天
        /// </summary>
        /// <returns></returns>
        public static List<GroupWarPlayer> getBaoming(int day)
        {
            List<GroupWarPlayer> l = new List<GroupWarPlayer>();
            foreach (GroupWarPlayer t in baoming)
            {
                if (t.cday == day)
                {
                    l.Add(t);
                }
            }
            return l;
        }
        /// <summary>
        /// 获取某个报名玩家
        /// </summary>
        /// <param name="gid"></param>
        /// <returns></returns>
        public static GroupWarPlayer getBaomingPlayer(ulong gid)
        {
            foreach (GroupWarPlayer t in baoming)
            {
                if (t.gid == gid)
                {
                    return t;
                }
            }
            return null;
        }

        /// <summary>
        /// 把某个报名玩家加入某个匹配队伍
        /// </summary>
        /// <param name="gid"></param>
        public static void addHuodongPlayer(WarGroup wg,ulong gid)
        {
            if(wg== WarGroup.None)
            {
                return;
            }
            //已经有的不加
            if (getHuodongPlayer(gid) != null)
            {
                return;
            }
            GroupWarPlayer wp = getBaomingPlayer(gid);
            if (wp != null)
            {
                wp.WGroup = wg;
                huodong.Add(wp);
            }
        }

        /// <summary>
        /// 添加活动的团队积分
        /// </summary>
        public static void addHuodongTeamWarAttackPercent(WarGroup wg,int score)
        {
            foreach (GroupWarPlayer t in huodong)
            {
                if (t.WGroup == wg)
                {
                    t.TeamWarAttackPercent += score;
                }
            }
        }

        /// <summary>
        /// 增加活动的时间累积积分
        /// </summary>
        /// <param name="wg"></param>
        /// <param name="score"></param>
        public static void addHuodongTimeWarAttackPercent(int score)
        {
            foreach (GroupWarPlayer t in huodong)
            {
                t.TimeWarAttackPercent += score;
            }
        }


        /// <summary>
        /// 获取某个活动玩家
        /// </summary>
        /// <param name="gid"></param>
        public static GroupWarPlayer getHuodongPlayer(ulong gid)
        {
            foreach (GroupWarPlayer t in huodong)
            {
                if (t.gid == gid)
                {
                    return t;
                }
            }
            return null;
        }
        
        public static int getHuodongPlayerCount()
        {
            return huodong.Count;
        }
        /// <summary>
        /// 删除活动玩家
        /// </summary>
        /// <param name="gid"></param>
        public static void delHuodongPlayer(ulong gid)
        {
            foreach (GroupWarPlayer t in huodong)
            {
                if (t.gid == gid)
                {
                    huodong.Remove(t);
                    return ;
                }
            }
        }

        /// <summary>
        /// 清除活动玩家
        /// </summary>
        public static void clearHuodongPlayer()
        {
            GroupAkill = 0;
            GroupBkill = 0;
            huodong.Clear();
        }

        //计算对赌情况，扣除对赌积分的哦
        //这个挺复杂的，要看双方队伍的情况
        public static void countBet()
        {
            if (huodong.Count == 0|| huodong.Count%2!=0)
            {
                return;
            }

            //这里划分A,B组，同时先重置部分报名积分，类型
            //1.计算A、B组的所有金币赌注，元宝赌注
            int GoldA = 0, GoldB = 0, CreditA = 0, CreditB = 0;
            foreach (GroupWarPlayer t in huodong)
            {
                if (t.costType == 1)
                {
                    t.Gold = t.getCostGold();
                    continue;
                }
                if (t.WGroup == WarGroup.GroupA)
                {
                    GoldA += t.getCostGold();
                    CreditA+= t.getCostCredit();
                }
                else
                {
                    GoldB += t.getCostGold();
                    CreditB += t.getCostCredit();
                }
            }
            //最大的累计赌注，取双方相对小的赌注
            int maxGold, maxCredit;
            if (GoldA > GoldB)
            {
                maxGold = GoldB;
            }
            else
            {
                maxGold = GoldA;
            }
            if(CreditA > CreditB)
            {
                maxCredit = CreditB;
            }
            else
            {
                maxCredit = CreditA;
            }
            //计算A,B组对赌的金币，元宝
            //重置累计金币
            GoldA = 0; GoldB = 0; CreditA = 0; CreditB = 0;
            foreach (GroupWarPlayer t in huodong)
            {
                if (t.costType == 1)
                {
                    t.Gold = t.getCostGold();
                    continue;
                }
                //本该扣除的金币,元宝
                int deGold = t.getCostGold();
                int deCredit = t.getCostCredit();
                if (t.WGroup == WarGroup.GroupA)
                {
                    if(deGold + GoldA> maxGold)
                    {
                        deGold = maxGold - GoldA;
                    }
                    if(deCredit + CreditA> maxCredit)
                    {
                        deCredit = maxCredit - CreditA;
                    }
                    GoldA += deGold;
                    CreditA += deCredit;
                }
                else
                {
                    if (deGold + GoldB > maxGold)
                    {
                        deGold = maxGold - GoldB;
                    }
                    if (deCredit + CreditB > maxCredit)
                    {
                        deCredit = maxCredit - CreditB;
                    }
                    GoldB += deGold;
                    CreditB += deCredit;
                }
                t.Gold = t.getCostGold();
                t.Credit = t.getCostCredit();
                t.deGold = deGold;
                t.deCredit = deCredit;
                //这里收取20%的手续费,不在这里收取，在赢了之后计算收取
                t.WinGold = deGold;
                t.WinCredit = deCredit;
            }
        }



        //结束游戏，计算玩家分数
        //计算玩家分数
        public static void countScore()
        {



        }

        /// <summary>
        /// 是否认输结束游戏
        /// </summary>
        /// <returns></returns>
        public static bool renshuEnd()
        {
            byte renshuA = 0, renshuB = 0;
            foreach (GroupWarPlayer t in huodong)
            {
                if (t.WinState == 2)
                {
                    if (t.WGroup == WarGroup.GroupA)
                    {
                        renshuA++;
                    }
                    else
                    {
                        renshuB++;
                    }
                }
            }
            //某个队伍超过半数（大于等于）认输，则结束
            if(renshuA*4>= huodong.Count)
            {
                return true;
            }
            if (renshuB * 4 >= huodong.Count)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 结束后计算
        /// 计算胜负
        /// </summary>
        /// <returns>1.A组胜利,2.B组胜利</returns>
        public static void countWin()
        {
            byte winteam = 0;
            //认输的玩家数
            byte renshuA=0, renshuB=0;
            foreach (GroupWarPlayer t in huodong)
            {
                if (t.WinState == 2)
                {
                    if(t.WGroup == WarGroup.GroupA)
                    {
                        renshuA++;
                    }
                    else
                    {
                        renshuB++;
                    }
                }
            }

            if(renshuA> renshuB)
            {
                winteam = 2;
            }

            if (renshuB > renshuA)
            {
                winteam = 1;
            }

            //1.杀敌数
            if (winteam == 0)
            {
                if (GroupAkill > GroupBkill)
                {
                    winteam = 1;
                }
                if (GroupAkill < GroupBkill)
                {
                    winteam = 2;
                }
            }
            //2.杀怪数,投注数
            if (winteam == 0)
            {
                int kmScoreA = 0, kmScoreB = 0;
                int GoldA = 0, GoldB = 0, CreditA = 0, CreditB = 0;
                foreach (GroupWarPlayer t in huodong)
                {
                    if (t.costType == 1)
                    {
                        t.Gold = t.getCostGold();
                        continue;
                    }
                    if (t.WGroup == WarGroup.GroupA)
                    {
                        GoldA += t.getCostGold();
                        CreditA += t.getCostCredit();
                        kmScoreA += t.killmon1 + t.killmon2 * 2 + t.killmon3 * 3;
                    }
                    else
                    {
                        GoldB += t.getCostGold();
                        CreditB += t.getCostCredit();
                        kmScoreB += t.killmon1 + t.killmon2 * 2 + t.killmon3 * 3;
                    }
                }
                if (kmScoreA > kmScoreB)
                {
                    winteam= 1;
                }
                else if (kmScoreA < kmScoreB)
                {
                    winteam = 2;
                }
                else if(CreditA > CreditB)
                {
                    winteam = 1;
                }
                else if (CreditA < CreditB)
                {
                    winteam = 2;
                }
                else if (GoldA > GoldB)
                {
                    winteam = 1;
                }
                else if (GoldA < GoldB)
                {
                    winteam = 2;
                }
            }
            //3.实在不行，就随机吧
            if (winteam == 0)
            {
                winteam = (byte)RandomUtils.Next(1, 3);
            }

            //A,B队，没认输的则胜利
            if (winteam == 1)
            {
                foreach (GroupWarPlayer t in huodong)
                {
                    if (t.WGroup == WarGroup.GroupA&& t.WinState==0)
                    {
                        t.WinState = 1;
                    }
                }
            }
            else
            {
                foreach (GroupWarPlayer t in huodong)
                {
                    if (t.WGroup == WarGroup.GroupB && t.WinState == 0)
                    {
                        t.WinState = 1;
                    }
                }
            }
            //未知的，全部判为输
            foreach (GroupWarPlayer t in huodong)
            {
                if (t.WinState == 0)
                {
                    t.WinState = 2;
                }
            }
        }


        //玩家角色id
        public ulong gid;

        //对赌成立的另外一组的玩家id
        public ulong facegid;

        //报名类型 1匹配   2军团
        public string name;


        //最后胜负关系 0：未知  1：胜利 2：失败
        public byte WinState;

        //战役编组
        public WarGroup WGroup;

        //编组序号
        public int widx;//

        //费用类型 1：10万金币报名 2：100万金币报名 3：1万元宝报名 4：10万元宝报名
        public byte costType;


        //报名金币
        public int Gold;//

        //报名元宝
        public int Credit;//

        //扣除金币（形成对赌才会扣除）
        public int deGold;//

        //扣除元宝（形成对赌才会扣除）
        public int deCredit;//

        //奖励金币
        public int WinGold;

        //奖励元宝
        public int WinCredit;


        //获得积分，输得要扣除积分，所以这里可能负数哦？
        public int score
        {
            set { }
            get {
                if (WinState == 1)
                {
                    return 100;
                }
                else
                {
                    return -80;
                }
            }
        }

        //杀死玩家数
        public int killuser;//

        //死亡次数
        public int dietime;//

        //杀死怪物数1
        public int killmon1;//

        //杀死怪物数2
        public int killmon2;//

        //杀死怪物数3
        public int killmon3;//

        //团队奖励层数，击杀BOSS类的，团队奖励的
        public int TeamWarAttackPercent=0;//

        //时间推移增加层数
        private int TimeWarAttackPercent = 0;//

        //基础解封层数
        private int baseWarAttackPercent
        {
            set { }
            get
            {
                switch (costType)
                {
                    case 1:
                        return 50;
                    case 2:
                        return 55;
                    case 3:
                        return 60;
                    case 4:
                        return 65;
                }
                return 50;
            }
        }
        //解封层数(封顶200)，就是2倍攻击
        public int WarAttackPercent
        {
            set { }
            get
            {
                int p = baseWarAttackPercent + TimeWarAttackPercent + TeamWarAttackPercent + killmon1 * 2 + killmon2 * 2 + killmon3 * 3 + killuser * 3;
                if (p > 200)
                {
                    p = 200;
                }
                return p;
            }
        }


        //赢的一方，赢得经验
        public int WinExp
        {
            set { }
            get
            {
                int winGoldExp = WinGold/2;
                int winCreditExp = WinCredit * 100;//
                if (winGoldExp > 100 * 10000)
                {
                    winGoldExp = 100 * 10000;
                }
                if(winCreditExp> 200 * 10000)
                {
                    winCreditExp = 200 * 10000;
                }

                int killExp = (killuser - dietime) * 5 * 10000;
                if (killExp < 0)
                {
                    killExp = 0;
                }
                if (killExp > 50 * 10000)
                {
                    killExp = 50 * 10000;
                }
                //小于10个玩家的，不算
                if (huodong.Count < 6)
                {
                    killExp = 0;
                }
                return winGoldExp+ winCreditExp+ killExp;
            }
        }


        //输的一方，赢得经验
        public int LoseExp
        {
            set { }
            get
            {
          
                int killExp = (killuser - dietime) * 5 * 10000;
                if (killExp < 0)
                {
                    killExp = 0;
                }
                if (killExp > 50 * 10000)
                {
                    killExp = 50 * 10000;
                }
                //小于10个玩家的，不算
                if (huodong.Count < 6)
                {
                    killExp = 0;
                }
                return killExp/2;
            }
        }



        //报名时间(报名日)
        public int cday;


        //报名类型 1匹配   2军团
        public byte warType;


        //名字颜色,根据分组绝对颜色
        public Color NameColour
        {
            set { }
            get {
                if(WGroup == WarGroup.GroupA)
                {
                    return Color.Yellow;
                }
                if (WGroup == WarGroup.GroupB)
                {
                    return Color.Blue;
                }
                return Color.White;
            }
        }



        public GroupWarPlayer(ulong gid, byte warType, byte costType)
        {
            this.gid = gid;
            this.warType = warType;
            this.costType = costType;
        }

        public GroupWarPlayer()
        {

        }

        public int getCostGold()
        {
            //费用类型 1：10万金币报名 2：100万金币报名 3：1万元宝报名 4：10万元宝报名
            switch (costType)
            {
                case 1:
                    return 10000 * 10;
                case 2:
                    return 10000 * 100;
            }
            return 0;
        }

        public int getCostCredit()
        {
            //费用类型 1：10万金币报名 2：100万金币报名 3：1万元宝报名 4：10万元宝报名
            switch (costType)
            {
                case 3:
                    return 10000 * 1;
                case 4:
                    return 10000 * 10;
            }
            return 0;
        }

        /// <summary>
        /// 检测是否够费用
        /// 包括金币，元宝
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool checkMoney(PlayerObject p)
        {
            //10万金币是基础，每次必扣的
            if (p.Account.Gold < 10000 * 10)
            {
                return false;
            }
            if(p.Account.Gold< getCostGold()+ 10000 * 10)
            {
                return false;
            }
            if (p.Account.Credit < getCostCredit())
            {
                return false;
            }
            return true;
        }

        

    }
}
