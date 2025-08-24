using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using BookingApi.Data.Dtos;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace BookingApi.Helpers;

/// <summary>
/// A helper class to retrieve database connection details from AWS Secrets Manager.
/// </summary>
public class DatabaseConfigHelper
{
    /// <summary>
    /// Asynchronously fetches database connection details from AWS Secrets Manager.
    /// </summary>
    /// <remarks>
    /// This method relies on the 'DB_SECRET_ARN' environment variable to identify which secret to fetch.
    /// The secret itself is expected to be a JSON object containing keys such as 'host', 'username', 'password', and 'dbname'.
    /// </remarks>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="DbConnectionInfoDto"/> with the connection details.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if the 'DB_SECRET_ARN' environment variable is not set.</exception>
    /// <exception cref="Amazon.SecretsManager.Model.ResourceNotFoundException">Thrown if the secret cannot be found in AWS Secrets Manager.</exception>
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