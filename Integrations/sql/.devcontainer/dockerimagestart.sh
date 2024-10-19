#!/bin/sh
# This only runs when Docker image is being built, e.g. when Dockerfile has been changed. Image content can be modified here, but not running services. 

echo "dockerimagestart.sh is running"

echo "dockerimagestart.sh started at: " $(date) >> /docker.log
echo "path is: " $(pwd) >> /docker.log


apt-get update && apt-get install -y sudo
apt-get install -y vim

cd ~
apt-get update && apt-get install -y unzip
wget https://aka.ms/sqlpackage-linux
mkdir sqlpackage
unzip sqlpackage-linux -d ~/sqlpackage 
echo "export PATH=\"\$PATH:$HOME/sqlpackage\"" >> ~/.bashrc
chmod a+x ~/sqlpackage/sqlpackage
# source ~/.bashrc



echo "/opt/mssql/bin/sqlservr &" >> ~/.bashrc