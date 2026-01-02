import React, { useState, useEffect } from 'react';
import {
    Button,
    Card,
    CardHeader,
    Spinner,
    Text,
    makeStyles,
    tokens,
    Table,
    TableBody,
    TableCell,
    TableRow,
    TableHeader,
    TableHeaderCell,
    TableCellLayout,
    Badge,
    ProgressBar,
} from '@fluentui/react-components';
import {
    ArrowLeft20Regular,
    Checkmark20Regular,
    Dismiss20Regular,
    Clock20Regular,
} from '@fluentui/react-icons';
import { useParams, useHistory } from 'react-router-dom';
import { BaseAxiosApiLoader } from '../../api/AxiosApiLoader';
import {
    getBatch,
    getMessageLogsByBatch,
    getTemplate,
} from '../../api/ApiCalls';
import { MessageBatchDto, MessageLogDto, MessageTemplateDto } from '../../apimodels/Models';

const useStyles = makeStyles({
    container: {
        padding: tokens.spacingVerticalXXL,
    },
    header: {
        marginBottom: tokens.spacingVerticalL,
    },
    backButton: {
        marginBottom: tokens.spacingVerticalM,
    },
    card: {
        marginBottom: tokens.spacingVerticalL,
    },
    summaryGrid: {
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
        gap: tokens.spacingHorizontalL,
        marginTop: tokens.spacingVerticalM,
    },
    summaryItem: {
        padding: tokens.spacingVerticalM,
        backgroundColor: tokens.colorNeutralBackground2,
        borderRadius: tokens.borderRadiusMedium,
    },
    summaryNumber: {
        fontSize: '32px',
        fontWeight: 'bold',
        marginTop: tokens.spacingVerticalS,
    },
    progressSection: {
        marginTop: tokens.spacingVerticalL,
    },
    error: {
        color: tokens.colorPaletteRedForeground1,
        marginTop: tokens.spacingVerticalM,
    },
    statusBadge: {
        display: 'inline-flex',
        alignItems: 'center',
        gap: tokens.spacingHorizontalXS,
    },
    errorDetails: {
        marginTop: tokens.spacingVerticalXS,
        fontSize: tokens.fontSizeBase200,
        color: tokens.colorPaletteRedForeground1,
    },
    batchInfo: {
        display: 'flex',
        flexDirection: 'column',
        gap: tokens.spacingVerticalS,
    },
});

interface BatchProgressPageProps {
    loader?: BaseAxiosApiLoader;
}

interface BatchProgressParams {
    batchId: string;
}

