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
    public static readonly string Selfie = File("selfie");
    public static readonly string WorkPhotos = File("photos");
    public static readonly string References = File("people");
    public static readonly string Role = File("person");
    public static readonly string HomeRepair = File("home-repair");
    public static readonly string Job = File("wrench");
    public static readonly string When = File("calendar");
    public static readonly string Budget = File("wallet");
    public static readonly string Noted = File("note-done");
    public static readonly string Report = File("flag");
    public static readonly string Idea = File("bulb");
    public static readonly string Done = File("done");

    // Answers that live with the code.
    public static readonly string Worker = File("worker");
    public static readonly string Home = File("home");
    public static readonly string More = File("more");
    public static readonly string Unsure = File("unsure");
    public static readonly string Quote = File("chat");
    public static readonly string Custom = File("edit");
    public static readonly string LowPrice = File("tag");
    public static readonly string HighPrice = File("trend");
    public static readonly string Scam = File("alert");
    public static readonly string Harassment = File("chat-alert");
    public static readonly string Fake = File("fake-id");
    public static readonly string Blocked = File("blocked");
    public static readonly string Employment = File("briefcase");

    /// <summary>The picture for a trade, job or answer id; "Others" jobs and ids without their own picture get <see cref="More"/>.</summary>
    public static string For(string id) => ById.TryGetValue(id, out var name) ? File(name) : More;

    /// <summary>No picture, or one of the app's: false for the emoji in a config saved by an app version before these pictures.</summary>
    public static bool IsImage(string? icon) => icon is null || icon.StartsWith(Folder, StringComparison.Ordinal);

    private static string File(string name) => $"{Folder}{name}.svg";

    private static readonly Dictionary<string, string> ById = new()
    {
        // Trades
        ["aircon"] = "aircon",
        ["plumbing"] = "plumbing",
        ["electrical"] = "electrical",
        ["roofing"] = "roofing",
        ["carpentry"] = "carpentry",
        ["welder"] = "welder",
        ["masonry_tiling"] = "masonry",
        ["car_mechanic"] = "mechanic",
        ["errand_runner"] = "errand",
        ["solar_cleaning"] = "solar",

        // Jobs
        ["aircon-cleaning"] = "clean",
        ["aircon-repair"] = "thermometer",
        ["aircon-leak"] = "drop",
        ["aircon-freon"] = "gas",
        ["aircon-install"] = "drill",
        ["aircon-relocate"] = "truck",
        ["aircon-pms"] = "checklist",
        ["plumbing-clog"] = "sink",
        ["plumbing-leak"] = "drop",
        ["plumbing-toilet"] = "toilet",
        ["plumbing-replace"] = "pipe",
        ["plumbing-install"] = "shower",
        ["plumbing-water-heater"] = "heater",
        ["plumbing-pump"] = "tank",
        ["plumbing-septic"] = "truck",
        ["electrical-no-power"] = "power-off",
        ["electrical-outlet"] = "outlet",
        ["electrical-lights"] = "bulb",
        ["electrical-wiring"] = "cable",
        ["electrical-breaker-panel"] = "panel",
        ["electrical-meter"] = "meter",
        ["electrical-cctv"] = "cctv",
        ["roofing-leak"] = "roof-leak",
        ["roofing-gutter"] = "gutter",
        ["roofing-sheet"] = "sheet",
        ["roofing-waterproofing"] = "shield",
        ["roofing-paint"] = "roller",
        ["carpentry-furniture"] = "chair",
        ["carpentry-door-window"] = "door",
        ["carpentry-cabinet"] = "cabinet",
        ["carpentry-ceiling-roof"] = "ceiling",
        ["carpentry-partition"] = "partition",
        ["welder-repair"] = "weld",
        ["welder-gate"] = "gate",
        ["welder-grills"] = "grill",
        ["welder-railings"] = "stairs",
        ["welder-frame"] = "truss",
        ["masonry-tiles"] = "tiles",
        ["masonry-cement"] = "trowel",
        ["masonry-crack"] = "crack",
        ["masonry-wall"] = "masonry",
        ["masonry-cr-renovation"] = "bath",
        ["mechanic-suspension"] = "wheel",
        ["mechanic-engine"] = "gear",
        ["mechanic-electronics"] = "battery",
        ["errand-grocery"] = "cart",
        ["errand-bills"] = "receipt",
        ["errand-pasabuy"] = "bag",
        ["errand-documents"] = "document",
        ["errand-parcel"] = "box",
        ["solar-cleaning"] = "clean",
        ["solar-checkup"] = "checklist",
        ["solar-inverter"] = "battery",

        // Experience
        ["under-1"] = "level-1",
        ["1-3"] = "level-2",
        ["3-10"] = "level-3",
        ["10-plus"] = "level-4",

        // Urgency
        ["today"] = "bolt",
        ["this-week"] = "calendar",
        ["canvassing"] = "search",

        // A worker's rate
        ["minimum"] = "tag",
        ["average"] = "money",
        ["above-average"] = "trend",
    };
}
