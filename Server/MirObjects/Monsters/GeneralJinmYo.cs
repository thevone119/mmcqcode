using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  GeneralJinmYo 灵猫将军
    ///  4种攻击手段
    ///  1.横扫，身前1格，双重打击
    ///  2.重击，双倍攻击，几率眩晕
    ///  3.雷电，远程攻击，单体魔法伤害，2倍攻击
    ///  4.超级雷电，远程攻击单体魔法伤害，2.5倍攻击
    public class GeneralJinmYo : MonsterObject
    {
        public long FearTime;//狂暴的时间
        public long ProcessTime;//冷静时间
        private byte _stage;//0正常，1狂暴

        public byte AttackRange = 2;//攻击范围1格
        private byte attType;
        protected internal GeneralJinmYo(MonsterInfo info)
            : base(info)
        {

        }

        protected override bool InAttackRange()
        {
            byte _AttackRange = AttackRange;
            if (RandomUtils.Next(10) < 3)
            {
                _AttackRange = 1;
            }

            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, _AttackRange);
        }

        protected override void ProcessAI()
        {
            if(!Dead && Envir.Time > ProcessTime)
            {
                ProcessTime = Envir.Time + 2000;
                if (HP > MaxHP / 5)
                {
                    if (RandomUtils.Next(2) == 0)
                    {
                        AttackRange = 2;
                    }
                    if (RandomUtils.Next(5) == 0)
                    {
                        AttackRange = 10;
                    }
                    //计算是否狂暴
                    if (Envir.Time > FearTime)
                    {
                        if (RandomUtils.Next(25) == 0)
                        {
                            FearTime = Envir.Time + 10000;//10秒狂暴
                        }
                    }
                }
                else {
                    if (RandomUtils.Next(2) == 0)
                    {
                        AttackRange = 2;
                    }
                    if (RandomUtils.Next(2) == 0)
                    {
                        AttackRange = 12;
                    }
                    //计算是否狂暴
                    if (Envir.Time > FearTime)
                    {
                        if (RandomUtils.Next(7) == 0)
                        {
                            FearTime = Envir.Time + 10000;//10秒狂暴
                        }
                    }
                }
                byte stage=0;
                if (Envir.Time < FearTime)
                {
                    AttackSpeed = Info.AttackSpeed-500;
                    stage = 1;
                    if (RandomUtils.Next(5) == 0)
                    {
                        AttackRange = 12;
                    }
                }
                else
                {
                    AttackSpeed = Info.AttackSpeed;
                }
                if(stage!= _stage)
                {
                    _stage = stage;
                    Broadcast(GetInfo());
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
            int Distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            int delay = Distance * 50 + 750; //50 MS per Step
            int damage = GetAttackPower(MinDC, MaxDC);
            DelayedAction action = null;
            if (_stage == 1)
            {
                damage = damage * 3 / 2;
                if (RandomUtils.Next(2) == 0)
                {
                    attType = 1;
                }
                else
                {
                    attType = 3;
                }
                if (Distance > 2)
                {
                    attType = 3;
                }
            }
            else
            {
                switch (RandomUtils.Next(4))
                {
                    case 0:
                    case 1:
                        attType = 0;
                        break;
                    case 2:
                        attType = 1;
                        break;
                    case 3:
                        attType = 2;
                        break;
                }
                if (Distance > 2)
                {
                    attType = 2;
                }
            }



            switch (attType)
            {
                case 0://横扫，身前1格，双重打击
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage/2, DefenceType.ACAgility);
                    ActionList.Add(action);
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay+300, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    break;
                case 1://重击，双倍攻击，几率眩晕,如果是狂暴状态，几率麻痹
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    if (_stage == 1)
                    {
                        if (RandomUtils.Next(2) == 1)
                        {
                            Target.ApplyPoison(new Poison
                            {
                                Owner = this,
                                Duration = 5,
                                PType = PoisonType.Frozen,
                                Value = damage,
                                TickSpeed = 2000
                            }, this);
                        }
                    }
                    else
                    {
                        if (RandomUtils.Next(3) == 1)
                        {
                            Target.ApplyPoison(new Poison
                            {
                                Owner = this,
                                Duration = 8,
                                PType = PoisonType.Stun,
                                Value = damage,
                                TickSpeed = 2000
                            }, this);
                        }
                    }
                    break;
         
                case 2://雷电，远程攻击，群体魔法伤害，2次伤害攻击
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                    List<MapObject> list = CurrentMap.getMapObjects(Target.CurrentLocation.X, Target.CurrentLocation.Y, 2);
                    for (int o = 0; o < list.Count; o++)
                    {
                        MapObject ob = list[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MAC);
                        ActionList.Add(action);
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay+300, ob, damage, DefenceType.MAC);
                        ActionList.Add(action);
                    }
                    break;
                case 3://狂暴雷电，远程攻击，群体魔法伤害，2次1.4倍伤害攻击
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1 });
                    List<MapObject> list2 = CurrentMap.getMapObjects(Target.CurrentLocation.X, Target.CurrentLocation.Y, 2);
                    for (int o = 0; o < list2.Count; o++)
                    {
                        MapObject ob = list2[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MAC);
                        ActionList.Add(action);
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay + 300, ob, damage, DefenceType.MAC);
                        ActionList.Add(action);
                    }
                    //Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 1 });
                    break;
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        //狂暴的时候，重新发送这个
        //恢复正常，也重新发送这个
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
