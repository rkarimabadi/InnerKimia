namespace InnerKimia.Application.DTOs
{
    public class PlacementResultDto
    {
        /// <summary>
        /// آیا جای‌گذاری موفق بود؟
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// وضعیت جدید بازی بعد از جای‌گذاری
        /// </summary>
        public GameStateDto? GameState { get; set; }

        /// <summary>
        /// کارت‌هایی که در این حرکت سنگ شدند
        /// </summary>
        public List<CardDto> StonedCards { get; set; } = new();

        /// <summary>
        /// پیام خطا در صورت عدم موفقیت
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// آیا بازی بعد از این حرکت تمام شد؟
        /// </summary>
        public bool IsGameOver { get; set; }

        /// <summary>
        /// آیا این حرکت باعث برد شد؟
        /// </summary>
        public bool IsWon { get; set; }

        public static PlacementResultDto Success(GameStateDto gameState, List<CardDto> stonedCards, bool isWon)
            => new()
            {
                IsSuccess = true,
                GameState = gameState,
                StonedCards = stonedCards,
                IsGameOver = isWon || gameState.IsGameOver,
                IsWon = isWon
            };

        public static PlacementResultDto Failure(string errorMessage)
            => new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
