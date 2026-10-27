# Sécurité

Sécurité inspecte Microsoft Defender, le pare-feu, les certificats et les services de connexion. Elle expose aussi l'état du TPM et de la sécurité de la plateforme, dont dépendent BitLocker et Windows Hello.

Guides pas à pas : [Chiffrer un lecteur avec BitLocker](encrypt-with-bitlocker.html) et [Rendre Windows plus confidentiel](make-windows-private.html).

## Outils de cette section

Inspectez la protection, le pare-feu, les certificats et les services de connexion.

### Microsoft Defender

Inspecte l'état de la protection et des signatures de Defender. La réparation automatique n'actualise les signatures que lorsque Defender signale une protection active et des signatures obsolètes. Utilise les sources de signatures configurées. Aucune modification de la protection, des exclusions ou des stratégies ; aucune analyse antivirus.

### Centre de sécurité Windows

Examine de manière délimitée les inscriptions d'antivirus avec l'état brut du fournisseur, l'historique exact des défaillances du service et la disponibilité actuelle de Centre de sécurité Windows, sans interpréter l'état de la protection. Un inventaire vide n'est pas un verdict sur l'état de la protection, et le nombre d'inscriptions, l'état brut ou les défaillances historiques ne peuvent pas autoriser une récupération ni une modification de sécurité. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. La surveillance de l'état de sécurité et les notifications existantes peuvent reprendre. La configuration de l'antivirus, les inscriptions des fournisseurs, les paramètres de protection, les exclusions, les notifications, la stratégie et le type de démarrage sont conservés. Aucune analyse, mise à jour ni modification de la protection n'est demandée. La vérification ne couvre que l'état stable du service, pas la présence d'un antivirus, l'état de la protection, la fraîcheur des signatures, la remise des notifications, la propriété des inscriptions ni la stratégie effective.

### Services de pare-feu

Lit les paramètres d'activation des profils Domaine, Privé et Public dans ActiveStore, puis vérifie les services Moteur de filtrage de base et Pare-feu Windows Defender. La récupération facultative ne démarre que des services activés et arrêtés, après de nouvelles vérifications des dépendances. La stratégie de filtrage existante peut prendre effet et interrompre l'accès réseau ou à distance. Les types de démarrage, les autorisations des services, les règles du pare-feu, les profils et les paramètres IPsec sont conservés. Aucune réinitialisation, modification de règle ni arrêt de service. La vérification établit uniquement que le service est en cours d'exécution ; la protection effective et la connectivité restent non vérifiées.

### Expiration des certificats

Inspecte les dates des certificats et construit des chaînes de confiance locales à partir du seul cache pour les magasins personnels de l'ordinateur et de l'utilisateur actuel, puis renvoie une seule conclusion opérationnelle délimitée. La révocation, la présentation côté serveur, les liaisons, l'inscription, le contrôle par stratégie, la confiance distante et l'utilisabilité de la clé privée restent non vérifiés. Aucun certificat, paramètre de confiance, inscription, liaison, clé privée, application, service ou stratégie n'est modifié.

### Gestionnaire d'identification

Vérifie la disponibilité du service Gestionnaire d'identification. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les applications existantes peuvent reprendre leurs demandes d'identifiants enregistrés. Le contenu du coffre, les mots de passe, les clés d'accès, les certificats, les jetons, les inscriptions d'identifiants, les autorisations, la stratégie, les autorisations du service et le type de démarrage sont conservés. Aucun contenu ni aucune opération d'identifiants n'est demandé. La vérification ne couvre que l'état stable du service, pas la récupération des identifiants, l'authentification ni l'accès des applications.

### Gestionnaire de comptes web

Examine l'historique du répartiteur de comptes avec une charge utile expurgée et la disponibilité exacte de Gestionnaire de comptes web. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les applications existantes peuvent reprendre leurs demandes de comptes et de jetons selon la stratégie actuelle. Les événements historiques ne peuvent pas autoriser une récupération ni établir l'état actuel des comptes. Les comptes, les identifiants, les jetons, les cookies, les inscriptions d'applications, les autorisations, l'état de connexion, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucune opération de compte, d'identifiant, de jeton, de cookie, d'autorisation ou de connexion. La vérification ne couvre que l'état stable du service, pas l'authentification, l'émission de jetons ni l'accès des applications.

### Assistant Connexion avec un compte Microsoft

Examine l'historique du répartiteur de comptes avec une charge utile expurgée et la disponibilité exacte de Assistant Connexion avec un compte Microsoft. La récupération facultative ne démarre que le service exact activé et arrêté, après de nouvelles vérifications des dépendances. Les demandes existantes de connexion avec un compte Microsoft peuvent reprendre selon la stratégie actuelle. Les événements historiques ne peuvent pas autoriser une récupération ni établir l'état actuel des comptes. Les comptes, les identifiants, les jetons, l'inscription de l'appareil, les données d'applications, les autorisations, l'état de connexion, la stratégie, les autorisations du service et le type de démarrage sont conservés. KillerBench ne sélectionne aucune opération de compte, d'identifiant, de jeton, d'inscription, d'autorisation ou de connexion. La vérification ne couvre que l'état stable du service, pas l'authentification, la connectivité du compte ni l'accès des applications.
