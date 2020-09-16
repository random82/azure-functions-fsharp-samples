namespace Company.Function

open Microsoft.Azure.Functions.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection;


type MultiplyResult = MultiplyResult of int
type AdditionResult = AdditionResult of int

// The function signature will work as an "interface"
type Multiplier = int -> int -> MultiplyResult 
type Adder = int -> int -> AdditionResult 



type MyStartup() = 
    inherit FunctionsStartup()

    let mutiply x y = MultiplyResult(x * y)

    let add x y = AdditionResult(x + y)

    override u.Configure(builder: IFunctionsHostBuilder) =
        // Our dependency

        builder.Services.AddHttpClient() |> ignore
        // We can use plain functions as injected dependencies
        builder.Services.AddSingleton<Multiplier>(mutiply) |> ignore
        builder.Services.AddSingleton<Adder>(add) |> ignore
        do()
        
// FSharp way to create assembly targeted attributes
[<assembly: FunctionsStartup(typeof<MyStartup>)>]
do()

