using System.Text.Json.Serialization;
using SideGigPh.Maui.Models;

namespace SideGigPh.Maui.Services.Local;

/// <summary>Source-generated serializer for the saved config (<c>config.json</c>), trim-safe in Release builds.</summary>
[JsonSerializable(typeof(Stamped<AppConfig>), TypeInfoPropertyName = "StampedAppConfig")]
internal partial class AppConfigJsonContext : JsonSerializerContext
{
}
