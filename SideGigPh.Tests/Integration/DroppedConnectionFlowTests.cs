using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The connection drops once on the way, as it does after the camera or the photo picker (the server closed the kept
/// connection while the person was away): the worker's selfie and the household's post still go through, once, and the
/// app never says "offline" on a working connection. Testers on two Android phones saw "No internet" on the selfie on
/// 22 Nov 2026 because writes were sent only once.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class DroppedConnectionFlowTests(ApiTestHost api)
{
    private static OnboardingState WorkerInTheInterview() => TestData.FinishedPro(new ProfessionalDraft
    {
        CategoryId = "aircon",
        ServiceIds = ["aircon-cleaning"],
        CityIds = ["taytay"],
        AreaId = ReferenceData.ServiceAreaId,
        ExperienceId = "3-10",
        RatePesos = 800,
    });

    [IntegrationFact]
    public async Task Selfie_WhenTheConnectionDropsOnce_IsSaved_AndTheAppStaysOnline()
    {
        var worker = new TestPhone(api, "Selfie", state: WorkerInTheInterview());
        await worker.Pro.LoadInterviewDraftAsync();
        worker.Connection.DropWrites = 1;

        var result = await worker.Pro.SetProfilePhotoAsync(new PhotoPick(PhotoPickStatus.Picked, [TestImages.Jpeg()]));

        result.Should().Be(new PhotoAddResult(1), "the upload goes once more on a fresh connection");
        worker.Pro.Draft.ProfilePhotoId.Should().NotBeNull();
        api.Blobs.Blobs.Should().ContainKey($"{worker.Pro.Draft.ProfilePhotoId}.jpg");
        worker.Network.IsOnline.Should().BeTrue("a dropped connection that worked on the next try is not offline");
    }

    [IntegrationFact]
    public async Task JobPost_WhenTheConnectionDropsOnce_IsPostedOnce()
    {
        var household = await TestPhone.SeekerAsync(api, "Poster");
        var tag = $"zq{Guid.NewGuid():N}"[..10];
        household.Connection.DropWrites = 1;

        var board = household.Get<JobBoard>();
        (await board.PostAsync(new JobPostDraft
        {
            CategoryId = "construction",
            ServiceId = "construction-plumbing",
            CityId = "taytay",
            BudgetPesos = 800,
            Title = "Barado ang lababo",
            Description = $"Barado ang lababo sa kusina {tag}",
        })).Should().Be(JobPostResult.Posted);

        await board.LoadMineAsync();
        board.Mine.Where(p => p.Description.EndsWith(tag)).Should().ContainSingle("a resend never makes a second post");
        household.Network.IsOnline.Should().BeTrue();
    }

    [IntegrationFact]
    public async Task Selfie_WhenEveryTryDrops_SaysNoInternet_KeepsThePhotoToTryAgain_ThenSaves()
    {
        var worker = new TestPhone(api, "Offline", state: WorkerInTheInterview());
        await worker.Pro.LoadInterviewDraftAsync();
        var pick = new PhotoPick(PhotoPickStatus.Picked, [TestImages.Jpeg()]);
        worker.Connection.DropWrites = int.MaxValue; // really offline

        var result = await worker.Pro.SetProfilePhotoAsync(pick);

        result.Added.Should().Be(0);
        result.CanRetry.Should().BeTrue("Try again sends the same photo");
        worker.Network.IsOnline.Should().BeFalse("nothing got through: this one really is offline");

        worker.Connection.DropWrites = 0; // back online: Try again
        (await worker.Pro.SetProfilePhotoAsync(pick)).Should().Be(new PhotoAddResult(1));
        worker.Network.IsOnline.Should().BeTrue();
    }
}
