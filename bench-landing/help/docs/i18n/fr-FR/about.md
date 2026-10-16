# À propos de KillerBench

KillerBench est un atelier de diagnostic, de réparation et de maintenance de Windows créé par Steve the Killer, qui fait partie de la [famille Killer Tools](https://killertools.net). La version 0.1.0 n'est pas encore publiée. Son application de bureau, son Engine et sa CLI partagent les preuves de diagnostic et les flux de réparation pris en charge.

## Disponibilité et portée

Aucun téléchargement public de KillerBench ni paquet de l'Engine n'est encore disponible. [Téléchargement et configuration](setup.html) décrit la cible Windows x64 actuelle et les exigences du Desktop Runtime. La validation native des réparations est encore en cours ; la présence d'un outil ne prouve pas que chaque panne, interruption ou configuration de Windows a été testée.

## Avis sur le code source et la distribution

KillerBench est distribué sous licence **GPLv3**. Sa licence de distribution est la [Licence publique générale GNU version 3](../license.txt). La licence décrit les autorisations et les conditions de copie, de modification et de distribution de l'œuvre couverte.

Le [dépôt public de KillerBench](https://github.com/SteveTheKiller/KillerBench) fournit pour l'instant des informations sur le projet. L'implémentation complète n'y est pas disponible publiquement. Ces guides pour développeurs décrivent une copie autorisée du code source ; ils ne proposent pas de téléchargement public du code source.

Avant de distribuer un binaire, son éditeur doit fournir le Code source correspondant selon une méthode autorisée par la [section 6 de la GPLv3](https://www.gnu.org/licenses/gpl-3.0.en.html#section6), y compris le code source et les éléments de compilation exigés par la licence. Les instructions de fourniture du code source du premier paquet public et les avis de tiers inclus restent en attente. La GPLv3 n'accorde pas l'autorisation de redistribuer des polices tierces ; les autorisations actuelles des polices doivent encore être vérifiées. Examinez la licence, les instructions relatives au code source et les avis du paquet publié avant de le redistribuer.

## Confidentialité et services externes

Les diagnostics et l'analyse des journaux s'exécutent en local, sans service d'IA ni compte KillerBench. Les journaux et les preuves conservées utilisent les dossiers `%LOCALAPPDATA%\KillerBench` du compte local. Les actions sélectionnées peuvent contacter Windows Update, des services de téléchargement de logiciels, des annuaires ou d'autres destinations désignées. La vérification des mises à jour au démarrage contacte GitHub, et les mises à jour des connaissances et la page d'installation de MCP contactent leurs services configurés. Un client d'IA utilisant MCP peut recevoir le résultat de référence renvoyé.

Ce site enregistre les choix de thème et de couleur d'accent dans le stockage local du navigateur. Ses guides n'exécutent aucun diagnostic sur votre ordinateur. Les journaux bruts, les fichiers importés et les preuves exportées peuvent contenir des noms d'ordinateurs, des comptes, des chemins ou du texte source sensible. Examinez-les avant de les partager ; n'incluez pas de clés de récupération, d'identifiants ni de données de clients dans un rapport public.

## Assistance

Les informations sur le projet sont disponibles dans le dépôt public. Pour obtenir de l'aide, utilisez les coordonnées publiées sur [le site de l'auteur](https://thekiller.net) ; indiquez la version de l'application, la version de Windows, l'outil exact, le résultat attendu et le résultat observé. Examinez et expurgez les preuves avant de les envoyer, et utilisez un canal privé approuvé pour les informations de clients. Conservez les journaux lorsque le résultat d'une réparation est incertain.

## Guides associés

- [Comment les modifications restent sûres](safety.html)
- [Rapports et preuves](reports.html)
- [Guide développeur de l'Engine](../engine/)
- [KillerMCP](../mcp.html)
