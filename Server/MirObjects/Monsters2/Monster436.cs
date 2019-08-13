using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  Monster436 昆仑叛军刺客 3种攻击手段
    /// </summary>
    public class Monster436 : MonsterObject
    {


        protected internal Monster436(MonsterInfo info)
            : base(info)
        {
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
            DelayedAction action = null;
            int rd = RandomUtils.Next(100);
            if (rd < 65)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.None);
                ActionList.Add(action);

            }
            else if(rd < 90)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.None);
                ActionList.Add(action);
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay+200, Target, damage, DefenceType.None);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                List<MapObject> listtargets = FindAllTargets(3, CurrentLocation);
                foreach(MapObject ob in listtargets)
                {
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay + 200, ob, damage*2, DefenceType.MAC);
                    ActionList.Add(action);
                }
            }
            

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }



       


    
     
    }
}
