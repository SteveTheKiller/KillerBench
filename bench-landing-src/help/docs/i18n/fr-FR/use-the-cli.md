# Exécuter KillerBench depuis des scripts

Utilisez la ligne de commande lorsque vous voulez des preuves KillerBench dans un script, une tâche RMM ou une console distante. `killerbench-cli` exécute la même Engine que l'application de bureau et produit du JSON.

## Premiers pas

1. Ouvrez PowerShell dans le dossier de `killerbench-cli.exe`, ou ajoutez ce dossier à votre chemin.
2. Exécutez `killerbench-cli help` pour lister toutes les commandes avec leur limite de sécurité.
3. Exécutez `killerbench-cli diagnostic-scopes` pour voir les 11 groupes de diagnostic recommandés.

## Lancer une analyse et enregistrer un rapport

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan` est uniquement diagnostique et refuse `--auto-repair`. Utilisez un flux de travail de réparation ciblé lorsqu'une action précise est nécessaire. `--report` écrit le même rapport thématisé que celui exporté par l'application. Utilisez `--scope` pour ne vérifier que certains domaines, et `common-check` pour obtenir un JSON combiné sans journal de flux de travail.

## Lire les codes de sortie

| Code | Signification |
| --- | --- |
| 0 | L'aperçu ou le résultat vérifié a réussi. |
| 1 | Une erreur de démarrage, de saisie ou de journal a arrêté la commande. |
| 2 | Les arguments n'ont pas été reconnus. |
| 3 | Bloqué, échoué ou non vérifié. |
| 4 | Un flux de travail a besoin d'un redémarrage avant de pouvoir vérifier. |
| 130 | Vous l'avez arrêtée. |

Un code 0 sur une commande en lecture seule signifie qu'elle s'est exécutée, pas que l'ordinateur est en bon état. Lisez le JSON pour connaître les constats.

## Effectuer une modification

Toute commande capable de modifier l'ordinateur affiche un aperçu par défaut. Lisez l'aperçu, puis relancez la même commande avec `--execute`. Si une demande est interrompue, exécutez la commande `-recover` correspondante ; elle signale le résultat réel et ne répète jamais la modification.

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## Rouvrir d'anciens résultats

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## L'utiliser depuis un assistant d'IA

[KillerMCP](../mcp.html) peut appeler la CLI installée pour rechercher des codes d'erreur du Gestionnaire de périphériques et de Windows.

## Voir aussi

- [Référence de la ligne de commande](cli.html)
- [Guide développeur de l'Engine](../engine/index.html)
- [Dépannage](troubleshooting.html)
