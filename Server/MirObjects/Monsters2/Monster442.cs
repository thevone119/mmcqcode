using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  Monster442 昆仑叛军箭神 小BOSS
    ///  3种攻击手段
    /// </summary>
    public class Monster442 : MonsterObject
    {


        protected internal Monster442(MonsterInfo info)
            : base(info)
        {
        }



        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, 8);
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
            if (rd < 65 )
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage , DefenceType.ACAgility);
                ActionList.Add(action);
                //减速效果
                if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist && RandomUtils.Next(100) < 40)
                {
                    Target.ApplyPoison(new Poison { Owner = this, Duration = RandomUtils.Next(6,10), PType = PoisonType.Slow, Value = damage / 10, TickSpeed = 2000 }, this);
                }
            }
            else if(rd<90)
            {
                //天箭，冰冻效果
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage*3/2, DefenceType.MAC);
                ActionList.Add(action);
                //冰冻效果
                if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist && RandomUtils.Next(100) < 40)
                {
                    Target.ApplyPoison(new Poison { Owner = this, Duration = RandomUtils.Next(3, 7), PType = PoisonType.Frozen, Value = damage / 10, TickSpeed = 1000 }, this);
                }
            }
            else
            {
                //全屏雷电攻击,直接秒，只针对玩家秒
                List<MapObject> listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 8);
                int maxcount = 0;
                for (int o = 0; o < listtargets.Count; o++)
                {
                    MapObject ob = listtargets[o];
                    if (ob.Race != ObjectType.Player) continue;
                    if (!ob.IsAttackTarget(this)) continue;
                    if (maxcount >= 5)
                    {
                        continue;
                    }
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = ob.ObjectID, Target=ob.CurrentLocation, Location = CurrentLocation, Type = 2 });
                    action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 500, ob, damage*2, DefenceType.MAC);
                    ActionList.Add(action);
                    maxcount++;
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

            if (!CanMove || Target == null) return;
            MoveTo(Target.CurrentLocation);
           
        }

    
     
    }
}
