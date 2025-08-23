using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using BookingApi.Data.Dtos;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace BookingApi.Helpers;

public class DatabaseConfigHelper
{
    public static async Task<DbConnectionInfoDto> GetDbConnectionInfoAsync()
    {
        string? secretArn = Environment.GetEnvironmentVariable("DB_SECRET_ARN");
        var client = new AmazonSecretsManagerClient();
        var secretValue = await client.GetSecretValueAsync(new GetSecretValueRequest
        {
            SecretId = secretArn
        });

        var secretJson = JObject.Parse(secretValue.SecretString);

        string host = secretJson["host"]!.ToString();
        string username = secretJson["username"]!.ToString();
        string password = secretJson["password"]!.ToString();
        string dbName = secretJson["dbname"]?.ToString() ?? "bookingdb";

        return new DbConnectionInfoDto
        {
            Host = host ?? secretJson["host"]?.ToString() ?? "localhost",
            Database = dbName ?? secretJson["dbname"]?.ToString() ?? "bookingdb",
            Username = username,
            Password = password
        };
    }
}