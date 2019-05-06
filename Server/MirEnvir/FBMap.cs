using Server.MirDatabase;
using Server.MirObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using S = ServerPackets;

namespace Server.MirEnvir
{
    //副本地图处理
    //轮回地狱副本
    public class FBMap : MapSpecialProcess
    {
        private static Dictionary<int, FBMap> fgdic = new Dictionary<int, FBMap>();

        private static byte Level_increase = 8;//每层增长的难度5%-10%
        private static byte Max_Level = 18;//副本最高层级

        //副本小怪1(比奇/沃玛)白怪
        public static string fbxiaoguiai = "红蛇|虎蛇|骷髅|骷髅战士|钳虫|蜈蚣|黑色恶蛆|红野猪|黑野猪|蓝蝰蛇|黄蝰蛇|祖玛卫士|大老鼠|祖玛雕像|狐狸战士|悲月刺蛙|悲月虎虫|悲月紫蛙|邪眼蛛蛙|邪眼蓝蛙|邪眼灰蛙|沃玛战士|沃玛勇士|沃玛战将|火焰沃玛|天狼蜘蛛|暴牙蜘蛛|黑颚蜘蛛|花吻蜘蛛|血巨人|邪恶巨人|牛头魔|牛魔斗士|牛魔侍卫|魔龙刀兵|魔龙破甲兵|魔龙巨蛾|魔龙力士";
        //副本的特效怪（蓝怪）
        public static string fbtxguai = "雷电僵尸|月魔蜘蛛|魔龙射手|地狱炮兵|地狱长矛鬼|地狱魔焰鬼|地狱双刃鬼";
        
        //副本的BOSS怪（红怪）
        public static string fbboss = "骷髅教主|白野猪|邪恶钳虫|邪恶毒蛇|祖玛赤雷|幻影寒虎|白灵蛇|狂热血蜥蜴|沃玛教主";

        //副本的BOSS怪（红怪）
        public static string fbboss2 = "虹魔教主_封魔|黄泉教主|牛魔王|破凰魔神|怨恶|地狱将军|冰狱魔王|石魔兽";

        //副本地图
        public static string fbmap = "FB_RD_1|FB_RD_2|FB_RD_3|FB_RD_4|FB_RD_5|FB_RD_6|FB_RD_7|FB_RD_8|FB_RD_9|FB_RD_10|FB_RD_11|FB_RD_12";



        //副本中，根据人物属性，计算怪物属性，这些是基础属性
        public ushort HP, MaxAC, MaxMAC, MaxDC,level;

        private long fb_starttime;//副本的开始时间(针对当前地图)
        private long fb_actime;//副本的处理时间，300毫秒处理一次
        private int fb_step;//副本执行阶段，步骤(针对当前地图)

        private string Title ="轮回地狱";//副本地图名称

        public int play_level;//玩家的等级，最高级的玩家

        public int fb_playcount;//副本的玩家数
        public int fb_level;//副本的层级
        public int fb_score;//累计得分
        public int fb_usertime;//累计用时
        public byte fb_type;//副本类型,轮回地狱副本（0）
        public int fb_id;//副本ID,每次进入创建副本ID
        public byte fb_mapsize = 0;//0:小地图 1:中等地图 2：大地图
        public short fb_moncount = 0;//当前副本的刷怪数
        public short fb_killmoncount = 0;//当前副本已杀怪数
        private List<ItemInfo> userItems = new List<ItemInfo>();
        //private int dorpItemCount = 0;//已爆装备数，爆过了就降低下爆率
        private byte play_type;//玩法 0：单人闯关 1：组队闯关   2:单人元宝  3：组队元宝


        //关卡类型 0:普通关卡 1：持续掉血（绿毒） 2：天灾（雷击）
        public bool has_poison;
        private long Poison_time;//上毒时间（10上一次毒）

        public bool has_Lightning;//是否有雷电
        private long Lightning_time;//雷电时间（3-15秒一次3个雷电）
        private Queue<SpellObject> LightningQueue = new Queue<SpellObject>();

        private bool hasnpc = false;//是否刷新了NPC补给

        private FBMap(int fb_id)
        {
            this.fb_id = fb_id;
            Title = "轮回" + (fb_level + 1)+"重天";
        }



        //返回一个单列
        public static FBMap getInstance(int fb_id)
        {
           
            if (!fgdic.ContainsKey(fb_id))
            {
                FBMap fb = new FBMap(fb_id);
                fgdic[fb_id]=fb;
            }
            return fgdic[fb_id];
        }

        //随机返回其中一组怪物，地图
        private string getRandom(string line)
        {
            string[] mon = line.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
            return mon[RandomUtils.Next(mon.Length)];
        }

