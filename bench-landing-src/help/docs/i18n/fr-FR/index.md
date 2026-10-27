# Aide de KillerBench

KillerBench est un atelier de diagnostic et de réparation de Windows destiné aux techniciens. Il vérifie d'abord l'ordinateur, explique ce qu'il a trouvé avec les preuves qui soutiennent chaque constat, et ne modifie Windows que lorsque vous choisissez une correction précise et la confirmez.

KillerBench est en développement. Aucun téléchargement public n'est encore disponible. Ces pages décrivent la version actuelle.

## Ce qu'il fait

- Exécute des diagnostics en lecture seule sur Windows Update, les périphériques, le stockage, le réseau, l'impression, la sécurité, les applications et les profils, puis rédige un rapport lisible.
- Propose des réparations protégées qui ne s'exécutent que lorsque des preuves récentes démontrent le problème exact, et vérifie ensuite le résultat.
- Couvre les tâches de configuration et de durcissement comme BitLocker, les paramètres de confidentialité, le désencombrement de Windows et la mise en service d'un nouvel ordinateur.
- Exécute les diagnostics et l'analyse des rapports en local, sans IA ni compte KillerBench. Certaines actions sélectionnées, comme les téléchargements et la sauvegarde de la clé de récupération dans Entra ID, peuvent contacter des services externes.
- Utilise le thème actif de l'application pour la console, les rapports, les confirmations et les boîtes de dialogue Ouvrir, Enregistrer et Dossier.

## Par où commencer

- [Téléchargement et configuration](setup.html)
- [À propos de KillerBench](about.html)

- [Votre premier diagnostic](first-run.html) présente une analyse complète du système.
- [L'espace de travail](workspace.html) explique les sections, les volets et les raccourcis.
- [Comment les modifications restent sûres](safety.html) explique ce que KillerBench vérifie avant et après chaque modification.
- Les [guides pas à pas](fix-windows-update.html) détaillent les tâches courantes.
- Les pages **Sections** décrivent chaque outil, regroupés comme dans l'application.
- [Ligne de commande](cli.html) répertorie toutes les commandes `killerbench-cli`.
- [Dépannage](troubleshooting.html) traite des actions bloquées, des redémarrages et des demandes interrompues.

Les développeurs qui veulent intégrer les diagnostics dans leurs propres outils peuvent lire le [Guide développeur de l'Engine](../engine/index.html).
