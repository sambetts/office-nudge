import React, { useState, useEffect, useRef } from 'react';
import {
    Button,
    Card,
    CardHeader,
    Input,
    Label,
    Textarea,
    Spinner,
    Text,
    makeStyles,
    tokens,
    Dropdown,
    Option,
    Table,
    TableBody,
    TableCell,
    TableRow,
    TableHeader,
    TableHeaderCell,
    TableCellLayout,
    Badge,
    Link,
} from '@fluentui/react-components';
import {
    DocumentAdd20Regular,
    Delete20Regular,
    Send20Regular,
    AddCircle20Regular,
    ArrowRight20Regular,
} from '@fluentui/react-icons';
import { useHistory } from 'react-router-dom';
import { BaseAxiosApiLoader } from '../../api/AxiosApiLoader';
import {
    getAllTemplates,
    parseFile,
    createBatchAndSend,
} from '../../api/ApiCalls';
import { MessageTemplateDto, CreateBatchAndSendRequest } from '../../apimodels/Models';

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
    formField: {
        marginBottom: tokens.spacingVerticalM,
    },
    fileUploadArea: {
        border: `2px dashed ${tokens.colorNeutralStroke1}`,
        borderRadius: tokens.borderRadiusMedium,
        padding: tokens.spacingVerticalXL,
        textAlign: 'center',
        cursor: 'pointer',
        backgroundColor: tokens.colorNeutralBackground3,
        ':hover': {
            backgroundColor: tokens.colorNeutralBackground3Hover,
        },
    },
    upnListContainer: {
        marginTop: tokens.spacingVerticalM,
        maxHeight: '300px',
        overflowY: 'auto',
        border: `1px solid ${tokens.colorNeutralStroke1}`,
        borderRadius: tokens.borderRadiusMedium,
        padding: tokens.spacingVerticalM,
    },
    upnItem: {
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: tokens.spacingVerticalS,
        borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    },
    addUpnContainer: {
        display: 'flex',
        gap: tokens.spacingHorizontalS,
        alignItems: 'flex-end',
    },
    buttonContainer: {
        display: 'flex',
        gap: tokens.spacingHorizontalM,
        marginTop: tokens.spacingVerticalL,
    },
    error: {
        color: tokens.colorPaletteRedForeground1,
        marginTop: tokens.spacingVerticalM,
    },
    success: {
        color: tokens.colorPaletteGreenForeground1,
        marginTop: tokens.spacingVerticalM,
    },
    successCard: {
        padding: tokens.spacingVerticalL,
        backgroundColor: tokens.colorPaletteGreenBackground2,
        borderRadius: tokens.borderRadiusMedium,
        marginTop: tokens.spacingVerticalM,
        marginBottom: tokens.spacingVerticalL,
    },
    hiddenInput: {
        display: 'none',
    },
    summaryCard: {
        padding: tokens.spacingVerticalM,
        backgroundColor: tokens.colorNeutralBackground2,
        borderRadius: tokens.borderRadiusMedium,
        marginTop: tokens.spacingVerticalM,
    },
});

interface SendNudgePageProps {
    loader?: BaseAxiosApiLoader;
}

