using System.Reflection;

namespace OpenJoconde.API
{
    /// <summary>Version du produit, celle de la dernière release (<c>Version</c> de <c>Directory.Build.props</c>)</summary>
    public static class AppVersion
    {
        /// <summary>Numéro de version sans le suffixe de révision que le SDK peut ajouter (« 1.2.0+abc123 » donne « 1.2.0 »)</summary>
        public static string Current { get; } = Read();

        private static string Read()
        {
            var informational = typeof(AppVersion).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            return (informational ?? "0.0.0").Split('+')[0];
        }
    }
}
