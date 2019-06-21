using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;
using System.Drawing;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    /// 复仇的恶灵
    /// 1.丢冰弹
    /// 2.喷怒气
    /// </summary>
    class AvengingSpirit : MonsterObject
    {
        private byte AttackType = 0;
        protected internal AvengingSpirit(MonsterInfo info)
            : base(info)
        {
           
        }

        //8格以内，都是攻击距离
        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > 8 || y > 8) return false;
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
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            //Point target = Functions.PointMove(CurrentLocation, Direction, 2);
            int delay = distance * 50 + 550; //50 MS per Step

            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

            if (distance <= 2 && RandomUtils.Next(2) == 0)
            {
                AttackType = 0;
            }
            else
            {
                AttackType = 1;
            }
         
            //呐喊
            if (AttackType == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                ActionList.Add(action);

                if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist && RandomUtils.Next(5)==0)
                {
                    Target.ApplyPoison(new Poison
                    {
                        Owner = this,
                        Duration = 6,
                        PType = PoisonType.Stun,
                        Value = damage,
                        TickSpeed = 2000
                    }, this);
                }
            }
            //丢毒
            if (AttackType == 1)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
                //中绿毒
                if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist && RandomUtils.Next(4) == 0)
                {
                    Target.ApplyPoison(new Poison
                    {
                        Owner = this,
                        Duration = damage/4,
                        PType = PoisonType.Green,
                        Value = damage / 8,
                        TickSpeed = 2000
                    }, this);
                }
            }
            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;
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
