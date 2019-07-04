﻿using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchoolBusWXWeb.Business;
using SchoolBusWXWeb.Models;
using SchoolBusWXWeb.Repository;
using SchoolBusWXWeb.StartupTask;
using SchoolBusWXWeb.Utilities;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP;
using Senparc.Weixin.RegisterServices;
// ReSharper disable CommentTypo
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable StringLiteralTypo

namespace SchoolBusWXWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.Configure<SiteConfig>(Configuration.GetSection("SiteConfig"));
            services.AddScoped<ISchoolBusBusines, SchoolBusBusines>();
            services.AddScoped<ISchoolBusRepository, SchoolBusRepository>();

            services.AddStartupTask<MqttStartupFilter>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddMemoryCache();                           // 使用本地缓存必须添加
            services.AddSession();                               // 使用Session
            #region 健康检擦服务
            services.AddHealthChecks().AddNpgSql(Configuration["SiteConfig:DefaultConnection"], failureStatus: HealthStatus.Degraded);
            services.AddHealthChecksUI();
            #endregion
            services.AddSenparcGlobalServices(Configuration)     // Senparc.CO2NET 全局注册
                    .AddSenparcWeixinServices(Configuration);    // Senparc.Weixin 注册
            services.AddMvc(options=>
            {
                // 会自动忽略不需要做CSRF验证的请求类型，例如HttpGet请求 Post请求就不需要添加[ValidateAntiForgeryToken]
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<SenparcSetting> senparcSetting, IOptions<SenparcWeixinSetting> senparcWeixinSetting)
        {
            app.SetUtilsProviderConfiguration(Configuration, loggerFactory); // 静态工具类
            app.UseEnableRequestRewind();  // 微信sdk使用
            app.UseSession();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }
            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            #region 健康检查中间件 https://localhost:5001/healthchecks-ui
            app.UseHealthChecks("/healthz", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            app.UseHealthChecksUI();
            #endregion

            #region 微信相关
            RegisterService.Start(env, senparcSetting.Value)
                .UseSenparcGlobal()               // 启动 CO2NET 全局注册，必须！
                .RegisterTraceLog(ConfigTraceLog) // 微信配置开始 注册日志(按需，建议) 配置TraceLog
                .UseSenparcWeixin(senparcWeixinSetting.Value, senparcSetting.Value)
                .RegisterMpAccount(senparcWeixinSetting.Value, "【刘哲测试】公众号"); // 注册公众号(可注册多个)
            #endregion

            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller=SchoolBus}/{action=GoAddress}/{id?}");
            });
        }
        /// <summary>
        /// 配置微信跟踪日志
        /// </summary>
        private static void ConfigTraceLog()
        {
            //这里设为Debug状态时，/App_Data/WeixinTraceLog/目录下会生成日志文件记录所有的API请求日志，正式发布版本建议关闭

            //如果全局的IsDebug（Senparc.CO2NET.Config.IsDebug）为false，此处可以单独设置true，否则自动为true
            SenparcTrace.SendCustomLog("系统日志", "系统启动");//只在Senparc.Weixin.Config.IsDebug = true的情况下生效

            //全局自定义日志记录回调
            SenparcTrace.OnLogFunc = () =>
            {
                //加入每次触发Log后需要执行的代码
            };

            // 当发生基于WeixinException的异常时触发
            WeixinTrace.OnWeixinExceptionFunc = ex =>
            {
                //加入每次触发WeixinExceptionLog后需要执行的代码

                //发送模板消息给管理员                             -- DPBMARK Redis
                //var eventService = new CommonService.EventService();
                //eventService.ConfigOnWeixinExceptionFunc(ex);      // DPBMARK_END
            };
        }
    }
}
