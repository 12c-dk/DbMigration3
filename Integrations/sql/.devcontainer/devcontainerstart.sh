#!/bin/sh
# This runs when the rebuilding the devcontainer from vscode, e.g. 'Dev Containers: Rebuild Container'

echo "devcontainerstart.sh is running"

echo "devcontainerstart.sh ran at: " $(date) >> /docker.log
echo "path is: " $(pwd) >> /docker.log

#&& nohup bash -c '/opt/mssql/bin/sqlservr &' 

#Virker
# /opt/mssql/bin/sqlservr &


# while ! test -d /somemount/share/folder
# do
#     echo "Waiting for mount /somemount/share/folder..."
#     ((c++)) && ((c==10)) && break
#     sleep 1
# done



# Enabling fhe following section stops the sql server, so it has to be manually started. 

# /opt/mssql-tools/bin/sqlcmd -S localhost,6000 -U SA -P "quoosugTh5" -Q "SELECT 1" || true

# echo "Waiting for SQL Server to start..." 
# echo "Waiting for SQL Server to start..." >> /docker.log
# until /opt/mssql-tools/bin/sqlcmd -S localhost,6000 -U SA -P "quoosugTh5" -Q "SELECT 1" > /dev/null 2>&1; do
#     # ((c++)) && ((c==10)) && echo "Server start failed" && break
#     ((c++)) && ((c==10)) && echo "Server start failed" && exit
#     sleep 1
#     echo "Retrying..."
# done

# echo "SQL Server started. Loading Adventureworks database..."
# echo "SQL Server started. Loading Adventureworks database..." >> /docker.log

# /opt/mssql-tools/bin/sqlcmd -S localhost,6000 -U SA -P "quoosugTh5" -i LoadAdventureworksDb.sql

