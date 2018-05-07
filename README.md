[![Build status](https://ci.appveyor.com/api/projects/status/du7alwfjuuce7u04?svg=true)](https://ci.appveyor.com/project/aliostad/cachecow)

CacheCow
========

An implementation of HTTP Caching in .NET Core and 4.52+ for HttpClient, ASP.NET Web API and ASP.NET MVC Core

## NOTE: Breaking changes in Version 2.0.0

There are quite a few changes in the *server-side* implementation of CacheCow due to [shortcomings of previous approach](http://byterot.blogspot.co.uk/2017/04/Future-CacheCow-birth-CacheCore-REST-HTTP-dotnetcore-middleware-caching-conditional-put-get.html) while the *client-side* mostly remained the same. Hence if you have used CacheCow.Server in the past and you would like to migrate, read the guide on migration further down. CacheCow.Client has some changes which may or may not break your code (depending whether you had used .NET 4.5 packages) which includes:

 - All interfaces are fully async now
 - Many cache storages no longer maintained and available including: MongoDb, Memcached, File and Elasticsearch. Currently only Redis, SQL and InMemory are supported although I accept PR if you would like to add your storage. 
 - ICacheStore implementations optionally supported a cache quota management interface which is no longer supported.
 - Renaming CacheCow client header from "x-cachecow" to "x-cachecow-client"
 - CacheCow.Client now copies any headers starting with x-cachecow it receives from the server to the response it delivers to your HttpClient.
 
## Concepts and definitions

It is useful to start with concepts but feel free to use it as a reference. If you get bored reading this 😀, jump straight to [samples](https://github.com/aliostad/CacheCow/tree/master/samples) and run a couple of them.

- **Resource**: A RESTful resource - generally identified by its URI.
- **Representation**: A particular embodiment of a resource according to its format, language, etc. For example `/api/car/1` is a resource and it can have two representations as JSON or XML. Also the same resource could have two representations in Chinese or Spanish. Headers by which representations will vary is defined by the `Vary` header from the server.
 - **HTTP Caching**: Caching as described in HTTP 1.1 Specification (RFC 7230 or 2616). Despite the common notion, the *representations* get cached on the *client* (not server) and the server is responsible for providing cache directives and validate conditional requests on *resources*.
 - **Cache directives**: These are various cache-related headers that the server provides to guide the client on how best cache and validate the resources. These headers include `Cache-Control`, `Vary`, `ETag` and in HTTP 1.0 would include `Expires`, `Last-Modified` and `Pragma` (for the purpose of this article, we include `ETag` and `Last-Modified` as part of directives although purely speaking they are not). CacheCow has `ICacheDirectiveProvider` interface responsible for controlling these headers.
 - **`Expires` header**: Defines expiry of a resource in absolute time. `Cache-Control` header provides richer semantic and supersedes it. CacheCow sets both to support both HTTP 1.0 and 1.1.  
 - **`Last-Modified` header**: Since its precision is at seconds, its use is not recommended for valiation and instead, it is recommended to use `ETag`.
 - **`ETag` header**: ETag or EntityTag is an opaque string that identifies a version of resource (not representation). If you have a high-precision Last-Modified date, it is better to turn it to ETag by binarising the date ([example](https://github.com/aliostad/CacheCow/blob/master/samples/CacheCow.Samples.Common/Extensions.cs#L15)).
 - **TimedETag**: A CacheCow construct that combines Last-Modifed and ETag (can have either of them but not both) and represents a version of the resource and implemented as `TimedEntityTagHeaderValue`. It is recommended to construct it with an ETag (due to low precision of DateTime in HTTP Spec's `Last-Modified` header).
 - **`ITimedETagExtractor`**: A CacheCow interface responsible for extracting TimedETag from view models sent back by the API. By default, it checks to see if the ViewModel has implemented ICacheResource, if so it enquires the TimedETag directly. If not, it will resort to serialising the ViewModel to JSON and use its hash as ETag. This can be expensive hence it is suggested to either your ViewModels implementing `ICacheResource` interface or implement `ITimedETagExtractor` - plenty of examples in the [samples](https://github.com/aliostad/CacheCow/tree/master/samples).
 - **Consistency**: Resources underlying the cached representationa could change without any notice making them invalid - this problem has been half-joking known as one of the [two most difficult problems in computer science](https://martinfowler.com/bliki/TwoHardThings.html), the other being nameing. For many resources this could be acceptable while for some simply not tolerable. HTTP solves this problem with a single round-trip call that would either approve the use of the version client has cached (by returing status 304) or return the latest version of the resource in the representation requested. This is known as conditional GET. HTTP also provides means for optimistic concurrency and only update (or delete) of the resource if it matches the ETag or has not changed since the `Last-Modified` date supplied. This is known as conditional PUT (or DELETE). CacheCow supports these scenarios OOB.
 - **Zero-Expiry resources**: These resources require a high consistency and the clients are allowed to cache the representations and re-use only if they make a conditional GET to confirm the version they have is valid. Essentially, the representation is *expired* as soon as it leaves the server but can be safely re-used if the client makes a conditional GET. It might seem that caching in these scenarios is not really beneficial, but in fact it helps to reduce network traffic, client and server resource usage and even can protect back-end systems. The trick is to know whether a resource has changed without loading it all the way from bak-end systems. For example, to know whether a record has been changed, we can check whether its LastMosifiedDate (or Timestamp) has been modified. Or for a list of records, the most recent LastModifiedDate can be used along with the count of records. Such queries will be much faster to run.
 
 CacheCow provides `ITimedETagQueryProvider` interface that 
 - **`ITimedETagQueryProvider`**: 
 
 


 

 
## Migrating older CacheCow.Server projects to the new CacheCow.Server.Core.Mvc or CacheCow.Server.WebApi 
