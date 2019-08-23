using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  盘蟹花
    ///  2种攻击手段
    /// </summary>
    public class Monster438 : MonsterObject
    {
        public bool Visible;
        public long VisibleTime;

        protected internal Monster438(MonsterInfo info)
            : base(info)
        {
            Visible = false;
        }

        protected override bool CanAttack
        {
            get
            {
                return Visible && base.CanAttack;
            }
        }
        public override bool Blocking
        {
            get
            {
                return Visible && base.Blocking;
            }
        }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, 8);
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

            if (RandomUtils.Next(100) < 65 && distance<2)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0, TargetID = Target.ObjectID });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage , DefenceType.AC);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1, TargetID = Target.ObjectID });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                ActionList.Add(action);

                if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist && RandomUtils.Next(100) < 50)
                {
                    Target.ApplyPoison(new Poison { Owner = this, Duration = damage / 10, PType = PoisonType.Green, Value = damage / 10, TickSpeed = 2000 }, this);
                }
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }



        protected override void ProcessAI()
        {
            if (!Dead && Envir.Time > VisibleTime)
            {
                VisibleTime = Envir.Time + 2000;

                bool visible = FindNearby(6);

                if (!Visible && visible)
                {
                    Visible = true;
                    CellTime = Envir.Time + 500;
                    Broadcast(GetInfo());
                    Broadcast(new S.ObjectShow { ObjectID = ObjectID });
                    ActionTime = Envir.Time + 1000;
                }

                if (Visible && !visible)
                {
                    Visible = false;
                    VisibleTime = Envir.Time + 3000;

                    Broadcast(new S.ObjectHide { ObjectID = ObjectID });

                    SetHP(MaxHP);
                }
            }

            base.ProcessAI();
        }

        public override bool IsAttackTarget(MonsterObject attacker)
        {
            return Visible && base.IsAttackTarget(attacker);
        }
        public override bool IsAttackTarget(PlayerObject attacker)
        {
            return Visible && base.IsAttackTarget(attacker);
        }

        protected override void ProcessRoam() { }

        protected override void ProcessSearch()
        {
            if (Visible)
                base.ProcessSearch();
        }

        public override Packet GetInfo()
        {
            if (!Visible) return null;

            return base.GetInfo();
        }


    }
}
