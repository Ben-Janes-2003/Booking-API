using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;
using System.Collections.Generic;
using Amazon.CDK.AWS.Lambda;

namespace InfrastructureServerless;

public class InfrastructureServerlessStack : Stack
{
    internal InfrastructureServerlessStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
        Vpc vpc = new(this, "BookingApiVpc", new VpcProps { MaxAzs = 2 });

        DatabaseCluster dbCluster = new(this, "Database", new DatabaseClusterProps
        {
            Engine = DatabaseClusterEngine.AuroraPostgres(new AuroraPostgresClusterEngineProps
            {
                Version = AuroraPostgresEngineVersion.VER_15_3
            }),
            Vpc = vpc,
            Writer = ClusterInstance.ServerlessV2("writer"),
            ServerlessV2MinCapacity = 0.5,
            ServerlessV2MaxCapacity = 1.0,
            EnableDataApi = false,
            DefaultDatabaseName = "bookingdb"
        });

        ISecret dbSecret = dbCluster.Secret;
        ISecret jwtKeySecret = Secret.FromSecretNameV2(this, "JwtKey", "BookingApi/JwtKey");
        ISecret adminSetupKeySecret = Secret.FromSecretNameV2(this, "AdminSetupKey", "BookingApi/AdminSetupKey");

        Function apiFunction = new(this, "ApiFunction", new FunctionProps
        {
            Runtime = Runtime.DOTNET_8,
            Handler = "BookingApi::BookingApi.LambdaEntryPoint::FunctionHandlerAsync",
            Code = Code.FromAsset("../BookingApi/bin/Release/net8.0/publish.zip"),
            Vpc = vpc,
            MemorySize = 512,
            Timeout = Duration.Seconds(30),
            Environment = new Dictionary<string, string>
            {
                { "ASPNETCORE_ENVIRONMENT", "Production" },
                { "DB_SECRET_ARN", dbSecret.SecretArn },
                { "Jwt__Key", jwtKeySecret.SecretArn },
                { "AdminSetupKey", adminSetupKeySecret.SecretArn }
            }
        });

        dbSecret.GrantRead(apiFunction);
        jwtKeySecret.GrantRead(apiFunction);
        adminSetupKeySecret.GrantRead(apiFunction);

        dbCluster.Connections.AllowDefaultPortFrom(apiFunction);

        HttpApi httpApi = new(this, "BookingApiGateway", new HttpApiProps
        {
            DefaultIntegration = new HttpLambdaIntegration("ApiIntegration", apiFunction)
        });

        apiFunction.AddEnvironment("Jwt__Issuer", httpApi.Url!);

        new CfnOutput(this, "ApiUrl", new CfnOutputProps
        {
            Value = httpApi.Url!
        });
    }
}