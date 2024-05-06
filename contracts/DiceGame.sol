// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

contract DiceGame {
    struct Player {
        uint256 joinTime;
        address playerAddress;
        uint256 stake;
        uint256 payout;
        uint8[4][] diceHistory;
        uint8[4] diceRolls;
        uint8 score;
    }
    enum State { Created, Started, Ended }
    State public state;
    address public dealer;
    mapping(address => Player) public players;
    address[] public playerAddresses;
    uint256 public dealerFeePercent = 15; // Initial dealer fee set to 15%
    uint256 public startBlock;
    uint256 public endBlock;
    Player[] public gamePlayers;

    event GameStarted(uint256 startBlock, uint256 endBlock);
    event PlayerJoined(address player, uint256 stake);
    event GameEnded(address player, uint256 payout);
    event DiceRolled(address player, uint8[4] diceRolls, uint8 score);
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

    modifier checkState(State _state, string memory message) {
        require(state == _state, message);
        _;
    }
    modifier notState(State _state, string memory message) {
        require(state != _state, message);
        _;
    }

    // Function to set the dealer fee percentage, only callable by the dealer
    function setDealerFeePercent(uint256 _feePercent) external onlyDealer {
        require(_feePercent < 100, "Fee percent must be less than 100");
        dealerFeePercent = _feePercent;
    }



    // Allows anyone to start the game
    function startGame() notState(State.Started, "Game has already started.") external {
        require(
            address(this).balance >= 1,
            "Contract balance must be at least 1 ether to start the game."
        );
        startBlock = block.number;
        endBlock = startBlock + 20; // Set the end block to current block + 20
        if (state == State.Ended) {
            resetGame();
        }
        state = State.Started;
        emit GameStarted(startBlock, endBlock);
        // Dealer automatically joins the game
        players[dealer] = Player({
            joinTime: block.timestamp,
            playerAddress: dealer,
            stake: 0,
            payout: 0,
            diceHistory: new uint8[4][](0),
            diceRolls: [0, 0, 0, 0],
            score: 0
        });
    }

    // Players join the game by depositing their stake
    function joinGame() checkState(State.Started, "Game has not started.") external payable withinGamePeriod {
        require(msg.sender != dealer, "Dealer cannot join the game.");
        require(
            players[msg.sender].stake == 0,
            "Player already joined."
        );
        require(
            msg.value > 0 ether && msg.value <= 0.1 ether,
            "Stake must be greater than 0 and less than or equal to 0.1 ether."
        );
        require(playerAddresses.length < 10, "Player limit reached.");
        
        players[msg.sender] = Player({
            joinTime: block.timestamp,
            playerAddress: msg.sender,
            stake: msg.value,
            payout: 0,
            diceHistory: new uint8[4][](0),
            diceRolls: [0, 0, 0, 0],
            score: 0
        });
        gamePlayers.push(players[msg.sender]);
        playerAddresses.push(msg.sender);
        emit PlayerJoined(msg.sender,  msg.value);
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
                        abi.encodePacked(
                            block.timestamp,
                            playerAddress,
                            i,
                            counter
                        )
                    )
                ) % 6) + 1
            );
        }
        uint8 score = calculateScore(diceRolls);
        // if score is 0, re-roll the dices
        if (score == 0) {
            players[playerAddress].diceHistory.push(diceRolls);
            emit DiceRolled(playerAddress, diceRolls, score);
            _rollDiceFor(playerAddress, counter); // Recursive call with incremented counter
        } else {
            players[playerAddress].score = score;
            players[playerAddress].diceRolls = diceRolls;
            emit DiceRolled(playerAddress, diceRolls, score);
        }
    }

    // End the game, determine the winner and finalize payout
    function endGame() checkState(State.Started, "Game not in progress.") public {
        require(
            msg.sender == dealer || block.number > endBlock,
            "Only the dealer can end the game early or wait till the end block."
        );
        
        // Roll dice for the dealer first
        rollDiceFor(dealer);

        // Decide payouts by comparing scores between the dealer and each player
        uint8 dealerScore = players[dealer].score;

        // if dice score is 3 (BG) or more than 12 (十八/一色)
        // no need to roll dice for players
        if (dealerScore <= 3 || dealerScore >= 12) {
            for (uint i = 0; i < playerAddresses.length; i++) {
                players[playerAddresses[i]].payout = 
                    dealerScore >= 12 ? 0 : players[playerAddresses[i]].stake * 2;
            }
        }
        else {
            // Roll dice for all players and the dealer
            for (uint i = 0; i < playerAddresses.length; i++) {
                rollDiceFor(playerAddresses[i]);
            }          
            for (uint i = 0; i < playerAddresses.length; i++) {
                uint8 playerScore = players[playerAddresses[i]].score;
                if (playerScore > dealerScore) {
                    // If score is same, dealer wins
                    // Player wins against the dealer, calculate payout
                    players[playerAddresses[i]].payout =
                        players[playerAddresses[i]].stake *
                        2;
                } else if (playerScore == dealerScore) {
                    // If score is same, player get back the stake
                    players[playerAddresses[i]].payout = players[playerAddresses[i]].stake; 
                } else {
                    players[playerAddresses[i]].payout = 0; // no payout
                }
            }
        }

        // update gamePlayers
        delete gamePlayers;
        gamePlayers.push(players[dealer]);
        for (uint i = 0; i < playerAddresses.length; i++) {
            gamePlayers.push(players[playerAddresses[i]]);
        }

        // Calculate and finalize payouts
        finalizePayout();

        state = State.Ended;
    }

    function finalizePayout() private {
        for (uint i = 0; i < playerAddresses.length; i++) {
            uint256 payout = players[playerAddresses[i]].payout;

            if (payout > 0) {
                uint256 dealerFee = (payout * dealerFeePercent) / 100;
                uint256 netPayout = payout - dealerFee;
                payable(playerAddresses[i]).transfer(netPayout);
            }
            emit GameEnded(playerAddresses[i], payout);
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
                return uint8(255); // 一色
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
        // Reset player data
        for (uint i = 0; i < playerAddresses.length; i++) {
            delete players[playerAddresses[i]];
        }
        // clear gamePlayers
        delete gamePlayers;
        playerAddresses = new address[](0);
    }
}
