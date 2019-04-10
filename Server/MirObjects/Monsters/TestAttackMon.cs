using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    /// 测试怪物的攻击手段，用于测试客户端AI
    public class TestAttackMon : MonsterObject
    {
        private byte attType;
        public byte ObjectAttackType=1;//攻击手段
        public byte ObjectRangeAttack=1;//范围攻击手段
        protected internal TestAttackMon(MonsterInfo info)
            : base(info)
        {

        }


        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, 3);
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
            int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + 750; //50 MS per Step
            DelayedAction action = null;


            if (attType>= ObjectAttackType+ ObjectRangeAttack)
            {
                attType = 0;
            }
            if(attType< ObjectAttackType)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = attType });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);

            }else
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, TargetID= Target.ObjectID, Direction = Direction, Location = CurrentLocation, Type = (byte)(attType- ObjectAttackType) });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }


            attType++;
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

       

    }
}
