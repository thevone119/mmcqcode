using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///   Monster434 昆仑叛军法师 3种攻击手段
    /// </summary>
    public class Monster434 : MonsterObject
    {


        protected internal Monster434(MonsterInfo info)
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
            int _AttackSpeed= AttackSpeed;
            if (RandomUtils.Next(100) < 70 && distance<2)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.AC);
                ActionList.Add(action);
            }
            else
            {
                _AttackSpeed = _AttackSpeed +1500;
                Point src = CurrentMap.RandomValidPoint(Target.CurrentLocation.X, Target.CurrentLocation.Y, 4);
                if (RandomUtils.Next(100) < 75)
                {
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Target = src, Type = 0 });
                    action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 2500, Target, damage, DefenceType.MAC, src);
                    ActionList.Add(action);
                }
                else
                {
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Target = src, Type = 1 });
                    action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 2500, Target, damage*2, DefenceType.MAC, src);
                    ActionList.Add(action);
                }
            }
            
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (_AttackSpeed);
        }

        protected override void CompleteRangeAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];
            Point src = (Point)data[3];
            List<MapObject> listtargets = CurrentMap.getMapObjects(src.X, src.Y, 3);
            for (int o = 0; o < listtargets.Count; o++)
            {
                MapObject ob = listtargets[o];
                if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                if (!ob.IsAttackTarget(this)) continue;
                ob.Attacked(this, damage, defence);
            }
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
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            if (distance > 6)
            {
                MoveTo(Target.CurrentLocation);
                return;
            }


            //40几率跑路
            if (RandomUtils.Next(100) < 60)
            {
                return;
            }
            MirDirection dir = Functions.DirectionFromPoint(Target.CurrentLocation, CurrentLocation);

            if (Walk(dir)) return;

            switch (RandomUtils.Next(2)) //No favour
            {
                case 0:
                    for (int i = 0; i < 7; i++)
                    {
                        dir = Functions.NextDir(dir);

                        if (Walk(dir))
                            return;
                    }
                    break;
                default:
                    for (int i = 0; i < 7; i++)
                    {
                        dir = Functions.PreviousDir(dir);

                        if (Walk(dir))
                            return;
                    }
                    break;
            }
        }



    }
}
