using Microsoft.Extensions.Logging.Abstractions;
using MobileAppTheme.Services.Local;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Maui.Services.Analytics;
using SideGigPh.Maui.Services.Api;
using SideGigPh.Maui.Services.Search;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Integration.Support;

/// <summary>
/// One person's phone running the app's real services, wired like <c>MauiProgram</c> (the same
/// <c>AddSideGigPhApi</c> registrations), talking to the in-memory API over HTTP with a real signed token.
/// Only the parts that need a real phone are stand-ins: the saved answers file, the gallery and the dialer.
/// </summary>
public sealed class TestPhone
{
    // Each test run has a fresh database, so counting up is enough to keep numbers unique.
    private static readonly long Base = Random.Shared.Next(100_000_000, 800_000_000);
    private static int _nextNumber;

    private readonly ServiceProvider _services;

    public TestPhone(ApiTestHost api, string name, bool verifiedPhone = true, OnboardingState? state = null, string? uid = null)
    {
        Uid = uid ?? $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}";
        PhoneNumber = verifiedPhone ? UniqueNumber() : null;
        Push.Token = $"fcm-{Uid}";
        Identity = new FakeIdentity(new AuthUser { Uid = Uid, DisplayName = name, PhoneNumber = PhoneNumber });
        Identity.TokenFor = user => api.Tokens.For(user.Uid, user.PhoneNumber);
        Store = new FakeOnboardingStore(state ?? new OnboardingState());
        Connection = new DroppingConnection(api.Server.CreateHandler());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IIdentityProvider>(Identity);
        services.AddSingleton<IOnboardingStore>(Store);
        services.AddSingleton<IPhotoPicker>(Picker);
        services.AddSingleton<IContactLauncher>(Dialer);
        services.AddSingleton<IPushNotifications>(Push);
        services.AddSingleton<ChatAlerts>();
        services.AddSingleton<INetworkStatus>(Network);
        services.AddSingleton<AppText>();
        services.AddSideGigPhApi(new ApiOptions(new Uri(api.Server.BaseAddress, "api/").ToString(), $"{InMemoryBlobContainer.ContainerUri}/", Connection));
        services.AddSingleton<ProfessionalSearch>();
        services.AddSingleton<FeedbackBoard>();
        services.AddSingleton<JobBoard>();
        services.AddSingleton<PickedContact>();
        services.AddSingleton<ProWorkspace>();
        services.AddSingleton<ListingRestore>();
        services.AddSingleton<ProBadges>();
        services.AddSingleton<BadgeReview>();
        services.AddSingleton<IPendingEventStore>(PendingEvents);
        services.AddSingleton<TarbahoAnalytics>();
        services.AddSingleton<AnalyticsUploader>();
        services.AddSingleton<SeekerWorkspace>();
        services.AddSingleton(provider => new LocalAccount(
            Store, Identity, provider.GetRequiredService<IUserRepository>(), provider.GetRequiredService<IPhotoStore>(),
            provider.GetRequiredService<IProfessionalProfileRepository>(), provider.GetRequiredService<IChatRepository>(),
            provider.GetRequiredService<IReviewRepository>(), provider.GetRequiredService<IJobPostRepository>(), provider.GetRequiredService<ProWorkspace>(),
            provider.GetRequiredService<SeekerWorkspace>(), provider.GetRequiredService<AppText>(), NullLogger<LocalAccount>.Instance));
        _services = services.BuildServiceProvider();
    }

    public string Uid { get; }

    public string? PhoneNumber { get; }

    public FakeIdentity Identity { get; }

    /// <summary>The phone's connection to the API: set <see cref="DroppingConnection.DropWrites"/> to break the next writes.</summary>
    public DroppingConnection Connection { get; }

    /// <summary>What the app shows as online / offline (the real <c>DeviceNetworkStatus</c>; no platform here, so connected).</summary>
    public DeviceNetworkStatus Network { get; } = new();

    public FakeOnboardingStore Store { get; }

    public FakePhotoPicker Picker { get; } = new();

    public FakeContactLauncher Dialer { get; } = new();

    /// <summary>The usage events waiting on this phone (<see cref="TarbahoAnalytics"/>).</summary>
    public FakePendingEvents PendingEvents { get; } = new();

    /// <summary>This phone's notifications: allowed, so <see cref="ChatAlerts"/> registers it with the API.</summary>
    public FakePush Push { get; } = new() { Allowed = true };

    public ProWorkspace Pro => _services.GetRequiredService<ProWorkspace>();

    public ProBadges Badges => _services.GetRequiredService<ProBadges>();

    public SeekerWorkspace Seeker => _services.GetRequiredService<SeekerWorkspace>();

    public LocalAccount Account => _services.GetRequiredService<LocalAccount>();

    public T Get<T>() where T : notnull => _services.GetRequiredService<T>();

    /// <summary>
    /// Taps Call on the chat with <paramref name="otherUid"/> (<see cref="PickedContact"/>): the number the dialer got,
    /// or null when no pick links the two (the page never sees the number itself).
    /// </summary>
    public async Task<string?> DialedAsync(string otherUid) =>
        await Get<PickedContact>().CallAsync(otherUid) == ContactOutcome.Opened ? Dialer.Called[^1] : null;

    /// <summary>A Philippine mobile number no other test person has.</summary>
    public static string UniqueNumber() => $"+639{(Base + Interlocked.Increment(ref _nextNumber)) % 1_000_000_000:D9}";

    /// <summary>
    /// A Skilled Worker who finished the interview and uploaded 3 photos (own + 2 work) through the API: everything
    /// Publish needs. Returns the phone with Pro home loaded.
    /// </summary>
    public static async Task<TestPhone> WorkerReadyToPublishAsync(ApiTestHost api, string name, string trade = "aircon",
        string[]? jobs = null, string[]? towns = null, int rate = 800)
    {
        var draft = new ProfessionalDraft
        {
            CategoryId = trade,
            ServiceIds = [.. jobs ?? ["aircon-cleaning"]],
            CityIds = [.. towns ?? ["taytay"]],
            AreaId = ReferenceData.ServiceAreaId,
            ExperienceId = "3-10",
            RatePesos = rate,
        };
        var phone = new TestPhone(api, name, state: TestData.FinishedPro(draft));
        (await phone.Pro.EnsureLoadedAsync()).Should().BeTrue();

        phone.Picker.Next = new PhotoPick(PhotoPickStatus.Picked, [TestImages.Jpeg()]);
        (await phone.Pro.SetProfilePhotoAsync(await phone.Picker.TakeSelfieAsync())).Failed.Should().BeFalse();
        phone.Picker.Next = new PhotoPick(PhotoPickStatus.Picked, [TestImages.Jpeg(), TestImages.Jpeg()]);
        // The whole result, so a failure says why (Rejected or ServerError = the server refused).
        (await phone.Pro.AddWorkPhotosAsync()).Should().Be(new PhotoAddResult(2));
        return phone;
    }

    /// <summary>A Seeker who finished the interview (aircon cleaning in Taytay, today, ₱500–1,000), with Seeker home loaded.</summary>
    public static async Task<TestPhone> SeekerAsync(ApiTestHost api, string name, SeekerAnswers? answers = null)
    {
        var phone = new TestPhone(api, name, verifiedPhone: false, state: TestData.FinishedSeeker(answers ?? TestData.AirconCleaningInTaytay()));
        (await phone.Seeker.EnsureLoadedAsync()).Should().BeTrue();
        return phone;
    }
}

