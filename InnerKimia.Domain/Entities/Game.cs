using InnerKimia.Domain.Events;
using InnerKimia.Domain.Services;
using InnerKimia.Domain.ValueObjects;

namespace InnerKimia.Domain.Entities
{
    public class Game
    {
        private readonly IElementRelationshipService _relationshipService;
        private readonly List<GameCard> _allCards;          // تمام 36 کارت
        private readonly PlayerHand _playerHand;
        private readonly Dictionary<Position, GameCard> _board;  // فقط کارت‌های OnBoard یا Stoned

        // پشته‌های مجزا
        private readonly Queue<GameCard> _drawPile;          // پشته برخورده (Deck)
        private readonly List<GameCard> _discardPile;        // کارت‌های باطل شده
        /// <summary>
        /// خلاصه وضعیت بازی برای لاگ و دیباگ
        /// </summary>
        public string Summary =>
            $"Status: {Status} | " +
            $"Board: {HealthyCardsOnBoardCount} healthy, {StonedCardsCount} stoned | " +
            $"Hand: {_playerHand.Count}/{PlayerHand.MaxSize} | " +
            $"Draw: {DrawPileCount} | Discard: {DiscardPileCount}";

        public IReadOnlyDictionary<Position, GameCard> Board => _board.AsReadOnly();
        public IReadOnlyCollection<GameCard> PlayerHandCards => _playerHand.Cards;
        public IReadOnlyCollection<GameCard> DrawPile => _drawPile.ToList().AsReadOnly();
        public IReadOnlyCollection<GameCard> DiscardPile => _discardPile.AsReadOnly();

        public int DrawPileCount => _drawPile.Count;
        public int DiscardPileCount => _discardPile.Count;
        public int OccupiedCellsCount => _board.Count;
        public int EmptyCellsCount => 9 - _board.Count;
        public int HealthyCardsOnBoardCount => _board.Values.Count(c => c.Status == CardStatus.OnBoard);
        public int StonedCardsCount => _board.Values.Count(c => c.Status == CardStatus.Stoned);
        public int TotalCardsCount => _allCards.Count;
        public int CardsInPlayCount => _playerHand.Count + _board.Count + _drawPile.Count;

        public bool IsDrawPileEmpty => _drawPile.Count == 0;
        public bool IsHandEmpty => _playerHand.Count == 0;
        public bool IsBoardFull => _board.Count == 9;
        public bool HasStones => _board.Values.Any(c => c.Status == CardStatus.Stoned);

        public GameStatus Status { get; private set; }
        public IReadOnlyCollection<GameCard> GetAllCards()
        {
            return _allCards.AsReadOnly();
        }
        public IReadOnlyCollection<IDomainEvent> DomainEvents { get; private set; }

        // سازنده برای شروع بازی جدید
        public Game(IElementRelationshipService relationshipService)
        {
            _relationshipService = relationshipService;
            _playerHand = new PlayerHand();
            _board = new Dictionary<Position, GameCard>();
            _discardPile = new List<GameCard>();

            // 1. ساخت دقیقاً 36 کارت، 9 کارت از هر نوع
            _allCards = CreateFullDeck();

            // 2. بر زدن کارت‌ها
            var shuffled = _allCards.OrderBy(_ => Guid.NewGuid()).ToList();
            _drawPile = new Queue<GameCard>(shuffled);

            // 3. برداشتن 3 کارت اول برای دست بازیکن
            DrawCardsToFillHand();

            Status = GameStatus.WaitingForFirstPlacement;
            DomainEvents = [];
        }

        /// <summary>
        /// Factory method برای بازسازی از دیتابیس
        /// </summary>
        public Game(
            IElementRelationshipService relationshipService,
            List<GameCard> allCards,
            PlayerHand hand,
            Dictionary<Position, GameCard> board,
            Queue<GameCard> drawPile,
            List<GameCard> discardPile,
            GameStatus status)
        {
            _relationshipService = relationshipService;
            _allCards = allCards;
            _playerHand = hand;
            _board = board;
            _drawPile = drawPile;
            _discardPile = discardPile;
            Status = status;
            DomainEvents = [];
        }

