using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SideGigPh.Contracts;
using SideGigPh.FrontDoor.EarlyAccess;
using SideGigPh.FrontDoor.Localization;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>
/// The Early Access form: which phone (Android or iPhone: it decides the invite and which email to ask for), "I am a"
/// (optional, filled from "How it works"), the email, one button. The email is checked on the phone first (empty, wrong
/// shape, a "did you mean @gmail.com?" for common typos), then sent once. Every outcome has its own words: sending, done,
/// already on the list (this browser's memory), didn't go through, too many tries, no internet; nothing typed is lost.
/// </summary>
public partial class EarlyAccessForm
{
    [Inject] private IEarlyAccessSignup Signups { get; set; } = default!;
    [Inject] private SignupState Signup { get; set; } = default!;
    [Inject] private PageScript Script { get; set; } = default!;

    private static readonly Dictionary<string, object?> EmailAttributes = new()
    {
        ["autocomplete"] = "email",
        ["autocapitalize"] = "off",
        ["spellcheck"] = "false",
        ["enterkeyhint"] = "send",
    };

    private static readonly Dictionary<string, object?> TrapAttributes = new()
    {
        ["tabindex"] = "-1",
        ["autocomplete"] = "off",
        ["aria-hidden"] = "true",
    };

    private readonly EditContext _context = new(new object());

    private string? _phone;
    private string? _role;
    private bool _roleChangedHere;
    private string _email = "";
    private string? _trap;
    private bool _submitted;
    private bool _phoneMissing;
    private string? _emailError;
    private Say? _problem;
    private bool _busy;
    private bool _justJoined;
    private bool _copied;

    /// <summary>The visitor's own pick here, else what they picked in "How it works".</summary>
    private string? Role => _roleChangedHere ? _role : Signup.SuggestedRole;

    private string EmailLabel => T(_phone switch
    {
        EarlyAccessRules.Android => Copy.Join.EmailLabelAndroid,
        _ => Copy.Join.EmailLabel,
    });

    private string? EmailHelp => _phone switch
    {
        EarlyAccessRules.Android => T(Copy.Join.EmailHelpAndroid),
        EarlyAccessRules.IPhone => T(Copy.Join.EmailHelpIphone),
        _ => null,
    };

    private bool IsIphone => _phone == EarlyAccessRules.IPhone;

    private string? Suggestion =>_busy ? null : EarlyAccessRules.SuggestFix(_email);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Signup.Changed += OnSignupChanged;
    }

    private void PickPhone(string phone)
    {
        _phone = phone;
        _phoneMissing = false;
    }

    private void PickRole(string? role)
    {
        _role = role;
        _roleChangedHere = true;
    }

    private void EmailChanged(string email)
    {
        _email = email;
        if (_submitted)
        {
            _emailError = EmailProblem();  // live once they tried to send
        }
    }

    private void UseSuggestion(string meant)
    {
        _email = meant;
        _emailError = _submitted ? EmailProblem() : null;
    }

    private string? EmailProblem() =>
        string.IsNullOrWhiteSpace(_email) ? T(Copy.Join.Empty)
        : EarlyAccessRules.IsValidEmail(_email) ? null
        : T(Copy.Join.Invalid);

    private async Task SubmitAsync()
    {
        if (_busy)
        {
            return;
        }

        _submitted = true;
        _problem = null;
        _phoneMissing = _phone is null;
        _emailError = EmailProblem();
        if (_phoneMissing || _emailError is not null)
        {
            return;
        }

        // Busy before the first await: a quick double tap (or Enter and a tap) must send one sign-up, one email.
        _busy = true;
        try
        {
            if (!await Script.IsOnlineAsync())
            {
                _problem = Copy.Join.Offline;
                return;
            }

            var email = EarlyAccessRules.Normalize(_email);
            var result = await Signups.JoinAsync(new EarlyAccessRequest(email, _phone!, Role, Language.Current, _trap));
            switch (result)
            {
                case SignupResult.Sent:
                    _justJoined = true;
                    await Script.WriteAsync(PageScript.JoinedKey, email);
                    Signup.SetJoined(email);
                    break;
                case SignupResult.InvalidEmail:
                    _emailError = T(Copy.Join.Invalid);
                    break;
                case SignupResult.TooManyTries:
                    _problem = Copy.Join.TooMany;
                    break;
                case SignupResult.Offline:
                    _problem = Copy.Join.Offline;
                    break;
                default:
                    _problem = Copy.Join.Error;
                    break;
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ShareAsync()
    {
        _copied = await Script.ShareAsync("Tarbaho", T(Copy.Join.ShareText), Sections.SiteUrl) == "copied";
    }

    private async Task AnotherEmailAsync()
    {
        _email = "";
        _submitted = false;
        _emailError = null;
        _justJoined = false;
        _copied = false;
        await Script.WriteAsync(PageScript.JoinedKey, null);
        Signup.SetJoined(null);
    }

    private void OnSignupChanged() => InvokeAsync(StateHasChanged);

    public override void Dispose()
    {
        Signup.Changed -= OnSignupChanged;
        base.Dispose();
    }
}
