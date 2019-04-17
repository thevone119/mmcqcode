using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using Client.MirControls;
using C = ClientPackets;


namespace Client.MirNetwork
{
    /// <summary>
    /// 网络请求的封装，所有的网络请求都通过此类进行
    /// 客户端封装得不错
    /// </summary>
    static class Network
    {
        //所有接受数据的长度
        private static long ReceiveDataLen = 0;
        //TPC客户端连接
        private static TcpClient _client;
        //连接次数
        public static int ConnectAttempt = 0;
        //是否连接上
        public static bool Connected;
        //超时参数
        public static long TimeOutTime, TimeConnected;
        //2个队列，发送队列与接收队列
        private static ConcurrentQueue<Packet> _receiveList;
        private static ConcurrentQueue<Packet> _sendList;
        //接收数据data，多余的数据也放到这里，每次接受的数据，多余的都放在这里存储
        static byte[] _rawData = new byte[0];

        //连接服务器
        public static void Connect()
        {
            if (_client != null)
                Disconnect();

            ConnectAttempt++;

            _client = new TcpClient {NoDelay = true};
            _client.BeginConnect(Settings.serverIp, Settings.serverPort, Connection, null);
        }
        //连接的异步回调
        private static void Connection(IAsyncResult result)
        {
            try
            {
                _client.EndConnect(result);
                //没有连接，则再次发起连接
                if (!_client.Connected)
                {
                    Connect();
                    return;
                }
                //初始化一些参数
                _receiveList = new ConcurrentQueue<Packet>();
                _sendList = new ConcurrentQueue<Packet>();
                _rawData = new byte[0];

                TimeOutTime = CMain.Time + Settings.TimeOut;
                TimeConnected = CMain.Time;

                //开始接受数据
                BeginReceive();
            }
            catch (SocketException)
            {
                Connect();
            }
            catch (Exception ex)
            {
                if (Settings.LogErrors) CMain.SaveError(ex.ToString());
                Disconnect();
            }
        }

        //开始接受数据
        private static void BeginReceive()
        {
            if (_client == null || !_client.Connected) return;
            //一次性最多接受8K
            byte[] rawBytes = new byte[8 * 1024];

            try
            {
                _client.Client.BeginReceive(rawBytes, 0, rawBytes.Length, SocketFlags.None, ReceiveData, rawBytes);
            }
            catch
            {
                Disconnect();
            }
        }
        //接受完成
        private static void ReceiveData(IAsyncResult result)
        {
            if (_client == null || !_client.Connected) return;

            int dataRead;

            try
            {
                dataRead = _client.Client.EndReceive(result);
            }
            catch
            {
                Disconnect();
                return;
            }

            if (dataRead == 0)
            {
                Disconnect();
            }
            //当前接受到的数据
            byte[] rawBytes = result.AsyncState as byte[];
            //之前还残留的数据
            byte[] temp = _rawData;
            //当前数据+残留数据
            _rawData = new byte[dataRead + temp.Length];
            //复制残留数据
            Buffer.BlockCopy(temp, 0, _rawData, 0, temp.Length);
            //复制当前数据
            Buffer.BlockCopy(rawBytes, 0, _rawData, temp.Length, dataRead);
            //统计
            ReceiveDataLen += dataRead;
            //MirLog.info("ReceiveDataLen:" + ReceiveDataLen);
            Packet p;
            
            while ((p = Packet.ReceivePacket(_rawData, out _rawData)) != null)
            {
                _receiveList.Enqueue(p);
            }

            //这里又调用接受数据？嵌套调用,不断的进行异步接受数据
            BeginReceive();
        }

        //发送数据
        private static void BeginSend(List<byte> data)
        {
            if (_client == null || !_client.Connected || data.Count == 0) return;
            
            try
            {
                _client.Client.BeginSend(data.ToArray(), 0, data.Count, SocketFlags.None, SendData, null);
            }
            catch
            {
                Disconnect();
            }
        }

        //发送数据的异步回调,回调没有什么处理啊？不用管丢包的啊？
        private static void SendData(IAsyncResult result)
        {
            try
            {
                _client.Client.EndSend(result);
            }
            catch
            { }
        }

        //断开连接
        public static void Disconnect()
        {
            if (_client == null) return;

            _client.Close();

            TimeConnected = 0;
            Connected = false;
            _sendList = null;
            _client = null;

            _receiveList = null;
        }

        /// <summary>
        /// 处理返回的数据，循环处理
        /// 处理发送的数据，循环处理
        /// 提供外部场景循环调用？
        /// -----这个适合线程死循环调用。
        /// 在Cmain中的Application_Idle中循环调用
        /// </summary>
        public static void Process()
        {
            //当连接断开时，处理一个就返回，处理最后一个？不太清楚这个到底是什么一个逻辑
            //这个应该是针对网络异常断开，处理最后一个数据,然后调用断开方法断开连接
            if (_client == null || !_client.Connected)
            {
                if (Connected)
                {
                    while (_receiveList != null && !_receiveList.IsEmpty)
                    {
                        Packet p;

                        if (!_receiveList.TryDequeue(out p) || p == null) continue;
                        //针对这2个数据不做处理？
                        if (!(p is ServerPackets.Disconnect) && !(p is ServerPackets.ClientVersion)) continue;
                        //活动场景处理数据
                        MirScene.ActiveScene.ProcessPacket(p);
                        //处理一个就设置为空？处理一个就直接返回
                        _receiveList = null;
                        return;
                    }

                    //MirMessageBox.Show("Lost connection with the server.", true);
                    MirMessageBox.Show("与服务器失去连接.", true);
                    Disconnect();
                    return;
                }
                return;
            }

            //超过5秒没有连接上，则再次发起连接？
            if (!Connected && TimeConnected > 0 && CMain.Time > TimeConnected + 5000)
            {
                Disconnect();
                Connect();
                return;
            }


            //这里又来一个处理？这个才是真正的处理，处理接受数据
            while (_receiveList != null && !_receiveList.IsEmpty)
            {
                Packet p;
                if (!_receiveList.TryDequeue(out p) || p == null) continue;
                MirLog.info("ProcessPacket:" + p.Index);
                MirScene.ActiveScene.ProcessPacket(p);
            }

            //超时发送心跳包？每5秒发送一个？心跳包是空的？
            if (CMain.Time > TimeOutTime && _sendList != null && _sendList.IsEmpty)
                _sendList.Enqueue(new C.KeepAlive());

            if (_sendList == null || _sendList.IsEmpty) return;

            TimeOutTime = CMain.Time + Settings.TimeOut;

            //处理发送的数据包,合并在一起进行发送？
            List<byte> data = new List<byte>();
            while (!_sendList.IsEmpty)
            {
                Packet p;
                if (!_sendList.TryDequeue(out p)) continue;
                data.AddRange(p.GetPacketBytes());
            }
            BeginSend(data);
        }
       
        //把数据放入队列，等待发送
        public static void Enqueue(Packet p)
        {
            if (_sendList != null && p != null)
            {
                _sendList.Enqueue(p);
                //MirLog.info("send idx:" + p.Index);
            }
        }
    }
}
