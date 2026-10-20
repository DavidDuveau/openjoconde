using OpenJoconde.Infrastructure.Services;
using Xunit;

namespace OpenJoconde.Infrastructure.Tests;

public class JocondeSourceFileTests
{
    [Theory]
    [InlineData("https://ministere-culture.s3.sbg.io.cloud.ovh.net/POP/joconde.csv", ".csv")]
    [InlineData("https://exemple.test/export/donnees.JSON?lang=fr", ".json")]
    [InlineData("https://exemple.test/export/donnees.xml", ".csv")]
    [InlineData("https://exemple.test/api/export", ".csv")]
    [InlineData("https://exemple.test/archive.zip", ".csv")]
    [InlineData("Data/Temp/joconde.json", ".json")]
    public void ExtensionFor_DeduitLeFormatDeLUrl(string url, string attendu)
    {
        Assert.Equal(attendu, JocondeSourceFile.ExtensionFor(url));
    }
}
