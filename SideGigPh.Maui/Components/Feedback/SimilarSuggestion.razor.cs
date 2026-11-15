using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Feedback;

/// <summary>
/// A suggestion already on the board that looks like the one being written (<see cref="SuggestPage"/>'s "Is it one
/// of these?"): its title, details and votes, and one button to say "same idea", which adds this person's vote.
/// </summary>
public partial class SimilarSuggestion : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public FeedbackPost Post { get; set; } = default!;

    [Parameter] public bool Disabled { get; set; }

    /// <summary>The person says theirs is the same as this one.</summary>
    [Parameter] public EventCallback<FeedbackPost> OnSame { get; set; }

    private string VotesLabel => Post.Votes == 1 ? T.Get(Copy.FeedbackVotesOne) : T.Format(Copy.FeedbackVotes, Post.Votes);
}
