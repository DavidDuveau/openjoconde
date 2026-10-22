#!/usr/bin/env python3
"""Vérifie la couverture du back : échoue si les lignes ou les branches passent sous le seuil.

Usage (depuis la racine du dépôt) :
    dotnet test OpenJoconde.sln --settings coverage.runsettings --results-directory ./TestResults
    python scripts/verifier-couverture-back.py [rapport.cobertura.xml] [--seuil 85]

Sans rapport en argument, lit le plus récent TestResults/*/coverage.cobertura.xml.
"""
import glob
import os
import re
import sys
import xml.etree.ElementTree as ET

SEUIL_PAR_DEFAUT = 85.0


def trouver_rapport(args):
    if args:
        return args[0]
    rapports = glob.glob(os.path.join("TestResults", "*", "coverage.cobertura.xml"))
    if not rapports:
        sys.exit("Aucun rapport de couverture : lancer dotnet test avec --settings coverage.runsettings")
    return max(rapports, key=os.path.getmtime)


def mesurer(rapport):
    racine = ET.parse(rapport).getroot()
    fichiers = {}
    for classe in racine.iter("class"):
        stats = fichiers.setdefault(classe.get("filename"), [0, 0, 0, 0])
        for ligne in classe.findall("./lines/line"):
            stats[0] += 1
            stats[1] += int(ligne.get("hits")) > 0
            if ligne.get("branch") == "True":
                m = re.search(r"\((\d+)/(\d+)\)", ligne.get("condition-coverage", ""))
                if m:
                    stats[2] += int(m.group(2))
                    stats[3] += int(m.group(1))
    return fichiers


def taux(couvert, total):
    return 100.0 * couvert / total if total else 100.0


def main():
    args = [a for a in sys.argv[1:]]
    seuil = SEUIL_PAR_DEFAUT
    if "--seuil" in args:
        i = args.index("--seuil")
        seuil = float(args[i + 1])
        del args[i:i + 2]

    rapport = trouver_rapport(args)
    fichiers = mesurer(rapport)
    lignes = sum(f[0] for f in fichiers.values())
    lignes_couvertes = sum(f[1] for f in fichiers.values())
    branches = sum(f[2] for f in fichiers.values())
    branches_couvertes = sum(f[3] for f in fichiers.values())
    taux_lignes = taux(lignes_couvertes, lignes)
    taux_branches = taux(branches_couvertes, branches)

    print(f"Rapport : {rapport}")
    print("Fichiers les moins couverts (lignes manquantes) :")
    for nom, (l, lc, b, bc) in sorted(fichiers.items(), key=lambda x: x[1][1] - x[1][0])[:8]:
        print(f"  {nom}: lignes {lc}/{l}, branches {bc}/{b}")
    print(f"Lignes   : {taux_lignes:.1f} % ({lignes_couvertes}/{lignes})")
    print(f"Branches : {taux_branches:.1f} % ({branches_couvertes}/{branches})")

    echecs = [n for n, t in (("lignes", taux_lignes), ("branches", taux_branches)) if t < seuil]
    if echecs:
        sys.exit(f"Couverture insuffisante ({' et '.join(echecs)}) : seuil {seuil:.0f} %")
    print(f"Seuil de {seuil:.0f} % respecté")


if __name__ == "__main__":
    main()
