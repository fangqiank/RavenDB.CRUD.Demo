using System.Text.Json.Serialization;

namespace RavenDB.CRUD.Demo.Models
{
    public class Review
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("productId")]
        public string ProductId { get; set; } = string.Empty;  // 引用 Product

        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; } = string.Empty;  // 引用 Customer

        [JsonPropertyName("rating")]
        public int Rating { get; set; }  // 1-5

        [JsonPropertyName("comment")]
        public string Comment { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("verifiedPurchase")]
        public bool VerifiedPurchase { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 冗余存储，方便显示
        [JsonPropertyName("customerName")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("productName")]
        public string? ProductName { get; set; }
    }
}
