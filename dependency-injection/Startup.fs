namespace FSharpFunction

open Microsoft.Azure.Functions.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection;

// That wiwl work as an "interface"
type Multiplier = int -> int -> int 

type MyStartup() = 
    inherit FunctionsStartup()
    override u.Configure(builder: IFunctionsHostBuilder) =
        // Our dependency
        let mutiply x y = x * y

        builder.Services.AddHttpClient() |> ignore
        // We can use plain functions as injected dependencies
        builder.Services.AddSingleton<Multiplier>(mutiply) |> ignore

// FSharp way to create assembly targeted attributes
[<assembly: FunctionsStartup(typeof<MyStartup>)>]
do()

