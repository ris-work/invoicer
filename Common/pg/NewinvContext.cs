using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RV.InvNew.Common;

public partial class NewinvContext : DbContext
{
    public NewinvContext() { }

    public NewinvContext(DbContextOptions<NewinvContext> options)
        : base(options) { }

    public virtual DbSet<AccountsBalance> AccountsBalances { get; set; }

    public virtual DbSet<AccountsInformation> AccountsInformations { get; set; }

    public virtual DbSet<AccountsJournalEntry> AccountsJournalEntries { get; set; }

    public virtual DbSet<AccountsJournalInformation> AccountsJournalInformations { get; set; }

    public virtual DbSet<AccountsType> AccountsTypes { get; set; }

    public virtual DbSet<ApiAuthorization> ApiAuthorizations { get; set; }

    public virtual DbSet<AuthorizedTerminal> AuthorizedTerminals { get; set; }

    public virtual DbSet<BundledPricing> BundledPricings { get; set; }

    public virtual DbSet<Catalogue> Catalogues { get; set; }

    public virtual DbSet<CategoriesBitmask> CategoriesBitmasks { get; set; }

    public virtual DbSet<CodesBatch> CodesBatches { get; set; }

    public virtual DbSet<CodesCatalogue> CodesCatalogues { get; set; }

    public virtual DbSet<Credential> Credentials { get; set; }

    public virtual DbSet<CustomerDiscount> CustomerDiscounts { get; set; }

    public virtual DbSet<DefaultDenyField> DefaultDenyFields { get; set; }

    public virtual DbSet<DescriptionsOtherLanguage> DescriptionsOtherLanguages { get; set; }

    public virtual DbSet<I18nLabel> I18nLabels { get; set; }

    public virtual DbSet<Idempotency> Idempotencies { get; set; }

    public virtual DbSet<Inventory> Inventories { get; set; }

    public virtual DbSet<InventoryImage> InventoryImages { get; set; }

    public virtual DbSet<IssuedInvoice> IssuedInvoices { get; set; }

    public virtual DbSet<LoyaltyPoint> LoyaltyPoints { get; set; }

