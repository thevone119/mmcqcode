using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  赤炎狼
    ///  2种攻击手段
    ///  近身普攻，和喷火攻击
    /// </summary>
    public class Demonwolf : MonsterObject
    {
        public long FearTime;

        public byte AttackRange = 3;

        protected internal Demonwolf(MonsterInfo info)
            : base(info)
        {

        }

        protected override void ProcessAI()
        {
            if (!Dead && Envir.Time > FearTime)
            {
                FearTime = Envir.Time + 2000;
                if (RandomUtils.Next(2) == 0)
                {
                    AttackRange = 2;
                }
                else
                {
                    AttackRange = 1;
                }
            }
            base.ProcessAI();
        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > AttackRange || y > AttackRange) return false;

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
            int delay = distance * 50 + 550; //50 MS per Step

            if (distance>1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                LineAttack(3);
            }
            else
            {
                if (RandomUtils.Next(3) == 0)
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    LineAttack(3);
                }
                else
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage * 4 / 3, DefenceType.ACAgility);
                    ActionList.Add(action);
                }
            }
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        private void LineAttack(int distance)
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

                            int delay = Functions.MaxDistance(CurrentLocation, ob.CurrentLocation) * 50 + 300; //50 MS per Step

                            DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MACAgility);
                            ActionList.Add(action);
                        }
                    }
                }
            }
        }

        

    }
}
