# Ligne de commande

`killerbench-cli` exécute le même Engine que l'application de bureau depuis PowerShell ou une console distante. Toute commande susceptible de modifier l'ordinateur affiche un aperçu par défaut et nécessite une option explicite, comme `--execute`, avant d'agir. Les commandes impriment les preuves JSON sur la sortie standard et la progression sur la sortie d'erreur standard.

Guides pas à pas : [Exécuter KillerBench depuis des scripts](use-the-cli.html).

Le code de sortie 0 signifie que l'aperçu ou le résultat vérifié a réussi ; 1, une erreur de démarrage, de saisie ou de journal ; 2, que les arguments n'ont pas été reconnus ; 3, bloqué, échoué ou non vérifié ; 4, qu'un flux de travail a besoin d'un redémarrage avant de pouvoir vérifier ; et 130, que vous l'avez arrêtée.

Exécutez `killerbench-cli` sans argument pour afficher cette liste sur votre propre ordinateur.

## Mises à jour et maintenance

- `bits-job-review JOB_GUID [--all-users] [--observe]`: Examen exact d'un transfert ou observation sur deux instantanés ; aucune nouvelle tentative.
- `bits-jobs [--all-users]`: États, propriétaires et nombres d'octets des tâches BITS ; aucune modification des transferts.
- `bits-retry JOB_GUID [--execute]`: Aperçu par défaut ; nouvelle tentative explicite d'une tâche exacte, sans affirmer que l'application a terminé.
- `bits-retry-recover JOB_GUID`: Deux instantanés en lecture seule ; archive les résultats stables sans nouvelle tentative.
- `component-repair-source`: Stratégie gérée de source de réparation DISM et conflits ; aucune modification.
- `delivery-optimization`: État du service Optimisation de la distribution en lecture seule ; preuves JSON.
- `delivery-optimization-activity`: Nombres de tâches actuels et totaux d'octets limités par la confidentialité ; aucune identité de transfert ni modification.
- `delivery-optimization-policy`: Paramètres classiques et Policy CSP en lecture seule ; valeurs sensibles et identités des fournisseurs exclues.
- `feature-service-events`: Lit l'historique exact des défaillances des services de temps, de recherche, WMI, du journal des événements et audio.
- `feature-update-policy`: Stratégie de version cible, de report, de pause et de protection en lecture seule ; preuves JSON.
- `pending-file-operations`: Résumé des opérations de fichiers en attente limité par la confidentialité, en lecture seule ; preuves JSON.
- `restart-check`: Sources de redémarrage en attente en lecture seule ; preuves JSON.
- `servicing-logs`: Analyse délimitée du journal CBS le plus récent, des archives CBS en texte brut et compressées, et des journaux DISM et Windows Setup ; aucune réparation.
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: Instantanés de l'activité de maintenance en lecture seule.
- `servicing-service-events`: Historiques exacts des défaillances de la chaîne de maintenance de Windows Update, en lecture seule.
- `servicing-service-states`: État actuel des six services de la chaîne de maintenance de Windows Update, en lecture seule.
- `setup-code DECIMAL_OR_0xHEX`: Référence documentée des codes de résultat de Windows Setup ; aucune inspection de l'ordinateur ni réparation.
- `update-applicability`: Preuves d'édition, de version et d'architecture de Windows, en lecture seule.
- `update-cache`: Totaux délimités du cache SoftwareDistribution en lecture seule ; aucun nom de fichier.
- `update-code DECIMAL_OR_0xHEX`: Référence documentée des codes de résultat de Windows Update ; aucune inspection de l'ordinateur ni réparation.
- `update-context`: Preuves de l'élévation actuelle et de la session de bureau en lecture seule ; aucun identifiant de compte.
- `update-engine-files`: Preuves de présence, de version et de signature du moteur natif de Windows Update, en lecture seule.
- `update-events`: Historique du client Windows Update des sept derniers jours en lecture seule ; JSON avec les preuves brutes.
- `update-followup DECIMAL_OR_0xHEX`: Nouveaux diagnostics en lecture seule choisis d'après un code de résultat documenté de Windows Update.
- `update-history`: Les 100 résultats d'opérations Windows Update les plus récents, en lecture seule ; preuves JSON.
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: Répétez `--select` ; aperçu par défaut.
- `update-policy`: Stratégie gérée de Windows Update en lecture seule ; preuves JSON.
- `update-policy-surfaces`: Preuves de la surface de configuration classique et Policy CSP en lecture seule ; valeurs et identités des fournisseurs exclues.
- `update-recover`: Vérifie une mise à jour en attente ; ne l'installe ni ne la relance jamais.
- `update-restart-policy`: Échéances de conformité, délais de grâce, suppression des redémarrages et comportement des notifications, en lecture seule.
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: Découverte et correspondances en JSON ; aucune installation.
- `update-service-events`: Historique exact des défaillances du service Windows Update, en lecture seule.
- `update-service-files`: Preuves exactes d'inscription et de signature de la DLL du service Windows Update, en lecture seule.
- `update-services`: Inscriptions des services Windows Update en lecture seule ; preuves JSON.
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: Conditionné à une nouvelle recherche ; aperçu par défaut ; conserve l'ancien magasin.
- `update-store-reset-recover`: Rapproche une réinitialisation de magasin préparée sans la répéter.
- `windows-capabilities`: États des capacités de Windows en ligne, en lecture seule ; résultat JSON.
- `windows-update-task`: État exact et historique d'exécution de la tâche Scheduled Start, en lecture seule ; aucune exécution de tâche ni opération de mise à jour.

