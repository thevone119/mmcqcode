using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using Client;


namespace Launcher
{
    public partial class AMain : Form
    {
        //总文件大小，完成大小，当前完成的大小，上次完成的大小
        long _totalBytes, _completedBytes, _currentBytes, _preBytes;
        //文件数，当前已完成数
        private int _fileCount, _currentCount;

        //当前下载的文件
        private FileInformation _currentFile;
        //是否已完成
        public bool Completed, Checked, CleanFiles, LabelSwitch, ErrorFound,WinClose, RefreshServer;
        
        public List<FileInformation> OldList;
        public Queue<FileInformation> DownloadList;

        //不用这个计算时间了，间隔统一是500毫秒，用这个计算即可
        private Stopwatch _stopwatch = Stopwatch.StartNew();

        public Thread _workThread;

        //这几个参数实现拖拽
        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        private string oldClientName = "OldClient.exe";//备份一份
        //配置窗口
        private Config ConfigForm = new Config();
        //是否需要重启,如果更新了当前客户端的exe,则需要重启
        private bool Restart = false;

        

        public AMain()
        {
            InitializeComponent();
            treeView1.Nodes.Clear();
            BackColor = Color.FromArgb(1, 0, 0);
            TransparencyKey = Color.FromArgb(1, 0, 0);
        }

