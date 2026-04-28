namespace InnerKimia.Application.DTOs
{
    public class GameStateDto
    {
        public Dictionary<string, CardDto?> Board { get; set; } = new();
        public List<CardDto> PlayerHand { get; set; } = new();
        public int DrawPileCount { get; set; }
        public int DiscardPileCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsGameOver { get; set; }
        public bool IsWon { get; set; }
        public List<GameEventDto> RecentEvents { get; set; } = new();
    }
}
