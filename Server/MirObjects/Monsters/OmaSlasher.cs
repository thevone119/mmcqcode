using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  奥玛斧头兵 破防
    ///  1种攻击，半月范围攻击，破防
    /// </summary>
    public class OmaSlasher : MonsterObject
    {
     
        protected internal OmaSlasher(MonsterInfo info)
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
            int damage = GetAttackPower(MinDC, MaxDC);
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            int delay = distance * 50 + 750; //50 MS per Step


            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });

            //半月方向
            MirDirection dir = Functions.PreviousDir(Direction);
            for (int i = 0; i < 4; i++)
            {
                Point target = Functions.PointMove(CurrentLocation, dir, 1);
                dir = Functions.NextDir(dir);
                if (!CurrentMap.ValidPoint(target)) continue;
                //cell = CurrentMap.GetCell(target);

                if (CurrentMap.Objects[target.X, target.Y] == null) continue;

                for (int o = 0; o < CurrentMap.Objects[target.X, target.Y].Count; o++)
                {
                    MapObject ob = CurrentMap.Objects[target.X, target.Y][o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;

                    DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.None);
                    ActionList.Add(action);
                    break;
                }
            }
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

    }
}
