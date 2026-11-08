# 計畫 1：後端地基與第一場歷練 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建好 .NET 10 後端與 MySQL／Flyway 管線。完成後，玩家可以註冊、登入、建立角色，並透過 API 推進歷練，取得一串戰鬥事件與收益。

**Architecture:** 後端分四層：Domain／Application／Infrastructure／Api，另有一個 Tests 專案。
- Domain 是純 C#，放戰鬥與歷練規則，不依賴任何套件。
- Application 放用例，並定義資料存取與時間等介面。
- Infrastructure 用 EF Core（Database-First，從 MySQL scaffold 產生）實作這些介面，並負責載入 JSON 遊戲內容。
- Api 是 Minimal API 加 JWT。
- 資料表結構只由 Flyway migration 決定。

**Tech Stack:**
- .NET 10、ASP.NET Core Minimal API
- EF Core 10＋`MySql.EntityFrameworkCore`（Oracle 官方 provider）
- MySQL 8.4、Flyway 11（Docker）
- xUnit v3、Testcontainers.MySql、`Microsoft.AspNetCore.Mvc.Testing`
- JWT Bearer、`Microsoft.AspNetCore.OpenApi`

**Spec:** `docs/superpowers/specs/2026-10-21-inkbound-design.md`

## Global Constraints

- 所有 .NET 專案 `net10.0`、`Nullable` 開啟、`TreatWarningsAsErrors=true`。`backend/global.json` 固定 SDK `10.0.100`，`rollForward: latestFeature`。
- `Inkbound.Domain` **沒有任何 PackageReference 或 ProjectReference**。
- `Inkbound.Application` 只引用 Domain，以及 `Microsoft.Extensions.DependencyInjection.Abstractions`。
- 遊戲規則只寫在 Domain；Api 不做任何遊戲計算。
- 資料表結構只由 `backend/db/ddl`、`backend/db/dml` 的 Flyway migration 決定：
  - 檔名符合 `^V\d{9}__[a-z0-9_]+\.sql$`；
  - ddl 和 dml 共用同一條版本號序列；
  - 已 commit 的 migration 不得修改；
  - 不得含 `DROP TABLE`、`DROP VIEW`、`DROP DATABASE`、`TRUNCATE`。
- 不使用 EF Core Migrations。`Inkbound.Infrastructure/Persistence/InkboundDbContext.cs` 與 `Persistence/Models/*.cs` 由 `scripts/scaffold-db.sh` 產生，**不得手改**，擴充寫在 `*.Partial.cs`。
- scaffold 產生的 model 只在 Infrastructure 使用。Application 與 Domain 只看見 Domain 型別。
- 資料表字元集 `utf8mb4`，排序規則 `utf8mb4_0900_ai_ci`，時間欄位 `DATETIME(6)`，存 UTC。
- 遊戲內容只放在 `backend/content/*.json`；資料庫只存玩家狀態。測試使用 `Inkbound.Tests/TestContent/`。
- 需要資料庫的測試依賴 Docker 執行中。
- Commit 訊息：英文 Conventional Commits，type 前加 emoji。**不加 Co-Authored-By 等產生標記**（使用者規則）。結構性與行為性變更分開 commit。
- 程式碼註解用中文，非必要不寫。
- 提交前檢查一律執行 `./scripts/check.sh`，以 exit code 判斷成敗。

## Roadmap（整份設計拆成七個計畫，本文件是計畫 1）

1. **後端地基與第一場歷練**（本計畫）
2. 前端地基與歷練主畫面：React＋Pixi 舞台、戰報、書法字退路、以輪詢播放事件
3. 境界成長、關卡與 Boss、自動推進、離線結算
4. 法寶、銘文、通貨、掉落演出
5. 功法與心法
6. 經脈圖
7. build 存檔、完整 docker compose、美術圖檔規格與提示詞範本

每個計畫在前一個完成後才寫，以實際程式碼為準。

## Review Focus

- **同時送出兩次推進**（連點、開兩個分頁）：只有一次生效，另一次回 409，收益不重複。→ Task 8 `UpdateAsync_RejectsStaleVersion`。
- **伺服器時間往回調**，使上次結算時間落在未來：回 200、沒有事件、收益為 0，不出錯。→ Task 8 `Advance_ReturnsNothing_WhenClockIsBehindLastSettlement`。
- **很久沒上線**（離線結算要到計畫 3 才做）：模擬上限 300 秒，回應很快，資料量有限；結算時間推到現在，馬上再推進一次會是空的。→ Task 8 `Advance_CapsSimulationAtFiveMinutes`。
- **中文角色名與前後空白**：「劍心」可以用；前後空白會被去掉；1 個字、13 個字、全空白都拒絕。→ Task 7 `Create_ValidatesName`。
- **戰鬥永遠打不完**（雙方都很耐打）：60 秒判定和局並計入時間，不會無限迴圈。→ Task 3 `Simulate_ReturnsStalemate_WhenTimeLimitReached`、Task 4 `Run_CountsStalemateAsConsumedTime`。

---

## File Structure

```
Inkbound/
├── CLAUDE.md                                  專案規則與指令
├── docker-compose.yml                         開發用 MySQL＋Flyway
├── scripts/
│   ├── check.sh                               check-sql＋build＋test
│   ├── check-sql.sh                           擋危險 SQL
│   └── scaffold-db.sh                         固定參數呼叫 dotnet ef dbcontext scaffold
└── backend/
    ├── global.json
    ├── Directory.Build.props
    ├── .config/dotnet-tools.json              固定 dotnet-ef 版本
    ├── Inkbound.slnx
    ├── db/ddl/V000000001__create_accounts_table.sql
    ├── db/ddl/V000000002__create_characters_table.sql
    ├── content/monsters.json, stages.json
    ├── Inkbound.Domain/
    │   ├── Randomness/IRandomSource.cs, SeededRandom.cs
    │   ├── Combat/CombatStats.cs, CombatSide.cs, BattleEvents.cs, DamageCalculator.cs, BattleSimulator.cs, BattleResult.cs
    │   ├── Adventure/AdventureSimulator.cs, AdventureRun.cs
    │   ├── Content/MonsterDefinition.cs, StageDefinition.cs
    │   └── Characters/Character.cs, PlayerStats.cs
    ├── Inkbound.Application/
    │   ├── Common/ApplicationExceptions.cs, IClock.cs, IRandomSourceFactory.cs
    │   ├── Content/IGameContent.cs
    │   ├── Accounts/AccountService.cs, IAccountRepository.cs, IPasswordHashing.cs, AccountCredentials.cs
    │   ├── Characters/CharacterService.cs, ICharacterRepository.cs
    │   ├── Adventure/AdventureService.cs, AdvanceResult.cs
    │   └── DependencyInjection.cs
    ├── Inkbound.Infrastructure/
    │   ├── Persistence/InkboundDbContext.cs, Models/*.cs   ← 產生的檔案
    │   ├── Persistence/InkboundDbContext.Partial.cs
    │   ├── Persistence/AccountRepository.cs, CharacterRepository.cs
    │   ├── Security/IdentityPasswordHashing.cs
    │   ├── Content/JsonGameContent.cs, GameContentException.cs
    │   ├── Time/SystemClock.cs, Randomness/SystemRandomSourceFactory.cs
    │   └── DependencyInjection.cs
    ├── Inkbound.Api/
    │   ├── Program.cs, appsettings.json, appsettings.Development.json
    │   ├── Auth/JwtOptions.cs, JwtTokenIssuer.cs, ClaimsPrincipalExtensions.cs
    │   ├── Errors/ApplicationExceptionHandler.cs
    │   └── Endpoints/Contracts.cs, AccountEndpoints.cs, CharacterEndpoints.cs, AdventureEndpoints.cs
    └── Inkbound.Tests/
        ├── Support/ScriptedRandom.cs, TestStats.cs, BackendPaths.cs, MigrationRunner.cs,
        │   MySqlDatabaseFixture.cs, FakeClock.cs, FixedSeedRandomFactory.cs,
        │   InkboundApiFactory.cs, ApiClientExtensions.cs
        ├── TestContent/monsters.json, stages.json
        ├── Domain/…Tests.cs  Infrastructure/…Tests.cs  Api/…Tests.cs
```

