using Client.MirNetwork;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using C = ClientPackets;

namespace Client
{
    public partial class PayForm : Form
    {
        public  uint money = 0;//当前选择的金额
        public  byte payType = 1;//支付方式1：支付宝；2：微信支付 
        public string payurl="";//支付链接
        public long oid;//订单号
        public string query_Link;//这个是查询支付是否成功的地址，开启线程循环进行查询，如果成功，则告诉服务器

        private int payState=0;//支付状态 0：未支付 1：已支付 2:支付结束（可能已支付，也可能未支付）

        private DateTime starttime = DateTime.Now;


        public PayForm()
        {
            InitializeComponent();
        }
        //重新定位位置
        public void ReLoc()
        {
            if (Program.Form == null)
            {
                return;
            }
            this.Left = Program.Form.Left+(Program.Form.Width-this.Width)/2;
            this.Top = Program.Form.Top+(Program.Form.Height - this.Height) / 2;
        }

        private void PayForm_Load(object sender, EventArgs e)
        {
            this.StartPosition = FormStartPosition.Manual;
            ReLoc();
            pictureBox1.ImageLocation = string.Format(@"http://mobile.qq.com/qrcode?url="+ payurl);
            lab_oid.Text = "订单号："+ oid;
            if (payType == 1)
            {
                //pic_title.Image= Client.Properties.Resources.zfb_logo;
                pic_title.BackgroundImage = Client.Properties.Resources.zfb_logo;
                pay_title.Text = "支付宝扫码支付";
            }
            else
            {
                pic_title.BackgroundImage = Client.Properties.Resources.wx_logo;
                pay_title.Text = "微信扫码支付";
            }
            lab_name.Text = "商品名称：传奇元宝充值(" + money + ")元";

            timer1.Start();
            //开启线程进行循环查询
            Thread t = new Thread(() => QueryThread());
            t.IsBackground = true;
            t.Start();
        }

        private void pic_close_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void pic_close_MouseEnter(object sender, EventArgs e)
        {
            pic_close.Image = Client.Properties.Resources.close_hover;
        }

        private void pic_close_MouseLeave(object sender, EventArgs e)
        {
            pic_close.Image = Client.Properties.Resources.close_base;
        }

        private void pic_close_MouseUp(object sender, MouseEventArgs e)
        {
            pic_close.Image = Client.Properties.Resources.close_base;
        }

        private void pic_close_MouseDown(object sender, MouseEventArgs e)
        {
            pic_close.Image = Client.Properties.Resources.close_pressed;
        }

        //定时器
        private void timer1_Tick(object sender, EventArgs e)
        {
            ReLoc();
            //未支付
            if (payState == 0)
            {
                TimeSpan span = (DateTime.Now - starttime);
                if (span.TotalMinutes >= 5)
                {
                    payState = 2;
                    //过期
                    lab_time.Text = "当前支付二维码已过期，请重新触发充值";
                    pictureBox1.Image = Client.Properties.Resources.qrcode_timeout;
                    return;
                }
                lab_time.Text = "二维码有效期：0时 " + (4 - span.Minutes) + "分 " + (59 - span.Seconds) + "秒";
            }
            //已支付
            if(payState == 1)
            {
                payState = 2;
                pictureBox1.Image = Client.Properties.Resources.pay_ok;
                lab_time.Text = "充值完成，元宝即将到账";
                Network.Enqueue(new C.RechargeEnd() { oid = oid });
            }
        }

        //循环查询结果线程
        private void QueryThread()
        {
            while (payState == 0)
            {
                //间隔2秒查询一次
                try
                {
                    Thread.Sleep(2000);
                    //MirLog.info("开始查询：" + query_Link);
                    string retstr = GetResult(query_Link + "&_t=" + DateTime.Now.Ticks);
                    //MirLog.info(retstr);
                    if (retstr != null && retstr.IndexOf("\"pay_state\":1") != -1)
                    {
                        payState = 1;
                    }
                }
                catch(Exception ex)
                {
                    MirLog.info("线程查询支付结果错误："+ex.Message);
                }
            }
        }


        private string HttpPost(string url, string postString)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] postData = Encoding.UTF8.GetBytes(postString);//编码，尤其是汉字，事先要看下抓取网页的编码方式    
                webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");//采取POST方式必须加的header，如果改为GET方式的话就去掉这句话即可    
                byte[] responseData = webClient.UploadData(url, "POST", postData);//得到返回字符流    
                return Encoding.UTF8.GetString(responseData);//解码   
            }
        }

        private string GetResult(string url)
        {
            using (WebClient wc = new WebClient())
            {
                string s = wc.DownloadString(url);
                s = HttpUtility.UrlDecode(s);
                return s;
            }
        }

        //窗体关闭时调用
        private void PayForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Stop();
            payState = 2;
        }
    }
}
