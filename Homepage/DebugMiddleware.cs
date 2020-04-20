using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Homepage
{
    public class DebugMiddleware 
    {
        private readonly RequestDelegate next;
        public DebugMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await next(context);
        }

    }
}
