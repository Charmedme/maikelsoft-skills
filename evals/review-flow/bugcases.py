"""Six small review cases for the bug-finding test: five with one real bug, and one clean change.

Each case starts from the orders-api fixture. Main can hold old code, and the branch holds the change of the
story. The tests of the branch pass, so only a review can find the bug. KEY is for the grader, and no run sees it.
"""
import pathlib

import prepare
import scenarios
from scenarios import ENDPOINTS, git, write

SLUG = "review-case"
FIND = """using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Orders;

internal static class OrderQueries
{
    public static Task<Order?> FindAsync(OrdersDbContext db, Guid id, CancellationToken ct) =>
        db.Orders.AsNoTracking().FirstOrDefaultAsync(order => order.Id == id, ct);
}
"""
STATUS_ENDPOINT = """
    private static async Task<Results<Ok<string>, NotFound>> GetStatusAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var order = await OrderQueries.FindAsync(db, id, ct);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(order.Status.ToString());
    }
"""
TOTAL_ENDPOINT = """
    private static async Task<Results<Ok<decimal>, NotFound>> GetTotalAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var order = await OrderQueries.FindAsync(db, id, ct);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(order.Total);
    }
"""
DEADLINE = """namespace Orders.Api.Orders;

internal static class OrderDeadlines
{
    public static DateTimeOffset CancelDeadline(this Order order, TimeSpan window) =>
        new DateTimeOffset(order.PlacedAt.UtcDateTime.Date, TimeSpan.Zero) + window;
}
"""
INFO_ENDPOINT = """
    private static async Task<Results<Ok<DateOnly>, NotFound>> GetLastCancelDayAsync(Guid id, OrdersDbContext db, IOptions<OrdersOptions> options, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(order => order.Id == id, ct);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(DateOnly.FromDateTime(order.CancelDeadline(options.Value.CancelWindow).UtcDateTime));
    }
"""
CANCEL_WITH_DEADLINE = """
    private static async Task<Results<Ok<OrderResponse>, NotFound, ProblemHttpResult>> CancelAsync(Guid id, OrdersDbContext db, TimeProvider time, IOptions<OrdersOptions> options, CancellationToken ct)
    {
        var order = await db.Orders.Include(order => order.Lines).FirstOrDefaultAsync(order => order.Id == id, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        var window = options.Value.CancelWindow;
        if (time.GetUtcNow() > order.CancelDeadline(window))
        {
            return TypedResults.Problem(title: "The cancel window is closed.", statusCode: StatusCodes.Status409Conflict);
        }

        if (order.Cancel(time.GetUtcNow(), window) == CancelOutcome.AlreadyShipped)
        {
            return TypedResults.Problem(title: "The order is shipped.", statusCode: StatusCodes.Status409Conflict);
        }

        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(order.ToResponse());
    }
"""
FACTORY = """using Microsoft.Extensions.Options;

namespace Orders.Api.Orders;

public sealed class OrderFactory(TimeProvider time, IOptions<OrdersOptions> options)
{
    public bool TryCreate(CreateOrderRequest request, out Order? order, out string? error)
    {
        order = null;
        error = null;
        if (request.Lines.Count >= options.Value.MaxLinesPerOrder)
        {
            error = $"An order can have at most {options.Value.MaxLinesPerOrder} lines.";
            return false;
        }

        order = new Order(request.CustomerId!.Value, time.GetUtcNow());
        foreach (var line in request.Lines)
        {
            order.AddLine(line.Sku, line.Quantity, line.UnitPrice);
        }

        return true;
    }
}
"""
RACE_ENDPOINTS = """
    private static async Task<Results<Ok<OrderResponse>, NotFound, ProblemHttpResult>> CancelAsync(Guid id, OrdersDbContext db, TimeProvider time, IOptions<OrdersOptions> options, CancellationToken ct)
    {
        var order = await db.Orders.Include(order => order.Lines).FirstOrDefaultAsync(order => order.Id == id, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        var outcome = order.Cancel(time.GetUtcNow(), options.Value.CancelWindow);
        if (outcome is CancelOutcome.AlreadyShipped or CancelOutcome.WindowClosed)
        {
            return TypedResults.Problem(title: "The order cannot be cancelled.", statusCode: StatusCodes.Status409Conflict);
        }

        return await SaveOrConflictAsync(order, db, ct);
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound, ProblemHttpResult>> ShipAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.Include(order => order.Lines).FirstOrDefaultAsync(order => order.Id == id, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        if (order.Status != OrderStatus.Placed)
        {
            return TypedResults.Problem(title: "Only a placed order can be shipped.", statusCode: StatusCodes.Status409Conflict);
        }

        order.MarkShipped();
        return await SaveOrConflictAsync(order, db, ct);
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound, ProblemHttpResult>> SaveOrConflictAsync(Order order, OrdersDbContext db, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Problem(title: "Another request changed the order.", statusCode: StatusCodes.Status409Conflict);
        }

        return TypedResults.Ok(order.ToResponse());
    }
"""
REFUND_ENDPOINT = """
    private static async Task<Results<Ok<decimal>, NotFound>> GetRefundPreviewAsync(Guid id, int percent, OrdersDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(order => order.Lines).FirstOrDefaultAsync(order => order.Id == id, ct);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(Math.Round(order.Total * percent / 100m, 2));
    }
"""
GET_ENDPOINT = """
    private static async Task<Results<Ok<OrderResponse>, NotFound>> GetByIdAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(order => order.Lines).FirstOrDefaultAsync(order => order.Id == id, ct);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(order.ToResponse());
    }
"""
TEST_HEAD = """using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CaseTests
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

    private async Task<OrderResponse> NewOrderAsync()
    {
        var created = await s_client.PostAsJsonAsync("/orders", new CreateOrderRequest(Guid.CreateVersion7(), [new CreateOrderLineRequest("SKU-1", 2, 10m)]), TestContext.CancellationToken);
        return (await created.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken))!;
    }
"""


