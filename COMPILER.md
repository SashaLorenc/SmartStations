# Compiler et tester Smart Stations sur ton PC

Même principe que Smart Chest : mêmes outils (SDK .NET, r2modman, Jötunn), même `Environment.props`.

## 0. Récupérer le code (une seule fois)

Dans PowerShell, dans le dossier où tu ranges tes projets (par ex. là où il y a déjà `SmartChest`) :

```
git clone https://github.com/SashaLorenc/SmartStations.git
cd SmartStations
```

Puis copie ton `Environment.props` de Smart Chest à la racine de `SmartStations` (il est identique : mêmes chemins Valheim, BepInEx et Jötunn).

## À chaque modification

```
git pull
dotnet build SmartStations/SmartStations.csproj -c Release
```

La DLL est copiée automatiquement dans `BepInEx/plugins/SmartStations/` de ton profil r2modman.

## Tester (fours et fermentateur)

Au lancement, `BepInEx/LogOutput.log` doit afficher `Smart Stations <version> chargé.` (ex. 1.1.0)

1. **Fonderie** : approche-toi, le survol affiche « [E] Ouvrir » et « [Maj + E] » + l'action d'origine. Appuie sur **E** : ton inventaire s'ouvre avec la fenêtre de la station au-dessus.
2. **Ajouter** : clique sur du minerai de cuivre dans ton inventaire, puis clique sur la zone **Entrée** : il y entre (jusqu'à 10). Essaie aussi le **clic droit** sur du charbon dans l'inventaire : il va dans **Combustible**. Boutons **Tout ajouter** et **Remplir**.
3. **Reprendre** : clic sur la case du minerai = tout revient dans l'inventaire ; clic droit = un seul.
4. **Produits** : laisse fondre, les lingots apparaissent dans **Produits** au lieu de tomber par terre. Clic dessus ou **Tout prendre**.
5. **Remplissage auto** : mets du minerai et du charbon dans un coffre à moins de 10 m, clique **Remplissage auto : NON** pour passer à OUI, ferme la fenêtre : en 5 s la fonderie se remplit depuis le coffre.
6. **Four à charbon, moulin à vent, rouet, raffinerie d'eitr** : même fenêtre, sans case Combustible quand la station n'en utilise pas.
7. **Fermentateur** : une seule entrée ; pendant la fermentation la ligne d'état affiche le pourcentage et le temps restant, puis les hydromels apparaissent dans **Produits**.
8. **Détruire** une fonderie qui contient des produits : tout tombe par terre, rien n'est perdu.

## Tester la version 1.0.0 (cuisson, ruche, sève)

1. **Grill** : allume un feu, pose un grill dessus, **E** : ta viande crue va dans **Entrée** (clic droit dans l'inventaire ou glisser-déposer). La ligne d'état donne le pourcentage et le temps restant. Une fois cuite, elle passe dans **Produits** et la ligne d'état te prévient en orange qu'il faut la retirer avant qu'elle brûle. Prendre un plat cuit donne de la compétence Cuisine.
2. **Four à pain** : même chose avec en plus la case **Combustible** (bois). Le bouton **Remplissage auto** y existe mais ne remet que du bois, jamais de nourriture.
3. **Ruche** : **E** ouvre une fenêtre avec seulement la case **Produits** (le miel), et une ligne d'état (bon biome, assez d'espace, nuit, nombre de pots prêts). Clic sur le miel : il arrive dans l'inventaire.
4. **Collecteur de sève** (posé sur une racine ancienne) : pareil, avec la sève.
5. **Maj + E** sur chacune de ces stations : action normale du jeu.

## Tester la version 1.1.0 (fabrication)

1. **Établi depuis les coffres** : mets du bois dans un coffre à côté de l'établi et vide ton inventaire. Ouvre l'établi : les recettes en bois sont fabricables, et chaque matériau affiche un **+N** vert (ce qu'il y a dans les coffres). Fabrique : le bois est retiré du coffre.
2. **Mélange** : avec 5 bois sur toi et 20 dans le coffre, une recette à 10 bois prend tes 5 et 5 dans le coffre.
3. **Construction** : marteau en main près du coffre, une pièce en bois est constructible et affiche **+N**. Pose-la : le bois part du coffre.
4. **Catégories** : à droite du menu de fabrication, un champ **Rechercher** et des boutons (Tout, Armes, Armures...). Seules les catégories présentes à la station apparaissent. Clique sur une catégorie, puis tape un nom (ex. « hache »). Pendant la saisie, E et Tab ne ferment pas l'inventaire.
5. **Avec Smart Chest** : les boutons reprennent tes catégories Smart Chest. Le log affiche `Menu de fabrication : catégories de Smart Chest utilisées.`
6. Si les boutons chevauchent quelque chose : `[Fabrication] PositionCategories` (Dessus, Dessous, Gauche, Droite) et `DecalageX / DecalageY`.

Multijoueur : seuls les coffres dont ta partie est « propriétaire » réseau et que personne n'a ouverts sont utilisés (en solo : tous). Si un patch ne s'applique pas après une mise à jour du jeu, le log affiche `Patch ... impossible` : envoie-moi la ligne.

## Créer le zip Thunderstore

Les fichiers de la page du mod sont dans `Thunderstore/` (manifest.json, README.md en anglais, CHANGELOG.md, icon.png 256x256).

1. Pour une nouvelle version : change `version_number` dans `Thunderstore/manifest.json` **et** `ModVersion` dans `SmartStations/Plugin.cs` (les deux doivent être identiques, le script vérifie), et ajoute une section dans `CHANGELOG.md`.
2. Depuis la racine du dépôt :
   ```
   powershell -ExecutionPolicy Bypass -File package.ps1
   ```
3. Le zip est créé dans `release/SmartStations-<version>.zip` (dossier ignoré par git). Envoie-le sur https://thunderstore.io/c/valheim/create/ comme pour Smart Chest.

Contenu du zip : `manifest.json`, `README.md`, `CHANGELOG.md`, `icon.png` à la racine, et `plugins/SmartStations.dll` + `plugins/Translations/`.

## Réglages

Dans `BepInEx/config/sashalorenc.SmartStations.cfg` (créé au premier lancement) :

- `[Stations]` : `Fours`, `Fermentateur`, `Cuisson`, `RucheEtSeve` (activer la fenêtre), `DistanceMax`.
- `[Produits]` : `GarderDansLaStation` (true/false), `Capacite` (100 par défaut ; au-delà ça tombe par terre).
- `[RemplissageAuto]` : `Autorise` (interrupteur général), `ActifParDefaut`, `Rayon` (10 m), `Intervalle` (5 s).
- `[Fabrication]` : `DepuisLesCoffres`, `ConstructionDepuisLesCoffres`, `Rayon` (15 m), `Categories`, `CategoriesSmartChest`, `PositionCategories`, `DecalageX`, `DecalageY`.

## Bon à savoir

- Le remplissage auto ne puise que dans les coffres posés par un joueur, auxquels tu as accès, que personne n'a ouverts, et dont ta partie est « propriétaire » réseau (en solo : tous). C'est ce qui empêche toute duplication sur serveur.
- Si tu désinstalles le mod, les produits encore gardés dans une station sont perdus : vide-les avant.
- Pas encore géré : la manette dans la fenêtre.

## En cas d'erreur

Copie-colle l'erreur complète (terminal ou `LogOutput.log`) dans le fil du projet.
