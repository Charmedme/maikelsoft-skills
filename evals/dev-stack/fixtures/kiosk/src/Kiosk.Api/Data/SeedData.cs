using Kiosk.Api.Basket;
using Kiosk.Api.Catalog;
using Kiosk.Api.Orders;
using Kiosk.Api.Reviews;
using Kiosk.Api.Users;

namespace Kiosk.Api.Data;

/// <summary>Demo data for local runs and tests.</summary>
public sealed class SeedData(KioskDbContext db, IPasswordHasher hasher, TimeProvider time)
{
    public async Task ApplyAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        db.SecurityQuestions.AddRange(
            new SecurityQuestion { Id = 1, Text = "What is the name of your first pet?" },
            new SecurityQuestion { Id = 2, Text = "In which city were you born?" },
            new SecurityQuestion { Id = 3, Text = "What was the make of your first car?" });

        var admin = NewUser("admin@kiosk.test", "Kiosk-Admin-2026!", Roles.Admin, "Shop admin", 1, "Pixel", now);
        admin.TotpSecret = "KIOSKTESTONLYTOTPSEED";
        var maria = NewUser("maria.lopez@kiosk.test", "Maria-Spring-2026!", Roles.Customer, "Maria", 2, "Valencia", now);
        var tom = NewUser("tom.berg@kiosk.test", "Tom-Garden-2026!", Roles.Customer, "Tom", 3, "Volvo", now);
        var finance = NewUser("finance@kiosk.test", "Finance-Ledger-2026!", Roles.Accounting, "Finance", 2, "Utrecht", now);
        var partner = NewUser("orders@partner-retail.test", "Partner-Feed-2026!", Roles.Partner, "Partner Retail", 1, "Rex", now);
        db.Users.AddRange(admin, maria, tom, finance, partner);

        var products = new[]
        {
            NewProduct("Desk Lamp", "Lighting", 39.95m, "LMP-100", "supply@lumen-works.test", "A steady <em>warm</em> light for long evenings."),
            NewProduct("Reading Lamp", "Lighting", 24.50m, "LMP-200", "supply@lumen-works.test", "Clip-on lamp with a flexible arm."),
            NewProduct("Ceramic Mug", "Kitchen", 9.95m, "MUG-010", "supply@clayhouse.test", "Hand-glazed mug, <strong>350 ml</strong>."),
            NewProduct("Tea Kettle", "Kitchen", 44.00m, "KTL-020", "supply@clayhouse.test", "Enamel kettle for gas and induction."),
            NewProduct("Wool Blanket", "Home", 79.00m, "BLK-300", "supply@northloom.test", "Soft blanket of Dutch wool."),
            NewProduct("Linen Cushion", "Home", 29.00m, "CSH-310", "supply@northloom.test", "Washed linen cover with a feather filling."),
            NewProduct("Notebook A5", "Office", 6.50m, "NTB-400", "orders@partner-retail.test", "Dotted pages, lay-flat binding."),
            NewProduct("Fountain Pen", "Office", 34.90m, "PEN-410", "orders@partner-retail.test", "Steel nib, refillable converter."),
        };
        db.Products.AddRange(products);
        await db.SaveChangesAsync(ct);

        db.Baskets.AddRange(
            new ShoppingBasket { UserId = admin.Id },
            new ShoppingBasket { UserId = maria.Id, Items = [new BasketItem { ProductId = products[0].Id, Quantity = 1 }] },
            new ShoppingBasket { UserId = tom.Id, Items = [new BasketItem { ProductId = products[2].Id, Quantity = 2 }] });

        db.Orders.AddRange(
            NewOrder("K-1001", maria.Id, null, OrderStatus.DamageReported, "Calle Mayor 4, Valencia", now.AddDays(-12), (products[3], 1)),
            NewOrder("K-1002", maria.Id, null, OrderStatus.Shipped, "Calle Mayor 4, Valencia", now.AddDays(-3), (products[4], 1), (products[5], 2)),
            NewOrder("K-1003", tom.Id, null, OrderStatus.Delivered, "Havenstraat 12, Rotterdam", now.AddDays(-20), (products[6], 3)),
            NewOrder("K-1004", null, "lena.vos@mail.test", OrderStatus.Paid, "Lindelaan 8, Utrecht", now.AddDays(-2), (products[1], 1)),
            NewOrder("K-1005", null, "sam.ortiz@mail.test", OrderStatus.Shipped, "Rua Augusta 30, Lisboa", now.AddDays(-1), (products[7], 1), (products[2], 4)));

        db.Reviews.AddRange(
            new Review { ProductId = products[0].Id, Author = maria.Email, Message = "Bright and steady.", Rating = 5, CreatedAt = now.AddDays(-30) },
            new Review { ProductId = products[0].Id, Author = tom.Email, Message = "Good lamp, short cable.", Rating = 4, CreatedAt = now.AddDays(-25) },
            new Review { ProductId = products[2].Id, Author = tom.Email, Message = "Nice glaze.", Rating = 3, CreatedAt = now.AddDays(-15) },
            new Review { ProductId = products[4].Id, Author = maria.Email, Message = "Very warm.", Rating = 5, CreatedAt = now.AddDays(-5) });

        await db.SaveChangesAsync(ct);
    }

    private User NewUser(string email, string password, string role, string name, int question, string answer, DateTimeOffset now) => new()
    {
        Email = email,
        PasswordHash = hasher.Hash(password),
        Role = role,
        DisplayName = name,
        SecurityQuestionId = question,
        SecurityAnswer = answer,
        CreatedAt = now.AddMonths(-6),
    };

    private static Product NewProduct(string name, string category, decimal price, string sku, string supplier, string description) => new()
    {
        Name = name,
        Category = category,
        Price = price,
        Sku = sku,
        SupplierEmail = supplier,
        Description = description,
        ImageUrl = $"/images/{sku.ToLowerInvariant()}.jpg",
        Stock = 25,
    };

    private static Order NewOrder(string number, int? customer, string? guest, OrderStatus status, string address, DateTimeOffset at, params (Product Product, int Quantity)[] lines)
    {
        var order = new Order
        {
            OrderNumber = number,
            CustomerId = customer,
            GuestEmail = guest,
            Status = status,
            DeliveryAddress = address,
            CreatedAt = at,
            Lines = lines.Select(l => new OrderLine
            {
                ProductId = l.Product.Id,
                ProductName = l.Product.Name,
                Quantity = l.Quantity,
                UnitPrice = l.Product.Price,
            }).ToList(),
        };
        order.Total = order.Lines.Sum(l => l.UnitPrice * l.Quantity);
        return order;
    }
}
