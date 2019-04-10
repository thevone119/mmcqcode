using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  FrozenZombie 暴雪僵尸
    ///  2种攻击手段
    ///  1.普通物理攻击
    ///  2.范围魔法攻击，周围全冰冻（1/3几率）
    /// </summary>
    public class FrozenZombie : MonsterObject
    {
        public byte AttackRange = 2;//攻击范围2格

        protected internal FrozenZombie(MonsterInfo info)
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
            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            int damage = GetAttackPower(MinDC, MaxDC);
            int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + 750; //50 MS per Step
            if (RandomUtils.Next(4) == 1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 0 });
                LineAttack(3);
            }
           
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        private void LineAttack(int distance)
        {
            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

            int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + 500;

            List<MapObject> list = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, distance);
            for (int o = 0; o < list.Count; o++)
            {
                MapObject ob = list[o];
                if (ob.Race == ObjectType.Monster || ob.Race == ObjectType.Player)
                {
                    if (!ob.IsAttackTarget(this)) continue;
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MAC);
                    ActionList.Add(action);
                    //ob.Attacked(this, damage, DefenceType.AC);
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist && RandomUtils.Next(5) == 1)
                    {
                        int poison = GetAttackPower(MinSC, MaxSC);
                        ob.ApplyPoison(new Poison
                        {
                            Owner = this,
                            Duration = 4,
                            PType = PoisonType.Slow,
                            Value = poison,
                            TickSpeed = 2000
                        }, this);
                    }
                }
                else continue;

                break;
            }
           
        }

    }
}
