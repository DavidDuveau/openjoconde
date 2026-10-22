using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenJoconde.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DatesAvantJesusChrist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La première version ne reconnaissait pas « av JC » (l'écriture de l'export Joconde) : « 5e siècle av JC »
            // était lu comme l'an 400 après J.-C. Les dates avant J.-C. sont maintenant des années négatives, pour un
            // tri chronologique : « 5e siècle av JC » (-500), « 4e quart 2e siècle av JC » (-200 + 75), « 5e millénaire
            // av JC » (-5000), « 40000 av JC,10000 av JC » (-40000), « 672 av JC-330 av JC » (-672).
            // La colonne stockée est recalculée par sa suppression puis sa recréation : changer la fonction ne suffit pas.
            migrationBuilder.DropIndex(
                name: "ix_artwork_creation_year_id",
                table: "artwork");

            migrationBuilder.DropColumn(
                name: "creation_year",
                table: "artwork");

            migrationBuilder.Sql(CreationYearFunction);

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

            migrationBuilder.Sql(PreviousCreationYearFunction);

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

        // Première année d'une date de création en texte libre : « 1850 », « vers 1850-1900 », « 4e quart 19e siècle »
        // (1875), « 1ère moitié 20e siècle » (1900), « 19e-20e siècle » (1800). Sans lecture possible (« néolithique »,
        // « Avant 1870 »), NULL : ces œuvres viennent en dernier.
        private const string CreationYearFunction = @"
CREATE OR REPLACE FUNCTION oj_creation_year(date_text text) RETURNS integer
LANGUAGE plpgsql IMMUTABLE PARALLEL SAFE AS $$
DECLARE
    s text := lower(split_part(coalesce(date_text, ''), ';', 1));
    before_christ boolean := s ~ '(^|[^a-z])av(\.|ant)?\s*j\.?\s*-?\s*c';
    explicit_year text;
    century integer;
    millennium integer;
    quarter integer;
    half integer;
    year integer;
BEGIN
    IF before_christ THEN
        -- « 40000 av JC,10000 av JC » : on lit le début de l'intervalle
        s := split_part(s, ',', 1);
    ELSIF s ~ 'av(\.|ant)\s' OR s ~ 'j\.?-c' THEN
        RETURN NULL;
    ELSE
        explicit_year := substring(s from '(?<![0-9])([0-9]{3,4})(?![0-9])');
        IF explicit_year IS NOT NULL THEN
            RETURN explicit_year::integer;
        END IF;
    END IF;

    century := substring(s from '([0-9]{1,2})(?:er|ère|e|ème)?(?:\s*-\s*[0-9]{1,2}(?:er|ère|e|ème)?)?\s*si[eè]cle')::integer;
    IF century IS NULL OR century < 1 THEN
        IF before_christ THEN
            millennium := substring(s from '([0-9]{1,2})(?:er|ère|e|ème)?\s*mill[ée]naire')::integer;
            IF millennium >= 1 THEN
                RETURN -millennium * 1000;
            END IF;
            explicit_year := substring(s from '(?<![0-9])([0-9]{1,5})(?![0-9])');
            IF explicit_year IS NOT NULL THEN
                RETURN -explicit_year::integer;
            END IF;
        END IF;
        RETURN NULL;
    END IF;

    -- Début du siècle : le 1er siècle après J.-C. commence en 0 ; le 1er siècle avant J.-C. en -100, le 5e en -500
    year := CASE WHEN before_christ THEN -century * 100 ELSE (century - 1) * 100 END;
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
$$;";

        // Version de TriDate : les dates avant J.-C. écrites « av JC » y sont lues comme des siècles après J.-C.
        private const string PreviousCreationYearFunction = @"
CREATE OR REPLACE FUNCTION oj_creation_year(date_text text) RETURNS integer
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
$$;";
    }
}
