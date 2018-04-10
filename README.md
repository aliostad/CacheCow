[![Build status](https://ci.appveyor.com/api/projects/status/du7alwfjuuce7u04?svg=true)](https://ci.appveyor.com/project/aliostad/cachecow)

CacheCow
========

An implementation of HTTP Caching for .NET Core and 4.52+

## Breaking changes in Version 2.0.0

There are quite a few changes in the server-side implementation of CacheCow due to [shortcomings of previous approach](!!) hence if you have used CacheCow.Server in the past and you would like to migrate, read the guide on migration further down.

CacheCow.Client has some changes which may or may not break your code (depending whether you had used .NET 4.5 packages) which includes:

 - All interfaces are fully async now
 - Many cache storages no longer maintained and available including: MongoDb, Memcached, File and Elasticsearch. Currently only Redis, SQL and InMemory are supported although I accept PR if you would like to add your storage. 
 - ICacheStore implementations optionally supported a cache quota management interface which is no longer supported.
 - Renaming CacheCow client header from "x-cachecow" to "x-cachecow-client"
 - CacheCow.Client now copies any headers starting with x-cachecow it receives from the server to the response it delivers to your HttpClient.
 
 
 
## Migrating older CacheCow.Server projects to the new CacheCow.Server.Core.Mvc or CacheCow.Server.WebApi 