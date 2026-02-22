global using Bytecodes = BishBytecode.Bytecodes;

namespace BishBytecode;

public abstract record BishBytecode
{
    public string? Tag;

    public abstract void Execute(BishFrame frame);

    public BishBytecode Tagged(string tag)
    {
        Tag = tag;
        return this;
    }
}