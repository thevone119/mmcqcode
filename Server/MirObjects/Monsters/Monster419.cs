using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///   冰宫巫师
    ///  3种攻击手段
    /// </summary>
    public class Monster419 : MonsterObject
    {

        private byte AttackType;
        protected internal Monster419(MonsterInfo info)
            : base(info)
        {
        }


        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, 8);
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
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            int delay = distance * 50 + 500; //50 MS per Step
            DelayedAction action = null;

            int rd = RandomUtils.Next(100);
            if (rd < 50)
            {
                AttackType = 0;
            }else if (rd < 70)
            {
                AttackType = 1;
            }
            else
            {
                AttackType = 2;
            }


            if (distance > 3)
            {
                AttackType = 2;
            }else if (distance > 1)
            {
                if (AttackType == 0)
                {
                    AttackType = 1;
                }
            }

            switch (AttackType)
            {
                case 0:
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    break;

                case 1:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    List<MapObject> listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 3);
                    for (int o = 0; o < listtargets.Count; o++)
                    {
                        MapObject ob = listtargets[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage*2, DefenceType.MAC);
                        ActionList.Add(action);
                        //减速
                        if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist && RandomUtils.Next(100) < 30)
                        {
                            Target.ApplyPoison(new Poison
                            {
                                Owner = this,
                                Duration = RandomUtils.Next(5, 15),
                                PType = PoisonType.Slow,
                                Value = damage,
                                TickSpeed = 1000
                            }, this);
                        }
                    }
                    break;


                case 2:
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1});
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MACAgility);
                    ActionList.Add(action);
                    break;
            }
            
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }





        protected override void ProcessTarget()
        {
            if (Target == null) return;

            if (InAttackRange() && CanAttack)
            {
                Attack();
                if (Target == null || Target.Dead)
                    FindTarget();

                return;
            }

            if (Envir.Time < ShockTime)
            {
                Target = null;
                return;
            }

            if (!CanMove || Target == null) return;
            MoveTo(Target.CurrentLocation);
        }



    }
}
