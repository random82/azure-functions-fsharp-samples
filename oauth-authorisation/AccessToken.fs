namespace Company.Function
open System
open System.Threading.Tasks
open Microsoft.Azure.WebJobs.Description
open Microsoft.Azure.WebJobs.Host.Bindings
open Microsoft.AspNetCore.Http
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims
open Microsoft.IdentityModel.Tokens
open Microsoft.IdentityModel.Protocols
open Microsoft.IdentityModel.Protocols.OpenIdConnect
open System.Threading
open Microsoft.Azure.WebJobs.Host.Protocols
open Microsoft.Azure.WebJobs.Host.Config
open Microsoft.Azure.WebJobs

module AccessTokenResult = 

    type SuccessResult = {   
        Principal: ClaimsPrincipal
    }

    type ErrorResult = {   
        Exception: Exception
    }

    type TokenResult = 
        | Success of SuccessResult 
        | Error of ErrorResult
        | Expired of unit
        | NoToken of unit

    let success principal = 
        Success {
            Principal = principal
        }

    let expired  =
        Expired 

    let error ex  =
        Error {
            Exception = ex
        }
        
    let noToken =
        NoToken 

[<AttributeUsage(AttributeTargets.Parameter ||| AttributeTargets.ReturnValue)>]
[<Binding>]
type AccessTokenAttribute() =
    inherit Attribute()

type AccessTokenValueProvider(request: HttpRequest, tokenValidationParameters) =
    let authHeaderName = "Authorization"
    let bearerPrefix = "Bearer "

    interface IValueProvider with

        member this.GetValueAsync() =
            try 
                if  request.Headers.ContainsKey(authHeaderName) &&
                    request.Headers.[authHeaderName].ToString().StartsWith(bearerPrefix) then
                
                    let token = request.Headers.[authHeaderName].ToString().Substring(bearerPrefix.Length)

                    // Validate the token
                    let validator = JwtSecurityTokenHandler()
                    let principal, securityToken = validator.ValidateToken(token, tokenValidationParameters);
                    Task.FromResult<obj>(AccessTokenResult.success(principal))
                else
                    Task.FromResult<obj>(AccessTokenResult.noToken())
            with
            | :? SecurityTokenExpiredException -> 
                Task.FromResult<obj>(AccessTokenResult.expired())
            | :? _ as ex ->
                Task.FromResult<obj>(AccessTokenResult.error(ex))

        member this.ToInvokeString(): string = 
            String.Empty

        member this.Type: Type = 
            typeof<ClaimsPrincipal>

type AccessTokenBinding(tokenValidationParameters) =
    interface IBinding with

        member this.BindAsync(context)  =
            // Get the HTTP request
            let request = context.BindingData.["req"] :?> HttpRequest
            Task.FromResult<IValueProvider>(AccessTokenValueProvider(request, tokenValidationParameters))

        member this.BindAsync(value: obj, context: ValueBindingContext): Task<IValueProvider> = 
            null

        member this.FromAttribute: bool = 
            true

        member this.ToParameterDescriptor() = 
            ParameterDescriptor()

type AccessTokenBindingProvider() = 
    let documentRetriever = HttpDocumentRetriever()

    let getKeys issuer = 
        async{
            let configurationManager = 
                ConfigurationManager<OpenIdConnectConfiguration>(
                    issuer + ".well-known/openid-configuration",
                    OpenIdConnectConfigurationRetriever(),
                    documentRetriever
                )

            let! openIdConfig = configurationManager.GetConfigurationAsync(CancellationToken.None) |> Async.AwaitTask
            let signingKeys = seq openIdConfig.SigningKeys
            return signingKeys
        }
    

    interface IBindingProvider with
        member this.TryCreateAsync(context: BindingProviderContext) =
            let issuer = Environment.GetEnvironmentVariable("JWT_AUTHORITY")
            let audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
            let keys =  getKeys issuer |> Async.RunSynchronously

            let tokenValidationParameters = 
                TokenValidationParameters(
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKeys = keys
                )  
            let binding = AccessTokenBinding(tokenValidationParameters)
            Task.FromResult<IBinding>(binding)


type AccessTokenExtensionProvider() =
    interface IExtensionConfigProvider with
        member this.Initialize(context: ExtensionConfigContext) =
            let provider = AccessTokenBindingProvider()
            context.AddBindingRule<AccessTokenAttribute>().Bind(provider) |> ignore

module IWebJobsBuilderExtensions =
    type IWebJobsBuilder with 
        member this.AddAccessTokenBinding() =
            this.AddExtension<AccessTokenExtensionProvider>()
