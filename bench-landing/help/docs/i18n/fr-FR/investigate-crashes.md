# Enquêter sur les plantages et les écrans bleus

Utilisez ce guide lorsque Windows redémarre de manière inattendue, affiche une erreur d'arrêt ou qu'une application plante sans cesse. KillerBench rassemble dans un seul rapport les preuves de plantages, d'arrêts, d'erreurs matérielles et de fiabilité. Il décrit ce qui a été enregistré et ne devine jamais une cause profonde.

## Vérifier l'ordinateur

1. Ouvrez **Diagnostics** et choisissez **Signes vitaux**.
2. Appuyez sur **Exécuter le diagnostic**. Le rapport combine la mémoire, l'état des disques, les problèmes de périphériques, l'historique de l'Analyseur de fiabilité, les enregistrements d'erreurs matérielles, les rapports de plantage, les arrêts inattendus et les résultats de l'Outil Diagnostics de mémoire Windows.
3. Lisez la conclusion et les preuves qui l'accompagnent. Les plantages historiques expliquent ce qui s'est passé ; ils ne prouvent pas qu'une panne est toujours présente.

## Affiner la recherche

- **Historique des erreurs matérielles** lit les enregistrements WHEA et de diagnostic de la mémoire avec les problèmes de périphériques et l'état des disques.
- **Historique de fiabilité** regroupe les plantages d'applications, les blocages et les arrêts inattendus des 14 derniers jours.
- **Erreurs d'application** se concentre sur les programmes qui plantent.
- **Historique d'installation des pilotes** affiche les installations de pilotes échouées associées aux périphériques présents actuellement.
- **Problèmes de périphériques** liste les périphériques présentant des codes d'erreur du Gestionnaire de périphériques.

## Lire les vidages sur incident

KillerBench liste la configuration des vidages et les métadonnées des fichiers de vidage, et peut analyser la structure des minividages pour en extraire l'exception et les noms de modules. L'application de bureau n'ouvre jamais le contenu des vidages. Avec un pack de connaissances de vérification des bogues chargé, les codes d'arrêt sont résolus vers leur signification documentée.

Conservez les vidages jusqu'à la fin de votre enquête. **Nettoyage** peut les supprimer, mais cela élimine les preuves.

## Depuis la ligne de commande

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

Voir [vitals-check](cli.html#cmd-vitals-check), [crash-review](cli.html#cmd-crash-review) et [device-code](cli.html#cmd-device-code).

## Voir aussi

- [Section Diagnostics](diagnostics.html#vitals)
- [Rapports et preuves](reports.html)
