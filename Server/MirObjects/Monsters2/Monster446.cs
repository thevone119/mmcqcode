using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  Monster446 昆仑终极BOSS
    ///  1.普通攻击
    ///  2.放鬼头
    ///  3.举刀 解毒，强化，放火圈
    ///  4.砸地板
    ///  5.变身，加强
    ///  6.小火球（r1）
    ///  7.大火球(r2)
    /// </summary>
    public class Monster446 : MonsterObject
    {
        private byte _stage = 0;//0:正常 1：毒免
        private long fireTime = 0;//放鬼头，放火圈的时间，间隔不能小于3秒
        private long ProcessTime = 0;
        private long PoisonTime = 0;//可被中毒的时间

        protected internal Monster446(MonsterInfo info)
            : base(info)
        {

        }



        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, 10);
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
            byte attacktype = 1;
            if (_stage == 0)
            {
                if (rd < 50)
                {
                    attacktype = 1;
                    if (distance >= 2)
                    {
                        attacktype = 6;
                    }
                }
                else if (rd < 80)
                {
                    attacktype = 6;
                }
                else
                {
                    if (Envir.Time > fireTime)
                    {
                        attacktype = 2;
                        fireTime = Envir.Time + 4000;
                    }
                    else
                    {
                        attacktype = 6;
                    }
                }
            }
            else
            {
                damage = damage * 3 / 2;
                if (rd < 40)
                {
                    attacktype = 4;
                    if (distance >= 3)
                    {
                        attacktype = 7;
                    }
                }
                else if (rd < 80)
                {
                    attacktype = 7;
                }
                else
                {
                    if (Envir.Time > fireTime)
                    {
                        attacktype = 3;
                        fireTime = Envir.Time + 12000;
                    }
                    else
                    {
                        attacktype = 7;
                    }
                }
            }
   
            //普通攻击
            if (attacktype == 1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            //2.放鬼头
            if (attacktype == 2)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                if (distance < 3)
                {
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                }
                
                int rcount = RandomUtils.Next(6,12);
                //一次放6个鬼头，随机放，如果重复了，就不放
                for (int i = 0; i < rcount; i++)
                {
                    Point p = CurrentMap.RandomValidPoint(CurrentLocation.X, CurrentLocation.Y, 6, 1);
                    //随机抽取5个点，放鬼头
                    SpellObject spellObj = new SpellObject
                    {
                        Spell = Spell.MonKITO,
                        Value = damage*3/2,
                        ExpireTime = Envir.Time + 5000,
                        TickSpeed = 2500,
                        Caster = null,
                        MonCaster = this,
                        CurrentLocation = p,
                        CurrentMap = CurrentMap,
                        Direction = MirDirection.Up
                    };
                    action = new DelayedAction(DelayedType.Spawn, Envir.Time +500, spellObj);
                    CurrentMap.ActionList.Add(action);
                }
            }
            //3.举刀 解毒，强化，放火圈
            if (attacktype == 3)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                //血量恢复
                ChangeHP(damage);
                //解除中毒
                PoisonList.Clear();
                PoisonTime = Envir.Time + RandomUtils.Next(5, 10) * 1000;
                //放火圈
                List<Point> list = new List<Point>();
                list.Add(new Point(CurrentLocation.X , CurrentLocation.Y+ 5));
                list.Add(new Point(CurrentLocation.X, CurrentLocation.Y - 5));
                list.Add(new Point(CurrentLocation.X + 5, CurrentLocation.Y + 5));
                list.Add(new Point(CurrentLocation.X + 5, CurrentLocation.Y));
                list.Add(new Point(CurrentLocation.X + 5, CurrentLocation.Y - 5));
                list.Add(new Point(CurrentLocation.X - 5, CurrentLocation.Y - 5));
                list.Add(new Point(CurrentLocation.X - 5, CurrentLocation.Y ));
                list.Add(new Point(CurrentLocation.X - 5, CurrentLocation.Y + 5));
 
                //一次放6个鬼头，随机放，如果重复了，就不放
                for (int i = 0; i < list.Count; i++)
                {
                    Point p = list[i];
                    if (!CurrentMap.Valid(p.X,p.Y))
                    {
                        continue;
                    }
                    SpellObject spellObj = new SpellObject
                    {
                        Spell = Spell.MonFireCircle,
                        Value = damage * 3 / 2,
                        ExpireTime = Envir.Time + 12000,
                        TickSpeed = 2000,
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
            //4.砸地板
            if (attacktype == 4)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 3 });
                List<MapObject> listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 3);
                for (int o = 0; o < listtargets.Count; o++)
                {
                    MapObject ob = listtargets[o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;
                    if(ob.Race == ObjectType.Monster)
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage * 2, DefenceType.MAC);
                    }
                    else
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MAC);
                    }
                    ActionList.Add(action);
                }
            }
            //6.小火球（r1）
            if (attacktype == 6)
            {
                damage += damage * distance / 30;
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
            }
            //7.大火球(r2)
            if (attacktype == 7)
            {
                damage += damage * distance / 30;
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1 });
                if (Target.Race == ObjectType.Monster)
                {
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage * 2, DefenceType.MAC);
                }
                else
                {
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                }
                ActionList.Add(action);
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        



        protected override void ProcessAI()
        {
            //狂暴计算
            if (!Dead && Envir.Time > ProcessTime && _stage==0)
            {
                ProcessTime = Envir.Time + 1000;
                if (HP < MaxHP/2 && _stage == 0)
                {
                    _stage = 1;
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 4 });
                    //Broadcast(GetInfo());
                    //延迟发送这个形态变幻
                    DelayedAction action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 500, Target, 100, DefenceType.MAC);
                    ActionList.Add(action);
                    AttackTime = Envir.Time + 2000;
                }
            }
            base.ProcessAI();
        }

        //有护盾的时候无法中毒
        public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true)
        {
            if (Envir.Time > PoisonTime)
            {
                base.ApplyPoison(p, Caster, NoResist, ignoreDefence);
            }
        }

        protected override void CompleteRangeAttack(IList<object> data)
        {
            Broadcast(GetInfo());
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
