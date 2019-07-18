using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  雪原冰狼
    ///  几率震晕，麻痹
    ///  2种攻击手段
    ///  近身普攻，和近身1.5倍爆发攻击
    /// </summary>
    public class SnowWolf : MonsterObject
    {
     
        protected internal SnowWolf(MonsterInfo info)
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

            int rd = 5;
            if (HP < MaxHP / 2)
            {
                rd = 4;
            }
            else if (HP < MaxHP / 4)
            {
                rd = 2;
            }
            if (RandomUtils.Next(rd) == 0 )
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                LineAttack(2);
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        private void LineAttack(int distance)
        {
            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;
            //damage = damage * 2;
            int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + 500;

            List<MapObject> list = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, distance);
            for (int o = 0; o < list.Count; o++)
            {
                MapObject ob = list[o];
                if (ob.Race == ObjectType.Monster || ob.Race == ObjectType.Player)
                {
                    if (!ob.IsAttackTarget(this)) continue;
                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MACAgility);
                    ActionList.Add(action);
                    //冰冻
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist&& RandomUtils.Next(100) < 30)
                    {
                        ob.ApplyPoison(new Poison
                        {
                            Owner = this,
                            Duration = RandomUtils.Next(5, 15),
                            PType = PoisonType.Slow,
                            Value = damage / 3,
                            TickSpeed = 1000
                        }, this);
                        //几率麻痹
                        if (RandomUtils.Next(100) < 10)
                        {
                            ob.ApplyPoison(new Poison
                            {
                                Owner = this,
                                Duration = RandomUtils.Next(3, 5),
                                PType = PoisonType.Frozen,
                                Value = damage / 3,
                                TickSpeed = 1000
                            }, this);
                        }
                    }
                }
            }
        }
    }
}
