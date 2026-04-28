using InnerKimia.Application.DTOs;
using InnerKimia.Application.Exceptions;
using InnerKimia.Application.Interfaces;
using InnerKimia.Application.Validators;
using InnerKimia.Domain.Entities;
using InnerKimia.Domain.Events;
using InnerKimia.Domain.Exceptions;
using InnerKimia.Domain.Services;
using InnerKimia.Domain.ValueObjects;
using Microsoft.Extensions.Logging;


namespace InnerKimia.Application.Services
{
    public class GameService : IGameService
    {
        private readonly IGameRepository _gameRepository;
        private readonly IElementRelationshipService _relationshipService;
        private readonly IPlaceCardValidator _placeCardValidator;
        private readonly IRemoveStoneValidator _removeStoneValidator;
        private readonly ILogger<GameService> _logger;

        public GameService(
            IGameRepository gameRepository,
            IElementRelationshipService relationshipService,
            IPlaceCardValidator placeCardValidator,
            IRemoveStoneValidator removeStoneValidator,
            ILogger<GameService> logger)
        {
            _gameRepository = gameRepository ?? throw new ArgumentNullException(nameof(gameRepository));
            _relationshipService = relationshipService ?? throw new ArgumentNullException(nameof(relationshipService));
            _placeCardValidator = placeCardValidator ?? throw new ArgumentNullException(nameof(placeCardValidator));
            _removeStoneValidator = removeStoneValidator ?? throw new ArgumentNullException(nameof(removeStoneValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<GameStateDto> StartNewGameAsync()
        {
            _logger.LogInformation("Starting a new game...");

            // 1. ایجاد بازی جدید از Domain
            var game = new Game(_relationshipService);

            // 2. ذخیره‌سازی در Repository
            await _gameRepository.SaveAsync(game);

            _logger.LogInformation("New game created");

            // 3. تبدیل به DTO و برگرداندن
            return MapToGameStateDto(game);
        }

        public async Task<GameStateDto> GetGameStateAsync()
        {
            _logger.LogInformation("Fetching game state");

            var game = await GetGameOrThrowAsync();

            return MapToGameStateDto(game);
        }

        public async Task<PlacementResultDto> PlaceCardAsync(Guid cardId, PositionDto positionDto)
        {
            _logger.LogInformation("Placing card {CardId} at position {Position} in game",
                cardId, positionDto);

            var validationResult = _placeCardValidator.Validate(cardId, positionDto);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Validation failed for placing card: {Errors}",
                    validationResult.ErrorMessage);

                return PlacementResultDto.Failure(validationResult.ErrorMessage);
            }

            try
            {
                // 1. بازیابی بازی
                var game = await GetGameOrThrowAsync();

                // 2. تبدیل PositionDto به Domain Position
                var position = new Position(positionDto.Row, positionDto.Column);

                // 3. ثبت Domain Events قبل از عملیات برای تشخیص تغییرات
                var domainEventsBefore = new List<IDomainEvent>();

                // 4. اجرای Business Logic
                if(game.Status == GameStatus.WaitingForFirstPlacement)
                {
                    game.PlaceInitialCard(cardId);
                }
                else
                {
                    game.PlaceCard(cardId, position);
                }

                // 5. جمع‌آوری رویدادهای جدید
                var newEvents = game.DomainEvents.ToList();
                var stonedCards = ExtractStonedCards(game, newEvents);

                // 6. ذخیره‌سازی تغییرات
                await _gameRepository.SaveAsync(game);

                // 7. بررسی وضعیت نهایی
                var isWon = newEvents.OfType<GameWonEvent>().Any();
                var gameState = MapToGameStateDto(game, newEvents);

                return PlacementResultDto.Success(gameState, stonedCards, isWon);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid placement attempt in game");
                return PlacementResultDto.Failure(ex.Message);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain rule violation in game");
                return PlacementResultDto.Failure(ex.Message);
            }
        }

        public async Task<GameStateDto> RemoveStoneAsync(Guid stonedCardId, List<Guid> burnedCardIds)
        {
            _logger.LogInformation("Removing stone {StonedCardId} by burning {BurnedCards} in game",
                stonedCardId, string.Join(",", burnedCardIds));
            
            var validationResult = _removeStoneValidator.Validate(stonedCardId, burnedCardIds);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Validation failed for removing stone: {Errors}",
                    validationResult.ErrorMessage);

                throw new InvalidOperationException(validationResult.ErrorMessage);
            }

            // 1. بازیابی بازی
            var game = await GetGameOrThrowAsync();

