using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  九尾狐狸 3种攻击手段
    ///  2种攻击手段
    /// </summary>
    public class Monster435 : MonsterObject
    {


        protected internal Monster435(MonsterInfo info)
            : base(info)
        {
        }



        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, 2);
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

            if (RandomUtils.Next(100) < 70)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                LineAttack(2, 0);
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                List<MapObject> listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 2);
                for (int o = 0; o < listtargets.Count; o++)
                {
                    MapObject ob = listtargets[o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage*2, DefenceType.MAC);
                    ActionList.Add(action);
                }
            }
            

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }


        private void LineAttack(int distance, byte AttackType)
        {
            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

            for (int i = 1; i <= distance; i++)
            {
                Point target = Functions.PointMove(CurrentLocation, Direction, i);

                if (target == Target.CurrentLocation)
                    Target.Attacked(this, damage, DefenceType.MACAgility);
                else
                {
                    if (!CurrentMap.ValidPoint(target)) continue;

                    //Cell cell = CurrentMap.GetCell(target);
                    if (CurrentMap.Objects[target.X, target.Y] == null) continue;

                    for (int o = 0; o < CurrentMap.Objects[target.X, target.Y].Count; o++)
                    {
                        MapObject ob = CurrentMap.Objects[target.X, target.Y][o];
                        if (ob.Race == ObjectType.Monster || ob.Race == ObjectType.Player)
                        {
                            if (!ob.IsAttackTarget(this)) continue;
                            ob.Attacked(this, damage, DefenceType.MACAgility);
                        }
                        else continue;

                        break;
                    }
                }
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
            MoveTo(Target.CurrentLocation);
           
        }

    
     
    }
}
