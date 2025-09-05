using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Entities;

namespace Backend.Utils.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Categoria> Categoria { get; set; }

    public virtual DbSet<Marca> Marca { get; set; }

    public virtual DbSet<SystemConfig> SystemConfig { get; set; }

    public virtual DbSet<SystemConfigValues> SystemConfigValues { get; set; }

    public virtual DbSet<SystemOrganization> SystemOrganization { get; set; }

    public virtual DbSet<SystemPermissions> SystemPermissions { get; set; }

    public virtual DbSet<SystemRoles> SystemRoles { get; set; }

    public virtual DbSet<SystemRolesPermissions> SystemRolesPermissions { get; set; }

    public virtual DbSet<SystemUsers> SystemUsers { get; set; }

    public virtual DbSet<SystemUsersPermissions> SystemUsersPermissions { get; set; }

    public virtual DbSet<SystemUsersRoles> SystemUsersRoles { get; set; }

    public virtual DbSet<ZToken> ZToken { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__categori__3214EC074EB48FFE");

            entity.ToTable("categoria");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");

            entity.HasOne(d => d.Creador).WithMany(p => p.CategoriaCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_categoria_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.CategoriaModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_categoria_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.Categoria)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_categoria_OrganizationId");
        });

        modelBuilder.Entity<Marca>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__marca__3214EC07117A2B75");

            entity.ToTable("marca");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.CodigoInterno)
                .HasMaxLength(50)
                .HasColumnName("codigo_interno");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");

            entity.HasOne(d => d.Creador).WithMany(p => p.MarcaCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_marca_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.MarcaModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_marca_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.Marca)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_marca_OrganizationId");
        });

        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_c__3214EC079F2EF73E");

            entity.ToTable("system_config");

            entity.HasIndex(e => e.Active, "IX_system_config_Active");

            entity.HasIndex(e => e.FechaCreacion, "IX_system_config_FechaCreacion");

            entity.HasIndex(e => e.Field, "IX_system_config_Field");

            entity.HasIndex(e => e.OrganizationId, "IX_system_config_OrganizationId");

            entity.HasIndex(e => e.TypeField, "IX_system_config_TypeField");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Field).HasMaxLength(255);
            entity.Property(e => e.TypeField).HasMaxLength(255);

            entity.HasOne(d => d.Creador).WithMany(p => p.SystemConfigCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_system_config_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.SystemConfigModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_system_config_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.SystemConfig)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_system_config_OrganizationId");
        });

        modelBuilder.Entity<SystemConfigValues>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_c__3214EC074D09BA28");

            entity.ToTable("system_config_values");

            entity.HasIndex(e => e.Active, "IX_system_config_values_Active");

            entity.HasIndex(e => e.FechaCreacion, "IX_system_config_values_FechaCreacion");

            entity.HasIndex(e => e.OrganizationId, "IX_system_config_values_OrganizationId");

            entity.HasIndex(e => e.SystemConfigId, "IX_system_config_values_SystemConfigId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Creador).WithMany(p => p.SystemConfigValuesCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_system_config_values_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.SystemConfigValuesModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_system_config_values_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.SystemConfigValues)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_system_config_values_OrganizationId");

            entity.HasOne(d => d.SystemConfig).WithMany(p => p.SystemConfigValues)
                .HasForeignKey(d => d.SystemConfigId)
                .HasConstraintName("FK_system_config_values_SystemConfigId");
        });

        modelBuilder.Entity<SystemOrganization>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_o__3214EC07309563DA");

            entity.ToTable("system_organization");

            entity.HasIndex(e => e.Active, "IX_system_organization_Active");

            entity.HasIndex(e => e.FechaCreacion, "IX_system_organization_FechaCreacion");

            entity.HasIndex(e => e.Nombre, "IX_system_organization_Nombre");

            entity.HasIndex(e => e.Rut, "IX_system_organization_Rut");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Nombre).HasMaxLength(255);
            entity.Property(e => e.Rut).HasMaxLength(50);
        });

        modelBuilder.Entity<SystemPermissions>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_p__3214EC07F5E16A5C");

            entity.ToTable("system_permissions");

            entity.HasIndex(e => e.Active, "IX_system_permissions_Active");

            entity.HasIndex(e => e.FechaCreacion, "IX_system_permissions_FechaCreacion");

            entity.HasIndex(e => e.Nombre, "IX_system_permissions_Nombre");

            entity.HasIndex(e => e.OrganizationId, "IX_system_permissions_OrganizationId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ActionKey)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.GroupKey)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.GrupoNombre)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Nombre).HasMaxLength(255);

            entity.HasOne(d => d.Creador).WithMany(p => p.SystemPermissionsCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_system_permissions_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.SystemPermissionsModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_system_permissions_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.SystemPermissions)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_system_permissions_OrganizationId");
        });

        modelBuilder.Entity<SystemRoles>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_r__3214EC0797BFD949");

            entity.ToTable("system_roles");

            entity.HasIndex(e => e.Active, "IX_system_roles_Active");

            entity.HasIndex(e => e.FechaCreacion, "IX_system_roles_FechaCreacion");

            entity.HasIndex(e => e.Nombre, "IX_system_roles_Nombre");

            entity.HasIndex(e => e.OrganizationId, "IX_system_roles_OrganizationId");

            entity.HasIndex(e => e.TypeRole, "IX_system_roles_TypeRole");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Nombre).HasMaxLength(255);
            entity.Property(e => e.TypeRole)
                .HasMaxLength(100)
                .HasDefaultValue("Access");

            entity.HasOne(d => d.Creador).WithMany(p => p.SystemRolesCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_system_roles_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.SystemRolesModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_system_roles_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.SystemRoles)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_system_roles_OrganizationId");
        });

        modelBuilder.Entity<SystemRolesPermissions>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_r__3214EC07194EC8EF");

            entity.ToTable("system_roles_permissions");

            entity.HasIndex(e => e.Active, "IX_system_roles_permissions_Active");

            entity.HasIndex(e => e.OrganizationId, "IX_system_roles_permissions_OrganizationId");

            entity.HasIndex(e => e.SystemPermissionsId, "IX_system_roles_permissions_PermissionId");

            entity.HasIndex(e => e.SystemRolesId, "IX_system_roles_permissions_RoleId");

            entity.HasIndex(e => new { e.SystemRolesId, e.SystemPermissionsId }, "UK_system_roles_permissions_RolePermission").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.SystemPermissionsId).HasColumnName("system_permissions_id");
            entity.Property(e => e.SystemRolesId).HasColumnName("system_roles_id");

            entity.HasOne(d => d.Creador).WithMany(p => p.SystemRolesPermissionsCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_system_roles_permissions_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.SystemRolesPermissionsModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_system_roles_permissions_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.SystemRolesPermissions)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_system_roles_permissions_OrganizationId");

            entity.HasOne(d => d.SystemPermissions).WithMany(p => p.SystemRolesPermissions)
                .HasForeignKey(d => d.SystemPermissionsId)
                .HasConstraintName("FK_system_roles_permissions_PermissionId");

            entity.HasOne(d => d.SystemRoles).WithMany(p => p.SystemRolesPermissions)
                .HasForeignKey(d => d.SystemRolesId)
                .HasConstraintName("FK_system_roles_permissions_RoleId");
        });

        modelBuilder.Entity<SystemUsers>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_u__3214EC07791C48C0");

            entity.ToTable("system_users");

            entity.HasIndex(e => e.Active, "IX_system_users_Active");

            entity.HasIndex(e => e.Email, "IX_system_users_Email");

            entity.HasIndex(e => e.FechaCreacion, "IX_system_users_FechaCreacion");

            entity.HasIndex(e => e.Nombre, "IX_system_users_Nombre");

            entity.HasIndex(e => e.OrganizationId, "IX_system_users_OrganizationId");

            entity.HasIndex(e => e.Email, "UQ__system_u__A9D10534ED32FE68").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Nombre).HasMaxLength(255);
            entity.Property(e => e.Password).HasMaxLength(255);

            entity.HasOne(d => d.Creador).WithMany(p => p.InverseCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_system_users_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.InverseModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_system_users_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.SystemUsers)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_system_users_OrganizationId");
        });

        modelBuilder.Entity<SystemUsersPermissions>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_u__3214EC07F445A514");

            entity.ToTable("system_users_permissions");

            entity.HasIndex(e => e.Active, "IX_system_users_permissions_Active");

            entity.HasIndex(e => e.OrganizationId, "IX_system_users_permissions_OrganizationId");

            entity.HasIndex(e => e.SystemPermissionsId, "IX_system_users_permissions_PermissionId");

            entity.HasIndex(e => e.SystemUsersId, "IX_system_users_permissions_UserId");

            entity.HasIndex(e => new { e.SystemUsersId, e.SystemPermissionsId }, "UK_system_users_permissions_UserPermission").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.SystemPermissionsId).HasColumnName("system_permissions_id");
            entity.Property(e => e.SystemUsersId).HasColumnName("system_users_id");

            entity.HasOne(d => d.Creador).WithMany(p => p.SystemUsersPermissionsCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_system_users_permissions_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.SystemUsersPermissionsModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_system_users_permissions_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.SystemUsersPermissions)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_system_users_permissions_OrganizationId");

            entity.HasOne(d => d.SystemPermissions).WithMany(p => p.SystemUsersPermissions)
                .HasForeignKey(d => d.SystemPermissionsId)
                .HasConstraintName("FK_system_users_permissions_PermissionId");

            entity.HasOne(d => d.SystemUsers).WithMany(p => p.SystemUsersPermissionsSystemUsers)
                .HasForeignKey(d => d.SystemUsersId)
                .HasConstraintName("FK_system_users_permissions_UserId");
        });

        modelBuilder.Entity<SystemUsersRoles>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_u__3214EC07EA0FD9CD");

            entity.ToTable("system_users_roles");

            entity.HasIndex(e => e.Active, "IX_system_users_roles_Active");

            entity.HasIndex(e => e.OrganizationId, "IX_system_users_roles_OrganizationId");

            entity.HasIndex(e => e.SystemRolesId, "IX_system_users_roles_RoleId");

            entity.HasIndex(e => e.SystemUsersId, "IX_system_users_roles_UserId");

            entity.HasIndex(e => new { e.SystemUsersId, e.SystemRolesId }, "UK_system_users_roles_UserRole").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.SystemRolesId).HasColumnName("system_roles_id");
            entity.Property(e => e.SystemUsersId).HasColumnName("system_users_id");

            entity.HasOne(d => d.Creador).WithMany(p => p.SystemUsersRolesCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_system_users_roles_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.SystemUsersRolesModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_system_users_roles_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.SystemUsersRoles)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_system_users_roles_OrganizationId");

            entity.HasOne(d => d.SystemRoles).WithMany(p => p.SystemUsersRoles)
                .HasForeignKey(d => d.SystemRolesId)
                .HasConstraintName("FK_system_users_roles_RoleId");

            entity.HasOne(d => d.SystemUsers).WithMany(p => p.SystemUsersRolesSystemUsers)
                .HasForeignKey(d => d.SystemUsersId)
                .HasConstraintName("FK_system_users_roles_UserId");
        });

        modelBuilder.Entity<ZToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__z_token__3213E83F3E7F66CC");

            entity.ToTable("z_token");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Data)
                .IsUnicode(false)
                .HasColumnName("data");
            entity.Property(e => e.Logout).HasColumnName("logout");
            entity.Property(e => e.Organizationid).HasColumnName("organizationid");
            entity.Property(e => e.Refresh).HasColumnName("refresh");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
