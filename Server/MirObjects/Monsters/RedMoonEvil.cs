using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    //赤月恶魔
    class RedMoonEvil : MonsterObject
    {
        protected override bool CanMove { get { return false; } }
        //protected override bool CanRegen { get { return false; } }
        

        protected internal RedMoonEvil(MonsterInfo info) : base(info)
        {
            Direction = MirDirection.Up;

            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;
        }

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;

            return Target.CurrentLocation != CurrentLocation && Functions.InRange(CurrentLocation, Target.CurrentLocation, Info.ViewRange);
        }

        public override void Turn(MirDirection dir)
        {
        }
        public override bool Walk(MirDirection dir) { return false; }


       
        //public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false) { }

        protected override void ProcessTarget()
        {
            if (!CanAttack) return;

            List<MapObject> targets = FindAllTargets(Info.ViewRange, CurrentLocation);
            if (targets.Count == 0) return;

            ShockTime = 0;

            Broadcast(new S.ObjectAttack {ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation});
            for (int i = 0; i < targets.Count; i++)
            {
                Target = targets[i];
                Attack();
            }


            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;
        }

        protected override void Attack()
        {

            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

            Target.Attacked(this, damage, DefenceType.ACAgility);

            Broadcast(new S.ObjectEffect{ ObjectID = Target.ObjectID, Effect = SpellEffect.RedMoonEvil});
        }

    }
}
