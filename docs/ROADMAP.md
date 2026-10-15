# Feuille de route

Établie le 2026-10-15 à partir des specs de `docs/superpowers/specs/` et de l'audit `Rimworld-mods/AUDIT-colonistbar.md` (§ 3, § 6).

Principe : reprendre par lots les fonctions de groupe de [LTO] Colony Groups, réécrites au style Radius et appliquées aux escouades de Colonist Bar. Colony Groups n'a pas de licence : son code sert de référence de comportement et n'est jamais recopié.

## Fait (1.0.0, publiée le 2026-10-15)

| Lot | Spec | Contenu |
|---|---|---|
| 1. Ordres de combat | `2026-10-14-ordres-de-combat-design.md` | Mobiliser, tir à volonté, regroupement sur le chef, assaut (plus forts, plus faibles, fuyards). `SquadAssault` (`GameComponent`), nœud `RCB_SquadAssault` dans l'arbre de pensée, ciblage vanilla filtré, sans `Lord` ni `DutyDef`. |
| 1 bis. Carte d'ordres et aides | `2026-10-15-carte-ordres-design.md` | Carte Radius à la place du menu déroulant (déplaçable, fermable), état sur l'onglet, épée sur le portrait, six raccourcis clavier (catégorie `RCB_Squads`). Les raccourcis, prévus pour plus tard, sont arrivés avec ce lot. |

Contrôles en jeu des deux lots : faits.

## Publication 1.0.0 (faite le 2026-10-15)

- `CHANGELOG.md` daté, tag `v1.0.0`, release GitHub avec le zip du dossier publié `Mods/RadiusUIColonistBarPlus`.
- Page Workshop : texte prêt dans `docs/WORKSHOP.md` (incompatibilité avec Colonist Bar d'origine et Colony Groups, multijoueur non pris en charge). L'envoi se fait depuis le jeu, par le joueur.

Prochaine étape : spec du lot 2.

## Lots suivants

Chaque lot a sa propre spec, puis son plan, avant tout code. Ordre proposé : du plus proche des ordres existants au plus intrusif pour les sauvegardes.

### Lot 2 : postes de combat

- Jusqu'à quatre formations par escouade, une position par pion (référence : `BattleStationsMenu`, `Formation` de Colony Groups).
- Ordre « aller au poste » depuis la carte d'ordres, et raccourci clavier.
- Positions sauvegardées par carte dans un `GameComponent` (ou dans `BarSquads` si la forme s'y prête) : à trancher dans la spec, avec le risque de sauvegarde.
- Petit ajout possible dans le même lot : capture des ennemis à terre, seul ordre de combat de Colony Groups non repris au lot 1.

### Lot 3 : ordres médicaux

- Aller au lit médical, secourir les pions tombés, soigner les blessés, pour les membres présents de l'escouade.
- Jobs vanilla seulement (`Rescue`, `TendPatient`, repos au lit), via `TryTakeOrderedJob`, comme `Regroup` : rien de neuf dans la sauvegarde.

### Lot 4 : travail et règles par escouade

- Travail : envoyer l'escouade sur une tâche, pause, repas, loisirs (référence : `WorkMenu`).
- Règles : tenue, drogues, nourriture, zone, soins appliqués à toute l'escouade en une fois (référence : `ManageMenu`). Appliquer les politiques vanilla aux pions plutôt que stocker une règle par escouade, pour ne rien ajouter à la sauvegarde.
- Lot le plus large : le découper dans sa spec si besoin.

### À évaluer, sans lot pour l'instant

- Menu du portrait (cité hors périmètre du lot 1, sans contenu défini).
- Factures réservées à une escouade : patchs de `Bill.PawnAllowedToStartAnew` et `Dialog_BillConfig`, donnée sauvegardée sur chaque facture. Coût élevé.
- Affichage de Colony Groups (barres de besoins, risque de crise) : Colonist Bar a déjà sa bande d'alertes ; à garder seulement si un manque réel apparaît en jeu.

## Règles communes à tous les lots

- Ne rien renommer de ce qui est sauvegardé ou lié par les joueurs : namespace `RadiusColonistBar`, noms de classes (`BarSquads`, `CCKits`, `CCHistory`, `SquadAssault`), defName des `KeyBindingDef` et `ThinkTreeDef`.
- Pas de `Lord` ni de `DutyDef` persistants : retirer le mod ne doit laisser qu'une erreur au chargement.
- Toute UI passe par le framework Radius (`CardChrome`, `Palette`, `RadiusFont`, `IconSet`), dans la carte d'ordres existante plutôt qu'un nouveau menu.
- Traductions : nouvelles clés en anglais dans `RadiusColonistBar.xml`, en français dans `RadiusColonistBarPlus.xml`. Le fork ne traduit jamais une clé d'origine : c'est le rôle de « Radius UI - Traduction française ».
- Vérification : build sans nouvel avertissement, puis liste de contrôles en jeu dans la spec.
