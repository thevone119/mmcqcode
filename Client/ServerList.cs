using System.IO;
using System;
using Client.MirSounds;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Client
{
    //服务器列表
    public class ServerList
    {
        private  const string cpath = @".\Config\ServerList";
        private static List<ServerInfo> slist = new List<ServerInfo>();

        public static  void Load()
        {
            FileInfo fileInf = new FileInfo(cpath);
            if (!fileInf.Exists)
            {
                return;
            }
            using (FileStream fs = new FileStream(cpath, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader m_streamReader = new StreamReader(fs, System.Text.Encoding.UTF8))
                {
                    string strLine = null;
                    
                    while ((strLine = m_streamReader.ReadLine()) != null)
                    {
                        if (strLine == null || strLine.Trim().Length < 3)
                        {
                            continue;
                        }
                        if (strLine.StartsWith("#"))
                        {
                            continue;
                        }
                        string[] sv = strLine.Trim().Split(';');
                        if (sv == null || sv.Length < 3)
                        {
                            continue;
                        }
                        try
                        {
                            ServerInfo si = new ServerInfo();
                            si.sid = int.Parse(sv[0]);
                            si.psid= int.Parse(sv[1]);
                            si.sname = sv[2];
                           
                            if (sv.Length>3 && sv[3]!=null && sv[3].Length > 5)
                            {
                                si.sip = sv[3];
                            }
                            if (sv.Length > 4)
                            {
                                int.TryParse(sv[4], out si.Port);
                            }
                            if(sv.Length > 5)
                            {
                                if (sv[5] != null && sv[5].Length > 3)
                                {
                                    si.sRemarks = sv[5];
                                }
                            }
                            slist.Add(si);
                        }
                        catch { }
                        
                    }
                }
            }

      
        }
        //获取服务器列表
        public static List<ServerInfo> getServerList()
        {
            return slist;
        }
        //根据父节点ID查找服务器列表
        public static List<ServerInfo> getServerList(int psid)
        {
            List<ServerInfo> newlist = new List<ServerInfo>();
            foreach(ServerInfo si in slist)
            {
                if (si.psid == psid)
                {
                    newlist.Add(si);
                }
            }
            return newlist;
        }
    }

    //服务器信息
    public class ServerInfo
    {
        public int sid;//服务器ID
        public int psid;//父节点ID,默认是0
        public string sname;//服务器名称
        public string sip;//服务器IP
        public int Port = 0;//服务器端口
        public string sRemarks;//服务器备注

        public ServerInfo()
        {

        }
        //是否游戏
        public bool isGameServer()
        {
            if(sip == null|| sip.IndexOf(".") == -1 || sip.Length < 7)
            {
                return false;
            }
            if (Port == 0)
            {
                return false;
            }
            return true;
        }
    }
}
