const fs = require("fs")
const data = require("../data/contracts.json")

function productParseName(name) {
    let words = name.split(" ")
    for (let i = 0; i < words.length; i++) {
        words[i] = words[i][0].toUpperCase() + words[i].substr(1)
    }
    return words.join(' ')
}

const getRowTemplate = (row) => `
{{contract define
    | id = ${row.id}
    | ProductBuyName = ${productParseName(row.product_to_buy_name)}
    | ProductBuyQty = ${row.product_to_buy_quantity}
    | ProductPayName = ${productParseName(row.product_to_pay_with_name)}
    | ProductPayQty = ${row.product_to_pay_with_quantity}
    | UnityMonth = ${row.unity_per_month}
    | UnityQty = ${row.unity_per_100_bought}
    | UnityEst = ${row.unity_to_establish}
    | MinReputation = ${row.min_reputation_required}
}}
`

let contractRows = data.contracts.map(row=>{
    return getRowTemplate(row)
})

fs.writeFileSync('./data/contracts_table.txt', contractRows.join(''), "utf8")