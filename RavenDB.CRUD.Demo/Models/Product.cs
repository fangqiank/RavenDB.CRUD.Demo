using System.Text.Json.Serialization;

namespace RavenDB.CRUD.Demo.Models
{
    public class Product
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("inStock")]
        public bool InStock { get; set; } = true;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
