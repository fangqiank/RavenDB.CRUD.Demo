using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using RavenDB.CRUD.Demo.Models;

namespace RavenDB.CRUD.Demo.Services
{
    public class RavenDBService : IAsyncDisposable
    {
        private readonly IDocumentStore _store;

        public RavenDBService(IConfiguration configuration)
        {
            var settings = configuration.GetSection("RavenDB");
            var urls = settings.GetValue<string[]>("Urls") ?? new[] { "http://localhost:8080" };
            var databaseName = settings.GetValue<string>("DatabaseName") ?? "ProductDB";

            _store = new DocumentStore
            {
                Urls = urls,
                Database = databaseName
            };

            _store.Initialize();

            EnsureDatabaseExists(databaseName);
        }

        private void EnsureDatabaseExists(string databaseName)
        {
            try
            {
                _store.Maintenance.Server.Send(
                    new CreateDatabaseOperation(new DatabaseRecord(databaseName)));
            }
            catch (ConcurrencyException)
            {
                // Database already exists — nothing to do.
            }
        }
        public IAsyncDocumentSession OpenSession()
        {
            return _store.OpenAsyncSession();
        }

        public async Task ExecuteIndexAsync(AbstractIndexCreationTask index)
        {
            await _store.ExecuteIndexAsync(index);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            using var session = OpenSession();

            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            await session.StoreAsync(product);
            await session.SaveChangesAsync();

            return product;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            using var session = OpenSession();

            var products = await session
                .Query<Product>()
                .ToListAsync();

            return products;
        }

        public async Task<Product?> GetProductByIdAsync(string id)
        {
            using var session = OpenSession();

            // 注意：RavenDB 的 ID 格式是 "products/1-A"，可能需要添加前缀
            var product = await session.LoadAsync<Product>(id);

            return product;
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(string category)
        {
            using var session = OpenSession();

            var products = await session
                .Query<Product>()
                .Where(p => p.Category == category)
                .ToListAsync();

            return products;
        }

        public async Task<Product?> UpdateProductAsync(string id, Product updatedProduct)
        {
            using var session = OpenSession();

            var existingProduct = await session.LoadAsync<Product>(id);

            if (existingProduct == null)
                return null;

            // 更新字段
            existingProduct.Name = updatedProduct.Name;
            existingProduct.Description = updatedProduct.Description;
            existingProduct.Price = updatedProduct.Price;
            existingProduct.Category = updatedProduct.Category;
            existingProduct.InStock = updatedProduct.InStock;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            await session.SaveChangesAsync();

            return existingProduct;
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            using var session = OpenSession();

            var product = await session.LoadAsync<Product>(id);

            if (product == null)
                return false;

            session.Delete(product);
            await session.SaveChangesAsync();

            return true;
        }

        public async Task<List<Product>> SearchProductsAsync(string searchTerm)
        {
            using var session = OpenSession();

            var products = await session
                .Query<Product>()
                .Search(p => p.Name, searchTerm)
                .Search(p => p.Description, searchTerm)
                .ToListAsync();

            return products;
        }


        public async Task<Order> CreateOrderWithRelationsAsync(Order order)
        {
            using var session = OpenSession();

            var customer = await session.LoadAsync<Customer>(order.CustomerId);
            if(customer == null)
                throw new Exception($"Customer with ID {order.CustomerId} not found.");

            foreach (var item in order.Items)
            {
                var product = await session.LoadAsync<Product>(item.ProductId);
                if (product == null)
                    throw new Exception($"Product with ID {item.ProductId} not found");

                item.ProductName = product.Name;
                if (item.UnitPrice == 0)
                    item.UnitPrice = product.Price;
            }

            order.Customer = customer;
            order.OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20);
            order.TotalAmount = order.Items.Sum(i => i.Subtotal);
            order.ShippingAddress = customer.Address; // 使用客户地址作为默认
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await session.StoreAsync(order);
            await session.SaveChangesAsync();

            return order;   
        }

        public async Task<Order?> GetOrderWithIncludesAsync(string orderId)
        {
            using var session = OpenSession();

            var order = await session
                .Include<Order>(o => o.CustomerId)
                .Include<Order>(o => o.Items.Select(i => i.ProductId))
                .LoadAsync<Order>(orderId);

            if(order == null)
                return null;

            order.Customer = await session.LoadAsync<Customer>(order.CustomerId);

            foreach (var item in order.Items)
            {
                item.Product = await session.LoadAsync<Product>(item.ProductId);
            }

            return order;
        }

