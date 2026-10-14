# Radius UI - Colonist Bar Plus

Mod RimWorld 1.6 : fork de **Radius UI - Colonist Bar** (astryl) avec des ordres de combat d'escouade. Il remplace Colonist Bar : désactivez-le. Incompatible avec [LTO] Colony Groups. Multijoueur non pris en charge.

- packageId : `mrbabilo.RadiusUI.ColonistBarPlus`
- Dépendances : Harmony et Radius UI Framework

Pour rester compatible avec les parties existantes, le fork garde le namespace `RadiusColonistBar`, la DLL `RadiusColonistBar.dll`, l'identifiant Harmony `astryl.radiusui.colonistbar` et les noms de classes du mod d'origine.

## Compiler

`deploy.sh` compile hors de l'arbre, copie la DLL dans `Assemblies/` et publie le mod dans le dossier `Mods` du jeu (chemins en tête du script).

## Licence

MIT, voir `LICENSE`. La notice d'astryl est conservée.
