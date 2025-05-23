import React, { useState } from 'react';
import {
  Container,
  TextField,
  Button,
  Paper,
  Typography,
  Box,
  CircularProgress,
  Alert,
  Card,
  CardContent,
  Divider
} from '@mui/material';

const SAMPLE_TRANSCRIPT = `Client: Hi, I'm interested in the property at 123 Main St.
Agent: Great! That's a beautiful 3-bedroom, 2-bath home in a great neighborhood.
Client: Yes, I saw it has an open house this weekend. What time is that?
Agent: The open house is on Saturday from 1-4 PM. Would you like me to schedule a private showing for you?
Client: That would be great! How about Sunday at 2 PM?
Agent: I'll check the schedule and get back to you shortly. Could I have your email address to send the confirmation?`;

const EmmaDemo = () => {
  const [message, setMessage] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [response, setResponse] = useState(null);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!message.trim()) {
      setError('Please enter a message');
      return;
    }

    setIsLoading(true);
    setError('');
    setResponse(null);

    try {
      // Call the API endpoint that processes the message using a relative path
      const response = await fetch(`/api/dataentry/process-demo`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          content: message,
          // Include any other required fields with default values
          organizationId: '00000000-0000-0000-0000-000000000000',
          agentId: '00000000-0000-0000-0000-000000000001',
          clientFirstName: 'Demo',
          clientLastName: 'User',
          messageType: 'Text',
          newConversation: true
        }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.error || `HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      setResponse(data);
    } catch (err) {
      console.error('Error processing message:', err);
      setError(err.message || 'Failed to process message. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleUseSample = () => {
    setMessage(SAMPLE_TRANSCRIPT);
    setError('');
  };

  return (
    <Container maxWidth="md" sx={{ mt: 4, mb: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        EMMA Demo
      </Typography>
      
      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" gutterBottom>
          Enter a conversation or message to analyze:
        </Typography>
        
        <Box component="form" onSubmit={handleSubmit}>
          <TextField
            fullWidth
            multiline
            rows={8}
            variant="outlined"
            placeholder="Paste a conversation, email, or notes here..."
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            disabled={isLoading}
            sx={{ mb: 2 }}
          />
          
          <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
            <Button
              type="submit"
              variant="contained"
              color="primary"
              disabled={isLoading || !message.trim()}
              startIcon={isLoading ? <CircularProgress size={20} /> : null}
            >
              {isLoading ? 'Analyzing...' : 'Analyze with EMMA'}
            </Button>
            
            <Button
              variant="outlined"
              onClick={handleUseSample}
              disabled={isLoading || message === SAMPLE_TRANSCRIPT}
            >
              Try Sample Transcript
            </Button>
          </Box>
          
          {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
        </Box>
      </Paper>
      
      {response && (
        <Paper sx={{ p: 3, mb: 3 }}>
          <Typography variant="h6" gutterBottom>
            EMMA's Analysis
          </Typography>
          
          <Card variant="outlined" sx={{ mb: 2 }}>
            <CardContent>
              <Typography variant="subtitle1" color="text.secondary" gutterBottom>
                Recommended Action
              </Typography>
              <Typography variant="h5" component="div" sx={{ mb: 1.5, color: 'primary.main' }}>
                {response.action?.action || 'No action'}
              </Typography>
              
              {response.action?.payload && (
                <>
                  <Divider sx={{ my: 2 }} />
                  <Typography variant="subtitle1" color="text.secondary" gutterBottom>
                    Suggested Response
                  </Typography>
                  <Typography variant="body1" sx={{ whiteSpace: 'pre-line' }}>
                    {response.action.payload}
                  </Typography>
                </>
              )}
            </CardContent>
          </Card>
          
          <Box sx={{ mt: 2 }}>
            <Typography variant="caption" color="text.secondary">
              <strong>Correlation ID:</strong> {response.correlationId}
            </Typography>
          </Box>
          
          <Box sx={{ mt: 2 }}>
            <details>
              <summary>View Raw Response</summary>
              <pre style={{
                backgroundColor: '#f5f5f5',
                padding: '10px',
                borderRadius: '4px',
                maxHeight: '200px',
                overflow: 'auto',
                marginTop: '10px'
              }}>
                {JSON.stringify(response, null, 2)}
              </pre>
            </details>
          </Box>
        </Paper>
      )}
    </Container>
  );
};

export default EmmaDemo;
