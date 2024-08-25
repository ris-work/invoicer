using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RV.InvNew.Common;

public partial class NewinvContext : DbContext
{
    public NewinvContext()
    {
    }

    public NewinvContext(DbContextOptions<NewinvContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ApiAuthorization> ApiAuthorizations { get; set; }

    public virtual DbSet<AuthorizedTerminal> AuthorizedTerminals { get; set; }

    public virtual DbSet<Catalogue> Catalogues { get; set; }

    public virtual DbSet<Credential> Credentials { get; set; }

    public virtual DbSet<DescriptionsOtherLanguage> DescriptionsOtherLanguages { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Sih> Sihs { get; set; }

    public virtual DbSet<SihCurrent> SihCurrents { get; set; }

    public virtual DbSet<Token> Tokens { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAuthorization> UserAuthorizations { get; set; }

    public virtual DbSet<VatCategory> VatCategories { get; set; }

    public virtual DbSet<VolumeDiscount> VolumeDiscounts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql((String)Config.model["ConnString"]);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiAuthorization>(entity =>
        {
            entity.HasKey(e => new { e.Userid, e.Authorization }).HasName("api_authorization_pkey");

            entity.ToTable("api_authorization");

            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Authorization).HasColumnName("authorization");
            entity.Property(e => e.Pubkey).HasColumnName("pubkey");
        });

        modelBuilder.Entity<AuthorizedTerminal>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("authorized_terminals");

            entity.Property(e => e.Terminalid).HasColumnName("terminalid");
            entity.Property(e => e.Userid)
                .ValueGeneratedOnAdd()
                .HasColumnName("userid");
        });