export const SendNudgePage: React.FC<SendNudgePageProps> = ({ loader }) => {
    const styles = useStyles();
    const history = useHistory();
    const fileInputRef = useRef<HTMLInputElement>(null);

    const [templates, setTemplates] = useState<MessageTemplateDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);
    const [createdBatchId, setCreatedBatchId] = useState<string | null>(null);

    // Form states
    const [batchName, setBatchName] = useState('');
    const [selectedTemplateId, setSelectedTemplateId] = useState<string>('');
    const [recipientUpns, setRecipientUpns] = useState<string[]>([]);
    const [newUpn, setNewUpn] = useState('');
    const [uploadedFileName, setUploadedFileName] = useState<string>('');
    const [sending, setSending] = useState(false);

    useEffect(() => {
        loadTemplates();
    }, [loader]);

    const loadTemplates = async () => {
        if (!loader) return;

        try {
            setLoading(true);
            setError(null);
            const data = await getAllTemplates(loader);
            setTemplates(data);
        } catch (err: any) {
            setError(err.message || 'Failed to load templates');
            console.error('Error loading templates:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleFileUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (!file || !loader) return;

        try {
            setError(null);
            setLoading(true);
            const response = await parseFile(loader, file);
            setRecipientUpns(response.upns);
            setUploadedFileName(file.name);
            setSuccess(`Loaded ${response.upns.length} recipients from ${file.name}`);
            setCreatedBatchId(null);
        } catch (err: any) {
            setError(err.message || 'Failed to parse file');
            console.error('Error parsing file:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleAddUpn = () => {
        if (!newUpn.trim()) return;

        if (recipientUpns.includes(newUpn.trim())) {
            setError('This UPN is already in the list');
            return;
        }

        setRecipientUpns([...recipientUpns, newUpn.trim()]);
        setNewUpn('');
        setError(null);
    };

    const handleRemoveUpn = (upn: string) => {
        setRecipientUpns(recipientUpns.filter(u => u !== upn));
    };

    const handleFileUploadClick = () => {
        fileInputRef.current?.click();
    };

    const handleSendNudges = async () => {
        if (!loader) return;

        // Validation
        if (!batchName.trim()) {
            setError('Batch name is required');
            return;
        }

        if (!selectedTemplateId) {
            setError('Please select a template');
            return;
        }

        if (recipientUpns.length === 0) {
            setError('Please add at least one recipient');
            return;
        }

        try {
            setError(null);
            setSuccess(null);
            setCreatedBatchId(null);
            setSending(true);

            const request: CreateBatchAndSendRequest = {
                batchName: batchName.trim(),
                templateId: selectedTemplateId,
                recipientUpns: recipientUpns,
            };

            const response = await createBatchAndSend(loader, request);
            setSuccess(`Successfully created batch "${batchName}" with ${response.messageCount} messages!`);
            setCreatedBatchId(response.batch.id);
            
            // Reset form
            setBatchName('');
            setSelectedTemplateId('');
            setRecipientUpns([]);
            setUploadedFileName('');
        } catch (err: any) {
            setError(err.message || 'Failed to send nudges');
            console.error('Error sending nudges:', err);
        } finally {
            setSending(false);
        }
    };

    const handleViewBatchProgress = () => {
        if (createdBatchId) {
            history.push(`/batch/${createdBatchId}`);
        }
    };

    const handleKeyPress = (event: React.KeyboardEvent) => {
        if (event.key === 'Enter') {
            handleAddUpn();
        }
    };

    if (loading && templates.length === 0) {
        return <Spinner label="Loading templates..." />;
    }

    const selectedTemplate = templates.find(t => t.id === selectedTemplateId);

    return (
        <div className={styles.container}>
            <div className={styles.header}>
                <h1>Send Nudge</h1>
                <Text>Select a template and add recipients to send nudge messages</Text>
            </div>

            {error && <div className={styles.error}>{error}</div>}
            
            {success && createdBatchId && (
                <Card className={styles.successCard}>
                    <div className={styles.success}>
                        <Text weight="semibold" size={400}>{success}</Text>
                    </div>
                    <div style={{ marginTop: tokens.spacingVerticalM }}>
                        <Button
                            appearance="primary"
                            icon={<ArrowRight20Regular />}
                            onClick={handleViewBatchProgress}
                        >
                            View Batch Progress
                        </Button>
                    </div>
                </Card>
            )}
            
            {success && !createdBatchId && <div className={styles.success}>{success}</div>}

            <Card className={styles.card}>
                <CardHeader header={<Text weight="semibold">Batch Details</Text>} />
                
                <div className={styles.formField}>
                    <Label htmlFor="batchName" required>Batch Name</Label>
                    <Input
                        id="batchName"
                        placeholder="e.g., Q1 2024 Reminder"
                        value={batchName}
                        onChange={(_, data) => setBatchName(data.value)}
                    />
                </div>

                <div className={styles.formField}>
                    <Label htmlFor="template" required>Select Template</Label>
                    <Dropdown
                        id="template"
                        placeholder="Select a message template"
                        value={selectedTemplate?.templateName || ''}
                        onOptionSelect={(_, data) => setSelectedTemplateId(data.optionValue as string)}
                    >
                        {templates.map((template) => (
                            <Option key={template.id} value={template.id}>
                                {template.templateName}
                            </Option>
                        ))}
                    </Dropdown>
                </div>

                {selectedTemplate && (
                    <div className={styles.summaryCard}>
                        <Text size={200}>
                            <strong>Selected Template:</strong> {selectedTemplate.templateName}
                        </Text>
                        <br />
                        <Text size={200}>
                            <strong>Created by:</strong> {selectedTemplate.createdByUpn}
                        </Text>
                    </div>
                )}
            </Card>

            <Card className={styles.card}>
                <CardHeader header={<Text weight="semibold">Recipients ({recipientUpns.length})</Text>} />

                <div className={styles.formField}>
                    <Label>Upload File (CSV or Excel)</Label>
                    <input
                        ref={fileInputRef}
                        type="file"
                        accept=".csv,.xlsx,.xls,.txt"
                        onChange={handleFileUpload}
                        className={styles.hiddenInput}
                    />
                    <div className={styles.fileUploadArea} onClick={handleFileUploadClick}>
                        <DocumentAdd20Regular />
                        <Text>
                            {uploadedFileName
                                ? `Uploaded: ${uploadedFileName}`
                                : 'Click to upload a file with user UPNs (single column)'}
                        </Text>
                    </div>
                </div>

                <div className={styles.formField}>
                    <Label htmlFor="newUpn">Or Add UPN Manually</Label>
                    <div className={styles.addUpnContainer}>
                        <Input
                            id="newUpn"
                            placeholder="user@example.com"
                            value={newUpn}
                            onChange={(_, data) => setNewUpn(data.value)}
                            onKeyPress={handleKeyPress}
                            style={{ flex: 1 }}
                        />
                        <Button
                            icon={<AddCircle20Regular />}
                            onClick={handleAddUpn}
                            disabled={!newUpn.trim()}
                        >
                            Add
                        </Button>
                    </div>
                </div>

                {recipientUpns.length > 0 && (
                    <div className={styles.upnListContainer}>
                        {recipientUpns.map((upn, index) => (
                            <div key={index} className={styles.upnItem}>
                                <Text>{upn}</Text>
                                <Button
                                    size="small"
                                    appearance="subtle"
                                    icon={<Delete20Regular />}
                                    onClick={() => handleRemoveUpn(upn)}
                                />
                            </div>
                        ))}
                    </div>
                )}
            </Card>

            <div className={styles.buttonContainer}>
                <Button
                    appearance="primary"
                    icon={<Send20Regular />}
                    onClick={handleSendNudges}
                    disabled={sending || !batchName || !selectedTemplateId || recipientUpns.length === 0}
                >
                    {sending ? 'Sending...' : `Send to ${recipientUpns.length} Recipients`}
                </Button>
            </div>
        </div>
    );
};
