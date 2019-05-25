using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  SnowWolfKing 雪太狼 
    ///  1.普通攻击 2格
    ///  2.狼叫  召唤小狼
    ///  3.双重打击 2倍攻击
    ///  4.践踏 减速
    ///  5.践踏 冰冻
    /// </summary>
    public class SnowWolfKing : MonsterObject
    {

        private byte attType;
        private long SpawnWolfTime = 0;
        private byte SpawnWolfCount;
        protected internal SnowWolfKing(MonsterInfo info)
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
            int Distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            int delay = Distance * 50 + 750; //50 MS per Step
            int damage = GetAttackPower(MinDC, MaxDC);
            attType = 0;
            if (HP<this.MaxHP/2 && RandomUtils.Next(100) < 40 && CanSpawnWolf() )
            {
                attType = 1;
            }
            if (HP < this.MaxHP / 2)
            {
                damage = damage * 2;
            }
            if (attType == 0)
            {
                int rd = RandomUtils.Next(100);
                if (rd < 50)
                {
                    attType = 0;
                }else if (rd < 70)
                {
                    attType = 2;
                }else if (rd < 90)
                {
                    attType = 3;
                }
                else
                {
                    attType = 4;
                }
            }

            DelayedAction action = null;
            if (attType == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            if (attType == 1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1});
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
                SpawnWolf();
            }
            if (attType == 2)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage*2, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            //范围减速
            if (attType == 3)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                List<MapObject> list = FindAllTargets(3, CurrentLocation, false);
                foreach (MapObject ob in list)
                {
                    if (!ob.IsAttackTarget(this)) continue;
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage * 3 / 2, DefenceType.MAC);
                    ActionList.Add(action);
                    //冰冻
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                    {
                        ob.ApplyPoison(new Poison
                        {
                            Owner = this,
                            Duration = RandomUtils.Next(8, 15),
                            PType = PoisonType.Slow,
                            Value = damage / 3,
                            TickSpeed = 1000
                        }, this);
                    }
                }
            }
            //范围冰冻
            if (attType == 4)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                List<MapObject> list = FindAllTargets(2, CurrentLocation, false);
                foreach (MapObject ob in list)
                {
                    if (!ob.IsAttackTarget(this)) continue;
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage * 3 / 2, DefenceType.MAC);
                    ActionList.Add(action);
                    //冰冻
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                    {
                        //几率麻痹
                        if (RandomUtils.Next(100) < 50)
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

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
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

            MoveTo(Target.CurrentLocation);
        }

        //是否召唤冰狼
        //如果身边有超过5个冰狼，则不召唤冰狼
        //10秒内不重复召唤
        private bool CanSpawnWolf()
        {
            if (SpawnWolfTime > Envir.Time)
            {
                return false;
            }
            if (SpawnWolfCount > 20)
            {
                return false;
            }
            MonsterObject mob = GetMonster(Envir.GetMonsterInfo(Settings.SnowWolfName));
            if (mob == null)
            {
                return false;
            }
            //周围5格的血狼不超过3个
            List<MapObject> Friends = FindFriendsNearby(5);
            if (Friends.Count > 3)
            {
                return false;
            }
            return true;
        }

        //召唤冰狼
        private void SpawnWolf()
        {
            SpawnWolfTime = Envir.Time + 10000;
            byte count = (byte)RandomUtils.Next(2, 7);
            SpawnWolfCount += count;
            for (int i=0;i< count; i++)
            {
                MonsterObject mob = GetMonster(Envir.GetMonsterInfo(Settings.SnowWolfName));
                if (mob == null)
                {
                    return;
                }
                mob.Target = Target;
                mob.ActionTime = Envir.Time + 1000;
                CurrentMap.ActionList.Add(new DelayedAction(DelayedType.Spawn, Envir.Time + 500, mob, CurrentMap.RandomValidPoint(CurrentLocation.X, CurrentLocation.Y, 5), this));
            }
        }
    }
}