def test_file(body):
    return TEST_HEAD + body + "}\n"


def replace_create(repo):
    """Move the body of CreateAsync into OrderFactory, in the way that the refactor of the case does."""
    path = repo / ENDPOINTS
    text = path.read_text(encoding="utf-8")
    start = text.index("    private static async Task<Results<Created<OrderResponse>, ValidationProblem>> CreateAsync(")
    end = text.index("    private static async Task<Ok<List<OrderResponse>>> ListForCustomerAsync(")
    path.write_text(text[:start] + """    private static async Task<Results<Created<OrderResponse>, ValidationProblem>> CreateAsync(
        CreateOrderRequest request,
        OrdersDbContext db,
        OrderFactory factory,
        CancellationToken ct)
    {
        if (!factory.TryCreate(request, out var order, out var error))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Lines)] = [error!] });
        }

        db.Orders.Add(order!);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/orders/{order!.Id}", order.ToResponse());
    }

""" + text[end:], encoding="utf-8")
    program = repo / "src/Orders.Api/Program.cs"
    source = program.read_text(encoding="utf-8")
    marker = "var app = builder.Build();"
    program.write_text(source.replace(marker, "builder.Services.AddScoped<OrderFactory>();\n\n" + marker, 1), encoding="utf-8")
    write(repo, "src/Orders.Api/Orders/OrderFactory.cs", FACTORY)


def old_status(repo):
    write(repo, "src/Orders.Api/Orders/OrderQueries.cs", FIND)
    scenarios.add_endpoint(repo, 'group.MapGet("/{id:guid}/status", GetStatusAsync);', STATUS_ENDPOINT)


