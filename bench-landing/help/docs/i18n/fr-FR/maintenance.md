# Maintenance

Maintenance garde l'ordinateur en ordre : nettoyage des fichiers, anciens profils d'utilisateur, tâches planifiées et désencombrement de Windows. Chaque outil prévisualise ce qu'il supprimerait avant toute suppression.

Guides pas à pas : [Libérer de l'espace disque](free-disk-space.html) et [Rendre Windows plus confidentiel](make-windows-private.html).

## Outils de cette section

Nettoyez les fichiers, les profils et les tâches planifiées, et désencombrez Windows.

### Nettoyage

Estime les cibles sélectionnées de fichiers temporaires, de caches et de preuves de diagnostic conservées à l'aide des réglages d'âge et de limite de fichiers du volet principal. Windows Temp et Windows SystemTemp sont des cibles indépendantes. Delivery Optimization est un choix de cache distinct et le contenu supprimé peut devoir être téléchargé à nouveau. La suppression permanente facultative utilise la case de nettoyage distincte, revérifie les candidats conservés dans la limite sélectionnée et consigne chaque résultat. La suppression contourne la Corbeille. Les cibles, l'âge et la limite de fichiers sont aussi configurables avec la commande cleanup de la CLI.

### Profils

Examine les profils Windows à l'aide du seuil d'âge sélectionné, des inscriptions ProfileList exactes et, éventuellement, des dépendances de comptes de services et de tâches planifiées, puis renvoie une seule conclusion d'examen délimitée. Les profils chargés, les profils système et celui du compte actuel sont exclus de l'examen. Les candidats restent en examen seul. Aucun profil, dossier, clé de registre, compte, service, tâche, identifiant, stratégie ou fichier n'est modifié, et aucune suppression n'est sélectionnée. Les dépendances des applications et la possibilité de restaurer les sauvegardes restent non vérifiées.

### Tâches planifiées

Inventorie les tâches planifiées visibles, leurs états et les principaux de sécurité indiqués. Aucune tâche n'est démarrée, inscrite ou modifiée. Les actions, les déclencheurs et la réussite de l'exécution ne sont pas vérifiés.

### Désencombrer Windows

Examine des domaines de nettoyage explicites de Windows comme Recall et l'IA, la télémétrie, les suggestions de recherche, le contenu grand public, la barre des tâches et Démarrer, les notifications, Edge, l'alimentation, la mise en veille prolongée, les langues d'Office et les logiciels préinstallés, pour le compte en cours d'exécution ou pour tous les profils. L'examen est en lecture seule. Le mode d'alimentation hautes performances, les minuteurs de veille et d'écran, ainsi que la mise en veille prolongée peuvent chacun être modifiés lorsque ce domaine est le seul sélectionné, avec des preuves récentes et une confirmation explicite.
