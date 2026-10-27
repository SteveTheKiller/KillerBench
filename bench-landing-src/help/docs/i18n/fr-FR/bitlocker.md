# BitLocker

BitLocker inspecte le chiffrement des lecteurs, les protecteurs et les informations de récupération, puis vous aide à sauvegarder, protéger ou modifier un lecteur. Il agit uniquement sur cet ordinateur.

Guides pas à pas : [Chiffrer un lecteur avec BitLocker](encrypt-with-bitlocker.html).

## Utiliser l'outil BitLocker

Choisissez la lettre du lecteur et une **Opération**, puis appuyez sur **Exécuter le diagnostic**.

- **Inspecter** lit l'état du chiffrement, l'état de la protection et les identités des protecteurs. C'est le choix par défaut et il ne modifie rien.
- **Sauvegarder la récupération** écrit le mot de passe de récupération sélectionné dans un nouveau fichier hors du lecteur protégé et le vérifie. Appuyez d'abord sur **Trouver les protecteurs de récupération**, choisissez le protecteur, puis confirmez la sauvegarde.
- **Chiffrer** sauvegarde le mot de passe de récupération sélectionné, vérifie la sauvegarde, puis lance une seule fois le chiffrement avec la méthode choisie. Le lecteur Windows a besoin d'un protecteur TPM et d'un mot de passe de récupération. Un lecteur de données n'a besoin que d'un mot de passe de récupération.
- **Reprendre la protection** réactive la protection sur un lecteur entièrement chiffré où elle avait été suspendue.

Chacune de ces opérations a sa propre case de confirmation, et KillerBench revérifie le lecteur juste avant d'agir.

## Autres actions BitLocker

Ouvrez **Autres actions BitLocker** pour les modifications situées hors de **Exécuter le diagnostic**. Choisissez une action, appuyez sur **Aperçu** pour lire exactement ce qu'elle exige, puis confirmez et appuyez sur **Exécuter l'action**.

- **Activer le déverrouillage automatique** fait se déverrouiller un lecteur de données chiffré au démarrage de Windows. Le lecteur Windows doit être entièrement chiffré avec la protection activée.
- **Ajouter un protecteur TPM** ou **Ajouter un mot de passe de récupération** crée un nouveau protecteur. KillerBench n'affiche ni ne journalise jamais un nouveau mot de passe de récupération ; sauvegardez-le donc immédiatement.
- **Sauvegarder le mot de passe de récupération dans Active Directory** ou **Microsoft Entra ID** envoie le protecteur sélectionné ci-dessus. KillerBench signale que Windows a accepté la demande. Il ne peut pas lire la copie de l'annuaire ; confirmez-la donc là-bas.
- **Déchiffrer ce lecteur** lance le déchiffrement, par exemple avant de chiffrer à nouveau avec une méthode imposée. Une seconde confirmation est nécessaire, car le lecteur n'est plus protégé ensuite, et l'action est refusée sur le lecteur Windows tant que d'autres lecteurs se déverrouillent automatiquement grâce à lui.

## TPM et FIPS

Si le TPM est présent mais pas prêt, la section Sécurité peut le préparer sans l'effacer. KillerBench n'efface jamais un TPM, car cela détruit les clés détenues par BitLocker et Windows Hello. Le basculement de la stratégie d'algorithmes FIPS est disponible sous forme de réparation explicite et réversible pour les organisations qui ont besoin de codes PIN BitLocker approuvés FIPS.

## Outils de cette section

Inspectez le chiffrement, les protecteurs et les informations de récupération, puis sauvegardez-les ou protégez le lecteur.

### BitLocker

Prévisualise les options BitLocker et inspecte le volume sélectionné. Ce flux de travail n'effectue aucun chiffrement ni aucune sauvegarde de récupération.
