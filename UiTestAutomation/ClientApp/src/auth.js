import { PublicClientApplication } from "@azure/msal-browser";

export const msalApp = new PublicClientApplication({
    auth: {
        clientId: "6fb262de-3cac-443d-ba6d-afeb50aa005c",
        authority: "https://login.microsoftonline.com/e67aa680-aaaf-43bf-9ecb-dd25f3e75fc5",
        navigateToLoginRequestUrl: false,
        redirectUri: window.location.origin
    },
    cache: {
        cacheLocation: "localStorage"
    }
});
const scopes = ["api://6fb262de-3cac-443d-ba6d-afeb50aa005c/Data.Read"];

export const getApiToken = async () => {
    const accounts = msalApp.getAllAccounts();
    if (!accounts || accounts.length === 0) {
        throw new Error('No account found');
    }

    return await msalApp.acquireTokenSilent({
        account: accounts[0], // TODO: Add account selection
        scopes
    })
};

export const redirectToSignIn = () => {
    msalApp.acquireTokenRedirect({
        scopes
    });
};