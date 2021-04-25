using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XStu.CacheMiddleware
{
    [AttributeUsage(AttributeTargets.Method)]
    public class XStuClearCacheAttribute : Attribute
    {
    }
}
