const fs = require("fs")
const machinesData = require("../calculator/data/machines.json")
const productsData = require("../calculator/data/products.json")

const storagesMap = {
    'unit_storage': 'Unit',
    'loose_storage': 'Loose',
    'fluid_storage': 'Fluid'
}

const getRowTemplate = (row) => `
{{product define
    | Name = ${row.name}
    | State = ${row.type}
}}`

let itemRows = ['<includeonly>']

Object.keys(storagesMap).forEach(machineId=>{
    let productType = storagesMap[machineId]
    let machineProducts = machinesData[machineId].products.input
    machineProducts.forEach(productId=>{
        let product = productsData[productId]
        itemRows.push(getRowTemplate({
            name: product.name,
            type: productType
        }))
    })
})

itemRows.push('</includeonly><noinclude>{{Documentation}}</noinclude>')

fs.writeFileSync('./data/products_table.txt', itemRows.join(''), "utf8")