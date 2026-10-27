# Rendre Windows plus confidentiel

Utilisez ce guide pour examiner et renforcer la confidentialité d'un ordinateur portable. KillerBench lit les paramètres actuels, montre ceux qu'une stratégie ou la gestion des appareils contrôle déjà, et ne modifie que les paramètres que vous choisissez, un par un, avec une annulation exacte.

## Examiner les paramètres actuels

1. Ouvrez **Confidentialité**.
2. Cochez les domaines à examiner. Tous les domaines sont décochés au départ.
3. Appuyez sur **Exécuter le diagnostic**. Le rapport affiche la valeur actuelle pour cet ordinateur et pour le compte qui exécute KillerBench, et signale les paramètres contrôlés par une stratégie.

## Modifier un paramètre

1. Choisissez le paramètre dans **Paramètre de confidentialité**, comme la caméra, le microphone, la localisation, les données de diagnostic, la publicité, l'historique du Presse-papiers, la recherche de contenu dans le cloud ou les points forts de la recherche.
2. Choisissez la nouvelle valeur et cochez **Appliquer quand je sélectionne Exécuter le diagnostic**.
3. Appuyez sur **Exécuter le diagnostic**. KillerBench vérifie si une stratégie contrôle le paramètre, enregistre la valeur actuelle, effectue cette seule modification et la relit.

Les paramètres contrôlés par une stratégie de groupe ou par la gestion des appareils ne sont pas touchés. Modifiez-les là où ils sont gérés.

## Annuler une modification

Chaque modification vérifiée conserve un journal. L'annulation correspondante restaure la valeur précédente exacte, là encore après une vérification de stratégie.

## Désencombrer Windows

Pour les suggestions, le contenu grand public, Recall et les fonctionnalités d'IA, l'encombrement de la barre des tâches et de Démarrer, Edge et les logiciels préinstallés, ouvrez **Maintenance** et choisissez **Désencombrer Windows**. L'examen est en lecture seule et rien n'est sélectionné automatiquement.

## Depuis la ligne de commande

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

Voir [privacy-plan](cli.html#cmd-privacy-plan), [privacy-setting](cli.html#cmd-privacy-setting), [privacy-setting-undo](cli.html#cmd-privacy-setting-undo) et [debloat-review](cli.html#cmd-debloat-review). Une clé non prise en charge affiche la liste des choix pris en charge.

## Voir aussi

- [Section Confidentialité](privacy.html)
- [Configurer un nouvel ordinateur](set-up-new-pc.html)
