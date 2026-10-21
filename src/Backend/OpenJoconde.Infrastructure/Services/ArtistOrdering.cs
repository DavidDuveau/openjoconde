using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using OpenJoconde.Core.Models;

namespace OpenJoconde.Infrastructure.Services
{
    /// <summary>
    /// Tri alphabétique des artistes : ceux dont le nom ne commence pas par une lettre (nom vide, « ? »…) viennent
    /// en dernier plutôt qu'en tête de liste
    /// </summary>
    public static class ArtistOrdering
    {
        public static IOrderedQueryable<Artist> ByName(IQueryable<Artist> query) =>
            query.OrderBy(a => Regex.IsMatch(a.LastName, "^[[:alpha:]]") ? 0 : 1)
                .ThenBy(a => a.LastName)
                .ThenBy(a => a.FirstName)
                .ThenBy(a => a.Id);
    }
}
