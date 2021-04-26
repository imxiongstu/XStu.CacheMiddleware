using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using StackExchange.Redis;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;
namespace XStu.CacheMiddleware
{
    /*==================================================================================
     * 
     *                        【XStu缓存中间件】基于Redis，高性能缓存AOP中间件
     *                         ★Author：ImXiongStu
     *                         ★支持一致性处理·高可用并发
     * 
     * 
     ==================================================================================*/
    /// <summary>
    /// XStu缓存中间件，构造函数需要填入Redis连接字符串
    /// </summary>
    public class XStuCacheMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDatabase _dataBase;
        private readonly ConnectionMultiplexer _connectionMultiplexer;
        private readonly string _redisConStr;

        public XStuCacheMiddleware(RequestDelegate next, string redisConStr)
        {
            _next = next;
            _redisConStr = redisConStr;
            _connectionMultiplexer = ConnectionMultiplexer.Connect(redisConStr + ",allowAdmin=true");
            _dataBase = _connectionMultiplexer.GetDatabase(10);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                //获取该请求的Action方法上面的缓存特性
                var endPointFeature = context.Features.Get<IEndpointFeature>();
                //获取启用缓存特性
                XStuEnableCacheAttribute xstuEnableCacheAttribute = endPointFeature?.Endpoint.Metadata.GetMetadata<XStuEnableCacheAttribute>();
                //获取清空缓存特性
                XStuClearCacheAttribute xStuClearCacheAttribute = endPointFeature?.Endpoint.Metadata.GetMetadata<XStuClearCacheAttribute>();
                //获取缓存Key
                string cacheKey = context.Request.Path + context.Request.QueryString;

                //是否含有清空缓存的属性，如果有清空缓存特性，则清空所有缓存
                if (xStuClearCacheAttribute != null)
                {
                    await _connectionMultiplexer.GetServer(_redisConStr).FlushDatabaseAsync(10);
                    await _next(context);
                    return;
                }

                //如果该请求的Action删除了缓存特性或此请求不是需要缓存Action请求，直接跳过本中间件,并删除此请求可能有的缓存（只删除单一缓存）
                if (xstuEnableCacheAttribute == null)
                {
                    _dataBase.KeyDelete(cacheKey);
                    await _next(context);
                    return;
                }

                #region TODO:=================================如果不存在该缓存键=====================================================
                if (!await _dataBase.KeyExistsAsync(cacheKey))
                {
                    //将返回流保存记录
                    var originResponse = context.Response.Body;
                    MemoryStream ms = new MemoryStream();
                    //将返回流变为可读写的内存流
                    context.Response.Body = ms;

                    //先去执行下一个委托中间件（各种其他的处理，包括业务逻辑，然后处理完来这里）
                    await _next(context);

                    //恢复内存流的读写位置
                    ms.Position = 0;
                    //将内存流的内容复制给原本的返回流
                    await ms.CopyToAsync(originResponse);
                    //恢复内存流的读写位置
                    ms.Position = 0;
                    //读取返回流信息
                    var cacheData = await new StreamReader(ms).ReadToEndAsync();

                    if (xstuEnableCacheAttribute.Expiry != 0)
                    {
                        _dataBase.StringSet(cacheKey, cacheData, TimeSpan.FromMinutes(xstuEnableCacheAttribute.Expiry));
                    }
                    else
                    {
                        _dataBase.StringSet(cacheKey, cacheData);
                    }
                    ms.Close();

                    //将上下文的返回流变为原本的返回流(这一步必须要放在最后，不然执行完这一步以后，后面的操作就不会继续了)
                    context.Response.Body = originResponse;
                    return;
                }
                #endregion

                #region TODO:=================================如果存在该缓存键=======================================================
                //设置返回Content-Type
                switch (xstuEnableCacheAttribute.ContentType)
                {
                    case ContentType.Json:
                        context.Response.ContentType = "application/json;charset=utf-8";
                        break;
                    case ContentType.Text:
                        context.Response.ContentType = "text/plain;charset=utf-8";
                        break;
                }
                //读取缓存
                var cache = _dataBase.StringGet(cacheKey);
                await context.Response.WriteAsync(cache);
                #endregion
            }
            catch
            {
            }
        }

    }
}
