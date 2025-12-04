import { useState } from 'react';
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
} from '@mui/material';
import { useOrganisations } from '../hooks/useOrganisations';
import { useMonthlyUserStatusReport } from '../hooks/useUserStatusReport';

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
];

export function UserStatusReport() {
  const [selectedOrgId, setSelectedOrgId] = useState<string>('');

  const {
    data: organisations,
    isLoading: isLoadingOrgs,
    isError: isOrgsError
  } = useOrganisations();

  const {
    data: report,
    isLoading: isLoadingReport,
    isError: isReportError,
    error: reportError
  } = useMonthlyUserStatusReport(selectedOrgId || null);

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Monthly User Status Report
      </Typography>
      <Typography variant="body1" color="textSecondary" paragraph>
        View the number of users and their status (Valid/Invalid) per month for a selected organisation.
      </Typography>

      <Card sx={{ mb: 3 }}>
        <CardContent>
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

          {isOrgsError && (
            <Alert severity="error" sx={{ mt: 2 }}>
              Failed to load organisations
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
                  {report.organisationName} - Monthly User Status
                </Typography>

                {report.monthlyData.length === 0 ? (
                  <Typography variant="body2" color="textSecondary" sx={{ textAlign: 'center', py: 4 }}>
                    No audit records found for this organisation.
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
