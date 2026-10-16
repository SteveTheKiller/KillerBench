# Résoudre les problèmes d'impression

Utilisez ce guide lorsque des travaux restent dans la file, qu'une imprimante apparaît hors ligne ou que la mauvaise imprimante est définie par défaut. KillerBench lit d'abord le spouleur, les files, les travaux, les pilotes et les ports, puis vous permet de corriger une file ou un travail exact.

## Vérifier les imprimantes

1. Ouvrez **Périphériques** et choisissez **Impression**.
2. Appuyez sur **Trouver les imprimantes locales** et choisissez la file qui pose problème. Pour vérifier toutes les files, choisissez **Utiliser les vérifications générales d'impression**.
3. Laissez **Réparer automatiquement les problèmes confirmés** désactivé et appuyez sur **Exécuter le diagnostic**.
4. Lisez le rapport. Il affiche l'état du Spouleur d'impression, les files en pause, le mode hors connexion, les travaux visibles, l'état des pilotes, les inscriptions de ports, les erreurs récentes de PrintService et, pour les imprimantes réseau, une tentative de connexion à l'adresse attribuée à l'imprimante.

Une connexion réussie ou un code d'état propre ne prouve pas que l'imprimante a du papier, du toner ou un mécanisme qui fonctionne.

## Réparer la file

1. Relancez **Impression** avec **Réparer automatiquement les problèmes confirmés** activé. Il peut démarrer le spouleur activé, reprendre une file exacte en pause qui contient des travaux en attente et désactiver le mode hors connexion.
2. Désactiver le mode hors connexion peut libérer les travaux en attente ; assurez-vous que ces travaux doivent toujours être imprimés.
3. Une file mise en pause volontairement et sans travail en attente reste en pause.

## Supprimer un travail bloqué

1. Appuyez sur **Trouver les travaux d'impression bloqués**.
2. Sélectionnez le travail qui affiche Erreur ou Bloqué et cochez **Annuler uniquement ce travail sélectionné**.
3. Appuyez sur **Exécuter le diagnostic**. Seul ce travail est annulé, et le résultat est relu.

## Corriger l'imprimante par défaut

Choisissez la file qui doit être celle par défaut, cochez **Réparer une imprimante par défaut manquante ou conflictuelle avec cette file**, puis appuyez sur **Exécuter le diagnostic**. Cela n'agit que lorsque l'imprimante par défaut est manquante ou en conflit.

## Depuis la ligne de commande

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

Voir [printer-list](cli.html#cmd-printer-list), [printer-check](cli.html#cmd-printer-check), [printer-repair](cli.html#cmd-printer-repair), [print-job-cancel](cli.html#cmd-print-job-cancel) et [default-printer-set](cli.html#cmd-default-printer-set).

## Voir aussi

- [Section Périphériques](devices.html#printing)
- [Résoudre les problèmes de réseau et d'internet](fix-network.html)
