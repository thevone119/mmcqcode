using Server.MirDatabase;
using Server.MirObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using S = ServerPackets;

namespace Server.MirEnvir
{
    //地图特殊处理基类
    //副本地图实现，怪物攻城等实现，都继承此类，注入地图中进行实现
    public abstract class MapSpecialProcess
    {
        public static Envir Envir
        {
            get { return SMain.Envir; }
        }

        //地图处理，传入地图Map
        public abstract void Process(Map map);

        //支持的3个参数
        public int param1;//参数1
        public int param2;//参数2
        public int param3;//参数3

        //虚函数，子类可以实现覆盖
        public virtual string getTitle()
        {
            return null;
        }

        //虚函数，子类可以实现覆盖
        public virtual void monDie(MonsterObject mon)
        {

        }


    }



}
