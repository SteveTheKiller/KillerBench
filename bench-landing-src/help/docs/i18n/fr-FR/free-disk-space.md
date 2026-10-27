# Libérer de l'espace disque

Utilisez ce guide lorsqu'un lecteur manque d'espace. KillerBench montre où part l'espace, prévisualise ce que chaque cible de nettoyage supprimerait et ne supprime qu'après que vous avez activé le nettoyage permanent.

## Voir l'espace restant

1. Ouvrez **Stockage** et choisissez **Stockage**.
2. Appuyez sur **Exécuter le diagnostic**. Le rapport signale chaque volume doté d'une lettre de lecteur ayant moins de 10 % d'espace libre et affiche l'allocation des clichés instantanés, les quotas par défaut et la stratégie de l'Assistant Stockage.
3. Pour les vidages sur incident et l'ancien contenu de mise à jour, exécutez aussi **Signes vitaux** dans **Diagnostics** et **Windows Update** dans **Mises à jour** ; la taille du cache des mises à jour y apparaît.

## Prévisualiser un nettoyage

1. Ouvrez **Maintenance** et choisissez **Nettoyage**.
2. Cochez les cibles voulues, comme **Fichiers temporaires de l'utilisateur actuel**, **Fichiers temporaires Windows**, **Caches de navigateur de l'utilisateur actuel**, **Cache Delivery Optimization** ou **Corbeille de l'utilisateur actuel**. Survolez une cible pour lire exactement les dossiers qu'elle couvre.
3. Réglez **Âge minimum (jours)** et **Nombre maximal de fichiers par cible**.
4. Appuyez sur **Exécuter le diagnostic** avec **Effectuer le nettoyage permanent sélectionné après des vérifications fraîches** désactivé. Le rapport liste les candidats et l'espace estimé pour chaque cible. Rien n'est supprimé.

## Supprimer les fichiers

1. Activez **Effectuer le nettoyage permanent sélectionné après des vérifications fraîches** et appuyez sur **Exécuter le diagnostic**.
2. KillerBench revérifie chaque candidat avant de le supprimer, ignore les fichiers récents, protégés ou en cours d'utilisation, et consigne chaque résultat dans le journal.

Le nettoyage permanent contourne la Corbeille et ne peut pas être annulé. Supprimer les rapports d'erreurs et les vidages sur incident élimine des preuves dont vous pourriez avoir besoin pour enquêter sur des plantages ; examinez-les d'abord.

## Supprimer l'installation Windows précédente

Après une mise à niveau de fonctionnalités, **Installation Windows précédente** dans **Nettoyage** peut supprimer la copie de restauration au moyen de la demande de suppression de restauration prise en charge par Windows. Elle s'exécute seule, nécessite **J'ai sauvegardé tous les fichiers et paramètres dont j'ai besoin depuis l'installation précédente**, et supprime définitivement la possibilité de revenir à la version antérieure.

## Depuis la ligne de commande

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

Voir [volume-capacity](cli.html#cmd-volume-capacity), [cleanup](cli.html#cmd-cleanup) et [previous-windows](cli.html#cmd-previous-windows).

## Voir aussi

- [Section Maintenance](maintenance.html#cleanup)
- [Section Stockage](storage.html)
- [Vérifier et réparer un lecteur](check-a-drive.html)
