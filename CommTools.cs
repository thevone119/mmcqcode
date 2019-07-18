using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Data.SQLite;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Drawing;
using System.Collections.Specialized;
using System.Net.NetworkInformation;

/// <summary>
/// 日志处理类，正式发布后，关闭日志输出
/// </summary>
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
        
        //File.AppendAllText( @".\mir2info.txt",msg);
    }

    public static void debug(string msg)
    {
        if (msglevel > 0)
        {
            return;
        }
        System.Console.WriteLine(msg);
        //File.AppendAllText(@".\mir2debug.txt", msg);
    }

    public static void error(string msg)
    {
        if (msglevel > 2)
        {
            return;
        }
        System.Console.WriteLine(msg);
        //File.AppendAllText(@".\mir2error.txt", msg);
    }
}


/// <summary>
/// 唯一值工具类
/// 构建一个多个服务器，多个线程并都不太可能重复的值(4位的随机ID+毫秒时间+递增数)
/// 固定部分（年月日时分秒+4位随机数+递增数（0-））
/// uint型为无符号32位整数，占4个字节，取值范围在0~4,294,967,295之间。10位
//long型为64位有符号整数，占8个字节，取值范围在9,223,372,036,854,775,808~9,223,372,036,854,775,807之间。19位
//ulong型为64位无符号整数，占8个字节，取值范围在0 ~18,446,744,073,709,551,615之间。20位
/// </summary>
public static class UniqueKeyHelper
{
    private static long slongNo = 1;
    private static int sintNo = 1;


    //计算的最小时间，从18年开始
    private static readonly DateTime minTime = Convert.ToDateTime("2018-01-01 00:00:00");
    //对象锁
    private static object lockobj = new object();
    private static long fixedNo = TotalMilliseconds();//11-12位的固定的时间
    private static long fixedAddNo = 0;//7位的固定自增

    //获取毫秒时间
    //从2018年开始，到现在的毫秒时间，一般都是11位，最多12位
    public static long TotalMilliseconds()
    {
        return (long)(DateTime.Now - minTime).TotalMilliseconds;
    }

    /// <summary>
    /// 获取下一个值
    /// 支持多线程并发
    /// </summary>
    /// <returns></returns>
    public static long NextLong()
    {
        if (Interlocked.Increment(ref slongNo) > long.MaxValue - 10000)
        {
            slongNo = 1;
        }
        return slongNo;
    }

    /// <summary>
    /// 获取下一个值
    /// 支持多线程并发
    /// </summary>
    /// <returns></returns>
    public static int NextInt()
    {
        if (Interlocked.Increment(ref sintNo) > int.MaxValue - 10000)
        {
            sintNo = 1;
        }
        return sintNo;
    }


    /// <summary>
    /// 获取下一个值
    /// 支持多线程并发
    /// 支持多服务器并发也没事,几乎不会出现重复的
    /// </summary>
    /// <returns></returns>
    public static long UniqueNext()
    {
        lock (lockobj)
        {
            fixedAddNo++;
            if (fixedAddNo>= 10000000)//超过8位，则复原
            {
                fixedNo= TotalMilliseconds();//11-12位的固定的时间
                fixedAddNo = 1;//循环自增加部分（7位自增）
            }
            //
            return long.Parse(fixedNo + fixedAddNo.ToString("0000000"));
        }
    }
}


//快速随机函数的工具而已嘛，需要搞什么线程么。
//不用线程产生的随机数竟然有问题。坑爹啊,还是用线程变量吧
public class RandomUtils
{
    private static int seed = Environment.TickCount;
    private static ThreadLocal<Random> RandomWrapper = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));
    //private static Random random = new Random();


    public static int Next()
    {
        return RandomWrapper.Value.Next();
    }
    public static int Next(int maxValue)
    {
        return RandomWrapper.Value.Next(maxValue);
    }
    public static int Next(int minValue, int maxValue)
    {
        return RandomWrapper.Value.Next(minValue, maxValue);
    }
    public static double NextDouble()
    {
        return RandomWrapper.Value.NextDouble();
    }

    //最大增加数，增加几率1/x
    //装备极品是用这个做的做的，所以加1点属性是
    public static int RandomomRange(int count, int rate)
    {
        int x = 0;
        for (int i = 0; i < count; i++) if (Next(rate) == 0) x++;
        return x;
    }


}

