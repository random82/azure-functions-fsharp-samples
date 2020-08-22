namespace Company.Function
open System;
open System.Security.Claims;
open System.Threading
open System.Threading.Tasks;
open Microsoft.AspNetCore.Authentication;
open Microsoft.AspNetCore.Authentication.JwtBearer;
open Microsoft.Azure.WebJobs;
open Microsoft.Azure.WebJobs.Extensions.Http;
open Microsoft.Extensions.DependencyInjection;
open Microsoft.IdentityModel.Tokens;
open Microsoft.IdentityModel.Protocols
open Microsoft.IdentityModel.Protocols.OpenIdConnect


module AuthenticationExtension =

    let private documentRetriever = HttpDocumentRetriever()

    let private AuthLevelClaimType = "http://schemas.microsoft.com/2017/07/functions/claims/authlevel";

    let getKeys issuer = 
        async{
            let configurationManager = 
                ConfigurationManager<OpenIdConnectConfiguration>(
                    issuer + ".well-known/openid-configuration",
                    OpenIdConnectConfigurationRetriever(),
                    documentRetriever
                )

            let! openIdConfig = configurationManager.GetConfigurationAsync(CancellationToken.None) |> Async.AwaitTask
            return seq openIdConfig.SigningKeys
        }

    let CreateTokenValidationParameters() = 
        let audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
        let issuer = Environment.GetEnvironmentVariable("JWT_AUTHORITY")
        let keys =  issuer |> getKeys |> Async.RunSynchronously

        TokenValidationParameters(
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKeys = keys
        )

    type AuthenticationBuilder with 
        member this.AddFunctionsJwtBearer() =
            let tokenValidationParameters = CreateTokenValidationParameters()
            this.AddJwtBearer("WebJobsAuthLevel", fun options -> 
                options.Events <- JwtBearerEvents(
                    OnMessageReceived = fun c ->
                        options.TokenValidationParameters <- tokenValidationParameters
                        Task.CompletedTask
                    ,OnTokenValidated = fun c ->
                        c.Principal.AddIdentity(
                            ClaimsIdentity(
                                [Claim(AuthLevelClaimType, AuthorizationLevel.Function.ToString())]
                            ))
                        c.Success()
                        Task.CompletedTask
                )
                options.TokenValidationParameters <- tokenValidationParameters
            )

    type IWebJobsBuilder with 
        member this.AddAuthentication() =
            this.Services.AddAuthentication()
                .AddFunctionsJwtBearer()

        member this.AddAuthentication configure =
            if (configure = null) then
                raise (ArgumentNullException("configure"))
            this.Services.Configure(configure) |> ignore
            this.AddAuthentication()
