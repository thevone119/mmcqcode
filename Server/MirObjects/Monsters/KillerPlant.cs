using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  KillerPlant 黑暗船长
    ///  5种攻击手段
    ///  0.挥刀砍，范围伤害
    ///  1.攻击2解毒，加血
    ///  2.踏地板，眩晕，麻痹
    ///  
    ///  3.放雷电，追踪雷电
    ///  4.放雷电，满天的雷电
    public class KillerPlant : MonsterObject
    {

        public byte AttackRange = 10;//攻击范围10格
        private byte attType;
        protected internal KillerPlant(MonsterInfo info)
            : base(info)
        {

        }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
        }

        protected override void ProcessAI()
        {
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
            //基础伤害200-400;
            //血少后伤害增加
            if(HP < MaxHP / 4)
            {
                damage = damage * 2;
            }
            else if (HP < MaxHP / 2)
            {
                damage = damage *3/2;
            }
            attType = 0;
            //如果中毒了，几率解毒
            if (PoisonList != null && PoisonList.Count > 0 && RandomUtils.Next(5)==0)
            {
                attType = 1;
            }
            if(attType==0 && Distance > 3)
            {
                attType = 3;
            }
            if (attType == 0 && Distance == 3)
            {
                attType = 4;
            }
            if (attType == 0 && Distance < 3)
            {
                int rd = RandomUtils.Next(100);
                if (rd < 40)
                {
                    attType = 0;
                }else if (rd < 70)
                {
                    attType = 4;
                }
                else
                {
                    attType = 2;
                }
            }

            DelayedAction action = null;
            List<MapObject> listtargets;
            switch (attType)
            {
                case 0:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage , DefenceType.ACAgility);
                    ActionList.Add(action);
                    break;
                case 1:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    //血量恢复
                    ChangeHP(damage *3);
                    //解除中毒
                    PoisonList.Clear();
                    break;
                case 2:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                    //踩踏，眩晕，麻痹
                    listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 2);
                    for (int o = 0; o < listtargets.Count; o++)
                    {
                        MapObject ob = listtargets[o];
                        if (ob.Race == ObjectType.Monster || ob.Race == ObjectType.Player)
                        {
                            if (!ob.IsAttackTarget(this)) continue;
                            action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.AC);
                            ActionList.Add(action);
                            if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist)
                            {
                                if (RandomUtils.Next(3) == 1)
                                {
                                    ob.ApplyPoison(new Poison
                                    {
                                        Owner = this,
                                        Duration = RandomUtils.Next(3,7),
                                        PType = PoisonType.Paralysis,
                                        Value = damage/3,
                                        TickSpeed = 1000
                                    }, this);
                                }
                                else
                                {
                                    ob.ApplyPoison(new Poison
                                    {
                                        Owner = this,
                                        Duration = RandomUtils.Next(5, 15),
                                        PType = PoisonType.Stun,
                                        Value = damage / 3,
                                        TickSpeed = 1000
                                    }, this);
                                }
                            }
                        }
                    }
                    break;
                case 3:
                    //放雷电，追踪雷电
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                    listtargets = CurrentMap.getMapObjects(Target.CurrentLocation.X, Target.CurrentLocation.Y, 2);
                    for (int o = 0; o < listtargets.Count; o++)
                    {
                        MapObject ob = listtargets[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MAC);
                        ActionList.Add(action);
                    }
                    break;
                case 4:
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1 });
                    listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 3);
                    for (int o = 0; o < listtargets.Count; o++)
                    {
                        MapObject ob = listtargets[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage*2/3, DefenceType.MAC);
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

        //走向攻击目标
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
