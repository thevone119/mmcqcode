using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  WoodBox 爆炸箱子 
    ///  死亡的时候炸一下
    ///  魔法免疫，只能攻击
    /// </summary>
    public class WoodBox : MonsterObject
    {
     
        protected internal WoodBox(MonsterInfo info)
            : base(info)
        {
            Direction = (MirDirection)RandomUtils.Next(8);
        }
        protected override bool CanMove { get { return false; } }
        protected override bool CanAttack { get { return false; } }
        protected override bool CanRegen { get { return false; } }

        protected override void Attack() { }
        protected override void FindTarget() { }

        public override void Turn(MirDirection dir)
        {
        }
        public override bool Walk(MirDirection dir)
        {
            return false;
        }

        public override void Die()
        {
            //死亡的时候炸一下
            List<MapObject>  listtargets = FindAllTargets(1, CurrentLocation, false);
            int damage = GetAttackPower(MinDC, MaxDC);
            foreach (MapObject ob in listtargets)
            {
                if (ob.IsAttackTarget(this))
                {
                    DelayedAction action = new DelayedAction(DelayedType.RangeDamage, Envir.Time + 400, ob, damage, DefenceType.None);
                    ActionList.Add(action);
                }
            }
            base.Die();
        }

        protected override void CompleteRangeAttack(IList<object> data)
        {
            MapObject target = (MapObject)data[0];
            int damage = (int)data[1];
            DefenceType defence = (DefenceType)data[2];

            if (target == null || !target.IsAttackTarget(this) || target.CurrentMap != CurrentMap || target.Node == null) return;
            //超过1格，炸不到
            int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);

            if (distance > 1)
            {
                return;
            }
            target.Attacked(this, damage, defence);
        }

        protected override void ProcessTarget() { }

        protected override void ProcessRegen() { }
        protected override void ProcessSearch() { }
        protected override void ProcessRoam() { }

        public override int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility)
        {
            int armour = 0;

            switch (type)
            {
                case DefenceType.ACAgility:
                    if (RandomUtils.Next(Agility + 1) > attacker.Accuracy) return 0;
                    armour = GetDefencePower(MinAC, MaxAC);
                    break;
                case DefenceType.AC:
                    armour = GetDefencePower(MinAC, MaxAC);
                    break;
                case DefenceType.MACAgility:
                    if (RandomUtils.Next(Agility + 1) > attacker.Accuracy) return 0;
                    armour = GetDefencePower(MinMAC, MaxMAC);
                    break;
                case DefenceType.MAC:
                    armour = GetDefencePower(MinMAC, MaxMAC);
                    break;
                case DefenceType.Agility:
                    if (RandomUtils.Next(Agility + 1) > attacker.Accuracy) return 0;
                    break;
            }

            if (armour >= damage) return 0;

            ShockTime = 0;

            if (attacker.Info.AI == 6)
                EXPOwner = null;
            else if (attacker.Master != null)
            {
                if (EXPOwner == null || EXPOwner.Dead)
                    EXPOwner = attacker.Master;

                if (EXPOwner == attacker.Master)
                    EXPOwnerTime = Envir.Time + EXPOwnerDelay;

            }

            Broadcast(new S.ObjectStruck { ObjectID = ObjectID, AttackerID = attacker.ObjectID, Direction = Direction, Location = CurrentLocation });

            ChangeHP(-1);
            return 1;

        }

        public override int Struck(int damage, DefenceType type = DefenceType.ACAgility)
        {
            return 0;
        }

        public override int Attacked(PlayerObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            int armour = 0;

            switch (type)
            {
                case DefenceType.ACAgility:
                    if (RandomUtils.Next(Agility + 1) > attacker.Accuracy) return 0;
                    armour = GetDefencePower(MinAC, MaxAC);
                    break;
                case DefenceType.AC:
                    armour = GetDefencePower(MinAC, MaxAC);
                    break;
                case DefenceType.MACAgility:
                    if (RandomUtils.Next(Agility + 1) > attacker.Accuracy) return 0;
                    armour = GetDefencePower(MinMAC, MaxMAC);
                    break;
                case DefenceType.MAC:
                    armour = GetDefencePower(MinMAC, MaxMAC);
                    break;
                case DefenceType.Agility:
                    if (RandomUtils.Next(Agility + 1) > attacker.Accuracy) return 0;
                    break;
            }

            if (armour >= damage) return 0;

            if (damageWeapon)
                attacker.DamageWeapon();

            ShockTime = 0;

            if (Master != null && Master != attacker)
                if (Envir.Time > Master.BrownTime && Master.PKPoints < 200)
                    attacker.BrownTime = Envir.Time + Settings.Minute;

            if (EXPOwner == null || EXPOwner.Dead)
                EXPOwner = attacker;

            if (EXPOwner == attacker)
                EXPOwnerTime = Envir.Time + EXPOwnerDelay;

            Broadcast(new S.ObjectStruck { ObjectID = ObjectID, AttackerID = attacker.ObjectID, Direction = Direction, Location = CurrentLocation });
            attacker.GatherElement();
            ChangeHP(-1);

            return 1;
        }

        public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true) { }


    }
}
