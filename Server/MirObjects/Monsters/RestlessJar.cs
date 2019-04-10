using Server.MirDatabase;
using Server.MirEnvir;
using System.Collections.Generic;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    //这个才是4眼神炉
    //这个不动
    public class RestlessJar : MonsterObject
    {
        //狂暴几率，根据血量得到相应的狂暴几率
        public int RageRate=10;
        //攻击范围12格
        public byte AttackRange = 12;
        private long dieTime;

        protected internal RestlessJar(MonsterInfo info)
            : base(info)
        {
            //视野内均可攻击，具体视野范围，看数据库的配置了
            //AttackRange = info.ViewRange;
        }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
        }

        //怪物死亡
        //触发壶中天
        public override void Die()
        {
            //发送全局的
            dieTime = Envir.Time;
            foreach (PlayerObject p in CurrentMap.Players)
            {
                p.ReceiveChat($"壶中天被击碎，2分钟后自动进入壶中天内部世界", ChatType.Announcement);
            }
            base.Die();
        }
        protected override void ProcessAI()
        {   
            //死亡2分钟后，把玩家传送到壶中天地图,只传送一次
            if (Dead && Envir.Time > dieTime+2*Settings.Minute)
            {
                dieTime = dieTime + 10 * Settings.Minute;
                Map map = SMain.Envir.GetMapByNameAndInstance("SP001");
                if (map != null)
                {
                    List<PlayerObject> list = new List<PlayerObject>();
                    foreach (PlayerObject p in CurrentMap.Players)
                    {
                        list.Add(p);
                    }
                    foreach (PlayerObject p in list)
                    {
                        p.TeleportRandom(10, 10, map);
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

            ShockTime = 0;
            

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);

            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;

            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

            if (HP < MaxHP*3 / 4  )
            {
                RageRate = 8;
            }
            if (HP < MaxHP*2 / 4 )
            {
                RageRate = 4;
            }
            if (HP < MaxHP*1 / 4 )
            {
                RageRate = 2;
            }

            //狂暴群体攻击，攻击系数乘1.5
            if (RandomUtils.Next(RageRate) == 0)
            {
                damage = (int)(damage * 1.5);
                List<MapObject> targets = FindAllTargets(AttackRange, CurrentLocation, false);
                for (int i = 0; i < targets.Count; i++)
                {
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = targets[i].ObjectID,Type=1 });
                    DelayedAction action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 800, targets[i], damage, DefenceType.MACAgility);
                    ActionList.Add(action);
                }
            }
            else
            {
                //单体攻击
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID});
                DelayedAction action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 800, Target, damage, DefenceType.MACAgility);
                ActionList.Add(action);
            }

            if (Target.Dead)
                FindTarget();

        }

        protected override void CompleteRangeAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;

            if (target.Attacked(this, damage, defence) <= 0) return;

            if (RandomUtils.Next(RageRate) == 0)
            {
                target.Pushed(this, Direction, 3);
            }
        }

    }
}
