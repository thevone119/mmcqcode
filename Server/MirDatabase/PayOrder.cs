using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Server.MirDatabase;
using Server.MirNetwork;
using Server.MirObjects;
using ServerPackets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;

namespace Server.MirEnvir
{
    //支付订单
    public class PayOrder
    {
        //多线程锁
        private static object lockObj = new object();
        //所有的订单（1天以内的）
        public static List<PayOrder> listall = new List<PayOrder>();
        private static string uid = "662b292a41104fb28a0aa9507f22121d";
        private static string sign_key = "5df29ca6f8ee4c12a85e8037dd56675b";
        private static string pay_url = Settings.PayUrl;

        //线程安全的队列(创建订单成功，等待支付的订单队列)
        private static ConcurrentQueue<PayOrder> payWait = new ConcurrentQueue<PayOrder>();

        //支付成功的订单,等待进行充值
        private static ConcurrentQueue<PayOrder> paySuccess = new ConcurrentQueue<PayOrder>();
        //处理等待队列的时间，处理所有的时间
        private static long processQueueTime,processAllTime;


        public ulong AccountId;//账户ID

        public long orderid;//订单ID

        public uint price;//支付金额

        public byte pay_type; //1：支付宝；2：微信支付

        public string payImgContent;//支付二维码图片内容

        public long create_time;//创建时间

  
        public byte pay_state = 0; //支付状态 -1：未知状态，0：等待支付,未支付 1：支付成功，2：支付失败，11：账户余额不足，12：账户套餐过期，13：支付超时

        public byte rec_state = 0;//充值状态， 0：未充值，1：已充值

        public long last_query_time;//最后查询时间(非数据库字段)

        public PayOrder()
        {

        }

        public PayOrder(ulong AccountId)
        {
            this.AccountId = AccountId;
            orderid = UniqueKeyHelper.UniqueNext();
            create_time = UniqueKeyHelper.TotalMilliseconds();
            pay_state = 0;
        }


        //创建支付订单
        //开启线程进行创建
        public static void CreateOrder(ulong AccountId, uint price, byte pay_type)
        {
            PayOrder porder = new PayOrder(AccountId);
            porder.price = price;
            porder.pay_type = pay_type;
            porder.CreateThread();
        }

        //客户端发送充值完成，服务器查询结果
        public static void PayEnd(long oid)
        {
            lock (lockObj)
            {
                foreach (PayOrder p in listall)
                {
                    if (p.orderid == oid)
                    {
                        p.queryThread();
                        break;
                    }
                }
            }
        }

        