def old_deadline(repo):
    write(repo, "src/Orders.Api/Orders/OrderDeadlines.cs", DEADLINE)
    scenarios.add_endpoint(repo, 'group.MapGet("/{id:guid}/last-cancel-day", GetLastCancelDayAsync);', INFO_ENDPOINT)


def old_total(repo):
    scenarios.add_endpoint(repo, 'group.MapGet("/{id:guid}/total", GetTotalAsync);', TOTAL_ENDPOINT.replace("await OrderQueries.FindAsync(db, id, ct)", "await db.Orders.AsNoTracking().FirstOrDefaultAsync(order => order.Id == id, ct)"))


def new_total(repo):
    scenarios.add_endpoint(repo, 'group.MapGet("/{id:guid}/total", GetTotalAsync);', TOTAL_ENDPOINT)


def new_cancel_deadline(repo):
    scenarios.add_endpoint(repo, 'group.MapPost("/{id:guid}/cancel", CancelAsync);', CANCEL_WITH_DEADLINE)


def new_race(repo):
    scenarios.add_endpoint(repo, 'group.MapPost("/{id:guid}/cancel", CancelAsync);\n        group.MapPost("/{id:guid}/ship", ShipAsync);', RACE_ENDPOINTS)


def new_refund(repo):
    scenarios.add_endpoint(repo, 'group.MapGet("/{id:guid}/refund-preview", GetRefundPreviewAsync);', REFUND_ENDPOINT)


def new_get(repo):
    scenarios.add_endpoint(repo, 'group.MapGet("/{id:guid}", GetByIdAsync);', GET_ENDPOINT)


