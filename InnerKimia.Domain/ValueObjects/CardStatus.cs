namespace InnerKimia.Domain.ValueObjects
{
    public enum CardStatus
    {
        InDeck,      // در پشته برخورده
        InHand,      // در دست بازیکن
        OnBoard,     // روی صفحه
        Stoned,      // سنگ شده روی صفحه
        Discarded    // باطل شده
    }
}
