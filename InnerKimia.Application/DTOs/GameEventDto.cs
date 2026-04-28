namespace InnerKimia.Application.DTOs
{
    public class GameEventDto
    {
        public string Type { get; set; } = string.Empty; // "Placement", "Stoning", "StoneRemoved", "Win", "Lose"
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<Guid> RelatedCardIds { get; set; } = [];
    }
}
