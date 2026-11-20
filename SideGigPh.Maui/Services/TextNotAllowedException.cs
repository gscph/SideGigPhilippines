using System.Net;

namespace SideGigPh.Maui.Services;

/// <summary>
/// The server refused what the person wrote (<c>422 text-not-allowed</c>): vulgar, sexual or swear words. Nothing was
/// saved or sent. Pages catch it before their general "didn't send" and show <see cref="Localization.Copy.TextNotAllowed"/>
/// so the person can rewrite it; it is a <see cref="HttpRequestException"/>, so a page that doesn't still shows its failure.
/// </summary>
public sealed class TextNotAllowedException(string message)
    : HttpRequestException(message, null, HttpStatusCode.UnprocessableEntity);
