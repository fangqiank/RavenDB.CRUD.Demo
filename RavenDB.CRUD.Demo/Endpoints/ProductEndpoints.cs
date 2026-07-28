using RavenDB.CRUD.Demo.Models;
using RavenDB.CRUD.Demo.Services;

namespace RavenDB.CRUD.Demo.Endpoints
{
    public static class ProductEndpoints
    {
        public static void MapProductEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/products")
                .WithTags("Products");

            group.MapGet("/", async (RavenDBService dbService, int limit = 100) =>
            {
                var products = await dbService.GetAllProductsAsync(limit);
                return Results.Ok(products);
            })
            .WithName("GetAllProducts")
            .WithDescription("获取所有产品列表");

            group.MapGet("/{*id}", async (string id, RavenDBService dbService) =>
            {
                var product = await dbService.GetProductByIdAsync(Uri.UnescapeDataString(id));
                return product is not null ? Results.Ok(product) : Results.NotFound();
            })
            .WithName("GetProductById")
            .WithDescription("根据 ID 获取产品详情");

            group.MapGet("/category/{category}", async (string category, RavenDBService dbService) =>
            {
                var products = await dbService.GetProductsByCategoryAsync(category);
                return Results.Ok(products);
            })
            .WithName("GetProductsByCategory")
            .WithDescription("根据分类获取产品列表");

            group.MapGet("/search", async (string q, RavenDBService dbService) =>
            {
                if (string.IsNullOrWhiteSpace(q))
                    return Results.BadRequest("搜索关键词 q 不能为空");

                var products = await dbService.SearchProductsAsync(q);
                return Results.Ok(products);
            })
           .WithName("SearchProducts")
           .WithDescription("搜索产品（名称或描述）");

            group.MapPost("/", async (Product product, RavenDBService dbService) =>
            {
                if (string.IsNullOrEmpty(product.Name))
                    return Results.BadRequest("产品名称不能为空");
                if (product.Price < 0)
                    return Results.BadRequest("产品价格不能为负数");

                var created = await dbService.CreateProductAsync(product);
                return Results.Created($"/api/products/{Uri.EscapeDataString(created.Id!)}", created);
            })
            .WithName("CreateProduct")
            .WithDescription("创建新产品");

            group.MapPut("/{*id}", async (string id, Product updatedProduct, RavenDBService dbService) =>
            {
                if (string.IsNullOrEmpty(updatedProduct.Name))
                    return Results.BadRequest("产品名称不能为空");
                if (updatedProduct.Price < 0)
                    return Results.BadRequest("产品价格不能为负数");

                var updated = await dbService.UpdateProductAsync(Uri.UnescapeDataString(id), updatedProduct);
                return updated is not null ? Results.Ok(updated) : Results.NotFound();
            })
            .WithName("UpdateProduct")
            .WithDescription("更新产品信息");

            group.MapDelete("/{*id}", async (string id, RavenDBService dbService) =>
            {
                var deleted = await dbService.DeleteProductAsync(Uri.UnescapeDataString(id));
                return deleted ? Results.NoContent() : Results.NotFound();
            })
            .WithName("DeleteProduct")
            .WithDescription("删除产品");
        }
    }
}
