namespace Company.Function

open Microsoft.Azure.Functions.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection;

// The function signature will work as an "interface"
type Multiplier = int -> int -> int 
type Adder = int -> int -> int 

type MyStartup() = 
    inherit FunctionsStartup()

    let mutiply x y = x * y

    let add x y = x + y

    override u.Configure(builder: IFunctionsHostBuilder) =
        // Our dependency

        builder.Services.AddHttpClient() |> ignore
        // We can use plain functions as injected dependencies
        builder.Services.AddSingleton<Multiplier>(mutiply) |> ignore
        builder.Services.AddSingleton<Adder>(add) |> ignore

// FSharp way to create assembly targeted attributes
[<assembly: FunctionsStartup(typeof<MyStartup>)>]
do()

