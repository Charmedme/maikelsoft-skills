"""The scenarios of the review-flow comparison: small repositories with planted problems.

Each scenario builds on the orders-api fixture of evals/dev-stack. The story is ORD-17 (cancel an order).
    story     no prepared code. The arm delivers the story.
    review    a built core with planted problems and a migration, in a run of mode light, at step 6.
    comments  a run after G2, with one earlier round and a new batch of pull request comments.
    cap       the same run with 3 earlier rounds. The new version shows the count and goes on.
    recur     the same run, and one comment says that an earlier fix broke the behavior. The new version must stop.
"""
import pathlib
import subprocess
import sys

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(ROOT / "evals" / "dev-stack"))
import prepare  # noqa: E402

SLUG = "ord-17-cancel"
STORY = ROOT / "evals" / "dev-team" / "stories" / "cancel-order.md"
ANSWERS = ROOT / "evals" / "dev-team" / "stories" / "cancel-order.answers.md"
ENDPOINTS = "src/Orders.Api/Orders/OrderEndpoints.cs"

# On main before the story: the total ignores the lines, because they are not loaded.
OLDER_ENDPOINT = """
    private static async Task<Results<Ok<decimal>, NotFound>> GetTotalAsync(
        Guid id,
        OrdersDbContext db,
        CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(order => order.Id == id, ct);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(order.Total);
    }
"""
# The built core. P1: a shipped order returns 200. P2: the window ignores the options. N1 to N3 are nits.
CANCEL_ENDPOINT = """
    private static async Task<IResult> CancelAsync(Guid id, OrdersDbContext db, TimeProvider time, CancellationToken ct)
    {
        // get the order
        var o = await db.Orders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (o is null)
        {
            return TypedResults.NotFound();
        }

        var outcome = o.Cancel(time.GetUtcNow(), TimeSpan.FromHours(24));
        if (outcome == CancelOutcome.WindowClosed)
        {
            return TypedResults.Problem(title: "The cancel window is closed.", statusCode: StatusCodes.Status409Conflict);
        }

        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(o.ToResponse());
    }
"""
CANCEL_TESTS = """using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CancelEndpointTests
{
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_client = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        await s_factory.MigrateAsync(context.CancellationToken);
        s_client = s_factory.CreateClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task Cancel_PlacedOrder_ReturnsCancelled()
    {
        var created = await s_client.PostAsJsonAsync(
            "/orders",
            new CreateOrderRequest(Guid.CreateVersion7(), [new CreateOrderLineRequest("SKU-1", 1, 1m)]),
            TestContext.CancellationToken);
        var order = await created.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken);

        var response = await s_client.PostAsync($"/orders/{order!.Id}/cancel", null, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelled = await response.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken);
        cancelled!.Status.Should().Be("Cancelled");
    }

    [TestMethod]
    public async Task Cancel_UnknownOrder_Returns404()
    {
        var response = await s_client.PostAsync($"/orders/{Guid.CreateVersion7()}/cancel", null, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
"""
MIGRATION = """using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Orders.Api.Data;

#nullable disable

namespace Orders.Api.Data.Migrations
{
    [DbContext(typeof(OrdersDbContext))]
    [Migration("20261030120000_OrderStatusIndex")]
    public partial class OrderStatusIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE INDEX ix_status_placed ON orders (\\"Status\\", \\"PlacedAt\\");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX ix_status_placed;");
        }
    }
}
"""
RUN_MD = """# Run: {slug}

| Field | Value |
|-------|-------|
| Story | Cancel an order (ORD-17) |
| Step | {step} |
| Status | {status} |
| Branch | dev-team/{slug} |
| Base | {{BASE}} |
| G1 | approved by the human on 2026-10-30 |
| G2 | {g2} |
| Answer sheet | none |
| Mode | light |

## Counters

| Counter | Limit | Value |
|---------|-------|-------|
| design-reflection | 1 | 0 |
| core-review | 1 | {core_review} |
| core-fix | 1 | 0 |
| security-review | 1 | 0 |
| security-fix | 1 | 0 |
| ux-review | 1 | 0 |
| ux-fix | 1 | 0 |
{followups}"""
DESIGN = """# Design: Cancel an order

Status: DESIGN_READY

## Summary
A new endpoint POST /orders/{id}/cancel calls the existing method Order.Cancel. The human also asked for an index on status and placed time for the new support report, so the change holds one small migration.

## Design
One endpoint in OrderEndpoints.cs. Errors use problem details. Expected failures are result values. The migration OrderStatusIndex adds the index that the human asked for.

## How to undo
Revert the commit. The migration has a Down step.

## Work split
- Core (senior): OrderEndpoints.cs, its tests, and the migration.
- Packages: None.
- Mode: light. No new package, no work packages, no change to sign-in or an outbound call. An index is a small change, so the senior did not count it as a schema change.

## Decisions
None.

## Assumptions to confirm
None.

## Questions for the human
None.
"""
LOG_HEAD = """# Log: {slug}

| Time | Agent | Step | Status | Files | Commit | Checks | Decisions | Assumptions | Reason |
|------|-------|------|--------|-------|--------|--------|-----------|-------------|--------|
| 10:00 | main | start | STARTED | run.md | - | - | - | - | run started |
| 10:05 | senior | design | DESIGN_READY | design.md | - | - | - | - | design done |
| 10:20 | main | G1 | G1_APPROVED | decision-sheet.md | - | - | - | - | approved |
| 10:50 | senior | build-core | CORE_DONE | core-report.md | - | - | - | - | core built |
"""
ROUND_RESOLVED = """# Follow-up {nn}: comments

| Thread | Location | Text | Class | Reason |
|--------|----------|------|-------|--------|
| T{nn}1 | OrderEndpoints.cs | {text} | {klass} | {reason} |
| T{nn}2 | CancelEndpointTests.cs | Add a test for an unknown id, case {nn}. | fix | Done in this round. |
"""
COMMENTS = """T41, OrderEndpoints.cs, CancelAsync. A shipped order gets 200 back and looks cancelled. Criterion 2 asks for 409.
T42, OrderEndpoints.cs, CancelAsync. nit: please rename `o` to `order`, and remove the comment "get the order".
T43, OrderEndpoints.cs, GetTotalAsync. This endpoint always returns 0, because the lines are not loaded. Line is from before this PR.
T44, OrderEndpoints.cs, CancelAsync. The closed window case must return 400, not 409.
"""
RECUR_COMMENT = "T45, OrderEndpoints.cs, CancelAsync. The log line that follow-up 02 added broke the behavior: the fix logs each cancel before the save, so a failed save still logs a cancel.\n"


