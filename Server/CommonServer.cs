using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;


public class HttpClient2
{
    public static string HttpUploadFile(string url, NameValueCollection files, NameValueCollection data)
    {
        return HttpUploadFile(url, files, data, Encoding.UTF8);
    }
    /// <summary>
    /// HttpUploadFile
    /// </summary>
    /// <param name="url"></param>
    /// <param name="files"></param>
    /// <param name="data"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
    public static string HttpUploadFile(string url, NameValueCollection files, NameValueCollection data, Encoding encoding)
    {
        string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
        byte[] boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
        byte[] endbytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

        //1.HttpWebRequest
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.ContentType = "multipart/form-data; boundary=" + boundary;
        request.Method = "POST";
        request.Timeout = 1000 * 30;
        request.KeepAlive = true;
        request.Credentials = CredentialCache.DefaultCredentials;
        //对发送的数据不使用缓存 
        //request.AllowWriteStreamBuffering = false;

        using (Stream stream = request.GetRequestStream())
        {
            //1.1 key/value
            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            if (data != null)
            {
                foreach (string key in data.Keys)
                {
                    stream.Write(boundarybytes, 0, boundarybytes.Length);
                    string formitem = string.Format(formdataTemplate, key, data[key]);
                    byte[] formitembytes = encoding.GetBytes(formitem);
                    stream.Write(formitembytes, 0, formitembytes.Length);

                    //form end
                    //stream.Write(endbytes, 0, endbytes.Length);
                }
            }

            //1.2 file
            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
            byte[] buffer = new byte[1024 * 1];
            int bytesRead = 0;
            foreach (string key in files.Keys)
            {
                {
                    stream.Write(boundarybytes, 0, boundarybytes.Length);
                    string header = string.Format(headerTemplate, key, Path.GetFileName(files[key]));
                    byte[] headerbytes = encoding.GetBytes(header);
                    stream.Write(headerbytes, 0, headerbytes.Length);
                    using (FileStream fileStream = new FileStream(files[key], FileMode.Open, FileAccess.Read))
                    {
                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            stream.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }
            //1.3 form end
            stream.Write(endbytes, 0, endbytes.Length);

            //2.WebResponse
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (StreamReader retstream = new StreamReader(response.GetResponseStream()))
            {
                return retstream.ReadToEnd();
            }
        }
    }

    /// <summary>
    /// post请求
    /// </summary>
    /// <param name="url"></param>
    /// <param name="postData">post数据</param>
    /// <returns></returns>
    public static string PostResponse(string url, Dictionary<string, string> postData)
    {
        if (url.StartsWith("https"))
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
       
        return null;
    }



    //postDataStr是要传递的参数,格式"roleId=1&uid=2"
    //contentType GBK,UTF-8,ASCII
    public static string PostHttp(string url, string postDataStr, string contentType)
    {
        string retString = null;
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        //request.CookieContainer = new CookieContainer();
        request.Timeout = 1000 * 15;
        //request.Accept = "Accept:text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        //request.Headers["Accept-Language"] = "zh-CN,zh;q=0.";
        //request.Headers["Accept-Charset"] = "GBK,utf-8;q=0.7,*;q=0.3";
        //request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36";
        //request.KeepAlive = true;
        //上面的http头看情况而定，但是下面俩必须加  
        request.ContentType = "application/x-www-form-urlencoded";
        request.Method = "POST";
        Encoding encoding = Encoding.Default;//根据网站的编码自定义  
        if ("UTF-8".Equals(contentType, StringComparison.CurrentCultureIgnoreCase))
        {
            encoding = Encoding.UTF8;
        }
        if ("GBK".Equals(contentType, StringComparison.CurrentCultureIgnoreCase))
        {
            encoding = Encoding.Default;
        }
        if ("ASCII".Equals(contentType, StringComparison.CurrentCultureIgnoreCase))
        {
            encoding = Encoding.ASCII;
        }
        byte[] postData = encoding.GetBytes(postDataStr);//postDataStr即为发送的数据，格式还是和上次说的一样  
        request.ContentLength = postData.Length;
        using (Stream requestStream = request.GetRequestStream())
        {
            requestStream.Write(postData, 0, postData.Length);
        }

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), encoding))
        {
            retString = streamReader.ReadToEnd();
        }
        response.Close();


        return retString;
    }

    /**
     * 下载文件
     * */
    public static bool HttpDownLoad(string url, string postDataStr, string contentType, string filename)
    {
        try
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            //request.CookieContainer = new CookieContainer(); 
            //request.Accept = "Accept:text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            //request.Headers["Accept-Language"] = "zh-CN,zh;q=0.";
            //request.Headers["Accept-Charset"] = "GBK,utf-8;q=0.7,*;q=0.3";
            //request.UserAgent = "User-Agent:Mozilla/5.0 (Windows NT 5.1) AppleWebKit/535.1 (KHTML, like Gecko) Chrome/14.0.835.202 Safari/535.1";
            //request.AllowWriteStreamBuffering = false;
            request.Timeout = 1000 * 30;
            request.KeepAlive = true;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (Stream responseStream = response.GetResponseStream())
            {
                FileStream writer = new FileStream(filename, FileMode.Create, FileAccess.Write);
                byte[] buff = new byte[1024];
                int c = 0; //实际读取的字节数 
                long readlong = 0;
                while ((c = responseStream.Read(buff, 0, buff.Length)) > 0)
                {
                    writer.Write(buff, 0, c);
                    writer.Flush();
                    readlong += c;
                }
                writer.Close();
                responseStream.Close();
                if (response.ContentLength > 0)
                {
                    if (response.ContentLength == readlong)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (readlong > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        catch (Exception)
        {
            //LogHelper.Error(ex);
        }
        return false;
    }

}