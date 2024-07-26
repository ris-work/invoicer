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

    public virtual DbSet<Credential> Credentials { get; set; }

    public virtual DbSet<Token> Tokens { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAuthorization> UserAuthorizations { get; set; }

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

        modelBuilder.Entity<Credential>(entity =>
        {
            entity.HasKey(e => e.Userid).HasName("credentials_pkey");

            entity.ToTable("credentials");

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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
