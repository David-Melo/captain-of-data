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

using Mafi;
using Mafi.Base.Prototypes.Buildings.ThermalStorages;
using Mafi.Base.Prototypes.Machines;
using Mafi.Base.Prototypes.Machines.PowerGenerators;
using Mafi.Collections;
using Mafi.Collections.ImmutableCollections;
using Mafi.Core;
using Mafi.Core.Buildings.Cargo;
using Mafi.Core.Buildings.Cargo.Modules;
using Mafi.Core.Buildings.Farms;
using Mafi.Core.Buildings.FuelStations;
using Mafi.Core.Buildings.Mine;
using Mafi.Core.Buildings.RainwaterHarvesters;
using Mafi.Core.Buildings.ResearchLab;
using Mafi.Core.Buildings.Settlements;
using Mafi.Core.Buildings.SpaceProgram;
using Mafi.Core.Buildings.Storages;
using Mafi.Core.Buildings.VehicleDepots;
using Mafi.Core.Buildings.Waste;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Factory.Datacenters;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.NuclearReactors;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Factory.Transports;
using Mafi.Core.Fleet;
using Mafi.Core.Game;
using Mafi.Core.Mods;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Core.Vehicles.Excavators;
using Mafi.Core.Vehicles.TreeHarvesters;
using Mafi.Core.Vehicles.Trucks;
using Mafi.Core.World.Contracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DataExtractorMod
{
    public sealed class DataExtractor : IMod
    {
        public string Name => "Data Extractor Mod By ItsDesm (modified by doubleaxe)";

        public int Version => 1;

        public bool IsUiOnly => false;

        public Option<IConfig> ModConfig => Option<IConfig>.None;

        private static readonly string MOD_ROOT_DIR_PATH = new FileSystemHelper().GetDirPath(FileType.Mod, false);
        private static readonly string MOD_DIR_PATH = Path.Combine(MOD_ROOT_DIR_PATH, "DataExtractor");
        private static readonly string PLUGIN_DIR_PATH = Path.Combine(MOD_DIR_PATH, "Plugins");

        private static readonly bool DEBUG = false;

        private ModManifest manifest;

        public ModManifest Manifest => manifest;
        public ModJsonConfig JsonConfig => new ModJsonConfig(this);

        public DataExtractor(ModManifest modManifest)
        {
            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            Log.Info(MOD_ROOT_DIR_PATH);
            Log.Info(MOD_DIR_PATH);
            Log.Info(PLUGIN_DIR_PATH);
            Log.Info("Loaded Data Extractor Mod By ItsDesm");

            manifest = modManifest;
        }

        public void MigrateJsonConfig(VersionSlim savedVersion, Dict<string, object> savedValues)
        {
        }

        public void Dispose()
        {
        }

        /*
         * -------------------------------------
         * JSON Formatters For Specific Machine Types
         * -------------------------------------
        */

        public static string MakeRecipeIOJsonObject(
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

        public static string MakeMachineJsonObject(
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
            string icon,
            string build_costs,
            string recipes,
            string subcategory = ""
        )
        {
            return MakeMachineJsonObject2(
                id,
                name,
                category,
                "",
                workers,
                maintenance_cost_units,
                maintenance_cost_quantity,
                electricity_consumed,
                electricity_generated,
                computing_consumed,
                computing_generated,
                "",
                capacity,
                unity_cost,
                research_speed,
                icon,
                build_costs,
                recipes,
                subcategory
            );
        }

        public static string MakeMachineJsonObject2(
            string id,
            string name,
            string category,
            string next_tier,
            string workers,
            string maintenance_cost_units,
            string maintenance_cost_quantity,
            string electricity_consumed,
            string electricity_generated,
            string computing_consumed,
            string computing_generated,
            string product_type,
            string capacity,
            string unity_cost,
            string research_speed,
            string icon,
            string build_costs,
            string recipes,
            string subcategory = ""
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"id\":\"{id}\"");
            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"category\":\"{category}\"");
            props.Add($"\"subcategory\":\"{subcategory}\"");
            props.Add($"\"next_tier\":\"{next_tier}\"");
            props.Add($"\"workers\":{workers}");
            props.Add($"\"maintenance_cost_units\":\"{maintenance_cost_units}\"");
            props.Add($"\"maintenance_cost_quantity\":{maintenance_cost_quantity}");
            props.Add($"\"electricity_consumed\":{electricity_consumed}");
            props.Add($"\"electricity_generated\":{electricity_generated}");
            props.Add($"\"computing_consumed\":{computing_consumed}");
            props.Add($"\"computing_generated\":{computing_generated}");
            props.Add($"\"product_type\":\"{product_type}\"");
            props.Add($"\"storage_capacity\":{capacity}");
            props.Add($"\"unity_cost\":{unity_cost}");
            props.Add($"\"research_speed\":{research_speed}");
            props.Add($"\"icon_path\":\"{icon}\"");
            props.Add($"\"build_costs\":[{build_costs}]");
            props.Add($"\"recipes\":[{recipes}]");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }

        public static string MakeTransportJsonObject(
            string id,
            string name,
            string category,
            string next_tier,
            string maintenance_cost_units,
            string maintenance_cost_quantity,
            string electricity_consumed,
            string throughput_per_second,
            string length_per_cost,
            string icon,
            string build_costs,
            string subcategory = ""
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"id\":\"{id}\"");
            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"category\":\"{category}\"");
            props.Add($"\"subcategory\":\"{subcategory}\"");
            props.Add($"\"next_tier\":\"{next_tier}\"");
            props.Add($"\"maintenance_cost_units\":\"{maintenance_cost_units}\"");
            props.Add($"\"maintenance_cost_quantity\":{maintenance_cost_quantity}");
            props.Add($"\"electricity_consumed\":{electricity_consumed}");
            props.Add($"\"throughput_per_second\":{throughput_per_second}");
            props.Add($"\"length_per_cost\":{length_per_cost}");
            props.Add($"\"icon_path\":\"{icon}\"");
            props.Add($"\"build_costs\":[{build_costs}]");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }

        private static string GetTopLevelCategory(ImmutableArray<ToolbarEntryData> categories, string fallbackCategory)
        {
            foreach (ToolbarEntryData cat in categories)
            {
                if (cat.CategoryProto.ParentCategory.HasValue)
                {
                    return cat.CategoryProto.ParentCategory.Value.Strings.Name.ToString();
                }
            }

            return fallbackCategory;
        }

        private static string GetSubcategory(ImmutableArray<ToolbarEntryData> categories)
        {
            foreach (ToolbarEntryData cat in categories)
            {
                if (cat.CategoryProto.ParentCategory.HasValue)
                {
                    return cat.CategoryProto.Strings.Name.ToString();
                }
            }

            return "";
        }

        public static string MakeRecipeJsonObject(
            string id,
            string name,
            string duration,
            string inputs,
            string outputs,
            string power_multiplier
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"id\":\"{id}\"");
            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"duration\":{duration}");
            props.Add($"\"inputs\":[{inputs}]");
            props.Add($"\"outputs\":[{outputs}]");
            props.Add($"\"power_multiplier\":{power_multiplier}");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }

        public static string MakeVehicleJsonObject(
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

        public static string MakeVehicleProductJsonObject(
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

        public static string MakeEngineJsonObject(
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

        public static string MakeGunJsonObject(
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

        public static string MakeArmorJsonObject(
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

        public static string MakeBridgeJsonObject(
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

        public static string MakeTankJsonObject(
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

        public static string MakeProductJsonObject(
            string id,
            string name,
            string type,
            string icon
        )
        {
            System.Text.StringBuilder obj = new System.Text.StringBuilder();

            List<string> props = new List<string> { };

            props.Add($"\"id\":\"{id}\"");
            props.Add($"\"name\":\"{name}\"");
            props.Add($"\"icon\":\"{productNameToIcon(name)}\"");
            props.Add($"\"type\":\"{type}\"");
            props.Add($"\"icon_path\":\"{icon}\"");

            obj.AppendLine("{");
            obj.AppendLine(props.JoinStrings(","));
            obj.AppendLine("}");
            return obj.ToString();
        }

        public static string MakeTerrainMaterialJsonObject(
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

        public static string MakeContractJsonObject(
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

        public static string MakeResearchJsonObject(
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

        public static string productNameToIcon(string n)
        {
            n = n.Replace("(", "");
            n = n.Replace(")", "");
            if (n.EndsWith(" IV"))
                n = n.Replace(" IV", "4");
            if (n.EndsWith(" V"))
                n = n.Replace(" V", "5");
            if (n.EndsWith(" III"))
                n = n.Replace(" III", "3");
            if (n.EndsWith(" II"))
                n = n.Replace(" II", "2");
            if (n.EndsWith(" I"))
                n = n.Replace(" I", "1");
            n = n.Replace(" ", "");
            return n;
        }

        public static string MakeRecipeJsonObject(
            ProtosDb protosDb,
            IRecipeForUi recipe,
            string defaultId = "",
            string defaultName = ""
        )
        {
            var duration = recipe.Duration.Seconds;
            var inputs = recipe.AllUserVisibleInputs;
            var outputs = recipe.AllUserVisibleOutputs;

            string recipe_id = recipe.Id.ToString();
            string recipe_name = (recipe is RecipeProto) ? ((RecipeProto)recipe).Strings.Name.ToString() : recipe.Id.ToString();
            if (recipe_id.Equals("RecipeForUiData") && !defaultId.IsEmpty())
            {
                recipe_id = defaultId;
            }
            if (recipe_name.Equals("RecipeForUiData") && !defaultName.IsEmpty())
            {
                recipe_name = defaultName;
            }
            string recipe_duration = duration.ToString();

            List<string> inputItems = new List<string> { };
            List<string> outputItems = new List<string> { };

            inputs.ForEach(delegate (RecipeInput input)
            {
                Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                string machineRecipeInputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                inputItems.Add(machineRecipeInputJson);
            });

            outputs.ForEach(delegate (RecipeOutput output)
            {
                Option<ProductProto> product = protosDb.Get<ProductProto>(output.Product.Id);
                string machineRecipeOutputJson = MakeRecipeIOJsonObject(output.Product.Strings.Name.ToString(), output.Quantity.Value.ToString());
                outputItems.Add(machineRecipeOutputJson);
            });

            string machineRecipeJson = MakeRecipeJsonObject(
                recipe_id,
                recipe_name,
                recipe_duration,
                inputItems.JoinStrings(","),
                outputItems.JoinStrings(","),
                (recipe is RecipeProto) ? ((RecipeProto)recipe).PowerMultiplier.ToDouble().ToString(System.Globalization.CultureInfo.InvariantCulture) : "1"
            );
            return machineRecipeJson;
        }

        public static List<string> MakeRecipesJsonObject(
            ProtosDb protosDb,
            IEnumerable<IRecipeForUi> machineRecipes,
            string defaultId = "",
            string defaultName = ""
        )
        {
            List<string> recipeItems = new List<string> { };

            int i = 0;
            foreach (IRecipeForUi recipe in machineRecipes)
            {
                string defaultIdThis = defaultId.IsEmpty() ? defaultId : (defaultId + ((i != 0) ? i.ToString() : ""));
                string defaultNameThis = defaultName.IsEmpty() ? defaultName : (defaultName + ((i != 0) ? (" " + i.ToString()) : ""));
                string machineRecipeJson = MakeRecipeJsonObject(
                    protosDb,
                    recipe,
                    defaultIdThis,
                    defaultNameThis
                );
                recipeItems.Add(machineRecipeJson);
                i++;
            }
            return recipeItems;
        }

        public static void DumpObject(List<string> DUMP, string name, object element)
        {
            if (!DEBUG)
                return;
            var content = GenericToDataString.ObjectDumper.Dump(element);
            DUMP.Add(name);
            DUMP.Add("");
            DUMP.Add(content);
            DUMP.Add("");
            DUMP.Add("");
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

            List<string> DUMP = new List<string> { };

            string game_version = typeof(Mafi.Base.BaseMod).GetTypeInfo().Assembly.GetName().Version.ToString();

            /*
             * -------------------------------------
             * Part 1  - Ship Upgrades.
             * -------------------------------------
            */

            List<string> upgradeItems = new List<string> { };
            List<string> engineItems = new List<string> { };
            List<string> gunItems = new List<string> { };
            List<string> armorItems = new List<string> { };
            List<string> bridgeItems = new List<string> { };
            List<string> tankItems = new List<string> { };

            IEnumerable<FleetEnginePartProto> engines = protosDb.All<FleetEnginePartProto>();
            foreach (FleetEnginePartProto item in engines)
            {

                try
                {

                    List<string> vehicleProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Value.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        vehicleProducts.Add(vehicleProductJson);
                    }

                    string vehicleJson = MakeEngineJsonObject(
                        item.Strings.Name.ToString(),
                        item.FuelCapacity.ToString(),
                        item.ExtraCrew.BonusValue.ToString(),
                        vehicleProducts.JoinStrings(",")
                    );
                    engineItems.Add(vehicleJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + item.ToString());
                    Log.Info("###################################################");
                }

            }
            upgradeItems.Add($"\"engines\":[{engineItems.JoinStrings(",")}]");

            IEnumerable<FleetWeaponProto> guns = protosDb.All<FleetWeaponProto>();
            foreach (FleetWeaponProto item in guns)
            {

                try
                {

                    List<string> vehicleProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Value.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        vehicleProducts.Add(vehicleProductJson);
                    }

                    string vehicleJson = MakeGunJsonObject(
                        item.Strings.Name.ToString(),
                        item.Range.ToString(),
                        item.Damage.ToString(),
                        item.ExtraCrew.BonusValue.ToString(),
                        vehicleProducts.JoinStrings(",")
                    );
                    gunItems.Add(vehicleJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + item.ToString());
                    Log.Info("###################################################");
                }

            }
            upgradeItems.Add($"\"weapons\":[{gunItems.JoinStrings(",")}]");

            IEnumerable<UpgradeHullProto> armor = protosDb.All<UpgradeHullProto>();
            foreach (UpgradeHullProto item in armor)
            {

                try
                {
                    List<string> vehicleProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Value.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        vehicleProducts.Add(vehicleProductJson);
                    }

                    string vehicleJson = MakeArmorJsonObject(
                        item.Strings.Name.ToString(),
                        "0",
                        "0",
                        vehicleProducts.JoinStrings(",")
                    );
                    armorItems.Add(vehicleJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + item.ToString());
                    Log.Info("###################################################");
                }

            }
            upgradeItems.Add($"\"armor\":[{armorItems.JoinStrings(",")}]");

            IEnumerable<FleetBridgePartProto> bridges = protosDb.All<FleetBridgePartProto>();
            foreach (FleetBridgePartProto item in bridges)
            {

                try
                {

                    List<string> vehicleProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Value.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        vehicleProducts.Add(vehicleProductJson);
                    }

                    string vehicleJson = MakeBridgeJsonObject(
                        item.Strings.Name.ToString(),
                        "0",
                        "0",
                        item.ExtraCrew.BonusValue.ToString(),
                        vehicleProducts.JoinStrings(",")
                    );
                    bridgeItems.Add(vehicleJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + item.ToString());
                    Log.Info("###################################################");
                }

            }
            upgradeItems.Add($"\"bridges\":[{bridgeItems.JoinStrings(",")}]");

            IEnumerable<FleetFuelTankPartProto> tanks = protosDb.All<FleetFuelTankPartProto>();
            foreach (FleetFuelTankPartProto item in tanks)
            {

                try
                {

                    List<string> vehicleProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Value.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        vehicleProducts.Add(vehicleProductJson);
                    }

                    string vehicleJson = MakeTankJsonObject(
                        item.Strings.Name.ToString(),
                        item.AddedFuelCapacity.ToString(),
                        vehicleProducts.JoinStrings(",")
                    );
                    tankItems.Add(vehicleJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + item.ToString());
                    Log.Info("###################################################");
                }

            }
            upgradeItems.Add($"\"fuel_tanks\":[{tankItems.JoinStrings(",")}]");

            File.WriteAllText("c:/temp/ship_upgrades.json", $"{{\"game_version\":\"{game_version}\",{upgradeItems.JoinStrings(",")}}}");

            /*
             * -------------------------------------
             * Part 2  - Vehicles.
             * -------------------------------------
            */

            List<string> vehicleItems = new List<string> { };

            List<DrivingEntityProto> vehicles = new List<DrivingEntityProto> { };
            foreach (TruckProto vehicle in protosDb.All<TruckProto>())
            {
                vehicles.Add(vehicle);
            }
            foreach (ExcavatorProto vehicle in protosDb.All<ExcavatorProto>())
            {
                vehicles.Add(vehicle);
            }
            foreach (TreeHarvesterProto vehicle in protosDb.All<TreeHarvesterProto>())
            {
                vehicles.Add(vehicle);
            }

            foreach (DrivingEntityProto vehicle in vehicles)
            {

                try
                {
                    List<string> vehicleProducts = new List<string> { };

                    foreach (ProductQuantity cost in vehicle.CostToBuild.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        vehicleProducts.Add(vehicleProductJson);
                    }

                    string vehicleJson = MakeVehicleJsonObject(
                        vehicle.Strings.Name.ToString(),
                        vehicleProducts.JoinStrings(",")
                    );
                    vehicleItems.Add(vehicleJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + vehicle.ToString());
                    Log.Info("###################################################");
                }

            }

            File.WriteAllText("c:/temp/vehicles.json", $"{{\"game_version\":\"{game_version}\",\"vehicles\":[{vehicleItems.JoinStrings(",")}]}}");

            /*
             * -------------------------------------
             * Part 3  - Power Generation Machines. (Behave Uniquely)
             * -------------------------------------
            */

            List<string> machineItems = new List<string> { };

            // -------------------------
            // Turbines
            // -------------------------

            IEnumerable<MechPowerGeneratorFromProductProto> turbines = protosDb.All<MechPowerGeneratorFromProductProto>();
            foreach (MechPowerGeneratorFromProductProto generator in turbines)
            {
                try
                {
                    string id = generator.Id.ToString();
                    string name = generator.Strings.Name.ToString();
                    string category = "";
                    string workers = generator.Costs.Workers.ToString();
                    string maintenance_cost_units = generator.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = generator.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string product_type = "";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";
                    string next_tier = "";
                    if (generator.Upgrade.NextTier.HasValue)
                    {
                        next_tier = generator.Upgrade.NextTier.Value.Id.ToString();
                    }

                    foreach (ToolbarEntryData cat in generator.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(generator.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in generator.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> recipeItems = MakeRecipesJsonObject(protosDb, new IRecipeForUi[] { generator.Recipe });

                    string machineJson = MakeMachineJsonObject2(
                        id,
                        name,
                        category,
                        next_tier,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        product_type,
                        capacity,
                        unity_cost,
                        research_speed,
                        generator.IconPath,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(","),
                        GetSubcategory(generator.Graphics.Categories)
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + generator.ToString());
                    Log.Info("###################################################");
                }
            }

            // -------------------------
            // Generators
            // -------------------------

            IEnumerable<ElectricityGeneratorFromMechPowerProto> generators = protosDb.All<ElectricityGeneratorFromMechPowerProto>();
            foreach (ElectricityGeneratorFromMechPowerProto generator in generators)
            {
                try
                {

                    string id = generator.Id.ToString();
                    string name = generator.Strings.Name.ToString();
                    string category = "";
                    string workers = generator.Costs.Workers.ToString();
                    string maintenance_cost_units = generator.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = generator.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = "";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarEntryData cat in generator.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(generator.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in generator.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> recipeItems = MakeRecipesJsonObject(protosDb, new IRecipeForUi[] { generator.Recipe });

                    var outputs = generator.Recipe.AllUserVisibleOutputs;
                    electricity_generated = outputs[0].Quantity.Value.ToString();

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
                        generator.IconPath,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(","),
                        GetSubcategory(generator.Graphics.Categories)
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + generator.ToString());
                    Log.Info("###################################################");
                }
            }

            // -------------------------
            // Solar Panels
            // -------------------------

            IEnumerable<SolarElectricityGeneratorProto> solar = protosDb.All<SolarElectricityGeneratorProto>();
            foreach (SolarElectricityGeneratorProto generator in solar)
            {
                try
                {

                    string id = generator.Id.ToString();
                    string name = generator.Strings.Name.ToString();
                    string category = "";
                    string workers = generator.Costs.Workers.ToString();
                    string maintenance_cost_units = generator.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = generator.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = generator.OutputElectricity.Value.ToString();
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarEntryData cat in generator.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(generator.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in generator.Costs.BaseConstructionCost.Products)
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
                        generator.IconPath,
                        machinesProducts.JoinStrings(","),
                        "",
                        GetSubcategory(generator.Graphics.Categories)
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + generator.ToString());
                    Log.Info("###################################################");
                }
            }

            // -------------------------
            // Disel Generator
            // -------------------------

            IEnumerable<ElectricityGeneratorFromProductProto> powerMachines = protosDb.All<ElectricityGeneratorFromProductProto>();
            foreach (ElectricityGeneratorFromProductProto generator in powerMachines)
            {
                try
                {

                    string id = generator.Id.ToString();
                    string name = generator.Strings.Name.ToString();
                    string category = "";
                    string workers = generator.Costs.Workers.ToString();
                    string maintenance_cost_units = generator.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = generator.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = "0";
                    string electricity_generated = generator.OutputElectricity.Value.ToString();
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarEntryData cat in generator.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(generator.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in generator.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> recipeItems = MakeRecipesJsonObject(protosDb, new IRecipeForUi[] { generator.Recipe });

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
                        generator.IconPath,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(","),
                        GetSubcategory(generator.Graphics.Categories)
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + generator.ToString());
                    Log.Info("###################################################");
                }
            }

            /*
             * -------------------------------------
             * Part 4  - General Machines.
             * -------------------------------------
            */

            //MaintenanceDepotProto is also MachineProto
            IEnumerable<MachineProto> machines = protosDb.All<MachineProto>();
            foreach (MachineProto machine in machines)
            {

                try
                {

                    List<IRecipeForUi> machineRecipes = machine.Recipes.AsEnumerable().ToList<IRecipeForUi>();

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                    string maintenance_cost_quantity = machine.Costs.Maintenance.MaintenancePerMonth.Value.ToString();
                    string electricity_consumed = machine.ElectricityConsumed.Quantity.Value.ToString();
                    string electricity_generated = "0";
                    string computing_consumed = machine.ComputingConsumed.Value.ToString();
                    string computing_generated = "0";
                    string product_type = "";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";
                    string next_tier = "";
                    if (machine.Upgrade.NextTier.HasValue)
                    {
                        next_tier = machine.Upgrade.NextTier.Value.Id.ToString();
                    }

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> recipeItems = MakeRecipesJsonObject(protosDb, machineRecipes);

                    string machineJson = MakeMachineJsonObject2(
                        id,
                        name,
                        category,
                        next_tier,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        product_type,
                        capacity,
                        unity_cost,
                        research_speed,
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(","),
                        GetSubcategory(machine.Graphics.Categories)
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR" + machine.ToString());
                    Log.Info("###################################################");
                }
            }

            /*
             * -------------------------------------
             * Part 5  - Buildings. Uses Simpler Proto LookUp Method
             * -------------------------------------
            */

            IEnumerable<FarmProto> farms = protosDb.All<FarmProto>();
            IEnumerable<ProductProto> fertilizers = protosDb.Filter<ProductProto>(delegate (ProductProto product)
            {
                return product.GetParam<FertilizerProductParam>().Value != null;
            });
            ProductProto[] fertilizersArray = fertilizers.ToArray<ProductProto>();
            IEnumerable<CropProto> crops = protosDb.All<CropProto>();

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
                    string product_type = "";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";
                    string next_tier = "";
                    if (item.Upgrade.NextTier.HasValue)
                    {
                        next_tier = item.Upgrade.NextTier.Value.Id.ToString();
                    }

                    foreach (ToolbarEntryData cat in item.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(item.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    //recipes are build for max fertility
                    List<string> recipeItems = new List<string> { };
                    foreach (CropProto crop in crops)
                    {
                        if (crop.RequiresGreenhouse && !item.IsGreenhouse)
                            continue;
                        if ((crop.ProductProduced == null) || (crop.ProductProduced.Quantity.Value == 0))
                            continue;

                        var duration = (crop.DaysToGrow * 2);
                        List<string> inputItems = new List<string> { };
                        List<string> outputItems = new List<string> { };

                        string machineRecipeInputJson;
                        string machineRecipeOutputJson;

                        machineRecipeOutputJson = MakeRecipeIOJsonObject(crop.ProductProduced.Product.Strings.Name.ToString(), crop.ProductProduced.Quantity.ScaledBy(item.YieldMultiplier).ToString());
                        outputItems.Add(machineRecipeOutputJson);

                        if (item.HasIrrigationAndFertilizerSupport)
                        {
                            if (crop.ConsumedWaterPerDay.Value != null)
                            {
                                machineRecipeInputJson = MakeRecipeIOJsonObject("Water", (crop.ConsumedWaterPerDay.Value.ScaledBy(item.DemandsMultiplier) * crop.DaysToGrow).ToString());
                                inputItems.Add(machineRecipeInputJson);
                            }
                        }

                        if (item.HasIrrigationAndFertilizerSupport && (fertilizersArray.Length != 0))
                        {
                            foreach (ProductProto fertilizer in fertilizersArray)
                            {
                                List<string> inputItems2 = new List<string>(inputItems);
                                Option<FertilizerProductParam> fertilizerParam = fertilizer.GetParam<FertilizerProductParam>();
                                Fix64 fertilizerFerDay = (crop.ConsumedFertilityPerDay.ToFix64() * crop.DaysToGrow) / fertilizerParam.Value.FertilityPerQuantity.ToFix64();
                                machineRecipeInputJson = MakeRecipeIOJsonObject(fertilizer.Strings.Name.ToString(), fertilizerFerDay.ScaledBy(item.DemandsMultiplier).ToFix32().ToString());
                                inputItems2.Add(machineRecipeInputJson);

                                string machineRecipeJson = MakeRecipeJsonObject(
                                    crop.Id.ToString() + "_" + fertilizer.Id.ToString(),
                                    crop.Strings.Name.ToString() + " " + fertilizer.Strings.Name.ToString(),
                                    duration.ToString(),
                                    inputItems2.JoinStrings(","),
                                    outputItems.JoinStrings(","),
                                    "1"
                                );
                                recipeItems.Add(machineRecipeJson);
                            }
                        }
                        else
                        {
                            string machineRecipeJson = MakeRecipeJsonObject(
                                crop.Id.ToString(),
                                crop.Strings.Name.ToString(),
                                duration.ToString(),
                                inputItems.JoinStrings(","),
                                outputItems.JoinStrings(","),
                                "1"
                            );
                            recipeItems.Add(machineRecipeJson);
                        }

                    }

                    string machineJson = MakeMachineJsonObject2(
                        id,
                        name,
                        category,
                        next_tier,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        product_type,
                        capacity,
                        unity_cost,
                        research_speed,
                        item.IconPath,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(","),
                        GetSubcategory(item.Graphics.Categories)
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
                    string product_type = "";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";
                    string next_tier = "";
                    if (item.Upgrade.NextTier.HasValue)
                    {
                        next_tier = item.Upgrade.NextTier.Value.Id.ToString();
                    }

                    foreach (ToolbarEntryData cat in item.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(item.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    //recipe is built for max animals (500 chickens)
                    List<string> recipeItems = new List<string> { };
                    var duration = 60;

                    List<string> inputItems = new List<string> { };
                    List<string> outputItems = new List<string> { };

                    string machineRecipeInputJson;
                    machineRecipeInputJson = MakeRecipeIOJsonObject(item.FoodPerAnimalPerMonth.Product.Strings.Name.ToString(), (item.AnimalsCapacity * item.FoodPerAnimalPerMonth.Quantity.Value).ToString());
                    inputItems.Add(machineRecipeInputJson);
                    machineRecipeInputJson = MakeRecipeIOJsonObject(item.WaterPerAnimalPerMonth.Product.Strings.Name.ToString(), (item.AnimalsCapacity * item.WaterPerAnimalPerMonth.Quantity.Value).ToString());
                    inputItems.Add(machineRecipeInputJson);

                    string machineRecipeOutputJson;
                    var produced = item.ProducedPerAnimalPerMonth;
                    if (produced != null)
                    {
                        machineRecipeOutputJson = MakeRecipeIOJsonObject(produced.Value.Product.Strings.Name.ToString(), (item.AnimalsCapacity * produced.Value.Quantity.Value).ToString());
                        outputItems.Add(machineRecipeOutputJson);
                    }
                    //must be divided by 100, but according to wiki it produces 10 carcass instead of 20
                    machineRecipeOutputJson = MakeRecipeIOJsonObject(item.CarcassProto.Strings.Name.ToString(), ((item.AnimalsBornPer100AnimalsPerMonth * item.AnimalsCapacity) / 200).ToString());
                    outputItems.Add(machineRecipeOutputJson);

                    string machineRecipeJson = MakeRecipeJsonObject(
                        id,
                        name,
                        duration.ToString(),
                        inputItems.JoinStrings(","),
                        outputItems.JoinStrings(","),
                        "1"
                    );
                    recipeItems.Add(machineRecipeJson);

                    string machineJson = MakeMachineJsonObject2(
                        id,
                        name,
                        category,
                        next_tier,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        product_type,
                        capacity,
                        unity_cost,
                        research_speed,
                        item.IconPath,
                        machinesProducts.JoinStrings(","),
                        machineRecipeJson,
                        GetSubcategory(item.Graphics.Categories)
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
                    string product_type = "";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";
                    string next_tier = "";
                    if (item.Upgrade.NextTier.HasValue)
                    {
                        next_tier = item.Upgrade.NextTier.Value.Id.ToString();
                    }

                    foreach (ToolbarEntryData cat in item.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(item.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject2(
                        id,
                        name,
                        category,
                        next_tier,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        product_type,
                        capacity,
                        unity_cost,
                        research_speed,
                        item.IconPath,
                        machinesProducts.JoinStrings(","),
                        "",
                        GetSubcategory(item.Graphics.Categories)
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
                    string product_type = "";
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
                    string next_tier = "";
                    if (item.Upgrade.NextTier.HasValue)
                    {
                        next_tier = item.Upgrade.NextTier.Value.Id.ToString();
                    }

                    foreach (ToolbarEntryData cat in item.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(item.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject2(
                        id,
                        name,
                        category,
                        next_tier,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        product_type,
                        capacity,
                        unity_cost,
                        research_speed,
                        item.IconPath,
                        machinesProducts.JoinStrings(","),
                        "",
                        GetSubcategory(item.Graphics.Categories)
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
                    Fix32 research_speed = (60 / item.DurationOfRecipe.Seconds) * item.SciencePerRecipe;
                    string product_type = "";
                    string capacity = "0";
                    string computing_generated = "0";
                    string next_tier = "";
                    if (item.Upgrade.NextTier.HasValue)
                    {
                        next_tier = item.Upgrade.NextTier.Value.Id.ToString();
                    }

                    if (item.Id.ToString() != "ResearchLab1")
                    {

                        List<string> recipeItems = MakeRecipesJsonObject(protosDb, item.Recipes.AsEnumerable(), id, name);
                        recipes = recipeItems.JoinStrings(",");
                    }

                    foreach (ToolbarEntryData cat in item.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(item.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in item.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject2(
                        id,
                        name,
                        category,
                        next_tier,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        product_type,
                        capacity,
                        unity_cost,
                        research_speed.ToString(),
                        item.IconPath,
                        machinesProducts.JoinStrings(","),
                        recipes,
                        GetSubcategory(item.Graphics.Categories)
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
                    string product_type = "";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";
                    string next_tier = "";
                    if (machine.Upgrade.NextTier.HasValue)
                    {
                        next_tier = machine.Upgrade.NextTier.Value.Id.ToString();
                    }

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject2(
                        id,
                        name,
                        category,
                        next_tier,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        product_type,
                        capacity,
                        unity_cost,
                        research_speed,
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        "",
                        GetSubcategory(machine.Graphics.Categories)
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
                    string product_type = "";
                    string capacity = machine.Capacity.ToString();
                    string unity_cost = "0";
                    string research_speed = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string next_tier = "";
                    if (machine.Upgrade.NextTier.HasValue)
                    {
                        next_tier = machine.Upgrade.NextTier.Value.Id.ToString();
                    }

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    string machineJson = MakeMachineJsonObject2(
                        id,
                        name,
                        category,
                        next_tier,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        product_type,
                        capacity,
                        unity_cost,
                        research_speed,
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        "",
                        GetSubcategory(machine.Graphics.Categories)
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
             * Part - Products.
             * -------------------------------------
            */
            List<string> productsJson = new List<string> { };

            List<string> countProdNames = new List<string> { };
            List<string> looseProdNames = new List<string> { };
            List<string> fluidProdNames = new List<string> { };
            List<string> moltenProdNames = new List<string> { };
            List<string> virtualProdNames = new List<string> { };

            IEnumerable<ProductProto> products = protosDb.All<ProductProto>();
            foreach (ProductProto product in products)
            {
                string type = null;
                if (product is CountableProductProto)
                {
                    countProdNames.Add(product.Strings.Name.ToString());
                    type = product.Type.ToString();
                }
                else if (product is LooseProductProto)
                {
                    looseProdNames.Add(product.Strings.Name.ToString());
                    type = product.Type.ToString();
                }
                else if (product is FluidProductProto)
                {
                    fluidProdNames.Add(product.Strings.Name.ToString());
                    type = product.Type.ToString();
                }
                else if (product is MoltenProductProto)
                {
                    moltenProdNames.Add(product.Strings.Name.ToString());
                    type = product.Type.ToString();
                }
                else if (product is VirtualProductProto)
                {
                    virtualProdNames.Add(product.Strings.Name.ToString());
                    type = product.Type.ToString();
                }
                if (type != null)
                {
                    productsJson.Add(MakeProductJsonObject(
                        product.Id.ToString(),
                        product.Strings.Name.ToString(),
                        type,
                        product.IconPath));
                }
            }

            File.WriteAllText("c:/temp/products.json", $"{{\"game_version\":\"{game_version}\",\"products\":[{productsJson.JoinStrings(",")}]}}");

            List<string> storageItems = new List<string> { };

            //NuclearWasteStorageProto is also instance of StorageProto
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
                    string product_type = machine.ProductType.Value.ToString();
                    string capacity = machine.Capacity.ToString();
                    string unity_cost = "0";
                    string research_speed = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string next_tier = "";
                    if (machine.Upgrade.NextTier.HasValue)
                    {
                        next_tier = machine.Upgrade.NextTier.Value.Id.ToString();
                    }

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> StorageInputs = new List<string> { };

                    if (machine.ProductType.Value.ToString() == "CountableProductProto")
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
                            recipe_name,
                            recipe_duration,
                            inputItems.JoinStrings(","),
                            outputItems.JoinStrings(","),
                            "1"
                        );
                        recipeItems.Add(machineRecipeJson);

                    }

                    string machineJson = MakeMachineJsonObject2(
                        id,
                        name,
                        category,
                        next_tier,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        product_type,
                        capacity,
                        unity_cost,
                        research_speed,
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        "",
                        GetSubcategory(machine.Graphics.Categories)
                    );
                    machineItems.Add(machineJson);

                    string storageJson = MakeMachineJsonObject2(
                        id,
                        name,
                        category,
                        next_tier,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        product_type,
                        capacity,
                        unity_cost,
                        research_speed,
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(","),
                        GetSubcategory(machine.Graphics.Categories)
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

            IEnumerable<ThermalStorageProto> thermalStorages = protosDb.All<ThermalStorageProto>();
            foreach (ThermalStorageProto machine in thermalStorages)
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
                    string product_type = machine.HeatProduct.Strings.Name.ToString();
                    string capacity = machine.Capacity.ToString();
                    string unity_cost = "0";
                    string research_speed = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string next_tier = "";

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> recipeItems = new List<string> { };
                    if (machine.Recipes != null)
                    {
                        recipeItems = MakeRecipesJsonObject(protosDb, machine.Recipes.AsEnumerable(), id, name);
                    }

                    string machineJson = MakeMachineJsonObject2(
                        id,
                        name,
                        category,
                        next_tier,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        product_type,
                        capacity,
                        unity_cost,
                        research_speed,
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(","),
                        GetSubcategory(machine.Graphics.Categories)
                    );
                    machineItems.Add(machineJson);

                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR ThermalStorage" + machine.Id.ToString());
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

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
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
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        "",
                        GetSubcategory(machine.Graphics.Categories)
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

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
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
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        "",
                        GetSubcategory(machine.Graphics.Categories)
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

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
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
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        "",
                        GetSubcategory(machine.Graphics.Categories)
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

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
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
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        "",
                        GetSubcategory(machine.Graphics.Categories)
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

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
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
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        "",
                        GetSubcategory(machine.Graphics.Categories)
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
                    string computing_consumed = machine.ComputingConsumed.Value.ToString();
                    string computing_generated = "0";
                    string product_type = "";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";
                    string next_tier = "";
                    if (machine.Upgrade.NextTier.HasValue)
                    {
                        next_tier = machine.Upgrade.NextTier.Value.Id.ToString();
                    }

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    //nuclear reactor gives all recipes at max power level
                    //it also contain enrichment recipe, but all values are 0, so we should use fix all recipes here
                    //we ignore provided recipes, and build our own for min and max power level
                    //List<string> recipeItems = MakeRecipesJsonObject(protosDb, machine.Recipes.AsEnumerable(), id, name);

                    List<string> recipeItems = new List<string> { };

                    //default recipes are actually build from reactor data itself (at max power level)
                    //here we rebuild these virtual recipes for all power level and for all enrichment levels
                    //nuclear reactor recipe can count everything by itself at any power level
                    //we should do some nasty enumeration to keep recipe ids backward compatible
                    var maxPowerLevel = machine.MaxPowerLevel;

                    //we keep recipe naming convention and order with prev version
                    //it is ugly, but backward compatible
                    int i = 0;
                    int[] compatPowerLevels = new int[] { machine.MaxPowerLevel, 1 };
                    int[] compatEnrichmentLevels = new int[] { 2 };
                    // must walk from max to min, to keep naming compatible
                    for (int powerLevel = maxPowerLevel; powerLevel >= 1; powerLevel--)
                    {
                        int fuelIndex = 1;
                        foreach (var fuel in machine.FuelPairs)
                        {
                            string recipe_id;
                            string recipe_name;
                            if (compatPowerLevels.Contains(powerLevel))
                            {
                                recipe_id = (id + ((i != 0) ? i.ToString() : ""));
                                recipe_name = (name + ((i != 0) ? (" " + i.ToString()) : ""));
                                i++;
                            }
                            else
                            {
                                recipe_id = (id + "-Fuel" + fuelIndex + "-PowerLevel" + powerLevel);
                                recipe_name = (name + " Fuel " + fuelIndex + " PowerLevel " + powerLevel);
                            }
                            fuelIndex++;

                            NuclearReactor.Recipe recipe;

                            List<string> inputItems;
                            List<string> outputItems;

                            string machineRecipeJson;
                            var powerLevelPercent = powerLevel * Percent.Hundred;

                            // there is no point in recipes without enrichment, if machine supports it
                            // we keep it to calculate proper ids for backward compatible names
                            if (!machine.Enrichment.HasValue)
                            {
                                recipe = new NuclearReactor.Recipe(machine, fuel, powerLevelPercent, 1, Percent.Hundred);
                                inputItems = new List<string> { };
                                outputItems = new List<string> { };

                                recipe.AllUserVisibleInputs.ForEach(delegate (RecipeInput input)
                                {
                                    Option<ProductProto> product = protosDb.Get<ProductProto>(input.Product.Id);
                                    string machineRecipeInputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), input.Quantity.Value.ToString());
                                    inputItems.Add(machineRecipeInputJson);
                                });

                                recipe.AllUserVisibleOutputs.ForEach(delegate (RecipeOutput output)
                                {
                                    Option<ProductProto> product = protosDb.Get<ProductProto>(output.Product.Id);
                                    string machineRecipeOutputJson = MakeRecipeIOJsonObject(output.Product.Strings.Name.ToString(), output.Quantity.Value.ToString());
                                    outputItems.Add(machineRecipeOutputJson);
                                });

                                machineRecipeJson = MakeRecipeJsonObject(
                                    recipe_id,
                                    recipe_name,
                                    recipe.Duration.Seconds.ToString(),
                                    inputItems.JoinStrings(","),
                                    outputItems.JoinStrings(","),
                                    "1"
                                );

                                recipeItems.Add(machineRecipeJson);
                            }

                            //merge enrichment recipe, actually they are independent and could be executed both
                            //because upkeep is not increaed, we should merge these
                            if (machine.Enrichment.HasValue)
                            {
                                var enrichment = machine.Enrichment.Value;
                                var enrichmentLevel = 1;
                                foreach (var enrichmentStep in enrichment.EnrichmentSteps)
                                {
                                    recipe = new NuclearReactor.Recipe(machine, fuel, powerLevelPercent, enrichmentStep.SteamReductionDiv, enrichmentStep.FuelMultiplier);
                                    inputItems = new List<string> { };
                                    outputItems = new List<string> { };

                                    var fuelPerMinute = recipe.FuelPerMinute;
                                    if (fuelPerMinute == null)
                                    {
                                        // coi bug (in sources)??? recalc if null
                                        fuelPerMinute = (recipe.Duration.Ticks / fuel.Duration.Ticks).ScaledByRounded(powerLevelPercent).Quantity();
                                    }
                                    // why it isn't calculated automatically??
                                    var fuelPerDuration = Fix32.FromInt(fuelPerMinute.Value).ScaledBy(recipe.FuelMultiplier) * recipe.Duration.Ticks / Duration.OneMinute.Ticks;

                                    recipe.AllUserVisibleInputs.ForEach(delegate (RecipeInput input)
                                    {
                                        var quantity = Fix32.FromInt(input.Quantity.Value);
                                        if (input.Product.Id == fuel.FuelInProto.Id)
                                        {
                                            quantity = fuelPerDuration;
                                        }
                                        string machineRecipeInputJson = MakeRecipeIOJsonObject(input.Product.Strings.Name.ToString(), quantity.ToString());
                                        inputItems.Add(machineRecipeInputJson);
                                    });

                                    recipe.AllUserVisibleOutputs.ForEach(delegate (RecipeOutput output)
                                    {
                                        var quantity = Fix32.FromInt(output.Quantity.Value);
                                        if (output.Product.Id == fuel.SpentFuelOutProto.Id)
                                        {
                                            quantity = fuelPerDuration;
                                        }
                                        string machineRecipeOutputJson = MakeRecipeIOJsonObject(output.Product.Strings.Name.ToString(), quantity.ToString());
                                        outputItems.Add(machineRecipeOutputJson);
                                    });

                                    var haveEnrichment = enrichmentStep.BreedingRatio != 0;
                                    if (haveEnrichment)
                                    {
                                        var enrichmentSpentPerDuration = fuelPerDuration * enrichmentStep.BreedingRatio;
                                        var enrichmentProcessedPerDuration = enrichmentSpentPerDuration * enrichment.ProcessedPerLevel;

                                        machineRecipeJson = MakeRecipeIOJsonObject(enrichment.InputProduct.Strings.Name.ToString(), enrichmentSpentPerDuration.ToString());
                                        inputItems.Add(machineRecipeJson);
                                        machineRecipeJson = MakeRecipeIOJsonObject(enrichment.OutputProduct.Strings.Name.ToString(), enrichmentProcessedPerDuration.ToString());
                                        outputItems.Add(machineRecipeJson);
                                    }

                                    //for backward compatibility we use above (null) recipe ids for zero enrichment
                                    if (haveEnrichment)
                                    {
                                        if (compatPowerLevels.Contains(powerLevel) && compatEnrichmentLevels.Contains(enrichmentLevel))
                                        {
                                            recipe_id = (id + ((i != 0) ? i.ToString() : ""));
                                            recipe_name = (name + ((i != 0) ? (" " + i.ToString()) : ""));
                                            i++;
                                        }
                                        else
                                        {
                                            recipe_id = (id + "-PowerLevel" + powerLevel + "-Enrichment" + enrichmentLevel);
                                            recipe_name = (name + " PowerLevel " + powerLevel + " Enrichment " + enrichmentLevel);
                                        }
                                    }

                                    machineRecipeJson = MakeRecipeJsonObject(
                                        recipe_id,
                                        recipe_name,
                                        recipe.Duration.Seconds.ToString(),
                                        inputItems.JoinStrings(","),
                                        outputItems.JoinStrings(","),
                                        "1"
                                    );

                                    recipeItems.Add(machineRecipeJson);
                                    enrichmentLevel++;
                                }
                            }
                        }
                    }

                    string machineJson = MakeMachineJsonObject2(
                        id,
                        name,
                        category,
                        next_tier,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        product_type,
                        capacity,
                        unity_cost,
                        research_speed,
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(","),
                        GetSubcategory(machine.Graphics.Categories)
                    );
                    machineItems.Add(machineJson);

                    DumpObject(DUMP, id, machine);
                    DumpObject(DUMP, id + "FuelPairs", machine.FuelPairs.AsEnumerable());
                    DumpObject(DUMP, id + "Enrichment", machine.Enrichment.HasValue ? machine.Enrichment.Value : null);
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

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
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
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        "",
                        GetSubcategory(machine.Graphics.Categories)
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

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
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
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        "",
                        GetSubcategory(machine.Graphics.Categories)
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
                    string product_type = "";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";
                    string next_tier = "";
                    if (machine.Upgrade.NextTier.HasValue)
                    {
                        next_tier = machine.Upgrade.NextTier.Value.Id.ToString();
                    }

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    List<string> recipeItems = MakeRecipesJsonObject(protosDb, machine.Recipes.AsEnumerable(), id, name);

                    string machineJson = MakeMachineJsonObject2(
                        id,
                        name,
                        category,
                        next_tier,
                        workers,
                        maintenance_cost_units,
                        maintenance_cost_quantity,
                        electricity_consumed,
                        electricity_generated,
                        computing_consumed,
                        computing_generated,
                        product_type,
                        capacity,
                        unity_cost,
                        research_speed,
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(","),
                        GetSubcategory(machine.Graphics.Categories)
                    );
                    machineItems.Add(machineJson);

                    DumpObject(DUMP, id, machine);
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

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    //machine.WaterCollectedPerDay.ToString() - wiki says 35-40 Units per year on average
                    //one day = 2 in game seconds, one month = 60 in game seconds
                    string machineRecipeOutputJson = MakeRecipeIOJsonObject(machine.WaterProto.Strings.Name.ToString(), "37");
                    string machineRecipeJson = MakeRecipeJsonObject(
                        id,
                        name,
                        "720",
                        "",
                        machineRecipeOutputJson,
                        "1"
                    );

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
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        machineRecipeJson,
                        GetSubcategory(machine.Graphics.Categories)
                    );
                    machineItems.Add(machineJson);

                    DumpObject(DUMP, id, machine);
                }
                catch
                {
                    Log.Info("###################################################");
                    Log.Info("ERROR Depot" + machine.Id.ToString());
                    Log.Info("###################################################");
                }
            }

            IEnumerable<DataCenterProto> dataCenters = protosDb.All<DataCenterProto>();
            IEnumerable<ServerRackProto> dataRacks = protosDb.All<ServerRackProto>();
            foreach (DataCenterProto machine in dataCenters)
            {

                try
                {

                    string id = machine.Id.ToString();
                    string name = machine.Strings.Name.ToString();
                    string category = "";
                    string workers = machine.Costs.Workers.ToString();
                    string maintenance_cost_units = "";
                    string maintenance_cost_quantity = "0";
                    string electricity_consumed = "0";
                    string electricity_generated = "0";
                    string computing_consumed = "0";
                    string computing_generated = "0";
                    string capacity = "0";
                    string unity_cost = "0";
                    string research_speed = "0";

                    foreach (ToolbarEntryData cat in machine.Graphics.Categories)
                    {
                        category = cat.CategoryProto.Strings.Name.ToString();
                    }
                    category = GetTopLevelCategory(machine.Graphics.Categories, category);

                    List<string> machinesProducts = new List<string> { };

                    foreach (ProductQuantity cost in machine.Costs.BaseConstructionCost.Products)
                    {
                        string vehicleProductJson = MakeVehicleProductJsonObject(
                            cost.Product.Strings.Name.ToString(),
                            cost.Quantity.ToString()
                        );
                        machinesProducts.Add(vehicleProductJson);
                    }

                    //generate recipe on max server racks (48)
                    int racks_capacity = machine.RacksCapacity;
                    List<string> recipeItems = new List<string> { };
                    foreach (ServerRackProto dataRack in dataRacks)
                    {
                        string maintenance_cost_units1 = machine.Costs.Maintenance.Product.Strings.Name.ToString();
                        string maintenance_cost_quantity1 = (machine.Costs.Maintenance.MaintenancePerMonth.Value + (racks_capacity * dataRack.Maintenance.Value)).ToString();

                        string recipe_name = dataRack.Strings.Name.ToString();
                        string recipe_duration = "60";

                        List<string> inputItems = new List<string> { };
                        List<string> outputItems = new List<string> { };

                        string machineRecipeInputJson;
                        machineRecipeInputJson = MakeRecipeIOJsonObject(machine.CoolantIn.Strings.Name.ToString(), (racks_capacity * dataRack.CoolantInPerMonth.Value).ToString());
                        inputItems.Add(machineRecipeInputJson);
                        machineRecipeInputJson = MakeRecipeIOJsonObject(maintenance_cost_units1, maintenance_cost_quantity1);
                        inputItems.Add(machineRecipeInputJson);
                        machineRecipeInputJson = MakeRecipeIOJsonObject("Electricity", (racks_capacity * dataRack.ConsumedPowerPerTick.Value).ToString());
                        inputItems.Add(machineRecipeInputJson);

                        string machineRecipeOutputJson;
                        machineRecipeOutputJson = MakeRecipeIOJsonObject(machine.CoolantOut.Strings.Name.ToString(), (racks_capacity * dataRack.CoolantOutPerMonth.Value).ToString());
                        outputItems.Add(machineRecipeOutputJson);
                        machineRecipeOutputJson = MakeRecipeIOJsonObject("Computing", (racks_capacity * dataRack.CreatedComputingPerTick.Value).ToString());
                        outputItems.Add(machineRecipeOutputJson);

                        string machineRecipeJson = MakeRecipeJsonObject(
                            dataRack.Id.ToString(),
                            recipe_name,
                            recipe_duration,
                            inputItems.JoinStrings(","),
                            outputItems.JoinStrings(","),
                            "1"
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
                        machine.IconPath,
                        machinesProducts.JoinStrings(","),
                        recipeItems.JoinStrings(","),
                        GetSubcategory(machine.Graphics.Categories)
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

            foreach (ServerRackProto machine in dataRacks)
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
                        "",
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
                    string mining_hardness = "";
                    string mined_quantity_per_tile_cubed = material.MinedQuantityPerTileCubed.ToString();
                    string disruption_recovery_time = material.DisruptionRecoveryTime.ToString();
                    string is_hardened_floor = "false";
                    string max_collapse_height_diff = material.MaxCollapseHeightDiff.ToString();
                    string min_collapse_height_diff = material.MinCollapseHeightDiff.ToString();
                    string mined_quantity_mult = material.MinedQuantityMult.ToString();
                    string vehicle_traversal_cost = "0";

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

            List<string> contractItems = new List<string> { };

            IEnumerable<ContractProto> contracts = protosDb.All<ContractProto>();

            foreach (ContractProto contract in contracts)
            {

                string contractJson = MakeContractJsonObject(
                    contract.Id.ToString(),
                    contract.ProductToBuy.Strings.Name.ToString(),
                    contract.GetQuantityToBuy(Percent.Hundred).ToString(),
                    contract.ProductToPayWith.Strings.Name.ToString(),
                    contract.QuantityToPayWith.ToString(),
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
                * Part - Transport
                * -------------------------------------
            */
            List<string> transportItems = new List<string> { };
            IEnumerable<TransportProto> transports = protosDb.All<TransportProto>();
            foreach (TransportProto transport in transports)
            {
                string category = "";
                foreach (ToolbarEntryData cat in transport.Graphics.Categories)
                {
                    category = cat.CategoryProto.Strings.Name.ToString();
                }
                category = GetTopLevelCategory(transport.Graphics.Categories, category);
                string next_tier = "";
                if (transport.Upgrade.NextTier.HasValue)
                {
                    next_tier = transport.Upgrade.NextTier.Value.Id.ToString();
                }
                string maintenance_cost_units = "";
                string maintenance_cost_quantity = "0";

                List<string> machinesProducts = new List<string> { };

                foreach (ProductQuantity cost in transport.Costs.BaseConstructionCost.Products)
                {
                    string vehicleProductJson = MakeVehicleProductJsonObject(
                        cost.Product.Strings.Name.ToString(),
                        cost.Quantity.ToString()
                    );
                    machinesProducts.Add(vehicleProductJson);
                }

                string transportsJson = MakeTransportJsonObject(
                    transport.Id.ToString(),
                    transport.Strings.Name.ToString(),
                    category,
                    next_tier,
                    maintenance_cost_units,
                    maintenance_cost_quantity,
                    transport.BaseElectricityCost.Value.ToString(),
                    (transport.ThroughputPerTick.Value * 10).ToString(),
                    transport.LengthPerCost.Value.ToString(),
                    transport.IconPath,
                    machinesProducts.JoinStrings(","),
                    GetSubcategory(transport.Graphics.Categories)
                );
                transportItems.Add(transportsJson);

            }

            File.WriteAllText("c:/temp/transports.json", $"{{\"game_version\":\"{game_version}\",\"transports\":[{transportItems.JoinStrings(",")}]}}");

            /*
                * -------------------------------------
                * Part 7  - Final JSON Export
                * -------------------------------------
            */

            File.WriteAllText("c:/temp/machines_and_buildings.json", $"{{\"game_version\":\"{game_version}\",\"machines_and_buildings\":[{machineItems.JoinStrings(",")}]}}");
            File.WriteAllText("c:/temp/storages.json", $"{{\"game_version\":\"{game_version}\",\"storages\":[{storageItems.JoinStrings(",")}]}}");

            /*
                * -------------------------------------
                * TODO - retrieve Mafi.Unity.AssetsDb instance, and get UnityEngine.Texture2D by icon path.
                * Then use UnityEngine.ImageConversionModule.ImageConversion to convert texture to png, and export it to file.
                * -------------------------------------
            */

            if (DUMP.Count != 0)
            {
                File.WriteAllLines("c:/temp/dump.txt", DUMP);
            }
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

        public void Initialize(DependencyResolver resolver, bool gameWasLoaded)
        {

        }

        public void Register(ImmutableArray<DataOnlyMod> mods, RegistrationContext context) { }

        public void EarlyInit(DependencyResolver resolver)
        {
        }
    }
}
