using SideGigPh.FrontDoor.Localization;

namespace SideGigPh.FrontDoor.Components.Landing;

/// <summary>Why Tarbaho, said once and plainly: pay the worker directly (in money green), own price, private numbers, nearby and pay after.</summary>
public partial class Promises
{
    private sealed record Promise(string Icon, Say Title, Say Text, bool Money = false);

    private static readonly Promise[] Items =
    [
        new("images/icons/money.svg", Copy.Promises.ZeroTitle, Copy.Promises.ZeroText, Money: true),
        new("images/icons/receipt.svg", Copy.Promises.PriceTitle, Copy.Promises.PriceText),
        new("images/icons/cyber-lock.svg", Copy.Promises.PrivateTitle, Copy.Promises.PrivateText),
        new("images/icons/pin.svg", Copy.Promises.NearbyTitle, Copy.Promises.NearbyText),
    ];
}
