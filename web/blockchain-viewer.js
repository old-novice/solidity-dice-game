class eventDecoder {
    constructor(abiItem) {
        this.abiItem = abiItem;
        this.eventName = abiItem.name;
    }
    decode(data) {
        const decodedArray = web3.eth.abi.decodeLog(
            this.abiItem.inputs.map(i => i.type), 
            data);
        const decoded = {};
        this.abiItem.inputs.forEach((input, index) => {
            let value = decodedArray[index];
            // Convert bigints to integers
            if (Array.isArray(value) && value.length) {
                value = value.map(v => typeof v === 'bigint' ? parseInt(v.toString()) : v);
            }
            decoded[input.name] = value;
        });
        return decoded;
    }
}

class blockChainViewer {
    // decodeEventABI not available in web3.js
    // self-implmentation 
    initEventDecoders(abi) {
        const eventDecoders = {};
        abi.forEach((item) => {
            if (item.type === 'event') {
                const signature = web3.eth.abi.encodeEventSignature(item);
                eventDecoders[signature] = new eventDecoder(item);
            }
        });
        return eventDecoders;
    }

    constructor(web3, contractAddress, contractAbi) {
        this.web3 = web3;
        this.contractAddress = contractAddress;
        this.contractAbi = contractAbi;
        this.contract = new this.web3.eth.Contract(this.contractAbi, this.contractAddress);
        this.eventDecoders = this.initEventDecoders(this.contractAbi);
    }

    async getRelatedLogs() {
        const list = [];
        const logs = await web3.eth.getPastLogs({
            address: this.contractAddress,
            fromBlock: 0,
            toBlock: 'latest'
        });
        const dupCheck = {};
        for (let log of logs) {
            if (dupCheck[log.blockNumber]) continue;
            dupCheck[log.blockNumber] = true;
            const record = {
                blockNo: log.blockNumber,
                transactionHash: log.transactionHash,
                time: (await web3.eth.getBlock(log.blockNumber)).timestamp,
            }
            const tranReceipt = await web3.eth.getTransactionReceipt(log.transactionHash);
            record.events = tranReceipt.logs.map((log) => {
                const signature = log.topics[0];
                const decoder = this.eventDecoders[signature];
                if (decoder) {
                    return {
                        name: decoder.eventName,
                        data: decoder.decode(log.data),
                    }
                }
            });
            list.push(record);
        }
        return list;

    }
}