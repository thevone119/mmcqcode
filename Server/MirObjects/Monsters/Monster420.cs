using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///   冰宫祭师
    ///  3种攻击手段
    /// </summary>
    public class Monster420 : MonsterObject
    {

        private byte AttackType;
        private long ProcessTime;
        protected internal Monster420(MonsterInfo info)
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

            int rd = RandomUtils.Next(100);
            if (rd < 65)
            {
                AttackType = 0;
            }else
            {
                AttackType = 1;
            }
        

            switch (AttackType)
            {
                case 0:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction,  Location = CurrentLocation, Type = 0 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    break;

                case 1:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MACAgility);
                    ActionList.Add(action);
                    //技能禁锢
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist && RandomUtils.Next(100) < 30)
                    {
                        Target.ApplyPoison(new Poison
                        {
                            Owner = this,
                            Duration = RandomUtils.Next(3, 10),
                            PType = PoisonType.Stun,
                            Value = damage,
                            TickSpeed = 1000
                        }, this);
                    }
                    break;

            }
            
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }


        protected override void ProcessAI()
        {
            if (!Dead && Envir.Time > ProcessTime)
            {
                ProcessTime = Envir.Time + 2000;
                MapObject fob = null;
                //查找附近7格内是否有友军中毒的
                List<MapObject> listf = FindFriendsNearby(7);
                foreach (MapObject ob in listf)
                {
                    if (ob.Dead) continue;
                    if (ob.PoisonList.Count <= 0)
                    {
                        continue;
                    }
                    fob = ob;
                    break;
                }
                if (fob != null)
                {
                    fob.PoisonList.Clear();
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = fob.ObjectID, Location = CurrentLocation, Type = 0 });
                }
            }
            base.ProcessAI();
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
