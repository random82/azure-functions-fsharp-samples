namespace Company.Function

open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open AccessTokenResult

// Using type notation for functions allows injections
type HttpTrigger() =
    [<FunctionName("HttpTrigger")>]
    member x.Run ([<HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)>]req: HttpRequest) 
                (log: ILogger)
                ([<AccessToken>] accessToken) =
        async {
            log.LogInformation("F# HTTP trigger function processed a request.")

            match accessToken with
            | Success token -> return OkObjectResult(token) :> IActionResult
            | NoToken _ -> return UnauthorizedObjectResult("whaat") :> IActionResult
            | Error ex -> return UnauthorizedObjectResult(ex.Exception) :> IActionResult
            | _ -> return BadRequestObjectResult("ascii shrug") :> IActionResult
            
        } |> Async.StartAsTask