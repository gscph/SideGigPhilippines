using System.Text.Json.Serialization;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Dev;

/// <summary>Source-generated serializer for the stubs' on-device files (trimming/AOT safe).</summary>
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<Chat>))]
[JsonSerializable(typeof(List<ChatMessage>))]
[JsonSerializable(typeof(List<Review>))]
[JsonSerializable(typeof(List<UserProfile>))]
internal partial class DevJsonContext : JsonSerializerContext
{
}
