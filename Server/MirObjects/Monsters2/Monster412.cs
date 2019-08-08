using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  冰宫刀卫
    ///  2种攻击手段
    ///  1.普通攻击
    ///  2.普通攻击+访问攻击
    /// </summary>
    public class Monster412 : MonsterObject
    {

        public long lastMove = 0;
        protected internal Monster412(MonsterInfo info)
            : base(info)
        {

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

            if (Envir.Time>lastMove+1000)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            else
            {
                //瞬移之后的那一刀，特别疼
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage*2 , DefenceType.AC);
                ActionList.Add(action);
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        protected override void ProcessTarget()
        {
            if (Target == null || !CanAttack) return;

            if (InAttackRange())
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

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);
            //瞬移
            if ((x > 5 || y > 5) && RandomUtils.Next(4) == 0)
            {
                if (CurrentMap.ValidPoint(Target.Back))
                {
                    Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    DelayedAction action = new DelayedAction(DelayedType.MapMovement, Envir.Time + 600, CurrentMap, Target.Back, CurrentMap, CurrentLocation);
                    ActionList.Add(action);
                    ActionTime = Envir.Time + 600;
                    AttackTime = Envir.Time + 600;
                    lastMove = Envir.Time + 600;
                    //Teleport(CurrentMap, Target.Back,false);
                    return;
                }
                else if (CurrentMap.ValidPoint(Target.Front))
                {
                    Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    DelayedAction action = new DelayedAction(DelayedType.MapMovement, Envir.Time + 600, CurrentMap, Target.Back, CurrentMap, CurrentLocation);
                    ActionList.Add(action);
                    ActionTime = Envir.Time + 600;
                    AttackTime = Envir.Time + 600;
                    lastMove = Envir.Time + 600;
                    return;
                }
            }
            MoveTo(Target.CurrentLocation);
        }

    }
}
