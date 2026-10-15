# Colonist Bar Plus — carte d'ordres d'escouade et aides en partie

Date : 2026-10-15. Suite de `2026-10-14-ordres-de-combat-design.md` : les ordres existent, cette spec change leur présentation et ajoute des aides. Aucune ligne de [LTO] Colony Groups n'est reprise (pas de licence).

## Objectif

Donner aux ordres d'escouade l'habillage de la suite Radius et les rendre plus rapides à utiliser en combat. Aujourd'hui ils passent par le menu déroulant vanilla (`SquadOrderMenu`, `RcbFloatMenu`), avec l'assaut dans un sous-menu.

Critères de réussite :

- un clic droit sur l'onglet d'une escouade ouvre une carte Radius qui montre l'état de l'escouade et tous les ordres sans sous-menu ;
- l'état d'une escouade (mobilisée, en assaut) se lit sur son onglet sans ouvrir la carte ;
- on voit sur la barre quels pions suivent encore un assaut ;
- les ordres sont accessibles au clavier pour le joueur qui le souhaite.

## 1. Carte d'ordres (`SquadOrderCard`)

### Fenêtre

- Classe `SquadOrderCard : Window`, sans fond ni ombre vanilla (`doWindowBackground = false`, `drawShadow = false`), dessinée avec le framework : `CardChrome` (carte arrondie, survol), `Palette`, `RadiusFont`, `IconSet`.
- Une seule carte ouverte à la fois : en ouvrir une ferme la précédente.
- Fermeture : clic en dehors (`closeOnClickedOutside`), Échap, ou nouveau clic droit sur l'onglet de la même escouade.
- Position : ancrée sous l'onglet (bord gauche aligné sur l'onglet), gardée dans l'écran. Ouverte depuis le Centre de commandement : près de la souris.
- La carte reste ouverte après un ordre et relit l'état à chaque image.
- Si l'escouade disparaît (dissoute, index hors limites), la carte se ferme.

### Contenu

1. **En-tête** : pastille à la couleur de l'escouade, nom, « {présents}/{total} sur la carte ». Présents = `SquadMembers.Able(Find.CurrentMap, index)` ; total = membres de l'escouade, toutes cartes confondues, hors morts.
2. **Ligne d'état** (texte atténué) :
   - mobilisation : « Mobilisée » (tous), « Mobilisée en partie », « Démobilisée » (aucun) ;
   - tir : « Tir à volonté » (tous), « Tir à volonté en partie », « Feu cessé » ;
   - assaut : le mode commun s'il y en a un (« Assaut : plus forts »), « Assaut en partie » si certains seulement, rien sinon.
3. **Ordres** : boutons à icône et libellé, du style des actions rapides de la fiche de survol, deux par ligne.
   - Mobiliser / Démobiliser (`Action/Draft`) : démobilise si tous sont mobilisés, sinon mobilise (règle actuelle de `SquadOrderMenu`).
   - Tir à volonté / Cesser le feu (`Alert/Threat`) : `SquadOrders.ToggleFireAtWill`.
   - Regrouper (`Action/DefPos`) : `SquadOrders.Regroup`.
4. **Assaut** : titre « ASSAUT », puis trois boutons côte à côte : Plus forts, Plus faibles, Fuyards (`Common/Sword` sur le premier ou sur les trois). Le mode commun actif est rempli à la couleur d'accent. Clic sur un mode inactif : `SquadOrders.Assault(index, mode)`. Clic sur le mode actif : arrête l'assaut des membres présents (nouvelle méthode `SquadOrders.StopAssault(index)`, qui appelle `SquadAssault.End` pour chacun ; les pions restent mobilisés).
5. **Pied (Centre de commandement seulement)** : Renommer, Monter, Descendre, Dissoudre, mêmes actions que le menu actuel du module, avec le rafraîchissement du module (`ctx.requestRefresh`).

Chaque bouton garde l'infobulle actuelle (`RCB_Orders_*Tip`). Aucun membre présent : boutons d'ordres et d'assaut grisés, ligne d'état remplacée par « Aucun membre de l'escouade sur cette carte » (`RCB_Orders_NoMember`).

### Intégration

