# UI testing an Azure AD protected single page app sample

This sample showcases how an Azure AD protected single page application using MSAL.js 2.x can be tested.
It uses the ROPC authentication flow to acquire tokens for a test user account,
and injects them into browser local storage before running the tests.
This way MSAL.js does not attempt to acquire tokens as it already has them in cache.

Repository contents:

- UiTestAutomation: ASP.NET Core application with API and React single page app
- UiTestAutomation.Cypress: UI tests for the app built with Cypress and JavaScript
- UiTestAutomation.Tests: UI tests for the app using Selenium and C#

## Setup

To run this application, you will need to create an app registration and a user account in an Azure AD tenant.
It is recommended that all of this is done in a development/test tenant and not your production environment.

Information for app registration:

- Supported accounts: Accounts in this directory only
- Redirect URI: `https://localhost:44338` (Single Page Application type)
- Add a client secret
- Define a scope on the _Expose an API_ tab
  - Accept the default app ID URI when asked or define your own
  - Name: Data.Read

Take note of:

- Directory id/tenant id
- Application id/client id
- Client secret
- App ID URI
- Scope id (shown after creating a scope, it's app ID URI + scope name)

The user account should be a local user in the AAD tenant with no MFA enabled.
Set a secure password on it.
Take note of:

- Username
- Password

Now that we have all of this info, we can configure the app.

To configure the API, open `UiTestAutomation/appsettings.json` and specify values in the Authentication section:

- Authority: `https://login.microsoftonline.com/` + your directory id/tenant id
- ClientId: your application id/client id
- AppIdUri: your App ID URI

To configure the single page app, open `UiTestAutomation/ClientApp/src/auth.js` and specify:

- clientId: your application id/client id
- authority: `https://login.microsoftonline.com/` + your directory id/tenant id
- scopes: array with the scope id of the scope you defined earlier

Now the app can be run and tested.
You should be able to view the weather forecast page that fetches data from the API.
If you get the data, everything is working correctly.

To configure the Cypress tests, create `UiTestAutomation.Cypress/cypress/support/authsettings.json`, and fill it in based on the `authsettings.sample.json` in the support folder:

- authority: `https://login.microsoftonline.com/` + your directory id/tenant id
- clientId: your application id/client id
- clientSecret: your application client secret
- apiScopes: array with the scope id of the scope you defined earlier
- username: Username of the created user
- password: Password of the created user

Configuring the Selenium tests is done through user secrets.
The values are the same as Cypress tests, except `apiScopes` needs to be `scopes` instead in the user secrets.
