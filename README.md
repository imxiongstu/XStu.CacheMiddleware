# XStu.CacheMiddleware
基于Redis的高性能.NetCore缓存中间件

##XStu缓存中间件##

**在Action上面添加[XStuCacheMiddleware]，即可缓存改方法上一次返回的内容**
**在Action上面添加[XStuClearCacheAttribute] 即可删除所有缓存**

务必开启Redis

XStuCacheMiddleware 可选属性ContentType、Expiry
