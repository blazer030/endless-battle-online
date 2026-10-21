using System.Globalization;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Application.Characters;

public sealed class CharacterService(
    ICharacterRepository characters,
    IJobProficiencyRepository proficiencies,
    IGameContent content,
    IClock clock)
{
    private const int MinimumNameLength = 2;
    private const int MaximumNameLength = 12;

    public async Task<Character> CreateAsync(long accountId, CreateCharacterCommand request, CancellationToken cancellationToken)
    {
        var trimmedName = request.Name.Trim();

        // 以「文字單位」計算長度，組合字元與 emoji 都算一個字
        var nameLength = new StringInfo(trimmedName).LengthInTextElements;
        if (nameLength < MinimumNameLength || nameLength > MaximumNameLength)
        {
            throw new InputValidationException($"角色名稱必須是 {MinimumNameLength}～{MaximumNameLength} 個字");
        }

        // 資料庫欄位 VARCHAR(12) 以 code point 計算；組合 emoji 一個字就可能佔好幾個
        if (trimmedName.EnumerateRunes().Count() > MaximumNameLength)
        {
            throw new InputValidationException("角色名稱含有過多組合字元，請改用較簡單的字");
        }

        if (content.AllJobs.All(candidate => candidate.Id != request.JobId))
        {
            throw new InputValidationException("不存在的職業");
        }

        var command = request with { Name = trimmedName };

        Character character;
        try
        {
            character = CharacterFactory.Create(
                accountId, command, content.GetJob(command.JobId), content.StartingPlaceId, content.StartingSubPlace, clock.UtcNow);
        }
        catch (CharacterCreationException exception)
        {
            throw new InputValidationException(exception.Message);
        }

        return await characters.AddAsync(character, cancellationToken);
    }

    public async Task<CharacterProfile> GetAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await characters.FindByAccountAsync(accountId, cancellationToken)
            ?? throw new NotFoundException("尚未建立角色");

        return new CharacterProfile(
            CharacterStamina.Regenerated(character, clock.UtcNow), await proficiencies.ListAsync(character.Id, cancellationToken));
    }
}
