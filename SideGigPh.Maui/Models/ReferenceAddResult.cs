namespace SideGigPh.Maui.Models;

/// <summary>Outcome of adding a reference (<c>ProWorkspace.AddReferenceAsync</c>).</summary>
public enum ReferenceAddResult
{
    Added,

    NoName,

    /// <summary>Not a Philippine mobile number.</summary>
    InvalidNumber,

    /// <summary>The pro's own number can't vouch for them.</summary>
    OwnNumber,

    /// <summary>That number is already a reference.</summary>
    Duplicate,

    /// <summary>Already at <see cref="ProfessionalDraft.MaxReferences"/>.</summary>
    Full,
}
