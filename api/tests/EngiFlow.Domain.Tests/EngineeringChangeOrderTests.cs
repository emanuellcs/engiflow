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
    public void ValidTransitionPath_ReachesApprovedAndRecordsReviewAuditTrail()
    {
        var eco = CreateEco();

        eco.SubmitForReview(RequesterId);
        eco.Approve(ApproverId);

        Assert.Equal(EcoStatus.Approved, eco.Status);
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
                Assert.Equal(EcoEventType.ReviewDecisionSubmitted, e.EventType);
                Assert.Equal(EcoStatus.UnderReview, e.OldStatus);
                Assert.Equal(EcoStatus.UnderReview, e.NewStatus);
            },
            e =>
            {
                Assert.Equal(EcoEventType.Approved, e.EventType);
                Assert.Equal(EcoStatus.UnderReview, e.OldStatus);
                Assert.Equal(EcoStatus.Approved, e.NewStatus);
            });
    }

    [Fact]
    public void Approve_FromDraft_ThrowsAndDoesNotAppendAuditEvent()
    {
        var eco = CreateEco();

        var exception = Assert.Throws<DomainException>(() => eco.Approve(ApproverId));

        Assert.Contains("under review", exception.Message);
        Assert.Equal(EcoStatus.Draft, eco.Status);
        Assert.Single(eco.Events);
    }

    [Fact]
    public void SubmitReviewDecision_ByEcoAuthor_ThrowsComplianceException()
    {
        var eco = CreateEco();
        eco.SubmitForReview(RequesterId);

        var exception = Assert.Throws<DomainException>(() =>
            eco.SubmitReviewDecision(RequesterId, EcoApprovalDecision.Approve, 1));

        Assert.Equal(
            "Compliance Rule: The author of the ECO cannot participate in its approval quorum",
            exception.Message);
        Assert.Equal(EcoStatus.UnderReview, eco.Status);
        Assert.Empty(eco.Approvals);
    }

    [Fact]
    public void Reject_FromUnderReview_ReturnsEcoToDraftForRevision()
    {
        var eco = CreateEco();
        eco.SubmitForReview(RequesterId);

        eco.Reject(ReviewerId, "Specification is incomplete.");

        Assert.Equal(EcoStatus.Draft, eco.Status);
        Assert.Equal(4, eco.Events.Count);
        Assert.Single(eco.Comments);
        Assert.False(eco.CanTransitionTo(EcoStatus.Approved));
        Assert.Throws<DomainException>(() => eco.Approve(ApproverId));
        Assert.Throws<DomainException>(() => eco.Implement(ApproverId));
    }

    [Fact]
    public void ApprovedEco_IsTerminalAndNotEditable()
    {
        var eco = CreateEco();
        eco.SubmitForReview(RequesterId);
        eco.Approve(ApproverId);

        Assert.False(eco.CanTransitionTo(EcoStatus.Rejected));
        Assert.Throws<DomainException>(() => eco.Reject(ReviewerId));
        Assert.Throws<DomainException>(() => eco.Implement(ApproverId));
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
    public void ReviewRounds_DoNotCountOldApprovalsAfterChangesAreRequested()
    {
        var eco = CreateEco();
        eco.SubmitForReview(RequesterId);
        eco.SubmitReviewDecision(ApproverId, EcoApprovalDecision.Approve, minApprovalsRequired: 2);
        eco.SubmitReviewDecision(ReviewerId, EcoApprovalDecision.RequestChanges, minApprovalsRequired: 2, "Revise drawing.");

        eco.SubmitForReview(RequesterId);
        eco.SubmitReviewDecision(ReviewerId, EcoApprovalDecision.Approve, minApprovalsRequired: 2);

        Assert.Equal(EcoStatus.UnderReview, eco.Status);

        eco.SubmitReviewDecision(ApproverId, EcoApprovalDecision.Approve, minApprovalsRequired: 2);

        Assert.Equal(EcoStatus.Approved, eco.Status);
        Assert.Equal(2, eco.ReviewRound);
        Assert.Equal(4, eco.Approvals.Count);
    }

    [Fact]
    public void DraftArtifactsAndComments_AreCapturedInTimeline()
    {
        var eco = CreateEco();

        var affectedItem = eco.AddAffectedItem(
            "BRK-1001",
            "Load-bearing bracket",
            "A",
            "B",
            EcoAffectedItemAction.Modify,
            RequesterId);
        var attachment = eco.AddAttachment(
            "analysis.pdf",
            1024,
            "tenants/company/ecos/eco/attachments/analysis.pdf",
            "application/pdf",
            RequesterId);
        var comment = eco.AddComment("Updated tolerance analysis is attached.", RequesterId);

        Assert.Equal(affectedItem, Assert.Single(eco.AffectedItems));
        Assert.Equal(attachment, Assert.Single(eco.Attachments));
        Assert.Equal(comment, Assert.Single(eco.Comments));
        Assert.Contains(eco.Events, ecoEvent => ecoEvent.EventType == EcoEventType.AffectedItemAdded);
        Assert.Contains(eco.Events, ecoEvent => ecoEvent.EventType == EcoEventType.AttachmentAdded);
        Assert.Contains(eco.Events, ecoEvent => ecoEvent.EventType == EcoEventType.CommentAdded);
    }

    [Fact]
    public void Cancel_FromUnderReview_IsTerminal()
    {
        var eco = CreateEco();
        eco.SubmitForReview(RequesterId);

        eco.Cancel(RequesterId);

        Assert.Equal(EcoStatus.Canceled, eco.Status);
        Assert.False(eco.CanTransitionTo(EcoStatus.Draft));
        Assert.Throws<DomainException>(() => eco.AddComment("Cannot comment.", RequesterId));
        Assert.Throws<DomainException>(() => eco.Approve(ApproverId));
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
