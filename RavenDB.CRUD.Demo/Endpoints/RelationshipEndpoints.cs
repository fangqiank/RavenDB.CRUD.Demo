using RavenDB.CRUD.Demo.Exceptions;
using RavenDB.CRUD.Demo.Models;
using RavenDB.CRUD.Demo.Services;

namespace RavenDB.CRUD.Demo.Endpoints
{
    public static class RelationshipEndpoints
    {
        public static void MapRelationshipEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/relationships")
                .WithTags("Relationships");

            group.MapPost("/orders", async (Order order, RavenDBService dbService) =>
            {
                if (order.Items is null || order.Items.Count == 0)
                    return Results.BadRequest("订单必须包含至少一个商品");
                if (order.Items.Any(i => i.Quantity <= 0))
                    return Results.BadRequest("订单商品数量必须大于 0");

                try
                {
                    var created = await dbService.CreateOrderWithRelationsAsync(order);
                    return Results.Created($"/api/relationships/orders/{Uri.EscapeDataString(created.Id!)}", created);
                }
                catch (EntityNotFoundException ex)
                {
                    return Results.NotFound(new { Error = ex.Message });
                }
            })
            .WithName("CreateOrderWithRelations")
            .WithDescription("创建包含 Customer 和 Product 关系的订单");

            group.MapGet("/orders/{*id}", async (string id, RavenDBService dbService) =>
            {
                var order = await dbService.GetOrderWithIncludesAsync(Uri.UnescapeDataString(id));
                return order is not null ? Results.Ok(order) : Results.NotFound();
            })
            .WithName("GetOrderWithIncludes")
            .WithDescription("获取订单并包含 Customer 和 Products 详细信息");

            group.MapGet("/customers/{customerId}/orders", async (string customerId, RavenDBService dbService) =>
            {
                try
                {
                    var orders = await dbService.GetOrdersByCustomerAsync(Uri.UnescapeDataString(customerId));
                    return Results.Ok(orders);
                }
                catch (EntityNotFoundException ex)
                {
                    return Results.NotFound(new { Error = ex.Message });
                }
            })
            .WithName("GetCustomerOrders")
            .WithDescription("获取指定客户的所有订单");

            group.MapGet("/products/{productId}/reviews", async (string productId, RavenDBService dbService) =>
            {
                var reviews = await dbService.GetProductReviewsAsync(Uri.UnescapeDataString(productId));
                return Results.Ok(reviews);
            })
            .WithName("GetProductReviews")
            .WithDescription("获取指定产品的所有评论");

            group.MapPost("/reviews", async (Review review, RavenDBService dbService) =>
            {
                if (review.Rating < 1 || review.Rating > 5)
                    return Results.BadRequest("评分必须在 1-5 之间");

                try
                {
                    var created = await dbService.CreateReviewAsync(review);
                    return Results.Created($"/api/relationships/reviews/{Uri.EscapeDataString(created.Id!)}", created);
                }
                catch (EntityNotFoundException ex)
                {
                    return Results.NotFound(new { Error = ex.Message });
                }
            })
            .WithName("CreateReview")
            .WithDescription("创建产品评论（关联 Customer 和 Product）");

            group.MapGet("/customers/{customerId}/purchase-history", async (string customerId, RavenDBService dbService) =>
            {
                try
                {
                    var history = await dbService.GetCustomerPurchaseHistoryAsync(Uri.UnescapeDataString(customerId));
                    return Results.Ok(history);
                }
                catch (EntityNotFoundException ex)
                {
                    return Results.NotFound(new { Error = ex.Message });
                }
            })
            .WithName("GetCustomerPurchaseHistory")
            .WithDescription("获取客户的完整购买历史，包括最喜欢的品类");

            group.MapPost("/seed", async (RavenDBService dbService) =>
            {
                await SeedData(dbService);
                return Results.Ok(new { Message = "示例数据创建成功" });
            })
            .WithName("SeedTestData")
            .WithDescription("创建测试数据（Customers, Products, Orders, Reviews）");
        }