## BitLocker et TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: Aperçu par défaut ; active le déverrouillage automatique pour un volume de données chiffré.
- `bitlocker-auto-unlock-recover`: Vérifie une demande de déverrouillage automatique interrompue sans la répéter.
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: Aperçu par défaut ; nouveau fichier de sauvegarde de récupération uniquement.
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: Vérifie une sauvegarde interrompue sans l'écrire à nouveau.
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: Aperçu par défaut ; lance le déchiffrement, par exemple avant de chiffrer à nouveau avec une méthode imposée.
- `bitlocker-decrypt-recover`: Vérifie une demande de déchiffrement interrompue sans la répéter.
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: Aperçu par défaut ; nécessite des protecteurs TPM et de récupération existants.
- `bitlocker-encrypt-recover`: Vérifie l'état de conversion en attente sans répéter la demande.
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: Aperçu par défaut ; sauvegarde un mot de passe de récupération existant dans Active Directory ou Microsoft Entra ID.
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: Aperçu des options uniquement.
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: Aperçu par défaut ; ajoute un protecteur TPM ou de mot de passe de récupération ; le mot de passe n'est jamais affiché.
- `bitlocker-protector-recover`: Vérifie une demande de protecteur interrompue sans la répéter.
- `bitlocker-protectors DRIVE:`: Identités et types des protecteurs uniquement ; aucun mot de passe de récupération.
- `bitlocker-resume DRIVE: [--execute]`: Aperçu par défaut ; uniquement un volume exact entièrement chiffré et suspendu.
- `bitlocker-resume-recover`: Vérifie l'état de protection en attente sans répéter la demande.

## Sécurité et confidentialité

- `certificate-chains`: Confiance de chaîne locale à partir du seul cache pour les certificats personnels, en lecture seule ; aucune révocation ni téléchargement.
- `defender-overview [--report]`: Vue d'ensemble de la protection, du calendrier des analyses et des menaces agrégées, en lecture seule ; aucune action.
- `defender-protection [--report]`: Préférences de protection et surfaces de stratégie reconnues, en lecture seule ; détails sensibles exclus ; aucune modification.
- `defender-scan Quick|Full [--execute] [--report]`: Aperçu par défaut ; une seule analyse Microsoft Defender explicite et protégée.
- `defender-scan-history [--report]`: Calendrier des analyses rapide et complète et état de retard, en lecture seule ; détails sensibles exclus ; aucune analyse.
- `defender-scan-recover [--report]`: Rapprochement en lecture seule sans lancer une autre analyse.
- `defender-threats [--report]`: Historique agrégé des menaces, indicateurs actifs actuels et ID de gravité ; détails sensibles exclus ; aucune action.
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: Aperçu par défaut ; profils locaux non gérés uniquement.
- `firewall-enable-recover`: Inspecte une modification en attente sans la répéter.
- `firewall-events`: Lit l'historique opérationnel délimité des avertissements, erreurs et événements critiques du Pare-feu Windows.
- `firewall-logging`: Lit la configuration de journalisation du trafic des profils Domaine, Privé et Public sans ouvrir les journaux.
- `firewall-rules`: Lit des nombres de règles effectives et de sources de stratégie limités par la confidentialité.
- `firewall-service-events`: Lit l'historique exact des défaillances de BFE et du Pare-feu Windows Defender dans le Gestionnaire de contrôle des services.
- `privacy-clipboard-history on|off [--execute]`: Aperçu par défaut ; valeur exacte de l'utilisateur actuel, stratégie préservée.
- `privacy-clipboard-history-recover`: Revérifie une modification interrompue sans la répéter.
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: Restauration protégée de la valeur précédente.
- `privacy-plan --select Camera --select ClipboardSync`: Examine uniquement les sélections ; aucun paramètre n'est modifié.
- `privacy-setting KEY on|off [--execute]`: Aperçu par défaut ; modification de l'utilisateur actuel qui préserve la stratégie ; les clés non valides affichent les choix pris en charge.
- `privacy-setting-recover KEY`: Revérifie une modification interrompue sans la répéter.
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: Restauration protégée de la valeur exacte.
- `security-check`: TPM, Démarrage sécurisé, protections de virtualisation et stratégie UAC en lecture seule ; preuves JSON.

