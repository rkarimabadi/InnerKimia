using InnerKimia.Domain.ValueObjects;

namespace InnerKimia.Domain.Snapshots
{
    public class GameSnapshot
    {
        public List<CardSnapshot> AllCards { get; set; } = new();
        
        // تغییر: استفاده از string به جای Position به عنوان کلید
        public Dictionary<string, Guid> Board { get; set; } = new();
        
        public List<Guid> DrawPileOrder { get; set; } = new();
        public List<Guid> DiscardPileOrder { get; set; } = new();
        public List<Guid> PlayerHandOrder { get; set; } = new();
        public GameStatus Status { get; set; }
        
        public GameSnapshot() { }
        
        public GameSnapshot(
            List<CardSnapshot> allCards,
            Dictionary<Position, Guid> board,
            List<Guid> drawPileOrder,
            List<Guid> discardPileOrder,
            List<Guid> playerHandOrder,
            GameStatus status)
        {
            AllCards = allCards;
            
            Board = board.ToDictionary(
                kvp => $"{kvp.Key.Row},{kvp.Key.Column}",
                kvp => kvp.Value
            );
            
            DrawPileOrder = drawPileOrder;
            DiscardPileOrder = discardPileOrder;
            PlayerHandOrder = playerHandOrder;
            Status = status;
        }
    }
}