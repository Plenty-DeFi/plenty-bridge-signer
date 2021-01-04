module Signer.Test

open Netezos.Forge.Utils
open Netezos.Keys
open Nichelson
open FsUnit.Xunit
open Xunit
open Signer.Tezos
open Signer

let multisig = "KT1MsooZb43dWi5GpHLeoYw5gyXj9viUuMcE"
let fa2Contract = "KT1LL3X5FcnUji8MVVWdi8bsjDATWqVvDgCB"

let benderContract =
    "KT1VUNmGa1JYJuNxNS4XDzwpsc9N1gpcCBN2%signer"

let target =
    { QuorumContract = TezosAddress.FromString multisig
      MinterContract = TezosAddress.FromString(benderContract)
      ChainId = "NetXm8tYqnMWky1" }

let mint =
    { Amount = 100I
      Owner = TezosAddress.FromString "tz1S792fHX5rvs6GYP49S1U58isZkp2bNmn6"
      TokenId = "contract_on_eth"
      TxId = "txId" }

let words = []

let key =
    Key.FromMnemonic(Mnemonic.Parse(words), "vespwozi.vztxobwc@tezos.example.org", "")

[<Fact>]
let ``should pack`` () =

    let v =
        Multisig.pack target mint
        |> Result.map (Encoder.byteToHex)

    match v with
    | Ok v ->
        v
        |> should
            equal
               "0x05070707070a00000004a83650210a000000160191d2931b012d854529bb38991cb439283b157c9400070700000508070705080707070700a4010a00000016000046f146853a32c121cfdcd4f446876ae36c4afc580707010000000f636f6e74726163745f6f6e5f6574680100000004747849640a0000001c01e5251ca070e1082433a3445733139b318fa80ca1007369676e6572"
    | Error err -> failwith err

[<Fact>]
let ``Should sign`` () =
    async {
        let sign = Signer.memorySigner key

        let! signature =
            Multisig.pack target mint
            |> AsyncResult.ofResult
            |> AsyncResult.bind sign

        let signature =
            match signature with
            | Ok v -> v
            | Error err -> failwith err

        signature
        |> (fun e -> e.ToBase58())
        |> should
            equal
               "edsigtvcit3FoxW4q7jatc84AJxs77kEnXRkpQwD8if31X6SGU8u4R6QXQBFVw3gUY7C99pW5QNXoCAe4dMHcrrerXjujG2fide"
    }

[<Fact>]
let ``Should talk to ledger`` () =
    async {
        let! key = Signer.ledgerKey

        key
        |> should equal "edpkuPLUBK5Vm2TcGedc8BZ3DiE9UigpuukwdCqfrdgiQPLhkeX2Ev"
    }

[<Fact>]
let ``Should sign with ledger`` () =
    async {
        let payload = Multisig.pack target mint
        let payload =
            match payload with
            | Ok v -> v
            | Error err -> failwith err

        let! ledger = Ledger.Client.get () |> Async.AwaitTask

        let buffer =
            Array.concat [| Hex.Parse("05")
                            payload |]


        let! signature = ledger.Sign(payload) |> Async.AwaitTask

        signature.ToBase58()
        |> should
            equal
               "edsigtmn1obykPqt74tbCzypKzuqvJ4ydRNjRRvh5i8VhyceaTLoLb9Bgq8NW4GEUv7LkHu8NHV4wm355VN1V1L3X9Tngnx3MxG"
    }