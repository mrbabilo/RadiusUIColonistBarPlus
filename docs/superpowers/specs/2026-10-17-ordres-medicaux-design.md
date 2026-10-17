# Colonist Bar Plus — ordres médicaux d'escouade

Date : 2026-10-17. Lot 3 de `docs/ROADMAP.md`. Suite de `2026-10-16-postes-de-combat-design.md`. Comportement de référence : `JobGiver_RescueNearby`, `JobGiver_TendWounded` et `WorkSearchUtility` de [LTO] Colony Groups ; aucune ligne n'en est reprise (pas de licence). Comportement vanilla suivi : `FloatMenuOptionProvider_RescuePawn`, `FloatMenuOptionProvider_DraftedTend`, `WorkGiver_Tend.JobOnThing`, `Building_Bed.GetBedRestFloatMenuOption`.

Le joueur a demandé de démarrer sans questions et de reprendre l'UX de Colonist Bar : les choix ci-dessous non dictés par le code existant sont marqués « choix de conception ».

## Objectif

Après un combat, ou pendant, relever et soigner l'escouade en trois clics au lieu d'un clic droit par pion et par blessé.

Critères de réussite :

- un clic envoie les membres valides secourir les pions à terre, chacun vers un blessé différent ;
- un clic envoie les membres capables de soigner vers les blessés, chacun vers un patient différent ;
- un clic envoie au lit, jusqu'à guérison, les membres qui en ont besoin ;
- chaque bouton dit combien de pions il concerne, et pourquoi il est grisé.

Rien n'est ajouté à la sauvegarde : uniquement des jobs vanilla donnés par `TryTakeOrderedJob`.

## 1. Comportement

### Termes

- **Membres aptes** : `SquadMembers.Able(Find.CurrentMap, index)`, comme les autres ordres (ni à terre, ni en crise, ni en repos mortel, ni mécanoïde).
- **Pions soignables** (choix de conception) : les colons et esclaves de la colonie présents sur la carte affichée (`FreeColonistsSpawned`, `SlavesOfColonySpawned`), membres de l'escouade ou non. Un colon à terre hors escouade meurt aussi bien qu'un membre ; l'escouade est souvent la seule force sur place. Les prisonniers, animaux et invités sont hors périmètre (vanilla les traite par d'autres règles).
- **Ordre de priorité des patients** (choix de conception) : d'abord les pions en hémorragie mortelle, avec le critère de la bande d'alertes (`BleedRateTotal > 0.01` et moins de 60 000 ticks avant la mort) ; puis les membres de l'escouade ; puis les autres. À égalité : temps avant la mort par hémorragie le plus court (`HealthUtility.TicksUntilDeathDueToBloodLoss`), puis `thingIDNumber`.
- **Lits distribués** : pendant un même clic, un lit n'est pas donné plus de fois qu'il n'a de places (`SleepingSlotsCount`). Un job mis en file derrière un job non interruptible ne réserve pas encore son lit : sans ce compte, deux pions recevraient la même place.
- **Membre occupé** : membre apte dont le job en cours est déjà `Rescue` ou `TendPatient`. Il n'est pas réaffecté : cliquer deux fois ne rebat pas les cartes.
- **Appariement** (commun à Secourir et Soigner) : patients pris dans l'ordre de priorité ; chacun reçoit le membre libre le plus proche (distance au carré) qui peut le prendre en charge ; un membre ne reçoit qu'un patient. Un patient déjà réservé par un autre pion échoue au test de réservation et n'est pas pris.

### Secourir

