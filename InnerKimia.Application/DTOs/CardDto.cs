namespace InnerKimia.Application.DTOs
{
    public class CardDto
    {
        public Guid Id { get; set; }
        public string Element { get; set; } = string.Empty;
        public string ElementSymbol { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public PositionDto? Position { get; set; }
        public bool IsStoned { get; set; }

        public static Dictionary<string, string> Elements => new() { { "Water", "آب" }, { "Earth", "زمین" }, { "Wind", "باد" }, { "Fire", "آتش" } };
    }
}
