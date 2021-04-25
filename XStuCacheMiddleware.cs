using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using StackExchange.Redis;
using System;
using System.IO;
using System.Threading.Tasks;

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

        public XStuCacheMiddleware(RequestDelegate next, string redisConStr)
        {
            _next = next;
            _dataBase = ConnectionMultiplexer.Connect(redisConStr).GetDatabase(10);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                //获取Action上面的缓存特性
                XStuCacheAttribute xstuCacheAttribute = context.Features.Get<IEndpointFeature>().Endpoint?.Metadata?.GetMetadata<XStuCacheAttribute>();
                //如果改Action未标识缓存特性，直接跳过本中间件
                if (xstuCacheAttribute == null) { await _next(context); return; }

                string cacheKey = context.Request.Path + context.Request.QueryString;

                #region 不存在缓存
                if (!await _dataBase.KeyExistsAsync(cacheKey))
                {
                    //TODO:=======================如果不存在该缓存键============================================================
                    //将返回流保存记录
                    var originResponse = context.Response.Body;
                    MemoryStream ms = new MemoryStream();
                    //将返回流变为可读写的内存流
                    context.Response.Body = ms;

                    //执行其他委托中间件
                    await _next(context);

                    //恢复内存流的读写位置
                    ms.Position = 0;
                    //将内存流的内容复制给原本的返回流
                    await ms.CopyToAsync(originResponse);
                    //恢复内存流的读写位置
                    ms.Position = 0;
                    //读取返回流信息
                    var cacheData = await new StreamReader(ms).ReadToEndAsync();

                    if (xstuCacheAttribute.Expiry != 0)
                    {
                        _dataBase.StringSet(cacheKey, cacheData, TimeSpan.FromMinutes(xstuCacheAttribute.Expiry));
                    }
                    else
                    {
                        _dataBase.StringSet(cacheKey, cacheData);
                    }
                    ms.Close();

                    //将上下文的返回流变为原本的返回流(这一步必须要放在最后，不然执行完这一步以后，后面的操作就不会继续了)
                    context.Response.Body = originResponse;
                    //将返回流的位置归零
                    context.Response.Body.Position = 0;
                    return;
                }
                #endregion

                #region 存在缓存
                //TODO:=================================如果存在该缓存键=======================================================
                //读取缓存
                var cache = _dataBase.StringGet(cacheKey);
                //设置返回Content-Type
                switch (xstuCacheAttribute.ContentType)
                {
                    case ContentType.Json:
                        context.Response.ContentType = "application/json;charset=utf-8";
                        break;
                    case ContentType.Text:
                        context.Response.ContentType = "text/plain;charset=utf-8";
                        break;
                }
                await context.Response.WriteAsync(cache);
                #endregion
            }
            catch
            {
            }
        }
    }



}
