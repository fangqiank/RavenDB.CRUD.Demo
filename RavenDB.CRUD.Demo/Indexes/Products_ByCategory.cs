using Raven.Client.Documents.Indexes;
using RavenDB.CRUD.Demo.Models;

namespace RavenDB.CRUD.Demo.Indexes
{
    public class Products_ByCategory: AbstractIndexCreationTask<Product>
    {
        public Products_ByCategory()
        {
            Map = products => from product in products
                              select new
                              {
                                  product.Category,
                                  product.Name,
                                  product.Price,
                                  product.InStock
                              };
        }
    }
}
