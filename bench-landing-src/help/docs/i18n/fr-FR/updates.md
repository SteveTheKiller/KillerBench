# Mises à jour

Mises à jour couvre Windows Update, les services qui le font fonctionner et le magasin de composants. Vous pouvez voir ce qui est installé, ce qui est en attente et pourquoi une mise à jour échoue, et examiner les mises à jour disponibles avant de choisir celles à installer.

Guides pas à pas : [Réparer Windows Update](fix-windows-update.html) et [Réparer les fichiers système](repair-system-files.html).

## Outils de cette section

Surveillez l'activité de maintenance, examinez les mises à jour disponibles et réparez la chaîne Windows Update.

### Moniteur d'activité de maintenance

Observe de manière délimitée l'activité de maintenance de Windows Update et de Setup sur un ou plusieurs instantanés. Les options règlent la fréquence d'actualisation et indiquent si les événements de mise à jour, l'historique des mises à jour, la tâche planifiée exacte de Windows Update, les états exacts des services, les indicateurs de redémarrage, les transferts en arrière-plan du compte actuel, l'activité d'Optimisation de la distribution et l'activité de Windows Installer limitée par la confidentialité sont inclus. Les identifiants de processus, les lignes de commande, les utilisateurs et les chemins sont exclus. L'activité ne prouve ni une progression, ni un achèvement, ni un blocage, ni une panne. Aucune action de mise à jour, d'installation, de transfert, de tâche, de service, de processus, de stratégie, de redémarrage ou de réparation n'est demandée.

### Mises à jour disponibles

Découvre les mises à jour de logiciels et de pilotes sélectionnées à l'aide de la source configurée, de Windows Update ou d'un service Microsoft Update déjà inscrit. La disponibilité de la source est vérifiée avant la découverte. La découverte n'installe rien, n'accepte aucune licence, n'inscrit aucune source et ne modifie aucune stratégie. Les mises à jour sélectionnées peuvent ensuite être installées séparément, seulement après avoir coché la case de confirmation. Cet outil est distinct de la réparation de la maintenance.

### Windows Update

Inventorie de manière délimitée les états des packages de maintenance, des fonctionnalités facultatives et des capacités de Windows, vérifie les indicateurs de redémarrage, l'espace libre et les services de mise à jour, observe Optimisation de la distribution sans la démarrer, puis analyse le magasin de composants et les fichiers système protégés. Les états en attente de packages, de fonctionnalités ou de capacités appellent un examen, mais n'autorisent aucune action sur les composants. La réparation automatique peut démarrer les services arrêtés Windows Update, BITS, Services de chiffrement, Programme d'installation pour les modules Windows et Update Orchestrator configurés pour démarrer automatiquement. Un service à démarrage manuel arrêté est signalé, pas démarré. Le démarrage d'Update Orchestrator peut reprendre l'activité de mise à jour planifiée existante selon la stratégie actuelle. Les packages, les fonctionnalités facultatives, les capacités, le cache d'Optimisation de la distribution, les pairs, la bande passante, la stratégie, l'inscription de la source, les planifications et les paramètres de démarrage sont conservés. DISM ou SFC exigent une preuve en direct de corruption. Ce flux de travail ne sélectionne ni n'approuve aucune mise à jour.

### Service Windows Update

Vérifie l'état exact du Service Windows Update, la DLL de service inscrite, l'activité de mise à jour actuelle délimitée et l'historique exact des défaillances du service. Les observations de la DLL, l'activité de mise à jour et les défaillances historiques ne peuvent pas autoriser le démarrage du service ni une opération de mise à jour. La récupération facultative ne démarre qu'un service automatique arrêté, après de nouvelles vérifications des dépendances et de l'inscription de l'exécutable. Un service à démarrage manuel arrêté reste purement informatif. L'activité de mise à jour existante peut reprendre selon la stratégie actuelle. La stratégie de mise à jour, l'inscription de la source, les planifications, les paramètres de redémarrage, les packages, les tâches BITS, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne, n'approuve ni n'installe aucune mise à jour. La vérification ne couvre que l'état stable du service, pas l'accessibilité de la source, l'applicabilité, le téléchargement, l'installation, l'achèvement ni la disponibilité au redémarrage.

### Optimisation de la distribution

Vérifie l'état exact du service Optimisation de la distribution. Un service à démarrage manuel arrêté reste purement informatif. Seul un état exact En pause peut proposer une récupération. La récupération facultative répète la vérification de l'état, envoie une seule demande de reprise délimitée et exige deux observations cohérentes En cours d'exécution. Le type de démarrage, le contenu du cache, les pairs, la bande passante, la source des mises à jour et la stratégie gérée sont conservés. L'activité de transfert, l'état du cache, la progression des mises à jour et leur achèvement restent non vérifiés.

### Service BITS

