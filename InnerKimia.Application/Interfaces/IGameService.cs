using InnerKimia.Application.DTOs;

namespace InnerKimia.Application.Interfaces
{
    public interface IGameService
    {
        /// <summary>
        /// شروع یک بازی جدید و برگرداندن وضعیت اولیه
        /// </summary>
        Task<GameStateDto> StartNewGameAsync();

        /// <summary>
        /// دریافت وضعیت فعلی بازی
        /// </summary>
        Task<GameStateDto> GetGameStateAsync();

        /// <summary>
        /// قرار دادن یک کارت در موقعیت مشخص
        /// </summary>
        Task<PlacementResultDto> PlaceCardAsync(Guid cardId, PositionDto positionDto);

        /// <summary>
        /// حذف یک سنگ با سوزاندن دو کارت از دست
        /// </summary>
        Task<GameStateDto> RemoveStoneAsync(Guid stonedCardId, List<Guid> burnedCardIds);
    }
}
