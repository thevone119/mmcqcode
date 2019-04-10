using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    /// 树的女王
    /// 攻击方式
    /// 0：推开
    /// 1:地刺
    /// 2：蜘蛛网
    /// 3：火雨
    /// </summary>
    class TreeQueen : MonsterObject
    {
        private byte attckType = 0;//攻击类型
        protected override bool CanMove { get { return false; } }
        //protected override bool CanRegen { get { return false; } }
        

        protected internal TreeQueen(MonsterInfo info) : base(info)
        {
            Direction = MirDirection.Up;

            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;
        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;

            return Target.CurrentLocation != CurrentLocation && Functions.InRange(CurrentLocation, Target.CurrentLocation, Info.ViewRange);
        }

        public override void Turn(MirDirection dir)
        {
        }
        public override bool Walk(MirDirection dir) { return false; }


       
        //public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false) { }

        protected override void ProcessTarget()
        {
            if (!CanAttack) return;

            List<MapObject> targets = FindAllTargets(Info.ViewRange, CurrentLocation);
            if (targets.Count == 0) return;
            List<MapObject> targets2 = FindAllTargets(2, CurrentLocation);
            //判断2格范围内是否有人，如果有人，则进行进身攻击
            if (RandomUtils.Next(10) < 3)
            {
                if (targets2.Count > 0)
                {
                    if (RandomUtils.Next(10) < 5)
                    {
                        attckType = 0;
                    }
                    else
                    {
                        attckType = 1;
                    }
                }
            }
            else
            {
                int rd = RandomUtils.Next(10);
                if (HP < MaxHP / 4)
                {
                    if (rd < 8)
                    {
                        attckType = 1;
                    }else if(rd < 9)
                    {
                        attckType = 2;
                    }
                    else
                    {
                        attckType = 3;
                    }
                }
                else
                {
                    if (rd < 7)
                    {
                        attckType = 1;
                    }
                    else if (rd < 8)
                    {
                        attckType = 2;
                    }
                    else
                    {
                        attckType = 3;
                    }
                }
            }
            ShockTime = 0;

            switch (attckType)
            {
                case 0:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type=0 });
                    for (int i = 0; i < targets2.Count; i++)
                    {
                        Target = targets2[i];
                        Attack();
                    }
                    break;
                case 1:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    for (int i = 0; i < targets.Count; i++)
                    {
                        Target = targets[i];
                        Attack();
                    }
                    break;
                case 2://随机挑选1个进行攻击,尽量挑选玩家
                    Target = targets[RandomUtils.Next(targets.Count)];
                    if (Target.Race != ObjectType.Player)
                    {
                        Target = targets[RandomUtils.Next(targets.Count)];
                    }
                    Attack();
                    break;
                case 3://随机挑选1个进行攻击
                    Target = targets[RandomUtils.Next(targets.Count)];
                    if (Target.Race != ObjectType.Player)
                    {
                        Target = targets[RandomUtils.Next(targets.Count)];
                    }
                    Attack();
                    break;
            }


            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;
        }

        protected override void Attack()
        {
            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            int delay = 800; //50 MS per Step
            DelayedAction action = null;
            switch (attckType)
            {
                case 0:
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    Target.Pushed(this, Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation),2);
                    break;
                case 1://近身地刺伤害增加1.5倍
                    if (distance < 3)
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, Target, damage*3/2, DefenceType.ACAgility);
                        ActionList.Add(action);
                    }
                    else
                    {
                        Broadcast(new S.ObjectEffect { ObjectID = Target.ObjectID, Effect = SpellEffect.TreeQueen });
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, Target, damage, DefenceType.MACAgility);
                        ActionList.Add(action);
                    }
                    break;
                case 2://随机挑选1个进行攻击,尽量挑选玩家
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID= Target.ObjectID, Location = CurrentLocation, Type = 0 });
                    List<MapObject> targets2 = FindAllTargets(2, Target.CurrentLocation);
                    for (int i = 0; i < targets2.Count; i++)
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, targets2[i], damage, DefenceType.MACAgility);
                        ActionList.Add(action);
                        if (RandomUtils.Next(Settings.PoisonResistWeight) >= targets2[i].PoisonResist)
                        {
                            if (RandomUtils.Next(10) < 7)
                            {
                                targets2[i].ApplyPoison(new Poison { PType = PoisonType.Paralysis, Duration = 6, TickSpeed = 1000 }, this);
                            }
                        }
                    }
                    break;
                case 3://随机挑选1个进行攻击,2段伤害
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1 });
                    List<MapObject> targets3 = FindAllTargets(3, Target.CurrentLocation);
                    for (int i = 0; i < targets3.Count; i++)
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, targets3[i], damage*2/3, DefenceType.MACAgility);
                        ActionList.Add(action);
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 800, targets3[i], damage*3/2, DefenceType.MACAgility);
                        ActionList.Add(action);
                    }
                    break;
            }
        }

    }
}
