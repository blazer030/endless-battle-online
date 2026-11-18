using System.Text.RegularExpressions;

namespace EndlessBattle.Domain;

// 後端所有原因代碼都在這裡：錯誤回應與正常回應的 reasons 共用；值是對外契約，前端依代碼決定文字。
// 全大寫底線，帶參數的代碼冒號後是參數（STAT_TOO_LOW:strength）
public static partial class ReasonCodes
{
    // 共通
    public const string RequestInvalid = "REQUEST_INVALID";
    public const string AuthenticationRequired = "AUTHENTICATION_REQUIRED";
    public const string CharacterNotFound = "CHARACTER_NOT_FOUND";
    public const string ItemNotFound = "ITEM_NOT_FOUND";
    public const string ConcurrentUpdate = "CONCURRENT_UPDATE";

    // 帳號
    public const string UsernameInvalid = "USERNAME_INVALID";
    public const string PasswordTooShort = "PASSWORD_TOO_SHORT";
    public const string UsernameTaken = "USERNAME_TAKEN";
    public const string AuthenticationFailed = "AUTHENTICATION_FAILED";
    public const string AccountNotFound = "ACCOUNT_NOT_FOUND";

    // 建角
    public const string CharacterNameLength = "CHARACTER_NAME_LENGTH";
    public const string CharacterNameTooComplex = "CHARACTER_NAME_TOO_COMPLEX";
    public const string CharacterNameTaken = "CHARACTER_NAME_TAKEN";
    public const string AccountHasCharacter = "ACCOUNT_HAS_CHARACTER";
    public const string JobUnknown = "JOB_UNKNOWN";
    public const string JobNotFirstTier = "JOB_NOT_FIRST_TIER";
    public const string StatBelowOne = "STAT_BELOW_ONE";
    public const string PointsNotSpent = "POINTS_NOT_SPENT";
    public const string SexInvalid = "SEX_INVALID";
    public const string ElementInvalid = "ELEMENT_INVALID";
    public const string BirthInvalid = "BIRTH_INVALID";

    // 背包、裝備、道具
    public const string BagFull = "BAG_FULL";
    public const string InvalidQuantity = "INVALID_QUANTITY";
    public const string AlreadyEquipped = "ALREADY_EQUIPPED";
    public const string SlotEmpty = "SLOT_EMPTY";
    public const string NotEquipment = "NOT_EQUIPMENT";
    public const string JobMismatch = "JOB_MISMATCH";
    public const string NotUsable = "NOT_USABLE";
    public const string NoEffect = "NO_EFFECT";
    public const string PetsFull = "PETS_FULL";
    public static string StatTooLow(string stat) => $"STAT_TOO_LOW:{stat}";

    // 商店、倉庫、銀行、旅館
    public const string NotEnoughMoney = "NOT_ENOUGH_MONEY";
    public const string NotForSale = "NOT_FOR_SALE";
    public const string InvalidLevel = "INVALID_LEVEL";
    public const string Equipped = "EQUIPPED";
    public const string StackLimit = "STACK_LIMIT";
    public const string DepotFull = "DEPOT_FULL";
    public const string InvalidAmount = "INVALID_AMOUNT";
    public const string NotEnoughBank = "NOT_ENOUGH_BANK";
    public const string LevelTooLow = "LEVEL_TOO_LOW";
    public const string UnknownRoom = "UNKNOWN_ROOM";

    // 鐵舖
    public const string AtRefineMax = "AT_REFINE_MAX";
    public const string NotRefineStone = "NOT_REFINE_STONE";
    public const string WrongStoneLevel = "WRONG_STONE_LEVEL";
    public const string CannotMerge = "CANNOT_MERGE";
    public const string MaxHoles = "MAX_HOLES";
    public const string HoleOutOfRange = "HOLE_OUT_OF_RANGE";
    public const string HoleOccupied = "HOLE_OCCUPIED";
    public const string HoleEmpty = "HOLE_EMPTY";
    public const string NotStone = "NOT_STONE";
    public const string DuplicateStone = "DUPLICATE_STONE";
    public const string RecipeUnknown = "RECIPE_UNKNOWN";
    public const string MissingMaterials = "MISSING_MATERIALS";
    public const string MissionIncomplete = "MISSION_INCOMPLETE";
    public static string MissingTool(string itemId) => $"MISSING_TOOL:{itemId}";

