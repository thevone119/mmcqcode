using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  暗黑战士
    ///  3种攻击手段
    ///  1.普通攻击
    ///  2.普通攻击
    ///  3.丢冰球
    /// </summary>
    public class DarkWraith : MonsterObject
    {
        private byte attType;
        protected internal DarkWraith(MonsterInfo info)
            : base(info)
        {

        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > 8 || y > 8) return false;

            return (x == 0) || (y == 0) || (x == y);
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
            int delay = distance * 50 + 750; //50 MS per Step
            int rd = RandomUtils.Next(100);
            if (rd < 50)
            {
                attType = 0;
            }
            else if (rd < 80)
            {
                attType = 1;
            }
            else {
                attType = 2;
            }

            if (distance > 2)
            {
                attType = 2;
            }


            if (attType == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            if (attType == 1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                List<MapObject> list = FindAllTargets(2, CurrentLocation, false);
                foreach (MapObject ob in list)
                {
                    if (!ob.IsAttackTarget(this)) continue;
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage * 3 / 2, DefenceType.AC);
                    ActionList.Add(action);
                }
            }

            if (attType == 2)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction,TargetID=Target.ObjectID, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
                Target.ApplyPoison(new Poison
                {
                    Owner = this,
                    Duration = RandomUtils.Next(2, 5),
                    PType = PoisonType.Frozen,
                    Value = damage / 3,
                    TickSpeed = 1000
                }, this);
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

            MoveTo(Target.CurrentLocation);
        }
    }
}
