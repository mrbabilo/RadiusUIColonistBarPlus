# Feuille de route

Établie le 2026-10-15 à partir des specs de `docs/superpowers/specs/` et de l'audit `Rimworld-mods/AUDIT-colonistbar.md` (§ 3, § 6).

Principe : reprendre par lots les fonctions de groupe de [LTO] Colony Groups, réécrites au style Radius et appliquées aux escouades de Colonist Bar. Colony Groups n'a pas de licence : son code sert de référence de comportement et n'est jamais recopié.

## Fait (lots 1 et 1 bis publiés en 1.0.0 le 2026-10-15 ; lot 2 et retouches d'ergonomie en 1.1.0 le 2026-10-17 ; lot 3 et retouches en 1.2.0 le 2026-10-18)

| Lot | Spec | Contenu |
|---|---|---|
| 1. Ordres de combat | `2026-10-14-ordres-de-combat-design.md` | Mobiliser, tir à volonté, regroupement sur le chef, assaut (plus forts, plus faibles, fuyards). `SquadAssault` (`GameComponent`), nœud `RCB_SquadAssault` dans l'arbre de pensée, ciblage vanilla filtré, sans `Lord` ni `DutyDef`. |
| 1 bis. Carte d'ordres et aides | `2026-10-15-carte-ordres-design.md` | Carte Radius à la place du menu déroulant (déplaçable, fermable), état sur l'onglet, épée sur le portrait, six raccourcis clavier (catégorie `RCB_Squads`). Les raccourcis, prévus pour plus tard, sont arrivés avec ce lot. |
| 2. Postes de combat | `2026-10-16-postes-de-combat-design.md` | Quatre postes par escouade, enregistrés par pion et par carte dans `BarSquads` ; Aller, Enregistrer, Effacer (avec confirmation) ; aperçu au survol ; raccourci `RCB_SquadGoToStation`. |
| 3. Ordres médicaux | `2026-10-17-ordres-medicaux-design.md` | Section Soins de la carte : Secourir, Soigner, Au lit ; patients = colons et esclaves de la carte, hémorragies mortelles puis membres d'abord ; un patient par membre, le plus proche ; aperçu au survol ; trois raccourcis. Jobs vanilla, rien de sauvegardé. |

Contrôles en jeu des lots 1, 1 bis et 2 : faits.

Retouches d'ergonomie pour la 1.1.0 (hors lot, sans changement de sauvegarde) : affectation rapide depuis la sélection, état des bascules et raisons des boutons grisés sur la carte, raccourcis dans les infobulles, confirmation de Dissoudre, infobulle de l'onglet d'escouade, délai et ligne d'aide de la fiche de survol. Contrôles en jeu faits.

## Publications (1.0.0 le 2026-10-15, 1.1.0 le 2026-10-17, 1.2.0 le 2026-10-18)

- `CHANGELOG.md` daté, tag `v1.0.0`, release GitHub avec le zip du dossier publié `Mods/RadiusUIColonistBarPlus`.
- Page Workshop : texte prêt dans `docs/WORKSHOP.md` (incompatibilité avec Colonist Bar d'origine et Colony Groups, multijoueur non pris en charge). L'envoi se fait depuis le jeu, par le joueur.

Lot 3 (ordres médicaux) : spec `2026-10-17-ordres-medicaux-design.md`, plan `2026-10-17-ordres-medicaux.md`, code fait, contrôles en jeu faits, publié en 1.2.0. Prochaine étape : lot 4, après sa spec.

Retouches d'ergonomie pour la 1.2.0 (hors lot, sans changement de sauvegarde) : état « à terre / à soigner » sur la carte et dans l'infobulle de l'onglet, secours et soins en cours soulignés, carte qui dit quand aucun membre présent n'est en état d'agir, position de la carte retenue pour la session, carte relue toutes les 6 images au lieu de chaque image. Contrôles en jeu faits.

## Lots suivants

Chaque lot a sa propre spec, puis son plan, avant tout code. Ordre proposé : du plus proche des ordres existants au plus intrusif pour les sauvegardes.

### Lot 4 : travail et règles par escouade

- Travail : envoyer l'escouade sur une tâche, pause, repas, loisirs (référence : `WorkMenu`).
- Règles : tenue, drogues, nourriture, zone, soins appliqués à toute l'escouade en une fois (référence : `ManageMenu`). Appliquer les politiques vanilla aux pions plutôt que stocker une règle par escouade, pour ne rien ajouter à la sauvegarde.
- Lot le plus large : le découper dans sa spec si besoin.

### À évaluer, sans lot pour l'instant

- Capture des ennemis à terre (sortie du lot 2) : choix du lit de prisonnier, de l'ennemi et du porteur.
- Menu du portrait (cité hors périmètre du lot 1, sans contenu défini).
- Réglages au style Radius : 107 options sur 7 onglets dessinées avec les widgets vanilla (`Listing_Standard`), sans recherche ni réinitialisation par onglet. Gros chantier, relevé par l'analyse d'ergonomie du 2026-10-17. Au passage : « Still to come » est écrit en dur dans la fiche de survol (`HoverPopout.DrawPanel`).
- Factures réservées à une escouade : patchs de `Bill.PawnAllowedToStartAnew` et `Dialog_BillConfig`, donnée sauvegardée sur chaque facture. Coût élevé.
- Affichage de Colony Groups (barres de besoins, risque de crise) : Colonist Bar a déjà sa bande d'alertes ; à garder seulement si un manque réel apparaît en jeu.

## Règles communes à tous les lots

- Ne rien renommer de ce qui est sauvegardé ou lié par les joueurs : namespace `RadiusColonistBar`, noms de classes (`BarSquads`, `CCKits`, `CCHistory`, `SquadAssault`), defName des `KeyBindingDef` et `ThinkTreeDef`.
- Pas de `Lord` ni de `DutyDef` persistants : retirer le mod ne doit laisser qu'une erreur au chargement.
- Toute UI passe par le framework Radius (`CardChrome`, `Palette`, `RadiusFont`, `IconSet`), dans la carte d'ordres existante plutôt qu'un nouveau menu.
- Traductions : nouvelles clés en anglais dans `RadiusColonistBar.xml`, en français dans `RadiusColonistBarPlus.xml`. Le fork ne traduit jamais une clé d'origine : c'est le rôle de « Radius UI - Traduction française ».
- Vérification : build sans nouvel avertissement, puis liste de contrôles en jeu dans la spec.
