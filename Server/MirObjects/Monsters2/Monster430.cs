using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    /// Monster430 千面妖王 毒妖林小BOSS  3种攻击手段
    ///  
    /// </summary>
    public class Monster430 : MonsterObject
    {


        protected internal Monster430(MonsterInfo info)
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
            byte atype = 0;
            if (distance < 2)
            {
                if(RandomUtils.Next(100) < 70)
                {
                    atype = 1;
                }
                else
                {
                    atype = 2;
                }
            }
            else
            {
                if (RandomUtils.Next(100) < 60)
                {
                    atype = 2;
                }
                else
                {
                    atype = 3;
                }
            }

            if (atype == 1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage*3/4, DefenceType.ACAgility);
                ActionList.Add(action);
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay+300, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }


            if (atype==2)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                List<MapObject> listtargets = CurrentMap.getMapObjects(Target.CurrentLocation.X, Target.CurrentLocation.Y, 2);
                for (int o = 0; o < listtargets.Count; o++)
                {
                    MapObject ob = listtargets[o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage , DefenceType.MAC);
                    ActionList.Add(action);

                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist )
                    {
                        if (RandomUtils.Next(100) < 65)
                        {
                            ob.ApplyPoison(new Poison { Owner = this, Duration = 5, PType = PoisonType.Slow, Value = damage / 10, TickSpeed = 2000 }, this);
                        }
                        else
                        {
                            ob.ApplyPoison(new Poison { Owner = this, Duration = 5, PType = PoisonType.Paralysis, Value = damage / 5, TickSpeed = 1000 }, this);
                        }
                    }
                }
            }
            if (atype == 3)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay + 300, Target, damage*2, DefenceType.MACAgility);
                ActionList.Add(action);
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
