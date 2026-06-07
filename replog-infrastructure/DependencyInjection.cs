using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using replog_infrastructure.Settings;

namespace replog_infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDynamoDb(this IServiceCollection services, IConfiguration configuration)
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
