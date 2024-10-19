# Introduction 

SQL container for testing sql connection.

Also works with ssms, when enabling trust server certificate under options

# Load Adventureworks database

On host, download the AdventureWorks database to the container_db folder. 

```Powershell
cd .\Integrations\sql\
mkdir container_db -Force
cd container_db
Invoke-WebRequest https://github.com/Microsoft/sql-server-samples/releases/download/adventureworks/AdventureWorks2019.bak -OutFile AdventureWorks2019.bak
cd ..
```

## Start project container using docker (For offline use)

```powershell
cd .\Integrations\sql\
docker build -t dbmigratedb . ; docker run -it -p 6000:1433 --rm dbmigratedb /bin/bash

docker build -t dbmigratedb . -f .\DockerfileSqlServer ; docker run -it -p 6000:1433 --rm dbmigratedb /bin/bash
```

In container, startup SQL server

```bash
ps -ef |grep /opt/mssql/bin/sqlservr
/opt/mssql/bin/sqlservr &
```

Import database and check. 

```bash
/opt/mssql-tools18/bin/sqlcmd -U sa -P 'quoosugTh5' -S localhost,1433 -Q "USE master;SELECT name from sys.databases" -C
/opt/mssql-tools18/bin/sqlcmd -S localhost,6000 -U SA -P "quoosugTh5" -i LoadAdventureworksDb.sql
```

Install common tools:

apt-get update && apt-get install -y sudo
apt-get install -y vim

Import bacpac

Worked:
```bash	
cd ~
apt-get update && apt-get install -y unzip
wget https://aka.ms/sqlpackage-linux
mkdir sqlpackage
unzip sqlpackage-linux -d ~/sqlpackage 
echo "export PATH=\"\$PATH:$HOME/sqlpackage\"" >> ~/.bashrc
chmod a+x ~/sqlpackage/sqlpackage
source ~/.bashrc
sqlpackage

cd /app
sqlpackage /Action:Import /SourceFile:container_db/KK_Temp.bacpac  /TargetConnectionString:"Data Source=localhost,6000;User ID=sa; Password=quoosugTh5; Initial Catalog=KK_Temp; Integrated Security=false;TrustServerCertificate=True;"
```

Import bacpac
```
.\sqlpackage\sqlpackage.exe /Action:Import /SourceFile:"c:\Users\newsl\OneDrive\Projekter\KK\opgaver\231124 csvView\Original\KK_Temp.bacpac" /TargetConnectionString:"Data Source=localhost,6000;User ID=sa; Password=quoosugTh5; Initial Catalog=KK_Temp; Integrated Security=false;"
```

Connect from host: 
```powershell
sqlcmd -S localhost,6000 -U sa -P 'quoosugTh5' -Q "USE master;SELECT name from sys.databases"
```

# References

[Microsoft SQL Server - Ubuntu based images](https://hub.docker.com/_/microsoft-mssql-server)

