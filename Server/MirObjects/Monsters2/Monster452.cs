using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  Monster452 多毒妖 毒妖林小BOSS
    ///  3种攻击手段
    ///  1.普通攻击
    ///  2.群毒攻击
    ///  3.解毒+地毒攻击
    /// </summary>
    public class Monster452 : MonsterObject
    {

        private long PoisonTime = 0;//可被中毒的时间

        //是否可以活动
        private bool _canMove = false;
        private long _checkMoveTime = 0;//AI计算的时间
        private string preMonName = "碑石妖";

        protected internal Monster452(MonsterInfo info)
            : base(info)
        {

        }


        private void checkCanMove()
        {
            if (!Dead && Envir.Time > _checkMoveTime)
            {
                _checkMoveTime = Envir.Time + 5000;
                bool has = false;
                foreach (MapObject ob in SMain.Envir.Objects)
                {
                    if (ob == null || ob.Dead || ob.CurrentMap != CurrentMap || ob.Race != ObjectType.Monster)
                    {
                        continue;
                    }
                    if (preMonName.Equals(ob.Name))
                    {
                        has = true;
                        break;
                    }
                }
                if (has)
                {
                    _canMove = false;
                    MoveTime = Envir.Time + 5200;
                    AttackTime = Envir.Time + 5200;
                }
                else
                {
                    _canMove = true;
                }
            }
        }


        protected override void ProcessAI()
        {
            checkCanMove();
            base.ProcessAI();
        }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, 7);
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            int atype = 1;
            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            int damage = GetAttackPower(MinDC, MaxDC);
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            int delay = distance * 50 + 500; //50 MS per Step
            DelayedAction action = null;
            if (distance <= 1 && RandomUtils.Next(100) < 30)
            {
                atype = 1;
            }
            else
            {
                if (RandomUtils.Next(100) < 60)
                {
                    atype = 2;
                }
                else
                {
                    atype = 3;
                }
            }
            if (atype == 1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                if (Target.Race == ObjectType.Monster)
                {
                    damage = damage * 2;
                }
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage * 4 / 3, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            if (atype == 2)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                List<MapObject> listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 3);
                for (int o = 0; o < listtargets.Count; o++)
                {
                    MapObject ob = listtargets[o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;
                    if (ob.Race == ObjectType.Monster)
                    {
                        damage = damage * 2;
                    }
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MAC);
                    ActionList.Add(action);
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist && RandomUtils.Next(100) < 30)
                    {
                        ob.ApplyPoison(new Poison { Owner = this, Duration = damage / 30, PType = PoisonType.Green, Value = damage / 5, TickSpeed = 2000 }, this);
                    }
                }
            }

            if (atype == 3)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                PoisonList.Clear();
                PoisonTime = Envir.Time + RandomUtils.Next(2, 5) * 1000;
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MACAgility);
                ActionList.Add(action);
                int rcount = RandomUtils.Next(2, 6);
                //一次放2-4个毒，随机放，如果重复了，就不放
                for (int i = 0; i < rcount; i++)
                {
                    Point p = CurrentMap.RandomValidPoint(CurrentLocation.X, CurrentLocation.Y, 6, 1);
                    //随机抽取5个点，放毒
                    SpellObject spellObj = new SpellObject
                    {
                        Spell = Spell.MonPoisonFog,
                        Value = damage,
                        ExpireTime = Envir.Time + 1000 * RandomUtils.Next(20, 30),
                        TickSpeed = 2000,
                        Caster = null,
                        MonCaster = this,
                        CurrentLocation = p,
                        CurrentMap = CurrentMap,
                        Direction = MirDirection.Up
                    };
                    action = new DelayedAction(DelayedType.Spawn, Envir.Time + 500, spellObj);
                    CurrentMap.ActionList.Add(action);
                }
            }
            

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        public override int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility)
        {
            if (!_canMove)
            {
                return 0;
            }
            return base.Attacked(attacker, damage, type);
        }
        public override int Attacked(PlayerObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            if (!_canMove)
            {
                return 0;
            }

            return base.Attacked(attacker, damage, type, damageWeapon);
        }

        //有护盾的时候无法中毒
        public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true)
        {
            if (!_canMove)
            {
                return;
            }
            if (Envir.Time > PoisonTime)
            {
                base.ApplyPoison(p, Caster, NoResist, ignoreDefence);
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
