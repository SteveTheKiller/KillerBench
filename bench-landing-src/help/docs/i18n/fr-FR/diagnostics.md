# Diagnostics

Diagnostics est le point de départ de chaque intervention. Chaque outil lit l'ordinateur et rédige un rapport. Rien ici ne modifie Windows. L'outil par défaut, l'analyse complète du système, exécute un large ensemble de vérifications en lecture seule et classe ce qui demande de l'attention.

Guides pas à pas : [Enquêter sur les plantages et les écrans bleus](investigate-crashes.html) et [Réparer les fichiers système](repair-system-files.html).

## Outils de cette section

Vérifiez d'abord l'ordinateur, puis lisez dans le rapport ce qui demande de l'attention et la prochaine étape sûre.

### Analyse complète du système

Inspecte 49 domaines de diagnostic et évalue les recettes de réparation installées au regard des preuves actuelles. Les vérifications peuvent prendre plusieurs minutes. L'analyse complète du système est uniquement diagnostique. Utilisez un flux de travail de réparation ciblé pour sélectionner et confirmer une action prise en charge après avoir examiné des prérequis récents. Les instantanés historiques, de sauvegarde et de transfert restent en lecture seule. Le contenu des vidages n'est pas ouvert. L'inspection du bit d'intégrité ne planifie ni réparation ni redémarrage. Cette analyse n'installe pas de mises à jour, ne modifie pas les pilotes, ne supprime pas de fichiers et n'effectue pas de test complet du matériel.

### Signes vitaux

Capture la mémoire disponible, l'état des disques indiqué par le fournisseur et l'état de la protection BitLocker, puis met en corrélation les signes vitaux actuels avec les problèmes de périphériques, l'historique de fiabilité, les enregistrements d'erreurs matérielles, les preuves de plantage, les arrêts inattendus et les résultats de l'Outil Diagnostics de mémoire Windows. Les indicateurs de redémarrage, l'espace libre du disque système, l'élévation et la configuration du service de mise à jour restent visibles. Les mots de passe de récupération ne sont pas collectés. Ce flux de travail n'exécute aucune réparation. Le résultat combiné n'est pas un test complet du matériel, du logiciel ni de la stabilité.

### Historique de fiabilité

Lit les enregistrements du Moniteur de fiabilité de Windows des 14 derniers jours avec les erreurs récentes du journal Application et les événements d'arrêt inattendu, puis renvoie une seule conclusion historique délimitée. Les enregistrements historiques n'établissent ni l'état actuel ni la cause profonde. Aucune réparation n'est sélectionnée et aucune application, aucun service ni aucun paramètre n'est modifié.

### Performances du démarrage

Lit les métadonnées des événements de performances de démarrage et d'arrêt de Windows des 14 derniers jours, en ne conservant que l'identité du fournisseur et des champs numériques de durée délimités, puis les met en corrélation avec les indicateurs de redémarrage en attente pris en charge. Les chemins, les noms de processus, les comptes et le reste de la charge utile des événements sont supprimés avant l'enregistrement des preuves. Les enregistrements historiques de dégradation n'identifient pas un goulot d'étranglement actuel et n'autorisent aucune modification du démarrage, des services, des applications, des pilotes, des tâches, de l'alimentation, du redémarrage ou des stratégies.

### Historique des erreurs matérielles

Lit l'historique de Windows Hardware Error Architecture et du diagnostic de la mémoire des 14 derniers jours, les problèmes actuels des périphériques Plug-and-Play, l'état des disques physiques et les compteurs de fiabilité du fournisseur, puis renvoie une seule conclusion matérielle délimitée. Les enregistrements doivent être mis en corrélation avec leur charge utile, les diagnostics du micrologiciel et les tests matériels du fabricant. Aucun composant défaillant ni aucune cause profonde n'est déduit, aucune réparation n'est sélectionnée, et aucun matériel, micrologiciel, pilote, disque, pool, volume, fichier, service, réglage d'alimentation, redémarrage ou paramètre n'est modifié.

### Historique d'installation des pilotes

Lit la fin conservée de l'historique d'installation des périphériques Windows SetupAPI, met en corrélation les sections en échec avec les identités Plug-and-Play exactes actuelles et renvoie une seule conclusion délimitée. Les sections historiques du journal ne prouvent pas qu'un périphérique est toujours présent ni qu'un échec est actuel. Aucun pilote n'est installé, supprimé, restauré ou mis à jour.

