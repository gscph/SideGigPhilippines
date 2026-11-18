using MobileAppTheme.Models;
using SideGigPh.Maui.Models;
using SideGigPh.Maui.Services;

namespace SideGigPh.Tests.Unit.Catalog;

/// <summary>
/// Every trade, job and answer shows one of the app's own pictures (SVG files shipped in <c>wwwroot/images/icons/</c>),
/// picked by id. A picture that isn't in the app would show as a broken image on a tile, and a config that still has
/// emoji (saved by an app version before the pictures) must not be used.
/// </summary>
public class AppImagesTests
{
    private static readonly AppConfig BuiltIn = ReferenceData.BuiltInConfig;

    private static IEnumerable<CatalogOption> WithPictures =>
        BuiltIn.Categories
            .Concat(BuiltIn.ServicesByCategory.Values.SelectMany(services => services))
            .Concat(BuiltIn.Experience)
            .Concat(BuiltIn.Urgencies)
            .Concat(ReferenceData.TradeChoices)
            .Concat(ReferenceData.FindByChoices)
            .Append(ReferenceData.UnsureOption);

    [Fact]
    public void EveryTradeJobAndAnswer_HasItsOwnPicture_OthersGetThePlus()
    {
        foreach (var option in WithPictures)
        {
            option.Icon.Should().NotBeNull($"{option.Id} is a tile with a picture");
            var isOthers = ReferenceData.IsOtherJob(option.Id) || option.Id == ReferenceData.OtherTradeId;
            (option.Icon == AppImages.More).Should().Be(isOthers, $"{option.Id}: only \"Others\" shows the plus");
        }
    }

    [Fact]
    public void EveryPicture_IsAFileInTheApp()
    {
        var named = typeof(AppImages).GetFields().Select(field => (string)field.GetValue(null)!);

        foreach (var picture in WithPictures.Select(option => option.Icon!).Concat(named).Distinct())
        {
            File.Exists(Path.Combine(AppFolder(), "wwwroot", picture)).Should().BeTrue($"{picture} is shown by the app");
        }
    }

    [Fact]
    public void IdWithoutAPicture_GetsThePlus_NotABrokenImage()
    {
        AppImages.For("a-trade-added-on-the-server").Should().Be(AppImages.More);
    }

    [Fact]
    public void ConfigSavedWithEmoji_IsNotUsable_SoItIsFetchedAgain()
    {
        var first = BuiltIn.Categories[0];
        var old = BuiltIn with { Categories = [first with { Icon = "❄️" }, .. BuiltIn.Categories.Skip(1)] };

        ReferenceData.CanUse(old).Should().BeFalse("an emoji would be shown as a broken image");
        ReferenceData.CanUse(BuiltIn).Should().BeTrue();
    }

    private static string AppFolder()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !Directory.Exists(Path.Combine(folder.FullName, "SideGigPh.Maui", "wwwroot")))
        {
            folder = folder.Parent;
        }

        return Path.Combine(folder!.FullName, "SideGigPh.Maui");
    }
}
