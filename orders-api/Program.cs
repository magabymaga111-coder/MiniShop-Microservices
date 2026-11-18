using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connStr));
builder.Services.AddHttpClient(); // для запросов к catalog-api
var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

app.MapGet("/orders", async (AppDbContext db) => await db.Orders.ToListAsync());

app.MapGet("/orders/{id}", async (int id, AppDbContext db) =>
    await db.Orders.FindAsync(id) is Order o ? Results.Ok(o) : Results.NotFound());

// POST: создать заказ, запросив цену товара у catalog-api
app.MapPost("/orders", async (OrderRequest req, AppDbContext db, IHttpClientFactory httpFactory, IConfiguration cfg) =>
{
    var catalogUrl = cfg["CatalogApiUrl"] ?? "http://catalog-api:8080";
    var client = httpFactory.CreateClient();
    
    // Запрос к другому сервису
    var response = await client.GetAsync($"{catalogUrl}/products/{req.ProductId}");
    if (!response.IsSuccessStatusCode) return Results.BadRequest("Product not found in Catalog");

    var product = await response.Content.ReadFromJsonAsync<ProductDto>();
    if (product is null) return Results.BadRequest("Invalid product data");

    var order = new Order
    {
        UserId = req.UserId,
        ProductId = req.ProductId,
        ProductName = product.Name,
        Quantity = req.Quantity,
        TotalPrice = product.Price * req.Quantity,
        CreatedAt = DateTime.UtcNow
    };
    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Created($"/orders/{order.Id}", order);
});

app.MapDelete("/orders/{id}", async (int id, AppDbContext db) =>
{
    var o = await db.Orders.FindAsync(id);
    if (o is null) return Results.NotFound();
    db.Orders.Remove(o);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderRequest
{
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) { }
    public DbSet<Order> Orders => Set<Order>();
}