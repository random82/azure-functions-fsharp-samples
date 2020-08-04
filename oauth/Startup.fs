namespace FSharpFunction

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
    type AccessTokenStatus = 
        Valid = 0
        | Expired = 1 
        | Error = 2
        | NoToken = 3


    type TokenResult = {   
        Principal: ClaimsPrincipal
        Status: AccessTokenStatus
        Exception: Exception
    }

    // let Success(principal: ClaimsPrincipal) = 
    //     TokenResult
    //     {
    //         Principal = principal
    //         Status = AccessTokenStatus.Valid
    //     }

    // let Expired  =
    //     TokenResult
    //     {
    //         status = AccessTokenStatus.Expired
    //     }

    // let Error(ex: Exception) =
    //     TokenResult
    //     {
    //         status = AccessTokenStatus.Error,
    //         ex = ex
    //     }

    // let NoToken() =
    //     TokenResult
    //     {
    //         status = AccessTokenStatus.NoToken
    //     };


type AccessTokenProvider(issuerToken:string, audience:string, issuer:string) = 
        let AUTH_HEADER_NAME = "Authorization"
        let BEARER_PREFIX = "Bearer "

        member x.ValidateToken(request: HttpRequest) =
            if request != null &&
                request.Headers.ContainsKey(AUTH_HEADER_NAME) &&
                request.Headers.[AUTH_HEADER_NAME].ToString().StartsWith(BEARER_PREFIX) then
            
                let token = request.Headers.[AUTH_HEADER_NAME].ToString().Substring(BEARER_PREFIX.Length);

                // Create the parameters
                let tokenParams = TokenValidationParameters(
                    RequireSignedTokens = true,
                    ValidAudience = audience,
                    ValidateAudience = true,
                    ValidIssuer = issuer,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(issuerToken))
                )

                // Validate the token
                let handler = JwtSecurityTokenHandler()
                let result, securityToken = handler.ValidateToken(token, tokenParams);
                AccessTokenResult.Success(result);
            
            else
                AccessTokenResult.NoToken();
            // with (SecurityTokenExpiredException)
            // {
            //     return AccessTokenResult.Expired();
            // }
            // catch (Exception ex)
            // {
            //     return AccessTokenResult.Error(ex);
            // }

type MyStartup() = 
    inherit FunctionsStartup()

    // override u.ConfigureAppConfiguration(builder: IFunctionsConfigurationBuilder) =
    //     ignore

    let jwtBearerOptions (cfg : JwtBearerOptions) =
        cfg.SaveToken <- true
        cfg.IncludeErrorDetails <- true
        cfg.Authority <- System.Environment.GetEnvironmentVariable("OAUTH_AUTHORITY")
        cfg.Audience <- System.Environment.GetEnvironmentVariable("OAUTH_AUDIENCE")
        cfg.TokenValidationParameters <- TokenValidationParameters(
            NameClaimType = ClaimTypes.NameIdentifier,
            ValidateAudience = true
        )

    let authenticationOptions (o : AuthenticationOptions) =
        o.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
        o.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme

    override u.Configure(builder: IFunctionsHostBuilder) =
        // Our dependency
        let mutiply x y = x * y

        builder.Services.AddHttpClient() |> ignore
        // We can use plain functions as injected dependencies
        builder.Services.AddAuthentication(authenticationOptions) 
                .AddJwtBearer(Action<JwtBearerOptions> jwtBearerOptions) |> ignore


// FSharp way to create assembly targeted attributes
[<assembly: FunctionsStartup(typeof<MyStartup>)>]
do()

