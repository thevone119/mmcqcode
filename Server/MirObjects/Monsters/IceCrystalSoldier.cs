using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  冰晶战士
    ///  3种攻击手段
    ///  1.普通攻击
    ///  2.普通攻击
    ///  3.范围攻击
    /// </summary>
    public class IceCrystalSoldier : MonsterObject
    {
        private byte attType;
        protected internal IceCrystalSoldier(MonsterInfo info)
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
            int delay = distance * 50 + 750; //50 MS per Step
            int rd = RandomUtils.Next(100);
            if (rd < 50)
            {
                attType = 0;
            }
            else if (rd < 80)
            {
                attType = 1;
            }
            else {
                attType = 2;
            }


            if (attType == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            if (attType == 1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage*2, DefenceType.MAC);
                ActionList.Add(action);
            }

            if (attType == 2)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                List<MapObject> list = FindAllTargets(3, CurrentLocation, false);
                foreach (MapObject ob in list)
                {
                    if (!ob.IsAttackTarget(this)) continue;
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage * 3 / 2, DefenceType.MAC);
                    ActionList.Add(action);
                    //冰冻
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                    {
                        ob.ApplyPoison(new Poison
                        {
                            Owner = this,
                            Duration = RandomUtils.Next(4, 10),
                            PType = PoisonType.Slow,
                            Value = damage / 3,
                            TickSpeed = 1000
                        }, this);
                        if (RandomUtils.Next(5)==1)
                        {
                            ob.ApplyPoison(new Poison
                            {
                                Owner = this,
                                Duration = RandomUtils.Next(2, 5),
                                PType = PoisonType.Frozen,
                                Value = damage / 3,
                                TickSpeed = 1000
                            }, this);
                        }
                    }
                }
            }
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

    }
}
