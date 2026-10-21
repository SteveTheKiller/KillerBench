# La technique de KillerBench

Un regard sous le capot : comment KillerBench lit un ordinateur Windows, garde ses preuves honnêtes et décide quand une modification est autorisée.

## Pile technique

KillerBench est une application Windows native. L'application de bureau, la ligne de commande et l'Engine qui les sous-tend partagent les mêmes preuves de diagnostic et les mêmes flux de réparation.

| Composant | Détail |
| --- | --- |
| Interface | WPF sur .NET 10 (`net10.0-windows`), Windows x64, cadre de fenêtre personnalisé |
| Engine | `Engine.Core`, `Engine` et `Engine.Windows`, sans aucune référence à WPF ni aux contrôles de bureau |
| Ligne de commande | `killerbench-cli`, un hôte distinct reposant sur le même Engine |
| Fournisseurs Windows | .NET, API Windows, PowerShell et outils en ligne de commande de Windows, chacun exécuté dans des limites fixes de durée, d'enregistrements et de taille |
| Rapports | Sortie brute, Rapport lisible, HTML, texte et JSON |
| Données de référence | Packs de connaissances versionnés pour les codes d'erreur, les identifiants matériels, les fabricants et les références d'assistance |
| Empaquetage | Programme d'installation : dépend du framework, nécessite .NET Desktop Runtime 10 pour Windows x64. Portable : autonome, sans installation de runtime |
| Apparence | 13 thèmes de la famille et 20 langues |

## Installation, dossiers et données

KillerBench existe sous deux formes. Le programme d'installation est le téléchargement standard et permet une installation par utilisateur ou pour tous les utilisateurs. L'édition portable embarque son propre runtime .NET et s'exécute sans installation.

KillerBench s'exécute avec les droits du compte qui l'a lancé. Il ne s'élève pas de lui-même. Lancez-le avec **Exécuter en tant qu'administrateur** lorsque des preuves protégées, comme les protecteurs BitLocker, ou une réparation nécessitent un accès administrateur. L'élévation ne sélectionne ni n'approuve jamais une réparation.

### Ce que fait le programme d'installation

