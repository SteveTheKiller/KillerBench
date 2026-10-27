# Applications

Applications examine les programmes installés, les packages du Store, Office et Outlook, les extensions de navigateur, les applications au démarrage et la recherche. Les outils de suppression et de réinitialisation affichent toujours d'abord un examen et ne modifient que ce que vous sélectionnez.

Guides pas à pas : [Configurer un nouvel ordinateur](set-up-new-pc.html).

## Outils de cette section

Examinez les applications installées, les packages du Store, Office et Outlook, les extensions de navigateur et la recherche.

### Applications installées

Lit les inscriptions de désinstallation des applications dans la portée sélectionnée de l'ordinateur ou du compte en cours d'exécution, puis combine cette portée avec des preuves délimitées d'erreurs du journal Application, du Moniteur de fiabilité et de redémarrages. Les autres comptes et les applications portables sont exclus. Les inscriptions et l'historique général ne prouvent pas exactement l'état ni la propriété d'une application. Aucune action de programme d'installation, de désinstallation, d'application, de processus, de service, de fichier, d'inscription, de compte, de stratégie ou de redémarrage n'est sélectionnée.

### Packages d'applications

Inventorie les packages Appx/MSIX inscrits pour la portée de compte sélectionnée. Le compte en cours d'exécution est le choix par défaut ; tous les utilisateurs exige un accès administrateur. Indique les identités exactes, les indicateurs de framework et de non-amovibilité, et le nombre de dépendances. Les packages provisionnés, les applications Win32, la propriété gérée et les données des applications ne sont pas évalués. Aucun package n'est supprimé.

### Extensions de navigateur

Inventorie de manière délimitée les identités des extensions Chrome, Edge, Brave et Firefox du compte en cours d'exécution. L'historique de navigation, les identifiants, les paramètres des extensions, le contenu des manifestes, les commandes et les valeurs de stratégie sont exclus. La présence ne prouve ni l'activation, ni la confiance, ni la propriété, ni qu'une suppression est sans risque. Aucun navigateur, aucune extension ni aucune stratégie n'est modifié.

### Applications au démarrage

Inventorie les noms d'inscription Run et RunOnce de l'ordinateur et du compte en cours d'exécution, les noms de fichiers du dossier Démarrage, les états d'approbation du démarrage de Windows et les noms et états des tâches planifiées au démarrage ou à l'ouverture de session, puis renvoie une seule conclusion sur l'environnement de démarrage. Les lignes de commande, les chemins cibles, les horodatages binaires d'approbation, les actions des tâches, les arguments, les comptes et les identifiants ne sont pas collectés. Une inscription ne prouve pas qu'un élément s'est exécuté, a causé un retard, est indésirable ou peut être désactivé sans risque. L'état d'approbation ne prouve pas qu'une inscription correspondante existe. Aucune modification d'inscription, de fichier, d'application, de tâche, de service ou de stratégie n'est sélectionnée.

### Suppression d'applications Microsoft

Examine les inscriptions sélectionnées de OneDrive, Outlook, Office/M365, Project et Teams avec des choix de portée de compte et de conservation. Les inventaires de l'ordinateur, du compte en cours d'exécution et des packages inscrits sont mis en correspondance ; l'examen pour tous les utilisateurs inclut aussi les packages provisionnés pour les nouveaux utilisateurs. Un package pris en charge du compte actuel peut recevoir un plan de protection exact et distinct dans l'application de bureau ou la CLI. La suppression de ce seul package est une action de bureau protégée distincte, avec ses propres confirmations. L'examen lui-même ne modifie aucun processus, application, package, fichier, identifiant, emplacement de dossier, valeur de registre ni donnée d'utilisateur.

### Réinitialisation d'Outlook

Examine le nouvel Outlook, Outlook classique ou les deux, avec des choix explicites pour les caches, les paramètres du profil, l'authentification, les fichiers OST, les compléments et la réinstallation du nouvel Outlook. L'examen du compte en cours d'exécution vérifie des emplacements connus délimités, des inscriptions exactes et la fermeture des processus. Une action distincte, confirmée séparément et limitée au cache, conserve les répertoires de cache exacts sous des noms de récupération uniques et vérifie chaque déplacement. L'examen pour tous les utilisateurs et les autres domaines de réinitialisation restent en examen seul. Aucun cache conservé, profil, fichier OST, identifiant, complément, package ou paramètre n'est supprimé.

### Inventaire Foxit

Inventorie les produits Foxit installés sur l'ordinateur et les valeurs de registre de leur programme de mise à jour dans les deux vues. Les installations utilisateur, la stratégie effective et la propriété des produits ne sont pas vérifiées. Aucune application ni aucun paramètre n'est modifié.

### Service Windows Installer

