# Dice Game

## Requisites
- Truffle
- Ganache

## Installation
1. Clone the repository
2. Start Ganache
3. Run `truffle compile`
4. Run `truffle migrate`

## Usage
```
truffle console
let instance = await DiceGame.deployed()
let accounts = await web3.eth.getAccounts()
instance.deposit({from: accounts[0], value: web3.utils.toWei("1", "ether")})
instance.startGame({from: accounts[0]})
instance.joinGame({from: accounts[1], value: web3.utils.toWei("0.1", "ether")})
instance.joinGame({from: accounts[2], value: web3.utils.toWei("0.1", "ether")})
instance.joinGame({from: accounts[3], value: web3.utils.toWei("0.1", "ether")})
await web3.eth.getBalance(instance.address)
instance.endGame()
instance.getPlayers()
await web3.eth.getBalance(instance.address)
```

## Test in Browser

1. `cd web`
2. `npm install solc fs`
3. `node .\compile.js` to create DiceGame.json
4. run test.html
