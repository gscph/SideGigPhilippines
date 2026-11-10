using System.Text.Json.Serialization;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Dev;

/// <summary>Source-generated serializer for the stubs' on-device files (trimming/AOT safe).</summary>
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<Inquiry>))]
[JsonSerializable(typeof(List<Review>))]
internal partial class DevJsonContext : JsonSerializerContext
{
}
