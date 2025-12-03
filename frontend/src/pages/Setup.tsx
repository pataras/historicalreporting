import { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Typography,
  LinearProgress,
  Alert,
  Stack,
  Divider,
} from '@mui/material';
import {
  PlayArrow as StartIcon,
  Stop as StopIcon,
  Delete as ClearIcon,
} from '@mui/icons-material';
import * as signalR from '@microsoft/signalr';

interface SeedProgress {
  stage: string;
  message: string;
  currentItem: number;
  totalItems: number;
  percentComplete: number;
  isComplete: boolean;
  hasError: boolean;
  errorMessage?: string;
}

export function Setup() {
  const [connectionId, setConnectionId] = useState<string | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [isSeeding, setIsSeeding] = useState(false);
  const [progress, setProgress] = useState<SeedProgress | null>(null);
  const [logs, setLogs] = useState<string[]>([]);

  const addLog = useCallback((message: string) => {
    const timestamp = new Date().toLocaleTimeString();
    setLogs(prev => [...prev, `[${timestamp}] ${message}`]);
  }, []);

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/seed-progress')
      .withAutomaticReconnect()
      .build();

    newConnection.on('Connected', (connId: string) => {
      setConnectionId(connId);
      addLog(`Connected to SignalR hub with ID: ${connId}`);
    });

    newConnection.on('SeedProgress', (data: SeedProgress) => {
      setProgress(data);
      addLog(`${data.stage}: ${data.message}`);

      if (data.isComplete) {
        setIsSeeding(false);
      }
    });

    newConnection.onclose(() => {
      setIsConnected(false);
      addLog('Disconnected from SignalR hub');
    });

    newConnection.onreconnecting(() => {
      addLog('Reconnecting to SignalR hub...');
    });

    newConnection.onreconnected(() => {
      setIsConnected(true);
      addLog('Reconnected to SignalR hub');
    });

    newConnection
      .start()
      .then(() => {
        setIsConnected(true);
        addLog('SignalR connection established');
      })
      .catch(err => {
        addLog(`SignalR connection failed: ${err.message}`);
      });

    return () => {
      newConnection.stop();
    };
  }, [addLog]);

  useEffect(() => {
    fetch('/api/setup/status')
      .then(res => res.json())
      .then(data => {
        setIsSeeding(data.isSeeding);
        if (data.isSeeding) {
          addLog('Seeding already in progress');
        }
      })
      .catch(err => addLog(`Failed to check status: ${err.message}`));
  }, [addLog]);

  const handleStartSeed = async () => {
    if (!connectionId) {
      addLog('Error: Not connected to SignalR hub');
      return;
    }

    setIsSeeding(true);
    setProgress(null);
    setLogs([]);
    addLog('Starting data seed...');

    try {
      const response = await fetch(`/api/setup/seed?connectionId=${connectionId}`, {
        method: 'POST',
      });

      if (!response.ok) {
        const error = await response.json();
        addLog(`Error: ${error.message}`);
        setIsSeeding(false);
      }
    } catch (err) {
      addLog(`Request failed: ${err instanceof Error ? err.message : 'Unknown error'}`);
      setIsSeeding(false);
    }
  };

  const handleCancel = async () => {
    addLog('Requesting cancellation...');
    try {
      await fetch('/api/setup/cancel', { method: 'POST' });
      addLog('Cancellation requested');
    } catch (err) {
      addLog(`Cancel failed: ${err instanceof Error ? err.message : 'Unknown error'}`);
    }
  };

  const handleClear = async () => {
    if (!connectionId) {
      addLog('Error: Not connected to SignalR hub');
      return;
    }

    setIsSeeding(true);
    setProgress(null);
    addLog('Clearing data...');

    try {
      const response = await fetch(`/api/setup/clear?connectionId=${connectionId}`, {
        method: 'POST',
      });

      if (!response.ok) {
        const error = await response.json();
        addLog(`Error: ${error.message}`);
      }
    } catch (err) {
      addLog(`Request failed: ${err instanceof Error ? err.message : 'Unknown error'}`);
    } finally {
      setIsSeeding(false);
    }
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Data Setup
      </Typography>
      <Typography variant="body1" color="text.secondary" paragraph>
        Generate test data for the historical reporting system. This will create organisations,
        departments, managers, users, and audit records.
      </Typography>

      <Stack direction="row" spacing={2} sx={{ mb: 3 }}>
        <Button
          variant="contained"
          color="primary"
          startIcon={<StartIcon />}
          onClick={handleStartSeed}
          disabled={!isConnected || isSeeding}
        >
          Start Seeding
        </Button>
        <Button
          variant="outlined"
          color="warning"
          startIcon={<StopIcon />}
          onClick={handleCancel}
          disabled={!isSeeding}
        >
          Cancel
        </Button>
        <Button
          variant="outlined"
          color="error"
          startIcon={<ClearIcon />}
          onClick={handleClear}
          disabled={!isConnected || isSeeding}
        >
          Clear All Data
        </Button>
      </Stack>

      {!isConnected && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Not connected to server. Waiting for connection...
        </Alert>
      )}

      {progress && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              {progress.stage}
            </Typography>
            <Typography variant="body2" color="text.secondary" gutterBottom>
              {progress.message}
            </Typography>
            <Box sx={{ display: 'flex', alignItems: 'center', mt: 2 }}>
              <Box sx={{ width: '100%', mr: 1 }}>
                <LinearProgress
                  variant="determinate"
                  value={progress.percentComplete}
                  color={progress.hasError ? 'error' : progress.isComplete ? 'success' : 'primary'}
                />
              </Box>
              <Box sx={{ minWidth: 50 }}>
                <Typography variant="body2" color="text.secondary">
                  {Math.round(progress.percentComplete)}%
                </Typography>
              </Box>
            </Box>
            {progress.totalItems > 0 && (
              <Typography variant="caption" color="text.secondary">
                {progress.currentItem.toLocaleString()} / {progress.totalItems.toLocaleString()} items
              </Typography>
            )}
            {progress.hasError && (
              <Alert severity="error" sx={{ mt: 2 }}>
                {progress.errorMessage}
              </Alert>
            )}
            {progress.isComplete && !progress.hasError && (
              <Alert severity="success" sx={{ mt: 2 }}>
                Operation completed successfully!
              </Alert>
            )}
          </CardContent>
        </Card>
      )}

      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Activity Log
          </Typography>
          <Divider sx={{ mb: 2 }} />
          <Box
            sx={{
              maxHeight: 300,
              overflow: 'auto',
              fontFamily: 'monospace',
              fontSize: '0.875rem',
              bgcolor: 'grey.100',
              p: 2,
              borderRadius: 1,
            }}
          >
            {logs.length === 0 ? (
              <Typography color="text.secondary">No activity yet</Typography>
            ) : (
              logs.map((log, index) => (
                <div key={index}>{log}</div>
              ))
            )}
          </Box>
        </CardContent>
      </Card>

      <Card sx={{ mt: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Data Generation Summary
          </Typography>
          <Divider sx={{ mb: 2 }} />
          <Stack spacing={1}>
            <Typography variant="body2">
              <strong>Organisations:</strong> 5 (7-letter acronyms)
            </Typography>
            <Typography variant="body2">
              <strong>Managers per org:</strong> 5 (1 global, 4 departmental)
            </Typography>
            <Typography variant="body2">
              <strong>Departments per org:</strong> 50 (35 as sub-departments with hierarchy)
            </Typography>
            <Typography variant="body2">
              <strong>Users per org:</strong> 5,000
            </Typography>
            <Typography variant="body2">
              <strong>Audit records per user:</strong> 3-10 (dates from 2022-2026)
            </Typography>
            <Typography variant="body2" sx={{ mt: 1 }}>
              <strong>Total estimated records:</strong> ~25,000 users, ~162,500 audit records
            </Typography>
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
