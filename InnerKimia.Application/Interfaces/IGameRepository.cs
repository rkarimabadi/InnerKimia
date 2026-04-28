using InnerKimia.Domain.Entities;

namespace InnerKimia.Application.Interfaces
{
    public interface IGameRepository
    {
        /// <summary>
        /// بازیابی بازی
        /// </summary>
        Task<Game?> GetAsync();

        /// <summary>
        /// ذخیره یا به‌روزرسانی بازی
        /// </summary>
        Task SaveAsync(Game game);

        /// <summary>
        /// حذف یک بازی
        /// </summary>
        Task DeleteAsync();
    }
}
