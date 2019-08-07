using Server.MirDatabase;
using Server.MirEnvir;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using S = ServerPackets;

namespace Server.MirObjects
{
    /// <summary>
    /// 契约兽的一些公式，计算在这里实现
    /// </summary>
    public class MyMonsterUtils
    {
        //通过这个字典，
        private static List<MonsterInfo> allmyMonster = new List<MonsterInfo>();


        public MyMonsterUtils()
        {

        }


        private static void loadMyMonsterList()
        {
            allmyMonster.Clear();
            foreach (MonsterInfo minfo in SMain.Envir.MonsterInfoList)
            {
                if (minfo.CanTreaty)
                {
                    allmyMonster.Add(minfo);
                }
            }
        }


        //契约兽获得经验
        public static void MyMonExp(MyMonster myMon, uint amount, PlayerObject player, MonsterObject mon)
        {
            if (myMon == null|| player==null || amount==0)
            {
                return;
            }
            //契约兽最高只比玩家高3级
            int maxLevel = player.Level + 3;
            if (myMon.MonLevel >= maxLevel) return;

            long currExp = myMon.currExp;

            currExp += amount;
            //经验没超标，则直接返回
            if (currExp < myMon.maxExp)
            {
                myMon.currExp = currExp;
                player.Enqueue(new S.MyMonstersExpUpdate { monidx = myMon.idx, MonLevel = myMon.MonLevel, currExp = myMon.currExp, maxExp = myMon.maxExp });
                return;
            }
            else
            {
                //如果没有传过来契约兽，则查找下契约兽是否存活，存活则更新下存活的契约兽
                if (mon == null && player.Pets!=null)
                {
                    //查找玩家的宠物，看有没此契约兽
                    for (int i = 0; i < player.Pets.Count; i++)
                    {
                        MonsterObject pet = player.Pets[i];
                        if (pet==null|| pet.Dead || pet.myMonster==null || pet.myMonster.idx!= myMon.idx)
                        {
                            continue;
                        }
                        mon = pet;
                    }
                }

                while (currExp >= myMon.maxExp)
                {
                    myMon.MonLevel++;
                    currExp -= myMon.maxExp;
                    RefreshMyMonLevelStats(myMon, mon);
                    if (myMon.MonLevel >= maxLevel) break;
                }
                myMon.currExp = currExp;
                player.GetMyMonsters();
                return;
            }
        }

