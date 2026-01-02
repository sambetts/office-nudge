import React, { PropsWithChildren } from 'react';

import { Layout } from './components/app/Layout';
import { Dashboard } from './pages/Home/Dashboard';
import { LoginPopupMSAL } from './pages/Login/LoginPopupMSAL';
import { Redirect, Route } from "react-router-dom";
import { FluentProvider, teamsLightTheme, Theme } from '@fluentui/react-components';
import { LoginPopupTeams } from './pages/Login/LoginPopupTeams';
import { BaseAxiosApiLoader } from './api/AxiosApiLoader';

export const AppRoutes: React.FC<PropsWithChildren<AppRoutesProps>> = (props) => {

    return (
        <FluentProvider theme={teamsLightTheme}>
            {props.apiLoader ?
                (
                    <Layout apiLoader={props.apiLoader}>
                        <Route exact path="/">
                            <Redirect to="/tabhome" />
                        </Route>
                        <Route exact path='/tabhome' render={() => <Dashboard loader={props.apiLoader} />} />
                    </Layout>
                )
                :
                (
                    <Layout>
                        <Route exact path="/">
                            {props.loginMethod === LoginMethod.MSAL &&
                                <LoginPopupMSAL />
                            }
                            {props.loginMethod === LoginMethod.TeamsSSO &&
                                <LoginPopupTeams onAuthReload={props.onAuthReload} />
                            }
                        </Route>
                    </Layout>
                )}
        </FluentProvider>
    );
}
interface AppRoutesProps {
    apiLoader?: BaseAxiosApiLoader,
    loginMethod?: LoginMethod,
    onAuthReload: Function,
    theme: Theme
}

export enum LoginMethod {
    MSAL,
    TeamsSSO
}