export const BatchProgressPage: React.FC<BatchProgressPageProps> = ({ loader }) => {
    const styles = useStyles();
    const history = useHistory();
    const { batchId } = useParams<BatchProgressParams>();

    const [batch, setBatch] = useState<MessageBatchDto | null>(null);
    const [template, setTemplate] = useState<MessageTemplateDto | null>(null);
    const [logs, setLogs] = useState<MessageLogDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [autoRefresh, setAutoRefresh] = useState(true);

    useEffect(() => {
        if (batchId && loader) {
            loadBatchData();
        }
    }, [batchId, loader]);

    useEffect(() => {
        if (!autoRefresh || !batchId || !loader) return;

        const interval = setInterval(() => {
            loadBatchData(true);
        }, 5000); // Refresh every 5 seconds

        return () => clearInterval(interval);
    }, [autoRefresh, batchId, loader]);

    const loadBatchData = async (silent: boolean = false) => {
        if (!loader) return;

        try {
            if (!silent) {
                setLoading(true);
            }
            setError(null);

            const [batchData, logsData] = await Promise.all([
                getBatch(loader, batchId),
                getMessageLogsByBatch(loader, batchId),
            ]);

            setBatch(batchData);
            setLogs(logsData);

            // Load template info
            if (batchData.templateId) {
                const templateData = await getTemplate(loader, batchData.templateId);
                setTemplate(templateData);
            }

            // Stop auto-refresh if all messages are in a final state
            const allFinal = logsData.every(
                log => log.status === 'Sent' || log.status === 'Failed'
            );
            if (allFinal) {
                setAutoRefresh(false);
            }
        } catch (err: any) {
            setError(err.message || 'Failed to load batch data');
            console.error('Error loading batch data:', err);
            setAutoRefresh(false);
        } finally {
            if (!silent) {
                setLoading(false);
            }
        }
    };

    const getStatusBadge = (status: string) => {
        switch (status.toLowerCase()) {
            case 'sent':
                return (
                    <Badge appearance="filled" color="success" className={styles.statusBadge}>
                        <Checkmark20Regular /> Sent
                    </Badge>
                );
            case 'failed':
                return (
                    <Badge appearance="filled" color="danger" className={styles.statusBadge}>
                        <Dismiss20Regular /> Failed
                    </Badge>
                );
            case 'pending':
                return (
                    <Badge appearance="filled" color="warning" className={styles.statusBadge}>
                        <Clock20Regular /> Pending
                    </Badge>
                );
            default:
                return <Badge appearance="outline">{status}</Badge>;
        }
    };

    const handleBack = () => {
        history.push('/sendnudge');
    };

    if (loading && !batch) {
        return (
            <div className={styles.container}>
                <Spinner label="Loading batch data..." />
            </div>
        );
    }

    if (error && !batch) {
        return (
            <div className={styles.container}>
                <div className={styles.error}>{error}</div>
                <Button className={styles.backButton} icon={<ArrowLeft20Regular />} onClick={handleBack}>
                    Back to Send Nudge
                </Button>
            </div>
        );
    }

    if (!batch) {
        return (
            <div className={styles.container}>
                <Text>Batch not found</Text>
                <Button className={styles.backButton} icon={<ArrowLeft20Regular />} onClick={handleBack}>
                    Back to Send Nudge
                </Button>
            </div>
        );
    }

    const sentCount = logs.filter(log => log.status === 'Sent').length;
    const failedCount = logs.filter(log => log.status === 'Failed').length;
    const pendingCount = logs.filter(log => log.status === 'Pending').length;
    const totalCount = logs.length;
    const progressPercent = totalCount > 0 ? ((sentCount + failedCount) / totalCount) * 100 : 0;

    return (
        <div className={styles.container}>
            <Button 
                className={styles.backButton} 
                icon={<ArrowLeft20Regular />} 
                onClick={handleBack}
                appearance="subtle"
            >
                Back to Send Nudge
            </Button>

            <div className={styles.header}>
                <h1>{batch.batchName}</h1>
                <Text>Batch Progress and Results</Text>
            </div>

            {error && <div className={styles.error}>{error}</div>}

            <Card className={styles.card}>
                <CardHeader header={<Text weight="semibold">Batch Information</Text>} />
                <div className={styles.batchInfo}>
                    <Text>
                        <strong>Batch ID:</strong> {batch.id}
                    </Text>
                    <Text>
                        <strong>Template:</strong> {template?.templateName || batch.templateId}
                    </Text>
                    <Text>
                        <strong>Sender:</strong> {batch.senderUpn}
                    </Text>
                    <Text>
                        <strong>Created:</strong> {new Date(batch.createdDate).toLocaleString()}
                    </Text>
                </div>
            </Card>

            <Card className={styles.card}>
                <CardHeader header={<Text weight="semibold">Progress Summary</Text>} />
                
                <div className={styles.summaryGrid}>
                    <div className={styles.summaryItem}>
                        <Text size={300}>Total</Text>
                        <div className={styles.summaryNumber}>{totalCount}</div>
                    </div>
                    <div className={styles.summaryItem}>
                        <Text size={300}>Sent</Text>
                        <div className={styles.summaryNumber} style={{ color: tokens.colorPaletteGreenForeground1 }}>
                            {sentCount}
                        </div>
                    </div>
                    <div className={styles.summaryItem}>
                        <Text size={300}>Failed</Text>
                        <div className={styles.summaryNumber} style={{ color: tokens.colorPaletteRedForeground1 }}>
                            {failedCount}
                        </div>
                    </div>
                    <div className={styles.summaryItem}>
                        <Text size={300}>Pending</Text>
                        <div className={styles.summaryNumber} style={{ color: tokens.colorPaletteYellowForeground1 }}>
                            {pendingCount}
                        </div>
                    </div>
                </div>

                <div className={styles.progressSection}>
                    <Text size={200}>Overall Progress</Text>
                    <ProgressBar value={progressPercent / 100} />
                    <Text size={200}>{Math.round(progressPercent)}% Complete</Text>
                </div>

                {autoRefresh && pendingCount > 0 && (
                    <div style={{ marginTop: tokens.spacingVerticalM }}>
                        <Text size={200}>
                            <Clock20Regular /> Auto-refreshing every 5 seconds...
                        </Text>
                    </div>
                )}
            </Card>

            <Card className={styles.card}>
                <CardHeader header={<Text weight="semibold">Message Details ({logs.length})</Text>} />
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHeaderCell>Recipient</TableHeaderCell>
                            <TableHeaderCell>Status</TableHeaderCell>
                            <TableHeaderCell>Sent Date</TableHeaderCell>
                            <TableHeaderCell>Details</TableHeaderCell>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {logs.map((log) => (
                            <TableRow key={log.id}>
                                <TableCell>
                                    <TableCellLayout>{log.recipientUpn || 'N/A'}</TableCellLayout>
                                </TableCell>
                                <TableCell>
                                    <TableCellLayout>{getStatusBadge(log.status)}</TableCellLayout>
                                </TableCell>
                                <TableCell>
                                    <TableCellLayout>
                                        {new Date(log.sentDate).toLocaleString()}
                                    </TableCellLayout>
                                </TableCell>
                                <TableCell>
                                    <TableCellLayout>
                                        {log.lastError && (
                                            <div className={styles.errorDetails}>
                                                {log.lastError}
                                            </div>
                                        )}
                                    </TableCellLayout>
                                </TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </Card>
        </div>
    );
};
