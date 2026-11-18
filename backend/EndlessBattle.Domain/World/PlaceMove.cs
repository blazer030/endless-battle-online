using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.World;

// 移動免費、不看等級；代碼依序：UNKNOWN_PLACE、restricted（禁區且沒有有效通行證也沒有鑰匙）、SUB_PLACE_NOT_ALLOWED、
// SUB_PLACE_REQUIRED、SUB_PLACE_OUT_OF_RANGE、SAME_PLACE（禁區通行證已到期時不算，可用鑰匙再次進入）
public static class PlaceMove
{
    public static IReadOnlyList<string> Evaluate(
        Character character, PlaceDefinition? target, int? subPlace, AreaPass? pass, bool hasKey, DateTimeOffset now)
    {
        if (target is null)
        {
            return [ReasonCodes.UnknownPlace];
        }

        var reasons = new List<string>();
        var needsKey = AreaKeys.NeedsKey(target, pass, now);
        if (needsKey && !hasKey)
        {
            reasons.Add(ReasonCodes.Restricted);
        }

        if (!target.HasSubPlaces && subPlace is not null)
        {
            reasons.Add(ReasonCodes.SubPlaceNotAllowed);
        }

        if (target.HasSubPlaces && subPlace is null)
        {
            reasons.Add(ReasonCodes.SubPlaceRequired);
        }

        if (target.HasSubPlaces && subPlace is { } index && target.SubPlaces.All(candidate => candidate.Index != index))
        {
            reasons.Add(ReasonCodes.SubPlaceOutOfRange);
        }

        if (character.PlaceId == target.Id && character.SubPlace == subPlace && !needsKey)
        {
            reasons.Add(ReasonCodes.SamePlace);
        }

        return reasons;
    }
}
