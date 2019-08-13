using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  昆仑叛军战神 小BOSS
    ///  2种攻击手段
    /// </summary>
    public class Monster441 : MonsterObject
    {


        protected internal Monster441(MonsterInfo info)
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

            if (RandomUtils.Next(100) < 70 && distance<3)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                List<MapObject> listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 3);
                for (int o = 0; o < listtargets.Count; o++)
                {
                    MapObject ob = listtargets[o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;
                    action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 500, ob, damage, DefenceType.MAC);
                    ActionList.Add(action);
                    action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 1200, ob, damage, DefenceType.MAC);
                    ActionList.Add(action);
                }
            }
            

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        protected override void CompleteRangeAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];
            if(Functions.MaxDistance(CurrentLocation, target.CurrentLocation) > 3)
            {
                return;
            }
            target.Attacked(this, damage, defence);
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
