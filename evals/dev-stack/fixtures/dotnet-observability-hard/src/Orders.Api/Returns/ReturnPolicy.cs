namespace Orders.Api.Returns;

public static class ReturnPolicy
{
    public static readonly TimeSpan Window = TimeSpan.FromDays(30);

    public static ReturnDecision Check(DateTimeOffset? dispatchedAt, DateTimeOffset now, ReturnReason reason)
    {
        if (dispatchedAt is null)
        {
            return ReturnDecision.NotShipped;
        }

        // Damaged and wrong items can always come back; other reasons only inside the window.
        if (reason is ReturnReason.Damaged or ReturnReason.WrongItem)
        {
            return ReturnDecision.Allowed;
        }

        return now - dispatchedAt.Value <= Window ? ReturnDecision.Allowed : ReturnDecision.WindowClosed;
    }
}

public enum ReturnDecision
{
    Allowed,
    NotShipped,
    WindowClosed,
}
