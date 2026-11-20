using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using MobileAppTheme.Auth;
using SideGigPh.Contracts;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;
using SideGigPh.Tests.Integration.Support;

namespace SideGigPh.Tests.Integration;

/// <summary>
/// Rude or sexual words are refused before anything is saved (<c>422 text-not-allowed</c>) in reviews of a worker and
/// of a household, the "Others" notes, and a listing's name and "Others" text. The word list ("gago ka") and the
/// outside service (<see cref="FakeTextModerator.Marker"/>) each refuse; the app keeps what was typed and says why.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class ReviewAndListingModerationTests(ApiTestHost api)
{
    private const string Rude = "gago ka";

    private static string Tag() => $"zm{Guid.NewGuid():N}"[..10];

    [IntegrationFact]
    public async Task Review_WithRudeWords_IsRefused_AndNothingIsSaved_ThenACleanOneIs()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Mario");
        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.Published);
        var seeker = await TestPhone.SeekerAsync(api, "Nena");
        var listing = (await seeker.Seeker.GetProAsync(worker.Uid))!;
        await seeker.Seeker.SendAsync(listing, "Hi po");
        await api.StartChatEarlierAsync(seeker.Seeker.ChatIdWith(worker.Uid), TimeSpan.FromHours(25));
        await seeker.Seeker.RefreshActivityAsync();

        var wordList = () => seeker.Seeker.SubmitReviewAsync(listing, 1, $"Ang bagal, {Rude}", "Nena");
        var service = () => seeker.Seeker.SubmitReviewAsync(listing, 1, $"Ang bagal {FakeTextModerator.Marker}", "Nena");
        var name = () => seeker.Seeker.SubmitReviewAsync(listing, 5, "Ayos", Rude);

        await wordList.Should().ThrowAsync<TextNotAllowedException>();
        await service.Should().ThrowAsync<TextNotAllowedException>();
        await name.Should().ThrowAsync<TextNotAllowedException>("the reviewer's name shows on the review too");
        (await worker.Get<IReviewRepository>().GetForProfessionalAsync(worker.Uid)).Should().BeEmpty();

        (await seeker.Seeker.SubmitReviewAsync(listing, 5, "Ayos, malinis", "Nena")).Should().BeTrue();
    }

    [IntegrationFact]
    public async Task HouseholdReview_WithRudeWords_IsRefused_BeforeTheJobIsChecked()
    {
        var worker = new TestPhone(api, "Lito");
        var household = new TestPhone(api, "Rosa");

        var rate = () => worker.Get<JobBoard>().RateHouseholdAsync(household.Uid, 1, $"Kuripot, {Rude}");
        using var flagged = await api.Client(api.Tokens.For(worker.Uid, worker.PhoneNumber)).PutAsJsonAsync("household-reviews",
            new SaveHouseholdReviewRequest(household.Uid, 1, FakeTextModerator.Marker), ApiJson.Default.SaveHouseholdReviewRequest);

        await rate.Should().ThrowAsync<TextNotAllowedException>("the card says why and keeps the comment");
        await ShouldBeRefusedAsync(flagged);
        (await worker.Get<JobBoard>().RateHouseholdAsync(household.Uid, 4, "Mabait")).Should().BeFalse(
            "a clean comment reaches the job check: there is no completed job");
    }

    [IntegrationFact]
    public async Task OthersNote_WithRudeWords_IsNotStored()
    {
        var tag = Tag();
        var client = api.Client(api.Tokens.For("note-writer"));

        using var wordList = await client.PostAsJsonAsync("other-jobs",
            new OtherJobRequest("construction", Wire.Seeker, $"{Rude} {tag}"), ApiJson.Default.OtherJobRequest);
        using var service = await client.PostAsJsonAsync("other-jobs",
            new OtherJobRequest("construction", Wire.Seeker, $"{FakeTextModerator.Marker} {tag}"), ApiJson.Default.OtherJobRequest);

        await ShouldBeRefusedAsync(wordList);
        await ShouldBeRefusedAsync(service);
        (await Scalar($"SELECT COUNT(*) FROM dbo.OtherJobNotes WHERE Text LIKE N'%{tag}%'")).Should().Be(0);
    }

    [IntegrationFact]
    public async Task Publish_WithARudeName_IsRefused_AndNothingGoesLive()
    {
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Rudy");
        worker.Identity.User = new AuthUser { Uid = worker.Uid, DisplayName = Rude, PhoneNumber = worker.PhoneNumber };

        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.TextNotAllowed);

        worker.Pro.IsPublished.Should().BeFalse();
        (await new TestPhone(api, "Viewer").Get<IProfessionalProfileRepository>().GetByIdAsync(worker.Uid)).Should().BeNull();
    }

    [IntegrationFact]
    public async Task LiveListingEdit_WithFlaggedOthersText_IsRefused_AndCustomersKeepTheOldText()
    {
        var tag = Tag();
        var worker = await TestPhone.WorkerReadyToPublishAsync(api, "Ben", trade: ReferenceData.OtherTradeId, jobs: [ReferenceData.OtherTradeId]);
        worker.Pro.Draft.OtherJobText = $"Paint ng gate {tag}";
        (await worker.Pro.PublishAsync()).Should().Be(PublishResult.Published);

        worker.Pro.Draft.OtherJobText = $"Paint {FakeTextModerator.Marker}";
        (await worker.Pro.ListingEditedAsync()).Should().Be(PublishResult.TextNotAllowed);

        worker.Pro.EditRefused.Should().BeTrue("Pro home says the edit didn't reach customers");
        var seen = await new TestPhone(api, "Viewer").Get<IProfessionalProfileRepository>().GetByIdAsync(worker.Uid);
        seen!.OtherWork.Should().Be($"Paint ng gate {tag}");
    }

    private static async Task ShouldBeRefusedAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync(ApiJson.Default.ApiError))!.Error.Should().Be(ApiErrors.TextNotAllowed);
    }

    private async Task<int> Scalar(string sql)
    {
        await using var connection = new SqlConnection(api.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return (int)(await command.ExecuteScalarAsync())!;
    }
}