        // ==================== اعمال بازی ====================

        /// <summary>
        /// قرار دادن اولین کارت (فقط در مرکز)
        /// </summary>
        public void PlaceInitialCard(Guid cardId)
        {
            if (Status != GameStatus.WaitingForFirstPlacement)
                throw new InvalidOperationException("Game is not in initial placement phase.");
            if (!_playerHand.Contains(cardId))
                throw new InvalidOperationException("Card not in hand.");

            var card = _allCards.First(c => c.Id == cardId);

            // قرار دادن روی مرکز
            InternalPlaceCard(card, Position.Center);

            // بررسی برد، البته نمیشه با یه کارت برد!
            CheckWinCondition();

            // پر کردن دست
            DrawCardsToFillHand();

            Status = GameStatus.InProgress;
        }

        /// <summary>
        /// قرار دادن کارت‌های بعدی با رعایت قانون همسایگی و سنگ
        /// </summary>
        public void PlaceCard(Guid cardId, Position targetPosition)
        {
            if (Status != GameStatus.InProgress && Status != GameStatus.WaitingForFirstPlacement)
                throw new InvalidOperationException("Game is not in progress.");
            if (!_playerHand.Contains(cardId))
                throw new InvalidOperationException("Card not in hand.");
            if (!targetPosition.IsValid)
                throw new InvalidOperationException("Invalid board position.");
            if (_board.ContainsKey(targetPosition))
                throw new InvalidOperationException("Position is already occupied.");

            // قانون ۴: باید حداقل یک همسایه داشته باشد
            var neighbors = targetPosition.GetOrthogonalNeighbors()
                .Where(pos => _board.ContainsKey(pos))
                .ToList();

            if (!neighbors.Any() && Status != GameStatus.WaitingForFirstPlacement)
                throw new InvalidOperationException("Card must be placed adjacent to an existing card.");

            var card = _allCards.First(c => c.Id == cardId);

            // قانون ۱ و ۲ و ۵: بررسی سنگ
            InternalPlaceCard(card, targetPosition);

            // اعمال قوانین سنگ
            var stonedCardIds = ApplyStoningRules(card, targetPosition, neighbors);

            // پر کردن دست
            DrawCardsToFillHand();

            // بررسی وضعیت بازی
            if (!CheckWinCondition())
            {
                CheckLoseCondition();
            }
        }

        /// <summary>
        /// حذف یک سنگ از صفحه با سوزاندن ۲ کارت از دست
        /// </summary>
        public void RemoveStonedCard(Guid stonedCardId, List<Guid> burnedCardIds)
        {
            if (Status != GameStatus.InProgress)
                throw new InvalidOperationException("Game is not in progress.");

            // کارت سنگ شده را پیدا کن
            var stonedCard = _allCards.FirstOrDefault(c => c.Id == stonedCardId && c.Status == CardStatus.Stoned);
            if (stonedCard == null)
                throw new InvalidOperationException("Specified card is not stoned or not on board.");

            // دو کارت از دست بسوزان
            var burnedCards = _playerHand.BurnCards(burnedCardIds);

            // کارت‌های سوخته به discard pile
            foreach (var burned in burnedCards)
            {
                _discardPile.Add(burned);
            }

            // حذف سنگ
            if (stonedCard.PositionOnBoard != null)
                _board.Remove(stonedCard.PositionOnBoard);

            stonedCard.DiscardStone();
            _discardPile.Add(stonedCard);

            // پر کردن دست
            DrawCardsToFillHand();

            // بررسی باخت
            CheckLoseCondition();
        }
        /// <summary>
        /// پیدا کردن یک کارت با شناسه
        /// </summary>
        public GameCard GetCardById(Guid cardId)
        {
            var card = _allCards.FirstOrDefault(c => c.Id == cardId);
            if (card == null)
                throw new InvalidOperationException($"Card with ID '{cardId}' not found in game.");
            return card;
        }
        /// <summary>
        /// بررسی وجود کارت با شناسه مشخص
        /// </summary>
        public bool HasCard(Guid cardId)
        {
            return _allCards.Any(c => c.Id == cardId);
        }