---

### Task 1: 後端方案骨架與可重現的亂數

**Files:**
- Create: `CLAUDE.md`, `.gitignore`, `scripts/check.sh`
- Create: `backend/global.json`, `backend/Directory.Build.props`, `backend/.config/dotnet-tools.json`, `backend/Inkbound.slnx`
- Create: 五個專案的 `.csproj`
- Create: `backend/Inkbound.Domain/Randomness/IRandomSource.cs`, `SeededRandom.cs`
- Test: `backend/Inkbound.Tests/Domain/SeededRandomTests.cs`

**Interfaces:**
- Produces:
  - `interface IRandomSource { double NextDouble(); int NextInt(int maxExclusive); }`
  - `sealed class SeededRandom(int seed) : IRandomSource`，內部用 `System.Random(seed)`。

- [ ] **Step 0: 前置條件** — 執行 `dotnet --list-sdks`，必須看到 `10.0.x`。沒有的話，請使用者從 https://dotnet.microsoft.com/download/dotnet/10.0 安裝 .NET 10 SDK（需要系統權限，不要自己用 sudo 安裝）。

- [ ] **Step 1: 建立方案與專案**

在 `backend/` 執行：

```bash
dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature
dotnet new sln -n Inkbound            # .NET 10 產生 Inkbound.slnx
dotnet new classlib -n Inkbound.Domain
dotnet new classlib -n Inkbound.Application
dotnet new classlib -n Inkbound.Infrastructure
dotnet new web -n Inkbound.Api
dotnet new classlib -n Inkbound.Tests
dotnet sln add Inkbound.*/Inkbound.*.csproj
dotnet add Inkbound.Application reference Inkbound.Domain
dotnet add Inkbound.Infrastructure reference Inkbound.Application Inkbound.Domain
dotnet add Inkbound.Api reference Inkbound.Application Inkbound.Infrastructure
dotnet add Inkbound.Tests reference Inkbound.Domain Inkbound.Application Inkbound.Infrastructure Inkbound.Api
dotnet add Inkbound.Tests package xunit.v3
dotnet add Inkbound.Tests package xunit.runner.visualstudio
dotnet add Inkbound.Tests package Microsoft.NET.Test.Sdk
dotnet new tool-manifest && dotnet tool install dotnet-ef --version "10.*"
```

- 刪除範本產生的 `Class1.cs`。
- `Inkbound.Tests.csproj` 加 `<OutputType>Exe</OutputType>`（xUnit v3 要求）。
- `Directory.Build.props` 設定 `TargetFramework=net10.0`、`Nullable=enable`、`ImplicitUsings=enable`、`TreatWarningsAsErrors=true`，並移除各 csproj 內重複的設定。
- `.gitignore` 用 `dotnet new gitignore` 產生後放在 repo 根目錄，另外加上 `.superpowers/`。

- [ ] **Step 2: 建立 `scripts/check.sh` 與 `CLAUDE.md`**

`scripts/check.sh` 先只做 `cd backend && dotnet build -warnaserror && dotnet test`，加上 `set -euo pipefail`；Task 2 再補 SQL 檢查。

`CLAUDE.md` 用中文寫，內容包括：
- 專案簡介；
- 本計畫的 Global Constraints；
- 指令：`./scripts/check.sh`、`docker compose up -d`、`./scripts/scaffold-db.sh`；
- 資料庫變更流程：先寫 migration，再 scaffold，再寫程式與測試。

- [ ] **Step 3: 執行 `./scripts/check.sh`**，預期 build 成功、零警告（目前沒有測試）。

- [ ] **Step 4: Commit**（結構性）

```bash
git add -A && git commit -m "🚧 chore: scaffold .NET 10 backend solution"
```

- [ ] **Step 5: Write the failing test** `backend/Inkbound.Tests/Domain/SeededRandomTests.cs`

```csharp
public class SeededRandomTests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var first = new SeededRandom(42);
        var second = new SeededRandom(42);
        var firstValues = Enumerable.Range(0, 5).Select(_ => first.NextDouble()).ToArray();
        var secondValues = Enumerable.Range(0, 5).Select(_ => second.NextDouble()).ToArray();
        Assert.Equal(firstValues, secondValues);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var first = new SeededRandom(1);
        var second = new SeededRandom(2);
        var firstValues = Enumerable.Range(0, 5).Select(_ => first.NextDouble()).ToArray();
        var secondValues = Enumerable.Range(0, 5).Select(_ => second.NextDouble()).ToArray();
        Assert.NotEqual(firstValues, secondValues);
    }

    [Fact]
    public void NextInt_StaysWithinRange_AndCoversAllValues()
    {
        var random = new SeededRandom(7);
        var values = Enumerable.Range(0, 1000).Select(_ => random.NextInt(3)).ToArray();
        Assert.All(values, value => Assert.InRange(value, 0, 2));
        Assert.Equal(new[] { 0, 1, 2 }, values.Distinct().Order().ToArray());
    }
}
```

- [ ] **Step 6: Run** `cd backend && dotnet test`，預期編譯失敗：`SeededRandom` 不存在。
- [ ] **Step 7: 實作** `IRandomSource`、`SeededRandom`，放在 `Inkbound.Domain.Randomness`。
- [ ] **Step 8: Run** `./scripts/check.sh`，預期 3 個測試通過。
- [ ] **Step 9: Commit**

```bash
git add -A && git commit -m "✨ feat: add seeded random source for reproducible simulations"
```

---

### Task 2: MySQL＋Flyway 管線、scaffold 與資料庫測試基礎

**Files:**
- Create: `docker-compose.yml`, `scripts/check-sql.sh`, `scripts/scaffold-db.sh`
- Create: `backend/db/ddl/V000000001__create_accounts_table.sql`, `V000000002__create_characters_table.sql`
- Generate: `backend/Inkbound.Infrastructure/Persistence/InkboundDbContext.cs`, `Persistence/Models/Account.cs`, `Persistence/Models/Character.cs`
- Create: `backend/Inkbound.Infrastructure/Persistence/InkboundDbContext.Partial.cs`
- Create: `backend/Inkbound.Api/appsettings.Development.json`
- Create: `backend/Inkbound.Tests/Support/BackendPaths.cs`, `MigrationRunner.cs`, `MySqlDatabaseFixture.cs`
- Modify: `scripts/check.sh`
- Test: `backend/Inkbound.Tests/Infrastructure/DatabaseSchemaTests.cs`