    public virtual DbSet<LoyaltyPointsRedemption> LoyaltyPointsRedemptions { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NotificationServicerType> NotificationServicerTypes { get; set; }

    public virtual DbSet<NotificationType> NotificationTypes { get; set; }

    public virtual DbSet<PermissionsExtendedApiCall> PermissionsExtendedApiCalls { get; set; }

    public virtual DbSet<PermissionsList> PermissionsLists { get; set; }

    public virtual DbSet<PermissionsListCategoriesName> PermissionsListCategoriesNames { get; set; }

    public virtual DbSet<PermissionsListUsersCategory> PermissionsListUsersCategories { get; set; }

    public virtual DbSet<Pii> Piis { get; set; }

    public virtual DbSet<PiiImage> PiiImages { get; set; }

    public virtual DbSet<Purchase> Purchases { get; set; }

    public virtual DbSet<Receipt> Receipts { get; set; }

    public virtual DbSet<ReceivedInvoice> ReceivedInvoices { get; set; }

    public virtual DbSet<Request> Requests { get; set; }

    public virtual DbSet<RequestsBad> RequestsBads { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    public virtual DbSet<Sih> Sihs { get; set; }

    public virtual DbSet<SihCurrent> SihCurrents { get; set; }

    public virtual DbSet<SuggestedPrice> SuggestedPrices { get; set; }

    public virtual DbSet<TieredDiscount> TieredDiscounts { get; set; }

    public virtual DbSet<Token> Tokens { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAuthorization> UserAuthorizations { get; set; }

    public virtual DbSet<UsersFieldLevelAccessControlsDenyList> UsersFieldLevelAccessControlsDenyLists { get; set; }

    public virtual DbSet<VatCategory> VatCategories { get; set; }

    public virtual DbSet<VolumeDiscount> VolumeDiscounts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        =>
        optionsBuilder.UseNpgsql("Host=127.0.0.1;Database=newinv;Username=rishi;Password=eeee");

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
            entity.Property(e => e.DoneWith).HasDefaultValue(false).HasColumnName("done_with");
            entity
                .Property(e => e.TimeAsEntered)
                .HasDefaultValueSql("now()")
                .HasColumnName("time_as_entered");
            entity.Property(e => e.TimeTai).HasDefaultValueSql("now()").HasColumnName("time_tai");
        });

        modelBuilder.Entity<AccountsInformation>(entity =>
        {
            entity
                .HasKey(e => new { e.AccountType, e.AccountNo })
                .HasName("accounts_information_pkey");

            entity.ToTable("accounts_information");

            entity.HasIndex(e => e.HumanFriendlyId, "human_friendly_id").IsUnique();

            entity.Property(e => e.AccountType).HasColumnName("account_type");
            entity.Property(e => e.AccountNo).HasColumnName("account_no");
            entity.Property(e => e.AccountI18nLabel).HasColumnName("account_i18n_label");
            entity
                .Property(e => e.AccountMax)
                .HasDefaultValueSql("1000000000")
                .HasColumnName("account_max");
            entity
                .Property(e => e.AccountMin)
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
            entity.Property(e => e.TimeTai).HasDefaultValueSql("now()").HasColumnName("time_tai");
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

            entity.ToTable(
                "accounts_types",
                tb => tb.HasComment("Always these four _real_ accounts")
            );

            entity.Property(e => e.AccountType).ValueGeneratedNever().HasColumnName("account_type");
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
            entity.HasNoKey().ToTable("authorized_terminals");

            entity.Property(e => e.Terminalid).HasColumnName("terminalid");
            entity.Property(e => e.Userid).ValueGeneratedOnAdd().HasColumnName("userid");
        });

        modelBuilder.Entity<BundledPricing>(entity =>
        {
            entity.HasKey(e => e.BundleId).HasName("bundled_pricing_pkey");

            entity.ToTable("bundled_pricing");

            entity.Property(e => e.BundleId).HasColumnName("bundle_id");
            entity.Property(e => e.Discount).HasColumnName("discount");
            entity.Property(e => e.Itemcode).HasColumnName("itemcode");
        });

        modelBuilder.Entity<Catalogue>(entity =>
        {
            entity.HasKey(e => e.Itemcode).HasName("catalogue_pkey");

            entity.ToTable("catalogue");

            entity.HasIndex(e => e.Description, "unique_desc").IsUnique();

            entity.Property(e => e.Itemcode).HasColumnName("itemcode");
            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity.Property(e => e.ActiveWeb).HasDefaultValue(false).HasColumnName("active_web");
            entity
                .Property(e => e.AllowPriceSuggestions)
                .HasDefaultValue(true)
                .HasColumnName("allow_price_suggestions");
            entity
                .Property(e => e.CategoriesBitmask)
                .HasDefaultValue(1L)
                .HasColumnName("categories_bitmask");
            entity
                .Property(e => e.CreatedOn)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_on");
            entity
                .Property(e => e.DefaultVatCategory)
                .HasDefaultValue(0L)
                .HasColumnName("default_vat_category");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DescriptionPos).HasColumnName("description_pos");
            entity.Property(e => e.DescriptionWeb).HasColumnName("description_web");
            entity
                .Property(e => e.DescriptionsOtherLanguages)
                .HasDefaultValue(0L)
                .HasColumnName("descriptions_other_languages");
            entity
                .Property(e => e.EnforceAboveCost)
                .HasDefaultValue(true)
                .HasColumnName("enforce_above_cost");
            entity
                .Property(e => e.ExpiryTrackingEnabled)
                .HasDefaultValue(false)
                .HasColumnName("expiry_tracking_enabled");
            entity.Property(e => e.HeightM).HasColumnName("height_m");
            entity.Property(e => e.LengthM).HasColumnName("length_m");
            entity
                .Property(e => e.MaxPerInvoice)
                .HasDefaultValueSql("1000000")
                .HasColumnName("max_per_invoice");
            entity
                .Property(e => e.MaxPerPerson)
                .HasDefaultValueSql("1000000")
                .HasColumnName("max_per_person");
            entity.Property(e => e.MinPerInvoice).HasColumnName("min_per_invoice");
            entity
                .Property(e => e.PermissionsCategory)
                .HasDefaultValue(0L)
                .HasColumnName("permissions_category");
            entity
                .Property(e => e.PriceManual)
                .HasDefaultValue(false)
                .HasColumnName("price_manual");
            entity
                .Property(e => e.ProcessDiscounts)
                .HasDefaultValue(true)
                .HasColumnName("process_discounts");
            entity.Property(e => e.Remarks).HasDefaultValueSql("''::text").HasColumnName("remarks");
            entity
                .Property(e => e.VatCategoryAdjustable)
                .HasDefaultValue(false)
                .HasColumnName("vat_category_adjustable");
            entity
                .Property(e => e.VatDependsOnUser)
                .HasDefaultValue(false)
                .HasColumnName("vat_depends_on_user");
            entity.Property(e => e.WeightPerUnitKg).HasColumnName("weight_per_unit_kg");
            entity.Property(e => e.WidthM).HasColumnName("width_m");
        });

