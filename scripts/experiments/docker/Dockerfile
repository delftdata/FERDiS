FROM mcr.microsoft.com/dotnet/runtime:3.1
RUN mkdir /usr/share/man/man1
RUN apt-get update
RUN apt install -y default-jre
RUN java -version
WORKDIR /app
COPY ./bin .
ENTRYPOINT ["dotnet", "BlackSP.Benchmarks.dll"]