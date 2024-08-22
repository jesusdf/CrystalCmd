﻿using Majorsilence.CrystalCmd.NetFrameworkServer.Controllers;
using Majorsilence.CrystalCmd.Server.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSwag.AspNet.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Majorsilence.CrystalCmd.NetFrameworkServer
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private static HealthCheckTask _backgroundHealthTask;
        public static IServiceProvider ServiceProvider { get; private set; }

        protected void Application_Start()
        {
            string workingfolder = WorkingFolder.GetMajorsilenceTempFolder();
            if (System.IO.Directory.Exists(workingfolder))
            {
                System.IO.Directory.Delete(workingfolder, true);
            }
            System.IO.Directory.CreateDirectory(workingfolder);

            RouteTable.Routes.MapOwinPath("swagger", app =>
            {
                app.UseSwaggerUi(typeof(WebApiApplication).Assembly, settings =>
                {
                    settings.MiddlewareBasePath = "/swagger";
                    //settings.GeneratorSettings.DefaultUrlTemplate = "api/{controller}/{id}";  //this is the default one
                    settings.GeneratorSettings.DefaultUrlTemplate = "{controller}/{action}/{id}";
                });
            });

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
            var logger = ServiceProvider.GetService<ILogger>();

            string rptFilePath = Server.MapPath("~/bin/thereport.rpt");
            _backgroundHealthTask = new HealthCheckTask(logger, rptFilePath);
            _backgroundHealthTask.Start();
        }

        protected void Application_End(object sender, EventArgs e)
        {
            _backgroundHealthTask?.Stop();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => {
                configure.ClearProviders();
                configure.AddConsole();
            })
           .Configure<LoggerFilterOptions>(options => options.MinLevel = Microsoft.Extensions.Logging.LogLevel.Information);

            services.AddSingleton<Microsoft.Extensions.Logging.ILogger>(s => {
                return s.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>().CreateLogger("CrystalCmd");
            });
        }
    }


}
