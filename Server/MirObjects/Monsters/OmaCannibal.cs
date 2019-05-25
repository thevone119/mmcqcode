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
    /// OmaCannibal 奥玛食人族
    /// Attack1 0：普通攻击0
    /// AttackRange1  放绿色毒液
    /// </summary>
    class OmaCannibal : MonsterObject
    {
        
        private byte attckType = 0;//攻击类型
        private byte AttackRange = 1;//攻击范围

        protected internal OmaCannibal(MonsterInfo info) : base(info)
        {
        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (RandomUtils.Next(10) < 4)
            {
                AttackRange = 1;
            }
            else
            {
                AttackRange = 9;
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
            //伤害根据血量升级
            int damage = GetAttackPower(MinDC, MaxDC);

            //确定攻击类型,不在攻击访问内，则几率出麻痹和群毒
            if (distance > 1)
            {
                attckType = 1;
            }
            else
            {
                if (RandomUtils.Next(10) < 3)
                {
                    attckType = 1;
                }
                else
                {
                    attckType = 0;
                }
            }
            //
            if (attckType == 0)
            {
                damage = damage * 3 / 2;
            }

            if (attckType == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            if (attckType == 1)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, TargetID = Target.ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0});
                action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + distance*50+750, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
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
            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
            //中毒
            if (target.Attacked(this, damage, defence) <= 0) return;
            if (RandomUtils.Next(Settings.PoisonResistWeight) >= target.PoisonResist)
            {
                target.ApplyPoison(new Poison { Owner = this, Duration = damage / 10, PType = PoisonType.Green, Value = damage / 10, TickSpeed = 2000 }, this);
            }
        }

       

    }
}