**Interfaces:**
- Produces:
  - `InkboundDbContext`，含 `DbSet<Account> Accounts`、`DbSet<Character> Characters`，命名空間 `Inkbound.Infrastructure.Persistence(.Models)`。
  - `Character.Version` 是 concurrency token。
  - `MySqlDatabaseFixture`（assembly fixture）：`string ConnectionString`、`InkboundDbContext CreateDbContext()`。
  - `BackendPaths.BackendDirectory`：往上找到含 `Inkbound.slnx` 的資料夾。
  - `MigrationRunner.ApplyAllAsync(string connectionString, string databaseDirectory)`。

- [ ] **Step 1: 寫 migration**

```sql
-- V000000001__create_accounts_table.sql
CREATE TABLE accounts (
    id            BIGINT       NOT NULL AUTO_INCREMENT,
    username      VARCHAR(32)  NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at    DATETIME(6)  NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_accounts_username (username)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

-- V000000002__create_characters_table.sql
CREATE TABLE characters (
    id               BIGINT      NOT NULL AUTO_INCREMENT,
    account_id       BIGINT      NOT NULL,
    name             VARCHAR(12) NOT NULL,
    experience       BIGINT      NOT NULL DEFAULT 0,
    spirit_stones    BIGINT      NOT NULL DEFAULT 0,
    current_stage_id VARCHAR(64) NOT NULL,
    last_settled_at  DATETIME(6) NOT NULL,
    version          INT         NOT NULL DEFAULT 0,
    created_at       DATETIME(6) NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uk_characters_account_id (account_id),
    UNIQUE KEY uk_characters_name (name),
    CONSTRAINT fk_characters_account FOREIGN KEY (account_id) REFERENCES accounts (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;
```

- [ ] **Step 2: `docker-compose.yml`**（repo 根目錄）

需要兩個服務：
- `mysql`：
  - 映像 `mysql:8.4`；
  - 環境變數 `MYSQL_DATABASE=inkbound`、`MYSQL_USER=inkbound`、`MYSQL_PASSWORD=inkbound_dev`、`MYSQL_ROOT_PASSWORD=root_dev`；
  - port `3306:3306`；
  - named volume `mysql-data`；
  - healthcheck 用 `mysqladmin ping`。
- `flyway`：
  - 映像 `flyway/flyway:11`；
  - 唯讀掛載 `backend/db/ddl` 到 `/flyway/sql/ddl`、`backend/db/dml` 到 `/flyway/sql/dml`；
  - `depends_on` 設 mysql 的 `service_healthy`；
  - command：

```
-url=jdbc:mysql://mysql:3306/inkbound?allowPublicKeyRetrieval=true&useSSL=false
-user=inkbound -password=inkbound_dev
-locations=filesystem:/flyway/sql/ddl,filesystem:/flyway/sql/dml
-validateMigrationNaming=true migrate
```

`backend/db/dml/` 目前是空的，放一個 `.gitkeep`。

- [ ] **Step 3: 驗證** `docker compose up -d && docker compose logs flyway`，預期看到 `Successfully applied 2 migrations`。

- [ ] **Step 4: `scripts/check-sql.sh`**

接受一個可選的目錄參數，預設 `backend/db`。遞迴掃描 `*.sql`：
- 用 grep -iE 比對 `drop (table|view|database)|truncate`，命中就印出檔名並 `exit 1`；
- 檔名不符 `^V[0-9]{9}__[a-z0-9_]+\.sql$` 也 `exit 1`；
- 全部通過就印 `SQL check passed`。

`scripts/check.sh` 在 build 之前先執行它。

驗證方式：
- 在暫存資料夾放一個含 `DROP TABLE x;` 的 `V000000099__bad.sql`，執行 `./scripts/check-sql.sh <暫存資料夾>`，預期 exit 1；
- 不帶參數執行，預期 exit 0。

- [ ] **Step 5: `scripts/scaffold-db.sh` 與 Infrastructure／Api 套件**

```bash
dotnet add backend/Inkbound.Infrastructure package MySql.EntityFrameworkCore --version "10.*"
dotnet add backend/Inkbound.Api package Microsoft.EntityFrameworkCore.Design --version "10.*"
```

`appsettings.Development.json` 加上：

```json
{ "ConnectionStrings": { "Inkbound": "Server=localhost;Port=3306;Database=inkbound;User ID=inkbound;Password=inkbound_dev;" } }
```

腳本內容：
- 先用 `docker compose exec -T mysql mysql -uinkbound -pinkbound_dev -N -e "SELECT table_name FROM information_schema.tables WHERE table_schema='inkbound' AND table_name <> 'flyway_schema_history'"` 列出資料表，每張表轉成一個 `--table` 參數；
- 再在 `backend/` 執行：

```bash
dotnet ef dbcontext scaffold "Name=ConnectionStrings:Inkbound" MySql.EntityFrameworkCore \
  --project Inkbound.Infrastructure --startup-project Inkbound.Api \
  --output-dir Persistence/Models --context-dir Persistence --context InkboundDbContext \
  --namespace Inkbound.Infrastructure.Persistence.Models --context-namespace Inkbound.Infrastructure.Persistence \
  --no-onconfiguring --force $TABLE_ARGUMENTS
```

- [ ] **Step 6: 執行 `./scripts/scaffold-db.sh`**，預期產生：
- `InkboundDbContext.cs`
- `Models/Account.cs`，屬性 `Id, Username, PasswordHash, CreatedAt`
- `Models/Character.cs`，屬性 `Id, AccountId, Name, Experience, SpiritStones, CurrentStageId, LastSettledAt, Version, CreatedAt, Account`

如果 provider 沒有把 snake_case 轉成 PascalCase，在腳本加上能達成的參數，**不要手改產生的檔案**。

- [ ] **Step 7: Commit**（結構性）

```bash
git add -A && git commit -m "🚧 chore: add MySQL and Flyway pipeline with scaffolded models"
```

- [ ] **Step 8: 測試基礎設施**

```bash
dotnet add backend/Inkbound.Tests package Testcontainers.MySql
```

- `MigrationRunner.ApplyAllAsync`：
  - 收集 `db/ddl` 與 `db/dml` 的 `.sql`，用 `^V(\d{9})__` 解析版本號；
  - 名稱不合法或版本號重複就丟 `InvalidOperationException`；
  - 依版本號排序，用 `MySql.Data.MySqlClient.MySqlScript` 逐檔執行。
- `MySqlDatabaseFixture : IAsyncLifetime`：
  - 啟動 `mysql:8.4` 的 MySqlContainer，資料庫名 `inkbound`；用 Testcontainers 目前不會觸發 obsolete 警告的 builder 寫法；
  - 跑 `MigrationRunner`；
  - 在 Tests 專案加上 `[assembly: AssemblyFixture(typeof(MySqlDatabaseFixture))]`；
  - `CreateDbContext()` 用 `UseMySQL(ConnectionString)` 建立。

- [ ] **Step 9: Write the failing test** `DatabaseSchemaTests.cs`

