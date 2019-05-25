using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  ChieftainSword 丹墨
    ///  4种攻击手段
    ///  1.双重打击
    ///  2.怒吼
    ///  3.百鸟归林
    ///  4.拍飞
    public class AncientBringer : MonsterObject
    {
        public long FearTime;
        public byte AttackRange = 2;//攻击范围2格

        private byte attType;
        protected internal AncientBringer(MonsterInfo info)
            : base(info)
        {

        }

        protected override bool InAttackRange()
        {
            byte _AttackRange = AttackRange;
            if (RandomUtils.Next(10) < 5)
            {
                _AttackRange = 1;
            }
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, _AttackRange);
        }

        protected override void ProcessAI()
        {
            if (!Dead && Envir.Time > FearTime)
            {
                FearTime = Envir.Time + 2000;
                if (HP > MaxHP / 6)
                {
                    if (RandomUtils.Next(8) == 0)
                    {
                        AttackRange = 8;
                    }
                    if (RandomUtils.Next(6) == 0)
                    {
                        AttackRange = 6;
                    }
                }
                else {
                    if (RandomUtils.Next(2) == 0)
                    {
                        AttackRange = 8;
                    }
                    if (RandomUtils.Next(2) == 0)
                    {
                        AttackRange = 6;
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

            if (HP < MaxHP / 10)
            {
                damage = damage * 3 / 2;
            }
            else if (HP < MaxHP / 4)
            {
                damage = damage * 4 / 3;
            }

            bool ranged = Functions.InRange(CurrentLocation, Target.CurrentLocation, 2);
            int rd = RandomUtils.Next(10);
            if (ranged)
            {
                if (rd == 0)
                {
                    attType = 2;
                }else if (rd < 5)
                {
                    attType = 1;
                }
                else
                {
                    attType = 0;
                }
            }
            else
            {
                if (rd < 7)
                {
                    attType = 1;
                }
                else
                {
                    attType = 2;
                }
                if (AttackRange > 6)
                {
                    attType = 2;
                }
            }
            DelayedAction action = null;
            switch (attType)
            {
                case 0://双重打击，几率流血
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
             
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);

                    action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);

                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                    {
                        if (RandomUtils.Next(2) == 0)
                        {
                            Target.ApplyPoison(new Poison { Owner = this, Duration = 2, PType = PoisonType.Bleeding, Value = damage/2, TickSpeed = 2000 }, this);
                        }
                    }
                    break;
                case 1://怒吼,身前3*3的范围伤害内伤害，几率眩晕,身前的全部击退
                    AttackRange = 2;
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    Point dp = Functions.PointMove(CurrentLocation, Direction, 3);
                    List<MapObject> list = CurrentMap.getMapObjects(dp.X, dp.Y, 3);
                    for (int o = 0; o < list.Count; o++)
                    {
                        MapObject ob = list[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, ob, damage, DefenceType.MAC);
                        ActionList.Add(action);
                        ob.Pushed(this, Direction, 2);
                        if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist)
                        {
                            if (RandomUtils.Next(2) == 0)
                            {
                                ob.ApplyPoison(new Poison { Owner = this, Duration = 5, PType = PoisonType.Stun, Value = damage / 2, TickSpeed = 2000 }, this);
                            }
                        }
                    }
                    break;
                case 2://白鸟归林，自身范围7格内，全部中，魔法1.5倍伤害
                    AttackRange = 2;
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 0 });
                    List<MapObject> list2 = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 7);
                    for (int o = 0; o < list2.Count; o++)
                    {
                        MapObject ob = list2[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 750, ob, damage, DefenceType.MAC);
                        ActionList.Add(action);
                        if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist)
                        {
                            if (RandomUtils.Next(2) == 0)
                            {
                                ob.ApplyPoison(new Poison { Owner = this, Duration = 6, PType = PoisonType.DelayedExplosion, Value = damage , TickSpeed = 2000 }, this);
                            }
                        }
                    }
                    break;
                case 3:
                    //Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 1 });
                    break;
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

       

    }
}
