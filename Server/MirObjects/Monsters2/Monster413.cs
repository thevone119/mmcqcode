using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  Monster413 冰宫护法
    ///  3种攻击手段
    ///  0 Attack1.普通攻击 
    ///  1 Attack2.砸地板 
    ///  2 AttackRange1 远程单点冰冻
    ///  3 Attack3：恢复护盾
    ///  4 Attack4：护盾破灭
    /// </summary>
    public class Monster413 : MonsterObject
    {

        private byte _stage = 0;//0:没有护盾 1：有护盾

        private int AttackedCount = 0;//被攻击次数（超过10下，护盾就碎了）
        private byte AttackType;
        protected internal Monster413(MonsterInfo info)
            : base(info)
        {
            _stage = 1;
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
            AttackType = 0;
            if (_stage == 0)
            {
                if (PoisonList.Count > 0)
                {
                    if (RandomUtils.Next(100) < 20)
                    {
                        AttackType = 3;
                    }
                }
                else
                {
                    if (RandomUtils.Next(100) < 10)
                    {
                        AttackType = 3;
                    }
                }
            }
            else
            {
                if (AttackedCount > 10)
                {
                    AttackedCount = 0;
                    AttackType = 4;
                }
            }

            if (AttackType == 0)
            {
                if (distance > 1)
                {
                    AttackType = 2;
                }
                else
                {
                    int rd = RandomUtils.Next(100);
                    if (rd < 50)
                    {
                        AttackType = 0;
                    }
                    else if(rd<80)
                    {
                        AttackType = 1;
                    }
                    else
                    {
                        AttackType = 2;
                    }
                }
            }
            switch (AttackType)
            {
                case 0:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    break;
                case 1:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage*2, DefenceType.AC);
                    ActionList.Add(action);
                    break;
                case 2:
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MACAgility);
                    ActionList.Add(action);
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist && RandomUtils.Next(100) < 50)
                    {
                        Target.ApplyPoison(new Poison
                        {
                            Owner = this,
                            Duration = RandomUtils.Next(5, 10),
                            PType = PoisonType.Slow,
                            Value = damage,
                            TickSpeed = 1000
                        }, this);
                    }
                    break;

                case 3:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                    _stage = 1;
                    Broadcast(GetInfo());
                    //清除状态
                    PoisonList.Clear();
                    //血量恢复
                    ChangeHP(damage * 3);
                    break;
                case 4:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 3 });
                    List<MapObject>  listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 3);
                    for (int o = 0; o < listtargets.Count; o++)
                    {
                        MapObject ob = listtargets[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage * 2, DefenceType.MAC);
                        ActionList.Add(action);
                    }
                    _stage = 0;
                    Broadcast(GetInfo());
                    break;
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
            }
            AttackedCount++;
            return base.Attacked(attacker, damage, type);
        }

        public override int Attacked(PlayerObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            if (_stage == 1)
            {
                damage = damage / 2;
            }
            AttackedCount++;
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
            if (distance > 5)
            {
                MoveTo(Target.CurrentLocation);
                return;
            }

            //血多就跑向玩家，血少就跑路
            if (this.HP > this.MaxHP / 2)
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
