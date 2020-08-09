namespace Company.Function

open System
open System.Security.Claims
open Microsoft.Azure.Functions.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection;
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open Microsoft.AspNetCore.Http
open System.IdentityModel.Tokens.Jwt
open System.Text
open Microsoft.IdentityModel.Protocols
open Microsoft.IdentityModel.Protocols.OpenIdConnect
open System.Threading


//https://www.ben-morris.com/custom-token-authentication-in-azure-functions-using-bindings/

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

type AccessTokenValidator = HttpRequest -> AccessTokenResult.TokenResult


type AccessTokenProvider(tokenValidationParameters: TokenValidationParameters) = 
    let authHeaderName = "Authorization"
    let bearerPrefix = "Bearer "

    member x.ValidateToken(request: HttpRequest) =
        try 
            if  request.Headers.ContainsKey(authHeaderName) &&
                request.Headers.[authHeaderName].ToString().StartsWith(bearerPrefix) then
            
                let token = request.Headers.[authHeaderName].ToString().Substring(bearerPrefix.Length)

                // Validate the token
                let validator = JwtSecurityTokenHandler()
                let principal, securityToken = validator.ValidateToken(token, tokenValidationParameters);
                AccessTokenResult.success(principal)
            else
                AccessTokenResult.noToken()
        with
        | :? SecurityTokenExpiredException -> 
            AccessTokenResult.expired()
        | :? _ as ex ->
            AccessTokenResult.error(ex)

type MyStartup() = 
    inherit FunctionsStartup()

    let jwtBearerOptions () =
        JwtBearerOptions(
            SaveToken = true,
            IncludeErrorDetails = true,
            Authority = "***REMOVED***",
            Audience = "***REMOVED***",
            TokenValidationParameters = TokenValidationParameters(
                NameClaimType = ClaimTypes.NameIdentifier,
                ValidateAudience = true
            )
        )

    let checkConfiguredSigningKey keys = 
        do()

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

            checkConfiguredSigningKey signingKeys
            return signingKeys
        }
        

    override u.Configure(builder: IFunctionsHostBuilder) =
        // Our dependency
        builder.Services.AddHttpClient() |> ignore
        // We can use plain functions as injected dependencies
        let issuer = "***REMOVED***"
        let audience = "***REMOVED***"

        let keys =  getKeys issuer |> Async.RunSynchronously

        let tokenValidationParameters = 
            TokenValidationParameters(
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKeys = keys
            )   

        builder.Services.AddSingleton<AccessTokenValidator>(AccessTokenProvider(tokenValidationParameters).ValidateToken) |> ignore


// FSharp way to create assembly targeted attributes
[<assembly: FunctionsStartup(typeof<MyStartup>)>]
do()

