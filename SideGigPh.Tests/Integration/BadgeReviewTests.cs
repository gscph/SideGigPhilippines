using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// The operator's badge review from the app (<see cref="BadgeReview"/> on the real API): only an operator sees the
/// way in and the queue, the proof photo loads on demand, and a decision reaches the worker's own badge list (a
/// rejection with its reason). Before this screen the operator had only SQL and raw HTTP.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class BadgeReviewTests(ApiTestHost api)
{
    private static readonly PhotoPick Proof = new(PhotoPickStatus.Picked, [TestImages.Jpeg()]);

    private TestPhone OperatorPhone() => new(api, "Ops", uid: ApiTestHost.AdminUid);

    [IntegrationFact]
    public async Task OnlyAnOperator_SeesTheWayInAndTheQueue()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Rodel");
        var review = worker.Get<BadgeReview>();

        (await review.IsOperatorAsync()).Should().BeFalse();
        await review.LoadAsync();
        review.Pending.Should().BeEmpty("the server refuses anyone else, and a refused read comes back empty");

        (await OperatorPhone().Get<BadgeReview>().IsOperatorAsync()).Should().BeTrue();
    }

    [IntegrationFact]
    public async Task TheOperator_SeesTheRequestAndItsPhoto_AndApproving_GivesTheWorkerTheBadge()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Ernie");
        await worker.Pro.PublishAsync();
        (await worker.Badges.SubmitAsync(new Badge(BadgeKind.Id), consent: true, Proof)).Should().Be(BadgeSubmitResult.Sent);
        var review = OperatorPhone().Get<BadgeReview>();

        await review.LoadAsync();
        var request = review.Pending.Should().ContainSingle(r => r.WorkerUid == worker.Uid).Which;
        request.Badge.Kind.Should().Be(BadgeKind.Id);
        request.WorkerName.Should().Be("Ernie");
        request.PhotoUrl.Should().NotBeNullOrEmpty("the operator compares it with the proof");
        request.HasDocument.Should().BeTrue();
        (await review.DocumentAsync(request)).Should().StartWith("data:image/jpeg;base64,");

        (await review.DecideAsync(request, approve: true, reason: null)).Should().BeTrue();

        review.Pending.Should().NotContain(r => r.Id == request.Id);
        await worker.Badges.LoadAsync();
        worker.Badges.StateOf(BadgeKind.Id).Should().Be(BadgeState.Approved);
    }

    [IntegrationFact]
    public async Task ARejection_NeedsAReason_WhichTheWorkerSees()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Jomar");
        (await worker.Badges.SubmitAsync(new Badge(BadgeKind.Id), consent: true, Proof)).Should().Be(BadgeSubmitResult.Sent);
        var review = OperatorPhone().Get<BadgeReview>();
        await review.LoadAsync();
        var request = review.Pending.Single(r => r.WorkerUid == worker.Uid);

        (await review.DecideAsync(request, approve: false, reason: "  ")).Should().BeFalse("nothing is sent without a reason");
        review.Pending.Should().Contain(r => r.Id == request.Id);

        (await review.DecideAsync(request, approve: false, reason: "  The photo is blurry.  ")).Should().BeTrue();

        await worker.Badges.LoadAsync();
        worker.Badges.StateOf(BadgeKind.Id).Should().Be(BadgeState.Rejected);
        worker.Badges.Of(BadgeKind.Id)!.RejectReason.Should().Be("The photo is blurry.");
    }
}
