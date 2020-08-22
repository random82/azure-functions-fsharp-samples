namespace Company.Function
open System;
open System.Security.Claims;
open System.Text;
open System.Threading.Tasks;
open Microsoft.AspNetCore.Authentication;
open Microsoft.AspNetCore.Authentication.JwtBearer;
open Microsoft.Azure.WebJobs;
open Microsoft.Azure.WebJobs.Extensions.Http;
open Microsoft.Extensions.DependencyInjection;
open Microsoft.IdentityModel.Tokens;
open Microsoft.IdentityModel.Protocols
open Microsoft.IdentityModel.Protocols.OpenIdConnect
open System.Threading
open Microsoft.Azure.WebJobs.Host.Bindings


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
            let signingKeys = seq openIdConfig.SigningKeys
            return signingKeys
        }

    let CreateTokenValidationParameters() = 
        let issuer = Environment.GetEnvironmentVariable("JWT_AUTHORITY")
        let audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
        let keys =  getKeys issuer |> Async.RunSynchronously

        let tokenValidationParameters = 
                TokenValidationParameters(
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKeys = keys
                )  
        tokenValidationParameters

    type AuthenticationBuilder with 
        member this.AddFunctionsJwtBearer() =
            this.AddJwtBearer("WebJobsAuthLevel", fun options -> 
                options.Events <- JwtBearerEvents(
                    OnMessageReceived = fun c ->
                        options.TokenValidationParameters <- CreateTokenValidationParameters()
                        Task.CompletedTask
                    ,OnTokenValidated = fun c ->
                        c.Principal.AddIdentity(
                            ClaimsIdentity(
                                [Claim(AuthLevelClaimType, AuthorizationLevel.Function.ToString())]
                            ))
                        c.Success()
                        Task.CompletedTask
                )
                options.TokenValidationParameters <- CreateTokenValidationParameters()
            )

    type IWebJobsBuilder with 
        member this.AddAuthentication() =
            let services = this.Services
            services.AddAuthentication()
                .AddFunctionsJwtBearer()

        member this.AddAuthentication configure =
            if (configure = null) then
                raise (ArgumentNullException("configure"))

            let services = this.Services

            services.Configure(configure) |> ignore
            this.AddAuthentication()
