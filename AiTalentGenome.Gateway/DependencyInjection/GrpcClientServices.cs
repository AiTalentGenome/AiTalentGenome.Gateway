using AiTalentGenome.Contracts.Identity;
using AiTalentGenome.Contracts.Vacancies;

namespace AiTalentGenome.Gateway.DependencyInjection;

public static class GrpcClientServices
{
    public static IServiceCollection AddGrpcClients(this IServiceCollection services, IConfiguration configuration)
    {
        // Регистрация клиента IdentityService
        services.AddGrpcClient<IdentityService.IdentityServiceClient>(options =>
            {
                var identityUrl = configuration["Services:IdentityUrl"] 
                                  ?? throw new InvalidOperationException("IdentityUrl is not configured");
            
                options.Address = new Uri(identityUrl);
            })
            .ConfigureChannel(options =>
            {
                // В .NET 10 можно настроить более агрессивный Keep-Alive для долгоживущих gRPC соединений
                options.HttpHandler = new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    EnableMultipleHttp2Connections = true
                };
            });

        services.AddGrpcClient<VacancyService.VacancyServiceClient>(options =>
            {
                var identityUrl = configuration["Services:VacancyUrl"] 
                                  ?? throw new InvalidOperationException("IdentityUrl is not configured");
            
                options.Address = new Uri(identityUrl);
            })
            .ConfigureChannel(options =>
            {
                // В .NET 10 можно настроить более агрессивный Keep-Alive для долгоживущих gRPC соединений
                options.HttpHandler = new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                    EnableMultipleHttp2Connections = true
                };
            });

        return services;
    }
}