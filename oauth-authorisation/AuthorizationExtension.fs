namespace Company.Function
open System
open System.Net.Http
open System.Threading.Tasks
open Microsoft.Azure.WebJobs.Host
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection



[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Method, AllowMultiple = true, Inherited = true)>]
type FunctionAuthorizeAttribute(policy: string)  =
    inherit FunctionInvocationFilterAttribute()

    let valueKey = "__AuthZProcessed"

    let isProcessed (context: FunctionExecutingContext) = 
        let found, _ =  context.Properties.TryGetValue valueKey
        match found with
        | false -> 
            context.Properties.[valueKey] <- true;
            false
        | true -> true

    let getHttpContext(context: FunctionExecutingContext) = 
        let requestOrMessage = 
            context.Arguments.Values 
            |> Seq.find (fun x -> 
                            match x with 
                            | :? HttpRequest -> true
                            | :? HttpRequestMessage -> true
                            | _ -> false)

        match requestOrMessage with
        | :? HttpRequest as request -> Some(request.HttpContext)
        | :? HttpRequestMessage as message -> Some(message.Properties.["HttpContext"] :?> HttpContext)
        | _ -> None

    let authorizeRequest(executingContext, httpContext: HttpContext) =
        let handler = httpContext.RequestServices.GetRequiredService<IFunctionHttpAuthorizationHandler>()
        handler.OnAuthorizingFunctionInstance executingContext httpContext
    
    new () = FunctionAuthorizeAttribute String.Empty

    interface IFunctionInvocationFilter with 
        member this.OnExecutingAsync(executingContext, cancellationToken) =
            match isProcessed executingContext with
            | false -> 
                let httpContextResult = getHttpContext executingContext
                match httpContextResult with
                | Some httpContext -> authorizeRequest(executingContext, httpContext) 
                                        |> Async.StartAsTask :> Task
                | _ -> Task.CompletedTask
            | true -> Task.CompletedTask

open Microsoft.Azure.WebJobs.Description
open Microsoft.Azure.WebJobs.Host.Config
[<Extension("FunctionAuthorize")>]
type FunctionsAuthorizeExtension() = 
    interface IExtensionConfigProvider with
        member this.Initialize(config) = 
            do()

open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Hosting
open Microsoft.Azure.WebJobs.Host.Bindings
type FunctionsAuthorizeWebJobsStartup =
    interface IWebJobsStartup with
        member this.Configure(builder) =
            builder.Services.AddSingleton<IBindingProvider, FunctionAuthorizeBindingProvider>();
            builder.Services.AddSingleton<IFunctionAuthorizationFilterIndex, FunctionAuthorizationFilterIndex>();
            builder.Services.AddSingleton<IFunctionHttpAuthorizationHandler, FunctionHttpAuthorizationHandler>();
            builder.AddExtension<FunctionAuthorizeExtension>();


[<assembly: WebJobsStartup(typeof<FunctionsAuthorizeWebJobsStartup>)>]
do()
