using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  IcePhantom 雪原恶鬼 
    ///  1.拍击
    ///  2.火蛇
    ///  3.冰蛇
    /// </summary>
    public class IcePhantom : MonsterObject
    {
        public bool Visible;
        public long VisibleTime;
        private byte hideTime = 0;
        private byte hideCount = 1;
        private byte attType;
        protected internal IcePhantom(MonsterInfo info)
            : base(info)
        {
            Visible = false;
            hideCount = (byte)RandomUtils.Next(1, 3);
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
            return CurrentMap == Target.CurrentMap && Visible && Functions.InRange(CurrentLocation, Target.CurrentLocation, 8);
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }
            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            int Distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
           
            int damage = GetAttackPower(MinDC, MaxDC);
            attType = 0;
            if (Distance > 2)
            {
                if (RandomUtils.Next(100) < 70)
                {
                    attType = 1;
                }
                else
                {
                    attType = 2;
                }
            }
            else
            {
                if (RandomUtils.Next(100) < 70)
                {
                    attType = 0;
                }
                else
                {
                    attType = 1;
                }
            }

            DelayedAction action = null;
            if (attType == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }

            //火蛇
            if (attType ==1)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                List<MapObject>  list = FindAllTargets(2, CurrentLocation, false);
                foreach(MapObject ob in list)
                {
                    if (!ob.IsAttackTarget(this)) continue;
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, ob, damage * 3 / 2, DefenceType.MAC);
                    ActionList.Add(action);
                }
                if (Distance > 2)
                {
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + 800, Target, damage * 3 / 2, DefenceType.MAC);
                    ActionList.Add(action);
                }
            }
            if (attType == 2)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1});
                action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
                //冰冻
                if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                {
                    Target.ApplyPoison(new Poison
                    {
                        Owner = this,
                        Duration = RandomUtils.Next(8, 15),
                        PType = PoisonType.Slow,
                        Value = damage / 3,
                        TickSpeed = 1000
                    }, this);
                    //几率麻痹
                    if (RandomUtils.Next(100) < 15)
                    {
                        Target.ApplyPoison(new Poison
                        {
                            Owner = this,
                            Duration = RandomUtils.Next(3, 5),
                            PType = PoisonType.Frozen,
                            Value = damage / 3,
                            TickSpeed = 1000
                        }, this);
                    }
                }
            }
     

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        protected override void ProcessAI()
        {
            //这里随机隐藏5-10秒，隐藏的时候恢复20%的血量
            if (!Dead && Envir.Time > VisibleTime)
            {
                VisibleTime = Envir.Time + 2000;

                bool visible = FindNearby(4);

                //显示出来
                if (!Visible && visible)
                {
                    Visible = true;
                    CellTime = Envir.Time + 500;
                    Broadcast(GetInfo());
                    Broadcast(new S.ObjectShow { ObjectID = ObjectID });
                    ActionTime = Envir.Time + 1000;
                    AttackTime = Envir.Time + 1000;
                }

                //隐藏起来
                if (Visible && HP < MaxHP / 2 && hideTime< hideCount)
                {
                    hideTime++;
                    Visible = false;
                    VisibleTime = Envir.Time + RandomUtils.Next(5, 15)*1000;

                    Broadcast(new S.ObjectHide { ObjectID = ObjectID });

                    SetHP(MaxHP*7/10);
                }
            }

            base.ProcessAI();
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

        public override bool IsAttackTarget(MonsterObject attacker)
        {
            return Visible && base.IsAttackTarget(attacker);
        }
        public override bool IsAttackTarget(PlayerObject attacker)
        {
            return Visible && base.IsAttackTarget(attacker);
        }

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
