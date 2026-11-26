using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;

namespace SideGigPh.Mcp;

/// <summary>
/// Signs the person in to Azure SQL with their own Microsoft (Entra) account: the first query opens the sign-in page in
/// the browser, then the login is remembered (Azure.Identity's token cache, encrypted for this Windows user, plus which
/// account it was in <see cref="RecordFile"/>), so later runs sign in silently until the login expires.
/// Used only when the connection string says nothing about how to sign in (<see cref="IsNeeded"/>).
/// </summary>
public sealed class SqlSignIn
{
    private static readonly string[] SqlScope = ["https://database.windows.net/.default"];

    private static readonly string RecordFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SideGigPh.Mcp", "sign-in.json");

    private readonly InteractiveBrowserCredentialOptions _options;
    private readonly SemaphoreSlim _once = new(1, 1);
    private InteractiveBrowserCredential _credential;

    public SqlSignIn(string? tenantId)
    {
        _options = new InteractiveBrowserCredentialOptions
        {
            TenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim(),
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions { Name = "SideGigPh.Mcp" },
            AuthenticationRecord = LoadRecord(),
        };
        _credential = new InteractiveBrowserCredential(_options);
    }

    /// <summary>True when the connection string has no sign-in of its own (no Authentication, user or Windows login).</summary>
    public static bool IsNeeded(string connectionString)
    {
        var settings = new SqlConnectionStringBuilder(connectionString);
        return settings.Authentication == SqlAuthenticationMethod.NotSpecified
               && string.IsNullOrEmpty(settings.UserID)
               && !settings.IntegratedSecurity;
    }

    /// <summary>A connection that gets its access token from this sign-in.</summary>
    public SqlConnection Connect(string connectionString) => new(connectionString)
    {
        AccessTokenCallback = async (_, cancellationToken) =>
        {
            var token = await TokenAsync(cancellationToken);
            return new SqlAuthenticationToken(token.Token, token.ExpiresOn);
        },
    };

    private async Task<AccessToken> TokenAsync(CancellationToken cancellationToken)
    {
        var request = new TokenRequestContext(SqlScope);
        if (_options.AuthenticationRecord is not null)
        {
            return await _credential.GetTokenAsync(request, cancellationToken);
        }

        await _once.WaitAsync(cancellationToken);
        try
        {
            if (_options.AuthenticationRecord is null)
            {
                // First time on this PC: the browser opens; remember the account for the next runs.
                _options.AuthenticationRecord = await _credential.AuthenticateAsync(request, cancellationToken);
                _credential = new InteractiveBrowserCredential(_options);
                await SaveRecordAsync(_options.AuthenticationRecord, cancellationToken);
            }
        }
        finally
        {
            _once.Release();
        }

        return await _credential.GetTokenAsync(request, cancellationToken);
    }

    private static AuthenticationRecord? LoadRecord()
    {
        try
        {
            using var file = File.OpenRead(RecordFile);
            return AuthenticationRecord.Deserialize(file);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return null; // never signed in here (or the file is broken): sign in again
        }
    }

    private static async Task SaveRecordAsync(AuthenticationRecord record, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RecordFile)!);
        await using var file = File.Create(RecordFile);
        await record.SerializeAsync(file, cancellationToken);
    }
}
