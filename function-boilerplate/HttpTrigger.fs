namespace Company.Function

open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging

module MyFunction =

    let getParamFromQueryString (req:HttpRequest) name = 
        if req.Query.ContainsKey(name) then
            let param = req.Query.[name].[0]            
            Some(param)
        else
            None

    [<FunctionName("HttpTrigger")>]
    let Run ([<HttpTrigger(AuthorizationLevel.Function, "get", Route = null)>]req: HttpRequest) (log: ILogger) =
        async {
            log.LogInformation("F# HTTP trigger function processed a request.")

            let name = getParamFromQueryString req "name"

            match name with
            | Some name ->
                return OkObjectResult("Hello " + name) :> IActionResult
            | _ -> return BadRequestResult() :> IActionResult
            
        } |> Async.StartAsTask