using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SideGigPh.Contracts;

namespace SideGigPh.Api;

/// <summary>
/// The HTML of the public feedback board (<see cref="SuggestFunctions"/>): English with a short Taglish line, in the
/// legal pages' look. Everything a visitor or the board wrote is HTML-encoded.
/// </summary>
internal static class SuggestPage
{
    public enum Notice
    {
        None,
        Posted,
    }

    /// <summary>
    /// What the visitor typed, whether it was refused (too short or long: <c>Problem</c>; rude words: <c>Rude</c>), and the
    /// look-alikes offered instead of posting it.
    /// </summary>
    public sealed record Draft(
        string Title = "", string Details = "", bool Problem = false, IReadOnlyList<FeedbackPostDto>? Similar = null, bool Rude = false);

    /// <summary>The note over the form when moderation refused the suggestion (the app's <c>Copy.TextNotAllowed</c>).</summary>
    public const string RudeNote = "Please remove rude or sexual words, then try again.";

    public static ContentResult Html(string root, IReadOnlyList<FeedbackPostDto> board, Draft draft, Notice notice)
    {
        var html = new StringBuilder();
        html.Append($$"""
            <!doctype html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta name="robots" content="noindex">
            <title>Tarbaho · Ideas</title>
            <style>
              body { margin: 0; background: #fff; color: #222; font: 16px/1.5 system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; }
              main { max-width: 680px; margin: 0 auto; padding: 24px 16px 64px; }
              h1 { font-size: 1.7rem; line-height: 1.2; margin: 0 0 4px; } h2 { font-size: 1.2rem; margin: 32px 0 8px; }
              .muted { color: #717171; margin: 0; }
              .card { border: 1px solid #ebebeb; border-radius: 12px; padding: 16px; margin: 16px 0; }
              .notice { background: #f3ecfa; border-color: #5B2A86; }
              .problem { color: #c13515; margin: 4px 0 0; }
              label { display: block; font-weight: 600; margin: 12px 0 4px; }
              input[type=text], textarea { box-sizing: border-box; width: 100%; font: inherit; padding: 12px; border: 1px solid #b0b0b0; border-radius: 12px; }
              textarea { min-height: 88px; resize: vertical; }
              .trap { position: absolute; left: -10000px; width: 1px; height: 1px; overflow: hidden; }
              button { font: inherit; font-weight: 600; border-radius: 8px; padding: 10px 16px; cursor: pointer; border: 1px solid #222; background: #fff; color: #222; }
              button.main { background: #5B2A86; border-color: #5B2A86; color: #fff; margin-top: 12px; }
              button.voted { background: #5B2A86; border-color: #5B2A86; color: #fff; }
              ul { list-style: none; padding: 0; margin: 0; }
              li { display: flex; gap: 12px; align-items: flex-start; padding: 14px 0; border-bottom: 1px solid #ebebeb; }
              li form { flex: none; }
              li .text { flex: 1; min-width: 0; overflow-wrap: anywhere; }
              li .title { font-weight: 600; margin: 0; } li .details { margin: 4px 0 0; color: #484848; }
              .votes { display: inline-block; min-width: 2.5em; text-align: center; font-weight: 700; }
              .tag { display: inline-block; font-size: 0.8rem; font-weight: 600; padding: 2px 8px; border-radius: 99px; background: #f7f7f7; margin-left: 6px; }
            </style>
            </head>
            <body><main>
            <h1>Tarbaho ideas</h1>
            <p class="muted">Upvote what you want most. The most votes go on top.</p>
            <p class="muted">I-upvote ang gusto mo. Ang may pinakamaraming boto, nasa itaas.</p>
            """);

        if (notice == Notice.Posted)
        {
            html.Append("""<div class="card notice"><strong>Thank you!</strong> Your idea is on the board. Salamat!</div>""");
        }

        if (draft.Similar is { Count: > 0 } similar)
        {
            html.Append("""<div class="card"><h2 style="margin-top:0">Is it one of these?</h2><p class="muted">Upvote it instead of posting it again. / Baka nandito na, i-upvote na lang.</p><ul>""");
            foreach (var post in similar)
            {
                Row(html, root, post);
            }

            html.Append($"""
                </ul>
                <form method="post" action="{Encode(root)}">
                <input type="hidden" name="title" value="{Encode(draft.Title)}">
                <input type="hidden" name="details" value="{Encode(draft.Details)}">
                <input type="hidden" name="step" value="post">
                <button type="submit" class="main">None of these, post mine</button>
                </form></div>
                """);
        }
        else
        {
            html.Append($"""
                <form class="card" method="post" action="{Encode(root)}">
                <label for="title">What would you like Tarbaho to do? / Ano'ng gusto mong idagdag namin?</label>
                <input type="text" id="title" name="title" maxlength="{FeedbackRules.MaxTitle}" required value="{Encode(draft.Title)}" placeholder="e.g. Pay with GCash">
                {(draft.Rude ? $"""<p class="problem" role="alert">{RudeNote} / Pakitanggal ang bastos o malaswang salita.</p>""" : "")}
                {(draft.Problem ? $"""<p class="problem">Write at least {FeedbackRules.MinTitle} characters (and details under {FeedbackRules.MaxDetails}).</p>""" : "")}
                <label for="details">More details (optional)</label>
                <textarea id="details" name="details" maxlength="{FeedbackRules.MaxDetails}">{Encode(draft.Details)}</textarea>
                <div class="trap" aria-hidden="true"><label for="{SuggestFunctions.TrapField}">Leave empty</label><input type="text" id="{SuggestFunctions.TrapField}" name="{SuggestFunctions.TrapField}" tabindex="-1" autocomplete="off"></div>
                <button type="submit" class="main">Next</button>
                </form>
                """);
        }

        html.Append("<h2>Ideas so far</h2>");
        if (board.Count == 0)
        {
            html.Append("""<p class="muted">No ideas yet. Be the first!</p>""");
        }
        else
        {
            html.Append("<ul>");
            foreach (var post in board)
            {
                Row(html, root, post);
            }

            html.Append("</ul>");
        }

        html.Append("</main></body></html>");
        return new ContentResult
        {
            Content = html.ToString(),
            ContentType = "text/html; charset=utf-8",
            StatusCode = draft.Rude ? StatusCodes.Status422UnprocessableEntity : StatusCodes.Status200OK,
        };
    }

    /// <summary>One suggestion: its votes with Upvote (or Voted, which takes the vote back), title, details and stage.</summary>
    private static void Row(StringBuilder html, string root, FeedbackPostDto post)
    {
        var stage = post.Status switch
        {
            FeedbackStatuses.Planned => """<span class="tag">Planned</span>""",
            FeedbackStatuses.Done => """<span class="tag">Done</span>""",
            _ => "",
        };
        html.Append($"""
            <li id="post-{post.Id}">
            <form method="post" action="{Encode(root)}/{post.Id}/vote">
            <input type="hidden" name="on" value="{(post.VotedByMe ? "0" : "1")}">
            <button type="submit"{(post.VotedByMe ? " class=\"voted\"" : "")} aria-label="{(post.VotedByMe ? "Take back your vote" : "Upvote")}">▲ <span class="votes">{post.Votes}</span></button>
            </form>
            <div class="text"><p class="title">{Encode(post.Title)}{stage}</p>{(string.IsNullOrWhiteSpace(post.Details) ? "" : $"<p class=\"details\">{Encode(post.Details)}</p>")}</div>
            </li>
            """);
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? "");
}
