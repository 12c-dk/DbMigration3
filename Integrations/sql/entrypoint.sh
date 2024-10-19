#!/bin/bash

#start SQL Server, start the script to create the DB and import the data, start the app
# /opt/mssql/bin/sqlservr 
# & /usr/src/app/import-data.sh & npm start 
date >> /hello.log
echo "path is" >> /hello.log
pwd >> /hello.log
touch hellothere2
touch /hellothere2
# touch /var/hellothere2

# /opt/mssql-tools/bin/sqlcmd -U sa -P 'quoosugTh5' -S localhost,6000 -i LoadAdventureworksDb.sql >> /hello.log
#/opt/mssql-tools/bin/sqlcmd -U sa -P 'quoosugTh5' -S localhost,6000 -i LoadAdventureworksDb.sql -o sqloutput.log >> /hello.log &
