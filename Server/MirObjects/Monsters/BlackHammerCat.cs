using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  黑镐猫卫
    ///  2种攻击手段
    ///  1.普通攻击 2格内攻击到
    ///  2.2次打击 3格内都几率麻痹
    /// </summary>
    public class BlackHammerCat : MonsterObject
    {
        public long FearTime;

        public byte AttackRange = 2;

        protected internal BlackHammerCat(MonsterInfo info)
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
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);

            if (distance <= 1 && RandomUtils.Next(5) != 1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                LineAttack(2, 0);
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                LineAttack(2,1);
            }
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        private void LineAttack(int distance,byte attacktype)
        {
            int damage = GetAttackPower(MinDC, MaxDC);
            long delay = 500;

            DelayedAction action = null;
            if (damage == 0) return;

            for (int i = 1; i <= distance; i++)
            {
                Point target = Functions.PointMove(CurrentLocation, Direction, i);
                if (target == Target.CurrentLocation)
                {
                    if (attacktype == 0)
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage*3/2, DefenceType.ACAgility);
                        ActionList.Add(action);
                    }
                    else
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                        ActionList.Add(action);
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay+ delay, Target, damage * 2, DefenceType.ACAgility);
                        ActionList.Add(action);
                    }
                }
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

                            if (attacktype == 0)
                            {
                                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage * 3 / 2, DefenceType.ACAgility);
                                ActionList.Add(action);
                            }
                            else
                            {
                                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.ACAgility);
                                ActionList.Add(action);
                                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay + delay, ob, damage * 2, DefenceType.ACAgility);
                                ActionList.Add(action);
                            }
                        }
                    }
                }
            }

        }

    }
}
