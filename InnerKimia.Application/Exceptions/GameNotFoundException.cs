namespace InnerKimia.Application.Exceptions
{
    public class GameNotFoundException : Exception
    {

        public GameNotFoundException()
            : base($"Game was not found.")
        {
        }
    }
}