CASES = {
    "c1-total": {
        "story": "Story ORD-30: Order total\n\nAs a support agent, I want the total of an order.\n\nAcceptance criteria:\n1. GET /orders/{id}/total returns 200 with the sum of the line totals of the order.\n2. An unknown order id returns 404.\n",
        "summary": "One endpoint that reuses OrderQueries.FindAsync, as the status endpoint does.",
        "old": old_status, "new": new_total,
        "tests": """
    [TestMethod]
    public async Task Total_KnownOrder_Returns200()
    {
        var order = await NewOrderAsync();
        var response = await s_client.GetAsync(new Uri($"/orders/{order.Id}/total", UriKind.Relative), TestContext.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Total_UnknownOrder_Returns404()
    {
        var response = await s_client.GetAsync(new Uri($"/orders/{Guid.CreateVersion7()}/total", UriKind.Relative), TestContext.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
""",
    },
    "c2-deadline": {
        "story": "Story ORD-31: Cancel an order\n\nAs a customer, I want to cancel my order within the cancel window.\n\nAcceptance criteria:\n1. POST /orders/{id}/cancel cancels a placed order until the cancel window has passed since the order was placed, and returns 200.\n2. After the window, the call returns 409 with a problem details body.\n3. A shipped order returns 409.\n4. An unknown order id returns 404.\n",
        "summary": "One endpoint. The window check reuses CancelDeadline, as the last-cancel-day endpoint does.",
        "old": old_deadline, "new": new_cancel_deadline,
        "tests": """
    [TestMethod]
    public async Task Cancel_InsideWindow_Returns200()
    {
        var order = await NewOrderAsync();
        s_factory.Time.Advance(TimeSpan.FromHours(1));
        var response = await s_client.PostAsync(new Uri($"/orders/{order.Id}/cancel", UriKind.Relative), null, TestContext.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Cancel_AfterWindow_Returns409()
    {
        var order = await NewOrderAsync();
        s_factory.Time.Advance(TimeSpan.FromHours(25));
        var response = await s_client.PostAsync(new Uri($"/orders/{order.Id}/cancel", UriKind.Relative), null, TestContext.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task Cancel_UnknownOrder_Returns404()
    {
        var response = await s_client.PostAsync(new Uri($"/orders/{Guid.CreateVersion7()}/cancel", UriKind.Relative), null, TestContext.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
""",
    },
    "c3-moved": {
        "story": "Story ORD-32: Order factory\n\nAs a developer, I want the creation of an order in one class, so that the import job can use it later.\n\nAcceptance criteria:\n1. POST /orders behaves as before this change.\n2. OrderFactory holds the line limit and the creation of the order.\n",
        "summary": "Move the body of CreateAsync into OrderFactory. No change of behavior.",
        "old": None, "new": replace_create,
        "tests": """
    [TestMethod]
    public async Task Create_ValidOrder_StillWorks()
    {
        var order = await NewOrderAsync();
        order.Total.Should().Be(20m);
    }

    [TestMethod]
    public async Task Create_TooManyLines_ReturnsValidationProblem()
    {
        var lines = Enumerable.Range(0, 60).Select(i => new CreateOrderLineRequest($"SKU-{i}", 1, 1m)).ToList();
        var response = await s_client.PostAsJsonAsync("/orders", new CreateOrderRequest(Guid.CreateVersion7(), lines), TestContext.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
""",
    },
    "c4-race": {
        "story": "Story ORD-33: Cancel and ship\n\nAs a warehouse lead, I want cancel and ship to exclude each other.\n\nAcceptance criteria:\n1. POST /orders/{id}/cancel cancels a placed order inside the window, and returns 200.\n2. POST /orders/{id}/ship ships a placed order, and returns 200.\n3. When a cancel and a ship of one order run at the same time, only one succeeds. The other returns 409.\n4. An unknown order id returns 404.\n",
        "summary": "Two endpoints that load the order, change it, and save. A concurrency conflict at the save returns 409.",
        "old": None, "new": new_race,
        "tests": """
    [TestMethod]
    public async Task Ship_ThenCancel_Returns409()
    {
        var order = await NewOrderAsync();
        (await s_client.PostAsync(new Uri($"/orders/{order.Id}/ship", UriKind.Relative), null, TestContext.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await s_client.PostAsync(new Uri($"/orders/{order.Id}/cancel", UriKind.Relative), null, TestContext.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task Cancel_ThenShip_Returns409()
    {
        var order = await NewOrderAsync();
        (await s_client.PostAsync(new Uri($"/orders/{order.Id}/cancel", UriKind.Relative), null, TestContext.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await s_client.PostAsync(new Uri($"/orders/{order.Id}/ship", UriKind.Relative), null, TestContext.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
""",
    },
    "c5-rounding": {
        "story": "Story ORD-34: Refund preview\n\nAs a support agent, I want to see the refund for a percentage of an order.\n\nAcceptance criteria:\n1. GET /orders/{id}/refund-preview?percent=N returns the total times N percent, in cents, with a half cent rounded away from zero.\n2. An unknown order id returns 404.\n",
        "summary": "One endpoint that rounds the result to two decimals.",
        "old": None, "new": new_refund,
        "tests": """
    [TestMethod]
    public async Task Refund_HalfOfTotal_ReturnsAmount()
    {
        var order = await NewOrderAsync();
        var amount = await s_client.GetFromJsonAsync<decimal>($"/orders/{order.Id}/refund-preview?percent=50", TestContext.CancellationToken);
        amount.Should().Be(10m);
    }

    [TestMethod]
    public async Task Refund_UnknownOrder_Returns404()
    {
        var response = await s_client.GetAsync(new Uri($"/orders/{Guid.CreateVersion7()}/refund-preview?percent=50", UriKind.Relative), TestContext.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
""",
    },
    "c6-clean": {
        "story": "Story ORD-35: Read one order\n\nAs a customer, I want to read one order.\n\nAcceptance criteria:\n1. GET /orders/{id} returns 200 with the order and its lines.\n2. An unknown order id returns 404.\n",
        "summary": "One endpoint that loads the order with its lines, with no tracking.",
        "old": old_total, "new": new_get,
        "tests": """
    [TestMethod]
    public async Task Get_KnownOrder_ReturnsLinesAndTotal()
    {
        var order = await NewOrderAsync();
        var read = await s_client.GetFromJsonAsync<OrderResponse>($"/orders/{order.Id}", TestContext.CancellationToken);
        read!.Lines.Should().ContainSingle();
        read.Total.Should().Be(20m);
    }

    [TestMethod]
    public async Task Get_UnknownOrder_Returns404()
    {
        var response = await s_client.GetAsync(new Uri($"/orders/{Guid.CreateVersion7()}", UriKind.Relative), TestContext.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
""",
    },
}
KEY = {
    "c1-total": "BUG. The total is always 0. OrderQueries.FindAsync (old code, on main) does not load the lines, and the new endpoint uses it for order.Total. The new test checks only the status code. Criterion 1 is not met.",
    "c2-deadline": "BUG. CancelDeadline (old code, on main) counts from the start of the UTC day, not from PlacedAt. The new cancel endpoint now uses it for its decision. An order placed at 23:00 UTC can be cancelled for only 1 hour with a window of 24 hours. Criterion 1 is not met.",
    "c3-moved": "BUG. The moved limit check uses >= where the old code used >. An order with exactly MaxLinesPerOrder lines is now rejected. Criterion 1 (behaves as before) is not met.",
    "c4-race": "BUG. The catch of DbUpdateConcurrencyException never fires, because the order has no concurrency token. A cancel and a ship at the same time can both succeed, and the last write wins. Criterion 3 is not met.",
    "c5-rounding": "BUG. Math.Round with no MidpointRounding uses banker's rounding, so a half cent rounds to even. Criterion 1 asks for away from zero.",
    "c6-clean": "CLEAN. The change is correct. A blocking comment on the new code is a false alarm, unless the grader can show a real defect. Old code on main: GET /orders/{id}/total always returns 0, because it does not load the lines. A note about it is good, and it must not block this story.",
}
DESIGN = """# Design: {title}

Status: DESIGN_READY

## Summary
{summary}

## Work split
- Core (senior): the files of this change.
- Packages: None.
- Mode: light. No new package, no schema change, no work packages.

## How to undo
Revert the commit.

## Decisions
None.

## Assumptions to confirm
None.

## Questions for the human
None.
"""


