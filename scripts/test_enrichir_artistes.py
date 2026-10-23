"""Tests du rapprochement Wikidata : python -m unittest scripts/test_enrichir_artistes.py"""
import importlib.util
import pathlib
import unittest

spec = importlib.util.spec_from_file_location("enrichir", pathlib.Path(__file__).with_name("enrichir-artistes.py"))
enrichir = importlib.util.module_from_spec(spec)
spec.loader.exec_module(enrichir)


def entite(nom="Claude Monet", naissance="+1840-11-14T00:00:00Z", deces="+1926-12-05T00:00:00Z", humain=True, frwiki=True):
    def declaration(valeur):
        return {"mainsnak": {"datavalue": {"value": valeur}}}
    return {
        "labels": {"fr": {"value": nom}},
        "sitelinks": {"frwiki": {"title": nom}} if frwiki else {},
        "claims": {
            "P31": [declaration({"id": "Q5" if humain else "Q43229"})],
            "P569": [declaration({"time": naissance})],
            "P570": [declaration({"time": deces})],
        },
    }


class Rapprochement(unittest.TestCase):
    candidat = {"nom": "Claude Monet", "naissance": 1840, "deces": 1926}

    def test_accepte_la_personne_dont_nom_et_annees_correspondent(self):
        self.assertTrue(enrichir.correspond(entite(), self.candidat))

    def test_ignore_les_accents_et_la_casse_du_nom(self):
        self.assertTrue(enrichir.correspond(entite("Claude MONÉT"), self.candidat))

    def test_refuse_un_homonyme_ne_une_autre_annee(self):
        self.assertFalse(enrichir.correspond(entite(naissance="+1841-01-01T00:00:00Z"), self.candidat))

    def test_refuse_une_annee_de_deces_differente_mais_pas_un_deces_inconnu(self):
        self.assertFalse(enrichir.correspond(entite(deces="+1930-01-01T00:00:00Z"), self.candidat))
        self.assertTrue(enrichir.correspond(entite(), {**self.candidat, "deces": None}))

    def test_refuse_ce_qui_nest_pas_un_humain_ou_sans_article_frwiki(self):
        self.assertFalse(enrichir.correspond(entite(humain=False), self.candidat))
        self.assertFalse(enrichir.correspond(entite(frwiki=False), self.candidat))

    def test_refuse_un_autre_nom(self):
        self.assertFalse(enrichir.correspond(entite("Edouard Manet"), self.candidat))


class Outils(unittest.TestCase):
    def test_nom_complet_ecarte_les_notices_collectives(self):
        self.assertEqual(enrichir.nom_complet("Claude", "Monet"), "Claude Monet")
        self.assertIsNone(enrichir.nom_complet("Jean-François (d'après);anonyme", "Millet"))
        self.assertIsNone(enrichir.nom_complet("", "Atelier de Rubens"))

    def test_annee(self):
        self.assertEqual(annee := enrichir.annee("+1840-11-14T00:00:00Z"), 1840)
        self.assertIsNone(enrichir.annee("actif 1940"))

    def test_une_entreprise_nest_pas_une_personne(self):
        self.assertFalse(enrichir.est_une_personne("Charles Jourdan est une société spécialisée dans la chaussure."))
        self.assertTrue(enrichir.est_une_personne("Claude Monet est un peintre français."))


if __name__ == "__main__":
    unittest.main()
