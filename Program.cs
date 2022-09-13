using Serilog;
using Splintermate.Delegation;
using Splintermate.Delegation.Sinks;
using Splintermate.Proxy;
using ILogger = Serilog.ILogger;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("delegation.json", false);

builder.Logging.ClearProviders();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.InMemory()
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.AddSerilog(logger);
builder.Services.AddSingleton<ILogger>(logger);

builder.Services.AddProxyRotator(builder.Configuration.GetSection("Proxy").Get<ProxyRotatorOptions>());

builder.Services.AddResponseCompression();

builder.Services.AddControllers();
builder.Services.AddSpaStaticFiles(config => { config.RootPath = "dist"; });

builder.Services.AddSingleton<CardDelegationService>();
builder.Services.AddSingleton<TokenDelegationService>();

var app = builder.Build();

app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

app.UseResponseCompression();
app.UseSpaStaticFiles();
app.UseRouting();
app.UseEndpoints(endpoints => { endpoints.MapDefaultControllerRoute(); });


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSpa(builder =>
{
    if (app.Environment.IsDevelopment())
    {
        builder.UseProxyToSpaDevelopmentServer("http://127.0.0.1:7217");
    }
});

app.Run();