# Dépannage

## Une action indique qu'elle est bloquée

Un résultat bloqué signifie que l'action demandée n'a pas pu se poursuivre. Lisez le message et le journal pour savoir si elle s'est arrêtée avant l'envoi ou après une étape précédente. Ne supposez pas qu'une opération partiellement terminée a été annulée. Les causes de prérequis les plus fréquentes sont les suivantes.

- **Un accès administrateur est requis.** Fermez KillerBench et relancez-le avec **Exécuter en tant qu'administrateur**.
- **Aucun redémarrage ne doit être en attente.** Windows attend un redémarrage. Redémarrez l'ordinateur, puis relancez l'action.
- **Une stratégie gérée est présente.** La stratégie de groupe ou la gestion des appareils contrôle ce paramètre. Modifiez-le à cet endroit.
- **Les preuves ont changé.** Quelque chose a changé entre la première vérification et la vérification finale. Relancez l'outil pour lire le nouvel état.

## Une demande précédente doit être rapprochée

KillerBench a trouvé un enregistrement en attente provenant d'une demande interrompue. Relancez la même action. Elle vérifie ce qui s'est passé et signale le résultat réel sans répéter la demande. En ligne de commande, utilisez la commande `-recover` correspondante, par exemple `killerbench-cli bitlocker-auto-unlock-recover`.

## Une vérification affiche Incomplètes ou Inconnu

Les preuves n'ont pas pu être lues, souvent parce que KillerBench ne s'exécutait pas en tant qu'administrateur ou qu'un service Windows était indisponible. KillerBench le signale honnêtement au lieu de supposer que l'ordinateur est en bon état.

## Où les fichiers sont conservés

Les journaux, les enregistrements en attente et les preuves enregistrées se trouvent sous `%LOCALAPPDATA%\KillerBench`. Chaque action protégée possède son propre dossier. Conservez-les lorsque vous avez besoin d'une trace de ce qui a changé.

## Signaler un problème

KillerBench est en développement. Joignez le rapport exporté et le journal d'activité lorsque vous signalez un problème, et retirez tout ce que vous ne voulez pas partager.
