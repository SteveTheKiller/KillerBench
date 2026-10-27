# Réparation

Réparation regroupe les outils capables de corriger quelque chose. Chacun diagnostique d'abord. Une réparation n'est proposée que lorsque des preuves récentes montrent le problème exact qu'elle corrige, et elle ne s'exécute qu'après que vous avez activé **Réparer automatiquement les problèmes confirmés** ou confirmé vous-même une réparation individuelle. Chaque réparation est journalisée et vérifiée ensuite.

Guides pas à pas : [Réparer les fichiers système](repair-system-files.html) et [Réparer Windows Update](fix-windows-update.html).

## Réparation automatique et manuelle

**Réparer automatiquement les problèmes confirmés** permet à un outil de diagnostic d'exécuter, après l'analyse, les réparations dont il prouve la nécessité. Cette option est désactivée par défaut.

Pour une seule réparation de maintenance, ouvrez **Avancé : journaux et réparations manuelles** et choisissez **Réparations**. Choisissez **Magasin de composants (DISM)** ou **Fichiers système (SFC)**, lisez l'aperçu, cochez **Exécuter cette réparation sur ce PC** et appuyez sur **Exécuter la réparation**. Voir [Comment les modifications restent sûres](safety.html) pour ce qui se passe avant et après chaque modification.

## Outils de cette section

Utilisez-les après qu'un diagnostic a identifié un problème. Les réparations exigent des preuves confirmées et une approbation explicite.

### Services Windows essentiels

Vérifie ensemble les services Alimentation, Plug-and-Play, Gestionnaire des utilisateurs, Client de stratégie de groupe, Service Interface du magasin réseau, Mappeur de points de terminaison RPC, Appel de procédure distante, Lanceur de processus serveur DCOM, Service d'infrastructure des tâches en arrière-plan et CoreMessaging. La récupération facultative ne démarre que des services automatiques activés exacts dont l'arrêt est confirmé par des preuves récentes après la fenêtre de démarrage et des vérifications complètes des dépendances. Les services à démarrage manuel restent inchangés lorsqu'ils sont inactifs. Les modes de gestion de l'alimentation, les périphériques, les pilotes, les comptes, les identifiants, les profils, les stratégies, les cartes réseau, les adresses, les itinéraires, le DNS, la configuration du pare-feu, les inscriptions, les points de terminaison, les inscriptions de tâches, les données des applications, les autorisations des services et les types de démarrage sont conservés. Aucune actualisation de stratégie, recherche de périphériques, action de compte, modification réseau, modification d'alimentation, modification d'inscription ni lancement de tâche n'est demandé. La vérification ne couvre que l'état stable des services, pas le comportement complet des fonctionnalités.

### Fichiers système

Inventorie de manière délimitée les états des packages de maintenance, des fonctionnalités facultatives et des capacités de Windows, vérifie les prérequis, analyse les fichiers système protégés et ne choisit SFC automatiquement que lorsque la vérification en direct confirme une corruption. Les états en attente de packages, de fonctionnalités ou de capacités appellent un examen, mais n'autorisent ni installation, ni suppression, ni nettoyage, ni restauration, ni activation, ni désactivation.

### Référentiel WMI

Vérifie la disponibilité du service WMI, puis la cohérence du référentiel. La réparation automatique peut démarrer un service arrêté éligible et récupérer une corruption confirmée, puis vérifier chaque résultat. Les paramètres de service désactivé sont conservés. Les inscriptions lisibles sont fusionnées ; un contenu illisible peut nécessiter la réinstallation du fournisseur. Les clients de gestion peuvent être interrompus. Aucune réinitialisation, suppression ni restauration automatique.

### Notifications système de Windows

Lit l'historique récent du Gestionnaire de contrôle des services, puis vérifie ensemble Système d'événements COM+ et Service de notification d'événements système. La récupération facultative vérifie que Système d'événements COM+ est prêt avant de démarrer SENS. Les notifications existantes d'ouverture de session, de réseau, d'alimentation et COM+ peuvent reprendre selon la configuration actuelle. Les catalogues COM+, les abonnements, les inscriptions de notifications, les consommateurs, les comptes, les autorisations, les paramètres de récupération, les stratégies et les types de démarrage sont conservés. Aucun abonnement, inscription, consommateur, compte, autorisation ni stratégie n'est modifié. La vérification ne couvre que l'état stable des services, pas la remise complète des notifications.

### Service de stratégie de diagnostic

Examine l'historique opérationnel délimité de la Stratégie de diagnostic, l'historique exact des défaillances du service et la disponibilité actuelle du service. Les preuves incomplètes sont prioritaires. Les enregistrements historiques ne peuvent pas autoriser la récupération du service ni une action de dépannage. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les scénarios de diagnostic existants peuvent reprendre. La stratégie, les données de dépannage et le type de démarrage sont conservés. Aucun utilitaire de résolution des problèmes ni correctif suggéré n'est exécuté. La vérification ne couvre que l'état stable du service, pas la détection des pannes, l'achèvement du diagnostic ni le résultat d'une réparation.

### Rapport d'erreurs Windows

