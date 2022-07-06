const fs = require("fs")

const rawData = require("../data/machines_and_buildings.json")
const rawDataMines = require("./data/mines.json")
const rawDataStorages = require("./data/storages.json")

function categoryNameToId(name) {
    return typeof name === 'string' && name.length ? name.replaceAll('&', 'and').toLowerCase().replaceAll(' ', '_').replaceAll('(', '').replaceAll(')', '') : null
}

function categoryParseName(name) {
    let words = name.replaceAll('(', '( ').replaceAll(')', ' )').split(" ");
    for (let i = 0; i < words.length; i++) {
        words[i] = words[i][0].toUpperCase() + words[i].substr(1)
    }
    return words.join(' ').replaceAll('( ', '(').replaceAll(' )', ')')
}

function machineNameToId(name) {
    return typeof name === 'string' && name.length ? name.toLowerCase().replaceAll(' ', '_').replaceAll('(', '').replaceAll(')', '') : null
}

function machineParseName(name) {
    let words = name.replaceAll('(', '( ').replaceAll(')', ' )').split(" ")
    for (let i = 0; i < words.length; i++) {
        words[i] = words[i][0].toUpperCase() + words[i].substr(1)
    }
    return words.join(' ').replaceAll('( ', '(').replaceAll(' )', ')')
}

function recipeNameToId(name) {
    return typeof name === 'string' && name.length ? name.replaceAll('&', 'and').toLowerCase().replaceAll(' ', '_').replaceAll('(', '').replaceAll(')', '') : null
}

function recipeParseName(name) {
    let words = name.replaceAll('(', '( ').replaceAll(')', ' )').split(" ")
    for (let i = 0; i < words.length; i++) {
        words[i] = words[i][0].toUpperCase() + words[i].substr(1)
    }
    return words.join(' ').replaceAll('( ', '(').replaceAll(' )', ')')
}

function productNameToId(name) {
    return typeof name === 'string' && name.length ? name.toLowerCase().replaceAll(' ', '_').replaceAll('(', '').replaceAll(')', '') : null
}

function productParseName(name) {
    let isSet = typeof name === 'string' && name.length
    if (!isSet) return null
    let words = name.replaceAll('(', '( ').replaceAll(')', ' )').split(" ")
    for (let i = 0; i < words.length; i++) {
        words[i] = words[i][0].toUpperCase() + words[i].substr(1)
    }
    return words.join(' ').replaceAll('( ', '(').replaceAll(' )', ')')
}

function productNameToIcon(n) {
    let name = n
    name = name.replaceAll('(', '')
    name = name.replaceAll(')', '')
    name = name.endsWith(' IV') ? name.replaceAll(' IV', '4') : name
    name = name.endsWith(' V') ? name.replaceAll(' V', '5') : name
    name = name.endsWith(' III') ? name.replaceAll(' III', '3') : name
    name = name.endsWith(' II') ? name.replaceAll(' II', '2') : name
    name = name.endsWith(' I') ? name.replaceAll(' I', '1') : name
    name = name.replaceAll(' ', '')
    return name
}

function sortObject(unordered) {
    return Object.keys(unordered).sort().reduce(
        (obj, key) => {
            obj[key] = unordered[key];
            return obj;
        },
        {}
    );
}

function calculateProduct60(originalDuration, quantity) {
    if (originalDuration === 0) return quantity
    return (60 / originalDuration) * quantity
}

const storages = [
    'StorageUnitT4',
    'StorageUnitT3',
    'StorageUnitT2',
    'StorageUnit',
    'StorageLooseT4',
    'StorageLooseT3',
    'StorageLooseT2',
    'StorageLoose',
    'StorageFluidT4',
    'StorageFluidT3',
    'StorageFluidT2',
    'StorageFluid',
    'NuclearWasteStorage',
]

const mines = [
    'MineTower'
]

const farms = []

let CATEGORIES_DATA = {}
let MACHINES_DATA = {}
let RECIPES_DATA = {}
let PRODUCTS_DATA = {}