        public async Task<List<Order>> GetOrdersByCustomerAsync(string customerId)
        {
            using var session = OpenSession();

            var orders = await session
                .Query<Order>()
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var customerIds = orders.Select(o => o.CustomerId).Distinct().ToList();
            var productIds = orders.SelectMany(o => o.Items.Select(i => i.ProductId)).Distinct().ToList();

            var customers = await session.LoadAsync<Customer>(customerIds);
            var products = await session.LoadAsync<Product>(productIds);

            foreach(var order in orders)
            {
                customers.TryGetValue(order.CustomerId, out var customer);
                order.Customer = customer;

                foreach (var item in order.Items)
                {
                    products.TryGetValue(item.ProductId, out var product);
                    item.Product = product;
                    item.ProductName = product?.Name ?? item.ProductName;
                }
            }

            return orders;
        }

        public async Task<List<Review>> GetProductReviewsAsync(string productId)
        {
            using var session = OpenSession();

            var reviews = await session
                .Query<Review>()
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var customerIds = reviews.Select(r => r.CustomerId).Distinct().ToList();
            var customers = await session.LoadAsync<Customer>(customerIds);

            foreach (var review in reviews)
            {
                customers.TryGetValue(review.CustomerId, out var customer);
                review.CustomerName = customer != null
                    ? $"{customer.FirstName} {customer.LastName}"
                    : "Unknown Customer";
            }

            return reviews;
        }

        public async Task<Review> CreateReviewAsync(Review review)
        {
            using var session = OpenSession();

            var product = await session.LoadAsync<Product>(review.ProductId);
            var customer = await session.LoadAsync<Customer>(review.CustomerId);

            if (product == null)
                throw new Exception($"Product with ID {review.ProductId} not found");
            if (customer == null)
                throw new Exception($"Customer with ID {review.CustomerId} not found");

            review.ProductName = product.Name;
            review.CustomerName = $"{customer.FirstName} {customer.LastName}";
            review.CreatedAt = DateTime.UtcNow;

            var hasOrder = await session
                .Query<Order>()
                .AnyAsync(o => o.CustomerId == review.CustomerId &&
                              o.Items.Any(i => i.ProductId == review.ProductId) &&
                              o.Status == OrderStatus.Delivered);

            review.VerifiedPurchase = hasOrder;

            await session.StoreAsync(review);
            await session.SaveChangesAsync();

            return review;
        }

        public async Task<CustomerPurchaseHistory> GetCustomerPurchaseHistoryAsync(string customerId)
        {
            using var session = OpenSession();

            var customer = await session.LoadAsync<Customer>(customerId);
            if (customer == null)
                throw new Exception($"Customer with ID {customerId} not found");

            var orders = await session
                .Query<Order>()
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var productIds = orders
                .SelectMany(o => o.Items.Select(i => i.ProductId))
                .Distinct()
                .ToList();

            var products = await session.LoadAsync<Product>(productIds);

            var history = new CustomerPurchaseHistory
            {
                Customer = customer,
                TotalOrders = orders.Count,
                TotalSpent = orders.Sum(o => o.TotalAmount),
                Orders = orders.Select(o => new OrderSummary
                {
                    Order = o,
                    Items = o.Items.Select(i => new OrderItemSummary
                    {
                        Product = products.GetValueOrDefault(i.ProductId),
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Subtotal = i.Subtotal
                    }).ToList()
                }).ToList(),
                FavoriteCategory = orders
                    .SelectMany(o => o.Items)
                    .GroupBy(i => products.GetValueOrDefault(i.ProductId)?.Category ?? "Unknown")
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault()
            };

            return history;
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            using var session = OpenSession();

            customer.CreatedAt = DateTime.UtcNow;
            customer.UpdatedAt = DateTime.UtcNow;

            await session.StoreAsync(customer);
            await session.SaveChangesAsync();

            return customer;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            using var session = OpenSession();
            return await session.Query<Customer>().ToListAsync();
        }

        public async Task<Customer?> GetCustomerByIdAsync(string id)
        {
            using var session = OpenSession();
            return await session.LoadAsync<Customer>(id);
        }

        public async ValueTask DisposeAsync()
        {
            _store?.Dispose();
            await Task.CompletedTask;
        }
    }

    public class CustomerPurchaseHistory
    {
        public Customer Customer { get; set; } = null!;
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public List<OrderSummary> Orders { get; set; } = new();
        public string? FavoriteCategory { get; set; }
    }
    public class OrderSummary
    {
        public Order Order { get; set; } = null!;
        public List<OrderItemSummary> Items { get; set; } = new();
    }

    public class OrderItemSummary
    {
        public Product? Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

}
