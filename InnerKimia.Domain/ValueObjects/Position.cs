namespace InnerKimia.Domain.ValueObjects
{
    public record Position(int Row, int Column)
    {
        public static Position Center => new(1, 1);
        public bool IsValid => Row is >= 0 and <= 2 && Column is >= 0 and <= 2;

        public IEnumerable<Position> GetOrthogonalNeighbors()
        {
            if (Row > 0)    yield return new Position(Row - 1, Column); // بالا
            if (Row < 2)    yield return new Position(Row + 1, Column); // پایین
            if (Column > 0) yield return new Position(Row, Column - 1); // چپ
            if (Column < 2) yield return new Position(Row, Column + 1); // راست
        }
    }
}
