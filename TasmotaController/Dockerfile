FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder  
 
RUN mkdir -p /root/src/app
WORKDIR /root/src/app
 
#copy just the project file over
# this prevents additional extraneous restores
# and allows us to re-use the intermediate layer
# This only happens again if we change the csproj.
# This means WAY faster builds!
COPY TasmotaController.csproj . 
#Because we have a custom nuget.config, copy it in
#COPY nuget.config . 
#RUN dotnet restore ./aspnetcoreapp.csproj 

COPY . .
RUN dotnet publish -c release -o published -r linux-arm 

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.4-buster-slim-arm32v7

WORKDIR /root/  
COPY --from=builder /root/src/app/published .

ENTRYPOINT ["dotnet", "TasmotaController.dll"]  
