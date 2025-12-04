import { useState, useCallback } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Grid,
  Alert,
  CircularProgress,
  Chip,
  Collapse,
  IconButton,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Divider,
  Tooltip,
} from '@mui/material';
import SendIcon from '@mui/icons-material/Send';
import HistoryIcon from '@mui/icons-material/History';
import CodeIcon from '@mui/icons-material/Code';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import DownloadIcon from '@mui/icons-material/Download';
import LightbulbIcon from '@mui/icons-material/Lightbulb';
import WarningIcon from '@mui/icons-material/Warning';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import ErrorIcon from '@mui/icons-material/Error';
import {
  useNlpQuery,
  useNlpQueryHistory,
  useNlpQuerySuggestions,
  useExportToCsv,
} from '../hooks/useNlpQuery';
import type { NlpQueryResponse } from '../types';

export function QueryAssistant() {
  const [query, setQuery] = useState('');
  const [showSql, setShowSql] = useState(false);
  const [showHistory, setShowHistory] = useState(false);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);

  const { processQueryAsync, isLoading, data: queryResult } = useNlpQuery();
  const { data: history } = useNlpQueryHistory();
  const { data: suggestions } = useNlpQuerySuggestions();
  const { mutate: exportToCsv, isPending: isExporting } = useExportToCsv();

  const handleSubmit = useCallback(async () => {
    if (!query.trim()) return;
    setPage(0);
    await processQueryAsync({ query: query.trim() });
  }, [query, processQueryAsync]);

  const handleKeyPress = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        handleSubmit();
      }
    },
    [handleSubmit]
  );

  const handleSuggestionClick = useCallback((suggestion: string) => {
    setQuery(suggestion);
  }, []);

  const handleHistoryClick = useCallback((historicalQuery: string) => {
    setQuery(historicalQuery);
    setShowHistory(false);
  }, []);

  const handleExport = useCallback(() => {
    if (query.trim()) {
      exportToCsv({ query: query.trim() });
    }
  }, [query, exportToCsv]);

  const handleChangePage = (_: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  return (
    <Box sx={{ p: 3 }}>
      <Grid container spacing={3}>
        {/* Header */}
        <Grid size={12}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Typography variant="h4" component="h1">
              Query Assistant
            </Typography>
            <Button
              variant="outlined"
              startIcon={<HistoryIcon />}
              onClick={() => setShowHistory(true)}
            >
              History
            </Button>
          </Box>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            Ask questions about your data in natural language
          </Typography>
        </Grid>

        {/* Query Input */}
        <Grid size={12}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', gap: 2 }}>
                <TextField
                  fullWidth
                  multiline
                  minRows={2}
                  maxRows={4}
                  placeholder="Ask a question about your data... (e.g., 'Show me the compliance rate by department')"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  onKeyDown={handleKeyPress}
                  disabled={isLoading}
                />
                <Button
                  variant="contained"
                  onClick={handleSubmit}
                  disabled={isLoading || !query.trim()}
                  sx={{ minWidth: 120 }}
                >
                  {isLoading ? (
                    <CircularProgress size={24} color="inherit" />
                  ) : (
                    <>
                      <SendIcon sx={{ mr: 1 }} />
                      Query
                    </>
                  )}
                </Button>
              </Box>

              {/* Suggestions */}
              {suggestions && suggestions.length > 0 && !queryResult && (
                <Box sx={{ mt: 2 }}>
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                    <LightbulbIcon sx={{ fontSize: 16, verticalAlign: 'middle', mr: 0.5 }} />
                    Try one of these:
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                    {suggestions.slice(0, 5).map((suggestion, index) => (
                      <Chip
                        key={index}
                        label={suggestion}
                        onClick={() => handleSuggestionClick(suggestion)}
                        variant="outlined"
                        size="small"
                        sx={{ cursor: 'pointer' }}
                      />
                    ))}
                  </Box>
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Results */}
        {queryResult && (
          <Grid size={12}>
            <ResultsDisplay
              result={queryResult}
              showSql={showSql}
              onToggleSql={() => setShowSql(!showSql)}
              onExport={handleExport}
              isExporting={isExporting}
              page={page}
              rowsPerPage={rowsPerPage}
              onPageChange={handleChangePage}
              onRowsPerPageChange={handleChangeRowsPerPage}
            />
          </Grid>
        )}
      </Grid>

      {/* History Drawer */}
      <Drawer anchor="right" open={showHistory} onClose={() => setShowHistory(false)}>
        <Box sx={{ width: 400, p: 2 }}>
          <Typography variant="h6" sx={{ mb: 2 }}>
            Query History
          </Typography>
          <Divider sx={{ mb: 2 }} />
          {history && history.length > 0 ? (
            <List>
              {history.map((item) => (
                <ListItem key={item.id} disablePadding>
                  <ListItemButton onClick={() => handleHistoryClick(item.query)}>
                    <ListItemText
                      primary={item.query}
                      secondary={
                        <Box
                          component="span"
                          sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
                        >
                          {item.success ? (
                            <CheckCircleIcon color="success" sx={{ fontSize: 16 }} />
                          ) : (
                            <ErrorIcon color="error" sx={{ fontSize: 16 }} />
                          )}
                          <span>
                            {item.resultCount} results -{' '}
                            {new Date(item.executedAt).toLocaleDateString()}
                          </span>
                        </Box>
                      }
                      primaryTypographyProps={{
                        noWrap: true,
                        sx: { maxWidth: 350 },
                      }}
                    />
                  </ListItemButton>
                </ListItem>
              ))}
            </List>
          ) : (
            <Typography color="text.secondary">No query history yet</Typography>
          )}
        </Box>
      </Drawer>
    </Box>
  );
}

interface ResultsDisplayProps {
  result: NlpQueryResponse;
  showSql: boolean;
  onToggleSql: () => void;
  onExport: () => void;
  isExporting: boolean;
  page: number;
  rowsPerPage: number;
  onPageChange: (event: unknown, newPage: number) => void;
  onRowsPerPageChange: (event: React.ChangeEvent<HTMLInputElement>) => void;
}

function ResultsDisplay({
  result,
  showSql,
  onToggleSql,
  onExport,
  isExporting,
  page,
  rowsPerPage,
  onPageChange,
  onRowsPerPageChange,
}: ResultsDisplayProps) {
  if (result.clarificationNeeded) {
    return (
      <Alert severity="info" icon={<LightbulbIcon />}>
        <Typography variant="body1">{result.clarificationMessage}</Typography>
      </Alert>
    );
  }

  if (result.error) {
    return (
      <Alert severity="error">
        <Typography variant="body1">{result.error}</Typography>
      </Alert>
    );
  }

  if (!result.success) {
    return (
      <Alert severity="warning">
        <Typography variant="body1">Unable to process your query. Please try rephrasing.</Typography>
      </Alert>
    );
  }

  const paginatedResults = result.results.slice(
    page * rowsPerPage,
    page * rowsPerPage + rowsPerPage
  );

  return (
    <Card>
      <CardContent>
        {/* Header with metadata */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
          <Box>
            <Typography variant="h6">Results</Typography>
            <Typography variant="body2" color="text.secondary">
              {result.explanation}
            </Typography>
            <Box sx={{ display: 'flex', gap: 1, mt: 1 }}>
              <Chip
                label={`${result.totalRows} rows`}
                size="small"
                color="primary"
                variant="outlined"
              />
              <Chip
                label={`${result.executionTimeMs}ms`}
                size="small"
                variant="outlined"
              />
              {result.wasTruncated && (
                <Chip
                  label="Results truncated"
                  size="small"
                  color="warning"
                  variant="outlined"
                />
              )}
            </Box>
          </Box>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Tooltip title="Export to CSV">
              <Button
                variant="outlined"
                size="small"
                onClick={onExport}
                disabled={isExporting}
                startIcon={isExporting ? <CircularProgress size={16} /> : <DownloadIcon />}
              >
                Export
              </Button>
            </Tooltip>
            <Tooltip title={showSql ? 'Hide SQL' : 'Show SQL'}>
              <IconButton onClick={onToggleSql} size="small">
                <CodeIcon />
              </IconButton>
            </Tooltip>
          </Box>
        </Box>

        {/* Warnings */}
        {result.warnings.length > 0 && (
          <Box sx={{ mb: 2 }}>
            {result.warnings.map((warning, index) => (
              <Alert key={index} severity="warning" sx={{ mb: 1 }} icon={<WarningIcon />}>
                {warning}
              </Alert>
            ))}
          </Box>
        )}

        {/* SQL Preview */}
        <Collapse in={showSql}>
          <Paper
            sx={{
              p: 2,
              mb: 2,
              bgcolor: 'grey.100',
              fontFamily: 'monospace',
              fontSize: '0.875rem',
              overflow: 'auto',
              maxHeight: 200,
            }}
          >
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
              <Typography variant="caption" color="text.secondary">
                Generated SQL
              </Typography>
              <IconButton size="small" onClick={onToggleSql}>
                <ExpandLessIcon />
              </IconButton>
            </Box>
            <pre style={{ margin: 0, whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
              {result.generatedSql}
            </pre>
          </Paper>
        </Collapse>

        {/* Data Grid */}
        {result.results.length > 0 ? (
          <>
            <TableContainer component={Paper} variant="outlined" sx={{ maxHeight: 500 }}>
              <Table stickyHeader size="small">
                <TableHead>
                  <TableRow>
                    {result.columns.map((column) => (
                      <TableCell key={column} sx={{ fontWeight: 'bold', bgcolor: 'grey.100' }}>
                        {column}
                      </TableCell>
                    ))}
                  </TableRow>
                </TableHead>
                <TableBody>
                  {paginatedResults.map((row, rowIndex) => (
                    <TableRow key={rowIndex} hover>
                      {result.columns.map((column) => (
                        <TableCell key={column}>
                          {formatCellValue(row[column])}
                        </TableCell>
                      ))}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
            <TablePagination
              rowsPerPageOptions={[10, 25, 50, 100]}
              component="div"
              count={result.results.length}
              rowsPerPage={rowsPerPage}
              page={page}
              onPageChange={onPageChange}
              onRowsPerPageChange={onRowsPerPageChange}
            />
          </>
        ) : (
          <Typography color="text.secondary" sx={{ textAlign: 'center', py: 4 }}>
            No results found
          </Typography>
        )}
      </CardContent>
    </Card>
  );
}

function formatCellValue(value: unknown): string {
  if (value === null || value === undefined) {
    return '-';
  }
  if (typeof value === 'number') {
    // Format numbers with commas for readability
    return value.toLocaleString();
  }
  if (typeof value === 'boolean') {
    return value ? 'Yes' : 'No';
  }
  if (value instanceof Date) {
    return value.toLocaleDateString();
  }
  return String(value);
}
