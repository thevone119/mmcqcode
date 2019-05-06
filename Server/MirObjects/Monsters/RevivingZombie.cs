using Server.MirDatabase;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    class RevivingZombie : MonsterObject
    {
        public uint RevivalCount;
        public int LifeCount;
        public long RevivalTime, DieTime;

        public override uint Experience
        {
            get { return Info.Experience * (100 - (25 * RevivalCount)) / 100; }
        }

        protected internal RevivingZombie(MonsterInfo info)
            : base(info)
        {
            RevivalCount = 0;
            LifeCount = RandomUtils.Next(2);//这里改下，最多复活2次
        }

        public override void Die()
        {
            DieTime = Envir.Time;
            RevivalTime = (4 + RandomUtils.Next(20)) * 1000;
            base.Die();
        }


        protected override void ProcessAI()
        {
            if (Dead && Envir.Time > DieTime + RevivalTime && RevivalCount < LifeCount)
            {
                RevivalCount++;
                //复活的把爆率减一倍
       
                uint newhp = MaxHP * (100 - (25 * RevivalCount)) / 100;
                Revive(newhp, false);
            }

            base.ProcessAI();
        }
    }
}