using InnerKimia.Application.DTOs;

namespace InnerKimia.Application.Validators
{
    // Application/Validators/IPlaceCardValidator.cs
    public interface IPlaceCardValidator
    {
        ValidationResult Validate(Guid cardId, PositionDto position);
    }
}
