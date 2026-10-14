# Radius UI - Colonist Bar Plus : ordres de combat d'escouade

Date : 2026-10-14. Statut : design validé en discussion, à relire avant le plan d'implémentation.

## Objectif

Créer « Radius UI - Colonist Bar Plus », un fork de Radius UI - Colonist Bar (astryl, MIT), sur le modèle de Radius UI - Inspector Plus. Ce premier lot y ajoute les ordres de combat de groupe de [LTO] Colony Groups (DerekBickley), réécrits au style Radius et appliqués aux escouades qui existent déjà dans Colonist Bar.

Colony Groups n'indique aucune licence : son code sert de référence de comportement et n'est pas recopié.

Hors de ce lot (lots suivants) : postes de combat, ordres médicaux, travail et règles par escouade, raccourcis clavier.

## Critères de réussite

- Un clic droit sur l'onglet d'une escouade ouvre un menu d'ordres : mobiliser ou démobiliser, tir à volonté, regroupement, assaut (plus fort, plus faible, fuyards).
- En assaut, l'escouade choisit ses cibles selon le mode, se met à couvert comme en vanilla et se rabat sur le ciblage vanilla quand aucune cible du mode n'est atteignable.
- Démobiliser un pion arrête son assaut et rétablit sa réponse aux hostiles.
- Une partie qui tournait avec Colonist Bar d'origine garde ses escouades.
- Retirer le mod en cours de partie ne laisse qu'une erreur au chargement, sans autre dégât.

## Fork

Dossier `RadiusUIColonistBarPlus/` (dépôt git propre, branche `main`), copie de `RadiusUIColonistBar/` avec :

- `About.xml` : nom « Radius UI - Colonist Bar Plus », packageId `mrbabilo.RadiusUI.ColonistBarPlus`, auteur mrbabilo, mêmes dépendances (Harmony, Radius UI Framework), `incompatibleWith` : `astryl.RadiusUI.ColonistBar`, `DerekBickley.LTOColonyGroupsFinal`.
- Gardés sans changement : namespace `RadiusColonistBar`, DLL `RadiusColonistBar.dll`, identifiant Harmony `astryl.radiusui.colonistbar`, noms des classes. Les sauvegardes référencent `BarSquads` (un `GameComponent`) par son nom de classe complet : le garder conserve les escouades d'une partie existante. Aucun mod de la suite Radius ne détecte Colonist Bar par son packageId (vérifié dans les sources décompilées), donc le changer ne casse rien dans la suite.
- Les réglages du mod sont stockés sous le nom du dossier : ils repartent des valeurs par défaut. Aucune incidence sur les sauvegardes.
- `deploy.sh` (même schéma que celui d'Inspector Plus : build hors de l'arbre, copie de la DLL, `rsync --delete` vers `Mods/RadiusUIColonistBarPlus`), `README.md`, `CHANGELOG.md`, `LICENSE` (notice MIT d'astryl conservée, mrbabilo ajouté), `.gitignore` (DLL ignorée).

## Composants ajoutés

Chacun dans son fichier, dans `Source/RadiusColonistBar/`.

### `SquadOrders`

Classe statique. Chaque méthode prend un index d'escouade (`BarSquads`) et agit sur les membres présents sur la carte affichée (`Find.CurrentMap`). Un membre couché, en crise mentale, prisonnier ou sans `drafter` est ignoré. Les ordres s'exécutent pion par pion : une exception est journalisée avec `Log.ErrorOnce` et les autres pions reçoivent l'ordre.

| Méthode | Effet |
|---|---|
| `Draft` / `Undraft` | `drafter.Drafted`. Démobiliser passe aussi par le patch du setter (voir plus bas), qui efface le mode d'assaut. |
| `ToggleFireAtWill` | Si tous les membres ont `FireAtWill`, le coupe ; sinon l'allume pour tous. |
| `Regroup` | Mobilise, efface les modes d'assaut, puis donne à chaque membre un job `Goto` vers le chef (premier membre de l'escouade présent sur la carte), `LocomotionUrgency.Sprint`, via `TryTakeOrderedJob`. |
| `Assault(mode)` | Mobilise, sélectionne l'escouade, enregistre chaque pion dans `SquadAssault` avec le mode et l'ancienne `hostilityResponse`, règle `hostilityResponse` sur `Attack`, puis termine le job en cours (`EndCurrentJob(JobCondition.InterruptForced)`) pour que l'arbre de pensée reprenne la main. |

Méthodes d'état pour le menu : nombre de membres présents, tous mobilisés ou non, tous en tir à volonté ou non.

### `SquadAssault` (`GameComponent`)