        public static void SaveError(string ex)
        {
            try
            {
                if (Settings.RemainingErrorLogs-- > 0)
                {
                    File.AppendAllText(@".\Config\Error.txt",
                                       string.Format("[{0}] {1}{2}", DateTime.Now, ex, Environment.NewLine));
                }
            }
            catch
            {
            }
        }
        //调用线程执行更新
        public void Start()
        {
            try
            {
                OldList = new List<FileInformation>();
                DownloadList = new Queue<FileInformation>();
                //刷新服务器列表
                ServerList.Load();
                RefreshServer = false;
                byte[] data = Download(Settings.P_PatchFileName);

                if (data != null)
                {
                    using (MemoryStream stream = new MemoryStream(data))
                    using (BinaryReader reader = new BinaryReader(stream))
                        ParseOld(reader);
                }
                else
                {
                    MessageBox.Show("找不到更新列表.");
                    Completed = true;
                    ErrorFound = true;
                    return;
                }

                _fileCount = OldList.Count;
                for (int i = 0; i < OldList.Count; i++)
                    CheckFile(OldList[i]);

                Checked = true;
                _fileCount = 0;
                _currentCount = 0;


                _fileCount = DownloadList.Count;
                DownloadAll();
                //刷新服务器列表
                ServerList.Load();
                RefreshServer = false;
            }
            catch (EndOfStreamException ex)
            {
                MessageBox.Show("读取更新列表错误");
                Completed = true;
                SaveError(ex.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
                Completed = true;
                SaveError(ex.ToString());
            }
        }

        
       

        //开始下载所有的文件
        private void DownloadAll()
        {
            if (DownloadList == null) return;

            if (DownloadList.Count == 0)
            {
                DownloadList = null;
                _currentFile = null;
                Completed = true;

                CleanUp();
                return;
            }
            while (DownloadList.Count > 0)
            {
                if (WinClose)
                {
                    return;
                }
                _currentFile = DownloadList.Dequeue();
                bool dret = Download(_currentFile);
                //下载失败，重新加入下载队列，并且
                if (!dret)
                {
                    //最多只允许失败2次
                    if (_currentFile.updateState < 2)
                    {
                        _currentFile.updateState = _currentFile.updateState + 1;
                        DownloadList.Enqueue(_currentFile);
                    }
                    else
                    {
                        ErrorFound = true;
                    }
                }
            }
            Completed = true;
            //清理文件
            CleanUp();
        }
        //清除没用的文件
        private void CleanUp()
        {
            if (OldList.Count == 0)
            {
                return;
            }
            if (!CleanFiles) return;

            string[] fileNames = Directory.GetFiles(@".\", "*.*", SearchOption.AllDirectories);
            string fileName;
            for (int i = 0; i < fileNames.Length; i++)
            {
                //截屏程序不清理
                if (fileNames[i].ToLower().IndexOf("screenshots") != -1)
                {
                    continue;
                }
                //配置不删
                if (fileNames[i].ToLower().IndexOf("config") != -1)
                {
                    continue;
                }
                //客户端不删
                if (fileNames[i].ToLower().IndexOf(System.AppDomain.CurrentDomain.FriendlyName.ToLower()) != -1)
                {
                    continue;
                }
                try
                {
                    if (!NeedFile(fileNames[i]))
                        File.Delete(fileNames[i]);
                }
                catch{}
            }
        }
        //是否需要的文件
        public bool NeedFile(string fileName)
        {
            for (int i = 0; i < OldList.Count; i++)
            {
                if (fileName.EndsWith(OldList[i].FileName))
                    return true;
            }

            return false;
        }

        //处理文件列表
        public void ParseOld(BinaryReader reader)
        {
            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
                OldList.Add(new FileInformation(reader));
        }
        //校验文件是否需要下载，是否需要重启客户端
        public void CheckFile(FileInformation old)
        {
            FileInformation info = GetFileInformation(Settings.P_Client + old.FileName);
            _currentCount++;

            if (info == null || old.Length != info.Length || old.Creation != info.Creation)
            {
                if ((old.FileName.EndsWith(System.AppDomain.CurrentDomain.FriendlyName)))
                {
                    //针对客户端程序，多加一个时间判断，如果是新的文件则不处理
                    if(info != null && info.Creation > old.Creation)
                    {
                        return;
                    }
                    FileMove(Settings.P_Client + System.AppDomain.CurrentDomain.FriendlyName, Settings.P_Client + oldClientName);
                    Restart = true;
                }
                DownloadList.Enqueue(old);
                _totalBytes += old.Length;
            }
        }

        //新的下载文件处理
        //直接下载覆盖本地文件
        //如果已经下载完成，则任务是成功了，否则是失败的
        //返回成功，失败
        public bool Download(FileInformation info)
        {
            bool ret = false;
            string fileName = info.FileName.Replace(@"\", "/");
            if (fileName != "PList.gz")
                fileName += ".gz";
            //不存在则创建
            if (!Directory.Exists(Settings.P_Client + Path.GetDirectoryName(info.FileName)))
                Directory.CreateDirectory(Settings.P_Client + Path.GetDirectoryName(info.FileName));

            long t_completedBytes = _completedBytes;
            _currentBytes = 0;
            try
            {
                FtpWebRequest reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(Settings.P_Host + fileName));
                reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                reqFTP.UseBinary = true;
                reqFTP.UsePassive = false;
                if (Settings.P_NeedLogin)
                {
                    reqFTP.Credentials = new NetworkCredential(Settings.P_Login, Settings.Password);
                }

                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();

                using (Stream ftpStream = response.GetResponseStream())
                {
                    using (FileStream outputStream = new FileStream(Settings.P_Client + info.FileName, FileMode.Create))
                    {
                        int bufferSize = 2048;
                        int readCount;
                        byte[] buffer = new byte[bufferSize];

                        readCount = ftpStream.Read(buffer, 0, bufferSize);
                        while (readCount > 0)
                        {
                            if (WinClose)
                            {
                                return false;
                            }
                            outputStream.Write(buffer, 0, readCount);
                            _completedBytes += readCount;
                            _currentBytes += readCount;
                            readCount = ftpStream.Read(buffer, 0, bufferSize);
                        }
                        //执行到这里，其实就已经是成功了
                        ret = true;
                        response.Close();
                    }
                }
            }
            catch(Exception e)
            {
                System.Console.WriteLine("下载文件失败：" + fileName);
                File.AppendAllText(@".\Config\Error.txt",
                                       string.Format("[{0}] {1}{2}", DateTime.Now, info.FileName + " could not be downloaded. (" + e.Message + ")", Environment.NewLine));
            }
            //如果失败了，需要进行重置
            if (!ret)
            {
                _completedBytes = t_completedBytes;
                _currentBytes = 0;
            }
            return ret;

        }

        
        
        //下载文件，返回的是字节数组
        public byte[] Download(string fileName)
        {
            fileName = fileName.Replace(@"\", "/");

            if (fileName != "PList.gz")
                fileName += Path.GetExtension(fileName);
            byte[] ret = new byte[1];
            try
            {
                using (MemoryStream mStream = new MemoryStream())
                {
                    using (BinaryWriter gStream = new BinaryWriter(mStream))
                    {
                        FtpWebRequest reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(Settings.P_Host + fileName));
                        reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                        reqFTP.UseBinary = true;
                        reqFTP.UsePassive = false;
                        if (Settings.P_NeedLogin)
                        {
                            reqFTP.Credentials = new NetworkCredential(Settings.P_Login, Settings.Password);
                        }
    
                        FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                        Stream ftpStream = response.GetResponseStream();
                        int bufferSize = 2048;
                        int readCount;
                        byte[] buffer = new byte[bufferSize];

                        readCount = ftpStream.Read(buffer, 0, bufferSize);
                        while (readCount > 0)
                        {
                            gStream.Write(buffer, 0, readCount);
                            readCount = ftpStream.Read(buffer, 0, bufferSize);
                        }
                        ret = mStream.ToArray();
                        ftpStream.Close();
                        response.Close();
                    }
                }
                //using (WebClient client = new WebClient())
                //{
                //    client.Credentials = new NetworkCredential(Settings.Login, Settings.Password);
                //   return client.DownloadData(Settings.Host  + fileName);
                //}
            }
            catch
            {
                System.Console.WriteLine("下载文件失败：" + fileName);
            }
            if (ret.Length > 1)
            {
                return ret;
            }
            return null;
        }

    
        //没有使用压缩解压
        public static byte[] Decompress(byte[] raw)
        {
            using (GZipStream gStream = new GZipStream(new MemoryStream(raw), CompressionMode.Decompress))
            {
                const int size = 4096; //4kb
                byte[] buffer = new byte[size];
                using (MemoryStream mStream = new MemoryStream())
                {
                    int count;
                    do
                    {
                        count = gStream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            mStream.Write(buffer, 0, count);
                        }
                    } while (count > 0);
                    return mStream.ToArray();
                }
            }
        }
        //没有使用压缩解压
        public static byte[] Compress(byte[] raw)
        {
            using (MemoryStream mStream = new MemoryStream())
            {
                using (GZipStream gStream = new GZipStream(mStream, CompressionMode.Compress, true))
                    gStream.Write(raw, 0, raw.Length);
                return mStream.ToArray();
            }
        }

        public FileInformation GetFileInformation(string fileName)
        {
            if (!File.Exists(fileName)) return null;

            FileInfo info = new FileInfo(fileName);
            return new FileInformation
            {
                FileName = fileName.Remove(0, Settings.P_Client.Length),
                Length = (int)info.Length,
                Creation = info.LastWriteTime
            };
        }

        private void AMain_Load(object sender, EventArgs e)
        {
            if (Settings.P_BrowserAddress != "") Main_browser.Navigate(new Uri(Settings.P_BrowserAddress));

            if (File.Exists(Settings.P_Client + oldClientName)) File.Delete(Settings.P_Client + oldClientName);

            //Launch_pb.Enabled = false;
            ProgressCurrent_pb.Width = 5;
            TotalProg_pb.Width = 5;
           
            _workThread = new Thread(Start) { IsBackground = true };
            _workThread.Start();
        }

        private void Launch_pb_Click(object sender, EventArgs e)
        {
            if (!Completed)
            {
                MessageBox.Show("正在进行客户端更新，请等待更新完成后再进入游戏.", "等待更新.");
                return;
            }
            //判断是否有选择分区
            if (treeView1.SelectedNode == null)
            {
                MessageBox.Show("请选择游戏分区.", "操作提醒.");
                return;
            }
            ServerInfo si = (ServerInfo)treeView1.SelectedNode.Tag;
            if (!si.isGameServer())
            {
                MessageBox.Show("请选择具体的游戏分区.", "操作提醒.");
                return;
            }
            Launch();
        }
        //开始游戏
        private void Launch()
        {
            if (ConfigForm.Visible) ConfigForm.Visible = false;
            //隐藏当前窗口，开新的窗口,游戏窗口
            Program.Form = new CMain();
            Program.Form.Closed += (s, args) => this.Close();
            Program.Form.Show();
            Program.PForm.Hide();
        }

        private void Close_pb_Click(object sender, EventArgs e)
        {
            if (ConfigForm.Visible) ConfigForm.Visible = false;
            Close();
        }

        private void Movement_panel_MouseClick(object sender, MouseEventArgs e)
        {
            if (ConfigForm.Visible) ConfigForm.Visible = false;
            dragging = true;
            dragCursorPoint = Cursor.Position;
            dragFormPoint = this.Location;
        }

        private void Movement_panel_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void Movement_panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(dif));
            }
        }

        private void Launch_pb_MouseEnter(object sender, EventArgs e)
        {
            Launch_pb.Image = Client.Properties.Resources.Launch_Hover;
        }

        private void Launch_pb_MouseLeave(object sender, EventArgs e)
        {
            Launch_pb.Image = Client.Properties.Resources.Launch_Base1;
        }

        private void Close_pb_MouseEnter(object sender, EventArgs e)
        {
            Close_pb.Image = Client.Properties.Resources.Cross_Hover;
        }

        private void Close_pb_MouseLeave(object sender, EventArgs e)
        {
            Close_pb.Image = Client.Properties.Resources.Cross_Base;
        }

        private void Launch_pb_MouseDown(object sender, MouseEventArgs e)
        {
            Launch_pb.Image = Client.Properties.Resources.Launch_Pressed;
        }

        private void Launch_pb_MouseUp(object sender, MouseEventArgs e)
        {
            Launch_pb.Image = Client.Properties.Resources.Launch_Base1;
        }

        private void Close_pb_MouseDown(object sender, MouseEventArgs e)
        {
            Close_pb.Image = Client.Properties.Resources.Cross_Pressed;
        }

        private void Close_pb_MouseUp(object sender, MouseEventArgs e)
        {
            Close_pb.Image = Client.Properties.Resources.Cross_Base;
        }

        private void ProgressCurrent_pb_SizeChanged(object sender, EventArgs e)
        {
            ProgEnd_pb.Location = new Point((ProgressCurrent_pb.Location.X + ProgressCurrent_pb.Width), ProgressCurrent_pb.Location.Y);
            if (ProgressCurrent_pb.Width == 0) ProgEnd_pb.Visible = false;
            else ProgEnd_pb.Visible = true;
        }



        private void Config_pb_MouseDown(object sender, MouseEventArgs e)
        {
            Config_pb.Image = Client.Properties.Resources.Config_Pressed;
        }

        private void Config_pb_MouseEnter(object sender, EventArgs e)
        {
            Config_pb.Image = Client.Properties.Resources.Config_Hover;
        }

        private void Config_pb_MouseLeave(object sender, EventArgs e)
        {
            Config_pb.Image = Client.Properties.Resources.Config_Base;
        }

        private void Config_pb_MouseUp(object sender, MouseEventArgs e)
        {
            Config_pb.Image = Client.Properties.Resources.Config_Base;
        }

        private void Config_pb_Click(object sender, EventArgs e)
        {
            if (ConfigForm.Visible) ConfigForm.Hide();
            else ConfigForm.Show(Program.PForm);
            ConfigForm.Location = new Point(Location.X + Config_pb.Location.X - 183, Location.Y + 36);
        }

        private void TotalProg_pb_SizeChanged(object sender, EventArgs e)
        {
            ProgTotalEnd_pb.Location = new Point((TotalProg_pb.Location.X + TotalProg_pb.Width), TotalProg_pb.Location.Y);
            if (TotalProg_pb.Width == 0) ProgTotalEnd_pb.Visible = false;
            else ProgTotalEnd_pb.Visible = true;
        }

        private void Main_browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (Main_browser.Url.AbsolutePath != "blank") Main_browser.Visible = true;
        }

        //定时监控
        private void InterfaceTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                //刷新服务器列表
                if (!RefreshServer&& ServerList.getServerList().Count>0)
                {
                    RefreshServer = true;
                    treeView1.Nodes.Clear();
                    //1级节点，父节点是0
                    List<ServerInfo> list1 = ServerList.getServerList(0);
                    foreach (ServerInfo si1 in list1)
                    {
                        TreeNode node1 = new TreeNode(si1.sname);
                        node1.Tag = si1;
                        treeView1.Nodes.Add(node1);
                        //2级节点
                        List<ServerInfo> list2 = ServerList.getServerList(si1.sid);
                        foreach(ServerInfo si2 in list2){
                            TreeNode node2 = new TreeNode(si2.sname);
                            node2.Tag = si2;
                            node1.Nodes.Add(node2);
                            treeView1.SelectedNode = node2;
                            //3级节点
                            List<ServerInfo> list3 = ServerList.getServerList(si2.sid);
                            foreach (ServerInfo si3 in list3)
                            {
                                TreeNode node3 = new TreeNode(si3.sname);
                                node3.Tag = si3;
                                node2.Nodes.Add(node3);
                                treeView1.SelectedNode = node3;
                            }
                        }
                    }
                    treeView1.ExpandAll();
                    treeView1.Refresh();
                }
                if (Completed)
                {
                    Launch_pb.Enabled = true;
                    InterfaceTimer.Enabled = false;
                    //出现过错误
                    if (ErrorFound)
                    {
                        CurrentFile_label.Text = "客户端更新发生错误，为了游戏体验，请重新更新.";
                    }
                    else
                    {
                        ActionLabel.Text = "";
                        CurrentFile_label.Text = "已完成更新.";
                        SpeedLabel.Text = "";
                        ProgressCurrent_pb.Width = 550;
                        TotalProg_pb.Width = 550;
                        CurrentFile_label.Visible = true;
                        CurrentPercent_label.Visible = false;
                        TotalPercent_label.Visible = false;
                        CurrentPercent_label.Text = "100%";
                        TotalPercent_label.Text = "100%";
                    }
                    
                    if (CleanFiles)
                    {
                        CleanFiles = false;
                        MessageBox.Show("你的客户端已清理.", "清理文件");
                    }

                    if (Restart)
                    {
                        Program.Restart = true;

                        MoveOldClientToCurrent();

                        Close();
                    }

                    if (Settings.P_AutoStart)
                    {
                        Launch();
                    }
                    return;
                }

                ActionLabel.Visible = true;
                SpeedLabel.Visible = true;
                CurrentFile_label.Visible = true;
                CurrentPercent_label.Visible = true;
                TotalPercent_label.Visible = true;

                if (LabelSwitch) ActionLabel.Text = string.Format("{0} 文件完成", _fileCount - _currentCount);
                else ActionLabel.Text = string.Format("{0:#,##0}MB 完成",  ((_totalBytes) - (_completedBytes + _currentBytes)) / 1024 / 1024);

                //ActionLabel.Text = string.Format("{0:#,##0}MB / {1:#,##0}MB", (_completedBytes + _currentBytes) / 1024 / 1024, _totalBytes / 1024 / 1024);

                if (_currentFile != null)
                {
                    //FileLabel.Text = string.Format("{0}, ({1:#,##0} MB) / ({2:#,##0} MB)", _currentFile.FileName, _currentBytes / 1024 / 1024, _currentFile.Compressed / 1024 / 1024);
                    CurrentFile_label.Text = string.Format("{0}", _currentFile.FileName);
                    SpeedLabel.Text = (_currentBytes / 1024F / _stopwatch.Elapsed.TotalSeconds).ToString("#,##0.##") + "KB/s";
                    CurrentPercent_label.Text = ((int)(100 * _currentBytes / _currentFile.Compressed)).ToString() + "%";
                    ProgressCurrent_pb.Width = (int)( 5.5 * (100 * _currentBytes / _currentFile.Compressed));
                }
                if (_totalBytes > 0)
                {
                    TotalPercent_label.Text = ((int)(100 * (_completedBytes + _currentBytes) / _totalBytes)).ToString() + "%";
                    TotalProg_pb.Width = (int)(5.5 * (100 * (_completedBytes + _currentBytes) / _totalBytes));
                }
            }
            catch
            {
                
            }
        }

        private void AMain_Click(object sender, EventArgs e)
        {
            if (ConfigForm.Visible) ConfigForm.Visible = false;
        }

        private void ActionLabel_Click(object sender, EventArgs e)
        {
            LabelSwitch = !LabelSwitch;
        }

        //窗口关闭的时候执行的方法
        private void AMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            WinClose = true;
            //需要把旧的客户端移动为新的客户端
            MoveOldClientToCurrent();
        }

        private void MoveOldClientToCurrent()
        {
            string oldClient = Settings.P_Client + oldClientName;
            string currentClient = Settings.P_Client + System.AppDomain.CurrentDomain.FriendlyName;

            if (!File.Exists(currentClient) && File.Exists(oldClient))
                FileMove(oldClient, currentClient);
        }
        //实现文件的重命名
        private void FileMove(string srcfile,string targetfile)
        {
            try
            {
                if (File.Exists(targetfile))
                {
                    File.Delete(targetfile);
                }
            }
            catch { }
            try
            {
                File.Move(srcfile, targetfile);
            }
            catch { }

            try
            {
                if (!File.Exists(targetfile)&& File.Exists(srcfile))
                {
                    FileInfo info = new FileInfo(srcfile);
                    info.MoveTo(targetfile);
                }
            }
            catch { }
        }

    }

    //文件列表信息
    public class FileInformation
    {
        //文件名称 相对路径，如：\Sound\wolf_ride01.wav  \Client.exe \Data\ChrSel.Lib
        public string FileName; //Relative.相对路径的文件名
        //文件长度，压缩后的文件长度
        public int Length, Compressed;
        //创建日期
        public DateTime Creation;
        //更新状态0：未更新 1-9是已更新的次数，一般尝试2次即可，10是更新完成，11是更新错误
        public int updateState=0;

        public FileInformation()
        {

        }
        public FileInformation(BinaryReader reader)
        {
            FileName = reader.ReadString();
            Length = reader.ReadInt32();
            Compressed = reader.ReadInt32();

            Creation = DateTime.FromBinary(reader.ReadInt64());
        }
        public void Save(BinaryWriter writer)
        {
            writer.Write(FileName);
            writer.Write(Length);
            writer.Write(Compressed);
            writer.Write(Creation.ToBinary());
        }
    }
}
