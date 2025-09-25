// GlobalUsings.cs - Importaciones globales para Frontend
// Este archivo permite que todos los Services de módulos estén disponibles globalmente

// Frontend Core Services
global using Frontend.Services;
global using Frontend.Services.Validation;
global using Frontend.Components.Base.Tables;
// global using Frontend.Components.Validation; // Comentado temporalmente hasta que exista

// System (específico para resolver conflictos) 
global using FrontendValidationResult = Frontend.Services.Validation.ValidationResult;

// Shared Models
global using Shared.Models.Entities;
global using Shared.Models.Requests;
global using Shared.Models.Responses;
global using Shared.Models.Builders;
global using Shared.Models.QueryModels;
global using Shared.Models.Entities.SystemEntities;



// Module Services
global using Frontend.Modules.Admin.SystemPermissions;
global using Frontend.Modules.Admin.SystemRoles;
global using Frontend.Modules.Admin.SystemUsers;
global using Frontend.Modules.Core.Localidades.Regions;
global using Frontend.Modules.Core.Localidades.Comunas;
// Components
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Web;
global using Microsoft.AspNetCore.Components.Authorization;
global using System.Linq.Expressions;

// Radzen
global using Radzen;
global using Radzen.Blazor;

// System
global using Microsoft.Extensions.Logging;
global using Frontend.Componentes.CustomRadzen.Dialog;

global using Shared.Models.Entities.Views;
