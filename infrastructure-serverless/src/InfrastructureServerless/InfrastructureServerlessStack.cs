using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;
using System.Collections.Generic;

namespace InfrastructureServerless;

public class InfrastructureServerlessStack : Stack
{
    internal InfrastructureServerlessStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
        Vpc vpc = new(this, "BookingApiVpc", new VpcProps { MaxAzs = 2, NatGateways = 0, IpAddresses = IpAddresses.Cidr("10.10.0.0/16") });

        vpc.AddInterfaceEndpoint("SecretsManagerEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.SECRETS_MANAGER
        });

        vpc.AddInterfaceEndpoint("CloudWatchLogsEndpoint", new InterfaceVpcEndpointOptions
        {
            Service = InterfaceVpcEndpointAwsService.CLOUDWATCH_LOGS
        });

        DatabaseInstance dbInstance = new DatabaseInstance(this, "Database", new DatabaseInstanceProps
        {
            Engine = DatabaseInstanceEngine.Postgres(new PostgresInstanceEngineProps
            {
                Version = PostgresEngineVersion.VER_15
            }),
            Vpc = vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED },
            InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(InstanceClass.BURSTABLE4_GRAVITON , InstanceSize.MICRO),
            MultiAz = false,
            AllocatedStorage = 20,
            RemovalPolicy = RemovalPolicy.DESTROY,
            DeletionProtection = false,
            Credentials = Credentials.FromGeneratedSecret("postgres"),
            DatabaseName = "bookingdb",
            StorageEncrypted = true
        });

        ISecret dbSecret = dbInstance.Secret;
        ISecret jwtKeySecret = Secret.FromSecretNameV2(this, "JwtKey", "BookingApi/JwtKey");
        ISecret adminSetupKeySecret = Secret.FromSecretNameV2(this, "AdminSetupKey", "BookingApi/AdminSetupKey");

        Function apiFunction = new(this, "ApiFunction", new FunctionProps
        {
            Runtime = Runtime.DOTNET_8,
            Handler = "BookingApi::BookingApi.LambdaEntryPoint::FunctionHandlerAsync",
            Code = Code.FromAsset("../BookingApi/bin/Release/net8.0/publish.zip"),
            Vpc = vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PRIVATE_ISOLATED },
            Timeout = Duration.Seconds(30),
            MemorySize = 256,
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

        dbInstance.Connections.AllowDefaultPortFrom(apiFunction);

        HttpApi httpApi = new(this, "BookingApiGateway", new HttpApiProps
        {
            DefaultIntegration = new HttpLambdaIntegration("ApiIntegration", apiFunction),
            CorsPreflight = new CorsPreflightOptions
            {
                AllowOrigins = new[] { "*" },
                AllowMethods = new[]
                {
                    CorsHttpMethod.GET,
                    CorsHttpMethod.POST,
                    CorsHttpMethod.PUT,
                    CorsHttpMethod.DELETE,
                    CorsHttpMethod.OPTIONS
                },
                AllowHeaders = new[] { "Content-Type", "Authorization" },
            }
        });

        new LogGroup(this, "ApiLogGroup", new LogGroupProps
        {
            Retention = RetentionDays.ONE_WEEK,
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        apiFunction.AddEnvironment("Jwt__Issuer", httpApi.Url!);

        new CfnOutput(this, "ApiUrl", new CfnOutputProps
        {
            Value = httpApi.Url!
        });
    }
}