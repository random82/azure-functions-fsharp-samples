namespace Company.Function

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging

[<AutoOpen>]
module HttpTools = 
    [<Literal>]
    let XParam = "x"
    [<Literal>]
    let YParam = "y"

    let getParamFromQueryString (req:HttpRequest) name = 
        if req.Query.ContainsKey(name) then
            let param = req.Query.[name].[0]
            match Int32.TryParse param with
            | true, i -> Some(i)
            | _ -> None
        else
            None

// Using type notation for functions allows injections
type HttpMultiplier(injectedMultiplier: Multiplier) =
    // For convenience, it's better to have a central place for  literals.
    [<FunctionName("Multiply")>]
    member x.Multiply ([<HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)>]req: HttpRequest) 
                    (log: ILogger) =
        async {
            log.LogInformation("F# Mutliply function processed a request.")

            let x = getParamFromQueryString req XParam
            let y = getParamFromQueryString req YParam

            match x, y with
            | Some x1, Some y1 ->
                let result = match injectedMultiplier with MulDef m -> m x1 y1 
                return OkObjectResult(result) :> IActionResult
            | _, _ -> return BadRequestResult() :> IActionResult
            
        } |> Async.StartAsTask

type HttpAdder(injectedAdder: Adder) =
    [<FunctionName("Add")>]
    member x.Add ([<HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)>]req: HttpRequest) 
                    (log: ILogger) =
        async {
            log.LogInformation("F# Add function processed a request.")

            let x = getParamFromQueryString req XParam
            let y = getParamFromQueryString req YParam

            match x, y with
            | Some x1, Some y1 ->
                let result = match injectedAdder with AddDef m -> m x1 y1 
                return OkObjectResult(result) :> IActionResult
            | _, _ -> return BadRequestResult() :> IActionResult
            
        } |> Async.StartAsTask