// Learn more about F# at http://fsharp.org
// return an integer exit code
module Program

open FSharp.Control
open FSharp.Data
open IthomeSpider
open Newtonsoft.Json
open System
open System.IO
open System.Text.RegularExpressions

[<EntryPoint>]
let main _ = 
    printfn "Hello World from F#!"
    let articleId = 326995
    async { 
        for id in [ articleId..(articleId + 10000) ] do
            try 
                let! comments = GetCommentsOf id |> AsyncSeq.toListAsync
                for c in comments do
                    File.AppendAllText("./data.json", JsonConvert.SerializeObject(c, Formatting.None) + "\n")
            with _ -> ()
    }
    |> Async.RunSynchronously
    printfn "%s" "complete!"
    0
