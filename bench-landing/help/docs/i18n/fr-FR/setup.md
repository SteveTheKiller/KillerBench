# Téléchargement et configuration

KillerBench 0.1.0 n'est pas encore publié. Aucun paquet ni téléchargement public de KillerBench n'est disponible pour l'instant. Cette page décrit les exigences actuelles du paquet pour que vous puissiez vous préparer sans télécharger de copie non officielle.

## Windows et runtime

- La cible de publication actuelle est **Windows x64**. Windows 11 x64 est la référence actuelle de validation native. Aucune distribution native ARM64, x86 ou Windows Server n'est fournie.
- KillerBench est empaqueté de deux façons. Le programme d'installation **dépend du framework** et nécessite **.NET Desktop Runtime 10 pour Windows x64**. Le .NET Runtime de base, ASP.NET Core Runtime et .NET Framework ne fournissent pas WPF. L'édition portable embarque son propre runtime et ne nécessite aucune installation séparée. L'outil en ligne de commande `killerbench-cli.exe` nécessite le .NET 10 Runtime pour Windows x64. Un SDK n'est nécessaire que pour le développement, pas pour un usage normal.
- Utilisez une version de Windows prise en charge par [.NET 10 sur Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows). La prise en charge du runtime ne signifie pas que les réparations de KillerBench sont validées sur chaque édition ou version prise en charge. La couverture de Windows 10 reste en attente et ses éditions prises en charge dépendent des exigences actuelles du cycle de vie de Microsoft.
- Procurez-vous le Desktop Runtime uniquement sur [la page de téléchargement .NET 10 de Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), en choisissant **.NET Desktop Runtime**, **Windows**, **x64**. Respectez les règles d'installation de logiciels de votre organisation.

Si `dotnet` est disponible, `dotnet --list-runtimes` doit afficher `Microsoft.WindowsDesktop.App 10.0.x` pour l'installation x64. Un runtime installé uniquement pour une autre architecture ne convient pas au paquet d'installation x64.

## Lorsqu'un paquet sera publié

1. Utilisez le téléchargement officiel accessible depuis ce site. Aucun lien de téléchargement de KillerBench n'est encore actif.
2. Lisez les exigences de ce paquet, la [licence GPLv3](../license.txt), les instructions relatives au Code source correspondant, les avis de tiers inclus et les informations de signature et de somme de contrôle. Un nom de fichier ne suffit pas à prouver l'authenticité.
3. Exécutez le programme d'installation pour le compte actuel, ou pour tous les utilisateurs après une demande d'élévation. Il ajoute un raccourci dans le menu Démarrer, un raccourci facultatif sur le bureau et une entrée dans Ajout/Suppression de programmes. L'édition portable s'exécute sans installation.
4. Lancez KillerBench avec votre compte habituel et suivez [Votre premier diagnostic](first-run.html). Les échecs d'accès du diagnostic restent visibles ; l'élévation est un choix distinct et délibéré.

Ne contournez pas les avertissements de sécurité de Windows pour exécuter une copie non vérifiée. Conservez une sauvegarde à jour avant de sélectionner une action qui modifie l'ordinateur.

Au démarrage, KillerBench interroge GitHub pour savoir s'il existe une version stable plus récente et demande votre accord avant de l'installer. Le téléchargement doit correspondre à la somme de contrôle SHA-256 publiée. Désactivez cette vérification avec **Rechercher les mises à jour au démarrage** dans À propos.

## Limites actuelles

La version actuelle comporte douze sections, un diagnostic en lecture seule, des rapports et des surfaces de réparation sélectionnées individuellement. La validation native des réparations est encore en cours. Une interface qui fonctionne, une compilation réussie ou le résultat d'un fixture ne prouvent pas le comportement d'une réparation sur un vrai ordinateur. Voir [Comment les modifications restent sûres](safety.html#development-status).

La console illustrative de la page d'accueil est une sortie synthétique. Les captures d'écran définitives de l'application et le premier paquet public sont encore en attente.

## Code source, licence et assistance

KillerBench est distribué sous licence GPLv3. Les instructions de fourniture du code source et les autorisations des polices tierces restent en attente pour le premier paquet public. Lisez [À propos de KillerBench](about.html) pour la disponibilité du code source, les avis de distribution, la confidentialité des preuves et les informations d'assistance. Le [guide de l'Engine](../engine/) s'adresse aux développeurs disposant d'une copie autorisée du code source ; il n'implique pas qu'un paquet public de l'Engine soit disponible.