/// <summary>
/// MD5的工具类
/// </summary>
public class MD5Utils
{
    private static JsonSerializerSettings settings = new JsonSerializerSettings()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        MaxDepth = 2
    };

    //计算文件的MD5，文件较大的时候，比较慢
    public static string GetMD5HashFromFile(string fileName)
    {
        long filelong = 0;
        try
        {
            FileInfo f = new FileInfo(fileName);
            if (!f.Exists)
            {
                return null;
            }
            filelong = f.Length;
            FileStream file = new FileStream(fileName, FileMode.Open);
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
        catch (Exception )
        {
        }

        return "ERROR:" + filelong;
    }
    //实现快速的文件MD5,只取10段1024的加上长度做MD5
    public static string GetFastMd5(string fileName)
    {
        long filelong = 0;
        try
        {
            FileInfo f = new FileInfo(fileName);
            if (!f.Exists)
            {
                return null;
            }
            filelong = f.Length;
            long step = filelong / 10;
            FileStream file = new FileStream(fileName, FileMode.Open);
            byte[] temp = new byte[102400];
            for(int i = 0; i < 10; i++)
            {
                file.Seek(step * i, SeekOrigin.Begin);
                int redlen = file.Read(temp, i*1024,1024);
            }
            file.Close();
            MemoryStream memory = new MemoryStream(temp);
            BinaryWriter memory2 = new BinaryWriter(memory);
            memory2.Write(filelong);
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(memory);
            memory.Close();
            memory2.Close();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
        catch (Exception)
        {
        }

        return "ERROR:" + filelong;
    }


    public static string MD5Encode(String origin)
    {
        byte[] result = Encoding.UTF8.GetBytes(origin);    //utf-8的字符串
        System.Security.Cryptography.MD5 md5 = new MD5CryptoServiceProvider();
        byte[] output = md5.ComputeHash(result);
        return BitConverter.ToString(output).Replace("-", "");  //tbMd5pass为输出加密文本
    }
    /// <summary>
    /// 用来判断对象中的数据是否发生变化
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public static string ObjectMD5(Object o)
    {
         return ObjectMD5Reflection(o);
    }

    /// <summary>
    /// 通过JSON获取对象的MD5
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public static string ObjectMD5Json(Object o)
    {
        string ojson = JsonConvert.SerializeObject(o, settings);
        return MD5Encode(ojson);
    }

    /// <summary>
    /// 通过反射获取对象的MD5
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public static string ObjectMD5Reflection(Object o)
    {
        StringBuilder sb = new StringBuilder();
        FieldInfo[]  fields = o.GetType().GetFields();
        for(int i=0;i< fields.Length; i++)
        {
            if (fields[i].IsStatic)
            {
                continue;
            }
            if (fields[i].IsDefined(typeof(JsonIgnoreAttribute), true)) continue;
            object v = fields[i].GetValue(o);
            if (v == null)
            {
                continue;
            }
            //如果是集合，则需要遍历集合
            if(v is IEnumerable)
            {
                IEnumerable iv = (IEnumerable)v;
                IEnumerator it = iv.GetEnumerator();
                while (it!=null && it.MoveNext())
                {
                    if (it != null&&it.Current != null)
                    {
                        sb.Append(it.Current.ToString());
                    }
                }
            }else
            {
                sb.Append(v.ToString());
            }
        }
        return MD5Encode(sb.ToString());
    }
}

//反射实现工具类
public class ReflectionUtils
{
    public static string ObjectToString(Object o)
    {
        StringBuilder sb = new StringBuilder();
        FieldInfo[] fields = o.GetType().GetFields();
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i].IsStatic)
            {
                continue;
            }
            if (fields[i].IsDefined(typeof(JsonIgnoreAttribute), true)) continue;
            object v = fields[i].GetValue(o);
            if (v == null)
            {
                continue;
            }
            sb.Append(","+fields[i].Name+"=");

            //如果是集合，则需要遍历集合
            sb.Append(v.ToString());
        }
        return sb.ToString();
    }

}


/// <summary>
/// 数据库对象工具，用于判断某个数据库对象是否发生改变
/// 记录数据库对象的状态
/// 0：没有变化
/// 1：新增
/// 2：修改
/// </summary>
public class DBObjectUtils
{
    //对象锁
    private static object lockobj = new object();

    //用这个来记录，保存对象是否发生变化 
    private static Dictionary<string, string> dbstate= new Dictionary<string, string>();
    //记录数据库的最大ID
    private static Dictionary<string, ulong> dbMAXid = new Dictionary<string, ulong>();



    /// <summary>
    /// 记录数据库对象的状态
    /// 0：没有变化
    /// 1：新增
    /// 2：修改
    /// </summary>
    /// <param name="o"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static byte ObjState(Object o, object id)
    {
        string key = o.GetType().Name + id;
        string md5 = MD5Utils.ObjectMD5(o);
        if (dbstate.ContainsKey(key))
        {
            if(dbstate[key]== md5)
            {
                return 0;
            }else
            {
                return 2;
            }
        }
        return 1;
    }
    //取下一个ID
    public static ulong getObjNextId(Object o)
    {
        lock (lockobj)
        {
            string maxkey = o.GetType().Name;
            if (dbMAXid.ContainsKey(maxkey))
            {
                dbMAXid[maxkey] = dbMAXid[maxkey] + 1;
                return dbMAXid[maxkey];
            }
            else
            {
                dbMAXid.Add(maxkey, 1);
                return 1;
            }
        }
    }

    //取当前最大ID
    public static ulong getObjCurrMaxId(Object o)
    {
        string maxkey = o.GetType().Name;
        if (dbMAXid.ContainsKey(maxkey))
        {
            dbMAXid[maxkey] = dbMAXid[maxkey] + 1;
            return dbMAXid[maxkey];
        }
        return 0;
    }

