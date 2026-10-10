namespace SimpleResult;

public class SimpleResultException : Exception
{
    public SimpleResultException()
    {
    }

    public SimpleResultException(string message) : base(message)
    {
    }

    public SimpleResultException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public SimpleResultException(Exception innerException) : base(innerException.Message, innerException)
    {
    }
}