## Services

- `service-binaries`: Présence des binaires de services locaux et état Authenticode, en lecture seule ; chemins exclus et chemins non locaux non contactés.
- `service-check NAME [NAME ...] | service-check --all`: États des services pris en charge et preuves de dépendances, en lecture seule.
- `service-dependencies`: Examen de l'intégrité des dépendances directes en lecture seule ; aucune modification des services.
- `service-dlls`: Présence des DLL de services inscrites et état Authenticode, en lecture seule ; chemins exclus et chemins non locaux non contactés.
- `service-events`: Historique des défaillances des services en lecture seule ; JSON avec les preuves brutes.
- `time-events`: Historique récent des événements du Temps Windows en lecture seule, avec preuves JSON.
- `service-logons`: Examen de la classe de compte des services et de l'interaction avec le bureau, en lecture seule ; identités de comptes exclues.
- `service-path-quoting`: Examen des guillemets dans les chemins d'exécutables de services, en lecture seule ; chemins complets et arguments exclus.
- `service-recovery-policy`: Résumé des actions de récupération des services installés, en lecture seule ; commandes et messages de redémarrage exclus.
- `service-startup-config`: Examen de la cohérence des modes de démarrage en lecture seule ; aucune modification des services.
- `service-transitions`: Observation répétée des états en attente des services, en lecture seule ; aucune action sur les services.
- `service-triggers`: Résumé des déclencheurs des services installés, en lecture seule ; charges utiles des déclencheurs exclues ; aucune modification.

## Stockage, fichiers et sauvegarde

- `backup-events`: Historique VSS et volsnap ; preuves JSON ; aucune modification.
- `backup-service-events`: Lit l'historique exact des défaillances de VSS, du fournisseur de clichés logiciels, de Sauvegarde Windows et du moteur en mode bloc.
- `backup-summary`: Résumé des opérations locales de Sauvegarde Windows ; aucune modification des sauvegardes.
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: Aperçu par défaut ; la Corbeille est tout ou rien et l'âge ne s'applique pas.
- `disk-cleanup-task`: État exact et historique d'exécution de la tâche, en lecture seule ; aucune exécution de tâche ni nettoyage.
- `disk-diagnostic`: Met en corrélation les tâches exactes de diagnostic de disque et l'historique des événements ; aucune action sur les tâches ni les disques.
- `disk-diagnostic-tasks`: Historique exact des tâches de collecte et de résolution, en lecture seule ; aucune action sur les tâches ni les disques.
- `disk-quotas`: Quotas par défaut et état de chaque volume, en lecture seule ; identités des utilisateurs exclues ; aucune modification.
- `file-history-events`: Nombres d'avertissements et d'erreurs de l'Historique des fichiers avec projection de confidentialité ; aucune modification.
- `file-history-status`: État de protection de l'Historique des fichiers du compte actuel ; aucune modification.
- `search-index-rebuild [--execute]`: Aperçu par défaut ; conserve l'index précédent et vérifie le nouvel état.
- `search-index-rebuild-recover`: Rapprochement en lecture seule sans répéter les commandes de réparation.
- `storage-check DRIVE_LETTER`: Vérification NTFS fixe en lecture seule ; résultats JSON et progression en direct.
- `storage-events`: Historique du stockage en lecture seule ; JSON avec les preuves brutes.
- `storage-jobs`: État et progression agrégés des tâches du module Storage, en lecture seule ; JSON ; aucune modification.
- `storage-reliability`: Compteurs de température, d'usure et d'E/S en lecture seule ; JSON ; aucune réparation.
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: Aperçu par défaut ; l'exécution exige la confirmation d'une sauvegarde ; les volumes système sont planifiés au redémarrage.
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Aperçu uniquement ; aucune réparation ni planification.
- `storage-repair-recover`: Vérifie une réparation du système de fichiers terminée ou planifiée sans la répéter.
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Collecte et lie les preuves actuelles ; aucune réparation ni planification.
- `storage-sense-policy`: Stratégie de nettoyage reconnue de l'ordinateur, en lecture seule ; paramètres utilisateur et identités des fichiers exclus.
- `storage-sense-task`: État exact et historique d'exécution de la tâche, en lecture seule ; aucune exécution de tâche ni nettoyage.
- `storage-service-events`: Lit l'historique exact des défaillances de Service de stockage, Optimiser les lecteurs, VSS et du fournisseur de clichés logiciels.
- `volume-capacity DRIVE:`: Espace disponible en lecture seule, avec preuves JSON.
- `volume-dirty DRIVE:`: État du bit d'intégrité en lecture seule ; aucune réparation ni redémarrage.
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: Analyse en lecture seule par défaut ; un volume exact en bon état et un support compatible sont requis.
- `volume-optimize-recover`: Vérifie une opération en attente terminée sans la répéter.
- `vss-providers`: Noms, versions et types des fournisseurs VSS, en lecture seule ; identifiants exclus ; aucune modification d'inscription.
- `vss-writers`: États signalés des rédacteurs ; interprétation de la sortie en anglais ; aucune réinitialisation.

