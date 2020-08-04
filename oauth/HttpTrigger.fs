namespace Company.Function

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging

// Using type notation for functions allows injections
type HttpTrigger(accessTokenProvider: AccessTokenProvider) =

    // For convenience, it's better to have a central place for the literal.
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

    [<FunctionName("HttpTrigger")>]
    member x.Run ([<HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)>]req: HttpRequest) (log: ILogger) =
        async {
            log.LogInformation("F# HTTP trigger function processed a request.")

            let x = getParamFromQueryString req XParam
            let y = getParamFromQueryString req YParam

            match x, y with
            | Some x1, Some y1 ->
                // Magic happens here
                let result = injectedMultiplier x1 y1
                return OkObjectResult(result) :> IActionResult
            | _, _ -> return BadRequestResult() :> IActionResult
            
        } |> Async.StartAsTask