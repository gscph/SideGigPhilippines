namespace SideGigPh.Maui.Services;

/// <summary>
/// The app's own pictures: one small SVG per trade, job, answer and interview question, shipped inside the app
/// (<c>wwwroot/images/icons/</c>), so they show offline and never depend on the phone's emoji font. All share one look:
/// a 48-unit box, ink outlines, a light brand tint and one brand-color detail (the colors are written in the files and
/// must match <c>AppTheme</c>). <see cref="For"/> picks the picture by id, because ids are stable; the emoji the
/// server's config still carries is for app versions before these pictures and is not shown.
/// </summary>
public static class AppImages
{
    private const string Folder = "images/icons/";

    // Interview questions and other full-screen steps (InterviewStep.Illustration).
    public static readonly string Trade = File("toolbox");
    public static readonly string Jobs = File("checklist");
    public static readonly string Town = File("pin");
    public static readonly string Experience = File("medal");
    public static readonly string Price = File("money");
    public static readonly string Preview = File("profile-card");
    public static readonly string Publish = File("megaphone");
    public static readonly string Promise = File("checklist");
    public static readonly string Selfie = File("selfie");
    public static readonly string SelfieGuide = File("selfie-guide");
    public static readonly string Number = File("phone");
    public static readonly string WorkPhotos = File("photos");
    public static readonly string Role = File("person");
    public static readonly string HomeRepair = File("home-repair");
    public static readonly string Job = File("wrench");
    public static readonly string When = File("calendar");
    public static readonly string Budget = File("wallet");
    public static readonly string Report = File("flag");
    public static readonly string Idea = File("bulb");
    public static readonly string Done = File("done");
    public static readonly string FindBy = File("people");

    // Answers that live with the code.
    public static readonly string Worker = File("worker");
    public static readonly string Home = File("home");
    public static readonly string More = File("more");
    public static readonly string Unsure = File("unsure");
    public static readonly string Chat = File("chat");
    public static readonly string Custom = File("edit");
    public static readonly string Scam = File("alert");
    public static readonly string Harassment = File("chat-alert");
    public static readonly string Fake = File("fake-id");
    public static readonly string Blocked = File("blocked");
    public static readonly string Employment = File("briefcase");
    public static readonly string SearchNow = File("search");
    public static readonly string PostRequest = File("megaphone");

    /// <summary>The picture for a trade, job or answer id; "Others" jobs and ids without their own picture get <see cref="More"/>.</summary>
    public static string For(string id) => ById.TryGetValue(id, out var name) ? File(name) : More;

    /// <summary>No picture, or one of the app's: false for the emoji in a config saved by an app version before these pictures.</summary>
    public static bool IsImage(string? icon) => icon is null || icon.StartsWith(Folder, StringComparison.Ordinal);

    private static string File(string name) => $"{Folder}{name}.svg";

    private static readonly Dictionary<string, string> ById = new()
    {
        // Trades
        ["aircon"] = "aircon",
        ["construction"] = "construction",
        ["car_mechanic"] = "mechanic",
        ["errand_runner"] = "errand",
        ["pest_control"] = "pest-control",
        ["driver"] = "driver",

        // Jobs
        ["aircon-cleaning"] = "clean",
        ["aircon-repair"] = "thermometer",
        ["aircon-leak"] = "drop",
        ["aircon-freon"] = "gas",
        ["aircon-install"] = "drill",
        ["aircon-relocate"] = "truck",
        ["aircon-pms"] = "checklist",
        ["construction-foreman"] = "foreman",
        ["construction-carpentry"] = "carpentry",
        ["construction-electrical"] = "electrical",
        ["construction-plumbing"] = "plumbing",
        ["construction-masonry"] = "masonry",
        ["construction-tiles"] = "tiles",
        ["construction-welding"] = "welder",
        ["construction-roofing"] = "roofing",
        ["construction-steelman"] = "rebar",
        ["construction-helper"] = "worker",
        ["mechanic-suspension"] = "wheel",
        ["mechanic-engine"] = "gear",
        ["mechanic-electronics"] = "battery",
        ["errand-house-cleaning"] = "house-clean",
        ["errand-grocery"] = "cart",
        ["errand-bills"] = "receipt",
        ["errand-pasabuy"] = "bag",
        ["errand-documents"] = "document",
        ["errand-parcel"] = "box",
        ["errand-car-cleaning"] = "car-wash",
        ["errand-gardening"] = "garden",
        ["pest-termites"] = "termite",
        ["pest-cockroaches"] = "cockroach",
        ["pest-rats"] = "rat",
        ["pest-mosquitoes"] = "mosquito",
        ["pest-bedbugs"] = "bedbug",
        ["pest-ants"] = "ant",
        ["pest-general"] = "spray",
        ["driver-family"] = "family-car",
        ["driver-out-of-town"] = "road-trip",
        ["driver-airport"] = "airplane",
        ["driver-motorcycle"] = "motorcycle",

        // Experience
        ["under-1"] = "level-1",
        ["1-3"] = "level-2",
        ["3-10"] = "level-3",
        ["10-plus"] = "level-4",

        // Urgency
        ["today"] = "bolt",
        ["this-week"] = "calendar",
        ["canvassing"] = "search",
    };
}
