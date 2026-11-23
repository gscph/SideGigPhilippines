namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>The page's anchors (header links and every "Get early access" button scroll to them) and its outside links.</summary>
public static class Sections
{
    public const string HowItWorks = "how";
    public const string EarlyAccess = "early-access";
    public const string Trades = "trades";
    public const string Faq = "faq";

    /// <summary>
    /// The top of the page. Not an element id: the buttons carry their target as <c>data-scroll-to</c> so the
    /// prerendered page's own script (index.html) can scroll before Blazor has started.
    /// </summary>
    public const string Top = "top";

    /// <summary>What gets focus (wide screens only) when a button scrolls to the form.</summary>
    public const string EmailFieldSelector = "input[type=email]";

    /// <summary>The public legal pages (docs/legal/, served by the API).</summary>
    public const string PrivacyUrl = "https://api.tarbaho.com/api/legal/privacy";
    public const string TermsUrl = "https://api.tarbaho.com/api/legal/terms";

    /// <summary>The public TestFlight link: anyone on an iPhone can install the beta now, no invite needed.</summary>
    public const string TestFlightUrl = "https://testflight.apple.com/join/1CNn1Ywt";

    /// <summary>The page's own address, shared by the Share buttons.</summary>
    public const string SiteUrl = "https://tarbaho.com/";
}