## Impression

- `print-events`: Historique d'administration de PrintService des sept derniers jours, en lecture seule ; preuves JSON.
- `print-extension-files`: Résumé de présence et de signature des binaires des processeurs et moniteurs d'impression, en lecture seule ; chemins complets exclus.
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: Aperçu par défaut ; uniquement un travail exact en état Erreur ou Bloqué.
- `print-job-cancel-recover [--folder PATH]`: Revérifie une annulation interrompue sans la répéter.
- `print-job-list`: Liste les travaux visibles actuels sans les modifier.
- `print-operational-events`: État du canal opérationnel de PrintService et historique disponible, en lecture seule ; preuves JSON.
- `print-policy`: Restrictions Point and Print et stratégie d'installation des pilotes, en lecture seule ; preuves JSON.
- `print-rendering-policy`: Stratégie de rendu et d'isolation des pilotes en lecture seule ; preuves JSON.
- `print-spooler-events`: Défaillances du Gestionnaire de contrôle des services liées à l'identité locale du Spouleur d'impression, en lecture seule ; preuves JSON.
- `printer-check --queue NAME [--queue NAME ...]`: Paramètres de la file locale sélectionnée, en lecture seule.
- `printer-conditions`: Codes d'état des files locales en lecture seule ; aucune vérification physique ni modification.
- `printer-driver-files`: Résumé de présence des fichiers inscrits et de signature des exécutables, en lecture seule ; chemins complets exclus.
- `printer-driver-models`: Résumé version 3, version 4 et prise en charge des packages, en lecture seule ; preuves JSON.
- `printer-drivers`: Inventaire et mise en correspondance des inscriptions de files et de pilotes, en lecture seule ; preuves JSON.
- `printer-endpoint-check`: Connexions délimitées aux points de terminaison exacts attribués des imprimantes TCP/IP standard ; aucune découverte, SNMP ni donnée d'impression.
- `printer-list`: Découvre les files locales sans modifier les paramètres.
- `printer-ports`: Mise en correspondance des inscriptions de files et de ports, en lecture seule ; preuves JSON.
- `printer-queue-attributes`: Attributs de spoule, de conservation et de partage des files locales, en lecture seule.
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: Aperçu par défaut ; désactiver le mode hors connexion peut libérer les travaux en attente.

## Réseau

