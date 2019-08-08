using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  Monster429 昆仑道士 5种攻击手段
    ///  1.普通攻击
    ///  2.加buf
    ///  3.普通符
    ///  4.减速符
    ///  5.高攻符
    /// </summary>
    public class Monster429 : MonsterObject
    {

        private byte _stage = 0;//0:没有护盾 1：有护盾
        private int AttackedCount = 0;//被攻击次数（超过10下，护盾就碎了）
        private long fireTime = 0;
        private long ProcessTime = 0;
        

        protected internal Monster429(MonsterInfo info)
            : base(info)
        {
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
            int rd = RandomUtils.Next(100);
            byte attackType = 1;
            if (_stage == 1)
            {
                damage = damage * 3 / 2;
                if (rd<65 && distance < 2)
                {
                    attackType = 1;
                }
                else
                {
                    attackType = 5;
                }
            }
            else
            {
                if (rd < 65 && distance < 2)
                {
                    attackType = 1;
                }
                else
                {
                    if (rd < 70)
                    {
                        attackType = 3;
                    }else if (rd < 85)
                    {
                        attackType = 2;
                    }
                    else
                    {
                        attackType = 4;
                    }
                }
            }

            ///  1.普通攻击
            ///  2.加buf
            ///  3.普通符
            ///  4.减速符
            ///  5.高攻符
            if (attackType==1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            if (attackType == 2)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                _stage = 1;
                AttackedCount = RandomUtils.Next(3);
                fireTime = Envir.Time + RandomUtils.Next(10, 20) * 1000;
                Broadcast(GetInfo());
            }
            if (attackType == 3)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MACAgility);
                ActionList.Add(action);
            }
            if (attackType == 4)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
                if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist && RandomUtils.Next(100)<85)
                {
                    Target.ApplyPoison(new Poison { Owner = this, Duration = RandomUtils.Next(6, 12), PType = PoisonType.Slow, Value = damage / 8, TickSpeed = 2000 }, this);
                }
            }
            if (attackType == 5)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 2 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay+300, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        public override int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility)
        {
            if (_stage == 1)
            {
                damage = damage / 2;
                AttackedCount++;
            }
            return base.Attacked(attacker, damage, type);
        }

        public override int Attacked(PlayerObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            if (_stage == 1)
            {
                damage = damage / 2;
                AttackedCount++;
            }
            return base.Attacked(attacker, damage, type, damageWeapon);
        }


        //有护盾的时候无法中毒
        public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true)
        {
            if (_stage == 0)
            {
                base.ApplyPoison(p, Caster, NoResist, ignoreDefence);
            }
        }

        protected override void ProcessAI()
        {
            //狂暴计算
            if (!Dead && Envir.Time > ProcessTime)
            {
                ProcessTime = Envir.Time + 1000;
                byte __stage = _stage;
                if (AttackedCount >= 10)
                {
                    AttackedCount = 0;
                    _stage = 0;
                }
                if(Envir.Time > fireTime)
                {
                    _stage = 0;
                }
               
                if (__stage != _stage)
                {
                    Broadcast(GetInfo());
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

            if (!CanMove || Target == null) return;
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            if (distance > 6)
            {
                MoveTo(Target.CurrentLocation);
                return;
            }


            //40几率跑路
            if (RandomUtils.Next(100) < 60)
            {
                return;
            }
            MirDirection dir = Functions.DirectionFromPoint(Target.CurrentLocation, CurrentLocation);

            if (Walk(dir)) return;

            switch (RandomUtils.Next(2)) //No favour
            {
                case 0:
                    for (int i = 0; i < 7; i++)
                    {
                        dir = Functions.NextDir(dir);

                        if (Walk(dir))
                            return;
                    }
                    break;
                default:
                    for (int i = 0; i < 7; i++)
                    {
                        dir = Functions.PreviousDir(dir);

                        if (Walk(dir))
                            return;
                    }
                    break;
            }
        }

        public override Packet GetInfo()
        {
            return new S.ObjectMonster
            {
                ObjectID = ObjectID,
                Name = Name,
                NameColour = NameColour,
                Location = CurrentLocation,
                Image = Info.Image,
                Direction = Direction,
                Effect = Info.Effect,
                AI = Info.AI,
                Light = Info.Light,
                Dead = Dead,
                Skeleton = Harvested,
                Poison = CurrentPoison,
                Hidden = Hidden,
                ExtraByte = _stage,
            };
        }

    }
}
