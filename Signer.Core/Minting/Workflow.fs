module Signer.Minting

open Nethereum.Contracts
open Nichelson
open Signer.Ethereum.Contract
open Signer.Tezos


let  toMintingParameters (e: EventLog<TransferEventDto>): MintingParameters =
    { Amount = e.Event.Value
      Owner = TezosAddress.FromString e.Event.TezosAddress
      TokenId = e.Log.Address
      TxId = e.Log.TransactionHash }

let  packAndSign (signer: Signer) (target: MintingTarget) (mint: MintingParameters) =
    asyncResult {
        let! payload = Multisig.pack target mint |> AsyncResult.ofResult
        let! signature = signer payload
        return (mint, signature)
    }

let  toEvent level (target: MintingTarget) (parameters: MintingParameters, signature: TezosSignature) =
    { Level = level
      Proof =
          { Signature = signature.ToBase58()
            Amount = parameters.Amount
            Owner = TezosAddress.Value(parameters.Owner)
            TokenId = parameters.TokenId
            TxId = parameters.TxId }
      Quorum =
          { QuorumContract = TezosAddress.Value(target.QuorumContract)
            MinterContract = TezosAddress.Value(target.MinterContract)
            ChainId = target.ChainId } }
    |> MintingSigned
    |> AsyncResult.ofSuccess

type MinterWorkflow = EventLog<TransferEventDto> -> DomainResult<(EventId * DomainEvent)>

let workflow (signer: Signer) (append: _ Append) (target: MintingTarget) :MinterWorkflow =
    let packAndSign = packAndSign signer target
    let append = append |> AsyncResult.bind
    
    fun logEvent -> 
        let toEvent =
            toEvent logEvent.Log.BlockNumber.Value target
            |> AsyncResult.bind

        logEvent
        |> toMintingParameters
        |> packAndSign
        |> toEvent
        |> append