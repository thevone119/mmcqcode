using System;
using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    //召唤蜘蛛,改为远程攻击
    public class VampireSpider : MonsterObject
    {
        public bool Summoned;
        public long AliveTime;
        public long deadTime;

        protected internal VampireSpider(MonsterInfo info) : base(info)
        {
            ActionTime = Envir.Time + 1000;
        }

        public override string Name
        {
            get { return Master == null ? Info.GameName : (Dead ? Info.GameName : string.Format("{0}({1})", Info.GameName, Master.Name)); }
            set { throw new NotSupportedException(); }
        }

        public override void Process()
        {
            //自动死亡处理，主人离开超过15格，或者超过时间，自动死亡
            if (!Dead && Summoned)
            {
                if (Master != null)
                {
                    bool selfDestruct = false;
                    if (FindObject(Master.ObjectID, 15) == null) selfDestruct = true;
                    if (Summoned && Envir.Time > AliveTime) selfDestruct = true;
                    if (selfDestruct && Master != null) Die();
                }
            }

            base.Process();
        }

        public override void Process(DelayedAction action)
        {
            switch (action.Type)
            {
                case DelayedType.Damage:
                    CompleteAttack(action.Params);
                    break;
                case DelayedType.RangeDamage:
                    CompleteRangeAttack(action.Params);
                    break;
                case DelayedType.Recall:
                    PetRecall((MapObject)action.Params[0]);
                    break;
            }
        }

        public void PetRecall(MapObject target)
        {
            if (target == null) return;
            if (Master == null) return;
            Teleport(Master.CurrentMap, target.CurrentLocation);
        }

        protected override void ProcessAI()
        {
            if (Dead) return;

            ProcessSearch();
            //todo ProcessRoaming(); needs no master follow just target roaming
            ProcessTarget();
        }

        public override void Die()
        {
            base.Die();

            //Explosion
            for (int y = CurrentLocation.Y - 1; y <= CurrentLocation.Y + 1; y++)
            {
                if (y < 0) continue;
                if (y >= CurrentMap.Height) break;

                for (int x = CurrentLocation.X - 1; x <= CurrentLocation.X + 1; x++)
                {
                    if (x < 0) continue;
                    if (x >= CurrentMap.Width) break;

                    //Cell cell = CurrentMap.GetCell(x, y);

                    if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                    for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                    {
                        MapObject target = CurrentMap.Objects[x, y][i];
                        switch (target.Race)
                        {
                            case ObjectType.Monster:
                            case ObjectType.Player:
                                //Only targets
                                if (!target.IsAttackTarget(this) || target.Dead) break;
                                int value = target.Attacked(this,10*PetLevel,DefenceType.MACAgility);
                                if (value <= 0) break;
                                if (Master != null) MasterVampire(value, target);
                                break;
                        }
                    }
                }
            }
        }
        //是否在攻击范围，改为远程攻击
        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;
            //这里改为远程攻击
            return Functions.InRange(CurrentLocation, Target.CurrentLocation, Info.ViewRange);
            /**
            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > 1 || y > 1) return false;
            return (x <= 1 && y <= 1) || (x == y || x % 2 == y % 2);
            **/
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

            AttackLogic();

            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;
            ShockTime = 0;

            if (Target.Dead) FindTarget();
        }

        private void AttackLogic()
        {
            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

            int value = 0;
            value = Target.Attacked(this, damage, DefenceType.Agility);
            if (value > 0 && Master != null) MasterVampire(value, Target);
        }

        private void MasterVampire(int value, MapObject ob)
        {
            if (Master == null) return;
            if (Master.VampAmount == 0) ((PlayerObject)Master).VampTime = Envir.Time + 1000;
            Master.VampAmount += (ushort)(value * (PetLevel + 1) * 0.25F);
            ob.Broadcast(new S.ObjectEffect { ObjectID = ob.ObjectID, Effect = SpellEffect.Bleeding, EffectType = 0 });
        }

        public override void Spawned()
        {
            base.Spawned();
            Summoned = true;
        }

        public override Packet GetInfo()
        {
            return new S.ObjectMonster
                {
                    ObjectID = ObjectID,
                    Name = Name,
                    NameColour = NameColour,
                    Location = CurrentLocation,
                    Image = Monster.VampireSpider,
                    Direction = Direction,
                    Effect = Info.Effect,
                    AI = Info.AI,
                    Light = Info.Light,
                    Dead = Dead,
                    Skeleton = Harvested,
                    Poison = CurrentPoison,
                    Hidden = Hidden,
                    Extra = Summoned,
                };
        }

    }
}
