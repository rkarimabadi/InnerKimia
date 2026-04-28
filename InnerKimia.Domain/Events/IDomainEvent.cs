using InnerKimia.Domain.ValueObjects;

namespace InnerKimia.Domain.Events
{
    public interface IDomainEvent { }

    public record CardDrawnEvent(Guid CardId) : IDomainEvent;
    public record CardPlacedOnBoardEvent(Guid CardId, Position Position) : IDomainEvent;
    public record CardStonedEvent(Guid CardId) : IDomainEvent;
    public record CardDiscardedEvent(Guid CardId, Position? Position) : IDomainEvent;
    public record CardBurnedEvent(Guid CardId) : IDomainEvent;
    public record GameWonEvent() : IDomainEvent;
    public record GameLostEvent(string Reason) : IDomainEvent;
}