```csharp
public class DatabaseSchemaTests(MySqlDatabaseFixture database)
{
    [Fact]
    public async Task Migrations_CreateAccountsAndCharacters()
    {
        await using var context = database.CreateDbContext();
        var account = new Account { Username = $"schema_{Guid.NewGuid():N}"[..32], PasswordHash = "hash", CreatedAt = DateTime.UtcNow };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        context.Characters.Add(new Character { AccountId = account.Id, Name = $"角{account.Id}", CurrentStageId = "stage", LastSettledAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        await using var reader = database.CreateDbContext();
        var stored = await reader.Characters.SingleAsync(character => character.AccountId == account.Id);
        Assert.Equal(0, stored.Experience);
        Assert.Equal(0, stored.Version);
    }

    [Fact]
    public async Task Accounts_RejectDuplicateUsername_CaseInsensitively()
    {
        var username = $"dup_{Guid.NewGuid():N}"[..20];
        await using var context = database.CreateDbContext();
        context.Accounts.Add(new Account { Username = username, PasswordHash = "hash", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();
        await using var second = database.CreateDbContext();
        second.Accounts.Add(new Account { Username = username.ToUpperInvariant(), PasswordHash = "hash", CreatedAt = DateTime.UtcNow });
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Characters_RejectStaleVersion()
    {
        // 建立一個帳號與角色（同第一個測試的方式），取得 characterId
        // contextA、contextB 各自載入同一個角色
        // contextA：SpiritStones = 10，Version += 1，SaveChanges 成功
        // contextB：SpiritStones = 20，Version += 1
        // Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contextB.SaveChangesAsync())
    }
}
```

- [ ] **Step 10: Run** `cd backend && dotnet test --filter DatabaseSchemaTests`，預期前兩個通過（它們只驗證 migration），`Characters_RejectStaleVersion` 失敗，因為還沒設定 concurrency token。
- [ ] **Step 11: 實作** `InkboundDbContext.Partial.cs`：用產生檔提供的 `partial void OnModelCreatingPartial(ModelBuilder modelBuilder)`，設定 `Character.Version` 為 `IsConcurrencyToken()`。
- [ ] **Step 12: Run** `./scripts/check.sh`，預期全部通過。
- [ ] **Step 13: Commit**

```bash
git add -A && git commit -m "✨ feat: guard character updates with optimistic concurrency"
```

---

### Task 3: 一對一戰鬥模擬

**Files:**
- Create: `backend/Inkbound.Domain/Combat/CombatStats.cs`, `CombatSide.cs`, `BattleEvents.cs`, `DamageCalculator.cs`, `BattleSimulator.cs`, `BattleResult.cs`
- Create: `backend/Inkbound.Tests/Support/ScriptedRandom.cs`, `TestStats.cs`
- Test: `backend/Inkbound.Tests/Domain/DamageCalculatorTests.cs`, `BattleSimulatorTests.cs`

**Interfaces:**
- Consumes: `IRandomSource`（Task 1）
- Produces:

```csharp
public sealed record CombatStats(int MaxHealth, int Attack, int Defense, double CritChance, double CritMultiplier, double AttacksPerSecond);
public enum CombatSide { Player, Monster }

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(EncounterEvent), "encounter")]
[JsonDerivedType(typeof(AttackEvent), "attack")]
[JsonDerivedType(typeof(DefeatEvent), "defeat")]
[JsonDerivedType(typeof(RewardEvent), "reward")]
public abstract record BattleEvent(double AtSeconds);
public sealed record EncounterEvent(double AtSeconds, string MonsterId) : BattleEvent(AtSeconds);
public sealed record AttackEvent(double AtSeconds, CombatSide Attacker, int Damage, bool IsCritical, int TargetHealthAfter) : BattleEvent(AtSeconds);
public sealed record DefeatEvent(double AtSeconds, CombatSide Loser) : BattleEvent(AtSeconds);
public sealed record RewardEvent(double AtSeconds, long Experience, long SpiritStones) : BattleEvent(AtSeconds);

public sealed record BattleResult(IReadOnlyList<BattleEvent> Events, CombatSide? Winner, double DurationSeconds);
public static class DamageCalculator { public static (int Damage, bool IsCritical) Calculate(CombatStats attacker, CombatStats defender, IRandomSource random); }
public static class BattleSimulator { public static BattleResult Simulate(CombatStats player, CombatStats monster, IRandomSource random, double timeLimitSeconds); }
```

測試輔助：
- `ScriptedRandom(double fallback, params double[] scripted) : IRandomSource`：依序回傳 scripted，用完後一律回傳 fallback；`NextInt(max)` 回傳 `(int)(NextDouble() * max)`；`int Consumed` 記錄已取用的次數。
- `TestStats.Make(int maxHealth = 100, int attack = 10, int defense = 0, double critChance = 0, double critMultiplier = 1.5, double attacksPerSecond = 1)`。

- [ ] **Step 1: Write the failing tests** `DamageCalculatorTests.cs`

```csharp
[Theory]
[InlineData(100, 0, 100)]
[InlineData(100, 100, 50)]
[InlineData(12, 2, 10)]
[InlineData(1, 1000, 1)]
public void NonCriticalDamage_FollowsArmorFormula(int attack, int defense, int expected)
{
    var (damage, isCritical) = DamageCalculator.Calculate(TestStats.Make(attack: attack), TestStats.Make(defense: defense), new ScriptedRandom(0.5));
    Assert.Equal(expected, damage);
    Assert.False(isCritical);
}

[Fact]
public void CriticalHit_MultipliesDamage()
{
    var attacker = TestStats.Make(attack: 100, critChance: 0.2, critMultiplier: 1.5);
    var (damage, isCritical) = DamageCalculator.Calculate(attacker, TestStats.Make(defense: 100), new ScriptedRandom(0.99, 0.1));
    Assert.Equal(75, damage);
    Assert.True(isCritical);
}

[Fact]
public void CritRoll_IsAlwaysConsumed()
{
    var random = new ScriptedRandom(0.99, 0.0);
    DamageCalculator.Calculate(TestStats.Make(critChance: 0), TestStats.Make(), random);
    Assert.Equal(1, random.Consumed);
}
```

- [ ] **Step 2: Run** `dotnet test --filter DamageCalculatorTests`，預期編譯失敗。
- [ ] **Step 3: 實作** `DamageCalculator.Calculate`，公式：
  - `raw = attack² / (attack + defense)`，以 double 計算；
  - 每次都先取一次 `random.NextDouble()`，小於 `CritChance` 就是會心，`raw *= CritMultiplier`；
  - `damage = Math.Max(1, (int)Math.Round(raw, MidpointRounding.AwayFromZero))`。
- [ ] **Step 4: Run**，預期通過。

- [ ] **Step 5: Write the failing tests** `BattleSimulatorTests.cs`

```csharp
[Fact]
public void Simulate_EndsWithDefeat_AndPlayerActsFirstOnTies()
{
    var player = TestStats.Make(maxHealth: 100, attack: 10);
    var monster = TestStats.Make(maxHealth: 20, attack: 1);
    var result = BattleSimulator.Simulate(player, monster, new ScriptedRandom(0.99), timeLimitSeconds: 60);
    Assert.Equal(new BattleEvent[]
    {
        new AttackEvent(1, CombatSide.Player, 10, false, 10),
        new AttackEvent(1, CombatSide.Monster, 1, false, 99),
        new AttackEvent(2, CombatSide.Player, 10, false, 0),
        new DefeatEvent(2, CombatSide.Monster),
    }, result.Events);
    Assert.Equal(CombatSide.Player, result.Winner);
    Assert.Equal(2, result.DurationSeconds);
}

[Fact]
public void Simulate_FasterSideAttacksMoreOften()
{
    var player = TestStats.Make(maxHealth: 1000, attack: 1, attacksPerSecond: 2);
    var monster = TestStats.Make(maxHealth: 1000, attack: 1, attacksPerSecond: 1);
    var result = BattleSimulator.Simulate(player, monster, new ScriptedRandom(0.99), timeLimitSeconds: 4);
    var attacks = result.Events.OfType<AttackEvent>().ToList();
    Assert.Equal(8, attacks.Count(attack => attack.Attacker == CombatSide.Player));
    Assert.Equal(4, attacks.Count(attack => attack.Attacker == CombatSide.Monster));
}

[Fact]
public void Simulate_PlayerCanLose()
{
    var result = BattleSimulator.Simulate(TestStats.Make(maxHealth: 5, attack: 1), TestStats.Make(maxHealth: 1000, attack: 50), new ScriptedRandom(0.99), 60);
    Assert.Equal(CombatSide.Monster, result.Winner);
    Assert.Equal(new DefeatEvent(1, CombatSide.Player), result.Events[^1]);
}

[Fact]
public void Simulate_ReturnsStalemate_WhenTimeLimitReached()
{
    var tank = TestStats.Make(maxHealth: 1_000_000, attack: 1);
    var result = BattleSimulator.Simulate(tank, tank, new ScriptedRandom(0.99), timeLimitSeconds: 5);
    Assert.Null(result.Winner);
    Assert.Equal(5, result.DurationSeconds);
    Assert.DoesNotContain(result.Events, battleEvent => battleEvent is DefeatEvent);
    Assert.All(result.Events, battleEvent => Assert.True(battleEvent.AtSeconds <= 5));
}
```

