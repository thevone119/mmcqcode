using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  奥玛巫医，3种攻击手段
    ///  1.攻击身前2格，砸地板
    ///  2.2个眼睛发出射线攻击，线性3格
    ///  3.加血，并且访问内攻击。身旁2格都受到攻击
    /// </summary>
    public class OmaWitchDoctor : MonsterObject
    {
        private byte attckType = 0;//攻击类型
        protected internal OmaWitchDoctor(MonsterInfo info)
            : base(info)
        {


        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            int AttackRange = RandomUtils.Next(4);
            return Target.CurrentLocation != CurrentLocation && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
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

            if (distance == 3)
            {
                attckType = 1;
            }
            else
            {
                if (HP < MaxHP / 2 && RandomUtils.Next(10)<5)
                {
                    attckType = 2;
                }
                if (RandomUtils.Next(10) < 3)
                {
                    attckType = 1;
                }
            }

            DelayedAction action;
            //砸地板
            if (attckType == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                List<MapObject> list = CurrentMap.getMapObjects(Target.CurrentLocation.X, Target.CurrentLocation.Y, 2);
                for (int o = 0; o < list.Count; o++)
                {
                    MapObject ob = list[o];
                    if (ob.Race == ObjectType.Monster || ob.Race == ObjectType.Player)
                    {
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MAC);
                        ActionList.Add(action);
                    }
                }
            }
            //射线
            if (attckType == 1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });

                action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, Target, damage*2, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            //回血
            if (attckType == 2)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });

                List<MapObject> list = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 2);
                for (int o = 0; o < list.Count; o++)
                {
                    MapObject ob = list[o];
                    if (ob.Race == ObjectType.Monster || ob.Race == ObjectType.Player)
                    {
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MAC);
                        ActionList.Add(action);
                    }
                }
                //回血
                ChangeHP(RandomUtils.Next((int)MaxHP / 5, (int)MaxHP /2));
            }
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }
    }
}
