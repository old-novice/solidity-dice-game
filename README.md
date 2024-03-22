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
2. `let instance = await DiceGame.deployed()` # Get the instance of the contract
3. `let accounts = await web3.eth.getAccounts()` # Get the accounts
4. `instance.placeBet(3, {from: accounts[0], value: web3.utils.toWei("1", "ether")})` # Place a bet with the number 3 of 1 ether, if the number is 3, you will win 2 ethers immediately.
5. `instance.getPastEvents('allEvents', {fromBlock: 0, toBlock: 'latest'})` # Get all the events, you will see the event `BetPlaced` and `BetSettled`.
6. `instance.withdrawBalance()` # Withdraw all the balance from the contract.