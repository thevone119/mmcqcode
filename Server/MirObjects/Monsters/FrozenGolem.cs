using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  FrozenGolem 雪原鬼尊 
    ///  1.拍击
    ///  2.风暴
    /// </summary>
    public class FrozenGolem : MonsterObject
    {

        private byte attType;
        protected internal FrozenGolem(MonsterInfo info)
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
            int Distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            int damage = GetAttackPower(MinDC, MaxDC);
            attType = 0;
            if (RandomUtils.Next(100) < 30)
            {
                attType = 1;
            }
            if (HP < MaxHP / 3)
            {
                attType = 1;
            }

            DelayedAction action = null;
            
            if (attType ==1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                List<MapObject>  list = FindAllTargets(2, CurrentLocation, false);
                foreach(MapObject ob in list)
                {
                    if (!ob.IsAttackTarget(this)) continue;
                    //几率冰冻
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist)
                    {
                        ob.ApplyPoison(new Poison
                        {
                            Owner = this,
                            Duration = RandomUtils.Next(5, 10),
                            PType = PoisonType.Slow,
                            Value = damage / 3,
                            TickSpeed = 1000
                        }, this);
                    }
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + 200, ob, damage*2/3, DefenceType.MAC);
                    ActionList.Add(action);
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, ob, damage, DefenceType.MAC);
                    ActionList.Add(action);
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + 800, ob, damage * 3 / 2, DefenceType.MAC);
                    ActionList.Add(action);
                }
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        
     

    }
}
