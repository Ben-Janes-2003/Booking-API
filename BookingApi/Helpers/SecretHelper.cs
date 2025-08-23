using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace BookingApi.Helpers;

public class SecretHelper
{
    private static readonly AmazonSecretsManagerClient _sm = new();

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