        //死循环调用
        //每500毫秒处理一次
        public static void Process()
        {
            try
            {
                if (SMain.Envir.Time < processQueueTime)
                {
                    return;
                }
                ProcessAll();
                processQueueTime = SMain.Envir.Time + 500;
                while (!payWait.IsEmpty)
                {
                    PayOrder p;
                    if (!payWait.TryDequeue(out p)) continue;
                    if (p != null)
                    {
                        //这里是对所有连接进行处理
                        lock (SMain.Envir.Connections)
                        {
                            for (int i = SMain.Envir.Connections.Count - 1; i >= 0; i--)
                            {
                                if (SMain.Envir.Connections[i].Account != null && SMain.Envir.Connections[i].Account.Index == p.AccountId)
                                {
                                    SMain.Envir.Connections[i].Enqueue(new RechargeLink() { orderid = p.orderid, ret_Link = p.payImgContent, payType = p.pay_type, money = p.price, query_Link = p.getQueryUrl() });
                                }
                            }
                        }
                    }
                }

                while (!paySuccess.IsEmpty)
                {
                    PayOrder p;
                    if (!paySuccess.TryDequeue(out p)) continue;
                    if (p != null && p.rec_state == 0 && p.pay_state == 1)
                    {
                        //先对所有账号进行处理,进行加积分处理
                        //
                        uint addCredit = 0;
                        for (int i = SMain.Envir.AccountList.Count - 1; i >= 0; i--)
                        {
                            if (SMain.Envir.AccountList[i].Index == p.AccountId)
                            {
                                if (p.price == 10)
                                {
                                    addCredit = Globals.Recharge10;
                                }
                                if (p.price == 20)
                                {
                                    addCredit = Globals.Recharge20;
                                }
                                if (p.price == 50)
                                {
                                    addCredit = Globals.Recharge50;
                                }
                                if (p.price == 100)
                                {
                                    addCredit = Globals.Recharge100;
                                }
                                if (addCredit > 0)
                                {
                                    SMain.Envir.AccountList[i].Credit += addCredit;
                                    SMain.EnqueueDebugging(SMain.Envir.AccountList[i].AccountID +" 充值元宝：" + addCredit);
                                    SMain.EnqueueDebugging(SMain.Envir.AccountList[i].UserName + " 充值元宝：" + addCredit);
                                    p.rec_state = 1;
                                }
                                else
                                {
                                    SMain.Enqueue("充值发生错误，未知元宝，金额对应关系");
                                    continue;
                                }
                            }
                        }

                        //这里是对所有连接进行处理
                        lock (SMain.Envir.Connections)
                        {
                            for (int i = SMain.Envir.Connections.Count - 1; i >= 0; i--)
                            {
                                if (SMain.Envir.Connections[i].Account != null && SMain.Envir.Connections[i].Account.Index == p.AccountId)
                                {
                                    SMain.Envir.Connections[i].Enqueue(new RechargeResult() { pay_state = p.pay_state, money = p.price, addCredit = addCredit });
                                    SMain.Envir.Connections[i].RefreshUserGold();
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                SMain.Enqueue(e);
            }
        }

        //处理所有的订单,每10秒处理一次,是针对超过5分钟，小于1天的，未支付的订单进行状态的查询
        //每次查询，时间延后1分钟，最多延后1小时
        private static void ProcessAll()
        {
            if (SMain.Envir.Time < processAllTime)
            {
                return;
            }
            processAllTime = SMain.Envir.Time + 1000*10;
            long ct = UniqueKeyHelper.TotalMilliseconds();
            long mint = 1000 * 60 * 5;
            long maxt = 1000 * 60 * 60 * 24;
            lock (lockObj)
            {
                foreach (PayOrder p in listall)
                {
                    if (p.pay_state != 0)
                    {
                        continue;
                    }
                    long uset = ct - p.create_time;
                    if (uset > mint && uset < maxt)
                    {
                        p.queryThread();
                    }
                }
            }
        }

        //对账，对所有未支付的进行一次对账
        //手工发起对账
        public static void Reconciliation()
        {
            lock (lockObj)
            {
                foreach (PayOrder p in listall)
                {
                    if (p.pay_state != 0)
                    {
                        continue;
                    }
                    p.queryThread(false);
                }
            }
        }


        //开启线程创建
        //实现创建订单接口
        //创建前先查询缓存中是否存在重复的订单
        //如果存在重复的订单，则直接返回重复的订单即可
        public void CreateThread()
        {
            lock (lockObj)
            {
                List<PayOrder> dellist = new List<PayOrder>();
                foreach (PayOrder p in listall)
                {
                    //过滤已支付，或者超过1分钟的订单
                    if (p.pay_state != 0 || create_time - p.create_time > 1000 * 60)
                    {
                        continue;
                    }
                    //如果存在，则直接更改之前的订单创建时间，并且重新发起创建请求
                    if (p.AccountId == AccountId && p.pay_type == pay_type && p.price == price)
                    {
                        dellist.Add(p);
                        p.create_time = create_time;
                        Thread t1 = new Thread(() => p.Create());
                        t1.IsBackground = true;
                        t1.Start();
                        return;
                    }
                }

                foreach (PayOrder o in dellist)
                {
                    listall.Remove(o);
                }
            }
            Thread t = new Thread(() => Create());
            t.IsBackground = true;
            t.Start();
        }

       
        private bool Create()
        {
            bool Success = false;
            PayInput pint = new PayInput();
            pint.uid = uid;
            pint.orderid = orderid + "";
            pint.pay_type = pay_type;
            pint.price = price;
            pint.nonce_str = pint.orderid;
            pint.pay_name = "传奇充值";
            pint.pay_demo = "传奇充值_" + AccountId;
            try
            {
                //发起支付
                string retstr = HttpPost(pay_url + "/payapi/create", pint.getPostData(sign_key));
             
                if (retstr != null && retstr.Length > 3)
                {
                    JObject jo = JObject.Parse(retstr);
                    if (jo["ret_code"].ToString() == "1")
                    {
                        SMain.Enqueue("创建支付订单成功:" + orderid);
                        string _payImgContent = jo["qrcode"].ToString();
                        if (_payImgContent != null && _payImgContent.Length > 0)
                        {
                            payImgContent = _payImgContent;
                            payWait.Enqueue(this);
                            listall.Add(this);
                            Success = true;
                        }
                    }
                    else
                    {
                        SMain.Enqueue("创建支付订单失败:" + retstr);
                    }
                }
            }
            catch (Exception ex)
            {
                SMain.Enqueue(ex);
            }
            return Success;
        }

        //开启线程查询
        public void queryThread(bool checktime=true)
        {
            long lt = UniqueKeyHelper.TotalMilliseconds();
            if (checktime)
            {
                //如果创建时间小于6分钟的。20秒一次
                if (lt - create_time < 1000 * 60 * 6)
                {
                    if (lt - last_query_time < 1000 * 20)
                    {
                        return;
                    }
                }//如果创建时间小于10分钟的。1分钟一次
                else if (lt - create_time < 1000 * 60 * 10)
                {
                    if (lt - last_query_time < 1000 * 60)
                    {
                        return;
                    }
                } //如果创建时间小于1小时的。5分钟一次
                else if (lt - create_time < 1000 * 60 * 60)
                {
                    if (lt - last_query_time < 1000 * 60 * 5)
                    {
                        return;
                    }
                } //如果创建时间小于1天的的。10分钟一次
                else if (lt - create_time < 1000 * 60 * 60* 24  )
                {
                    if (lt - last_query_time < 1000 * 60 * 10)
                    {
                        return;
                    }
                }
                //大于1天的，1个小时一次
                else
                {
                    if (lt - last_query_time < 1000 * 60 * 60)
                    {
                        return;
                    }
                }
            }
            last_query_time = lt;

            Thread t = new Thread(() => query());
            t.IsBackground = true;
            t.Start();
        }

        //实现查询接口
        private bool query()
        {

            bool Success = false;
            PayInput pint = new PayInput();
            pint.uid = uid;
            pint.orderid = orderid + "";
            pint.nonce_str = pint.orderid;
            try
            {
                //订单查询接口
                string retstr = HttpPost(pay_url + "/payapi/query", pint.getPostData(sign_key));
                if (retstr != null && retstr.Length > 3)
                {
                    JObject jo = JObject.Parse(retstr);
                    if (jo["ret_code"].ToString() == "1")
                    {
                        string _pay_state = jo["pay_state"].ToString();
                        if (_pay_state != null && _pay_state.Length > 0 && pay_state!=1)
                        {
                            pay_state = byte.Parse(_pay_state);
                            if (pay_state == 1)
                            {
                                paySuccess.Enqueue(this);
                                Success = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SMain.Enqueue(ex);
            }
            return Success;
        }

        public string getQueryUrl()
        {
            PayInput pint = new PayInput();
            pint.uid = uid;
            pint.orderid = orderid + "";
            pint.nonce_str = pint.orderid;
            return pay_url + "/payapi/query?"+ pint.getPostData(sign_key);
        }

        private  string HttpPost(string url, string postString)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] postData = Encoding.UTF8.GetBytes(postString);//编码，尤其是汉字，事先要看下抓取网页的编码方式    
                webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");//采取POST方式必须加的header，如果改为GET方式的话就去掉这句话即可    
                byte[] responseData = webClient.UploadData(url, "POST", postData);//得到返回字符流    
                return Encoding.UTF8.GetString(responseData);//解码   
            }
        }

        private  string GetResult(string url)
        {
            using (WebClient wc = new WebClient())
            {
                string s = wc.DownloadString(url);
                s = HttpUtility.UrlDecode(s);
                return s;
            }
        }

        /// <summary>
        /// 加载所有数据库中加载
        /// </summary>
        /// <returns></returns>
        public static void loadAll()
        {
            //只查询3天内的未充值的订单
            long qtime = UniqueKeyHelper.TotalMilliseconds() - Settings.Day * 3;
            DbDataReader read = MirRunDB.ExecuteReader("select * from PayOrder where rec_state=0 and create_time>" + qtime);
            while (read.Read())
            {
                PayOrder obj = new PayOrder();
                obj.orderid = read.GetInt64(read.GetOrdinal("orderid"));
                obj.price = (uint)read.GetInt32(read.GetOrdinal("price"));
                obj.AccountId = (ulong)read.GetInt64(read.GetOrdinal("AccountId"));
                obj.create_time = read.GetInt64(read.GetOrdinal("create_time"));
                obj.pay_state = read.GetByte(read.GetOrdinal("pay_state"));
                obj.pay_type = read.GetByte(read.GetOrdinal("pay_type"));
                obj.rec_state = read.GetByte(read.GetOrdinal("rec_state"));
                obj.payImgContent = read.GetString(read.GetOrdinal("payImgContent"));
                DBObjectUtils.updateObjState(obj, obj.orderid);
                listall.Add(obj);
                //如果已支付，但是未充值的，则加入待充值列表
                if(obj.pay_state==1 && obj.rec_state == 0)
                {
                    paySuccess.Enqueue(obj);
                }
                //未支付的，查询一次订单状态
                if(obj.pay_state != 1)
                {
                    obj.queryThread();
                }
            }
        }

        public static void SaveAll()
        {
            long ct = UniqueKeyHelper.TotalMilliseconds();
            long mimtime = ct - Settings.Day*7;//7天时间时间的，进行删除
            lock (lockObj)
            {
                List<PayOrder> dellist = new List<PayOrder>();
                foreach (PayOrder o in listall)
                {
                    o.SaveDB();
                    if(o.create_time< mimtime)
                    {
                        dellist.Add(o);
                    }
                }
                foreach (PayOrder o in dellist)
                {
                    listall.Remove(o);
                }
            }
        }


        //保存到数据库
        public void SaveDB()
        {
            byte state = DBObjectUtils.ObjState(this, orderid);
            if (state == 0)//没有改变
            {
                return;
            }
            List<SQLiteParameter> lp = new List<SQLiteParameter>();
            lp.Add(new SQLiteParameter("price", price));
            lp.Add(new SQLiteParameter("AccountId", AccountId));
            lp.Add(new SQLiteParameter("create_time", create_time));
            lp.Add(new SQLiteParameter("pay_state", pay_state));
            lp.Add(new SQLiteParameter("pay_type", pay_type));
            lp.Add(new SQLiteParameter("payImgContent", payImgContent));
            lp.Add(new SQLiteParameter("rec_state", rec_state));
            //新增
            if (state == 1)
            {
                if (orderid > 0)
                {
                    lp.Add(new SQLiteParameter("orderid", orderid));
                }
                string sql = "insert into PayOrder" + SQLiteHelper.createInsertSql(lp.ToArray());
                MirRunDB.Execute(sql, lp.ToArray());
            }
            //修改
            if (state == 2)
            {
                string sql = "update PayOrder set " + SQLiteHelper.createUpdateSql(lp.ToArray()) + " where orderid=@orderid";
                lp.Add(new SQLiteParameter("orderid", orderid));
                MirRunDB.Execute(sql, lp.ToArray());
            }
            DBObjectUtils.updateObjState(this, orderid);
        }



    }

    // player bool NewMail (process in envir loop) - send all mail on login

    // Send mail from player (auto from player)
    // Send mail from Envir (mir administrator)

    //支付订单，通过这个创建
    public class PayInput
    {
        //针对订单创建
        public string uid;
        public string orderid;
        public string nonce_str;//随机字符串
        public float price;//支付金额
        public string pay_ext1;
        public string pay_ext2;
        public int pay_type; //1：支付宝；2：微信支付
        public string pay_name;
        public string pay_demo;
        public string return_url;
        //签名字段
        public string sign;

        /**
         * 对pay对象进行签名
         * @return
         */
        public string MarkSign(string sign_key)
        {
            //所有参数加入list
            List<string> kvlist = new List<string>();
            //采用反射获取所有的属性，并计算签名
            FieldInfo[] fields = this.GetType().GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].IsStatic)
                {
                    continue;
                }
                //if (fields[i].IsDefined(typeof(JsonIgnoreAttribute), true)) continue;
                string key = fields[i].Name;
                if (key == "sign")
                {
                    continue;
                }
                object v = fields[i].GetValue(this);
                if (v == null || v.ToString().Length == 0)
                {
                    continue;
                }
                kvlist.Add(key + "=" + v);
            }
            //对所有的参数进行排序
            kvlist.Sort();

            StringBuilder stringA = new StringBuilder();
            //组成相关的
            foreach (string kv in kvlist)
            {
                stringA.Append(kv + "&");
            }
            stringA.Append("sign_key=" + sign_key);

            //签名
            return MD5Utils.MD5Encode(stringA.ToString()).ToUpper();
        }

        /**
         * 返回所有的参数map
         * @param sign_key
         * @return
         * @throws IllegalAccessException
         */
        public string getPostData(string sign_key)
        {
            StringBuilder ret = new StringBuilder();
            //所有参数加入list
            List<string> kvlist = new List<string>();
            //采用反射获取所有的属性，并计算签名
            FieldInfo[] fields = this.GetType().GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].IsStatic)
                {
                    continue;
                }
                //if (fields[i].IsDefined(typeof(JsonIgnoreAttribute), true)) continue;
                string key = fields[i].Name;
                if (key == "sign")
                {
                    continue;
                }
                object v = fields[i].GetValue(this);
                if (v == null || v.ToString().Length == 0)
                {
                    continue;
                }
                kvlist.Add(key + "=" + v);
                //postData.Add(key, v.ToString());
            }
            //对所有的参数进行排序
            kvlist.Sort();

            StringBuilder stringA = new StringBuilder();
            //组成相关的
            foreach (string kv in kvlist)
            {
                stringA.Append(kv + "&");
                ret.Append(kv + "&");
            }
            stringA.Append("sign_key=" + sign_key);
            ret.Append("sign=" + MD5Utils.MD5Encode(stringA.ToString()).ToUpper());
            
            return ret.ToString();
        }


    }
}
