namespace InnerKimia.Domain.Exceptions
{

    /// <summary>
    /// کلاس پایه برای تمام Exception‌های لایه Domain
    /// </summary>
    public abstract class DomainException : Exception
    {
        /// <summary>
        /// کد خطا برای استفاده در فرانت‌اند یا لاگ‌گیری
        /// </summary>
        public string ErrorCode { get; }

        protected DomainException(string message, string errorCode = "DOMAIN_ERROR")
            : base(message)
        {
            ErrorCode = errorCode;
        }

        protected DomainException(string message, Exception innerException, string errorCode = "DOMAIN_ERROR")
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
