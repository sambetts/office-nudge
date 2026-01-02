
import React from 'react';
import 'chartjs-adapter-date-fns'
import { ServiceConfiguration } from '../../apimodels/Models';
import { Button, Caption1, Card, CardHeader, Spinner, Text } from '@fluentui/react-components';
import { ChartContainer } from '../../components/app/ChartContainer';
import { MoreHorizontal20Regular } from "@fluentui/react-icons";
import { BaseAxiosApiLoader } from '../../api/AxiosApiLoader';
import { useStyles } from '../../utils/styles';
import { getClientConfig } from '../../api/ApiCalls';
import { RandomChart } from './RandomChart';


export const Dashboard: React.FC<{ loader?: BaseAxiosApiLoader }> = (props) => {

    const [serviceConfig, setServiceConfig] = React.useState<ServiceConfiguration | null>(null);
    const [error, setError] = React.useState<string | null>(null);
    const styles = useStyles();

    React.useEffect(() => {
        if (props.loader)
            getClientConfig(props.loader).then((d) => {
                setServiceConfig(d);
            }).catch((e: Error) => {
                console.error("Error: ", e);
                setError(e.toString());
            });
    }, [props.loader]);

    return (
        <div>
            <section className="page--header">
                <div className="page-title">
                    <h1>Teams SSO App</h1>

                    <p>Welcome to the Teams SSO example App control panel.</p>

                    {error ? <p className='error'>{error}</p>
                        :
                        <>
                            {serviceConfig ?
                                <div>
                                    <ChartContainer>

                                        <div className='nav'>
                                            <ul>
                                                <li>
                                                    <Card className={styles.card}>
                                                        <CardHeader
                                                            header={<Text weight="semibold">Survey Stats</Text>}
                                                            description={
                                                                <Caption1 className={styles.caption}>Responded vs surveyed</Caption1>
                                                            }
                                                            action={
                                                                <Button
                                                                    appearance="transparent"
                                                                    icon={<MoreHorizontal20Regular />}
                                                                    aria-label="More options"
                                                                />
                                                            }
                                                        />

                                                        <p className={styles.text}>
                                                            Random stat #2.
                                                        </p>

                                                        <RandomChart />
                                                    </Card>
                                                </li>
                                                <li>
                                                    <Card className={styles.card}>
                                                        <CardHeader
                                                            header={<Text weight="semibold">Another Stat</Text>}
                                                            description={
                                                                <Caption1 className={styles.caption}>Active Users vs Inactive</Caption1>
                                                            }
                                                            action={
                                                                <Button
                                                                    appearance="transparent"
                                                                    icon={<MoreHorizontal20Regular />}
                                                                    aria-label="More options"
                                                                />
                                                            }
                                                        />

                                                        <p className={styles.text}>
                                                            Some users are more active than others. This chart shows the distribution.
                                                        </p>

                                                        <RandomChart />
                                                    </Card>
                                                </li>
                                            </ul>
                                        </div>
                                    </ChartContainer>
                                </div> : <Spinner />
                            }
                        </>
                    }

                </div >
            </section >

        </div >
    );
};
