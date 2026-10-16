# Rapports et preuves

Chaque exécution produit un rapport que vous pouvez lire, filtrer et exporter. Le rapport sépare toujours ce qui a été prouvé de ce qui a seulement été observé.

## Les états que vous verrez

| État | Signification |
| --- | --- |
| À examiner | Les preuves étayent un problème qui mérite votre attention. |
| Information | Des preuves ont été collectées et sont affichées pour le contexte. |
| Incomplètes ou Inconnu | Une vérification n'a pas pu se terminer ou la preuve n'était pas disponible. Cela ne compte jamais comme un bon état. |
| Historique | Un événement passé. Il explique l'historique mais ne prouve pas une panne actuelle. |
| Vérifié | Une réparation ou une modification a été confirmée en relisant le résultat. |

## La sortie brute et le rapport lisible

**Sortie brute** conserve les preuves techniques avec les numéros de ligne d'origine pour que vous puissiez remonter de chaque constat à sa source. Le **Rapport lisible** résume la même exécution en langage clair, en commençant par la prochaine étape sûre.

## Journaux enregistrés

KillerBench peut aussi lire des journaux Windows enregistrés, comme ceux de CBS et de Setup, sans rien exécuter sur l'ordinateur. Les constats renvoient à la ligne exacte qui les a produits. **Charger un exemple**, dans Sources de session, ouvre un exemple synthétique que vous pouvez explorer sans risque.

## Exporter

**Exporter le rapport** enregistre le rapport avec ses chemins sources, ses entrées analysées et ses extraits de preuves, pour relecture avant de le joindre à un ticket. Les journaux bruts et les extraits de preuves peuvent contenir des noms d'ordinateurs, des comptes, des chemins ou du texte source sensible. Les collecteurs protégés limitent ce qu'ils conservent, mais les preuves importées ne sont pas forcément expurgées. Examinez et retirez les informations sensibles avant de partager.
