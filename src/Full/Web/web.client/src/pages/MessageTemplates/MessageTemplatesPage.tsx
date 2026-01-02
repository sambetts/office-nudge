import React, { useState, useEffect } from 'react';
import {
    Button,
    Card,
    CardHeader,
    Table,
    TableBody,
    TableCell,
    TableRow,
    TableHeader,
    TableHeaderCell,
    TableCellLayout,
    Dialog,
    DialogTrigger,
    DialogSurface,
    DialogTitle,
    DialogBody,
    DialogActions,
    DialogContent,
    Input,
    Label,
    Textarea,
    Spinner,
    Text,
    makeStyles,
    tokens
} from '@fluentui/react-components';
import { Add20Regular, Edit20Regular, Delete20Regular, Eye20Regular } from '@fluentui/react-icons';
import { BaseAxiosApiLoader } from '../../api/AxiosApiLoader';
import {
    getAllTemplates,
    createTemplate,
    updateTemplate,
    deleteTemplate,
    getTemplateJson
} from '../../api/ApiCalls';
import { MessageTemplateDto, CreateTemplateRequest, UpdateTemplateRequest } from '../../apimodels/Models';

const useStyles = makeStyles({
    container: {
        padding: tokens.spacingVerticalXXL,
    },
    header: {
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: tokens.spacingVerticalL,
    },
    card: {
        marginBottom: tokens.spacingVerticalL,
    },
    formField: {
        marginBottom: tokens.spacingVerticalM,
    },
    jsonTextarea: {
        minHeight: '200px',
        fontFamily: 'monospace',
    },
    actionButtons: {
        display: 'flex',
        gap: tokens.spacingHorizontalS,
    },
    error: {
        color: tokens.colorPaletteRedForeground1,
        marginTop: tokens.spacingVerticalM,
    }
});

interface MessageTemplatesPageProps {
    loader?: BaseAxiosApiLoader;
}

