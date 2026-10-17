using System.Text;

namespace Orders.Api.EvalProbes;

// Eval probe for dotnet-analyzers-trap. The using directive above is not used (IDE0005).
internal static class UnusedUsingProbe
{
    internal static int Value => 1;
}