        private static async Task SeedData(RavenDBService dbService)
        {
            // 幂等：若已存在种子产品则跳过，避免重复调用产生重复记录
            var existing = await dbService.GetAllProductsAsync();
            if (existing.Any(p => p.Name == "iPhone 15 Pro"))
                return;

            var customers = new List<Customer>
        {
            new()
            {
                FirstName = "张",
                LastName = "三",
                Email = "zhangsan@email.com",
                Phone = "13800000001",
                Address = new Address
                {
                    Street = "朝阳区建国路88号",
                    City = "北京",
                    State = "北京",
                    ZipCode = "100020",
                    Country = "中国"
                }
            },
            new()
            {
                FirstName = "李",
                LastName = "四",
                Email = "lisi@email.com",
                Phone = "13800000002",
                Address = new Address
                {
                    Street = "浦东新区世纪大道100号",
                    City = "上海",
                    State = "上海",
                    ZipCode = "200120",
                    Country = "中国"
                }
            },
            new()
            {
                FirstName = "王",
                LastName = "五",
                Email = "wangwu@email.com",
                Phone = "13800000003",
                Address = new Address
                {
                    Street = "天河区天河路385号",
                    City = "广州",
                    State = "广东",
                    ZipCode = "510620",
                    Country = "中国"
                }
            }
        };

            foreach (var customer in customers)
            {
                await dbService.CreateCustomerAsync(customer);
            }

            // 2. 创建 Products（使用已有的 CreateProductAsync）
            var products = new List<Product>
        {
            new() { Name = "iPhone 15 Pro", Description = "Apple 最新旗舰手机", Price = 8999.99m, Category = "Electronics", InStock = true },
            new() { Name = "MacBook Pro 16", Description = "Apple 高性能笔记本电脑", Price = 19999.99m, Category = "Electronics", InStock = true },
            new() { Name = "AirPods Pro 2", Description = "Apple 降噪耳机", Price = 1899.99m, Category = "Audio", InStock = true },
            new() { Name = "iPad Air", Description = "Apple 平板电脑", Price = 4799.99m, Category = "Electronics", InStock = false },
            new() { Name = "Samsung 65寸电视", Description = "Samsung QLED 4K电视", Price = 6999.99m, Category = "TV", InStock = true }
        };

            foreach (var product in products)
            {
                await dbService.CreateProductAsync(product);
            }

            // 获取已保存的客户和产品（从数据库重新加载以获取ID）
            var savedCustomers = await dbService.GetAllCustomersAsync();
            var savedProducts = await dbService.GetAllProductsAsync();

            // 3. 创建 Orders（包含关系）
            var orders = new List<Order>
        {
            new()
            {
                CustomerId = savedCustomers[0].Id!,
                Status = OrderStatus.Delivered,
                Items = new List<OrderItem>
                {
                    new() { ProductId = savedProducts[0].Id!, Quantity = 2, UnitPrice = 8999.99m },
                    new() { ProductId = savedProducts[2].Id!, Quantity = 1, UnitPrice = 1899.99m }
                },
                Notes = "第一次购买"
            },
            new()
            {
                CustomerId = savedCustomers[0].Id!,
                Status = OrderStatus.Shipped,
                Items = new List<OrderItem>
                {
                    new() { ProductId = savedProducts[1].Id!, Quantity = 1, UnitPrice = 19999.99m }
                },
                Notes = "工作用"
            },
            new()
            {
                CustomerId = savedCustomers[1].Id!,
                Status = OrderStatus.Delivered,
                Items = new List<OrderItem>
                {
                    new() { ProductId = savedProducts[0].Id!, Quantity = 1, UnitPrice = 8999.99m },
                    new() { ProductId = savedProducts[3].Id!, Quantity = 1, UnitPrice = 4799.99m },
                    new() { ProductId = savedProducts[4].Id!, Quantity = 1, UnitPrice = 6999.99m }
                },
                Notes = "家庭娱乐购买"
            }
        };

            foreach (var order in orders)
            {
                await dbService.CreateOrderWithRelationsAsync(order);
            }

            // 4. 创建 Reviews
            var reviews = new List<Review>
        {
            new()
            {
                ProductId = savedProducts[0].Id!,
                CustomerId = savedCustomers[0].Id!,
                Rating = 5,
                Title = "非常满意！",
                Comment = "手机性能强劲，拍照效果非常好！",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                ProductId = savedProducts[0].Id!,
                CustomerId = savedCustomers[1].Id!,
                Rating = 4,
                Title = "还不错",
                Comment = "价格稍贵，但质量确实好",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new()
            {
                ProductId = savedProducts[1].Id!,
                CustomerId = savedCustomers[0].Id!,
                Rating = 5,
                Title = "工作效率提升神器",
                Comment = "屏幕大，性能强，编译代码飞快！",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                ProductId = savedProducts[2].Id!,
                CustomerId = savedCustomers[2].Id!,
                Rating = 5,
                Title = "降噪效果很好",
                Comment = "飞机上使用效果完美",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

            foreach (var review in reviews)
            {
                await dbService.CreateReviewAsync(review);
            }
        }
    }
}