        //副本怪物死亡，怪物死亡会调用这个方法
        public override void monDie(MonsterObject mon)
        {
            fb_killmoncount++;
        }


        //副本地图刷怪
        private void RefreshFBMon(Map map)
        {
            //定位一个相对较远的刷新点，所有怪都在一个点上刷新
            if (map.Players.Count == 0)
            {
                return;
            }
            Point location = map.Players[0].CurrentLocation;
            int MaxDistance = 0;
            Point RefreshPoint= Point.Empty;//刷新点
            for (int i = 0; i < 10; i++)
            {
                Point tp =  map.RandomValidPoint();
                if(Functions.MaxDistance(location, tp)> MaxDistance)
                {
                    MaxDistance = Functions.MaxDistance(location, tp);
                    RefreshPoint = tp;
                }
            }
            

            //List<MonsterObject> list = new List<MonsterObject>();
            fb_mapsize = 0;
            fb_moncount = 0;
            fb_killmoncount = 0;
            //SMain.Enqueue("副本刷怪：MaxAC："+ MaxAC+ ",MaxDC:"+ MaxDC);
            int mcount1 = 6;
            int mcount2 = 1;//1组
            //int mcount3 = 1;
            //中等地图
            if (map.Width>50 )
            {
                fb_mapsize = 1;
                mcount1 = 8;
                if (RandomUtils.Next(10)<5)
                {
                    mcount2 = RandomUtils.Next(1,3);
                }
            }
            //较大地图
            if (map.Width > 70)
            {
                fb_mapsize = 2;
                mcount1 = 10;
                if (RandomUtils.Next(10) < 7)
                {
                    mcount2 = RandomUtils.Next(1, 3);
                }
            }
            //刷新小怪，每个玩家就是一种小怪
            for(int i=0;i< fb_playcount; i++)
            {
                string mname = getRandom(fbxiaoguiai);
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
                for(int c=0;c< mcount1; c++)
                {
                    MonsterInfo _minfo = mInfo.Clone();
                    _minfo.Name = "轮回" + _minfo.Name;
                    //1.设置小怪的血量
                    _minfo.Level = (ushort)(_minfo.Level + 10);
                    _minfo.HP = (uint)(level * 15);//基础血量
                    _minfo.HP = (ushort)(_minfo.HP + _minfo.HP * fb_level * Level_increase / 100);//成长血量
                    _minfo.MaxAC = (ushort)(level / 4 + fb_level);
                    //最大防御不能高于玩家攻击,否则打不动的
                    if (_minfo.MaxAC > MaxDC / 2)
                    {
                        _minfo.MaxAC = (ushort)(MaxDC / 2);
                    }
                    //3.小怪的攻击
                    _minfo.MaxDC = (ushort)(MaxDC * 2 / 3);//基础攻击
                    _minfo.MaxDC = (ushort)(_minfo.MaxDC + _minfo.MaxDC * fb_level * Level_increase / 100);//成长攻击
                    int Accuracy = _minfo.Accuracy + fb_level; ;//每级增加1点准确
                    if (Accuracy > 200)
                    {
                        _minfo.Accuracy = 200;
                    }
                    else
                    {
                        _minfo.Accuracy = (byte)Accuracy;
                    }
                    //限制怪物最高敏捷
                    if (_minfo.Agility > 40)
                    {
                        _minfo.Agility = 40;
                    }
                    //4.攻速
                    int AttackSpeed = 2000 - (fb_level * Level_increase * 10);
                    if (AttackSpeed < 800)
                    {
                        AttackSpeed = 800;
                    }
                    _minfo.AttackSpeed = (ushort)AttackSpeed;
                    //5.移速
                    int MoveSpeed = _minfo.MoveSpeed - (fb_level * Level_increase * 10);
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
                    monster.SpawnNew(map, RefreshPoint);
                    fb_moncount++;
                }
            }

            //刷新蓝怪，蓝怪按组，3个一组
            mcount2 = mcount2 * fb_playcount;
            for (int i = 0; i < mcount2; i++)
            {

                string mname = getRandom(fbtxguai);
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
                for (int c = 0; c < 3; c++)
                {
                    MonsterInfo _minfo = mInfo.Clone();
                    _minfo.Name = "轮回" + _minfo.Name;
                    //1.设置小怪的血量
                    //_minfo.Level = 99;
                    _minfo.CanTame = false;
                    _minfo.ViewRange = (byte)(_minfo.ViewRange + 3);
                    _minfo.HP = (uint)(level * 50);//基础血量
                    _minfo.HP = (ushort)(_minfo.HP + _minfo.HP * fb_level * Level_increase / 100);//成长血量
                    //2.小怪的防御（根据等级）
                    _minfo.MaxAC = (ushort)(level / 3 + fb_level);
                    //最大防御不能高于玩家攻击,否则打不动的
                    if (_minfo.MaxAC > MaxDC * 2 / 3)
                    {
                        _minfo.MaxAC = (ushort)(MaxDC * 2 / 3);
                    }
                    //3.小怪的攻击
                    _minfo.MaxDC = (ushort)(MaxDC * 12 / 10);//1.2倍的基础攻击
                    _minfo.MaxDC = (ushort)(_minfo.MaxDC + _minfo.MaxDC * fb_level * Level_increase / 100);//成长攻击
                    int Accuracy = _minfo.Accuracy + fb_level; ;//每级增加1点准确
                    if (Accuracy > 200)
                    {
                        _minfo.Accuracy = 200;
                    }
                    else
                    {
                        _minfo.Accuracy = (byte)Accuracy;
                    }
                    //限制怪物最高敏捷
                    if (_minfo.Agility > 40)
                    {
                        _minfo.Agility = 40;
                    }
                    //4.攻速
                    
                    int AttackSpeed = 2000 - (fb_level * Level_increase * 10);
                    if (AttackSpeed < 800)
                    {
                        AttackSpeed = 800;
                    }
                    _minfo.AttackSpeed = (ushort)AttackSpeed;
                    //5.移速
                    int MoveSpeed = _minfo.MoveSpeed - (fb_level * Level_increase * 10);
                    if (MoveSpeed < 800)
                    {
                        MoveSpeed = 800;
                    }
                    if (MoveSpeed > 2500)
                    {
                        MoveSpeed = 2500;
                    }
                    _minfo.MoveSpeed = (ushort)MoveSpeed;

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
                    monster.SpawnNew(map, RefreshPoint);
                    fb_moncount++;
                }
            }


            //刷新BOSS,每个玩家一个
            for (int i = 0; i < fb_playcount; i++)
            {
                string mname = getRandom(fbboss);
                if (fb_level >= 5)
                {
                    mname = getRandom(fbboss2);
                }
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
                for (int c = 0; c < 1; c++)
                {
                    MonsterInfo _minfo = mInfo.Clone();
                    _minfo.Name = "轮回" + _minfo.Name;
                    //1.设置小怪的血量
                    //_minfo.Level = 99;
                    _minfo.ViewRange = (byte)(_minfo.ViewRange + 3);
                    _minfo.CanTame = false;
                    _minfo.HP = (uint)(level * 300);//基础血量
                    _minfo.HP = (ushort)(_minfo.HP + _minfo.HP * fb_level * Level_increase / 100);//成长血量
                    //2.小怪的防御（根据等级）
                    _minfo.MaxAC = (ushort)(level /2);
                    _minfo.MaxAC = (ushort)(level / 2 + fb_level);//防御成长
                    //最大防御不能高于玩家攻击,否则打不动的
                    if (_minfo.MaxAC > MaxDC * 2 / 3)
                    {
                        _minfo.MaxAC = (ushort)(MaxDC * 2 / 3);
                    }
                    //3.小怪的攻击
                    _minfo.MaxDC = (ushort)(MaxDC * 14 / 10);//1.2倍的基础攻击
                    _minfo.MaxDC = (ushort)(_minfo.MaxDC + _minfo.MaxDC * fb_level * Level_increase / 100);//成长攻击
                    int Accuracy = _minfo.Accuracy + fb_level; ;//每级增加1点准确
                    if (Accuracy > 200)
                    {
                        _minfo.Accuracy = 200;
                    }
                    else
                    {
                        _minfo.Accuracy = (byte)Accuracy;
                    }
                    //限制怪物最高敏捷
                    if (_minfo.Agility > 40)
                    {
                        _minfo.Agility = 40;
                    }
                    //4.攻速
                    int AttackSpeed = 1700 - (fb_level * Level_increase * 10);
                    if (AttackSpeed < 500)
                    {
                        AttackSpeed = 500;
                    }
                    _minfo.AttackSpeed = (ushort)AttackSpeed;
                    //5.移速
                    int MoveSpeed = _minfo.MoveSpeed - (fb_level * Level_increase * 10);
                    if (MoveSpeed < 500)
                    {
                        MoveSpeed = 500;
                    }
                    if (MoveSpeed > 2500)
                    {
                        MoveSpeed = 2500;
                    }
                    _minfo.MoveSpeed = (ushort)MoveSpeed;
                    //monster.HealTime
                    
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
                    //默认是0.022f;降低恢复速度
                    monster.HealthScale = 0.001f;

                    monster.SpawnNew(map, RefreshPoint);
                    fb_moncount++;
                }
            }
            //return list;
        }

