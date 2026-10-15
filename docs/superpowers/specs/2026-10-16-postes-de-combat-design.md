# Colonist Bar Plus — postes de combat d'escouade

Date : 2026-10-16. Lot 2 de `docs/ROADMAP.md`. Suite de `2026-10-14-ordres-de-combat-design.md` et `2026-10-15-carte-ordres-design.md`. Comportement de référence : `Formation`, `BattleStationsMenu` et `TacticUtils.SetBattleStations` de [LTO] Colony Groups. Aucune ligne n'en est reprise (pas de licence).

## Objectif

Préparer la défense d'une colonie : le joueur place une fois chaque membre d'une escouade à son poste, l'enregistre, puis rappelle toute l'escouade à ces positions en un clic quand un raid arrive.

Critères de réussite :

- un poste s'enregistre depuis la position actuelle des membres, sans outil de ciblage ;
- « Aller au poste » mobilise l'escouade et envoie chaque membre à sa case ;
- les postes survivent à la sauvegarde et ne valent que pour la carte où ils ont été enregistrés ;
- le joueur voit sur la carte où ira chaque membre avant de donner l'ordre.

Hors périmètre de ce lot : la capture des ennemis à terre (reportée, voir § 6).

## 1. Comportement

### Postes et poste actif

- Chaque pion a quatre emplacements de poste (1 à 4). Un poste = une carte (`Map.uniqueID`) + une case (`IntVec3`).
- Les postes appartiennent au **pion**, pas à l'escouade. Dissoudre ou réordonner une escouade ne décale aucun poste. Un pion qui change d'escouade garde ses postes.
- Chaque escouade a un **poste actif** (1 à 4, 1 par défaut), sauvegardé. Les trois actions ci-dessous portent sur l'emplacement du poste actif.
- « Membres aptes » = `SquadMembers.Able(Find.CurrentMap, index)` : mêmes exclusions que les autres ordres (mécanoïdes, pions à terre, en crise, en repos mortel, prisonniers).

### Enregistrer

- Chaque membre apte prend sa case actuelle comme poste actif, sur la carte affichée. Un poste déjà enregistré à cet emplacement est remplacé, y compris s'il était sur une autre carte.
- Les membres non aptes (absents, à terre, en crise) gardent leur poste précédent.
- La sélection n'a aucun effet : tous les membres aptes sont pris. Pour retoucher un seul pion : le déplacer, puis réenregistrer ; les autres n'ont pas bougé, leur poste reste le même.
- Message `MessageTypeDefOf.SilentInput` : « Poste {N} enregistré : {nombre} pions ».
- Bouton grisé sans membre apte.

### Aller au poste

- Chaque membre apte :
  1. est mobilisé ;
  2. sort de l'assaut (`SquadAssault.Get?.End(pawn)`, comme `SquadOrders.Regroup`) ;
  3. s'il a un poste à cet emplacement **sur la carte affichée** : reçoit un job `Goto` en sprint (`LocomotionUrgency.Sprint`) vers sa case, par `TryTakeOrderedJob`. Si la case n'est plus praticable (`!cell.Standable(map)`, mur construit), destination = `RCellFinder.BestOrderedGotoDestNear(cell, pawn)` ; destination invalide : le pion reste sur place.
- Un membre sans poste sur cette carte est mobilisé et reste sur place.
- Chaque pion passe par `SquadOrders.Try` : l'échec d'un pion n'arrête pas les autres.
- Bouton grisé si aucun membre apte n'a de poste à cet emplacement sur la carte affichée.

### Effacer

