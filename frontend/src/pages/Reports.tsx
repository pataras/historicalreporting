import {
  Box,
  Card,
  CardContent,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  CircularProgress,
  Alert,
} from '@mui/material';
import { useReports } from '../hooks/useReports';
import type { ReportStatus } from '../types';

function getStatusColor(status: ReportStatus): 'default' | 'primary' | 'success' | 'warning' {
  switch (status) {
    case 'Active':
      return 'success';
    case 'Draft':
      return 'warning';
    case 'Archived':
      return 'default';
    default:
      return 'default';
  }
}

export function Reports() {
  const { data: reports, isLoading, isError, error } = useReports();

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (isError) {
    return (
      <Alert severity="error" sx={{ mt: 2 }}>
        Failed to load reports: {error?.message || 'Unknown error'}
      </Alert>
    );
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Reports
      </Typography>
      <Typography variant="body1" color="textSecondary" paragraph>
        Manage your historical reports.
      </Typography>

      <Card>
        <CardContent>
          {!reports || reports.length === 0 ? (
            <Typography variant="body2" color="textSecondary" sx={{ textAlign: 'center', py: 4 }}>
              No reports found. Create your first report to get started.
            </Typography>
          ) : (
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Name</TableCell>
                    <TableCell>Description</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Created</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {reports.map((report) => (
                    <TableRow key={report.id} hover>
                      <TableCell>{report.name}</TableCell>
                      <TableCell>{report.description || '-'}</TableCell>
                      <TableCell>
                        <Chip
                          label={report.status}
                          color={getStatusColor(report.status)}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        {new Date(report.createdAt).toLocaleDateString()}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
