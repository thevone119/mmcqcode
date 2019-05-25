using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    /// 攻击力设置 200-400
    ///  水晶兽 冰雪守护神
    ///  1.摆尾
    ///  2.拍击
    ///  3.旋转（2次攻击，秒杀）
    ///  
    ///  4.方向放冰球
    ///  5.拍地板，冰冻，范围伤害
    ///  6.冰雨，2次伤害，秒杀
    /// </summary>
    public class CrystalBeast : MonsterObject
    {
        private byte attType;
        protected internal CrystalBeast(MonsterInfo info)
            : base(info)
        {

        }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, 4);
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
            int rd = RandomUtils.Next(100);
            //攻击力
            if (HP < MaxHP / 2)
            {
                damage = damage * 3 / 2;
            }

            if(HP < MaxHP / 3)
            {
                //攻击距离
                if (distance < 3)
                {
                    if (rd < 30)
                    {
                        attType = 0;
                    }
                    else if (rd < 50)
                    {
                        attType = 1;
                    }
                    else if (rd < 70)
                    {
                        attType = 2;
                    }
                    else if (rd < 80)
                    {
                        attType = 3;
                    }
                    else if (rd < 95)
                    {
                        attType = 4;
                    }
                    else
                    {
                        attType = 5;
                    }
                }
                else
                {
                    if (rd < 50)
                    {
                        attType = 3;
                    }
                    else if (rd < 80)
                    {
                        attType = 4;
                    }
                    else 
                    {
                        attType = 5;
                    }
                }
            }
            else
            {
                //攻击距离
                if (distance < 3)
                {
                    if (rd < 10)
                    {
                        attType = 0;
                    }
                    else if (rd < 20)
                    {
                        attType = 1;
                    }
                    else if (rd < 40)
                    {
                        attType = 2;
                    }
                    else if (rd < 60)
                    {
                        attType = 3;
                    }
                    else if (rd < 85)
                    {
                        attType = 4;
                    }
                    else
                    {
                        attType = 5;
                    }
                }
                else
                {
                    if (rd < 30)
                    {
                        attType = 3;
                    }
                    else if (rd < 60)
                    {
                        attType = 4;
                    }
                    else
                    {
                        attType = 5;
                    }
                }
            }
            DelayedAction action = null;
            List<MapObject> targets = null;
            switch (attType)
            {
                case 0:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    break;
                case 1:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.AC);
                    ActionList.Add(action);
                    break;
                case 2:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.AC);
                    ActionList.Add(action);
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay+300, Target, damage, DefenceType.AC);
                    ActionList.Add(action);
                    break;

                case 3:
                    //这个是方向攻击
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                    targets = FindAllTargets(4, CurrentLocation, false);
                    foreach(MapObject ob in targets)
                    {
                        action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + delay , ob, damage, DefenceType.MACAgility);
                        ActionList.Add(action);
                    }
                    break;

                case 4:
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1 });
                    targets = FindAllTargets(4, CurrentLocation, false);
                    foreach (MapObject ob in targets)
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MACAgility);
                        ActionList.Add(action);
                        //冰冻
                        if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                        {
                            ob.ApplyPoison(new Poison
                            {
                                Owner = this,
                                Duration = RandomUtils.Next(4, 7),
                                PType = PoisonType.Frozen,
                                Value = damage ,
                                TickSpeed = 1000
                            }, this);
                        }
                    }
                    break;

                case 5:
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 2 });
                    targets = FindAllTargets(4, CurrentLocation, false);
                    foreach (MapObject ob in targets)
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MACAgility);
                        ActionList.Add(action);
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay+300, ob, damage, DefenceType.MAC);
                        ActionList.Add(action);
                    }
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
            //判断方向是否正确
            if(Functions.DirectionFromPoint(CurrentLocation, target.CurrentLocation)!= Direction)
            {
                return;
            }
  
            bool isdir = false;
            int x = Math.Abs(target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(target.CurrentLocation.Y - CurrentLocation.Y);
            if (x > 4 || y > 4)
            {
                return;
            }
      
            if ((x == 0) || (y == 0) || (x == y))
            {
                isdir = true;
            }
            if (!isdir)
            {
                return;
            }

            
            if (target.Attacked(this, damage, defence) <= 0) return;
            //减速
            if (RandomUtils.Next(Settings.PoisonResistWeight) >= target.PoisonResist)
            {
                target.ApplyPoison(new Poison
                {
                    Owner = this,
                    Duration = RandomUtils.Next(7, 20),
                    PType = PoisonType.Slow,
                    Value = damage,
                    TickSpeed = 1000
                }, this);
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

            MoveTo(Target.CurrentLocation);
        }

    }
}
