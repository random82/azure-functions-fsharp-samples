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
    member x.Run ([<HttpTrigger(AuthorizationLevel.Function, "post", Route = null)>]req: HttpRequest) 
                (log: ILogger) =
        async {

            log.LogInformation("F# HTTP trigger function processed a request.")


        } |> Async.StartAsTask