        modelBuilder.Entity<CategoriesBitmask>(entity =>
        {
            entity.HasKey(e => e.Bitmask).HasName("categories_bitmask_pkey");

            entity.ToTable("categories_bitmask");

            entity.Property(e => e.Bitmask).ValueGeneratedNever().HasColumnName("bitmask");
            entity.Property(e => e.I18nLabel).HasColumnName("i18n_label");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<CodesBatch>(entity =>
        {
            entity
                .HasKey(e => new
                {
                    e.Code,
                    e.Itemcode,
                    e.Batchcode,
                })
                .HasName("codes_batches_pkey");

            entity.ToTable("codes_batches");

            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Itemcode).HasColumnName("itemcode");
            entity.Property(e => e.Batchcode).HasColumnName("batchcode");
            entity
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("time with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Enabled).HasDefaultValue(true).HasColumnName("enabled");
        });

        modelBuilder.Entity<CodesCatalogue>(entity =>
        {
            entity.HasKey(e => new { e.Code, e.Itemcode }).HasName("codes_catalogue_pkey");

            entity.ToTable("codes_catalogue");

            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Itemcode).HasColumnName("itemcode");
            entity
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("time with time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Enabled).HasDefaultValue(true).HasColumnName("enabled");
        });

        modelBuilder.Entity<Credential>(entity =>
        {
            entity.HasKey(e => e.Userid).HasName("credentials_pkey");

            entity.ToTable("credentials");

            entity.HasIndex(e => e.Username, "username_unique").IsUnique();

            entity.Property(e => e.Userid).HasColumnName("userid");
            entity.Property(e => e.Active).HasDefaultValue(false).HasColumnName("active");
            entity
                .Property(e => e.CreatedTime)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_time");
            entity.Property(e => e.Modified).HasColumnName("modified");
            entity.Property(e => e.PasswordPbkdf2).HasColumnName("password_pbkdf2");
            entity.Property(e => e.Pubkey).HasColumnName("pubkey");
            entity.Property(e => e.Username).HasColumnName("username");
            entity.Property(e => e.ValidUntil).HasColumnName("valid_until");
        });

        modelBuilder.Entity<CustomerDiscount>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("customer_discounts_pkey");

            entity.ToTable("customer_discounts");

            entity.Property(e => e.CustomerId).ValueGeneratedNever().HasColumnName("customer_id");
            entity
                .Property(e => e.LoyaltyPaidToAccountId)
                .HasColumnName("loyalty_paid_to_account_id");
            entity.Property(e => e.LoyaltyRate).HasColumnName("loyalty_rate");
            entity
                .Property(e => e.RecommendedDiscountPercent)
                .HasColumnName("recommended_discount_percent");
        });

        modelBuilder.Entity<DefaultDenyField>(entity =>
        {
            entity.HasKey(e => e.Field).HasName("default_deny_fields_pkey");

            entity.ToTable(
                "default_deny_fields",
                tb => tb.HasComment("Of the form [object].[field] or [field]")
            );

            entity.Property(e => e.Field).HasColumnName("field");
            entity
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("time with time zone")
                .HasColumnName("created_at");
        });

