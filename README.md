# Smart Stations

Mod Valheim (BepInEx + Jötunn) : les stations de production s'ouvrent comme un coffre, avec des cases au lieu de « cliquer pour ajouter ».

- Une fenêtre à trois zones : **Entrée** (minerai, bois, sève...), **Combustible** (charbon, bois...), **Produits** (lingots, charbon, hydromel...).
- Fonctionne avec toutes les stations du jeu basées sur le four (four à charbon, fonderie, haut fourneau, raffinerie d'eitr, moulin à vent, rouet...), le fermentateur, les stations de cuisson (grill, grill en fer, four à pain) et la ruche et le collecteur de sève (case Produits seulement). Les stations d'autres mods construites sur le même modèle marchent aussi.
- Les produits finis restent dans la station au lieu de tomber par terre (réglable).
- **Remplissage auto** depuis les coffres proches, activable station par station avec un bouton dans la fenêtre.
- Fonctionne en solo et sur serveur : seul le « propriétaire » réseau d'une station la modifie, aucun objet ne peut être dupliqué.

Voir `COMPILER.md` pour compiler, tester et créer le zip Thunderstore (`package.ps1`).

## Fabrication

- Les stations de fabrication (établi, forge, chaudron, table d'artisan...) utilisent aussi les matériaux des coffres à moins de 15 m de la station. Un **+N** vert à côté de chaque matériau indique ce que contiennent les coffres.
- La construction (marteau, houe, cultivateur) utilise les coffres à moins de 15 m du joueur.
- À côté du menu de fabrication : un champ **Rechercher** et un bouton par catégorie présente à la station. Avec Smart Chest installé, ce sont ses catégories (avec tes réglages d'objets).

## Utilisation

- **E** sur une station : ouvre la fenêtre (avec ton inventaire). **Maj + E** garde l'action d'origine du jeu.
- **Ajouter** : prends un objet dans ton inventaire et pose-le sur la fenêtre, ou fais un **clic droit** dessus dans l'inventaire. Il va tout seul dans la bonne case. Boutons **Tout ajouter** / **Remplir** : prend tout ce qui convient dans ton inventaire.
- **Reprendre** : clic sur une case = tout ce type d'objet, clic droit = un seul. **Tout prendre** vide les produits.
- **Remplissage auto : OUI / NON** : la station va chercher toute seule minerais et combustible dans les coffres à moins de 10 m (toutes les 5 s). Les stations de cuisson ne prennent que leur combustible, pour que la nourriture ne brûle jamais sans toi.

## Organisation du code

| Dossier | Rôle |
|---|---|
| `Stations/` | Vue commune d'une station (`StationView`), une classe par type de station du jeu (`SmelterStation`, `FermenterStation`, `CookingStationView`, `CollectorStation` pour la ruche et la sève), produits gardés dans le ZDO (`OutputStore`), RPC réseau (`StationRpc`). |
| `Patches/` | Patchs Harmony : ouverture avec E, texte de survol, produits gardés, clic droit dans l'inventaire. |
| `AutoFill/` | Remplissage automatique depuis les coffres proches. |
| `Crafting/` | Fabrication et construction depuis les coffres proches, catégories des recettes (intégrées ou Smart Chest), détection des mods concurrents. |
| `UI/` | Fenêtre de la station et ses cases. |

Pour ajouter un nouveau type de station : une classe qui hérite de `StationView`, un `Get(...)` dans `StationRegistry` et un patch d'ouverture.
