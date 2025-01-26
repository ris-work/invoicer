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

    public virtual DbSet<AccountsBalance> AccountsBalances { get; set; }

    public virtual DbSet<AccountsInformation> AccountsInformations { get; set; }

    public virtual DbSet<AccountsJournalEntry> AccountsJournalEntries { get; set; }

    public virtual DbSet<AccountsJournalInformation> AccountsJournalInformations { get; set; }

    public virtual DbSet<AccountsType> AccountsTypes { get; set; }

    public virtual DbSet<ApiAuthorization> ApiAuthorizations { get; set; }

    public virtual DbSet<AuthorizedTerminal> AuthorizedTerminals { get; set; }

    public virtual DbSet<Catalogue> Catalogues { get; set; }

    public virtual DbSet<CategoriesBitmask> CategoriesBitmasks { get; set; }

    public virtual DbSet<Credential> Credentials { get; set; }

    public virtual DbSet<DescriptionsOtherLanguage> DescriptionsOtherLanguages { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NotificationServicerType> NotificationServicerTypes { get; set; }

    public virtual DbSet<NotificationType> NotificationTypes { get; set; }

    public virtual DbSet<PermissionsList> PermissionsLists { get; set; }

    public virtual DbSet<PermissionsListCategoriesName> PermissionsListCategoriesNames { get; set; }

    public virtual DbSet<PermissionsListUsersCategory> PermissionsListUsersCategories { get; set; }

    public virtual DbSet<Request> Requests { get; set; }

    public virtual DbSet<Sih> Sihs { get; set; }

    public virtual DbSet<SihCurrent> SihCurrents { get; set; }

    public virtual DbSet<Token> Tokens { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAuthorization> UserAuthorizations { get; set; }

    public virtual DbSet<VatCategory> VatCategories { get; set; }

    public virtual DbSet<VolumeDiscount> VolumeDiscounts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    {
        optionsBuilder.UseNpgsql((String)Config.model["ConnString"]);
        optionsBuilder.LogTo(Console.WriteLine);
     }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountsBalance>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("accounts_balances", tb => tb.HasComment("Positive is debit"));

            entity.Property(e => e.AccountNo).HasColumnName("account_no");
            entity.Property(e => e.AccountType).HasColumnName("account_type");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.DoneWith)
                .HasDefaultValue(false)
                .HasColumnName("done_with");
            entity.Property(e => e.TimeAsEntered)
                .HasDefaultValueSql("now()")
                .HasColumnName("time_as_entered");
            entity.Property(e => e.TimeTai)
                .HasDefaultValueSql("now()")
                .HasColumnName("time_tai");
        });

        modelBuilder.Entity<AccountsInformation>(entity =>
        {
            entity.HasKey(e => new { e.AccountType, e.AccountNo }).HasName("accounts_information_pkey");

            entity.ToTable("accounts_information");

            entity.HasIndex(e => e.HumanFriendlyId, "human_friendly_id").IsUnique();

            entity.Property(e => e.AccountType).HasColumnName("account_type");
            entity.Property(e => e.AccountNo).HasColumnName("account_no");
            entity.Property(e => e.AccountI18nLabel).HasColumnName("account_i18n_label");
            entity.Property(e => e.AccountMax)
                .HasDefaultValueSql("1000000000")
                .HasColumnName("account_max");
            entity.Property(e => e.AccountMin)
                .HasDefaultValueSql("'-1000000000'::integer")
                .HasColumnName("account_min");
            entity.Property(e => e.AccountName).HasColumnName("account_name");
            entity.Property(e => e.AccountPii).HasColumnName("account_pii");
            entity.Property(e => e.HumanFriendlyId).HasColumnName("human_friendly_id");
        });

        modelBuilder.Entity<AccountsJournalEntry>(entity =>
        {
            entity.HasKey(e => e.JournalUnivSeq).HasName("accounts_journal_entries_pkey");

            entity.ToTable("accounts_journal_entries");

            entity.Property(e => e.JournalUnivSeq).HasColumnName("journal_univ_seq");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.CreditAccountNo).HasColumnName("credit_account_no");
            entity.Property(e => e.CreditAccountType).HasColumnName("credit_account_type");
            entity.Property(e => e.DebitAccountNo).HasColumnName("debit_account_no");
            entity.Property(e => e.DebitAccountType).HasColumnName("debit_account_type");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.JournalNo).HasColumnName("journal_no");
            entity.Property(e => e.Ref).HasColumnName("ref");
            entity.Property(e => e.RefNo).HasColumnName("ref_no");
            entity.Property(e => e.TimeAsEntered).HasColumnName("time_as_entered");
            entity.Property(e => e.TimeTai)
                .HasDefaultValueSql("now()")
                .HasColumnName("time_tai");
        });

        modelBuilder.Entity<AccountsJournalInformation>(entity =>
        {
            entity.HasKey(e => e.JournalId).HasName("accounts_journal_information_pkey");

            entity.ToTable("accounts_journal_information");

            entity.Property(e => e.JournalId).HasColumnName("journal_id");
            entity.Property(e => e.JournalI18nLabel).HasColumnName("journal_i18n_label");
            entity.Property(e => e.JournalName).HasColumnName("journal_name");
        });

        modelBuilder.Entity<AccountsType>(entity =>
        {
            entity.HasKey(e => e.AccountType).HasName("accounts_types_pkey");

            entity.ToTable("accounts_types", tb => tb.HasComment("Always these four _real_ accounts"));

            entity.Property(e => e.AccountType)
                .ValueGeneratedNever()
                .HasColumnName("account_type");
            entity.Property(e => e.AccountTypeI18nLabel).HasColumnName("account_type_i18n_label");
            entity.Property(e => e.AccountTypeName).HasColumnName("account_type_name");
        });

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
            entity.Property(e => e.CategoriesBitmask)
                .HasDefaultValue(1L)
                .HasColumnName("categories_bitmask");
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
            entity.Property(e => e.PermissionsCategory)
                .HasDefaultValue(0L)
                .HasColumnName("permissions_category");
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

        modelBuilder.Entity<CategoriesBitmask>(entity =>
        {
            entity.HasKey(e => e.Bitmask).HasName("categories_bitmask_pkey");

            entity.ToTable("categories_bitmask");

            entity.Property(e => e.Bitmask)
                .ValueGeneratedNever()
                .HasColumnName("bitmask");
            entity.Property(e => e.I18nLabel).HasColumnName("i18n_label");
            entity.Property(e => e.Name).HasColumnName("name");
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

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotifId).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.Property(e => e.NotifId).HasColumnName("notif_id");
            entity.Property(e => e.NotifIsDone)
                .HasDefaultValue(false)
                .HasColumnName("notif_is_done");
            entity.Property(e => e.NotifOtherStatus).HasColumnName("notif_other_status");
            entity.Property(e => e.NotifTarget).HasColumnName("notif_target");
            entity.Property(e => e.NotifType)
                .HasDefaultValueSql("'INTERNAL'::text")
                .HasColumnName("notif_type");
            entity.Property(e => e.TimeExpiresTai).HasColumnName("time_expires_tai");
            entity.Property(e => e.TimeTai).HasColumnName("time_tai");
        });

        modelBuilder.Entity<NotificationServicerType>(entity =>
        {
            entity.HasKey(e => e.NotificationServicerTypeId).HasName("notification_servicer_types_pkey");

            entity.ToTable("notification_servicer_types");

            entity.Property(e => e.NotificationServicerTypeId).HasColumnName("notification_servicer_type_id");
            entity.Property(e => e.NotificationServicerName).HasColumnName("notification_servicer_name");
        });

        modelBuilder.Entity<NotificationType>(entity =>
        {
            entity.HasKey(e => e.NotificationTypeId).HasName("notification_types_pkey");

            entity.ToTable("notification_types");

            entity.Property(e => e.NotificationTypeId).HasColumnName("notification_type_id");
            entity.Property(e => e.NotificationService).HasColumnName("notification_service");
            entity.Property(e => e.NotificationServiceOtherArgs)
                .HasDefaultValueSql("''::text")
                .HasColumnName("notification_service_other_args");
            entity.Property(e => e.NotificationServicerType).HasColumnName("notification_servicer_type");
            entity.Property(e => e.NotificationTypeName).HasColumnName("notification_type_name");
        });

        modelBuilder.Entity<PermissionsList>(entity =>
        {
            entity.HasKey(e => e.Permission).HasName("permissions_list_pkey");

            entity.ToTable("permissions_list", tb => tb.HasComment("Comma-separated, no spaces"));
        });

        modelBuilder.Entity<PermissionsListCategoriesName>(entity =>
        {
            entity.HasKey(e => e.Category).HasName("permissions_list_categories_names_pkey");

            entity.ToTable("permissions_list_categories_names");

            entity.Property(e => e.Category)
                .ValueGeneratedNever()
                .HasColumnName("category");
            entity.Property(e => e.CategoryName).HasColumnName("category_name");
            entity.Property(e => e.LabelI18n).HasColumnName("label_i18n");
        });

        modelBuilder.Entity<PermissionsListUsersCategory>(entity =>
        {
            entity.HasKey(e => e.Userid).HasName("permissions_list_users_categories_pkey");

            entity.ToTable("permissions_list_users_categories");

            entity.Property(e => e.Userid)
                .ValueGeneratedNever()
                .HasColumnName("userid");
            entity.Property(e => e.Categories)
                .HasDefaultValue(0L)
                .HasColumnName("categories");
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("requests");

            entity.Property(e => e.Principal).HasColumnName("principal");
            entity.Property(e => e.RequestBody).HasColumnName("request_body");
            entity.Property(e => e.TimeTai)
                .HasDefaultValueSql("now()")
                .HasColumnType("time with time zone")
                .HasColumnName("time_tai");
            entity.Property(e => e.Token).HasColumnName("token");
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
            entity.Property(e => e.CategoriesBitmask)
                .HasDefaultValue(0L)
                .HasColumnName("categories_bitmask");
            entity.Property(e => e.NotValidAfter).HasColumnName("not_valid_after");
            entity.Property(e => e.Privileges)
                .HasDefaultValueSql("''::text")
                .HasColumnName("privileges");
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
            entity.HasKey(e => e.Userid).HasName("user_authorization_pkey");

            entity.ToTable("user_authorization", tb => tb.HasComment("user_cap: Comma-separated\nuser_default_cap: Comma-separated"));

            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.UserCap).HasColumnName("user_cap");
            entity.Property(e => e.UserDefaultCap)
                .HasDefaultValueSql("''::text")
                .HasColumnName("user_default_cap");
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