        /// <summary>
        /// دریافت کارت‌های روی صفحه (OnBoard)
        /// </summary>
        public IEnumerable<GameCard> GetCardsOnBoard()
        {
            return _board.Values.Where(c => c.Status == CardStatus.OnBoard);
        }
        /// <summary>
        /// دریافت کارت‌های سنگ شده
        /// </summary>
        public IEnumerable<GameCard> GetStonedCards()
        {
            return _board.Values.Where(c => c.Status == CardStatus.Stoned);
        }

        /// <summary>
        /// دریافت کارت در یک موقعیت خاص
        /// </summary>
        public GameCard? GetCardAtPosition(Position position)
        {
            _board.TryGetValue(position, out var card);
            return card;
        }

        /// <summary>
        /// دریافت کارت‌های باطل شده (Discarded)
        /// </summary>
        public IEnumerable<GameCard> GetDiscardedCards()
        {
            return _discardPile;
        }

        /// <summary>
        /// بازسازی بازی از وضعیت ذخیره شده در LocalStorage
        /// </summary>
        /// <param name="relationshipService">سرویس تشخیص رابطه عناصر</param>
        /// <param name="status">وضعیت بازی</param>
        /// <param name="drawPileCards">کارت‌های پشته برخورده (به ترتیب)</param>
        /// <param name="playerHandCards">کارت‌های دست بازیکن</param>
        /// <param name="board">کارت‌های روی صفحه با موقعیت</param>
        /// <returns>بازی بازسازی شده</returns>
        public static Game RestoreFromState(
            IElementRelationshipService relationshipService,
            GameStatus status,
            List<GameCard> drawPileCards,
            List<GameCard> playerHandCards,
            Dictionary<Position, GameCard> board)
        {
            if (relationshipService == null)
                throw new ArgumentNullException(nameof(relationshipService));

            if (drawPileCards == null)
                throw new ArgumentNullException(nameof(drawPileCards));

            if (playerHandCards == null)
                throw new ArgumentNullException(nameof(playerHandCards));

            if (board == null)
                throw new ArgumentNullException(nameof(board));

            //  بازسازی دست بازیکن
            var hand = new PlayerHand();
            foreach (var card in playerHandCards)
            {
                var _card = new GameCard(card.Id, card.Element, CardStatus.InDeck, card.PositionOnBoard);
                hand.AddCard(_card);
            }

            // جمع‌آوری همه کارت‌ها در یک لیست
            var allCards = new List<GameCard>();
            allCards.AddRange(drawPileCards);
            allCards.AddRange(playerHandCards);
            allCards.AddRange(board.Values);

            var discardPile = new List<GameCard>();

            var game = new Game(relationshipService, allCards, hand, board, new Queue<GameCard>(drawPileCards), discardPile, status);

            //  اعتبارسنجی نهایی
            game.ValidateState();

            return game;
        }

        /// <summary>
        /// اعتبارسنجی وضعیت بازی بعد از بازسازی
        /// </summary>
        private void ValidateState()
        {
            // تعداد کارت‌ها باید ۳۶ باشد
            if (_allCards.Count != 36)
                throw new InvalidOperationException(
                    $"Invalid card count after restore: {_allCards.Count}. Expected 36.");

            // هر عنصر باید ۹ کارت داشته باشد
            foreach (ElementType element in Enum.GetValues<ElementType>())
            {
                var count = _allCards.Count(c => c.Element == element);
                if (count != 9)
                    throw new InvalidOperationException(
                        $"Invalid {element} card count: {count}. Expected 9.");
            }
        }
        // ==================== متدهای خصوصی ====================

