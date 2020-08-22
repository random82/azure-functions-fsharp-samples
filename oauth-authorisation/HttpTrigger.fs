namespace Company.Function

open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Extensions.Http
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open AccessTokenResult
open System.Security.Claims

// Using type notation for functions allows injections
type HttpTrigger() =
    [<FunctionName("HttpTrigger")>]
    member x.Run ([<HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)>]req: HttpRequest) 
                (log: ILogger)
                ([<AccessToken>] accessToken) =
        async {

            log.LogInformation("F# HTTP trigger function processed a request.")

            let unpackUserId (token: SuccessResult) = 
                let userId  = token.Principal.Claims |> Seq.find (fun it -> it.Type = ClaimTypes.NameIdentifier) |> fun it -> it.Value
                userId

            match accessToken with
            | Success token -> 
                            let userId = unpackUserId token 
                            return OkObjectResult("Hello " + userId) :> IActionResult
            | NoToken _ -> return UnauthorizedObjectResult("whaat") :> IActionResult
            | Error ex -> return UnauthorizedObjectResult(ex.Exception) :> IActionResult
            | Expired _ -> return BadRequestObjectResult("Expired") :> IActionResult
        } |> Async.StartAsTask