- `RcbIsland` : le clic droit sur un onglet d'escouade ouvre la carte (`SquadOrderCard.Open(index, tabRect)`) au lieu de `SquadOrderMenu.Open`. Clic droit sur l'onglet d'une carte déjà ouverte pour cette escouade : la ferme.
- `Module_Squads.SquadMenu` : ouvre la carte en mode Centre de commandement au lieu du menu déroulant.
- `SquadOrderMenu` est supprimé. `RcbFloatMenu` et `FloatMenuOptionSub` restent : le menu clic droit d'un pion les utilise.

## 2. Aides en partie

### État sur l'onglet

Dans le dessin des onglets (`RcbIsland.DrawTab` ou la boucle qui remplit `tabRects`), pour les onglets d'escouade seulement :

- point à la couleur d'accent si au moins un membre présent est mobilisé ;
- petite épée (`Common/Sword`) si au moins un membre présent est en assaut.

Le calcul ne doit pas parcourir les pions à chaque image pour chaque onglet sans limite : un cache par escouade, recalculé au plus quelques fois par seconde (même principe que les caches existants de la barre).

### Icône sur le portrait

Chaque pion en assaut (`SquadAssault.TryGetMode`) porte une petite épée dans un coin de son portrait dans la barre. Passage par `BadgeLayer` si son modèle s'y prête, sinon par le dessin superposé du portrait (`Patch_DrawColonist_Overlay`). L'icône ne cache ni les badges existants ni l'état de mobilisation vanilla.

### Raccourcis clavier

- Six `KeyBindingDef` sans touche par défaut, catégorie nouvelle `RCB_Squads` (« Radius UI - Escouades ») : `RCB_SquadDraft`, `RCB_SquadFireAtWill`, `RCB_SquadRegroup`, `RCB_SquadAssaultStrongest`, `RCB_SquadAssaultWeakest`, `RCB_SquadAssaultFleeing`.
- Cible : l'escouade de chaque pion sélectionné (`BarSquads.squadOf`, clé `thingIDNumber`), chacune une seule fois. Aucun pion sélectionné n'appartient à une escouade : message « Aucune escouade sélectionnée » (`MessageTypeDefOf.RejectInput`).
- Mobiliser au clavier suit la règle de la carte : démobilise une escouade si tous ses membres présents sont mobilisés, sinon mobilise.
- Lecture des touches : là où la barre traite déjà ses entrées (même endroit que `RCB_FollowCam`, `FollowCam.cs`, par `KeyDownEvent`), seulement quand aucune fenêtre de saisie de texte n'a le focus.
- Le defName porte l'identité, le libellé est libre : ne jamais renommer un defName publié (perte des touches des joueurs).

## 3. Textes

Nouvelles clés en anglais dans `Languages/English/Keyed/RadiusColonistBar.xml` (bloc Colonist Bar Plus) et en français dans `Languages/French/Keyed/RadiusColonistBarPlus.xml` : en-tête, états, titre « Assaut », message sans escouade, libellés des raccourcis. Les libellés de `KeyBindingDef` et de `KeyBindingCategoryDef` en français passent par `Languages/French/DefInjected/`.

## 4. Hors périmètre

- Postes de combat, ordres médicaux, envoi au travail, règles par groupe (idées de Colony Groups) : spec à part si besoin.
- Changement des ordres eux-mêmes (logique de `SquadOrders`, `JobGiver_SquadAssault`) : seule `StopAssault` est ajoutée.

## 5. Vérification

Pas de tests automatisés dans le dépôt. Build sans erreur (avertissements : les 6 actuels), puis contrôle en jeu :

1. Clic droit sur un onglet d'escouade : carte sous l'onglet, état juste, chaque ordre agit et l'état se met à jour.
2. Mode d'assaut actif surligné ; clic dessus : assaut arrêté, pions toujours mobilisés.
3. Escouade sans membre sur la carte : boutons grisés et message.
4. Clic en dehors, Échap, second clic droit : la carte se ferme.
5. Module Escouades : la carte s'ouvre près de la souris, avec le pied de gestion ; renommer et dissoudre rafraîchissent le module.
6. Onglet : point quand mobilisée, épée pendant un assaut ; portrait : épée sur les pions en assaut seulement.
7. Raccourcis : assignés dans Options, ils agissent sur l'escouade des pions sélectionnés ; sans escouade, message.
8. Français et anglais : aucun texte en dur, aucun débordement.
