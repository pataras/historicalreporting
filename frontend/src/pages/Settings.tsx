import {
  Box,
  Card,
  CardContent,
  Typography,
  List,
  ListItem,
  ListItemText,
  Divider,
} from '@mui/material';
import { useHealth } from '../hooks/useHealth';

export function Settings() {
  const { data: health } = useHealth();

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Settings
      </Typography>
      <Typography variant="body1" color="textSecondary" paragraph>
        Application configuration and system information.
      </Typography>

      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            System Information
          </Typography>
          <List>
            <ListItem>
              <ListItemText
                primary="API Status"
                secondary={health?.status || 'Unknown'}
              />
            </ListItem>
            <Divider />
            <ListItem>
              <ListItemText
                primary="API Version"
                secondary={health?.version || 'Unknown'}
              />
            </ListItem>
            <Divider />
            <ListItem>
              <ListItemText
                primary="Last Health Check"
                secondary={health?.timestamp ? new Date(health.timestamp).toLocaleString() : 'Never'}
              />
            </ListItem>
          </List>
        </CardContent>
      </Card>
    </Box>
  );
}