def git(repo, *args):
    cmd = ["git", "-c", "user.name=Dev", "-c", "user.email=dev@example.com", "-c", "commit.gpgsign=false", "-C", str(repo), *args]
    return subprocess.run(cmd, check=True, capture_output=True, text=True).stdout.strip()


def write(repo, rel, text):
    path = repo / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def add_endpoint(repo, route, method):
    path = repo / ENDPOINTS
    text = path.read_text(encoding="utf-8")
    text = text.replace('        return app;\n', f'        {route}\n        return app;\n', 1)
    index = text.rindex("}")
    path.write_text(text[:index].rstrip("\n") + "\n" + method + "}\n", encoding="utf-8")


def base_repo(repo):
    """Main with the older bug, then the branch of the story."""
    prepare.make_repo({"kind": "write", "fixture": "orders-api"}, repo)
    add_endpoint(repo, 'group.MapGet("/{id:guid}/total", GetTotalAsync);', OLDER_ENDPOINT)
    git(repo, "add", "-A")
    git(repo, "commit", "-qm", "Order total endpoint")
    base = git(repo, "rev-parse", "HEAD")
    git(repo, "switch", "-qc", f"dev-team/{SLUG}")
    return base


def built_core(repo, migration):
    add_endpoint(repo, 'group.MapPost("/{id:guid}/cancel", CancelAsync);', CANCEL_ENDPOINT)
    write(repo, "tests/Orders.Api.Tests/CancelEndpointTests.cs", CANCEL_TESTS)
    if migration:
        write(repo, "src/Orders.Api/Data/Migrations/20261030120000_OrderStatusIndex.cs", MIGRATION)
    git(repo, "add", "-A")
    git(repo, "commit", "-qm", "Add the cancel endpoint")


def run_folder(repo, base, step, status, g2, core_review, followups, log):
    folder = f"docs/work/{SLUG}"
    run_md = RUN_MD.format(slug=SLUG, step=step, status=status, g2=g2, core_review=core_review, followups=followups)
    write(repo, f"{folder}/run.md", run_md.replace("{BASE}", base))
    write(repo, f"{folder}/story.md", STORY.read_text(encoding="utf-8"))
    write(repo, f"{folder}/design.md", DESIGN)
    write(repo, f"{folder}/decision-sheet.md", "# Decision sheet\n\nNone open.\n")
    write(repo, f"{folder}/core-report.md", "# Core report\n\nStatus: CORE_DONE\n\nThe endpoint and two tests are built.\n")
    write(repo, f"{folder}/log.md", LOG_HEAD.format(slug=SLUG) + log)
    return folder


