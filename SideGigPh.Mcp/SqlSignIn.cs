using System.Data.Common;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SideGigPh.Mcp;

/// <summary>
/// Signs the person in to Azure SQL with their own Microsoft (Entra) account: the first query opens the sign-in page in
/// the browser, then the login is remembered (Azure.Identity's token cache, encrypted for this Windows user, plus which
/// account it was in <see cref="RecordFile"/>), so later runs sign in silently until the login expires.
/// Used only when the connection string says nothing about how to sign in (<see cref="IsNeeded"/>).
/// The sign-in goes to the database's own directory (tenant), which the server names itself
/// (<see cref="FindTenantAsync"/>), so only accounts in or invited to it can be picked; AZURE_TENANT_ID overrides it.
/// </summary>
/// <remarks>
/// The browser step runs in <see cref="ReadyAsync"/>, before the connection opens (<see cref="Interceptor"/>), with
/// <see cref="BrowserTimeout"/>: SqlClient's own token callback is cut off by the connect timeout (15 s), too short to
/// type a password and approve MFA. By then the token is cached, so the callback only reads it.
/// </remarks>
public sealed class SqlSignIn
{
    /// <summary>How long the person has to finish signing in in the browser.</summary>
    public static readonly TimeSpan BrowserTimeout = TimeSpan.FromMinutes(5);

    private static readonly TokenRequestContext SqlToken = new(["https://database.windows.net/.default"]);

    private static readonly string RecordFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SideGigPh.Mcp", "sign-in.json");

    private readonly string _connectionString;
    private readonly string? _tenantId;
    private readonly SemaphoreSlim _once = new(1, 1);
    private InteractiveBrowserCredentialOptions? _options;
    private InteractiveBrowserCredential? _credential;

    public SqlSignIn(string connectionString, string? tenantId)
    {
        _connectionString = connectionString;
        _tenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim();
        Interceptor = new SignInFirst(this);
    }

    /// <summary>Add to the DbContext: signs in before each connection opens.</summary>
    public IInterceptor Interceptor { get; }

    /// <summary>True when the connection string has no sign-in of its own (no Authentication, user, password or Windows login).</summary>
    public static bool IsNeeded(string connectionString)
    {
        var settings = new SqlConnectionStringBuilder(connectionString);
        return settings.Authentication == SqlAuthenticationMethod.NotSpecified
               && string.IsNullOrEmpty(settings.UserID)
               && string.IsNullOrEmpty(settings.Password)
               && !settings.IntegratedSecurity;
    }

    /// <summary>A connection that gets its access token from this sign-in.</summary>
    public SqlConnection Connect() => new(_connectionString)
    {
        AccessTokenCallback = async (_, cancellationToken) =>
        {
            var credential = _credential ?? throw new InvalidOperationException("Not signed in yet: ReadyAsync runs first.");
            var token = await credential.GetTokenAsync(SqlToken, cancellationToken); // cached by ReadyAsync: no browser
            return new SqlAuthenticationToken(token.Token, token.ExpiresOn);
        },
    };

    /// <summary>
    /// Makes sure there is a valid login: silent when one is remembered, else the browser (the first time, or when it
    /// expired), with <see cref="BrowserTimeout"/> to finish. One sign-in at a time.
    /// </summary>
    public async Task ReadyAsync(CancellationToken cancellationToken)
    {
        await _once.WaitAsync(cancellationToken);
        try
        {
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limit.CancelAfter(BrowserTimeout);
            if (_options is null || _credential is null)
            {
                var record = LoadRecord();
                _options = new InteractiveBrowserCredentialOptions
                {
                    TenantId = _tenantId ?? record?.TenantId ?? await FindTenantAsync(_connectionString, limit.Token),
                    TokenCachePersistenceOptions = new TokenCachePersistenceOptions { Name = "SideGigPh.Mcp" },
                    AuthenticationRecord = record,
                };
                _credential = new InteractiveBrowserCredential(_options);
            }

            if (_options.AuthenticationRecord is not null)
            {
                await _credential.GetTokenAsync(SqlToken, limit.Token);
                return;
            }

            // First time on this PC: the browser opens; remember the account for the next runs.
            _options.AuthenticationRecord = await _credential.AuthenticateAsync(SqlToken, limit.Token);
            _credential = new InteractiveBrowserCredential(_options);
            await SaveRecordAsync(_options.AuthenticationRecord);
        }
        finally
        {
            _once.Release();
        }
    }

    /// <summary>
    /// The directory (tenant) the database signs people in with: SqlClient hands the token callback the server's
    /// sign-in authority (<c>https://login.windows.net/&lt;tenant&gt;</c>), so a first connection stops right there,
    /// before any sign-in. Null when the server doesn't say (the browser then offers every account).
    /// </summary>
    public static async Task<string?> FindTenantAsync(string connectionString, CancellationToken cancellationToken)
    {
        string? authority = null;
        await using var probe = new SqlConnection(connectionString)
        {
            AccessTokenCallback = (parameters, _) =>
            {
                authority = parameters.Authority;
                throw new OperationCanceledException("Only asking the server for its sign-in authority.");
            },
        };
        try
        {
            await probe.OpenAsync(cancellationToken);
        }
        catch (Exception e) when (e is SqlException or OperationCanceledException)
        {
            // Expected: the probe never signs in.
        }

        return TenantOf(authority);
    }

    /// <summary>The tenant at the end of a sign-in authority such as <c>https://login.windows.net/&lt;tenant&gt;</c>.</summary>
    public static string? TenantOf(string? authority) =>
        Uri.TryCreate(authority, UriKind.Absolute, out var uri) && uri.Segments.LastOrDefault()?.Trim('/') is { Length: > 0 } tenant
            && !string.Equals(tenant, "common", StringComparison.OrdinalIgnoreCase)
            ? tenant.ToLowerInvariant()
            : null;

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

    private static async Task SaveRecordAsync(AuthenticationRecord record)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RecordFile)!);
            await using var file = File.Create(RecordFile);
            await record.SerializeAsync(file);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // This run is signed in all the same; the next run asks again.
            await Console.Error.WriteLineAsync($"Couldn't remember the sign-in in {RecordFile}: {e.Message}");
        }
    }

    /// <summary>Runs <see cref="ReadyAsync"/> before EF opens a connection, outside SqlClient's connect timeout.</summary>
    private sealed class SignInFirst(SqlSignIn signIn) : DbConnectionInterceptor
    {
        public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            await signIn.ReadyAsync(cancellationToken);
            return result;
        }

        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            signIn.ReadyAsync(CancellationToken.None).GetAwaiter().GetResult();
            return result;
        }
    }
}
