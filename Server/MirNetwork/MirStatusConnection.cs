using System;
using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Windows.Forms;
using System.IO;

namespace Server.MirNetwork
{
    /// <summary>
    /// 连接状态保持
    /// </summary>
    public class MirStatusConnection
    {
        //客户端IP
        public readonly string IPAddress;
        //客户端的连接
        private TcpClient _client;
        //下次发送时间
        private long NextSendTime;
        //是否断开连接,正在断开连接
        private bool _disconnecting;
        //是否连接
        public bool Connected;
        public bool Disconnecting
        {
            get { return _disconnecting; }
            set
            {
                if (_disconnecting == value) return;
                _disconnecting = value;
                TimeOutTime = SMain.Envir.Time + 500;
            }
        }
        //连接的时间
        public readonly long TimeConnected;
        //超时时间，10秒超时
        public long TimeOutTime;


        public MirStatusConnection(TcpClient client)
        {
            try
            {
                IPAddress = client.Client.RemoteEndPoint.ToString().Split(':')[0];

                _client = client;
                _client.NoDelay = true;

                TimeConnected = SMain.Envir.Time;
                TimeOutTime = TimeConnected + Settings.TimeOut;
                Connected = true;
            }
            catch (Exception ex)
            {
                File.AppendAllText(Settings.LogPath + "Error Log (" + DateTime.Now.Date.ToString("dd-MM-yyyy") + ").txt",
                                           String.Format("[{0}]: {1}" + Environment.NewLine, DateTime.Now, ex.ToString()));
            }
        }
        //发送数据
        private void BeginSend(byte[] data)
        {
            if (!Connected || data.Length == 0) return;

            try
            {
                _client.Client.BeginSend(data, 0, data.Length, SocketFlags.None, SendData, null);
            }
            catch
            {
                Disconnecting = true;
            }
        }
        //发送数据结束
        private void SendData(IAsyncResult result)
        {
            try
            {
                _client.Client.EndSend(result);
            }
            catch
            { }
        }

        public void Process()
        {
            try
            {
                if (_client == null || !_client.Connected)
                {
                    Disconnect();
                    return;
                }

                if (SMain.Envir.Time > TimeOutTime || Disconnecting)
                {
                    Disconnect();
                    return;
                }

                //这个是干嘛啊？每10秒发送一个这样的东西？这个不是符合格式的包啊？
                if (SMain.Envir.Time > NextSendTime)
                {
                    NextSendTime = SMain.Envir.Time + 10000;
                    string output = string.Format("c;/NoName/{0}/CrystalM2/{1}//;", SMain.Envir.PlayerCount, Application.ProductVersion);

                    BeginSend(Encoding.ASCII.GetBytes(output));
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(Settings.LogPath + "Error Log (" + DateTime.Now.Date.ToString("dd-MM-yyyy") + ").txt",
                                           String.Format("[{0}]: {1}" + Environment.NewLine, DateTime.Now, ex.ToString()));
            }
        }
        //断开连接
        public void Disconnect()
        {
            try
            {
                if (!Connected) return;

                Connected = false;

                lock (SMain.Envir.StatusConnections)
                    SMain.Envir.StatusConnections.Remove(this);

                if (_client != null) _client.Client.Dispose();
                _client = null;
            }
            catch (Exception ex)
            {
                File.AppendAllText(Settings.LogPath + "Error Log (" + DateTime.Now.Date.ToString("dd-MM-yyyy") + ").txt",
                                           String.Format("[{0}]: {1}" + Environment.NewLine, DateTime.Now, ex.ToString()));
            }
        }

        public void SendDisconnect()
        {
            Disconnecting = true;
        }
    }
}
