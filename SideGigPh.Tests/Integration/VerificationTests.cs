using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SideGigPh.AzureSQL;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Worker verification end to end on the real API and database: a worker sends an ID with consent, the AI read helps
/// the operator, the operator decides, the badge shows on the listing, and the photo is gone afterwards (only the
/// result is kept). Nobody but an operator sees a document, and every look is logged.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class VerificationTests(ApiTestHost api)
{
    private HttpClient Operator => api.Client(api.Tokens.For(ApiTestHost.AdminUid));

    private static ByteArrayContent Photo() => new(TestImages.Jpeg());

    [IntegrationFact]
    public async Task ApprovedId_IsABadgeOnTheListing_AndThePhotoIsDeleted()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Vina");
        await worker.Pro.PublishAsync();
        var client = api.Client(api.Tokens.For(worker.Uid, worker.PhoneNumber));
        api.IdReader.Next = new IdScan(ScanStatuses.Read, "idDocument.nationalIdentityCard", "VINA SANTOS", "1234-5678", new DateOnly(2031, 5, 1));

        using var sent = await client.PostAsync("verifications/id?consent=true", Photo());
        sent.StatusCode.Should().Be(HttpStatusCode.OK);
        var request = (await sent.Content.ReadFromJsonAsync(ApiJson.Default.VerificationDto))!;
        request.Status.Should().Be(VerificationStatuses.Pending);
        api.PrivateFiles.Files.Keys.Should().Contain(name => name.StartsWith(worker.Uid + "/"));

        // The operator sees what the AI read and the worker's profile photo, and looks at the document.
        var queue = (await Operator.GetFromJsonAsync("ops/verifications?take=50", ApiJson.Default.ListAdminVerificationDto))!;
        var waiting = queue.Single(v => v.Id == request.Id);
        waiting.ScannedName.Should().Be("VINA SANTOS");
        waiting.ScannedExpiry.Should().Be(new DateOnly(2031, 5, 1));
        waiting.ProfilePhotoUrl.Should().NotBeNull();
        using var document = await Operator.GetAsync($"ops/verifications/{request.Id}/document");
        document.StatusCode.Should().Be(HttpStatusCode.OK);
        document.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");

        using var decided = await Operator.PostAsJsonAsync($"ops/verifications/{request.Id}/decision", new VerificationDecisionRequest(true), ApiJson.Default.VerificationDecisionRequest);
        decided.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The badge is on the listing; the photo and everything read from it are gone; the log says who did what.
        var listing = await new TestPhone(api, "Viewer").Get<IProfessionalProfileRepository>().GetByIdAsync(worker.Uid);
        listing.Should().NotBeNull();
        var dto = (await client.GetFromJsonAsync($"professionals/{worker.Uid}", ApiJson.Default.ListingDto))!;
        dto.Badges.Should().ContainSingle().Which.Kind.Should().Be(VerificationKinds.Id);
        api.PrivateFiles.Files.Keys.Should().NotContain(name => name.StartsWith(worker.Uid + "/"));
        using var scope = api.Server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SideGigPhDb>();
        var row = await db.WorkerVerifications.SingleAsync(v => v.Id == request.Id);
        (row.DocumentBlob, row.ScannedName, row.ScannedNumber, row.ScannedExpiry).Should().Be((null, null, null, null));
        (await db.VerificationAudit.Where(a => a.VerificationId == request.Id).OrderBy(a => a.Id).Select(a => a.Action).ToListAsync())
            .Should().Equal("submitted", "scanned", "viewed", "approved", "deleted");
        (await client.GetFromJsonAsync("verifications/mine", ApiJson.Default.ListVerificationDto))!
            .Single().Status.Should().Be(VerificationStatuses.Approved);
    }

    [IntegrationFact]
    public async Task FromTheApp_ASentProofIsBeingChecked_ThenTheBadgeShowsAndMovesTheWorkerUp()
    {
        // Drivers only in this test. Same listing for both, and "Zaldy" sorts after "Abner": only his badge can put him first.
        var plain = await TestPhone.WorkerReadyToPublishAsync(api, "Abner", trade: "driver", jobs: ["driver-airport"]);
        await plain.Pro.PublishAsync();
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Zaldy", trade: "driver", jobs: ["driver-airport"]);
        await worker.Pro.PublishAsync();
        var photo = new PhotoPick(PhotoPickStatus.Picked, [TestImages.Jpeg()]);

        (await worker.Badges.SubmitAsync(new Badge(BadgeKind.Project, BadgeCatalog.OtherCompany, "JM Builders"), consent: true, photo)).Should().Be(BadgeSubmitResult.Sent);
        (await worker.Badges.SubmitAsync(new Badge(BadgeKind.Project, BadgeCatalog.OtherCompany, "JM Builders"), consent: true, photo)).Should().Be(BadgeSubmitResult.AlreadySent);
        worker.Badges.StateOf(BadgeKind.Project).Should().Be(BadgeState.Pending);

        var request = worker.Badges.Of(BadgeKind.Project).Single();
        using var decided = await Operator.PostAsJsonAsync($"ops/verifications/{request.Id}/decision", new VerificationDecisionRequest(true), ApiJson.Default.VerificationDecisionRequest);
        decided.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await worker.Badges.LoadAsync();
        worker.Badges.ApprovedKinds.Should().Be(1);

        var answers = TestData.AirconCleaningInTaytay();
        answers.CategoryId = "driver";
        answers.ServiceId = "driver-airport";
        var results = await (await TestPhone.SeekerAsync(api, "Bina", answers)).Seeker.SearchAsync();

        results.InTown.Select(h => h.Pro.Id).Should().Equal(worker.Uid, plain.Uid);
        results.InTown[0].Pro.Badges.Should().Equal(new Badge(BadgeKind.Project, BadgeCatalog.OtherCompany, "JM Builders"));
    }

    [IntegrationFact]
    public async Task WithoutConsent_NothingIsStoredOrRead()
    {
        var uid = $"noconsent-{Guid.NewGuid():N}";
        var reads = api.IdReader.Reads;

        using var response = await api.Client(api.Tokens.For(uid)).PostAsync("verifications/id", Photo());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.ConsentRequired);
        api.PrivateFiles.Files.Keys.Should().NotContain(name => name.StartsWith(uid + "/"));
        api.IdReader.Reads.Should().Be(reads);
    }

    [IntegrationFact]
    public async Task TheSameBadgeTwice_IsAlreadySent_UntilRejected()
    {
        var uid = $"twice-{Guid.NewGuid():N}";
        var client = api.Client(api.Tokens.For(uid));
        using var first = await client.PostAsync("verifications/tesda?consent=true", Photo());
        var request = (await first.Content.ReadFromJsonAsync(ApiJson.Default.VerificationDto))!;

        using var second = await client.PostAsync("verifications/tesda?consent=true", Photo());
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Rejected with a reason the worker sees: they can send a better photo.
        using var rejected = await Operator.PostAsJsonAsync($"ops/verifications/{request.Id}/decision",
            new VerificationDecisionRequest(false, "The certificate number can't be read"), ApiJson.Default.VerificationDecisionRequest);
        rejected.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync("verifications/mine", ApiJson.Default.ListVerificationDto))!
            .Single().RejectReason.Should().Be("The certificate number can't be read");
        using var again = await client.PostAsync("verifications/tesda?consent=true", Photo());
        again.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [IntegrationFact]
    public async Task ClearanceBadge_IsStored_NeverReadByAi_AndShowsOnceApproved()
    {
        var uid = $"clear-{Guid.NewGuid():N}";
        var client = api.Client(api.Tokens.For(uid));
        var reads = api.IdReader.Reads;

        using var sent = await client.PostAsync("verifications/clearance?consent=true", Photo());
        sent.StatusCode.Should().Be(HttpStatusCode.OK);
        var request = (await sent.Content.ReadFromJsonAsync(ApiJson.Default.VerificationDto))!;
        request.Kind.Should().Be(VerificationKinds.Clearance);
        request.BadgeId.Should().BeEmpty();
        api.IdReader.Reads.Should().Be(reads, "only a government ID is read by AI");

        using var approved = await Operator.PostAsJsonAsync($"ops/verifications/{request.Id}/decision",
            new VerificationDecisionRequest(true), ApiJson.Default.VerificationDecisionRequest);
        approved.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync("verifications/mine", ApiJson.Default.ListVerificationDto))!
            .Single().Status.Should().Be(VerificationStatuses.Approved);
    }

    [IntegrationFact]
    public async Task DeveloperBadge_NeedsAKnownDeveloper_AndIsNeverReadByAi()
    {
        var uid = $"dev-{Guid.NewGuid():N}";
        var client = api.Client(api.Tokens.For(uid));
        var reads = api.IdReader.Reads;

        using var unknown = await client.PostAsync("verifications/developer?consent=true&badgeId=acme", Photo());
        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var missing = await client.PostAsync("verifications/developer?consent=true", Photo());
        missing.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var notABadge = await client.PostAsync("verifications/selfie?consent=true", Photo());
        notABadge.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var sent = await client.PostAsync($"verifications/developer?consent=true&badgeId={DeveloperBadges.AyalaLand}&projectName=Park%20Terraces", Photo());
        var request = (await sent.Content.ReadFromJsonAsync(ApiJson.Default.VerificationDto))!;
        (request.BadgeId, request.ProjectName).Should().Be((DeveloperBadges.AyalaLand, "Park Terraces"));
        using var other = await client.PostAsync($"verifications/developer?consent=true&badgeId={DeveloperBadges.Dmci}", Photo());
        other.StatusCode.Should().Be(HttpStatusCode.OK, "each developer is its own badge");

        // Any other company, typed by the worker: the name is required, and each company is its own badge.
        using var nameless = await client.PostAsync($"verifications/developer?consent=true&badgeId={DeveloperBadges.Other}", Photo());
        nameless.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var local = await client.PostAsync($"verifications/developer?consent=true&badgeId={DeveloperBadges.Other}&companyName=JM%20Builders", Photo());
        (await local.Content.ReadFromJsonAsync(ApiJson.Default.VerificationDto))!.CompanyName.Should().Be("JM Builders");
        using var sameCompany = await client.PostAsync($"verifications/developer?consent=true&badgeId={DeveloperBadges.Other}&companyName=JM%20Builders", Photo());
        sameCompany.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var anotherCompany = await client.PostAsync($"verifications/developer?consent=true&badgeId={DeveloperBadges.Other}&companyName=Rizal%20Homes", Photo());
        anotherCompany.StatusCode.Should().Be(HttpStatusCode.OK);
        api.IdReader.Reads.Should().Be(reads, "only a government ID is read by AI");
    }

    [IntegrationFact]
    public async Task OnlyAnOperator_SeesTheQueueTheDocumentsAndDecides()
    {
        var uid = $"nosy-{Guid.NewGuid():N}";
        var client = api.Client(api.Tokens.For(uid));
        using var sent = await client.PostAsync("verifications/id?consent=true", Photo());
        var request = (await sent.Content.ReadFromJsonAsync(ApiJson.Default.VerificationDto))!;

        // Not even the worker who sent it gets the document back.
        foreach (var call in new Func<Task<HttpResponseMessage>>[]
        {
            () => client.GetAsync("ops/verifications"),
            () => client.GetAsync($"ops/verifications/{request.Id}/document"),
            () => client.PostAsJsonAsync($"ops/verifications/{request.Id}/decision", new VerificationDecisionRequest(true), ApiJson.Default.VerificationDecisionRequest),
        })
        {
            using var response = await call();
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.NotAdmin);
        }

        (await client.GetFromJsonAsync("verifications/mine", ApiJson.Default.ListVerificationDto))!.Single().Status.Should().Be(VerificationStatuses.Pending);
    }

    [IntegrationFact]
    public async Task DeleteAccount_RemovesTheRequestsAndTheWaitingPhoto()
    {
        var worker = new TestPhone(api, "Gone");
        var client = api.Client(api.Tokens.For(worker.Uid));
        using var sent = await client.PostAsync("verifications/id?consent=true", Photo());
        sent.StatusCode.Should().Be(HttpStatusCode.OK);

        using var deleted = await client.DeleteAsync("users/me/account");

        deleted.IsSuccessStatusCode.Should().BeTrue();
        api.PrivateFiles.Files.Keys.Should().NotContain(name => name.StartsWith(worker.Uid + "/"));
        using var scope = api.Server.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<SideGigPhDb>().WorkerVerifications.AnyAsync(v => v.Uid == worker.Uid)).Should().BeFalse();
    }
}
