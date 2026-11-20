namespace SideGigPh.Admin.Services;

/// <summary>Who is signed in on the operator site. <see cref="Uid"/> is what goes in the API's <c>AdminUids</c> setting.</summary>
public sealed record AdminUser(string Uid, string? Email, string? Name);
