# DevContainer

SQL container for testing sql connection.

Also works with ssms, when enabling trust server certificate under options


# Connect

Connect to database container (Connect from host): 

```bash
docker exec -it dbmigration_devcontainer-database-1 /bin/bash
```

# Load Adventureworks database

## From container

Load Adventureworks database from container and drop database. 

```bash
cd /var/opt/mssql/data
/opt/mssql-tools/bin/sqlcmd -U sa -P 'quoosugTh5' -S localhost,1433 -Q "USE master;SELECT name from sys.databases"
/opt/mssql-tools/bin/sqlcmd -S localhost,1433 -U SA -P "quoosugTh5" -i /var/opt/mssql/data/LoadAdventureworksDb.sql
/opt/mssql-tools/bin/sqlcmd -U sa -P 'quoosugTh5' -S localhost,1433 -Q "drop database AdventureWorks2019"
```


See StartSqlServer.md for manual startup and load of sql container.

[StartSqlServer.md](../Integrations/sql/StartSqlServer.md)

# Debugging dockerfile changes

Build container and login interactively. Container will be removed when exiting. This way changes can be tested in container quickly and trying again in a short cycle. 

```bash
docker build -t dbmigratedb . -f .\DockerfileSqlServer ; docker run -it -p 6000:6000 --rm dbmigratedb /bin/bash
```

When signed in to the container, Dockerfile RUN commands can be tested. 

Observation: When running "sqlservr &" in Dockerfile and building container using 'docker build' the sqlservr is not running on startup. But with the same change to Dockerfile and using docker-compose.yml the sqlservr is running on startup.

# Reload databases

Databases are created from the LoadAdventureworksDb.sql script. To recreate databses, delete the database container and rebuild the devcontainer. 

# Setup SSH between containers

```bash
passwd
apt-get update
apt-get install vim  -y
apt-get install openssh-client openssh-server -y
vim /etc/ssh/sshd_config 

# Change or add the line "PermitRootLogin yes"
:q #To exit vim

service ssh restart

ssh root@database
```

