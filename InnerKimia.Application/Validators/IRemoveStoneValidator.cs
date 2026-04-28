namespace InnerKimia.Application.Validators
{
    // Application/Validators/IRemoveStoneValidator.cs
    public interface IRemoveStoneValidator
    {
        ValidationResult Validate( Guid stonedCardId, List<Guid> burnedCardIds);
    }
}
