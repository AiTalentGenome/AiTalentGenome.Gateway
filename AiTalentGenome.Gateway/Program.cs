// AiTalentGenome.Gateway/Program.cs

using AiTalentGenome.Gateway.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "FrontendPolicy";

// 2. Настраиваем CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:8000") // Адрес твоего Next.js
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // КРИТИЧНО для работы куки
    });
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // Например, 10 МБ
});

builder.Services.AddControllers();
builder.Services.AddOpenApi(); 

builder.Services.AddGrpcClients(builder.Configuration);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseCors(CorsPolicy);

app.MapOpenApi();

app.MapScalarApiReference(options => 
{
    options
        .WithTitle("AiTalentGenome API Gateway")
        .WithTheme(ScalarTheme.Moon)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.MapControllers();
app.MapReverseProxy();

app.Run();