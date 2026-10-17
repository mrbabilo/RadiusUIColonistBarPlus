# Page Workshop

Texte à coller dans la description de la page Steam Workshop (BBCode, en anglais comme `About.xml`). Le mod s'envoie depuis le jeu : mode développeur, liste des mods, bouton « Upload to Workshop ». Le jeu crée alors `About/PublishedFileId.txt` dans le dossier **publié** (`Mods/RadiusUIColonistBarPlus`). Le recopier aussitôt dans `About/` du projet et le commiter : sinon le prochain `deploy.sh` (`rsync --delete`) l'efface, et l'envoi suivant crée une seconde page au lieu de mettre à jour la première.

Titre : `Radius UI - Colonist Bar Plus`

```
A fork of [url=https://steamcommunity.com/sharedfiles/filedetails/?id=3788278207]Radius UI - Colonist Bar[/url] by astryl, with squad combat orders.

[h2]Important[/h2]
[list]
[*]Replaces Radius UI - Colonist Bar: [b]disable the original[/b]. A save made with Colonist Bar keeps its squads, kits and histories.
[*][b]Not compatible[/b] with [LTO] Colony Groups.
[*][b]Multiplayer is not supported.[/b]
[*]Requires Harmony and [url=https://steamcommunity.com/sharedfiles/filedetails/?id=3786107692]Radius UI Framework[/url].
[/list]

[h2]Squad orders[/h2]
Right-click a squad tab on the bar, or open a squad in the Command Center, to get its order card:
[list]
[*][b]Draft / Undraft[/b] the squad
[*][b]Fire at will / Hold fire[/b]
[*][b]Regroup[/b]: everyone runs to the squad leader
[*][b]Assault[/b]: the squad attacks on its own, targeting the strongest enemies, the weakest ones, or those fleeing. It takes cover like vanilla and attacks any enemy in range when none matches.
[*][b]Battle stations[/b]: four per squad. Save records where the present members stand; Go drafts the squad, stops its assault and sends each member sprinting to their cell. Hovering a station shows its cells on the map.
[*][b]Members[/b]: add the selected colonists to the squad, remove them, or select every member on the map. In the Command Center, a new squad takes the current selection.
[/list]
The card shows the squad's state, stays open after an order, can be dragged, and closes with its button, a click outside or Escape. An assault stops when you give a member an order, undraft them or switch off fire at will.

Without opening the card:
[list]
[*]a squad tab shows a dot when members are drafted, and a sword during an assault;
[*]a colonist taking part in an assault shows a sword on their portrait;
[*]seven key bindings (draft, fire at will, regroup, three assault modes, go to the active battle station) in Options > Controls > "Radius UI - Squads", with no default key.
[/list]

Orders only affect members on the current map. Mechanoids and colonists who are downed, in a mental break or in deathrest are left out.

[h2]Saves[/h2]
The mod adds a game component that remembers each colonist's assault mode. Battle stations are saved with the squads. Removing the mod mid-game gives one error on load; only assaults in progress and battle stations are lost.

[h2]From the original Colonist Bar[/h2]
Replaces the colonist bar with a single slab across the top of the screen: each colonist gets a portrait, their name, role and current task, and two meters you choose. An alert band underneath names whoever needs attention. A Command Center window handles health, weapons, loadouts and squads, and the bar folds away when you are not using it.

[h2]Credits[/h2]
[list]
[*]Radius UI - Colonist Bar by astryl (MIT), base of this mod.
[*][LTO] Colony Groups by DerekBickley: idea of group orders and reference behaviour. No code was copied.
[/list]

Source code and French notes: [url=https://github.com/mrbabilo/RadiusUIColonistBarPlus]github.com/mrbabilo/RadiusUIColonistBarPlus[/url]. MIT licence.
```
