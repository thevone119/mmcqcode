using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;
using System.Drawing;
using System;

namespace Server.MirObjects.Monsters
{
    public class HellLord : MonsterObject
    {   
        protected override bool CanMove { get { return false; } }
        protected override bool CanRegen { get { return false; } }

        private byte _stage = 0;
        private bool _begin = false;

        private bool _raged = false;
        private long _rageDelay = Settings.Minute * 2;
        private long _rageTime;

        private int _bombChance = 3;
        private int _bombSpreadMin = 5;
        private int _bombSpreadMax = 20;

        private int _quakeSpreadMin = 5;
        private int _quakeSpreadMax = 15;
        private int _quakeCount = 5;

        protected internal HellLord(MonsterInfo info) : base(info)
        {
            Direction = MirDirection.Up;
            _begin = true;
        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;

            return true;
        }

        public override void Turn(MirDirection dir)
        {
        }
        public override bool Walk(MirDirection dir) { return false; }


        public override int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility)
        {
            if (_stage >= 4)
            {
                return base.Attacked(attacker, damage, type);
            }

            return 0;
        }
        public override int Attacked(PlayerObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            if (_stage >= 4)
            {
                return base.Attacked(attacker, damage, type, damageWeapon);
            }

            return 0;
        }

        public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true)
        {

        }

        public override void Process()
        {
            if(CurrentMap.Players.Count == 0 && _stage > 0)
            {
                _stage = 0;
                _begin = true;
            }

            base.Process();
        }

        protected override void ProcessTarget()
        {
            if (CurrentMap.Players.Count == 0) return;

            if (!CanAttack) return;

            if (_raged && _rageTime < Envir.Time && _stage < 4 || _begin)
            {
                if (_begin)
                {
                    _begin = false;
                }

                _raged = false;

                SpawnKnight();

                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });
            }

            if (RandomUtils.Next(_bombChance) == 0 || _raged)
            {
                SpawnBomb();
            }

            SpawnQuakes();

            ActionTime = Envir.Time + 600;
            AttackTime = Envir.Time + AttackSpeed;
        }

        protected override void Attack()
        {
           
        }

        private void SpawnQuakes()
        {
            int count = RandomUtils.Next(1, _raged ? _quakeCount * 2 : _quakeCount);
            int distance = RandomUtils.Next(_quakeSpreadMin, _quakeSpreadMax);

            for (int j = 0; j < CurrentMap.Players.Count; j++)
            {
                Point playerLocation = CurrentMap.Players[j].CurrentLocation;

                for (int i = 0; i < count; i++)
                {
                    Point location = new Point(playerLocation.X + RandomUtils.Next(-distance, distance + 1),
                                             playerLocation.Y + RandomUtils.Next(-distance, distance + 1));

                    if(RandomUtils.Next(10) == 0)
                    {
                        location = playerLocation;
                    }

                    if (!CurrentMap.ValidPoint(location)) continue;

                    SpellObject spellObj = null;

                    switch (RandomUtils.Next(2))
                    {
                        case 0:
                            {
                                spellObj = new SpellObject
                                {
                                    Spell = Spell.MapQuake1,
                                    Value = RandomUtils.Next(RandomUtils.Next(MinDC, MaxDC)),
                                    ExpireTime = Envir.Time + (2000),
                                    TickSpeed = 500,
                                    Caster = null,
                                    CurrentLocation = location,
                                    CurrentMap = CurrentMap,
                                    Direction = MirDirection.Up
                                };
                            }
                            break;
                        case 1:
                            {
                                spellObj = new SpellObject
                                {
                                    Spell = Spell.MapQuake2,
                                    Value = RandomUtils.Next(RandomUtils.Next(MinDC, MaxDC)),
                                    ExpireTime = Envir.Time + (2000),
                                    TickSpeed = 500,
                                    Caster = null,
                                    CurrentLocation = location,
                                    CurrentMap = CurrentMap,
                                    Direction = MirDirection.Up
                                };
                            }
                            break;
                    }

                    DelayedAction action = new DelayedAction(DelayedType.Spawn, Envir.Time + RandomUtils.Next(5000), spellObj);
                    CurrentMap.ActionList.Add(action);
                }
            }

        }
        //刷新小怪，这里控制下，不要无限刷新？
        private void SpawnBomb()
        {
            //当前地图怪物超过400，不刷新了
            if (CurrentMap.MonsterCount > 400)
            {
                return;
            }

            int NearCount = 0;
            //周围7格的怪物超过100个，也不刷新了
            for (int d = 0; d <= 7; d++)
            {
                for (int y = CurrentLocation.Y - d; y <= CurrentLocation.Y + d; y++)
                {
                    if (y < 0) continue;
                    if (y >= CurrentMap.Height) break;

                    for (int x = CurrentLocation.X - d; x <= CurrentLocation.X + d; x += Math.Abs(y - CurrentLocation.Y) == d ? 1 : d * 2)
                    {
                        if (x < 0) continue;
                        if (x >= CurrentMap.Width) break;

                        //Cell cell = CurrentMap.GetCell(x, y);
                        if (!CurrentMap.Valid(x, y) || CurrentMap.Objects[x, y] == null) continue;
                        NearCount+=CurrentMap.Objects[x, y].Count;
                    }
                }
            }
            if (NearCount > 100)
            {
                return;
            }

            int distance = RandomUtils.Next(_bombSpreadMin, _bombSpreadMax);

            for (int j = 0; j < CurrentMap.Players.Count; j++)
            {
                Point playerLocation = CurrentMap.Players[j].CurrentLocation;

                Point location = new Point(playerLocation.X + RandomUtils.Next(-distance, distance + 1),
                                             playerLocation.Y + RandomUtils.Next(-distance, distance + 1));

                MonsterObject mob = null;
                switch (RandomUtils.Next(3))
                {
                    case 0:
                        mob = GetMonster(Envir.GetMonsterInfo(Settings.HellBomb1));
                        break;
                    case 1:
                        mob = GetMonster(Envir.GetMonsterInfo(Settings.HellBomb2));
                        break;
                    case 2:
                        mob = GetMonster(Envir.GetMonsterInfo(Settings.HellBomb3));
                        break;
                }

                if (mob == null) return;

                mob.Spawn(CurrentMap, location);
            }     
        }

        private void SpawnKnight()
        {
            MonsterObject mob = null;

            switch (_stage)
            {
                case 0:
                    mob = GetMonster(Envir.GetMonsterInfo(Settings.HellKnight1));
                    break;
                case 1:
                    mob = GetMonster(Envir.GetMonsterInfo(Settings.HellKnight2));
                    break;
                case 2:
                    mob = GetMonster(Envir.GetMonsterInfo(Settings.HellKnight3));
                    break;
                case 3:
                    mob = GetMonster(Envir.GetMonsterInfo(Settings.HellKnight4));
                    break;
            }

            if (mob == null || !(mob is HellKnight)) return;

            HellKnight knight = (HellKnight)mob;
            knight.Owner = this;
            knight.Lord = this;

            Point front = Functions.PointMove(CurrentLocation, MirDirection.DownLeft, 12);

            for (int i = 0; i < 50; i++)
            {
                Point location = new Point(front.X + RandomUtils.Next(-10, 10),
                                         front.Y + RandomUtils.Next(-10, 10));

                if (CurrentMap.ValidPoint(location))
                {
                    DelayedAction action = new DelayedAction(DelayedType.Spawn, Envir.Time + 500, knight, location);
                    CurrentMap.ActionList.Add(action);
                    break;
                }
            }
        }

        public void KnightKilled()
        {
            _rageTime = Envir.Time + _rageDelay;
            _raged = true;

            _stage += 1;

            Broadcast(GetInfo());
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