- `dhcp-check`: Éligibilité des baux DHCP manquants ou expirés, en lecture seule ; preuves JSON, aucun renouvellement.
- `dhcp-events`: Historique des événements du client DHCP IPv4 et IPv6, en lecture seule ; résultat JSON.
- `dhcp-leases`: Preuves d'horodatage, d'expiration et de serveur du bail, en lecture seule ; résultat JSON.
- `dhcp-renew [--execute]`: Aperçu par défaut ; uniquement les baux DHCP exacts manquants ou expirés des cartes physiques connectées.
- `dhcp-renew-recover`: Rapprochement en lecture seule sans demander un autre bail.
- `dns-cache HOST [--execute]`: Aperçu par défaut ; vidage protégé du cache local avec vérification de l'hôte exact.
- `dns-cache-recover`: Revérifie une opération de cache interrompue sans répéter le vidage.
- `dns-cache-summary`: État du cache DNS local et nombres par type d'enregistrement, limités par la confidentialité ; aucune modification.
- `dns-check HOST [--timeout-seconds 1..60]`: Résolution de noms système délimitée ; résultat JSON.
- `dns-client-config`: Preuves de suffixe, d'inscription, de dévolution et de stratégie, en lecture seule ; résultat JSON.
- `dns-client-events`: Historique des avertissements et des échecs du client DNS limité par la confidentialité, en lecture seule ; résultat JSON.
- `dns-nrpt-policy`: Preuves de la stratégie effective d'espace de noms DNS, en lecture seule ; résultat JSON.
- `ip-addresses`: Examen de l'état, de l'origine et des conflits des adresses IP actuelles, en lecture seule ; preuves JSON.
- `ip-interface-config`: Preuves de configuration des interfaces IP en lecture seule ; résultat JSON.
- `network-firewall`: Met en corrélation les attributions réseau actives avec l'activation du profil de pare-feu correspondant.
- `network-offload`: Configuration globale du déchargement réseau de Windows en lecture seule ; résultat JSON.
- `network-profiles`: Catégories actuelles des profils et classifications de connectivité, en lecture seule ; noms de réseaux exclus.
- `network-service-events`: Lit l'historique exact des défaillances de DHCP, DNS, Station de travail, NLA, Service Liste des réseaux et Assistance IP.
- `proxy-check`: Preuves du proxy WinHTTP enregistré et du proxy Internet du compte actuel, en lecture seule.
- `route-priority`: Comparaison de la métrique effective de l'itinéraire par défaut, en lecture seule ; preuves JSON.
- `smb-client-config`: Configuration de signature, d'accès invité et de transport SMB, en lecture seule.
- `smb-connections`: Négociation de protocole active en lecture seule ; identités distantes exclues.
- `smb-server-config`: Configuration du protocole, de la signature, du chiffrement et du multicanal SMB, en lecture seule.
- `smb-shares`: Inventaire délimité des partages SMB en lecture seule ; chemins, autorisations, sessions et comptes exclus.
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: Vérification de connexion délimitée ; aucune donnée d'application envoyée.
- `tcp-port-pressure`: Plages TCP dynamiques et utilisation agrégée des ports locaux, en lecture seule ; points de terminaison et processus exclus.
- `tcp-settings`: Preuves de configuration des modèles TCP de Windows, en lecture seule ; résultat JSON.
- `vpn-profiles`: Configuration des profils VPN du compte actuel en lecture seule ; identifiants exclus.
- `winrm-listeners`: Résumé de l'inscription des écouteurs WinRM en lecture seule ; empreintes de certificats exclues.
- `winsock-catalog`: Structure du catalogue natif des protocoles Winsock, en lecture seule ; résultat JSON.
- `winsock-namespaces`: Structure native des fournisseurs d'espaces de noms Winsock, en lecture seule ; résultat JSON.
- `winsock-provider-files`: Présence et signatures des fichiers des fournisseurs de protocoles, en lecture seule ; résultat JSON.
- `winsock-readiness`: Disponibilité native des protocoles, des fournisseurs d'espaces de noms et des fichiers de fournisseurs Winsock, en lecture seule ; résultats JSON.

## Applications

- `app-folder-review FOLDER_NAME [--report]`: Examen de dossiers exacts à racine fixe ; aucune suppression.
- `app-package-review FULL_NAME [--all-users]`: Cible exacte et packages dépendants ; aucune suppression.
- `app-packages [--all-users]`: Inventaire des packages Appx/MSIX inscrits uniquement ; aucune suppression.
- `browser-extensions`: Examen des identités des extensions Chrome, Edge, Brave et Firefox ; aucune suppression.
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: Examen uniquement ; aucune modification du système.
- `depot-review [application and configuration selections] [--json]`: Examen uniquement ; aucun provisionnement.
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: Vérification Authenticode et du signataire de l'éditeur, en lecture seule ; rien n'est exécuté.
- `foxit-inventory`: Inventaire des produits de l'ordinateur en lecture seule ; sortie JSON.
- `installed-apps [--current-user]`: Inscriptions de désinstallation et indicateurs de capacité ; aucune suppression.
- `installed-apps-compare BEFORE.json AFTER.json`: Différences d'inscription par rapport à un inventaire enregistré ; aucune modification de l'ordinateur.
- `installed-hotfixes`: Inventaire délimité des correctifs Windows installés, en lecture seule ; preuves JSON.
- `installer-activity`: Preuves des processus Installer et des marqueurs de transaction, en lecture seule ; preuves JSON.
- `installer-check`: Stratégie d'Installer et disponibilité du moteur, en lecture seule ; preuves JSON.
- `installer-engine-files`: Résumé de présence et Authenticode des fichiers requis d'Installer, en lecture seule ; preuves JSON.
- `installer-events`: Historique MSI des sept derniers jours en lecture seule ; preuves JSON.
- `installer-logging`: Stratégie de journalisation et de débogage d'Installer, en lecture seule ; preuves JSON.
- `installer-pending-state`: Corrélation entre opérations de fichiers en attente et activité d'Installer, en lecture seule ; preuves JSON.
- `installer-registration-repair [--execute]`: Aperçu par défaut ; uniquement un défaut exact d'inscription du service ; ne désinscrit jamais le moteur.
- `installer-registration-repair-recover`: Revérifie une inscription interrompue sans répéter les commandes.
- `installer-security-policy`: Stratégie de sécurité et de correctifs d'Installer, en lecture seule ; preuves JSON.
- `installer-service-events`: Défaillances du Gestionnaire de contrôle des services liées à l'identité locale de Windows Installer, en lecture seule ; preuves JSON.
- `installer-service-registration`: Résumé de l'inscription du service Windows Installer, en lecture seule ; preuves JSON.
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: Nouvel aperçu de l'éligibilité exacte ; aucun fournisseur de suppression.
- `mace-package-recover`: Rapproche à nouveau un enregistrement interrompu de suppression d'un package d'application ; ne répète jamais la suppression.
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: Examen uniquement ; aucune suppression.
- `msi-cache`: Visibilité du cache inscrit de Windows Installer, en lecture seule ; preuves JSON.
- `msi-code NONNEGATIVE_DECIMAL`: Référence documentée des codes de retour de Windows Installer ; aucune inspection de l'ordinateur ni réparation.
- `msi-product {PRODUCT-CODE}`: État exact de l'inscription MSI en lecture seule ; preuves JSON.
- `msi-repair {PRODUCT-CODE} [--execute]`: Aperçu par défaut ; uniquement un produit exact endommagé ou mal configuré.
- `msi-repair-recover`: Revérifie la réparation interrompue d'un produit exact sans la répéter.
- `msi-sources`: Métadonnées des sources inscrites de Windows Installer limitées par la confidentialité, en lecture seule ; preuves JSON.
- `orca-cache-reset --mode New|Classic|Both [--execute]`: Aperçu par défaut ; conserve les caches exacts du compte en cours d'exécution sous des noms de récupération.
- `orca-cache-reset-recover`: Vérifie une conservation de cache interrompue sans répéter de déplacement.
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: Examen uniquement ; aucune modification d'Outlook.
- `startup-apps`: Examen des inscriptions de démarrage et des tâches de démarrage ou d'ouverture de session ; aucune désactivation.
- `third-party-update ID SOURCE VERSION [--accept-agreements] [--execute]`: Aperçu par défaut ; une seule version exacte.
- `third-party-update-recover`: Vérifie une version exacte en attente sans relancer la mise à jour.
- `third-party-updates`: Découverte des mises à jour d'applications avec Microsoft.WinGet.Client, en lecture seule ; aucune modification des applications.

