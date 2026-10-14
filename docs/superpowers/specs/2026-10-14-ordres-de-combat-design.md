# Radius UI - Colonist Bar Plus : ordres de combat d'escouade

Date : 2026-10-14. Révision 2, après relecture critique (voir `Rimworld-mods/AUDIT-colonistbar.md`, § 7). Statut : à relire avant le plan d'implémentation.

## Objectif

Créer « Radius UI - Colonist Bar Plus », un fork de Radius UI - Colonist Bar (astryl, MIT), sur le modèle de Radius UI - Inspector Plus. Ce premier lot y ajoute les ordres de combat de groupe de [LTO] Colony Groups (DerekBickley), réécrits au style Radius et appliqués aux escouades qui existent déjà dans Colonist Bar.

Colony Groups n'indique aucune licence : son code sert de référence de comportement et n'est pas recopié.

Hors de ce lot (lots suivants) : postes de combat, ordres médicaux, travail et règles par escouade, raccourcis clavier, menu du portrait.

## Critères de réussite

- Un clic droit sur l'onglet d'une escouade, ou le menu d'une escouade dans le module Escouades du Centre de commandement, ouvre les ordres : mobiliser ou démobiliser, tir à volonté, regroupement, assaut (plus fort, plus faible, fuyards).
- En assaut, chaque pion vise une cible du mode quand il en atteint une, tire dessus, se met à couvert comme en vanilla, et se rabat sur le ciblage vanilla sinon.
- L'assaut ne se fige jamais : un ennemi qui arrive plus tard est attaqué.
- Un ordre donné à la main à un pion en assaut est exécuté et met fin à son assaut.
- Démobiliser un pion arrête son assaut.
- Une partie qui tournait avec Colonist Bar d'origine garde ses escouades, kits et historiques.
- Retirer le mod en cours de partie ne laisse qu'une erreur au chargement, sans autre trace sur les pions.

## Fork

Dossier `RadiusUIColonistBarPlus/` (dépôt git propre, branche `main`), copie de `RadiusUIColonistBar/` avec :

