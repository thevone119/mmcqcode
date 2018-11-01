using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Server.MirEnvir
{
    //关于各种重生的
    public class RespawnSave
    {
        public bool Spawned = false;
        public ulong NextSpawnTick = 0;
        public int RespawnIndex = 0;

        public RespawnSave()
        { }

        public RespawnSave(BinaryReader reader)
        {
            Spawned = reader.ReadBoolean();
            NextSpawnTick = reader.ReadUInt64();
            RespawnIndex = reader.ReadInt32();
        }

        public void save(BinaryWriter writer)
        {
            writer.Write(Spawned);
            writer.Write(NextSpawnTick);
            writer.Write(RespawnIndex);
        }
    }

    public class RespawnTickOption
    {
        public int UserCount = 1;//用户数
        public double DelayLoss = 1.0;//延时

        public RespawnTickOption()
        {

        }
        public RespawnTickOption(BinaryReader reader)
        {
            UserCount = reader.ReadInt32();
            DelayLoss = reader.ReadDouble();
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(UserCount);
            writer.Write(DelayLoss);
        }

        public override string ToString()
        {
            return string.Format("+{0} users", UserCount);
        }
    }

    //怪物重生时间
    public class RespawnTimer
    {
        public byte BaseSpawnRate = 20;//amount of minutes between respawnticks (with no bonus)//计数间隔
        public ulong CurrentTickcounter = 0; //counter used to respawn everything//当前已经使用计数
        public long LastTick = 0; //what 'time' was the last tick?//最后的计数器(最后的时间)
        public int LastUsercount = 0; //stops it from having to check delay each time,每次超时的用户数(最后的用户数)
        public long CurrentDelay = 0;
        public List<RespawnTickOption> Respawn = new List<RespawnTickOption>();

        public RespawnTimer()
        {
            RespawnTickOption Option = new RespawnTickOption { UserCount = 0, DelayLoss = 1.0 };
            Respawn.Add(Option);
            //LastTick = SMain.Envir.Time;
        }

        public RespawnTimer(BinaryReader reader)
        {
            BaseSpawnRate = reader.ReadByte();
            CurrentTickcounter = reader.ReadUInt64();
            LastTick = SMain.Envir.Time;
            Respawn.Clear();
            int Optioncount = reader.ReadInt32();
            for (int i = 0; i < Optioncount; i++)
            {
                RespawnTickOption Option = new RespawnTickOption(reader);
                Respawn.Add(Option);
            }
            CurrentDelay = (long)Math.Round((double)BaseSpawnRate * (double)60000);
        }
        public void Save(BinaryWriter writer)
        {
            writer.Write(BaseSpawnRate);
            writer.Write(CurrentTickcounter);
            writer.Write(Respawn.Count);
            foreach (RespawnTickOption Option in Respawn)
                Option.Save(writer);
        }


        public void Process()
        {
            //通过总是重新检查滴答的速度，我们减少了在用户数量快速上升或下降的情况下（比如在服务器重新启动之后）让重生变得愚蠢的机会。
            //by always rechecking tickspeed we reduce the chance of having respawns get silly on situations where usercount goes up or down fast (like say after a server reboot)
            GetTickSpeed();

            if (SMain.Envir.Time >= (LastTick + CurrentDelay))
            {
                CurrentTickcounter++;
                //通过使用长而不是龙在这里你基本上有一个巨大的安全区在复活的蜱群。
                if (CurrentTickcounter == long.MaxValue) //by using long instead of ulong here you basicaly have a huge safe zone on the respawn ticks of mobs
                {
                    CurrentTickcounter = 0;
                }
                LastTick = SMain.Envir.Time;
            }
        }

        public void GetTickSpeed()
        {
            //用户数没有变化则返回
            if (LastUsercount == SMain.Envir.PlayerCount) return;
            //
            LastUsercount = SMain.Envir.PlayerCount;
            double bonus = 1.0;
            foreach (RespawnTickOption Option in Respawn)
            {
                if (Option.UserCount <= LastUsercount)
                    bonus = Math.Min(bonus, Option.DelayLoss);
            }
            CurrentDelay = (long)Math.Round((BaseSpawnRate * 60000) * bonus);
        }
    }
}