using System.Linq.Expressions;
using System.Text.Json;

namespace Kiosk.Api.Reviews;

/// <summary>Reads the review selector. Only a plain review id is accepted.</summary>
public static class ReviewFilter
{
    public static Expression<Func<Review, bool>> ById(JsonElement selector)
    {
        if (selector.ValueKind == JsonValueKind.Number && selector.TryGetInt32(out var id))
        {
            return r => r.Id == id;
        }

        throw new FormatException("The review selector must be a review id.");
    }
}
