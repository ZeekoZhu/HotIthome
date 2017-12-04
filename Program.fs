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
        MailboxProcessor<Comment list>.Start(fun inbox -> 
            let rec messageLoop() = 
                async { 
                    let! comments = inbox.Receive()
                    if comments.Length > 1000 then 
                        do printfn "%d has %d comments" comments.Head.ArticleId comments.Length
                    let sb = StringBuilder()
                    let aggregate comment = 
                        sb.AppendLine(JsonConvert.SerializeObject(comment, Formatting.None) + ",") |> ignore
                    comments |> Seq.iter aggregate
                    do! File.AppendAllTextAsync("./data.json", sb.ToString()) |> Async.AwaitTask
                    return! messageLoop()
                }
            messageLoop())
    
    member this.Save comments = agent.Post comments

type ProcessWriter(target : int) = 
    let mutable count = 0
    
    member this.Agent = 
        MailboxProcessor<int>.Start(fun inbox -> 
            let rec messageLoop() = 
                async { 
                    let! size = inbox.Receive()
                    count <- count + size
                    printfn "download %d %%" (100 * count / this.Target)
                    return! messageLoop()
                }
            messageLoop())
    
    member this.Target = target
    member this.Update size = this.Agent.Post size

let batch size list = 
    list
    |> List.mapi (fun i x -> i, x)
    |> List.groupBy (fun (i, x) -> i / size)
    |> List.map (fun (_, x) -> x |> List.map (fun (_, y) -> y))

[<EntryPoint>]
let main _ = 
    printfn "Hello World from F#!"
    let articleId = 326995
    let batchSize = 10
    let count = 100
    let writer = CommentWriter()
    let processWriter = ProcessWriter(count)
    
    let makeTask id = 
        async { 
            try 
                let! comments = GetCommentsOf id |> AsyncSeq.toListAsync
                writer.Save comments |> ignore
                processWriter.Update 1
            with Failure e -> failwith e
        }
    File.WriteAllText("./data.json", "[")
    let batchDownload (range : int list) = 
        range
        |> List.map makeTask
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore
        
    [ articleId..(articleId + count - 1) ]
    |> batch batchSize
    |> List.iter batchDownload
    File.AppendAllText("./data.json", "]")
    printfn "%s" "complete!"
    0
