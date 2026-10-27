# Vérifier et réparer un lecteur

Utilisez ce guide lorsqu'un volume signale des erreurs, que Windows demande d'analyser un lecteur ou que des fichiers refusent de s'ouvrir. KillerBench vérifie d'abord le volume en lecture seule et ne planifie ou n'exécute une réparation du système de fichiers qu'après que vous avez confirmé disposer d'une sauvegarde.

## Avant de commencer

- Sauvegardez les données importantes du volume. Une réparation du système de fichiers peut modifier ou supprimer des fichiers endommagés.
- Exécutez KillerBench en tant qu'administrateur.

## Vérifier le volume

1. Ouvrez **Stockage** et choisissez **Stockage**.
2. Choisissez le lecteur dans **Volume à vérifier** et appuyez sur **Exécuter le diagnostic**.
3. Lisez le rapport. Il affiche l'état du disque et du volume, le bit d'intégrité, l'autoréparation NTFS en ligne, les erreurs de contrôleur et de périphériques de disque, les compteurs de fiabilité et les événements de stockage récents, comme les réinitialisations, les nouvelles tentatives et les retraits inattendus.

Une vérification propre signifie qu'aucune réparation n'est proposée. La température du disque, l'usure et les compteurs d'E/S non corrigées sont des preuves à examiner et ne déclenchent pas de réparation à eux seuls.

## Réparer le système de fichiers

1. Appuyez sur **Examiner la réparation du système de fichiers** et choisissez **Réparation ponctuelle** ou **Analyse et réparation hors ligne**.
2. Pour un volume de données, cochez **Autoriser la mise hors ligne d'un volume non système** si vous pouvez fermer tout ce qui l'utilise. Le volume Windows ne peut pas être mis hors ligne ; sa réparation est donc planifiée pour le prochain redémarrage.
3. Cochez **Les données importantes de ce volume sont sauvegardées**.
4. Confirmez la réparation. KillerBench revérifie le volume exact juste avant d'agir et consigne d'abord une entrée en attente.
5. Pour une réparation planifiée, redémarrez quand cela vous convient. KillerBench vérifie le résultat après le redémarrage sans relancer la réparation.

## Optimiser ou relancer Retrim

Le même outil peut analyser le volume et exécuter l'optimisation recommandée par Windows, **Retrim du stockage à semi-conducteurs** ou une défragmentation de disque dur. Chacune est une action confirmée séparément sur un seul volume exact en bon état.

## Depuis la ligne de commande

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

Voir [storage-check](cli.html#cmd-storage-check), [storage-repair-plan](cli.html#cmd-storage-repair-plan), [storage-repair](cli.html#cmd-storage-repair) et [volume-optimize](cli.html#cmd-volume-optimize).

## Voir aussi

- [Section Stockage](storage.html)
- [Libérer de l'espace disque](free-disk-space.html)
- [Dépannage](troubleshooting.html)
