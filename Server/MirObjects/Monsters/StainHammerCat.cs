using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  铁锤猫卫
    ///  1种攻击手段
    ///  近身2格内都被攻击，2格距离的几率眩晕
    /// </summary>
    public class StainHammerCat : MonsterObject
    {
        public long FearTime;

        public byte AttackRange = 2;

        protected internal StainHammerCat(MonsterInfo info)
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
 
            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
            LineAttack(2);
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

                        DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.ACAgility);
                        ActionList.Add(action);

                        if (i == 2 && RandomUtils.Next(5)==1)
                        {
                            ob.ApplyPoison(new Poison
                            {
                                Owner = this,
                                Duration = 5,
                                PType = PoisonType.Stun,
                                Value = damage,
                                TickSpeed = 2000
                            }, this);
                        }
                    }
                }
            }
        }

        

    }
}
