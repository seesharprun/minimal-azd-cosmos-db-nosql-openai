# Sample application

1. Run `azd init`
1. Run `azd provision`
1. After provision, endpoints are automatically written back to .NET secrets store
1. Run code
    1. Change to `/src/web` directory
    1. Run code using `dotnet watch run`
1. or Debug code
    1. Use `F5` in Visual Studio Code

## Review code

Most appliation code can be found in the `src/web/Components/Pages/Home.razor` and `src/web/Components/Pages/Home.razor.cs` files.
