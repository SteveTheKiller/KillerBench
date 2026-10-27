# Votre premier diagnostic

Commencez par [Téléchargement et configuration](setup.html), puis lancez une Analyse complète du système. Elle collecte des preuves de diagnostic et enregistre un journal local ; elle n'exécute aucune réparation. Un constat décrit la portée inspectée, pas toutes les pannes possibles de l'ordinateur.

## Lancer l'analyse

1. Ouvrez KillerBench. La section **Diagnostics** s'ouvre avec **Analyse complète du système** sélectionnée.
2. Laissez **Réparer automatiquement les problèmes confirmés** désactivé pour votre première exécution.
3. Appuyez sur **Exécuter le diagnostic**. La progression s'affiche dans **Sortie brute**, à droite, pendant l'exécution de chaque vérification.
4. À la fin, le volet de sortie passe au **Rapport lisible**.

Commencez sans élévation. Certaines preuves Windows, comme les protecteurs BitLocker et les services protégés, nécessitent des droits d'administrateur. Avec un compte standard, ces vérifications signalent que la preuve n'est pas disponible. Si vous avez besoin de cette preuve, terminez l'exécution en cours, conservez le rapport, puis rouvrez volontairement KillerBench avec **Exécuter en tant qu'administrateur**. L'élévation ne sélectionne ni n'approuve aucune réparation.

## Lire le rapport

Le rapport commence par les nombres de **À examiner**, **Réparations vérifiées**, **Incomplètes** et **Contrôlées**, suivis d'une étape suivante recommandée.

- **Ce qui demande de l'attention** liste les problèmes étayés par les preuves, du plus important au moins important.
- **Informations collectées** montre ce qui a été lu, y compris les preuves incomplètes ou indisponibles.
- Les événements historiques sont marqués comme historiques. Une erreur du mois dernier n'est pas traitée comme une panne actuelle.

L'absence de preuve ne compte jamais comme un bon état. Si une vérification n'a pas pu s'exécuter, le rapport le dit.

## Choisir vous-même les vérifications

Ouvrez **Choisir les vérifications (avancé)** pour sélectionner des vérifications individuelles plutôt que l'analyse complète. Chaque section de l'application propose aussi des outils ciblés, décrits dans les pages **Sections** de ce guide.

## Corriger ce que vous avez trouvé

Lorsque le rapport recommande une correction, il indique la section et l'outil à ouvrir. Lisez [Comment les modifications restent sûres](safety.html) avant votre première réparation. La validation native est encore en cours ; un diagnostic réussi ne prouve pas que chaque réparation proposée a été testée sur votre version de Windows. Conservez des sauvegardes et examinez la cible exacte et les conséquences avant toute modification.
