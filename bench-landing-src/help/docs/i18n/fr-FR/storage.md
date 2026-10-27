# Stockage

Inspecte les disques, les volumes, les Espaces de stockage et les services de maintenance des lecteurs. Le diagnostic ne sélectionne aucune opération de disque ; toute action prise en charge qui modifie l'ordinateur exige sa propre cible exacte, ses prérequis et sa confirmation.

Guides pas à pas : [Vérifier et réparer un lecteur](check-a-drive.html) et [Libérer de l'espace disque](free-disk-space.html).

## Outils de cette section

### Stockage

Cartographie de manière délimitée les identités des disques, des partitions et des volumes dotés d'une lettre de lecteur, évalue la pression à 10 % d'espace libre sur chaque volume doté d'une lettre, inspecte l'allocation du stockage des clichés instantanés VSS et les quotas par défaut de chaque volume, résume la progression actuelle des tâches du module Storage, vérifie le volume NTFS sélectionné et son état d'autoréparation en ligne, et inspecte les périphériques de disque et de contrôleur de ce PC. Les seuils de capacité, les paramètres de quota et les états des tâches sont des preuves à examiner et ne sélectionnent ni nettoyage, ni redimensionnement, ni modification de quota, ni contrôle de tâche. La correspondance des lettres de lecteur est ponctuelle et ne couvre pas tous les chemins de montage. Les constats sur les périphériques ne sont pas associés au volume sélectionné. Le volume Windows est sélectionné par défaut. Aucun fichier, cliché, tâche ou pilote n'est modifié, aucun paramètre n'est changé, aucun volume n'est démonté et aucun redémarrage n'est planifié.

### Espaces de stockage

Inventorie les pools d'Espaces de stockage non primordiaux et les disques virtuels, y compris leur intégrité, leur état opérationnel, leur capacité, leur allocation et leur résilience, puis renvoie une seule conclusion opérationnelle délimitée. Windows peut démarrer à la demande son fournisseur de gestion des Espaces de stockage pour répondre à cette requête en lecture seule. Les pools, les disques virtuels, les disques physiques, les volumes, la résilience, l'allocation, les paramètres de maintenance, la stratégie et le type de démarrage sont conservés. Aucune opération de pool, de disque, de volume, d'allocation, d'attachement, de réparation, d'optimisation ou de paramétrage n'est demandée. Les résultats du fournisseur ne vérifient ni l'accessibilité du pool, ni la redondance, ni l'intégrité des données, ni la possibilité de restaurer les sauvegardes, ni les E/S des applications.

### Service de stockage

Examine de manière délimitée les tâches actuelles du module Storage, l'historique exact des défaillances du service et la disponibilité actuelle du Service de stockage. Les preuves incomplètes sont prioritaires. L'état des tâches et les défaillances historiques ne peuvent pas autoriser la récupération du service ni des modifications de stockage. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les notifications de stockage existantes et l'activité de stockage gérée par stratégie peuvent reprendre. Les volumes, les partitions, les systèmes de fichiers, les points de montage, les périphériques, les pilotes, les paramètres de l'Assistant Stockage, la stratégie de maintenance, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucune opération de tâche, de volume, de partition, de point de montage, de fichier, de périphérique, de pilote ou de paramètre de stockage. La vérification ne couvre que l'état stable du service, pas le résultat des tâches ni les opérations de périphérique, de volume ou de stratégie de stockage.

### Service Optimiser les lecteurs

Vérifie la disponibilité du service Optimiser les lecteurs. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. L'activité d'optimisation déjà planifiée par Windows peut reprendre selon la stratégie de maintenance actuelle. Les volumes, les systèmes de fichiers, les données, les planifications d'optimisation, la stratégie de maintenance, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucun volume et ne lance aucune analyse, aucune commande TRIM, aucune défragmentation, aucune optimisation ni aucune planification sur un lecteur. La vérification ne couvre que l'état stable du service, pas l'analyse, l'optimisation ni l'intégrité du périphérique.
