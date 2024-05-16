# ThereforeWorkflowRunner
## Overview
The Therefore Workflow Runner system provides the capability to schedule a recurring workflow task in the Therefore Document Management System. This allows a workflow to be repeated indefinitely without requiring any loops to be configured in the Therefore workflow itself.

The system also enables a workflow to be triggered from a Xero Webhook notification. This allows a workflow to be run immediately after an addition/change in Xero, without requiring polling. Xero Webhooks require configuration through the Xero Developer site.

All functions require an API Authorisation Key to be submitted with the request.

The Web Front End is extemely basic as it was just designed to be functional, and it will require an Auth Key for every request as session state is not kept.

## Installation
Most straightforward way to deploy is to use the docker image:

`docker pull fybre/thereforeworkflowrunner`

Sample extract from **docker-compose.yml**

 ```
thereforeworkflowrunner:
    image: fybre/thereforeworkflowrunner:latest
    container_name: thereforeworkflowrunner
    restart: 'unless-stopped'
    volumes:
      - /local/path/to/data:/app/Data
    ports:
      - 8080:8080
```
Internal port used for web server is 8080, so map as appropriate.
/app/Data contains the SQLite database, so can be mapped to a local volume if required. The database will be created if it doesn't exist.
A users.json file in this directory will ensure user/auth key accounts are created in the database. Contents of this file as below:
```
[
  {
    "Name": "Admin",
    "AuthKey": "Password to Add",
    "Role": 0
  },
    {
    "Name": "User1",
    "AuthKey": "Another Password",
    "Role": 1
  }
]
```
Roles: 0 = Admin, 1 = User

If no users exist in the database, and the users.json file is not available, a default user "AutoAdmin" will be created with a random key. This key will be echoed to the Docker console.

**NOTE**

The docker image should ALWAYS be run behind an SSL terminated reverse proxy server such an nginx, or a tunnel such as Cloudflare tunnels, as the Auth Keys are sent as part of parameters.
