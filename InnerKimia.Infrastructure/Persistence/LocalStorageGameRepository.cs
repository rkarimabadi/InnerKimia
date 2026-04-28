using System.Text.Json;
using InnerKimia.Application.Interfaces;
using InnerKimia.Domain.Entities;
using InnerKimia.Domain.Services;
using InnerKimia.Domain.Snapshots;
using InnerKimia.Domain.ValueObjects;
using InnerKimia.Infrastructure.JsonConverters;
using Microsoft.JSInterop;

namespace InnerKimia.Infrastructure.Repositories
{
    public class LocalStorageGameRepository : IGameRepository
    {
        private const string STORAGE_KEY = "innerKimia_game_snapshot";
        private readonly IJSRuntime _jsRuntime;
        private readonly IElementRelationshipService _relationshipService;
        private Game? _cachedGame;
        
        public LocalStorageGameRepository(
            IJSRuntime jsRuntime,
            IElementRelationshipService relationshipService)
        {
            _jsRuntime = jsRuntime;
            _relationshipService = relationshipService;
        }
        
        public async Task SaveAsync(Game game)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));
            
            var snapshot = GetSnapshotFromGame(game);
            var json = JsonSerializer.Serialize(snapshot, GetJsonOptions());
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", STORAGE_KEY, json);
            _cachedGame = game;
        }
        
        public async Task<Game?> GetAsync()
        {
            if (_cachedGame != null)
                return _cachedGame;
            
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", STORAGE_KEY);
            
            if (string.IsNullOrEmpty(json))
                return null;
            
            var snapshot = JsonSerializer.Deserialize<GameSnapshot>(json, GetJsonOptions());
            if (snapshot == null)
                return null;
            
            var game = RestoreGameFromSnapshot(snapshot);
            _cachedGame = game;
            
            return game;
        }
        
        public async Task DeleteAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", STORAGE_KEY);
            _cachedGame = null;
        }
        
        private GameSnapshot GetSnapshotFromGame(Game game)
        {
            var allCards = game.GetAllCards().Select(c => new CardSnapshot
            {
                Id = c.Id,
                Element = c.Element,
                Status = c.Status,
                PositionOnBoard = c.PositionOnBoard
            }).ToList();
            
            // ساخت Board با Position کلید
            var boardWithPositionKey = new Dictionary<Position, Guid>();
            foreach (var card in game.GetCardsOnBoard().Concat(game.GetStonedCards()))
            {
                if (card.PositionOnBoard != null)
                    boardWithPositionKey[card.PositionOnBoard] = card.Id;
            }
            
            return new GameSnapshot(
                allCards,
                boardWithPositionKey,
                game.DrawPile.Select(c => c.Id).ToList(),
                game.DiscardPile.Select(c => c.Id).ToList(),
                game.PlayerHandCards.Select(c => c.Id).ToList(),
                game.Status
            );
        }
        
        private Game RestoreGameFromSnapshot(GameSnapshot snapshot)
        {
            var cardMap = snapshot.AllCards.ToDictionary(
                c => c.Id,
                c => new GameCard(c.Id, c.Element, c.Status, c.PositionOnBoard)
            );
            
            var allCards = snapshot.AllCards.Select(c => cardMap[c.Id]).ToList();
            
            // بازسازی دست بازیکن
            var hand = new PlayerHand();
            foreach (var cardId in snapshot.PlayerHandOrder)
            {
                if (cardMap.TryGetValue(cardId, out var card))
                {
                    if (card.Status != CardStatus.InHand)
                        ForceCardStatus(card, CardStatus.InHand);
                    AddCardToHand(hand, card);
                }
            }
            
            // بازسازی صفحه - تبدیل string key به Position
            var board = new Dictionary<Position, GameCard>();
            foreach (var (positionKey, cardId) in snapshot.Board)
            {
                if (cardMap.TryGetValue(cardId, out var card))
                {
                    var position = ParsePosition(positionKey);
                    board[position] = card;
                    if (card.PositionOnBoard == null || !card.PositionOnBoard.Equals(position))
                        ForceCardPosition(card, position);
                }
            }
            
            // بازسازی پشته برخورده
            var drawPileQueue = new Queue<GameCard>();
            foreach (var cardId in snapshot.DrawPileOrder)
            {
                if (cardMap.TryGetValue(cardId, out var card))
                {
                    if (card.Status != CardStatus.InDeck)
                        ForceCardStatus(card, CardStatus.InDeck);
                    drawPileQueue.Enqueue(card);
                }
            }
            
            // بازسازی پشته باطله
            var discardPile = new List<GameCard>();
            foreach (var cardId in snapshot.DiscardPileOrder)
            {
                if (cardMap.TryGetValue(cardId, out var card))
                {
                    if (card.Status != CardStatus.Discarded)
                        ForceCardStatus(card, CardStatus.Discarded);
                    discardPile.Add(card);
                }
            }
            
            return new Game(
                _relationshipService,
                allCards,
                hand,
                board,
                drawPileQueue,
                discardPile,
                snapshot.Status
            );
        }
        
        // متدهای کمکی
        private Position ParsePosition(string positionKey)
        {
            var parts = positionKey.Split(',');
            if (parts.Length == 2 && 
                int.TryParse(parts[0], out int row) && 
                int.TryParse(parts[1], out int col))
            {
                return new Position(row, col);
            }
            throw new InvalidOperationException($"Invalid position key: {positionKey}");
        }
        
        private void ForceCardStatus(GameCard card, CardStatus newStatus)
        {
            var statusField = typeof(GameCard).GetField(
                "<Status>k__BackingField",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (statusField != null)
                statusField.SetValue(card, newStatus);
        }
        
        private void ForceCardPosition(GameCard card, Position position)
        {
            var positionField = typeof(GameCard).GetField(
                "<PositionOnBoard>k__BackingField",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (positionField != null)
                positionField.SetValue(card, position);
        }
        
        private void AddCardToHand(PlayerHand hand, GameCard card)
        {
            var cardsField = typeof(PlayerHand).GetField("_cards",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (cardsField != null)
            {
                var cards = cardsField.GetValue(hand) as List<GameCard>;
                cards?.Add(card);
            }
        }
        
        private JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = 
                { 
                    new ElementTypeJsonConverter(), 
                    new CardStatusJsonConverter(),
                    new PositionValueConverter()  
                }
            };
        }
    }
}