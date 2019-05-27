using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  红花仙子（5种攻击手段）
    ///  1.近身攻击
    ///  2.抖花粉 2
    ///  3.净化
    ///  4远程1
    ///  5远程2 冰冻
    /// </summary>
    public class Monster409 : MonsterObject
    {
        private byte attType;
        protected internal Monster409(MonsterInfo info)
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
            int delay = distance * 50 + 550; //50 MS per Step
            int rd = RandomUtils.Next(100);
            //
            if (distance > 2)
            {
                if (rd < 60)
                {
                    attType = 3;
                }
                else
                {
                    attType = 4;
                }
            }
            else
            {
                if (rd < 40)
                {
                    attType = 1;
                }
                else if(rd < 70)
                {
                    attType = 3;
                }
                else
                {
                    attType = 4;
                }
            }

            if (distance == 1 && RandomUtils.Next(3) == 0)
            {
                attType = 0;
            }
            //如果中毒了，几率解毒
            if (PoisonList != null && PoisonList.Count > 0 && RandomUtils.Next(5) == 0)
            {
                attType = 2;
            }

            //如果血量过少，则加血哦
            if (HP<MaxHP/3 && RandomUtils.Next(5) == 0)
            {
                attType = 2;
            }

            DelayedAction action = null;
            List<MapObject> listtargets;
            switch (attType)
            {
                case 0:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    break;
                case 1:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    //抖花粉 封印技能
                    listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 2);
                    for (int o = 0; o < listtargets.Count; o++)
                    {
                        MapObject ob = listtargets[o];
                        if (ob.Race == ObjectType.Monster || ob.Race == ObjectType.Player)
                        {
                            if (!ob.IsAttackTarget(this)) continue;
                            action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MAC);
                            ActionList.Add(action);
                            if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist)
                            {
                                ob.ApplyPoison(new Poison
                                {
                                    Owner = this,
                                    Duration = RandomUtils.Next(5, 15),
                                    PType = PoisonType.Stun,
                                    Value = damage ,
                                    TickSpeed = 1000
                                }, this);
                            }
                        }
                    }
                    break;
                case 2:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                    //血量恢复
                    ChangeHP(damage * 3);
                    //解除中毒
                    PoisonList.Clear();
                    break;
                case 3:
                    //放追踪技能
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                    ActionList.Add(action);
                    break;
                case 4:
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1 });
                    listtargets = CurrentMap.getMapObjects(Target.CurrentLocation.X, Target.CurrentLocation.Y, 2);
                    for (int o = 0; o < listtargets.Count; o++)
                    {
                        MapObject ob = listtargets[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage * 2 / 3, DefenceType.MAC);
                        ActionList.Add(action);
                        if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist && RandomUtils.Next(100)<50)
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
                    break;
            }


            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
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



    }
}
