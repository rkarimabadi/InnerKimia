using InnerKimia.Domain.ValueObjects;

namespace InnerKimia.Domain.Entities
{
    public class PlayerHand
    {
        private readonly List<GameCard> _cards = new();
        public IReadOnlyCollection<GameCard> Cards => _cards.AsReadOnly();

        public const int MaxSize = 3;
        public int Count => _cards.Count;
        public bool IsFull => Count >= MaxSize;
        public bool NeedsRefill => Count < MaxSize;

        internal void AddCard(GameCard card)
        {
            if (IsFull)
                throw new InvalidOperationException("Player hand is full.");
            if (card.Status != CardStatus.InDeck)
                throw new InvalidOperationException($"Card must be in deck to draw. Current: {card.Status}");

            card.DrawToHand();
            _cards.Add(card);
        }

        internal void RemoveCard(GameCard card)
        {
            if (!_cards.Any(c => c.Id == card.Id))
                throw new InvalidOperationException("Card not found in hand.");

            _cards.Remove(card);
        }

        /// <summary>
        /// برای حذف سنگ دو کارت دلخواه از دست سوزانده می‌شوند. 
        /// </summary>
        internal List<GameCard> BurnCards(List<Guid> cardIds)
        {
            if (cardIds.Count != 2)
                throw new InvalidOperationException("Exactly 2 cards must be burned to remove a stone.");
            if (cardIds.Distinct().Count() != 2)
                throw new InvalidOperationException("Cannot burn the same card twice.");

            var burnedCards = new List<GameCard>();
            foreach (var cardId in cardIds)
            {
                var card = _cards.FirstOrDefault(c => c.Id == cardId);
                if (card == null)
                    throw new InvalidOperationException($"Card {cardId} not in hand.");

                card.BurnFromHand();
                burnedCards.Add(card);
            }

            foreach (var card in burnedCards)
                _cards.Remove(card);

            return burnedCards;
        }

        internal bool Contains(Guid cardId) => _cards.Any(c => c.Id == cardId);
    }
}
