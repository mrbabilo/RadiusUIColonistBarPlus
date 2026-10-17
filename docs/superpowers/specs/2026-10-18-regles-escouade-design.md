# Colonist Bar Plus — onglets de la carte d'ordres et règles d'escouade

Date : 2026-10-18. Lot 4 de `docs/ROADMAP.md`, première partie (4a). La seconde partie (4b, onglet Travail : envoyer l'escouade sur un type de travail, manger, dormir, copier priorités et emploi du temps du chef) aura sa propre spec.

Comportement de référence : `ManagementMenu` de [LTO] Colony Groups (règles de tenue, nourriture, drogues, zones appliquées au groupe) ; aucune ligne n'en est reprise. UX de référence : le menu « Règles » du portrait de Colonist Bar (`Patch_HandleClicks_RightClickMenu.BuildPoliciesMenu`), qui applique une règle à toute la sélection et affiche « Mixed » quand elle diffère.

Le joueur a demandé de démarrer sans questions, de s'inspirer de l'UX de Colonist Bar et de l'améliorer : les choix non dictés par le code existant sont marqués « choix de conception ».

## Objectif

Régler en une fois les règles vanilla de toute une escouade, depuis la carte d'ordres, sans ouvrir l'onglet Affectation ni sélectionner chaque pion. La carte, qui a grandi lot après lot, passe en onglets pour rester compacte.

Critères de réussite :

- la carte d'ordres a deux onglets, Combat et Règles, et ne change pas de hauteur en passant de l'un à l'autre ;
- l'onglet Règles montre, pour chaque règle, la valeur commune de l'escouade ou « Mixte » ;
- un choix dans la liste l'applique à tous les membres concernés ;
- rien n'est ajouté à la sauvegarde : les règles vanilla sont posées sur les pions.

## 1. Onglets de la carte

- Bande d'onglets sous la ligne d'état : « Combat », « Règles » (« Travail » viendra en 4b). Style des onglets de la barre (`RcbIsland` : libellé `MicroLabel`, onglet actif sur fond `CardChrome.Rounded`), hauteur 22, suivie d'un écart de 8.
- **Combat** : le contenu actuel (Mobiliser, Tir à volonté, Regroupement, Sélectionner, postes de combat, assaut, soins). Les soins restent dans Combat : on les donne pendant ou juste après le combat.
- **Commun aux onglets**, sous le contenu : la section Membres et, depuis le Centre de commandement, la ligne Renommer / Monter / Descendre / Dissoudre.
- Hauteur de la carte = hauteur commune + la plus haute des deux zones d'onglet : passer d'un onglet à l'autre ne fait pas sauter la carte ni ses boutons du bas.
- Onglet ouvert retenu pour la session, commun à toutes les escouades (choix de conception : on enchaîne souvent les mêmes réglages d'une escouade à l'autre).
- Clic sur un onglet : son `SoundDefOf.Click`, comme les boutons de la carte.

## 2. Onglet Règles

### Membres concernés

- Tenue, nourriture, drogues, soins médicaux, réaction aux menaces : **tous les membres vivants** de l'escouade, sur toutes les cartes, en caravane ou en transport (`PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists` filtré par `SquadIndexOf`, comme `SquadStatus.total`). Une règle n'est pas liée à une carte.
- Zone : membres présents sur la **carte affichée** seulement (une zone appartient à une carte ; `playerSettings.AreaRestrictionInPawnCurrentMap`).
- Un pion sans le suivi concerné (`outfits`, `foodRestriction`, `drugs`, `playerSettings` nuls) est ignoré pour cette règle.

### Lignes

Six lignes, hauteur `BtnH`, écart `Gap`. Chacune : libellé à gauche (micro, 92 px), bouton de valeur au milieu, bouton « Modifier » (`AssignTabEdit`) à droite (72 px) quand vanilla a une fenêtre de gestion ; sinon le bouton de valeur prend la largeur.

| Ligne | Valeur | Liste | Modifier |
|---|---|---|---|
| Tenue | `outfits.CurrentApparelPolicy` | `outfitDatabase.AllOutfits` | `Dialog_ManageApparelPolicies` |
| Nourriture | `foodRestriction.CurrentFoodPolicy` | `foodRestrictionDatabase.AllFoodRestrictions` | `Dialog_ManageFoodPolicies` |
| Drogues | `drugs.CurrentPolicy` | `drugPolicyDatabase.AllPolicies` | `Dialog_ManageDrugPolicies` |
| Soins médicaux | `playerSettings.medCare` | les cinq `MedicalCareCategory`, libellés `GetLabel()` | — |
| Zone | `playerSettings.AreaRestrictionInPawnCurrentMap` (membres dont `SupportsAllowedAreas` est vrai) | « Sans restriction » (clé vanilla `NoAreaAllowed`) puis les zones de la carte `AssignableAsAllowed()`, libellé `Label` | `Dialog_ManageAreas(Find.CurrentMap)` |
| Menaces | `playerSettings.hostilityResponse` | `HostilityResponseMode` Ignore / Attack / Flee, libellés `GetLabel()` | — |

- Bouton de valeur : la valeur commune, ou « Mixte » quand les membres diffèrent, ou grisé avec sa raison (aucun membre concerné ; Zone : aucun membre sur cette carte). Infobulle : la règle, puis jusqu'à 8 lignes « Nom : valeur » quand elle est mixte, puis « +{n} autres ».
- Clic : menu `RcbFloatMenu` (celui du portrait) avec les choix ; la valeur commune actuelle est suivie de « (actuelle) ». Menaces : « Attaquer » n'est jamais posé sur un pion incapable de violence (règle du bouton vanilla) ; le message compte à part les pions laissés tels quels. La carte reste ouverte : un menu ouvert après elle est au-dessus, et `WindowStack` ne ferme que les fenêtres au-dessus de celle qu'on clique.
- Choix : posé sur chaque membre concerné, chacun dans `SquadOrders.Try`. Message `SilentInput` « {règle} : {valeur} pour {n} membres ». `SquadStatus.Invalidate()` pour que la carte relise aussitôt.
- Modifier : ferme la carte, puis ouvre la fenêtre vanilla sur la valeur commune (ou celle du premier membre si mixte). La carte est sur la couche `Super`, au-dessus des fenêtres : laissée ouverte, elle couvrirait la fenêtre et prendrait ses clics (même règle que Renommer).
- Tenue : quand au moins un membre porte un vêtement forcé, une ligne « Vêtements forcés : {n} membres » avec le bouton « Libérer » (`outfits.forcedHandler.Reset()` pour chacun), comme l'onglet Affectation vanilla. Choix de conception : la ligne n'apparaît que si besoin, sous les six règles, dans la réserve de hauteur de l'onglet, pour ne pas décaler les autres lignes.
- Conditions par règle (colonnes de l'onglet Affectation) : tenue `outfits`, nourriture `foodRestriction`, drogues `drugs`, soins `playerSettings` non nuls ; zone : de la colonie, mutant qui respecte les zones, `SupportsAllowedAreas` ; menaces : humanoïde avec `playerSettings`.

### Coût

Valeurs et comptes calculés dans l'instantané de la carte (toutes les 6 images ou après un ordre, comme le reste) : un passage sur les membres, une chaîne par ligne. Rien n'est relu à chaque image.

## 3. Menu « Règles » du portrait (amélioration de l'existant)

Le menu du portrait écrit en dur « Outfit: », « Food: », « Drugs: », « Mixed », « None », « (n selected) », « No policies available » et le message de confirmation en anglais. Ces textes passent par des clés `RCB_Policies_*` (anglais dans le fichier du fork, français dans `RadiusColonistBarPlus.xml`), comme les boutons de la fiche de survol en 1.0.0. Les libellés « Mixte » et « Sans restriction » sont partagés avec l'onglet Règles. Pas d'autre changement de comportement.

## 4. Code

| Fichier | Rôle |
|---|---|
| `SquadRules.cs` (nouveau) | Classe statique : `Members(index)` (toutes cartes), `Rule` (enum des six règles), lecture de la valeur commune, menus de choix, application, ouverture des fenêtres vanilla. |
| `SquadOrderCard.cs` | Bande d'onglets, onglet Règles, hauteur commune ; instantané étendu aux règles. |
| `Patch_HandleClicks_RightClickMenu.cs` | Textes du menu Règles par clés. |

## 5. Textes

- Clés `RCB_Tab_Combat`, `RCB_Tab_Rules`, `RCB_Rules_*` (libellés des six lignes, Modifier, Mixte, raisons des boutons grisés, « +{n} autres », message, vêtements forcés, Libérer), `RCB_Policies_*` (menu du portrait).
- Textes vanilla repris : `NoAreaAllowed` (« Sans restriction »), `AssignTabEdit` (« Modifier… »), `GetLabel()` des soins médicaux et des réactions aux menaces.
- Documentation : README (onglets, section Règles), CHANGELOG « 1.3.0 — non publiée », ROADMAP (4a fait, 4b à faire).

## 6. Hors périmètre (4b ou plus tard)

- Onglet Travail (4b).
- Emploi du temps, priorités de travail, idéoligion, rôles.
- Règles d'enfant ou de mécanoïde.
- Garder une règle « d'escouade » qui s'imposerait aux nouveaux membres : il faudrait la sauvegarder.

## 7. Vérification

Build sans nouvel avertissement. Contrôles en jeu :

1. Onglets : la carte garde sa hauteur ; l'onglet choisi reste ouvert à la réouverture, aussi pour une autre escouade.
2. Tous les membres en tenue « Tout » : la ligne l'affiche ; un membre changé dans l'onglet Affectation : « Mixte », l'infobulle nomme chacun.
3. Choisir une tenue : tous les membres, y compris un membre en caravane, la reçoivent ; message avec le bon nombre.
4. Zone : seuls les membres de la carte affichée changent ; aucun membre sur la carte : ligne grisée avec sa raison.
5. Modifier : la carte se ferme, la fenêtre vanilla s'ouvre sur la bonne règle et reçoit les clics.
6. Vêtement forcé sur un membre : la ligne apparaît, « Libérer » la vide.
7. Soins médicaux et menaces : posés sur tous les membres.
8. Menu Règles du portrait en français : plus aucun texte anglais.
9. Un pacifiste dans l'escouade : Menaces → Attaquer le laisse tel quel, le message le compte à part.
10. Un enfant ou un esclave dans l'escouade : aucune exception, seulement les règles que vanilla lui permet.
