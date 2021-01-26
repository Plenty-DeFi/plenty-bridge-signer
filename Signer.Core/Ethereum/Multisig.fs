module Signer.Ethereum.Multisig

open System.Text
open Nethereum.Web3
open Org.BouncyCastle.Utilities.Encoders

type UnwrapParameters = {
    TokenContract: string
    Destination: string
    Amount: bigint
    OperationId: string
}

let transferCall (web3: Web3) erc20Address destination amount =
    let erc20 =
        web3.Eth.GetContract(Contract.erc20Abi, erc20Address)

    let transfer = erc20.GetFunction("transfer")

    let data =
        transfer.CreateCallInput(destination, amount).Data

    Hex.Decode(data.[2..])

let transactionHash (web3: Web3) lockingContractAddress (parameters:UnwrapParameters) =
    async {
        let locking =
            web3.Eth.GetContract(Contract.lockingContractAbi, lockingContractAddress)

        let data =
            transferCall web3 parameters.TokenContract parameters.Destination parameters.Amount

        let! hash =
            locking
                .GetFunction("getTransactionHash")
                .CallAsync(parameters.Destination, 0, data, Encoding.UTF8.GetBytes(parameters.OperationId))
            |> Async.AwaitTask

        return hash
    }
