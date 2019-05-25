using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;
using System.Drawing;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    /// 水手长（2种形态）
    /// 1.刀砍
    /// 2.丢火球
    /// </summary>
    class ClawBeast : MonsterObject
    {
        private byte state = 0;
        private byte AttackRange = 3;
        protected internal ClawBeast(MonsterInfo info)
            : base(info)
        {
            if(RandomUtils.Next(100) < 20)
            {
                state = 1;
                AttackRange = 7;
            }
            else
            {
                state = 0;
                AttackRange = 3;
            }
        }

        //8格以内，都是攻击距离
        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;
 
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
        }

        protected override void Attack()
        {

            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }
            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            //Point target = Functions.PointMove(CurrentLocation, Direction, 2);
            int delay = distance * 50 + 550; //50 MS per Step

            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;
            //
            if (AttackRange > 3)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage*2, DefenceType.MAC);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
            }
            
            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;
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
                ExtraByte = state,
            };
        }

    }
}
