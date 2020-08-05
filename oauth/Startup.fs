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


type AccessTokenProvider(issuer: string, audience: string) = 
    let authHeaderName = "Authorization"
    let bearerPrefix = "Bearer "

    member x.ValidateToken(request: HttpRequest) =
        try 
            if  request.Headers.ContainsKey(authHeaderName) &&
                request.Headers.[authHeaderName].ToString().StartsWith(bearerPrefix) then
            
                let token = request.Headers.[authHeaderName].ToString().Substring(bearerPrefix.Length)
                // Create the parameters
                let tokenParams = TokenValidationParameters(
                    ValidAudience = audience,
                    ValidateAudience = true,
                    ValidIssuer = issuer,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = false,
                    ValidateLifetime = true
                )

                // Validate the token
                let handler = JwtSecurityTokenHandler()
                let result, securityToken = handler.ValidateToken(token, tokenParams);
                AccessTokenResult.success(result)
            else
                AccessTokenResult.noToken()
        with
        | :? SecurityTokenExpiredException -> 
            AccessTokenResult.expired()
        | :? _ as ex ->
            AccessTokenResult.error(ex)

type MyStartup() = 
    inherit FunctionsStartup()


    override u.Configure(builder: IFunctionsHostBuilder) =
        // Our dependency
        builder.Services.AddHttpClient() |> ignore
        // We can use plain functions as injected dependencies
        let issuer = "***REMOVED***"
        let audience = "***REMOVED***"
        builder.Services.AddSingleton<AccessTokenValidator>(AccessTokenProvider(issuer, audience).ValidateToken) |> ignore


// FSharp way to create assembly targeted attributes
[<assembly: FunctionsStartup(typeof<MyStartup>)>]
do()