- [ ] **Step 6: Run**，預期失敗。
- [ ] **Step 7: 實作** `BattleSimulator.Simulate`。

這是事件驅動的迴圈：
- 雙方的下次出手時間各從 `1 / AttacksPerSecond` 開始；
- 每回合取較早出手的一方，同時間時玩家先；
- 出手時間超過 `timeLimitSeconds` 就以和局結束，`Winner = null`，`DurationSeconds = timeLimitSeconds`；
- 否則計算傷害，扣血但不低於 0，產生 `AttackEvent`；
- 對方血量歸 0 時，產生 `DefeatEvent` 並結束；
- 出手方的下次時間加上 `1 / AttacksPerSecond`。

- [ ] **Step 8: Run** `./scripts/check.sh`，預期全部通過。
- [ ] **Step 9: Commit**

```bash
git add -A && git commit -m "✨ feat: simulate one-on-one battles as timed events"
```

---

### Task 4: 連續歷練模擬

**Files:**
- Create: `backend/Inkbound.Domain/Content/MonsterDefinition.cs`, `StageDefinition.cs`, `backend/Inkbound.Domain/Adventure/AdventureSimulator.cs`, `AdventureRun.cs`
- Test: `backend/Inkbound.Tests/Domain/AdventureSimulatorTests.cs`

**Interfaces:**
- Consumes: `BattleSimulator`, `BattleEvent` 系列, `CombatStats`, `IRandomSource`
- Produces:

```csharp
public sealed record MonsterDefinition(string Id, string Name, CombatStats Stats, long ExperienceReward, long SpiritStoneReward);
public sealed record StageDefinition(string Id, string Name, IReadOnlyList<string> MonsterIds);
public sealed record AdventureRun(IReadOnlyList<BattleEvent> Events, double ConsumedSeconds, long ExperienceGained, long SpiritStonesGained);
public static class AdventureSimulator
{
    public const double BattleTimeLimitSeconds = 60;
    public const double RestSecondsBetweenFights = 1;
    public static AdventureRun Run(CombatStats player, IReadOnlyList<MonsterDefinition> stageMonsters, double availableSeconds, IRandomSource random);
}
```

- [ ] **Step 1: Write the failing tests**

```csharp
private static MonsterDefinition Monster(string id, int maxHealth, int attack) =>
    new(id, id, TestStats.Make(maxHealth: maxHealth, attack: attack), ExperienceReward: 5, SpiritStoneReward: 2);

[Fact]
public void Run_ChainsFightsWithRestAndRewards()
{
    var run = AdventureSimulator.Run(TestStats.Make(attack: 10), [Monster("sprite", 20, 1)], availableSeconds: 10, new ScriptedRandom(0.99));
    Assert.Equal(new double[] { 0, 3, 6 }, run.Events.OfType<EncounterEvent>().Select(encounter => encounter.AtSeconds));
    Assert.Equal(new double[] { 2, 5, 8 }, run.Events.OfType<RewardEvent>().Select(reward => reward.AtSeconds));
    Assert.Equal(9, run.ConsumedSeconds);
    Assert.Equal(15, run.ExperienceGained);
    Assert.Equal(6, run.SpiritStonesGained);
}

[Fact]
public void Run_GivesNoReward_WhenPlayerLoses_AndKeepsFighting()
{
    var run = AdventureSimulator.Run(TestStats.Make(maxHealth: 5, attack: 1), [Monster("boss", 1000, 50)], availableSeconds: 30, new ScriptedRandom(0.99));
    Assert.Empty(run.Events.OfType<RewardEvent>());
    Assert.True(run.Events.OfType<EncounterEvent>().Count() >= 2);
    Assert.Equal(0, run.ExperienceGained);
}

[Fact]
public void Run_CountsStalemateAsConsumedTime()
{
    var tank = TestStats.Make(maxHealth: 1_000_000, attack: 1);
    var run = AdventureSimulator.Run(tank, [new MonsterDefinition("rock", "rock", tank, 5, 2)], availableSeconds: 130, new ScriptedRandom(0.99));
    Assert.Equal(new double[] { 0, 61 }, run.Events.OfType<EncounterEvent>().Select(encounter => encounter.AtSeconds));
    Assert.Equal(122, run.ConsumedSeconds);
}

[Fact]
public void Run_ConsumesNothing_WhenNoTimeAvailable()
{
    var run = AdventureSimulator.Run(TestStats.Make(), [Monster("sprite", 20, 1)], availableSeconds: 0, new ScriptedRandom(0.99));
    Assert.Empty(run.Events);
    Assert.Equal(0, run.ConsumedSeconds);
}

[Fact]
public void Run_PicksMonsterFromStageList()
{
    var run = AdventureSimulator.Run(TestStats.Make(attack: 10), [Monster("first", 20, 1), Monster("second", 20, 1)], availableSeconds: 3, new ScriptedRandom(0.99, 0.7));
    Assert.Equal("second", run.Events.OfType<EncounterEvent>().First().MonsterId);
}
```

- [ ] **Step 2: Run**，預期編譯失敗。
- [ ] **Step 3: 實作** `AdventureSimulator.Run`

```
cursor = 0
loop:
  monster = stageMonsters[random.NextInt(count)]    // 每場先抽妖怪，再進入戰鬥
  remaining = availableSeconds - cursor
  limit = min(BattleTimeLimitSeconds, remaining)
  battle = BattleSimulator.Simulate(player, monster.Stats, random, limit)
  if battle.Winner is null && limit < BattleTimeLimitSeconds: break   // 時間不夠打完，捨棄這場
  加入 EncounterEvent(cursor, monster.Id)，以及把每個事件的時間平移 cursor（e with { AtSeconds = e.AtSeconds + cursor }）
  if battle.Winner == Player: 加入 RewardEvent(cursor + battle.DurationSeconds, ...)，並累加收益
  cursor += battle.DurationSeconds + RestSecondsBetweenFights
ConsumedSeconds = min(cursor, availableSeconds)
```

`remaining <= 0` 時 `Simulate` 會立刻回傳和局，觸發 break。

