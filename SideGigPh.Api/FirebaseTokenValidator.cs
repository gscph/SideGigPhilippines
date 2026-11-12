using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace SideGigPh.Api;

/// <summary>The caller, from their Firebase ID token: the uid, and the phone number the account verified by SMS code (if any).</summary>
public sealed record Caller(string Uid, string? PhoneNumber);

/// <summary>
/// Checks the Firebase ID token the app sends as "Authorization: Bearer …" and says who is calling.
/// Google's signing keys are fetched from Firebase's public OpenID configuration and cached.
/// </summary>
public sealed class FirebaseTokenValidator(string projectId)
{
    private readonly ConfigurationManager<OpenIdConnectConfiguration> keys = new(
        $"https://securetoken.google.com/{projectId}/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever());

    private readonly JsonWebTokenHandler handler = new();

    /// <summary>The caller, or null when the token is missing, expired or not from this project.</summary>
    public async Task<Caller?> GetCallerAsync(HttpRequest request, CancellationToken cancellationToken)
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

        if (!result.IsValid || !result.Claims.TryGetValue("sub", out var sub) || sub is not string { Length: > 0 } uid)
        {
            return null;
        }

        // Firebase adds phone_number once the account has verified a number (phone sign-in, or a number linked to Google).
        var phone = result.Claims.TryGetValue("phone_number", out var value) && value is string { Length: > 0 } number ? number : null;
        return new Caller(uid, phone);
    }

    /// <summary>The caller's uid, or null when the token is missing, expired or not from this project.</summary>
    public async Task<string?> GetUidAsync(HttpRequest request, CancellationToken cancellationToken) =>
        (await GetCallerAsync(request, cancellationToken))?.Uid;
}
