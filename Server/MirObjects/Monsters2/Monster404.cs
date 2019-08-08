using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  冰焰鼠
    ///  1.近身攻击
    ///  死亡的时候炸一下，2倍攻击
    /// </summary>
    public class Monster404 : MonsterObject
    {

        protected internal Monster404(MonsterInfo info)
            : base(info)
        {

        }



        public override void Die()
        {
            //死亡的时候炸一下
            List<MapObject> listtargets = FindAllTargets(1, CurrentLocation, false);
            int damage = GetAttackPower(MinDC, MaxDC);
            foreach (MapObject ob in listtargets)
            {
                if (ob.IsAttackTarget(this))
                {
                    DelayedAction action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 500, ob, damage*2, DefenceType.None);
                    ActionList.Add(action);
                }
            }
            base.Die();
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
            int delay = distance * 50 + 750; //50 MS per Step
            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction,  Location = CurrentLocation, Type = 0 });
            DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
            ActionList.Add(action);
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
            //超过1格，炸不到
            int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);

            if (distance > 1)
            {
                return;
            }
            target.Attacked(this, damage, defence);
        }



    }
}
