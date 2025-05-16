namespace SuperUtils
{
    //public static Result<object> Failure(string error) => new Result<object>(default, error);

    public class Result<T>
    {

        public T Data { get; }
        public string Error { get; }
        public bool IsSuccess => Data    != null && Error == null;
        public bool IsFailure => Error != null;

        private Result(T data, string error)
        {
            Data = data;
            Error = error;
        }

        public static Result<T> Success(T data) => new Result<T>(data, null);
        public static Result<T> Failure(string error) => new Result<T>(default, error);
    }
}
