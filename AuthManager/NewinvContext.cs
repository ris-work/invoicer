using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AuthManager;

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

    public virtual DbSet<TokenAuthorization> TokenAuthorizations { get; set; }

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
            entity.Property(e => e.Created)
                .HasColumnType("time with time zone")
                .HasColumnName("created");
            entity.Property(e => e.Modified).HasColumnName("modified");
            entity.Property(e => e.PasswordPbkdf2)
                .IsRequired()
                .HasColumnName("password_pbkdf2");
            entity.Property(e => e.Pubkey).HasColumnName("pubkey");
            entity.Property(e => e.Username)
                .IsRequired()
                .HasColumnName("username");
            entity.Property(e => e.ValidUntil).HasColumnName("valid_until");
        });

        modelBuilder.Entity<Token>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tokens");

            entity.Property(e => e.Tokenid).HasColumnName("tokenid");
            entity.Property(e => e.Tokensecret)
                .IsRequired()
                .HasColumnName("tokensecret");
            entity.Property(e => e.Tokenvalue)
                .IsRequired()
                .HasColumnName("tokenvalue");
            entity.Property(e => e.Userid).HasColumnName("userid");
        });

        modelBuilder.Entity<TokenAuthorization>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("token_authorization");

            entity.Property(e => e.Cap)
                .IsRequired()
                .HasColumnName("cap");
            entity.Property(e => e.Tokenid).HasColumnName("tokenid");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
