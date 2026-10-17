# Radius UI - Colonist Bar Plus

Mod RimWorld 1.6 : fork de **Radius UI - Colonist Bar** (astryl) avec des ordres de combat d'escouade. Il remplace Colonist Bar : désactivez-le. Incompatible avec [LTO] Colony Groups. Multijoueur non pris en charge.

- packageId : `mrbabilo.RadiusUI.ColonistBarPlus`
- Dépendances : Harmony et Radius UI Framework

Pour rester compatible avec les parties existantes, le fork garde le namespace `RadiusColonistBar`, la DLL `RadiusColonistBar.dll`, l'identifiant Harmony `astryl.radiusui.colonistbar` et les noms de classes du mod d'origine.

## Ordres d'escouade

Un clic droit sur l'onglet d'une escouade ouvre sa carte d'ordres, déplaçable à la souris ; son carré en haut à droite, un second clic droit, un clic en dehors ou Échap la ferme. Depuis le module Escouades du Centre de commandement, la carte s'ouvre près de la souris et ajoute Renommer, Monter, Descendre et Dissoudre. La carte montre l'état de l'escouade (membres présents, mobilisation, tir à volonté, assaut) et reste ouverte après un ordre :

- **Mobiliser / Démobiliser l'escouade** ;
- **Tir à volonté / Cesser le feu** ;
- **Regroupement** : tout le monde court vers le chef (le premier membre dans l'ordre de la barre) ;
- **Assaut** : l'escouade attaque d'elle-même, en visant d'abord les ennemis les plus forts, les plus faibles, ou ceux qui fuient. Elle se met à couvert comme en vanilla et attaque n'importe quel ennemi à portée quand aucun ne correspond. L'assaut s'arrête quand on démobilise un membre, qu'on lui donne un ordre, ou qu'on coupe le tir à volonté (pour toute l'escouade ou avec le bouton d'un seul pion). L'attaque en cours s'interrompt aussitôt.
  Le mode d'assaut en cours est surligné : un clic dessus arrête l'assaut, les membres restent mobilisés.

La section « Membres » de la carte gère l'escouade à partir de la sélection : **Ajouter la sélection** y met les colons et esclaves sélectionnés (un pion d'une autre escouade en change), **Retirer la sélection** en sort les membres sélectionnés, et **Sélectionner**, à côté de Regroupement, sélectionne les membres présents. Dans le module Escouades, « Nouvelle escouade » prend la sélection quand il y en a une. Dissoudre demande confirmation.

Mobiliser et Tir à volonté sont surlignés quand toute l'escouade est dans cet état, soulignés quand seule une partie l'est. Un bouton grisé dit pourquoi dans son infobulle, et les infobulles indiquent la touche des raccourcis qui en ont une.

Les ordres ne touchent que les membres présents sur la carte affichée. Les mécanoïdes, les pions à terre, en crise de nerfs ou en repos mortel sont ignorés.

Sans ouvrir la carte :