Vérifie les stratégies de restriction, de restauration, d'élévation, de journalisation et de débogage d'Installer, les marqueurs d'opérations en cours, les binaires du moteur et la visibilité des packages en cache inscrits avant la récupération du service. Met en corrélation les résultats récents en échec et réussis uniquement pour les identités de produit exactes signalées. Si les prérequis sont satisfaits, la réparation automatique peut démarrer le service activé et vérifier qu'il est En cours d'exécution. Les résultats historiques, la journalisation, la visibilité du cache et les constats de débogage n'autorisent aucune modification de package ou de stratégie. Les stratégies, les journaux, les marqueurs de transaction, les types de démarrage et les packages en cache sont conservés. L'installation d'un package précis n'est pas testée.

### Windows Search

Vérifie la disponibilité du service Windows Search, l'activité actuelle de SystemIndex et l'historique récent des événements de Search. La réparation automatique peut démarrer un service activé et arrêté et vérifier qu'il est en cours d'exécution. Un choix explicite et distinct de reconstruction de l'index conserve l'index précédent, démarre un nouvel index et vérifie l'état exact du service et du répertoire. Les emplacements indexés, la stratégie, le type de démarrage et les données conservées sont préservés. Démarrer le service ou reconstruire l'index peut reprendre l'indexation en arrière-plan. L'exhaustivité des résultats de recherche reste non testée.

### Préparation des applications

Vérifie la disponibilité du service Préparation des applications. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. La première connexion et le provisionnement des applications existants peuvent reprendre selon la stratégie actuelle. Les packages, les inscriptions, les licences, les profils utilisateur, les données des applications, la stratégie et le type de démarrage sont conservés. KillerBench ne sélectionne aucune opération de package. La vérification ne couvre que l'état stable du service, pas la connexion, le provisionnement ni le lancement des applications.

### Service de référentiel d'état

Vérifie la disponibilité du Service de référentiel d'état. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les demandes existantes d'état du modèle d'application peuvent reprendre. Les inscriptions, les packages, les licences, les profils, les données du navigateur et des applications, le contenu du référentiel, la stratégie et le type de démarrage sont conservés. Aucune réinitialisation, réinscription ni suppression n'est demandée. La vérification ne couvre que l'état stable du service, pas la cohérence du référentiel ni le fonctionnement des applications.

### Service de déploiement AppX

Vérifie la disponibilité du Service de déploiement AppX. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les demandes existantes de déploiement, d'inscription ou de maintenance peuvent reprendre selon la stratégie actuelle. Les packages, les inscriptions, les licences, les profils, les données des applications, le contenu du référentiel, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucune opération de package. La vérification ne couvre que l'état stable du service, pas le déploiement, l'inscription ni le lancement des applications.

### Service de licences de client

Vérifie la disponibilité du Service de licences de client. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les demandes existantes de licences d'applications du Store peuvent reprendre selon la stratégie actuelle. Les packages, les inscriptions, les licences, les jetons, les comptes, les profils, les données des applications, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucune opération de package, de licence, de jeton ou de compte. La vérification ne couvre que l'état stable du service, pas la validation des licences ni le lancement des applications.

### Service d'installation du Microsoft Store

Vérifie l'historique récent des installations du Microsoft Store et la disponibilité exacte du service d'installation du Store. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les demandes existantes de téléchargement et d'installation du Store peuvent reprendre selon la stratégie actuelle. Les packages, les inscriptions, les licences, les comptes, les données des applications, l'état des téléchargements, les sources, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucune opération de package, de téléchargement, de licence, de compte, de source ou d'installation. La vérification ne couvre que l'état stable du service, pas les résultats des téléchargements, des installations, des licences ni des applications.

### Service Gestionnaire de licences Windows

Vérifie la disponibilité du Service Gestionnaire de licences Windows. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les vérifications existantes de licences d'applications peuvent reprendre selon la stratégie actuelle. Les packages, les inscriptions, les licences, les jetons, les comptes, les données des applications, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucune opération de package, de licence, de jeton, de compte, d'inscription ou de droit d'utilisation. La vérification ne couvre que l'état stable du service, pas la validation des licences, les droits d'utilisation ni le lancement des applications.

### Service PushToInstall de Windows

Vérifie la disponibilité du Service PushToInstall de Windows. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Une demande d'installation à distance déjà approuvée peut reprendre selon la stratégie actuelle. Les packages, les inscriptions, les licences, les comptes, les données des applications, les demandes en file d'attente, les sources, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench ne crée, n'approuve et ne sélectionne aucune demande, aucun package, aucune source, aucun compte ni aucune installation. La vérification ne couvre que l'état stable du service, pas le traitement des demandes, le téléchargement, l'installation ni les résultats des applications.