- `About.xml` : nom « Radius UI - Colonist Bar Plus », packageId `mrbabilo.RadiusUI.ColonistBarPlus`, auteur mrbabilo, mêmes dépendances (Harmony, Radius UI Framework), `incompatibleWith` : `astryl.RadiusUI.ColonistBar`, `DerekBickley.LTOColonyGroupsFinal`. Description : multijoueur non pris en charge.
- Gardés sans changement : namespace `RadiusColonistBar`, DLL `RadiusColonistBar.dll`, identifiant Harmony `astryl.radiusui.colonistbar`, noms de toutes les classes. Les sauvegardes référencent par nom de classe complet les trois `GameComponent` du mod d'origine : `BarSquads` (escouades), `CCKits` (kits d'équipement), `CCHistory` (historiques du Centre de commandement). Le `KeyBindingDef` `RCB_FollowCam` garde son defName, sinon les joueurs perdent leur raccourci. Aucun mod de la suite Radius ne détecte Colonist Bar par son packageId (grep sur les sources décompilées).
- Les réglages du mod sont stockés sous le nom du dossier : ils repartent des valeurs par défaut. Aucune incidence sur les sauvegardes.
- `deploy.sh` (même schéma que celui d'Inspector Plus : build hors de l'arbre, copie de la DLL, `rsync --delete` vers `Mods/RadiusUIColonistBarPlus`), `README.md`, `CHANGELOG.md`, `LICENSE` (notice MIT d'astryl conservée, mrbabilo ajouté), `.gitignore` (DLL ignorée).

## Composants ajoutés

Chacun dans son fichier, dans `Source/RadiusColonistBar/`.

### `SquadMembers`

Membres d'une escouade sur une carte : `map.mapPawns.FreeColonistsSpawned` puis `SlavesOfColonySpawned`, filtrés par `BarSquads.SquadIndexOf(pawn) == index`. Exclus : mécanoïdes (`IsColonyMech`) et goules ou autres sous-humains (`IsSubhuman`), dont l'arbre de pensée n'est pas patché.

Pour un ordre, un membre est **apte** s'il n'est ni couché, ni en crise mentale, ni prisonnier, et a un `drafter`.

**Chef** : le premier membre apte dans l'ordre de la barre (`Find.ColonistBar.Entries`) ; à défaut (barre vide ou pion absent de la barre), le plus petit `thingIDNumber`. `BarSquads` n'a pas de liste ordonnée de membres (`squadOf` est un dictionnaire) : c'est pourquoi le chef est défini ainsi.

### `SquadOrders`

Classe statique. Chaque méthode prend un index d'escouade et agit sur les membres aptes de la carte affichée (`Find.CurrentMap`). Les ordres s'exécutent pion par pion : une exception est journalisée avec `Log.ErrorOnce` et les autres pions reçoivent l'ordre.

| Méthode | Effet |
|---|---|
| `Draft` | `drafter.Drafted = true`. Le setter vanilla remet `FireAtWill` à vrai à chaque mobilisation. |
| `Undraft` | `drafter.Drafted = false` (le patch du setter efface le mode d'assaut). |
| `ToggleFireAtWill` | Si tous les membres aptes ont `FireAtWill`, le coupe pour tous et met fin à leur assaut ; sinon l'allume pour tous. |
| `Regroup` | Mobilise, met fin à l'assaut, puis donne à chaque membre sauf le chef un job `Goto` vers `RCellFinder.BestOrderedGotoDestNear(chef.Position, pawn)`, `LocomotionUrgency.Sprint`, via `TryTakeOrderedJob`. |
| `Assault(mode)` | Mobilise, force `FireAtWill = true`, sélectionne les membres aptes, inscrit chacun dans `SquadAssault` avec le mode (un pion déjà en assaut change simplement de mode), puis termine son job en cours (`EndCurrentJob(JobCondition.InterruptForced)`) pour que l'arbre de pensée reprenne la main. |

Méthodes d'état pour le menu : nombre de membres aptes sur la carte, tous mobilisés ou non, tous en tir à volonté ou non, mode commun s'il y en a un.

### `SquadAssault` (`GameComponent`)

- Données : `Dictionary<Pawn, AssaultMode>`, avec `AssaultMode` : `Strongest`, `Weakest`, `Fleeing`. Rien d'autre : la réponse aux hostiles du pion n'est jamais modifiée durablement (voir `JobGiver_SquadAssault`).
- API : `Set(pawn, mode)`, `TryGetMode(pawn, out mode)`, `Clear(pawn)`.
- Ménage, toutes les 250 ticks et avant chaque sauvegarde : retire les pions morts, détruits, couchés, absents de toute carte ou non mobilisés.
- Sauvegarde : `Scribe_Collections.Look(ref modes, "squadAssault", LookMode.Reference, LookMode.Value, ref tmpPawns, ref tmpModes, logNullErrors: false)`. Une référence de pion non résolue au chargement est sautée sans erreur ; le ménage suivant fait le reste.

### `JobGiver_SquadAssault`

Hérite de `JobGiver_AIFightEnemies` (vanilla) : il garde la position de tir à couvert, le corps à corps et les intervalles d'expiration vanilla. Les colons n'utilisent pas de capacités dans ce giver (`JobGiver_AIFightEnemy.TryGiveJob` les exclut avec `!pawn.IsColonist`), comme en vanilla. `targetAcquireRadius` 20 et `targetKeepRadius` 23 (valeurs de Colony Groups), renseignés dans le XML du nœud (champs `protected`, recopiés par `DeepCopy`).

**`TryGiveJob(pawn)`** (`protected override`) :

1. Le vanilla renvoie `null` si la réponse aux hostiles d'un colon n'est pas `Attack`. On la règle sur `Attack` le temps de l'appel à `base.TryGiveJob`, puis on la rétablit dans un `finally`. Rien ne reste sur le pion, même si le mod est retiré.
2. Si `base` renvoie un `Wait_Combat` et que `pawn.mindState.enemyTarget` est atteignable par l'arme à distance depuis la position actuelle (`verb.CanHitTarget`), on renvoie à la place un job `AttackStatic` sur cette cible : `expiryInterval` = `ExpiryInterval_ShooterSucceeded`, `checkOverrideOnExpire = true`, `endIfCantShootTargetFromCurPos = true`. Sans cela, le tir partirait du ciblage vanilla de `Wait_Combat` et ignorerait le mode.
3. Si `base` renvoie `null` (aucune cible), on renvoie un `Wait_Combat` avec `expiryInterval` tiré entre 120 et 250 ticks et `checkOverrideOnExpire = true`. Sans cela, le nœud suivant (`JobGiver_Orders`) donne un `Wait_Combat` sans expiration et l'escouade reste plantée devant les ennemis qui arrivent ensuite.
4. Tout autre job renvoyé par `base` (`Goto` vers une position de tir, attaque au corps à corps) passe tel quel.

**Choix de la cible** (`FindAttackTarget`, `protected override`) : un champ statique `passFilter` (filtre de la passe en cours) est posé autour de chaque appel à `base.FindAttackTarget`, dans un `try/finally`. `ExtraTargetValidator` (`protected override`) renvoie `base` et, si `passFilter` est posé, le filtre.

- Candidats : `map.attackTargetsCache.GetPotentialTargetsFor(pawn)`, ne gardant que les pions non couchés, hors brouillard, à moins de `targetAcquireRadius` du pion. Les cibles qui ne sont pas des pions (tourelles, mortiers) ne passent aucun filtre de mode : seul le repli vanilla les prend.
- `Strongest` : valeurs distinctes de `kindDef.combatPower` parmi les candidats, de la plus haute à la plus basse ; une passe par valeur, trois au plus, filtre « `combatPower` égal à cette valeur ». La première cible trouvée gagne.
- `Weakest` : même chose de la plus basse à la plus haute.
- `Fleeing` : une passe, filtre « pion en fuite » : job `Flee` ou `FleeAndCower`, état mental `PanicFlee`, duty `ExitMapRandom` ou `ExitMapBest`, ou job courant avec `exitMapOnArrival`.
- Repli : si aucune passe ne trouve de cible, un dernier appel à `base.FindAttackTarget` sans filtre (ciblage vanilla).
- Coût : au plus quatre appels au ciblage vanilla par décision, une décision par expiration de job (de l'ordre de 450 ticks par pion). Pas de cache.

### `ThinkTreeDef` `RCB_SquadAssault`

Fichier `Defs/ThinkTreeDefs/SquadAssault.xml` :

- `insertTag` `Humanlike_PostMentalState`, `insertPriority` 100. Ce crochet, prévu par le jeu pour les mods, est appelé dans `Humanlike.xml` juste avant `ThinkNode_QueuedJob` et avant la branche des pions mobilisés (`JobGiver_Orders`). Pas de patch XPath : un autre mod qui modifie `Humanlike` ne le casse pas.
- Racine : `ThinkNode_ConditionalSquadAssault` (satisfait si le pion est mobilisé et a un mode dans `SquadAssault`), avec dessous `RadiusColonistBar.JobGiver_SquadAssault`.
- Pas de `DutyDef`, de `Lord` ni de `LordJob`.

### Patchs Harmony

Classes `[HarmonyPatch]` à part, patchées par le `HarmonyBootstrap` existant (une classe en échec est sautée avec un avertissement).

- **Setter `Pawn_DraftController.Drafted`** : postfix ; si `Current.Game != null` et `!__instance.Drafted`, appelle `SquadAssault.Clear(__instance.pawn)`. Sûr pendant le chargement : `ExposeData` écrit le champ `draftedInt` sans passer par le setter.
- **`Pawn_JobTracker.TryTakeOrderedJob`** : postfix ; si `__result` est vrai, appelle `SquadAssault.Clear` pour le pion. Ce point d'entrée sert aux ordres du joueur (`job.playerForced` y est mis à vrai) ; sans lui, le nœud d'assaut, qui passe avant la file de jobs, reprendrait la main sur l'ordre du joueur.

### Menus des ordres

- **Onglet d'escouade** : clic droit sur un onglet de la rangée d'onglets (`RcbIsland.cs:1488-1497`). Le test `tabRects[j].Contains(position)` et `RcbCohorts.TokenIsSquad(railTok[j], out si)` passe avant la branche existante `SetToken(0)`, avec `chromeClaimedDown = true` et `current.Use()`. Ailleurs sur la rangée, le clic droit garde son effet : revenir à « Tous ».
  La rangée n'existe que si le réglage `barCohortTabs` est actif (par défaut), s'il y a au moins deux onglets et si la bande est dépliée. Les escouades rangées dans le menu « +N » (`OpenSpillMenu`) n'ont pas de clic droit. D'où le second point d'accès.
- **Module Escouades** (`Module_Squads.SquadMenu`, l. 116) : les mêmes ordres sont ajoutés en tête du menu d'une escouade, avant Renommer.
- Les deux appellent un constructeur unique `SquadOrderMenu.Options(index)`. Fenêtre : `RcbFloatMenu`. Options : Mobiliser ou Démobiliser (selon l'état), Tir à volonté (marqué actif si tous l'ont), Regroupement, Assaut (sous-menu via `FloatMenuOptionSub` : Plus fort, Plus faible, Fuyards, le mode actif marqué).
- Infobulle traduite sur chaque option. Sans membre apte sur la carte affichée, options désactivées avec la raison.
- `FloatMenuOptionSub` impose une infobulle anglaise codée en dur (« Right-click: open as a submenu… ») : remplacée par la clé `RCB_Sub_RightClickTip`.
- Son : clic vanilla. Les sons de Colony Groups ne sont pas repris.

### Traductions

- Nouvelles clés `RCB_Orders_*` et `RCB_Sub_RightClickTip` dans `Languages/English/Keyed/RadiusColonistBar.xml`, et dans un nouveau `Languages/French/Keyed/RadiusColonistBarPlus.xml` qui ne contient **que ces clés ajoutées**.
- Les clés d'origine de Colonist Bar sont traduites par « Radius UI - Traduction française » (`RadiusUIFrench/`). Une même clé `Keyed` définie par deux mods dans la même langue est refusée la seconde fois (« Duplicate keyed translation key », `Verse.LoadedLanguage`) : le fork ne traduit jamais une clé d'origine.

## Sauvegardes

| Donnée | Risque | Parade |
|---|---|---|
| `SquadAssault` (`GameComponent`) | Mod retiré : erreur « Could not find class » au chargement, bloc ignoré, modes d'assaut perdus. | Aucune donnée critique dedans. |
| Références de pions dans `SquadAssault` | Pion disparu. | `logNullErrors: false`, ménage avant sauvegarde. |
| Jobs en cours | Aucun : jobs vanilla (`AttackStatic`, `Wait_Combat`, `Goto`, attaque au corps à corps). | — |
| `ThinkTreeDef` `RCB_SquadAssault` | Aucun : non sauvegardé. | — |
| `BarSquads`, `CCKits`, `CCHistory`, `RCB_FollowCam` | Déjà présents dans Colonist Bar d'origine. | Noms de classes et defName conservés. |
| `hostilityResponse` | Aucun : modifié seulement le temps d'un appel, rétabli dans un `finally`. | — |

## Vérification

Pas de tests automatisés dans ce dépôt. Chaque étape compile sans erreur ni nouvel avertissement. Contrôle en jeu, à faire par le joueur :

1. Player.log sans erreur `[Radius UI - Colonist Bar]` au démarrage ; `RCB_SquadAssault` chargé.
2. Chaque ordre essayé sur une escouade pendant un raid, depuis l'onglet et depuis le module Escouades.
3. En assaut « plus fort » : les tireurs visent l'ennemi le plus fort à portée ; repli vanilla quand aucun ennemi du mode n'est à portée.
4. Un ennemi qui arrive après la fin d'un combat est attaqué sans nouvel ordre.
5. Un ordre de déplacement donné à un pion en assaut est exécuté, et le pion sort de l'assaut.
6. Réponse aux hostiles inchangée après l'assaut.
7. Sauvegarde puis rechargement pendant un assaut : l'assaut reprend.
8. Partie chargée après retrait du mod : une seule erreur attendue, pions normaux.
9. Partie venant de Colonist Bar d'origine : escouades, kits et historiques repris.
