using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Data;
using Orders.Api.Inventory;
using Orders.Api.Vouchers;

namespace Orders.Api.Orders;

public static class OrderEndpoints
{
    private const int ListLimit = 50;

    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders");
        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListForCustomerAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/{id:guid}/cancel", CancelAsync);
        return app;
    }

    private static async Task<Results<Created<OrderResponse>, ValidationProblem, Conflict<string>>> CreateAsync(
        CreateOrderRequest request,
        OrdersDbContext db,
        StockReservationService reservations,
        TimeProvider time,
        IOptions<OrdersOptions> options,
        CancellationToken ct)
    {
        if (request.Lines.Count > options.Value.MaxLinesPerOrder)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Lines)] = [$"An order can have at most {options.Value.MaxLinesPerOrder} lines."],
            });
        }

        if (!await db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.CustomerId)] = ["The customer does not exist."],
            });
        }

        VoucherTerms? voucher = null;
        if (request.VoucherCode is not null)
        {
            if (!Voucher.IsValidCode(request.VoucherCode))
            {
                return UnknownVoucher(request);
            }

            var code = Voucher.Normalize(request.VoucherCode);
            voucher = await db.Vouchers.AsNoTracking()
                .Where(v => v.Code == code)
                .Select(v => new VoucherTerms(v.Id, v.Code, v.PercentOff))
                .FirstOrDefaultAsync(ct);
            if (voucher is null)
            {
                return UnknownVoucher(request);
            }
        }

        // The voucher use, the stock reservations and the order are one unit of work. The execution strategy
        // runs it again after a transient error (EnableRetryOnFailure), so it starts clean each time.
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            if (voucher is not null)
            {
                // One conditional UPDATE: the row lock makes parallel orders take the uses one by one,
                // and the condition stops at MaxUses.
                var taken = await db.Vouchers
                    .Where(v => v.Id == voucher.Id && v.Used < v.MaxUses)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.Used, v => v.Used + 1), ct);
                if (taken == 0)
                {
                    await transaction.RollbackAsync(ct);
                    return (Results<Created<OrderResponse>, ValidationProblem, Conflict<string>>)TypedResults.Conflict("The voucher is used up.");
                }
            }

            foreach (var line in request.Lines)
            {
                if (!await reservations.TryReserveAsync(line.Sku, line.Quantity, ct))
                {
                    await transaction.RollbackAsync(ct);
                    return TypedResults.Conflict($"Not enough stock for {line.Sku}.");
                }
            }

            var order = new Order(request.CustomerId!.Value, time.GetUtcNow(), request.CustomerReference, request.DeliveryInstructions);
            foreach (var line in request.Lines)
            {
                order.AddLine(StockItem.Normalize(line.Sku), line.Quantity, line.UnitPrice);
            }

            db.Orders.Add(order);
            if (voucher is not null)
            {
                order.ApplyVoucher(voucher.Code, Voucher.DiscountOn(order.Total, voucher.PercentOff));
                db.VoucherRedemptions.Add(new VoucherRedemption(voucher.Id, order.CustomerId, order.Id));
            }

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // The unique index on (VoucherId, CustomerId): this customer used the voucher before.
                await transaction.RollbackAsync(ct);
                return TypedResults.Conflict("The customer used this voucher before.");
            }

            await transaction.CommitAsync(ct);
            return TypedResults.Created($"/orders/{order.Id}", order.ToResponse());
        });
    }

    private static ValidationProblem UnknownVoucher(CreateOrderRequest request) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(request.VoucherCode)] = ["The voucher code does not exist."],
        });

    private sealed record VoucherTerms(Guid Id, string Code, int PercentOff);

    private static async Task<Results<Ok<OrderResponse>, NotFound>> GetAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(order.ToResponse());
    }

    private static async Task<Ok<List<OrderResponse>>> ListForCustomerAsync(
        Guid customerId,
        OrdersDbContext db,
        CancellationToken ct)
    {
        var orders = await db.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.PlacedAt)
            .ThenBy(order => order.Id)
            .Take(ListLimit)
            .ToListAsync(ct);

        return TypedResults.Ok(orders.Select(order => order.ToResponse()).ToList());
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound, Conflict<string>>> CancelAsync(
        Guid id,
        OrdersDbContext db,
        TimeProvider time,
        IOptions<OrdersOptions> options,
        CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        var outcome = order.Cancel(time.GetUtcNow(), options.Value.CancelWindow);
        switch (outcome)
        {
            case CancelOutcome.AlreadyShipped:
                return TypedResults.Conflict("The order is already shipped.");
            case CancelOutcome.WindowClosed:
                return TypedResults.Conflict("The cancel window is closed.");
            case CancelOutcome.Cancelled:
                await db.SaveChangesAsync(ct);
                break;
        }

        return TypedResults.Ok(order.ToResponse());
    }
}
