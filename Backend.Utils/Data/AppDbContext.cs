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

    public virtual DbSet<Region> Region { get; set; }

    public virtual DbSet<SystemAuditoria> SystemAuditoria { get; set; }

    public virtual DbSet<SystemAuditoriaDetalle> SystemAuditoriaDetalle { get; set; }

    public virtual DbSet<SystemCamposAuditables> SystemCamposAuditables { get; set; }

    public virtual DbSet<SystemConfig> SystemConfig { get; set; }

    public virtual DbSet<SystemConfigValues> SystemConfigValues { get; set; }

    public virtual DbSet<SystemCustomFieldAuditLog> SystemCustomFieldAuditLog { get; set; }

    public virtual DbSet<SystemCustomFieldDefinitions> SystemCustomFieldDefinitions { get; set; }

    public virtual DbSet<SystemCustomFieldTemplates> SystemCustomFieldTemplates { get; set; }

    public virtual DbSet<SystemFormEntities> SystemFormEntities { get; set; }

    public virtual DbSet<SystemOrganization> SystemOrganization { get; set; }

    public virtual DbSet<SystemPermissions> SystemPermissions { get; set; }

    public virtual DbSet<SystemRoles> SystemRoles { get; set; }

    public virtual DbSet<SystemRolesPermissions> SystemRolesPermissions { get; set; }

    public virtual DbSet<SystemTablasAuditables> SystemTablasAuditables { get; set; }

    public virtual DbSet<SystemUsers> SystemUsers { get; set; }

    public virtual DbSet<SystemUsersPermissions> SystemUsersPermissions { get; set; }

    public virtual DbSet<SystemUsersRoles> SystemUsersRoles { get; set; }

    public virtual DbSet<VwAuditoriaCompleta> VwAuditoriaCompleta { get; set; }

    public virtual DbSet<ZToken> ZToken { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Region>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__region__3214EC0768C38155");

            entity.ToTable("region", tb => tb.HasComment("Core.Localidades"));

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");

            entity.HasOne(d => d.Creador).WithMany(p => p.RegionCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_region_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.RegionModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_region_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.Region)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_region_OrganizationId");
        });

        modelBuilder.Entity<SystemAuditoria>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_a__3214EC073D80A7F7");

            entity.ToTable("system_auditoria");

            entity.HasIndex(e => e.Action, "IX_system_auditoria_Action");

            entity.HasIndex(e => e.FechaCreacion, "IX_system_auditoria_FechaCreacion");

            entity.HasIndex(e => e.OrganizationId, "IX_system_auditoria_OrganizationId");

            entity.HasIndex(e => e.RegistroId, "IX_system_auditoria_RegistroId");

            entity.HasIndex(e => e.Tabla, "IX_system_auditoria_Tabla");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Comentario).HasMaxLength(1000);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Tabla).HasMaxLength(255);

            entity.HasOne(d => d.Creador).WithMany(p => p.SystemAuditoriaCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_system_auditoria_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.SystemAuditoriaModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_system_auditoria_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.SystemAuditoria)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_system_auditoria_OrganizationId");
        });

        modelBuilder.Entity<SystemAuditoriaDetalle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_a__3214EC078F2AA1E8");

            entity.ToTable("system_auditoria_detalle");

            entity.HasIndex(e => e.AuditoriaId, "IX_system_auditoria_detalle_AuditoriaId");

            entity.HasIndex(e => e.Campo, "IX_system_auditoria_detalle_Campo");

            entity.HasIndex(e => e.FechaCreacion, "IX_system_auditoria_detalle_FechaCreacion");

            entity.HasIndex(e => e.OrganizationId, "IX_system_auditoria_detalle_OrganizationId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Campo).HasMaxLength(255);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Auditoria).WithMany(p => p.SystemAuditoriaDetalle)
                .HasForeignKey(d => d.AuditoriaId)
                .HasConstraintName("FK_system_auditoria_detalle_AuditoriaId");

            entity.HasOne(d => d.Creador).WithMany(p => p.SystemAuditoriaDetalleCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_system_auditoria_detalle_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.SystemAuditoriaDetalleModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_system_auditoria_detalle_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.SystemAuditoriaDetalle)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_system_auditoria_detalle_OrganizationId");
        });

        modelBuilder.Entity<SystemCamposAuditables>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_c__3213E83F85BACA2B");

            entity.ToTable("system_campos_auditables");

            entity.HasIndex(e => new { e.Tabla, e.Activo }, "IX_system_campos_auditables_tabla_activo").HasFilter("([activo]=(1))");

            entity.HasIndex(e => new { e.OrganizacionId, e.Tabla, e.Campo }, "UQ__system_c__18C6AACB92F5F062").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Campo)
                .HasMaxLength(200)
                .HasColumnName("campo");
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.IsCustom)
                .HasDefaultValue(false)
                .HasColumnName("is_custom");
            entity.Property(e => e.OrganizacionId).HasColumnName("organizacion_id");
            entity.Property(e => e.Tabla)
                .HasMaxLength(100)
                .HasColumnName("tabla");
        });

        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_c__3214EC07E3CD3930");

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
        });

        modelBuilder.Entity<SystemConfigValues>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_c__3214EC072C31AD9D");

            entity.ToTable("system_config_values");

            entity.HasIndex(e => e.Active, "IX_system_config_values_Active");

            entity.HasIndex(e => e.FechaCreacion, "IX_system_config_values_FechaCreacion");

            entity.HasIndex(e => e.OrganizationId, "IX_system_config_values_OrganizationId");

            entity.HasIndex(e => e.SystemConfigId, "IX_system_config_values_SystemConfigId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.SystemConfig).WithMany(p => p.SystemConfigValues)
                .HasForeignKey(d => d.SystemConfigId)
                .HasConstraintName("FK_system_config_values_SystemConfigId");
        });

        modelBuilder.Entity<SystemCustomFieldAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_c__3214EC07BB842A91");

            entity.ToTable("system_custom_field_audit_log");

            entity.HasIndex(e => new { e.CustomFieldDefinitionId, e.FechaCreacion }, "IX_system_custom_field_audit_log_definition").IsDescending(false, true);

            entity.HasIndex(e => new { e.EntityName, e.OrganizationId, e.FechaCreacion }, "IX_system_custom_field_audit_log_entity_date").IsDescending(false, false, true);

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ChangeReason).HasMaxLength(500);
            entity.Property(e => e.ChangeType).HasMaxLength(50);
            entity.Property(e => e.ChangedProperties).HasMaxLength(1000);
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FieldName).HasMaxLength(100);

            entity.HasOne(d => d.Creador).WithMany(p => p.SystemCustomFieldAuditLog)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_system_custom_field_audit_CreadorId");

            entity.HasOne(d => d.CustomFieldDefinition).WithMany(p => p.SystemCustomFieldAuditLog)
                .HasForeignKey(d => d.CustomFieldDefinitionId)
                .HasConstraintName("FK_system_custom_field_audit_definition");

            entity.HasOne(d => d.Organization).WithMany(p => p.SystemCustomFieldAuditLog)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_system_custom_field_audit_OrganizationId");
        });

        modelBuilder.Entity<SystemCustomFieldDefinitions>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_c__3214EC07A3F6A09E");

            entity.ToTable("system_custom_field_definitions");

            entity.HasIndex(e => new { e.EntityName, e.OrganizationId, e.Active, e.IsEnabled }, "IX_system_custom_field_definitions_entity_org_active");

            entity.HasIndex(e => new { e.FieldName, e.EntityName, e.OrganizationId }, "IX_system_custom_field_definitions_field_name").HasFilter("([Active]=(1) AND [IsEnabled]=(1))");

            entity.HasIndex(e => new { e.EntityName, e.OrganizationId, e.SortOrder, e.Active }, "IX_system_custom_field_definitions_sort_order");

            entity.HasIndex(e => new { e.EntityName, e.FieldName, e.OrganizationId }, "UQ_system_custom_field_definitions_entity_field_org").IsUnique();

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DisplayName).HasMaxLength(255);
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FieldName).HasMaxLength(100);
            entity.Property(e => e.FieldType)
                .HasMaxLength(50)
                .HasDefaultValue("text");
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.PermissionCreate).HasMaxLength(255);
            entity.Property(e => e.PermissionUpdate).HasMaxLength(255);
            entity.Property(e => e.PermissionView).HasMaxLength(255);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Uiconfig).HasColumnName("UIConfig");
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasOne(d => d.Creador).WithMany(p => p.SystemCustomFieldDefinitionsCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_system_custom_field_definitions_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.SystemCustomFieldDefinitionsModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_system_custom_field_definitions_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.SystemCustomFieldDefinitions)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_system_custom_field_definitions_OrganizationId");
        });

        modelBuilder.Entity<SystemCustomFieldTemplates>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_c__3214EC072D9ACCEC");

            entity.ToTable("system_custom_field_templates");

            entity.HasIndex(e => new { e.Category, e.Active }, "IX_system_custom_field_templates_category");

            entity.HasIndex(e => new { e.TargetEntityName, e.OrganizationId, e.Active }, "IX_system_custom_field_templates_target_entity");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.TargetEntityName).HasMaxLength(100);
            entity.Property(e => e.TemplateName).HasMaxLength(255);

            entity.HasOne(d => d.Creador).WithMany(p => p.SystemCustomFieldTemplatesCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_system_custom_field_templates_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.SystemCustomFieldTemplatesModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_system_custom_field_templates_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.SystemCustomFieldTemplates)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_system_custom_field_templates_OrganizationId");
        });

        modelBuilder.Entity<SystemFormEntities>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_f__3214EC07FEBA3E89");

            entity.ToTable("system_form_entities");

            entity.HasIndex(e => e.Active, "IX_system_form_entities_Active");

            entity.HasIndex(e => e.Category, "IX_system_form_entities_Category");

            entity.HasIndex(e => e.EntityName, "IX_system_form_entities_EntityName");

            entity.HasIndex(e => e.OrganizationId, "IX_system_form_entities_OrganizationId");

            entity.HasIndex(e => e.SortOrder, "IX_system_form_entities_SortOrder");

            entity.HasIndex(e => e.TableName, "IX_system_form_entities_TableName");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.AllowCustomFields).HasDefaultValue(true);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IconName).HasMaxLength(50);
            entity.Property(e => e.SortOrder).HasDefaultValue(100);
            entity.Property(e => e.TableName).HasMaxLength(100);

            entity.HasOne(d => d.Creador).WithMany(p => p.SystemFormEntitiesCreador)
                .HasForeignKey(d => d.CreadorId)
                .HasConstraintName("FK_system_form_entities_CreadorId");

            entity.HasOne(d => d.Modificador).WithMany(p => p.SystemFormEntitiesModificador)
                .HasForeignKey(d => d.ModificadorId)
                .HasConstraintName("FK_system_form_entities_ModificadorId");

            entity.HasOne(d => d.Organization).WithMany(p => p.SystemFormEntities)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("FK_system_form_entities_OrganizationId");
        });

        modelBuilder.Entity<SystemOrganization>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_o__3214EC0751ED52ED");

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
            entity.HasKey(e => e.Id).HasName("PK__system_p__3214EC07B035D284");

            entity.ToTable("system_permissions");

            entity.HasIndex(e => e.ActionKey, "IX_system_permissions_ActionKey");

            entity.HasIndex(e => e.Active, "IX_system_permissions_Active");

            entity.HasIndex(e => e.FechaCreacion, "IX_system_permissions_FechaCreacion");

            entity.HasIndex(e => e.GroupKey, "IX_system_permissions_GroupKey");

            entity.HasIndex(e => e.GrupoNombre, "IX_system_permissions_GrupoNombre");

            entity.HasIndex(e => e.Nombre, "IX_system_permissions_Nombre");

            entity.HasIndex(e => e.OrganizationId, "IX_system_permissions_OrganizationId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ActionKey).HasMaxLength(255);
            entity.Property(e => e.Active).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.GroupKey).HasMaxLength(255);
            entity.Property(e => e.GrupoNombre).HasMaxLength(255);
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
            entity.HasKey(e => e.Id).HasName("PK__system_r__3214EC0796B36133");

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
            entity.HasKey(e => e.Id).HasName("PK__system_r__3214EC07C99FBBD8");

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

        modelBuilder.Entity<SystemTablasAuditables>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_t__3213E83FF4FD7864");

            entity.ToTable("system_tablas_auditables");

            entity.HasIndex(e => e.Activo, "IX_system_tablas_auditables_activo").HasFilter("([activo]=(1))");

            entity.HasIndex(e => new { e.Tabla, e.Activo }, "IX_system_tablas_auditables_tabla_activo");

            entity.HasIndex(e => new { e.OrganizacionId, e.Tabla }, "UQ__system_t__62D073F09D231801").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.CreadoPor).HasColumnName("creado_por");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaModificacion)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("fecha_modificacion");
            entity.Property(e => e.OrganizacionId).HasColumnName("organizacion_id");
            entity.Property(e => e.Tabla)
                .HasMaxLength(100)
                .HasColumnName("tabla");
            entity.Property(e => e.TriggerCreado)
                .HasDefaultValue(false)
                .HasColumnName("trigger_creado");
        });

        modelBuilder.Entity<SystemUsers>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__system_u__3214EC073D04B979");

            entity.ToTable("system_users");

            entity.HasIndex(e => e.Active, "IX_system_users_Active");

            entity.HasIndex(e => e.Email, "IX_system_users_Email");

            entity.HasIndex(e => e.FechaCreacion, "IX_system_users_FechaCreacion");

            entity.HasIndex(e => e.Nombre, "IX_system_users_Nombre");

            entity.HasIndex(e => e.OrganizationId, "IX_system_users_OrganizationId");

            entity.HasIndex(e => e.Email, "UQ__system_u__A9D10534ED600C9D").IsUnique();

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
            entity.HasKey(e => e.Id).HasName("PK__system_u__3214EC0756931A72");

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
            entity.HasKey(e => e.Id).HasName("PK__system_u__3214EC078B524898");

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

        modelBuilder.Entity<VwAuditoriaCompleta>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_auditoria_completa");

            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.Campo).HasMaxLength(255);
            entity.Property(e => e.Tabla).HasMaxLength(255);
        });

        modelBuilder.Entity<ZToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__z_token__3213E83F8EC393A0");

            entity.ToTable("z_token");

            entity.HasIndex(e => e.Logout, "IX_z_token_Logout");

            entity.HasIndex(e => e.Organizationid, "IX_z_token_OrganizationId");

            entity.HasIndex(e => e.Refresh, "IX_z_token_Refresh");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Data)
                .IsUnicode(false)
                .HasColumnName("data");
            entity.Property(e => e.Logout).HasColumnName("logout");
            entity.Property(e => e.Organizationid).HasColumnName("organizationid");
            entity.Property(e => e.Refresh).HasColumnName("refresh");

            entity.HasOne(d => d.Organization).WithMany(p => p.ZToken)
                .HasForeignKey(d => d.Organizationid)
                .HasConstraintName("FK_z_token_OrganizationId");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
