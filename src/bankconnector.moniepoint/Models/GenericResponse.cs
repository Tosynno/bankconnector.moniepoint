namespace zone.bankconnector.moniepoint.Models
{
    public class GenericResponse<T> : BaseResponse where T : new()
    {
        public T Data { get; set; } = new T();
    }
}
