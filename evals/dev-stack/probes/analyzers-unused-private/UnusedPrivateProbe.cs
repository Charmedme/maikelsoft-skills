namespace Orders.Api.EvalProbes;

// Eval probe for dotnet-analyzers-trap. The private method is never called (IDE0051 or RCS1213).
internal static class UnusedPrivateProbe
{
    internal static int Value => 1;

    private static int Helper() => 2;
}
