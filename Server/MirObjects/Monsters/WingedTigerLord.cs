using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    //野兽王
    //改版了AI
    class WingedTigerLord : MonsterObject
    {
        enum AttackType
        {
            SingleSlash,
            Tornado,
            Stomp
        }
        //跺脚,龙卷风
        private bool stomp;

        private byte AttackRange = 2;
        public long FearTime;

        protected internal WingedTigerLord(MonsterInfo info) : base(info)
        {
        }

        protected override bool InAttackRange()
        {
            byte _AttackRange = AttackRange;
            if (RandomUtils.Next(10) < 4)
            {
                _AttackRange = 1;
            }
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, _AttackRange);
        }


        protected override void ProcessAI()
        {
            if (!Dead && Envir.Time > FearTime)
            {
                FearTime = Envir.Time + 2000;
                if (HP>MaxHP/4)
                {
                    if (RandomUtils.Next(7) == 0)
                        stomp = true;
                    if (RandomUtils.Next(6) == 0)
                    {
                        AttackRange = 10;

                    }
                }
                else{
                    if (RandomUtils.Next(4) == 0)
                        stomp = true;
                    if (RandomUtils.Next(2) == 0)
                    {
                        AttackRange = 10;

                    }
                }
            }
            base.ProcessAI();
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            DelayedAction action;

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            bool ranged = Functions.InRange(CurrentLocation, Target.CurrentLocation, 2);
            //伤害根据血量升级
            int damage = GetAttackPower(MinDC, MaxDC);
            if (HP < MaxHP / 5)
            {
                damage = damage * 3 / 2;
            }
            else if(HP < MaxHP / 2)
            {
                damage = damage * 4 / 3;
            }

            //不在范围内
            if (!ranged)
            {
                
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0, TargetID = Target.ObjectID });

                List<MapObject> targets = FindAllTargets(1, Target.CurrentLocation);

                for (int i = 0; i < targets.Count; i++)
                {
                    action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 1000, targets[i], damage, DefenceType.MAC);
                    ActionList.Add(action);
                }

                ActionTime = Envir.Time + 800;
                AttackTime = Envir.Time + AttackSpeed;
                AttackRange = 2;
                return;
            }

            //在范围内
            if (ranged)
            {
                //跺脚
                if (stomp)
                {
                    //Foot stomp
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });

                    List<MapObject> list = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 2);
                    for (int o = 0; o < list.Count; o++)
                    {
                        MapObject ob = list[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        //宝宝叛变了
                        if(ob.Race == ObjectType.Monster && ob.Master!=null)
                        {
                            MonsterObject mon = (MonsterObject)ob;
                            mon.TameTime = Envir.Time;
                        }

                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, ob, damage, DefenceType.ACAgility, AttackType.Stomp);
                        ActionList.Add(action);
                    }

                    ActionTime = Envir.Time + 800;
                    AttackTime = Envir.Time + AttackSpeed;

                    stomp = false;
                    return;
                }

                switch (RandomUtils.Next(2))
                {
                    case 0:
                        //Slash
                        Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });

           
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, Target, damage, DefenceType.ACAgility, AttackType.SingleSlash);
                        ActionList.Add(action);


                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, Target, damage, DefenceType.ACAgility, AttackType.SingleSlash);
                        ActionList.Add(action);
                
                        break;
                    case 1:
                        //Two hand slash
                        Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });

                        damage = GetAttackPower(MinDC, MaxDC);
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 300, Target, damage*3/2, DefenceType.ACAgility, AttackType.SingleSlash);
                        ActionList.Add(action);
                        break;
                }
            }

            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + AttackSpeed;
            ShockTime = 0;
        }

        protected override void CompleteRangeAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            //眩晕的时间长一点
            int poisonTime = GetAttackPower(MinSC, MaxSC)*3;

            if (target.Attacked(this, damage, defence) <= 0) return;

            if (RandomUtils.Next(Settings.PoisonResistWeight) >= target.PoisonResist)
            {
                if (RandomUtils.Next(100) < 65)
                {
                    target.ApplyPoison(new Poison { Owner = this, Duration = 5, PType = PoisonType.Stun, Value = poisonTime, TickSpeed = 2000 }, this);
                    Broadcast(new S.ObjectEffect { ObjectID = target.ObjectID, Effect = SpellEffect.Stunned, Time = (uint)poisonTime * 1000 });
                }
            }
        }

        protected override void CompleteAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];
            AttackType type = (AttackType)data[3];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            int poisonTime = GetAttackPower(MinSC, MaxSC);

            if (target.Attacked(this, damage, defence) <= 0) return;

            switch (type)
            {
                case AttackType.Stomp:
                    {
                        if (RandomUtils.Next(Settings.PoisonResistWeight) >= target.PoisonResist)
                        {
                            if (RandomUtils.Next(2) == 0)
                            {
                                target.ApplyPoison(new Poison { Owner = this, Duration = 5, PType = PoisonType.Paralysis, Value = poisonTime, TickSpeed = 2000 }, this);
                            }
                        }
                    }
                    break;
            }
        }
    }
}
