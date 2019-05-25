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
    /// ShellFighter 斗争者
    /// 攻击方式
    /// Attack1 0：普通攻击0
    /// Attack2 1:隔位攻击1
    /// Attack3 2：隔位攻击2
    /// AttackRange1 3：放群毒，周围都中毒3
    /// AttackRange2 4:放蜘蛛网，束缚，麻痹4
    /// </summary>
    class ShellFighter : MonsterObject
    {
        
        private byte attckType = 0;//攻击类型
        private byte AttackRange = 2;//攻击范围

        protected internal ShellFighter(MonsterInfo info) : base(info)
        {
            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;
        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (RandomUtils.Next(10) < 6)
            {
                AttackRange = 1;
            }
            else
            {
                AttackRange = 8;
            }
            return Target.CurrentLocation != CurrentLocation && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
        }


        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            DelayedAction action;

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            bool ranged = Functions.InRange(CurrentLocation, Target.CurrentLocation, 2);
            //伤害根据血量升级
            int damage = GetAttackPower(MinDC, MaxDC);
            if (HP < MaxHP / 8)
            {
                damage = damage * 4 / 2;
            }
            else if (HP < MaxHP / 4)
            {
                damage = damage * 4 / 3;
            }
            //确定攻击类型,不在攻击访问内，则几率出麻痹和群毒
            if (distance > 2)
            {
                if (RandomUtils.Next(10) < 6)
                {
                    attckType = 3;
                }
                else
                {
                    attckType = 4;
                }
            }
            else
            {
                //在攻击访问内
                if (distance == 1 && RandomUtils.Next(10)<3)
                {
                    attckType = 0;
                }
                else
                {
                    attckType = (byte)RandomUtils.Next(5);
                }
            }
            //
            if (attckType == 1)
            {
                damage = damage * 3 / 2;
            }
            if (attckType == 2)
            {
                damage = damage * 4 / 2;
            }



            if (attckType == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            if (attckType == 1|| attckType==2)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = attckType });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
            }

            //放全屏毒
            if (attckType == 3)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0, TargetID = Target.ObjectID });
                List<MapObject> targets = FindAllTargets(8, CurrentLocation);
                for (int i = 0; i < targets.Count; i++)
                {
                    action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 1000, targets[i], damage, DefenceType.MAC, 0);
                    ActionList.Add(action);
                    action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 2000, targets[i], damage, DefenceType.MAC, 0);
                    ActionList.Add(action);
                    action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 3000, targets[i], damage, DefenceType.MAC, 0);
                    ActionList.Add(action);
                }
            }
            if(attckType==4)//麻痹
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1, TargetID = Target.ObjectID });
                List<MapObject> targets = FindAllTargets(2, Target.CurrentLocation);
                for (int i = 0; i < targets.Count; i++)
                {
                    action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 1000, targets[i], damage, DefenceType.MAC, 1);
                    ActionList.Add(action);
                }
            }
            ActionTime = Envir.Time + 800;
            AttackTime = Envir.Time + AttackSpeed;
            return;
        }

        protected override void CompleteRangeAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];
            int aType = (int)data[3];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
            if (aType == 0)
            {
                //中毒
                if (target.Attacked(this, damage, defence) <= 0) return;
                if (RandomUtils.Next(Settings.PoisonResistWeight) >= target.PoisonResist)
                {
                    target.ApplyPoison(new Poison { Owner = this, Duration = damage/10, PType = PoisonType.Green, Value = damage / 10, TickSpeed = 2000 }, this);
                }
            }
            else
            {
                //麻痹
                int poisonTime = RandomUtils.Next(3, 10);
                if (target.Attacked(this, damage, defence) <= 0) return;
                if (RandomUtils.Next(Settings.PoisonResistWeight) >= target.PoisonResist)
                {
                    target.ApplyPoison(new Poison { Owner = this, Duration = 5, PType = PoisonType.Paralysis, Value = poisonTime, TickSpeed = 2000 }, this);
                    //Broadcast(new S.ObjectEffect { ObjectID = target.ObjectID, Effect = SpellEffect.Stunned, Time = (uint)poisonTime * 1000 });
                }
            }
        }

       

    }
}
