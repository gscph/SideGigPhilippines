using Microsoft.AspNetCore.Components;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services.Localization;

namespace SideGigPh.Maui.Components.Feedback;

/// <summary>
/// One suggestion on the feedback board: the vote button with the count on the left (filled purple once this person
/// voted), then the title, the details, where it stands (Planned / Done), and Remove for the writer's own while
/// nobody else voted, or a report flag for someone else's.
/// </summary>
public partial class FeedbackRow : ComponentBase
{
    [Inject] private AppText T { get; set; } = default!;

    [Parameter, EditorRequired] public FeedbackPost Post { get; set; } = default!;

    /// <summary>The vote button was tapped: vote, or take the vote back.</summary>
    [Parameter] public EventCallback<FeedbackPost> OnVote { get; set; }

    [Parameter] public EventCallback<FeedbackPost> OnRemove { get; set; }

    [Parameter] public EventCallback<FeedbackPost> OnReport { get; set; }

    private string VotesLabel => Post.Votes == 1 ? T.Get(Copy.FeedbackVotesOne) : T.Format(Copy.FeedbackVotes, Post.Votes);
}
