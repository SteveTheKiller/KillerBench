# Mise en service

Mise en service prépare un nouvel ordinateur. Elle examine les applications et la configuration que vous sélectionnez et nomme l'ordinateur. Elle ne télécharge ni n'installe rien d'elle-même.

Guides pas à pas : [Configurer un nouvel ordinateur](set-up-new-pc.html).

## Vérifier un programme d'installation avant de l'exécuter

La ligne de commande peut vérifier le programme d'installation d'un éditeur avant que vous ne l'exécutiez :

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
```

KillerBench ne fait confiance à un programme d'installation que s'il porte une signature Authenticode valide au nom exact de l'organisation de l'éditeur. Il n'exécute jamais le programme d'installation.

## Outils de cette section

Préparez un nouvel ordinateur : applications, nom et configuration.

### Mise en service

Examine les applications de provisionnement et les groupes de configuration explicitement sélectionnés. Les inscriptions actuelles de désinstallation, de packages d'applications et d'Office Démarrer en un clic sont mises en correspondance sans télécharger ni exécuter de programmes d'installation. Les sélections de configuration décrivent leurs conséquences et renvoient vers des flux de travail protégés distincts lorsqu'ils existent. Aucune action de téléchargement, d'installation, de compte, de mise à jour, de nettoyage, de stratégie, de registre, de service, de profil ou de redémarrage n'a lieu.

### Nom de l'ordinateur

Compare le nom proposé pour l'ordinateur à l'identité actuelle et au rôle dans le domaine. Aucun changement de nom, aucune collecte d'identifiants et aucun redémarrage. La disponibilité du nom, les autorisations et les dépendances des applications restent non vérifiées.
