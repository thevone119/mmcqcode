using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  Monster453 巨斧妖 毒妖林小BOSS
    ///  1.普通攻击
    ///  2.旋转
    ///  3.旋转，跑到某个点
    ///  4.释放技能，举手
    ///  5.开天斩
    ///  6.飞虎头
    /// </summary>
    public class Monster453 : MonsterObject
    {
        private byte atype = 1;
        private long PoisonTime = 0;//可被中毒的时间
        private long atype4Time = 0;//可以放技能4的时间

        //是否可以活动
        private bool _canMove = false;
        private long _checkMoveTime = 0;//AI计算的时间
        private string preMonName = "多毒妖";


        protected internal Monster453(MonsterInfo info)
            : base(info)
        {
        }

        private void checkCanMove()
        {
            if (!Dead && Envir.Time > _checkMoveTime)
            {
                _checkMoveTime = Envir.Time + 5000;
                bool has = false;
                foreach (MapObject ob in SMain.Envir.Objects)
                {
                    if (ob == null || ob.Dead || ob.CurrentMap != CurrentMap || ob.Race != ObjectType.Monster)
                    {
                        continue;
                    }
                    if (preMonName.Equals(ob.Name))
                    {
                        has = true;
                        break;
                    }
                }
                if (has)
                {
                    _canMove = false;
                    MoveTime = Envir.Time + 5200;
                    AttackTime = Envir.Time + 5200;
                }
                else
                {
                    _canMove = true;
                }
            }
        }

        protected override void ProcessAI()
        {
            checkCanMove();
            base.ProcessAI();
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

            atype = 0;
            if (distance > 4)
            {
                atype = 6;
            }
            if(Envir.Time > atype4Time && RandomUtils.Next(100) < 20 && this.HP<this.MaxHP*2/3)
            {
                atype = 4;
            }
            if (atype == 0)
            {
                int rd = RandomUtils.Next(100);
                if (rd < 50)
                {
                    if (distance <= 2 )
                    {
                        atype = 1;
                    }
                    else
                    {
                        atype = 6;
                    }
                }else if (rd < 85)
                {
                    Point movepoint = Functions.PointMove(CurrentLocation, Direction, 3);
                    if (CurrentMap.EmptyPoint(movepoint.X, movepoint.Y))
                    {
                        atype = 3;
                    }
                    else
                    {
                        atype = 5;
                    }
                }else{
                    atype = 5;
                }
            }



            if (atype == 1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            if (atype == 2)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                List<MapObject> listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 2);
                for (int o = 0; o < listtargets.Count; o++)
                {
                    MapObject ob = listtargets[o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;
                    if (ob.Race == ObjectType.Monster)
                    {
                        damage = damage * 2;
                    }
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MAC);
                    ActionList.Add(action);
                }
            }
            if (atype == 3)
            {
                //向某个方向旋转，跑动旋转
                Point movepoint = Functions.PointMove(CurrentLocation, Direction, 3);
                if (CurrentMap.EmptyPoint(movepoint.X, movepoint.Y))
                {
                    Run(Direction,3,false);
                }
                LineAttack1(damage * 4 / 3, 3);
            }
            //释放技能
            if (atype == 4)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type =2 });
                PoisonTime = Envir.Time + RandomUtils.Next(8, 15) * 1000;
                atype4Time = Envir.Time + RandomUtils.Next(25, 30) * 1000;
                int rcount = RandomUtils.Next(2, 5);
                //一次放2-4个毒，随机放，如果重复了，就不放
                for (int i = 0; i < rcount; i++)
                {
                    Point p = CurrentMap.RandomValidPoint(CurrentLocation.X, CurrentLocation.Y, 6, 1);
                    //随机抽取5个点，放毒
                    SpellObject spellObj = new SpellObject
                    {
                        Spell = Spell.MonRotateAxe,
                        Value = damage,
                        ExpireTime = Envir.Time + 1000 * RandomUtils.Next(20, 30),
                        TickSpeed = 2000,
                        Caster = null,
                        MonCaster = this,
                        CurrentLocation = p,
                        CurrentMap = CurrentMap,
                        Direction = MirDirection.Up
                    };
                    action = new DelayedAction(DelayedType.Spawn, Envir.Time + 500, spellObj);
                    CurrentMap.ActionList.Add(action);
                }
                //放一个旗帜
                for (int i = 0; i < 1; i++)
                {
                    Point p = CurrentMap.RandomValidPoint(CurrentLocation.X, CurrentLocation.Y, 6, 1);
                    //随机抽取5个点，放毒
                    SpellObject spellObj = new SpellObject
                    {
                        Spell = Spell.MonGhostFlag1,
                        Value = damage,
                        ExpireTime = Envir.Time + 1000 * RandomUtils.Next(15, 20),
                        TickSpeed = 3000,
                        Caster = null,
                        MonCaster = this,
                        CurrentLocation = p,
                        CurrentMap = CurrentMap,
                        Direction = MirDirection.Up
                    };
                    action = new DelayedAction(DelayedType.Spawn, Envir.Time + 500, spellObj);
                    CurrentMap.ActionList.Add(action);
                }
            }
            //开天斩
            if (atype == 5)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 3 });
                LineAttack2(damage * 2, 4);
            }
            //飞虎头
            if (atype == 6)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }


            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        protected override void ProcessTarget()
        {
            if (Target == null) return;

            if (InAttackRange() && CanAttack)
            {
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

            if (!CanMove || Target == null) return;
            MoveTo(Target.CurrentLocation);
           
        }

        public override int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility)
        {
            if (!_canMove)
            {
                return 0;
            }
            return base.Attacked(attacker, damage, type);
        }
        public override int Attacked(PlayerObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            if (!_canMove)
            {
                return 0;
            }

            return base.Attacked(attacker, damage, type, damageWeapon);
        }
        //有护盾的时候无法中毒
        public override void ApplyPoison(Poison p, MapObject Caster = null, bool NoResist = false, bool ignoreDefence = true)
        {
            if (!_canMove)
            {
                return;
            }
            if (Envir.Time > PoisonTime)
            {
                base.ApplyPoison(p, Caster, NoResist, ignoreDefence);
            }
        }

        //旋转斩
        //当前位置起，周边2格，周边的攻击1.3倍
        private void LineAttack1(int damage,int distance)
        {
            DelayedAction action = null;
            Dictionary<uint, uint> dic = new Dictionary<uint, uint>();
            for (int i = 0; i <= distance; i++)
            {
                Point target = Functions.PointMove(CurrentLocation, Direction, i);
                List<MapObject> listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 2);
                for (int o = 0; o < listtargets.Count; o++)
                {
                    MapObject ob = listtargets[o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;
               
                    if (dic.ContainsKey(ob.ObjectID))
                    {
                        continue;
                    }
                    dic[ob.ObjectID] = 1;
                    if (ob.Race == ObjectType.Monster)
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, ob, damage*2, DefenceType.ACAgility);
                        ActionList.Add(action);
                    }
                    else
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, ob, damage, DefenceType.ACAgility);
                        ActionList.Add(action);
                    }
                }
            }
        }
        //开天斩
        private void LineAttack2(int damage, int distance)
        {
            DelayedAction action = null;
            Dictionary<uint, uint> dic = new Dictionary<uint, uint>();
            for (int i = 1; i <= distance; i++)
            {
                Point target = Functions.PointMove(CurrentLocation, Direction, i);
                List<MapObject> listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 2);
                for (int o = 0; o < listtargets.Count; o++)
                {
                    MapObject ob = listtargets[o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;
                    if (dic.ContainsKey(ob.ObjectID))
                    {
                        continue;
                    }
                    dic[ob.ObjectID] = 1;

                    if (ob.Race == ObjectType.Monster)
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, ob, damage*2, DefenceType.MACAgility);
                        ActionList.Add(action);
                    }
                    else
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, ob, damage, DefenceType.MACAgility);
                        ActionList.Add(action);
                    }
                }
            }
        }

    }
}
