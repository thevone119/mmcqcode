using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  Monster444 昆仑叛军刺客 小BOSS
    ///  2种攻击手段
    /// </summary>
    public class Monster444 : MonsterObject
    {


        protected internal Monster444(MonsterInfo info)
            : base(info)
        {
        }



        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, 2);
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

            if (RandomUtils.Next(100) < 85)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                LineAttack(2);
                //如果隔位有位置，则瞬移过去
                Point target = Functions.PointMove(CurrentLocation, Direction, 2);
                if (CurrentMap.ValidPoint(target))
                {
                    action = new DelayedAction(DelayedType.MapMovement, Envir.Time + 1000, CurrentMap, target, CurrentMap, CurrentLocation);
                    ActionList.Add(action);
                }
            }
            

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }


        private void LineAttack(int distance)
        {
            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;
            damage = damage * 2;

            for (int i = 1; i <= distance; i++)
            {
                Point target = Functions.PointMove(CurrentLocation, Direction, i);

                if (target == Target.CurrentLocation)
                    Target.Attacked(this, damage, DefenceType.MACAgility);
                else
                {
                    if (!CurrentMap.ValidPoint(target)) continue;

                    //Cell cell = CurrentMap.GetCell(target);
                    if (CurrentMap.Objects[target.X, target.Y] == null) continue;

                    for (int o = 0; o < CurrentMap.Objects[target.X, target.Y].Count; o++)
                    {
                        MapObject ob = CurrentMap.Objects[target.X, target.Y][o];
                        if (ob.Race == ObjectType.Monster || ob.Race == ObjectType.Player)
                        {
                            if (!ob.IsAttackTarget(this)) continue;
                            ob.Attacked(this, damage, DefenceType.MACAgility);
                        }
                        else continue;

                        break;
                    }
                }
            }
        }





        protected override void ProcessTarget()
        {
            if (Target == null || !CanAttack) return;

            if (InAttackRange())
            {
                //几率瞬移逃跑
                if (Target != null && this.HP < this.MaxHP / 2 && RandomUtils.Next(2) == 0)
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                    DelayedAction action = new DelayedAction(DelayedType.MapMovement, Envir.Time + 1000, CurrentMap, CurrentMap.RandomValidPoint(CurrentLocation.X, CurrentLocation.Y, 8), CurrentMap,CurrentLocation);
                    ActionList.Add(action);
                    return;
                }
                Attack();
                if (Target == null || Target.Dead)
                    FindTarget();
                return;
            }

            if (Envir.Time < ShockTime)
            {
                Target = null;
                return;
            }

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);
            //瞬移
            if ((x > 3 || y > 3) && RandomUtils.Next(4) == 0)
            {
                if (CurrentMap.ValidPoint(Target.Back))
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                    DelayedAction action = new DelayedAction(DelayedType.MapMovement, Envir.Time + 1000, CurrentMap, Target.Back, CurrentMap, CurrentLocation);
                    ActionList.Add(action);

                    Attack();
                    return;
                }
                else if (CurrentMap.ValidPoint(Target.Front))
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                    DelayedAction action = new DelayedAction(DelayedType.MapMovement, Envir.Time + 1000, CurrentMap, Target.Front, CurrentMap, CurrentLocation);
                    ActionList.Add(action);
                    Attack();
                    return;
                }
            }
            MoveTo(Target.CurrentLocation);
        }


    }
}