- Données : `Dictionary<Pawn, AssaultEntry>`, où `AssaultEntry` contient `mode` (`AssaultMode` : `Strongest`, `Weakest`, `Fleeing`) et `previousResponse` (`HostilityResponseMode`).
- API : `Set(pawn, mode)`, `TryGetMode(pawn, out mode)`, `Clear(pawn)` (rétablit `previousResponse` si le pion a encore ses `playerSettings`).
- Ménage : avant chaque sauvegarde et toutes les 250 ticks, retire les pions morts, détruits, couchés, non présents sur une carte ou non mobilisés (en rétablissant leur réponse aux hostiles quand c'est possible). Au chargement, retire les entrées dont le pion est nul (référence non résolue).
- Sauvegarde : `Scribe_Collections.Look(ref entries, "assault", LookMode.Reference, LookMode.Deep)`, avec les listes de travail habituelles. `AssaultEntry` implémente `IExposable`.

### `JobGiver_SquadAssault`

- Hérite de `JobGiver_AIFightEnemies` (vanilla) : position de tir à couvert, corps à corps, capacités. Contrairement à Colony Groups, l'escouade ne prend pas de drogues de combat d'elle-même.
- `targetAcquireRadius` 20, `targetKeepRadius` 23 (valeurs de Colony Groups), réglés dans le XML du nœud.
- Surcharge `ExtraTargetValidator(pawn, target)` : `base` et le filtre du mode du pion.
  - `Strongest` : `combatPower` du `kindDef` de la cible égal au maximum parmi les ennemis du pion sur la carte (hors brouillard, non couchés).
  - `Weakest` : même chose avec le minimum.
  - `Fleeing` : cible pion dont le job courant est `Flee` ou `FleeAndCower`.
- Surcharge `FindAttackTarget(pawn)` : appelle `base` avec le filtre ; si aucune cible, rappelle `base` sans filtre (repli vanilla).
- Le maximum et le minimum de `combatPower` sont calculés une fois par tick et par carte (cache `(map, tick) → (min, max)`), à partir de `map.attackTargetsCache.GetPotentialTargetsFor(pawn)`.

### `ThinkNode_ConditionalSquadAssault` et `Patches/SquadAssault.xml`

- Nœud conditionnel : satisfait si le pion est mobilisé et a un mode dans `SquadAssault`.
- Patch XML `PatchOperationInsert` dans `ThinkTreeDef[defName="Humanlike"]`, juste avant `li[@Class="ThinkNode_QueuedJob"]` (même point d'insertion que Colony Groups), contenant le nœud conditionnel et, dessous, `RadiusColonistBar.JobGiver_SquadAssault`.
- Pas de `DutyDef`, pas de `Lord`, pas de `LordJob`.

### Patch Harmony du setter `Pawn_DraftController.Drafted`

Postfix : quand la valeur passe à `false`, appelle `SquadAssault.Clear(pawn)`. Classe `[HarmonyPatch]` à part, patchée par le `HarmonyBootstrap` existant, qui saute une classe en échec avec un avertissement.

### Menu des ordres

- Déclencheur : clic droit sur un onglet d'escouade de la rangée d'onglets de la barre (`RcbIsland`, jetons d'escouade `RcbCohorts.TokenIsSquad`). Le clic droit ailleurs sur la rangée garde son comportement actuel : revenir à « Tous ».
- Fenêtre : `RcbFloatMenu`. Options : Mobiliser ou Démobiliser (selon l'état), Tir à volonté (marqué actif si tous l'ont), Regroupement, Assaut (sous-menu via `FloatMenuOptionSub` : Plus fort, Plus faible, Fuyards).
- Infobulle sur chaque option. Si aucun membre n'est sur la carte affichée, options désactivées avec la raison.
- Son : clic vanilla. Les sons de Colony Groups ne sont pas repris.

### Traductions

Nouvelles clés `RCB_Orders_*` dans `Languages/English/Keyed/RadiusColonistBar.xml` et dans un nouveau `Languages/French/Keyed/RadiusColonistBar.xml` (le mod d'origine n'a pas de français ; seules les nouvelles clés y sont traduites dans ce lot).

## Sauvegardes

| Donnée | Risque | Parade |
|---|---|---|
| `SquadAssault` (`GameComponent`) | Mod retiré : erreur « Could not find class » au chargement, bloc ignoré, modes d'assaut perdus. | Aucune donnée critique dedans. |
| Références de pions dans `SquadAssault` | Pion disparu : avertissement de référence non résolue. | Ménage avant sauvegarde, purge des entrées nulles au chargement. |
| Jobs en cours | Aucun : jobs vanilla (`AttackStatic`, `AttackMelee`, `Goto`). | — |
| Patch de l'arbre de pensée | Aucun : non sauvegardé. Si un autre mod a réécrit `Humanlike`, erreur au démarrage et assaut inactif. | Les pions restent sur le comportement vanilla. |
| `BarSquads` | Déjà présent dans Colonist Bar d'origine. | Noms de classes conservés. |
| `hostilityResponse` | Réglage vanilla du pion modifié pendant l'assaut. | Rétabli à la démobilisation et au ménage. |

## Vérification

Pas de tests automatisés dans ce dépôt. Chaque étape compile sans erreur ni nouvel avertissement. Contrôle en jeu, à faire par le joueur :

1. Player.log sans erreur `[Radius UI - Colonist Bar]` au démarrage, patch XML appliqué.
2. Chaque ordre essayé sur une escouade pendant un raid ; repli vanilla quand le mode n'a pas de cible.
3. Réponse aux hostiles rétablie après démobilisation.
4. Sauvegarde puis rechargement pendant un assaut : l'assaut reprend.
5. Partie chargée après retrait du mod : une seule erreur attendue.
6. Partie venant de Colonist Bar d'origine : escouades reprises.
