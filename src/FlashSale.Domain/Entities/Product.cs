namespace FlashSale.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int TotalStock { get; private set; }

    private Product() { }

    public static Product Create(string sku, string name, decimal price, int initialStock)
    {
        if (initialStock < 0)
            throw new ArgumentException("El stock inicial no puede ser negativo.", nameof(initialStock));

        return new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            Name = name,
            Price = price,
            TotalStock = initialStock
        };
    }
}