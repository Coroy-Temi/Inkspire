using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PoetryPlatform.Web.Models;
using PoetryPlatform.Web.Models.Payments;

namespace PoetryPlatform.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Poem> Poems { get; set; } = null!;
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<PoemLike> PoemLikes => Set<PoemLike>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<PoemView> PoemViews => Set<PoemView>();
    public DbSet<CommentFlag> CommentFlags { get; set; } = null!;
    public DbSet<BookOrder> BookOrders => Set<BookOrder>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<PaymentWebhookLog> PaymentWebhookLogs => Set<PaymentWebhookLog>();
    public DbSet<AuthorPayoutAccount> AuthorPayoutAccounts => Set<AuthorPayoutAccount>();
    public DbSet<AuthorEarning> AuthorEarnings => Set<AuthorEarning>();
    public DbSet<PayoutBatch> PayoutBatches => Set<PayoutBatch>();
    public DbSet<PayoutItem> PayoutItems => Set<PayoutItem>();
    public DbSet<ReaderSubscriptionPlan> ReaderSubscriptionPlans => Set<ReaderSubscriptionPlan>();
    public DbSet<ReaderSubscription> ReaderSubscriptions => Set<ReaderSubscription>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.CountryCode).HasMaxLength(8);
        });

        builder.Entity<BookOrder>(entity =>
        {
            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.HasIndex(o => new { o.PoemId, o.BuyerId, o.Status });

            entity.Property(o => o.Currency).HasMaxLength(12);
            entity.Property(o => o.OrderNumber).HasMaxLength(64);

            entity.HasOne(o => o.Poem)
                .WithMany()
                .HasForeignKey(o => o.PoemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Buyer)
                .WithMany()
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Author)
                .WithMany()
                .HasForeignKey(o => o.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentRecord>(entity =>
        {
            entity.HasIndex(p => new { p.BookOrderId, p.ExternalReference });
            entity.Property(p => p.Currency).HasMaxLength(12);
            entity.Property(p => p.ExternalReference).HasMaxLength(128);
            entity.Property(p => p.ExternalEventId).HasMaxLength(128);
            entity.Property(p => p.ObjectType).HasMaxLength(64);
        });

        builder.Entity<PaymentWebhookLog>(entity =>
        {
            entity.HasIndex(p => new { p.Provider, p.EventId });
            entity.Property(p => p.EventId).HasMaxLength(128);
            entity.Property(p => p.EventType).HasMaxLength(128);
        });

        builder.Entity<AuthorPayoutAccount>(entity =>
        {
            entity.HasIndex(a => new { a.AuthorId, a.Provider }).IsUnique();
            entity.Property(a => a.CountryCode).HasMaxLength(8);
            entity.Property(a => a.Currency).HasMaxLength(12);
            entity.Property(a => a.BusinessName).HasMaxLength(160);
            entity.Property(a => a.BankCode).HasMaxLength(32);
            entity.Property(a => a.BankName).HasMaxLength(160);
            entity.Property(a => a.AccountNumber).HasMaxLength(64);
            entity.Property(a => a.AccountName).HasMaxLength(160);
            entity.Property(a => a.PaystackRecipientCode).HasMaxLength(128);
            entity.Property(a => a.PaystackRecipientType).HasMaxLength(64);
            entity.Property(a => a.PaystackSubaccountCode).HasMaxLength(128);
            entity.Property(a => a.PaystackSubaccountId).HasMaxLength(64);
            entity.Property(a => a.StripeAccountId).HasMaxLength(128);

            entity.HasOne(a => a.Author)
                .WithMany()
                .HasForeignKey(a => a.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuthorEarning>(entity =>
        {
            entity.HasIndex(e => e.BookOrderId).IsUnique();
            entity.HasIndex(e => new { e.AuthorId, e.Status });
            entity.Property(e => e.Currency).HasMaxLength(12);
            entity.Property(e => e.ExternalSettlementReference).HasMaxLength(128);

            entity.HasOne(e => e.BookOrder)
                .WithOne()
                .HasForeignKey<AuthorEarning>(e => e.BookOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AuthorPayoutAccount)
                .WithMany(a => a.Earnings)
                .HasForeignKey(e => e.AuthorPayoutAccountId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PayoutBatch>(entity =>
        {
            entity.HasIndex(b => b.BatchReference).IsUnique();
            entity.Property(b => b.BatchReference).HasMaxLength(64);
            entity.Property(b => b.Currency).HasMaxLength(12);
        });

        builder.Entity<PayoutItem>(entity =>
        {
            entity.HasIndex(i => i.AuthorEarningId).IsUnique();
            entity.Property(i => i.Currency).HasMaxLength(12);
            entity.Property(i => i.ExternalTransferId).HasMaxLength(128);
            entity.Property(i => i.ExternalReference).HasMaxLength(128);

            entity.HasOne(i => i.PayoutBatch)
                .WithMany(b => b.Items)
                .HasForeignKey(i => i.PayoutBatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.AuthorEarning)
                .WithOne(e => e.PayoutItem)
                .HasForeignKey<PayoutItem>(i => i.AuthorEarningId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.AuthorPayoutAccount)
                .WithMany(a => a.PayoutItems)
                .HasForeignKey(i => i.AuthorPayoutAccountId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ReaderSubscriptionPlan>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(120);
            entity.Property(p => p.Currency).HasMaxLength(12);
        });

        builder.Entity<ReaderSubscription>(entity =>
        {
            entity.HasIndex(s => new { s.ReaderId, s.Status });
            entity.Property(s => s.Currency).HasMaxLength(12);
            entity.Property(s => s.ExternalSubscriptionId).HasMaxLength(128);
            entity.Property(s => s.ExternalCustomerId).HasMaxLength(128);

            entity.HasOne(s => s.Reader)
                .WithMany()
                .HasForeignKey(s => s.ReaderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.ReaderSubscriptionPlan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.ReaderSubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
