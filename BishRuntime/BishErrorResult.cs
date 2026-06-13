namespace BishRuntime;

public class BishErrorResult(BishError error) : BishObject
{
    public readonly BishError Error = error;

    public override BishType DefaultType => StaticType;

    public new static readonly BishType StaticType = new("Error");

    [Builtin]
    public static BishString Repr(BishErrorResult self, BishReprContext ctx) => BishString.Repr(self.Error, ctx);

    [Builtin("hook")]
    public static void Create(BishObject _) => throw BishException.OfType_ErrorResult();
    
    [Builtin("hook")]
    public static BishError Get_error(BishErrorResult self) => self.Error;

    [Builtin("hook")]
    public static BishObject Get(BishErrorResult self, BishString name) => throw new BishException(self.Error);

    [Builtin("hook")]
    public static BishObject Set(BishErrorResult self, BishString name, BishObject _) =>
        throw new BishException(self.Error);

    [Builtin("hook")]
    public static BishObject Def(BishErrorResult self, BishString name, BishObject _) =>
        throw new BishException(self.Error);

    [Builtin("hook")]
    public static BishObject Del(BishErrorResult self, BishString name) => throw new BishException(self.Error);

    [Builtin]
    public static BishBool Bool(BishErrorResult a) => BishBool.False;

    [Builtin]
    public static BishBool Nullish(BishErrorResult a) => BishBool.True;
}