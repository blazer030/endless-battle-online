using EndlessBattle.Domain.Forge;
using EndlessBattle.Domain.Items;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Domain.Forge;

// 孔位從 1 起算；魔石種類 = 魔石的道具 id
public class SocketTests
{
    private static readonly ItemDefinition Sword = TestItems.Equipment("sword", holes: 2);
    private static readonly ItemDefinition Attack = FixedStone("attack-stone", StatBonus.Zero with { At = 5 });
    private static readonly ItemDefinition Guard = FixedStone("guard-stone", StatBonus.Zero with { Df = 3 });

    private static ItemDefinition FixedStone(string id, StatBonus bonus) =>
        TestItems.Item(id, ItemCategory.Stone) with { Stone = new StoneSpec(false, bonus, null, null) };

    private static InventoryEntry Equipment(int holes = 2, params SocketedStone[] sockets) =>
        TestItems.Entry(1, "sword") with { Holes = holes, Sockets = sockets };

    [Fact]
    public void Socket_RejectsDuplicateStoneKind()
    {
        var equipment = Equipment(2, new SocketedStone(1, "attack-stone", Attack.Stone!.Fixed));

        Assert.Equal(["duplicateStone"], Socket.CheckInsert(equipment, Sword, 2, Attack));
        Assert.Empty(Socket.CheckInsert(equipment, Sword, 2, Guard));
        Assert.Throws<InvalidOperationException>(() => Socket.Insert(equipment, 2, Attack, new ScriptedRandom(0.0)));
    }

    // 孔位必須在 1～裝備自己的孔數之間且是空的
    [Fact]
    public void Socket_RejectsInvalidHoleAndItems()
    {
        var equipment = Equipment(2, new SocketedStone(1, "guard-stone", Guard.Stone!.Fixed));

        Assert.Equal(["notEquipment"], Socket.CheckInsert(equipment, TestItems.Item("ore", ItemCategory.Material), 2, Attack));
        Assert.Equal(["notStone"], Socket.CheckInsert(equipment, Sword, 2, TestItems.Item("ore", ItemCategory.Material)));
        Assert.Equal(["holeOutOfRange"], Socket.CheckInsert(equipment, Sword, 0, Attack));
        Assert.Equal(["holeOutOfRange"], Socket.CheckInsert(equipment, Sword, 3, Attack));
        Assert.Equal(["holeOccupied"], Socket.CheckInsert(equipment, Sword, 1, Attack));
    }

    // 隨機魔石在鑲嵌當下依 Str、Smart、Life、Vit、Agi、Au、Be、At、Mat、Df、Mdf、HpMax 的順序各擲一次 rand(min, max)，
    // 結果寫進孔位後固定；前 7 項與 Df、Mdf 範圍是 [0, 0]
    [Fact]
    public void Socket_RandomStoneRollsOnce()
    {
        var chaos = TestItems.Item("chaos-stone", ItemCategory.Stone) with
        {
            Stone = new StoneSpec(
                true,
                StatBonus.Zero,
                StatBonus.Zero with { At = 1, Mat = 1, HpMax = 10 },
                StatBonus.Zero with { At = 10, Mat = 10, HpMax = 50 }),
        };
        var equipment = Equipment();
        double[] zeros = [0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0];
        var random = new ScriptedRandom(0.0, [.. zeros, 0.65, 0.25, 0.0, 0.0, 20.5 / 41]);

        var outcome = Socket.Insert(equipment, 2, chaos, random);

        var rolled = StatBonus.Zero with { At = 7, Mat = 3, HpMax = 30 };
        Assert.Equal(new SocketedStone(2, "chaos-stone", rolled), outcome.Socket);
        Assert.Equal([outcome.Socket], outcome.Entry.Sockets);
        Assert.Equal(12, random.Consumed);
        Assert.Empty(equipment.Sockets);
    }

    [Fact]
    public void Socket_FixedStoneConsumesNoRng()
    {
        var random = new ScriptedRandom(0.0);

        var outcome = Socket.Insert(Equipment(), 1, Attack, random);

        Assert.Equal(new SocketedStone(1, "attack-stone", StatBonus.Zero with { At = 5 }), outcome.Socket);
        Assert.Equal(0, random.Consumed);
    }

    // rand(1, 100) < 25 回收（24%），否則魔石消失；兩者都會清空該孔
    [Fact]
    public void Unsocket_RecoversAt24Percent()
    {
        var equipment = Equipment(2, new SocketedStone(1, "attack-stone", Attack.Stone!.Fixed), new SocketedStone(2, "guard-stone", Guard.Stone!.Fixed));

        var recovered = Socket.Remove(equipment, 1, new ScriptedRandom(0.0, 0.235));
        var lost = Socket.Remove(equipment, 1, new ScriptedRandom(0.0, 0.245));

        Assert.True(recovered.Recovered);
        Assert.Equal("attack-stone", recovered.StoneId);
        Assert.Equal(["guard-stone"], recovered.Entry.Sockets.Select(socket => socket.StoneId));
        Assert.False(lost.Recovered);
        Assert.Equal(["guard-stone"], lost.Entry.Sockets.Select(socket => socket.StoneId));
        Assert.Equal(2, equipment.Sockets.Count);
    }

    [Fact]
    public void Unsocket_RejectsEmptyOrOutOfRangeHole()
    {
        var equipment = Equipment(2, new SocketedStone(1, "attack-stone", Attack.Stone!.Fixed));

        Assert.Equal(["holeEmpty"], Socket.CheckRemove(equipment, Sword, 2));
        Assert.Equal(["holeOutOfRange"], Socket.CheckRemove(equipment, Sword, 3));
        Assert.Equal(["notEquipment"], Socket.CheckRemove(equipment, TestItems.Item("ore", ItemCategory.Material), 1));
        Assert.Empty(Socket.CheckRemove(equipment, Sword, 1));
    }

    // rand(1, 100) ≤ 8 成功，同一筆道具孔數 +1；失敗時裝備原封不動
    [Fact]
    public void Drill_SucceedsAt8Percent()
    {
        var equipment = Equipment(2, new SocketedStone(1, "attack-stone", Attack.Stone!.Fixed));

        var succeeded = Socket.Drill(equipment, Sword, new ScriptedRandom(0.0, 0.075));
        var failed = Socket.Drill(equipment, Sword, new ScriptedRandom(0.0, 0.085));

        Assert.True(succeeded.Success);
        Assert.Equal(equipment with { Holes = 3 }, succeeded.Entry);
        Assert.False(failed.Success);
        Assert.Same(equipment, failed.Entry);
    }

    [Fact]
    public void Drill_StopsAtFourHoles()
    {
        Assert.Equal(["maxHoles"], Socket.CheckDrill(Equipment(4), Sword));
        Assert.Empty(Socket.CheckDrill(Equipment(3), Sword));
        Assert.Equal(["notEquipment"], Socket.CheckDrill(Equipment(0), TestItems.Item("ore", ItemCategory.Material)));
    }
}
