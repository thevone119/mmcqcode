using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  淹死的奴隶 
    ///  1.甩铁链，范围攻击
    ///  2.喷气，魔法攻击，眩晕
    /// </summary>
    public class Nadz : MonsterObject
    {
     
        protected internal Nadz(MonsterInfo info)
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

            //眩晕
            if (RandomUtils.Next(4) == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                //攻击
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 400, Target, damage, DefenceType.MACAgility);
                if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist && RandomUtils.Next(100) < 20)
                {
                    Target.ApplyPoison(new Poison
                    {
                        Owner = this,
                        Duration = 4,
                        PType = PoisonType.Stun,
                        Value = damage,
                        TickSpeed = 2000
                    }, this);
                }
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                List<MapObject> Targets = FindAllTargets(1, CurrentLocation, false);
                foreach(MapObject ob in Targets)
                {
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 400, ob, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                }
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        
    }
}
