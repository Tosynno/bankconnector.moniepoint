using NLog;
using NLog.Web;
using Polly;
using zone.bankconnector.moniepoint.Encryption;
using zone.bankconnector.moniepoint.Https;
using zone.bankconnector.moniepoint.Https.Handlers;
using zone.bankconnector.moniepoint.Interfaces;
using zone.bankconnector.moniepoint.Services;
using zone.bankconnector.moniepoint.Utilities;
using Microsoft.Extensions.Options;

var logger = LogManager.Setup()
    .LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

try
{
    logger.Info("=== Zone BankConnector Moniepoint starting ===");

    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();
    builder.Services.Configure<TeamAptOptions>(
        builder.Configuration.GetSection(TeamAptOptions.SectionName));

    builder.Services.AddSingleton<EncryptionServiceFactory>();
    builder.Services.AddSingleton<IEncryptionServiceFactory>(sp =>
        sp.GetRequiredService<EncryptionServiceFactory>());

    builder.Services.AddTransient<LoggingHandler>();

    builder.Services
        .AddHttpClient<IHttpClientService, HttpClientService>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<TeamAptOptions>>().Value;

            client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
            client.Timeout     = TimeSpan.FromSeconds(
                builder.Configuration.GetValue<int?>("HttpClient:TimeoutSeconds") ?? 40);

            client.DefaultRequestHeaders.Add("x-api-key", opts.ApiKey);
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(5))
        .AddHttpMessageHandler<LoggingHandler>()
        .AddStandardResilienceHandler(o =>
        {
            o.AttemptTimeout.Timeout           = TimeSpan.FromSeconds(30);
            o.CircuitBreaker.SamplingDuration  = TimeSpan.FromSeconds(60);
            o.CircuitBreaker.FailureRatio      = 0.5;
            o.CircuitBreaker.MinimumThroughput = 10;
            o.CircuitBreaker.BreakDuration     = TimeSpan.FromSeconds(60);
            o.Retry.MaxRetryAttempts           = 3;
            o.Retry.Delay                      = TimeSpan.FromSeconds(1);
            o.Retry.BackoffType                = DelayBackoffType.Exponential;
            o.Retry.UseJitter                  = true;
        });

    builder.Services.AddScoped<IMonieTransferService, MonieTransferService>();
    builder.Services.AddScoped<IConnector, MonieBankConnector>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title       = "Zone BankConnector — Moniepoint / TeamApt MEKS Switch",
            Version     = "v1",
            Description =
                "Inter-bank funds transfer via TeamApt MEKS Switch.\n\n" +
                "**Encryption**: controlled by `Credentials:IsPgp` in appsettings.json\n" +
                "- `\"Y\"` → PGP (Legacy)\n" +
                "- `\"N\"` or `null` → RSA / ISO 20022 (default)\n\n" +
                "**Flow**: Name Enquiry → Funds Transfer → Status Query (if code 09/97)"
        });
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    var isPgp = builder.Configuration["Credentials:IsPgp"] ?? "(null → RSA default)";
    logger.Info("Application ready. IsPgp={IsPgp}", isPgp);

    app.Run();
}
catch (Exception ex)
{
    logger.Fatal(ex, "Application terminated unexpectedly.");
    throw;
}
finally
{
    LogManager.Shutdown();
}
