namespace SideGigPh.Admin.Components.Shared;

/// <summary>One choice in a list's filter menu: the value sent to the API (empty = no filter) and what the operator reads.</summary>
public sealed record FilterChoice(string Value, string Label);
