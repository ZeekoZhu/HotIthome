// Learn more about F# at http://fsharp.org
// return an integer exit code
module Program

open FSharp.Control
open FSharp.Data
open IthomeSpider
open Newtonsoft.Json
open System
open System.IO
open System.Text

type CommentWriter() = 
    
    let agent = 
        MailboxProcessor<Comment seq>.Start(fun inbox -> 
            let rec messageLoop() = 
                async { 
                    let! comments = inbox.Receive()
                    let sb = StringBuilder()
                    let aggregate comment = 
                        sb.AppendLine(JsonConvert.SerializeObject(comment, Formatting.None) + ",") |> ignore
                    comments |> Seq.iter aggregate
                    do! File.AppendAllTextAsync("./data.json", sb.ToString()) |> Async.AwaitTask
                    return! messageLoop()
                }
            messageLoop())
    
    member this.Save comments = agent.Post comments

[<EntryPoint>]
let main _ = 
    printfn "Hello World from F#!"
    let articleId = 326995
    let writer = CommentWriter()
    
    let makeTask id = 
        async { 
            try 
                let! comments = GetCommentsOf id |> AsyncSeq.toListAsync
                printfn "%d" comments.Length
                writer.Save comments
            with Failure e -> failwith e
        }
    File.WriteAllText("./data.json", "[")
    [ articleId..(articleId + 1) ]
    |> List.map makeTask
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
    File.AppendAllText("./data.json", "]")
    printfn "%s" "complete!"
    0
