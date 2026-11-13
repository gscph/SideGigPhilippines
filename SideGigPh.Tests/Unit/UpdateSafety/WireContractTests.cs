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
        "pro-1", "Maria", "+639171234567", "plumbing", "rizal", ["plumbing-clog"], ["antipolo"], "1-3", "under-500",
        new PhotoDto("p1", "https://x/p1.jpg"), [new PhotoDto("w1", "https://x/w1.jpg")], new ReviewSummaryDto(4.5, 3), 2);

    private static ChatDto Chat() => new(
        "pro-1_seeker-1", "pro-1", "seeker-1", "Maria", "Ana", "plumbing-clog", "taytay", "today", "under-500",
        new MessageDto("m1", "pro-1_seeker-1", "seeker-1", Wire.Text, "Hi po", At), null, At, At);

    [Fact]
    public void ListingDto_FieldNamesNeverChange() =>
        FieldNames(Listing(), ApiJson.Default.ListingDto).Should().Equal(
            "id", "displayName", "mobileNumber", "categoryId", "areaId", "serviceIds", "cityIds", "experienceId",
            "rateTierId", "profilePhoto", "workPhotos", "reviews", "referenceCount");

    [Fact]
    public void PublishListingRequest_FieldNamesNeverChange() =>
        FieldNames(
            new PublishListingRequest("Juan", "aircon", "rizal", ["aircon-cleaning"], ["taytay"], "1-3", "quote", "p1", ["w1", "w2"],
                [new ReferenceDto("Nena", "+639181111111")]),
            ApiJson.Default.PublishListingRequest).Should().Equal(
            "displayName", "categoryId", "areaId", "serviceIds", "cityIds", "experienceId", "rateTierId", "profilePhotoId",
            "workPhotoIds", "references");

    [Fact]
    public void ChatDto_FieldNamesNeverChange() =>
        FieldNames(Chat(), ApiJson.Default.ChatDto).Should().Equal(
            "id", "professionalId", "seekerId", "professionalName", "seekerName", "serviceId", "cityId", "urgencyId",
            "budgetId", "lastMessage", "seekerReadAt", "professionalReadAt", "createdAt");

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
            .Should().Equal("professionalId", "seekerId", "reviewerName", "rating", "comment", "serviceId", "createdAt", "updatedAt");
        FieldNames(new SaveReviewRequest("pro-1", "Ana D.", 5, null, null), ApiJson.Default.SaveReviewRequest)
            .Should().Equal("professionalId", "reviewerName", "rating", "comment", "serviceId");
    }

    [Fact]
    public void SaveDeviceRequest_FieldNamesNeverChange() =>
        FieldNames(new SaveDeviceRequest("token", Wire.Android), ApiJson.Default.SaveDeviceRequest).Should().Equal("token", "platform");

    // Phones already installed read these keys from a notification and created these Android channels at launch.
    [Fact]
    public void NotificationKeysAndChannels_NeverChange()
    {
        PushData.ChatId.Should().Be("chatId");
        PushData.ProfessionalId.Should().Be("professionalId");
        PushData.Side.Should().Be("side");
        PushData.CustomersChannel.Should().Be("customers");
        PushData.RepliesChannel.Should().Be("replies");
    }

    [Fact]
    public void ErrorAnswer_FieldNamesNeverChange() =>
        FieldNames(new ApiError(ApiErrors.PhoneTaken, "d"), ApiJson.Default.ApiError).Should().Equal("error", "detail");

    // The app decides what to show from these exact words (e.g. "This number already has a listing").
    [Fact]
    public void ErrorCodesAndWireWords_NeverChange()
    {
        ApiErrors.PhoneTaken.Should().Be("phone-taken");
        ApiErrors.PhotoInUse.Should().Be("photo-in-use");
        ApiErrors.BadBody.Should().Be("bad-body");
        ApiErrors.PhoneRequired.Should().Be("phone-required");
        ApiErrors.NoChat.Should().Be("no-chat");
        ApiErrors.Exception.Should().Be("exception");
        ApiErrors.Invalid("references").Should().Be("invalid-references");

        Wire.English.Should().Be("english");
        Wire.Taglish.Should().Be("taglish");
        Wire.Seeker.Should().Be("seeker");
        Wire.Professional.Should().Be("professional");
        Wire.Text.Should().Be("text");
        Wire.Call.Should().Be("call");
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
             "serviceIds":["aircon-cleaning"],"cityIds":["taytay"],"experienceId":null,"rateTierId":null,
             "profilePhoto":null,"workPhotos":[]}
            """;

        var listing = JsonSerializer.Deserialize(json, ApiJson.Default.ListingDto)!;

        listing.Reviews.Should().BeNull();
        listing.ReferenceCount.Should().Be(0);
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
