using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  Monster439 昆仑叛军武士 3种攻击手段
    ///  3种攻击手段
    /// </summary>
    public class Monster439 : MonsterObject
    {

        private byte _stage = 0;//0:没有护盾 1：有护盾
        private long fireTime = 0;
        private long ProcessTime = 0;

        protected internal Monster439(MonsterInfo info)
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

            if (RandomUtils.Next(100) < 65 && distance<2)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage , DefenceType.ACAgility);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                List<MapObject> listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 2);
                for (int o = 0; o < listtargets.Count; o++)
                {
                    MapObject ob = listtargets[o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage*3/2, DefenceType.MAC);
                    ActionList.Add(action);
                }
            }
            

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        protected override void ProcessAI()
        {
            //狂暴计算
            if (!Dead && Envir.Time > ProcessTime)
            {
                ProcessTime = Envir.Time + 1000;
                byte __stage = _stage;
                if (Envir.Time > fireTime)
                {
                    _stage = 0;
                }
                //
                if(Envir.Time> AttackTime && _stage == 0 && RandomUtils.Next(100)<10)
                {
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    PoisonList.Clear();
                    _stage = 1;
                    fireTime = Envir.Time + RandomUtils.Next(5, 12) * 1000;
                    ShockTime = 0;
                    ActionTime = Envir.Time + 500;
                    AttackTime = Envir.Time + (AttackSpeed);
                }
                if (__stage != _stage)
                {
                    Broadcast(GetInfo());
                }
            }
            base.ProcessAI();
        }

        public override int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility)
        {
            if (_stage == 1)
            {
                damage = damage / 2;
            }
            return base.Attacked(attacker, damage, type);
        }

        public override int Attacked(PlayerObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            if (_stage == 1)
            {
                damage = damage / 2;
            }
            return base.Attacked(attacker, damage, type, damageWeapon);
        }


        //有护盾的时候无法中毒
        public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true)
        {
            if (_stage == 0)
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


        public override Packet GetInfo()
        {
            return new S.ObjectMonster
            {
                ObjectID = ObjectID,
                Name = Name,
                NameColour = NameColour,
                Location = CurrentLocation,
                Image = Info.Image,
                Direction = Direction,
                Effect = Info.Effect,
                AI = Info.AI,
                Light = Info.Light,
                Dead = Dead,
                Skeleton = Harvested,
                Poison = CurrentPoison,
                Hidden = Hidden,
                ExtraByte = _stage,
            };
        }

    }
}
