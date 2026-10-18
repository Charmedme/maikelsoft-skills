namespace Orders.Api.EvalProbes;

// Eval probe for dotnet-analyzers-trap. Only RCS1124 (inline local variable) reports this code.
// RCS1124 is Hidden by default. It shows as a warning only when the whole Roslynator category is raised.
internal static class HiddenRuleProbe
{
    internal static int Sum(int left, int right)
    {
        var total = left + right;
        return total;
    }
}
