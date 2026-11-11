using Plugin.Firebase.Firestore;

namespace SideGigPh.Maui.Services.Firestore;

// The shapes of the Firestore documents. They match firebase/firestore.rules field for field (the
// rules refuse any other field), and docs/data-model.md §4. Only this folder knows about them:
// the app's models in Models/ never get Firestore types.
//
// Every list starts empty (never null), so a write always carries the field the rules require.
// "Never read" read-times are written as UnixEpoch, so no property needs to be nullable.

/// <summary><c>users/{uid}</c>: private to the person.</summary>
public sealed class UserDoc : IFirestoreObject
{
    [FirestoreProperty("displayName")] public string? DisplayName { get; set; }

    [FirestoreProperty("phoneNumber")] public string? PhoneNumber { get; set; }

    [FirestoreProperty("email")] public string? Email { get; set; }

    /// <summary><c>english</c> or <c>taglish</c>.</summary>
    [FirestoreProperty("language")] public string Language { get; set; } = "english";

    /// <summary><c>seeker</c> or <c>professional</c>, or null before they choose.</summary>
    [FirestoreProperty("role")] public string? Role { get; set; }

    [FirestoreProperty("incomeGoalId")] public string? IncomeGoalId { get; set; }

    [FirestoreProperty("createdAt")] public DateTimeOffset CreatedAt { get; set; }

    [FirestoreProperty("updatedAt")] public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>A photo kept by <c>SideGigPh.Api</c>: its SHA-256 id and its public URL.</summary>
public sealed class PhotoDoc : IFirestoreObject
{
    [FirestoreProperty("id")] public string Id { get; set; } = string.Empty;

    [FirestoreProperty("url")] public string Url { get; set; } = string.Empty;
}

/// <summary><c>professionals/{uid}</c>: the published Skilled Worker listing.</summary>
public sealed class ProfessionalDoc : IFirestoreObject
{
    [FirestoreDocumentId] public string? Id { get; set; }

    [FirestoreProperty("displayName")] public string DisplayName { get; set; } = string.Empty;

    [FirestoreProperty("mobileNumber")] public string MobileNumber { get; set; } = string.Empty;

    [FirestoreProperty("categoryId")] public string CategoryId { get; set; } = string.Empty;

    [FirestoreProperty("serviceIds")] public IList<string> ServiceIds { get; set; } = [];

    [FirestoreProperty("areaId")] public string AreaId { get; set; } = string.Empty;

    [FirestoreProperty("cityIds")] public IList<string> CityIds { get; set; } = [];

    [FirestoreProperty("experienceId")] public string? ExperienceId { get; set; }

    [FirestoreProperty("rateTierId")] public string? RateTierId { get; set; }

    [FirestoreProperty("profilePhoto")] public PhotoDoc? ProfilePhoto { get; set; }

    [FirestoreProperty("workPhotos")] public IList<PhotoDoc> WorkPhotos { get; set; } = [];

    [FirestoreProperty("publishedAt")] public DateTimeOffset PublishedAt { get; set; }

    [FirestoreProperty("updatedAt")] public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary><c>reviews/{proId}_{seekerUid}</c>.</summary>
public sealed class ReviewDoc : IFirestoreObject
{
    [FirestoreDocumentId] public string? Id { get; set; }

    [FirestoreProperty("professionalId")] public string ProfessionalId { get; set; } = string.Empty;

    [FirestoreProperty("seekerUid")] public string SeekerUid { get; set; } = string.Empty;

    [FirestoreProperty("reviewerName")] public string ReviewerName { get; set; } = string.Empty;

    [FirestoreProperty("rating")] public long Rating { get; set; }

    [FirestoreProperty("comment")] public string? Comment { get; set; }

    [FirestoreProperty("serviceId")] public string? ServiceId { get; set; }

    /// <summary>Set once, when the review is first written (the rules don't let it change).</summary>
    [FirestoreProperty("createdAt")] public DateTimeOffset CreatedAt { get; set; }

    [FirestoreProperty("updatedAt")] public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>The last message, copied into the chat so an Inbox needs no other reads.</summary>
public sealed class LastMessageDoc : IFirestoreObject
{
    [FirestoreProperty("id")] public string Id { get; set; } = string.Empty;

    [FirestoreProperty("text")] public string? Text { get; set; }

    /// <summary><c>text</c> or <c>call</c>.</summary>
    [FirestoreProperty("kind")] public string Kind { get; set; } = "text";

    [FirestoreProperty("senderUid")] public string SenderUid { get; set; } = string.Empty;

    [FirestoreProperty("at")] public DateTimeOffset At { get; set; }
}

/// <summary><c>chats/{proId}_{seekerUid}</c>.</summary>
public sealed class ChatDoc : IFirestoreObject
{
    [FirestoreDocumentId] public string? Id { get; set; }

    [FirestoreProperty("professionalId")] public string ProfessionalId { get; set; } = string.Empty;

    [FirestoreProperty("seekerUid")] public string SeekerUid { get; set; } = string.Empty;

    [FirestoreProperty("professionalName")] public string ProfessionalName { get; set; } = string.Empty;

    [FirestoreProperty("seekerName")] public string SeekerName { get; set; } = string.Empty;

    [FirestoreProperty("serviceId")] public string? ServiceId { get; set; }

    [FirestoreProperty("cityId")] public string? CityId { get; set; }

    [FirestoreProperty("urgencyId")] public string? UrgencyId { get; set; }

    [FirestoreProperty("budgetId")] public string? BudgetId { get; set; }

    [FirestoreProperty("lastMessage")] public LastMessageDoc LastMessage { get; set; } = new();

    [FirestoreProperty("lastMessageAt")] public DateTimeOffset LastMessageAt { get; set; }

    [FirestoreProperty("seekerReadAt")] public DateTimeOffset SeekerReadAt { get; set; } = DateTimeOffset.UnixEpoch;

    [FirestoreProperty("professionalReadAt")] public DateTimeOffset ProfessionalReadAt { get; set; } = DateTimeOffset.UnixEpoch;

    [FirestoreProperty("createdAt")] public DateTimeOffset CreatedAt { get; set; }
}

/// <summary><c>chats/{chatId}/messages/{messageId}</c>. The chat id is in the path, so it isn't stored here.</summary>
public sealed class MessageDoc : IFirestoreObject
{
    [FirestoreDocumentId] public string? Id { get; set; }

    [FirestoreProperty("senderUid")] public string SenderUid { get; set; } = string.Empty;

    /// <summary><c>text</c> or <c>call</c>.</summary>
    [FirestoreProperty("kind")] public string Kind { get; set; } = "text";

    [FirestoreProperty("text")] public string? Text { get; set; }

    [FirestoreProperty("at")] public DateTimeOffset At { get; set; }
}
