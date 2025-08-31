using Microsoft.EntityFrameworkCore;
using Backend.Utils.Data;
using Shared.Models.Entities;

namespace Backend.Modules.Categoria
{
    public class CategoriaService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoriaService> _logger;

        public CategoriaService(AppDbContext context, ILogger<CategoriaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Shared.Models.Entities.Categoria>> GetAllAsync()
        {
            try
            {
                return await _context.Categoria
                    .Where(c => c.Active)
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categorias");
                throw;
            }
        }

        public async Task<Shared.Models.Entities.Categoria?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _context.Categoria
                    .FirstOrDefaultAsync(c => c.Id == id && c.Active);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categoria by ID: {Id}", id);
                throw;
            }
        }

        public async Task<Shared.Models.Entities.Categoria> CreateAsync(CreateCategoriaRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var categoria = new Shared.Models.Entities.Categoria
                {
                    Id = Guid.NewGuid(),
                    Nombre = request.Nombre,
                    Descripcion = request.Descripcion,
                    OrganizationId = request.OrganizationId,
                    FechaCreacion = DateTime.UtcNow,
                    FechaModificacion = DateTime.UtcNow,
                    Active = true
                };

                _context.Categoria.Add(categoria);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Categoria created successfully: {Id} - {Nombre}", categoria.Id, categoria.Nombre);
                return categoria;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating categoria: {Nombre}", request.Nombre);
                throw;
            }
        }

        public async Task<Shared.Models.Entities.Categoria?> UpdateAsync(Guid id, UpdateCategoriaRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var categoria = await _context.Categoria
                    .FirstOrDefaultAsync(c => c.Id == id && c.Active);

                if (categoria == null)
                {
                    return null;
                }

                categoria.Nombre = request.Nombre;
                categoria.Descripcion = request.Descripcion;
                categoria.Active = request.Active;
                categoria.FechaModificacion = DateTime.UtcNow;

                _context.Categoria.Update(categoria);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Categoria updated successfully: {Id} - {Nombre}", categoria.Id, categoria.Nombre);
                return categoria;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating categoria: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var categoria = await _context.Categoria
                    .FirstOrDefaultAsync(c => c.Id == id && c.Active);

                if (categoria == null)
                {
                    return false;
                }

                // Soft delete - solo marcamos como inactivo
                categoria.Active = false;
                categoria.FechaModificacion = DateTime.UtcNow;

                _context.Categoria.Update(categoria);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Categoria deleted successfully: {Id} - {Nombre}", categoria.Id, categoria.Nombre);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting categoria: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
                return await _context.Categoria
                    .AnyAsync(c => c.Id == id && c.Active);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if categoria exists: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsByNameAsync(string nombre, Guid? excludeId = null)
        {
            try
            {
                var query = _context.Categoria
                    .Where(c => c.Nombre.ToLower() == nombre.ToLower() && c.Active);

                if (excludeId.HasValue)
                {
                    query = query.Where(c => c.Id != excludeId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if categoria exists by name: {Nombre}", nombre);
                throw;
            }
        }

        public async Task<List<Shared.Models.Entities.Categoria>> SearchAsync(string searchTerm)
        {
            try
            {
                return await _context.Categoria
                    .Where(c => c.Active && 
                               (c.Nombre.Contains(searchTerm) || 
                                (c.Descripcion != null && c.Descripcion.Contains(searchTerm))))
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching categorias: {SearchTerm}", searchTerm);
                throw;
            }
        }
    }
}