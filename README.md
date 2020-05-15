# WoW_Mapper
 
Creates minimaps for http://nyarly.fr/ExplorationReboot/CommunityMap/
Will create for each maps :
- area id (leaflet tiles)
- unknown (zone leaflet tiles
- minimap (leaflet tiles)
- impass map (leaflet tiles)
- models for each tiles (json)
- wdt info (json)
- death areas (json)
- area id info (json)
- wdt borders (json)
- unreferenced info (json)

Adds each map to a unique version.json file.

Needs :
- area table db2
- map db2
- listfile

Requires netvips, casclib, sereniablp, newtonsoft json, from nuget

Modify casc folder paths in Program.cs.