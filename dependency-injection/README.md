# Sample - dependency injection

## Preparing the environment

### Install VSCode and Ionide

[Official guide](https://docs.microsoft.com/en-us/dotnet/fsharp/get-started/get-started-vscode)

### Install Azure Functions Core tools

I'm using Azure Functions v3 in this example

```powershell
npm i -g azure-functions-core-tools@3 --unsafe-perm true
```

OR

```powershell
choco install azure-functions-core-tools
```

[More details and steps for non-Windows environments](https://www.npmjs.com/package/azure-functions-core-tools)

## Executing sample

Navigate to `./dependency-injection' folder.

```powershell
func start
```