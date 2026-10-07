# NotesAPI

A REST API and WPF desktop client for a note-taking application.

## Features

* Notes CRUD
* Categories
* Search, filtering, sorting and pagination
* User registration and login
* JWT authentication
* Role-based authorization

## Technologies

* C#
* .NET 9
* ASP.NET Core Minimal API
* Entity Framework Core
* SQLite
* WPF / XAML
* JWT
* Swagger / OpenAPI

## Structure

* **NotesAPI** — ASP.NET Core REST API
* **NotesClient** — WPF desktop client

The client communicates with the API over HTTP.

## Setup

Requires **Visual Studio 2022** and **.NET 9 SDK**.

The API uses SQLite for local data storage and ASP.NET Core User Secrets for the JWT signing key.

Open `NotesAPI.sln`, configure the JWT secret through **Manage User Secrets**, and run the API and client.

## License

No license specified.
