namespace EndlessBattle.Domain.Forge;

// 鍛造消耗的道具（內容 id）；缺少時原因代碼為 missingTool:<id>
public static class ForgeTools
{
    public const string SafeRefineStone = "safe-refine-stone", StoneRemover = "stone-remover", HoleDriller = "hole-driller";

    public static string Missing(string toolId) => $"missingTool:{toolId}";
}
