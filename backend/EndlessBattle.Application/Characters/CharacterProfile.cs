using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Progression;

namespace EndlessBattle.Application.Characters;

// Equipment 用於顯示有效能力值
public sealed record CharacterProfile(Character Character, IReadOnlyList<JobProficiency> JobProficiency, EquipmentBonus Equipment);
