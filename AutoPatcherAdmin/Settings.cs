namespace AutoPatcherAdmin
{
    //FTP上传客户端文件更新
    public static class Settings
    {
        private static readonly InIReader Reader = new InIReader(@".\PatchAdmin.ini");
        //客户端路径
        public static string Client = @"S:\Patch\";
        //服务器端路径
        public static string Host = @"ftp://212.67.209.184/";
        //账号密码
        public static string Login = string.Empty;
        public static string Password = string.Empty;
        public static bool AllowCleanUp = true;//是否清除服务器中多余，没用的文件

        public static void Load()
        {
            Client = Reader.ReadString("AutoPatcher", "Client", Client);
            if (!Client.EndsWith("\\") && !Client.EndsWith("/"))
            {
                Client = Client + "\\";
            }
            Host = Reader.ReadString("AutoPatcher", "Host", Host);
            Login = Reader.ReadString("AutoPatcher", "Login", Login);
            Password = Reader.ReadString("AutoPatcher", "Password", Password);

            AllowCleanUp = Reader.ReadBoolean("AutoPatcher", "AllowCleanUp", AllowCleanUp);
        }

        public static void Save()
        {
            Reader.Write("AutoPatcher", "Client", Client);
            Reader.Write("AutoPatcher", "Host", Host);
            Reader.Write("AutoPatcher", "Login", Login);
            Reader.Write("AutoPatcher", "Password", Password);
            Reader.Write("AutoPatcher", "AllowCleanUp", AllowCleanUp);
        }
    }
}