/// <summary>Real, different-every-time photos (a random colour), as the phone's picker would hand them over.</summary>
public static class TestImages
{
    public static byte[] Jpeg()
    {
        // A random colour and size, like real photos, with a fresh Guid drawn as gray pixels across the top rows so
        // every photo is different by construction.
        var color = new ImageMagick.MagickColor((byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256));
        using var image = new ImageMagick.MagickImage(color, (uint)Random.Shared.Next(40, 400), (uint)Random.Shared.Next(30, 300)) { Format = ImageMagick.MagickFormat.Jpeg };
        using var pixels = image.GetPixels();
        var stamp = Guid.NewGuid().ToByteArray();
        for (var i = 0; i < stamp.Length; i++)
        {
            // 2x2 blocks, so the stamp survives JPEG's smoothing.
            var level = (ushort)(stamp[i] * 257); // the Q16 build: 0..65535
            var gray = new[] { level, level, level };
            for (var dy = 0; dy < 2; dy++)
            {
                pixels.SetPixel(i * 2, dy, gray);
                pixels.SetPixel(i * 2 + 1, dy, gray);
            }
        }

        return image.ToByteArray();
    }

    /// <summary>
    /// A photo of a paper (white, portrait, like a clearance shot from above) with a QR code holding
    /// <paramref name="qrText"/> printed on its lower right, as a JPEG.
    /// </summary>
    public static byte[] PaperWithQr(string qrText)
    {
        const int qrSide = 240;
        var matrix = new ZXing.QrCode.QRCodeWriter().encode(qrText, ZXing.BarcodeFormat.QR_CODE, qrSide, qrSide);
        var gray = new byte[matrix.Width * matrix.Height];
        for (var y = 0; y < matrix.Height; y++)
        {
            for (var x = 0; x < matrix.Width; x++)
            {
                gray[y * matrix.Width + x] = matrix[x, y] ? (byte)0 : (byte)255;
            }
        }

        using var qr = new ImageMagick.MagickImage(gray, new ImageMagick.MagickReadSettings
        {
            Width = (uint)matrix.Width, Height = (uint)matrix.Height, Format = ImageMagick.MagickFormat.Gray, Depth = 8,
        });
        using var paper = new ImageMagick.MagickImage(ImageMagick.MagickColors.White, 900, 1200) { Format = ImageMagick.MagickFormat.Jpeg };
        paper.Composite(qr, 900 - qrSide - 60, 1200 - qrSide - 60, ImageMagick.CompositeOperator.Over);
        return paper.ToByteArray();
    }
}
