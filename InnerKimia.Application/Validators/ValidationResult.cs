namespace InnerKimia.Application.Validators
{
    // Application/Validators/ValidationResult.cs
    public class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; }

        public ValidationResult(List<string> errors)
        {
            Errors = errors ?? new List<string>();
        }

        public ValidationResult() : this(new List<string>()) { }

        public string ErrorMessage => string.Join("; ", Errors);

        public static ValidationResult Success() => new();
        public static ValidationResult Failure(string error) => new(new List<string> { error });
        public static ValidationResult Failure(List<string> errors) => new(errors);
    }
}
