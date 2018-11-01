using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
/// <summary>
/// 日志处理类，正式发布后，关闭日志输出
/// </summary>
namespace Client
{
    public class MirLog
    {
        //日志级别，目前分为3级别，0:debug,1:info,2:error 
        private static readonly short msglevel = 0;

        public static void info(string msg)
        {
            if (msglevel > 1)
            {
                return;
            }
            System.Console.WriteLine(msg);
            File.AppendAllText( @".\mir2info.txt",msg);
        }

        public static void debug(string msg)
        {
            if (msglevel > 0)
            {
                return;
            }
            System.Console.WriteLine(msg);
            File.AppendAllText(@".\mir2debug.txt", msg);
        }

        public static void error(string msg)
        {
            if (msglevel > 2)
            {
                return;
            }
            System.Console.WriteLine(msg);
            File.AppendAllText(@".\mir2error.txt", msg);
        }
    }
}
