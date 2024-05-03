// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract DiceGame {
    struct Player {
        uint256 joinTime;
        address playerAddress;
        uint256 stake;
        uint8[4] diceRolls;
        uint8 score;
    }

    address public dealer;
    mapping(address => Player) public players;
    address[] public playerAddresses;
    mapping(address => uint256) public stakes;
    address public winner;
    uint256 public totalStakes;
    uint256 public dealerFeePercent = 15; // Initial dealer fee set to 15%
    uint256 public startBlock;
    uint256 public endBlock;
    bool public gameStarted;
    Player[] public gamePlayers;

    event GameStarted(uint256 startBlock, uint256 endBlock);
    event PlayerJoined(address player);
    event GameEnded(address winner);
    event DiceRolled(address player, uint8[4] diceRolls, uint8 score);
    event DealerWithdraw(uint256 amount);
    event DepositReceived(address from, uint256 amount);

    constructor() {
        dealer = msg.sender;
    }

    modifier onlyDealer() {
        require(msg.sender == dealer, "Only the dealer can call this.");
        _;
    }

    modifier withinGamePeriod() {
        require(block.number <= endBlock, "The game period has ended.");
        _;
    }

    // Function to set the dealer fee percentage, only callable by the dealer
    function setDealerFeePercent(uint256 _feePercent) external onlyDealer {
        require(_feePercent < 100, "Fee percent must be less than 100");
        dealerFeePercent = _feePercent;
    }

    // Allows anyone to start the game
    function startGame() external {
        require(!gameStarted, "Game has already started.");
        startBlock = block.number;
        endBlock = startBlock + 20; // Set the end block to current block + 20
        gameStarted = true;
        emit GameStarted(startBlock, endBlock);
    }
    // Players join the game by depositing their stake
    function joinGame() external payable withinGamePeriod {
        require(gameStarted, "Game has not started.");
        require(msg.sender != dealer, "Dealer cannot join the game.");
        require(
            players[msg.sender].diceRolls.length != 0,
            "Player already joined."
        );
        require(msg.value > 0, "Stake must be greater than 0");

        players[msg.sender] = Player({joinTime: block.timestamp, playerAddress: msg.sender, stake: msg.value, diceRolls: [0,0,0,0], score: 0});
        gamePlayers.push(players[msg.sender]);
        playerAddresses.push(msg.sender);
        stakes[msg.sender] = msg.value;
        totalStakes += msg.value;
        emit PlayerJoined(msg.sender);
        rollDiceFor(msg.sender); // Generate dice roll results for the player
    }

    function getPlayers() public view returns (Player[] memory) {
        return gamePlayers;
    }

    // Generate dice roll results for a player
    function rollDiceFor(address playerAddress) internal {
        _rollDiceFor(playerAddress, 0); // Start recursion with counter at 0
    }
    function _rollDiceFor(address playerAddress, uint8 counter) internal {
        // Increment the recursion counter
        counter++;

        uint8[4] memory diceRolls;
        // randomly generate dice rolls for the player
        for (uint8 i = 0; i < diceRolls.length; i++) {
            diceRolls[i] = uint8(
                (uint256(
                    keccak256(
                        abi.encodePacked(block.timestamp, playerAddress, i, counter)
                    )
                ) % 6) + 1
            );
        }
        uint8 score = calculateScore(diceRolls);
        // if score is 0, re-roll the dices
        if (score == 0) {
            _rollDiceFor(playerAddress, counter); // Recursive call with incremented counter
        } else {
            players[playerAddress].score = score;
            players[playerAddress].diceRolls = diceRolls;
            emit DiceRolled(playerAddress, diceRolls, score);
        }
    }

    // End the game, determine the winner and finalize payout
    function endGame() public {
        require(gameStarted, "Game not in progress.");
        require(
            msg.sender == dealer || block.number > endBlock,
            "Only the dealer can end the game early or wait till the end block."
        );

        // If there is only one player, they gamble directly against the dealer
        if (playerAddresses.length == 1 && playerAddresses[0] != dealer) {
            // The dealer also rolls dice and calculates score
            rollDiceFor(dealer);
            uint8 dealerScore = players[dealer].score;
            uint8 playerScore = players[playerAddresses[0]].score;

            // Decide the winner
            winner = playerScore > dealerScore ? playerAddresses[0] : dealer;
        } else {
            // If there are multiple players, use the original logic to determine the winner
            uint8 highestScore = 0;
            for (uint i = 0; i < playerAddresses.length; i++) {
                if (players[playerAddresses[i]].score > highestScore) {
                    highestScore = players[playerAddresses[i]].score;
                    winner = playerAddresses[i];
                }
            }
        }

        // Calculate dealer fee and finalize payout
        finalizePayout();
        emit GameEnded(winner);

        // Reset game state
        resetGame();
    }

    function finalizePayout() private {
        uint256 dealerFee = (totalStakes * dealerFeePercent) / 100;
        uint256 payout = totalStakes - dealerFee;

        if (winner != address(0)) {
            payable(winner).transfer(payout);
        } else {
            // In case of a draw, refund all stakes
            for (uint i = 0; i < playerAddresses.length; i++) {
                payable(playerAddresses[i]).transfer(
                    stakes[playerAddresses[i]]
                );
            }
        }
    }

    function calculateScore(uint8[4] memory dice) public pure returns (uint8) {
        // 統計每個點數出現的次數
        uint8[6] memory counts;
        for (uint i = 0; i < dice.length; i++) {
            require(
                dice[i] >= 1 && dice[i] <= 6,
                "Dice value must be between 1 and 6"
            );
            counts[dice[i] - 1]++;
        }

        uint8 uniqueValues = 0; // 不重複的點數數量
        uint8 pairCounts = 0; // 對子出現次數
        for (uint i = 0; i < counts.length; i++) {
            if (counts[i] == 4) {
                return uint8((i + 1) * 4); // 一色
            } else if (counts[i] == 3) {
                return 0; // 三顆相同，需要重擲
            } else if (counts[i] == 2) {
                pairCounts++;
                uniqueValues++;
            } else if (counts[i] > 0) {
                uniqueValues++;
            }
        }

        // 檢查是否為無面
        if (uniqueValues == 4) {
            return 0; // 表示需要重擲
        }

        // 計算剩餘點數
        uint8 score = 0;
        for (uint i = 5; ; i--) {
            if (counts[i] == 2 && pairCounts == 2) {
                // 兩組對子取其大
                return uint8((i + 1) * 2);
            } else if (counts[i] == 1) {
                // 忽略對子分數
                score += uint8(i + 1);
            }
            if (i == 0) {
                // When i is equal to 0 and the loop tries to decrement i, it would cause an underflow
                // To prevent this, we break the loop when i is 0
                break;
            }
        }
        return score;
    }

    // Helper function: calculate the maximum of two numbers
    function max(uint8 a, uint8 b) private pure returns (uint8) {
        return a > b ? a : b;
    }

    // Dealer withdraws the fee
    function withdraw() external onlyDealer {
        payable(dealer).transfer(address(this).balance);
    }

    // Allows anyone to deposit to the contract
    function deposit() external payable {
        emit DepositReceived(msg.sender, msg.value);
    }

    // Resets the game state
    function resetGame() private {
        gameStarted = false;
        playerAddresses = new address[](0);
        winner = address(0);
        totalStakes = 0;
    }
}
