#!/usr/bin/env python3
"""
Construit le fond de carte de la page d'accueil : contours des régions et des départements, outre-mer compris, allégés
pour le navigateur. Écrit src/Frontend/src/assets/geo/france.json.

Source : https://github.com/gregoiredavid/france-geojson (contours IGN Admin Express, Licence Ouverte 2.0).
Métropole : version simplifiée ; outre-mer : version complète, allégée ici.

Usage : python scripts/construire-fond-de-carte.py [dossier_des_sources]
Sans dossier, les fichiers sont téléchargés dans un dossier temporaire.
"""
import json
import sys
import tempfile
import urllib.request
from pathlib import Path

BASE = "https://raw.githubusercontent.com/gregoiredavid/france-geojson/master/"
FICHIERS = [
    "regions-version-simplifiee.geojson",
    "departements-version-simplifiee.geojson",
    "regions-avec-outre-mer.geojson",
    "departements-avec-outre-mer.geojson",
]
SORTIE = Path(__file__).resolve().parent.parent / "src" / "Frontend" / "src" / "assets" / "geo" / "france.json"

OUTRE_MER = {"971", "972", "973", "974", "976"}
# Les cinq régions d'outre-mer sont aussi des départements : même code côté carte, codes de région propres à la source
REGION_OUTRE_MER = {"971": "01", "972": "02", "973": "03", "974": "04", "976": "06"}

# Région de chaque département métropolitain (codes INSEE des régions)
REGION_DE = {
    "11": ["75", "77", "78", "91", "92", "93", "94", "95"],
    "24": ["18", "28", "36", "37", "41", "45"],
    "27": ["21", "25", "39", "58", "70", "71", "89", "90"],
    "28": ["14", "27", "50", "61", "76"],
    "32": ["02", "59", "60", "62", "80"],
    "44": ["08", "10", "51", "52", "54", "55", "57", "67", "68", "88"],
    "52": ["44", "49", "53", "72", "85"],
    "53": ["22", "29", "35", "56"],
    "75": ["16", "17", "19", "23", "24", "33", "40", "47", "64", "79", "86", "87"],
    "76": ["09", "11", "12", "30", "31", "32", "34", "46", "48", "65", "66", "81", "82"],
    "84": ["01", "03", "07", "15", "26", "38", "42", "43", "63", "69", "73", "74"],
    "93": ["04", "05", "06", "13", "83", "84"],
    "94": ["2A", "2B"],
}
REGION_PAR_DEPARTEMENT = {d: r for r, ds in REGION_DE.items() for d in ds}


def charger(dossier: Path, nom: str) -> dict:
    chemin = dossier / nom
    if not chemin.exists():
        print(f"Téléchargement de {nom}…")
        urllib.request.urlretrieve(BASE + nom, chemin)
    return json.loads(chemin.read_text(encoding="utf-8"))


def distance_segment(p, a, b) -> float:
    (px, py), (ax, ay), (bx, by) = p, a, b
    dx, dy = bx - ax, by - ay
    if dx == dy == 0:
        return ((px - ax) ** 2 + (py - ay) ** 2) ** 0.5
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy)))
    return ((px - (ax + t * dx)) ** 2 + (py - (ay + t * dy)) ** 2) ** 0.5


def douglas_peucker(points, tolerance):
    """Allège une ligne en gardant les points qui s'écartent de plus de la tolérance (degrés)."""
    if len(points) < 3:
        return points
    garde = [False] * len(points)
    garde[0] = garde[-1] = True
    pile = [(0, len(points) - 1)]
    while pile:
        debut, fin = pile.pop()
        maxi, index = 0.0, -1
        for i in range(debut + 1, fin):
            d = distance_segment(points[i], points[debut], points[fin])
            if d > maxi:
                maxi, index = d, i
        if maxi > tolerance:
            garde[index] = True
            pile.append((debut, index))
            pile.append((index, fin))
    return [p for p, g in zip(points, garde) if g]


def alleger_anneau(anneau, tolerance, decimales):
    # Anneau fermé : on coupe au point le plus éloigné du premier pour ne pas figer l'allègement sur la jointure
    points = [tuple(p[:2]) for p in anneau[:-1]]
    if len(points) < 4:
        return None
    loin = max(range(len(points)), key=lambda i: (points[i][0] - points[0][0]) ** 2 + (points[i][1] - points[0][1]) ** 2)
    a = douglas_peucker(points[: loin + 1], tolerance)
    b = douglas_peucker(points[loin:] + [points[0]], tolerance)
    ligne = a + b[1:]
    arrondi = []
    for x, y in ligne:
        p = (round(x, decimales), round(y, decimales))
        if not arrondi or arrondi[-1] != p:
            arrondi.append(p)
    if arrondi[0] != arrondi[-1]:
        arrondi.append(arrondi[0])
    return [list(p) for p in arrondi] if len(arrondi) >= 4 else None


def aire(anneau) -> float:
    """Aire signée (formule du lacet) : négative pour un anneau tournant dans le sens des aiguilles d'une montre."""
    return sum(x1 * y2 - x2 * y1 for (x1, y1), (x2, y2) in zip(anneau, anneau[1:])) / 2


def orienter(polygone):
    """
    D3 attend les anneaux extérieurs dans le sens horaire et les trous dans le sens inverse (l'inverse de la norme
    GeoJSON) : sinon il dessine le monde entier moins le polygone.
    """
    sortie = []
    for i, anneau in enumerate(polygone):
        horaire = aire(anneau) < 0
        sortie.append(anneau if horaire == (i == 0) else anneau[::-1])
    return sortie