    //更新对象状态
    public static void updateObjState(Object o, object id)
    {
        ulong cid = ulong.Parse(id.ToString());
        if (cid == 0)
        {
            return;
        }
        string maxkey = o.GetType().Name;
        //更新ID
        if (dbMAXid.ContainsKey(maxkey))
        {
            ulong lastid = dbMAXid[maxkey];
            if (cid > lastid)
            {
                dbMAXid[maxkey] = cid;
            }
        }
        else
        {
            dbMAXid[maxkey] = cid;
        }
        //更新MD5
        string key = o.GetType().Name + id;
        string md5 = MD5Utils.ObjectMD5(o);
        if (dbstate.ContainsKey(key))
        {
            dbstate[key] = md5;
            return;
        }
        dbstate.Add(key, md5);
    }
}

//自定义注解类
class DBField : Attribute
{
    public Boolean PrimaryKey = false;
    //1:bool,2:byte,3:short,4:int,5:long,6:float,7:double,
    //11:string,12:jsonstring
    public byte Type = 1;
}

///
/// SQLiteHelper类
/// 看怎么支持多线程并发处理哦
/// 
///
public class SQLiteHelper : System.IDisposable
{
    private SQLiteConnection _SQLiteConn = null;
    private SQLiteTransaction _SQLiteTrans = null;

    private bool _IsRunTrans = false;//正在进行事物
    private bool _autocommit;//是否自动提交
    private static object lockTrans = new object();

    ///
    /// 构造函数
    ///
    public SQLiteHelper(string ConnectionString)
    {
        _SQLiteConn = new SQLiteConnection(ConnectionString);
        //this._SQLiteConn.Commit += new SQLiteCommitHandler(_SQLiteConn_Commit);
        this._SQLiteConn.RollBack += new EventHandler(_SQLiteConn_RollBack);
    }

    ~SQLiteHelper()
    {
        this.Dispose();
    }

    /// <summary>
    /// 打开数据连接
    /// </summary>
    private void Open()
    {
        if (_SQLiteConn.State == ConnectionState.Closed)
        {
            _SQLiteConn.Open();
        }
    }

    /// <summary>
    /// 执行SQL语句
    /// </summary>
    /// <param name="command"></param>
    /// <param name="parameter"></param>
    /// <returns></returns>
    public int Execute(string command, params SQLiteParameter[] parameter)
    {
        int result = -1;

        this.Open();
        using (SQLiteCommand sqlitecmd = new SQLiteCommand(command, _SQLiteConn))
        {
            if (parameter != null)
            {
                sqlitecmd.Parameters.AddRange(parameter);
            }
            result = sqlitecmd.ExecuteNonQuery();
        }
        return result;
    }

    /// <summary>
    /// 执行SQL语句
    /// </summary>
    /// <param name="command"></param>
    /// <returns>返回第一行第一列值</returns>
    public object ExecuteScalar(string command, params SQLiteParameter[] parmeter)
    {
        object result = null;
        this.Open();
        using (SQLiteCommand sqlitecmd = new SQLiteCommand(command, _SQLiteConn))
        {
            if (parmeter != null)
            {
                sqlitecmd.Parameters.AddRange(parmeter);
            }
            result = sqlitecmd.ExecuteScalar();
        }
        return result;
    }
    /// <summary>
    /// 执行SQL语句，获取数据集
    /// </summary>
    /// <param name="sql"></param>
    /// <param name="paras"></param>
    /// <returns></returns>
    public DataTable QueryDataTable(string sql, params SQLiteParameter[] paras )
    {
        DataTable dt = new DataTable();
        this.Open();
        using (SQLiteCommand sqlitecmd = new SQLiteCommand(sql, this._SQLiteConn))
        {
            if (paras != null)
            {
                sqlitecmd.Parameters.AddRange(paras);
            }
            using (SQLiteDataReader sdr = sqlitecmd.ExecuteReader(CommandBehavior.Default))
            {
                dt.Load(sdr);
            }
        }
        return dt;
    }

    /// <summary>
    /// 返回DbDataReader，读完记得关闭reader
    /// </summary>
    /// <param name="sql"></param>
    /// <param name="paras"></param>
    /// <returns></returns>
    public DbDataReader ExecuteReader(string sql, params SQLiteParameter[] paras)
    {
        this.Open();
        using (SQLiteCommand sqlitecmd = new SQLiteCommand(sql, this._SQLiteConn))
        {
            if (paras != null)
            {
                sqlitecmd.Parameters.AddRange(paras);
            }
            return sqlitecmd.ExecuteReader(CommandBehavior.Default);
        }
    }

