namespace Company.Function

open Company.Function.AuthenticationExtension
open Microsoft.Azure.Functions.Extensions.DependencyInjection
open Microsoft.Azure.WebJobs.Hosting
open Microsoft.Azure.WebJobs

type MyStartup() = 
    interface IWebJobsStartup with
        member u.Configure(builder: IWebJobsBuilder) =
            builder.AddAuthentication() |> ignore
// FSharp way to create assembly targeted attributes
[<assembly: FunctionsStartup(typeof<MyStartup>)>]
do()
