import React, { useState, useEffect } from 'react';
import {
    Button,
    Card,
    CardHeader,
    Label,
    Spinner,
    Text,
    Textarea,
    makeStyles,
    tokens,
    Badge,
    MessageBar,
    MessageBarBody,
    MessageBarTitle,
} from '@fluentui/react-components';
import { 
    Settings20Regular, 
    ArrowReset20Regular, 
    Save20Regular,
    Sparkle20Regular,
    Info20Regular,
} from '@fluentui/react-icons';
import { BaseAxiosApiLoader } from '../../api/AxiosApiLoader';
import { getSettings, updateSettings, resetSettingsToDefaults, getCopilotConnectedStatus } from '../../api/ApiCalls';
import { AppSettingsDto, CopilotConnectedStatusDto } from '../../apimodels/Models';

const useStyles = makeStyles({
    container: {
        padding: tokens.spacingVerticalXXL,
        maxWidth: '900px',
    },
    header: {
        marginBottom: tokens.spacingVerticalL,
        display: 'flex',
        alignItems: 'center',
        gap: tokens.spacingHorizontalM,
    },
    card: {
        marginBottom: tokens.spacingVerticalL,
    },
    formField: {
        marginBottom: tokens.spacingVerticalM,
    },
    textarea: {
        width: '100%',
        minHeight: '300px',
        maxHeight: 'none !important',
        fontFamily: 'monospace',

        '& textarea': {
            maxHeight: 'none !important',
            minHeight: '300px',
        },
    },
    buttonContainer: {
        display: 'flex',
        gap: tokens.spacingHorizontalM,
        marginTop: tokens.spacingVerticalL,
    },
    error: {
        color: tokens.colorPaletteRedForeground1,
        marginBottom: tokens.spacingVerticalM,
    },
    success: {
        color: tokens.colorPaletteGreenForeground1,
        marginBottom: tokens.spacingVerticalM,
    },
    infoCard: {
        padding: tokens.spacingVerticalM,
        backgroundColor: tokens.colorNeutralBackground2,
        borderRadius: tokens.borderRadiusMedium,
        marginBottom: tokens.spacingVerticalM,
    },
    metaInfo: {
        marginTop: tokens.spacingVerticalM,
        padding: tokens.spacingVerticalS,
        backgroundColor: tokens.colorNeutralBackground3,
        borderRadius: tokens.borderRadiusSmall,
    },
    copilotBadge: {
        display: 'flex',
        alignItems: 'center',
        gap: tokens.spacingHorizontalXS,
        marginBottom: tokens.spacingVerticalM,
    },
});

interface SettingsPageProps {
    loader?: BaseAxiosApiLoader;
}

