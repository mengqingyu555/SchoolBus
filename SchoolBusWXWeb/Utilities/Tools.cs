﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qiniu.IO;
using Qiniu.IO.Model;
using Qiniu.Util;
using SchoolBusWXWeb.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SchoolBusWXWeb.Utilities
{
    public static class Tools
    {
        private static ILogger _toollogger;
        private static SiteConfig _settings;

        private static readonly string[] ImageExtensions = { ".jpg", ".png", ".gif", ".jpeg", ".bmp" };

        public static IApplicationBuilder SetUtilsProviderConfiguration(this IApplicationBuilder serviceProvider,
            IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _settings = configuration.GetSection("SiteConfig").Get<SiteConfig>();
            _toollogger = loggerFactory.CreateLogger("Tools");
            return serviceProvider;
        }

        /// <summary>
        ///     上传图片到七牛
        /// </summary>
        /// <param name="imgInfo"></param>
        /// <returns></returns>
        public static async Task<bool> UploadStream(ImgInfo imgInfo)
        {
            var res = false;
            try
            {
                if (imgInfo != null && !string.IsNullOrEmpty(imgInfo.FileName) && imgInfo.ImageId > 0)
                {
                    var fileName = imgInfo.FileName;
                    var extensionName = fileName.Substring(fileName.LastIndexOf(".", StringComparison.Ordinal));
                    if (ImageExtensions.Contains(extensionName.ToLower()))
                    {
                        // 上传策略，参见
                        // https://developer.qiniu.com/kodo/manual/put-policy
                        var mac = new Mac(_settings.AccessKey, _settings.SecretKey);
                        // 如果需要设置为"覆盖"上传(如果云端已有同名文件则覆盖)，请使用 SCOPE = "BUCKET:KEY" putPolicy.Scope = bucket + ":" + saveKey;
                        var putPolicy = new PutPolicy
                        {
                            Scope = _settings.Bucketfirst + ":" + imgInfo.SaveKey
                        };
                        putPolicy.SetExpires(3600); // 上传策略有效期(对应于生成的凭证的有效期)

                        // putPolicy.DeleteAfterDays = 1;  // 上传到云端多少天后自动删除该文件，如果不设置（即保持默认默认）则不删除

                        // 生成上传凭证，参见
                        // https://developer.qiniu.com/kodo/manual/upload-token
                        var jstr = putPolicy.ToJsonString();
                        var token = Auth.CreateUploadToken(mac, jstr);
                        var fu = new FormUploader();
                        var result = await fu.UploadStreamAsync(imgInfo.FileStream, imgInfo.SaveKey, token);
                        if (result.Code == 200) res = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _toollogger.LogError("上传图片到云存储异常:异常信息:" + ex);
            }

            return res;
        }

        /// <summary>
        ///     获取七牛Token
        /// </summary>
        /// <returns></returns>
        public static Token GetUploadToken()
        {
            var mac = new Mac(_settings.AccessKey, _settings.SecretKey);
            var auth = new Auth(mac);
            var putPolicy = new PutPolicy
            {
                Scope = _settings.Bucketfirst
            };
            // 上传策略有效期(对应于生成的凭证的有效期)
            putPolicy.SetExpires(3600);
            // putPolicy.DeleteAfterDays = 1; // 上传到云端多少天后自动删除该文件，如果不设置（即保持默认默认）则不删除 
            return new Token
            {
                uptoken = auth.CreateUploadToken(putPolicy.ToJsonString())
            };
        }


        /// <summary>
        ///     生成验证码图片
        /// </summary>
        public static byte[] CreateCheckCodeImage()
        {
            var bb = default(byte[]);
            try
            {
                var checkCode = VerficationCodeSrc(5);

                if (!string.IsNullOrEmpty(checkCode.Trim()))
                {
                    var image = new Bitmap(90, 28);
                    var g = Graphics.FromImage(image);
                    try
                    {
                        //生成随机生成器
                        var random = new Random();
                        //清空图片背景色
                        g.Clear(Color.White);
                        //画图片的背景噪音线
                        for (var i = 0; i < 2; i++)
                        {
                            var x1 = random.Next(image.Width);
                            var x2 = random.Next(image.Width);
                            var y1 = random.Next(image.Height);
                            var y2 = random.Next(image.Height);
                            g.DrawLine(new Pen(Color.Black), x1, y1, x2, y2);
                        }

                        var font = new Font("Arial", 16, FontStyle.Bold);

                        var brush = new LinearGradientBrush(new Rectangle(0, 0, image.Width, image.Height), Color.Blue,
                            Color.DarkRed, 1.2f, true);

                        g.DrawString(checkCode, font, brush, 2, 2);
                        //画图片的前景噪音点
                        for (var i = 0; i < 100; i++)
                        {
                            var x = random.Next(image.Width);
                            var y = random.Next(image.Height);
                            image.SetPixel(x, y, Color.FromArgb(random.Next()));
                        }

                        //画图片的边框线
                        g.DrawRectangle(new Pen(Color.Silver), 0, 0, image.Width - 1, image.Height - 1);
                        using (var stream = new MemoryStream())
                        {
                            image.Save(stream, ImageFormat.Gif);
                            bb = stream.GetBuffer();
                        }
                    }
                    finally
                    {
                        g.Dispose();
                        image.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _toollogger.LogError("使用System.Drawing.Common生成验证码失败: 异常信息:" + ex);
            }

            return bb;
        }

        /// <summary>
        ///     生成验证码字符串
        /// </summary>
        /// <param name="numberOfChars"></param>
        /// <returns></returns>
        private static string VerficationCodeSrc(int numberOfChars)
        {
            if (numberOfChars > 36)
                throw new InvalidOperationException("Random Word Charecters can not be greater than 36.");
            var columns = new char[36];
            //字母
            for (var charPos = 97; charPos < 97 + 26; charPos++)
                columns[charPos - 97] = (char)charPos;
            //数字
            for (var intPos = 48; intPos <= 57; intPos++)
                columns[intPos - 22] = (char)intPos;

            var randomBuilder = new StringBuilder();
            var randomSeed = new Random();
            for (var incr = 0; incr < numberOfChars; incr++)
                randomBuilder.Append(columns[randomSeed.Next(36)].ToString());

            return randomBuilder.ToString();
        }

        /// <summary>
        ///     上传图片到七牛
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="fileStream"></param>
        /// <param name="saveKey"></param>
        /// <returns></returns>
        public static async Task<bool> UploadStream(string bucket, Stream fileStream, string saveKey)
        {
            var res = false;
            try
            {
                var mac = new Mac(_settings.AccessKey, _settings.SecretKey);
                var putPolicy = new PutPolicy
                {
                    Scope = bucket + ":" + saveKey
                };
                putPolicy.SetExpires(3600); // 上传策略有效期(对应于生成的凭证的有效期)
                var jstr = putPolicy.ToJsonString();
                var token = Auth.CreateUploadToken(mac, jstr);
                var fu = new FormUploader();
                var result = await fu.UploadStreamAsync(fileStream, saveKey, token);
                if (result.Code == 200) res = true;
            }
            catch (Exception ex)
            {
                _toollogger.LogError("上传图片到云存储异常:异常信息:" + ex);
            }

            return res;
        }


        /// <summary>
        ///     批量下载图片上传到七牛 参考c#并发编程实例 2.6节
        /// </summary>
        /// <param name="srclist"></param>
        /// <returns></returns>
        public static async Task<bool> UploadImgAsync(List<SrcModel> srclist)
        {
            var b = false;
            try
            {
                var processingTasks = srclist.Select(async t =>
                {
                    // 对结果得处理
                    var stream = await HttpClientHelper.HttpGetStreamAsync(t.Oldsrc);
                    if (stream != null)
                    {
                        b = await UploadStream(_settings.Bucketfirst, stream, t.Newsrc);
                        if (!b) _toollogger.LogError("[QN]上传图片:" + t.Newsrc + " 失败.原图地址:" + t.Oldsrc);
                    }
                    else
                    {
                        _toollogger.LogError("[QN]" + t.Oldsrc + ",获取该图片流失败");
                    }
                }).ToArray();
                await Task.WhenAll(processingTasks);
            }
            catch (Exception ex)
            {
                _toollogger.LogError("批量下载图片上传到QINIU异常:" + ex);
            }

            return b;
        }

        /// <summary>
        ///     SHA1加密方法
        /// </summary>
        /// <param name="str">需要加密的字符串</param>
        /// <returns></returns>
        public static string GetSha1Str(string str)
        {
            var _byte = Encoding.Default.GetBytes(str);
            HashAlgorithm ha = new SHA1CryptoServiceProvider();
            _byte = ha.ComputeHash(_byte);
            var sha1Str = new StringBuilder();
            foreach (var b in _byte) sha1Str.AppendFormat("{0:x2}", b);
            return sha1Str.ToString();
        }

        public static async Task<bool> Down(string oldsrc, string newsrc)
        {
            var stream = await HttpClientHelper.HttpGetStreamAsync(oldsrc);
            var b = await UploadStream(_settings.Bucketfirst, stream, newsrc);
            return b;
        }

        public static async Task<string> DownloadAllAsync(IEnumerable<string> urls)
        {
            var httpClient = new HttpClient();
            // 定义每一个url 的使用方法。
            var downloads = urls.Select(url => httpClient.GetStringAsync(url));
            // 注意，到这里，序列还没有求值，所以所有任务都还没真正启动。
            // 下面，所有的URL 下载同步开始。
            var downloadTasks = downloads.ToArray();
            // 到这里，所有的任务已经开始执行了。
            // 用异步方式等待所有下载完成。
            var htmlPages = await Task.WhenAll(downloadTasks);
            return string.Concat(htmlPages);
        }

        public static bool IsUrl(string str)
        {
            try
            {
                const string url = @"^http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";
                return Regex.IsMatch(str, url);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static SiteConfig GetInitConst()
        {
            return AppSetting.GetConfig();
        }

        /// <summary>
        ///  替换Html标签 最快 https://www.cnblogs.com/jaxu/p/3682042.html
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string StripTagsCharArray(string source)
        {
            var array = new char[source.Length];
            var arrayIndex = 0;
            var inside = false;

            foreach (var @let in source)
            {
                switch (@let)
                {
                    case '<':
                        inside = true;
                        continue;
                    case '>':
                        inside = false;
                        continue;
                }

                if (inside) continue;
                array[arrayIndex] = @let;
                arrayIndex++;
            }

            return new string(array, 0, arrayIndex);
        }

        /// <summary>
        ///     Ajax重定向 ajax请求不能用Redirect,使用下面方法.如果想使用,表单提交前检查用onsubmit="return checkpm();"
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <param name="url"></param>
        public static void AjaxRedireUrl(IHttpContextAccessor httpContextAccessor, string url)
        {
            var isAjax = httpContextAccessor.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (!isAjax) return;
            httpContextAccessor.HttpContext.Response.Headers.Add("Redirect", "true");
            httpContextAccessor.HttpContext.Response.Headers.Add("RedirectUrl", url);
        }

        
        #region List和datatable相互转换

        /// <summary>
        ///     Convert a List{T} to a DataTable.
        /// </summary>
        public static DataTable ToDataTable<T>(List<T> items)
        {
            var tb = new DataTable(typeof(T).Name);

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var t = GetCoreType(prop.PropertyType);
                tb.Columns.Add(prop.Name.Replace("_", "__"), t); // 支持列名带下划线
            }

            foreach (var item in items)
            {
                var values = new object[props.Length];

                for (var i = 0; i < props.Length; i++) values[i] = props[i].GetValue(item, null);

                tb.Rows.Add(values);
            }

            return tb;
        }

        /// <summary>
        ///     Determine of specified type is nullable
        /// </summary>
        public static bool IsNullable(Type t)
        {
            return !t.IsValueType || t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        ///     Return underlying type if type is Nullable otherwise return the type
        /// </summary>
        public static Type GetCoreType(Type t)
        {
            if (t != null && IsNullable(t))
            {
                if (!t.IsValueType)
                    return t;
                return Nullable.GetUnderlyingType(t);
            }

            return t;
        }

        //*************************************************
        /// <summary>
        ///     List转Datatable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
        public static IList<T> ConvertTo<T>(DataTable table)
        {
            if (table == null) return null;

            var rows = new List<DataRow>();

            foreach (DataRow row in table.Rows) rows.Add(row);

            return ConvertTo<T>(rows);
        }

        public static IList<T> ConvertTo<T>(IList<DataRow> rows)
        {
            IList<T> list = null;

            if (rows != null)
            {
                list = rows.Select(CreateItem<T>).ToList();
            }

            return list;
        }

        public static T CreateItem<T>(DataRow row)
        {
            var obj = default(T);
            if (row == null) return obj;
            obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in row.Table.Columns)
            {
                var prop = obj.GetType().GetProperty(column.ColumnName);
                try
                {
                    var value = row[column.ColumnName];
                    prop.SetValue(obj, value, null);
                }
                catch
                {
                    //You can log something here     
                    //throw;    
                }
            }

            return obj;
        }

        #endregion
    }
}
