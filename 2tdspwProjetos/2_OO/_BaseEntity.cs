namespace _2_OO;

public abstract class _BaseEntity
{
    public int Id { get; set; }
    public bool IsAvailable { get; set; }

    public void MakeAvailable() => IsAvailable = true;
}