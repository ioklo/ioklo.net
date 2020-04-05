using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Homepage.DbModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Homepage
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<MainDbContext>(optionBuilder => optionBuilder.UseSqlite("Data Source=db/main.db"));

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = FacebookDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddFacebook(options =>
                {
                    options.AppId = Configuration["Authentication:Facebook:AppId"];
                    options.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
                    // options.CallbackPath = Configuration["Authentication:Facebook:CallbackPath"];
                });
            
            var mvcBuilder = services.AddControllersWithViews();
#if DEBUG
            if (Env.IsDevelopment())
            {
                mvcBuilder.AddRazorRuntimeCompilation();
            }
#endif
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseHttpsRedirection();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                // app.UseHsts();

                var forwardedHeadersOptions = new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                };

                // NOTICE: ������ ��, �⺻ �ɼǿ� loopback�� �� �ִµ� docker�� �̿��ؼ� ���ٸ� localhost�� �ƴϹǷ� �˻縦 �ǳʶٰų� �߰��ϰų� �ؾ��Ѵ�
                //         ���� KnowNetworks, KnownProxies����Ʈ�� �Ѱ��� ������ Known���� �˻��ϰ�, �ƴ϶�� �׳� ����Ѵ�
                forwardedHeadersOptions.KnownNetworks.Clear();
                forwardedHeadersOptions.KnownProxies.Clear();

                app.UseForwardedHeaders(forwardedHeadersOptions);
            }
            
            app.UseStaticFiles();

            app.UseRouting();

            app.UseMiddleware<DebugMiddleware>();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/SignOut", async context =>
                    {
                        await context.SignOutAsync();
                        context.Response.Cookies.Delete("loggedIn");
                        await context.Response.WriteAsync("<script>window.opener.updateStatus(); self.opener = self;window.close();</script>");
                    });

                endpoints.MapControllerRoute("main", "{action=Index}/{id?}", new { controller = "Main" });
            });
        }
    }
}
