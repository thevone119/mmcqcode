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
    //怪物攻城战争处理
    //怪物攻城，攻占土城
    //1.通过命令开启攻城战役
    //2.攻城时间定为1-2个小时
    //3.分批怪物，一次刷很多怪物在土城外集合，分批进攻土城
    //4.每批怪物组成方阵对土城发起进攻，进入土城后攻占土城安全区，土城安全区停止加血，停止穿人，土城大刀消失，巡逻消失
    //5.分3-5批进攻，每批50-100个怪物
    //6.下批进攻，上上批直接消失掉
    //7.怪物攻城，可以加入怪物方，杀人爆装备--先不要搞这个
    //1：骷髅，沃玛大军，2.牛魔大军，3.封魔大军，4.魔龙大军，5.狐狸/月氏大军，6.业火大军
    //怪物攻城名称,先锋，射手，散兵，统领
    public class MonWar: MapSpecialProcess
    {
        //副本小怪1(比奇/沃玛)白怪
        public static string fbxiaoguiai = "魔龙破甲兵|狐狸战士|冰狱野人|地狱巨镰鬼|铁锤猫卫|石巨人";

        //副本的特效怪（黄怪）
        public static string fbtxguai = "魔龙射手|狐狸法师|冰狱战将|地狱双刃鬼|灵猫法师|神殿树人";

        //副本的特效怪（蓝怪）
        public static string fbboss = "破凰魔神|石魔兽|冰狱魔王|怨恶|灵猫将军|野兽王";




        private Point p1=new Point(354,335), p2= new Point(309,301), MiddlePoint = new Point(330, 330), MiddlePoint2 = new Point(320, 330);//怪物攻城的2个集合点 ，从2个方向进攻土城


        //最后处理时间
        private long lastPtime;

        private int fb_step;//副本执行阶段,不同阶段，不一样的
        private int fb_level;//副本的层级
        private long fb_starttime;//副本的开始时间(针对当前地图)
        private int fb_playcount;//副本的玩家数

        private int moncount = 0;//所有刷怪数量，刷1200个怪在土城吧。哈哈，每一波200个，3分钟刷
        private int mcount1, mcount2, mcount3;//3种不同怪物的数量
        private long fb_refreshmon;//刷怪开始时间



        //各种处理
        public override void Process(Map map)
        {
            //200毫秒处理一次哦
            if (lastPtime > Envir.Time)
            {
                return;
            }
            lastPtime = Envir.Time + 200;

            if (fb_starttime == 0)
            {
                fb_starttime = Envir.Time;
                fb_step = 0;
            }
            //检测玩家数，玩家属性更新
            fb_playcount = map.Players.Count;
            if (fb_playcount == 0)
            {
                return;
            }

            //1.发送全服公告
            if (fb_step == 0)
            {
                foreach(PlayerObject p in Envir.Players)
                {
                    p.ReceiveChat($"1分钟后怪物发起攻城，目标进攻地点盟重土城", ChatType.System);
                    p.ReceiveChat($"1分钟后怪物发起攻城，目标进攻地点盟重土城", ChatType.System);
                }
                fb_step++;
                lastPtime = Envir.Time + 1000*60*1;
                //3小时内，安全区无法自动治疗
                map.SafeZoneHealingTime = Envir.Time + 1000 * 60 * 60 * 3;
                return;
            }

            //刷怪处理(一直刷)
            if (fb_step == 1)
            {
                RefreshFBMon(map);
                return;
            }

            //结束
            if (fb_level > 5)
            {
                //2小时候结束
                if (Envir.Time > fb_starttime + 1000 * 60 * 60 * 2)
                {
                    map.mapSProcess = null;
                }
            }

        }

        //随机取某个点周围的点
        private Point getPoint(Map map, Point p,int mind,int maxd)
        {
            
            for (int i = 0; i < 20; i++)
            {
                int dx = RandomUtils.Next(mind, maxd);
                int dy = RandomUtils.Next(mind, maxd);
                Point dp=Point.Empty;
                switch (RandomUtils.Next(4))
                {
                    case 0:
                        dp = new Point(p.X + dx, p.Y + dy);
                        break;
                    case 1:
                        dp = new Point(p.X + dx, p.Y - dy);
                        break;
                    case 2:
                        dp = new Point(p.X - dx, p.Y + dy);
                        break;
                    case 3:
                        dp = new Point(p.X - dx, p.Y - dy);
                        break;
                }
                if (map.ValidPoint(dp))
                {
                    return dp;
                }
            }
            return p;
        }
        
        //随机返回其中一组怪物，地图
        private string getMonName(string line,int idx)
        {
            string[] mon = line.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
            if(idx< mon.Length)
            {
                return mon[idx];
            }
            return mon[RandomUtils.Next(mon.Length)];
        }



        private void drop(Map map)
        {
            //每次掉落4-8个宝玉在土城
            DropInfo drop1 = DropInfo.FromLine("副本_宝玉", String.Format("1/1 G_宝玉 {0}-{1}", 4, 10));

            DropInfo drop2 = DropInfo.FromLine("副本_祖玛", String.Format("1/1 G_祖玛首饰 {0}-{1}", 5, 10));

            DropInfo drop3 = DropInfo.FromLine("副本_金条", String.Format("1/1 金条 {0}-{1}", 1, 1));

            DropInfo drop4 = DropInfo.FromLine("副本_经验", String.Format("1/1 经验加成(50%)1 {0}-{1}", 1, 1));

            DropInfo drop5 = DropInfo.FromLine("副本_经验", String.Format("1/1 经验加成(100%)1 {0}-{1}", 1, 1));
            DropInfo drop6 = DropInfo.FromLine("副本_金砖", String.Format("1/1 金砖 {0}-{1}", 1, 1));

            foreach (var player in Envir.Players)
            {
                player.ReceiveChat($"攻城怪物掉落大量宝物在 盟重土城", ChatType.System2);
            }
            foreach (ItemInfo ditem in drop1.DropItems())
            {
                Point p = getPoint(map, MiddlePoint, 0, 20);
                UserItem item = ditem.CreateDropItem();
                if (item == null) continue;
     
                ItemObject ob = new ItemObject(map,item,p)
                {
                };
                ob.Drop(Settings.DropRange + 1);
            }

            foreach (ItemInfo ditem in drop2.DropItems())
            {
                Point p = getPoint(map, MiddlePoint, 0, 20);
                UserItem item = ditem.CreateDropItem();
                if (item == null) continue;

                ItemObject ob = new ItemObject(map, item, p)
                {
                };
                ob.Drop(Settings.DropRange + 1);
            }

            
            //最后一波，金砖，100%经验卷
            if (fb_level == 5)
            {
                foreach (ItemInfo ditem in drop5.DropItems())
                {
                    Point p = getPoint(map, MiddlePoint, 0, 10);
                    foreach (var player in Envir.Players)
                    {
                        player.ReceiveChat($"攻城怪物掉落【经验加成100%】在 盟重（{p.X}，{p.Y}）处", ChatType.System2);
                    }
                    UserItem item = ditem.CreateDropItem();
                    if (item == null) continue;

                    ItemObject ob = new ItemObject(map, item, p)
                    {
                    };
                    ob.Drop(Settings.DropRange + 1);
                }

                foreach (ItemInfo ditem in drop6.DropItems())
                {
                    Point p = getPoint(map, MiddlePoint, 0, 10);
                    foreach (var player in Envir.Players)
                    {
                        player.ReceiveChat($"攻城怪物掉落【金砖】在 盟重（{p.X}，{p.Y}）处", ChatType.System2);
                    }
                    UserItem item = ditem.CreateDropItem();
                    if (item == null) continue;

                    ItemObject ob = new ItemObject(map, item, p)
                    {
                    };
                    ob.Drop(Settings.DropRange + 1);
                }
            }
            else
            {
                if (RandomUtils.Next(2) == 1)
                {
                    foreach (ItemInfo ditem in drop3.DropItems())
                    {
                        Point p = getPoint(map, MiddlePoint, 5, 30);
                        foreach (var player in Envir.Players)
                        {
                            player.ReceiveChat($"攻城怪物掉落【金条】在 盟重（{p.X}，{p.Y}）处", ChatType.System2);
                        }
                        UserItem item = ditem.CreateDropItem();
                        if (item == null) continue;

                        ItemObject ob = new ItemObject(map, item, p)
                        {
                        };
                        ob.Drop(Settings.DropRange + 1);
                    }
                }
                else
                {
                    foreach (ItemInfo ditem in drop3.DropItems())
                    {
                        Point p = map.RandomValidPoint();
                        foreach (var player in Envir.Players)
                        {
                            player.ReceiveChat($"攻城怪物掉落【金条】在 盟重（{p.X}，{p.Y}）处", ChatType.System2);
                        }
                        UserItem item = ditem.CreateDropItem();
                        if (item == null) continue;
                        ItemObject ob = new ItemObject(map, item, p)
                        {
                        };
                        ob.Drop(Settings.DropRange + 1);
                    }
                }

                if (RandomUtils.Next(3) == 1)
                {
                    foreach (ItemInfo ditem in drop4.DropItems())
                    {
                        Point p = getPoint(map, MiddlePoint, 0, 20);
                        foreach (var player in Envir.Players)
                        {
                            player.ReceiveChat($"攻城怪物掉落【经验加成50%】在 盟重（{p.X}，{p.Y}）处", ChatType.System2);
                        }
                        UserItem item = ditem.CreateDropItem();
                        if (item == null) continue;

                        ItemObject ob = new ItemObject(map, item, p)
                        {
                        };
                        ob.Drop(Settings.DropRange + 1);
                    }
                }
                else
                {
                    foreach (ItemInfo ditem in drop4.DropItems())
                    {
                        Point p = map.RandomValidPoint();
                        foreach (var player in Envir.Players)
                        {
                            player.ReceiveChat($"攻城怪物掉落【经验加成50%】在 盟重（{p.X}，{p.Y}）处", ChatType.System2);
                        }
                        UserItem item = ditem.CreateDropItem();
                        if (item == null) continue;

                        ItemObject ob = new ItemObject(map, item, p)
                        {
                        };
                        ob.Drop(Settings.DropRange + 1);
                    }
                }
            }
        }


        /// <summary>
        /// 修改刷怪处理脚本
        /// 2秒刷一次,一次刷2个，相当于每秒刷1个了
        /// </summary>
        /// <param name="map"></param>
        private void RefreshFBMon(Map map)
        {
            if (Envir.Time<fb_refreshmon)
            {
                return;
            }
            //刷完6波怪，就结束了
            if (fb_level > 5)
            {
                fb_step++;
                return;
            }
            fb_refreshmon = Envir.Time + 4000;

            //第一波
            if(fb_level==0&& moncount == 0)
            {
                drop(map);
                foreach (PlayerObject p in Envir.Players)
                {
                    p.ReceiveChat($"第{fb_level + 1}波怪物袭击盟重土城", ChatType.System);
                    p.ReceiveChat($"第{fb_level + 1}波怪物袭击盟重土城", ChatType.System);
                }
            }

            //200个怪物一层，一共刷1200个怪物
            if (moncount > 200)
            {
                moncount = 0;
                fb_level++;
                //刷完6波怪，就结束了
                if (fb_level > 5)
                {
                    fb_step++;
                    return;
                }
                drop(map);
                foreach (PlayerObject p in Envir.Players)
                {
                    p.ReceiveChat($"第{fb_level + 1}波怪物袭击盟重土城", ChatType.System);
                    p.ReceiveChat($"第{fb_level + 1}波怪物袭击盟重土城", ChatType.System);
                }
            }

            //怪物全城随机刷，部分怪物步行到安全区
            int rd = RandomUtils.Next(100);
            int montype = 1;
            if (rd < 50)
            {
                montype = 1;
            }
            else if (rd < 80)
            {
                montype = 2;
            }
            else
            {
                montype = 3;
            }
            //第3种怪物
            if (montype == 3 && mcount3 > (fb_level + 1) * 2)
            {
                montype = 1;
            }
            //第1种怪物
            if (montype==1 && mcount1 > (fb_level+1)*150)
            {
                montype = 2;
            }
            //怪物刷新位置
            int mpoint = RandomUtils.Next(100);
            Point SP = Point.Empty;//怪物刷新位置
            //30几率刷在安全区
            //30几率刷在玩家身边
            //30几率大范围刷
            if (rd < 25)//30的几率刷在安全区
            {
                SP= MiddlePoint;
            }
            else if (rd < 40)
            {
                SP = MiddlePoint2;
            }
            else if (rd < 70)
            {
                foreach(PlayerObject p in map.Players)
                {
                    //如果玩家在安全区域，则刷在玩家身边
                    if(Functions.InRange(p.Back, MiddlePoint2, Globals.DataRange))
                    {
                        SP = p.Back;
                        break;
                    }
                }
            }
            else
            {
                SP = map.RandomValidPoint(MiddlePoint2.X, MiddlePoint2.Y,30);
            }
            //刷怪，每次刷2个，15几率怪物向安全区域移动
            switch (montype)
            {
                case 1:
                    mcount1 += 4;
                    moncount += 4;
                    RefreshFBMon1(map, SP, 4);
                    break;
                case 2:
                    mcount2 += 2;
                    moncount += 2;
                    RefreshFBMon2(map, SP, 2);
                    break;
                case 3:
                    mcount3 += 2;
                    moncount += 2;
                    RefreshFBMon3(map, SP, 2);
                    break;
            }





        }


        /// <summary>
        /// 刷小怪
        /// </summary>
        /// <param name="map"></param>
        /// <param name="SP"></param>
        private void RefreshFBMon1(Map map,Point SP,int count)
        {
            //刷新小怪，50个先锋小怪
            for (int i = 0; i < count; i++)
            {
                string mname = getMonName(fbxiaoguiai, fb_level);
                MonsterInfo mInfo = null;
                for (int j = 0; j < 5; j++)
                {
                    mInfo = Envir.GetMonsterInfo(mname);
                    if (mInfo != null)
                    {
                        break;
                    }
                }
                if (mInfo == null)
                {
                    continue;
                }
                MonsterInfo _minfo = mInfo.Clone();
                //加入经验爆率
                DropInfo drop5 = DropInfo.FromLine("副本_经验", String.Format("1/200 经验加成(50%)1"));
                drop5.Chance = 1.0 / 300.0;
                _minfo.Drops.Add(drop5);

                _minfo.CoolEye = 10;
                _minfo.Name = _minfo.Name + "[先锋]";
                //1.设置小怪的血量
                _minfo.Level = (ushort)(_minfo.Level+ 10);
                _minfo.HP = (uint)(1000 + fb_level * 100);//基础血量
                _minfo.MaxAC = (ushort)(30+ fb_level*2);
                //3.小怪的攻击
                _minfo.MaxDC = (ushort)(70 + fb_level * 5);//基础攻击
                _minfo.MaxSC = (ushort)(70 + fb_level * 5);//基础攻击
                _minfo.MaxMC = (ushort)(70 + fb_level * 5);//基础攻击
                //4.攻速
                int AttackSpeed = 1300 - (fb_level  * 100);
                if (AttackSpeed < 800)
                {
                    AttackSpeed = 800;
                }
                _minfo.AttackSpeed = (ushort)AttackSpeed;
                //5.移速
                int MoveSpeed = 1400 - (fb_level  * 10);
                if (MoveSpeed < 800)
                {
                    MoveSpeed = 800;
                }
                if (MoveSpeed > 2500)
                {
                    MoveSpeed = 2500;
                }
                _minfo.MoveSpeed = (ushort)MoveSpeed;
                _minfo.ViewRange = (byte)(_minfo.ViewRange + 3);

                //防御，魔域一致
                _minfo.MaxMAC = _minfo.MaxAC;
                _minfo.MaxMC = _minfo.MaxDC;
                //最小值等于最大值除2
                _minfo.MinAC = (ushort)(_minfo.MaxAC / 2);
                _minfo.MinMAC = (ushort)(_minfo.MaxMAC / 2);
                _minfo.MinMC = (ushort)(_minfo.MaxMC / 2);
                _minfo.MinDC = (ushort)(_minfo.MaxDC / 2);

                MonsterObject monster = MonsterObject.GetMonster(_minfo);
                if (monster == null) continue;
                monster.IsCopy = true;
                monster.RefreshAll();
                if (RandomUtils.Next(100) < 15)
                {
                    List<RouteInfo> r = new List<RouteInfo>();
                    r.Add(new RouteInfo(map.RandomValidPoint(MiddlePoint2.X, MiddlePoint2.Y, 10), 2000));
                    monster.Route = r;
                }
                monster.SpawnNew(map, getPoint(map, SP,0,6));

            }
        }


        private void RefreshFBMon2(Map map, Point SP, int count)
        {
            for (int i = 0; i < count; i++)
            {
                string mname = getMonName(fbtxguai, fb_level);
                MonsterInfo mInfo = null;
                for (int j = 0; j < 5; j++)
                {
                    mInfo = Envir.GetMonsterInfo(mname);
                    if (mInfo != null)
                    {
                        break;
                    }
                }
                if (mInfo == null)
                {
                    continue;
                }
                MonsterInfo _minfo = mInfo.Clone();

                //加入经验爆率
                DropInfo drop5 = DropInfo.FromLine("副本_经验", String.Format("1/40 经验加成(50%)1"));

                drop5.Chance = 1.0 / 120.0;
                _minfo.Drops.Add(drop5);

                _minfo.CoolEye = 20;
                _minfo.Name = _minfo.Name + "[射手]";
                //1.设置小怪的血量
                _minfo.Level = (ushort)(_minfo.Level + 10);
                _minfo.HP = (uint)(2000 + fb_level * 200);//基础血量
                _minfo.MaxAC = (ushort)(35 + fb_level * 2);
                //3.小怪的攻击
                _minfo.MaxDC = (ushort)(85 + fb_level * 5);//基础攻击
                _minfo.MaxSC = (ushort)(85 + fb_level * 5);//基础攻击
                _minfo.MaxMC = (ushort)(85 + fb_level * 5);//基础攻击
                //4.攻速
                int AttackSpeed = 1300 - (fb_level * 100);
                if (AttackSpeed < 800)
                {
                    AttackSpeed = 800;
                }
                _minfo.AttackSpeed = (ushort)AttackSpeed;
                //5.移速
                int MoveSpeed = 1400 - (fb_level * 10);
                if (MoveSpeed < 800)
                {
                    MoveSpeed = 800;
                }
                if (MoveSpeed > 2500)
                {
                    MoveSpeed = 2500;
                }
                _minfo.MoveSpeed = (ushort)MoveSpeed;
                _minfo.ViewRange = (byte)(_minfo.ViewRange + 3);

                //防御，魔域一致
                _minfo.MaxMAC = _minfo.MaxAC;
                _minfo.MaxMC = _minfo.MaxDC;
                //最小值等于最大值除2
                _minfo.MinAC = (ushort)(_minfo.MaxAC / 2);
                _minfo.MinMAC = (ushort)(_minfo.MaxMAC / 2);
                _minfo.MinMC = (ushort)(_minfo.MaxMC / 2);
                _minfo.MinDC = (ushort)(_minfo.MaxDC / 2);

                MonsterObject monster = MonsterObject.GetMonster(_minfo);
                if (monster == null) continue;
                monster.IsCopy = true;
                monster.NameColour = Color.DeepSkyBlue;
                monster.ChangeNameColour = Color.DeepSkyBlue;
                monster.RefreshAll();
                if (RandomUtils.Next(100) < 15)
                {
                    List<RouteInfo> r = new List<RouteInfo>();
                    r.Add(new RouteInfo(map.RandomValidPoint(MiddlePoint2.X, MiddlePoint2.Y, 10), 2000));
                    monster.Route = r;
                }
                monster.SpawnNew(map, getPoint(map, SP, 0, 6));
            }
        }


        private void RefreshFBMon3(Map map, Point SP, int count)
        {
            //刷1个BOSS
            for (int i = 0; i < count; i++)
            {
                string mname = getMonName(fbboss, fb_level);
                MonsterInfo mInfo = null;
                for (int j = 0; j < 5; j++)
                {
                    mInfo = Envir.GetMonsterInfo(mname);
                    if (mInfo != null)
                    {
                        break;
                    }
                }
                if (mInfo == null)
                {
                    continue;
                }
                MonsterInfo _minfo = mInfo.Clone();
                //加入经验爆率
                DropInfo drop6 = DropInfo.FromLine("副本_经验", String.Format("1/10 经验加成(100%)1"));

                //drop5.Chance = 1 / 6;
                drop6.Chance = 1.0 / 12.0;
                //_minfo.Drops.Add(drop5);
                _minfo.Drops.Add(drop6);


                _minfo.CoolEye = 80;
                _minfo.Name = _minfo.Name + "[统领]";
                //1.设置小怪的血量
                _minfo.Level = (ushort)(_minfo.Level + 10);
                _minfo.HP = (uint)(30000 + fb_level * 3000);//基础血量
                _minfo.MaxAC = (ushort)(70 + fb_level * 5);
                //3.小怪的攻击
                _minfo.MaxDC = (ushort)(130 + fb_level * 10);//基础攻击
                _minfo.MaxSC = (ushort)(130 + fb_level * 10);//基础攻击
                _minfo.MaxMC = (ushort)(130 + fb_level * 10);//基础攻击
                //4.攻速
                int AttackSpeed = 1300 - (fb_level * 100);
                if (AttackSpeed < 800)
                {
                    AttackSpeed = 800;
                }
                _minfo.AttackSpeed = (ushort)AttackSpeed;
                //5.移速
                int MoveSpeed = 1400 - (fb_level * 10);
                if (MoveSpeed < 800)
                {
                    MoveSpeed = 800;
                }
                if (MoveSpeed > 2500)
                {
                    MoveSpeed = 2500;
                }
                _minfo.MoveSpeed = (ushort)MoveSpeed;
                _minfo.ViewRange = (byte)(_minfo.ViewRange + 3);

                //防御，魔域一致
                _minfo.MaxMAC = _minfo.MaxAC;
                _minfo.MaxMC = _minfo.MaxDC;
                //最小值等于最大值除2
                _minfo.MinAC = (ushort)(_minfo.MaxAC / 2);
                _minfo.MinMAC = (ushort)(_minfo.MaxMAC / 2);
                _minfo.MinMC = (ushort)(_minfo.MaxMC / 2);
                _minfo.MinDC = (ushort)(_minfo.MaxDC / 2);

                MonsterObject monster = MonsterObject.GetMonster(_minfo);
                if (monster == null) continue;
                monster.IsCopy = true;
                monster.NameColour = Color.Red;
                monster.ChangeNameColour = Color.Red;
                monster.RefreshAll();
                if (RandomUtils.Next(100) < 35)
                {
                    List<RouteInfo> r = new List<RouteInfo>();
                    r.Add(new RouteInfo(map.RandomValidPoint(MiddlePoint2.X, MiddlePoint2.Y, 10), 2000));
                    monster.Route = r;
                }
                monster.SpawnNew(map, getPoint(map, SP, 0, 6));
            }
        }
        //副本怪物死亡，怪物死亡会调用这个方法
        public override void monDie(MonsterObject mon)
        {
            //allmon.Remove(mon);
            //fb_killmoncount++;

            //部分经验卷等在这里爆
            if (mon == null || mon.EXPOwner == null)
            {
                return;
            }
            if (mon.Name.Contains("[先锋]"))
            {
                if (RandomUtils.Next(mcount1) >= 2)
                {
                    return;
                }
            }
            if (mon.Name.Contains("[射手]"))
            {
                if (RandomUtils.Next(mcount2) >= 2)
                {
                    return;
                }
            }
            if (mon.Name.Contains("[统领]"))
            {
                if (RandomUtils.Next(mcount3) >= 2)
                {
                    return;
                }
            }
            DropInfo drop3 = DropInfo.FromLine("副本_金条", String.Format("1/1 金条 {0}-{1}", 1, 1));
            if (RandomUtils.Next(100) < 55)
            {
                drop3 = DropInfo.FromLine("副本_经验", String.Format("1/1 经验加成(50%)1 {0}-{1}", 1, 1));
            }


            foreach (ItemInfo ditem in drop3.DropItems())
            {
                Point p = getPoint(mon.CurrentMap, MiddlePoint2, 0, 10);
                foreach (var player in Envir.Players)
                {
                    player.ReceiveChat($"攻城怪物掉落【经验加成50%】在 盟重（{p.X}，{p.Y}）处", ChatType.System2);
                }
                UserItem item = ditem.CreateDropItem();
                if (item == null) continue;

                ItemObject ob = new ItemObject(mon.CurrentMap, item, p)
                {
                };
                ob.Drop(Settings.DropRange + 1);
            }
        }




    }
}
