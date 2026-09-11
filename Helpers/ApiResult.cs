namespace Ice_Cream_Parlour_Eproject.Helpers
{
    /// <summary>
    /// Generic wrapper class for API responses to provide consistent result formatting.
    /// </summary>
    /// <typeparam name="T">The type of data contained in the response</typeparam>
    public class ApiResult<T>
    {
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The data returned from the operation.
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// The error message if the operation failed.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static ApiResult<T> SuccessResult(T data, string? message = "Operation completed successfully")
        {
            return new ApiResult<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        public static ApiResult<T> FailureResult(string message)
        {
            return new ApiResult<T>
            {
                Success = false,
                Data = default,
                Message = message
            };
        }
    }

    /// <summary>
    /// Non-generic version of ApiResult for operations that don't return data.
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The message describing the result.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static ApiResult SuccessResult(string? message = "Operation completed successfully")
        {
            return new ApiResult
            {
                Success = true,
                Message = message
            };
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        public static ApiResult FailureResult(string message)
        {
            return new ApiResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