## Système

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: Recherche locale exacte des attributions du Bluetooth SIG ; aucune requête réseau.
- `crash-config`: Mode et chemins de vidage Windows enregistrés ; aucune modification.
- `crash-dumps`: Métadonnées des fichiers de vidage Windows par défaut ; aucune analyse du contenu ni modification.
- `crash-events`: Rapports d'erreurs système récents ; aucun diagnostic ni modification.
- `crash-review [--knowledge PACK_PATH ...]`: Jusqu'à 32 packs ; examen combiné des plantages, des arrêts, de la structure délimitée des minividages et, en option, de la référence exacte de vérification des bogues ; aucune modification.
- `default-printer`: Sélection par défaut et paramètre de gestion automatique, en lecture seule ; preuves JSON.
- `default-printer-set QUEUE [--execute] [--folder PATH]`: Aperçu par défaut ; uniquement si l'imprimante par défaut est manquante ou en conflit.
- `default-printer-set-recover [--folder PATH]`: Revérifie une sélection interrompue sans la répéter.
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: Preuves de périphériques en lecture seule avec des références exactes facultatives de pilotes provenant de jusqu'à 32 packs.
- `device-code DECIMAL_OR_0xHEX`: Référence hors ligne des codes de problème du Gestionnaire de périphériques.
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: Candidats de référence uniquement.
- `event-channel-enable CHANNEL [--execute]`: Aperçu par défaut ; active un seul canal de diagnostic exact non géré et conserve les autres paramètres.
- `event-channel-enable-recover`: Rapprochement en lecture seule sans répéter la commande d'activation.
- `event-channels`: État, rétention, taille et saturation de quatre canaux de diagnostic, en lecture seule.
- `font-registrations`: Vérification délimitée des inscriptions et de la présence des fichiers de l'ordinateur et de l'utilisateur actuel, en lecture seule.
- `memory-diagnostic-results`: Historique délimité de l'Outil Diagnostics de mémoire Windows, en lecture seule ; preuves JSON.
- `minidump-analyze PATH`: Métadonnées structurelles délimitées des minividages ; aucune page mémoire, aucun symbole ni modification.
- `name-rename PROPOSED_NAME [--execute]`: Aperçu par défaut ; PC autonomes en groupe de travail uniquement ; ne redémarre jamais Windows.
- `name-rename-recover`: Vérifie un changement de nom en attente sans le répéter.
- `name-review PROPOSED_NAME`: Examen de l'identité et du nom proposé, en lecture seule.
- `profiles --recover`: Vérifie l'absence consignée et conserve les preuves de récupération ; ne relance jamais la suppression.
- `profiles --recover-backup`: Vérifie et archive un artefact de sauvegarde en attente ; ne relance jamais la copie.
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: Aperçu d'abord ; la copie native protégée exige une exécution explicite et une destination NTFS locale inexistante.
- `profiles --select SID --create-backup-artifact DESTINATION`: Compare une sauvegarde déjà copiée et crée son artefact fixe ; ne copie ni ne supprime aucun profil.
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: Examen en lecture seule ; la vérification exige un SID sélectionné.
- `task-check`: Inventaire des tâches, constats classés et preuves JSON, en lecture seule.
- `task-definition TASK_FOLDER TASK_NAME`: XML exact de la tâche en lecture seule ; indisponible ne signifie pas absent.
- `task-inventory`: Identités et états des tâches en lecture seule ; sortie JSON.
- `task-journal RECORD.json [--completion COMPLETION.json]`: Examen historique de la récupération ; n'autorise jamais de nouvelle tentative.
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: Valide et examine un brouillon de tâche ; aucune inscription.
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: Aperçu par défaut ; définition désactivée en création seule sous Système local.
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: Rapprochement en lecture seule de la définition exacte ; ne relance ni n'exécute jamais une tâche.
- `task-xml PLAN.json YYYY-MM-DD`: XML d'examen désactivé sur la sortie standard ; planifications de calendrier dans le fuseau local uniquement ; aucune inscription.
- `time-config`: Preuves du mode, des homologues et de la stratégie du Temps Windows, en lecture seule.
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: Aperçu par défaut ; le mode personnalisé exige des homologues.
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: Aperçu uniquement.
- `time-recover --folder PATH`: Vérifie et archive un enregistrement en attente ; ne relance jamais les modifications.
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: Lecture seule ; le mode personnalisé exige des homologues.
- `wmi-provider`: Requête locale du fournisseur du système d'exploitation, en lecture seule ; preuves JSON.

