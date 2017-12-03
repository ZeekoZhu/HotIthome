// Learn more about F# at http://fsharp.org
// return an integer exit code
module Program

open FSharp.Control
open FSharp.Data
open IthomeSpider
open System
open System.Text.RegularExpressions

[<EntryPoint>]
let main _ = 
    printfn "Hello World from F#!"
    let articleId = 336995
    async {
        let! comments = GetCommentsOf articleId |> AsyncSeq.toListAsync
        for c in comments do printfn "%d %s" c.Id c.Content
        printfn "%d" comments.Length 
    } |> Async.Start
    Console.ReadKey() |> ignore
    0
