const gasLimit = 3000000;
const app = Vue.createApp({
    data() {
        return {
            contractAddress: '',
            dealerAddress: '',
            providerUrl: '',
            gameStatus: '',
            gameList: [],
            currGameNumber: '',
            gameLogs: [],
            gameGroups: {},
            busy: false,
            errMessage: '',
            abi: null,
            bytecode: null,
            gameStarted: false,
            userAddress: '',
            depositAmount: 0.1,
            betAmount: 0.005,
            errorMessage: '',
            accounts: [],
            playerAddress: '',
            maxBlockNumber: '00000000',
            contractBalance: '',
            ganacheMode: false
        }
    },
    async created() {
        const self = this;
        fetch("DiceGame.json").then(response => response.json()).then(info => {
            self.abi = info.abi;
            self.bytecode = info.bytecode;
        });
        await this.init();
        if (ganacheMode) {
            web3 = new Web3(new Web3.providers.HttpProvider(this.providerUrl));
        }
        const accounts = await web3.eth.getAccounts();
        this.userAddress = accounts.shift();
        // if userAccount is not the dealer, add it to the accounts back
        if (this.dealerAddress && this.dealerAddress != this.userAddress) {
            accounts.unshift(this.userAddress);
        }
        this.accounts = accounts;
        if (accounts.length) {
            this.playerAddress = accounts[0];
        }
        this.queryHistory();
        document.querySelector('[v-cloak]').removeAttribute('v-cloak');
    },
    computed: {
        filteredGameLogs() {
            if (this.currGameNumber === '*') {
                return this.gameLogs;
            }
            return this.gameLogs.filter(l => l.gameNumber === this.currGameNumber);
        }
    },
    methods: {
        setBusy() {
            this.busy = true;
            this.errorMessage = '';
        },
        showError(message) {
            this.errorMessage = message;
            this.busy = false;
        },
        async init() {
            var data = await fetch('/contract').then(response => response.json());
            this.contractAddress = data.contractAddress;
            this.dealerAddress = data.dealerAddress;
            this.providerUrl = data.providerUrl;
        },
        async deployContract() {
            this.setBusy();
            const contract = new web3.eth.Contract(this.abi);
            contract.options.data = '0x' + this.bytecode;
            const deployTx = contract.deploy();
            try {
                const deployedContract = await deployTx.send({
                    from: this.userAddress,
                    gas: gasLimit
                });
                this.contractAddress = deployedContract.options.address;
                this.dealerAddress = this.userAddress;
                await fetch('/contract', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ contractAddress: this.contractAddress, dealerAddress: this.dealerAddress })
                });
                this.busy = false;
            } catch (error) {
                this.showError(error);
            }
        },
        async deposit() {
            this.setBusy();
            const contract = new web3.eth.Contract(this.abi, this.contractAddress);
            const tx = contract.methods.deposit();
            try {
                const res = await tx.send({
                    from: this.userAddress,
                    value: web3.utils.toWei(parseFloat(this.depositAmount), 'ether'),
                    gas: gasLimit
                });
                this.queryHistory();
            }
            catch (error) {
                this.showError(error);
            }
        },
        async sync() {
            await fetch('/sync').then(() => this.queryHistory());
        },
        async startGame() {
            this.setBusy();
            const contract = new web3.eth.Contract(this.abi, this.contractAddress);
            tx = contract.methods.startGame();
            try {
                await tx.send({
                    from: this.userAddress,
                    gas: gasLimit
                });
                this.sync();
            }
            catch (error) {
                this.showError(error);
            }
        },
        async joinGame() {
            this.setBusy();
            const contract = new web3.eth.Contract(this.abi, this.contractAddress);
            const tx = contract.methods.joinGame();
            try {
                await tx.send({
                    from: this.playerAddress,
                    value: web3.utils.toWei(parseFloat(this.betAmount), 'ether'),
                    gas: gasLimit
                });
                this.sync();
            }
            catch (error) {
                this.showError(error);
            }
        },
        async endGame() {
            this.setBusy();
            const contract = new web3.eth.Contract(this.abi, this.contractAddress);
            const tx = contract.methods.endGame();
            try {
                const receipt = await tx.send({
                    from: this.userAddress,
                    gas: gasLimit
                });
                this.sync();
            }
            catch (error) {
                this.showError(error);
            }
        },
        async getContractBalance() {
            const balance = await web3.eth.getBalance(this.contractAddress);
            this.contractBalance = web3.utils.fromWei(balance, 'ether');
        },
        genEventLogHtml(log, bets) 
        {
            const address = log.match(/[@]0x[0-9a-fA-F]{40}/)?.[0];
            if (address) {
                log = log.replace(address, `<span class="address" title="${address}">${address.substr(0, 8).toLowerCase()}..</span>`);
            }
            let dealerMark = '';
            if (log.includes('骰')) {
                const points = log.match(/\[([0-9,]+)\]/)[1];
                const score = parseInt(log.match(/點數：(\d+)/)[1] ?? "0");
                const validMark = score > 0 ? 'valid' : 'invalid';
                dealerMark = log.includes('莊家') && score > 0 ? 'dealer' : 'player';
                const pointHtmls = `<span class="dice ${validMark}">${points.split(',').map(p => `<span class="p${p}"></span>`).join('')}</span>`;
                log = log.replace(`[${points}]`, pointHtmls);
                log = log.replace(/點數：(\d+)/g, `<span class="score ${dealerMark} ${validMark}">$1 點</span>`);
                log = log.replace(/(十八|一色)/g, '<span class="ko tag">$1</span>');
                log = log.replace(/(逼嘰)/g, '<span class="over tag">$1</span>');
                log = log.replace(/(贏)/g, '<span class="win result">$1</span>');
                log = log.replace(/(輸)/g, '<span class="lose result">$1</span>');
                log = log.replace(/(平手)/g, '<span class="tie result">$1</span>');
                log = log.replace(/(通賠)/g, '<span class="total lose">$1</span>');
                log = log.replace(/(通殺)/g, '<span class="total win">$1</span>');
            }
            else if (log.includes('結算')) {
                const trasnAmt = parseFloat(log.match(/(\d+\.\d+) ETH/)[1]);
                const betAmt = bets[address] ?? 0;
                let mark = '無輸贏';
                let css = 'tie';
                if (trasnAmt > betAmt) {
                    mark = '贏'  + betAmt.toFixed(3);
                    css = 'win';
                }
                else if (trasnAmt < betAmt) {
                    mark = '輸'  + betAmt.toFixed(3);
                    css = 'lose';
                }
                log += ` <span class="win-lose-amt ${css}">${mark}<span>`;
            }
            return `<div>${log}</div`;
        },
        async queryHistory() {
            this.gameLogs = await fetch('/history').then(response => response.json());
            const gameGroups = {};
            const betData = {};
            const nowTag = 'Now';
            let maxBlockNumber;
            this.gameLogs.forEach(l => {
                // format for display
                l.timeStamp = l.timeStamp.replace('T', ' ');
                l.blockNumber = l.blockNumber.replace('0x', '');
                maxBlockNumber = l.blockNumber;
                if (l.gameNumber) l.gameNumber = l.gameNumber.replace('0x', '');

                if (l.eventLogs?.join('').includes('儲值')) {
                    l.gameNumber = '*';
                }
                if (!l.gameNumber) l.gameNumber = nowTag;
                if (!gameGroups[l.gameNumber]) {
                    gameGroups[l.gameNumber] = [];
                    betData[l.gameNumber] = {};
                }
                gameGroups[l.gameNumber].push(l);
            });
            if (maxBlockNumber > this.maxBlockNumber) {
                this.maxBlockNumber = maxBlockNumber;
            }
            this.gameGroups = gameGroups;
            const gameList = [];
            let gameCount = 0;
            this.gameLogs.forEach(l => {
                if (l.eventLogs?.join('').includes('開局')) {
                    l.rowSpan = gameGroups[l.gameNumber].length;
                    l.gameCount = ++gameCount;
                    const label = l.gameNumber == nowTag ? '進行中' : `第 ${l.gameNumber} 局`;
                    gameList.push({ number: l.gameNumber, label: `${l.timeStamp} ${label}` });
                }
                // collect bet data
                l.eventLogs.forEach(log => {
                    if (log.includes('下注')) {
                        const address = log.match(/[@]0x[0-9a-fA-F]{40}/)[0];
                        const amount = parseFloat(log.match(/(\d+\.\d+) ETH/)[1]);
                        betData[l.gameNumber][address] = amount;
                    }
                });
                l.eventLogHtmls = l.eventLogs.map(log => {
                    return this.genEventLogHtml(log, betData[l.gameNumber]);
                });
            });
            gameList.reverse();
            gameList.push({ number: '*', label: '所有記錄' })
            this.gameList = gameList;
            this.currGameNumber = gameList[0]?.number ?? "";
            this.gameStarted = false;
            if (gameGroups[nowTag]?.length) {
                this.currGameNumber = nowTag;
                this.gameStarted = true;
            }
            this.getContractBalance();
            this.busy = false;
        }
    }
});
var vm;

