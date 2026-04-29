namespace BishRuntime;

public static class BishIterator
{
    public static readonly BishType Type = new("Iterator");
    public static readonly BishType AsyncType = new("AsyncIterator");

    public class Stop : BishObject
    {
        public static readonly Stop Instance = new();

        private Stop()
        {
        }

        public override string ToString() => "IteratorStop";
    }
}