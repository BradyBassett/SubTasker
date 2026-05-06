namespace SubTaskerBackend.Exceptions
{
	public class UnauthorizedException : HttpException
    {
        public UnauthorizedException(string message) : base(message, StatusCodes.Status401Unauthorized) { }
    }
}