        private void InternalPlaceCard(GameCard card, Position position)
        {
            _playerHand.RemoveCard(card);
            card.PlaceOnBoard(position);
            _board[position] = card;
        }

        private List<GameCard> ApplyStoningRules(GameCard placedCard, Position targetPosition, List<Position> neighborPositions)
        {
            var stonedCards = new HashSet<GameCard>();

            foreach (var neighborPos in neighborPositions)
            {
                var neighbor = _board[neighborPos];
                if (neighbor.Status == CardStatus.Stoned)
                    continue;
                var relationship = _relationshipService.DetermineRelationship(placedCard.Element, neighbor.Element);

                switch (relationship)
                {
                    case ElementRelationship.Same:
                    case ElementRelationship.Opposite:
                        // قانون ۱ و ۲: همشکل یا متضاد → هر دو سنگ می‌شوند
                        stonedCards.Add(placedCard);
                        stonedCards.Add(neighbor);
                        break;

                    case ElementRelationship.Neutral:
                        // قانون ۵: بررسی خنثی مضاعف
                        var neighborNeighbors = neighborPos.GetOrthogonalNeighbors()
                            .Where(pos => _board.ContainsKey(pos) && pos != targetPosition)
                            .Select(pos => _board[pos]);

                        foreach (var n2 in neighborNeighbors)
                        {
                            if (n2.Status == CardStatus.OnBoard)
                            {
                                var rel2 = _relationshipService.DetermineRelationship(neighbor.Element, n2.Element);
                                if (rel2 == ElementRelationship.Neutral)
                                {
                                    stonedCards.Add(placedCard);
                                    stonedCards.Add(neighbor);
                                    stonedCards.Add(n2);
                                }
                            }
                        }
                        break;

                    case ElementRelationship.Friendly:
                        // قانون ۳: کارتهای دوست - همه چیز خوب است
                        break;
                }
            }

            // اعمال تغییر وضعیت به Stoned
            foreach (var card in stonedCards)
            {
                if (card.Status == CardStatus.OnBoard) // فقط کارت‌های سالم سنگ می‌شوند
                    card.TurnToStone();
            }

            return stonedCards.ToList();
        }

        private void DrawCardsToFillHand()
        {
            while (_playerHand.NeedsRefill && _drawPile.Any())
            {
                var card = _drawPile.Dequeue();
                _playerHand.AddCard(card);
            }
        }

        private bool CheckWinCondition()
        {
            // برد: همه ۹ خانه پر شده AND هیچ سنگی وجود نداشته باشد
            var activeCardsOnBoard = _board.Values.Count(c => c.Status == CardStatus.OnBoard);

            if (activeCardsOnBoard == 9 && !_board.Values.Any(c => c.Status == CardStatus.Stoned))
            {
                Status = GameStatus.Won;
                DomainEvents = new List<IDomainEvent> { new GameWonEvent() };
                return true;
            }
            return false;
        }

        private void CheckLoseCondition()
        {
            // باخت: پشته خالی AND دست خالی AND هنوز صفحه پر نشده
            if (_drawPile.Count == 0 && _playerHand.Count == 0)
            {
                var activeCards = _board.Values.Count(c => c.Status == CardStatus.OnBoard);
                if (activeCards < 9 || _board.Values.Any(c => c.Status == CardStatus.Stoned))
                {
                    Status = GameStatus.Lost;
                    DomainEvents = new List<IDomainEvent> {
                    new GameLostEvent("No more cards in draw pile and hand is empty.")
                };
                }
            }
        }

        private static List<GameCard> CreateFullDeck()
        {
            var cards = new List<GameCard>(36);
            foreach (ElementType element in Enum.GetValues<ElementType>())
            {
                for (int i = 0; i < 9; i++) // دقیقاً ۹ عدد از هر نوع
                {
                    cards.Add(new GameCard(element));
                }
            }
            return cards;
        }

    }
}
