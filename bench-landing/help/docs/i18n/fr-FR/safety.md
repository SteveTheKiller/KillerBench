# Comment les modifications restent sûres

KillerBench traite le diagnostic et la modification comme deux tâches distinctes. Le diagnostic ne fait que lire. L'analyse complète du système n'exécute jamais de réparation. Sélectionnez une réparation ou une action précise, examinez ses options et ses prérequis, puis confirmez la modification proposée.

## Avant une modification

1. **Des preuves récentes.** L'outil relit l'état actuel. Un rapport enregistré ou un ancien événement n'autorise jamais une modification.
2. **Une éligibilité exacte.** La modification n'est proposée que si les preuves correspondent au problème exact qu'elle corrige. Tout élément incomplet la bloque.
3. **Votre confirmation.** Examinez l'action sélectionnée dans sa boîte de confirmation. Certaines actions, comme le déchiffrement d'un lecteur, demandent une seconde confirmation. La boîte suit le thème actif de l'application ; son apparence ne change pas les exigences de l'action.
4. **Les prérequis.** Les droits d'administrateur, l'état de redémarrage et le contrôle par une stratégie ou par la gestion des appareils sont vérifiés. Les paramètres contrôlés par une stratégie ne sont pas touchés.
5. **Un enregistrement en attente.** KillerBench consigne ce qu'il s'apprête à faire avant de le demander à Windows.
6. **Une dernière vérification.** Tout est revérifié juste avant la demande, si bien qu'un changement entre-temps l'arrête.

## Après une modification

- KillerBench relit le résultat et n'indique **Vérifié** que lorsque Windows affiche l'état attendu.
- Si Windows signale une réussite mais que l'état n'a pas changé, le résultat le dit et rien n'est répété.
- Chaque modification est enregistrée dans un journal sous `%LOCALAPPDATA%\KillerBench`.

## En cas d'interruption

Les actions journalisées prises en charge conservent un enregistrement en attente lorsqu'une demande a pu atteindre Windows sans que la vérification soit terminée. Utilisez le chemin de rapprochement documenté de l'action, ou sa commande `-recover` correspondante, pour inspecter cet enregistrement. Le rapprochement n'est pas une restauration automatique. Conservez le journal quand le résultat est incertain et ne répétez pas la modification à la main. Le comportement en cas de plantage, d'annulation ou de démarrage ultérieur doit encore faire l'objet d'une validation native propre à chaque action.

## Ce que KillerBench ne fera pas

- Effacer un TPM, supprimer des protecteurs qu'il n'a pas créés ou afficher des mots de passe de récupération.
- Modifier un paramètre contrôlé par une stratégie de groupe ou par la gestion des appareils.
- Exécuter une réparation simplement parce qu'un événement historique ou un rapport importé mentionne un problème.

Les diagnostics et l'analyse des rapports s'exécutent en local. Une action que vous sélectionnez peut contacter une destination externe, comme un téléchargement de logiciel ou la sauvegarde de la clé de récupération dans Active Directory ou Entra ID. Vérifiez la cible avant de confirmer. Les captures enregistrées et les preuves brutes peuvent contenir l'identité de l'ordinateur et des chemins sources ; examinez-les avant de les partager.

## État du développement

La couverture des réparations continue de s'étendre. Une compilation réussie ou une vérification simulée ne prouve pas le comportement natif d'une réparation. Le fichier `VM-VALIDATION.md` du dépôt suit les scénarios sur des machines Windows jetables et la validation restante.
