using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using Splintermate.Proxy;
using ILogger = Serilog.ILogger;

namespace Splintermate.Delegation
{
    class Program
    {
        public static IConfiguration Configuration;

        static int Main(string[] args)
        {
            try
            {
                MainAsync(args).Wait();
                Console.WriteLine("Press <any key> to exit");
                Console.ReadKey();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Fatal exception occured: {Message}", ex.Message);
                Console.WriteLine("Press <any key> to exit");
                Console.ReadKey();
                return 1;
            }
        }

        private static async Task MainAsync(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile("delegation.json", false)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(Serilog.Events.LogEventLevel.Debug)
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .CreateLogger();

            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, Configuration);


            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                Log.Information("Starting service");
                var mode = Configuration.GetValue<string>("Delegation:Mode");

                if (mode == "cards")
                    await serviceProvider.GetService<CardDelegationService>()!.Run();
                else if (mode == "tokens")
                    await serviceProvider.GetService<TokenDelegationService>()!.Run();
                else
                    Log.Warning("Invalid mode {Mode} in delegation.json, allowed values [cards,tokens]", mode);

                Log.Information("Ending service");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error running service");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ILogger>(Log.Logger);
            services.AddSingleton(configuration);

            /* Proxy settings */
            var proxyConfig = Configuration.GetSection("Proxy");

            services.AddProxyRotator(options =>
            {
                options.Enabled = proxyConfig.GetValue<bool>("Enabled");
                options.Url = proxyConfig.GetValue<string>("Url");
                options.Username = proxyConfig.GetValue<string>("Username");
                options.Password = proxyConfig.GetValue<string>("Password");

                options.BypassProxyOnLocal = false;
            });

            services.AddTransient<CardDelegationService>();
            services.AddTransient<TokenDelegationService>();
        }
    }
}