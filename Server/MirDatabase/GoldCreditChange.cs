using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.MirDatabase
{
    //金币元宝兑换
    public class GoldCreditChange
    {
        private static uint giveGold = 80000;
        private static uint giveCredit = 800;

        //元宝换金币
        public static uint CreditBuyGold()
        {
            uint retgold = giveGold;
            if(giveGold>= 50000 + 1000)
            {
                giveGold -= 1000;
            }
            if(giveCredit<= 800 - 10)
            {
                giveCredit += 10;
            }
            return retgold;
        }

        //金币换元宝
        public static uint GoldBuyCredit()
        {
            uint retCredit = giveCredit;
            if (giveGold <= 80000 - 1000)
            {
                giveGold += 1000;
            }
            if (giveCredit >= 500 + 10)
            {
                giveCredit -= 10;
            }
            return retCredit;
        }
    }
}
