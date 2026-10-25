namespace Orders.Api.Returns;

public enum ReturnReason
{
    Damaged = 0,
    WrongItem = 1,
    NotAsDescribed = 2,
    NoLongerNeeded = 3,
}

public enum ReturnStatus
{
    Requested = 0,
    Approved = 1,
    Rejected = 2,
}
