using SideGigPh.Contracts;

namespace SideGigPh.Maui.Services.Localization;

/// <summary>The words for calling a picked job off (<see cref="PickCancel"/>): each reason, and the chat message sent with it.</summary>
public static class PickCancelText
{
    public static string CancelReason(this AppText text, string reason) => text.Get(reason switch
    {
        PickCancelReasons.CantMakeIt => Copy.CancelReasonCantMakeIt,
        PickCancelReasons.NoReply => Copy.CancelReasonNoReply,
        PickCancelReasons.NoShow => Copy.CancelReasonNoShow,
        PickCancelReasons.NotNeeded => Copy.CancelReasonNotNeeded,
        PickCancelReasons.Price => Copy.CancelReasonPrice,
        _ => Copy.CancelReasonOther,
    });

    /// <summary>"I cancelled this: Aircon cleaning. Reason: I can't make it." plus the note, if any, on its own line.</summary>
    public static string CancelMessage(this AppText text, string service, string reason, string? note)
    {
        var message = text.Format(Copy.CancelPickMessage, service, text.CancelReason(reason));
        return PickCancel.CleanNote(note) is { } words ? $"{message}\n{words}" : message;
    }
}
