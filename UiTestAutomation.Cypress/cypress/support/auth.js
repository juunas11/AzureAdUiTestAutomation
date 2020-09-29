/// <reference types="cypress" />

import { decode } from "jsonwebtoken";
import authSettings from "./authsettings.json";

const {
  authority,
  clientId,
  clientSecret,
  apiScopes,
  username,
  password,
} = authSettings;
const environment = "login.windows.net";

const buildAccountEntity = (
  homeAccountId,
  realm,
  localAccountId,
  username,
  name
) => {
  return {
    authorityType: "MSSTS",
    // This could be filled in but it involves a bit of custom base64 encoding
    // and would make this sample more complicated.
    // This value does not seem to get used, so we can leave it out.
    clientInfo: "",
    homeAccountId,
    environment,
    realm,
    localAccountId,
    username,
    name,
  };
};

const buildIdTokenEntity = (homeAccountId, idToken, realm) => {
  return {
    credentialType: "IdToken",
    homeAccountId,
    environment,
    clientId,
    secret: idToken,
    realm,
  };
};

const buildAccessTokenEntity = (
  homeAccountId,
  accessToken,
  expiresIn,
  extExpiresIn,
  realm,
  scopes
) => {
  const now = Math.floor(Date.now() / 1000);
  return {
    homeAccountId,
    credentialType: "AccessToken",
    secret: accessToken,
    cachedAt: now.toString(),
    expiresOn: (now + expiresIn).toString(),
    extendedExpiresOn: (now + extExpiresIn).toString(),
    environment,
    clientId,
    realm,
    target: scopes.map((s) => s.toLowerCase()).join(" "),
    // Scopes _must_ be lowercase or the token won't be found
  };
};

const injectTokens = (tokenResponse) => {
  const idToken = decode(tokenResponse.id_token);
  const localAccountId = idToken.oid || idToken.sid;
  const realm = idToken.tid;
  const homeAccountId = `${localAccountId}.${realm}`;
  const username = idToken.preferred_username;
  const name = idToken.name;

  const accountKey = `${homeAccountId}-${environment}-${realm}`;
  const accountEntity = buildAccountEntity(
    homeAccountId,
    realm,
    localAccountId,
    username,
    name
  );

  const idTokenKey = `${homeAccountId}-${environment}-idtoken-${clientId}-${realm}-`;
  const idTokenEntity = buildIdTokenEntity(
    homeAccountId,
    tokenResponse.id_token,
    realm
  );

  const accessTokenKey = `${homeAccountId}-${environment}-accesstoken-${clientId}-${realm}-${apiScopes.join(
    " "
  )}`;
  const accessTokenEntity = buildAccessTokenEntity(
    homeAccountId,
    tokenResponse.access_token,
    tokenResponse.expires_in,
    tokenResponse.ext_expires_in,
    realm,
    apiScopes
  );

  localStorage.setItem(accountKey, JSON.stringify(accountEntity));
  localStorage.setItem(idTokenKey, JSON.stringify(idTokenEntity));
  localStorage.setItem(accessTokenKey, JSON.stringify(accessTokenEntity));
};

export const login = (cachedTokenResponse) => {
  let tokenResponse = null;
  let chainable = cy.visit("/");

  if (!cachedTokenResponse) {
    chainable = chainable.request({
      url: authority + "/oauth2/v2.0/token",
      method: "POST",
      body: {
        grant_type: "password",
        client_id: clientId,
        client_secret: clientSecret,
        scope: ["openid profile"].concat(apiScopes).join(" "),
        username: username,
        password: password,
      },
      form: true,
    });
  } else {
    chainable = chainable.then(() => {
      return {
        body: cachedTokenResponse,
      };
    });
  }

  chainable
    .then((response) => {
      injectTokens(response.body);
      tokenResponse = response.body;
    })
    .reload()
    .then(() => {
      return tokenResponse;
    });

  return chainable;
};
