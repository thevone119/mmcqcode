using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  AssassinBird 神殿刺鸟，如何神殿怪物
    ///  3种攻击手段
    ///  1.普通攻击
    ///  2.踢腿，击退
    ///  3.飞环，击晕
    /// </summary>
    public class AssassinBird : MonsterObject
    {
        public long FearTime;
        public byte AttackRange = 7;

        protected internal AssassinBird(MonsterInfo info)
            : base(info)
        {

        }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
        }

        protected override void ProcessAI()
        {
            if (!Dead && Envir.Time > FearTime)
            {
                FearTime = Envir.Time + 2000;
                if (HP > MaxHP / 2)
                {
                    if (RandomUtils.Next(5) == 0)
                    {
                        AttackRange = 7;
                    }
                    else
                    {
                        AttackRange = 1;
                    }
                }
                else {
                    if (RandomUtils.Next(10) < 5)
                    {
                        AttackRange = 7;
                    }
                    else
                    {
                        AttackRange = 1;
                    }
                }
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

 
            if (distance==1)
            {
                if(RandomUtils.Next(10) < 7)
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                }
                else
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    Target.Pushed(this, Direction, 2);
                }
            }
            else
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, TargetID= Target.ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MACAgility);
                ActionList.Add(action);
                if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                {
                    if (RandomUtils.Next(8) == 0)
                    {
                        Target.ApplyPoison(new Poison { Owner = this, Duration = 5, PType = PoisonType.Stun, Value = damage / 5, TickSpeed = 2000 }, this);
                    }
                }
            }
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        

    }
}
