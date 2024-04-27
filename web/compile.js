const path = require('path');
const fs = require('fs');
const solc = require('solc');

const solName = 'DiceGame';
// read file from ../contracts/DiceGame.sol
// join path
const solPath = path.resolve(__dirname, '../contracts', solName + '.sol');
const source = fs.readFileSync(solPath, 'UTF-8');
const sources = {};
sources[solName] = { content: source };
const input = {
    language: 'Solidity',
    sources: sources,
    settings: {
        outputSelection: {
            '*': {
                '*': [ '*' ]
            }
        }
    }
}; 
const output = JSON.parse(solc.compile(JSON.stringify(input)));
const contractInfo = {
    abi: output.contracts[solName][solName].abi,
    bytecode: output.contracts[solName][solName].evm.bytecode.object
};
console.log(contractInfo);
// dump contract info as json file
fs.writeFileSync(solName + '.json', JSON.stringify(contractInfo, null, 2));