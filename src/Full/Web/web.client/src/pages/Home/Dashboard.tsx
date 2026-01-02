import React from 'react';
import 'chartjs-adapter-date-fns'
import { ServiceConfiguration, MessageStatusStatsDto, UserCoverageStatsDto } from '../../apimodels/Models';
import { Button, Caption1, Card, CardHeader, Spinner, Text } from '@fluentui/react-components';
import { ChartContainer } from '../../components/app/ChartContainer';
import { MoreHorizontal20Regular } from "@fluentui/react-icons";
import { BaseAxiosApiLoader } from '../../api/AxiosApiLoader';
import { useStyles } from '../../utils/styles';
import { getClientConfig, getMessageStatusStats, getUserCoverageStats } from '../../api/ApiCalls';
import { MessageStatusChart } from './MessageStatusChart';
import { UserCoverageChart } from './UserCoverageChart';


export const Dashboard: React.FC<{ loader?: BaseAxiosApiLoader }> = (props) => {

    const [serviceConfig, setServiceConfig] = React.useState<ServiceConfiguration | null>(null);
    const [messageStats, setMessageStats] = React.useState<MessageStatusStatsDto | null>(null);
    const [userStats, setUserStats] = React.useState<UserCoverageStatsDto | null>(null);
    const [error, setError] = React.useState<string | null>(null);
    const [loadingStats, setLoadingStats] = React.useState(true);
    const styles = useStyles();

    React.useEffect(() => {
        if (props.loader) {
            getClientConfig(props.loader).then((d) => {
                setServiceConfig(d);
            }).catch((e: Error) => {
                console.error("Error: ", e);
                setError(e.toString());
            });

            // Load statistics
            loadStatistics();
        }
    }, [props.loader]);

    const loadStatistics = async () => {
        if (!props.loader) return;

        try {
            setLoadingStats(true);
            const [msgStats, usrStats] = await Promise.all([
                getMessageStatusStats(props.loader),
                getUserCoverageStats(props.loader)
            ]);
            setMessageStats(msgStats);
            setUserStats(usrStats);
        } catch (e: any) {
            console.error("Error loading statistics: ", e);
            setError(e.toString());
        } finally {
            setLoadingStats(false);
        }
    };

    return (
        <div>
            <section className="page--header">
                <div className="page-title">
                    <h1>Office Nudge Dashboard</h1>

                    <p>Welcome to the Office Nudge control panel. View message statistics and user coverage below.</p>

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
                                                            header={<Text weight="semibold">Message Status</Text>}
                                                            description={
                                                                <Caption1 className={styles.caption}>Sent, Failed, and Pending Messages</Caption1>
                                                            }
                                                            action={
                                                                <Button
                                                                    appearance="transparent"
                                                                    icon={<MoreHorizontal20Regular />}
                                                                    aria-label="More options"
                                                                />
                                                            }
                                                        />

                                                        {loadingStats ? (
                                                            <Spinner label="Loading statistics..." />
                                                        ) : messageStats ? (
                                                            <>
                                                                <p className={styles.text}>
                                                                    <strong>Total Messages:</strong> {messageStats.totalCount}<br />
                                                                    <strong>Sent:</strong> {messageStats.sentCount} | 
                                                                    <strong> Failed:</strong> {messageStats.failedCount} | 
                                                                    <strong> Pending:</strong> {messageStats.pendingCount}
                                                                </p>
                                                                <MessageStatusChart stats={messageStats} />
                                                            </>
                                                        ) : (
                                                            <p className={styles.text}>No data available</p>
                                                        )}
                                                    </Card>
                                                </li>
                                                <li>
                                                    <Card className={styles.card}>
                                                        <CardHeader
                                                            header={<Text weight="semibold">User Coverage</Text>}
                                                            description={
                                                                <Caption1 className={styles.caption}>Users Messaged vs Total Users in Tenant</Caption1>
                                                            }
                                                            action={
                                                                <Button
                                                                    appearance="transparent"
                                                                    icon={<MoreHorizontal20Regular />}
                                                                    aria-label="More options"
                                                                />
                                                            }
                                                        />

                                                        {loadingStats ? (
                                                            <Spinner label="Loading statistics..." />
                                                        ) : userStats ? (
                                                            <>
                                                                <p className={styles.text}>
                                                                    <strong>Total Users in Tenant:</strong> {userStats.totalUsersInTenant}<br />
                                                                    <strong>Users Messaged:</strong> {userStats.usersMessaged}<br />
                                                                    <strong>Coverage:</strong> {userStats.coveragePercentage.toFixed(2)}%
                                                                </p>
                                                                <UserCoverageChart stats={userStats} />
                                                            </>
                                                        ) : (
                                                            <p className={styles.text}>No data available</p>
                                                        )}
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