Le programme d'installation vérifie et extrait l'application dans `%LOCALAPPDATA%\Programs\KillerBench\`, ou dans `%ProgramFiles%\KillerBench\` après une demande d'élévation pour tous les utilisateurs. Il ajoute un raccourci dans le menu Démarrer, un raccourci facultatif sur le bureau et une entrée dans Ajout/Suppression de programmes avec une désinstallation normale et silencieuse. L'édition portable vérifie et extrait son contenu dans `%LOCALAPPDATA%\KillerBench\Portable\` pour l'exécution en cours et nettoie les copies abandonnées au démarrage suivant.

### Où se trouvent les éléments

| Emplacement | Contenu |
| --- | --- |
| `%LOCALAPPDATA%\KillerBench\appearance.json` | Thème, couleur d'accent, échelle de l'interface et langue |
| `%LOCALAPPDATA%\KillerBench\Workflows\` | Un journal JSON par exécution de flux de travail : décisions, sortie de commande délimitée et résultats des réparations |
| `%LOCALAPPDATA%\KillerBench\Repairs\` | Journaux d'exécution des réparations de maintenance comme DISM et SFC |
| Dossiers de fonctionnalités sous `%LOCALAPPDATA%\KillerBench\` | Protections en attente et résultats des actions protégées individuelles, par exemple `BitLockerBackup`, `Time`, `Firewall` et `Profiles` |
| `%LOCALAPPDATA%\KillerBench\agent-repairs.json` | Le niveau de réparation KillerMCP choisi dans À propos |
| `HKCU` ou `HKLM\Software\KillerBench` | Le chemin d'installation, selon la portée de l'installation |
| Un fichier `.kbench` de votre choix | Une capture enregistrée avec ses preuves conservées |

Rien n'est envoyé. Ne supprimez un journal que lorsque vous n'avez plus besoin des preuves d'un résultat incertain.

## Le diagnostic d'abord

KillerBench traite le diagnostic et la modification comme deux tâches distinctes. Le diagnostic ne fait que lire. Une modification n'a lieu que lorsque vous choisissez une réparation ou une action précise, examinez ses options et ses prérequis, puis la confirmez.

L'analyse complète du système est purement diagnostique, y compris lorsqu'un flux de travail ciblé a la réparation automatique activée. Les captures enregistrées, les journaux importés et les événements historiques n'autorisent jamais une réparation.

### Collecte délimitée

Chaque commande native et chaque fournisseur Windows s'exécute dans des limites fixes de durée, d'enregistrements et de taille. Un fournisseur consigne son code de sortie, son dépassement de délai, sa troncature, sa sortie standard et sa sortie d'erreur. Un fournisseur en échec ou tronqué ne peut jamais être lu comme une réponse propre et vide.

### Conservation minimale

Les collecteurs conservent ce dont un constat a besoin. De nombreux résultats gardent des noms, des catégories, des nombres, des états et des horodatages, en laissant de côté les identifiants, les lignes de commande, les chemins complets, les identités de comptes, les points de terminaison réseau et les charges utiles d'événements sans rapport.

## Des preuves que vous pouvez suivre

Chaque constat est lié aux preuves qui l'ont produit, et le rapport sépare toujours ce qui a été prouvé de ce qui a seulement été observé.

| État | Signification |
| --- | --- |
| À examiner | Les preuves étayent un problème qui mérite votre attention. |
| Information | Des preuves ont été collectées et sont affichées pour le contexte. |
| Incomplètes ou Inconnu | Une vérification n'a pas pu se terminer ou la preuve n'était pas disponible. Cela ne compte jamais comme un bon état. |
| Historique | Un événement passé. Il explique l'historique mais ne prouve pas une panne actuelle. |
| Vérifié | Une réparation ou une modification a été confirmée en relisant le résultat. |

### Inconnu est un résultat

Inconnu signifie que l'accès a été refusé, qu'une source manquait, qu'un fournisseur a échoué, que la sortie était mal formée, qu'une limite a été dépassée ou que les preuves se contredisaient. KillerBench affiche cette limite et la prochaine étape utile au lieu de la masquer.

### L'historique reste l'historique

Les enregistrements d'événements et les journaux importés décrivent ce qui a été consigné à un moment passé. Ils peuvent orienter vers un code d'erreur ou la prochaine portée de diagnostic. Ils ne prouvent pas que la panne est toujours active.

## Réparations protégées

Les réparations sont des opérations typées dotées de règles d'éligibilité, d'exécution, de vérification et de récupération. Ce ne sont pas des chaînes de commande tirées d'une ligne de journal. Chaque réparation prise en charge suit la même séquence :

1. Collecter des preuves actuelles complètes.
2. Correspondre à une recette prise en charge.
3. Examiner les options requises et leurs conséquences.
4. Confirmer les prérequis administratifs et de stratégie.
5. Recollecter les preuves décisives juste avant la modification.
6. Écrire un enregistrement de préparation durable.
7. Exécuter une seule action native délimitée.
8. Collecter de nouvelles preuves de vérification.
9. Consigner un résultat vérifié, bloqué, arrêté ou incertain.

Si les preuves décisives changent avant l'exécution de l'action, KillerBench s'arrête sans l'effectuer.

### Choix explicites

Les réparations qui dépendent d'une option exigent de vous un choix exact, comme un volume, une file d'impression, une destination de clé de récupération, une valeur d'alimentation, une version de package ou une source de réparation. KillerBench n'invente jamais ces valeurs.

### Ordinateurs gérés

La stratégie de groupe et la gestion des appareils peuvent rendre une modification locale inefficace ou inappropriée. Les réparations prises en charge vérifient qui contrôle un paramètre et s'arrêtent lorsque ce contrôle est géré ou inconnu. Les paramètres contrôlés par une stratégie ne sont pas touchés.

### Réparation automatique

La réparation automatique ne fait que laisser avancer les recettes éligibles. Elle ne contourne pas les preuves, ne choisit pas les options manquantes, n'ignore pas les stratégies, ne relance pas une action incertaine, ne redémarre pas Windows et ne transforme pas en réparation un constat qui demande un examen.

## Journaux et interruptions

Avant que KillerBench ne demande une modification à Windows, il consigne ce qu'il s'apprête à faire. Chaque modification est enregistrée dans un journal sous `%LOCALAPPDATA%\KillerBench`.

Après la modification, KillerBench relit le résultat et n'indique **Vérifié** que lorsque Windows affiche l'état attendu. Si Windows signale une réussite mais que l'état n'a pas changé, le résultat le dit et rien n'est répété.

Un processus peut s'arrêter après que Windows a reçu une demande, mais avant la fin de la vérification. L'enregistrement en attente reste, et un rapprochement ultérieur lit l'état actuel pour déterminer ce qui s'est passé. Le rapprochement n'est pas une restauration automatique et ne répète jamais la modification d'origine. Conservez le journal lorsque le résultat est incertain.

## Constantes et limites

| Spécification | Valeur |
| --- | --- |
| Couverture du diagnostic | L'analyse complète du système parcourt 49 domaines de diagnostic et vérifie 96 recettes de réparation protégées |
| Lecture des sources texte | 1 000 000 caractères par source, 2 000 lignes correspondantes affichées |
| Sources de session | 12 dans la barre latérale ; une capture contient jusqu'à 24 sources et 64 Mo |
| Requêtes d'événements | Les 200 événements les plus récents par canal sur les 7 derniers jours |
| Analyse de Windows Update | 15 secondes par source, deux minutes au total |
| Analyse des journaux | 10 000 entrées analysées, 256 codes distincts, 8 extraits de 320 caractères par code |
| Archive CBS compressée | Archive de 256 Mio, membre de journal de 512 Mio, extraction de 30 secondes |
| CHKDSK en lecture seule | Dix minutes ; une réparation est limitée à 65 minutes |
| Vérifications DNS et TCP | Dix secondes par défaut, une minute au maximum |
| Inventaire des services | Jusqu'à 1 024 services installés |
| Packs de connaissances | Jusqu'à 32 ouverts à la fois |
| Examen des fichiers volumineux | Fichiers d'au moins 100 Mo |
| Moniteur d'activité de maintenance | Intervalle de 1 à 60 secondes, de 1 à 120 instantanés |
| Brouillons de tâches planifiées | Limite d'exécution de 1 à 4 320 minutes |
| Sources de temps personnalisées | Jusqu'à huit homologues NTP |
| Exportation de l'activité | Jusqu'à 150 entrées conservées |
| Langues et thèmes | 20 langues, 13 thèmes avec variantes d'accent |

## Ce qu'il ne fera pas

- Effacer un TPM, supprimer des protecteurs qu'il n'a pas créés ou afficher des mots de passe de récupération.
- Modifier un paramètre contrôlé par une stratégie de groupe ou par la gestion des appareils.
- Exécuter une réparation simplement parce qu'un événement historique ou un rapport importé mentionne un problème.
- Transformer des preuves manquantes, partielles ou en échec en verdict de bon état.

## Rapports

Chaque exécution produit un rapport que vous pouvez lire, filtrer et exporter. Un rapport distingue :

1. Ce qui a été inspecté
2. Le problème trouvé
3. Les preuves qui étayent le constat
4. L'action proposée ou effectuée
5. La façon dont le résultat a été vérifié
6. Ce qui reste inconnu ou nécessite un redémarrage

**Sortie brute** conserve les preuves techniques avec leurs numéros de ligne d'origine pour que vous puissiez remonter de chaque constat à sa source. Le **Rapport lisible** résume la même exécution en langage clair, en commençant par la prochaine étape sûre. Les rapports HTML utilisent le système de thèmes de Killer Tools et restent lisibles sans l'application. Le texte privilégie la portabilité, et le JSON conserve des champs structurés pour un autre programme.

## Journaux enregistrés et captures

KillerBench peut examiner des preuves sans rien exécuter sur l'ordinateur. Déposez un journal texte sur la fenêtre, ou collez son chemin et choisissez **Ouvrir un journal**. Les lignes CBS, CSI et Setup sont analysées avec leurs horodatages, et les constats renvoient à la ligne d'origine exacte. Les fichiers EVTX et XML d'événements peuvent être importés de la même façon.

**Analyser Windows Update** est une collecte à la demande, en lecture seule, des journaux CBS, DISM, Panther et de restauration de Setup, des canaux d'événements de mise à jour et de maintenance, des états des services de mise à jour, de l'espace libre, de la durée de fonctionnement, des indicateurs de redémarrage en attente et de la stratégie de mise à jour sélectionnée. **Enregistrer la capture** écrit les preuves conservées dans un fichier `.kbench`. **Ouvrir une capture** le rouvre sans les fichiers d'origine et l'évalue à nouveau avec les règles installées, de sorte que les mêmes règles donnent les mêmes constats.

## Mode démo

Lancez `KillerBench.exe --demo` pour essayer **Exécuter le diagnostic** sur un poste de travail fictif. Chaque flux de travail produit des preuves synthétiques et une progression minutée. Aucun fournisseur Windows n'est appelé, aucune commande de réparation n'est exécutée et aucun journal n'est écrit. Les boutons d'outils distincts, hors de **Exécuter le diagnostic**, restent actifs ; ne les utilisez donc pas comme actions de démonstration.

`KillerBench.exe --demo-screenshot` ouvre, en lecture seule, un rapport d'Analyse complète du système pour un ordinateur fictif figé. Les clics dans l'espace de travail, les raccourcis et le dépôt de fichiers sont bloqués.

## Packs de connaissances

Les données de référence volumineuses et souvent mises à jour se trouvent dans des packs de connaissances plutôt que dans la logique de diagnostic : codes d'erreur de Windows et de Setup, identifiants de fabricants et de périphériques, références PCI, USB, ACPI et UEFI, données de fabricants par OUI réseau, emplacements de pilotes et d'assistance, et liens d'assistance sélectionnés.

Chaque pack indique son format, la version de son schéma, sa source, la révision de la source, l'heure de génération et le hachage du contenu, et KillerBench les valide avant usage. Un pack inconnu ou non valide reste indisponible au lieu de céder la place à une supposition. Une mise à jour est téléchargée dans un emplacement temporaire, validée, puis remplace le pack actif en une seule étape ; une mise à jour qui échoue laisse donc le pack précédent en place.

Les données de connaissances peuvent expliquer un identifiant ou suggérer où regarder ensuite. Elles ne peuvent pas transformer un code historique en panne actuelle, choisir automatiquement un pilote ni autoriser une réparation.

## Mises à jour et signature

Au démarrage, KillerBench demande à GitHub la dernière version stable, avec une limite de quatre secondes, et ne propose une mise à jour que si cette version est plus récente. Vous pouvez désactiver cette vérification avec **Rechercher les mises à jour au démarrage** dans À propos, ou dans l'invite de mise à jour. Le téléchargement est le paquet qui correspond à la copie en cours d'exécution, programme d'installation ou portable, et son SHA-256 doit correspondre au `SHA256SUMS.txt` de la version avant toute installation. L'application se ferme et se rouvre d'elle-même pour terminer. Il n'y a aucun service en arrière-plan.

La carte À propos affiche la version, l'éditeur et l'empreinte de la signature de code, ainsi que le SHA-256 de l'exécutable en cours, pour que vous puissiez vérifier la copie dont vous disposez.

## KillerMCP

KillerBench n'inclut aucun agent d'IA. [KillerMCP](../mcp.html) peut en connecter un au même Engine. Les **Options de KillerMCP** dans À propos déterminent ce que cet agent peut faire :

| Paramètre | Effet |
| --- | --- |
| Désactivé (par défaut) | L'agent ne peut demander aucune réparation. |
| Autoriser les réparations sûres | L'agent peut demander les réparations classées comme sûres. |
| Autoriser aussi les réparations perturbatrices | Les réparations perturbatrices deviennent disponibles, et chacune nécessite toujours une approbation. |

Seule une personne présente devant cet ordinateur peut relever le niveau. Après un résultat de réparation incertain, KillerMCP écrit un verrou, et les réparations de l'agent restent bloquées jusqu'à ce que quelqu'un le retire dans À propos.

## Localisation

L'interface est disponible en 20 langues : anglais, bengali, tchèque, allemand, espagnol, français, hongrois, italien, japonais, kazakh, norvégien, polonais, portugais (Brésil), russe, slovène, turc, ukrainien, vietnamien, et chinois simplifié et traditionnel. Un changement de langue s'applique immédiatement, y compris aux noms des sections et des flux de travail, au texte d'accueil et à un rapport lisible ouvert. Tout texte manquant s'affiche en anglais.

Les preuves, les constats de l'Engine, les journaux et la Sortie brute restent en anglais, comme un journal Windows, afin qu'un rapport se lise de la même façon sur chaque ordinateur et qu'on puisse y rechercher un texte exact.

## Ligne de commande et codes de sortie

`killerbench-cli` exécute le même Engine sans l'application de bureau. La progression va vers la sortie d'erreur standard et le JSON vers la sortie standard. La plupart des commandes affichent un aperçu par défaut, et une modification nécessite `--execute`. Les commandes `-recover` correspondantes examinent une action interrompue sans la répéter. Voir [Exécuter KillerBench depuis des scripts](use-the-cli.html) et la [référence des commandes](cli.html).

| Code de sortie | Commandes de flux de travail | Analyse et ouverture |
| --- | --- | --- |
| 0 | Aucun problème non résolu dans les portées vérifiées | Couverture terminée |
| 1 | Erreur de démarrage ou de journal | Échec |
| 2 | Arguments non valides | Arguments non valides |
| 3 | Attention requise | Collecte partielle |
| 4 | Redémarrage requis avant la vérification | |
| 130 | Arrêté | Annulé ou délai global dépassé |

Aucun code de sortie ne certifie qu'un ordinateur est en bon état.

## Architecture

L'Engine est divisé en trois projets sans interface. `Engine.Core` fournit les primitives et les recettes déclaratives, `Engine` gère les flux de travail partagés et les preuves, et `Engine.Windows` inscrit les fournisseurs natifs. L'application de bureau, la CLI et les tests sont des hôtes distincts.

| Couche | Responsabilité |
| --- | --- |
| Collecteurs de capacités | Lire un état Windows délimité pour une portée exacte |
| Analyseurs de preuves | Valider la forme, l'identité, l'exhaustivité et les limites |
| Interprétation du diagnostic | Convertir les preuves en états explicites de flux de travail |
| Orchestration des flux de travail | Combiner vérifications, progression, recettes et journaux |
| Coordinateurs de réparation | Revérifier l'éligibilité, préparer, modifier, vérifier et rapprocher |
| Rapports | Présenter les problèmes, les actions, les résultats et les limites |
| Packs de connaissances | Résoudre des données de référence versionnées sans modifier la logique |

Les collecteurs échouent de manière fermée lorsque l'identité est ambiguë, que la sortie est mal formée, qu'une limite est dépassée ou que l'autorité requise ne peut pas être inspectée. L'Engine cible aujourd'hui Windows et n'est pas multiplateforme. Le [Guide développeur de l'Engine](../engine/index.html) couvre l'API, la CLI et l'intégration de l'Engine dans un autre hôte .NET.

## Confidentialité et réseau

Les diagnostics et l'analyse des journaux s'exécutent en local, sans service d'IA ni compte KillerBench. Les journaux et les preuves conservées restent dans les dossiers `%LOCALAPPDATA%\KillerBench` du compte local.

Les actions que vous sélectionnez peuvent contacter leur destination, comme Windows Update, des services de téléchargement de logiciels ou la sauvegarde de la clé de récupération dans Active Directory ou Entra ID. La vérification des mises à jour au démarrage contacte GitHub, et les mises à jour des connaissances et le programme d'installation de KillerMCP contactent leurs services configurés. Un client d'IA utilisant [KillerMCP](../mcp.html) peut recevoir le résultat de référence qu'il a demandé.

Les journaux bruts, les fichiers importés et les preuves exportées peuvent contenir des noms d'ordinateurs, des comptes, des chemins ou du texte source sensible. Examinez-les avant de les partager, et n'incluez jamais de clés de récupération, d'identifiants ni de données de clients dans un rapport public.

## État du développement

KillerBench est en développement et aucun téléchargement public n'est encore disponible. La couverture des réparations continue de s'étendre et la validation native des réparations est encore en cours. Une compilation réussie ou une vérification simulée ne prouve pas le comportement natif d'une réparation. Des machines virtuelles Windows jetables servent à valider chaque modification native sur différentes versions de Windows et différents états de panne. Voir [Comment les modifications restent sûres](safety.html) pour le modèle de sécurité complet.

## Glossaire

Définitions en langage clair des termes utilisés sur cette page, par ordre alphabétique.

| Terme | Signification |
| --- | --- |
| BITS | Service de transfert intelligent en arrière-plan, le service Windows qui télécharge les mises à jour et d'autres fichiers en arrière-plan. |
| Capture | Un fichier `.kbench` enregistré contenant les preuves conservées, pour pouvoir les réexaminer sans l'ordinateur d'origine. |
| CBS | Component-Based Servicing, le sous-système Windows qui installe les mises à jour et les fonctionnalités. Son journal est `CBS.log`. |
| DISM | Deployment Image Servicing and Management, l'outil Windows qui vérifie et répare le magasin de composants. |
| Échec fermé | Lorsque les preuves sont ambiguës, manquantes ou trop volumineuses, le résultat est Inconnu et aucune modification n'est autorisée. |
| Élévation | Exécution avec des droits d'administrateur après une invite de Windows. KillerBench ne s'élève jamais de lui-même. |
| Flux de travail | L'exécution d'un outil : les vérifications qu'il collecte, les recettes qu'il peut utiliser et le rapport qu'il produit. |
| Journal | Un enregistrement JSON local écrit avant une modification et mis à jour après, pour pouvoir examiner plus tard une action interrompue. |
| Pack de connaissances | Un fichier versionné et validé de données de référence, comme des codes d'erreur ou des identifiants matériels. |
| Panther | Le dossier dans lequel le programme d'installation de Windows écrit ses journaux de mise à niveau. |
| Preuve | Ce que KillerBench a réellement lu dans Windows, conservé avec sa source pour qu'un constat puisse y être rattaché. |
| Propriétaire de stratégie | La stratégie de groupe ou la gestion des appareils qui contrôle un paramètre. KillerBench ne touche pas aux paramètres contrôlés. |
| Protection en attente | L'enregistrement qui marque une modification comme peut-être envoyée à Windows. Il bloque toute répétition jusqu'à ce que le rapprochement lise le résultat. |
| Rapprochement | Lire l'état actuel après une interruption pour savoir ce qui s'est passé, sans répéter la modification. |
| Recette | Une réparation typée dotée de ses propres règles d'éligibilité, d'exécution, de vérification et de récupération. |
| SFC | Vérificateur des fichiers système, l'outil Windows qui vérifie et restaure les fichiers système protégés. |
| TPM | Module de plateforme sécurisée, la puce de sécurité qu'utilise BitLocker pour protéger la clé du lecteur. |
| Vérifié | Le résultat a été relu dans Windows et correspond à ce que la modification devait faire. |
