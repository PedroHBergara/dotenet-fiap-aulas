namespace _2_OO;

public class Print : _BaseEntity
{
    public required Card Card { get; set; }
    public required Rarity Rarity { get; set; }
    public required int Number { get; set; }
    public required string Set { get; set; }
}