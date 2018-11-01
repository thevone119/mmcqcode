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

/// <summary>
/// 唯一值工具类
/// 构建一个多个服务器，多个线程并都不太可能重复的值(4位的随机ID+毫秒时间+递增数)
/// 固定部分（年月日时分秒+4位随机数+递增数（0-））
/// uint型为无符号32位整数，占4个字节，取值范围在0~4,294,967,295之间。
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
public class RandomUtils
{

    private static Random random = new Random();


    public static int Next()
    {
        return random.Next();
    }
    public static int Next(int maxValue)
    {
        return random.Next(maxValue);
    }
    public static int Next(int minValue, int maxValue)
    {
        return random.Next(minValue, maxValue);
    }
    public static double NextDouble()
    {
        return random.NextDouble();
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
        catch (Exception e)
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