    public static string createInsertSql(params SQLiteParameter[] paras) { 
        StringBuilder sb = new StringBuilder();
        StringBuilder sb2 = new StringBuilder();
        sb.Append("(");
        sb2.Append("(");
        for (int i=0;i< paras.Length; i++)
        {
            if(i!= paras.Length - 1)
            {
                sb.Append(paras[i].ParameterName + ",");
                sb2.Append("@"+paras[i].ParameterName + ",");
            }
            else
            {
                sb.Append(paras[i].ParameterName );
                sb2.Append("@" + paras[i].ParameterName);
            }
        }
        sb.Append(")");
        sb2.Append(")");

        return sb.ToString()+" values"+sb2.ToString();
    }

    public static string createUpdateSql(params SQLiteParameter[] paras)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < paras.Length; i++)
        {
            if (i != paras.Length - 1)
            {
                sb.Append(paras[i].ParameterName + "=@"+ paras[i].ParameterName+",");

            }
            else
            {
                sb.Append(paras[i].ParameterName + "=@" + paras[i].ParameterName );
            }
        }
        return sb.ToString() ;
    }




    /// <summary>
    /// 执行SQL语句，获取数据集
    /// </summary>
    /// <param name="command"></param>
    /// <param name="tablename"></param>
    /// <returns></returns>
    public DataSet QuerytDataSet(string command, string tablename = null)
    {
        DataSet ds = new DataSet();
        this.Open();
        using (SQLiteCommand sqlitecmd = new SQLiteCommand(command, this._SQLiteConn))
        {
            using (SQLiteDataAdapter sqliteadapter = new SQLiteDataAdapter(sqlitecmd))
            {
                if (string.Empty.Equals(tablename))
                {
                    sqliteadapter.Fill(ds);
                }
                else
                {
                    sqliteadapter.Fill(ds, tablename);
                }
            }
        }
        return ds;
    }



    /// <summary>
    /// 开始事物,要保证只能开启一个事务
    /// </summary>
    public void BeginTransaction()
    {
        this.Open();
        lock (lockTrans)
        {
            if (this._IsRunTrans)
            {
                return;
            }
            this._IsRunTrans = true;
            _SQLiteTrans = this._SQLiteConn.BeginTransaction();
        }
    }

    /// <summary>
    /// 提交事物
    /// </summary>
    public void Commit()
    {
        lock (lockTrans)
        {
            if (_IsRunTrans)
            {
                _SQLiteTrans.Commit();
                _IsRunTrans = false;
            }
        }
    }
    /// <summary>
    /// 事务回滚
    /// </summary>
    public void Rollback()
    {
        if (this._IsRunTrans)
        {
            this._SQLiteTrans.Rollback();
            this._IsRunTrans = false;
        }
    }


    /// <summary>
    /// 关闭数据库连接
    /// </summary>
    private void Close()
    {
        try
        {
            if (this._SQLiteConn != null && this._SQLiteConn.State != ConnectionState.Closed)
            {
                if (this._IsRunTrans)
                {
                    this.Commit();
                }
                _SQLiteConn.Close();
            }
        }
        catch (Exception )
        {

        }
    }



    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        this.Close();
    }

    public bool AutoCommit
    {
        get { return this._autocommit; }
        set { this._autocommit = value; }
    }

    #region 事件
    void _SQLiteConn_RollBack(object sender, EventArgs e)
    {
        this._IsRunTrans = false;
    }

    void _SQLiteConn_Commit(object sender, CommitEventArgs e)
    {
        this._IsRunTrans = false;
    }
    #endregion

}

/// <summary>
/// 这个是mir游戏的配置数据
/// 包括各种道具配置，怪物配置，服务器参数等配置数据
/// 
/// </summary>
public class MirConfigDB
{
    private static string connectionString = "Data Source=" + AppDomain.CurrentDomain.BaseDirectory + @"mir_config.db;";
    private static SQLiteHelper sqlHelper;

    private static int currSqe = -1;
    private static int nextSqe = -1;
    private static object locksqe = new object();

    private static long lastModifyTime = 0;

    private static bool UpdateConfigDB = false;//是否更新
    //初始化加载
    static MirConfigDB()
    {
        if (sqlHelper == null)
        {
            sqlHelper = new SQLiteHelper(connectionString);
            lastModifyTime = File.GetLastWriteTime(AppDomain.CurrentDomain.BaseDirectory + @"mir_config.db;").ToBinary();
        }
    }
    //数据库文件是否被修改过
    public static bool isModify2()
    {
        long lt = File.GetLastWriteTime(AppDomain.CurrentDomain.BaseDirectory + @"mir_config.db;").ToBinary(); 
        if(lt!= lastModifyTime)
        {
            lastModifyTime = lt;
            return true;
        }
        return false;
    }

    public static int Execute(string command, params SQLiteParameter[] parameter)
    {
        if (!UpdateConfigDB)
        {
            return 0;
        }
        return sqlHelper.Execute(command, parameter);
    }

    public static object ExecuteScalar(string command, params SQLiteParameter[] parmeter)
    {
        return sqlHelper.ExecuteScalar(command, parmeter);
    }

    public static DataTable QueryDataTable(string sql, SQLiteParameter[] paras = null)
    {
        return sqlHelper.QueryDataTable(sql, paras);
    }



