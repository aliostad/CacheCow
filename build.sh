dotnet build CacheCow.CoreOnly.sln
dotnet test ./test/CacheCow.Tests/CacheCow.Tests.csproj -l trx
dotnet test ./test/CacheCow.Client.Tests/CacheCow.Client.Tests.csproj -l trx
dotnet test ./test/CacheCow.Server.Core.Mvc.Tests/CacheCow.Server.Core.Mvc.Tests.csproj -l trx
dotnet test ./test/CacheCow.Client.RedisCacheStore.Tests/CacheCow.Client.RedisCacheStore.Tests.csproj -l trx
dotnet test ./test/CacheCow.Client.NatsKeyValueCacheStore.Tests/CacheCow.Client.NatsKeyValueCacheStore.Tests.csproj -l trx
