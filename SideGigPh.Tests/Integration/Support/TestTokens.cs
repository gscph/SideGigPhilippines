using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace SideGigPh.Tests.Integration.Support;

/// <summary>
/// Signs ID tokens the way Firebase does (RS256, issuer and audience = the project), with a key made for the test run.
/// The API's real <c>FirebaseTokenValidator</c> checks them against that key instead of Google's, so sign-in is
/// tested end to end without a Google account.
/// </summary>
public sealed class TestTokens
{
    public const string ProjectId = "sidegigph-tests";

    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "test-key" };

    public IConfigurationManager<OpenIdConnectConfiguration> SigningKeys
    {
        get
        {
            var config = new OpenIdConnectConfiguration();
            config.SigningKeys.Add(_key);
            return new StaticConfigurationManager<OpenIdConnectConfiguration>(config);
        }
    }

    /// <summary>A valid token for <paramref name="uid"/>; with <paramref name="phone"/> once the account verified a number.</summary>
    public string For(string uid, string? phone = null, string issuerProject = ProjectId, DateTime? expires = null)
    {
        var claims = new List<Claim> { new("sub", uid) };
        if (phone is not null)
        {
            claims.Add(new Claim("phone_number", phone));
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = $"https://securetoken.google.com/{issuerProject}",
            Audience = issuerProject,
            Subject = new ClaimsIdentity(claims),
            NotBefore = (expires ?? DateTime.UtcNow.AddHours(1)).AddHours(-2),
            Expires = expires ?? DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256),
        });
    }
}