Examine la configuration des vidages sur incident, les métadonnées délimitées des fichiers de vidage, les rapports récents de plantage du système, l'historique exact des défaillances du service Rapport d'erreurs Windows et la disponibilité actuelle du service. Les rapports historiques n'établissent ni une panne actuelle ni une cause profonde et ne peuvent pas autoriser une récupération. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les rapports en file d'attente existants peuvent reprendre selon la stratégie actuelle. Les archives de rapports, les vidages, le consentement, la stratégie d'envoi et le type de démarrage sont conservés. Aucun envoi ni aucune suppression n'est demandé. La vérification ne couvre que l'état stable du service, pas la création, l'envoi ou la remise des rapports, la résolution des problèmes ni le diagnostic des plantages.

### Collecteur d'événements de Windows

Examine l'inventaire délimité des abonnements de Collecteur d'événements de Windows, l'historique exact des défaillances du service et la disponibilité actuelle du service. Un inventaire vide peut être intentionnel, et le nombre d'abonnements ou les défaillances historiques ne peuvent pas autoriser une récupération. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les abonnements existants peuvent reprendre la collecte d'événements à distance. Les abonnements, les événements transférés, les identifiants, les paramètres des canaux, la stratégie et le type de démarrage sont conservés. La vérification ne couvre que l'état stable du service, pas l'accessibilité des sources, l'authentification, la remise à distance, la fraîcheur des événements ni l'état des abonnements.

### Journal des événements Windows

Inspecte quatre canaux de diagnostic pour connaître leur état d'activation, le mode de rétention, la taille actuelle, la limite configurée, le nombre d'enregistrements et la saturation, puis vérifie la disponibilité du service Journal des événements Windows. La réparation automatique peut démarrer un service activé et arrêté après la vérification des dépendances et vérifier qu'il est en cours d'exécution. Une action ciblée confirmée séparément peut activer un seul canal de diagnostic exact désactivé et non géré, en conservant tous les autres paramètres signalés. Les journaux, les autorisations des canaux, la rétention, les limites de taille, les types de démarrage et les paramètres gérés sont conservés. La remise réussie des événements et la capacité d'archivage restent non vérifiées.

### Service de temps Windows

Vérifie la configuration, le service et la synchronisation du Temps Windows. La réparation automatique peut démarrer W32Time lorsque l'état actuel signale l'erreur exacte de service arrêté et que de nouvelles vérifications de stratégie, de mode et de dépendances réussissent, puis demander une resynchronisation et vérifier le nouvel état. Cela peut ajuster l'horloge à l'aide des sources déjà configurées. Le type de démarrage, la configuration des sources et les paramètres gérés sont conservés.

### Service Planificateur de tâches

Inventorie de manière délimitée l'état des tâches planifiées, les résultats d'exécution et le nombre d'exécutions manquées, puis les combine avec l'historique exact des défaillances du service Planificateur de tâches avant de vérifier la disponibilité actuelle du service. Les constats sur les tâches et les défaillances historiques du service ne peuvent pas autoriser le démarrage du service. La récupération facultative ne démarre qu'un service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les tâches activées existantes peuvent démarrer sous leurs comptes configurés, y compris les déclencheurs manqués. Aucune définition de tâche, principal de sécurité, référence d'identifiant, stratégie ni type de démarrage n'est modifié, et aucune tâche n'est démarrée ou supprimée directement. La vérification ne couvre que l'état stable du service, pas la réussite des actions, la validité des identifiants ni les résultats futurs des tâches.

### Service de profil utilisateur

Vérifie la disponibilité du Service de profil utilisateur. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les demandes existantes de connexion et de profil peuvent reprendre. Les dossiers de profil, les ruches du registre, les autorisations, les inscriptions de la liste des profils, les identifiants, les données utilisateur, la stratégie, les autorisations du service et le type de démarrage sont conservés. Aucune opération de profil n'est demandée. La vérification ne couvre que l'état stable du service, pas la connexion, le chargement des profils ni l'accès aux données utilisateur.

### Services de sauvegarde

Vérifie les services Cliché instantané des volumes, Fournisseur de cliché instantané de logiciel Microsoft, Sauvegarde Windows et Service du moteur de sauvegarde en mode bloc, puis lit l'état des rédacteurs VSS, les types de fournisseurs inscrits, les clichés actuels et l'allocation du stockage des clichés. Des services à démarrage manuel arrêtés peuvent être normaux lorsqu'ils sont inactifs. La récupération facultative ne démarre que des services automatiques arrêtés, après la vérification des dépendances, puis vérifie qu'ils sont en cours d'exécution. Le travail de sauvegarde planifié ou interrompu existant peut reprendre. Les paramètres de démarrage désactivé et manuel, les clichés, le stockage des clichés, les inscriptions des fournisseurs, les plans de sauvegarde, les catalogues, les planifications, les cibles, les identifiants et la rétention sont conservés. Aucune sauvegarde n'est sélectionnée ni reconfigurée, aucun service n'est redémarré et aucun cliché ni catalogue n'est supprimé. L'inscription des fournisseurs, l'état des rédacteurs, l'allocation du stockage et l'état stable des services ne vérifient ni l'achèvement de la sauvegarde, ni l'accès à la destination, ni la réussite d'une restauration.
