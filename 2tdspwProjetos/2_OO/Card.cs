namespace _2_OO;

public class Card : _BaseEntity
{
    public required string Name { get; set; }
    public string? Text { get; set; }
    public string? ManaCost { get; set; } //= "{2}{W}";
    public int? Cmc { get; set; } //= 3;
    public List<string> Types { get; set; } = [];
    public List<string> Subtypes { get; set; } = [];
}