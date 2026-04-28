using InnerKimia.Domain.ValueObjects;

namespace InnerKimia.Domain.Snapshots
{
    public class CardSnapshot
    {
        public Guid Id { get; set; }
        public ElementType Element { get; set; }
        public CardStatus Status { get; set; }
        public Position? PositionOnBoard { get; set; }
        
        public CardSnapshot() { }
        
        public CardSnapshot(Guid id, ElementType element, CardStatus status, Position? positionOnBoard)
        {
            Id = id;
            Element = element;
            Status = status;
            PositionOnBoard = positionOnBoard;
        }
    }
}