using System;

namespace XStu.CacheMiddleware
{
    [AttributeUsage(AttributeTargets.Method)]
    public class XStuEnableCacheAttribute : Attribute
    {
        /// <summary>
        /// 过期时间(分)
        /// </summary>
        public double Expiry { get; set; } = 0;
        /// <summary>
        /// 返回文件的Content-Type
        /// </summary>
        public ContentType ContentType { get; set; } = ContentType.Text;

        /// <summary>
        /// 应用缓存范围
        /// </summary>
        public ApplyRange ApplyRange { get; set; } = ApplyRange.ALL;
    }


    public enum ApplyRange
    {
        Single,
        ALL
    }

    public enum ContentType
    {
        Json,
        Text
    }


}
