namespace MommyAndSunny.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base($"Không tìm thấy {entityName} với khóa '{key}'")
    {
    }
}
