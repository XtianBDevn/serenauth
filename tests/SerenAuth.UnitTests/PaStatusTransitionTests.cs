using FluentAssertions;
using SerenAuth.Domain.Entities;
using SerenAuth.Domain.Enums;
using SerenAuth.Domain.ValueObjects;
using Xunit;

namespace SerenAuth.UnitTests;

public class PaStatusTransitionTests
{
    private static PriorAuthorization NewDraft() => PriorAuthorization.CreateDraft(
        organizationId: "org1",
        patientId: "p1",
        providerId: "pr1",
        cpt: CptCode.Create("90935"),
        icd10: Icd10Code.Create("N18.6"),
        payer: Payer.Create("BCBS"),
        aiConfidence: 0.9);

    [Fact]
    public void New_drafts_start_in_draft_status()
    {
        NewDraft().Status.Should().Be(PaStatus.Draft);
    }

    [Fact]
    public void Submit_transitions_draft_to_pending()
    {
        var pa = NewDraft();
        pa.Submit();
        pa.Status.Should().Be(PaStatus.Pending);
    }

    [Fact]
    public void Submit_twice_throws()
    {
        var pa = NewDraft();
        pa.Submit();
        var act = () => pa.Submit();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approve_requires_pending()
    {
        var pa = NewDraft();
        var act = () => pa.Approve();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approve_after_submit_succeeds()
    {
        var pa = NewDraft();
        pa.Submit();
        pa.Approve();
        pa.Status.Should().Be(PaStatus.Approved);
    }

    [Fact]
    public void Deny_after_submit_succeeds()
    {
        var pa = NewDraft();
        pa.Submit();
        pa.Deny();
        pa.Status.Should().Be(PaStatus.Denied);
    }

    [Fact]
    public void Cannot_re_submit_an_approved_authorization()
    {
        var pa = NewDraft();
        pa.Submit();
        pa.Approve();
        var act = () => pa.Submit();
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void AiConfidence_must_be_in_range(double bad)
    {
        var act = () => PriorAuthorization.CreateDraft(
            "org1", "p1", "pr1",
            CptCode.Create("90935"),
            Icd10Code.Create("N18.6"),
            Payer.Create("BCBS"),
            bad);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
