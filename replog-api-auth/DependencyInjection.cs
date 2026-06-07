using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.Extensions.Options;
using replog_api_auth.Interfaces;
using replog_api_auth.Repositories;
using replog_api_auth.Settings;

namespace replog_api_auth;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddDynamoDb(configuration)
            .AddScoped<IUserRepository, UserRepository>();
    }

    private static IServiceCollection AddDynamoDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DynamoDbSettings>(configuration.GetSection("DynamoDB"));

        services.AddSingleton<IAmazonDynamoDB>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<DynamoDbSettings>>().Value;
            var clientConfig = new AmazonDynamoDBConfig { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(settings.Region) };

            if (!string.IsNullOrEmpty(settings.ServiceURL))
                clientConfig.ServiceURL = settings.ServiceURL;

            if (!string.IsNullOrEmpty(settings.AccessKey))
                return new AmazonDynamoDBClient(new BasicAWSCredentials(settings.AccessKey, settings.SecretKey), clientConfig);

            return new AmazonDynamoDBClient(clientConfig);
        });

        return services;
    }
}