        //获取NPC的刷新点，装备也爆在这个NPC附近
        private Point getNPCPoint(Map map)
        {
            //先随机在
            int x = map.Players[0].CurrentLocation.X;
            int y = map.Players[0].CurrentLocation.Y;
            for (int w = 2; w < 5; w++)
            {
                for (int h = 2; h < 5; h++)
                {
                    if (map.EmptyPoint(x - w, y - h))
                    {
                        return new Point(x - w, y - h);
                    }
                    if (map.EmptyPoint(x + w, y - h))
                    {
                        return new Point(x + w, y - h);
                    }
                    if (map.EmptyPoint(x + w, y + h))
                    {
                        return new Point(x + w, y + h);
                    }
                    if (map.EmptyPoint(x - w, y + h))
                    {
                        return new Point(x - w, y + h);
                    }
                }
            }
            return map.RandomValidPoint();
        }

        //爆东西,刷新NPC
        private void DropFBMon(Map map)
        {
            //
            if (userItems.Count < 4)
            {
                if (play_level < 40)
                {
                    userItems.AddRange(ItemInfo.queryByGroupName("祖玛首饰"));
                }else if (play_level < 45)
                {
                    userItems.AddRange(ItemInfo.queryByGroupName("赤月首饰"));
                }
                else if (play_level < 50)
                {
                    userItems.AddRange(ItemInfo.queryByGroupName("狐狸首饰"));
                }
                else if (play_level < 55)
                {
                    userItems.AddRange(ItemInfo.queryByGroupName("魔神首饰"));
                }
                else
                {
                    userItems.AddRange(ItemInfo.queryByGroupName("首饰50"));
                }
            }
            hasnpc = false;
            string PlayerNames = "";
            //1.刷新一个零时的NPC(目前零时补给NPC还有些问题哟)
            Point point = getNPCPoint(map);
            
            //地图越大，越容易出现补给NPC
            if (RandomUtils.Next(2) == 0)
            {
                NPCInfo npc = Envir.GetNPCInfoByName("零时补给").Clone();
                npc.Location = point;
                NPCObject npco = new NPCObject(npc,false) { CurrentMap = map };
                npco.SpawnNew(map, point);
         
                hasnpc = true;
                //SMain.Enqueue("刷新NPC");
            }
            double DropRate = 1;
            DropRate = DropRate * (1 + map.Players[0].ItemDropRateOffset / 100.0f + Level_increase * fb_level*2 / 100.0f);
            //DropRate = DropRate + fb_mapsize * 0.05;
           // SMain.Enqueue("当前地狱层数："+ fb_level+",爆率倍数:"+ DropRate+",玩家经验倍率："+ map.Players[0].ItemDropRateOffset);
            //2.爆东西
            List<ItemInfo> dropItems = new List<ItemInfo>();
            List<UserItem> dropSAItems = new List<UserItem>();//这个是玩家轮回的装备

            foreach (PlayerObject p in map.Players)
            {
                if(p.Level> play_level)
                {
                    play_level = p.Level;
                }
                PlayerNames += p.Name + " ";
                double sadorpChange = 0;//轮回装备爆出的几率
                for(int i=0;i< p.Info.Equipment.Length; i++)
                {
                    if(p.Info.Equipment[i]== null|| p.Info.Equipment[i].Info==null || p.Info.Equipment[i].Info.Type> ItemType.Ring)
                    {
                        continue;
                    }
                    if (p.Info.SaItem != null &&p.Info.SaItemType==0)
                    {
                        if(p.Info.SaItem.Info.Index== p.Info.Equipment[i].Info.Index)
                        {
                            sadorpChange = 0.3f;//有相同的装备，则爆率是1/3
                        }
                        if (sadorpChange < 0.1)
                        {
                            sadorpChange = 0.15f;//没有相同的装备，则爆率是1/6
                        }
                    }
                }
                //爆出轮回装备,1/3的几率爆点
                if(RandomUtils.NextDouble() <= sadorpChange * DropRate)
                {
                    UserItem saitem = p.Info.SaItem.Clone();
                    p.Info.SaItem = null;
                    if (saitem.samsaratype== (byte)AwakeType.DC)
                    {
                        saitem.samsaracount++;
                        saitem.SA_DC++;
                        if (RandomUtils.Next(3) == 1)
                        {
                            saitem.samsaracount++;
                            saitem.SA_DC++;
                        }
                    }
                    if (saitem.samsaratype == (byte)AwakeType.MC)
                    {
                        saitem.samsaracount++;
                        saitem.SA_MC++;
                        if (RandomUtils.Next(3) == 1)
                        {
                            saitem.samsaracount++;
                            saitem.SA_MC++;
                        }
                    }
                    if (saitem.samsaratype == (byte)AwakeType.SC)
                    {
                        saitem.samsaracount++;
                        saitem.SA_SC++;
                        if (RandomUtils.Next(3) == 1)
                        {
                            saitem.samsaracount++;
                            saitem.SA_SC++;
                        }
                    }
                    if (saitem.samsaratype == (byte)AwakeType.AC)
                    {
                        saitem.samsaracount++;
                        saitem.SA_AC++;
                        if (RandomUtils.Next(4) == 1)
                        {
                            saitem.samsaracount++;
                            saitem.SA_AC++;
                        }
                    }
                    if (saitem.samsaratype == (byte)AwakeType.MAC)
                    {
                        saitem.samsaracount++;
                        saitem.SA_MAC++;
                        if (RandomUtils.Next(4) == 1)
                        {
                            saitem.samsaracount++;
                            saitem.SA_MAC++;
                        }
                    }
                    //直接放背包，背包放不下，则爆出来
                    if (p.CanGainItem(saitem, false))
                    {
                        p.GainItem(saitem);
                    }
                    else
                    {
                        dropSAItems.Add(saitem);
                    }
                    
                    //SMain.Enqueue($"恭喜[{p.Name}]闯入轮回，找回自己的装备 {saitem.FriendlyName}，在{Title}");
                    //如果物品需要通知，则发送通知
                    foreach (var player in Envir.Players)
                    {
                        player.ReceiveChat($"恭喜[{p.Name}]闯入轮回，找回自己的装备 {saitem.FriendlyName}，在{Title}", ChatType.System2);
                    }
                }
            }
            //这里使用当前的人数计算
            int pcount = map.Players.Count;
            //药水爆率
            int dropmaxcount = pcount + fb_level / 4;
            int dropmincount = pcount + fb_level / 7;
            DropInfo drop1 = DropInfo.FromLine("副本爆率1", String.Format("1/1 G_药剂3 {0}-{1}", pcount * 4+fb_level, pcount * 8 + fb_level ));
            drop1.isPrice = false;
            DropInfo drop2 = DropInfo.FromLine("副本_宝玉", String.Format("1/1 G_宝玉 {0}-{1}", dropmincount, dropmaxcount));
            drop2.isPrice = false;
            DropInfo drop3 = DropInfo.FromLine("副本_符纸", String.Format("1/2 G_轮回符纸 {0}-{1}", dropmincount, dropmaxcount));
            drop3.isPrice = false;
            DropInfo drop4 = DropInfo.FromLine("副本_身上的装备", String.Format("1/4 力量戒指 {0}-{1}", 1, 1));
            drop4.ItemList = userItems;
            if (play_level < 40)
            {
                drop4.Chance = 1.0 / 3.0;
            }
            else if (play_level < 45)
            {
                drop4.Chance = 1.0 / 4.0;
            }
            else if (play_level < 50)
            {
                drop4.Chance = 1.0 / 5.0;
            }
            else 
            {
                drop4.Chance = 1.0 / 6.0;
            }
            //
            if (play_type==2|| play_type == 3)
            {
                drop4.Chance = drop4.Chance * 1.2;
            }



            //必爆的补给品
            if (drop1.isDrop(DropRate))
            {
                dropItems.AddRange(drop1.DropItems());
            }
            if (drop2.isDrop(DropRate))
            {
                dropItems.AddRange(drop2.DropItems());
            }
            //符纸的话，如果已经开了第一关外，这里就不爆了
            if (drop3.isDrop(DropRate)& Envir.GetMapByNameAndInstance("HELL00") == null)
            {
                dropItems.AddRange(drop3.DropItems());//符纸放在地狱爆
            }
            if (drop4.isDrop(DropRate) && userItems.Count> 3)
            {
                if (RandomUtils.Next(Math.Max(20 - fb_level,2)) == 1)
                {
                    drop4.isPrice = false;
                    dropItems.AddRange(drop4.DropItems());
                }
                else
                {
                    drop4.isPrice = true;
                    dropItems.AddRange(drop4.DropItems());
                }
            }

            foreach (ItemInfo ditem in dropItems)
            {
                UserItem item = ditem.CreateDropItem();
                if (item == null) continue;
                ItemObject ob = new ItemObject(map.Players[0], item)
                {
                    Owner = map.Players[0],
                    OwnerTime = Envir.Time + Settings.Minute,
                };
                //如果物品需要通知，则发送通知
                if (item.Info.GlobalDropNotify)
                {
                    foreach (var player in Envir.Players)
                    {
                        player.ReceiveChat($"{PlayerNames} 轮回闯关成功，掉落 {item.FriendlyName}，在{Title}", ChatType.System2);
                    }
                }
                ob.Drop(Settings.DropRange+1);
            }
            //上面发了通知，这里就不发通知了
            foreach (UserItem item in dropSAItems)
            {
                if (item == null) continue;
                ItemObject ob = new ItemObject(map.Players[0], item)
                {
                    Owner = map.Players[0],
                    OwnerTime = Envir.Time + Settings.Minute,
                };
                ob.Drop(Settings.DropRange + 1);
            }
        }