## Connaissances, validation et Engine

- `knowledge PACK_PATH CONTEXT CODE`: Recherche de référence locale ; aucune exécution de réparation.
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: Activation locale vérifiée ; aucun téléchargement.
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Vérifie le pack local actif ; aucune modification.
- `knowledge-compare BEFORE_JSON AFTER_JSON`: Modifications de référence uniquement ; aucune installation de pack.
- `knowledge-info PACK_PATH`: Métadonnées du pack et nombres d'entrées par contexte ; aucune modification.
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: Associe des références contextuelles aux codes observés.
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Sélectionne le pack conservé vérifié le plus récent ; aucun téléchargement.
- `knowledge-search PACK_PATH CONTEXT TEXT`: Jusqu'à 50 correspondances de texte de référence ; aucune exécution de réparation.
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: Résout une identité exacte dans jusqu'à 32 packs ; un conflit n'est jamais sélectionné.
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: Vérifie chaque base de données installée configurée ; aucun téléchargement ni activation.
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: Récupère et active de manière atomique des bases de données candidates signées depuis les points de terminaison HTTPS configurés.
- `open <file.kbench> [--json]`: Rouvre une capture enregistrée et l'évalue à nouveau avec les règles installées.
- `scan --output <new-file.kbench> [--json]`: Capture Windows Update en lecture seule enregistrée dans un nouveau fichier.
- `validation-coverage RECORD.json [...]`: Résume les preuves natives sur VM de la version actuelle ; aucun diagnostic ni réparation.
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: Plan de scénarios sur VM jetables en lecture seule ; aucun diagnostic ni réparation.
- `validation-record RECORD.json`: Valide un enregistrement délimité de preuves natives sur VM ; aucun diagnostic ni réparation.
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: Rapport local de couverture sur VM de la version actuelle.
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Exécute un plan d'options partagé validé en lecture seule.
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: Inventaire uniquement.
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Journal de diagnostic ciblé ; aucune réparation.
- `workflow computer-name --name PROPOSED_NAME [--json]`: Examen journalisé uniquement.
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: État exact facultatif du produit inclus dans le journal.
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: Diagnostic facultatif de la cible sélectionnée inclus dans le journal.
- `workflow storage [--volume D:] [--json]`: Vérification du système de fichiers en lecture seule journalisée.
- `workflow-journal FILE.json`: Contenu historique ; le code 0 signifie que la lecture a réussi, pas que l'état est bon.
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: Affiche ou valide des contrôles indépendants de l'hôte ; aucune analyse ni réparation.
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: Rapport historique uniquement ; aucune commande exécutée.

## Autres commandes

