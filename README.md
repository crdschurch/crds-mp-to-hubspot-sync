# crds-mp-hubspot-sync

Syncs newly registered MP contacts to HubSpot

## Development

#### Install dependencies

```
dotnet restore
```

#### Environment variables

create a .env file in the project root with the following variables and set
values

```
API_PASSWORD
API_USER
APP_LOG_ROOT
HUBSPOT_API_KEY
MP_OAUTH_BASE_URL
MP_REST_API_ENDPOINT
CRDS_MP_COMMON_CLIENT_ID
CRDS_MP_COMMON_CLIENT_SECRET
```

#### Running the app

```sh
cd Crossroads.Service.HubSpot.Sync.App
dotnet run
# or if you want to watch files for changes
dotnet watch run
```

> Your project will run in the console

#### Running tests

```sh
dotnet test
# or if you want to watch test files for changes
dotnet watch test
```

## Deployment


## Logging

To use logging, you may inject the ILogger<T> instance into the constructor of your dependent class definition, where T is the type of the subscribing class.

Then, add the actual logging call as such: _logger.LogError("Error in GetEventOpportunity: " + ex.Message, ex);

Other config settings live in appsettings.json.
