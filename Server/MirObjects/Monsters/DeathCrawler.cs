using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  死灵
    ///  2种攻击手段
    ///  1.普通攻击
    ///  2.隐身（被攻击到半血的时候隐身，一次，满血，5-10秒出现在玩家身边）
    /// </summary>
    public class DeathCrawler : MonsterObject
    {
        public bool Visible;
        public long VisibleTime;
        public bool hide,show;

        protected internal DeathCrawler(MonsterInfo info)
            : base(info)
        {
            Visible = true;
        }

        protected override bool CanAttack
        {
            get
            {
                return Visible && base.CanAttack;
            }
        }

        public override bool Blocking
        {
            get
            {
                return Visible && base.Blocking;
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
            DelayedAction action = null;

            if (show)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
                //上毒
                Target.ApplyPoison(new Poison
                {
                    Owner = this,
                    Duration = 10,
                    PType = PoisonType.Green,
                    Value = 20,
                    TickSpeed = 2000
                }, this);
                show = false;
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        protected override void ProcessAI()
        {
            if (!Dead && Envir.Time > VisibleTime)
            {
                VisibleTime = Envir.Time + 2000;

                if (!Visible)
                {
                    //判断附近7格内有有没玩家，有玩家就移动到玩家生活，显现出来，直接释放毒液
                    Point rp =  CurrentMap.RandomValidPoint(CurrentLocation.X, CurrentLocation.Y, 3);

                    List<MapObject> list = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 7);
                    for (int o = 0; o < list.Count; o++)
                    {
                        MapObject ob = list[o];
                        if (ob.Race == ObjectType.Player)
                        {
                            if (!ob.IsAttackTarget(this)) continue;
                            if (CurrentMap.ValidPoint(ob.Back))
                            {
                                rp = ob.Back;
                                break;
                            }
                            if (CurrentMap.ValidPoint(ob.Front))
                            {
                                rp = ob.Front;
                                break;
                            }
                        }
                    }

                    //CurrentLocation = rp;
                    SetHP(MaxHP);
                    Visible = true;
                    CellTime = Envir.Time + 500;
                    Broadcast(GetInfo());
                    Broadcast(new S.ObjectShow { ObjectID = ObjectID });
                    ActionTime = Envir.Time + 1000;
                    Teleport(CurrentMap,rp,false);
                    show = true;
                }
                //隐身5-10秒，满血
                if (Visible && !hide && HP < MaxHP/2)
                {
                    Visible = false;
                    VisibleTime = Envir.Time + RandomUtils.Next(5,10)*1000;
                    Broadcast(new S.ObjectHide { ObjectID = ObjectID });
                    
                    hide = true;
                }
            }

            base.ProcessAI();
        }

        public override bool IsAttackTarget(MonsterObject attacker)
        {
            return Visible && base.IsAttackTarget(attacker);
        }
        public override bool IsAttackTarget(PlayerObject attacker)
        {
            return Visible && base.IsAttackTarget(attacker);
        }

        public override Packet GetInfo()
        {
            if (!Visible) return null;

            return base.GetInfo();
        }
    }
}
