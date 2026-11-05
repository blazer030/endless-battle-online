namespace EndlessBattle.Domain.Forge;

// 鍛造消耗的道具（內容 id）：原版的分離魔石LV1、劣質打孔石，安全精鍊石原版資料沒有、用自訂的；缺少時原因代碼為 missingTool:<id>
public static class ForgeTools
{
    public const string SafeRefineStone = "safe-refine-stone", StoneRemover = "item-1378", HoleDriller = "item-3361";

    public static string Missing(string toolId) => $"missingTool:{toolId}";
}
