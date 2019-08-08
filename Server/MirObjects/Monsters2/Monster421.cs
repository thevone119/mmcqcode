using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  冰雪女皇
    ///  3种攻击手段
    /// </summary>
    public class Monster421 : MonsterObject
    {
        private byte AttackType;
        private long ProcessTime;
        private int _AttackSpeed;
        private ushort _MoveSpeed;
        private byte _stage = 0;//0:正常 1：狂暴

        protected internal Monster421(MonsterInfo info)
            : base(info)
        {

        }


        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, 2);
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
            if (_stage == 1)
            {
                damage = damage * 3 / 2;
            }
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            int delay = distance * 50 + 500; //50 MS per Step
            DelayedAction action = null;

            byte _AttackType = AttackType;
            int rd = RandomUtils.Next(100);
            if (rd < 60)
            {
                AttackType = 0;
            }else if(rd < 80)
            {
                AttackType = 1;
            }
            else
            {
                AttackType = 2;
            }
            //狂暴状态增加这个的几率
            if (_AttackType!=1 && _stage == 1&& RandomUtils.Next(100)<30)
            {
                AttackType = 1;
            }

            if (_AttackType == 1 && RandomUtils.Next(100) < 60)
            {
                AttackType = 2;
            }
        

            switch (AttackType)
            {
                case 0:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction,  Location = CurrentLocation, Type = 0 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage*3/2, DefenceType.ACAgility);
                    ActionList.Add(action);
                    break;

                case 1:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                   
                    List<MapObject> list = CurrentMap.getMapObjects(Target.CurrentLocation.X, Target.CurrentLocation.Y, 2);
                    foreach(MapObject ob in list)
                    {
                        if (!ob.IsAttackTarget(this))
                        {
                            continue;
                        }
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MACAgility);
                        ActionList.Add(action);
                        if(ob.Race== ObjectType.Player)
                        {
                            //范围冰冻
                            if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                            {
                                Target.ApplyPoison(new Poison
                                {
                                    Owner = this,
                                    Duration = RandomUtils.Next(3, 5),
                                    PType = PoisonType.Frozen,
                                    Value = damage,
                                    TickSpeed = 1000
                                }, this);
                            }
                        }
                        else
                        {
                            //范围冰冻
                            if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                            {
                                Target.ApplyPoison(new Poison
                                {
                                    Owner = this,
                                    Duration = RandomUtils.Next(8, 20),
                                    PType = PoisonType.Frozen,
                                    Value = damage,
                                    TickSpeed = 1000
                                }, this);
                            }
                        }
                    }
                    break;
                case 2:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                    action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 500, Target, damage * 3 / 2, DefenceType.MACAgility);
                    ActionList.Add(action);
                    action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 800, Target, damage*2, DefenceType.MACAgility);
                    ActionList.Add(action);
                    break;

            }
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        protected override void CompleteRangeAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
            //超过2格，打不到
            int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);
            if (distance > 2)
            {
                return;
            }
            //方向不对，打不到
            MirDirection dir = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            if(dir!= Direction)
            {
                return;
            }

            target.Attacked(this, damage, defence);
        }

        protected override void ProcessAI()
        {
            //狂暴计算
            if (!Dead && Envir.Time > ProcessTime)
            {
                ProcessTime = Envir.Time + 2000;
                if (_AttackSpeed == 0)
                {
                    _AttackSpeed = this.AttackSpeed;
                }
                if (_MoveSpeed == 0)
                {
                    _MoveSpeed = this.MoveSpeed;
                }
                byte __stage = _stage;
                if (HP < MaxHP / 4)
                {
                    _stage = 1;
                }
                else
                {
                    if (_stage == 0)
                    {
                        if (RandomUtils.Next(100) < 10)
                        {
                            _stage = 1;
                        }
                    }
                    else
                    {
                        if (RandomUtils.Next(100) < 50)
                        {
                            _stage = 0;
                        }
                    }
                }
                if (_stage == 1)
                {
                    AttackSpeed = _AttackSpeed * 2 / 3;
                    MoveSpeed = (ushort)(_MoveSpeed * 2 / 3);
                }
                else
                {
                    AttackSpeed = _AttackSpeed;
                    MoveSpeed = _MoveSpeed;
                }

                if(__stage != _stage)
                {
                    Broadcast(GetInfo());
                    ProcessTime = Envir.Time + 5000;
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
            MoveTo(Target.CurrentLocation);
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
