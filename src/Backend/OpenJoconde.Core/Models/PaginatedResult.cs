using System;
using System.Collections.Generic;

namespace OpenJoconde.Core.Models
{
    /// <summary>
    /// Résultat paginé commun à toutes les listes de l'API
    /// </summary>
    /// <typeparam name="T">Type d'élément</typeparam>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// Éléments de la page courante
        /// </summary>
        public required IReadOnlyList<T> Items { get; init; }

        /// <summary>
        /// Nombre total d'éléments (toutes pages confondues)
        /// </summary>
        public int TotalItems { get; init; }

        /// <summary>
        /// Numéro de la page courante (à partir de 1)
        /// </summary>
        public int Page { get; init; }

        /// <summary>
        /// Nombre d'éléments par page
        /// </summary>
        public int PageSize { get; init; }

        /// <summary>
        /// Nombre total de pages
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
    }
}
