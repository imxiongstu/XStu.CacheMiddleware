using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XStu.CacheMiddleware
{
    public class XStuCacheAttribute : Attribute
    {
        /// <summary>
        /// 过期时间
        /// </summary>
        public double Expiry { get; set; }
        /// <summary>
        /// 返回文件的Content-Type
        /// </summary>
        public string ContentType { get; set; }
    }
}