    public static DataSet QuerytDataSet(string command, string tablename = null)
    {
        return sqlHelper.QuerytDataSet(command, tablename);
    }

    public static DbDataReader ExecuteReader(string command, params SQLiteParameter[] paras)
    {
        return sqlHelper.ExecuteReader(command, paras);
    }


    //关闭数据库连接
    public static void close()
    {
        sqlHelper.Dispose();
    }


    //测试插入数据试下
    public static void testinst()
    {
        int id = getNextSeq("", 10);
        string insql = @"insert into testdb(id,name) values(@id,@name)";
        sqlHelper.Execute(insql, new SQLiteParameter("id", id), new SQLiteParameter("id", "name"));
    }


    public static void BeginTransaction()
    {
        sqlHelper.BeginTransaction();
    }

    public static void Commit()
    {
        sqlHelper.Commit();
    }


    /// <summary>
    /// 获取下一个序列
    /// </summary>
    /// <param name="tbname"></param>
    /// <returns></returns>
    public static int getNextSeq(string tbname, int step)
    {
        lock (locksqe)
        {
            if (currSqe > 0 && currSqe < nextSqe)
            {
                return currSqe++;
            }
            string sql = @"select SEQUENCE_NEXT_HI_VALUE from t_sys_db_generator where SEQUENCE_NAME=@SEQUENCE_NAME";
            string sqlinst = @"insert into t_sys_db_generator(SEQUENCE_NAME,SEQUENCE_NEXT_HI_VALUE) values(@SEQUENCE_NAME,@SEQUENCE_NEXT_HI_VALUE)";
            string sqlup = @"update t_sys_db_generator set SEQUENCE_NEXT_HI_VALUE=SEQUENCE_NEXT_HI_VALUE+@SEQUENCE_NEXT_HI_VALUE where SEQUENCE_NAME=@SEQUENCE_NAME";
            SQLiteParameter spn = new SQLiteParameter("SEQUENCE_NAME", tbname);
            SQLiteParameter spv = new SQLiteParameter("SEQUENCE_NEXT_HI_VALUE", step);

            DbDataReader reader = sqlHelper.ExecuteReader(sql, spn);
            if (reader.Read())
            {
                currSqe = reader.GetInt32(reader.GetOrdinal("SEQUENCE_NEXT_HI_VALUE"));
                reader.Close();
                sqlHelper.Execute(sqlup, spv, spn);
                nextSqe = currSqe + step;
            }
            else
            {
                sqlHelper.Execute(sqlinst, spn, spv);
                currSqe = 1;
                nextSqe = step;
            }
        }

        return currSqe;
    }
}


/// <summary>
/// 这个是运行时数据，比如账号信息，角色信息，以及角色下的装备等信息
/// </summary>
public class MirRunDB
{
    private static string connectionString = "Data Source=" + AppDomain.CurrentDomain.BaseDirectory + @"mir_run.db;";
    private static SQLiteHelper sqlHelper;

    private static int currSqe = -1;
    private static int nextSqe = -1;
    private static object locksqe = new object();
    //初始化加载
    static MirRunDB()
    {
        if (sqlHelper == null)
        {
            sqlHelper = new SQLiteHelper(connectionString);
        }
    }

    public static int Execute(string command, params SQLiteParameter[] parameter )
    {
        return sqlHelper.Execute(command, parameter);
    }

    public static object ExecuteScalar(string command, SQLiteParameter[] parmeter = null)
    {
        return sqlHelper.ExecuteScalar(command, parmeter);
    }

    public static DataTable QueryDataTable(string sql, SQLiteParameter[] paras = null)
    {
        return sqlHelper.QueryDataTable(sql, paras);
    }



    public static DataSet QuerytDataSet(string command, string tablename = null)
    {
        return sqlHelper.QuerytDataSet(command, tablename);
    }

    public static DbDataReader ExecuteReader(string command, params SQLiteParameter[] paras)
    {
        return sqlHelper.ExecuteReader(command, paras);
    }


    //关闭数据库连接
    public static void close()
    {
        sqlHelper.Dispose();
    }


    //测试插入数据试下
    public static void testinst()
    {
        int id = getNextSeq("", 10);
        string insql = @"insert into testdb(id,name) values(@id,@name)";
        sqlHelper.Execute(insql, new SQLiteParameter("id", id), new SQLiteParameter("id", "name"));
    }


    public static void BeginTransaction()
    {
        sqlHelper.BeginTransaction();
    }

    public static void Commit()
    {
        sqlHelper.Commit();
    }