- [ ] **Step 4: Run** `./scripts/check.sh`，預期通過。
- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "✨ feat: chain battles into a continuous adventure run"
```

---

### Task 5: JSON 遊戲內容載入

**Files:**
- Create: `backend/Inkbound.Application/Content/IGameContent.cs`
- Create: `backend/Inkbound.Infrastructure/Content/JsonGameContent.cs`, `GameContentException.cs`
- Create: `backend/content/monsters.json`, `backend/content/stages.json`
- Create: `backend/Inkbound.Tests/TestContent/monsters.json`, `stages.json`（csproj 設定複製到輸出目錄）
- Test: `backend/Inkbound.Tests/Infrastructure/JsonGameContentTests.cs`

**Interfaces:**
- Produces:

```csharp
public interface IGameContent
{
    string StartingStageId { get; }
    StageDefinition GetStage(string stageId);        // 找不到就丟 GameContentException
    MonsterDefinition GetMonster(string monsterId);  // 同上
}
public sealed class JsonGameContent : IGameContent { public static JsonGameContent Load(string directory); }
public sealed class GameContentException(string message) : Exception(message);
```

JSON 格式（`JsonSerializerDefaults.Web`，camelCase）：

```json
// monsters.json
{ "monsters": [ { "id": "snow-sprite", "name": "雪精",
    "stats": { "maxHealth": 40, "attack": 6, "defense": 2, "critChance": 0, "critMultiplier": 1.5, "attacksPerSecond": 0.8 },
    "experienceReward": 5, "spiritStoneReward": 2 } ] }
// stages.json
{ "startingStageId": "frost-peak-1",
  "stages": [ { "id": "frost-peak-1", "name": "冷峰寒地・一", "monsterIds": ["snow-sprite", "frost-wolf"] } ] }
```

正式內容 `backend/content/` 放：
- `snow-sprite`（如上）；
- `frost-wolf`：寒狼，maxHealth 60、attack 8、defense 3、critChance 0.05、critMultiplier 1.5、attacksPerSecond 1.0，經驗 8、靈石 3。

測試內容 `TestContent/` 放：
- `test-sprite`、`test-wolf`，數值分別和上面兩隻相同；
- 起始關卡 `test-stage-1`。

- [ ] **Step 1: Write the failing tests**
  - `Load_ShippedContent_IsValid`：`JsonGameContent.Load(Path.Combine(BackendPaths.BackendDirectory, "content"))` 不丟例外，且起始關卡的每個 `MonsterIds` 都能用 `GetMonster` 取得。
  - `Load_TestContent_ExposesStagesAndMonsters`：`GetStage("test-stage-1").MonsterIds` 等於 `["test-sprite", "test-wolf"]`；`GetMonster("test-wolf").Stats.MaxHealth == 60`。
  - `Load_Rejects_InvalidContent`（Theory）：每個案例在暫存資料夾寫入壞掉的 JSON，斷言 `GameContentException` 的訊息包含指定的 id：
    - 關卡引用不存在的 `ghost-monster`；
    - 妖怪 id 重複；
    - `startingStageId` 指向不存在的關卡；
    - `attacksPerSecond` 為 0；
    - `maxHealth` 為 0。
  - `GetMonster_UnknownId_Throws`：訊息包含該 id。
- [ ] **Step 2: Run**，預期失敗。
- [ ] **Step 3: 實作** `JsonGameContent.Load`：讀兩個檔，載入時一次完成所有驗證，查詢用 Dictionary。
- [ ] **Step 4: Run** `./scripts/check.sh`，預期通過。
- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "✨ feat: load and validate game content from JSON"
```

---

### Task 6: 帳號註冊、登入與 API 骨架

**Files:**
- Create: `backend/Inkbound.Application/Common/ApplicationExceptions.cs`, `IClock.cs`, `IRandomSourceFactory.cs`, `DependencyInjection.cs`
- Create: `backend/Inkbound.Application/Accounts/AccountService.cs`, `IAccountRepository.cs`, `IPasswordHashing.cs`, `AccountCredentials.cs`
- Create: `backend/Inkbound.Infrastructure/Persistence/AccountRepository.cs`, `Security/IdentityPasswordHashing.cs`, `Time/SystemClock.cs`, `Randomness/SystemRandomSourceFactory.cs`, `DependencyInjection.cs`
- Create: `backend/Inkbound.Api/Program.cs`（取代範本）、`appsettings.json`、`Auth/*`、`Errors/ApplicationExceptionHandler.cs`、`Endpoints/Contracts.cs`、`Endpoints/AccountEndpoints.cs`
- Create: `backend/Inkbound.Tests/Support/FakeClock.cs`, `FixedSeedRandomFactory.cs`, `InkboundApiFactory.cs`, `ApiClientExtensions.cs`
- Test: `backend/Inkbound.Tests/Api/AccountEndpointsTests.cs`

**Interfaces:**
- Consumes: `InkboundDbContext`、`MySqlDatabaseFixture`（Task 2）、`JsonGameContent`（Task 5）
- Produces:

```csharp
// Application
public sealed class ConflictException(string message) : Exception(message);              // → 409
public sealed class NotFoundException(string message) : Exception(message);              // → 404
public sealed class InputValidationException(string message) : Exception(message);       // → 400
public sealed class AuthenticationFailedException() : Exception("帳號或密碼錯誤");       // → 401
public interface IClock { DateTimeOffset UtcNow { get; } }
public interface IRandomSourceFactory { IRandomSource Create(); }
public interface IPasswordHashing { string Hash(string password); bool Verify(string passwordHash, string password); }
public sealed record AccountCredentials(long Id, string Username, string PasswordHash);
public interface IAccountRepository
{
    Task<long> AddAsync(string username, string passwordHash, DateTimeOffset createdAt, CancellationToken cancellationToken); // 重複 → ConflictException
    Task<AccountCredentials?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<AccountCredentials?> FindByIdAsync(long accountId, CancellationToken cancellationToken);
}
public sealed class AccountService
{
    Task<long> RegisterAsync(string username, string password, CancellationToken cancellationToken);
    Task<long> LoginAsync(string username, string password, CancellationToken cancellationToken);  // 回傳 accountId
}
public static IServiceCollection AddApplication(this IServiceCollection services);
// Infrastructure
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string contentRootPath);
// Api
public static long GetAccountId(this ClaimsPrincipal user);   // 讀 "sub"
// Tests
public sealed class InkboundApiFactory(MySqlDatabaseFixture database) : WebApplicationFactory<Program> { public FakeClock Clock { get; } }
public static Task<HttpClient> CreateAuthenticatedClientAsync(this InkboundApiFactory factory);  // 隨機帳號註冊並登入，設定 Bearer
```

HTTP 介面：
- `POST /api/accounts/register` `{ username, password }` → `201 { accountId }`
- `POST /api/accounts/login` `{ username, password }` → `200 { accessToken }`
- `GET /api/accounts/me`（需登入）→ `200 { accountId, username }`
- 錯誤一律回 ProblemDetails。

規則：
- username 3～32 字元，只允許 `[A-Za-z0-9_]`；password 至少 8 字元；不符就 `InputValidationException`。
- 帳號不存在和密碼錯誤都丟同一個 `AuthenticationFailedException`。
- 唯一鍵衝突是 MySQL 錯誤碼 1062，Repository 轉成 `ConflictException`。

設定：
- JWT：`Jwt:Issuer=inkbound`、`Jwt:Audience=inkbound`、`Jwt:SigningKey`（至少 32 bytes；dev 值放 `appsettings.Development.json`）、`Jwt:LifetimeDays=30`。
- `JwtBearerOptions.MapInboundClaims = false`，`sub` 存 accountId。
- `Content:Directory`：dev 為 `../content`，相對於 Api 的 ContentRootPath。
- `JsonGameContent` 在啟動時載入，註冊為 singleton。

