using System;
using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    //鬼魅蛇
    public class CharmedSnake : MonsterObject
    {
        public bool Summoned;
        public long AliveTime;
        public MapObject MasterTotem;

        protected internal CharmedSnake(MonsterInfo info) : base(info)
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
            if (!Dead && Summoned)
            {
                bool selfDestruct = false;
                if (Master != null)
                {
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
            }
        }

       // protected override void ProcessAI()
       // {
           // ProcessSearch();
           // ProcessRoam();
           // ProcessTarget();
        //}

        protected override void ProcessAI()
        {
            if (Dead) return;

            ProcessSearch();
            //todo ProcessRoaming(); needs no master follow just target roaming
            ProcessTarget();
        }

        protected override void ProcessSearch()
        {
            if (Envir.Time < SearchTime) return;
            SearchTime = Envir.Time + SearchDelay;

            //Stacking or Infront of master - Move
            bool stacking = false;

            //Cell cell = CurrentMap.GetCell(CurrentLocation);

            if (CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y] != null)
                for (int i = 0; i < CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y].Count; i++)
                {
                    MapObject ob = CurrentMap.Objects[CurrentLocation.X, CurrentLocation.Y][i];
                    if (ob == this || !ob.Blocking) continue;
                    stacking = true;
                    break;
                }

            if (CanMove && ((MasterTotem != null && MasterTotem.Front == CurrentLocation) || stacking))
            {
                //Walk Randomly
                if (!Walk(Direction))
                {
                    MirDirection dir = Direction;

                    switch (RandomUtils.Next(3)) // favour Clockwise
                    {
                        case 0:
                            for (int i = 0; i < 7; i++)
                            {
                                dir = Functions.NextDir(dir);

                                if (Walk(dir))
                                    break;
                            }
                            break;
                        default:
                            for (int i = 0; i < 7; i++)
                            {
                                dir = Functions.PreviousDir(dir);

                                if (Walk(dir))
                                    break;
                            }
                            break;
                    }
                }
            }

            if (Target == null || RandomUtils.Next(3) == 0) FindTarget();
        }
        protected override void ProcessRoam()
        {
            if (Target != null || Envir.Time < RoamTime) return;

            if (ProcessRoute()) return;

            if (MasterTotem != null)
            {
                MoveTo(MasterTotem.Back);
                return;
            }

            RoamTime = Envir.Time + RoamDelay;
            if (RandomUtils.Next(10) != 0) return;

            switch (RandomUtils.Next(3)) //Face Walk
            {
                case 0:
                    Turn((MirDirection)RandomUtils.Next(8));
                    break;
                default:
                    Walk(Direction);
                    break;
            }
        }
        protected override void ProcessTarget()
        {
            if (Target == null || !CanAttack) return;

            if (InAttackRange())
            {
                Attack();
                if (Target.Dead)
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

            //if (Target.Attacked(this, damage, DefenceType.MAC) <= 0) return;
            Target.Attacked(this, damage, DefenceType.Agility);

            if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
            {
                if (RandomUtils.Next(10) <= PetLevel)
                {
                    Target.ApplyPoison(new Poison { PType = PoisonType.Paralysis, Duration = 4 + PetLevel, TickSpeed = 1000 }, this);
                }
            }
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
                                target.Attacked(this, 10 * PetLevel, DefenceType.Agility);
                                break;
                        }
                    }
                }
            }
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
                Image = Monster.CharmedSnake,
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
