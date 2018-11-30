using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace AutoPatcherAdmin
{
    public partial class AMain : Form
    {
        public const string PatchFileName = @"PList.gz";
        //排除列表
        public string[] ExcludeList = new string[] { "Thumbs.db" };

        public List<FileInformation> OldList, NewList;
        public Queue<FileInformation> UploadList;
        private Stopwatch _stopwatch = Stopwatch.StartNew();
        //所有的
        long _totalBytes, _completedBytes;
        //当前的
        long _preBytes,_currBytes, _currcompletedBytes;
        string _currFileName="";//当前上传的文件名
        bool stop = false;//出现上传错误


        public AMain()
        {
            InitializeComponent();

            ClientTextBox.Text = Settings.Client;
            HostTextBox.Text = Settings.Host;
            LoginTextBox.Text = Settings.Login;
            PasswordTextBox.Text = Settings.Password;
            AllowCleanCheckBox.Checked = Settings.AllowCleanUp;
            //加大连接并发数
            System.Net.ServicePointManager.DefaultConnectionLimit = 256;
            timer1.Start();
        }

        private void ProcessButton_Click(object sender, EventArgs e)
        {
            try
            {
                ProcessButton.Enabled = false;
                Settings.Client = ClientTextBox.Text;
                Settings.Host = HostTextBox.Text;
                Settings.Host = Settings.Host.Replace(@"\", "/");
                if (!Settings.Host.EndsWith("/"))
                {
                    Settings.Host = Settings.Host + "/";
                }
                Settings.Login = LoginTextBox.Text;
                Settings.Password = PasswordTextBox.Text;
                Settings.AllowCleanUp = AllowCleanCheckBox.Checked;

                OldList = new List<FileInformation>();
                NewList = new List<FileInformation>();
                UploadList = new Queue<FileInformation>();

                byte[] data = Download(PatchFileName);

                if (data != null)
                {
                    using (MemoryStream stream = new MemoryStream(data))
                    using (BinaryReader reader = new BinaryReader(stream))
                        ParseOld(reader);
                }

                ActionLabel.Text = "Checking Files...";
                Refresh();
                
                CheckFiles();

                for (int i = 0; i < NewList.Count; i++)
                {
                    FileInformation info = NewList[i];

                    if (InExcludeList(info.FileName)) continue;

                    if (NeedUpdate(info))
                    {
                        UploadList.Enqueue(info);
                        _totalBytes += info.Length;
                    }
                    else
                    {
                        for (int o = 0; o < OldList.Count; o++)
                        {
                            if (OldList[o].FileName != info.FileName) continue;
                            NewList[i] = OldList[o];
                            break;
                        }
                    }
                }
                Thread thread1 = new Thread(delegate () { uploadAll(); });
                thread1.IsBackground = true;
                thread1.Start();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                ActionLabel.Text = "Error...";
            }

        }

  

        private void CleanUp()
        {
            if (!Settings.AllowCleanUp) return;

            for (int i = 0; i < OldList.Count; i++)
            {
                if (NeedFile(OldList[i].FileName)) continue;

                try
                {
                    FtpWebRequest request = (FtpWebRequest) WebRequest.Create(new Uri(Settings.Host + OldList[i].FileName + ".gz"));
                    request.Credentials = new NetworkCredential(Settings.Login, Settings.Password);
                    request.Method = WebRequestMethods.Ftp.DeleteFile;
                    FtpWebResponse response = (FtpWebResponse) request.GetResponse();
                    response.Close();
                }
                catch 
                {

                }
            }

        }
        public bool NeedFile(string fileName)
        {
            for (int i = 0; i < NewList.Count; i++)
            {
                if (fileName.EndsWith(NewList[i].FileName) && !InExcludeList(NewList[i].FileName))
                    return true;
            }

            return false;
        }

        public void ParseOld(BinaryReader reader)
        {
            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
                OldList.Add(new FileInformation(reader));
        }

        //最新的更新列表
        public byte[] CreateNew()
        {
            List<FileInformation> curr = new List<FileInformation>();
            for (int i = 0; i < NewList.Count; i++)
            {
                //完成的才更新，否则写入旧的
                if (NewList[i].update)
                {
                    curr.Add(NewList[i]);
                }
                else
                {
                    for (int o = 0; o < OldList.Count; o++)
                    {
                        if (OldList[o].FileName != NewList[i].FileName) continue;
                        curr.Add(OldList[o]);
                        break;
                    }
                }
            }

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(curr.Count);
                for (int i = 0; i < curr.Count; i++)
                {
                    curr[i].Save(writer);
                }
                return stream.ToArray();
            }
        }
        public void CheckFiles()
        {
            string[] files = Directory.GetFiles(Settings.Client, "*.*" ,SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
                NewList.Add(GetFileInformation(files[i]));
        }

        public bool InExcludeList(string fileName)
        {
            foreach (var item in ExcludeList)
            {
                if (fileName.EndsWith(item)) return true;
            }

            return false;
        }

        public bool NeedUpdate(FileInformation info)
        {
            for (int i = 0; i < OldList.Count; i++)
            {
                FileInformation old = OldList[i];
                if (old.FileName != info.FileName) continue;

                if (old.Length != info.Length) return true;
                if (old.Creation != info.Creation) return true;

                return false;
            }
            return true;
        }

        public FileInformation GetFileInformation(string fileName)
        {
            FileInfo info = new FileInfo(fileName);

            FileInformation file =  new FileInformation
                {
                    FileName = fileName.Remove(0, Settings.Client.Length),
                    Length = (int) info.Length,
                    Creation = info.LastWriteTime
                };

            if (file.FileName == "AutoPatcher.exe")
                file.FileName = "AutoPatcher.gz";

            return file;
        }


        public byte[] Download(string fileName)
        {
            try
            {
                using (MemoryStream mStream = new MemoryStream())
                {
                    using (BinaryWriter gStream = new BinaryWriter(mStream))
                    {
                        FtpWebRequest reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(Settings.Host + fileName));
                        reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                        reqFTP.UseBinary = true;
                        reqFTP.UsePassive = false;
                        reqFTP.Credentials = new NetworkCredential(Settings.Login, Settings.Password);
                        FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                        Stream ftpStream = response.GetResponseStream();
                        long cl = response.ContentLength;
                        int bufferSize = 2048;
                        int readCount;
                        byte[] buffer = new byte[bufferSize];

                        readCount = ftpStream.Read(buffer, 0, bufferSize);
                        while (readCount > 0)
                        {
                            gStream.Write(buffer, 0, readCount);
                            readCount = ftpStream.Read(buffer, 0, bufferSize);
                        }
                        byte[] ret= mStream.ToArray();
                        ftpStream.Close();
                        response.Close();
                        return ret;
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
                System.Console.WriteLine("下载文件失败："+ fileName);
                return null;
            }
        }

        //上传所有的文件
        //这个开线程进行处理
        public void uploadAll()
        {
            while (UploadList.Count > 0)
            {
                FileInformation info = UploadList.Dequeue();
                Upload2(info);
                if (stop)
                {
                    return;
                }
            }
            CleanUp();
            //上传完成后，还要传一个列表
            UploadPatchFile();
        }

        //上传最新的列表
        public void UploadPatchFile()
        {
            string uri = Settings.Host + PatchFileName;
            FtpWebRequest reqFTP;
            byte[] buff = CreateNew();
            if(buff==null|| buff.Length < 10)
            {
                return;
            }
            reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));
            reqFTP.Credentials = new NetworkCredential(Settings.Login, Settings.Password);
            reqFTP.KeepAlive = false;
            reqFTP.Method = WebRequestMethods.Ftp.UploadFile;
            reqFTP.UseBinary = true;
            reqFTP.UsePassive = false;
            //reqFTP.ContentLength = buff.Length;
            try
            {
                using (Stream strm = reqFTP.GetRequestStream())
                {
                    strm.Write(buff, 0, buff.Length);
                }
                System.Console.WriteLine("列表上传完成：" );
            }
            catch (Exception ex)
            {
                throw new Exception("UploadPatchFile Upload Error --> " + ex.Message);
            }
        }

        /// <summary>
        /// 上传,如果失败了，则再次放到队列尾部
        /// </summary>
        /// <param name="filename"></param>
        public void Upload2(FileInformation info)
        {
            string fileName = info.FileName.Replace(@"\", "/");
            FileInfo fileInf = new FileInfo(Settings.Client+ fileName);
  
            if (fileName != "AutoPatcher.gz" && fileName != "PList.gz")
                fileName += ".gz";
            System.Console.WriteLine("上传文件3：" + fileName+",len:"+fileInf.Length);
            info.Compressed = (int)fileInf.Length;
            string uri = Settings.Host + fileName;
            _currFileName = fileName;
            FtpWebRequest reqFTP;
            long tt = _completedBytes;
            _currBytes = 0;
            _currcompletedBytes = fileInf.Length;
            
            //DefaultConnectionLimit
            reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));
            reqFTP.Credentials = new NetworkCredential(Settings.Login, Settings.Password);
            reqFTP.KeepAlive = false;
            reqFTP.Method = WebRequestMethods.Ftp.UploadFile;
            reqFTP.UseBinary = true;
            reqFTP.UsePassive = false;
            //reqFTP.Timeout = System.Threading.Timeout.Infinite;
            //reqFTP.ContentLength = fileInf.Length;
            try
            {
                int buffLength = 2048;
                byte[] buff = new byte[buffLength];
                int contentLen;
                using (FileStream fs = fileInf.OpenRead())
                {
                    using (Stream strm = reqFTP.GetRequestStream())
                    {
                        contentLen = fs.Read(buff, 0, buffLength);
                        while (contentLen != 0)
                        {
                            strm.Write(buff, 0, contentLen);
                            contentLen = fs.Read(buff, 0, buffLength);
                            _currBytes += contentLen;
                            _completedBytes += contentLen;
                            //2M刷新一次
                            if (_completedBytes % 204800 == 0)
                            {
                                strm.Flush();
                            }
                        }
                        strm.Flush();
                        _completedBytes = tt + info.Compressed;
                        info.update = true;
                        System.Console.WriteLine("上传文件：" + fileName + ",完成");
                        //strm.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                if (info.update == false)
                {
                    System.Console.WriteLine("上传文件：" + fileName + ",错误:" + ex.Message);
                    CheckDirectory(Path.GetDirectoryName(fileName));
                    _completedBytes = tt;
                    UploadList.Enqueue(info);
                }
                //throw new Exception("Ftphelper Upload Error --> " + ex.Message);
            }
        }


        public void Upload(FileInformation info, byte[] raw, bool retry = true)
        {
            string fileName = info.FileName.Replace(@"\", "/");

            if (fileName != "AutoPatcher.gz" && fileName != "PList.gz")
                fileName += ".gz";
            System.Console.WriteLine("上传文件：" + fileName);
            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential(Settings.Login, Settings.Password);
                //client.UseBinary = true;

                byte[] data = !retry ? raw : raw;
                info.Compressed = data.Length;

                client.UploadProgressChanged += (o, e) =>
                    {
                        int value = (int)(100 * e.BytesSent / e.TotalBytesToSend);
                        progressBar2.Value = value > progressBar2.Maximum ? progressBar2.Maximum : value;

                        FileLabel.Text = fileName;
                        SizeLabel.Text = string.Format("{0} KB / {1} KB", e.BytesSent / 1024, e.TotalBytesToSend  / 1024);
                        SpeedLabel.Text = ((double) e.BytesSent/1024/_stopwatch.Elapsed.TotalSeconds).ToString("0.##") + " KB/s";
                    };

                client.UploadDataCompleted += (o, e) =>
                    {
                        _completedBytes += info.Length;

                        if (e.Error != null && retry)
                        {
                            CheckDirectory(Path.GetDirectoryName(fileName));
                            Upload(info, data, false);
                            return;
                        }

                        if (info.FileName == PatchFileName)
                        {
                            FileLabel.Text = "Complete...";
                            SizeLabel.Text = "Complete...";
                            SpeedLabel.Text = "Complete...";
                            return;
                        }

                        progressBar1.Value = (int)(_completedBytes * 100 / _totalBytes) > 100 ? 100 : (int)(_completedBytes * 100 / _totalBytes);
                        //BeginUpload();
                    };

                _stopwatch = Stopwatch.StartNew();

                client.UploadDataAsync(new Uri(Settings.Host + fileName), data);
            }
        }

        public void CheckDirectory(string directory)
        {
            string Directory = "";
            char[] splitChar = { '\\' };
            string[] DirectoryList = directory.Split(splitChar);

            foreach (string directoryCheck in DirectoryList)
            {
                Directory += "\\" + directoryCheck;
            try
              {
                        if (string.IsNullOrEmpty(Directory)) return;

                        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(Settings.Host + Directory + "/");
                        request.Credentials = new NetworkCredential(Settings.Login, Settings.Password);
                        request.Method = WebRequestMethods.Ftp.MakeDirectory;

                        request.UsePassive = true;
                        request.UseBinary = true;
                        request.KeepAlive = false;
                        FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                        Stream ftpStream = response.GetResponseStream();

                        if (ftpStream != null) ftpStream.Close();
                        response.Close();

              }
                    catch (WebException ex)
                    {
                        FtpWebResponse response = (FtpWebResponse)ex.Response;
                        response.Close();
                    }
            }
        }
        //解压
        public static byte[] Decompress2(byte[] raw)
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
        //压缩
        public static byte[] Compress2(byte[] raw)
        {
            using (MemoryStream mStream = new MemoryStream())
            {
                using (GZipStream gStream = new GZipStream(mStream, CompressionMode.Compress, true))
                    gStream.Write(raw, 0, raw.Length);
                return mStream.ToArray();
            }
        }

        private void AMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            stop = true;
        }

        //定时执行
        //刷新界面进度
        private void timer1_Tick(object sender, EventArgs e)
        {
            //最后上传列表
            if (_completedBytes >= _totalBytes)
            {
                ActionLabel.Text = string.Format("Complete...");
                ProcessButton.Enabled = true;
                return;
            }
            if (_currcompletedBytes > 0)
            {
                int value = (int)(100 * _currBytes / _currcompletedBytes);
                progressBar2.Value = value > progressBar2.Maximum ? progressBar2.Maximum : value;
            }
            if (_totalBytes > 0)
            {
                progressBar1.Value = (int)(_completedBytes * 100 / _totalBytes) > 100 ? 100 : (int)(_completedBytes * 100 / _totalBytes);
            }

            FileLabel.Text = _currFileName;
            SizeLabel.Text = string.Format("{0} KB / {1} KB", _currBytes / 1024, _currcompletedBytes / 1024);
            SpeedLabel.Text = ((double)(_currBytes- _preBytes) / 1024 / 0.5).ToString("0.##") + " KB/s";
            ActionLabel.Text = string.Format("Uploading... Files: {0}, Total Size: {1:#,##0}MB (Uncompressed)", UploadList.Count, (_totalBytes - _completedBytes) / 1048576);
            _preBytes = _currBytes;
        }

        private void ListButton_Click(object sender, EventArgs e)
        {
            Settings.Client = ClientTextBox.Text;
            Settings.Host = HostTextBox.Text;
            Settings.Host = Settings.Host.Replace(@"\", "/");
            if (!Settings.Host.EndsWith("/"))
            {
                Settings.Host = Settings.Host + "/";
            }
            Settings.Login = LoginTextBox.Text;
            Settings.Password = PasswordTextBox.Text;
            //处理过的就不处理了哦。
            if (NewList == null || NewList.Count == 0)
            {
                OldList = new List<FileInformation>();
                NewList = new List<FileInformation>();
                byte[] data = Download(PatchFileName);

                if (data != null)
                {
                    using (MemoryStream stream = new MemoryStream(data))
                    using (BinaryReader reader = new BinaryReader(stream))
                        ParseOld(reader);
                }


                CheckFiles();


                for (int i = 0; i < NewList.Count; i++)
                {
                    FileInformation info = NewList[i];
                    for (int o = 0; o < OldList.Count; o++)
                    {
                        if (OldList[o].FileName != info.FileName) continue;
                        NewList[i].Compressed = OldList[o].Compressed;
                        break;
                    }
                }
            }
            

            Thread thread1 = new Thread(delegate () { UploadPatchFile();  });
            thread1.IsBackground = true;
            thread1.Start();
            //Upload(new FileInformation { FileName = PatchFileName }, CreateNew());
            
        }

        private void SourceLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            SourceLinkLabel.LinkVisited = true;
            Process.Start("http://www.lomcn.org/forum/member.php?141-Jamie-Hello");
        }

        private void AMain_Load(object sender, EventArgs e)
        {

        }

    }

    public class FileInformation
    {
        public string FileName; //Relative.
        public int Length, Compressed;
        public DateTime Creation;
        public bool update;//是否已更新，包括是否已上传，是否已下载等

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
