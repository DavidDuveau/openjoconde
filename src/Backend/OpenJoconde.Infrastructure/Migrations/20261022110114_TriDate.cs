using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJoconde.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TriDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Première année d'une date de création en texte libre : « 1850 », « vers 1850-1900 », « 4e quart 19e siècle »
            // (1875), « 1ère moitié 20e siècle » (1900), « 19e-20e siècle » (1800). Sans lecture possible (« néolithique »,
            // « avant J.-C. »), NULL : ces œuvres viennent en dernier.
            migrationBuilder.Sql(@"
CREATE FUNCTION oj_creation_year(date_text text) RETURNS integer
LANGUAGE plpgsql IMMUTABLE PARALLEL SAFE AS $$
DECLARE
    s text := lower(split_part(coalesce(date_text, ''), ';', 1));
    explicit_year text;
    century integer;
    quarter integer;
    half integer;
    year integer;
BEGIN
    IF s ~ 'av(\.|ant)\s' OR s ~ 'j\.?-c' THEN
        RETURN NULL;
    END IF;

    explicit_year := substring(s from '(?<![0-9])([0-9]{3,4})(?![0-9])');
    IF explicit_year IS NOT NULL THEN
        RETURN explicit_year::integer;
    END IF;

    century := substring(s from '([0-9]{1,2})(?:er|ère|e|ème)?(?:\s*-\s*[0-9]{1,2}(?:er|ère|e|ème)?)?\s*si[eè]cle')::integer;
    IF century IS NULL OR century < 1 THEN
        RETURN NULL;
    END IF;

    year := (century - 1) * 100;
    quarter := substring(s from '([1-4])(?:ère|er|ème|e)\s+quart')::integer;
    half := substring(s from '([1-2])(?:ère|er|ème|e)\s+moiti')::integer;
    IF quarter IS NOT NULL THEN
        year := year + 25 * (quarter - 1);
    ELSIF half IS NOT NULL THEN
        year := year + 50 * (half - 1);
    ELSIF s ~ 'd[ée]but' THEN
        year := year;
    ELSIF s ~ 'milieu' THEN
        year := year + 40;
    ELSIF s ~ '(^|\s)fin\s' THEN
        year := year + 75;
    END IF;
    RETURN year;
END
$$;");

            migrationBuilder.AddColumn<int>(
                name: "creation_year",
                table: "artwork",
                type: "integer",
                nullable: true,
                computedColumnSql: "oj_creation_year(creation_date)",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ix_artwork_creation_year_id",
                table: "artwork",
                columns: new[] { "creation_year", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_artwork_creation_year_id",
                table: "artwork");

            migrationBuilder.DropColumn(
                name: "creation_year",
                table: "artwork");

            migrationBuilder.Sql("DROP FUNCTION oj_creation_year(text);");
        }
    }
}
