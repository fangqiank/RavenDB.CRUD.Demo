using System.Text.Json.Serialization;

namespace RavenDB.CRUD.Demo.Models
{
    public class Order
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; } = string.Empty;  // 引用 Customer

        [JsonPropertyName("customer")]
        public Customer? Customer { get; set; }  // 读时水化（Include），不持久化整对象

        [JsonPropertyName("customerName")]
        public string? CustomerName { get; set; }  // 冗余存储，方便显示

        [JsonPropertyName("orderDate")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("status")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [JsonPropertyName("items")]
        public List<OrderItem> Items { get; set; } = new();  // 嵌套的 Order Items

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("shippingAddress")]
        public Address ShippingAddress { get; set; } = new();

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class OrderItem
    {
        [JsonPropertyName("productId")]
        public string ProductId { get; set; } = string.Empty;  // 引用 Product

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;  // 冗余存储，方便查询

        [JsonPropertyName("product")]
        public Product? Product { get; set; }  // 可选的嵌套 Product

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }  // 单件折扣金额（per-unit discount amount）

        [JsonPropertyName("subtotal")]
        public decimal Subtotal => Quantity * (UnitPrice - Discount);
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }
}
