// AiTalentGenome.Gateway/Program.cs

using AiTalentGenome.Gateway.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi(); 

builder.Services.AddGrpcClients(builder.Configuration);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

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