using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  Monster437 昆仑叛军和尚 3种攻击手段
    ///  线性攻击
    /// </summary>
    public class Monster437 : MonsterObject
    {


        protected internal Monster437(MonsterInfo info)
            : base(info)
        {
        }



        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > 3 || y > 3) return false;

            return (x == 0) || (y == 0) || (x == y);
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
                LineAttack(3,0);
            }
            else if (rd < 90)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                List<MapObject> listtargets = FindAllTargets(2, Functions.PointMove(CurrentLocation, Direction, 2));
                foreach (MapObject ob in listtargets)
                {
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay + 200, ob, damage * 2, DefenceType.MAC);
                    ActionList.Add(action);
                }
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                LineAttack(3,2);
             
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }


        private void LineAttack(int distance, byte AttackType)
        {
            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

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
                            //禁魔
                            if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist)
                            {
                                ob.ApplyPoison(new Poison
                                {
                                    Owner = this,
                                    Duration = RandomUtils.Next(6, 12),
                                    PType = PoisonType.Stun,
                                    Value = damage,
                                    TickSpeed = 1000
                                }, this);
                            }
                        }
                        else continue;

                        break;
                    }
                }
            }
        }







        protected override void ProcessTarget()
        {
            if (Target == null) return;

            if (InAttackRange() && CanAttack)
            {
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

            if (!CanMove || Target == null) return;
            MoveTo(Target.CurrentLocation);
           
        }

    
     
    }
}