- Retire le poste actif de **tous** les membres de l'escouade (présents ou non, toutes cartes).
- Action sans retour : elle passe par une confirmation (`Dialog_MessageBox.CreateConfirmation`, « Effacer le poste {N} de l'escouade {nom} ? », bouton destructif). La carte se ferme avant d'ouvrir la confirmation, comme pour Renommer : sur la couche `Super`, elle couvrirait la boîte et capterait ses clics.
- Message `SilentInput` après confirmation : « Poste {N} effacé ».
- Bouton grisé si aucun membre de l'escouade n'a de poste à cet emplacement.

## 2. Données et sauvegardes

Les données vont dans `BarSquads`, classe déjà sauvegardée et déjà propre au fork. Pas de nouveau composant.

### Poste actif

- Champ `activeStation` : `List<int>` parallèle à `names`, valeurs 0 à 3. Clé de sauvegarde `mcbActiveStation`.
- `PostLoadInit` : liste nulle remplacée, complétée par des 0 jusqu'à `names.Count` (comme `colors` et `icons`), valeurs hors 0-3 ramenées à 0.
- `NewSquad` ajoute 0. `Disband(i)` retire l'entrée `i`. `MoveSquad(i, dir)` échange les deux entrées. Même endroit que le traitement de `colors` et `icons`.

### Postes

- En mémoire : `Dictionary<int, Station>`, clé = `thingIDNumber * 4 + emplacement` (emplacement 0-3). `Station` : structure privée `mapId` (`int`) + `cell` (`IntVec3`).
- Sauvegarde : quatre listes plates, clés `mcbStationPawn`, `mcbStationSlot`, `mcbStationMap`, `mcbStationCell` (`LookMode.Value`), construites au `Saving`, relues au `LoadingVars` (même schéma que `hiddenScratch`).
- Chargement : listes absentes, nulles ou de longueurs différentes : postes vides, sans exception ; emplacement hors 0-3 : entrée ignorée.
- Accesseurs : `bool StationOf(Pawn, int slot, out int mapId, out IntVec3 cell)`, `void SetStation(Pawn, int slot, Map, IntVec3)`, `void ClearStation(int pawnId, int slot)`. « Effacer » et le comptage toutes cartes parcourent les identifiants de `squadOf` qui pointent vers l'escouade : aucun `Pawn` n'est nécessaire pour les membres absents.

### Nettoyage

À la sauvegarde seulement, une entrée est retirée si sa carte n'existe plus (aucune carte de `Find.Maps` n'a ce `uniqueID`). Aucune autre règle.

Pourquoi pas de règle sur le pion : un test « pion vivant du joueur » (`PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction`) ne voit pas les pions hors carte (mission de quête, enlèvement, vol du vaisseau) et effacerait leurs postes à la sauvegarde. Contrepartie acceptée : les postes d'un pion mort restent, au plus quatre petites entrées par pion.

### Compatibilité

- Partie faite avec Plus, rouverte avec Colonist Bar d'origine : nœuds inconnus ignorés par `Scribe`, postes perdus, aucune erreur **nouvelle** (l'absence de `SquadAssault` en donne déjà une).
- Partie ancienne ouverte avec Plus : listes absentes, aucun poste, poste actif 1.
- Retrait du mod : aucune erreur de plus qu'aujourd'hui (`BarSquads` et `SquadAssault` donnent déjà la leur).
- Aucun `Lord`, `DutyDef` ni `MapComponent` : un `MapComponent` écrirait une entrée dans chaque carte sauvegardée.

## 3. Interface

### Carte d'ordres (`SquadOrderCard`)

Nouvelle section entre la ligne Regroupement et la section Assaut :

1. Titre micro « POSTES DE COMBAT » (même style que « ASSAUT »).
2. Ligne de quatre pastilles numérotées (hauteur 24 px). Clic = cette pastille devient le poste actif de l'escouade. Pastille active : couleur d'accent. Pastille pleine si au moins un membre apte a ce poste sur la carte affichée ; sinon vide (contour atténué). Infobulle : « Poste {N} : {nombre} pions enregistrés sur cette carte ».
3. Ligne de trois boutons, mêmes dimensions que les autres ordres : Aller au poste, Enregistrer, Effacer. Infobulles décrivant l'action, avec le numéro du poste actif.

`ContentHeight` grandit de la hauteur de cette section (espacement, titre 18, pastilles 24, écart, boutons `BtnH`).

Coût par image : la carte appelle `SquadMembers.Able` **une fois** par image pour la section et passe la liste aux pastilles, aux boutons et au survol. `Able` trie et relit la barre : l'appeler par pastille le ferait cinq fois par image.

### Aperçu sur la carte