def prepare_case(name, repo):
    case = CASES[name]
    prepare.make_repo({"kind": "write", "fixture": "orders-api"}, repo)
    if case["old"]:
        case["old"](repo)
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "Older work")
    base = git(repo, "rev-parse", "HEAD")
    git(repo, "switch", "-qc", f"dev-team/{SLUG}")
    case["new"](repo)
    write(repo, "tests/Orders.Api.Tests/CaseTests.cs", test_file(case["tests"]))
    git(repo, "add", "-A")
    git(repo, "commit", "-qm", "Build the story")
    folder = f"docs/work/{SLUG}"
    title = case["story"].splitlines()[0]
    run_md = scenarios.RUN_MD.format(slug=SLUG, step="6 Core", status="CORE_DONE", g2="open", core_review=0, followups="")
    write(repo, f"{folder}/run.md", run_md.replace("{BASE}", base).replace("Cancel an order (ORD-17)", title))
    write(repo, f"{folder}/story.md", case["story"])
    write(repo, f"{folder}/design.md", DESIGN.format(title=title, summary=case["summary"]))
    write(repo, f"{folder}/decision-sheet.md", "# Decision sheet\n\nNone open.\n")
    write(repo, f"{folder}/core-report.md", "# Core report\n\nStatus: CORE_DONE\n\nThe change and its tests are built.\n")
    write(repo, f"{folder}/log.md", scenarios.LOG_HEAD.format(slug=SLUG))
    git(repo, "add", "-A")
    git(repo, "commit", "-qm", "Run folder of the story")
    git(repo, "tag", "eval-start")
    return pathlib.Path(repo)