// detect mode 
const ganacheMode = location.search.includes('ganache');
setTimeout(async () => {
    if (!ganacheMode) {
        if (!window.ethereum) {
            alert('Please install MetaMask!');
        } else {
            await window.ethereum.request({ method: 'eth_requestAccounts' });
            web3 = new Web3(window.ethereum);
            vm = app.mount('#app');
        }
    }
    else vm = app.mount('#app');
    vm.ganacheMode = ganacheMode;
}, 0);

var source = new EventSource('/sse');
source.onmessage = function (event) {
    if (!vm) return;
    var maxBlockNumber = event.data;
    if (maxBlockNumber > vm.maxBlockNumber) {
        vm.queryHistory();
    }
};
source.onerror = function (event) {
    console.error(event);
};


const faces = ['front', 'left', 'bottom', 'top', 'right', 'back'];
const oppsiteFaces = {
    front: 'back',
    right: 'left',
    back: 'front',
    left: 'right',
    top: 'bottom',
    bottom: 'top'
};

class DiceCube extends HTMLElement {
    constructor() {
        super();
        this.innerHTML = `
      <div class="dice-cube">
        <div class="cube">
          <div class="cube__face cube__face--front"></div>
          <div class="cube__face cube__face--back"></div>
          <div class="cube__face cube__face--right"></div>
          <div class="cube__face cube__face--left"></div>
          <div class="cube__face cube__face--top"></div>
          <div class="cube__face cube__face--bottom"></div>
        </div>  
      </div>
    `;
        this.face = 'front';
        this.prevFace = '';
    }
    get points() {
        return faces.indexOf(this.face) + 1;
    }
    roll(face) {
        let cube = this.querySelector('.cube');
        cube.className = 'cube';
        cube.classList.add(`show-${face}`);
        this.prevFace = this.side;
        this.face = face;
        return new Promise(resolve => {
            setTimeout(() => {
                resolve();
            }, 2500);
        });
    }
    randomFace() {
        return faces[Math.floor(Math.random() * faces.length)];
    }
    async throw() {
        let nextFace;
        do {
            nextFace = this.randomFace();
        } while (nextFace === this.face ||
        nextFace === this.prevFace ||
            nextFace === oppsiteFaces[this.face]
        );
        await this.roll(nextFace);
    }
}
customElements.define('dice-cube', DiceCube);
function rollDice(force) {
    if (force || (vm && vm.gameStarted))
        document.querySelectorAll('dice-cube').forEach(cube => cube.throw());
}
rollDice(true);
setInterval(rollDice, 2500);