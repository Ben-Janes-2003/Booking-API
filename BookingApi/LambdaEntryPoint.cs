using Amazon.Lambda.AspNetCoreServer;

namespace BookingApi;

/// <summary>
/// This class extends from APIGatewayHttpApiV2ProxyFunction which contains the logic for processing
/// API Gateway HTTP API events. It serves as the main entry point for all requests coming from API Gateway into the Lambda function.
/// </summary>
public class LambdaEntryPoint : APIGatewayHttpApiV2ProxyFunction
{
    /// <summary>
    /// The Init method is called once during the Lambda instance startup. It's the ideal place to
    /// bootstrap the ASP.NET Core host. The builder is configured to use the <see cref="Startup"/> class for all
    /// service and middleware configuration.
    /// </summary>
    /// <param name="builder">The IWebHostBuilder to configure.</param>
    protected override void Init(IWebHostBuilder builder)
    {
        builder.UseStartup<Startup>();
    }
}