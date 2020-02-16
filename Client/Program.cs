using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using Launcher;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Client.MirSounds;

namespace Client
{
    internal static class Program
    {
        public static CMain Form;
        public static AMain PForm;

        public static bool Restart;



        [STAThread]
        private static void Main(string[] args)
        {

            #if DEBUG
            //Settings.UseTestConfig = true;
            #endif

            //MirLog.info("DEBUG:" + Settings.UseTestConfig);
            try
            {
                //加大连接并发数
                System.Net.ServicePointManager.DefaultConnectionLimit = 256;

                if (UpdatePatcher()) return;

                if (RuntimePolicyHelper.LegacyV2RuntimeEnabledSuccessfully == true) { }

                long currExeLen = new FileInfo(Process.GetCurrentProcess().MainModule.FileName).Length;
                //客户端多开限制，只运行开3个客户端
                int currClient = 0;
                string[] dsmach = { "Config", "Data", "DirectX", "Map", "Sound" };
                Process[] ps = Process.GetProcesses();
                foreach(Process p in ps)
                {
                    try
                    {
                        if (p.MainModule.FileName == null)
                        {
                            continue;
                        }
                        MirLog.info(p.MainModule.FileName);
                        if (new FileInfo(p.MainModule.FileName).Length == currExeLen)
                        {
                            currClient++;
                        }

                        FileInfo f = new FileInfo(p.MainModule.FileName);
                        DirectoryInfo[] ds = f.Directory.GetDirectories();
                        int dsmachcount = 0;
                        foreach (DirectoryInfo di in ds)
                        {
                            foreach (string dm in dsmach)
                            {
                                if (di.Name.ToLower().Equals(dm.ToLower()))
                                {
                                    dsmachcount++;
                                }
                            }
                        }
                        if (dsmachcount >= 5)
                        {
                            //currClient++;
                        }
                    }
                    catch(Exception e)
                    {
                        MirLog.error(e.Message);
                    }
                }
                if (currClient >= 4)
                {
                    MirLog.info("最多只运行同时打开3个客户端");
                    MessageBox.Show("最多只运行同时打开3个客户端", "提示", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
                    Application.Exit();
                    return;
                }
                

                Packet.IsServer = false;
                Settings.Load();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.Run(PForm = new Launcher.AMain());

                //if (Settings.P_Patcher) Application.Run(PForm = new Launcher.AMain());
                //else Application.Run(Form = new CMain());
                //Application.Run(Form = new CMain());
                //Application.Run( new Test());
                Settings.Save();
                CMain.InputKeys.Save();

                if (Restart)
                {
                    Application.Restart();
                }
            }
            catch (Exception ex)
            {
                CMain.SaveError(ex.ToString());
            }
        }

        //自动更新方法
        //如果存在AutoPatcher.gz，则执行AutoPatcher.gz。并退出当前应用
        private static bool UpdatePatcher()
        {
            try
            {
                const string fromName = @".\AutoPatcher.gz", toName = @".\AutoPatcher.exe";
                if (!File.Exists(fromName)) return false;

                Process[] processes = Process.GetProcessesByName("AutoPatcher");

                if (processes.Length > 0)
                {
                    string patcherPath = Application.StartupPath + @"\AutoPatcher.exe";

                    for (int i = 0; i < processes.Length; i++)
                        if (processes[i].MainModule.FileName == patcherPath)
                            processes[i].Kill();

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    bool wait = true;
                    processes = Process.GetProcessesByName("AutoPatcher");

                    while (wait)
                    {
                        wait = false;
                        for (int i = 0; i < processes.Length; i++)
                            if (processes[i].MainModule.FileName == patcherPath)
                            {
                                wait = true;
                            }

                        if (stopwatch.ElapsedMilliseconds <= 3000) continue;
                        MessageBox.Show("在更新过程中关闭自动更新程序失败.");
                        return true;
                    }
                }

                if (File.Exists(toName)) File.Delete(toName);
                File.Move(fromName, toName);
                Process.Start(toName, "Auto");

                return true;
            }
            catch (Exception ex)
            {
                CMain.SaveError(ex.ToString());
                
                throw;
            }
        }

        //.Net版本之间是有一定联系的，目前为止微软推出了3个版本的CLR，分别是 1.1， 2.0 ， 4.0 并且你要注意的是 .Net 4是基于CLR4的，而.Net 2.0 3.0 3.5都是基于 CLR2.0， 3.0 3.5其实只是在2.0的基础上增加了新的功能，并没有改变CLR。

        public static class RuntimePolicyHelper
        {
            public static bool LegacyV2RuntimeEnabledSuccessfully { get; private set; }

            static RuntimePolicyHelper()
            {
                ICLRRuntimeInfo clrRuntimeInfo =
                    (ICLRRuntimeInfo)RuntimeEnvironment.GetRuntimeInterfaceAsObject(
                        Guid.Empty,
                        typeof(ICLRRuntimeInfo).GUID);
                try
                {
                    clrRuntimeInfo.BindAsLegacyV2Runtime();
                    LegacyV2RuntimeEnabledSuccessfully = true;
                }
                catch (COMException)
                {
                    // This occurs with an HRESULT meaning 
                    // "A different runtime was already bound to the legacy CLR version 2 activation policy."
                    LegacyV2RuntimeEnabledSuccessfully = false;
                }
            }

            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("BD39D1D2-BA2F-486A-89B0-B4B0CB466891")]
            private interface ICLRRuntimeInfo
            {
                void xGetVersionString();
                void xGetRuntimeDirectory();
                void xIsLoaded();
                void xIsLoadable();
                void xLoadErrorString();
                void xLoadLibrary();
                void xGetProcAddress();
                void xGetInterface();
                void xSetDefaultStartupFlags();
                void xGetDefaultStartupFlags();

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void BindAsLegacyV2Runtime();
            }
        }

    }
}
