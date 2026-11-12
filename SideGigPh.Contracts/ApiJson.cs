using System.Text.Json;
using System.Text.Json.Serialization;

namespace SideGigPh.Contracts;

/// <summary>Source-generated JSON for every contract (camelCase, like the API's ASP.NET Core defaults). Trim-safe on the phone.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ApiError))]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(PhotoDto))]
[JsonSerializable(typeof(ListingDto))]
[JsonSerializable(typeof(List<ListingDto>))]
[JsonSerializable(typeof(PublishListingRequest))]
[JsonSerializable(typeof(ReferenceDto))]
[JsonSerializable(typeof(List<ReferenceDto>))]
[JsonSerializable(typeof(ReviewDto))]
[JsonSerializable(typeof(List<ReviewDto>))]
[JsonSerializable(typeof(SaveReviewRequest))]
[JsonSerializable(typeof(ChatDto))]
[JsonSerializable(typeof(List<ChatDto>))]
[JsonSerializable(typeof(List<MessageDto>))]
[JsonSerializable(typeof(SendMessageRequest))]
public sealed partial class ApiJson : JsonSerializerContext;