def alleger_geometrie(geometrie, tolerance, decimales=3, surface_min=0.0):
    polygones = geometrie["coordinates"] if geometrie["type"] == "MultiPolygon" else [geometrie["coordinates"]]
    sortie = []
    for polygone in polygones:
        # Les îlots plus petits que le seuil sont écartés (le trait serait invisible à l'échelle d'une région)
        exterieur = polygone[0]
        xs, ys = [p[0] for p in exterieur], [p[1] for p in exterieur]
        if (max(xs) - min(xs)) * (max(ys) - min(ys)) < surface_min:
            continue
        anneaux = [alleger_anneau(r, tolerance, decimales) for r in polygone]
        if anneaux[0] is None:
            continue
        sortie.append(orienter([a for a in anneaux if a is not None]))
    if not sortie:
        raise ValueError("Géométrie vide après allègement")
    if len(sortie) == 1:
        return {"type": "Polygon", "coordinates": sortie[0]}
    return {"type": "MultiPolygon", "coordinates": sortie}


def dans_anneau(point, anneau) -> bool:
    x, y = point
    dedans = False
    for (x1, y1), (x2, y2) in zip(anneau, anneau[1:]):
        if (y1 > y) != (y2 > y) and x < (x2 - x1) * (y - y1) / (y2 - y1) + x1:
            dedans = not dedans
    return dedans


def dans_geometrie(point, geometrie) -> bool:
    polygones = geometrie["coordinates"] if geometrie["type"] == "MultiPolygon" else [geometrie["coordinates"]]
    return any(dans_anneau(point, p[0]) and not any(dans_anneau(point, t) for t in p[1:]) for p in polygones)


def construire(dossier: Path) -> dict:
    regions_metro = charger(dossier, "regions-version-simplifiee.geojson")["features"]
    departements_metro = charger(dossier, "departements-version-simplifiee.geojson")["features"]
    regions_om = charger(dossier, "regions-avec-outre-mer.geojson")["features"]
    departements_om = charger(dossier, "departements-avec-outre-mer.geojson")["features"]

    regions, departements = [], []
    for f in regions_metro:
        regions.append({"code": f["properties"]["code"], "nom": f["properties"]["nom"], "outreMer": False,
                        "geometry": alleger_geometrie(f["geometry"], 0.004, surface_min=0.0005)})
    for f in regions_om:
        if f["properties"]["code"] in REGION_OUTRE_MER.values():
            code = next(d for d, r in REGION_OUTRE_MER.items() if r == f["properties"]["code"])
            regions.append({"code": code, "nom": f["properties"]["nom"], "outreMer": True,
                            "geometry": alleger_geometrie(f["geometry"], 0.004, surface_min=0.0005)})
    for f in departements_metro:
        code = f["properties"]["code"]
        departements.append({"code": code, "nom": f["properties"]["nom"], "region": REGION_PAR_DEPARTEMENT[code],
                             "outreMer": False, "geometry": alleger_geometrie(f["geometry"], 0.003, surface_min=0.0002)})
    for f in departements_om:
        code = f["properties"]["code"]
        if code in OUTRE_MER:
            departements.append({"code": code, "nom": f["properties"]["nom"], "region": code, "outreMer": True,
                                 "geometry": alleger_geometrie(f["geometry"], 0.004, surface_min=0.0005)})

    verifier(regions, departements)
    return {"regions": regions, "departements": departements}


def verifier(regions, departements):
    """Chaque département doit tomber dans la région qu'on lui donne : sinon la table ci-dessus est fausse."""
    par_code = {r["code"]: r for r in regions}
    assert len(regions) == 18, f"{len(regions)} régions au lieu de 18"
    assert len(departements) == 101, f"{len(departements)} départements au lieu de 101"
    assert set(REGION_PAR_DEPARTEMENT) == {d["code"] for d in departements if not d["outreMer"]}
    for d in departements:
        geometrie = d["geometry"]
        polygone = geometrie["coordinates"] if geometrie["type"] == "Polygon" else max(geometrie["coordinates"], key=lambda p: len(p[0]))
        exterieur = polygone[0]
        # Des sommets du département, ramenés vers son centre : la majorité doit tomber dans sa région (ceux du bord
        # peuvent déborder d'un trait sur une région voisine, un département mal rangé tombe presque tout ailleurs)
        cx = sum(p[0] for p in exterieur) / len(exterieur)
        cy = sum(p[1] for p in exterieur) / len(exterieur)
        pas = max(1, len(exterieur) // 40)
        points = [(x + (cx - x) * 0.2, y + (cy - y) * 0.2) for x, y in exterieur[::pas]]
        region = par_code[d["region"]]
        dedans = sum(dans_geometrie(pt, region["geometry"]) for pt in points)
        assert dedans >= 0.6 * len(points), f"{d['nom']} ({d['code']}) n'est pas dans la région {region['nom']}"


def main():
    dossier = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(tempfile.mkdtemp(prefix="fond_de_carte_"))
    dossier.mkdir(parents=True, exist_ok=True)
    donnees = construire(dossier)
    SORTIE.parent.mkdir(parents=True, exist_ok=True)
    SORTIE.write_text(json.dumps(donnees, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
    print(f"{SORTIE} : {SORTIE.stat().st_size // 1024} Ko, {len(donnees['regions'])} régions, {len(donnees['departements'])} départements")


if __name__ == "__main__":
    main()
