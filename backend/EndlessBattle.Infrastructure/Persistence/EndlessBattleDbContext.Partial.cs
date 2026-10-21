using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

// 補充 scaffold 產生的 EndlessBattleDbContext；產生的檔案不得手改，額外設定寫在這裡
public partial class EndlessBattleDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // 樂觀鎖：UPDATE 時帶上原本的 version，版本不符就丟 DbUpdateConcurrencyException
        modelBuilder.Entity<Character>().Property(character => character.Version).IsConcurrencyToken();
    }
}
