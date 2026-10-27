# Réparer Windows Update

Utilisez ce guide lorsque les mises à jour échouent, se bloquent ou se réinstallent sans cesse. KillerBench lit d'abord la chaîne de mise à jour, explique l'échec avec les preuves qui le soutiennent, et ne démarre un service ou ne répare le magasin de composants que lorsque des preuves récentes démontrent ce problème exact.

## Avant de commencer

- Exécutez KillerBench en tant qu'administrateur. Les services de mise à jour, le magasin de composants et les fichiers système protégés nécessitent une élévation pour fournir des preuves complètes.
- Redémarrez d'abord si Windows attend déjà un redémarrage. De nombreuses réparations de mise à jour restent bloquées tant qu'un redémarrage est en attente.
- Laissez l'ordinateur branché sur secteur pendant la maintenance.

## Comprendre pourquoi les mises à jour échouent

1. Ouvrez **Mises à jour** et choisissez **Windows Update** dans la barre latérale des outils.
2. Laissez **Réparer automatiquement les problèmes confirmés** désactivé pour le premier passage et appuyez sur **Exécuter le diagnostic**.
3. Lisez le **Rapport lisible**. Il liste les services de mise à jour arrêtés, les redémarrages en attente, le manque d'espace libre, les corruptions du magasin de composants et les codes d'erreur exacts enregistrés par Windows, avec une étape suivante recommandée.
4. Si le rapport mentionne un code d'erreur, les vérifications de suivi correspondantes s'exécutent automatiquement. Vous pouvez aussi en rechercher un vous-même avec `killerbench-cli update-code 0x80070002`.

## Surveiller une mise à jour qui semble bloquée

Choisissez **Moniteur d'activité de maintenance** dans **Mises à jour** et appuyez sur **Exécuter le diagnostic**. Il prend des instantanés répétés de l'activité de mise à jour, de Setup et d'Installer, des totaux de transfert et des indicateurs de redémarrage. L'activité est présentée uniquement comme preuve ; elle n'affirme jamais d'elle-même un blocage ou une panne.

## Réparer ce que le rapport a trouvé

1. Relancez **Windows Update** avec **Réparer automatiquement les problèmes confirmés** activé. Il peut démarrer des services de mise à jour activés dont l'arrêt est confirmé, et n'exécute DISM ou SFC que lorsque la vérification en direct confirme une corruption.
2. Pour un seul service, choisissez plutôt son outil ciblé, comme **Service BITS**, **Services de chiffrement**, **Programme d'installation pour les modules Windows** ou **Service Update Orchestrator**. Chacun ne propose une récupération que pour ce service exact.
3. Quand le rapport demande un redémarrage, redémarrez avant d'exécuter quoi que ce soit d'autre. KillerBench consigne les résultats qui exigent un redémarrage et reporte les tâches suivantes jusqu'au redémarrage.

Chaque réparation est journalisée et relue. Voir [Comment les modifications restent sûres](safety.html) pour la séquence complète.

## Installer une mise à jour précise

1. Choisissez **Mises à jour disponibles** dans **Mises à jour** et appuyez sur **Exécuter le diagnostic** pour découvrir les mises à jour de la source configurée.
2. Sélectionnez les mises à jour voulues et appuyez sur **Prévisualiser la sélection**. L'aperçu indique exactement ce qui serait installé et tout redémarrage nécessaire.
3. Lisez l'aperçu, cochez la case de confirmation, puis appuyez sur **Installer**. La case se décoche après chaque exécution, et KillerBench ne sélectionne ni n'approuve jamais une mise à jour à votre place.

## Depuis la ligne de commande

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

Voir [update-search](cli.html#cmd-update-search), [update-history](cli.html#cmd-update-history), [servicing-monitor](cli.html#cmd-servicing-monitor) et [update-plan](cli.html#cmd-update-plan). Si une installation a été interrompue, exécutez [update-recover](cli.html#cmd-update-recover) ; il vérifie le résultat et ne relance jamais la mise à jour.

## Voir aussi

- [Section Mises à jour](updates.html)
- [Réparer les fichiers système](repair-system-files.html)
- [Dépannage](troubleshooting.html)
