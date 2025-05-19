using InboxWrap.Clients;
using InboxWrap.Configuration;
using InboxWrap.Models.Reponses;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HashiCorpConfig>(builder.Configuration.GetSection("HashiCorp"));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddHttpClient<ISecretsManagerClient, SecretsManagerClient>();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    ISecretsManagerClient? secretsClient = serviceProvider.GetRequiredService<ISecretsManagerClient>();
    Secret? connectionString = secretsClient
        .GetSecretAsync("ConnectionString")
        .GetAwaiter()
        .GetResult();

    options.UseNpgsql(connectionString?.StaticVersion?.Value);
});

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.UseSerilogRequestLogging();

app.Run();
