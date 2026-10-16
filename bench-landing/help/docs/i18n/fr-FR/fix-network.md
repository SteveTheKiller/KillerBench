# Résoudre les problèmes de réseau et d'internet

Utilisez ce guide lorsqu'un ordinateur n'a pas internet, ne parvient pas à joindre un serveur ou un nom, ou perd sans cesse sa connexion. KillerBench vérifie dans l'ordre la carte réseau, l'adresse, la passerelle, le DNS et le pare-feu, et ne renouvelle un bail ou ne démarre un service que lorsque des preuves récentes démontrent que c'est nécessaire.

## Vérifier la connexion

1. Ouvrez **Réseau** et choisissez **Réseau**.
2. Pour tester quelque chose de précis, remplissez **Recherche de nom d'hôte (facultatif)**, ou **Destination TCP (facultatif)** et **Port TCP**. Laissez-les vides pour ne vérifier que la configuration locale. Aucune donnée d'application n'est envoyée.
3. Réglez **Délai par recherche ou connexion (secondes)** si le réseau est lent.
4. Laissez **Réparer automatiquement les problèmes confirmés** désactivé et appuyez sur **Exécuter le diagnostic**.
5. Lisez le rapport. Il distingue une adresse manquante, une passerelle manquante ou injoignable, des problèmes de configuration DNS, les paramètres de proxy, les incohérences de profil du pare-feu et une recherche ou une connexion échouée, et indique l'étape suivante pour chaque cas.

Une réponse de la passerelle ne prouve pas l'accès à internet, et une recherche qui échoue via votre résolveur configuré ne signifie pas que le site est hors service.

## Réparer ce que le rapport a trouvé

1. Relancez **Réseau** avec **Réparer automatiquement les problèmes confirmés** activé. Il peut démarrer les services activés Client DNS, Client DHCP, client SMB, Connaissance des emplacements réseau, Service Liste des réseaux et Assistance IP, et renouveler les cartes DHCP physiques connectées qui n'ont pas d'adresse IPv4 utilisable.
2. Attendez-vous à une brève coupure de la connexion pendant un renouvellement. Les adresses statiques et les cartes virtuelles ne sont jamais modifiées.
3. Pour un seul nom en cache incorrect, utilisez le vidage du cache DNS pour cet hôte exact au lieu de tout effacer.

Les itinéraires, les entrées Hosts, les serveurs DNS, les règles du pare-feu, les fournisseurs Winsock et les profils réseau sont toujours conservés.

## Sans fil, authentification filaire et proxys

- **Réseau sans fil** vérifie la carte et le service de configuration automatique WLAN, et peut démarrer le service s'il est arrêté.
- **Authentification filaire (802.1X)** fait de même pour le service de configuration automatique de réseau câblé sur les réseaux qui exigent une authentification au port.
- **Proxy automatique WinHTTP** affiche les paramètres de proxy enregistrés pour l'ordinateur et le compte actuel. KillerBench ne récupère jamais d'URL PAC et ne modifie pas les paramètres de proxy pendant le diagnostic.

## Depuis la ligne de commande

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

Voir [workflow](cli.html#cmd-workflow), [dns-check](cli.html#cmd-dns-check), [tcp-check](cli.html#cmd-tcp-check), [dhcp-renew](cli.html#cmd-dhcp-renew) et [dns-cache](cli.html#cmd-dns-cache).

## Voir aussi

- [Section Réseau](network.html)
- [Résoudre les problèmes d'impression](fix-printing.html)
- [Comment les modifications restent sûres](safety.html)
