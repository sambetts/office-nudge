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
} from '@fluentui/react-components';
import { Eye20Regular } from '@fluentui/react-icons';
import { useHistory } from 'react-router-dom';
import { BaseAxiosApiLoader } from '../../api/AxiosApiLoader';
import { getAllBatches, getAllTemplates } from '../../api/ApiCalls';
import { MessageBatchDto, MessageTemplateDto } from '../../apimodels/Models';

const useStyles = makeStyles({
    container: {
        padding: tokens.spacingVerticalXXL,
    },
    header: {
        marginBottom: tokens.spacingVerticalL,
    },
    card: {
        marginBottom: tokens.spacingVerticalL,
    },
    error: {
        color: tokens.colorPaletteRedForeground1,
        marginTop: tokens.spacingVerticalM,
    },
    actionButtons: {
        display: 'flex',
        gap: tokens.spacingHorizontalS,
    },
    emptyState: {
        padding: tokens.spacingVerticalXXL,
        textAlign: 'center',
    },
});

interface BatchHistoryPageProps {
    loader?: BaseAxiosApiLoader;
}

export const BatchHistoryPage: React.FC<BatchHistoryPageProps> = ({ loader }) => {
    const styles = useStyles();
    const history = useHistory();

    const [batches, setBatches] = useState<MessageBatchDto[]>([]);
    const [templates, setTemplates] = useState<Map<string, MessageTemplateDto>>(new Map());
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        loadData();
    }, [loader]);

    const loadData = async () => {
        if (!loader) return;

        try {
            setLoading(true);
            setError(null);

            const [batchesData, templatesData] = await Promise.all([
                getAllBatches(loader),
                getAllTemplates(loader),
            ]);

            // Sort batches by created date (newest first)
            batchesData.sort((a, b) => 
                new Date(b.createdDate).getTime() - new Date(a.createdDate).getTime()
            );

            setBatches(batchesData);

            // Create a map of templates for quick lookup
            const templateMap = new Map<string, MessageTemplateDto>();
            templatesData.forEach(t => templateMap.set(t.id, t));
            setTemplates(templateMap);
        } catch (err: any) {
            setError(err.message || 'Failed to load batch history');
            console.error('Error loading batch history:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleViewBatch = (batchId: string) => {
        history.push(`/batch/${batchId}`);
    };

    if (loading) {
        return (
            <div className={styles.container}>
                <Spinner label="Loading batch history..." />
            </div>
        );
    }

    return (
        <div className={styles.container}>
            <div className={styles.header}>
                <h1>Batch History</h1>
                <Text>View all message batches and their progress</Text>
            </div>

            {error && <div className={styles.error}>{error}</div>}

            <Card className={styles.card}>
                <CardHeader header={<Text weight="semibold">All Batches ({batches.length})</Text>} />
                
                {batches.length === 0 ? (
                    <div className={styles.emptyState}>
                        <Text size={400}>No batches found</Text>
                        <br />
                        <Text>Create your first batch from the Send Nudge page</Text>
                    </div>
                ) : (
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHeaderCell>Batch Name</TableHeaderCell>
                                <TableHeaderCell>Template</TableHeaderCell>
                                <TableHeaderCell>Sender</TableHeaderCell>
                                <TableHeaderCell>Created Date</TableHeaderCell>
                                <TableHeaderCell>Actions</TableHeaderCell>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {batches.map((batch) => {
                                const template = templates.get(batch.templateId);
                                return (
                                    <TableRow key={batch.id}>
                                        <TableCell>
                                            <TableCellLayout>{batch.batchName}</TableCellLayout>
                                        </TableCell>
                                        <TableCell>
                                            <TableCellLayout>
                                                {template?.templateName || batch.templateId}
                                            </TableCellLayout>
                                        </TableCell>
                                        <TableCell>
                                            <TableCellLayout>{batch.senderUpn}</TableCellLayout>
                                        </TableCell>
                                        <TableCell>
                                            <TableCellLayout>
                                                {new Date(batch.createdDate).toLocaleString()}
                                            </TableCellLayout>
                                        </TableCell>
                                        <TableCell>
                                            <TableCellLayout>
                                                <div className={styles.actionButtons}>
                                                    <Button
                                                        size="small"
                                                        icon={<Eye20Regular />}
                                                        onClick={() => handleViewBatch(batch.id)}
                                                    >
                                                        View Progress
                                                    </Button>
                                                </div>
                                            </TableCellLayout>
                                        </TableCell>
                                    </TableRow>
                                );
                            })}
                        </TableBody>
                    </Table>
                )}
            </Card>
        </div>
    );
};