    // 技能與轉職
    public const string SameJob = "SAME_JOB";
    public const string ParentProficiencyTooLow = "PARENT_PROFICIENCY_TOO_LOW";
    public const string SkillUnknown = "SKILL_UNKNOWN";
    public const string WrongJob = "WRONG_JOB";
    public const string MaxLevel = "MAX_LEVEL";
    public const string NotEnoughProficiency = "NOT_ENOUGH_PROFICIENCY";
    public const string Prerequisite = "PREREQUISITE";
    public const string SlotOutOfRange = "SLOT_OUT_OF_RANGE";
    public const string DuplicateSlot = "DUPLICATE_SLOT";
    public const string NotLearned = "NOT_LEARNED";
    public const string DuplicateSkill = "DUPLICATE_SKILL";
    public const string ActiveInPassiveSlot = "ACTIVE_IN_PASSIVE_SLOT";
    public const string PassiveInActiveSlot = "PASSIVE_IN_ACTIVE_SLOT";

    // 地圖、設定
    public const string UnknownPlace = "UNKNOWN_PLACE";
    public const string Restricted = "RESTRICTED";
    public const string SamePlace = "SAME_PLACE";
    public const string SubPlaceNotAllowed = "SUB_PLACE_NOT_ALLOWED";
    public const string SubPlaceRequired = "SUB_PLACE_REQUIRED";
    public const string SubPlaceOutOfRange = "SUB_PLACE_OUT_OF_RANGE";
    public const string InvalidPercent = "INVALID_PERCENT";
    public const string InvalidAttack = "INVALID_ATTACK";

    // 同伴（寵物、傭兵）
    public const string Full = "FULL";
    public const string TooSoon = "TOO_SOON";
    public const string OverOwner = "OVER_OWNER";
    public const string NameInvalid = "NAME_INVALID";
    public const string PetUnknown = "PET_UNKNOWN";
    public const string FoodUnknown = "FOOD_UNKNOWN";
    public const string MercenaryHired = "MERCENARY_HIRED";
    public const string HonourOnly = "HONOUR_ONLY";
    public const string PlaceRestricted = "PLACE_RESTRICTED";
    public const string MercenaryUnknown = "MERCENARY_UNKNOWN";
    public const string NoMercenary = "NO_MERCENARY";
    public const string PlaceUnknown = "PLACE_UNKNOWN";

    // 勳章所
    public const string MedalsMissing = "MEDALS_MISSING";
    public const string HonourUnknown = "HONOUR_UNKNOWN";

    // 任務
    public const string MissionUnknown = "MISSION_UNKNOWN";
    public const string AlreadyActive = "ALREADY_ACTIVE";
    public const string AlreadyCompleted = "ALREADY_COMPLETED";
    public const string TooMany = "TOO_MANY";
    public const string SexMismatch = "SEX_MISMATCH";
    public const string BirthMismatch = "BIRTH_MISMATCH";
    public const string PrerequisiteMissing = "PREREQUISITE_MISSING";
    public const string MissionConflict = "MISSION_CONFLICT";
    public const string MissionNotActive = "MISSION_NOT_ACTIVE";
    public const string ItemsMissing = "ITEMS_MISSING";
    public const string ObjectiveIncomplete = "OBJECTIVE_INCOMPLETE";
    public const string PetMissing = "PET_MISSING";
    public const string SkillTooLow = "SKILL_TOO_LOW";

    [GeneratedRegex("^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*(:.+)?$")]
    private static partial Regex Spelling();

    public static bool IsWellFormed(string code) => Spelling().IsMatch(code);
}
