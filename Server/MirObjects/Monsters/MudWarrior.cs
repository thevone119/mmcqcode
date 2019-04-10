using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  泥巨人
    ///  2种攻击手段
    ///  1.普通攻击(拉人) 
    ///  2.重击
    ///  死掉了，又重新复活的
    /// </summary>
    public class MudWarrior : MonsterObject
    {
        public long FearTime;

        public byte AttackRange = 3;//攻击范围2格

        public uint RevivalCount;
        public int LifeCount;
        public long RevivalTime, DieTime;

        protected internal MudWarrior(MonsterInfo info)
            : base(info)
        {
            RevivalCount = 0;
            LifeCount = RandomUtils.Next(4);//这里改下，最多复活2次
        }


        public override void Die()
        {
            DieTime = Envir.Time;
            RevivalTime = (4 + RandomUtils.Next(20)) * 1000;
            //没有真正死亡，是不爆东西的
            if(RevivalCount< LifeCount)
            {
                EXPOwner = null;
            }
            base.Die();
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

        protected override void ProcessAI()
        {
            if (!Dead && Envir.Time > FearTime)
            {
                FearTime = Envir.Time + 2000;
                if (RandomUtils.Next(2) == 0)
                {
                    AttackRange = 3;
                }
                else
                {
                    AttackRange = 1;
                }
            }
            //复活
            if (Dead && Envir.Time > DieTime + RevivalTime && RevivalCount < LifeCount)
            {
                RevivalCount++;
                Revive(MaxHP, false);
            }
            base.ProcessAI();
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

            if(distance > 1)//这条线上没有人，才拉，否则不拉
            {
                for (int i = 1; i < distance; i++)
                {
                    Point target = Functions.PointMove(CurrentLocation, Direction, i);
                    if (!CurrentMap.EmptyPoint(target.X, target.Y))
                    {
                        AttackRange = 1;
                        return;
                    }
                }
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
                MirDirection pushdir = Functions.DirectionFromPoint(Target.CurrentLocation, CurrentLocation);
                Target.Pushed(this, pushdir, distance-1);
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        

    }
}
