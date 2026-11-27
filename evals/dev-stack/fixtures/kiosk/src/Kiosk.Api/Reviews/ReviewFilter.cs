using System.Linq.Expressions;
using System.Text.Json;

namespace Kiosk.Api.Reviews;

/// <summary>
/// Reads the review selector of the moderation tool. A plain number selects one review. An object with an
/// operator ($ne, $in, $gt, $lt) selects a range, so moderators can edit a batch in one call.
/// </summary>
public static class ReviewFilter
{
    public static Expression<Func<Review, bool>> ById(JsonElement selector)
    {
        if (selector.ValueKind == JsonValueKind.Number)
        {
            var id = selector.GetInt32();
            return r => r.Id == id;
        }

        if (selector.ValueKind == JsonValueKind.Object)
        {
            foreach (var op in selector.EnumerateObject())
            {
                switch (op.Name)
                {
                    case "$ne":
                        var ne = op.Value.GetInt32();
                        return r => r.Id != ne;
                    case "$gt":
                        var gt = op.Value.GetInt32();
                        return r => r.Id > gt;
                    case "$lt":
                        var lt = op.Value.GetInt32();
                        return r => r.Id < lt;
                    case "$in":
                        var ids = op.Value.EnumerateArray().Select(v => v.GetInt32()).ToList();
                        return r => ids.Contains(r.Id);
                }
            }
        }

        throw new FormatException("The review selector is not valid.");
    }
}
