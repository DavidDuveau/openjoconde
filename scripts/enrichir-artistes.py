#!/usr/bin/env python3
"""Enrichit les artistes de la base avec Wikidata et Wikipédia (US-VISUALISATION-03).

La notice Joconde ne donne ni biographie, ni nationalité, ni courant : on les rapproche de Wikidata. Un rapprochement
n'est retenu que s'il est sûr : une seule personne dont le nom, l'année de naissance (et de décès quand elle est connue)
correspondent exactement, avec un article Wikipédia en français. Un doute vaut un refus : mieux vaut pas de biographie
qu'une biographie d'un homonyme.

Sources et licences : Wikidata (CC0) ; extraits de Wikipédia (CC BY-SA 4.0), à citer avec le lien de l'article, que
l'API et le front affichent.

Usage : python scripts/enrichir-artistes.py [--minimum-oeuvres 3] [--limite N] [--cache fichier.json]
        Variables PG* de psql pour la connexion (PGPASSWORD, PGHOST, PGUSER, PGDATABASE).
Reprend là où il s'est arrêté grâce au cache.
"""
import argparse
import json
import os
import re
import subprocess
import sys
import time
import unicodedata
import urllib.parse
import urllib.request

AGENT = "openjoconde-enrichissement/0.1 (https://github.com/DavidDuveau/openjoconde)"
PSQL = os.environ.get("PSQL", r"C:\Program Files\PostgreSQL\17\bin\psql")


def normaliser(texte):
    """Minuscules, sans accents ni ponctuation : la forme sous laquelle on compare deux noms."""
    sans_accent = unicodedata.normalize("NFD", texte or "")
    sans_accent = "".join(c for c in sans_accent if unicodedata.category(c) != "Mn")
    return re.sub(r"[^a-z0-9]+", " ", sans_accent.lower()).strip()


def annee(valeur):
    """L'année d'une date de Wikidata (+1840-00-00T00:00:00Z) ou de la notice (1840) ; None si absente ou incertaine."""
    trouve = re.match(r"^\+?(\d{4})(?:-|$)", valeur or "")
    return int(trouve.group(1)) if trouve else None


def nom_complet(prenom, nom):
    """« Monet » + « Claude » -> « Claude Monet » ; None pour les noms qui n'en sont pas (anonymes, « d'après »...)."""
    brut = f"{prenom or ''} {nom or ''}".strip()
    if re.search(r"[;()/]|anonyme|atelier|école|ecole|d'après|manufacture", brut, re.I):
        return None
    return brut or None


def valeurs(entite, propriete):
    """Les valeurs de la déclaration (identifiants d'entité ou dates) d'une propriété Wikidata."""
    resultat = []
    for declaration in entite.get("claims", {}).get(propriete, []):
        valeur = declaration.get("mainsnak", {}).get("datavalue", {}).get("value")
        if isinstance(valeur, dict):
            resultat.append(valeur.get("id") or valeur.get("time"))
    return [v for v in resultat if v]


def correspond(entite, candidat):
    """Vrai si l'entité est bien la personne de la notice : humain, même nom, mêmes années, article frwiki."""
    if "Q5" not in valeurs(entite, "P31") or "frwiki" not in entite.get("sitelinks", {}):
        return False
    noms = [entite.get("labels", {}).get("fr", {}).get("value", "")]
    noms += [a["value"] for a in entite.get("aliases", {}).get("fr", [])]
    if normaliser(candidat["nom"]) not in {normaliser(n) for n in noms}:
        return False
    naissances = {annee(v) for v in valeurs(entite, "P569")}
    if candidat["naissance"] not in naissances:
        return False
    deces = {annee(v) for v in valeurs(entite, "P570")}
    return candidat["deces"] is None or not deces or candidat["deces"] in deces


def est_une_personne(extrait):
    """Faux quand l'article rapproché parle d'une entreprise ou d'une famille portant le nom de la personne."""
    return not re.search(r"\best (?:une?|la|le) (?:société|entreprise|marque|maison|manufacture|famille|dynastie)\b", extrait or "")


def appeler(url, tentatives=4):
    for essai in range(tentatives):
        try:
            requete = urllib.request.Request(url, headers={"User-Agent": AGENT})
            with urllib.request.urlopen(requete, timeout=30) as reponse:
                return json.load(reponse)
        except Exception as erreur:  # réseau, 429, 5xx : on patiente puis on réessaie
            if essai == tentatives - 1:
                print(f"  abandon {url[:90]} : {erreur}", file=sys.stderr)
                return None
            time.sleep(2 ** (essai + 1))


def api_wikidata(**parametres):
    parametres["format"] = "json"
    return appeler("https://www.wikidata.org/w/api.php?" + urllib.parse.urlencode(parametres))


