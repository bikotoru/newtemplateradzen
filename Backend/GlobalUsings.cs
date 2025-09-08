// GlobalUsings.cs - Importaciones globales para Backend
// Este archivo permite que todos los Services de módulos estén disponibles globalmente

// Auth Services
global using Backend.Modules.Auth.Login;
global using Backend.Modules.Admin.SystemPermissions;
global using Backend.Modules.Admin.SystemRoles;
global using Backend.Modules.Admin.SystemUsers;

// Utils Services
global using Backend.Utils.Security;
global using Backend.Utils.Data;
global using Backend.Utils.Services;

// Shared Models
global using Shared.Models.Entities.SystemEntities;
global using Shared.Models.Entities;
global using Shared.Models.DTOs.Auth;
global using Shared.Models.Requests;
global using Shared.Models.Responses;
global using Shared.Models.QueryModels;

// System
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Logging;