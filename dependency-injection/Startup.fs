namespace Company.Function

open Microsoft.Azure.Functions.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection;

// The function signature will work as an "interface"
type Multiplier = MulDef of (int -> int -> int)
type Adder = AddDef of (int -> int -> int)



type MyStartup() = 
    inherit FunctionsStartup()

    let multiply = MulDef(fun x y -> x * y)

    let add = AddDef(fun x y -> x + y)

    override u.Configure(builder: IFunctionsHostBuilder) =
        // Our dependency

        builder.Services.AddHttpClient() |> ignore
        // We can use plain functions as injected dependencies
        builder.Services.AddSingleton<Multiplier>(multiply) |> ignore
        builder.Services.AddSingleton<Adder>(add) |> ignore
        do()
        
// FSharp way to create assembly targeted attributes
[<assembly: FunctionsStartup(typeof<MyStartup>)>]
do()

