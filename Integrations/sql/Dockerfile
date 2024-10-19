#FROM mcr.microsoft.com/mssql/server:2022-preview-ubuntu-22.04
FROM mcr.microsoft.com/mssql/server:latest
     
USER root
 
# Install node/npm
# RUN apt-get -y update  && \
#         apt-get install -y curl && \
#         curl -sL https://deb.nodesource.com/setup_14.x | bash - && \
#         apt-get install -y nodejs && \
#         apt-get install -y dos2unix

# Install tedious, the driver for SQL Server for Node.js
# RUN npm install tedious

# Create app directory
RUN mkdir -p /usr/src/app
WORKDIR /usr/src/app

 
COPY container_db /var/opt/mssql/data

# Configure the required environmental variables
ENV ACCEPT_EULA=Y
# ENV SA_PASSWORD=$PASSWORD
ENV SA_PASSWORD="quoosugTh5"
ENV MSSQL_SA_PASSWORD="quoosugTh5"

COPY . /app
WORKDIR /app

COPY . /usr/src/app

COPY ./mssql.conf /var/opt/mssql/



# Install SQL Tools: SQLPackage and sqlcmd
# COPY mssql/installSQLtools.sh installSQLtools.sh
# RUN bash ./installSQLtools.sh \
#      && apt-get clean -y && rm -rf /var/lib/apt/lists/* /tmp/library-scripts


# Expose doesn't seem to have effect with devcontainer
# EXPOSE 6000

# ENTRYPOINT ["./entrypoint.sh"]

COPY entrypoint.sh /entrypoint.sh
# Use the RUN command to execute it during build
# RUN /entrypoint.sh
RUN chmod +x /entrypoint.sh 

# The workspace files from devcontainer project are copied to container after Dockerfile is run, so required files needs to be copied manually. 
COPY ./.devcontainer/devcontainerstart.sh /
COPY ./.devcontainer/dockerimagestart.sh /

#The date printed is the date the image is created. So the date is stored in the image, and is not updated on instanciation
RUN sh /dockerimagestart.sh
 
