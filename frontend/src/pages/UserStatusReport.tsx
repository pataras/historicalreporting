import { useState, useEffect } from 'react';
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
  CircularProgress,
  Alert,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Stack,
} from '@mui/material';
import { useOrganisations } from '../hooks/useOrganisations';
import { useMonthlyUserStatusReport, useMonthlyUserStatusReportByManager } from '../hooks/useUserStatusReport';
import { useManagersByOrganisation } from '../hooks/useManagers';

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
];

export function UserStatusReport() {
  const [selectedOrgId, setSelectedOrgId] = useState<string>('');
  const [selectedManagerId, setSelectedManagerId] = useState<string>('');

  const {
    data: organisations,
    isLoading: isLoadingOrgs,
    isError: isOrgsError
  } = useOrganisations();

  const {
    data: managers,
    isLoading: isLoadingManagers,
    isError: isManagersError
  } = useManagersByOrganisation(selectedOrgId || null);

  // Reset manager selection when organisation changes
  useEffect(() => {
    setSelectedManagerId('');
  }, [selectedOrgId]);

  // Use manager-filtered report when manager is selected, otherwise use org-level report
  const {
    data: orgReport,
    isLoading: isLoadingOrgReport,
    isError: isOrgReportError,
    error: orgReportError
  } = useMonthlyUserStatusReport(selectedOrgId && !selectedManagerId ? selectedOrgId : null);

  const {
    data: managerReport,
    isLoading: isLoadingManagerReport,
    isError: isManagerReportError,
    error: managerReportError
  } = useMonthlyUserStatusReportByManager(
    selectedOrgId && selectedManagerId ? selectedOrgId : null,
    selectedManagerId || null
  );

  const report = selectedManagerId ? managerReport : orgReport;
  const isLoadingReport = selectedManagerId ? isLoadingManagerReport : isLoadingOrgReport;
  const isReportError = selectedManagerId ? isManagerReportError : isOrgReportError;
  const reportError = selectedManagerId ? managerReportError : orgReportError;

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Monthly User Status Report
      </Typography>
      <Typography variant="body1" color="textSecondary" paragraph>
        View the number of users and their status (Valid/Invalid) per month for a selected organisation and manager.
      </Typography>

      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Stack spacing={2}>
            <FormControl fullWidth>
              <InputLabel id="organisation-select-label">Organisation</InputLabel>
              <Select
                labelId="organisation-select-label"
                id="organisation-select"
                value={selectedOrgId}
                label="Organisation"
                onChange={(e) => setSelectedOrgId(e.target.value)}
                disabled={isLoadingOrgs}
              >
                {isLoadingOrgs && (
                  <MenuItem value="">Loading...</MenuItem>
                )}
                {organisations?.map((org) => (
                  <MenuItem key={org.id} value={org.id}>
                    {org.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            {selectedOrgId && (
              <FormControl fullWidth>
                <InputLabel id="manager-select-label">Manager</InputLabel>
                <Select
                  labelId="manager-select-label"
                  id="manager-select"
                  value={selectedManagerId}
                  label="Manager"
                  onChange={(e) => setSelectedManagerId(e.target.value)}
                  disabled={isLoadingManagers}
                >
                  <MenuItem value="">
                    <em>All (Organisation Level)</em>
                  </MenuItem>
                  {isLoadingManagers && (
                    <MenuItem value="" disabled>Loading managers...</MenuItem>
                  )}
                  {managers?.map((manager) => (
                    <MenuItem key={manager.id} value={manager.id}>
                      {manager.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}
          </Stack>

          {isOrgsError && (
            <Alert severity="error" sx={{ mt: 2 }}>
              Failed to load organisations
            </Alert>
          )}

          {isManagersError && (
            <Alert severity="error" sx={{ mt: 2 }}>
              Failed to load managers
            </Alert>
          )}
        </CardContent>
      </Card>

      {selectedOrgId && (
        <Card>
          <CardContent>
            {isLoadingReport && (
              <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                <CircularProgress />
              </Box>
            )}

            {isReportError && (
              <Alert severity="error">
                Failed to load report: {reportError?.message || 'Unknown error'}
              </Alert>
            )}

            {report && !isLoadingReport && (
              <>
                <Typography variant="h6" gutterBottom>
                  {report.organisationName}
                  {report.managerName && ` - ${report.managerName}`}
                  {' - Monthly User Status'}
                </Typography>

                {report.monthlyData.length === 0 ? (
                  <Typography variant="body2" color="textSecondary" sx={{ textAlign: 'center', py: 4 }}>
                    No audit records found for this selection.
                  </Typography>
                ) : (
                  <TableContainer>
                    <Table>
                      <TableHead>
                        <TableRow>
                          <TableCell>Year</TableCell>
                          <TableCell>Month</TableCell>
                          <TableCell align="right">Valid</TableCell>
                          <TableCell align="right">Invalid</TableCell>
                          <TableCell align="right">Total</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {report.monthlyData.map((row) => (
                          <TableRow key={`${row.year}-${row.month}`} hover>
                            <TableCell>{row.year}</TableCell>
                            <TableCell>{MONTH_NAMES[row.month - 1]}</TableCell>
                            <TableCell align="right" sx={{ color: 'success.main' }}>
                              {row.validCount.toLocaleString()}
                            </TableCell>
                            <TableCell align="right" sx={{ color: 'error.main' }}>
                              {row.invalidCount.toLocaleString()}
                            </TableCell>
                            <TableCell align="right">
                              {row.totalCount.toLocaleString()}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                )}
              </>
            )}
          </CardContent>
        </Card>
      )}

      {!selectedOrgId && (
        <Card>
          <CardContent>
            <Typography variant="body2" color="textSecondary" sx={{ textAlign: 'center', py: 4 }}>
              Select an organisation to view the monthly user status report.
            </Typography>
          </CardContent>
        </Card>
      )}
    </Box>
  );
}