- [ ] **Step 1: 套件**

```bash
dotnet add backend/Inkbound.Application package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add backend/Inkbound.Infrastructure package Microsoft.Extensions.Identity.Core
dotnet add backend/Inkbound.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add backend/Inkbound.Api package Microsoft.AspNetCore.OpenApi
dotnet add backend/Inkbound.Tests package Microsoft.AspNetCore.Mvc.Testing
```

- [ ] **Step 2: Write the failing tests** `AccountEndpointsTests.cs`

```csharp
public class AccountEndpointsTests(MySqlDatabaseFixture database)
{
    [Fact]
    public async Task Register_ThenLogin_GrantsAccessToProtectedEndpoint()
    {
        await using var factory = new InkboundApiFactory(database);
        var client = factory.CreateClient();
        var username = $"user_{Guid.NewGuid():N}"[..20];
        var register = await client.PostAsJsonAsync("/api/accounts/register", new { username, password = "password123" });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var login = await client.PostAsJsonAsync("/api/accounts/login", new { username, password = "password123" });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/accounts/me");
        Assert.Equal(username, me.GetProperty("username").GetString());
    }

    [Fact]
    public async Task Register_Returns409_ForDuplicateUsernameIgnoringCase() { /* 先註冊 lower，再註冊 ToUpperInvariant → 409 */ }

    [Theory]
    [InlineData("ab", "password123")]
    [InlineData("has space", "password123")]
    [InlineData("valid_name", "short")]
    public async Task Register_Returns400_ForInvalidInput(string username, string password) { /* → 400 */ }

    [Fact]
    public async Task Login_Returns401_WithSameDetail_ForWrongPasswordAndUnknownUser() { /* 兩次都 401，ProblemDetails.detail 相同 */ }

    [Fact]
    public async Task ProtectedEndpoint_Returns401_WithoutToken() { /* GET /api/accounts/me → 401 */ }
}
```

- [ ] **Step 3: Run** `dotnet test --filter AccountEndpointsTests`，預期失敗。
- [ ] **Step 4: 實作** Application、Infrastructure、Api 三層。

Program.cs 依序：
- `AddApplication`
- `AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath)`
- JWT 驗證與 `AddAuthorization`
- `AddProblemDetails`
- `AddExceptionHandler<ApplicationExceptionHandler>`
- `AddOpenApi`
- `ConfigureHttpJsonOptions` 加 `JsonStringEnumConverter`
- `UseExceptionHandler`
- 對應 endpoints
- Development 環境 `MapOpenApi`
- 檔尾加 `public partial class Program;`

InkboundApiFactory：
- 用 `UseSetting` 覆寫 `ConnectionStrings:Inkbound`（fixture 的連線字串）、`Content:Directory`（TestContent 的絕對路徑）、`Jwt:SigningKey`（測試用）；
- 在 `ConfigureTestServices` 把 `IClock` 換成 `FakeClock`（起始 `2026-01-01T00:00:00Z`），把 `IRandomSourceFactory` 換成 `FixedSeedRandomFactory(42)`。

- [ ] **Step 5: Run** `./scripts/check.sh`，預期全部通過。
- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "✨ feat: add account registration and JWT login"
```

---

### Task 7: 建立與查詢角色

**Files:**
- Create: `backend/Inkbound.Domain/Characters/Character.cs`, `PlayerStats.cs`
- Create: `backend/Inkbound.Application/Characters/CharacterService.cs`, `ICharacterRepository.cs`
- Create: `backend/Inkbound.Infrastructure/Persistence/CharacterRepository.cs`
- Create: `backend/Inkbound.Api/Endpoints/CharacterEndpoints.cs`；Modify: `Endpoints/Contracts.cs`
- Test: `backend/Inkbound.Tests/Api/CharacterEndpointsTests.cs`

**Interfaces:**
- Consumes: `IGameContent.StartingStageId`、`IClock`、例外型別、`CreateAuthenticatedClientAsync`
- Produces:

```csharp
public sealed record Character(long Id, long AccountId, string Name, long Experience, long SpiritStones,
    string CurrentStageId, DateTimeOffset LastSettledAt, int Version);
public static class PlayerStats { public static CombatStats Starting { get; } = new(120, 12, 4, 0.10, 1.5, 1.0); }
public interface ICharacterRepository
{
    Task<Character?> FindByAccountAsync(long accountId, CancellationToken cancellationToken);
    Task<Character> AddAsync(Character character, CancellationToken cancellationToken);   // 帳號或名稱重複 → ConflictException
}
public sealed class CharacterService
{
    Task<Character> CreateAsync(long accountId, string name, CancellationToken cancellationToken);
    Task<Character> GetAsync(long accountId, CancellationToken cancellationToken);         // 沒有角色 → NotFoundException
}
// Api 回應
public sealed record CharacterResponse(string Name, long Experience, long SpiritStones, string CurrentStageId);
```

HTTP 介面：
- `POST /api/characters` `{ name }` → `201 CharacterResponse`
- `GET /api/characters/me` → `200 CharacterResponse` 或 `404`
- 兩者都需要登入。

名稱規則：
- 先 Trim；
- 用 `StringInfo` 計算文字單位數，必須在 2～12 之間，否則 `InputValidationException`。

新角色：
- `CurrentStageId = content.StartingStageId`
- `LastSettledAt = clock.UtcNow`
- `Version = 0`

Repository 在 scaffold 產生的 `Character` 與 Domain 的 `Character` 之間轉換：
- 用 `using CharacterRow = Inkbound.Infrastructure.Persistence.Models.Character;` 避免名稱衝突；
- DATETIME 與 `DateTimeOffset` 之間一律以 UTC 轉換。

- [ ] **Step 1: Write the failing tests**
  - `Create_ReturnsCharacterAtStartingStage`：`"墨客"` → 201，`{ name: "墨客", experience: 0, spiritStones: 0, currentStageId: "test-stage-1" }`；`GET /me` 回傳相同內容。
  - `GetMe_Returns404_WhenNoCharacter`。
  - `Create_Returns409_WhenAccountAlreadyHasCharacter`。
  - `Create_Returns409_WhenNameTaken`：兩個帳號用同一個名字。名字要以隨機方式產生，例如 `$"名{Random.Shared.Next(100000, 999999)}"`，避免和其他測試互相影響。
  - `Create_ValidatesName`（Theory）：
    - `"墨"` → 400；
    - 13 個中文字 → 400；
    - `"   "` → 400；
    - `"  劍心  "` → 201，name 為 `"劍心"`。這個案例的名字也要加隨機後綴，避免重跑時名稱衝突，同時保持在 12 字以內。
  - `Endpoints_Require_Authentication`：兩個端點不帶 token 都回 401。
- [ ] **Step 2: Run**，預期失敗。
- [ ] **Step 3: 實作**上述介面與端點。
- [ ] **Step 4: Run** `./scripts/check.sh`，預期通過。
- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "✨ feat: create and view a cultivator character"
```

---

### Task 8: 推進歷練 API

**Files:**
- Create: `backend/Inkbound.Application/Adventure/AdventureService.cs`, `AdvanceResult.cs`
- Modify: `backend/Inkbound.Application/Characters/ICharacterRepository.cs`, `backend/Inkbound.Infrastructure/Persistence/CharacterRepository.cs`（新增 `UpdateAsync`）
- Create: `backend/Inkbound.Api/Endpoints/AdventureEndpoints.cs`；Modify: `Contracts.cs`
- Create: `backend/Inkbound.Tests/Support/InMemoryCharacterRepository.cs`
- Test: `backend/Inkbound.Tests/Infrastructure/CharacterRepositoryTests.cs`, `backend/Inkbound.Tests/Application/AdventureServiceTests.cs`, `backend/Inkbound.Tests/Api/AdventureEndpointsTests.cs`

