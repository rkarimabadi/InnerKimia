namespace InnerKimia.Application.Validators
{
    // Application/Validators/RemoveStoneValidator.cs
    public class RemoveStoneValidator : IRemoveStoneValidator
    {
        public ValidationResult Validate(Guid stonedCardId, List<Guid> burnedCardIds)
        {
            var errors = new List<string>();

            if (stonedCardId == Guid.Empty)
                errors.Add("Stoned card ID is required.");

            if (burnedCardIds == null)
            {
                errors.Add("Burned cards list is required.");
            }
            else
            {
                if (burnedCardIds.Count != 2)
                    errors.Add($"Exactly 2 cards must be burned. Provided: {burnedCardIds.Count}.");

                if (burnedCardIds.Any(id => id == Guid.Empty))
                    errors.Add("Invalid card ID in burned cards list.");

                if (burnedCardIds.Distinct().Count() != burnedCardIds.Count)
                    errors.Add("Cannot burn the same card twice.");
            }

            return new ValidationResult(errors);
        }
    }
}
