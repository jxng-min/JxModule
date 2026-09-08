namespace JxModule.Terminal
{
    public readonly struct JxCommandResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }

        private JxCommandResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public static JxCommandResult Success(string message = null)
        {
            return new JxCommandResult(true, message);
        }

        public static JxCommandResult Failure(string message = null)
        {
            return new JxCommandResult(false, message);
        }
    }
}