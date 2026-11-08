using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.World;

// 移動免費、不看等級；代碼依序：unknownPlace、restricted（禁區且沒有有效通行證也沒有鑰匙）、subPlaceNotAllowed、
// subPlaceRequired、subPlaceOutOfRange、samePlace（禁區通行證已到期時不算，可用鑰匙再次進入）
public static class PlaceMove
{
    public static IReadOnlyList<string> Evaluate(
        Character character, PlaceDefinition? target, int? subPlace, AreaPass? pass, bool hasKey, DateTimeOffset now)
    {
        if (target is null)
        {
            return ["unknownPlace"];
        }

        var reasons = new List<string>();
        var needsKey = AreaKeys.NeedsKey(target, pass, now);
        if (needsKey && !hasKey)
        {
            reasons.Add("restricted");
        }

        if (!target.HasSubPlaces && subPlace is not null)
        {
            reasons.Add("subPlaceNotAllowed");
        }

        if (target.HasSubPlaces && subPlace is null)
        {
            reasons.Add("subPlaceRequired");
        }

        if (target.HasSubPlaces && subPlace is { } index && target.SubPlaces.All(candidate => candidate.Index != index))
        {
            reasons.Add("subPlaceOutOfRange");
        }

        if (character.PlaceId == target.Id && character.SubPlace == subPlace && !needsKey)
        {
            reasons.Add("samePlace");
        }

        return reasons;
    }
}
