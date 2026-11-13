using SideGigPh.Maui.Models;
using SideGigPh.Tests.TestSupport;

namespace SideGigPh.Tests.Unit.Publishing;

/// <summary>
/// References (people who vouch for a worker) are required to publish. These tests make sure the References page
/// accepts numbers the way Filipinos type them and refuses, with a clear reason, what the server would refuse later.
/// </summary>
public class ReferenceTests
{
    private static ProfessionalDraft DraftWithoutReferences()
    {
        var draft = TestData.ReadyDraft();
        draft.References = [];
        return draft;
    }

    [Theory]
    [InlineData("0918 111 1111")]
    [InlineData("+63 918-111-1111")]
    [InlineData("9181111111")]
    public async Task AddReference_NumberTypedLocally_IsSavedAsE164(string typed)
    {
        var app = await FakeApp.ProAsync(DraftWithoutReferences());

        var result = await app.Pro.AddReferenceAsync("Aling Nena", typed);

        result.Should().Be(ReferenceAddResult.Added);
        app.Pro.Draft.References.Should().ContainSingle()
            .Which.Should().Be(new WorkReference("Aling Nena", "+639181111111"));
    }

    [Fact]
    public async Task AddReference_WorkersOwnNumber_IsRefused()
    {
        var app = await FakeApp.ProAsync(DraftWithoutReferences());

        var result = await app.Pro.AddReferenceAsync("Me", "0917 123 4567");

        result.Should().Be(ReferenceAddResult.OwnNumber);
    }

    [Fact]
    public async Task AddReference_SameNumberTwice_IsRefused()
    {
        var app = await FakeApp.ProAsync(DraftWithoutReferences());
        await app.Pro.AddReferenceAsync("Aling Nena", "09181111111");

        var result = await app.Pro.AddReferenceAsync("Mang Jose", "+639181111111");

        result.Should().Be(ReferenceAddResult.Duplicate);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("02 8123 4567")] // a landline
    [InlineData("")]
    public async Task AddReference_NotAMobileNumber_IsRefused(string typed)
    {
        var app = await FakeApp.ProAsync(DraftWithoutReferences());

        (await app.Pro.AddReferenceAsync("Aling Nena", typed)).Should().Be(ReferenceAddResult.InvalidNumber);
    }

    [Fact]
    public async Task AddReference_NoName_IsRefused()
    {
        var app = await FakeApp.ProAsync(DraftWithoutReferences());

        (await app.Pro.AddReferenceAsync("   ", "09181111111")).Should().Be(ReferenceAddResult.NoName);
    }

    [Fact]
    public async Task AddReference_AlreadyThree_IsRefused()
    {
        var app = await FakeApp.ProAsync(DraftWithoutReferences());
        await app.Pro.AddReferenceAsync("A", "09181111111");
        await app.Pro.AddReferenceAsync("B", "09182222222");
        await app.Pro.AddReferenceAsync("C", "09183333333");

        (await app.Pro.AddReferenceAsync("D", "09184444444")).Should().Be(ReferenceAddResult.Full);
    }

    [Fact]
    public async Task RemoveReference_BeforePublishing_RemovesIt()
    {
        var app = await FakeApp.ProAsync(TestData.ReadyDraft());

        await app.Pro.RemoveReferenceAsync(app.Pro.Draft.References[0]);

        app.Pro.Draft.References.Should().BeEmpty();
        app.Store.Saves.Should().Be(1);
    }

    [Fact]
    public async Task RemoveReference_OneOfTwoOnPublishedListing_RemovesAndRepublishes()
    {
        var draft = TestData.ReadyDraft();
        draft.PublishedAt = DateTimeOffset.Now;
        draft.References.Add(new WorkReference("Mang Jose", "+639182222222"));
        var app = await FakeApp.ProAsync(draft);

        await app.Pro.RemoveReferenceAsync(draft.References[0]);

        app.Pro.Draft.References.Should().ContainSingle().Which.Name.Should().Be("Mang Jose");
        app.Profiles.Published.Should().ContainSingle().Which.References.Should().HaveCount(1);
    }

    [Fact]
    public async Task RemoveReference_LastOneOnPublishedListing_IsNotAllowed()
    {
        // A live listing must keep at least one reference, or the server would take it down on the next save.
        var draft = TestData.ReadyDraft();
        draft.PublishedAt = DateTimeOffset.Now;
        var app = await FakeApp.ProAsync(draft);

        await app.Pro.RemoveReferenceAsync(draft.References[0]);

        app.Pro.CanRemoveReference.Should().BeFalse();
        app.Pro.Draft.References.Should().HaveCount(1);
    }

    [Fact]
    public async Task RemoveWorkPhoto_BelowTwoOnPublishedListing_IsNotAllowed()
    {
        var draft = TestData.ReadyDraft();
        draft.PublishedAt = DateTimeOffset.Now;
        var app = await FakeApp.ProAsync(draft);

        await app.Pro.RemoveWorkPhotoAsync(TestData.PhotoId(2));

        app.Pro.CanRemoveWorkPhoto.Should().BeFalse();
        app.Pro.Draft.WorkPhotoIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddReference_OnPublishedListing_RepublishesSoTheLiveListingKeepsUp()
    {
        var draft = TestData.ReadyDraft();
        draft.PublishedAt = DateTimeOffset.Now;
        var app = await FakeApp.ProAsync(draft);

        await app.Pro.AddReferenceAsync("Mang Jose", "09182222222");

        app.Profiles.Published.Should().ContainSingle()
            .Which.References.Should().HaveCount(2);
    }
}
