﻿using System;
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

        long _totalBytes, _completedBytes, _currentBytes;
        private int _fileCount, _currentCount;

        private FileInformation _currentFile;
        //是否已完成
        public bool Completed, Checked, CleanFiles, LabelSwitch, ErrorFound;
        
        public List<FileInformation> OldList;
        public Queue<FileInformation> DownloadList;

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
            BackColor = Color.FromArgb(1, 0, 0);
            TransparencyKey = Color.FromArgb(1, 0, 0);
        }

        public static void SaveError(string ex)
        {
            try
            {
                if (Settings.RemainingErrorLogs-- > 0)
                {
                    File.AppendAllText(@".\Error.txt",
                                       string.Format("[{0}] {1}{2}", DateTime.Now, ex, Environment.NewLine));
                }
            }
            catch
            {
            }
        }

        public void Start()
        {
            try
            {
                OldList = new List<FileInformation>();
                DownloadList = new Queue<FileInformation>();

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
                    return;
                }

                _fileCount = OldList.Count;
                for (int i = 0; i < OldList.Count; i++)
                    CheckFile(OldList[i]);

                Checked = true;
                _fileCount = 0;
                _currentCount = 0;


                _fileCount = DownloadList.Count;
                BeginDownload();
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

        

        private void BeginDownload()
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

            _currentFile = DownloadList.Dequeue();

            Download(_currentFile);
        }
        //清除没用的文件
        private void CleanUp()
        {
            if (!CleanFiles) return;

            string[] fileNames = Directory.GetFiles(@".\", "*.*", SearchOption.AllDirectories);
            string fileName;
            for (int i = 0; i < fileNames.Length; i++)
            {
                if (fileNames[i].StartsWith(".\\Screenshots\\")) continue;

                fileName = Path.GetFileName(fileNames[i]);

                if (fileName == "Mir2Config.ini" || fileName == System.AppDomain.CurrentDomain.FriendlyName) continue;

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

        public void CheckFile(FileInformation old)
        {
            FileInformation info = GetFileInformation(Settings.P_Client + old.FileName);
            _currentCount++;

            if (info == null || old.Length != info.Length || old.Creation != info.Creation)
            {
                if ((old.FileName.EndsWith(System.AppDomain.CurrentDomain.FriendlyName)))
                {
                    File.Move(Settings.P_Client + System.AppDomain.CurrentDomain.FriendlyName, Settings.P_Client + oldClientName);
                    Restart = true;
                }

                DownloadList.Enqueue(old);
                _totalBytes += old.Length;
            }
        }

        public void Download(FileInformation info)
        {
            string fileName = info.FileName.Replace(@"\", "/");

            if (fileName != "PList.gz")
                fileName += ".gz";

            try
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadProgressChanged += (o, e) =>
                        {
                            _currentBytes = e.BytesReceived;
                        };
                    client.DownloadDataCompleted += (o, e) =>
                        {
                            if (e.Error != null)
                            {
                                File.AppendAllText(@".\Error.txt",
                                       string.Format("[{0}] {1}{2}", DateTime.Now, info.FileName + " could not be downloaded. (" + e.Error.Message + ")", Environment.NewLine));
                                ErrorFound = true;
                            }
                            else
                            {
                                _currentCount++;
                                _completedBytes += _currentBytes;
                                _currentBytes = 0;
                                _stopwatch.Stop();

                            if (!Directory.Exists(Settings.P_Client + Path.GetDirectoryName(info.FileName)))
                                Directory.CreateDirectory(Settings.P_Client + Path.GetDirectoryName(info.FileName));

                            File.WriteAllBytes(Settings.P_Client + info.FileName, e.Result);
                            File.SetLastWriteTime(Settings.P_Client + info.FileName, info.Creation);
                            }
                            BeginDownload();
                        };

                    if (Settings.P_NeedLogin) client.Credentials = new NetworkCredential(Settings.AccountID, Settings.Password);


                    _stopwatch = Stopwatch.StartNew();
                    client.DownloadDataAsync(new Uri(Settings.P_Host + fileName));
                }
            }
            catch
            {
                MessageBox.Show(string.Format("下载文件错误: {0}", fileName));
            }
        }

        public byte[] Download(string fileName)
        {
            fileName = fileName.Replace(@"\", "/");

            if (fileName != "PList.gz")
                fileName += Path.GetExtension(fileName);

            try
            {
                using (WebClient client = new WebClient())
                {
                    if (Settings.P_NeedLogin)
                        client.Credentials = new NetworkCredential(Settings.P_Login, Settings.Password);
                    else
                        client.Credentials = new NetworkCredential("", "");

                    return client.DownloadData(Settings.P_Host + Path.ChangeExtension(fileName, ".gz"));
                }
            }
            catch
            {
                return null;
            }
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
                if (Completed)
                {
                    
                    ActionLabel.Text = "";
                    CurrentFile_label.Text = "已完成更新.";
                    SpeedLabel.Text = "";
                    ProgressCurrent_pb.Width = 550;
                    TotalProg_pb.Width = 550;
                    CurrentFile_label.Visible = true;
                    CurrentPercent_label.Visible = true;
                    TotalPercent_label.Visible = true;
                    CurrentPercent_label.Text = "100%";
                    TotalPercent_label.Text = "100%";
                    InterfaceTimer.Enabled = false;
                    Launch_pb.Enabled = true;
                    if (ErrorFound) MessageBox.Show("一个或多个文件更新错误，为了游戏体验，请重新更新.", "更新错误.");
                    ErrorFound = false;

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
                TotalPercent_label.Text = ((int)(100 * (_completedBytes + _currentBytes) / _totalBytes)).ToString() + "%";
                TotalProg_pb.Width = (int)(5.5 * (100 * (_completedBytes + _currentBytes) / _totalBytes));
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


        private void AMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            MoveOldClientToCurrent();
        }

        private void MoveOldClientToCurrent()
        {
            string oldClient = Settings.P_Client + oldClientName;
            string currentClient = Settings.P_Client + System.AppDomain.CurrentDomain.FriendlyName;

            if (!File.Exists(currentClient) && File.Exists(oldClient))
                File.Move(oldClient, currentClient);
        }

    }

    //文件列表信息
    public class FileInformation
    {
        //文件名称
        public string FileName; //Relative.
        //文件长度，压缩后的文件长度
        public int Length, Compressed;
        //创建日期
        public DateTime Creation;

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
