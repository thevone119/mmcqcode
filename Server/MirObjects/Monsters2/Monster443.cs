using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///   Monster443 昆仑叛军道尊 小BOSS
    ///  3种攻击手段
    /// </summary>
    public class Monster443 : MonsterObject
    {

        private byte _stage = 0;//0:正常 1：毒免
        private long fireTime = 0;
        private long ProcessTime = 0;

        protected internal Monster443(MonsterInfo info)
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
                //吸血
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
            }
            else if(rd<80 && PoisonList.Count>0)
            {
                //治愈
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Target = Target.CurrentLocation, Location = CurrentLocation, Type = 2 });
                PoisonList.Clear();
                //血量恢复
                ChangeHP(damage * 3);
                //5-10秒内无法中毒
                _stage = 1;
                fireTime = Envir.Time + RandomUtils.Next(5, 15) * 1000;
   
            }
            else
            {
                //毒云
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Target = Target.CurrentLocation, Location = CurrentLocation, Type = 1 });
                action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 1000, Target, damage * 3 / 2, DefenceType.MAC, Target.CurrentLocation);
                ActionList.Add(action);

                action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 1800, Target, damage * 3 / 2, DefenceType.MAC, Target.CurrentLocation);
                ActionList.Add(action);

                action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 2500, Target, damage * 3 / 2, DefenceType.MAC, Target.CurrentLocation);
                ActionList.Add(action);
            }
            

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }


        //有护盾的时候无法中毒
        public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true)
        {
            if (_stage == 0)
            {
                base.ApplyPoison(p, Caster, NoResist, ignoreDefence);
            }
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

                if (__stage != _stage)
                {
                    Broadcast(GetInfo());
                }
            }
            base.ProcessAI();
        }

        protected override void CompleteRangeAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];
            Point point = (Point)data[3];
            List<MapObject>  tags = FindAllTargets(2,point);
            foreach(MapObject ob in tags)
            {
                ob.Attacked(this, damage, defence);
            }
        }





    }
}
