using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.IO;

namespace XStu.CacheMiddleware
{
    /*==================================================================================
     * 
     *                        【XStu缓存中间件】基于Redis高性能缓存
     *                                Author：ImXiongStu
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
                if (!await _dataBase.KeyExistsAsync(cacheKey))
                {
                    //TODO:=======================如果不存在该缓存键============================
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
                    //将上下文的返回流变为原本的返回流
                    context.Response.Body = originResponse;
                    //将返回流的位置归零
                    context.Response.Body.Position = 0;
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
                    return;
                }

                //TODO:=================================如果存在该缓存键=============================
                var cache = _dataBase.StringGet(cacheKey);
                context.Response.ContentType = xstuCacheAttribute.ContentType;
                await context.Response.WriteAsync(cache);
            }
            catch
            {

            }
        }
    }



}
