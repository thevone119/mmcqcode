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




        private Point p1=new Point(354,335), p2= new Point(309,301), p3 = new Point(330, 330);//怪物攻城的2个集合点 ，从2个方向进攻土城

        private int moveEnd = 0;

        //所有产生的怪，都记录在这里，死亡的就删除掉
        private List<MonsterObject> allmon = new List<MonsterObject>();


        //最后处理时间
        private long lastPtime;

        private int fb_step;//副本执行阶段,不同阶段，不一样的
        private int fb_level;//副本的层级
        private long fb_starttime;//副本的开始时间(针对当前地图)
        private int fb_playcount;//副本的玩家数


        //各种处理
        public override void Process(Map map)
        {
            //500毫秒处理一次哦
            if (lastPtime > Envir.Time)
            {
                return;
            }
            lastPtime = Envir.Time + 500;

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
                    p.ReceiveChat($"2分钟后怪物发起攻城，目标进攻地点盟重土城", ChatType.System);
                    p.ReceiveChat($"2分钟后怪物发起攻城，目标进攻地点盟重土城", ChatType.System);
                }
                fb_step++;
                lastPtime = Envir.Time + 1000*60*1;
                //3小时内，安全区无法自动治疗
                map.SafeZoneHealingTime = Envir.Time + 1000 * 60 * 60 * 3;
                return;
            }

            //刷怪处理
            if (fb_step == 1)
            {
                foreach (PlayerObject p in Envir.Players)
                {
                    p.ReceiveChat($"第{fb_level + 1}波怪物袭击盟重土城", ChatType.System);
                    p.ReceiveChat($"第{fb_level + 1}波怪物袭击盟重土城", ChatType.System);
                }
                RefreshFBMon(map, p1);
                RefreshFBMon(map, p2);
                fb_step++;
                lastPtime = Envir.Time + 1000  * 2;
                return;
            }

            //掉落宝物
            if (fb_step == 2)
            {
                drop(map);
                fb_step++;
                lastPtime = Envir.Time + 1000 * 3;
                return;
            }

            //怪物向指定地点移动
            if (fb_step == 3)
            {
                if (moveEnd < 15)
                {
                    foreach (MonsterObject m in allmon)
                    {
                        if (Functions.InRange(m.CurrentLocation, p3, 5))
                        {
                            continue;
                        }
                        m.MoveAndFly(p3);
                    }
                    moveEnd++;
                }
                else
                {
                    moveEnd = 0;
                    fb_step = 1;
                    fb_level++;
                    lastPtime = fb_starttime+ fb_level*1000*60*4;
                }
            }


            

            //结束
            if (fb_level > 5)
            {
                map.mapSProcess = null;
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
            DropInfo drop6 = DropInfo.FromLine("副本_金条", String.Format("1/1 金条 {0}-{1}", 1, 1));

            foreach (var player in Envir.Players)
            {
                player.ReceiveChat($"攻城怪物掉落大量宝物在 盟重土城", ChatType.System2);
            }
            foreach (ItemInfo ditem in drop1.DropItems())
            {
                Point p = getPoint(map, p3, 0, 20);
                UserItem item = ditem.CreateDropItem();
                if (item == null) continue;
     
                ItemObject ob = new ItemObject(map,item,p)
                {
                };
                ob.Drop(Settings.DropRange + 1);
            }

            foreach (ItemInfo ditem in drop2.DropItems())
            {
                Point p = getPoint(map, p3, 0, 20);
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
                    Point p = map.RandomValidPoint();
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
                    Point p = getPoint(map, p3, 0, 10);
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
                if (RandomUtils.Next(4) == 1)
                {
                    foreach (ItemInfo ditem in drop3.DropItems())
                    {
                        Point p = getPoint(map, p3, 5, 30);
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

                if (RandomUtils.Next(4) == 1)
                {
                    foreach (ItemInfo ditem in drop4.DropItems())
                    {
                        Point p = getPoint(map, p3, 0, 20);
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



        //副本地图刷怪
        //线程刷怪？
        private void RefreshFBMon(Map map,Point SP)
        {
            //刷新小怪，50个先锋小怪
            for (int i = 0; i < 60; i++)
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
                drop5.Chance = 1.0 / 150.0;
                _minfo.Drops.Add(drop5);

                _minfo.CoolEye = 10;
                _minfo.Name = _minfo.Name + "[先锋]";
                //1.设置小怪的血量
                _minfo.Level = (ushort)(_minfo.Level+ 10);
                _minfo.HP = (uint)(800 + fb_level * 100);//基础血量
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
                monster.SpawnNew(map, getPoint(map, SP,2,6));
                allmon.Add(monster);
                Thread.Sleep(10);
            }

            //10个远程怪
            for (int i = 0; i < 15; i++)
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
                
                drop5.Chance = 1.0 / 50.0;
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

                monster.SpawnNew(map, getPoint(map, SP, 2, 6));
                allmon.Add(monster);
                Thread.Sleep(10);
            }


            //刷1个BOSS
            for (int i = 0; i < 1; i++)
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
                drop6.Chance = 1.0 / 10.0;
                //_minfo.Drops.Add(drop5);
                _minfo.Drops.Add(drop6);


                _minfo.CoolEye = 80;
                _minfo.Name = _minfo.Name + "[统领]";
                //1.设置小怪的血量
                _minfo.Level = (ushort)(_minfo.Level + 10);
                _minfo.HP = (uint)(30000 + fb_level * 2000);//基础血量
                _minfo.MaxAC = (ushort)(70 + fb_level * 2);
                //3.小怪的攻击
                _minfo.MaxDC = (ushort)(140 + fb_level * 10);//基础攻击
                _minfo.MaxSC = (ushort)(140 + fb_level * 10);//基础攻击
                _minfo.MaxMC = (ushort)(140 + fb_level * 10);//基础攻击
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

                monster.SpawnNew(map, getPoint(map, SP, 0, 4));
                allmon.Add(monster);
            }
            //return list;
        }

        //副本怪物死亡，怪物死亡会调用这个方法
        public override void monDie(MonsterObject mon)
        {
            allmon.Remove(mon);
            //fb_killmoncount++;
        }




    }
}
