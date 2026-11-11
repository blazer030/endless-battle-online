namespace EndlessBattle.Domain.Characters;

// WOG 七項能力：力量、智力、生命、體質、敏捷、魅力、信仰
public sealed record BaseStats(int Strength, int Intelligence, int Life, int Vitality, int Agility, int Charisma, int Faith);