Vérifie l'état exact du Service de transfert intelligent en arrière-plan, les tâches de transfert du compte actuel et l'historique exact et délimité des défaillances du service. Les défaillances historiques et les instantanés de transfert ne peuvent pas autoriser le démarrage du service. La récupération facultative ne démarre que le service exact lorsqu'il est arrêté et configuré pour démarrer automatiquement, après de nouvelles vérifications des dépendances. Un service à démarrage manuel arrêté reste purement informatif. Les applications existantes peuvent reprendre les transferts en arrière-plan en file d'attente. Les tâches, les priorités, les propriétaires, les destinations, les paramètres de proxy, la stratégie et le type de démarrage sont conservés. La vérification ne couvre que l'état stable du service, pas la progression des transferts, la propriété des applications ni l'achèvement.

### Transferts en arrière-plan (BITS)

Lit les tâches de transfert BITS dans la portée de compte sélectionnée et examine les identités exactes des tâches. Les noms des files n'établissent pas à quelle application elles appartiennent. Aucun transfert n'est repris, terminé, annulé, supprimé ou réattribué.

### Services de chiffrement

Vérifie l'état exact de Services de chiffrement, la DLL de service inscrite et l'historique exact et délimité des défaillances du service. Les défaillances historiques et les observations de la DLL ne peuvent pas autoriser le démarrage du service ni le remplacement de composants. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les opérations existantes de catalogue, de certificats et de mise à jour peuvent reprendre. Les certificats, les clés, les catalogues, les paramètres de confiance, la stratégie et le type de démarrage sont conservés. La vérification ne couvre que l'état stable du service, pas la validation des certificats, des signatures, des catalogues ni de la confiance.

### Programme d'installation pour les modules Windows

Vérifie l'état exact de Programme d'installation pour les modules Windows, les transitions délimitées des packages de maintenance et l'historique exact des défaillances du service. Les défaillances historiques et les états des packages ne peuvent pas autoriser le démarrage du service ni une opération sur les packages. La récupération facultative ne démarre que le service exact lorsqu'il est arrêté et configuré pour démarrer automatiquement, après de nouvelles vérifications des dépendances. Un service à démarrage manuel arrêté reste purement informatif. L'activité de maintenance existante peut reprendre. Les packages, le contenu du magasin de composants, les sources, la stratégie et le type de démarrage sont conservés. La vérification ne couvre que l'état stable du service, pas l'intégrité du magasin de composants, l'applicabilité des packages, l'achèvement de la maintenance ni la disponibilité au redémarrage.

### Service Windows Update Medic

Vérifie l'état exact du Service Windows Update Medic, la DLL de service inscrite, l'activité de mise à jour actuelle délimitée et l'historique exact des défaillances du service. Les observations de la DLL, l'activité de mise à jour et les défaillances historiques ne peuvent pas autoriser le démarrage du service ni une action sur les composants. Un service à démarrage manuel arrêté peut être normal. Lorsque cet outil exact est sélectionné, la récupération facultative ne peut démarrer que le service activé et arrêté, après de nouvelles vérifications des dépendances. La correction par Windows peut reprendre selon la stratégie actuelle et peut réparer des composants de mise à jour ou déclencher une activité de mise à jour. La stratégie de mise à jour, l'inscription de la source, les planifications, les packages, les tâches BITS, les paramètres de redémarrage, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucune mise à jour et ne modifie aucun paramètre de mise à jour. La vérification ne couvre que l'état stable du service, pas la correction, l'intégrité des composants, l'applicabilité, le téléchargement, l'installation ni la disponibilité au redémarrage.

### Service Update Orchestrator

Vérifie l'état exact du Service Update Orchestrator, la tâche intégrée exacte de Windows Update, l'activité actuelle délimitée et l'historique exact des défaillances du service. Les résultats de la tâche, l'activité de mise à jour et les défaillances historiques ne peuvent pas autoriser le démarrage du service ni une opération de mise à jour. La récupération facultative ne démarre qu'un service automatique arrêté, après de nouvelles vérifications des dépendances et de l'inscription de l'exécutable. Un service à démarrage manuel arrêté reste purement informatif. L'activité de mise à jour planifiée existante peut reprendre selon la stratégie actuelle et peut télécharger ou installer des mises à jour déjà approuvées. La stratégie de mise à jour, l'inscription de la source, les planifications, les paramètres de redémarrage, les packages, les tâches BITS, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne ni n'approuve aucune mise à jour. La vérification ne couvre que l'état stable du service, pas l'applicabilité, le téléchargement, l'installation, l'achèvement ni la disponibilité au redémarrage.

### Nettoyage du magasin de composants

Analyse si Windows recommande un nettoyage du magasin de composants. La maintenance facultative supprime immédiatement les composants remplacés, puis relance l'analyse. Aucun ResetBase ni suppression manuelle dans WinSxS. Aucune annulation automatique ; l'espace disque récupéré n'est pas mesuré.