        //刷新契约兽等级属性数据
        public static void RefreshMyMonLevelStats(MyMonster myMon, MonsterObject mon)
        {
            if (myMon == null)
            {
                return;
            }
            myMon.maxExp = myMon.MonLevel < Settings.MyMonstersExpList.Count ? Settings.MyMonstersExpList[myMon.MonLevel - 1] : 0;
            //3维成长系数
            byte AllUp = (byte)(myMon.AcUp + myMon.MacUp + myMon.DcUp);
         
            //固定成长（这个要做成线性增长，就是等级越高，成长越厉害）
            ushort AllAdd = (ushort)(myMon.MonLevel/5 + myMon.MonLevel / 10 + myMon.MonLevel);

            //本体加成长都算进这里
            MonsterInfo minfoup = SMain.Envir.GetMonsterInfo(myMon.MonIndex);
            if (myMon.UpMonIndex > 0)
            {
                MonsterInfo minfoup2 = SMain.Envir.GetMonsterInfo(myMon.UpMonIndex);
                if (minfoup2 != null)
                {
                    minfoup = minfoup2;
                }
            }
            if (minfoup == null)
            {
                return;
            }
            //本体
            myMon.sHP = minfoup.HP;
            myMon.sMinAC = minfoup.MinAC;
            myMon.sMaxAC = minfoup.MaxAC;
            myMon.sMinMAC = minfoup.MinMAC;
            myMon.sMaxMAC = minfoup.MaxMAC;
            myMon.sMinDC = minfoup.MinDC;
            myMon.sMaxDC = minfoup.MaxDC;
            myMon.sAccuracy = minfoup.Accuracy;
            myMon.sAgility = minfoup.Agility;
            //成长体
            myMon.uHP = (uint)AllAdd * 20;
            myMon.uMinAC = (ushort)(AllAdd * 0.7);
            myMon.uMaxAC = (ushort)(AllAdd * 1.4);
            myMon.uMinMAC = (ushort)(AllAdd * 0.7);
            myMon.uMaxMAC = (ushort)(AllAdd * 1.4);
            myMon.uMinDC = (ushort)(AllAdd * 0.6);
            myMon.uMaxDC = (ushort)(AllAdd * 1.1);
            myMon.uAccuracy = (ushort)(AllAdd * 0.4);//准确
            myMon.uAgility = (ushort)(AllAdd * 0.35);//敏捷
            //最终体
            myMon.tHP = (uint)((myMon.sHP + myMon.uHP)* AllUp / 30.0);
            myMon.tMinAC = (ushort)((myMon.sMinAC + myMon.uMinAC) * myMon.AcUp / 10.0);
            myMon.tMaxAC = (ushort)((myMon.sMaxAC + myMon.uMaxAC) * myMon.AcUp / 10.0);
            myMon.tMinMAC = (ushort)((myMon.sMinMAC + myMon.uMinMAC) * myMon.MacUp / 10.0);
            myMon.tMaxMAC = (ushort)((myMon.sMaxMAC + myMon.uMaxMAC) * myMon.MacUp / 10.0);
            myMon.tMinDC = (ushort)((myMon.sMinDC + myMon.uMinDC) * myMon.DcUp / 10.0);
            myMon.tMaxDC = (ushort)((myMon.sMaxDC + myMon.uMaxDC) * myMon.DcUp / 10.0);
            myMon.tAccuracy = (ushort)((myMon.sAccuracy + myMon.uAccuracy) * AllUp / 30.0);
            myMon.tAgility = (ushort)((myMon.sAgility + myMon.uAgility) * AllUp / 30.0);

            if (myMon.tAccuracy > 200)
            {
                myMon.tAccuracy = 200;
            }
            if (myMon.tAgility > 200)
            {
                myMon.tAgility = 200;
            }

            //金刚 大幅提升双抗（增加1倍），降低攻击（减少30%）
            if (myMon.hasMonSk(MyMonSkill.MyMonSK2))
            {
                myMon.tMinAC = (ushort)(myMon.tMinAC * 1.6 );
                myMon.tMaxAC = (ushort)(myMon.tMaxAC * 1.6 );
                myMon.tMinMAC = (ushort)(myMon.tMinMAC * 1.6);
                myMon.tMaxMAC = (ushort)(myMon.tMaxMAC * 1.6);

                myMon.tMinDC = (ushort)(myMon.tMinDC * 0.7);
                myMon.tMaxDC = (ushort)(myMon.tMaxDC * 0.7);
            }

            //矫捷 敏捷，准确提升（提升50%）
            if (myMon.hasMonSk(MyMonSkill.MyMonSK5))
            {
                int Accuracy = myMon.tAccuracy;
                Accuracy = (int)(Accuracy * 1.6);
                if (Accuracy > 200)
                {
                    Accuracy = 200;
                }
                myMon.tAccuracy = (byte)Accuracy;

                int Agility = myMon.tAgility;
                Agility = (int)(Agility * 1.6);
                if (Agility > 200)
                {
                    Agility = 200;
                }
                myMon.tAgility = (byte)Agility;
            }
            //兽血 血量提升，血量恢复提升（提升1倍）
            if (myMon.hasMonSk(MyMonSkill.MyMonSK3))
            {
                myMon.tHP = (uint)(myMon.tHP * 1.8) ;
            }



            //刷新具体怪物的属性
            if (mon==null|| mon.Info == null)
            {
                return;
            }
            //这里要重新加载处理，要以原始的数据作为副本，否则会无限叠加
            MonsterInfo _minfo = SMain.Envir.GetMonsterInfo(myMon.MonIndex);
            mon.Info = _minfo.Clone();
            //各种属性设置
            mon.Info.Name = myMon.getName();

            mon.Info.Level = myMon.MonLevel;
            mon.Info.HP = myMon.tHP;

         

            mon.Info.MinAC = myMon.tMinAC;
            mon.Info.MaxAC = myMon.tMaxAC;
            mon.Info.MinMAC = myMon.tMinMAC;
            mon.Info.MaxMAC = myMon.tMaxMAC;

            //
            mon.Info.MinDC = myMon.tMinDC;
            mon.Info.MaxDC = myMon.tMaxDC;
            //
            mon.Info.Accuracy = (byte)(myMon.tAccuracy);
            mon.Info.Agility = (byte)(myMon.tAgility);


     
            //兽血 血量提升，血量恢复提升提升
            if (myMon.hasMonSk(MyMonSkill.MyMonSK3))
            {
                mon.HealthScale = 0.035F;
            }

            //狂暴 攻速，移速提升（提升50%）
            if (myMon.hasMonSk(MyMonSkill.MyMonSK4))
            {
                int AttackSpeed = mon.Info.AttackSpeed;
                AttackSpeed = (int)(AttackSpeed *0.65);
                if (AttackSpeed < 500)
                {
                    AttackSpeed = 500;
                }
                if (AttackSpeed > 3000)
                {
                    AttackSpeed = 3000;
                }
                mon.Info.AttackSpeed = (ushort)AttackSpeed;

                //移速
                int MoveSpeed = mon.Info.MoveSpeed;
                //
                MoveSpeed = (int)(MoveSpeed * 0.5);
                if (MoveSpeed < 300)
                {
                    MoveSpeed = 300;
                }
                if (MoveSpeed > 3000)
                {
                    MoveSpeed = 3000;
                }
                mon.Info.MoveSpeed = (ushort)MoveSpeed;
            }

            
            
            if (mon != null)
            {
                mon.RefreshAll();
                mon.OperateTime = 0;
                mon.BroadcastHealthChange();
            }
        }

        /// <summary>
        /// 随机抽取契约兽
        /// </summary>
        /// <param name="minLevel"></param>
        /// <param name="maxLevel"></param>
        /// <returns></returns>
        public static MonsterInfo RandomMonster(int minLevel,int maxLevel,int bosstype)
        {

            if (allmyMonster.Count == 0)
            {
                loadMyMonsterList();
            }
            if(minLevel<=0 && maxLevel <= 0 && bosstype<0)
            {
                return allmyMonster[RandomUtils.Next(allmyMonster.Count)].Clone();
            }
            List<MonsterInfo> list = new List<MonsterInfo>();

            foreach (MonsterInfo minfo in allmyMonster)
            {
                if(minLevel>0 && minfo.Level < minLevel)
                {
                    continue;
                }
                if (maxLevel > 0 && minfo.Level > maxLevel)
                {
                    continue;
                }
                if (bosstype >= 0 && minfo.bosstype != bosstype)
                {
                    continue;
                }
                list.Add(minfo);
            }
           
            if (list.Count > 0)
            {
                return list[RandomUtils.Next(list.Count)].Clone();
            }
            return null;
        }



    }
}
