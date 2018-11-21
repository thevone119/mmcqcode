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
    private static long TotalMilliseconds()
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
            sb.Append(v.ToString());
        }
        return MD5Encode(sb.ToString());
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
            if (dbMAXid.ContainsKey(o.GetType().Name))
            {
                dbMAXid[o.GetType().Name] = dbMAXid[o.GetType().Name] + 1;
                return dbMAXid[o.GetType().Name];
            }
            else
            {
                dbMAXid.Add(o.GetType().Name, 1);
                return 1;
            }
        }
    }

    //取当前最大ID
    public static ulong getObjCurrMaxId(Object o)
    {
        if (dbMAXid.ContainsKey(o.GetType().Name))
        {
            dbMAXid[o.GetType().Name] = dbMAXid[o.GetType().Name] + 1;
            return dbMAXid[o.GetType().Name];
        }
        return 0;
    }

    //更新对象状态
    public static void updateObjState(Object o, object id)
    {
        ulong cid = ulong.Parse(id.ToString());
        //更新ID
        if (dbMAXid.ContainsKey(o.GetType().Name))
        {
            ulong lastid = dbMAXid[o.GetType().Name];
            
            if (cid > lastid)
            {
                dbMAXid[o.GetType().Name] = lastid;
            }
        }
        else
        {
            dbMAXid[o.GetType().Name] = cid;
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
    //初始化加载
    static MirConfigDB()
    {
        if (sqlHelper == null)
        {
            sqlHelper = new SQLiteHelper(connectionString);
        }
    }



    public static int Execute(string command, params SQLiteParameter[] parameter)
    {
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

    public static DbDataReader ExecuteReader(string command, SQLiteParameter[] paras = null)
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
    List<RoutePoint> Open_List = new List<RoutePoint>();
  

    //从开启列表查找F值最小的节点(就是G+H最小的点)
    private RoutePoint GetMinFFromOpenList()
    {
        RoutePoint Pmin = null;
        foreach (RoutePoint p in Open_List) if (Pmin == null || Pmin.G + Pmin.H > p.G + p.H) Pmin = p;
        return Pmin;
    }

 

    //从开启列表返回对应坐标的点
    private RoutePoint GetPointFromOpenList(int x, int y)
    {
        foreach (RoutePoint p in Open_List) if (p.x == x && p.y == y) return p;
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
        Open_List.Add(pa);
        bool isEnd = false;//是否结束
        while (Open_List.Count> 0 && !isEnd)
        {
            RoutePoint p0 = GetMinFFromOpenList();
            if (p0 == null) return myp;
            Open_List.Remove(p0);
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
                    Open_List.Add(childCell);
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
/// 文件的key，map加载器
/// </summary>
public class FileKVMap
{
    private  Dictionary<string, string> d = new Dictionary<string, string>();


    public FileKVMap(string filepath)
    {

        FileInfo fi = new FileInfo(filepath);
        if (fi.Exists)
        {
            
            FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            using (StreamReader m_streamReader = new StreamReader(fs, System.Text.Encoding.UTF8))
            {
                string strLine = null;
                while ((strLine = m_streamReader.ReadLine()) != null)
                {
                    if(strLine==null|| strLine.Trim().Length < 3)
                    {
                        continue;
                    }
                    string[] sv= strLine.Trim().Split('=');
                    if(sv==null || sv.Length != 2)
                    {
                        continue;
                    }
                    if(d.ContainsKey(sv[0]))
                    {
                        d[sv[0]] = sv[1];
                    }
                    else
                    {
                        d.Add(sv[0],sv[1]);
                    }
                }
            }
            
        }
    }

    public string getValue(string key)
    {
        if (d.ContainsKey(key))
        {
            return d[key];
        }
        return null;
    }
}

/// <summary>
/// 语言包处理
/// </summary>
public class LanguageUtils
{
    private static FileKVMap fmap = new FileKVMap(@".\language.txt");

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