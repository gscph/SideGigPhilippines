using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace SideGigPh.Api;

/// <summary>
/// Checks the Firebase ID token the app sends as "Authorization: Bearer …" and gives the user's uid.
/// Google's signing keys are fetched from Firebase's public OpenID configuration and cached.
/// </summary>
public sealed class FirebaseTokenValidator(string projectId)
{
    private readonly ConfigurationManager<OpenIdConnectConfiguration> keys = new(
        $"https://securetoken.google.com/{projectId}/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever());

    private readonly JsonWebTokenHandler handler = new();

    /// <summary>The signed-in user's uid, or null when the token is missing, expired or not from this project.</summary>
    public async Task<string?> GetUidAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        var header = request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.Ordinal))
        {
            return null;
        }

        var config = await keys.GetConfigurationAsync(cancellationToken);
        var result = await handler.ValidateTokenAsync(header["Bearer ".Length..], new TokenValidationParameters
        {
            ValidIssuer = $"https://securetoken.google.com/{projectId}",
            ValidAudience = projectId,
            IssuerSigningKeys = config.SigningKeys,
        });

        return result.IsValid && result.Claims.TryGetValue("sub", out var sub) && sub is string uid && uid.Length > 0
            ? uid
            : null;
    }
}
