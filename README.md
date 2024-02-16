# ProjectASP
This is project for my ASP.NET course in university

## To create database using EF code first, following these step below
Run on the package manager console
```
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```