    /// <summary>
    /// 获取下一个序列
    /// </summary>
    /// <param name="tbname"></param>
    /// <returns></returns>
    public static int getNextSeq(string tbname, int step)
    {
        lock (locksqe)
        {
            if (currSqe > 0 && currSqe < nextSqe)
            {
                return currSqe++;
            }
            string sql = @"select SEQUENCE_NEXT_HI_VALUE from t_sys_db_generator where SEQUENCE_NAME=@SEQUENCE_NAME";
            string sqlinst = @"insert into t_sys_db_generator(SEQUENCE_NAME,SEQUENCE_NEXT_HI_VALUE) values(@SEQUENCE_NAME,@SEQUENCE_NEXT_HI_VALUE)";
            string sqlup = @"update t_sys_db_generator set SEQUENCE_NEXT_HI_VALUE=SEQUENCE_NEXT_HI_VALUE+@SEQUENCE_NEXT_HI_VALUE where SEQUENCE_NAME=@SEQUENCE_NAME";
            SQLiteParameter spn = new SQLiteParameter("SEQUENCE_NAME", tbname);
            SQLiteParameter spv = new SQLiteParameter("SEQUENCE_NEXT_HI_VALUE", step);

            DbDataReader reader = sqlHelper.ExecuteReader(sql, spn);
            if (reader.Read())
            {
                currSqe = reader.GetInt32(reader.GetOrdinal("SEQUENCE_NEXT_HI_VALUE"));
                reader.Close();
                sqlHelper.Execute(sqlup, spv, spn);
                nextSqe = currSqe + step;
            }
            else
            {
                sqlHelper.Execute(sqlinst, spn, spv);
                currSqe = 1;
                nextSqe = step;
            }
        }

        return currSqe;
    }
}


/// <summary>
/// 自动寻路中的点的定义
/// A星寻路法的点定义
/// </summary>
public class RoutePoint
{
    public int y;
    public int x;
    public int G;//从开始点到达当前点的距离，移动距离
    public int H;//当前点到终点的距离，这个是估算值
    public RoutePoint father;//上一个节点

    public RoutePoint()
    {

    }

    public RoutePoint(Point p)
    {
        this.x = p.X;
        this.y = p.Y;
    }

    public RoutePoint(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public RoutePoint(int x0, int y0, int G0, int H0, RoutePoint F)
    {
        x = x0;
        y = y0;
        G = G0;
        H = H0;
        father = F;
    }
   
    public override string ToString()
    {
        return "{" + x + "," + y + "}";
    }

    public Point getPoint()
    {
        return new Point(x,y);
    }
}

/// <summary>
/// 自动寻路的实现
/// </summary>
public class AutoRoute
{
    //数组用1表示可通过，0表示障碍物,3表示关闭
    byte[,] R;

    int w, h;//宽，高

    public AutoRoute(byte[,] R)
    {
        this.R = R;
        this.w = R.GetLength(0);
        this.h = R.GetLength(1);
    }

    //开启列表(这个开启列表其实还可以优化的)
    //List<RoutePoint> Open_List = new List<RoutePoint>();
    Dictionary<string,RoutePoint> Open_dic = new Dictionary<string,RoutePoint>();

    //从开启列表查找F值最小的节点(就是G+H最小的点)
    private RoutePoint GetMinFFromOpenList()
    {
        RoutePoint Pmin = null;
        //foreach (RoutePoint p in Open_List) if (Pmin == null || Pmin.G + Pmin.H > p.G + p.H) Pmin = p;
        foreach (RoutePoint p in Open_dic.Values)
        {
            if (Pmin == null || Pmin.G + Pmin.H > p.G + p.H) Pmin = p;
        }
        return Pmin;
    }
    //加入开启列表
    private void OpenAdd(RoutePoint p)
    {
        //Open_List.Add(p);
        string key = p.x + "," + p.y;
        if (!Open_dic.ContainsKey(key))
        {
            Open_dic.Add(key,p);
        }
    }
    //从开启列表删除
    private void OpenRemove(RoutePoint p)
    {
        //Open_List.Remove(p);
        string key = p.x + "," + p.y;
        Open_dic.Remove(key);
    }

    //从开启列表返回对应坐标的点
    private RoutePoint GetPointFromOpenList(int x, int y)
    {
        string key = x + "," + y;
        if (Open_dic.ContainsKey(key))
        {
            return Open_dic[key];
        }
        return null;
    }

    //计算某个点的G值
    private int GetG(RoutePoint p)
    {
        if (p.father == null) return 0;
        if (p.x == p.father.x || p.y == p.father.y) return p.father.G + 10;
        else return p.father.G + 14;
    }

    //计算某个点的H值
    private int GetH(RoutePoint p, RoutePoint pb)
    {
        return Math.Abs(p.x - pb.x)*10 + Math.Abs(p.y - pb.y)*10;
    }


    /// <summary>
    /// 寻找附近可到坐标
    /// 附近的8个点
    /// </summary>
    /// <param name="cp">当前坐标</param>
    /// <returns>附近可达坐标数组</returns>
    private List<RoutePoint> FindNearCell(RoutePoint p0)
    {
        List<RoutePoint> NearCellPoints = new List<RoutePoint>();
        for (int xt = p0.x - 1; xt <= p0.x + 1; xt++)
        {
            for (int yt = p0.y - 1; yt <= p0.y + 1; yt++)
            {
                //排除超过边界的点
                if(xt<0|| yt<0|| xt>=w|| yt >= h)
                {
                    continue;
                }
                //排除自己
                if(xt== p0.x && yt== p0.y)
                {
                    continue;
                }
                //排除不通的点
                if (R[xt,yt]!=1)
                {
                    continue;
                }
                NearCellPoints.Add(new RoutePoint(xt, yt));
            }
        }
        return NearCellPoints;
    }


