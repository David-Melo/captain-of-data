/*
 * Data Extraction Mod
 * 
 * -------------------------------------
 * DESCRIPTION
 * -------------------------------------
 * 
 * This is a simple and somewhat messy mod that extracts in game data.
 * It was designed to export the game data in JSON with the majority of relevant fields.
 * 
 * The current version of the mod only covers buildings/machines that are part of the production chain recipes.
 * Thus, any buildings that are not part of production chaings (ramps, transports, retaining walls, etc) are not included.
 * The only exception is Vehicles and Ship upgrade parts which were specifically requested by someone.
 * 
 * Each object in the game has a Proto (prototype) that defines the object's properties and capabilities.
 * These prototypes are registerd with the game's Prototype Database (ProtoDB).
 * 
 * What this mod does is fetch the Protos for the ProtoDB and read the relevant properties and formats and exports them into JSON format.
 * 
 * Not All Objects share the same prototype, while most Machines do share the same prototype (MachineProto) many of the other specialized buildings
 * have their own unique protos.
 * 
 * There are two methods used to get the Protos from the DB, The first uses a list of Proto IDs to get the Protos from the DB, this requires having
 * a list of the Ids to lookup. These IDs can be found in Mafi.Base.Ids. With this I could then get each individual instance and lookup its Prototype.
 * 
 * The second methods is more simple.
 * 
 * After getting more familiar familiar with the code, I realized that you could just request all instances of a specific Proto from the DB, this
 * eliminates a few steps, mainly having the need of specifying a list of Ids.
 * 
 * The first part of the code uses the old method and I have not converted it to the new format.
 * 
 * -------------------------------------
 * JSON FORMATTING AND EXPORTING
 * -------------------------------------
 * 
 * I could not get the usual JSON serialization methods to work so I created a simple yet sloppy implementation to have proper JSON formatting.
 * There are different types of buildings/machines and thus there are several functions for formatting each kinds. Those are the first functions
 * you will see in the code below.
 * 
 * The resulting JSON files are exported to C:/temp but can be changed below, the folder might need to exists beforehand to prevent possible errors.
 * 
 * -------------------------------------
 * LIST OF MISSING ITEMS
 * -------------------------------------
 * Ids.Buildings.TradeDock
 * 
 * Ids.Buildings.MineTower
 * 
 * Ids.Buildings.HeadquartersT1
 * 
 * Ids.Machines.Flywheel
 * 
 * Ids.Buildings.Beacon
 * 
 * Ids.Buildings.Clinic
 * 
 * Ids.Buildings.SettlementPillar
 * Ids.Buildings.SettlementFountain
 * Ids.Buildings.SettlementSquare1
 * Ids.Buildings.SettlementSquare2
 * 
 * Ids.Buildings.Shipyard
 * Ids.Buildings.Shipyard2
 * 
 * Ids.Buildings.VehicleRamp
 * Ids.Buildings.VehicleRamp2
 * Ids.Buildings.VehicleRamp3
 * 
 * Ids.Buildings.RetainingWallStraight1
 * Ids.Buildings.RetainingWallStraight4
 * Ids.Buildings.RetainingWallCorner
 * Ids.Buildings.RetainingWallCross
 * Ids.Buildings.RetainingWallTee
 * 
 * Ids.Buildings.BarrierStraight1
 * Ids.Buildings.BarrierCorner
 * 
 * Ids.Buildings.BarrierCross
 * Ids.Buildings.BarrierTee
 * Ids.Buildings.BarrierEnding
 * 
 * Ids.Buildings.StatueOfMaintenance
 * Ids.Buildings.StatueOfMaintenanceGolden
 * 
 * Ids.Buildings.TombOfCaptainsStage1
 * Ids.Buildings.TombOfCaptainsStage2
 * Ids.Buildings.TombOfCaptainsStage3
 * Ids.Buildings.TombOfCaptainsStage4
 * Ids.Buildings.TombOfCaptainsStage5
 * Ids.Buildings.TombOfCaptainsStageFinal
 * 
 */

using System.IO;
using System.Collections.Generic;
using Mafi;
using Mafi.Base;
using Mafi.Core;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;
using Mafi.Core.Products;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Datacenters;
using Mafi.Core.Factory.NuclearReactors;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Maintenance;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Fleet;
using Mafi.Core.Vehicles.Excavators;
using Mafi.Core.Vehicles.TreeHarvesters;
using Mafi.Core.Buildings.Farms;
using Mafi.Core.Buildings.Cargo.Modules;
using Mafi.Core.Buildings.Cargo;
using Mafi.Core.Buildings.ResearchLab;
using Mafi.Core.Buildings.VehicleDepots;
using Mafi.Core.Buildings.FuelStations;
using Mafi.Core.Buildings.Storages;
using Mafi.Core.Buildings.Settlements;
using Mafi.Core.Buildings.SpaceProgram;
using Mafi.Core.Buildings.Waste;
using Mafi.Core.Buildings.RainwaterHarvesters;
using Mafi.Collections.ImmutableCollections;
using Mafi.Collections.ReadonlyCollections;
using Mafi.Base.Prototypes.Machines.PowerGenerators;
using System.Reflection;
using Mafi.Core.Buildings.Mine;
using UnityEngine;
using Mafi.Core.Research;
using Mafi.Core.UnlockingTree;
using Mafi.Core.World.Contracts;

namespace DataExtractorMod {
	public sealed class DataExtractor : IMod
    {
        public string Name => "Data Extractor Mod By ItsDesm";

        public int Version => 1;

        public bool IsUiOnly => false;

        public static readonly string MOD_ROOT_DIR_PATH = new FileSystemHelper().GetDirPath(FileType.Mod, false);
        public static readonly string MOD_DIR_PATH = Path.Combine(MOD_ROOT_DIR_PATH, "DataExtractor");
        public static readonly string PLUGIN_DIR_PATH = Path.Combine(MOD_DIR_PATH, "Plugins");

        public DataExtractor() {
            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            Log.Info(MOD_ROOT_DIR_PATH);
            Log.Info(MOD_DIR_PATH);
            Log.Info(PLUGIN_DIR_PATH);
            Log.Info("Loaded Data Extractor Mod By ItsDesm");
        }



        /*
         * -------------------------------------
         * JSON Formatters For Specific Machine Types
         * -------------------------------------
        */

        public string MakeRecipeIOJsonObject(
            string name,
            string quantity
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"quantity\":{quantity}");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");

            return obj.ToString();
        }

