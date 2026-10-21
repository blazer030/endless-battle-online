using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Progression;

namespace EndlessBattle.Application.Characters;

public sealed record CharacterProfile(Character Character, IReadOnlyList<JobProficiency> JobProficiency);
