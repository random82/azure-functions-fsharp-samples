namespace Company.Function
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open System.Reflection
open Microsoft.Azure.WebJobs
open Microsoft.AspNetCore.Authorization
open Microsoft.Azure.WebJobs.Host
open System.Threading.Tasks

type FunctionAuthorizationContext(result: IActionResult, context: HttpContext) = 
    member val HttpContext = context with get

    member val Result = result with get, set

type IFunctionAuthorizeFilter = 
    abstract member AuthorizeAsync: context: FunctionAuthorizationContext 
                                    -> Async

type IFunctionAuthorizationFilterIndex =
    abstract member GetAuthorizationFilter: functionName: string 
                                            -> IFunctionAuthorizeFilter
    abstract member AddAuthorizationFilter: functionMethod: MethodInfo 
                                            -> nameAttribute: FunctionNameAttribute 
                                            -> authorizeData: seq<IAuthorizeData>
                                            -> unit

type IFunctionHttpAuthorizationHandler =
    abstract member OnAuthorizingFunctionInstance: functionContext: FunctionExecutingContext 
                                                    ->  httpContext: HttpContext 
                                                    -> Async<unit>