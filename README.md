# Radius UI - Colonist Bar Plus

Mod RimWorld 1.6 : fork de **Radius UI - Colonist Bar** (astryl) avec des ordres de combat d'escouade. Il remplace Colonist Bar : désactivez-le. Incompatible avec [LTO] Colony Groups. Multijoueur non pris en charge.

- packageId : `mrbabilo.RadiusUI.ColonistBarPlus`
- Dépendances : Harmony et Radius UI Framework

Pour rester compatible avec les parties existantes, le fork garde le namespace `RadiusColonistBar`, la DLL `RadiusColonistBar.dll`, l'identifiant Harmony `astryl.radiusui.colonistbar` et les noms de classes du mod d'origine.

## Ordres d'escouade

Un clic droit sur l'onglet d'une escouade ouvre sa carte d'ordres ; un second clic droit, un clic en dehors ou Échap la ferme. Depuis le module Escouades du Centre de commandement, la carte s'ouvre près de la souris et ajoute Renommer, Monter, Descendre et Dissoudre. La carte montre l'état de l'escouade (membres présents, mobilisation, tir à volonté, assaut) et reste ouverte après un ordre :

- **Mobiliser / Démobiliser l'escouade** ;
- **Tir à volonté / Cesser le feu** ;
- **Regroupement** : tout le monde court vers le chef (le premier membre dans l'ordre de la barre) ;
- **Assaut** : l'escouade attaque d'elle-même, en visant d'abord les ennemis les plus forts, les plus faibles, ou ceux qui fuient. Elle se met à couvert comme en vanilla et attaque n'importe quel ennemi à portée quand aucun ne correspond. L'assaut s'arrête quand on démobilise un membre, qu'on lui donne un ordre, ou qu'on coupe le tir à volonté (pour toute l'escouade ou avec le bouton d'un seul pion). L'attaque en cours s'interrompt aussitôt.
  Le mode d'assaut en cours est surligné : un clic dessus arrête l'assaut, les membres restent mobilisés.

Les ordres ne touchent que les membres présents sur la carte affichée. Les mécanoïdes, les pions à terre, en crise de nerfs ou en repos mortel sont ignorés.

Sans ouvrir la carte :

- l'onglet d'une escouade porte un point quand des membres sont mobilisés, et une épée pendant un assaut ;
- le portrait d'un pion qui suit un assaut porte une épée dans le coin bas droit ;
- six raccourcis clavier (mobiliser, tir à volonté, regrouper, trois modes d'assaut) se règlent dans Options > Commandes > « Radius UI - Escouades ». Ils n'ont pas de touche par défaut et visent l'escouade de chaque pion sélectionné.

Les onglets d'escouade n'apparaissent que si le réglage des onglets de groupes est actif et qu'il y a au moins deux onglets.

## Sauvegardes

Le mod ajoute un composant de partie (`SquadAssault`) qui retient le mode d'assaut de chaque pion. Le retirer en cours de partie donne une erreur au chargement ; seuls les assauts en cours sont perdus.

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