        //传送到下一个关卡
        private void MoveNextFBMap(Map map)
        {
            string mname = getRandom(fbmap);
            fb_level++;
            fb_starttime = 0;
            fb_step = 0;
            Title = "轮回" + (fb_level + 1) + "重天";

            //下一关是否有毒
            int has_change = 10-fb_level;
            if (has_change < 1)
            {
                has_change = 1;
            }
            if (RandomUtils.Next(has_change) == 0)
            {
                has_poison = true;
            }
            else
            {
                has_poison = false;
            }
            //下一关是否有雷
            if (RandomUtils.Next(has_change) == 0)
            {
                has_Lightning = true;
            }
            else
            {
                has_Lightning = false;
            }
            Map nextmap = SMain.Envir.GetMapByNameCopy(mname, fb_id);
            map.mapSProcess = null;
            List<PlayerObject> list = new List<PlayerObject>();
            foreach (PlayerObject p in map.Players)
            {
                if (p.Dead)
                {
                    continue;
                }
                list.Add(p);
                //
            }

            foreach (PlayerObject p in list)
            {
                p.TeleportRandom(200, 0, nextmap);
            }
        }

        //检测玩家的次数，
        //groupytpe:1：单人闯关次数 1：组队闯关次数
        public static bool checkFBTime(PlayerObject p,byte play_type, bool update=false)
        {
            if (p.IsGM)
            {
                return true;
            }
            if (p == null && p.Info==null)
            {
                return false;
            }
            string countdaykey = "FB_DY_DAY"+ play_type;
            string countkey = "FB_DY_COUNT"+ play_type;
            //+Envir.Now.DayOfYear
            int count = p.Info.getSaveValue(countkey);
            int countday = p.Info.getSaveValue(countdaykey);
            if (countday != Envir.Now.DayOfYear)
            {
                count = 0;
                if (update)
                {
                    p.Info.putSaveValue(countdaykey, Envir.Now.DayOfYear);
                }
            }
            count++;
            if (count > 1)
            {
                return false;
            }

            if (update)
            {
                p.Info.putSaveValue(countkey, count);
            }
            return true;
        }

