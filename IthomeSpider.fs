module IthomeSpider

open FSharp.Control
open FSharp.Data
open System.Text.RegularExpressions

type Comment = 
    { Id : int
      Content : string
      VoteUp : int
      VoteDown : int
      ArticleId : int }

let commentUrl = "https://dyn.ithome.com/ithome/getajaxdata.aspx"
let commentFrameUrl = "https://dyn.ithome.com/comment/"

let getCommentHashOf articleId = 
    let url = commentFrameUrl + articleId
    async { let! iframe = HtmlDocument.AsyncLoad(url)
            return iframe.CssSelect("#hash").Head.AttributeValue("value") }

/// 从 html 片段中提取评论信息
let parseCommentEntry articleId (entryNode : HtmlNode) = 
    let voteUpA = entryNode.CssSelect("a.s").Head
    let commentId = voteUpA.TryGetAttribute("id").Value.Value().Substring(8) |> int
    let content = entryNode.CssSelect("p").Head.InnerText()
    let voteReg (voteType : string) input = Regex.Match(input, voteType + """\((\d+)\)""").Groups.[1].Value |> int
    let voteUp = voteReg "支持" (entryNode.CssSelect("a.s").Head.InnerText())
    let voteDown = voteReg "反对" (entryNode.CssSelect("a.a").Head.InnerText())
    { Id = commentId
      Content = content
      VoteUp = voteUp
      VoteDown = voteDown
      ArticleId = articleId }

/// 获取指定新闻下的评论的异步序列
let GetCommentsOf(articleId : int) = 
    let parseCommentEntry = parseCommentEntry articleId
    
    let rec loadCommentsAtPage (index : int) = 
        asyncSeq { 
            let! hash = getCommentHashOf (string articleId)
            let rec httpRetry() = 
                async { 
                    let! resp = Http.AsyncRequest(commentUrl, httpMethod = "POST", silentHttpErrors = true, 
                                                  body = FormValues [ "newsID", (string articleId)
                                                                      "order", "false"
                                                                      "page", (string index)
                                                                      "type", "commentpage"
                                                                      "hash", hash ])
                    match resp.StatusCode with
                    | 504 -> 
                        printfn "retry after 10s"
                        do! Async.Sleep(10 * 1000)
                        return! httpRetry()
                    | 200 -> 
                        return match resp.Body with
                               | Text s -> Ok s
                               | _ -> Error "response content error"
                    | _ -> return Error "response content error"
                }
            let! resp = httpRetry()
            match resp with
            | Ok response -> 
                if (response.Length) <> 0 then 
                    let commentsInCurrentPage = 
                        HtmlDocument.Parse("""<html><body>""" + response + """</body></html>""").CssSelect(".entry") 
                        |> Seq.map parseCommentEntry
                    for c in commentsInCurrentPage do
                        yield c
                    yield! loadCommentsAtPage (index + 1)
                else ()
            | Error _ -> ()
        }
    loadCommentsAtPage 1

/// 获取指定集合中新闻的评论
let GetCommentsOfRange(articles : int list) = 
    asyncSeq { 
        for id in articles do
            yield! GetCommentsOf id
    }
