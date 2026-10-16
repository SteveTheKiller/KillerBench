# Périphériques

Périphériques couvre les imprimantes, l'audio, les caméras, Bluetooth, la saisie et les services Windows dont dépend le matériel connecté.

Guides pas à pas : [Résoudre les problèmes d'impression](fix-printing.html) et [Enquêter sur les plantages et les écrans bleus](investigate-crashes.html).

## Outils de cette section

Impression, audio, caméras, Bluetooth, saisie et services qui font fonctionner le matériel connecté.

### Impression

Vérifie le Spouleur d'impression, les pauses des files locales, les travaux d'impression visibles, les paramètres hors connexion et l'état signalé par les pilotes, puis inventorie les inscriptions des files, des pilotes, des ports, des points de terminaison TCP/IP standard, des processeurs d'impression et des moniteurs d'impression. Met en corrélation les problèmes récents de PrintService uniquement lorsqu'une identité de file actuelle exacte est présente. Les points de terminaison TCP/IP standard attribués reçoivent une seule tentative de connexion délimitée, sans données d'application, SNMP ni travail d'impression. La réparation automatique peut démarrer le spouleur activé, reprendre uniquement les files exactes en pause avec des preuves récentes de travaux en attente, et désactiver le mode hors connexion. Les pauses volontaires sans travail restent en examen seul. Les connexions distantes, les chaînes de communauté SNMP, les inscriptions d'extensions et le contenu des travaux sont conservés. Un code d'état ou une connexion réussie n'établit ni l'identité de l'imprimante, ni les consommables, ni la mécanique, ni l'acceptation par la file, ni la réussite du rendu, ni l'impression physique.

### Audio

Vérifie les identités et les formats partagés des entrées et sorties multimédias par défaut, l'état des périphériques et des pilotes audio, Générateur de points de terminaison audio Windows et Audio Windows. La réparation automatique démarre les services activés et arrêtés et vérifie qu'ils sont en cours d'exécution. Les paramètres de démarrage, les pilotes, les périphériques sélectionnés, le mode exclusif et les niveaux de volume sont conservés. La lecture et l'enregistrement du son ainsi que le routage des applications restent non testés.

### Prise en charge des caméras

Inspecte les erreurs des périphériques de caméra et d'imagerie, les ID matériels, les preuves de pilotes installés et la disponibilité de Serveur de trames de la caméra Windows. La récupération facultative ne démarre qu'un service activé et arrêté, après la vérification des dépendances. Une application qui demande déjà l'accès à la caméra peut reprendre selon les autorisations existantes. Les paramètres de confidentialité de la caméra, les autorisations, la configuration des périphériques, la stratégie, les pilotes et le type de démarrage sont conservés. Aucune réinitialisation, modification d'autorisation ni installation de pilote. La vérification ne couvre que la configuration signalée du périphérique et l'état en cours d'exécution du service, pas la capture d'images, l'accès lié à la confidentialité ni le fonctionnement physique.

### Prise en charge de Bluetooth

Inspecte les erreurs des périphériques Bluetooth, les ID matériels, les preuves de pilotes installés et la disponibilité de Service de prise en charge Bluetooth. La récupération facultative ne démarre qu'un service activé et arrêté, après la vérification des dépendances ; les appareils couplés peuvent se reconnecter. Les couplages, les paramètres radio, la configuration des périphériques, la stratégie et le type de démarrage sont conservés. Aucune réinitialisation, suppression d'appareil ni installation de pilote. La vérification ne couvre que la configuration signalée du périphérique et l'état en cours d'exécution du service, pas la découverte, le couplage, l'audio ni le transfert de données.

### Prise en charge des cartes à puce

Inspecte les erreurs des lecteurs de cartes à puce, les ID matériels, les preuves de pilotes installés et la disponibilité du service Carte à puce. La récupération facultative ne démarre qu'un service activé et arrêté, après la vérification des dépendances. Les applications peuvent recommencer à détecter les cartes insérées selon la stratégie existante. Les certificats, les codes PIN, les identifiants, la configuration des lecteurs et des cartes, la stratégie, les pilotes et le type de démarrage sont conservés. Aucune initialisation, inscription, opération sur le code PIN, réinitialisation de lecteur ni installation de pilote. La vérification ne couvre que la configuration signalée du lecteur et l'état en cours d'exécution du service, pas la détection des cartes, l'authentification ni l'accès des applications.

### Prise en charge biométrique

Inspecte les erreurs des périphériques biométriques, les ID matériels, les preuves de pilotes installés et la disponibilité de Service de biométrie Windows. La récupération facultative ne démarre qu'un service activé et arrêté, après la vérification des dépendances. Les demandes existantes de Windows Hello et des applications peuvent reprendre selon la stratégie actuelle. Les modèles biométriques, les données Windows Hello, les identifiants, la configuration des périphériques, les autorisations, la stratégie, les pilotes et le type de démarrage sont conservés. Aucune inscription, suppression de modèle, réinitialisation de capteur, modification d'autorisation ni installation de pilote. La vérification ne couvre que la configuration signalée du capteur et l'état en cours d'exécution du service, pas la capture, la correspondance, la connexion ni l'accès des applications.

### Service du périphérique d'interface utilisateur

Vérifie la disponibilité de Service du périphérique d'interface utilisateur. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les touches d'accès rapide, les commandes multimédias et les autres fonctionnalités HID existantes peuvent reprendre. Les périphériques, les pilotes, les attributions de touches, les paramètres d'accessibilité, les paramètres de saisie, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucune opération sur les périphériques, les pilotes, les attributions ou les paramètres de saisie. La vérification ne couvre que l'état stable du service, pas le comportement des touches, des commandes ni des applications.

### Service Énumérateur d'appareils mobiles

Vérifie la disponibilité de Service Énumérateur d'appareils mobiles. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les applications existantes peuvent recommencer à détecter les appareils mobiles connectés et à y accéder. Les appareils, les pilotes, les fichiers, les bibliothèques, les paramètres de synchronisation, les autorisations, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucune opération sur les appareils, les fichiers, les bibliothèques, la synchronisation ou les transferts. La vérification ne couvre que l'état stable du service, pas l'énumération, l'accès, la synchronisation ni le transfert.

### Acquisition d'image Windows (WIA)

Vérifie la disponibilité du service Acquisition d'image Windows. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les demandes existantes de numérisation et d'acquisition d'images fixes peuvent reprendre selon les autorisations actuelles des applications. Les périphériques, les pilotes, les données d'image, les autorisations des applications, les paramètres de confidentialité, les paramètres de transfert, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucune opération de périphérique, de pilote, d'image, d'autorisation, de transfert ou d'acquisition. La vérification ne couvre que l'état stable du service, pas le fonctionnement du scanneur, de la caméra, du transfert ni des applications.

### Détection matériel noyau

Inspecte des preuves Plug-and-Play délimitées sur les contrôleurs de stockage, les disques et les appareils mobiles ainsi que les références des pilotes installés, puis vérifie la disponibilité du service Détection matériel noyau. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les notifications existantes d'exécution automatique et d'arrivée de matériel peuvent reprendre selon la stratégie actuelle. Les observations de périphériques et l'historique conservé ne peuvent pas autoriser une récupération. Les périphériques, les pilotes, les volumes, les points de montage, les paramètres d'exécution automatique, les gestionnaires, la stratégie, les autorisations du service et le type de démarrage sont conservés. Aucune opération de périphérique ou de paramètre n'est demandée. La vérification ne couvre que l'état stable du service, pas la remise des notifications, la disponibilité des supports, l'accès aux volumes ni le fonctionnement physique des périphériques.

### Service d'installation de périphérique

Vérifie la disponibilité de Service d'installation de périphérique. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Le travail d'installation de périphériques demandé par Windows peut reprendre selon la stratégie actuelle. Les périphériques, les pilotes, les packages, la configuration, les liaisons, les paramètres de classe, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne et n'installe rien. La vérification ne couvre que l'état stable du service, pas l'installation ni le fonctionnement des périphériques.

### Service d'association de périphérique

Vérifie l'historique récent de configuration des périphériques et du Plug-and-Play, puis la disponibilité de Service d'association de périphérique. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Le travail d'association de périphériques demandé par Windows peut reprendre. Les appareils couplés et associés, les pilotes, les identifiants, les métadonnées, les autorisations, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucune opération de périphérique, de couplage, de suppression, de réinitialisation ou de pilote. La vérification ne couvre que l'état stable du service, pas la découverte, le couplage, l'association ni le fonctionnement des périphériques.

### Service d'amélioration de l'affichage

Inspecte les preuves concernant les cartes graphiques et les moniteurs, puis vérifie la disponibilité de Service d'amélioration de l'affichage. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les demandes existantes de luminosité, de couleur et d'amélioration peuvent reprendre. La configuration de l'affichage, la luminosité, les profils de couleur, les paramètres HDR, les périphériques, les pilotes, les paramètres d'alimentation, la stratégie, les autorisations du service et le type de démarrage sont conservés. Aucun paramètre d'affichage n'est modifié. La vérification ne couvre que l'état stable du service, pas le comportement de l'amélioration, la qualité d'image ni le fonctionnement physique de l'écran.

### Service de gestion de la saisie de texte

Inspecte les preuves concernant les claviers, les souris et les périphériques HID, puis vérifie la disponibilité de Service de gestion de la saisie de texte. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les demandes existantes de clavier tactile, d'écriture manuscrite, d'emoji et de saisie de texte avancée peuvent reprendre. Les dictionnaires, les données d'écriture manuscrite, les paramètres de langue et de clavier, les paramètres d'accessibilité et de confidentialité, les périphériques, les pilotes, la stratégie, les autorisations du service et le type de démarrage sont conservés. Aucun texte saisi, aucune frappe ni aucun contenu manuscrit n'est collecté. La vérification ne couvre que l'état stable du service, pas la saisie de texte, la reconnaissance ni le fonctionnement des périphériques.

### Service de cache de police Windows

Vérifie la disponibilité de Service de cache de police Windows. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les applications existantes peuvent recommencer à utiliser le cache de polices actuel. Les polices installées, les fichiers de polices, les données des applications, les fichiers de cache, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench n'installe ni ne supprime aucune police et ne supprime ni ne reconstruit le cache. La vérification ne couvre que l'état stable du service, pas le rendu du texte ni le comportement des applications.

### Hôte du fournisseur de découverte de fonctions

Vérifie la disponibilité de Hôte du fournisseur de découverte de fonctions. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les applications existantes peuvent recommencer à découvrir des périphériques réseau au moyen des fournisseurs installés. Les profils réseau, les partages, les périphériques, les fournisseurs, les règles du pare-feu, les paramètres de découverte, les identifiants, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucune opération de périphérique, de partage, de fournisseur, de règle de pare-feu ou de paramètre de découverte. La vérification ne couvre que l'état stable du service, pas l'état des fournisseurs, la découverte des périphériques ni la connectivité.

### Publication des ressources de découverte de fonctions

Vérifie la disponibilité de Publication des ressources de découverte de fonctions. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Ce PC peut devenir détectable selon les paramètres existants de découverte du réseau et du pare-feu. Les profils réseau, les partages, les ressources publiées, les règles du pare-feu, les paramètres de découverte, les identifiants, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucune opération de partage, de ressource, de règle de pare-feu ou de paramètre de découverte. La vérification ne couvre que l'état stable du service, pas la publication, la visibilité, l'accès ni la connectivité.
