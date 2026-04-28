namespace InnerKimia.Application.DTOs
{
    public class PositionDto
    {
        public int Row { get; set; }
        public int Column { get; set; }

        public PositionDto() { }

        public PositionDto(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public override string ToString() => $"({Row},{Column})";
    }
}
