# What's new in this fork

Added solution file and project. I have only `VS Community 2019`, so sorry, solution is for this studio and `Windows .Net 4.8`.

In order to open properly, you should set `COI_ROOT` environment variable to your Captain Of Industry Steam installation, and set your `PATH` variable to `%COI_ROOT%\Captain of Industry_Data\Managed`.

In order to run, place compiled `DataExtractor.dll` to folder `Users\[user]\AppData\Roaming\Captain of Industry\Mods\DataExtractor`

Image icons: use [AssetStudio](https://github.com/Perfare/AssetStudio)

- Updated for latest version `0.8.1a (b550)`, and migrated everything to `protosDb.All` getting rid of hardcoded ids, and automatically catching new items/buildings.
- Added products.json, product ids, recipe ids, product icon references.
- Added transports.json, conveyor belts throughput.
- Added details on farms, recipes, production, consumption.
- Added details on storages - item type to store.
- Added icons.

# Original REAMDE is below

# Captain Of Industry Data Export

This repo contains an export of the game data in JSON with the majority of relevant fields.

The current version of the mod only covers buildings/machines that are part of the production chain recipes. Thus, any buildings that are not part of production chaings (ramps, transports, retaining walls, etc) are not included.

The only exception is Vehicles and Ship upgrade parts which were specifically requested.

## Included Mod and Source Code

I have also included the Mod files and the source code, although in order to build from source you will need to setup the dev environment as only the main source code is included, no project or solution.

## Notes on Missing Data

This export does not contain everything. It does contains a majority of the constructable buildings and machines. 

### Missing Items

Most of the items NOT included are listed below, although some might be mising.

 * Ids.Buildings.TradeDock
 * Ids.Buildings.MineTower
 * Ids.Buildings.HeadquartersT1
 * Ids.Machines.Flywheel
 * Ids.Buildings.Beacon
 * Ids.Buildings.Clinic
 * Ids.Buildings.SettlementPillar
 * Ids.Buildings.SettlementFountain
 * Ids.Buildings.SettlementSquare1
 * Ids.Buildings.SettlementSquare2
 * Ids.Buildings.Shipyard
 * Ids.Buildings.Shipyard2
 * Ids.Buildings.VehicleRamp
 * Ids.Buildings.VehicleRamp2
 * Ids.Buildings.VehicleRamp3
 * Ids.Buildings.RetainingWallStraight1
 * Ids.Buildings.RetainingWallStraight4
 * Ids.Buildings.RetainingWallCorner
 * Ids.Buildings.RetainingWallCross
 * Ids.Buildings.RetainingWallTee
 * Ids.Buildings.BarrierStraight1
 * Ids.Buildings.BarrierCorner
 * Ids.Buildings.BarrierCross
 * Ids.Buildings.BarrierTee
 * Ids.Buildings.BarrierEnding
 * Ids.Buildings.StatueOfMaintenance
 * Ids.Buildings.StatueOfMaintenanceGolden
 * Ids.Buildings.TombOfCaptainsStage1
 * Ids.Buildings.TombOfCaptainsStage2
 * Ids.Buildings.TombOfCaptainsStage3
 * Ids.Buildings.TombOfCaptainsStage4
 * Ids.Buildings.TombOfCaptainsStage5
 * Ids.Buildings.TombOfCaptainsStageFinal