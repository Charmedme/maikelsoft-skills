namespace Orders.Api.EvalProbes;

// Eval probe for dotnet-analyzers-trap. Only Meziantou.Analyzer reports this line (MA0006, warning by default).
internal static class MeziantouProbe
{
    internal static bool SameSku(string left, string right) => left == right;
}
