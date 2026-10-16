# Chiffrer un lecteur avec BitLocker

Utilisez ce guide pour activer BitLocker, sauvegarder la clé de récupération et confirmer que le lecteur est protégé. KillerBench sauvegarde et vérifie le mot de passe de récupération avant de lancer le chiffrement, et il n'affiche ni ne journalise jamais un mot de passe de récupération.

## Avant de commencer

- Exécutez KillerBench en tant qu'administrateur.
- Le lecteur Windows a besoin d'un protecteur TPM et d'un protecteur de mot de passe de récupération. Un lecteur de données n'a besoin que d'un mot de passe de récupération.
- Décidez où ira la sauvegarde de la clé de récupération : un fichier hors du lecteur que vous chiffrez et, éventuellement, Active Directory ou Microsoft Entra ID.

## Vérifier le lecteur

1. Ouvrez **BitLocker** et choisissez le lecteur dans **Volume**.
2. Laissez l'opération sur **Inspecter** et appuyez sur **Exécuter le diagnostic**. Le rapport affiche l'état du chiffrement, l'état de la protection et les identités des protecteurs.
3. Si le TPM est présent mais pas prêt, préparez-le d'abord depuis **Sécurité**. KillerBench n'efface jamais un TPM.

## Ajouter les protecteurs nécessaires

1. Ouvrez **Autres actions BitLocker**.
2. Choisissez **Ajouter un protecteur TPM** ou **Ajouter un mot de passe de récupération**, appuyez sur **Aperçu**, cochez **Appliquer cette modification au lecteur sélectionné quand j'appuie sur Exécuter l'action**, puis appuyez sur **Exécuter l'action**.
3. Sauvegardez immédiatement le nouveau mot de passe de récupération. KillerBench ne l'affiche pas.

## Sauvegarder la clé de récupération et chiffrer

1. Appuyez sur **Trouver les protecteurs de récupération** et sélectionnez le protecteur de mot de passe de récupération.
2. Saisissez un chemin complet pour le fichier de sauvegarde, hors du lecteur que vous chiffrez.
3. Choisissez **Chiffrer**, sélectionnez la méthode de chiffrement et cochez **Sauvegarder la clé de récupération sélectionnée, puis démarrer le chiffrement quand je sélectionne Exécuter le diagnostic**.
4. Appuyez sur **Exécuter le diagnostic**. KillerBench écrit la sauvegarde, la vérifie, puis lance le chiffrement une seule fois.
5. Le chiffrement se poursuit en arrière-plan. Exécutez **Inspecter** plus tard pour confirmer que le lecteur est entièrement chiffré avec la protection activée.

## Sauvegarder dans Active Directory ou Entra ID

Dans **Autres actions BitLocker**, choisissez la sauvegarde dans l'annuaire pour le protecteur sélectionné. KillerBench signale que Windows a accepté la demande, mais il ne peut pas lire la copie de l'annuaire ; confirmez-la donc dans Active Directory ou le centre d'administration Entra.

## Lecteurs de données et déverrouillage automatique

Une fois un lecteur de données chiffré, **Activer le déverrouillage automatique** le fait se déverrouiller au démarrage de Windows. Le lecteur Windows doit déjà être entièrement chiffré avec la protection activée.

## Depuis la ligne de commande

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

Voir [bitlocker-protectors](cli.html#cmd-bitlocker-protectors), [bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add), [bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) et [bitlocker-escrow](cli.html#cmd-bitlocker-escrow). Chacune a une commande `-recover` correspondante pour une demande interrompue.

## Voir aussi

- [Section BitLocker](bitlocker.html)
- [Section Sécurité](security.html)
- [Comment les modifications restent sûres](safety.html)
