namespace TodoList.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} com id '{key}' não foi encontrado.")
    {
    }
}
