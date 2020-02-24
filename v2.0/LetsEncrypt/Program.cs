using System;

using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.IO;

using LetsEncrypt.Helpers;
using LetsEncrypt.Services;

namespace LetsEncrypt
{
    class Program
    {
        public IConfiguration Configuration { get; }

        static void Main(string[] args) {
            // create service collection
            var services = new ServiceCollection();
            ConfigureServices(services);

            // create service provider
            var serviceProvider = services.BuildServiceProvider();

            // entry to run app
            serviceProvider.GetService<App>().Run();
        }

        public static void ConfigureServices(IServiceCollection services) {
            // build configuration
            IConfiguration Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .Build();

            
            // configure strongly typed settings objects
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            // Dependency Injection
            services.AddScoped<IKeyService, KeyService>();
            services.AddScoped<IJwsService, JwsService>();
            services.AddScoped<ILetsEncryptService, LetsEncryptService>();
            services.AddScoped<ITerminalService,TerminalService>();

            // add app
            services.AddTransient<App>();
        }
    }


}
