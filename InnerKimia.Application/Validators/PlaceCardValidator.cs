using InnerKimia.Application.DTOs;

namespace InnerKimia.Application.Validators
{

    public class PlaceCardValidator : IPlaceCardValidator
    {
        public ValidationResult Validate( Guid cardId, PositionDto position)
        {
            var errors = new List<string>();

            // اعتبارسنجی CardId
            if (cardId == Guid.Empty)
                errors.Add("Card ID is required.");

            // اعتبارسنجی Position
            if (position == null)
            {
                errors.Add("Position is required.");
            }
            else
            {
                if (position.Row < 0 || position.Row > 2)
                    errors.Add($"Invalid row: {position.Row}. Must be between 0-2.");

                if (position.Column < 0 || position.Column > 2)
                    errors.Add($"Invalid column: {position.Column}. Must be between 0-2.");
            }

            return new ValidationResult(errors);
        }
    }
}
