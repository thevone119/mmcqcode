using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  ChieftainSword 阳龙王
    ///  5种攻击手段
    ///  1.劈砍
    ///  2.横扫
    ///  3.踏步
    ///  4.旋转砸地
    ///  5.冲撞
    ///  
    ///   frame.Frames.Add(MirAction.Attack1, new Frame(96, 8, 0, 100));
            //frame.Frames.Add(MirAction.Attack2, new Frame(160, 9, 0, 100));
            //frame.Frames.Add(MirAction.Attack3, new Frame(232, 10, 0, 100));
            //frame.Frames.Add(MirAction.AttackRange1, new Frame(312, 10, 0, 200));
            //frame.Frames.Add(MirAction.AttackRange2, new Frame(384, 9, 0, 200));
    /// </summary>
    public class ChieftainSword : MonsterObject
    {
        public long FearTime;
        public byte AttackRange = 5;//攻击范围5格


        private byte attType;
        protected internal ChieftainSword(MonsterInfo info)
            : base(info)
        {

        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > AttackRange || y > AttackRange) return false;

            return true;
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }
            if (attType > 6)
            {
                attType = 0;
            }
            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            int damage = GetAttackPower(MinDC, MaxDC);
            int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + 750; //50 MS per Step

            switch (attType)
            {
                case 0:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });

                    break;
                case 1:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });

                    break;
                case 2:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2});
    
                    break;
                case 3:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 3 });

                    break;
                case 4:
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 0 });

                    break;
                case 5:
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 1 });
        
                    break;
                case 6:
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 2 });
                    break;
            }

            if (attType > 4)
            {
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            else
            {

                DelayedAction action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            

            attType++;


          

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        private void LineAttack(int distance, bool push = false)
        {
            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

            int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + 500;

            for (int i = distance; i >= 1; i--)
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

                        if (push)
                        {
                            ob.Pushed(this, Direction, distance - 1);
                        }

                        ob.Attacked(this, damage, DefenceType.ACAgility);

                    }
                    else continue;

                    break;
                }
            }
        }

    }
}