- Patients : pions soignables pour lesquels `HealthAIUtility.WantsToBeRescued` est vrai (à terre, pas au lit, pas en crise sauf rage, pas pris par un rituel ou une caravane en formation…) et `PawnStatusUtil.DownedIsIncidental` est faux, soins médicaux non réglés sur « Aucun soin » (comme vanilla, qui refuse alors le secours). Le filtre `DownedIsIncidental` est celui de la bande d'alertes : il écarte les bébés, toujours « à terre », que vanilla confie aux soins aux enfants et non au secours.
- Secouriste possible : membre apte libre, capable de manipulation (`PawnCapacityDefOf.Manipulation`), pour lequel `HealthAIUtility.CanRescueNow(secouriste, patient, forced: true)` est vrai. `forced: true` comme le menu vanilla : l'ordre vient du joueur, la proximité d'un ennemi ne l'arrête pas. `forced` ignore aussi les réservations : `CanReserve(patient)` est testé à part, sinon un membre partirait vers un pion qu'un autre colon emporte déjà.
- Lit : `RestUtility.FindBedFor(patient, secouriste, checkSocialProperness: false, ignoreOtherReservations: false, statut)`, puis la même recherche avec `ignoreOtherReservations: true`, comme vanilla ; `statut` = `GuestStatus.Slave` pour un esclave (comme `FloatMenuOptionProvider_ReturnSlaveToBed`, qui le ramène à un lit d'esclave), `null` sinon. Un lit déjà distribué à toutes ses places pendant ce clic est refusé. Sans lit, ce patient est sauté.
- Job : `JobDefOf.Rescue` (patient, lit), `count = 1`, via `TryTakeOrderedJob`. Le secouriste sort de l'assaut (`SquadAssault.Get?.End`) et garde son état de mobilisation.
- Message `SilentInput` : « Secours : {n} pions en route ». Patients restés sans secouriste faute de lit : message `RejectInput` « `CannotRescue` : `NoNonPrisonerBed` » (ou `NoSlaveBed` pour un esclave), textes vanilla.

### Soigner

- Patients : pions soignables pour lesquels `HealthAIUtility.ShouldBeTendedNowByPlayer` est vrai, hors crise agressive (`InAggroMentalState`), comme `WorkGiver_Tend`. Un membre apte blessé est aussi un patient.
- Soigneur possible : membre apte libre, travail « Médecin » non désactivé (`WorkTypeIsDisabled(WorkTypeDefOf.Doctor)`), capable de manipulation, qui peut réserver et atteindre le patient (`CanReserveAndReach`, `PathEndMode.ClosestTouch`, `Danger.Deadly`). Se soigner soi-même n'est possible que si `playerSettings.selfTend` est actif ; la distance nulle le fait passer en premier quand il l'est.
- Médicament : `HealthAIUtility.FindBestMedicine(soigneur, patient)`, inventaire ou carte, selon les soins médicaux du patient, comme `WorkGiver_Tend`. Choix de conception : pas la règle « inventaire seulement » du soin mobilisé vanilla, qui soigne presque toujours sans médicament.
- Job : `JobDefOf.TendPatient` construit comme `WorkGiver_Tend.JobOnThing` (cible C = porteur du médicament quand il est dans l'inventaire d'un autre pion), `count = 1`, `draftedTend` vrai pour un patient debout, qui attend alors le soigneur au lieu de s'éloigner (comme le soin mobilisé vanilla), faux pour un patient au lit, dont l'attente forcée remplacerait le repos. Le soigneur sort de l'assaut et garde son état de mobilisation.
- Message `SilentInput` : « Soins : {n} pions en route ». Patients sans soigneur parce qu'aucun membre ne peut soigner : rien de plus que l'infobulle grisée.

### Au lit

- Concernés : membres aptes pour lesquels `HealthAIUtility.ShouldSeekMedicalRest` est vrai.
- Chacun est démobilisé (vanilla refuse le repos médical à un pion mobilisé ; la démobilisation arrête aussi l'assaut), puis reçoit `JobDefOf.LayDown` sur `RestUtility.FindBedFor(pion, pion, checkSocialProperness: false)` (lit médical d'abord, puis le sien ; pour un esclave, `CanUseBedNow` lit son statut et ne garde que les lits d'esclave), avec `restUntilHealed = true`, comme « Utiliser le lit médical ». Même compte des lits distribués que pour Secourir. Sans lit : pion sauté, message `RejectInput` `NoNonPrisonerBed` (ou `NoSlaveBed`).
- Message `SilentInput` : « Au lit : {n} pions ».

### Commun

- Chaque pion passe par `SquadOrders.Try` : l'échec d'un pion n'arrête pas les autres.
- Le message de réussite ne compte que les jobs acceptés par `TryTakeOrderedJob`. Aucun pris (membres tous occupés, patients déjà pris) : message `RejectInput` « Aucun membre libre n'a pu prendre cet ordre. »
- `SquadStatus.Invalidate()` après chaque ordre.

## 2. Interface (UX de Colonist Bar)

### Carte d'ordres

Nouvelle section « SOINS » entre « ASSAUT » et « MEMBRES », même gabarit que l'assaut : titre micro 18 px, puis trois boutons d'un tiers de largeur, hauteur `BtnH`.

| Bouton | Icône | Libellé |
|---|---|---|
| Secourir | `IconSet.Alert.Crit` (celle des pions à terre dans la bande d'alertes) | « Secourir (n) » |
| Soigner | `IconSet.Status.Medical` | « Soigner (n) » |
| Au lit | `IconSet.Status.Zzz` | « Au lit (n) » |

- n = nombre de patients (Secourir, Soigner) ou de membres concernés (Au lit), sans test de chemin ni de lit : le compte doit rester bon marché, il est refait à chaque image. Pour Soigner, le compte lit `PawnUiCache.Get(p).needsTend` (déjà en cache, celui de la bande d'alertes) ; le clic refait le test complet `ShouldBeTendedNowByPlayer`.
- Infobulle : ce que fait l'ordre, puis la liste des patients (au plus 6, puis « +{n} autres »), une ligne par pion « Nom · état », l'état reprenant les clés de la bande d'alertes (`RCB_Cond_BleedingOut`, `RCB_Cond_Downed`, `RCB_Cond_NeedsTend`). Puis la touche du raccourci (`SquadHotkeys.WithKey`).
- Grisé, avec la raison sous l'infobulle (`Tip`) :
  - aucun membre apte : `RCB_Orders_NoMember` ;
  - Secourir sans patient : « Personne n'est à terre sur cette carte. » ;
  - Soigner sans patient : « Personne n'a besoin de soins sur cette carte. » ;
  - Soigner sans soigneur possible : « Aucun membre présent ne peut soigner. » ;
  - Au lit sans membre concerné : « Aucun membre n'a besoin de repos médical. »
- `ContentHeight` grandit de `10 + 18 + BtnH`.
- Coût par image : la section calcule une seule fois patients et membres concernés, et passe les listes aux trois boutons et au survol.

### Aperçu sur la carte

Comme les postes de combat : survoler un bouton entoure sur la carte la case de chaque patient (Secourir, Soigner) ou de chaque membre concerné (Au lit), contour `GenDraw.DrawFieldEdges` à la couleur de l'escouade. Pas de ligne : l'appariement n'est calculé qu'au clic (tests de chemin et de lit trop chers pour chaque image). Même transmission que `SquadStations.Hover` / `DrawHover`, appelée depuis `BarSquads.GameComponentUpdate`.

### Raccourcis

Trois `KeyBindingDef` sans touche par défaut, catégorie `RCB_Squads` : `RCB_SquadRescue`, `RCB_SquadTend`, `RCB_SquadToBed`. Même règle que les sept existants : escouade de chaque pion sélectionné, une fois par escouade.

## 3. Code

| Fichier | Rôle |
|---|---|
| `SquadMedical.cs` (nouveau) | Classe statique sur le modèle de `SquadStations` : `Rescue(index)`, `Tend(index)`, `ToBed(index)`, recherche des patients et des membres concernés, appariement, `Hover`, `DrawHover`. |
| `SquadOrderCard.cs` | Section « Soins » § 2. |
| `BarSquads.cs` | `GameComponentUpdate` appelle aussi `SquadMedical.DrawHover()`. |
| `SquadHotkeys.cs`, `Defs/KeyBindings_Squads.xml` | Trois raccourcis, trois valeurs de `SquadHotkeys.Key`. |

Commentaires en tête des nouveaux blocs, en anglais, une phrase sur le pourquoi, comme les fichiers d'ordres existants.

## 4. Textes

- Anglais : `Languages/English/Keyed/RadiusColonistBar.xml` ; français : `Languages/French/Keyed/RadiusColonistBarPlus.xml` ; raccourcis en français : `Languages/French/DefInjected/KeyBindingDef/`.
- Clés préfixées `RCB_Medical_` : titre, trois libellés, trois infobulles, raisons des boutons grisés, « +{n} autres », trois messages.
- Les clés d'origine reprises (`RCB_Cond_*`, `NoNonPrisonerBed`) ne sont pas redéfinies dans le fork.
- Documentation : section « Soins » dans `README.md`, `CHANGELOG.md` « 1.2.0 — non publiée », `docs/ROADMAP.md`. `About.xml` et `docs/WORKSHOP.md` à la publication.

## 5. Hors périmètre

- Prisonniers, animaux, invités comme patients.
- Opérations chirurgicales, transfusions, nourrir un patient.
- Secours ou soins automatiques et continus (comportement de Colony Groups avec un `DutyDef`) : contraire à la règle « pas de `Lord` ni de `DutyDef` ». Un clic = une affectation.
- Lignes soigneur-patient dans l'aperçu.
- Repère médical sur l'onglet d'escouade : la bande d'alertes nomme déjà les pions à terre et à soigner.

## 6. Vérification

Build sans nouvel avertissement (`deploy.sh`). Contrôles en jeu :

1. Deux membres à terre, trois membres valides, deux lits médicaux : « Secourir (2) » ; deux secouristes différents emportent chacun un blessé dans un lit médical ; le troisième membre ne bouge pas.
2. Recliquer « Secourir » pendant le secours : personne n'est réaffecté.
3. Aucun lit libre : le message vanilla « pas de lit » s'affiche, aucun job. Un lit médical double et trois blessés : deux secourus dans ce lit, le troisième ailleurs ou message.
11. Un bébé dans la colonie : il ne compte pas dans « Secourir (n) ».
12. Un esclave à terre : il est porté dans un lit d'esclave.
13. « Au lit », puis « Soigner » : le patient reste au lit, soigné, jusqu'à guérison.
14. Un colon hors escouade emporte déjà un blessé : « Secourir » n'envoie personne vers ce blessé.
4. Trois blessés, un seul membre médecin : « Soigner (3) » ; le médecin soigne le patient le plus urgent, avec un médicament de la réserve si la règle du patient l'autorise.
5. Aucun médecin dans l'escouade : « Soigner » grisé, infobulle « Aucun membre présent ne peut soigner. »
6. Un membre blessé mobilisé : « Au lit (1) » ; il est démobilisé et va au lit médical jusqu'à guérison.
7. Assaut en cours, puis Secourir ou Soigner : les membres envoyés sortent de l'assaut, les autres continuent.
8. Survol des trois boutons : contours sur les patients ou les membres ; rien sans survol.
9. Raccourcis assignés : même effet que les boutons pour l'escouade des pions sélectionnés.
10. Colon hors escouade à terre : il est secouru après les membres de l'escouade.
