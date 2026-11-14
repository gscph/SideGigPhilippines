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
[JsonSerializable(typeof(SaveDeviceRequest))]
[JsonSerializable(typeof(ConfigDto))]
[JsonSerializable(typeof(OptionDto))]
[JsonSerializable(typeof(List<OptionDto>))]
[JsonSerializable(typeof(CategoryDto))]
[JsonSerializable(typeof(List<CategoryDto>))]
[JsonSerializable(typeof(ServiceDto))]
[JsonSerializable(typeof(List<ServiceDto>))]
[JsonSerializable(typeof(PriceGuideDto))]
[JsonSerializable(typeof(OtherJobRequest))]
[JsonSerializable(typeof(SearchHitDto))]
[JsonSerializable(typeof(List<SearchHitDto>))]
[JsonSerializable(typeof(SearchPageDto))]
[JsonSerializable(typeof(ErrorReportDto))]
[JsonSerializable(typeof(List<ErrorReportDto>))]
[JsonSerializable(typeof(ErrorReportBatch))]
public sealed partial class ApiJson : JsonSerializerContext;
