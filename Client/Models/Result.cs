namespace Client.Models
{
    public sealed class Result
    {
        public bool Ok { get; }
        public string? FailType { get; }
        private Result(bool ok, string? failType) { Ok = ok; FailType = failType; }

        public static Result Success() => new(true, null);
        public static Result Fail() => new(false, null);
        public static Result NotAuthorized() => new(false, "Not Authorized");
    }

    public sealed class Result<T>
    {
        public bool Ok { get; }
        public string? Error { get; }
        public T? Value { get; }
        private Result(bool ok, T? value, string? error) { Ok = ok; Value = value; Error = error; }

        public static Result<T> Success(T value) => new(true, value, null);
        public static Result<T> Fail(string error) => new(false, default, error);
    }
}
