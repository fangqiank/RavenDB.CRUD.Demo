namespace RavenDB.CRUD.Demo.Exceptions
{
    /// <summary>
    /// 业务实体未找到时抛出。端点应将其映射为 HTTP 404，
    /// 以区别于请求格式错误（400）与服务端错误（500）。
    /// </summary>
    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string message) : base(message)
        {
        }
    }
}
