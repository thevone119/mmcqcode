using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  冰宫画卷(范围放毒)
    ///  2种攻击手段
    /// </summary>
    public class Monster428 : MonsterObject
    {


        protected internal Monster428(MonsterInfo info)
            : base(info)
        {
        }



        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, 3);
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

            if (RandomUtils.Next(100) < 65 && distance<3)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                List<MapObject> listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 2);
                for (int o = 0; o < listtargets.Count; o++)
                {
                    MapObject ob = listtargets[o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage , DefenceType.MAC);
                    ActionList.Add(action);

                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist && RandomUtils.Next(100) < 40)
                    {
                        ob.ApplyPoison(new Poison { Owner = this, Duration = damage / 10, PType = PoisonType.Green, Value = damage / 10, TickSpeed = 2000 }, this);
                    }
                }
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                List<MapObject> listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 3);
                for (int o = 0; o < listtargets.Count; o++)
                {
                    MapObject ob = listtargets[o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage*3/2, DefenceType.MAC);
                    ActionList.Add(action);
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist && RandomUtils.Next(100)<40)
                    {
                        ob.ApplyPoison(new Poison { Owner = this, Duration = damage / 10, PType = PoisonType.Green, Value = damage / 5, TickSpeed = 2000 }, this);
                    }
                }
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
