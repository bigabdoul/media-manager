An ASP.NET Core 2 Web Application for organizing, listening to, and viewing personal media files. 
In order to get up and running with this project, there're a few things that you need to check.

# If you get a message like this: *The default database connection string is not configured...*
Make sure that you set up a database connection string. Do that in the command prompt:
```dotnet user-secrets set DefaultConnection <your database connection string>```

For example:
```dotnet user-secrets set DefaultConnection Server=(localdb)\\mssqllocaldb;Database=MyArts;Trusted_Connection=True;MultipleActiveResultSets=True;```

# A database operation failed while processing the request.

There are migrations for ApplicationDbContext that have not been applied to the database
* 00000000000000_CreateIdentitySchema
* 20180120224642_CustomizedApplicationUser
* 20180123125905_MediaFileId

In Visual Studio, you can use the Package Manager Console to apply pending migrations to the database:
`PM> Update-Database`

Alternatively, you can apply pending migrations from a command prompt at your project directory:
`> dotnet ef database update`