### Points de restauration du système

Liste jusqu'à 256 points de restauration du système disponibles, examine l'éligibilité explicite à la création et renvoie une seule conclusion de disponibilité délimitée. L'inventaire ne prouve pas que la protection est activée pour chaque volume ni qu'une restauration réussira. Aucun point de restauration n'est créé, supprimé ou appliqué pendant le diagnostic.

### Environnement de récupération Windows

Lit la configuration actuelle de l'Environnement de récupération Windows avec REAgentC, met en corrélation un emplacement local activé avec le disque et la partition exacts actuels, et renvoie une seule conclusion de disponibilité délimitée. La réparation automatique facultative peut activer une configuration existante désactivée, puis vérifier l'état indiqué. L'image de récupération n'est ni téléchargée, ni déplacée, ni remplacée, aucun démarrage en mode récupération n'est lancé et Windows n'est pas redémarré. L'état activé et la présence de la partition ne prouvent pas que l'image de récupération peut démarrer ou réparer ce PC.

### Activation de Windows

Lit l'état actuel de la licence Windows sans collecter de clés de produit complètes ou partielles, conserve les métadonnées expurgées des événements d'avertissement, d'erreur et critiques de la Protection logicielle des sept derniers jours, puis renvoie une seule conclusion opérationnelle délimitée. La charge utile des événements est supprimée avant l'enregistrement des preuves. Les enregistrements historiques restent un contexte lorsque la licence actuelle est signalée comme Sous licence. Aucune action d'activation, de clé de produit, de jeton, de licence, de compte, de service ou de stratégie n'est sélectionnée. L'état du fournisseur et l'historique des événements ne prouvent ni le droit d'abonnement, ni l'accessibilité du serveur d'activation, ni l'intégrité du magasin de jetons, ni la validité future, ni la cause profonde.

### Services Windows

Inventorie les noms de services installés, les noms complets, les états, les modes de démarrage et les types d'actions de récupération limités par la confidentialité, sans collecter de comptes, de chemins d'exécutables, de commandes de récupération ni de messages de redémarrage. Examine la cohérence des modes de démarrage, les identités et les cycles des dépendances directes, répète les états en attente exacts, inspecte les services pris en charge et les dépendances des services arrêtés, puis met en corrélation les défaillances récentes du Gestionnaire de contrôle des services uniquement lorsqu'un nom de service exact ou un nom complet unique est présent. L'utilisation des groupes d'ordre de chargement est comptée sans développer leurs membres, et les identités ambiguës restent non résolues. Des services à démarrage manuel arrêtés peuvent être normaux. Aucun service n'est démarré et les paramètres de démarrage, de dépendances et de récupération sont conservés. Utilisez le flux de travail Réparation correspondant lorsqu'une fonctionnalité précise a besoin d'une récupération.

### Erreurs d'application

Lit les événements récents d'erreur et critiques du journal Application avec un historique délimité du Moniteur de fiabilité, puis renvoie une seule conclusion sur l'historique de stabilité. Les enregistrements historiques ne prouvent ni une panne actuelle ni une cause profonde et orientent seulement vers un diagnostic plus ciblé. Aucune application n'est arrêtée, réparée, supprimée ou reconfigurée.

### Périphériques d'affichage

Inspecte les cartes graphiques et les moniteurs à la recherche d'erreurs de périphérique signalées, des ID matériels et des versions de pilotes installées. Aucune installation de pilote, réinitialisation de périphérique ni modification de résolution. La qualité d'image et les connexions physiques ne sont pas testées.

### Périphériques d'entrée

Inspecte les erreurs des claviers, des souris et des périphériques HID ainsi que l'inventaire des pilotes. Aucune frappe n'est capturée, aucun périphérique n'est réinitialisé et aucun pilote n'est modifié. Le fonctionnement physique de la saisie n'est pas testé.

### Problèmes de périphériques

Recherche les codes d'erreur de configuration Plug-and-Play non nuls dans toutes les classes de périphériques, puis met en corrélation les preuves des périphériques présents avec l'historique délimité de SetupAPI sur l'installation des pilotes, la configuration des périphériques et le Plug-and-Play. Les enregistrements historiques ne suffisent pas à établir une panne actuelle. Examinez l'identité exacte du périphérique et les preuves de pilotes approuvés sans modifier le matériel, les pilotes ni la configuration. Les périphériques absents et le fonctionnement physique ne sont pas vérifiés.
