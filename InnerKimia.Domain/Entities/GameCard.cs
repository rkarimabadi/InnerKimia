using InnerKimia.Domain.Events;
using InnerKimia.Domain.ValueObjects;

namespace InnerKimia.Domain.Entities
{
    public class GameCard
    {
        public Guid Id { get; }
        public ElementType Element { get; }

        public CardStatus Status { get; private set; }
        public Position? PositionOnBoard { get; private set; }

        // Domain Events
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public GameCard(ElementType element)
        {
            Id = Guid.NewGuid();
            Element = element;
            Status = CardStatus.InDeck;
        }

        public GameCard(Guid id, ElementType element, CardStatus status, Position? position)
        {
            Id = id;
            Element = element;
            Status = status;
            PositionOnBoard = position;
        }

        public void DrawToHand()
        {
            if (Status != CardStatus.InDeck)
                throw new InvalidOperationException($"Cannot draw card. Current status: {Status}");

            Status = CardStatus.InHand;
            _domainEvents.Add(new CardDrawnEvent(Id));
        }

        public void PlaceOnBoard(Position position)
        {
            if (Status != CardStatus.InHand)
                throw new InvalidOperationException($"Cannot place card on board. Current status: {Status}");

            if (!position.IsValid)
                throw new ArgumentException($"Invalid board position: {position}");

            Status = CardStatus.OnBoard;
            PositionOnBoard = position;
            _domainEvents.Add(new CardPlacedOnBoardEvent(Id, position));
        }

        public void TurnToStone()
        {
            if (Status != CardStatus.OnBoard)
                throw new InvalidOperationException($"Cannot stone card. Current status: {Status}");

            Status = CardStatus.Stoned;
            _domainEvents.Add(new CardStonedEvent(Id));
        }

        /// <summary>
        /// حذف سنگ از صفحه - کارت باطل می‌شود
        /// </summary>
        internal void DiscardStone()
        {
            if (Status != CardStatus.Stoned)
                throw new InvalidOperationException($"Cannot discard stone. Current status: {Status}");

            Status = CardStatus.Discarded;
            var position = PositionOnBoard;
            PositionOnBoard = null;
            _domainEvents.Add(new CardDiscardedEvent(Id, position));
        }

        /// <summary>
        /// سوزاندن کارت از دست بازیکن برای حذف سنگ
        /// </summary>
        internal void BurnFromHand()
        {
            if (Status != CardStatus.InHand)
                throw new InvalidOperationException($"Cannot burn card from hand. Current status: {Status}");

            Status = CardStatus.Discarded;
            _domainEvents.Add(new CardBurnedEvent(Id));
        }
        internal void ClearDomainEvents() => _domainEvents.Clear();
    }
}