    //入口，开始节点pa,目标节点pb
    public List<Point> FindeWay(Point _pa, Point _pb)
    {
        RoutePoint pa = new RoutePoint(_pa);
        RoutePoint pb = new RoutePoint(_pb);
        List<Point> myp = new List<Point>();
        OpenAdd(pa);
        bool isEnd = false;//是否结束
        while (Open_dic.Count> 0 && !isEnd)
        {
            RoutePoint p0 = GetMinFFromOpenList();
            if (p0 == null) return myp;
            OpenRemove(p0);
            R[p0.x, p0.y] = 3;//关闭掉
            foreach(RoutePoint childCell in FindNearCell(p0))
            {
                //如果已经到了终点，把终点的父亲指向当前点，并直接结束掉
                if(childCell.x== pb.x && childCell.y == pb.y)
                {
                    pb.father = p0;
                    isEnd = true;
                    break;
                }
                //在开启列表,则取出来，重新计算G值，如果从当前节点过去的G值更低，则更新那个点的G值和父亲
                RoutePoint pt = GetPointFromOpenList(childCell.x, childCell.y);
                if (pt!=null)
                {
                    int G_new = 0;
                    if (p0.x == pt.x || p0.y == pt.y)
                        G_new = p0.G + 10;
                    else
                        G_new = p0.G + 14;

                    if (G_new < pt.G)
                    {
                        pt.father = p0;
                        pt.G = G_new;
                    }
                }
                else
                {
                    //不在开启列表中,全新的点,则加入开启列表
                    childCell.father = p0;
                    childCell.G = GetG(childCell);
                    childCell.H = GetH(childCell, pb);
                    OpenAdd(childCell);
                }
            }
        }
        //终点在列表里，起点不在
        while (pb.father != null)
        {
            myp.Add(pb.getPoint());
            pb = pb.father;
        }
        return myp;
    }

}

/// <summary>
/// 获取文件的编码格式
/// </summary>
public class EncodingType
{
    /// <summary>
    /// 给定文件的路径，读取文件的二进制数据，判断文件的编码类型
    /// </summary>
    /// <param name="FILE_NAME">文件路径</param>
    /// <returns>文件的编码类型</returns>
    public static System.Text.Encoding GetType(string FILE_NAME)
    {
        FileStream fs = new FileStream(FILE_NAME, FileMode.Open, FileAccess.Read);
        Encoding r = GetType(fs);
        fs.Close();
        return r;
    }

    /// <summary>
    /// 通过给定的文件流，判断文件的编码类型
    /// </summary>
    /// <param name="fs">文件流</param>
    /// <returns>文件的编码类型</returns>
    public static System.Text.Encoding GetType(FileStream fs)
    {
        byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
        byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
        byte[] UTF8 = new byte[] { 0xEF, 0xBB, 0xBF }; //带BOM
        Encoding reVal = Encoding.Default;

        BinaryReader r = new BinaryReader(fs, System.Text.Encoding.Default);
        int i;
        int.TryParse(fs.Length.ToString(), out i);
        byte[] ss = r.ReadBytes(i);
        if (IsUTF8Bytes(ss) || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
        {
            reVal = Encoding.UTF8;
        }
        else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
        {
            reVal = Encoding.BigEndianUnicode;
        }
        else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
        {
            reVal = Encoding.Unicode;
        }
        r.Close();
        return reVal;
    }

    /// <summary>
    /// 判断是否是不带 BOM 的 UTF8 格式
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private static bool IsUTF8Bytes(byte[] data)
    {
        int charByteCounter = 1; //计算当前正分析的字符应还有的字节数
        byte curByte; //当前分析的字节.
        for (int i = 0; i < data.Length; i++)
        {
            curByte = data[i];
            if (charByteCounter == 1)
            {
                if (curByte >= 0x80)
                {
                    //判断当前
                    while (((curByte <<= 1) & 0x80) != 0)
                    {
                        charByteCounter++;
                    }
                    //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X 
                    if (charByteCounter == 1 || charByteCounter > 6)
                    {
                        return false;
                    }
                }
            }
            else
            {
                //若是UTF-8 此时第一位必须为1
                if ((curByte & 0xC0) != 0x80)
                {
                    return false;
                }
                charByteCounter--;
            }
        }
        if (charByteCounter > 1)
        {
            throw new Exception("非预期的byte格式");
        }
        return true;
    }

}



/// <summary>
/// 文件的key，map加载器
/// </summary>
public class FileKVMap
{
    private  Dictionary<string, string> d = new Dictionary<string, string>();
    private string filepath;
    private long LastAccessTime;


