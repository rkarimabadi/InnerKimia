using InnerKimia.Domain.ValueObjects;

namespace InnerKimia.Domain.Services
{
    public class ElementRelationshipService : IElementRelationshipService
    {
        public ElementRelationship DetermineRelationship(ElementType first, ElementType second)
        {
            if (first == second) return ElementRelationship.Same;

            if ((first == ElementType.Water && second == ElementType.Fire) ||
                (first == ElementType.Fire && second == ElementType.Water) ||
                (first == ElementType.Earth && second == ElementType.Wind) ||
                (first == ElementType.Wind && second == ElementType.Earth))
                return ElementRelationship.Opposite;

            if ((first == ElementType.Fire && second == ElementType.Wind) ||
                (first == ElementType.Wind && second == ElementType.Fire) ||
                (first == ElementType.Water && second == ElementType.Earth) ||
                (first == ElementType.Earth && second == ElementType.Water))
                return ElementRelationship.Friendly;

            return ElementRelationship.Neutral;
        }
    }
}
