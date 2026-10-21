using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Stamina;

namespace EndlessBattle.Application.Characters;

public static class CharacterStamina
{
    // 只在回傳值反映行動力回復，不寫回；寫入留給結算
    public static Character Regenerated(Character character, DateTimeOffset now)
    {
        var stamina = StaminaRules.Regenerate(new StaminaState(character.ActNum, character.ActNumTime), now);
        return character with { ActNum = stamina.ActNum, ActNumTime = stamina.LastRegenAt };
    }
}
