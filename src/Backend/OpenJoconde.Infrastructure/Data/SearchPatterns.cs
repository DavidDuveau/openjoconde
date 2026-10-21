namespace OpenJoconde.Infrastructure.Data
{
    /// <summary>
    /// Motifs pour ILIKE : le terme saisi est échappé, pour que % et _ soient cherchés tels quels
    /// </summary>
    public static class SearchPatterns
    {
        private static string Escape(string term) =>
            term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

        /// <summary>« terme% » : valeurs qui commencent par le terme</summary>
        public static string Prefix(string term) => Escape(term) + "%";

        /// <summary>« %terme% » : valeurs qui contiennent le terme</summary>
        public static string Contains(string term) => "%" + Escape(term) + "%";
    }
}
