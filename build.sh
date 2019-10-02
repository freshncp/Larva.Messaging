#!/bin/sh
# build
dotnet build -c Release -f 'netstandard1.6' -o `pwd`/nuget/Larva.Messaging/lib/netstandard1.6/ `pwd`/src/Larva.Messaging/
dotnet build -c Release -f 'netstandard2.0' -o `pwd`/nuget/Larva.Messaging/lib/netstandard2.0/ `pwd`/src/Larva.Messaging/

dotnet build -c Release -f 'netstandard1.6' -o `pwd`/nuget/Larva.Messaging.RabbitMQ/lib/netstandard1.6/ `pwd`/src/Larva.Messaging.RabbitMQ/
dotnet build -c Release -f 'netstandard2.0' -o `pwd`/nuget/Larva.Messaging.RabbitMQ/lib/netstandard2.0/ `pwd`/src/Larva.Messaging.RabbitMQ/

# test
#dotnet test `pwd`/src/DynamicProxyTests/

# clean
rm -rf nuget/*/.DS_Store
rm -rf nuget/*/*/.DS_Store
rm -rf nuget/*/*/*/.DS_Store
rm -rf nuget/*/lib/*/*.pdb
rm -rf nuget/*/lib/*/*.json
rm -rf nuget/Larva.Messaging.RabbitMQ/lib/*/Larva.Messaging.dll

# pack
nuget pack -OutputDirectory `pwd`/packages/ `pwd`/nuget/Larva.Messaging/Larva.Messaging.nuspec
nuget pack -OutputDirectory `pwd`/packages/ `pwd`/nuget/Larva.Messaging.RabbitMQ/Larva.Messaging.RabbitMQ.nuspec
