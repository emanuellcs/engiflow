using EngiFlow.Domain.Ecos;
using EngiFlow.Domain.Exceptions;
using EngiFlow.Domain.ValueObjects;

namespace EngiFlow.Domain.Tests;

public sealed class EngineeringChangeOrderTests
{
    private static readonly CompanyId CompanyId = CompanyId.New();
    private static readonly UserId RequesterId = UserId.New();
    private static readonly UserId ReviewerId = UserId.New();
    private static readonly UserId ApproverId = UserId.New();

    [Fact]
    public void Create_StartsAsDraftAndWritesCreationEvent()
    {
        var eco = CreateEco();

        var created = Assert.Single(eco.Events);

        Assert.Equal(CompanyId, eco.CompanyId);
        Assert.Equal(EcoStatus.Draft, eco.Status);
        Assert.Equal(EcoEventType.Created, created.EventType);
        Assert.Null(created.OldStatus);
        Assert.Equal(EcoStatus.Draft, created.NewStatus);
        Assert.Equal(RequesterId, created.ActorUserId);
        Assert.Equal(CompanyId, created.CompanyId);
        Assert.Equal(eco.Id, created.EngineeringChangeOrderId);
    }

    [Fact]
    public void ValidTransitionPath_ReachesImplementedAndRecordsAuditTrail()
    {
        var eco = CreateEco();

        eco.SubmitForReview(RequesterId);
        eco.Approve(ApproverId);
        eco.Implement(ApproverId);

        Assert.Equal(EcoStatus.Implemented, eco.Status);
        Assert.Collection(
            eco.Events,
            e => Assert.Equal(EcoEventType.Created, e.EventType),
            e =>
            {
                Assert.Equal(EcoEventType.SubmittedForReview, e.EventType);
                Assert.Equal(EcoStatus.Draft, e.OldStatus);
                Assert.Equal(EcoStatus.UnderReview, e.NewStatus);
            },
            e =>
            {
                Assert.Equal(EcoEventType.Approved, e.EventType);
                Assert.Equal(EcoStatus.UnderReview, e.OldStatus);
                Assert.Equal(EcoStatus.Approved, e.NewStatus);
            },
            e =>
            {
                Assert.Equal(EcoEventType.Implemented, e.EventType);
                Assert.Equal(EcoStatus.Approved, e.OldStatus);
                Assert.Equal(EcoStatus.Implemented, e.NewStatus);
            });
    }

    [Fact]
    public void Approve_FromDraft_ThrowsAndDoesNotAppendAuditEvent()
    {
        var eco = CreateEco();

        var exception = Assert.Throws<DomainException>(() => eco.Approve(ApproverId));

        Assert.Contains("cannot transition", exception.Message);
        Assert.Equal(EcoStatus.Draft, eco.Status);
        Assert.Single(eco.Events);
    }

    [Fact]
    public void Reject_FromUnderReview_IsTerminal()
    {
        var eco = CreateEco();
        eco.SubmitForReview(RequesterId);

        eco.Reject(ReviewerId, "Specification is incomplete.");

        Assert.Equal(EcoStatus.Rejected, eco.Status);
        Assert.Equal(3, eco.Events.Count);
        Assert.False(eco.CanTransitionTo(EcoStatus.Approved));
        Assert.Throws<DomainException>(() => eco.Approve(ApproverId));
        Assert.Throws<DomainException>(() => eco.Implement(ApproverId));
    }

    [Fact]
    public void ImplementedEco_IsTerminalAndNotEditable()
    {
        var eco = CreateEco();
        eco.SubmitForReview(RequesterId);
        eco.Approve(ApproverId);
        eco.Implement(ApproverId);

        Assert.False(eco.CanTransitionTo(EcoStatus.Rejected));
        Assert.Throws<DomainException>(() => eco.Reject(ReviewerId));
        Assert.Throws<DomainException>(() => eco.UpdateDetails(
            "Use titanium bracket",
            "Update load-bearing bracket material.",
            EcoPriority.Critical,
            RequesterId));
    }

    [Fact]
    public void UpdateDetails_WhenDraft_RecordsAuditEvent()
    {
        var eco = CreateEco();

        eco.UpdateDetails(
            "Use reinforced aluminum bracket",
            "Update load-bearing bracket material and torque values.",
            EcoPriority.High,
            RequesterId);

        var update = eco.Events.Last();
        Assert.Equal("Use reinforced aluminum bracket", eco.Title);
        Assert.Equal(EcoPriority.High, eco.Priority);
        Assert.Equal(EcoEventType.DetailsUpdated, update.EventType);
        Assert.Equal(EcoStatus.Draft, update.OldStatus);
        Assert.Equal(EcoStatus.Draft, update.NewStatus);
    }

    [Fact]
    public void SubmitForReview_WithEmptyActor_Throws()
    {
        var eco = CreateEco();

        Assert.Throws<DomainException>(() => eco.SubmitForReview(default));
    }

    private static EngineeringChangeOrder CreateEco()
    {
        return EngineeringChangeOrder.Create(
            CompanyId,
            "Use aluminum bracket",
            "Update load-bearing bracket material from steel to aluminum.",
            EcoPriority.Medium,
            RequesterId);
    }
}
