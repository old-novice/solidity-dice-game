// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract DiceGame {
    address public owner;
    uint256 public minimumBet;

    // Event that is emitted when a bet is placed
    event BetPlaced(address indexed player, uint256 amount, uint256 prediction);
    // Event that is emitted when a bet is settled
    event BetSettled(address indexed player, uint256 amount, uint256 prediction, uint256 diceRoll, bool won);

    constructor(uint256 _minimumBet) {
        owner = msg.sender;
        minimumBet = _minimumBet;
    }

    // Modifier to restrict function calls to only the contract owner
    modifier onlyOwner() {
        require(msg.sender == owner, "Only the contract owner can call this function.");
        _;
    }

    // Function to place a bet
    function placeBet(uint256 _prediction) external payable {
        require(msg.value >= minimumBet, "Bet does not meet the minimum bet requirement.");
        require(_prediction >= 1 && _prediction <= 6, "Prediction must be between 1 and 6.");

        // Emit event for the bet
        emit BetPlaced(msg.sender, msg.value, _prediction);

        // This is a simplified and insecure way of generating a "random" number
        uint256 diceRoll = (uint(keccak256(abi.encodePacked(block.timestamp, block.difficulty, msg.sender))) % 6) + 1;

        bool won = diceRoll == _prediction;

        if (won) {
            // If the player wins, send them twice the amount they bet
            (bool sent,) = msg.sender.call{value: msg.value * 2}("");
            require(sent, "Failed to send Ether");
        }

        // Emit event for bet settlement
        emit BetSettled(msg.sender, msg.value, _prediction, diceRoll, won);
    }

    // Function to withdraw contract balance (for owner)
    function withdrawBalance() external onlyOwner {
        (bool sent,) = owner.call{value: address(this).balance}("");
        require(sent, "Failed to send Ether");
    }
}