export const MessageTemplatesPage: React.FC<MessageTemplatesPageProps> = ({ loader }) => {
    const styles = useStyles();
    const [templates, setTemplates] = useState<MessageTemplateDto[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // Dialog states
    const [createDialogOpen, setCreateDialogOpen] = useState(false);
    const [editDialogOpen, setEditDialogOpen] = useState(false);
    const [viewDialogOpen, setViewDialogOpen] = useState(false);
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

    // Form states
    const [templateName, setTemplateName] = useState('');
    const [jsonPayload, setJsonPayload] = useState('');
    const [selectedTemplate, setSelectedTemplate] = useState<MessageTemplateDto | null>(null);
    const [viewJson, setViewJson] = useState('');

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

    const handleCreate = async () => {
        if (!loader) return;

        try {
            const request: CreateTemplateRequest = {
                templateName,
                jsonPayload
            };
            await createTemplate(loader, request);
            setCreateDialogOpen(false);
            setTemplateName('');
            setJsonPayload('');
            await loadTemplates();
        } catch (err: any) {
            setError(err.message || 'Failed to create template');
            console.error('Error creating template:', err);
        }
    };

    const handleEdit = async () => {
        if (!loader || !selectedTemplate) return;

        try {
            const request: UpdateTemplateRequest = {
                templateName,
                jsonPayload
            };
            await updateTemplate(loader, selectedTemplate.id, request);
            setEditDialogOpen(false);
            setSelectedTemplate(null);
            setTemplateName('');
            setJsonPayload('');
            await loadTemplates();
        } catch (err: any) {
            setError(err.message || 'Failed to update template');
            console.error('Error updating template:', err);
        }
    };

    const handleDelete = async () => {
        if (!loader || !selectedTemplate) return;

        try {
            await deleteTemplate(loader, selectedTemplate.id);
            setDeleteDialogOpen(false);
            setSelectedTemplate(null);
            await loadTemplates();
        } catch (err: any) {
            setError(err.message || 'Failed to delete template');
            console.error('Error deleting template:', err);
        }
    };

    const handleView = async (template: MessageTemplateDto) => {
        if (!loader) return;

        try {
            const response = await getTemplateJson(loader, template.id);
            setViewJson(response.json);
            setSelectedTemplate(template);
            setViewDialogOpen(true);
        } catch (err: any) {
            setError(err.message || 'Failed to load template JSON');
            console.error('Error loading template JSON:', err);
        }
    };

    const openEditDialog = async (template: MessageTemplateDto) => {
        if (!loader) return;

        try {
            const response = await getTemplateJson(loader, template.id);
            setSelectedTemplate(template);
            setTemplateName(template.templateName);
            setJsonPayload(response.json);
            setEditDialogOpen(true);
        } catch (err: any) {
            setError(err.message || 'Failed to load template for editing');
            console.error('Error loading template:', err);
        }
    };

    const openDeleteDialog = (template: MessageTemplateDto) => {
        setSelectedTemplate(template);
        setDeleteDialogOpen(true);
    };

    if (loading) {
        return <Spinner label="Loading templates..." />;
    }

    return (
        <div className={styles.container}>
            <div className={styles.header}>
                <div>
                    <h1>Message Templates</h1>
                    <Text>Manage Teams adaptive card message templates</Text>
                </div>
                <Dialog open={createDialogOpen} onOpenChange={(_, data) => setCreateDialogOpen(data.open)}>
                    <DialogTrigger disableButtonEnhancement>
                        <Button appearance="primary" icon={<Add20Regular />}>
                            New Template
                        </Button>
                    </DialogTrigger>
                    <DialogSurface>
                        <DialogBody>
                            <DialogTitle>Create New Template</DialogTitle>
                            <DialogContent>
                                <div className={styles.formField}>
                                    <Label htmlFor="templateName">Template Name</Label>
                                    <Input
                                        id="templateName"
                                        value={templateName}
                                        onChange={(_, data) => setTemplateName(data.value)}
                                    />
                                </div>
                                <div className={styles.formField}>
                                    <Label htmlFor="jsonPayload">JSON Payload</Label>
                                    <Textarea
                                        id="jsonPayload"
                                        className={styles.jsonTextarea}
                                        value={jsonPayload}
                                        onChange={(_, data) => setJsonPayload(data.value)}
                                    />
                                </div>
                            </DialogContent>
                            <DialogActions>
                                <DialogTrigger disableButtonEnhancement>
                                    <Button appearance="secondary">Cancel</Button>
                                </DialogTrigger>
                                <Button appearance="primary" onClick={handleCreate}>Create</Button>
                            </DialogActions>
                        </DialogBody>
                    </DialogSurface>
                </Dialog>
            </div>

            {error && <div className={styles.error}>{error}</div>}

            <Card className={styles.card}>
                <CardHeader header={<Text weight="semibold">Templates</Text>} />
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHeaderCell>Name</TableHeaderCell>
                            <TableHeaderCell>Created By</TableHeaderCell>
                            <TableHeaderCell>Created Date</TableHeaderCell>
                            <TableHeaderCell>Actions</TableHeaderCell>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {templates.map((template) => (
                            <TableRow key={template.id}>
                                <TableCell>
                                    <TableCellLayout>{template.templateName}</TableCellLayout>
                                </TableCell>
                                <TableCell>
                                    <TableCellLayout>{template.createdByUpn}</TableCellLayout>
                                </TableCell>
                                <TableCell>
                                    <TableCellLayout>
                                        {new Date(template.createdDate).toLocaleDateString()}
                                    </TableCellLayout>
                                </TableCell>
                                <TableCell>
                                    <TableCellLayout>
                                        <div className={styles.actionButtons}>
                                            <Button
                                                size="small"
                                                icon={<Eye20Regular />}
                                                onClick={() => handleView(template)}
                                            />
                                            <Button
                                                size="small"
                                                icon={<Edit20Regular />}
                                                onClick={() => openEditDialog(template)}
                                            />
                                            <Button
                                                size="small"
                                                icon={<Delete20Regular />}
                                                onClick={() => openDeleteDialog(template)}
                                            />
                                        </div>
                                    </TableCellLayout>
                                </TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </Card>

            {/* Edit Dialog */}
            <Dialog open={editDialogOpen} onOpenChange={(_, data) => setEditDialogOpen(data.open)}>
                <DialogSurface>
                    <DialogBody>
                        <DialogTitle>Edit Template</DialogTitle>
                        <DialogContent>
                            <div className={styles.formField}>
                                <Label htmlFor="editTemplateName">Template Name</Label>
                                <Input
                                    id="editTemplateName"
                                    value={templateName}
                                    onChange={(_, data) => setTemplateName(data.value)}
                                />
                            </div>
                            <div className={styles.formField}>
                                <Label htmlFor="editJsonPayload">JSON Payload</Label>
                                <Textarea
                                    id="editJsonPayload"
                                    className={styles.jsonTextarea}
                                    value={jsonPayload}
                                    onChange={(_, data) => setJsonPayload(data.value)}
                                />
                            </div>
                        </DialogContent>
                        <DialogActions>
                            <Button appearance="secondary" onClick={() => setEditDialogOpen(false)}>
                                Cancel
                            </Button>
                            <Button appearance="primary" onClick={handleEdit}>Save</Button>
                        </DialogActions>
                    </DialogBody>
                </DialogSurface>
            </Dialog>

            {/* View Dialog */}
            <Dialog open={viewDialogOpen} onOpenChange={(_, data) => setViewDialogOpen(data.open)}>
                <DialogSurface>
                    <DialogBody>
                        <DialogTitle>{selectedTemplate?.templateName}</DialogTitle>
                        <DialogContent>
                            <pre style={{ whiteSpace: 'pre-wrap', wordWrap: 'break-word' }}>
                                {viewJson}
                            </pre>
                        </DialogContent>
                        <DialogActions>
                            <Button appearance="secondary" onClick={() => setViewDialogOpen(false)}>
                                Close
                            </Button>
                        </DialogActions>
                    </DialogBody>
                </DialogSurface>
            </Dialog>

            {/* Delete Confirmation Dialog */}
            <Dialog open={deleteDialogOpen} onOpenChange={(_, data) => setDeleteDialogOpen(data.open)}>
                <DialogSurface>
                    <DialogBody>
                        <DialogTitle>Confirm Delete</DialogTitle>
                        <DialogContent>
                            <Text>
                                Are you sure you want to delete the template "{selectedTemplate?.templateName}"?
                                This action cannot be undone.
                            </Text>
                        </DialogContent>
                        <DialogActions>
                            <Button appearance="secondary" onClick={() => setDeleteDialogOpen(false)}>
                                Cancel
                            </Button>
                            <Button appearance="primary" onClick={handleDelete}>Delete</Button>
                        </DialogActions>
                    </DialogBody>
                </DialogSurface>
            </Dialog>
        </div>
    );
};
