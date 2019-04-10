using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  SeedingsGeneral 灵猫圣兽
    ///  4种攻击手段
    ///  1.撕咬，身前1格
    ///  2.拍飞，身前2个，直线距离的，全部击退2格
    ///  3.身前1格，拍地板，身前1格位置2*2范围内的攻击，几率眩晕
    ///  4.吐气攻击，远程攻击7格访问内都攻击到，单点，几率冰冻
    public class SeedingsGeneral : MonsterObject
    {
        public long FearTime;
        public byte AttackRange = 1;//攻击范围1格

        private byte attType;
        protected internal SeedingsGeneral(MonsterInfo info)
            : base(info)
        {

        }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
        }

        protected override void ProcessAI()
        {
            if (!Dead && Envir.Time > FearTime)
            {
                FearTime = Envir.Time + 2000;
                if (HP > MaxHP / 5)
                {
                    if (RandomUtils.Next(2) == 0)
                    {
                        AttackRange = 2;
                    }
                    if (RandomUtils.Next(5) == 0)
                    {
                        AttackRange = 8;
                    }
                }
                else {
                    if (RandomUtils.Next(2) == 0)
                    {
                        AttackRange = 2;
                    }
                    if (RandomUtils.Next(2) == 0)
                    {
                        AttackRange = 8;
                    }
                }
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
            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            int damage = GetAttackPower(MinDC, MaxDC);
            int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + 750; //50 MS per Step
            DelayedAction action = null;

            if (HP < MaxHP / 10)
            {
                damage = damage * 3 / 2;
            }
            else if (HP < MaxHP / 4)
            {
                damage = damage * 4 / 3;
            }
            int rd = RandomUtils.Next(10);
            int Distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            if (Distance <= 1)
            {
                if (rd <= 1)
                {
                    attType = 2;
                }
                else if (rd <= 3)
                {
                    attType = 1;
                }
                else
                {
                    attType = 0;
                }
            }
            if (Distance == 2)
            {
                attType = 2;
            }
            if (Distance > 2)
            {
                attType = 3;
            }

            switch (attType)
            {
                case 0://撕咬，身前1格
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    break;
                case 1://拍飞，身前2个，直线距离的，全部击退2格
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    LineAttack(2, true);
                    break;
                case 2://身前1格，拍地板，身前1格位置2*2范围内的攻击，几率眩晕
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                    Point dp = Functions.PointMove(CurrentLocation, Direction, 1);
                    List<MapObject> list = CurrentMap.getMapObjects(dp.X, dp.Y, 2);
                    for (int o = 0; o < list.Count; o++)
                    {
                        MapObject ob = list[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, ob, damage*4/3, DefenceType.MAC);
                        ActionList.Add(action);
                        if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist)
                        {
                            if (RandomUtils.Next(3) == 0)
                            {
                                ob.ApplyPoison(new Poison { Owner = this, Duration = 5, PType = PoisonType.Stun, Value = damage / 2, TickSpeed = 2000 }, this);
                            }
                        }
                    }
                    break;
                case 3://吐气攻击，远程攻击7格访问内都攻击到，单点，几率冰冻
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                    ActionList.Add(action);
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                    {
                        if (RandomUtils.Next(3) == 0)
                        {
                            Target.ApplyPoison(new Poison { Owner = this, Duration = 5, PType = PoisonType.Slow, Value = damage / 2, TickSpeed = 2000 }, this);
                        }else if(RandomUtils.Next(10) == 0)
                        {
                            Target.ApplyPoison(new Poison { Owner = this, Duration = 3, PType = PoisonType.Frozen, Value = damage / 2, TickSpeed = 2000 }, this);
                        }
                    }
                    //Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 1 });
                    break;
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        private void LineAttack(int distance, bool push = false)
        {
            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

            int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + 500;

            for (int i = distance; i >= 1; i--)
            {
                Point target = Functions.PointMove(CurrentLocation, Direction, i);

                if (!CurrentMap.ValidPoint(target)) continue;

                //Cell cell = CurrentMap.GetCell(target);
                if (CurrentMap.Objects[target.X, target.Y] == null) continue;

                for (int o = 0; o < CurrentMap.Objects[target.X, target.Y].Count; o++)
                {
                    MapObject ob = CurrentMap.Objects[target.X, target.Y][o];
                    if (ob.Race == ObjectType.Monster || ob.Race == ObjectType.Player)
                    {
                        if (!ob.IsAttackTarget(this)) continue;

                        if (push)
                        {
                            ob.Pushed(this, Direction, distance - 1);
                        }

                        ob.Attacked(this, damage, DefenceType.ACAgility);
                    }
                    else continue;

                    break;
                }
            }
        }



    }
}
