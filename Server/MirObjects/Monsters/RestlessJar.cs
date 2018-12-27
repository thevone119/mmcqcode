using Server.MirDatabase;
using System.Collections.Generic;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    //这个才是4眼神炉
    //这个不动
    public class RestlessJar : MonsterObject
    {
        //狂暴几率，根据血量得到相应的狂暴几率
        public int RageRate=10;
        //攻击范围8格
        public byte AttackRange = 8;

        protected internal RestlessJar(MonsterInfo info)
            : base(info)
        {
            //视野内均可攻击，具体视野范围，看数据库的配置了
            AttackRange = info.ViewRange;
        }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            ShockTime = 0;


            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);

            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;

            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

            if (HP < MaxHP*3 / 4  )
            {
                RageRate = 8;
            }
            if (HP < MaxHP*2 / 4 )
            {
                RageRate = 4;
            }
            if (HP < MaxHP*1 / 4 )
            {
                RageRate = 2;
            }

            //狂暴群体攻击，攻击系数乘1.5
            if (RandomUtils.Next(RageRate) == 0)
            {
                damage = (int)(damage * 1.5);
                List<MapObject> targets = FindAllTargets(AttackRange, CurrentLocation, false);
                for (int i = 0; i < targets.Count; i++)
                {
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = targets[i].ObjectID,Type=1 });
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 800, targets[i], damage, DefenceType.MACAgility);
                    ActionList.Add(action);
                }
            }
            else
            {
                //单体攻击
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID});
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 800, Target, damage, DefenceType.MACAgility);
                ActionList.Add(action);
            }

            if (Target.Dead)
                FindTarget();

        }
    }
}