**Interfaces:**
- Consumes: `AdventureSimulator.Run`、`PlayerStats.Starting`、`IGameContent`、`IClock`、`IRandomSourceFactory`、`ICharacterRepository`
- Produces:

```csharp
// ICharacterRepository 新增：以 character.Version 為預期版本更新，成功回傳 Version + 1 的角色；版本不符 → ConflictException
Task<Character> UpdateAsync(Character character, CancellationToken cancellationToken);
public sealed record AdvanceResult(IReadOnlyList<BattleEvent> Events, long ExperienceGained, long SpiritStonesGained, Character Character);
public sealed class AdventureService
{
    public const double MaxSimulatedSeconds = 300;   // 計畫 3 以離線結算取代
    Task<AdvanceResult> AdvanceAsync(long accountId, CancellationToken cancellationToken);
}
// Api 回應
public sealed record AdvanceResponse(IReadOnlyList<BattleEvent> Events, long ExperienceGained, long SpiritStonesGained, CharacterResponse Character);
```

HTTP 介面：`POST /api/adventure/advance`，需要登入。
- 成功 → `200 AdvanceResponse`；
- 沒有角色 → 404；
- 版本衝突 → 409。

`AdvanceAsync` 的流程：

```
character = repository.FindByAccountAsync ?? throw NotFoundException
now = clock.UtcNow
elapsed = (now - character.LastSettledAt).TotalSeconds
if elapsed <= 0: return AdvanceResult([], 0, 0, character)            // 不寫入
capped = elapsed > MaxSimulatedSeconds
stage = content.GetStage(character.CurrentStageId)
run = AdventureSimulator.Run(PlayerStats.Starting, stage.MonsterIds.Select(content.GetMonster).ToList(),
                             Math.Min(elapsed, MaxSimulatedSeconds), randomFactory.Create())
settledAt = capped ? now : character.LastSettledAt.AddSeconds(run.ConsumedSeconds)
updated = await repository.UpdateAsync(character with { Experience += run.ExperienceGained,
            SpiritStones += run.SpiritStonesGained, LastSettledAt = settledAt })
return new AdvanceResult(run.Events, run.ExperienceGained, run.SpiritStonesGained, updated)
```

`UpdateAsync` 的做法：
- 依 Id 載入資料列；
- 把 `Version` 的 OriginalValue 設成 `character.Version`；
- 寫入新值，並把 `Version` 設為 `character.Version + 1`；
- `DbUpdateConcurrencyException` 轉成 `ConflictException`。

- [ ] **Step 1: Write the failing test** `CharacterRepositoryTests.UpdateAsync_RejectsStaleVersion`
  - 用 Infrastructure 的 `AccountRepository` 與 `CharacterRepository`（直接以 fixture 的 DbContext 建立）新增帳號與角色；
  - 分兩次 `FindByAccountAsync` 取得兩份 `Version = 0` 的快照；
  - 第一份 `UpdateAsync` 成功，回傳的 `Version == 1`；
  - 第二份 `UpdateAsync` → `Assert.ThrowsAsync<ConflictException>`。
- [ ] **Step 2: Run**，預期失敗（`UpdateAsync` 不存在）。
- [ ] **Step 3: 實作** `UpdateAsync`。
- [ ] **Step 4: Run**，預期通過。

- [ ] **Step 5: Write the failing tests** `AdventureEndpointsTests.cs`
  - 每個測試都用 `factory.CreateAuthenticatedClientAsync()`，先 `POST /api/characters` 建立角色，此時 FakeClock 在起始時間。

```csharp
[Fact]
public async Task Advance_AfterTenSeconds_ReturnsEventsAndPersistsGains()
{
    // factory.Clock.Advance(TimeSpan.FromSeconds(10)); POST /api/adventure/advance
    // 斷言：200；events[0].type == "encounter"；events 中至少一個 type == "reward"
    // experienceGained > 0；character.experience == experienceGained
    // GET /api/characters/me 的 experience 等於 experienceGained
}

[Fact]
public async Task Advance_ReturnsNothing_WhenClockIsBehindLastSettlement()
{
    // factory.Clock.Advance(TimeSpan.FromSeconds(-5)); POST advance
    // → 200，events 為空，experienceGained == 0
}

[Fact]
public async Task Advance_CapsSimulationAtFiveMinutes()
{
    // factory.Clock.Advance(TimeSpan.FromHours(1)); POST advance
    // → 所有 events 的 atSeconds <= 300
    // 不移動時鐘，立刻再 POST 一次 → events 為空（已結算到現在）
}

[Fact]
public async Task Advance_Returns404_WhenNoCharacter() { }

[Fact]
public async Task OpenApiDocument_DescribesPolymorphicEvents()
{
    // GET /openapi/v1.json → 200，內容包含 "encounter" 與 "attack" 兩個 discriminator 值
}
```

- [ ] **Step 6: Write the failing tests** `backend/Inkbound.Tests/Application/AdventureServiceTests.cs`

這組測試證明「時間不夠打完的那場不會吃掉時間」。它不經過 HTTP，直接組裝 `AdventureService`：
- `InMemoryCharacterRepository`（新增於 `Support/`，`UpdateAsync` 一樣會檢查 Version）；
- `JsonGameContent.Load(TestContent)`；
- `FakeClock`；
- 回傳 `new ScriptedRandom(0.99)` 的 random factory。

手算的預期值：
- `0.99` 讓妖怪一律抽到 index 1，也就是 `test-wolf`，而且不會會心；
- 起始角色攻擊 12，對防禦 3 的寒狼，每下 `144/15 = 9.6 → 10` 點；寒狼 60 血，第 6 秒倒下；
- 休息 1 秒，到第 7 秒；剩 3 秒打不完下一場，捨棄。

```csharp
[Fact]
public async Task Advance_SettlesOnlyConsumedSeconds()
{
    // 角色 LastSettledAt = 起始時間；clock 前進 10 秒；AdvanceAsync
    // 斷言：ExperienceGained == 8、SpiritStonesGained == 3
    //      repository 中角色的 LastSettledAt == 起始時間 + 7 秒
}

[Fact]
public async Task Advance_SettlesToNow_WhenCapped()
{
    // clock 前進 1 小時；AdvanceAsync
    // 斷言：LastSettledAt == clock.UtcNow；所有事件 AtSeconds <= 300
}
```

- [ ] **Step 7: Run**，預期 API 與應用層的新測試都失敗。
- [ ] **Step 8: 實作** `AdventureService` 與端點。
- [ ] **Step 9: Run** `./scripts/check.sh`，預期全部通過。
- [ ] **Step 10: 手動驗證**
  1. `docker compose up -d`
  2. `cd backend && dotnet run --project Inkbound.Api`
  3. 用 curl 依序註冊、登入、建立角色；
  4. 等 10 秒後呼叫 `POST /api/adventure/advance`。

  預期回應中有 `encounter`、`attack`、`defeat`、`reward` 事件，且 `experienceGained > 0`。
- [ ] **Step 11: Commit**

```bash
git add -A && git commit -m "✨ feat: advance idle adventure and settle rewards"
```