        modelBuilder.Entity<Catalogue>(entity =>
        {
            entity.HasKey(e => e.Itemcode).HasName("catalogue_pkey");

            entity.ToTable("catalogue");

            entity.HasIndex(e => e.Description, "unique_desc").IsUnique();

            entity.Property(e => e.Itemcode).HasColumnName("itemcode");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.ActiveWeb)
                .HasDefaultValue(false)
                .HasColumnName("active_web");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_on");
            entity.Property(e => e.DefaultVatCategory)
                .HasDefaultValue(0L)
                .HasColumnName("default_vat_category");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DescriptionPos).HasColumnName("description_pos");
            entity.Property(e => e.DescriptionWeb).HasColumnName("description_web");
            entity.Property(e => e.DescriptionsOtherLanguages)
                .HasDefaultValue(0L)
                .HasColumnName("descriptions_other_languages");
            entity.Property(e => e.EnforceAboveCost)
                .HasDefaultValue(true)
                .HasColumnName("enforce_above_cost");
            entity.Property(e => e.ExpiryTrackingEnabled)
                .HasDefaultValue(false)
                .HasColumnName("expiry_tracking_enabled");
            entity.Property(e => e.PriceManual)
                .HasDefaultValue(false)
                .HasColumnName("price_manual");
            entity.Property(e => e.VatCategoryAdjustable)
                .HasDefaultValue(false)
                .HasColumnName("vat_category_adjustable");
            entity.Property(e => e.VatDependsOnUser)
                .HasDefaultValue(false)
                .HasColumnName("vat_depends_on_user");
        });

        modelBuilder.Entity<Credential>(entity =>
        {
            entity.HasKey(e => e.Userid).HasName("credentials_pkey");

            entity.ToTable("credentials");

            entity.HasIndex(e => e.Username, "username_unique").IsUnique();

            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Active)
                .HasDefaultValue(false)
                .HasColumnName("active");
            entity.Property(e => e.CreatedTime)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_time");
            entity.Property(e => e.Modified).HasColumnName("modified");
            entity.Property(e => e.PasswordPbkdf2).HasColumnName("password_pbkdf2");
            entity.Property(e => e.Pubkey).HasColumnName("pubkey");
            entity.Property(e => e.Username).HasColumnName("username");
            entity.Property(e => e.ValidUntil).HasColumnName("valid_until");
        });

        modelBuilder.Entity<DescriptionsOtherLanguage>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("descriptions_other_languages");

            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DescriptionPos).HasColumnName("description_pos");
            entity.Property(e => e.DescriptionWeb).HasColumnName("description_web");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Language)
                .HasMaxLength(5)
                .HasColumnName("language");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => new { e.Itemcode, e.Batchcode }).HasName("inventory_pkey");

            entity.ToTable("inventory", tb => tb.HasComment("Internal inventory management functions"));

            entity.Property(e => e.Itemcode)
                .ValueGeneratedOnAdd()
                .HasColumnName("itemcode");
            entity.Property(e => e.Batchcode).HasColumnName("batchcode");
            entity.Property(e => e.BatchEnabled)
                .HasDefaultValue(true)
                .HasColumnName("batch_enabled");
            entity.Property(e => e.CostPrice).HasColumnName("cost_price");
            entity.Property(e => e.ExpDate).HasColumnName("exp_date");
            entity.Property(e => e.MarkedPrice).HasColumnName("marked_price");
            entity.Property(e => e.MeasurementUnit)
                .HasDefaultValueSql("'qty'::text")
                .HasColumnName("measurement_unit");
            entity.Property(e => e.MfgDate)
                .HasDefaultValueSql("now()")
                .HasColumnName("mfg_date");
            entity.Property(e => e.PackedSize)
                .HasDefaultValueSql("1")
                .HasColumnName("packed_size");
            entity.Property(e => e.SellingPrice).HasColumnName("selling_price");
            entity.Property(e => e.Suppliercode)
                .HasDefaultValue(0L)
                .HasColumnName("suppliercode");
            entity.Property(e => e.Units).HasColumnName("units");
            entity.Property(e => e.UserDiscounts)
                .HasDefaultValue(false)
                .HasColumnName("user_discounts");
            entity.Property(e => e.VolumeDiscounts)
                .HasDefaultValue(false)
                .HasColumnName("volume_discounts");
        });

        modelBuilder.Entity<Sih>(entity =>
        {
            entity.HasKey(e => e.Itemcode).HasName("sih_pkey");

            entity.ToTable("sih", "imported_dummy");

            entity.Property(e => e.Itemcode)
                .ValueGeneratedNever()
                .HasColumnName("itemcode");
            entity.Property(e => e.Cost).HasColumnName("cost");
            entity.Property(e => e.Desc).HasColumnName("desc");
            entity.Property(e => e.Sell).HasColumnName("sell");
            entity.Property(e => e.Sih1).HasColumnName("sih");
        });

        modelBuilder.Entity<SihCurrent>(entity =>
        {
            entity.HasKey(e => e.Itemcode).HasName("sih_current_pkey");

            entity.ToTable("sih_current", "imported_dummy");

            entity.Property(e => e.Itemcode)
                .ValueGeneratedNever()
                .HasColumnName("itemcode");
            entity.Property(e => e.Cost).HasColumnName("cost");
            entity.Property(e => e.Desc).HasColumnName("desc");
            entity.Property(e => e.Sell).HasColumnName("sell");
            entity.Property(e => e.Sih).HasColumnName("sih");
        });

        modelBuilder.Entity<Token>(entity =>
        {
            entity.HasKey(e => e.Tokenid).HasName("tokens_pkey");

            entity.ToTable("tokens");

            entity.Property(e => e.Tokenid)
                .HasDefaultValueSql("(random())::text")
                .HasColumnName("tokenid");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.NotValidAfter).HasColumnName("not_valid_after");
            entity.Property(e => e.Tokensecret).HasColumnName("tokensecret");
            entity.Property(e => e.Tokenvalue).HasColumnName("tokenvalue");
            entity.Property(e => e.Userid).HasColumnName("userid");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Userid).HasName("users_pkey");

            entity.ToTable("users");

            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Phone).HasColumnName("phone");
        });

        modelBuilder.Entity<UserAuthorization>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("user_authorization");

            entity.Property(e => e.UserCap).HasColumnName("user_cap");
            entity.Property(e => e.Userid)
                .ValueGeneratedOnAdd()
                .HasColumnName("userid");
        });

        modelBuilder.Entity<VatCategory>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("vat_categories");

            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.VatCategoryId)
                .ValueGeneratedOnAdd()
                .HasColumnName("vat_category_id");
            entity.Property(e => e.VatName).HasColumnName("vat_name");
            entity.Property(e => e.VatPercentage).HasColumnName("vat_percentage");
        });

        modelBuilder.Entity<VolumeDiscount>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("volume_discounts");

            entity.Property(e => e.DiscountPerUnit).HasColumnName("discount_per_unit");
            entity.Property(e => e.Itemcode).HasColumnName("itemcode");
            entity.Property(e => e.StartFrom)
                .HasDefaultValue(1L)
                .HasColumnName("start_from");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
