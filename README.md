# spisum-core

**SpisUm** is czech electronic record management system which is according to czech legislation.

## Version

v1.0-beta3

## Prerequisities

First you have to deploy project https://github.com/ISFG/alfresco-core, at least 10 minutes after deploy of alfresco-core can be deployed spisum-core. Alfresco-core will make Alfresco AGS and database, spisum-core will make structure in Alfresco AGS for SpisUm.

- GIT
- Docker
- Docker-compose

## How to run application

In file **ISFG.SpisUm/appsettings.json** change URL address to your URL address where you will run the application (not important, it's used by Swagger)

```json
{
    "Api": {
        "Url": "https://hostname.domain/",
        ...
    }
}
```

For signing documents with use of 602 SW you have to set in both scripts following

```json
"Signer": {
    "Base": {
        "Uri": "secusign_url",
        "SecurityMode": "TransportCredentialOnly",
        "ClientCredentialType": "Basic",
        "UserNamePasswordCredentials": {
                "UserName": "username",
                "Password": "password"
        }
    }
}
```

```bash
$ git clone https://github.com/ISFG/spisum-core.git -b master --single-branch spisum-core
$ cd spisum-core
$ docker-compose up -d --build
```

## Note

If you found the issue let us know on development@spisum.cz or create issue directly in GIT.
