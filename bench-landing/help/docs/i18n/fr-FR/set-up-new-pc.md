# Configurer un nouvel ordinateur

Utilisez ce guide pour préparer un PC neuf ou fraîchement réinitialisé. KillerBench examine l'ordinateur, les applications que vous prévoyez d'installer et le nom que vous voulez lui donner, puis vous oriente vers les outils protégés qui effectuent chaque modification.

## Partir d'un état de référence

1. Exécutez **Analyse complète du système** depuis **Diagnostics** et conservez le rapport comme trace de l'état initial de l'ordinateur.
2. Installez les mises à jour en attente. Voir [Réparer Windows Update](fix-windows-update.html) si certaines échouent.

## Examiner les applications et la configuration

1. Ouvrez **Mise en service** et choisissez **Mise en service**.
2. Sélectionnez les applications et les groupes de configuration que vous prévoyez de mettre en place et appuyez sur **Exécuter le diagnostic**. L'examen les compare à ce qui est déjà installé. KillerBench ne télécharge ni n'exécute de programmes d'installation.
3. Avant d'exécuter le programme d'installation d'un éditeur, vérifiez-le depuis la ligne de commande. KillerBench ne lui fait confiance que s'il porte une signature valide au nom exact de l'organisation de cet éditeur.

## Nommer l'ordinateur

1. Choisissez **Nom de l'ordinateur**, saisissez le nom et appuyez sur **Exécuter le diagnostic** pour le comparer à l'identité actuelle et au rôle dans le domaine.
2. Sur un ordinateur de groupe de travail, cochez **Renommer ce PC de groupe de travail après des vérifications fraîches** pour le renommer. Un redémarrage est nécessaire pour terminer.

## Le protéger et le mettre en ordre

- [Chiffrez un lecteur avec BitLocker](encrypt-with-bitlocker.html) et sauvegardez la clé de récupération.
- [Rendez Windows plus confidentiel](make-windows-private.html) et examinez **Désencombrer Windows** dans **Maintenance**.
- Créez un point de restauration avec **Préparer un point de restauration** une fois que tout est en place.

## Depuis la ligne de commande

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

Voir [workflow](cli.html#cmd-workflow) et [restore-point](cli.html#cmd-restore-point).

## Voir aussi

- [Section Mise en service](onboarding.html)
- [Votre premier diagnostic](first-run.html)
