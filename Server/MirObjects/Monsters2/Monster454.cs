using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  毒妖女皇 毒妖林BOSS
    ///  1.普通攻击
    ///  2.脉冲，范围麻痹
    ///  3.撒花,放鬼头
    ///  4.无敌，护盾
    ///  5.闪现，瞬移
    ///  6.远程单体雷电，禁魔
    /// </summary>
    public class Monster454 : MonsterObject
    {

        private byte atype = 1;
        private byte _TeleportTime = 0;
        private byte MaxTeleportTime = 1;
        private byte _stage = 0;
        private long PoisonTime = 0;//可被中毒的时间
        private long protectTime = 0;//护盾的时间
        private long ProcessTime = 0;//AI计算的时间
        

        //攻击的次数,每10次攻击放1次鬼头
        private long _actime = 0;


        //是否可以活动
        private bool _canMove = false;
        private long _checkMoveTime = 0;//AI计算的时间
        private string preMonName = "巨斧妖";


        protected internal Monster454(MonsterInfo info)
            : base(info)
        {
            //MaxTeleportTime = (byte)RandomUtils.Next(1, 3);
        }


        private void checkCanMove()
        {
            if (!Dead && Envir.Time > _checkMoveTime)
            {
                _checkMoveTime = Envir.Time + 5000;
                bool has = false;
                foreach(MapObject ob in SMain.Envir.Objects)
                {
                    if (ob == null || ob.Dead || ob.CurrentMap != CurrentMap || ob.Race != ObjectType.Monster)
                    {
                        continue;
                    }
                    if (preMonName.Equals(ob.Name))
                    {
                        has = true;
                        break;
                    }
                }
                if (has)
                {
                    _canMove = false;
                    MoveTime = Envir.Time + 5200;
                    AttackTime = Envir.Time + 5200;
                }
                else
                {
                    _canMove = true;
                }
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
            if (_stage == 1)
            {
                damage = damage * 13 / 10;
            }
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            int delay = distance * 50 + 500; //50 MS per Step
            DelayedAction action = null;
            _actime++;
            if (_actime > 1000)
            {
                _actime = 1;
            }
            byte p_atype = atype;//上一次攻击的类型

            atype = 0;
            if (distance <= 2 )
            {
                if (RandomUtils.Next(100) < 70)
                {
                    atype = 1;
                }
                else
                {
                    atype = 2;
                }
            }
            else
            {
                atype = 6;
            }


            //护盾
            if(this.HP<this.MaxHP*3/5 && RandomUtils.Next(100) < 20 && Envir.Time > protectTime)
            {
                atype = 4;
            }

            //瞬移
            if (this.HP < this.MaxHP / 5 && RandomUtils.Next(100) < 30 && Envir.Time > protectTime && _TeleportTime < MaxTeleportTime)
            {
                atype = 5;
            }


            //放完鬼头后，下次攻击放麻痹或者减速
            if (_actime % 10 == 0)
            {
                atype = 3;
            }
            if (p_atype == 3)
            {
                if (distance > 3)
                {
                    atype = 6;
                }
                else
                {
                    atype = 2;
                }
            }


            //1.普通攻击
            if (atype == 1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            //2.脉冲，范围麻痹
            if (atype == 2)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                Point movepoint = Functions.PointMove(CurrentLocation, Direction, 2);
               
                List<MapObject> listtargets = CurrentMap.getMapObjects(movepoint.X, movepoint.Y, 2);
                for (int o = 0; o < listtargets.Count; o++)
                {
                    MapObject ob = listtargets[o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;

                    if (ob.Race == ObjectType.Monster)
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage*2, DefenceType.MAC);
                        ActionList.Add(action);
                    }
                    else
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MAC);
                        ActionList.Add(action);
                    }
                   
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist && RandomUtils.Next(100) < 60)
                    {
                        ob.ApplyPoison(new Poison
                        {
                            Owner = this,
                            Duration = RandomUtils.Next(4, 8),
                            PType = PoisonType.Frozen,
                            Value = damage,
                            TickSpeed = 1000
                        }, this);
                    }
                }
            }
            //3.撒花,放鬼头
            if (atype == 3)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                if (RandomUtils.Next(100) < 50)
                {
                    //放火圈
                    List<Point> list = new List<Point>();
                    list.Add(new Point(CurrentLocation.X, CurrentLocation.Y + 5));
                    list.Add(new Point(CurrentLocation.X, CurrentLocation.Y - 5));
                    list.Add(new Point(CurrentLocation.X + 5, CurrentLocation.Y + 5));
                    list.Add(new Point(CurrentLocation.X + 5, CurrentLocation.Y));
                    list.Add(new Point(CurrentLocation.X + 5, CurrentLocation.Y - 5));
                    list.Add(new Point(CurrentLocation.X - 5, CurrentLocation.Y - 5));
                    list.Add(new Point(CurrentLocation.X - 5, CurrentLocation.Y));
                    list.Add(new Point(CurrentLocation.X - 5, CurrentLocation.Y + 5));

                    //一次放6个鬼头，随机放，如果重复了，就不放
                    for (int i = 0; i < list.Count; i++)
                    {
                        Point p = list[i];
                        if (!CurrentMap.Valid(p.X, p.Y))
                        {
                            continue;
                        }
                        SpellObject spellObj = new SpellObject
                        {
                            Spell = Spell.MonGhostHead,
                            Value = damage * 3,
                            ExpireTime = Envir.Time + 15000,
                            TickSpeed = 3000,
                            Caster = null,
                            MonCaster = this,
                            CurrentLocation = p,
                            CurrentMap = CurrentMap,
                            Direction = MirDirection.Up
                        };
                        action = new DelayedAction(DelayedType.Spawn, Envir.Time + 500, spellObj);
                        CurrentMap.ActionList.Add(action);
                    }
                }
                else
                {
                    int rcount = RandomUtils.Next(5, 10);
                    //一次放3-6个毒，随机放，如果重复了，就不放
                    for (int i = 0; i < rcount; i++)
                    {
                        Point p = CurrentMap.RandomValidPoint(CurrentLocation.X, CurrentLocation.Y, 6, 1);
                        //随机抽取5个点，放毒
                        SpellObject spellObj = new SpellObject
                        {
                            Spell = Spell.MonGhostHead,
                            Value = damage * 2,
                            ExpireTime = Envir.Time + 15000,
                            TickSpeed = 3000,
                            Caster = null,
                            MonCaster = this,
                            CurrentLocation = p,
                            CurrentMap = CurrentMap,
                            Direction = MirDirection.Up
                        };
                        action = new DelayedAction(DelayedType.Spawn, Envir.Time + 500, spellObj);
                        CurrentMap.ActionList.Add(action);
                    }
                }
            }
            //4.无敌，护盾
            if (atype == 4)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 3 });
                protectTime = Envir.Time + 1000 * RandomUtils.Next(15, 30);
                PoisonTime = Envir.Time + 1000 * RandomUtils.Next(5, 12);
                PoisonList.Clear();
                ProcessTime = Envir.Time + 500;
            }

            //5.闪现，瞬移
            if (atype == 5)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 4 });
                PoisonList.Clear();
                action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 700, Target, damage , DefenceType.MACAgility,5);
                ActionList.Add(action);
                _TeleportTime++;
            }

            //6.远程单体雷电，减速
            if (atype == 6)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MACAgility);
                ActionList.Add(action);
                if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist && RandomUtils.Next(100) < 70)
                {
                    Target.ApplyPoison(new Poison { Owner = this, Duration = RandomUtils.Next(6, 16), PType = PoisonType.Slow, Value = damage / 2, TickSpeed = 2000 }, this);
                }
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
            int _at = (int)data[3];
            //5.闪现，瞬移
            if (_at == 5)
            {
                //10-100之间，随机闪现
                Point p = CurrentMap.RandomValidPoint(CurrentLocation.X, CurrentLocation.Y, 100, 10);
                Teleport(CurrentMap, p, false, 2);
                return;
            }

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
            target.Attacked(this, damage, defence);
        }

        protected override void ProcessAI()
        {
            checkCanMove();
            //狂暴计算
            if (!Dead && Envir.Time > ProcessTime)
            {
                ProcessTime = Envir.Time + 1000;

                byte __stage = _stage;
                if (Envir.Time > protectTime)
                {
                    _stage = 0;
                }
                else
                {
                    _stage = 1;
                }

                if (__stage != _stage)
                {
                    Broadcast(GetInfo());
                    ProcessTime = Envir.Time + 5000;
                }
            }
            base.ProcessAI();
        }


        public override int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility)
        {
            if (!_canMove)
            {
                return 0;
            }
            if (_stage == 0)
            {
                return base.Attacked(attacker, damage, type);
            }
            else
            {
                return base.Attacked(attacker, damage/2, type);
            }
        }
        public override int Attacked(PlayerObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            if (!_canMove)
            {
                return 0;
            }

            if (_stage == 0)
            {
                return base.Attacked(attacker, damage, type, damageWeapon);
            }
            else
            {
                return base.Attacked(attacker, damage/2, type, damageWeapon);
            }
        }

        //有护盾的时候无法中毒
        public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true)
        {
            if (!_canMove)
            {
                return;
            }
            if (Envir.Time > PoisonTime)
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
