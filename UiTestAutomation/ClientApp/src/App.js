import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { FetchData } from './components/FetchData';
import { Counter } from './components/Counter';
import { msalApp, getApiToken, redirectToSignIn } from "./auth"

import './custom.css'

export default class App extends Component {
    static displayName = App.name;

    constructor(props) {
        super(props);
        this.state = {
            authenticationInProgress: true,
            isAuthenticated: false
        };
        this.signIn = this.signIn.bind(this);
    }

    componentDidMount() {
        this.handleAuthentication();
    }

    async handleAuthentication() {
        try {
            const result = await msalApp.handleRedirectResponse();
            let account;
            if (!result) {
                const tokenResult = await getApiToken();
                account = tokenResult.account;
            } else {
                account = result.account;
            }

            console.info(`User with id ${account.username} signed in`);

            this.setState({
                isAuthenticated: true,
                authenticationInProgress: false
            });
        } catch {
            this.setState({
                isAuthenticated: false,
                authenticationInProgress: false
            });
        }
    }

    signIn() {
        redirectToSignIn();
    }

    render() {
        if (this.state.authenticationInProgress) {
            return <p>Please wait...</p>
        }

        if (!this.state.isAuthenticated) {
            return (
                <>
                    <p>You need to sign in</p>
                    <button type="button" onClick={this.signIn}>Sign in</button>
                </>
            );
        }

        return (
            <Layout>
                <Route exact path='/' component={Home} />
                <Route path='/counter' component={Counter} />
                <Route path='/fetch-data' component={FetchData} />
            </Layout>
        );
    }
}
