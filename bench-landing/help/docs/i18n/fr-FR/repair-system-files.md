# Réparer les fichiers système

Utilisez ce guide lorsque des composants Windows sont corrompus : des fonctionnalités refusent de s'installer, des mises à jour signalent des erreurs de composants ou des outils système plantent. KillerBench n'exécute DISM et SFC qu'après qu'une vérification en direct a confirmé une corruption, puis vérifie le résultat.

## Avant de commencer

- Exécutez KillerBench en tant qu'administrateur.
- Réglez d'abord tout redémarrage en attente.
- Si l'ordinateur est géré, vérifiez si une stratégie dirige DISM vers une source de réparation précise. KillerBench signale cette stratégie et ne la modifie pas.

## Rechercher une corruption

1. Ouvrez **Réparation** et choisissez **Fichiers système**.
2. Laissez **Réparer automatiquement les problèmes confirmés** désactivé et appuyez sur **Exécuter le diagnostic**.
3. Lisez le rapport. Il inventorie les états des packages de maintenance, des fonctionnalités facultatives et des capacités, vérifie les prérequis et analyse les fichiers système protégés. Un résultat propre signifie qu'aucune réparation n'est proposée.

## Réparer une corruption confirmée

1. Activez **Réparer automatiquement les problèmes confirmés** et appuyez sur **Exécuter la réparation**. KillerBench ne choisit DISM ou SFC que lorsque la vérification en direct confirme une corruption.
2. Attendez la vérification. Le rapport n'affiche **Vérifié** que lorsque la vérification de suivi relit un état réparé.
3. Si le rapport indique que le magasin est irrécupérable ou que la source de réparation est indisponible, arrêtez-vous là. Répéter la réparation ne changera pas le résultat.

Une exécution interrompue de DISM ou de SFC est rapprochée au démarrage suivant sans répéter la commande de maintenance.

## Nettoyer le magasin de composants

Choisissez **Nettoyage du magasin de composants** dans **Mises à jour**. Il demande d'abord à Windows si un nettoyage est recommandé. La maintenance facultative supprime les composants remplacés et relance l'analyse. Il n'utilise jamais ResetBase et ne supprime jamais de contenu WinSxS à la main.

## Depuis la ligne de commande

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

Voir [component-repair-source](cli.html#cmd-component-repair-source), [repair](cli.html#cmd-repair) et [servicing-logs](cli.html#cmd-servicing-logs). `repair` affiche un aperçu par défaut et n'agit qu'avec `--execute`.

## Voir aussi

- [Réparer Windows Update](fix-windows-update.html)
- [Section Réparation](repair.html#system-files)
- [Comment les modifications restent sûres](safety.html)