- l'onglet d'une escouade porte un point quand des membres sont mobilisés, et une épée pendant un assaut ;
- le portrait d'un pion qui suit un assaut porte une épée dans le coin bas droit ;
- six raccourcis clavier (mobiliser, tir à volonté, regrouper, trois modes d'assaut), plus ceux des postes de combat et des soins, se règlent dans Options > Commandes > « Radius UI - Escouades ». Ils n'ont pas de touche par défaut et visent l'escouade de chaque pion sélectionné.

L'infobulle d'un onglet d'escouade commence par la ligne d'état de la carte. Une carte déplacée à la souris se rouvre à la même place jusqu'à la fin de la session.

Les onglets d'escouade n'apparaissent que si le réglage des onglets de groupes est actif et qu'il y a au moins deux onglets.

## Postes de combat

La carte d'ordres a une section « Postes de combat » : quatre postes par escouade, dont un actif (pastille surlignée, un clic pour changer).

- **Enregistrer** : chaque membre présent sur la carte prend sa position actuelle comme poste actif. Les membres absents, à terre ou en crise gardent leur poste précédent. Pour retoucher un seul pion : le déplacer, puis réenregistrer.
- **Aller au poste** : l'escouade se mobilise, sort de l'assaut et chaque membre court à sa case. Une case devenue impraticable est remplacée par la case libre la plus proche. Un membre sans poste sur cette carte est mobilisé et reste sur place.
- **Effacer** : retire le poste actif de tous les membres de l'escouade, sur toutes les cartes, après confirmation.

Survoler une pastille ou « Aller au poste » montre sur la carte la case de chaque membre. Un poste appartient au pion et ne vaut que pour la carte où il a été enregistré : un pion qui change d'escouade garde ses postes. Un septième raccourci, « aller au poste de combat actif », se règle avec les autres.

## Soins

La section « Soins » de la carte d'ordres relève et soigne en un clic. Les patients sont les colons et esclaves de la carte affichée, membres de l'escouade ou non : d'abord ceux en hémorragie mortelle, puis les membres, puis les autres. Chaque patient reçoit le membre libre le plus proche, un patient par membre ; un membre qui secourt ou soigne déjà n'est pas réaffecté.

- **Secourir** : porte les pions à terre dans un lit, médical d'abord (lit d'esclave pour un esclave). Les bébés n'en font pas partie : le jeu les confie aux soins aux enfants.
- **Soigner** : les membres qui savent soigner s'occupent des blessés, avec le meilleur médicament que la règle de soins du patient autorise. Un patient debout attend son soigneur.
- **Au lit** : les membres blessés sont démobilisés et se reposent dans un lit médical jusqu'à guérison.

Chaque bouton indique le nombre de pions concernés ; son infobulle les nomme avec leur état, et son survol les entoure sur la carte. Secourir et Soigner restent soulignés tant que des membres secourent ou soignent ; la ligne d'état de la carte compte les membres à terre et à soigner. Trois raccourcis de plus, sans touche par défaut : secourir, soigner, envoyer au lit. Rien n'est ajouté à la sauvegarde : ce sont des ordres vanilla.

## Fiche de survol

La fiche d'un pion s'ouvre après un court délai de survol de son portrait : 0,25 s par défaut, de 0 à 1 s dans l'onglet Panneau de survol des réglages. Passer d'un portrait au voisin la change sans attendre. Une ligne en bas de la fiche rappelle les clics droits du portrait.

## Sauvegardes

Le mod ajoute un composant de partie (`SquadAssault`) qui retient le mode d'assaut de chaque pion. Le retirer en cours de partie donne une erreur au chargement ; seuls les assauts en cours et les postes de combat sont perdus.

Les postes de combat sont sauvegardés avec les escouades. Une partie rouverte avec le Colonist Bar d'origine les perd sans autre erreur.

## Traduction

Le français de ce fork ne contient que les textes qu'il ajoute (`Languages/French/Keyed/RadiusColonistBarPlus.xml`). Les textes d'origine de Colonist Bar sont traduits par le mod « Radius UI - Traduction française ».

Certains textes du mod d'origine sont écrits en dur dans son code, hors de portée d'un mod de traduction. Le fork les passe par des clés : l'infobulle des sous-menus et les boutons de la carte de survol d'un pion (Mobiliser, Centrer, Santé, Bio, Social, Matériel, Renommer, Règles, Défendre). Les boutons reprennent les traductions du jeu quand elles existent.

## Crédits

| Composant | Auteur | Source | Licence |
|---|---|---|---|
| Radius UI - Colonist Bar (base du mod) | astryl | [Steam Workshop 3788278207](https://steamcommunity.com/sharedfiles/filedetails/?id=3788278207) | MIT |
| [LTO] Colony Groups (idée des ordres de groupe, comportement de référence ; code non repris) | DerekBickley | [Steam Workshop 2345493945](https://steamcommunity.com/sharedfiles/filedetails/?id=2345493945) | non indiquée |

## Compiler

`deploy.sh` compile hors de l'arbre, copie la DLL dans `Assemblies/` et publie le mod dans le dossier `Mods` du jeu (chemins en tête du script).

## Licence

MIT, voir `LICENSE`. La notice d'astryl est conservée.
