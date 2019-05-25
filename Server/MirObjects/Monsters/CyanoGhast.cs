using Server.MirDatabase;
using System;
using Server.MirEnvir;
using S = ServerPackets;
using System.Drawing;

namespace Server.MirObjects.Monsters
{
    //赤血鬼魂
    //1.破隐身
    //2.瞬移
    //3.上毒
    class CyanoGhast : MonsterObject
    {

        protected internal CyanoGhast(MonsterInfo info)
            : base(info)
        {
            CoolEye = true;

        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > 2 || y > 2) return false;
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
            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
            LineAttack(2);


            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;
            ShockTime = 0;
        }

        private void LineAttack(int distance)
        {

            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

            for (int i = 1; i <= distance; i++)
            {
                Point target = Functions.PointMove(CurrentLocation, Direction, i);

                if (target == Target.CurrentLocation)
                {
                    if (Target.Attacked(this, damage, DefenceType.MACAgility) > 0 )
                    {
                        int ptype = RandomUtils.Next(4);
                        if (ptype == 0)
                        {
                            if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                            {
                                int poison = GetAttackPower(MinSC, MaxSC);

                                Target.ApplyPoison(new Poison
                                {
                                    Owner = this,
                                    Duration = 5,
                                    PType = PoisonType.Green,
                                    Value = poison,
                                    TickSpeed = 2000
                                }, this);
                            }
                        }
                        if (ptype == 1)
                        {
                            if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                            {
                                int poison = GetAttackPower(MinSC, MaxSC);

                                Target.ApplyPoison(new Poison
                                {
                                    Owner = this,
                                    Duration = 5,
                                    PType = PoisonType.Slow,
                                    Value = poison,
                                    TickSpeed = 2000
                                }, this);
                            }
                        }
                    }
                }
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

                            if (ob.Attacked(this, damage, DefenceType.MACAgility) > 0 && RandomUtils.Next(8) == 0)
                            {
                                int ptype = RandomUtils.Next(5);
                                if (ptype == 0)
                                {
                                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                                    {
                                        int poison = GetAttackPower(MinSC, MaxSC);

                                        ob.ApplyPoison(new Poison
                                        {
                                            Owner = this,
                                            Duration = 5,
                                            PType = PoisonType.Green,
                                            Value = poison,
                                            TickSpeed = 2000
                                        }, this);
                                    }
                                }
                                if (ptype == 1)
                                {
                                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                                    {
                                        int poison = GetAttackPower(MinSC, MaxSC);

                                        ob.ApplyPoison(new Poison
                                        {
                                            Owner = this,
                                            Duration = 5,
                                            PType = PoisonType.Slow,
                                            Value = poison,
                                            TickSpeed = 2000
                                        }, this);
                                    }
                                }
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
            if (Target == null || !CanAttack) return;

            if (InAttackRange())
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

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);
            //瞬移
            if ((x > 5 || y > 5)&&RandomUtils.Next(3)==0)
            {
                if(CurrentMap.ValidPoint(Target.Back)){
                    Teleport(CurrentMap, Target.Back);
                    return;
                }
                else if (CurrentMap.ValidPoint(Target.Front))
                {
                    Teleport(CurrentMap, Target.Front);
                    return;
                }
            }
            MoveTo(Target.CurrentLocation);
        }

    }
}