def after_g2(repo, rounds):
    base = base_repo(repo)
    built_core(repo, migration=False)
    rows = ['| 11:10 | main | G2 | G2_SHOWN | g2.md | - | - | - | - | shown |\n']
    counters = ""
    for n in range(1, rounds + 1):
        nn = f"{n:02d}"
        text, klass, reason = (("The window closed case must return 400, not 409.", "reject", "Criterion 3 asks for 409.") if n == 1
                               else ("Add a log line for each cancel.", "fix", "Done in this round."))
        write(repo, f"docs/work/{SLUG}/follow-ups/{nn}-comments.md", ROUND_RESOLVED.format(nn=nn, text=text, klass=klass, reason=reason))
        write(repo, f"docs/work/{SLUG}/follow-ups/{nn}-replies.md", f"# Replies {nn}\n\nDone.\n")
        rows += [f"| 12:0{n} | main | followup {nn} | FOLLOWUP_STARTED | - | - | - | - | - | started |\n",
                 f"| 12:1{n} | main | followup {nn} | FOLLOWUP_DONE | - | - | - | - | - | done |\n"]
        counters += f"| followup {nn} fix | 1 | 1 |\n| followup {nn} review | 1 | 1 |\n"
    run_folder(repo, base, "after G2", "FOLLOWUP_DONE", "shown on 2026-10-30", 1, counters, "".join(rows))
    write(repo, f"docs/work/{SLUG}/core-review.md", "# Review: core\n\nStatus: APPROVED\n")
    git(repo, "add", "-A")
    git(repo, "commit", "-qm", "Run folder of the story")


def prepare_scenario(name, repo):
    if name == "story":
        prepare.make_repo({"kind": "write", "fixture": "orders-api"}, repo)
    elif name == "review":
        base = base_repo(repo)
        built_core(repo, migration=True)
        run_folder(repo, base, "6 Core", "CORE_DONE", "open", 0, "", "")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "Run folder of the story")
    elif name == "comments":
        after_g2(repo, 1)
    elif name == "cap":
        after_g2(repo, 3)
    elif name == "recur":
        after_g2(repo, 3)
        path = repo / ENDPOINTS
        text = path.read_text(encoding="utf-8")
        write(repo, ENDPOINTS, text.replace("        await db.SaveChangesAsync(ct);\n        return TypedResults.Ok(o.ToResponse());",
                                            '        Console.WriteLine($"Order {o.Id} cancelled.");\n        await db.SaveChangesAsync(ct);\n        return TypedResults.Ok(o.ToResponse());'))
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "Follow-up 02: log each cancel")
    else:
        raise ValueError(name)
    git(repo, "tag", "eval-start")


AFTER = (" Nobody can answer questions in this run. Give your result and any question for the human in your final message."
         " The answer sheet of the human is {answers}. Take each answer of the human from that sheet.")
SUFFIX = " Do not push. Do not call any tool whose name starts with mcp__."
PROMPTS = {
    "story": {
        "team": "Use the dev-team:deliver-story skill to deliver the story in {story}.",
        "baseline": "Deliver the story in {story}. Work on a new branch and commit.",
    },
    "review": {
        "team": f"Use the dev-team:deliver-story skill to continue the run in docs/work/{SLUG}. G1 is approved and the core is built, so restart at step 6.",
        "baseline": f"The branch dev-team/{SLUG} holds a built change for the story in docs/work/{SLUG}/story.md. Review it against main, fix what must be fixed before a merge, commit, and tell me if it is ready to push.",
    },
    "comments": {
        "team": f"Use the dev-team:process-review skill for the run in docs/work/{SLUG}. These are the new review comments of the pull request:\n{COMMENTS}",
        "baseline": f"The branch dev-team/{SLUG} is a pull request for the story in docs/work/{SLUG}/story.md. These are the new review comments. Handle them, commit, and draft a reply for each thread:\n{COMMENTS}",
    },
}
PROMPTS["cap"] = PROMPTS["comments"]
PROMPTS["recur"] = {"team": PROMPTS["comments"]["team"] + RECUR_COMMENT, "baseline": PROMPTS["comments"]["baseline"] + RECUR_COMMENT}


def prompt_for(name, arm, folder):
    """The prompt names the input by its copy in the folder of the run, so that it shows no path of this repository."""
    folder = pathlib.Path(folder)
    folder.mkdir(parents=True, exist_ok=True)
    (folder / "story.md").write_text(STORY.read_text(encoding="utf-8"), encoding="utf-8")
    (folder / "answers.md").write_text(ANSWERS.read_text(encoding="utf-8"), encoding="utf-8")
    key = "baseline" if arm == "baseline" else "team"
    text = PROMPTS[name][key].replace("{story}", str(folder / "story.md"))
    return text + AFTER.replace("{answers}", str(folder / "answers.md")) + SUFFIX
