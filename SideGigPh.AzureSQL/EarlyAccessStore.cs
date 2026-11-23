using Microsoft.EntityFrameworkCore;

namespace SideGigPh.AzureSQL;

/// <summary>One cleaned Early Access sign-up from the landing page (the rules are <c>EarlyAccessRules</c>).</summary>
public sealed record EarlyAccessSignup(string Email, string Phone, string? Role, string? Language);

/// <summary>
/// The landing page's Early Access sign-ups (<c>EarlyAccessSignups</c>, one row per email address), read by the team in
/// SQL to send the Play testing or TestFlight invites. A person removed on request is deleted by hand in SQL.
/// </summary>
public sealed class EarlyAccessStore(SideGigPhDb db)
{
    /// <summary>
    /// Keeps the sign-up. The same address again (a repeat, a second visit, another phone) updates its row, so a write
    /// sent twice is one sign-up.
    /// </summary>
    public async Task SaveAsync(EarlyAccessSignup signup, DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            await UpsertAsync(signup, now, cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlErrors.Violates(ex, Constraints.EarlyAccessKey))
        {
            // The same address saved by another request in the same moment: update that row instead.
            db.ChangeTracker.Clear();
            await UpsertAsync(signup, now, cancellationToken);
        }
    }

    private async Task UpsertAsync(EarlyAccessSignup signup, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var row = await db.EarlyAccessSignups.FirstOrDefaultAsync(s => s.Email == signup.Email, cancellationToken);
        if (row is null)
        {
            db.EarlyAccessSignups.Add(new EarlyAccessSignupRow
            {
                Email = signup.Email,
                Phone = signup.Phone,
                Role = signup.Role,
                Language = signup.Language,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        else
        {
            row.Phone = signup.Phone;
            row.Role = signup.Role ?? row.Role;
            row.Language = signup.Language ?? row.Language;
            row.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
