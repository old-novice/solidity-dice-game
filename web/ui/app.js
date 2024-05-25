const ethEnabled = async () => {
    if (window.ethereum) {
        await window.ethereum.request({ method: 'eth_requestAccounts' });
        window.web3 = new Web3(window.ethereum);
        return true;
    }
    return false;
}

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
            depositAmount: 1,
            betAmount: 0.1,
            errorMessage: '',
            accounts: [],
            playerAddress: '',
            maxBlockNumber: '00000000'
        }
    },
    async created() {
        const self = this;
        fetch("DiceGame.json").then(response => response.json()).then(info => {
            self.abi = info.abi;
            self.bytecode = info.bytecode;
        });
        await this.init();
        web3 = new Web3(new Web3.providers.HttpProvider(this.providerUrl));
        const accounts = await web3.eth.getAccounts();
        this.userAddress = accounts.shift();
        // if userAccount is not the dealer, add it to the accounts back
        if (this.dealerAddress && this.dealerAddress === this.userAddress) {
            accounts.unshift(this.userAddress);
        }
        this.accounts = accounts;
        if (accounts.length) {
            this.playerAddress = accounts[0];
        }
        this.queryHistory();    
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
                this.dealerAddress = this.accountAddress;
                await this.fetch('/contract', {
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
            debugger;
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
        genEventLogHtml(log) {
            log = log.replace(/[@]0x[0-9a-fA-F]{40}/g, (address) => { return `<span class="address" title="${address}">${address.substr(0, 8).toLowerCase()}..</span>` });
            if (log.includes('骰')) {
                const points = log.match(/\[([0-9,]+)\]/)[1];
                const validMark = log.match(/點數：(\d+)/)[1] != '0' ? 'valid' : 'invalid';
                const pointHtmls = `<span class="dice ${validMark}">${points.split(',').map(p => `<span class="p${p}"></span>`).join('')}</span>`;
                log = log.replace(`[${points}]`, pointHtmls);
                log = log.replace(/點數：(\d+)/g, '<span class="score">$1 點</span>');
                log = log.replace(/(十八|一色)/g, '<span class="ko tag">$1</span>');
                log = log.replace(/(逼嘰)/g, '<span class="over tag">$1</span>');
                log = log.replace(/(贏)/g, '<span class="win result">$1</span>');
                log = log.replace(/(輸)/g, '<span class="lose result">$1</span>');
                log = log.replace(/(平手)/g, '<span class="tie result">$1</span>');
                log = log.replace(/(通賠)/g, '<span class="total lose">$1</span>');
                log = log.replace(/(通殺)/g, '<span class="total win">$1</span>');
            }
            return `<div>${log}</div`;
        },
        async queryHistory() {
            this.gameLogs = await fetch('/history').then(response => response.json());
            const gameGroups = {};
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
                    const label =  l.gameNumber == nowTag ? '進行中' : `第 ${l.gameNumber} 局`;
                    gameList.push({ number: l.gameNumber, label: `${l.timeStamp} ${label}` });
                }
                l.eventLogHtmls = l.eventLogs.map(this.genEventLogHtml);
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
            this.busy = false;
        }
    }
});
var vm = app.mount('#app');

var source = new EventSource('/sse');
source.onmessage = function (event) {
    var maxBlockNumber = event.data;
    if (maxBlockNumber > vm.maxBlockNumber) {
        vm.queryHistory();
    }   
};
source.onerror = function (event) {
    console.error(event);
};