export const SettingsPage: React.FC<SettingsPageProps> = ({ loader }) => {
    const styles = useStyles();

    const [settings, setSettings] = useState<AppSettingsDto | null>(null);
    const [copilotStatus, setCopilotStatus] = useState<CopilotConnectedStatusDto | null>(null);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);

    // Form state
    const [followUpChatPrompt, setFollowUpChatPrompt] = useState<string>('');
    const [hasChanges, setHasChanges] = useState(false);

    useEffect(() => {
        loadData();
    }, [loader]);

    const loadData = async () => {
        if (!loader) return;

        try {
            setLoading(true);
            setError(null);

            const [settingsData, copilotData] = await Promise.all([
                getSettings(loader),
                getCopilotConnectedStatus(loader),
            ]);

            setSettings(settingsData);
            setCopilotStatus(copilotData);
            
            // Initialize form with current value or default prompt if no custom value
            setFollowUpChatPrompt(settingsData.followUpChatSystemPrompt || settingsData.defaultFollowUpChatSystemPrompt);
            setHasChanges(false);
        } catch (err: any) {
            setError(err.message || 'Failed to load settings');
            console.error('Error loading settings:', err);
        } finally {
            setLoading(false);
        }
    };

    const handlePromptChange = (value: string) => {
        setFollowUpChatPrompt(value);
        // Check if there are changes from the saved value (or default if none saved)
        const savedValue = settings?.followUpChatSystemPrompt || settings?.defaultFollowUpChatSystemPrompt || '';
        setHasChanges(value !== savedValue);
    };

    const handleSave = async () => {
        if (!loader || !settings) return;

        try {
            setSaving(true);
            setError(null);
            setSuccess(null);

            // If the prompt matches the default, save as null (to use default)
            const promptToSave = followUpChatPrompt.trim() === settings.defaultFollowUpChatSystemPrompt 
                ? null 
                : (followUpChatPrompt.trim() || null);

            const updatedSettings = await updateSettings(loader, {
                followUpChatSystemPrompt: promptToSave,
            });

            setSettings(updatedSettings);
            setFollowUpChatPrompt(updatedSettings.followUpChatSystemPrompt || updatedSettings.defaultFollowUpChatSystemPrompt);
            setHasChanges(false);
            setSuccess('Settings saved successfully!');
        } catch (err: any) {
            setError(err.message || 'Failed to save settings');
            console.error('Error saving settings:', err);
        } finally {
            setSaving(false);
        }
    };

    const handleResetToDefaults = async () => {
        if (!loader || !settings) return;

        if (!window.confirm('Are you sure you want to reset the system prompt to the default? This will discard any custom prompt.')) {
            return;
        }

        try {
            setSaving(true);
            setError(null);
            setSuccess(null);

            const updatedSettings = await resetSettingsToDefaults(loader);

            setSettings(updatedSettings);
            setFollowUpChatPrompt(updatedSettings.defaultFollowUpChatSystemPrompt);
            setHasChanges(false);
            setSuccess('Settings reset to defaults successfully!');
        } catch (err: any) {
            setError(err.message || 'Failed to reset settings');
            console.error('Error resetting settings:', err);
        } finally {
            setSaving(false);
        }
    };

    if (loading) {
        return (
            <div className={styles.container}>
                <Spinner label="Loading settings..." />
            </div>
        );
    }

    const isUsingDefault = !settings?.followUpChatSystemPrompt;

    return (
        <div className={styles.container}>
            <div className={styles.header}>
                <Settings20Regular />
                <h1>Settings</h1>
            </div>

            {!copilotStatus?.isEnabled && (
                <MessageBar intent="warning" style={{ marginBottom: tokens.spacingVerticalM }}>
                    <MessageBarBody>
                        <MessageBarTitle>AI Features Not Configured</MessageBarTitle>
                        These settings apply to AI-powered features which require AI Foundry configuration.
                        Configure AI Foundry in your application settings to enable these features.
                    </MessageBarBody>
                </MessageBar>
            )}

            {copilotStatus?.isEnabled && (
                <div className={styles.copilotBadge}>
                    <Sparkle20Regular />
                    <Badge appearance="filled" color="brand">Copilot Connected</Badge>
                    <Text size={200}>AI features are enabled</Text>
                </div>
            )}

            {error && <div className={styles.error}>{error}</div>}
            {success && <div className={styles.success}>{success}</div>}

            <Card className={styles.card}>
                <CardHeader 
                    header={<Text weight="semibold">Follow-up Chat System Prompt</Text>}
                    description={
                        <Text size={200}>
                            Configure the system prompt used when users reply to nudge messages.
                            This prompt instructs the AI on how to respond to user questions and feedback.
                        </Text>
                    }
                />

                <div className={styles.infoCard}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: tokens.spacingHorizontalS }}>
                        <Info20Regular />
                        <Text size={200}>
                            {isUsingDefault 
                                ? 'Currently using the default system prompt. Enter a custom prompt below to override it.'
                                : 'Using a custom system prompt. Clear the text to revert to the default.'}
                        </Text>
                    </div>
                </div>

                <div className={styles.formField}>
                    <Label htmlFor="systemPrompt">
                        System Prompt {isUsingDefault && '(Using Default)'}
                    </Label>
                    <Textarea
                        id="systemPrompt"
                        className={styles.textarea}
                        value={followUpChatPrompt}
                        onChange={(_, data) => handlePromptChange(data.value)}
                        placeholder="Enter a system prompt..."
                        resize="vertical"
                        style={{ maxHeight: 'none' }}
                    />
                </div>

                {settings?.lastModifiedDate && (
                    <div className={styles.metaInfo}>
                        <Text size={200}>
                            Last modified: {new Date(settings.lastModifiedDate).toLocaleString()}
                            {settings.lastModifiedByUpn && ` by ${settings.lastModifiedByUpn}`}
                        </Text>
                    </div>
                )}

                <div className={styles.buttonContainer}>
                    <Button
                        appearance="primary"
                        icon={<Save20Regular />}
                        onClick={handleSave}
                        disabled={!hasChanges || saving}
                    >
                        {saving ? 'Saving...' : 'Save Changes'}
                    </Button>
                    <Button
                        appearance="secondary"
                        icon={<ArrowReset20Regular />}
                        onClick={handleResetToDefaults}
                        disabled={saving || isUsingDefault}
                    >
                        Reset to Default
                    </Button>
                </div>
            </Card>
        </div>
    );
};
