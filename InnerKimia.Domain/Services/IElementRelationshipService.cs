using InnerKimia.Domain.ValueObjects;

namespace InnerKimia.Domain.Services
{
    public interface IElementRelationshipService
    {
        ElementRelationship DetermineRelationship(ElementType first, ElementType second);
    }

}
