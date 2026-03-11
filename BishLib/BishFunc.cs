using BishRuntime;

namespace BishLib;

public static class BishFuncModule
{
    public static BishObject Module => new BishObject
    {
        Members = new Dictionary<string, BishObject>
        {
            ["Func"] = BishFunc.StaticType,
            ["Arg"] = BishArgObject.StaticType
        }
    };
}