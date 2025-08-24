using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace BookingApi.Helpers;

/// <summary>
/// A helper class to resolve strings that may be literals or ARNs for secrets in AWS Secrets Manager.
/// </summary>
public class SecretHelper
{
    private static readonly AmazonSecretsManagerClient _sm = new();

    /// <summary>
    /// Asynchronously resolves a string. If the string is identified as an AWS Secrets Manager ARN,
    /// it fetches the secret value; otherwise, it returns the original string.
    /// </summary>
    /// <param name="idOrLiteral">The string to resolve, which can be a literal value or a secret ARN.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the secret value as a string
    /// if the input was an ARN, or the original literal string otherwise. Returns null if the input is null or whitespace.
    /// </returns>
    /// <exception cref="Amazon.SecretsManager.Model.ResourceNotFoundException">Thrown if the input is an ARN but the secret cannot be found in AWS Secrets Manager.</exception>
    public static async Task<string?> ResolveAsync(string? idOrLiteral)
    {
        if (string.IsNullOrWhiteSpace(idOrLiteral)) return null;

        if (idOrLiteral.StartsWith("arn:aws:secretsmanager:") || idOrLiteral.Contains("/"))
        {
            var resp = await _sm.GetSecretValueAsync(new GetSecretValueRequest { SecretId = idOrLiteral });
            return resp.SecretString;
        }

        return idOrLiteral;
    }

}
