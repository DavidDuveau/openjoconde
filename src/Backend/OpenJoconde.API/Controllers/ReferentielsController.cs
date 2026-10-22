using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenJoconde.Core.Dtos;
using OpenJoconde.Core.Models;
using OpenJoconde.Infrastructure.Data;

namespace OpenJoconde.API.Controllers
{
    /// <summary>
    /// Listes de référence des filtres de recherche : domaines, techniques et périodes. Les techniques sont plus de
    /// cinquante mille : la liste est paginée et se cherche par une partie du nom.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class ReferentielsController : ControllerBase
    {
        private readonly OpenJocondeDbContext _context;

        public ReferentielsController(OpenJocondeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <param name="name">Partie du nom, sans tenir compte de la casse</param>
        /// <param name="page">Numéro de page (commence à 1)</param>
        /// <param name="pageSize">Nombre d'éléments par page (100 au plus)</param>
        [HttpGet("api/domains")]
        public Task<ActionResult<PaginatedResult<NamedItemDto>>> GetDomains(
            [FromQuery] string? name = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
            List(_context.Domains.Select(d => new NamedItemDto { Id = d.Id, Name = d.Name }), name, page, pageSize);

        /// <inheritdoc cref="GetDomains"/>
        [HttpGet("api/techniques")]
        public Task<ActionResult<PaginatedResult<NamedItemDto>>> GetTechniques(
            [FromQuery] string? name = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
            List(_context.Techniques.Select(t => new NamedItemDto { Id = t.Id, Name = t.Name }), name, page, pageSize);

        /// <inheritdoc cref="GetDomains"/>
        [HttpGet("api/periods")]
        public Task<ActionResult<PaginatedResult<NamedItemDto>>> GetPeriods(
            [FromQuery] string? name = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
            List(_context.Periods.Select(p => new NamedItemDto { Id = p.Id, Name = p.Name }), name, page, pageSize);

        private static async Task<ActionResult<PaginatedResult<NamedItemDto>>> List(
            IQueryable<NamedItemDto> items, string? name, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            if (!string.IsNullOrWhiteSpace(name))
            {
                var pattern = SearchPatterns.Contains(name.Trim());
                items = items.Where(i => EF.Functions.ILike(i.Name, pattern));
            }

            var ordered = items.OrderBy(i => i.Name);
            return new OkObjectResult(new PaginatedResult<NamedItemDto>
            {
                Items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(),
                TotalItems = await ordered.CountAsync(),
                Page = page,
                PageSize = pageSize
            });
        }
    }
}
