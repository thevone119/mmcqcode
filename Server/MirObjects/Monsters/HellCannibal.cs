using System;
using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    public class HellCannibal : MonsterObject
    {
        protected internal HellCannibal(MonsterInfo info)
            : base(info)
        {
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);

            ShockTime = 0;
            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;

            if (RandomUtils.Next(5) > 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });

                int damage = GetAttackPower(MinDC, MaxDC);
                if (damage == 0) return;

                Target.Attacked(this, damage, DefenceType.ACAgility);
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });

                MirDirection dir = Functions.PreviousDir(Direction);
                Point location = CurrentLocation;
                //Cell cell;

                for (int y = location.Y - 2; y <= location.Y + 2; y++)
                {
                    if (y < 0) continue;
                    if (y >= CurrentMap.Height) break;

                    for (int x = location.X - 2; x <= location.X + 2; x++)
                    {
                        if (x < 0) continue;
                        if (x >= CurrentMap.Width) break;

                        //cell = CurrentMap.GetCell(x, y);

                        if (!CurrentMap.Valid(x,y) || CurrentMap.Objects[x,y] == null) continue;

                        for (int i = 0; i < CurrentMap.Objects[x, y].Count; i++)
                        {
                            MapObject target = CurrentMap.Objects[x, y][i];
                            switch (target.Race)
                            {
                                case ObjectType.Player:
                                case ObjectType.Monster:
                                    {
                                        if (!target.IsAttackTarget(this)) continue;
                                        if (RandomUtils.Next(Settings.MagicResistWeight) < target.MagicResist) continue;

                                        target.ApplyPoison(new Poison { PType = PoisonType.Red, Duration = RandomUtils.Next(10,30), TickSpeed = 1000 }, this);
                                    }
                                    break;
                            }
                        }
                    }
                }

            }
        }
    }
}
