using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using InboxWrap.Clients;
using InboxWrap.Configuration;
using InboxWrap.Infrastructure.Queues;
using InboxWrap.Infrastructure.RateLimiters;
using InboxWrap.Repositories;
using InboxWrap.Services;
using InboxWrap.Updaters;
using InboxWrap.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

string? connectionString = builder.Configuration.GetConnectionString("PostgresConnection");

// Configuration
builder.Services.Configure<PostgresConfig>(builder.Configuration.GetSection("Postgres"));
builder.Services.Configure<HashiCorpConfig>(builder.Configuration.GetSection("HashiCorp"));
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("Jwt"));

PostgresConfig postgresConfig = builder.Configuration.GetSection("Postgres").Get<PostgresConfig>()!;
HashiCorpConfig hashiCorpConfig = builder.Configuration.GetSection("HashiCorp").Get<HashiCorpConfig>()!;
JwtConfig jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfig>()!;

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    string connectionString =
        $"Host={postgresConfig.Host};"+
        $"Port={postgresConfig.Port};"+
        $"Database={postgresConfig.Database};"+
        $"Username={postgresConfig.Username};"+
        $"Password={postgresConfig.Password}";

    try
    {
        options.UseNpgsql(connectionString);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to configure db context: {ex.Message}");
        throw;
    }
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "https://id.inboxwrap.com",
            ValidAudience = "user",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtConfig.Secret))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Read token from cookie instead of header
                context.Token = context.Request.Cookies["access_token"];
                return Task.CompletedTask;
            }
        };

    });

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, IPAddress>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress!,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("AuthPolicy", httpContext =>
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetTokenBucketLimiter(ipAddress, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 5, // Max requests in period.
            TokensPerPeriod = 5, // Replenish tokens each period.
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            AutoReplenishment = true,
            QueueLimit = 0, // No queuing.
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });

    options.RejectionStatusCode = 429;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"error\":\"Too many requests. Please try again later.\"}", token);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddControllers();

// Rate Limiters
builder.Services.AddSingleton<IRateLimiter>(new GroqRateLimiter(30, 6000));

// Clients
builder.Services.AddHttpClient<ISecretsManagerClient, SecretsManagerClient>();
builder.Services.AddHttpClient<IMicrosoftAzureClient, MicrosoftAzureClient>();
builder.Services.AddHttpClient<IGroqClient, GroqClient>();
builder.Services.AddHttpClient<IPostmarkClient, PostmarkClient>();

// Repostories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IConnectedAccountRepository, ConnectedAccountRepository>();
builder.Services.AddScoped<ISummaryRepository, SummaryRepository>();

// Services
builder.Services.AddSingleton<ISummaryQueue, InMemorySummaryQueue>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMicrosoftProviderService, MicrosoftProviderService>();
builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddScoped<IEmailFetchService, EmailFetchService>();
builder.Services.AddScoped<ISummaryGenerationService, SummaryGenerationService>();
builder.Services.AddScoped<ISummaryEmailDispatchService, SummaryEmailDispatchService>();

// Updaters
builder.Services.AddScoped<IMailUpdater, StripHtmlUpdater>();
builder.Services.AddScoped<IMailUpdater, TrimWhitespaceUpdater>();
builder.Services.AddScoped<IMailUpdater, LimitLengthUpdater>();

// Workers
builder.Services.AddHostedService<EmailFetchWorker>();
builder.Services.AddHostedService<SummaryGenerationWorker>();
builder.Services.AddHostedService<SummaryEmailDispatcher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers()
    .RequireAuthorization();

app.UseSerilogRequestLogging();

app.Run();
