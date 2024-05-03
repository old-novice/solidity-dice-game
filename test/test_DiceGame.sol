// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "truffle/Assert.sol";
import "../contracts/DiceGame.sol";

contract DiceGameTest {
    function testCalculateScore() public {
        DiceGame diceGame = new DiceGame();
        uint8[4] memory dice;
        uint8 expectedScore;

        // Test case 1: All dice have the same value (一色)
        dice = [6, 6, 6, 6];
        expectedScore = 255;
        Assert.equal(diceGame.calculateScore(dice), expectedScore, "Incorrect score for test case 1");

        // Test case 2: Three dice have the same value (需要重擲)
        dice = [2, 2, 2, 3];
        expectedScore = 0;
        Assert.equal(diceGame.calculateScore(dice), expectedScore, "Incorrect score for test case 2");

        // Test case 3: Three dice have the same value (需要重擲)
        dice = [5, 3, 5, 5];
        expectedScore = 0;
        Assert.equal(diceGame.calculateScore(dice), expectedScore, "Incorrect score for test case 3");

        // Test case 4: Two pairs of dice (兩組對子取其大)
        dice = [1, 1, 2, 2];
        expectedScore = 4;
        Assert.equal(diceGame.calculateScore(dice), expectedScore, "Incorrect score for test case 4");

        // Test case 5: Two pairs of dice (兩組對子取其大)
        dice = [4, 4, 2, 2];
        expectedScore = 8;
        Assert.equal(diceGame.calculateScore(dice), expectedScore, "Incorrect score for test case 5");

        // Test case 6: One pair of dice (取剩餘點數加總)
        dice = [2, 2, 3, 1];
        expectedScore = 4;
        Assert.equal(diceGame.calculateScore(dice), expectedScore, "Incorrect score for test case 6");

        // Test case 7: 無面 (需要重擲)
        dice = [6, 5, 4, 3];
        expectedScore = 0;
        Assert.equal(diceGame.calculateScore(dice), expectedScore, "Incorrect score for test case 7");

        // Test case 8: 無面 (需要重擲)
        dice = [1, 2, 3, 4];
        expectedScore = 0;
        Assert.equal(diceGame.calculateScore(dice), expectedScore, "Incorrect score for test case 8");

        // Test case 9: One pair of dice (取剩餘點數加總)
        dice = [6, 5, 6, 3];
        expectedScore = 8;
        Assert.equal(diceGame.calculateScore(dice), expectedScore, "Incorrect score for test case 9");

        // Test case 10: One pair of dice (取剩餘點數加總)
        dice = [3, 1, 4, 1];
        expectedScore = 7;
        Assert.equal(diceGame.calculateScore(dice), expectedScore, "Incorrect score for test case 10");
    }
}