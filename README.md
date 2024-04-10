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
1. `truffle console`
1. `let instance = await DiceGame.deployed()`
1. `let accounts = await web3.eth.getAccounts()`
1. `instance.startGame({from: accounts[1]})`
1. `instance.joinGame({from: accounts[1], value: web3.utils.toWei("1", "ether")})`
1. `instance.joinGame({from: accounts[2], value: web3.utils.toWei("1", "ether")})`
1. `instance.joinGame({from: accounts[3], value: web3.utils.toWei("1", "ether")})`
1. `instance.endGame()`