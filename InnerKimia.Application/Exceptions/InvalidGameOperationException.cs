namespace InnerKimia.Application.Exceptions
{
    public class InvalidGameOperationException : Exception
    {
        public InvalidGameOperationException(string message) : base(message) { }
        public InvalidGameOperationException(string message, Exception inner) : base(message, inner) { }
    }
}
