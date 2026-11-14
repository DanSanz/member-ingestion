namespace Scheme.Common.Audit;

/// <summary>
/// One audit event. The audit store is readable by the service desk, so <see cref="Detail"/> must
/// never contain personal data (dates of birth, NI numbers, names, bank details). Refer to a member
/// by member reference.
/// </summary>
public sealed record AuditEvent(
    string EventType,
    string SubjectRef,
    string Detail,
    string? Actor,
    string? RuleId,
    DateTimeOffset RecordedAt);

/// <summary>Where a service records audit events. Production uses <see cref="HttpAuditLog"/>.</summary>
public interface IAuditLog
{
    /// <summary>Records one audit event and returns it.</summary>
    /// <param name="eventType">What happened, in snake_case, for example <c>member_file_loaded</c>.</param>
    /// <param name="subjectRef">What it happened to: a file ID or a member reference. Never personal data.</param>
    /// <param name="detail">A short description. Never personal data.</param>
    /// <param name="actor">The person or service account responsible.</param>
    /// <param name="ruleId">
    /// The business rule that caused the event, where one did. The quarterly audit check fails events
    /// raised by a rule that do not carry both <paramref name="actor"/> and <paramref name="ruleId"/>.
    /// </param>
    AuditEvent Record(string eventType, string subjectRef, string detail, string? actor = null, string? ruleId = null);
}

/// <summary>Validates and timestamps events; subclasses decide where they go.</summary>
public abstract class AuditLogBase(TimeProvider? clock) : IAuditLog
{
    private readonly TimeProvider clock = clock ?? TimeProvider.System;

    public AuditEvent Record(string eventType, string subjectRef, string detail, string? actor = null, string? ruleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectRef);
        var auditEvent = new AuditEvent(eventType, subjectRef, detail, actor, ruleId, clock.GetUtcNow());
        Write(auditEvent);
        return auditEvent;
    }

    protected abstract void Write(AuditEvent auditEvent);
}

/// <summary>Keeps events in memory. For tests and dry runs.</summary>
public sealed class InMemoryAuditLog(TimeProvider? clock = null) : AuditLogBase(clock)
{
    private readonly List<AuditEvent> events = [];
    private readonly Lock gate = new();

    public IReadOnlyList<AuditEvent> Events
    {
        get { lock (gate) { return [.. events]; } }
    }

    public IReadOnlyList<AuditEvent> OfType(string eventType) =>
        [.. Events.Where(e => e.EventType == eventType)];

    public void Clear()
    {
        lock (gate) { events.Clear(); }
    }

    protected override void Write(AuditEvent auditEvent)
    {
        lock (gate) { events.Add(auditEvent); }
    }
}