def chercher(nom):
    reponse = api_wikidata(action="wbsearchentities", search=nom, language="fr", uselang="fr", limit=5, type="item")
    return [r["id"] for r in (reponse or {}).get("search", [])]


def entites(identifiants, langue="fr"):
    resultat = {}
    for debut in range(0, len(identifiants), 50):
        lot = identifiants[debut:debut + 50]
        reponse = api_wikidata(action="wbgetentities", ids="|".join(lot), props="claims|sitelinks|labels|aliases",
                               languages=langue)
        resultat.update((reponse or {}).get("entities", {}))
    return resultat


def extrait_wikipedia(titre):
    reponse = appeler("https://fr.wikipedia.org/api/rest_v1/page/summary/" + urllib.parse.quote(titre.replace(" ", "_"), safe=""))
    texte = (reponse or {}).get("extract")
    return texte.strip() if texte else None


def candidats(minimum):
    sql = ("select a.id, coalesce(a.first_name,''), a.last_name, a.birth_date, coalesce(a.death_date,'') "
           "from artist a where a.birth_date ~ '^[0-9]{4}$' and a.biography is null and "
           f"(select count(*) from artwork_artist l where l.artist_id = a.id) >= {minimum} order by a.id")
    sortie = subprocess.run([PSQL, "-w", "-At", "-F", "\t", "-c", sql], capture_output=True, text=True, check=True,
                            encoding="utf-8").stdout
    for ligne in sortie.splitlines():
        id_, prenom, nom, naissance, deces = ligne.split("\t")
        complet = nom_complet(prenom, nom)
        if complet:
            yield {"id": id_, "nom": complet, "naissance": int(naissance),
                   "deces": int(deces) if re.fullmatch(r"\d{4}", deces) else None}


def sql_texte(valeur):
    return "null" if valeur is None else "'" + valeur.replace("'", "''") + "'"


def main():
    lecture = argparse.ArgumentParser()
    lecture.add_argument("--minimum-oeuvres", type=int, default=3)
    lecture.add_argument("--limite", type=int)
    lecture.add_argument("--cache", default="enrichissement-artistes.json")
    lecture.add_argument("--sql", default="enrichissement-artistes.sql")
    options = lecture.parse_args()

    cache = json.load(open(options.cache, encoding="utf-8")) if os.path.exists(options.cache) else {}
    liste = list(candidats(options.minimum_oeuvres))[: options.limite]
    print(f"{len(liste)} candidats, {len(cache)} déjà traités")

    for numero, candidat in enumerate(liste, 1):
        if candidat["id"] in cache:
            continue
        retenus = [i for i in chercher(candidat["nom"])]
        trouves = entites(retenus) if retenus else {}
        bons = [(i, e) for i, e in trouves.items() if correspond(e, candidat)]
        resultat = None
        if len(bons) == 1:
            identifiant, entite = bons[0]
            titre = entite["sitelinks"]["frwiki"]["title"]
            liens = valeurs(entite, "P27") + valeurs(entite, "P135")
            etiquettes = entites(sorted(set(liens))) if liens else {}
            etiquette = lambda q: etiquettes.get(q, {}).get("labels", {}).get("fr", {}).get("value")
            biographie = extrait_wikipedia(titre)
            resultat = None if not est_une_personne(biographie) else {
                "wikidata": identifiant,
                "wikipedia": "https://fr.wikipedia.org/wiki/" + urllib.parse.quote(titre.replace(" ", "_")),
                "biographie": biographie,
                "nationalite": next(filter(None, map(etiquette, valeurs(entite, "P27"))), None),
                "courant": next(filter(None, map(etiquette, valeurs(entite, "P135"))), None),
            }
        cache[candidat["id"]] = resultat
        if numero % 25 == 0:
            json.dump(cache, open(options.cache, "w", encoding="utf-8"), ensure_ascii=False)
            print(f"  {numero}/{len(liste)} ; {sum(1 for v in cache.values() if v)} rapprochés")
        time.sleep(0.3)
    json.dump(cache, open(options.cache, "w", encoding="utf-8"), ensure_ascii=False)

    with open(options.sql, "w", encoding="utf-8") as sortie:
        sortie.write("begin;\n")
        for id_, r in cache.items():
            if r and r["biographie"]:
                sortie.write(
                    "update artist set wikidata_id = {}, wikipedia_url = {}, biography = {}, "
                    "nationality = coalesce(nationality, {}), movement = {} where id = '{}';\n".format(
                        sql_texte(r["wikidata"]), sql_texte(r["wikipedia"]), sql_texte(r["biographie"]),
                        sql_texte(r["nationalite"]), sql_texte(r["courant"]), id_))
        sortie.write("commit;\n")
    print(f"Écrit {options.sql} : {sum(1 for v in cache.values() if v and v['biographie'])} artistes enrichis. "
          f"Application : psql -f {options.sql}")


if __name__ == "__main__":
    main()