    public FileKVMap(string filepath)
    {
        this.filepath = filepath;
        load();
    }

    public string getValue(string key)
    {
        if (d.ContainsKey(key))
        {
            return d[key];
        }
        return null;
    }

    //装载数据
    private void load()
    {
        FileInfo fi = new FileInfo(filepath);
        if (fi.Exists)
        {
            LastAccessTime = fi.LastWriteTime.Ticks;
            string[] lines = File.ReadAllLines(filepath, EncodingType.GetType(filepath));
            foreach(string strLine in lines)
            {
                if (strLine == null || strLine.Trim().Length < 3 || strLine.StartsWith("#"))
                {
                    continue;
                }
                string[] sv = strLine.Trim().Split('=');
                if (sv == null || sv.Length != 2)
                {
                    continue;
                }
                if (d.ContainsKey(sv[0]))
                {
                    d[sv[0]] = sv[1];
                }
                else
                {
                    d.Add(sv[0], sv[1]);
                }
            }
        }
    }

    public void reLoad()
    {
        FileInfo fi = new FileInfo(filepath);
        if (fi.Exists && LastAccessTime!=fi.LastAccessTime.Ticks)
        {
            load();
        }
    }
}

/// <summary>
/// 语言包处理
/// </summary>
public class LanguageUtils
{
    private static FileKVMap fmap = new FileKVMap(@".\config\language.txt");

    /// <summary>
    /// 替换string.Format的方法
    /// </summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static String Format(String format, params object[] args)
    {
        string v = fmap.getValue(format);
        if (v == null)
        {
            if (args == null)
            {
                return format;
            }
            return string.Format(format, args);
        }
        if (args == null)
        {
            return v;
        }
        return string.Format(v, args);
    }
}


/// <summary>
/// CMD命令的转义处理
/// 服务器端进行的转义
/// </summary>
public class CMDTransform
{

    private static FileKVMap fmap = new FileKVMap(@".\Configs\cmdTransform.ini");

    //CMD命令的转义处理，针对客户端输入的中文命令
    public static string Transform(string cmd)
    {
        string v = fmap.getValue(cmd);
        if (v != null)
        {
            return v.ToUpper();
        }
        return cmd.ToUpper();
    }

    public static void reLoad()
    {
        fmap.reLoad();
    }
}

/// <summary>
/// 获取系统的版本
/// </summary>
public class OSystem
{
    private const string Windows2000 = "5.0";
    private const string WindowsXP = "5.1";
    private const string Windows2003 = "5.2";
    private const string Windows2008 = "6.0";
    private const string Windows7 = "6.1";
    private const string Windows8OrWindows81 = "6.2";
    private const string Windows10 = "10.0";

    private string OSystemName;
    private static string version;

    //-1:未知 1：XP 2:非XP
    private static int isxp = -1; 


    public void setOSystemName(string oSystemName)
    {
        this.OSystemName = oSystemName;
    }

    //获取系统版本号
    public static string GetOSystem()
    {
        if (version == null)
        {
            version = System.Environment.OSVersion.Version.Major + "." + System.Environment.OSVersion.Version.Minor;
        }
        return version;
    }

    //是否XP系统
    public static bool isXP()
    {
        if (isxp == -1) {
            string v = GetOSystem();
            if (v != null && v.IndexOf(".") != -1 && v.StartsWith("5."))
            {
                isxp = 1;
            }
            else
            {
                isxp = 2;
            }
        }
        if (isxp == 1) {
            return true;
        }
        return false;
    }


    public OSystem()
    {
        version = System.Environment.OSVersion.Version.Major + "." + System.Environment.OSVersion.Version.Minor;
        switch (version)
        {
            case Windows2000:
                setOSystemName("Windows2000");
                break;
            case WindowsXP:
                setOSystemName("WindowsXP");
                break;
            case Windows2003:
                setOSystemName("Windows2003");
                break;
            case Windows2008:
                setOSystemName("Windows2008");
                break;
            case Windows7:
                setOSystemName("Windows7");
                break;
            case Windows8OrWindows81:
                setOSystemName("Windows8.OrWindows8.1");
                break;
            case Windows10:
                setOSystemName("Windows10");
                break;
        }
    }

    /// <summary>
    /// 返回电脑的MAC地址
    /// </summary>
    /// <returns>The MAC address.</returns>
    public static string GetMacAddress()
    {
        try
        {
            string macAddresses = string.Empty;
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    macAddresses += nic.GetPhysicalAddress().ToString();
                    break;
                }
            }
            return macAddresses;
        }catch(Exception ex)
        {

        }
        return "";
    }

    /// <summary>
    /// "00-00-00-00-00-00";
    /// 判断是否物理地址
    /// </summary>
    /// <param name="mac"></param>
    /// <returns></returns>
    public static bool isMacAddress(string mac)
    {
        if (mac == null || mac.Length<10)
        {
            return false;
        }
        string nmac = mac.Replace("00","");
        
        if (nmac==null|| nmac.Length < 8)
        {
            return false;
        }
        return true;
    }

}

