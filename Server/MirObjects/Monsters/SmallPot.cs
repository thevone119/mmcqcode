using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  SmallPot 如何使者 小BOSS,也会复活？靠
    ///  2种攻击手段
    ///  1.拳击
    ///  2.拍击
    ///  3.仗击
    ///  4.唱歌，释放魔法
    ///  死掉了，又重新复活的
    /// </summary>
    public class SmallPot : MonsterObject
    {
        public long FearTime;

        public byte AttackRange = 2;//攻击范围2格

        public uint RevivalCount;
        public int LifeCount;
        public long RevivalTime, DieTime;



        protected internal SmallPot(MonsterInfo info)
            : base(info)
        {
            RevivalCount = 0;
            LifeCount = RandomUtils.Next(1,3);//复活1-2次，最多复活2次
        }


        public override void Die()
        {
            DieTime = Envir.Time;
            RevivalTime = (10 + RandomUtils.Next(5,30)) * 1000;
            //没有真正死亡，是不爆东西的
            if(RevivalCount< LifeCount)
            {
                if (RevivalCount == LifeCount - 1)
                {
                    DropGold(10000);
                }
                EXPOwner = null;
            }

            base.Die();
        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > AttackRange || y > AttackRange) return false;

            return (x == 0) || (y == 0) || (x == y);
        }

        protected override void ProcessAI()
        {
            if (!Dead && Envir.Time > FearTime)
            {
                FearTime = Envir.Time + 2000;
                if (RandomUtils.Next(10) < 7)
                {
                    AttackRange = 2;
                }
                else
                {
                    AttackRange = 1;
                }
                //假死
                if(HP < MaxHP / 2 && RandomUtils.Next(3)==0 && RevivalCount < LifeCount)
                {
                    Die();
                }
            }
            //复活
            if (Dead && Envir.Time > DieTime + RevivalTime && RevivalCount < LifeCount)
            {
                RevivalCount++;
                Revive(MaxHP*2/3, false);
            }
            base.ProcessAI();
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }
            int rd = RandomUtils.Next(10);
            byte attckType = 0;
            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            int damage = GetAttackPower(MinDC, MaxDC);
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            int delay = distance * 50 + 750; //50 MS per Step
            DelayedAction action = null;
            if (distance > 1)//这条线上没有人，才拉，否则不拉
            {
                if(RevivalCount >= LifeCount)
                {
                    if (rd < 7)
                    {
                        attckType = 2;
                    }
                    else
                    {
                        attckType = 3;
                    }
                }
                else
                {
                    if (rd < 7)
                    {
                        attckType = 2;
                    }
                    else
                    {
                        attckType = 1;
                    }
                }
            }
            else
            {
                if (RevivalCount >= LifeCount)
                {
                    if (rd < 7)
                    {
                        attckType = 2;
                    }
                    else
                    {
                        attckType = 3;
                    }
                }
                else
                {
                    if (rd < 7)
                    {
                        attckType = 0;
                    }
                    else
                    {
                        attckType = 1;
                    }
                }
            }

            switch (attckType)
            {
                case 0://拳击
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    break;
                case 1://拍击
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                    {
                        if (RandomUtils.Next(2) == 0)
                        {
                            Target.ApplyPoison(new Poison { PType = PoisonType.Stun, Duration = 8, TickSpeed = 2000 }, this);
                        }
                    }
                    break;

                case 2://仗击(半月的范围攻击，1.5倍伤害)
                       //
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                    List<MapObject> list = CurrentMap.getMapObjects(Target.CurrentLocation.X, Target.CurrentLocation.Y, 1);
                    for (int o = 0; o < list.Count; o++)
                    {
                        MapObject ob = list[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage*14/10, DefenceType.None);
                        ActionList.Add(action);
                    }
                    break;
                case 3://唱歌，释放魔法，范围禁锢，麻痹
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    List<MapObject> list2 = CurrentMap.getMapObjects(Target.CurrentLocation.X, Target.CurrentLocation.Y, 2);
                    for (int o = 0; o < list2.Count; o++)
                    {
                        MapObject ob = list2[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage * 18 / 10, DefenceType.MAC);
                        ActionList.Add(action);
                        if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist)
                        {
                            if (RandomUtils.Next(10) < 7)
                            {
                                ob.ApplyPoison(new Poison { PType = PoisonType.Paralysis, Duration = 6, TickSpeed = 1000 }, this);
                            }
                        }
                    }
                    break;
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        

    }
}