let buildings = [
    ...rawData.machines_and_buildings,
    ...rawDataMines.mines
]

function parseBuildings(items, override = false) {

    items.forEach(m => {

        //## ##################
        //## Prep Items
        //## ##################

        // Prep Category
        let category = {
            id: categoryNameToId(m.category),
            name: categoryParseName(m.category),
            machines: [],
            recipes: []
        }

        // Prep Machine
        let machine = {
            id: machineNameToId(m.name),
            game_id: m.id,
            icon: `${m.id}.png`,
            name: machineParseName(m.name),
            category_id: categoryNameToId(m.category),
            category_name: categoryParseName(m.category),
            isMine: mines.indexOf(m.id) > -1,
            isStorage: storages.indexOf(m.id) > -1,
            isFarm: farms.indexOf(m.id) > -1,
            workers: m.workers,
            maintenance_cost_units: productNameToId(m.maintenance_cost_units),
            maintenance_cost_quantity: m.maintenance_cost_quantity,
            electricity_consumed: m.electricity_consumed,
            electricity_generated: m.electricity_generated,
            computing_consumed: m.computing_consumed,
            computing_generated: m.computing_generated,
            storage_capacity: m.storage_capacity,
            unity_cost: m.unity_cost,
            research_speed: m.research_speed,
            build_costs: [],
            recipes: [],
            products: {
                input: [],
                output: []
            }
        }

        m.build_costs.forEach(c => {
            let { product, quantity } = c
            let cost = {
                id: productNameToId(product),
                name: product,
                quantity
            }
            machine.build_costs.push(cost)
        })

        // Prep Recipes

        m.recipes.forEach(r => {

            try {

                let recipeId = recipeNameToId(r.name)

                if (RECIPES_DATA.hasOwnProperty(recipeId)) {
                    let dupsCount = Object.keys(RECIPES_DATA).filter(r => r.startsWith(recipeId+'_')).length + 1
                    recipeId += "_" + ( dupsCount + 1 )
                }

                let recipe = {
                    id: recipeId,
                    name: recipeParseName(r.name),
                    machine: machine.id,
                    duration: 60, //r.duration,
                    inputs: [],
                    outputs: []
                }

                // Set Products

                r.inputs.forEach(p => {

                    let product = {
                        id: productNameToId(p.name),
                        name: productParseName(p.name),
                        quantity: calculateProduct60(r.duration, p.quantity)
                    }

                    // Add New Product
                    if (!PRODUCTS_DATA.hasOwnProperty(product.id)) {
                        if (!product.name) {
                            console.log('NoProductName', product, recipeId)
                        }
                        PRODUCTS_DATA[product.id] = {
                            id: product.id,
                            name: product.name,
                            icon: `${productNameToIcon(product.name)}.png`,
                            recipes: {
                                input: [],
                                output: [],
                            },
                            machines: {
                                input: [],
                                output: [],
                            }
                        }
                    }

                    // Update Product
                    if (PRODUCTS_DATA.hasOwnProperty(product.id)) {
                        if (PRODUCTS_DATA[product.id].recipes.input.indexOf(recipeId) < 0) {
                            PRODUCTS_DATA[product.id].recipes.input = [...PRODUCTS_DATA[product.id].recipes.input, recipeId].sort((a, b) => a.localeCompare(b))
                        }
                    }

                    if (PRODUCTS_DATA[product.id].machines.input.indexOf(machine.id) < 0) {
                        PRODUCTS_DATA[product.id].machines.input = [...PRODUCTS_DATA[product.id].machines.input, machine.id].sort((a, b) => a.localeCompare(b))
                    }

                    if (machine.products.input.indexOf(product.id) < 0) {
                        machine.products.input = [...machine.products.input, product.id].sort((a, b) => a.localeCompare(b))
                    }

                    recipe.inputs.push(product)

                })

                r.outputs.forEach(p => {

                    let product = {
                        id: productNameToId(p.name),
                        name: productParseName(p.name),
                        quantity: calculateProduct60(r.duration, p.quantity)
                    }

                    // Add New Product
                    if (!PRODUCTS_DATA.hasOwnProperty(product.id)) {
                        if (!product.name) {
                            console.log('NoProductName', product, recipeId)
                        }
                        PRODUCTS_DATA[product.id] = {
                            id: product.id,
                            name: product.name,
                            icon: `${productNameToIcon(product.name)}.png`,
                            recipes: {
                                input: [],
                                output: [],
                            },
                            machines: {
                                input: [],
                                output: [],
                            }
                        }
                    }

                    // Update Product
                    if (PRODUCTS_DATA.hasOwnProperty(product.id)) {

                        if (PRODUCTS_DATA[product.id].recipes.output.indexOf(recipeId) < 0) {
                            PRODUCTS_DATA[product.id].recipes.output = [...PRODUCTS_DATA[product.id].recipes.output, recipeId].sort((a, b) => a.localeCompare(b))
                        }

                        if (PRODUCTS_DATA[product.id].machines.output.indexOf(machine.id) < 0) {
                            PRODUCTS_DATA[product.id].machines.output = [...PRODUCTS_DATA[product.id].machines.output, machine.id].sort((a, b) => a.localeCompare(b))
                        }

                    }

                    if (machine.products.output.indexOf(product.id) < 0) {
                        machine.products.output = [...machine.products.output, product.id].sort((a, b) => a.localeCompare(b))
                    }

                    recipe.outputs.push(product)

                })

                // Add New Recipe
                if (!RECIPES_DATA.hasOwnProperty(recipe.id)) {
                    RECIPES_DATA[recipe.id] = {
                        ...recipe
                    }
                }

                machine.recipes.push(recipe.id)

            } catch (e) {
                console.log(r)
                console.error(e)
            }

        })

        //## ##################
        //## Set Lists
        //## ##################

        // Add New Category
        if (!CATEGORIES_DATA.hasOwnProperty(category.id)) {
            CATEGORIES_DATA[category.id] = category
        }

        // Update Category
        if (CATEGORIES_DATA.hasOwnProperty(category.id)) {
            CATEGORIES_DATA[category.id] = {
                ...CATEGORIES_DATA[category.id],
                machines: [
                    ...CATEGORIES_DATA[category.id].machines,
                    machine.id
                ].sort((a, b) => a.localeCompare(b))
            }
            machine.recipes.forEach(r => {
                if (CATEGORIES_DATA[category.id].recipes.indexOf(r) < 0) {
                    CATEGORIES_DATA[category.id].recipes = [...CATEGORIES_DATA[category.id].recipes, r].sort((a, b) => a.localeCompare(b))
                }
            })

        }

        // Add New Machine
        if (!MACHINES_DATA.hasOwnProperty(machine.id) || override === true) {
            MACHINES_DATA[machine.id] = {
                ...machine
            }
        }

        // Update Machine
        if (MACHINES_DATA.hasOwnProperty(machine.id)) {
            MACHINES_DATA[machine.id].recipes = machine.recipes.sort((a, b) => a.localeCompare(b))
        }

    })

}

parseBuildings(buildings)
parseBuildings(rawDataStorages.storages, true)

let recipesOutput = sortObject(RECIPES_DATA)
let categoriesOutput = sortObject(CATEGORIES_DATA)
let machinesOutput = sortObject(MACHINES_DATA)
let productsOutput = sortObject(PRODUCTS_DATA)

fs.writeFileSync(`./data/recipes.json`, JSON.stringify(recipesOutput, null, 4), "utf8")
fs.writeFileSync('./data/categories.json', JSON.stringify(categoriesOutput, null, 4), "utf8")
fs.writeFileSync('./data/machines.json', JSON.stringify(machinesOutput, null, 4), "utf8")
fs.writeFileSync('./data/products.json', JSON.stringify(productsOutput, null, 4), "utf8")