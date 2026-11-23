using Microsoft.AspNetCore.Http;

namespace SideGigPh.Api;

/// <summary>Who is calling a public route, for its per-visitor limits (the feedback board, Early Access).</summary>
public static class ClientAddress
{
    /// <summary>
    /// The visitor's address: the last <c>X-Forwarded-For</c> entry (the one Azure's front end added; earlier ones are
    /// whatever the client sent), else the connection's.
    /// </summary>
    public static string Of(HttpRequest request)
    {
        var forwarded = request.Headers["X-Forwarded-For"].ToString();
        if (forwarded.Length > 0)
        {
            return forwarded.Split(',')[^1].Trim();
        }

        return request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
