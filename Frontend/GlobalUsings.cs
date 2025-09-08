// GlobalUsings.cs - Importaciones globales para Frontend
// Este archivo permite que todos los Services de módulos estén disponibles globalmente

// Frontend Core Services
global using Frontend.Services;
global using Frontend.Services.Validation;
global using Frontend.Components.Base.Tables;
global using Frontend.Components.Validation;

// System (específico para resolver conflictos) 
global using FrontendValidationResult = Frontend.Services.Validation.ValidationResult;

// Shared Models
global using Shared.Models.Entities;
global using Shared.Models.Requests;
global using Shared.Models.Responses;
global using Shared.Models.Builders;
global using Shared.Models.QueryModels;

// Components
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Web;
global using Microsoft.AspNetCore.Components.Authorization;
global using System.Linq.Expressions;

// Radzen
global using Radzen;

// System
global using Microsoft.Extensions.Logging;