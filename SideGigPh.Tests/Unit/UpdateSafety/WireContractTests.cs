using System.Text.Json;
using SideGigPh.Contracts;

namespace SideGigPh.Tests.Unit.UpdateSafety;

/// <summary>
/// The app and the API talk JSON (<c>SideGigPh.Contracts</c>). Phones keep running old app versions for weeks
/// after the API is updated, so renaming a field, or changing a code like "phone-taken", breaks every user who
/// hasn't updated. These snapshots fail when a JSON name changes. Adding a new field with a default is fine:
/// add it to the expected list. Renaming or removing one needs a plan for old app versions first.
/// </summary>
public class WireContractTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 13, 8, 0, 0, TimeSpan.FromHours(8));

    private static string[] FieldNames<T>(T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(value, type));
        return doc.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
    }

    private static T RoundTrip<T>(T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type) =>
        JsonSerializer.Deserialize(JsonSerializer.Serialize(value, type), type)!;

    private static ListingDto Listing() => new(
        "pro-1", "Maria", "+639171234567", "plumbing", "rizal", ["plumbing-clog"], ["antipolo"], "1-3", 1200,
        new PhotoDto("p1", "https://x/p1.jpg"), [new PhotoDto("w1", "https://x/w1.jpg")], new ReviewSummaryDto(4.5, 3));

    private static ChatDto Chat() => new(
        "pro-1_seeker-1", "pro-1", "seeker-1", "Maria", "Ana", "plumbing-clog", "taytay", "today", "under-500",
        new MessageDto("m1", "pro-1_seeker-1", "seeker-1", Wire.Text, "Hi po", At), null, At, At);

    // "referenceCount" was removed on purpose when references were dropped: an older app reads a missing int as 0,
    // which only hides its "N references" row (Listing_FromOlderApiWithoutNewerFields_StillReads covers the reading).
    [Fact]
    public void ListingDto_FieldNamesNeverChange() =>
        FieldNames(Listing(), ApiJson.Default.ListingDto).Should().Equal(
            "id", "displayName", "mobileNumber", "categoryId", "areaId", "serviceIds", "cityIds", "experienceId",
            "ratePesos", "profilePhoto", "workPhotos", "reviews", "badges", "otherWork", "completedJobs");

    // "references" was removed on purpose when references were dropped: an older app that still sends it is read
    // with the field ignored (ServerListingRulesTests.OldAppBody_WithReferences_ReadsAndIsAccepted).
    [Fact]
    public void PublishListingRequest_FieldNamesNeverChange() =>
        FieldNames(
            new PublishListingRequest("Juan", "aircon", "rizal", ["aircon-cleaning"], ["taytay"], "1-3", 1200, "p1", ["w1", "w2"]),
            ApiJson.Default.PublishListingRequest).Should().Equal(
            "displayName", "categoryId", "areaId", "serviceIds", "cityIds", "experienceId", "ratePesos", "profilePhotoId",
            "workPhotoIds", "latitude", "longitude", "radiusKm", "otherWork");

    [Fact]
    public void ChatDto_FieldNamesNeverChange() =>
        FieldNames(Chat(), ApiJson.Default.ChatDto).Should().Equal(
            "id", "professionalId", "seekerId", "professionalName", "seekerName", "serviceId", "cityId", "urgencyId",
            "budgetId", "lastMessage", "seekerReadAt", "professionalReadAt", "createdAt", "serviceName");

    [Fact]
    public void SendMessageRequest_FieldNamesNeverChange() =>
        FieldNames(
            new SendMessageRequest("m1", "pro-1", "seeker-1", Wire.Text, "Hi", "Maria", "Ana", null, null, null, null),
            ApiJson.Default.SendMessageRequest).Should().Equal(
            "messageId", "professionalId", "seekerId", "kind", "text", "professionalName", "seekerName", "serviceId",
            "cityId", "urgencyId", "budgetId");

    [Fact]
    public void UserDto_FieldNamesNeverChange() =>
        FieldNames(
            new UserDto("uid", "Juan", "+639171234567", null, Wire.Taglish, Wire.Professional, "10000", At, At, "p1"),
            ApiJson.Default.UserDto).Should().Equal(
            "uid", "displayName", "phoneNumber", "email", "language", "role", "incomeGoalId", "createdAt", "updatedAt",
            "profilePhotoId");

    [Fact]
    public void ReviewContracts_FieldNamesNeverChange()
    {
        FieldNames(new ReviewDto("pro-1", "seeker-1", "Ana D.", 5, "Ok", "plumbing-clog", At, At), ApiJson.Default.ReviewDto)
            .Should().Equal("professionalId", "seekerId", "reviewerName", "rating", "comment", "serviceId", "createdAt", "updatedAt", "verifiedJob", "hidden", "locked");
        FieldNames(new SaveReviewRequest("pro-1", "Ana D.", 5, null, null), ApiJson.Default.SaveReviewRequest)
            .Should().Equal("professionalId", "reviewerName", "rating", "comment", "serviceId");
    }

    [Fact]
    public void SaveDeviceRequest_FieldNamesNeverChange() =>
        FieldNames(new SaveDeviceRequest("token", Wire.Android), ApiJson.Default.SaveDeviceRequest).Should().Equal("token", "platform");

    [Fact]
    public void ConfigContracts_FieldNamesNeverChange()
    {
        var price = new PriceGuideDto(755, 1000, 1500);
        var service = new ServiceDto("aircon-cleaning", "Aircon cleaning", "Pa-linis ng aircon", "🧽", price);
        var category = new CategoryDto("aircon", "Aircon Technician", "Aircon Technician", "❄️", "Cleaning", "Linis", [service]);

        var option = new OptionDto("3-10", "3–10 years", "3–10 taon", null, 0);

        var barangay = new BarangayDto("angono-san-isidro", "angono", "San Isidro", "San Isidro");
        FieldNames(new ConfigDto([category], [option], [option], [option], [option], "angono", [barangay]), ApiJson.Default.ConfigDto)
            .Should().Equal("catalog", "towns", "experience", "urgencies", "incomeGoals", "defaultTownId", "barangays");
        FieldNames(barangay, ApiJson.Default.BarangayDto).Should().Equal("id", "townId", "english", "taglish");
        FieldNames(option, ApiJson.Default.OptionDto).Should().Equal("id", "english", "taglish", "icon", "amount");
        FieldNames(category, ApiJson.Default.CategoryDto).Should().Equal("id", "english", "taglish", "icon", "hintEnglish", "hintTaglish", "services");
        FieldNames(service, ApiJson.Default.ServiceDto).Should().Equal("id", "english", "taglish", "icon", "price");
        FieldNames(price, ApiJson.Default.PriceGuideDto).Should().Equal("min", "average", "high");
        FieldNames(new OtherJobRequest("aircon", Wire.Seeker, "thermostat"), ApiJson.Default.OtherJobRequest).Should().Equal("categoryId", "side", "text");
    }

    // Phones keep error reports for days before sending them, so an older app's batch must still be read.
    [Fact]
    public void ErrorReportFieldNamesAndKinds_NeverChange()
    {
        var report = new ErrorReportDto(ErrorKinds.Slow, "Loading", "Stuck", "stack", "/start/pro/done", "trail", 2, At, At);
        FieldNames(new ErrorReportBatch("id", "1.0", "48466c9", AppChannels.Prod, "android 14", "samsung", [report]), ApiJson.Default.ErrorReportBatch)
            .Should().Equal("installId", "appVersion", "codeVersion", "channel", "platform", "device", "reports");
        FieldNames(report, ApiJson.Default.ErrorReportDto)
            .Should().Equal("kind", "source", "message", "detail", "route", "trail", "count", "firstAt", "lastAt");
        ErrorKinds.Error.Should().Be("error");
        ErrorKinds.Crash.Should().Be("crash");
        ErrorKinds.Slow.Should().Be("slow");
        AppChannels.Dev.Should().Be("dev");
        AppChannels.Beta.Should().Be("beta");
        AppChannels.Prod.Should().Be("prod");
    }

    // Job posts: an installed app reads these fields, and tells an open post from a closed one by these words.
    [Fact]
    public void JobPostFieldNamesAndStatuses_NeverChange()
    {
        var post = new JobPostDto(7, "plumbing", "plumbing-clog", "Barado ang lababo", 800, "taytay", JobPostStatuses.Open, [], true, false, 2, At,
            BarangayId: "taytay-muzon");
        FieldNames(post, ApiJson.Default.JobPostDto).Should().Equal(
            "id", "categoryId", "serviceId", "description", "budgetPesos", "cityId", "status", "photos", "mine", "offersService", "distanceKm", "createdAt",
            "offerCount", "myOfferPesos", "pickedProId", "budgetMinPesos", "householdReviews", "barangayId",
            "neededOn", "title", "tradeName", "serviceName");
        FieldNames(new JobPostFeedDto([post], true, true), ApiJson.Default.JobPostFeedDto).Should().Equal("posts", "hasMore", "hasListing");
        FieldNames(new NewJobPostRequest("plumbing", "plumbing-clog", "Barado ang lababo", 800, "taytay", [], 14.5, 121.1, BarangayId: "taytay-muzon"),
                ApiJson.Default.NewJobPostRequest)
            .Should().Equal("categoryId", "serviceId", "description", "budgetPesos", "cityId", "photoIds", "latitude", "longitude", "budgetMinPesos",
                "barangayId", "neededOn", "title");
        JobPostStatuses.Open.Should().Be("open");
        JobPostStatuses.Closed.Should().Be("closed");
        JobPostStatuses.Hidden.Should().Be("hidden");
    }

    // Offers on job posts: an installed app reads these fields, and tells a picked offer by this word.
    [Fact]
    public void JobOfferFieldNamesAndStatuses_NeverChange()
    {
        var offer = new JobOfferDto(3, 7, SideGigPh.Tests.TestSupport.TestData.Dto("Mang Ben"), 650, "Bukas", JobOfferStatuses.Sent, false, 2, At);
        FieldNames(offer, ApiJson.Default.JobOfferDto).Should().Equal(
            "id", "postId", "pro", "pricePesos", "note", "status", "takesBudget", "distanceKm", "createdAt");
        FieldNames(new SendOfferRequest(650, "Bukas"), ApiJson.Default.SendOfferRequest).Should().Equal("pricePesos", "note");
        JobOfferStatuses.Sent.Should().Be("sent");
        JobOfferStatuses.Picked.Should().Be("picked");
        JobOfferStatuses.Withdrawn.Should().Be("withdrawn");
        JobOfferStatuses.Cancelled.Should().Be("cancelled");
    }

    // Calling off a picked job: the reason ids are stored in JobOffers.CancelReason and the 409 is read by the app.
    [Fact]
    public void Cancel_reasons_and_the_completed_error_keep_their_words()
    {
        PickCancelReasons.All.Should().Equal("cant-make-it", "no-reply", "no-show", "not-needed", "price", "other");
        PickCancelSides.Household.Should().Be("household");
        PickCancelSides.Worker.Should().Be("worker");
        ApiErrors.JobCompleted.Should().Be("job-completed");
    }

    // The household's exact place for the picked worker: an installed app sends and reads these fields.
    [Fact]
    public void Meet_location_field_names_never_change()
    {
        FieldNames(new ShareMeetLocationRequest(14.5, 121.1), ApiJson.Default.ShareMeetLocationRequest).Should().Equal("latitude", "longitude");
        FieldNames(new PickedJobDto(1, "plumbing-clog", 900, At, true, false, false, MeetLatitude: 14.5, MeetLongitude: 121.1, MeetSharedAt: At),
                ApiJson.Default.PickedJobDto)
            .Should().Contain(["meetLatitude", "meetLongitude", "meetSharedAt"]);
    }

    // A post's, chat's and picked job's own copy of a trade's or job's name (an operator's rename never changes it).
    [Fact]
    public void Saved_catalog_names_field_names_never_change()
    {
        FieldNames(new CatalogNameDto("Aircon cleaning", "Linis ng aircon"), ApiJson.Default.CatalogNameDto).Should().Equal("english", "taglish");
        FieldNames(new PickedJobDto(1, "plumbing-clog", 900, At, true, false, false), ApiJson.Default.PickedJobDto).Should().Contain("serviceName");
    }

    // The feedback board: an installed app reads these fields and shows a status by these words.
    [Fact]
    public void FeedbackFieldNamesAndStatuses_NeverChange()
    {
        var post = new FeedbackPostDto(7, "Pay with GCash", "details", 3, FeedbackStatuses.Open, true, false, At);
        FieldNames(post, ApiJson.Default.FeedbackPostDto)
            .Should().Equal("id", "title", "details", "votes", "status", "votedByMe", "mine", "createdAt");
        FieldNames(new FeedbackPageDto([post], true), ApiJson.Default.FeedbackPageDto).Should().Equal("posts", "hasMore");
        FieldNames(new NewFeedbackRequest("Pay with GCash", "details", Wire.Seeker), ApiJson.Default.NewFeedbackRequest).Should().Equal("title", "details", "side");
        FieldNames(new FeedbackVoteDto(7, 3, true), ApiJson.Default.FeedbackVoteDto).Should().Equal("id", "votes", "votedByMe");
        FeedbackStatuses.Open.Should().Be("open");
        FeedbackStatuses.Planned.Should().Be("planned");
        FeedbackStatuses.Done.Should().Be("done");
        FeedbackStatuses.Declined.Should().Be("declined");
        FeedbackStatuses.Hidden.Should().Be("hidden");
    }

    // Installed apps send the first header and read the second; the server's logs and the error reports rely on both.
    [Fact]
    public void VersionHeaders_NeverChange()
    {
        ApiHeaders.AppVersion.Should().Be("X-App-Version");
        ApiHeaders.ApiVersion.Should().Be("X-Api-Version");
    }

    // Phones already installed read these keys from a notification and created these Android channels at launch.
    [Fact]
    public void NotificationKeysAndChannels_NeverChange()
    {
        PushData.ChatId.Should().Be("chatId");
        PushData.ProfessionalId.Should().Be("professionalId");
        PushData.Side.Should().Be("side");
        PushData.CustomersChannel.Should().Be("customers");
        PushData.RepliesChannel.Should().Be("replies");
        PushData.PostId.Should().Be("postId");
        PushData.RequestsChannel.Should().Be("requests");
        PushData.OffersChannel.Should().Be("offers");
    }

    [Fact]
    public void ErrorAnswer_FieldNamesNeverChange() =>
        FieldNames(new ApiError(ApiErrors.PhoneTaken, "d"), ApiJson.Default.ApiError).Should().Equal("error", "detail");

    // The app decides what to show from these exact words (e.g. "This number already has a listing").
    [Fact]
    public void ErrorCodesAndWireWords_NeverChange()
    {
        ApiErrors.PhoneTaken.Should().Be("phone-taken");
        ApiErrors.PhoneUsedBySeeker.Should().Be("phone-used-by-seeker");
        ApiErrors.PhotoInUse.Should().Be("photo-in-use");
        ApiErrors.BadBody.Should().Be("bad-body");
        ApiErrors.PhoneRequired.Should().Be("phone-required");
        ApiErrors.NoChat.Should().Be("no-chat");
        ApiErrors.ReviewTooSoon.Should().Be("review-too-soon");
        ApiErrors.ReviewLocked.Should().Be("review-locked");
        ApiErrors.Exception.Should().Be("exception");
        ApiErrors.TooManyRequests.Should().Be("too-many-requests");
        ApiErrors.Invalid("workPhotoIds").Should().Be("invalid-workPhotoIds");
        ApiErrors.ConsentRequired.Should().Be("consent-required");
        ApiErrors.AlreadySent.Should().Be("already-sent");
        ApiErrors.NotAdmin.Should().Be("not-admin");
        ApiErrors.OthersStays.Should().Be("others-stays");
        VerificationKinds.All.Should().Equal("id", "tesda", "developer");
        DeveloperBadges.All.Should().Equal("ayala-land", "megaworld", "robinsons-land", "sm", "dmci", "other");

        Wire.English.Should().Be("english");
        Wire.Taglish.Should().Be("taglish");
        Wire.Seeker.Should().Be("seeker");
        Wire.Professional.Should().Be("professional");
        Wire.Text.Should().Be("text");
        Wire.Call.Should().Be("call");
        Wire.Pin.Should().Be("pin");
        Wire.Android.Should().Be("android");
        Wire.Ios.Should().Be("ios");
    }

    [Fact]
    public void Listing_RoundTrips()
    {
        var back = RoundTrip(Listing(), ApiJson.Default.ListingDto);

        back.Should().BeEquivalentTo(Listing());
    }

    [Fact]
    public void Chat_RoundTrips() =>
        RoundTrip(Chat(), ApiJson.Default.ChatDto).Should().BeEquivalentTo(Chat());

    // An older API that doesn't send the newer fields yet must still be readable by a newer app.
    [Fact]
    public void Listing_FromOlderApiWithoutNewerFields_StillReads()
    {
        const string json = """
            {"id":"p","displayName":"Maria","mobileNumber":"+639171234567","categoryId":"aircon","areaId":"rizal",
             "serviceIds":["aircon-cleaning"],"cityIds":["taytay"],"experienceId":null,"ratePesos":800,
             "profilePhoto":null,"workPhotos":[]}
            """;

        var listing = JsonSerializer.Deserialize(json, ApiJson.Default.ListingDto)!;

        listing.Reviews.Should().BeNull();
        listing.Badges.Should().BeNull();
    }

    // The phone app is trimmed: a contract without a [JsonSerializable] line in ApiJson fails only on a real phone.
    [Fact]
    public void EveryContract_IsRegisteredInApiJson()
    {
        var contracts = typeof(ApiJson).Assembly.GetExportedTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "SideGigPh.Contracts" && t != typeof(ApiJson));

        foreach (var type in contracts)
        {
            ApiJson.Default.GetTypeInfo(type).Should().NotBeNull($"{type.Name} needs [JsonSerializable(typeof({type.Name}))] in ApiJson");
        }
    }
}