            // 2. اعتبارسنجی تعداد کارت‌های سوختنی
            if (burnedCardIds.Count != 2)
                throw new InvalidOperationException("Exactly 2 cards must be burned to remove a stone.");

            // 3. اجرای عملیات حذف سنگ
            game.RemoveStonedCard(stonedCardId, burnedCardIds);

            // 4. ذخیره‌سازی
            await _gameRepository.SaveAsync(game);

            // 5. برگرداندن وضعیت جدید
            var events = game.DomainEvents.ToList();
            return MapToGameStateDto(game, events);
        }

        // ==================== Private Helper Methods ====================

        private async Task<Game> GetGameOrThrowAsync()
        {
            var game = await _gameRepository.GetAsync();
            if (game == null)
            {
                _logger.LogError("Game not found");
                throw new GameNotFoundException();
            }
            return game;
        }

        private List<CardDto> ExtractStonedCards(Game game, List<IDomainEvent> events)
        {
            return events
                .OfType<CardStonedEvent>()
                .Select(e =>
                {
                    var card = game.GetCardById(e.CardId);
                    return MapToCardDto(card);
                })
                .ToList();
        }

        private GameStateDto MapToGameStateDto(Game game, List<IDomainEvent>? events = null)
        {
            var dto = new GameStateDto
            {
                Status = game.Status.ToString(),
                DrawPileCount = game.DrawPileCount,
                DiscardPileCount = game.DiscardPileCount,
                IsGameOver = game.Status is GameStatus.Won or GameStatus.Lost,
                IsWon = game.Status == GameStatus.Won,
                PlayerHand = game.PlayerHandCards.Select(MapToCardDto).ToList(),
                Board = MapBoardToDto(game.Board),
                RecentEvents = MapEventsToDto(events ?? game.DomainEvents.ToList())
            };

            return dto;
        }

        private Dictionary<string, CardDto?> MapBoardToDto(IReadOnlyDictionary<Position, GameCard> board)
        {
            var boardDto = new Dictionary<string, CardDto?>();

            // مقداردهی اولیه همه ۹ خانه
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    var key = $"{row},{col}";
                    boardDto[key] = null;
                }
            }

            // پر کردن خانه‌های اشغال شده
            foreach (var (position, card) in board)
            {
                var key = $"{position.Row},{position.Column}";
                boardDto[key] = MapToCardDto(card);
            }

            return boardDto;
        }

        private CardDto MapToCardDto(GameCard card)
        {
            return new CardDto
            {
                Id = card.Id,
                Element = card.Element.ToString(),
                ElementSymbol = GetElementSymbol(card.Element),
                Status = card.Status.ToString(),
                Position = card.PositionOnBoard != null
                    ? new PositionDto(card.PositionOnBoard.Row, card.PositionOnBoard.Column)
                    : null,
                IsStoned = card.Status == CardStatus.Stoned
            };
        }

        private static string GetElementSymbol(ElementType element) => element switch
        {
            ElementType.Water => "💧",
            ElementType.Earth => "🌍",
            ElementType.Fire => "🔥",
            ElementType.Wind => "💨",
            _ => "❓"
        };

        private static List<GameEventDto> MapEventsToDto(List<IDomainEvent> events)
        {
            var eventDtos = new List<GameEventDto>();

            foreach (var domainEvent in events)
            {
                var dto = domainEvent switch
                {
                    CardPlacedOnBoardEvent e => new GameEventDto
                    {
                        Type = "Placement",
                        Message = $"Card {e.CardId} placed at {e.Position}",
                        RelatedCardIds = new List<Guid> { e.CardId }
                    },
                    CardStonedEvent e => new GameEventDto
                    {
                        Type = "Stoning",
                        Message = $"Card {e.CardId} has been stoned!",
                        RelatedCardIds = new List<Guid> { e.CardId }
                    },
                    CardDiscardedEvent e => new GameEventDto
                    {
                        Type = "StoneRemoved",
                        Message = $"Stone at {e.Position} has been removed",
                        RelatedCardIds = new List<Guid> { e.CardId }
                    },
                    GameWonEvent => new GameEventDto
                    {
                        Type = "Win",
                        Message = "🎉 Congratulations! You've won the game!",
                    },
                    GameLostEvent e => new GameEventDto
                    {
                        Type = "Lose",
                        Message = $"Game Over: {e.Reason}",
                    },
                    _ => null
                };

                if (dto != null)
                    eventDtos.Add(dto);
            }

            return eventDtos;
        }
    }
}