        modelBuilder.Entity<DescriptionsOtherLanguage>(entity =>
        {
            entity.HasNoKey().ToTable("descriptions_other_languages");

            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DescriptionPos).HasColumnName("description_pos");
            entity.Property(e => e.DescriptionWeb).HasColumnName("description_web");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Language).HasMaxLength(5).HasColumnName("language");
        });

        modelBuilder.Entity<I18nLabel>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.Lang }).HasName("i18n_labels_pkey");

            entity.ToTable("i18n_labels");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Lang).HasColumnName("lang");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<Idempotency>(entity =>
        {
            entity.HasKey(e => e.Key).HasName("idempotency_pkey");

            entity.ToTable("idempotency");

            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Request).HasColumnName("request");
            entity
                .Property(e => e.TimeTai)
                .HasDefaultValueSql("now()")
                .HasColumnType("time with time zone")
                .HasColumnName("time_tai");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => new { e.Itemcode, e.Batchcode }).HasName("inventory_pkey");

            entity.ToTable(
                "inventory",
                tb => tb.HasComment("Internal inventory management functions")
            );

            entity.Property(e => e.Itemcode).ValueGeneratedOnAdd().HasColumnName("itemcode");
            entity.Property(e => e.Batchcode).HasColumnName("batchcode");
            entity
                .Property(e => e.BatchEnabled)
                .HasDefaultValue(true)
                .HasColumnName("batch_enabled");
            entity.Property(e => e.CostPrice).HasColumnName("cost_price");
            entity.Property(e => e.ExpDate).HasColumnName("exp_date");
            entity
                .Property(e => e.LastCountedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_counted_at");
            entity.Property(e => e.MarkedPrice).HasColumnName("marked_price");
            entity
                .Property(e => e.MeasurementUnit)
                .HasDefaultValueSql("'qty'::text")
                .HasColumnName("measurement_unit");
            entity.Property(e => e.MfgDate).HasDefaultValueSql("now()").HasColumnName("mfg_date");
            entity.Property(e => e.PackedSize).HasDefaultValueSql("1").HasColumnName("packed_size");
            entity.Property(e => e.Remarks).HasDefaultValueSql("''::text").HasColumnName("remarks");
            entity.Property(e => e.SellingPrice).HasColumnName("selling_price");
            entity.Property(e => e.Suppliercode).HasDefaultValue(0L).HasColumnName("suppliercode");
            entity.Property(e => e.Units).HasColumnName("units");
            entity
                .Property(e => e.UserDiscounts)
                .HasDefaultValue(false)
                .HasColumnName("user_discounts");
            entity
                .Property(e => e.VolumeDiscounts)
                .HasDefaultValue(false)
                .HasColumnName("volume_discounts");
        });

        modelBuilder.Entity<InventoryImage>(entity =>
        {
            entity.HasKey(e => new { e.Imageid, e.Itemcode }).HasName("inventory_images_pkey");

            entity.ToTable("inventory_images");

            entity.Property(e => e.Imageid).HasDefaultValue(0L).HasColumnName("imageid");
            entity.Property(e => e.Itemcode).HasColumnName("itemcode");
            entity.Property(e => e.ImageBase64).HasColumnName("image_base64");
        });

        modelBuilder.Entity<IssuedInvoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("issued_invoices_pkey");

            entity.ToTable("issued_invoices", tb => tb.HasComment("Issued invoices only"));

            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.Customer).HasColumnName("customer");
            entity.Property(e => e.InvoiceHumanFriendly).HasColumnName("invoice_human_friendly");
            entity
                .Property(e => e.InvoiceTime)
                .HasDefaultValueSql("now()")
                .HasColumnName("invoice_time");
            entity
                .Property(e => e.InvoiceTimePosted)
                .HasDefaultValueSql("now()")
                .HasColumnType("time with time zone")
                .HasColumnName("invoice_time_posted");
            entity.Property(e => e.IsPosted).HasDefaultValue(false).HasColumnName("is_posted");
            entity.Property(e => e.IsSettled).HasColumnName("is_settled");
            entity.Property(e => e.IssuedValue).HasColumnName("issued_value");
            entity.Property(e => e.PaidValue).HasColumnName("paid_value");
        });

        modelBuilder.Entity<LoyaltyPoint>(entity =>
        {
            entity.HasKey(e => e.PointsId).HasName("loyality_points_pkey");

            entity.ToTable("loyalty_points");

            entity
                .Property(e => e.PointsId)
                .HasDefaultValueSql("nextval('loyality_points_points_id_seq'::regclass)")
                .HasColumnName("points_id");
            entity.Property(e => e.CustId).HasColumnName("cust_id");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity
                .Property(e => e.ValidFrom)
                .HasDefaultValueSql("now()")
                .HasColumnName("valid_from");
            entity.Property(e => e.ValidUntil).HasColumnName("valid_until");
        });

        modelBuilder.Entity<LoyaltyPointsRedemption>(entity =>
        {
            entity.HasKey(e => e.RedemptionId).HasName("loyalty_points_redemption_pkey");

            entity.ToTable("loyalty_points_redemption");

            entity.Property(e => e.RedemptionId).HasColumnName("redemption_id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.CustId).HasColumnName("cust_id");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity
                .Property(e => e.TimeIssued)
                .HasDefaultValueSql("now()")
                .HasColumnType("time with time zone")
                .HasColumnName("time_issued");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotifId).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.Property(e => e.NotifId).HasColumnName("notif_id");
            entity
                .Property(e => e.NotifContents)
                .HasDefaultValueSql("''::text")
                .HasColumnName("notif_contents");
            entity
                .Property(e => e.NotifFrom)
                .HasDefaultValueSql("'InvoicerBackend'::text")
                .HasColumnName("notif_from");
            entity
                .Property(e => e.NotifIsDone)
                .HasDefaultValue(false)
                .HasColumnName("notif_is_done");
            entity.Property(e => e.NotifOtherStatus).HasColumnName("notif_other_status");
            entity.Property(e => e.NotifPriority).HasColumnName("notif_priority");
            entity
                .Property(e => e.NotifSource)
                .HasDefaultValueSql("'InvoicerBackend'::text")
                .HasColumnName("notif_source");
            entity.Property(e => e.NotifTarget).HasColumnName("notif_target");
            entity
                .Property(e => e.NotifType)
                .HasDefaultValueSql("'INTERNAL'::text")
                .HasColumnName("notif_type");
            entity.Property(e => e.TimeExpiresTai).HasColumnName("time_expires_tai");
            entity.Property(e => e.TimeTai).HasColumnName("time_tai");
        });

        modelBuilder.Entity<NotificationServicerType>(entity =>
        {
            entity
                .HasKey(e => e.NotificationServicerTypeId)
                .HasName("notification_servicer_types_pkey");

            entity.ToTable("notification_servicer_types");

            entity
                .Property(e => e.NotificationServicerTypeId)
                .HasColumnName("notification_servicer_type_id");
            entity
                .Property(e => e.NotificationServicerName)
                .HasColumnName("notification_servicer_name");
        });

        modelBuilder.Entity<NotificationType>(entity =>
        {
            entity.HasKey(e => e.NotificationTypeId).HasName("notification_types_pkey");

            entity.ToTable("notification_types");

            entity.Property(e => e.NotificationTypeId).HasColumnName("notification_type_id");
            entity.Property(e => e.NotificationService).HasColumnName("notification_service");
            entity
                .Property(e => e.NotificationServiceOtherArgs)
                .HasDefaultValueSql("''::text")
                .HasColumnName("notification_service_other_args");
            entity
                .Property(e => e.NotificationServicerType)
                .HasColumnName("notification_servicer_type");
            entity.Property(e => e.NotificationTypeName).HasColumnName("notification_type_name");
        });

        modelBuilder.Entity<PermissionsExtendedApiCall>(entity =>
        {
            entity
                .HasKey(e => new { e.UserId, e.ApiCall })
                .HasName("permissions_extended_api_call_pkey");

            entity.ToTable("permissions_extended_api_call");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ApiCall).HasColumnName("api_call");
            entity.Property(e => e.AllowedAttributes).HasColumnName("allowed_attributes");
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

            entity.Property(e => e.Category).ValueGeneratedNever().HasColumnName("category");
            entity.Property(e => e.CategoryName).HasColumnName("category_name");
            entity.Property(e => e.LabelI18n).HasColumnName("label_i18n");
        });

        modelBuilder.Entity<PermissionsListUsersCategory>(entity =>
        {
            entity.HasKey(e => e.Userid).HasName("permissions_list_users_categories_pkey");

            entity.ToTable("permissions_list_users_categories");

            entity.Property(e => e.Userid).ValueGeneratedNever().HasColumnName("userid");
            entity.Property(e => e.Categories).HasDefaultValue(0L).HasColumnName("categories");
        });

        modelBuilder.Entity<Pii>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pii_pkey");

            entity.ToTable("pii");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Fax).HasColumnName("fax");
            entity
                .Property(e => e.Gender)
                .HasDefaultValueSql("'unspecified'::text")
                .HasColumnName("gender");
            entity.Property(e => e.Im).HasColumnName("IM");
            entity.Property(e => e.IsCompany).HasDefaultValue(false).HasColumnName("is_company");
            entity.Property(e => e.Mobile).HasColumnName("mobile");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Sip).HasColumnName("SIP");
            entity.Property(e => e.Telephone).HasColumnName("telephone");
            entity.Property(e => e.Title).HasColumnName("title");
        });

        modelBuilder.Entity<PiiImage>(entity =>
        {
            entity.HasKey(e => new { e.PiiId, e.ImageNo }).HasName("pii_images_pkey");

            entity.ToTable(
                "pii_images",
                tb =>
                    tb.HasComment(
                        "Photos of people, companies - any person, including non-natural persons."
                    )
            );

            entity.Property(e => e.PiiId).HasColumnName("pii_id");
            entity.Property(e => e.ImageNo).HasDefaultValue(0L).HasColumnName("image_no");
            entity.Property(e => e.Image).HasColumnName("image");
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasNoKey().ToTable("purchases");

            entity
                .Property(e => e.AddedDate)
                .HasDefaultValueSql("now()")
                .HasColumnType("time with time zone")
                .HasColumnName("added_date");
            entity.Property(e => e.CostPerPack).HasColumnName("cost_per_pack");
            entity.Property(e => e.CostPerUnit).HasColumnName("cost_per_unit");
            entity.Property(e => e.DiscountAbsolute).HasColumnName("discount_absolute");
            entity.Property(e => e.DiscountPercentage).HasColumnName("discount_percentage");
            entity
                .Property(e => e.ExpiryDate)
                .HasColumnType("time with time zone")
                .HasColumnName("expiry_date");
            entity.Property(e => e.FreePacks).HasDefaultValue(0L).HasColumnName("free_packs");
            entity.Property(e => e.FreeUnits).HasColumnName("free_units");
            entity.Property(e => e.GrossCostPerUnit).HasColumnName("gross_cost_per_unit");
            entity.Property(e => e.GrossProfitAbsolute).HasColumnName("gross_profit_absolute");
            entity.Property(e => e.GrossProfitPercentage).HasColumnName("gross_profit_percentage");
            entity.Property(e => e.Itemcode).HasColumnName("itemcode");
            entity
                .Property(e => e.ManufacturerBatchId)
                .HasDefaultValueSql("''::text")
                .HasColumnName("manufacturer_batch_id");
            entity
                .Property(e => e.ManufacturingDate)
                .HasColumnType("time with time zone")
                .HasColumnName("manufacturing_date");
            entity.Property(e => e.NetTotalPrice).HasColumnName("net_total_price");
            entity.Property(e => e.PackQuantity).HasDefaultValue(0L).HasColumnName("pack_quantity");
            entity.Property(e => e.PackSize).HasDefaultValue(0L).HasColumnName("pack_size");
            entity
                .Property(e => e.ProductName)
                .HasDefaultValueSql("''::text")
                .HasColumnName("product_name");
            entity
                .Property(e => e.ReceivedAsUnitQuantity)
                .HasColumnName("received_as_unit_quantity");
            entity.Property(e => e.ReceivedInvoiceId).HasColumnName("received_invoice_id");
            entity.Property(e => e.SellingPrice).HasColumnName("selling_price");
            entity.Property(e => e.TotalAmountDue).HasColumnName("total_amount_due");
            entity.Property(e => e.TotalUnits).HasColumnName("total_units");
            entity.Property(e => e.VatAbsolute).HasColumnName("VAT_absolute");
            entity.Property(e => e.VatCategory).HasDefaultValue(0L).HasColumnName("VAT_category");
            entity
                .Property(e => e.VatCategoryName)
                .HasDefaultValueSql("''::text")
                .HasColumnName("VAT_category_name");
            entity.Property(e => e.VatPercentage).HasColumnName("VAT_percentage");
        });

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId).HasName("receipts_pkey");

            entity.ToTable("receipts");

            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity
                .Property(e => e.TimeReceived)
                .HasDefaultValueSql("now()")
                .HasColumnType("time with time zone")
                .HasColumnName("time_received");
        });

        modelBuilder.Entity<ReceivedInvoice>(entity =>
        {
            entity.HasKey(e => e.ReceivedInvoiceNo).HasName("received_invoices_pkey");

            entity.ToTable("received_invoices");

            entity.Property(e => e.ReceivedInvoiceNo).HasColumnName("received_invoice_no");
            entity
                .Property(e => e.DefaultVatCategory)
                .HasDefaultValue(0L)
                .HasColumnName("default_VAT_category");
            entity
                .Property(e => e.DefaultVatCategoryName)
                .HasDefaultValueSql("''::text")
                .HasColumnName("default_VAT_category_name");
            entity.Property(e => e.DefaultVatPercentage).HasColumnName("default_VAT_percentage");
            entity.Property(e => e.Discount).HasColumnName("discount");
            entity.Property(e => e.DiscountPercentage).HasColumnName("discount_percentage");
            entity.Property(e => e.GrossTotal).HasColumnName("gross_total");
            entity.Property(e => e.IsPosted).HasDefaultValue(false).HasColumnName("is_posted");
            entity.Property(e => e.IsSettled).HasDefaultValue(false).HasColumnName("is_settled");
            entity.Property(e => e.Reference).HasColumnName("reference");
            entity.Property(e => e.Remarks).HasDefaultValueSql("''::text").HasColumnName("remarks");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity
                .Property(e => e.SupplierName)
                .HasDefaultValueSql("''::text")
                .HasColumnName("supplier_name");
            entity.Property(e => e.TransportCharges).HasColumnName("transport_charges");
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasNoKey().ToTable("requests");

            entity.Property(e => e.Endpoint).HasColumnName("endpoint");
            entity.Property(e => e.Principal).HasColumnName("principal");
            entity
                .Property(e => e.ProvidedPrivilegeLevels)
                .HasColumnName("provided_privilege_levels");
            entity.Property(e => e.RequestBody).HasColumnName("request_body");
            entity.Property(e => e.RequestedAction).HasColumnName("requested_action");
            entity
                .Property(e => e.RequestedPrivilegeLevel)
                .HasColumnName("requested_privilege_level");
            entity
                .Property(e => e.TimeTai)
                .HasDefaultValueSql("now()")
                .HasColumnType("time with time zone")
                .HasColumnName("time_tai");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.Type).HasColumnName("type");
        });

        modelBuilder.Entity<RequestsBad>(entity =>
        {
            entity.HasNoKey().ToTable("requests_bad");

            entity.Property(e => e.Endpoint).HasColumnName("endpoint");
            entity.Property(e => e.Principal).HasColumnName("principal");
            entity
                .Property(e => e.ProvidedPrivilegeLevels)
                .HasColumnName("provided_privilege_levels");
            entity.Property(e => e.RequestBody).HasColumnName("request_body");
            entity.Property(e => e.RequestedAction).HasColumnName("requested_action");
            entity
                .Property(e => e.RequestedPrivilegeLevel)
                .HasColumnName("requested_privilege_level");
            entity.Property(e => e.TimeTai).HasDefaultValueSql("now()").HasColumnName("time_tai");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.Type).HasColumnName("type");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.SaleId).HasName("sales_pkey");

            entity.ToTable("sales", tb => tb.HasComment("Sales data go here"));

            entity.Property(e => e.SaleId).HasColumnName("sale_id");
            entity.Property(e => e.Batchcode).HasColumnName("batchcode");
            entity
                .Property(e => e.ClientRecordedTimeClosing)
                .HasDefaultValueSql("now()")
                .HasColumnName("client_recorded_time_closing");
            entity
                .Property(e => e.ClientRecordedTimeOpening)
                .HasDefaultValueSql("now()")
                .HasColumnName("client_recorded_time_opening");
            entity.Property(e => e.Discount).HasColumnName("discount");
            entity.Property(e => e.DiscountRate).HasColumnName("discount_rate");
            entity
                .Property(e => e.EnteredAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("entered_at");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.Itemcode).HasColumnName("itemcode");
            entity.Property(e => e.LoyalityPointsIssued).HasColumnName("loyality_points_issued");
            entity
                .Property(e => e.LoyalityPointsPercentage)
                .HasColumnName("loyality_points_percentage");
            entity
                .Property(e => e.ProductName)
                .HasDefaultValueSql("''::text")
                .HasColumnName("product_name");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.SalesHumanFriendly).HasColumnName("sales_human_friendly");
            entity.Property(e => e.SellingPrice).HasColumnName("selling_price");
            entity
                .Property(e => e.TotalEffectiveSellingPrice)
                .HasColumnName("total_effective_selling_price");
            entity.Property(e => e.VatAsCharged).HasColumnName("vat_as_charged");
            entity.Property(e => e.VatCategory).HasColumnName("vat_category");
            entity.Property(e => e.VatRatePercentage).HasColumnName("vat_rate_percentage");
        });

        modelBuilder.Entity<Sih>(entity =>
        {
            entity.HasKey(e => e.Itemcode).HasName("sih_pkey");

            entity.ToTable("sih", "imported_dummy");

            entity.Property(e => e.Itemcode).ValueGeneratedNever().HasColumnName("itemcode");
            entity.Property(e => e.Cost).HasColumnName("cost");
            entity.Property(e => e.Desc).HasColumnName("desc");
            entity.Property(e => e.Sell).HasColumnName("sell");
            entity.Property(e => e.Sih1).HasColumnName("sih");
        });

        modelBuilder.Entity<SihCurrent>(entity =>
        {
            entity.HasKey(e => e.Itemcode).HasName("sih_current_pkey");

            entity.ToTable("sih_current", "imported_dummy");

            entity.Property(e => e.Itemcode).ValueGeneratedNever().HasColumnName("itemcode");
            entity.Property(e => e.Cost).HasColumnName("cost");
            entity.Property(e => e.Desc).HasColumnName("desc");
            entity.Property(e => e.Sell).HasColumnName("sell");
            entity.Property(e => e.Sih).HasColumnName("sih");
        });

        modelBuilder.Entity<SuggestedPrice>(entity =>
        {
            entity.HasKey(e => new { e.Itemcode, e.Price }).HasName("suggested_prices_pkey");

            entity.ToTable("suggested_prices");

            entity.Property(e => e.Itemcode).HasColumnName("itemcode");
            entity.Property(e => e.Price).HasColumnName("price");
        });

        modelBuilder.Entity<TieredDiscount>(entity =>
        {
            entity.HasNoKey().ToTable("tiered_discounts");

            entity.Property(e => e.DiscountPercentage).HasColumnName("discount_percentage");
            entity.Property(e => e.Itemcode).HasColumnName("itemcode");
            entity.Property(e => e.MinQty).HasColumnName("min_qty");
        });

        modelBuilder.Entity<Token>(entity =>
        {
            entity.HasKey(e => e.Tokenid).HasName("tokens_pkey");

            entity.ToTable("tokens");

            entity
                .Property(e => e.Tokenid)
                .HasDefaultValueSql("(random())::text")
                .HasColumnName("tokenid");
            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity
                .Property(e => e.CategoriesBitmask)
                .HasDefaultValue(0L)
                .HasColumnName("categories_bitmask");
            entity.Property(e => e.NotValidAfter).HasColumnName("not_valid_after");
            entity
                .Property(e => e.Privileges)
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

            entity.ToTable(
                "user_authorization",
                tb => tb.HasComment("user_cap: Comma-separated\nuser_default_cap: Comma-separated")
            );

            entity.Property(e => e.Userid).HasColumnName("userid");
            entity
                .Property(e => e.CheckExtendedAuthorization)
                .HasDefaultValue(false)
                .HasColumnName("check_extended_authorization");
            entity.Property(e => e.UserCap).HasColumnName("user_cap");
            entity
                .Property(e => e.UserDefaultCap)
                .HasDefaultValueSql("''::text")
                .HasColumnName("user_default_cap");
        });

        modelBuilder.Entity<UsersFieldLevelAccessControlsDenyList>(entity =>
        {
            entity.HasNoKey().ToTable("users_field_level_access_controls_deny_list");

            entity.Property(e => e.DeniedField).HasColumnName("denied_field");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<VatCategory>(entity =>
        {
            entity.HasNoKey().ToTable("vat_categories");

            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity
                .Property(e => e.VatCategoryId)
                .ValueGeneratedOnAdd()
                .HasColumnName("vat_category_id");
            entity.Property(e => e.VatName).HasColumnName("vat_name");
            entity.Property(e => e.VatPercentage).HasColumnName("vat_percentage");
        });

        modelBuilder.Entity<VolumeDiscount>(entity =>
        {
            entity.HasNoKey().ToTable("volume_discounts");

            entity.Property(e => e.DiscountPerUnit).HasColumnName("discount_per_unit");
            entity.Property(e => e.Itemcode).HasColumnName("itemcode");
            entity.Property(e => e.StartFrom).HasDefaultValue(1L).HasColumnName("start_from");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