- `<analyze|investigate> <text-log-path> [--json]`: Analyse des preuves texte locales.
- `adapter-bindings`: Résumé des liaisons des protocoles principaux, en lecture seule ; preuves JSON.
- `adapter-statistics`: Compteurs d'octets, d'erreurs et de rejets des cartes, en lecture seule ; preuves JSON.
- `application-events`: Erreurs récentes du journal Application, nombres groupés et preuves JSON, en lecture seule.
- `applocker-policy`: Modes de collecte effectifs et nombres agrégés de règles, en lecture seule ; détails des règles exclus ; aucune modification.
- `common-check [--scope NAME ...]`: Tous les domaines de diagnostic par défaut ; uniquement les domaines sélectionnés s'ils sont indiqués ; JSON en lecture seule.
- `delete-notifications`: Configuration de notification de suppression NTFS et ReFS, en lecture seule ; JSON ; aucune optimisation ni modification de paramètre.
- `diagnostic-scopes [--all]`: Liste 11 groupes de résultats recommandés, ou les 49 domaines en lecture seule ; aucune collecte.
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Journal de diagnostic ciblé ; aucune réparation.
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Tous les domaines de diagnostic ; aucune réparation sauf si `--auto-repair` est indiqué explicitement.
- `gateway-check`: Vérifications ICMP délimitées des prochains sauts par défaut exacts configurés ; aucun balayage d'adresses.
- `infrastructure-service-events`: Lit un instantané délimité et exact des défaillances des services de connectivité, de gestion à distance, de notification et des services principaux.
- `investigate-batch FILE [FILE ...]`: Jusqu'à 12 fichiers ; JSON Lines ; code 3 pour les importations échouées ou partielles.
- `licensing-check`: État d'activation de Windows sans clés de produit, en lecture seule ; preuves JSON.
- `mac-vendor PACK_PATH MAC_ADDRESS`: Recherche dans un pack local par plus long préfixe ; aucune requête réseau.
- `ncsi-policy`: Stratégie de classification de la connectivité de Windows, en lecture seule ; destinations de sondage exclues.
- `neighbor-cache`: Preuves d'accessibilité des voisins limitées par la confidentialité, en lecture seule ; résultat JSON.
- `ntfs-self-healing DRIVE:`: Configuration de réparation NTFS en ligne du volume sélectionné, en lecture seule ; aucune réparation ni modification de paramètre.
- `pagefile-check`: Résumé de la gestion, de l'allocation et de l'utilisation du fichier d'échange, en lecture seule ; chemins complets exclus.
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: Recherche exacte dans un pack local ; aucune requête réseau ni sélection de pilote.
- `physical-disks`: État actuel d'intégrité, opérationnel, de support, de bus et d'utilisation, en lecture seule ; JSON ; aucune réparation.
- `platform-service-events`: Lit un instantané délimité et exact des défaillances des services d'applications, d'identité, de profil et de prise en charge des périphériques.
- `previous-windows [--execute --backup-confirmed]`: Aperçu par défaut ; supprime définitivement la capacité de restauration de Windows au moyen de DISM.
- `previous-windows-recover`: Rapprochement en lecture seule sans répéter la suppression de la restauration.
- `provisioned-packages`: Packages préparés pour les nouveaux utilisateurs dans le Windows local ; aucune suppression.
- `recipes [WORKFLOW]`: Liste les définitions installées ou les dépendances et limites propres à un flux de travail ; aucune exécution.
- `repair <dism|sfc> [--execute]`: Aperçu par défaut.
- `reserved-storage`: État du stockage réservé de Windows en ligne, en lecture seule ; preuves JSON.
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: Aperçu par défaut ; crée un point après de nouvelles vérifications.
- `restore-point-recover`: Vérifie une demande en attente sans créer un autre point.
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: Aperçu uniquement.
- `shadow-copies`: États et attributs actuels des clichés VSS, en lecture seule ; identités exclues ; aucune modification des clichés.
- `shadow-storage`: Allocation et pression de la zone différentielle VSS, en lecture seule ; JSON ; aucun redimensionnement ni action sur les clichés.
- `uac-policy`: Activation de l'UAC et stratégie d'invite d'élévation, en lecture seule ; aucune donnée de compte ni d'identifiant.
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: Recherche locale exacte dans le registre UEFI ; aucune requête réseau ni sélection de pilote.
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: Recherche exacte dans un pack local ; aucune requête réseau ni sélection de pilote.
- `vitals-check`: Instantanés de Windows, du matériel, du compte, des ressources, de la batterie et des disques physiques, en lecture seule ; preuves JSON.
- `win32-code DECIMAL_OR_0xHEX`: Recherche hors ligne de messages dans le Windows installé.
- `workflows`: Liste les clés et les titres des flux de travail installés ; aucune analyse ni réparation.
