using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  铁甲犀牛
    ///  3种攻击手段
    ///  近身普攻，踏脚，冲撞
    /// </summary>
    public class HardenRhino : MonsterObject
    {

        long CollisionTime = 0;

        protected internal HardenRhino(MonsterInfo info)
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

            int rd = 8;
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
                //判断目标身后是否空，如果空则冲撞
                //
                Point target = Functions.PointMove(CurrentLocation, Direction,2);
                if (RandomUtils.Next(2) == 0 && CurrentMap.EmptyPoint(target.X, target.Y))
                {
                    //冲撞
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                    LineAttack(2, 1);
                    //移动到地图某个位置
                    DelayedAction action = new DelayedAction(DelayedType.MapMovement, Envir.Time + 1200, CurrentMap, target, CurrentMap, CurrentLocation);
                    ActionList.Add(action);
                    CollisionTime = Envir.Time + 2000;
                    AttackTime = Envir.Time + (2000);
                }
                else
                {
                    //踩踏
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    LineAttack(2, 0);
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

        private void LineAttack(int distance,byte attacktype)
        {
            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;
            int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + 500;
            //踩踏,范围攻击
            if (attacktype == 0)
            {
                List<MapObject> list = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, distance);
                for (int o = 0; o < list.Count; o++)
                {
                    MapObject ob = list[o];
                    if (ob.Race == ObjectType.Monster || ob.Race == ObjectType.Player)
                    {
                        if (!ob.IsAttackTarget(this)) continue;
                        DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.AC);
                        ActionList.Add(action);
                        //ob.Attacked(this, damage, DefenceType.AC);
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