- Survol d'une pastille : postes de **cet** emplacement. Survol d'« Aller au poste » : postes du poste actif.
- Pour chaque membre apte qui a un poste à cet emplacement sur la carte affichée : contour de la case (`GenDraw.DrawFieldEdges`, couleur de l'escouade) et ligne du pion à la case (`GenDraw.DrawLineBetween`).
- Transmission : la carte, dans son `OnGUI`, inscrit escouade, emplacement et `Time.frameCount` dans un état statique de `SquadStations`. `BarSquads.GameComponentUpdate` appelle `SquadStations.DrawHover()`, qui dessine seulement si l'inscription date de la frame en cours ou de la précédente. Rien n'est dessiné sans survol.

### Raccourci clavier

- `KeyBindingDef` `RCB_SquadGoToStation`, catégorie `RCB_Squads`, sans touche par défaut : « Aller au poste actif ».
- Même règle que les six raccourcis existants (`SquadHotkeys`) : vise l'escouade de chaque pion sélectionné, une fois par escouade.

## 4. Code

| Fichier | Rôle |
|---|---|
| `BarSquads.cs` | Données § 2, accesseurs, `GameComponentUpdate`. |
| `SquadStations.cs` (nouveau) | Classe statique, sur le modèle de `SquadOrders` : `Save(index)`, `Go(index)`, `Clear(index)`, `CountOnMap(index, slot, map)`, `CountAny(index, slot)`, état du survol, `DrawHover()`. |
| `SquadOrderCard.cs` | Section « Postes de combat » § 3. |
| `SquadHotkeys.cs`, `Defs/KeyBindings_Squads.xml` | Septième raccourci. |
| `SquadStatus.cs` | `Invalidate` après chaque action, comme les autres ordres. |

Commentaires en tête des nouveaux blocs, dans le style des fichiers d'ordres existants (anglais, une phrase sur le pourquoi).

## 5. Textes

- Anglais : `Languages/English/Keyed/RadiusColonistBar.xml`. Français : `Languages/French/Keyed/RadiusColonistBarPlus.xml`. Libellé et description du raccourci en français : `Languages/French/DefInjected/KeyBindingDef/`.
- Clés préfixées `RCB_Stations_` : titre, Aller, Enregistrer, Effacer, infobulles, messages, infobulle des pastilles.
- Documentation : section « Postes de combat » dans `README.md` ; `CHANGELOG.md` « 1.1.0 — non publiée » ; `docs/ROADMAP.md` (lot 2 fait, capture des ennemis à terre reportée). Description de `About.xml` et `docs/WORKSHOP.md` complétées à la publication.

## 6. Hors périmètre

- Capture des ennemis à terre : autre mécanique (lit de prisonnier, choix de l'ennemi, porteur). Reste dans la roadmap, sans lot.
- Enregistrement par ciblage sur la carte, case par case.
- Enregistrement limité aux pions sélectionnés.
- Affichage permanent des postes.
- Effacer depuis le raccourci clavier : pas de raccourci pour une action sans retour.
- Postes relatifs au vaisseau (Odyssey) : un poste est lié à l'identifiant de sa carte. Si un déplacement du vaisseau gravitationnel crée une nouvelle carte, les postes enregistrés sur l'ancienne ne s'appliquent plus (à confirmer en jeu, sans correctif dans ce lot).

## 7. Vérification

Build sans nouvel avertissement (`deploy.sh`). Contrôles en jeu :

1. Enregistrer le poste 1, déplacer les pions, « Aller au poste » : chacun revient à sa case, mobilisé, en sprint ; le message donne le bon nombre de pions.
2. Pastille 2 sans poste : pastille vide, « Aller au poste » et « Effacer » grisés.
3. Survol d'une pastille et d'« Aller au poste » : contours et lignes visibles ; rien sans survol.
4. Assaut en cours, puis « Aller au poste » : assaut arrêté, épée retirée du portrait et de l'onglet.
5. Mur construit sur une case enregistrée : le pion s'arrête à côté.
6. Sauvegarder, recharger : postes et poste actif conservés.
7. Dissoudre une escouade placée au-dessus d'une autre : le poste actif de la seconde ne change pas.
8. Raccourci assigné : les pions sélectionnés vont au poste actif de leur escouade.
9. Partie sauvegardée avec Plus, ouverte avec Colonist Bar d'origine : seule l'erreur déjà connue de `SquadAssault`, aucune sur les postes.
10. « Effacer » : la carte se ferme, la confirmation s'ouvre ; Annuler garde les postes, Confirmer les retire.
