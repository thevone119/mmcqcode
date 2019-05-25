using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  GasToad 蛤蟆，如何神殿怪物
    ///  3种攻击手段
    ///  1.普通攻击
    ///  2.跳跃过去
    ///  3.范围毒性攻击
    /// </summary>
    public class GasToad : MonsterObject
    {

        long CollisionTime = 0;

        protected internal GasToad(MonsterInfo info)
            : base(info)
        {

        }

        protected override void ProcessTarget()
        {
            if (Envir.Time > CollisionTime)
            {
                base.ProcessTarget();
            }
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

 
            if (RandomUtils.Next(3) == 0)
            {
                //判断目标身后是否空，如果空则冲撞
                Point target = Functions.PointMove(CurrentLocation, Direction, 2);
                if (RandomUtils.Next(2) == 0 && CurrentMap.EmptyPoint(target.X, target.Y))
                {
                    //冲撞
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    LineAttack(2, 0);
                    //移动到地图某个位置
                    DelayedAction action = new DelayedAction(DelayedType.MapMovement, Envir.Time + 600, CurrentMap, target, CurrentMap, CurrentLocation);
                    ActionList.Add(action);
                    CollisionTime = Envir.Time + 1200;
                    AttackTime = Envir.Time + (2000);
                }
                else
                {
                    //范围毒性攻击
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                    LineAttack(2, 1);
                    AttackTime = Envir.Time + (AttackSpeed);
                }
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
                AttackTime = Envir.Time + (AttackSpeed);
            }
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
        }

        private void LineAttack(int distance, byte attacktype)
        {
            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;
            int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + 500;
            //范围毒性攻击
            if (attacktype == 1)
            {
                List<MapObject> list = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, distance);
                for (int o = 0; o < list.Count; o++)
                {
                    MapObject ob = list[o];
                    if (ob.Race == ObjectType.Monster || ob.Race == ObjectType.Player)
                    {
                        if (!ob.IsAttackTarget(this)) continue;
                        if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist)
                        {
                            if (RandomUtils.Next(2) == 0)
                            {
                                ob.ApplyPoison(new Poison { Owner = this, Duration = 20, PType = PoisonType.Green, Value = damage / 5, TickSpeed = 2000 }, this);
                            }
                        }
                    }
                }
            }
            else//冲撞，线性攻击
            {
                Point target = Functions.PointMove(CurrentLocation, Direction, 1);
                damage = damage * 2;
                if (!CurrentMap.ValidPoint(target)) return;
                //Cell cell = CurrentMap.GetCell(target);
                if (CurrentMap.Objects[target.X, target.Y] == null) return;
                for (int o = 0; o < CurrentMap.Objects[target.X, target.Y].Count; o++)
                {
                    MapObject ob = CurrentMap.Objects[target.X, target.Y][o];
                    if (ob.Race == ObjectType.Monster || ob.Race == ObjectType.Player)
                    {
                        if (!ob.IsAttackTarget(this)) continue;
                        DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.AC);
                        ActionList.Add(action);
                        //ob.Attacked(this, damage, DefenceType.AC);
                    }
                }
            }
        }

    }
}