        //各种副本处理
        public override void Process(Map map)
        {
            //副本500毫秒处理一次哦
            if (fb_actime > Envir.Time)
            {
                return;
            }
            fb_actime = Envir.Time + 500;
            play_type = (byte)param1;
      
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
            if ((play_type == 1|| play_type==3) && fb_playcount == 1)
            {
                fb_playcount = 2;
            }
            ushort _MaxDC = 0, _level = 0;
            //
            foreach (PlayerObject p in map.Players)
            {
                if (p.MaxHP > HP)
                {
                    HP = p.MaxHP;
                }
                if (p.MaxAC > MaxAC)
                {
                    MaxAC = p.MaxAC;
                }
                if (p.MaxMAC > MaxMAC)
                {
                    MaxMAC = p.MaxMAC;
                }
                ushort _dc = 0;
                //计算攻击
                if (p.MaxDC > _dc)
                {
                    _dc = p.MaxDC;
                    ushort _dc2 = (ushort)(p.Level / 3);//针对物理攻击，每3级降1点攻击
                    if (_dc > _dc2)
                    {
                        _dc = (ushort)(_dc - _dc2);
                    }
                }
                if (p.MaxMC > _dc)
                {
                    _dc = p.MaxMC;
                }
                if (p.MaxSC > _dc)
                {
                    _dc = p.MaxSC;
                }
                if (p.Info.Class == MirClass.Warrior)
                {
                    _MaxDC += (ushort)(_dc / 8 + p.Level);
                }
                else
                {
                    _MaxDC += (ushort)(_dc / 5 + p.Level);
                }
                _level += p.Level;
                //记录用户的装备
                for (int i = 0; i < p.Info.Equipment.Length; i++)
                {
                    if (p.Info.Equipment[i] == null || p.Info.Equipment[i].Info == null || p.Info.Equipment[i].Info.Type > ItemType.Ring)
                    {
                        continue;
                    }
                    if (p.Info.Equipment[i].Info.Name.Contains("技巧项链"))
                    {
                        continue;
                    }
                    if (p.Info.Equipment[i].Info.GroupName != null && p.Info.Equipment[i].Info.GroupName.Contains("特殊首饰1"))
                    {
                        continue;
                    }
                    //神话，史诗2个级别的装备不入轮回
                    if (p.Info.Equipment[i].Info.Grade == ItemGrade.Mythical)
                    {
                        continue;
                    }
                    if (p.Info.Equipment[i].Info.Grade == ItemGrade.Ancient)
                    {
                        continue;
                    }
                    if (p.Info.Equipment[i].Info.Grade == ItemGrade.Epic)
                    {
                        continue;
                    }
                    bool has = false;
                    foreach (ItemInfo h in userItems)
                    {
                        if (h.Index == p.Info.Equipment[i].Info.Index)
                        {
                            has = true;
                            break;
                        }
                    }
                    if (!has)
                    {
                        userItems.Add(p.Info.Equipment[i].Info);
                    }
                }
            }
            if (MaxDC < (_MaxDC / fb_playcount))
            {
                MaxDC = (ushort)(_MaxDC / fb_playcount);
            }
            level = (ushort)(_level / fb_playcount);
            //最低的攻击要大于等级基数
            if (MaxDC < level)
            {
                MaxDC = (ushort)(level);
            }

            //为玩家上绿毒（10秒上一次）
            if (has_poison && fb_step == 3 && Poison_time < Envir.Time)
            {
                Poison_time = Envir.Time + RandomUtils.Next(22000, 27000);
                foreach (PlayerObject p in map.Players)
                {
                    p.ApplyPoison(new Poison
                    {
                        Duration = 10,
                        Owner = null,
                        PType = PoisonType.Green,
                        TickSpeed = 2000,
                        Value = (fb_level) * 2 + p.MaxHP / 60
                    }, null, true);
                }
            }
   
            //天灾，雷击（这个设计一下，可以躲避的）
            int lightcount = LightningQueue.Count;
            if (has_Lightning && lightcount > 0)
            {
                for (int i = 0; i < lightcount; i++)
                {
                    SpellObject Lightning = LightningQueue.Dequeue();
                    if (Lightning.StartTime> Envir.Time)
                    {
                        LightningQueue.Enqueue(Lightning);
                    }
                    else
                    {
                        Lightning.StartTime = Envir.Time-1;
                        Lightning.ExpireTime = Envir.Time + 600;
                        map.AddObject(Lightning);
                        Lightning.Spawned();
                    }
                }
            }

            if (has_Lightning && fb_step == 3 && Lightning_time < Envir.Time)
            {
                int maxtime = 30000 - fb_level * 1000;
                if (maxtime < 15000)
                {
                    maxtime = 15000;
                }
                Lightning_time = Envir.Time + RandomUtils.Next(10000, maxtime);
                foreach (PlayerObject p in map.Players)
                {
                    //多次伤害(3次伤害)
                    SpellObject Lightning = null;
                    Lightning = new SpellObject
                    {
                        Spell = Spell.MapLightning,
                        Value = p.MaxHP / 10 + fb_level * 2+ p.MaxHP / 100* fb_level,
                        ExpireTime = Envir.Time + (600),
                        TickSpeed = 500,
                        Caster = null,
                        CurrentLocation = p.CurrentLocation,
                        CurrentMap = map,
                        Direction = MirDirection.Up
                    };
                    map.AddObject(Lightning);
                    Lightning.Spawned();
                }
                //这里追加2次
                for (int i = 0; i < 2; i++)
                {
                    foreach (PlayerObject p in map.Players)
                    {
                        LightningQueue.Enqueue(new SpellObject
                        {
                            Spell = Spell.MapLightning,
                            Value = p.MaxHP / (8- i*2) + fb_level * 2 + p.MaxHP / 100 * fb_level,
                            StartTime = Envir.Time + (1200*(i+1)),
                            ExpireTime = Envir.Time + (600),
                            TickSpeed = 400,
                            Caster = null,
                            CurrentLocation = new Point(p.CurrentLocation.X, p.CurrentLocation.Y),
                            CurrentMap = map,
                            Direction = MirDirection.Up
                        });
                    }
                }
            }



            //1.校验处理,初始化参数处理
            if (fb_step == 0 && fb_level==0)
            {
                List<PlayerObject> leaveList = new List<PlayerObject>();
                //检测当前玩家，地狱副本闯关次数
                foreach (PlayerObject p in map.Players)
                {
                    if (!checkFBTime(p, play_type,true))
                    {
                        leaveList.Add(p);
                        p.ReceiveChat($"每个玩家每天只能参与1次此类的地狱闯关，您已超过闯关次数，请明天再来闯关", ChatType.System);
                        continue;
                    }
                    else
                    {
                        if(play_type==0|| play_type == 1)
                        {
                            if (p.Account.Gold > 50000)
                            {
                                p.LoseGold(50000);
                            }
                            else
                            {
                                leaveList.Add(p);
                            }
                        }
                        else
                        {
                            if (p.Account.Credit > 3000)
                            {
                                p.LoseCredit(3000);
                            }
                            else
                            {
                                leaveList.Add(p);
                            }
                        }
                    }
                }
                foreach (PlayerObject p in leaveList)
                {
                    p.TeleportEscape(100);
                }
                fb_step++;
                return;
            }

            if (fb_step == 0)
            {
                fb_step++;
                return;
            }

            //2.发送信息
            if (fb_step == 1)
            {
                //地图天灾的间隔，等级越高，天灾越频繁
                map.FireInterval = map.FireInterval - fb_level * 1000;
                if (map.FireInterval < 3000)
                {
                    map.FireInterval = 3000;
                }
                double DropRate = 1;
                DropRate = DropRate * (1 + map.Players[0].ItemDropRateOffset / 100.0f + Level_increase * fb_level * 2 / 100.0f);
                if (fb_level == 0)
                {
                    foreach (PlayerObject p in map.Players)
                    {
                        p.ReceiveChat(String.Format("欢迎进入轮回空间，5秒钟后开始闯关，当前关卡爆率{0:F},请准备...", DropRate), ChatType.Announcement);
                    }
                }
                else
                {
                   
                    foreach (PlayerObject p in map.Players)
                    {
                        p.ReceiveChat(String.Format("5秒钟后开始闯关，当前关卡爆率{0:F},请准备...", DropRate), ChatType.Announcement);
                    }
                }
                fb_actime = Envir.Time + 5000;
                fb_step++;
                return;
            }

            //3.刷怪处理
            if (fb_step == 2)
            {
                RefreshFBMon(map);
                fb_step++;
                fb_actime = Envir.Time + 5000;//5秒后再检测了
                return;
            }

            //检测怪物是否已全部被杀死
            if (fb_step == 3)
            {
                if (fb_killmoncount>=fb_moncount)
                {
                    //爆东西
                    DropFBMon(map);
                    //计算积分(通过分数70分，时间分数30分，每分钟减1分，每分钟减1分)
                    int score = 70;//通过分70
                    long usetime = (Envir.Time - fb_starttime - 6000);//用时
                    fb_usertime += (int)usetime;
                    long usetminute = usetime / Settings.Minute;//分钟
                    long useSecond = (usetime - usetminute* Settings.Minute)/ Settings.Second;
                    int time_score = (int)(30 - usetminute);
                    if (time_score < 0)
                    {
                        time_score = 0;
                    }
                    score += time_score;
                    fb_score += score;
                    //记录玩家积分
                
                    foreach (PlayerObject p in map.Players)
                    {
                        if(p.Info.getFb1_score() < fb_score)
                        {
                            p.Info.setFb1_score(fb_score);
                        }
                        p.ReceiveChat($"闯关成功，当前层数{fb_level + 1},用时{usetminute}分{useSecond}秒，得分{score}，累计分{fb_score},历史最高{p.Info.getFb1_score()}分，您的积分将记录于地榜中...", ChatType.System2);
                    }
                    //发送消息
                    if (fb_level+1 >= Max_Level)
                    {
                        foreach (PlayerObject p in map.Players)
                        {
                            p.ReceiveChat("您已闯关最大的关卡，无法继续闯关，收拾收拾自己回去吧", ChatType.Announcement);
                        }
                    }
                    else
                    {
                        foreach (PlayerObject p in map.Players)
                        {
                            if (hasnpc)
                            {
                                p.ReceiveChat($"闯关成功，40秒后自动进入第{fb_level + 2}关，请做好准备", ChatType.Announcement);
                            }
                            else
                            {
                                p.ReceiveChat($"闯关成功，20秒后自动进入第{fb_level + 2}关，请做好准备", ChatType.Announcement);
                            }
                        }
                    }
                    
                    fb_step++;
                    if (hasnpc)
                    {
                        fb_actime = Envir.Time + 40000;//60秒后传送到下一关
                    }
                    else
                    {
                        fb_actime = Envir.Time + 20000;//20秒后传送到下一关
                    }
                    
                    return;
                }
            }

            //传送到下一个地图
            if (fb_step == 4)
            {
                //已通过，传送出去(不传了，让他们自己出去)
                if (fb_level + 1 >= Max_Level)
                {
                    return;
                }
                //fb_step++;
                MoveNextFBMap(map);
                return;
            }
        }

        //子类可以实现覆盖
        public override string getTitle()
        {
            return Title;
        }
    }



}
