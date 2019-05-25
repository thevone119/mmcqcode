using Server.MirDatabase;
using System;
using Server.MirEnvir;
using S = ServerPackets;
using System.Drawing;

namespace Server.MirObjects.Monsters
{
    //奥玛刺客
    //1.破隐身
    //2.瞬移
    class OmaAssassin : MonsterObject
    {

        protected internal OmaAssassin(MonsterInfo info)
            : base(info)
        {
            CoolEye = true;

        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > 2 || y > 2) return false;
            return (x == 0) || (y == 0) || (x == y);
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }
            int damage = GetAttackPower(MinDC, MaxDC);

            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation });

            DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, Target, damage, DefenceType.None);
            ActionList.Add(action);


            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;
            ShockTime = 0;
        }

        

        protected override void ProcessTarget()
        {
            if (Target == null || !CanAttack) return;

            if (InAttackRange())
            {
                //几率瞬移逃跑
                if (Target!=null && this.HP < this.MaxHP / 2 && RandomUtils.Next(2) == 0)
                {
                    Teleport(CurrentMap, CurrentMap.RandomValidPoint(CurrentLocation.X, CurrentLocation.Y,8));
                    return;
                }
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

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);
            //瞬移
            if ((x > 6 || y > 6) && RandomUtils.Next(3)==0)
            {
                if(CurrentMap.ValidPoint(Target.Back)){
                    Teleport(CurrentMap, Target.Back);
                    Attack();
                    return;
                }
                else if (CurrentMap.ValidPoint(Target.Front))
                {
                    Teleport(CurrentMap, Target.Front);
                    Attack();
                    return;
                }
            }
            MoveTo(Target.CurrentLocation);
        }

    }
}