        public string MakeMachineJsonObject (
            string id,
            string name, 
            string category, 
            string workers,
            string maintenance_cost_units,
            string maintenance_cost_quantity,
            string electricity_consumed,
            string electricity_generated,
            string computing_consumed,
            string computing_generated,
            string capacity,
            string unity_cost,
            string research_speed,
            string build_costs,
            string recipes
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"id\":\"{id}\"");
            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"category\":\"{category}\"");
            props.Add($"\"workers\":{workers}");
            props.Add($"\"maintenance_cost_units\":\"{maintenance_cost_units}\"");
            props.Add($"\"maintenance_cost_quantity\":{maintenance_cost_quantity}");
            props.Add($"\"electricity_consumed\":{electricity_consumed}");
            props.Add($"\"electricity_generated\":{electricity_generated}");
            props.Add($"\"computing_consumed\":{computing_consumed}");
            props.Add($"\"computing_generated\":{computing_generated}");
            props.Add($"\"storage_capacity\":{capacity}");
            props.Add($"\"unity_cost\":{unity_cost}");
            props.Add($"\"research_speed\":{research_speed}");
            props.Add($"\"build_costs\":[{build_costs}]");
            props.Add($"\"recipes\":[{recipes}]");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }

        public string MakeRecipeJsonObject(
            string name,
            string duration,
            string inputs,
            string outputs
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"duration\":{duration}");
            props.Add($"\"inputs\":[{inputs}]");
            props.Add($"\"outputs\":[{outputs}]");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }

        public string MakeVehicleJsonObject(
            string name,
            string costs
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"costs\":[{costs}]");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }

        public string MakeVehicleProductJsonObject(
            string product,
            string quantity
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"product\":\"{product}\"");
            props.Add($"\"quantity\":{quantity}");            

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }

        public string MakeEngineJsonObject(
            string name,
            string capacity,
            string crew,
            string costs
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"fuel_capacity\":{capacity}");
            props.Add($"\"extra_crew_needed\":{crew}");
            props.Add($"\"costs\":[{costs}]");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }
        
        public string MakeGunJsonObject(
            string name,
            string range,
            string damage,
            string crew,
            string costs
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"range\":{range}");
            props.Add($"\"damage\":{damage}");
            props.Add($"\"extra_crew_needed\":{crew}");
            props.Add($"\"costs\":[{costs}]");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }
        
        public string MakeArmorJsonObject(
            string name,
            string hp,
            string armor,
            string costs
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"hp_upgrade\":{hp}");
            props.Add($"\"armor_upgrade\":{armor}");
            props.Add($"\"costs\":[{costs}]");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }

        public string MakeBridgeJsonObject(
            string name,
            string hp,
            string radar,
            string crew,
            string costs
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"hp_upgrade\":{hp}");
            props.Add($"\"radar_upgrade\":{radar}");
            props.Add($"\"extra_crew_needed\":{crew}");
            props.Add($"\"costs\":[{costs}]");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }
        
        public string MakeTankJsonObject(
            string name,
            string added_capacity,
            string costs
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"added_capacity\":{added_capacity}");
            props.Add($"\"costs\":[{costs}]");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }

        public string MakeTerrainMaterialJsonObject(
            string id,
            string name,
            string mined_product,
            string mining_hardness,
            string mined_quantity_per_tile_cubed,
            string disruption_recovery_time,
            string is_hardened_floor,
            string max_collapse_height_diff,
            string min_collapse_height_diff,
            string mined_quantity_mult,
            string vehicle_traversal_cost
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"id\":\"{id}\"");
            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"mined_product\":\"{mined_product}\"");
            props.Add($"\"mining_hardness\":\"{mining_hardness}\"");
            props.Add($"\"mined_quantity_per_tile_cubed\":{mined_quantity_per_tile_cubed}");
            props.Add($"\"disruption_recovery_time\":{disruption_recovery_time}");
            props.Add($"\"is_hardened_floor\":{is_hardened_floor}");
            props.Add($"\"max_collapse_height_diff\":{max_collapse_height_diff}");
            props.Add($"\"min_collapse_height_diff\":{min_collapse_height_diff}");
            props.Add($"\"mined_quantity_mult\":\"{mined_quantity_mult}\"");
            props.Add($"\"vehicle_traversal_cost\":{vehicle_traversal_cost}");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }

         public string MakeContractJsonObject(
            string id,
            string product_to_buy_name,
            string product_to_buy_quantity,
            string product_to_pay_with_name,
            string product_to_pay_with_quantity,
            string unity_per_month,
            string unity_per_100_bought,
            string unity_to_establish,
            string min_reputation_required
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"id\":\"{id}\"");
            props.Add($"\"product_to_buy_name\":\"{product_to_buy_name}\"");
            props.Add($"\"product_to_buy_quantity\":{product_to_buy_quantity}");
            props.Add($"\"product_to_pay_with_name\":\"{product_to_pay_with_name}\"");
            props.Add($"\"product_to_pay_with_quantity\":{product_to_pay_with_quantity}");
            props.Add($"\"unity_per_month\":{unity_per_month}");
            props.Add($"\"unity_per_100_bought\":{unity_per_100_bought}");
            props.Add($"\"unity_to_establish\":{unity_to_establish}");
            props.Add($"\"min_reputation_required\":{min_reputation_required}");


            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }

        public string MakeResearchJsonObject(
            string id,
            string name,
            string difficulty,
            string total_steps
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"id\":\"{id}\"");
            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"difficulty\":{difficulty}");
            props.Add($"\"total_steps\":{total_steps}");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }

        /*
         * -------------------------------------
         * Main Mod Code
         * -------------------------------------
         * The logic runs within the RegisterDepencies stage due to me not being able to get the code running correctly otherwise.
         * This feels like it might not be the right place for it, but it works so...
        */

        public void RegisterDependencies(DependencyResolverBuilder depBuilder, ProtosDb protosDb, bool gameWasLoaded)
        {

            string game_version = typeof(Mafi.Base.BaseMod).GetTypeInfo().Assembly.GetName().Version.ToString();

            /*
             * -------------------------------------
             * Part 1  - Ship Upgrades. Uses ProtoID List Method
             * -------------------------------------
            */

            List<string> upgradeItems = new List<string> { };
            List<string> engineItems = new List<string> { };
            List<string> gunItems = new List<string> { };
            List<string> armorItems = new List<string> { };
            List<string> bridgeItems = new List<string> { };
            List<string> tankItems = new List<string> { };

            FleetEntityPartProto.ID[] engines = new FleetEntityPartProto.ID[] {
                Ids.Fleet.Engines.EngineT1,
                Ids.Fleet.Engines.EngineT2,
                Ids.Fleet.Engines.EngineT3
            };
            foreach (FleetEntityPartProto.ID upgradeId in engines)
            {

                try
                {

                    Option<FleetEnginePartProto> item = protosDb.Get<FleetEnginePartProto>(upgradeId);

                    List<string> vehicleProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Value.Value.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        vehicleProducts.Add(vehicleProductJson);
                    }

                    string vehicleJson = MakeEngineJsonObject(
                        item.Value.Strings.Name.ToString(),
                        item.Value.FuelCapacity.ToString(),
                        item.Value.ExtraCrew.BonusValue.ToString(),
                        vehicleProducts.JoinStrings(",")
                    );
                    engineItems.Add(vehicleJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + upgradeId.ToString());
                    Log.Info("###################################################");
                }

            }
            upgradeItems.Add($"\"engines\":[{engineItems.JoinStrings(",")}]");

            FleetWeaponProto.ID[] guns = new FleetWeaponProto.ID[] {
                Ids.Fleet.Weapons.Gun0,
                Ids.Fleet.Weapons.Gun1,
                Ids.Fleet.Weapons.Gun2,
                Ids.Fleet.Weapons.Gun3,
                Ids.Fleet.Weapons.Gun1Rear,
                Ids.Fleet.Weapons.Gun1Rear,
                Ids.Fleet.Weapons.Gun3Rear
            };
            foreach (FleetEntityPartProto.ID upgradeId in guns)
            {

                try
                {

                    Option<FleetWeaponProto> item = protosDb.Get<FleetWeaponProto>(upgradeId);

                    List<string> vehicleProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Value.Value.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        vehicleProducts.Add(vehicleProductJson);
                    }

                    string vehicleJson = MakeGunJsonObject(
                        item.Value.Strings.Name.ToString(),
                        item.Value.Range.ToString(),
                        item.Value.Damage.ToString(),
                        item.Value.ExtraCrew.BonusValue.ToString(),
                        vehicleProducts.JoinStrings(",")
                    );
                    gunItems.Add(vehicleJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + upgradeId.ToString());
                    Log.Info("###################################################");
                }

            }
            upgradeItems.Add($"\"weapons\":[{gunItems.JoinStrings(",")}]");

            FleetWeaponProto.ID[] armor = new FleetWeaponProto.ID[] {
                Ids.Fleet.Armor.ArmorT1,
                Ids.Fleet.Armor.ArmorT2
            };
            foreach (FleetEntityPartProto.ID upgradeId in armor)
            {

                try
                {

                    Option<UpgradeHullProto> item = protosDb.Get<UpgradeHullProto>(upgradeId);

                    List<string> vehicleProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Value.Value.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        vehicleProducts.Add(vehicleProductJson);
                    }

                    string vehicleJson = MakeArmorJsonObject(
                        item.Value.Strings.Name.ToString(),
                        "0",
                        "0",
                        vehicleProducts.JoinStrings(",")
                    );
                    armorItems.Add(vehicleJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + upgradeId.ToString());
                    Log.Info("###################################################");
                }

            }
            upgradeItems.Add($"\"armor\":[{armorItems.JoinStrings(",")}]");

            FleetWeaponProto.ID[] bridges = new FleetWeaponProto.ID[] {
                Ids.Fleet.Bridges.BridgeT1,
                Ids.Fleet.Bridges.BridgeT2,
                Ids.Fleet.Bridges.BridgeT3
            };
            foreach (FleetEntityPartProto.ID upgradeId in bridges)
            {

                try
                {

                    Option<FleetBridgePartProto> item = protosDb.Get<FleetBridgePartProto>(upgradeId);

                    List<string> vehicleProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Value.Value.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        vehicleProducts.Add(vehicleProductJson);
                    }

                    string vehicleJson = MakeBridgeJsonObject(
                        item.Value.Strings.Name.ToString(),
                        "0",
                        "0",
                        item.Value.ExtraCrew.BonusValue.ToString(),
                        vehicleProducts.JoinStrings(",")
                    );
                    bridgeItems.Add(vehicleJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + upgradeId.ToString());
                    Log.Info("###################################################");
                }

            }
            upgradeItems.Add($"\"bridges\":[{bridgeItems.JoinStrings(",")}]");

            FleetWeaponProto.ID[] tanks = new FleetWeaponProto.ID[] {
                Ids.Fleet.FuelTanks.FuelTankT1
            };
            foreach (FleetEntityPartProto.ID upgradeId in tanks)
            {

                try
                {

                    Option<FleetFuelTankPartProto> item = protosDb.Get<FleetFuelTankPartProto>(upgradeId);

                    List<string> vehicleProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Value.Value.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        vehicleProducts.Add(vehicleProductJson);
                    }
                    
                    string vehicleJson = MakeTankJsonObject(
                        item.Value.Strings.Name.ToString(),
                        item.Value.AddedFuelCapacity.ToString(),
                        vehicleProducts.JoinStrings(",")
                    );
                    tankItems.Add(vehicleJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + upgradeId.ToString());
                    Log.Info("###################################################");
                }

            }
            upgradeItems.Add($"\"fuel_tanks\":[{tankItems.JoinStrings(",")}]");

            File.WriteAllText("c:/temp/ship_upgrades.json", $"{{\"game_version\":\"{game_version}\",{upgradeItems.JoinStrings(",")}}}");

            /*
             * -------------------------------------
             * Part 2  - Vehicles. Uses ProtoID List Method
             * -------------------------------------
            */

            List<string> vehicleItems = new List<string> { };

            DynamicEntityProto.ID[] vehicles = new DynamicEntityProto.ID[] {
                Ids.Vehicles.TruckT1.Id,
                Ids.Vehicles.TruckT2.Id,
                Ids.Vehicles.TruckT3Fluid.Id,
                Ids.Vehicles.TruckT3Loose.Id,
            };
            foreach (DynamicEntityProto.ID vehicleId in vehicles)
            {

                try
                {

                    Option<Mafi.Core.Vehicles.Trucks.TruckProto> vehicle = protosDb.Get<Mafi.Core.Vehicles.Trucks.TruckProto>(vehicleId);

                    List<string> vehicleProducts = new List<string> { };

                    foreach (ProductQuantity cost in vehicle.Value.CostToBuild.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        vehicleProducts.Add(vehicleProductJson);
                    }

                    string vehicleJson = MakeVehicleJsonObject(
                        vehicle.Value.Strings.Name.ToString(),
                        vehicleProducts.JoinStrings(",")
                    );
                    vehicleItems.Add(vehicleJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + vehicleId.ToString());
                    Log.Info("###################################################");
                }

            }

            DynamicEntityProto.ID[] excavators = new DynamicEntityProto.ID[] {
                Ids.Vehicles.ExcavatorT1,
                Ids.Vehicles.ExcavatorT2,
                Ids.Vehicles.ExcavatorT3,
            };
            foreach (DynamicEntityProto.ID excavatorId in excavators)
            {

                try
                {

                    Option<ExcavatorProto> vehicle = protosDb.Get<ExcavatorProto>(excavatorId);

                    List<string> vehicleProducts = new List<string> { };

                    foreach (ProductQuantity cost in vehicle.Value.CostToBuild.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        vehicleProducts.Add(vehicleProductJson);
                    }

                    string vehicleJson = MakeVehicleJsonObject(
                        vehicle.Value.Strings.Name.ToString(),
                        vehicleProducts.JoinStrings(",")
                    );
                    vehicleItems.Add(vehicleJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + excavatorId.ToString());
                    Log.Info("###################################################");
                }

            }

            DynamicEntityProto.ID[] harvesters = new DynamicEntityProto.ID[] {
                Ids.Vehicles.TreeHarvesterT1,
                Ids.Vehicles.TreeHarvesterT2
            };
            foreach (DynamicEntityProto.ID harvesterId in harvesters)
            {

                try
                {

                    Option<TreeHarvesterProto> vehicle = protosDb.Get<TreeHarvesterProto>(harvesterId);

                    List<string> vehicleProducts = new List<string> { };

                    foreach (ProductQuantity cost in vehicle.Value.CostToBuild.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        vehicleProducts.Add(vehicleProductJson);
                    }

                    string vehicleJson = MakeVehicleJsonObject(
                        vehicle.Value.Strings.Name.ToString(),
                        vehicleProducts.JoinStrings(",")
                    );
                    vehicleItems.Add(vehicleJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + harvesterId.ToString());
                    Log.Info("###################################################");
                }

            }

            File.WriteAllText("c:/temp/vehicles.json", $"{{\"game_version\":\"{game_version}\",\"vehicles\":[{vehicleItems.JoinStrings(",")}]}}");

            /*
             * -------------------------------------
             * Part 3  - Power Generation Machines. (Behave Uniquely) Uses ProtoID List Method
             * -------------------------------------
            */

            List<string> machineItems = new List<string> { };

            // -------------------------
            // Turbines
            // -------------------------

            MechPowerGeneratorFromProductProto.ID[] turbines = new MechPowerGeneratorFromProductProto.ID[] {
                Ids.Machines.TurbineLowPressT2,
                Ids.Machines.TurbineLowPress,
                Ids.Machines.TurbineHighPressT2,
                Ids.Machines.TurbineHighPress
            };
            foreach (MechPowerGeneratorFromProductProto.ID machineId in turbines)
            {
                try
                {

                    Option<MechPowerGeneratorFromProductProto> generator = protosDb.Get<MechPowerGeneratorFromProductProto>(machineId);

                    string id = generator.Value.Id.ToString();
                    string name = generator.Value.Strings.Name.ToString();
                    string category = "";
                    string workers = generator.Value.Costs.Workers.ToString();
                    string maintenance_cost_units = generator.Value.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = generator.Value.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in generator.Value.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }
;
                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in generator.Value.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> recipeItems = new List<string> { };

                    var duration = (generator.Value.Recipe.Duration / 10);
                    var inputs = generator.Value.Recipe.AllUserVisibleInputs;
                    var outputs = generator.Value.Recipe.AllUserVisibleOutputs;

                    string recipe_name = generator.Value.Recipe.Id.ToString();
                    string recipe_duration = duration.ToString();

                    List<string> inputItems = new List<string> { };
                    List<string> outputItems = new List<string> { };

                    inputs.ForEach(delegate (RecipeInput input)
                    {
                        Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                        string machineRecipeInputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                        inputItems.Add(machineRecipeInputJson);
                    });

                    outputs.ForEach(delegate (RecipeOutput input)
                    {
                        Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                        string machineRecipeOutputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                        outputItems.Add(machineRecipeOutputJson);
                    });

                    string machineRecipeJson = MakeRecipeJsonObject(
                        recipe_name,
                        recipe_duration,
                        inputItems.JoinStrings(","),
                        outputItems.JoinStrings(",")
                    );
                    recipeItems.Add(machineRecipeJson);

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(",")
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + machineId.ToString());
                    Log.Info("###################################################");
                }
            }

            // -------------------------
            // Generators
            // -------------------------

            ElectricityGeneratorFromMechPowerProto.ID[] generators = new ElectricityGeneratorFromMechPowerProto.ID[] {
                Ids.Machines.PowerGeneratorT1,
                Ids.Machines.PowerGeneratorT2,
            };
            foreach (ElectricityGeneratorFromMechPowerProto.ID machineId in generators)
            {
                try
                {

                    Option<ElectricityGeneratorFromMechPowerProto> generator = protosDb.Get<ElectricityGeneratorFromMechPowerProto>(machineId);

                    string id = generator.Value.Id.ToString();
                    string name = generator.Value.Strings.Name.ToString();
                    string category = "";
                    string workers = generator.Value.Costs.Workers.ToString();
                    string maintenance_cost_units = generator.Value.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = generator.Value.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in generator.Value.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }
;
                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in generator.Value.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> recipeItems = new List<string> { };

                    var duration = (generator.Value.Recipe.Duration / 10);
                    var inputs = generator.Value.Recipe.AllUserVisibleInputs;
                    var outputs = generator.Value.Recipe.AllUserVisibleOutputs;

                    string recipe_name = generator.Value.Recipe.Id.ToString();
                    string recipe_duration = duration.ToString();

                    List<string> inputItems = new List<string> { };
                    List<string> outputItems = new List<string> { };

                    inputs.ForEach(delegate (RecipeInput input)
                    {
                        Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                        string machineRecipeInputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                        inputItems.Add(machineRecipeInputJson);
                    });

                    outputs.ForEach(delegate (RecipeOutput input)
                    {
                        Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                        string machineRecipeOutputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                        outputItems.Add(machineRecipeOutputJson);
                        electricity_generated = input.Quantity.Value.ToString();
                    });

                    string machineRecipeJson = MakeRecipeJsonObject(
                        recipe_name,
                        recipe_duration,
                        inputItems.JoinStrings(","),
                        outputItems.JoinStrings(",")
                    );
                    recipeItems.Add(machineRecipeJson);

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(",")
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + machineId.ToString());
                    Log.Info("###################################################");
                }
            }

            // -------------------------
            // Solar Panels
            // -------------------------

            Mafi.Base.Prototypes.Machines.SolarElectricityGeneratorProto.ID[] solar = new Mafi.Base.Prototypes.Machines.SolarElectricityGeneratorProto.ID[] {
                Ids.Machines.SolarPanel,
                Ids.Machines.SolarPanelMono
            };
            foreach (Mafi.Base.Prototypes.Machines.SolarElectricityGeneratorProto.ID machineId in solar)
            {
                try
                {

                    Option<Mafi.Base.Prototypes.Machines.SolarElectricityGeneratorProto> generator = protosDb.Get<Mafi.Base.Prototypes.Machines.SolarElectricityGeneratorProto>(machineId);

                    string id = generator.Value.Id.ToString();
                    string name = generator.Value.Strings.Name.ToString();
                    string category = "";
                    string workers = generator.Value.Costs.Workers.ToString();
                    string maintenance_cost_units = generator.Value.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = generator.Value.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = generator.Value.OutputElectricity.Value.ToString();
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in generator.Value.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }
;
                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in generator.Value.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + machineId.ToString());
                    Log.Info("###################################################");
                }
            }

            // -------------------------
            // Disel Generator
            // -------------------------

            ElectricityGeneratorFromProductProto.ID[] powerMachines = new ElectricityGeneratorFromProductProto.ID[] {
                Ids.Machines.DieselGenerator
            };
            foreach (ElectricityGeneratorFromProductProto.ID machineId in powerMachines)
            {
                try
                {

                    Option<ElectricityGeneratorFromProductProto> generator = protosDb.Get<ElectricityGeneratorFromProductProto>(machineId);

                    string id = generator.Value.Id.ToString();
                    string name = generator.Value.Strings.Name.ToString();
                    string category = "";
                    string workers = generator.Value.Costs.Workers.ToString();
                    string maintenance_cost_units = generator.Value.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = generator.Value.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = generator.Value.OutputElectricity.Value.ToString();
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in generator.Value.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }
;
                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in generator.Value.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> recipeItems = new List<string> { };

                    var duration = (generator.Value.Recipe.Duration / 10);
                    var inputs = generator.Value.Recipe.AllUserVisibleInputs;
                    var outputs = generator.Value.Recipe.AllUserVisibleOutputs;

                    string recipe_name = generator.Value.Recipe.Id.ToString();
                    string recipe_duration = duration.ToString();

                    List<string> inputItems = new List<string> { };
                    List<string> outputItems = new List<string> { };

                    inputs.ForEach(delegate (RecipeInput input)
                    {
                        Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                        string machineRecipeInputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                        inputItems.Add(machineRecipeInputJson);
                    });

                    outputs.ForEach(delegate (RecipeOutput input)
                    {
                        Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                        string machineRecipeOutputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                        outputItems.Add(machineRecipeOutputJson);
                    });

                    string machineRecipeJson = MakeRecipeJsonObject(
                        recipe_name,
                        recipe_duration,
                        inputItems.JoinStrings(","),
                        outputItems.JoinStrings(",")
                    );
                    recipeItems.Add(machineRecipeJson);

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(",")
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + machineId.ToString());
                    Log.Info("###################################################");
                }
            }

            /*
             * -------------------------------------
             * Part 4  - General Machines. Uses ProtoID List Method
             * -------------------------------------
            */

            MachineProto.ID[] machines = new MachineProto.ID[]
            {
                Ids.Buildings.MaintenanceDepotT0,
                Ids.Buildings.MaintenanceDepotT1,
                Ids.Buildings.MaintenanceDepotT2,
                Ids.Buildings.MaintenanceDepotT3,
                Ids.Machines.BasicDieselDistiller,
                Ids.Machines.OilPump,
                Ids.Machines.Crusher,
                Ids.Machines.SmeltingFurnaceT2,
                Ids.Machines.SmeltingFurnaceT1,
                Ids.Machines.Caster,
                Ids.Machines.CasterT2,
                Ids.Machines.CasterCooled,
                Ids.Machines.CasterCooledT2,
                Ids.Machines.OxygenFurnace,
                Ids.Machines.OxygenFurnaceT2,
                Ids.Machines.ExhaustScrubber,
                Ids.Machines.Electrolyzer,
                Ids.Machines.AirSeparator,
                Ids.Machines.CopperElectrolysis,
                Ids.Machines.RotaryKiln,
                Ids.Machines.RotaryKilnGas,
                Ids.Machines.ConcreteMixer,
                Ids.Machines.ConcreteMixerT2,
                Ids.Machines.ConcreteMixerT3,
                Ids.Machines.GeneralMixer,
                Ids.Machines.FoodMill,
                Ids.Machines.BakingUnit,
                Ids.Machines.FoodProcessor,
                Ids.Machines.SolidBurner,
                Ids.Machines.WaterChiller,
                Ids.Machines.ThermalDesalinator,
                Ids.Machines.AssemblyManual,
                Ids.Machines.AssemblyElectrified,
                Ids.Machines.AssemblyElectrifiedT2,
                Ids.Machines.AssemblyRoboticT1,
                Ids.Machines.AssemblyRoboticT2,
                Ids.Machines.MicrochipMachine,
                Ids.Machines.MicrochipMachineT2,
                Ids.Machines.WasteDump,
                Ids.Machines.LandWaterPump,
                Ids.Machines.GasInjectionPump,
                Ids.Machines.OceanWaterPumpT1,
                Ids.Machines.OceanWaterPumpLarge,
                Ids.Machines.ChemicalPlant,
                Ids.Machines.ChemicalPlant2,
                Ids.Machines.WaterTreatmentPlant,
                Ids.Machines.EvaporationPond,
                Ids.Machines.EvaporationPondHeated,
                Ids.Machines.AnaerobicDigester,
                Ids.Machines.BoilerCoal,
                Ids.Machines.BoilerGas,
                Ids.Machines.BoilerElectric,
                Ids.Machines.SmokeStack,
                Ids.Machines.SmokeStackLarge,
                Ids.Machines.CoolingTowerT1,
                Ids.Machines.CoolingTowerT2,
                Ids.Machines.GlassMakerT1,
                Ids.Machines.GlassMakerT2,
                Ids.Machines.FermentationTank,
                Ids.Machines.UraniumEnrichmentPlant,
                Ids.Machines.ArcFurnace,
                Ids.Machines.ArcFurnace2,
                Ids.Machines.SiliconReactor,
                Ids.Machines.SiliconCrystallizer,
                Ids.Machines.CharcoalMaker,
                Ids.Machines.SettlingTank,
                Ids.Machines.GoldFurnace,
                Ids.Machines.DistillationTowerT1,
                Ids.Machines.DistillationTowerT2,
                Ids.Machines.DistillationTowerT3,
                Ids.Machines.VacuumDistillationTower,
                Ids.Machines.HydroCrackerT1,
                Ids.Machines.Flare,
                Ids.Machines.HydrogenReformer,
                Ids.Machines.SourWaterStripper,
                Ids.Machines.PolymerizationPlant
            };
            foreach (MachineProto.ID machineId in machines)
            {

                try
                {

                    Option<MachineProto> machine = protosDb.Get<MachineProto>(machineId);

                    IIndexable<RecipeProto> machineRecipes = machine.Value.Recipes;

                    string id = machine.Value.Id.ToString();
                    string name = machine.Value.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Value.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Value.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Value.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = machine.Value.ElectricityConsumed.Quantity.Value.ToString();
                    string electricity_generated = "0";
                    string computing_consumed = machine.Value.ComputingConsumed.Value.ToString();
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in machine.Value.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Value.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> recipeItems = new List<string> { };

                    foreach (RecipeProto recipe in machineRecipes)
                    {

                        var duration = (recipe.Duration / 10);
                        var inputs = recipe.AllUserVisibleInputs;
                        var outputs = recipe.AllUserVisibleOutputs;

                        string recipe_name = recipe.Strings.Name.ToString();
                        string recipe_duration = duration.ToString();

                        List<string> inputItems = new List<string> { };
                        List<string> outputItems = new List<string> { };

                        inputs.ForEach(delegate (RecipeInput input)
                        {
                            Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                            string machineRecipeInputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                            inputItems.Add(machineRecipeInputJson);
                        });

                        outputs.ForEach(delegate (RecipeOutput input)
                        {
                            Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                            string machineRecipeOutputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                            outputItems.Add(machineRecipeOutputJson);
                        });

                        string machineRecipeJson = MakeRecipeJsonObject(
                            recipe_name,
                            recipe_duration,
                            inputItems.JoinStrings(","),
                            outputItems.JoinStrings(",")
                        );
                        recipeItems.Add(machineRecipeJson);

                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(",")
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");                    
                    Log.Info("ERROR"+machineId.ToString());
                    Log.Info("###################################################");
                }
            }

            /*
             * -------------------------------------
             * Part 5  - Buildings. Uses Simpler Proto LookUp Method
             * -------------------------------------
            */

            IEnumerable<FarmProto> farms = protosDb.All<FarmProto>();
            foreach (FarmProto item in farms)
            {

                try
                {

                    string id = item.Id.ToString();
                    string name = item.Strings.Name.ToString();
                    string category = "";
                    string workers = item.Costs.Workers.ToString();
                    string maintenance_cost_units = item.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = item.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in item.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + item.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<AnimalFarmProto> animalFarms = protosDb.All<AnimalFarmProto>();
            foreach (AnimalFarmProto item in animalFarms)
            {

                try
                {

                    string id = item.Id.ToString();
                    string name = item.Strings.Name.ToString();
                    string category = "";
                    string workers = item.Costs.Workers.ToString();
                    string maintenance_cost_units = item.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = item.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in item.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + item.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<CargoDepotProto> cargoDepots = protosDb.All<CargoDepotProto>();
            foreach (CargoDepotProto item in cargoDepots)
            {

                try
                {

                    string id = item.Id.ToString();
                    string name = item.Strings.Name.ToString();
                    string category = "";
                    string workers = item.Costs.Workers.ToString();
                    string maintenance_cost_units = item.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = item.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in item.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + item.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<CargoDepotModuleProto> cargoModules = protosDb.All<CargoDepotModuleProto>();
            foreach (CargoDepotModuleProto item in cargoModules)
            {

                try
                {

                    string id = item.Id.ToString();
                    string name = item.Strings.Name.ToString();
                    string category = "";
                    string capacity = item.Capacity.ToString();
                    string workers = item.Costs.Workers.ToString();
                    string maintenance_cost_units = item.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = item.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string unity_cost = "0";
                    string research_speed = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";

                    foreach (ToolbarCategoryProto cat in item.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + item.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<ResearchLabProto> labs = protosDb.All<ResearchLabProto>();
            foreach (ResearchLabProto item in labs)
            {

                try
                {

                    string id = item.Id.ToString();
                    string name = item.Strings.Name.ToString();
                    string category = "";
                    string workers = item.Costs.Workers.ToString();
                    string maintenance_cost_units = item.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = item.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = item.ElectricityConsumed.Quantity.Value.ToString();
                    string electricity_generated = "0";
                    string computing_consumed = item.ComputingConsumed.ToString();
                    string unity_cost = item.UnityMonthlyCost.ToString();
                    string recipes = "";
                    Fix32 research_speed = (60 / item.DurationForRecipe.Seconds) * item.StepsPerRecipe;
                    string capacity = "0";
                    string computing_generated = "0";

                    if ( item.Id.ToString() != "ResearchLab1"){

                        List<string> inputList = new List<string> { };
                        inputList.Add($"\"name\":\"{item.ConsumedPerRecipe.Product.Strings.Name}\"");
                        inputList.Add($"\"quantity\":{item.ConsumedPerRecipe.Quantity.Value}");

                        List<string> outputList = new List<string> { };
                        outputList.Add($"\"name\":\"{item.ProducedPerRecipe.Product.Strings.Name}\"");
                        outputList.Add($"\"quantity\":{item.ProducedPerRecipe.Quantity.Value}");

                        string inputsObj = ($"\"inputs\":[{{{inputList.JoinStrings(",")}}}],");
                        string outputsObj = ($"\"outputs\":[{{{outputList.JoinStrings(",")}}}]");

                        recipes = $"{{\"name\":\"{name}\",\"duration\":{item.DurationForRecipe.Seconds},{inputsObj + outputsObj}}}";

                    }

                    foreach (ToolbarCategoryProto cat in item.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed.ToString(),
                        machinesProducts.JoinStrings(","),
                        recipes
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + item.ToString() + item.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<MaintenanceDepotProto> depots = protosDb.All<MaintenanceDepotProto>();
            foreach (MaintenanceDepotProto machine in depots)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = machine.ElectricityConsumed.Quantity.Value.ToString();
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> recipeItems = new List<string> { };

                    IIndexable<RecipeProto> machineRecipes = machine.Recipes;

                    foreach (RecipeProto recipe in machineRecipes)
                    {

                        var duration = (recipe.Duration / 10);
                        var inputs = recipe.AllUserVisibleInputs;
                        var outputs = recipe.AllUserVisibleOutputs;

                        string recipe_name = recipe.Strings.Name.ToString();
                        string recipe_duration = duration.ToString();

                        List<string> inputItems = new List<string> { };
                        List<string> outputItems = new List<string> { };

                        inputs.ForEach(delegate (RecipeInput input)
                        {
                            Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                            string machineRecipeInputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                            inputItems.Add(machineRecipeInputJson);
                        });

                        outputs.ForEach(delegate (RecipeOutput input)
                        {
                            Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                            string machineRecipeOutputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                            outputItems.Add(machineRecipeOutputJson);
                        });

                        string machineRecipeJson = MakeRecipeJsonObject(
                            recipe_name,
                            recipe_duration,
                            inputItems.JoinStrings(","),
                            outputItems.JoinStrings(",")
                        );
                        recipeItems.Add(machineRecipeJson);

                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(",")
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<VehicleDepotProto> depotsVehicles = protosDb.All<VehicleDepotProto>();
            foreach (VehicleDepotProto machine in depotsVehicles)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = machine.ElectricityConsumed.Quantity.Value.ToString();
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<FuelStationProto> depotsFuelds = protosDb.All<FuelStationProto>();
            foreach (FuelStationProto machine in depotsFuelds)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string capacity = machine.Capacity.ToString();
                    string unity_cost = "0";
                    string research_speed = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );

                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            List<string> countProds = new List<string> { };
            List<string> countProdNames = new List<string> { };
            IEnumerable<CountableProductProto> countableProducts = protosDb.All<CountableProductProto>();
            foreach (CountableProductProto product in countableProducts)
            {
                countProds.Add("\"" + product.Strings.Name.ToString() + "\"");
                countProdNames.Add(product.Strings.Name.ToString());
            }
            string countProdsJson = countProds.JoinStrings(",");

            List<string> looseProds = new List<string> { };
            List<string> looseProdNames = new List<string> { };
            IEnumerable<LooseProductProto> looseProducts = protosDb.All<LooseProductProto>();
            foreach (LooseProductProto product in looseProducts)
            {
                looseProds.Add("\"" + product.Strings.Name.ToString() + "\"");
                looseProdNames.Add(product.Strings.Name.ToString());
            }
            string looseProdsJson = looseProds.JoinStrings(",");

            List<string> fluidProds = new List<string> { };
            List<string> fluidProdNames = new List<string> { };
            IEnumerable<FluidProductProto> fluidProducts = protosDb.All<FluidProductProto>();
            foreach (FluidProductProto product in fluidProducts)
            {
                fluidProds.Add("\"" + product.Strings.Name.ToString() + "\"");
                fluidProdNames.Add(product.Strings.Name.ToString());
            }
            string fluidProdsJson = fluidProds.JoinStrings(",");

            List<string> storageItems = new List<string> { };

            IEnumerable<StorageProto> storages = protosDb.All<StorageProto>();
            foreach (StorageProto machine in storages)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string capacity = machine.Capacity.ToString();
                    string unity_cost = "0";
                    string research_speed = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> StorageInputs = new List<string> { };

                    if ( machine.ProductType.Value.ToString() == "CountableProductProto")
                    {
                        StorageInputs = countProdNames;
                    }

                    if (machine.ProductType.Value.ToString() == "LooseProductProto")
                    {
                        StorageInputs = looseProdNames;
                    }

                    if (machine.ProductType.Value.ToString() == "FluidProductProto")
                    {
                        StorageInputs = fluidProdNames;
                    }

                    List<string> recipeItems = new List<string> { };

                    foreach (string input in StorageInputs)
                    {

                        var duration = 0;

                        string recipe_name = input + " Storage";
                        string recipe_duration = duration.ToString();

                        List<string> inputItems = new List<string> { };
                        List<string> outputItems = new List<string> { };

                        string machineRecipeInputJson = MakeRecipeIOJsonObject(input, machine.Capacity.ToString());
                        inputItems.Add(machineRecipeInputJson);
                        outputItems.Add(machineRecipeInputJson);

                        string machineRecipeJson = MakeRecipeJsonObject(
                            recipe_name,
                            recipe_duration,
                            inputItems.JoinStrings(","),
                            outputItems.JoinStrings(",")
                        );
                        recipeItems.Add(machineRecipeJson);

                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                    string storageJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(",")
                    );
                    storageItems.Add(storageJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<NuclearWasteStorageProto> storagesNuclear = protosDb.All<NuclearWasteStorageProto>();
            foreach (NuclearWasteStorageProto machine in storagesNuclear)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string capacity = machine.Capacity.ToString();
                    string unity_cost = "0";
                    string research_speed = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<SettlementHousingModuleProto> housing = protosDb.All<SettlementHousingModuleProto>();
            foreach (SettlementHousingModuleProto machine in housing)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string capacity = machine.Capacity.ToString();
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<MineTowerProto> mines = protosDb.All<MineTowerProto>();
            foreach (MineTowerProto machine in mines)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string capacity = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<SettlementWasteModuleProto> housingWaste = protosDb.All<SettlementWasteModuleProto>();
            foreach (SettlementWasteModuleProto machine in housingWaste)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string capacity = machine.Capacity.ToString();
                    string unity_cost = "0";
                    string research_speed = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<SettlementFoodModuleProto> housingFood = protosDb.All<SettlementFoodModuleProto>();
            foreach (SettlementFoodModuleProto machine in housingFood)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string capacity = machine.CapacityPerBuffer.ToString();
                    string unity_cost = "0";
                    string research_speed = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<SettlementModuleProto> housingNeed = protosDb.All<SettlementModuleProto>();
            foreach (SettlementModuleProto machine in housingNeed)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = machine.ElectricityConsumed.Quantity.Value.ToString();
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<NuclearReactorProto> reactors = protosDb.All<NuclearReactorProto>();
            foreach (NuclearReactorProto machine in reactors)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> recipeItems = new List<string> { };

                    IRecipeForUi recipe = machine.Recipe;

                    var duration = (recipe.Duration / 10);
                    var inputs = recipe.AllUserVisibleInputs;
                    var outputs = recipe.AllUserVisibleOutputs;

                    string recipe_name = recipe.Id.ToString();

                    string recipe_duration = duration.ToString();

                    List<string> inputItems = new List<string> { };
                    List<string> outputItems = new List<string> { };

                    inputs.ForEach(delegate (RecipeInput input)
                    {
                        Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                        string machineRecipeInputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                        inputItems.Add(machineRecipeInputJson);
                    });

                    outputs.ForEach(delegate (RecipeOutput input)
                    {
                        Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                        string machineRecipeOutputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                        outputItems.Add(machineRecipeOutputJson);
                    });

                    string machineRecipeJson = MakeRecipeJsonObject(
                        recipe_name,
                        recipe_duration,
                        inputItems.JoinStrings(","),
                        outputItems.JoinStrings(",")
                    );
                    recipeItems.Add(machineRecipeJson);

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(",")
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<RocketAssemblyBuildingProto> rocketAssembly = protosDb.All<RocketAssemblyBuildingProto>();
            foreach (RocketAssemblyBuildingProto machine in rocketAssembly)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = machine.ElectricityConsumed.Value.ToString();
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<RocketLaunchPadProto> rocketPad = protosDb.All<RocketLaunchPadProto>();
            foreach (RocketLaunchPadProto machine in rocketPad)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<WasteSortingPlantProto> wastePlant = protosDb.All<WasteSortingPlantProto>();
            foreach (WasteSortingPlantProto machine in wastePlant)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> recipeItems = new List<string> { };

                    IRecipeForUi recipe = machine.Recipe;

                    var duration = (recipe.Duration / 10);
                    var inputs = recipe.AllUserVisibleInputs;
                    var outputs = recipe.AllUserVisibleOutputs;

                    string recipe_name = recipe.Id.ToString();

                    string recipe_duration = duration.ToString();

                    List<string> inputItems = new List<string> { };
                    List<string> outputItems = new List<string> { };

                    inputs.ForEach(delegate (RecipeInput input)
                    {
                        Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                        string machineRecipeInputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                        inputItems.Add(machineRecipeInputJson);
                    });

                    outputs.ForEach(delegate (RecipeOutput input)
                    {
                        Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                        string machineRecipeOutputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                        outputItems.Add(machineRecipeOutputJson);
                    });

                    string machineRecipeJson = MakeRecipeJsonObject(
                        recipe_name,
                        recipe_duration,
                        inputItems.JoinStrings(","),
                        outputItems.JoinStrings(",")
                    );
                    recipeItems.Add(machineRecipeJson);

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(",")
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<RainwaterHarvesterProto> rainwaterHarvester = protosDb.All<RainwaterHarvesterProto>();
            foreach (RainwaterHarvesterProto machine in rainwaterHarvester)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<DataCenterProto> dataCenters = protosDb.All<DataCenterProto>();
            foreach (DataCenterProto machine in dataCenters)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarCategoryProto cat in machine.Graphics.Categories)
                    {
                        category = cat.Strings.Name.ToString();
                    }

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.Price.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        ""
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<ServerRackProto> dataRack = protosDb.All<ServerRackProto>();
            foreach (ServerRackProto machine in dataRack)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "Data center";
                    string workers = "0";
                    string maintenance_cost_units = "Maintenance III";
                    string maintenance_cost_quantity = machine.Maintenance.Value.ToString();
                    string electricity_consumed = machine.ConsumedPowerPerTick.Value.ToString();
                    string electricity_generated = "0";
                    string computing_generated = machine.CreatedComputingPerTick.Value.ToString();
                    string computing_consumed = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    List<string> machinesProducts = new List<string> { };

                    string vehicleProductJson = MakeVehicleProductJsonObject(
                            machine.ProductToAddThis.Product.Strings.Name.ToString(),
                            machine.ProductToAddThis.Quantity.Value.ToString()
                    );
                    machinesProducts.Add(vehicleProductJson);

                    List<string> recipeItems = new List<string> { };
                    string recipe_name = "Create Computing Power";
                    string recipe_duration = "60";

                    List<string> inputItems = new List<string> { };
                    List<string> outputItems = new List<string> { };

                    string machineRecipeInputJson = MakeRecipeIOJsonObject("Chilled water", machine.CoolantInPerMonth.Value.ToString());
                    inputItems.Add(machineRecipeInputJson);

                    string machineRecipeOutputJson = MakeRecipeIOJsonObject("Water", machine.CoolantOutPerMonth.Value.ToString());
                    outputItems.Add(machineRecipeOutputJson);

                    string machineRecipeJson = MakeRecipeJsonObject(
                        recipe_name,
                        recipe_duration,
                        inputItems.JoinStrings(","),
                        outputItems.JoinStrings(",")
                    );
                    recipeItems.Add(machineRecipeJson);

                    string machineJson = MakeMachineJsonObject(
                        id,
                        name,
                        category,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        capacity,
                        unity_cost,
                        research_speed,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(",")
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            /*
             * -------------------------------------
             * Part 6  - Terrain Materials. Uses Simpler Proto LookUp Method
             * -------------------------------------
            */

            List<string> materialItems = new List<string> { };

            IEnumerable<TerrainMaterialProto> materials = protosDb.All<TerrainMaterialProto>();
            foreach (TerrainMaterialProto material in materials)
            {

                try
                {
                    string id = material.Id.ToString();
                    string name = material.Strings.Name.ToString();
                    string mined_product = material.MinedProduct.Strings.Name.ToString();
                    string mining_hardness = material.MiningHardness.ToString();
                    string mined_quantity_per_tile_cubed = material.MinedQuantityPerTileCubed.ToString();
                    string disruption_recovery_time = material.DisruptionRecoveryTime.ToString();
                    string is_hardened_floor = material.IsHardenedFloor.ToString().ToLower();
                    string max_collapse_height_diff = material.MaxCollapseHeightDiff.ToString();
                    string min_collapse_height_diff = material.MinCollapseHeightDiff.ToString();
                    string mined_quantity_mult = material.MinedQuantityMult.ToString();
                    string vehicle_traversal_cost = material.VehicleTraversalCost.ToString();

                    string materialJson = MakeTerrainMaterialJsonObject(
                        id,
                        name,
                        mined_product,
                        mining_hardness,
                        mined_quantity_per_tile_cubed,
                        disruption_recovery_time,
                        is_hardened_floor,
                        max_collapse_height_diff,
                        min_collapse_height_diff,
                        mined_quantity_mult,
                        vehicle_traversal_cost
                    );
                    materialItems.Add(materialJson);


                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Material" + material.Id.ToString());
                    Log.Info("###################################################");
                }

            }

            File.WriteAllText("c:/temp/terrain_materials.json", $"{{\"game_version\":\"{game_version}\",\"terrain_materials\":[{materialItems.JoinStrings(",")}]}}");

            List<string> researchItems = new List<string> { };

            IEnumerable<ResearchNodeProto> researchNodes = protosDb.All<ResearchNodeProto>();

            foreach (ResearchNodeProto researchNode in researchNodes)
            {

                string researchJson = MakeResearchJsonObject(
                    researchNode.Id.ToString(),
                    researchNode.Strings.Name.ToString(),
                    researchNode.Difficulty.ToString(),
                    researchNode.TotalStepsRequired.ToString()
                );
                researchItems.Add(researchJson);

            }

            File.WriteAllText("c:/temp/research.json", $"{{\"game_version\":\"{game_version}\",\"research\":[{researchItems.JoinStrings(",")}]}}");

            List<string> contractItems = new List<string> { };

            IEnumerable<ContractProto> contracts = protosDb.All<ContractProto>();

            foreach (ContractProto contract in contracts)
            {

                string contractJson = MakeContractJsonObject(
                    contract.Id.ToString(),
                    contract.ProductToBuy.Product.Strings.Name.ToString(),
                    contract.ProductToBuy.Quantity.ToString(),
                    contract.ProductToPayWith.Product.Strings.Name.ToString(),
                    contract.ProductToPayWith.Quantity.ToString(),
                    contract.UpointsPerMonth.ToString(),
                    contract.UpointsPer100ProductsBought.ToString(),
                    contract.UpointsToEstablish.ToString(),
                    contract.MinReputationRequired.ToString()
                );
                contractItems.Add(contractJson);

            }

            File.WriteAllText("c:/temp/contracts.json", $"{{\"game_version\":\"{game_version}\",\"contracts\":[{contractItems.JoinStrings(",")}]}}");


            /*
                * -------------------------------------
                * Part 7  - Final JSON Export
                * -------------------------------------
            */

            File.WriteAllText("c:/temp/machines_and_buildings.json", $"{{\"game_version\":\"{game_version}\",\"machines_and_buildings\":[{machineItems.JoinStrings(",")}]}}");
            File.WriteAllText("c:/temp/storages.json", $"{{\"game_version\":\"{game_version}\",\"storages\":[{storageItems.JoinStrings(",")}]}}");

        }

        /*
         * -------------------------------------
         * Empty Implementation Of Required Mod Methods
         * -------------------------------------
        */

        public void RegisterPrototypes(ProtoRegistrator registrator) { }

/*        public void Main(string[] args)
        {
            var websocketServer = new WebSocketServer("ws://0.0.0.0:8181");
            websocketServer.Start(connection =>
            {
                connection.OnOpen = () =>
                  Console.WriteLine("OnOpen");
                connection.OnClose = () =>
                  Console.WriteLine("OnClose");
                connection.OnMessage = message =>
                {
                    Console.WriteLine($"OnMessage {message}");
                    connection.Send($"Echo: {message}");
                };
                connection.OnBinary = bytes =>
                  Console.WriteLine($"OnBinary {Encoding.UTF8.GetString(bytes)}");
                connection.OnError = exception =>
                  Console.WriteLine($"OnError {exception.Message}");
                connection.OnPing = bytes =>
                  Console.WriteLine("OnPing");
                connection.OnPong = bytes =>
                  Console.WriteLine("OnPong");
            });
        }*/

        public void Initialize(DependencyResolver resolver, bool gameWasLoaded) {
            
        }

        public void Register(ImmutableArray<DataOnlyMod> mods, RegistrationContext context) {}

    }
}
