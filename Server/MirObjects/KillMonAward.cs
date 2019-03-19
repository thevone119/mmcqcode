using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.MirObjects
{
    //杀怪奖励机制
    //配置信息
    //这个直接放到playerObject里了
    public class KillMonAward
    {
        public List<string> KillMonNameList = new List<string>();//杀怪名称列表
        public int KillMonCount1 = 500;
        public int KillMonCount2 = 500;
        public string KillMonRemind1 = "";//杀怪提醒
        public string KillMonRemind2 = "";//杀怪提醒


    }


}
