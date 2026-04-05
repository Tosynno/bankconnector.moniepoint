namespace zone.bankconnector.moniepoint.Models
{
    public class GenericResponse<T> : BaseResponse
    {
        public T? Data { get; set; }
    }
}
